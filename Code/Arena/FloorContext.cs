using System;

/// <summary>Which faces of a <see cref="FloorContext.WallBox"/> to build; the hidden ones cost nothing if left out.</summary>
[Flags]
public enum BoxFaces
{
	Front = 1,
	Top = 2,
	Bottom = 4,
	Ends = 8,
	Back = 16,
}

/// <summary>
/// What a theme needs to know about the room it dresses, and the wall-space helpers it builds with.
///
/// Wall space: a side 0..3 (+X, +Y, −X, −Y), u along the wall from its left end as seen from inside
/// (0..1680 over the grid edge), v up from the tile top, depth into the wall from the grid edge
/// (0 = the collider's inner face, <see cref="Recess"/> = the wall face, <see cref="WallThickness"/> =
/// the facade). Everything a theme adds lands in <see cref="Kit"/>, the floor's one model.
/// </summary>
public sealed class FloorContext
{
	public int Floor { get; init; }

	/// <summary>Local z of this floor's walkable surface.</summary>
	public float TileTopZ { get; init; }

	/// <summary>Local z of the underside above (TileTopZ + <see cref="ClearHeight"/>); the roof base for floor 0.</summary>
	public float CeilingZ { get; init; }

	/// <summary>Local z where this floor's wall segment starts: the tile underside, or the lava top for the last floor.</summary>
	public float BottomZ { get; init; }

	/// <summary>This floor's batch; everything the theme adds lands in one Model.</summary>
	public MeshKit Kit { get; init; }

	public ArenaTheme Theme { get; init; }

	/// <summary>Parent of what cannot be batched into <see cref="Kit"/> — lights. Sits at the arena's origin.</summary>
	public GameObject Root { get; init; }

	public const float HalfSpan = Arena.GridSize * Arena.TileSize * 0.5f; // 840
	public const float Span = HalfSpan * 2f;
	public const float WallThickness = 64f;
	public const float ClearHeight = Arena.FloorGap - Arena.TileThickness; // 268

	/// <summary>
	/// Depth of the wall face behind the front layer. Posts, mouldings and (later) alcove props live in
	/// the 40 units between it and the collider plane, so nothing pokes into the play volume.
	/// </summary>
	public const float Recess = 40f;

	/// <summary>
	/// How far everything that meets the wall face runs on behind it. Faces that only touch the big
	/// wall quad meet it along T-junctions, and a T-junction rasterises with pixel cracks that show
	/// the sky behind the tower; running a piece on into the wall puts more of itself behind any crack.
	/// </summary>
	public const float Embed = 2f;

	/// <summary>v of the slab edge's top: a step below the tiles, so the ledge in each bay never reads as more floor.</summary>
	public const float LedgeTop = -6f;

	/// <summary>Lights per floor (spec budget: ≤ 48 for the tower, none casting shadows).</summary>
	public const int MaxLights = 6;

	/// <summary>
	/// Softer than the default inverse-square falloff (1): with a short radius — kept short of the
	/// floor below, since the lights cast no shadows — a lamp's pool would otherwise end in a hot spot
	/// by the wall and leave the floor in front of it unlit.
	/// </summary>
	const float LampFalloff = 0.8f;

	/// <summary>
	/// Outer half-angle, in degrees, of a lamp's cone aimed straight down: wide enough to wash the
	/// walls beside and below the lamp, and short of the horizontal, so nothing level with the lamp
	/// or above it is lit. The lights cast no shadows, so a slab stops nothing: a lamp under the
	/// cornice is about fifty units from the wall feet of the room upstairs, and as a point light it
	/// lit them brighter than anything in its own room (6b review).
	/// </summary>
	public const float LampCone = 80f;

	/// <summary>
	/// Degrees over which a cone fades from full strength to nothing at its edge: wide, because a
	/// lamp 64 units from the wall face draws the edge of its cone on the wall as an arc, and a short
	/// fade drew it as the hard rim of a stage spot.
	/// </summary>
	const float ConeFade = 55f;

	/// <summary>How far short of the next floor's tile tops a lamp's reach stops.</summary>
	const float LeakMargin = 12f;

	/// <summary>
	/// The furthest a lamp <paramref name="v"/> above this floor may reach: short of the tile tops one
	/// <see cref="Arena.FloorGap"/> below, which the unshadowed light would otherwise light through the slab.
	/// </summary>
	public static float LampReach( float v ) => v + Arena.FloorGap - LeakMargin;

	int _lights;

	/// <summary>Into the wall (+depth), exact per side so faces on the grid edge land on exactly ±840.</summary>
	public static Vector3 Out( int side ) => side switch
	{
		0 => new Vector3( 1f, 0f, 0f ), 1 => new Vector3( 0f, 1f, 0f ), 2 => new Vector3( -1f, 0f, 0f ), _ => new Vector3( 0f, -1f, 0f ),
	};

	/// <summary>Along the wall (+u): to the right, seen from inside facing the wall.</summary>
	public static Vector3 Along( int side ) => side switch
	{
		0 => new Vector3( 0f, -1f, 0f ), 1 => new Vector3( 1f, 0f, 0f ), 2 => new Vector3( 0f, 1f, 0f ), _ => new Vector3( -1f, 0f, 0f ),
	};

	/// <summary>Wall-space → room-local (the arena's local space).</summary>
	public Vector3 OnWall( int side, float u, float v, float depth )
		=> Out( side ) * (HalfSpan + depth) + Along( side ) * (u - HalfSpan) + Vector3.Up * (TileTopZ + v);

	/// <summary>Rotation whose Forward points out of the wall into the room, Up = world up.</summary>
	public Rotation WallFacing( int side ) => Rotation.LookAt( -Out( side ), Vector3.Up );

	/// <summary>
	/// An unshadowed lamp (shadow-casting lights are the expensive kind, and a room has four to six).
	/// With a <paramref name="cone"/> — the outer half-angle in degrees — it is a spot light aimed along
	/// <paramref name="direction"/>, fading in over its last <see cref="ConeFade"/> degrees; without
	/// one, a point light.
	///
	/// Nothing here casts shadows, so the slabs between floors stop no light: a lamp lights whatever
	/// lies inside its radius (and its cone) on every floor. Wall lamps are therefore cones aimed below
	/// the horizontal (<see cref="LampCone"/>) with a radius short of the floor below
	/// (<see cref="LampReach"/>), and a point light belongs only where its radius stays short of both
	/// slabs. Warns past <see cref="MaxLights"/> so a theme that overspends shows up in the console.
	/// </summary>
	public void AddLight( Vector3 localPosition, Color color, float radius, Vector3 direction = default, float cone = 0f )
	{
		if ( ++_lights > MaxLights )
			Log.Warning( $"Floor {Floor}: {_lights} lights, over the budget of {MaxLights}" );

		var holder = new GameObject( Root, true, "Light" );
		holder.LocalPosition = localPosition;
		ArenaGenerator.MakeLocalOnly( holder );

		if ( cone > 0f )
		{
			// A spot shines along its forward axis. LookAt's default up is world up, which leaves the
			// rotation undefined for a lamp aimed straight down or up, so those take +X as up instead.
			holder.LocalRotation = Rotation.LookAt( direction, MathF.Abs( direction.z ) > 0.9f ? Vector3.Forward : Vector3.Up );
			var spot = holder.AddComponent<SpotLight>();
			spot.LightColor = color;
			spot.Radius = radius;
			spot.Attenuation = LampFalloff;
			spot.ConeOuter = cone;
			spot.ConeInner = MathF.Max( cone - ConeFade, 0f );
			spot.Shadows = false;
			return;
		}

		var light = holder.AddComponent<PointLight>();
		light.LightColor = color;
		light.Radius = radius;
		light.Attenuation = LampFalloff;
		light.Shadows = false;
	}

	/// <summary>
	/// A box in wall space wearing two bands of a trim atlas: <paramref name="frontBand"/> on the face
	/// toward the room (and the back), <paramref name="sideBand"/> on top, bottom and ends. The band
	/// tiles along the box's run — u for a horizontal moulding, v for a post (<paramref name="vertical"/>)
	/// — at <see cref="ArenaTheme.TrimRepeat"/>, continuing from the run's wall coordinate, and fits
	/// across. Every face's UVs keep the texture's handedness, so the painted profile is lit the right way.
	/// </summary>
	public void WallBox( int side, Material material, Rect frontBand, Rect sideBand, bool vertical,
		float u0, float u1, float v0, float v1, float d0, float d1, BoxFaces faces )
	{
		var along = Along( side );
		var up = Vector3.Up;
		var into = Out( side );
		var width = u1 - u0; var height = v1 - v0; var depth = d1 - d0;

		if ( faces.HasFlag( BoxFaces.Front ) )
		{
			if ( vertical ) BandFace( material, frontBand, OnWall( side, u0, v0, d0 ), up, height, v0, along, width, -into );
			else BandFace( material, frontBand, OnWall( side, u0, v0, d0 ), along, width, u0, up, height, -into );
		}

		if ( faces.HasFlag( BoxFaces.Back ) )
		{
			if ( vertical ) BandFace( material, frontBand, OnWall( side, u0, v0, d1 ), up, height, v0, along, width, into );
			else BandFace( material, frontBand, OnWall( side, u0, v0, d1 ), along, width, u0, up, height, into );
		}

		if ( faces.HasFlag( BoxFaces.Top ) )
			BandFace( material, sideBand, OnWall( side, u0, v1, d0 ), along, width, u0, into, depth, up );

		if ( faces.HasFlag( BoxFaces.Bottom ) )
			BandFace( material, sideBand, OnWall( side, u0, v0, d0 ), along, width, u0, into, depth, -up );

		if ( faces.HasFlag( BoxFaces.Ends ) )
		{
			if ( vertical )
			{
				BandFace( material, sideBand, OnWall( side, u0, v0, d0 ), up, height, v0, into, depth, -along );
				BandFace( material, sideBand, OnWall( side, u1, v0, d0 ), up, height, v0, into, depth, along );
			}
			else
			{
				BandFace( material, sideBand, OnWall( side, u0, v0, d0 ), into, depth, d0, up, height, -along );
				BandFace( material, sideBand, OnWall( side, u1, v0, d0 ), into, depth, d0, up, height, along );
			}
		}
	}

	/// <summary>
	/// A face spanning <paramref name="origin"/> + a·<paramref name="run"/> + b·<paramref name="across"/>
	/// that tiles <paramref name="band"/> along the run (starting at <paramref name="runStart"/> world
	/// units) and fits it across, wound to face <paramref name="normal"/>. The band's top row goes on
	/// whichever edge keeps the mapping unmirrored.
	/// </summary>
	public void BandFace( Material material, Rect band, Vector3 origin, Vector3 run, float runLength, float runStart, Vector3 across, float acrossLength, Vector3 normal )
	{
		var upright = Vector3.Dot( Vector3.Cross( run, across ), normal ) > 0f;
		Face( material, origin, run, runLength, across, acrossLength, normal, ( a, b ) =>
		{
			var t = upright ? 1f - b / acrossLength : b / acrossLength;
			return new Vector2( band.Left + (runStart + a) / ArenaTheme.TrimRepeat * band.Width, band.Top + t * band.Height );
		} );
	}

	/// <summary>
	/// A face on the wall plane at <paramref name="depth"/>, from u0..u1 and v0..v1, facing the room or
	/// (<paramref name="outward"/>) the outside, with UVs from a function of the wall coordinates (u, v).
	/// The caller keeps the mapping unmirrored: facing the room texture right is +u, facing out it is −u.
	/// </summary>
	public void WallFace( int side, Material material, float u0, float u1, float v0, float v1, float depth, bool outward, Func<float, float, Vector2> uv )
	{
		var into = Out( side );
		Face( material, OnWall( side, u0, v0, depth ), Along( side ), u1 - u0, Vector3.Up, v1 - v0, outward ? into : -into,
			( a, b ) => uv( u0 + a, v0 + b ) );
	}

	/// <summary>
	/// A whole picture (an atlas cell) fitted to one rectangle on the wall plane, upright, seen from
	/// the room or from <paramref name="outward"/> — a window pane, a blind, a sign board.
	/// </summary>
	public void WallPicture( int side, Material material, Rect cell, float u0, float u1, float v0, float v1, float depth, bool outward )
	{
		WallFace( side, material, u0, u1, v0, v1, depth, outward, ( u, v ) =>
		{
			var x = (u - u0) / (u1 - u0);
			if ( outward ) x = 1f - x;
			return new Vector2( cell.Left + x * cell.Width, cell.Top + (v1 - v) / (v1 - v0) * cell.Height );
		} );
	}

	/// <summary>One quad from an origin and two axes, wound to face <paramref name="normal"/>, UV from a function of (a, b).</summary>
	void Face( Material material, Vector3 origin, Vector3 axisA, float lengthA, Vector3 axisB, float lengthB, Vector3 normal, Func<float, float, Vector2> uv )
	{
		var p00 = origin;
		var p10 = origin + axisA * lengthA;
		var p11 = p10 + axisB * lengthB;
		var p01 = origin + axisB * lengthB;

		if ( Vector3.Dot( Vector3.Cross( axisA, axisB ), normal ) > 0f )
			Kit.Quad( material, p00, p10, p11, p01, uv( 0f, 0f ), uv( lengthA, 0f ), uv( lengthA, lengthB ), uv( 0f, lengthB ) );
		else
			Kit.Quad( material, p00, p01, p11, p10, uv( 0f, 0f ), uv( 0f, lengthB ), uv( lengthA, lengthB ), uv( lengthA, 0f ) );
	}
}
