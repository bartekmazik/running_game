using System;
using System.Collections.Generic;

/// <summary>How a primitive's faces land on a material: tiled at a world scale, or fitted into an atlas cell.</summary>
public readonly struct Skin
{
	public readonly Material Material;

	/// <summary>World units per texture repeat. Zero means "fit": each face spans <see cref="Region"/> once.</summary>
	public readonly float Repeat;

	public readonly Rect Region;

	public Skin( Material material, float repeat )
	{
		Material = material;
		Repeat = repeat;
		Region = new Rect( 0f, 0f, 1f, 1f );
	}

	public Skin( Material material, Rect region )
	{
		Material = material;
		Repeat = 0f;
		Region = region;
	}
}

/// <summary>
/// Builds procedural geometry into one batch per material, then into a single Model — a whole room's
/// walls and props become a handful of draw calls. Flat-shaded on purpose: faceted props suit the
/// flat-colour look and keep the normals honest on bevels.
/// </summary>
public sealed class MeshKit
{
	/// <summary>Spike finding 1: the engine front-faces anticlockwise (seen from the front), so no flip.</summary>
	const bool FlipWinding = false;

	sealed class Batch
	{
		public readonly List<Vertex> Vertices = new();
		public readonly List<int> Indices = new();
	}

	readonly Dictionary<Material, Batch> _batches = new();
	BBox _bounds;
	bool _hasBounds;

	public bool IsEmpty => _batches.Count == 0;

	/// <summary>Triangles in every batch so far: what the model will cost to draw.</summary>
	public int Triangles
	{
		get
		{
			var count = 0;
			foreach ( var batch in _batches.Values ) count += batch.Indices.Count / 3;
			return count;
		}
	}

	Batch For( Material material )
	{
		if ( !_batches.TryGetValue( material, out var batch ) )
			_batches[material] = batch = new Batch();
		return batch;
	}

	void Grow( Vector3 p )
	{
		_bounds = _hasBounds ? _bounds.AddPoint( p ) : new BBox( p, p );
		_hasBounds = true;
	}

	/// <summary>
	/// Corners a→b→c→d run anticlockwise seen from the front. UV: U runs along a→b, V grows downward (texture top = face up).
	/// On horizontal faces that puts texture-top toward world +Y and texture-left toward world -X; wall faces put texture-top up.
	/// </summary>
	public void Quad( Skin skin, Vector3 a, Vector3 b, Vector3 c, Vector3 d )
	{
		var uAxis = (b - a).Normal;
		var normal = Vector3.Cross( b - a, d - a ).Normal;
		var vAxis = Vector3.Cross( normal, uAxis ); // up the face

		Vector4 Uv( Vector3 p, float fu, float fv )
		{
			if ( skin.Repeat > 0f )
				return new Vector4( Vector3.Dot( p, uAxis ) / skin.Repeat, -Vector3.Dot( p, vAxis ) / skin.Repeat, 0f, 0f );

			var r = skin.Region;
			return new Vector4( r.Left + fu * r.Width, r.Top + (1f - fv) * r.Height, 0f, 0f );
		}

		var batch = For( skin.Material );
		var start = batch.Vertices.Count;
		batch.Vertices.Add( new Vertex( a, normal, uAxis, Uv( a, 0f, 0f ) ) );
		batch.Vertices.Add( new Vertex( b, normal, uAxis, Uv( b, 1f, 0f ) ) );
		batch.Vertices.Add( new Vertex( c, normal, uAxis, Uv( c, 1f, 1f ) ) );
		batch.Vertices.Add( new Vertex( d, normal, uAxis, Uv( d, 0f, 1f ) ) );
		AddTriangle( batch, start, start + 1, start + 2 );
		AddTriangle( batch, start, start + 2, start + 3 );

		Grow( a ); Grow( b ); Grow( c ); Grow( d );
	}

	/// <summary>
	/// A quad with explicit UVs, for faces whose texture has to line up with a neighbour's — a chamfer
	/// that carries its top face's texture on over the edge, unfolded. Corners run anticlockwise seen
	/// from the front. The tangent (+U direction) is taken from the UV layout rather than the first
	/// edge, so the normal map is lit from the right side however the UVs are turned. Give UVs whose
	/// layout matches the face's shape (an unfolding), and the two triangles share one mapping — no kink.
	/// <paramref name="shading"/>, when given, is the normal the face is lit with instead of its own
	/// (the winding, which decides which side is drawn, still follows the corners).
	/// </summary>
	public void Quad( Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD, Vector3? shading = null )
	{
		var normal = shading ?? Vector3.Cross( b - a, d - a ).Normal;
		var e1 = b - a; var e2 = d - a;
		var du1 = uvB.x - uvA.x; var dv1 = uvB.y - uvA.y;
		var du2 = uvD.x - uvA.x; var dv2 = uvD.y - uvA.y;
		var determinant = du1 * dv2 - du2 * dv1;
		var tangent = ((e1 * dv2 - e2 * dv1) / determinant).Normal;

		// The bitangent's sign: the shader rebuilds +V as the normal × tangent times the tangent's w,
		// which the engine's vertex sets to −1 — right for every face mapped the texture's own way round.
		// A face whose UVs are mirrored (a wall length wearing its pattern flipped) has +V on the other
		// side of that cross product, and with w left at −1 its normal map's vertical slopes would flip.
		var bitangent = (e2 * du1 - e1 * du2) / determinant;
		var handedness = Vector3.Dot( Vector3.Cross( Vector3.Cross( b - a, d - a ), tangent ), bitangent ) > 0f ? 1f : -1f;

		var batch = For( material );
		var start = batch.Vertices.Count;
		batch.Vertices.Add( Corner( a, normal, tangent, handedness, uvA ) );
		batch.Vertices.Add( Corner( b, normal, tangent, handedness, uvB ) );
		batch.Vertices.Add( Corner( c, normal, tangent, handedness, uvC ) );
		batch.Vertices.Add( Corner( d, normal, tangent, handedness, uvD ) );
		AddTriangle( batch, start, start + 1, start + 2 );
		AddTriangle( batch, start, start + 2, start + 3 );

		Grow( a ); Grow( b ); Grow( c ); Grow( d );
	}

	/// <summary>A vertex with its tangent's handedness set (see <see cref="Quad(Material, Vector3, Vector3, Vector3, Vector3, Vector2, Vector2, Vector2, Vector2, Vector3?)"/>).</summary>
	static Vertex Corner( Vector3 position, Vector3 normal, Vector3 tangent, float handedness, Vector2 uv )
	{
		var vertex = new Vertex( position, normal, tangent, new Vector4( uv.x, uv.y, 0f, 0f ) );
		vertex.Tangent = new Vector4( tangent.x, tangent.y, tangent.z, handedness );
		return vertex;
	}

	public void Tri( Skin skin, Vector3 a, Vector3 b, Vector3 c )
	{
		var uAxis = (b - a).Normal;
		var normal = Vector3.Cross( b - a, c - a ).Normal;
		var vAxis = Vector3.Cross( normal, uAxis );

		Vector4 Uv( Vector3 p, float fu, float fv )
		{
			if ( skin.Repeat > 0f )
				return new Vector4( Vector3.Dot( p, uAxis ) / skin.Repeat, -Vector3.Dot( p, vAxis ) / skin.Repeat, 0f, 0f );
			var r = skin.Region;
			return new Vector4( r.Left + fu * r.Width, r.Top + (1f - fv) * r.Height, 0f, 0f );
		}

		var batch = For( skin.Material );
		var start = batch.Vertices.Count;
		batch.Vertices.Add( new Vertex( a, normal, uAxis, Uv( a, 0f, 0f ) ) );
		batch.Vertices.Add( new Vertex( b, normal, uAxis, Uv( b, 1f, 0f ) ) );
		batch.Vertices.Add( new Vertex( c, normal, uAxis, Uv( c, 0.5f, 1f ) ) );
		AddTriangle( batch, start, start + 1, start + 2 );

		Grow( a ); Grow( b ); Grow( c );
	}

	static void AddTriangle( Batch batch, int a, int b, int c )
	{
		batch.Indices.Add( a );
		batch.Indices.Add( FlipWinding ? c : b );
		batch.Indices.Add( FlipWinding ? b : c );
	}

	/// <summary>A quad whose winding is fixed to face <paramref name="outward"/>, whatever order the corners came in.</summary>
	void QuadFacing( Skin skin, Vector3 outward, Vector3 a, Vector3 b, Vector3 c, Vector3 d )
	{
		if ( Vector3.Dot( Vector3.Cross( b - a, d - a ), outward ) >= 0f ) Quad( skin, a, b, c, d );
		else Quad( skin, d, c, b, a );
	}

	void TriFacing( Skin skin, Vector3 outward, Vector3 a, Vector3 b, Vector3 c )
	{
		if ( Vector3.Dot( Vector3.Cross( b - a, c - a ), outward ) >= 0f ) Tri( skin, a, b, c );
		else Tri( skin, a, c, b );
	}

	public void Box( Skin skin, Vector3 center, Vector3 size ) => Box( skin, center, size, Rotation.Identity );

	public void Box( Skin skin, Vector3 center, Vector3 size, Rotation rotation ) => BevelBox( skin, center, size, 0f, rotation );

	public void BevelBox( Skin skin, Vector3 center, Vector3 size, float bevel ) => BevelBox( skin, center, size, bevel, Rotation.Identity );

	/// <summary>
	/// A box with every edge chamfered by <paramref name="bevel"/>: six inset faces, twelve edge strips,
	/// eight corner triangles. The chamfers catch light, which is what makes a generated block read
	/// as an object rather than a grey cut-out. Zero bevel gives a plain six-face box.
	/// </summary>
	public void BevelBox( Skin skin, Vector3 center, Vector3 size, float bevel, Rotation rotation )
	{
		var h = size * 0.5f;
		bevel = MathF.Min( bevel, MathF.Min( h.x, MathF.Min( h.y, h.z ) ) * 0.9f );
		var i = new Vector3( h.x - bevel, h.y - bevel, h.z - bevel );

		Vector3 P( float x, float y, float z ) => center + rotation * new Vector3( x, y, z );
		Vector3 Out( float x, float y, float z ) => rotation * new Vector3( x, y, z );

		for ( var s = -1; s <= 1; s += 2 )
		{
			QuadFacing( skin, Out( s, 0, 0 ), P( s * h.x, -i.y, -i.z ), P( s * h.x, i.y, -i.z ), P( s * h.x, i.y, i.z ), P( s * h.x, -i.y, i.z ) );
			QuadFacing( skin, Out( 0, s, 0 ), P( -i.x, s * h.y, -i.z ), P( i.x, s * h.y, -i.z ), P( i.x, s * h.y, i.z ), P( -i.x, s * h.y, i.z ) );
			QuadFacing( skin, Out( 0, 0, s ), P( -i.x, -i.y, s * h.z ), P( i.x, -i.y, s * h.z ), P( i.x, i.y, s * h.z ), P( -i.x, i.y, s * h.z ) );
		}

		if ( bevel <= 0f ) return;

		for ( var sa = -1; sa <= 1; sa += 2 )
		for ( var sb = -1; sb <= 1; sb += 2 )
		{
			// Edges along Z (between the X and Y faces), along Y (X and Z), along X (Y and Z).
			QuadFacing( skin, Out( sa, sb, 0 ),
				P( sa * h.x, sb * i.y, -i.z ), P( sa * h.x, sb * i.y, i.z ), P( sa * i.x, sb * h.y, i.z ), P( sa * i.x, sb * h.y, -i.z ) );
			QuadFacing( skin, Out( sa, 0, sb ),
				P( sa * h.x, -i.y, sb * i.z ), P( sa * h.x, i.y, sb * i.z ), P( sa * i.x, i.y, sb * h.z ), P( sa * i.x, -i.y, sb * h.z ) );
			QuadFacing( skin, Out( 0, sa, sb ),
				P( -i.x, sa * h.y, sb * i.z ), P( i.x, sa * h.y, sb * i.z ), P( i.x, sa * i.y, sb * h.z ), P( -i.x, sa * i.y, sb * h.z ) );

			for ( var sc = -1; sc <= 1; sc += 2 )
				TriFacing( skin, Out( sa, sb, sc ), P( sa * h.x, sb * i.y, sc * i.z ), P( sa * i.x, sb * h.y, sc * i.z ), P( sa * i.x, sb * i.y, sc * h.z ) );
		}
	}

	public void Lathe( Skin skin, Vector3 baseCenter, Vector2[] profile, int sides ) => Lathe( skin, baseCenter, profile, sides, Rotation.Identity );

	/// <summary>
	/// Spins a (radius, height) profile around the local Z axis — pots, lamps, pillars, bottles, cones.
	/// Faceted: each segment is its own quad, so a 10-sided pot reads as a crafted object.
	/// Profile order matters: bottom to top. Listed top to bottom, the solid turns inside out (faces wind inward).
	/// Caps keep explicit facing (bottom -up, top +up) since a fan's winding depends on which end it closes.
	/// </summary>
	public void Lathe( Skin skin, Vector3 baseCenter, Vector2[] profile, int sides, Rotation rotation, bool capBottom = true, bool capTop = true )
	{
		Vector3 Ring( int step, Vector2 rz )
		{
			var angle = step * MathF.Tau / sides;
			return baseCenter + rotation * new Vector3( MathF.Cos( angle ) * rz.x, MathF.Sin( angle ) * rz.x, rz.y );
		}

		for ( var p = 0; p < profile.Length - 1; p++ )
		for ( var s = 0; s < sides; s++ )
		{
			var a = Ring( s, profile[p] );
			var b = Ring( s + 1, profile[p] );
			var c = Ring( s + 1, profile[p + 1] );
			var d = Ring( s, profile[p + 1] );
			var bottomApex = profile[p].x <= 0.0001f;
			var topApex = profile[p + 1].x <= 0.0001f;

			if ( bottomApex && topApex ) continue;
			// Wound by construction, not by an outward test (a horizontal ledge has no radial component to test).
			// Sweep is anticlockwise about +Z and the profile runs bottom to top, so cross( b - a, d - a ) points outward
			// on a vertical wall, down where the radius grows, up where it shrinks: right for a solid of revolution,
			// and rotation preserves handedness. Apex cases keep the quad's triangles: (a,b,c) when c == d,
			// (a,c,d) when a == b, so their normals equal the quad's.
			if ( topApex ) Tri( skin, a, b, c );
			else if ( bottomApex ) Tri( skin, a, c, d );
			else Quad( skin, a, b, c, d );
		}

		var up = rotation * Vector3.Up;
		if ( capBottom && profile[0].x > 0.0001f )
		{
			var centre = baseCenter + rotation * new Vector3( 0f, 0f, profile[0].y );
			for ( var s = 0; s < sides; s++ )
				TriFacing( skin, -up, centre, Ring( s, profile[0] ), Ring( s + 1, profile[0] ) );
		}

		var last = profile[^1];
		if ( capTop && last.x > 0.0001f )
		{
			var centre = baseCenter + rotation * new Vector3( 0f, 0f, last.y );
			for ( var s = 0; s < sides; s++ )
				TriFacing( skin, up, centre, Ring( s, last ), Ring( s + 1, last ) );
		}
	}

	public void Sphere( Skin skin, Vector3 center, float radius, int rings, int sides )
	{
		var profile = new Vector2[rings + 1];
		for ( var r = 0; r <= rings; r++ )
		{
			var a = MathF.PI * r / rings - MathF.PI * 0.5f;
			profile[r] = new Vector2( MathF.Cos( a ) * radius, MathF.Sin( a ) * radius + radius );
		}
		Lathe( skin, center - Vector3.Up * radius, profile, sides, Rotation.Identity, false, false );
	}

	/// <summary>One vertex with an explicit normal and tangent (+U), for the smooth-shaded primitives below.</summary>
	void Add( Batch batch, Vector3 position, Vector3 normal, Vector3 tangent, Vector2 uv )
	{
		batch.Vertices.Add( new Vertex( position, normal, tangent, new Vector4( uv.x, uv.y, 0f, 0f ) ) );
		Grow( position );
	}

	/// <summary>
	/// A lathe (see <see cref="Lathe(Skin, Vector3, Vector2[], int, Rotation, bool, bool)"/>) shaded smooth —
	/// a pot, a bulb, an icicle, a snowman — where flat facets read as a low-poly model at standing
	/// distance. Normals come from the profile: where two of its segments meet at less than
	/// <paramref name="crease"/> degrees they are averaged, so a body curves smoothly, while a sharper turn
	/// (the step under a pot's rim) keeps its edge. The skin's region wraps once round the solid and once
	/// along the profile (texture top at the profile's end), so a texture runs on round it instead of
	/// repeating on every facet; <paramref name="turn"/> (0..1 of a revolution) starts the sweep elsewhere,
	/// so two lathes of one profile do not show the same side.
	/// </summary>
	public void SmoothLathe( Skin skin, Vector3 baseCenter, Vector2[] profile, int sides, Rotation rotation, bool capBottom = true, bool capTop = true, float crease = 45f, float turn = 0f )
	{
		var batch = For( skin.Material );
		var region = skin.Region;
		var count = profile.Length;
		var along = new float[count];
		for ( var p = 1; p < count; p++ ) along[p] = along[p - 1] + (profile[p] - profile[p - 1]).Length;
		var total = MathF.Max( along[^1], 0.0001f );

		// Each segment's outward normal in the (radius, height) plane: right of the direction of travel,
		// which faces out on a wall climbing the profile, up where the radius shrinks, in where it falls back.
		var segment = new Vector2[count - 1];
		for ( var p = 0; p + 1 < count; p++ )
		{
			var d = profile[p + 1] - profile[p];
			segment[p] = new Vector2( d.y, -d.x ).Normal;
		}

		var smoothBelow = MathF.Cos( crease * MathF.PI / 180f );
		Vector2 Blend( int own, int neighbour )
		{
			if ( neighbour < 0 || neighbour >= segment.Length ) return segment[own];
			var a = segment[own]; var b = segment[neighbour];
			return a.x * b.x + a.y * b.y < smoothBelow ? a : (a + b).Normal;
		}

		Vector3 Radial( int step ) { var angle = (step / (float)sides + turn) * MathF.Tau; return new Vector3( MathF.Cos( angle ), MathF.Sin( angle ), 0f ); }

		// One row of vertices round the solid at profile point p (the seam doubled, for the texture to wrap),
		// shared by the segments above and below it unless a crease gives them different normals.
		int Row( int p, Vector2 normal )
		{
			var start = batch.Vertices.Count;
			for ( var step = 0; step <= sides; step++ )
			{
				var radial = Radial( step );
				var position = baseCenter + rotation * (radial * profile[p].x + Vector3.Up * profile[p].y);
				var tangent = rotation * new Vector3( -radial.y, radial.x, 0f );
				Add( batch, position, (rotation * (radial * normal.x + Vector3.Up * normal.y)).Normal, tangent,
					new Vector2( region.Left + region.Width * step / sides, region.Top + region.Height * (1f - along[p] / total) ) );
			}
			return start;
		}

		var shared = -1;
		var sharedNormal = Vector2.Zero;
		for ( var p = 0; p + 1 < count; p++ )
		{
			var bottomApex = profile[p].x <= 0.0001f;
			var topApex = profile[p + 1].x <= 0.0001f;
			if ( bottomApex && topApex ) { shared = -1; continue; }
			var n0 = Blend( p, p - 1 );
			var n1 = Blend( p, p + 1 );
			var bottom = shared >= 0 && sharedNormal == n0 ? shared : Row( p, n0 );
			var top = Row( p + 1, n1 );
			for ( var s = 0; s < sides; s++ )
			{
				// Same triangles as the flat lathe: an apex quad keeps the one that is not degenerate.
				if ( !bottomApex ) AddTriangle( batch, bottom + s, bottom + s + 1, top + s + 1 );
				if ( !topApex ) AddTriangle( batch, bottom + s, top + s + 1, top + s );
			}
			shared = top;
			sharedNormal = n1;
		}

		if ( capBottom && profile[0].x > 0.0001f ) Cap( batch, region, baseCenter, profile[0], sides, rotation, turn, false );
		if ( capTop && profile[^1].x > 0.0001f ) Cap( batch, region, baseCenter, profile[^1], sides, rotation, turn, true );
	}

	/// <summary>A flat disc closing a smooth lathe, facing up or down, its texture the region's middle.</summary>
	void Cap( Batch batch, Rect region, Vector3 baseCenter, Vector2 rim, int sides, Rotation rotation, float turn, bool top )
	{
		var normal = rotation * (top ? Vector3.Up : Vector3.Down);
		var tangent = rotation * Vector3.Forward;
		var middle = new Vector2( region.Left + region.Width * 0.5f, region.Top + region.Height * 0.5f );
		var centre = baseCenter + rotation * (Vector3.Up * rim.y);
		var start = batch.Vertices.Count;
		Add( batch, centre, normal, tangent, middle );
		for ( var s = 0; s <= sides; s++ )
		{
			var a = (s / (float)sides + turn) * MathF.Tau;
			Add( batch, centre + rotation * new Vector3( MathF.Cos( a ) * rim.x, MathF.Sin( a ) * rim.x, 0f ), normal, tangent,
				middle + new Vector2( MathF.Cos( a ) * region.Width, MathF.Sin( a ) * region.Height ) * 0.45f );
		}

		// A fan round the shared centre. Anticlockwise about the axis faces up: swap the rim pair for a cap facing down.
		for ( var s = 0; s < sides; s++ )
		{
			if ( top ) AddTriangle( batch, start, start + 1 + s, start + 2 + s );
			else AddTriangle( batch, start, start + 2 + s, start + 1 + s );
		}
	}

	/// <summary>
	/// A smooth ellipsoid of half-sizes <paramref name="radii"/> (local x, y, z) turned by
	/// <paramref name="rotation"/>: a flattened sphere — a leaf clump, a heap of snow, a berry, a bulb.
	/// Normals are the true ellipsoid's (the unit sphere's scaled by the inverse radii), so a squashed one
	/// is lit as a flat cushion rather than as a stretched ball. The region wraps once round it.
	/// </summary>
	public void Ellipsoid( Skin skin, Vector3 center, Vector3 radii, Rotation rotation, int rings, int sides )
	{
		var batch = For( skin.Material );
		var region = skin.Region;

		var start = batch.Vertices.Count;
		for ( var ring = 0; ring <= rings; ring++ )
		for ( var step = 0; step <= sides; step++ )
		{
			var latitude = MathF.PI * ring / rings - MathF.PI * 0.5f;
			var longitude = MathF.Tau * step / sides;
			var c = MathF.Cos( latitude );
			var unit = new Vector3( c * MathF.Cos( longitude ), c * MathF.Sin( longitude ), MathF.Sin( latitude ) );
			var position = center + rotation * new Vector3( unit.x * radii.x, unit.y * radii.y, unit.z * radii.z );
			var normal = (rotation * new Vector3( unit.x / radii.x, unit.y / radii.y, unit.z / radii.z )).Normal;
			var tangent = rotation * new Vector3( -MathF.Sin( longitude ), MathF.Cos( longitude ), 0f );
			Add( batch, position, normal, tangent, new Vector2( region.Left + region.Width * step / sides, region.Top + region.Height * (1f - ring / (float)rings) ) );
		}

		// One vertex per (ring, step), shared by the four quads round it; the poles keep a vertex per
		// step, for their tangents and the texture's wrap.
		var row = sides + 1;
		for ( var r = 0; r < rings; r++ )
		for ( var s = 0; s < sides; s++ )
		{
			var a = start + r * row + s;
			if ( r > 0 ) AddTriangle( batch, a, a + 1, a + row + 1 );
			if ( r < rings - 1 ) AddTriangle( batch, a, a + row + 1, a + row );
		}
	}

	/// <summary>
	/// A leaf, a frond's leaflet, a ribbon, a ski: a blade from <paramref name="root"/> along the rotation's
	/// Forward, its face toward the rotation's Up, <paramref name="width"/> across at its widest (at
	/// <paramref name="widest"/> of the length), narrowing to <paramref name="tip"/> of that at the end and to
	/// a stalk at the root. It bends by <paramref name="bend"/> radians from root to tip (positive droops
	/// away from Up) and is lit as if folded up along its midrib by <paramref name="fold"/> of its half-width,
	/// so the two halves catch the light differently, as a real leaf's do. <paramref name="teeth"/> narrows every
	/// other station by that share, so the edge runs in points — a fern's or a palm's leaflets. Both sides
	/// are drawn, unless <paramref name="oneSided"/> — for a material that renders back faces itself. The
	/// region maps along the blade left to right, its middle row on the midrib.
	/// </summary>
	public void Blade( Skin skin, Vector3 root, Rotation rotation, float length, float width, float bend, float fold = 0.25f, int segments = 4, float tip = 0f, float widest = 0.4f,
		bool oneSided = false, float teeth = 0f )
	{
		var region = skin.Region;
		var forward = rotation.Forward; var up = rotation.Up; var left = rotation.Left;

		// Stations along the blade, evenly spaced but for the one nearest the widest point, which moves
		// onto it — so even a two-segment leaf is a diamond at its full width, never a sliver.
		segments = Math.Max( segments, 2 );
		var stations = new float[segments + 1];
		for ( var i = 0; i <= segments; i++ ) stations[i] = i / (float)segments;
		var nearest = Math.Clamp( (int)MathF.Round( widest * segments ), 1, segments - 1 );
		stations[nearest] = widest;

		var centres = new Vector3[segments + 1];
		var ups = new Vector3[segments + 1];
		var halves = new float[segments + 1];
		centres[0] = root;
		for ( var i = 0; i <= segments; i++ )
		{
			var t = stations[i];
			var angle = bend * t;
			ups[i] = forward * MathF.Sin( angle ) + up * MathF.Cos( angle );
			if ( i > 0 )
			{
				var mid = bend * (t + stations[i - 1]) * 0.5f;
				centres[i] = centres[i - 1] + (forward * MathF.Cos( mid ) - up * MathF.Sin( mid )) * (length * (t - stations[i - 1]));
			}
			var shape = t < widest
				? 0.18f + 0.82f * MathF.Sin( t / widest * MathF.PI * 0.5f )
				: tip + (1f - tip) * MathF.Cos( (t - widest) / (1f - widest) * MathF.PI * 0.5f );
			halves[i] = width * 0.5f * shape * (i % 2 == 1 && i < segments ? 1f - teeth : 1f);
		}

		// One strip from edge to edge: each station carries two vertices a side, shared by the segments either
		// side of it. The fold up the midrib is not modelled — it would double the faces — but lit: each
		// edge takes the normal its folded half would have, averaged over the segments it joins, so the two
		// halves still shade apart (in a gradient across the midrib rather than a crease) and a bent blade
		// curves smoothly instead of in facets.
		var stride = segments + 1;
		var lefts = new Vector3[stride]; var rights = new Vector3[stride]; var mids = new Vector3[stride];
		for ( var i = 0; i <= segments; i++ )
		{
			mids[i] = centres[i] + ups[i] * (fold * halves[i]);
			lefts[i] = centres[i] + left * halves[i];
			rights[i] = centres[i] - left * halves[i];
		}

		// Each half's normal per segment, turned to face the blade's Up; a segment with no area (a blade
		// folded flat onto itself) is left out.
		var leftNormals = new Vector3[stride]; var rightNormals = new Vector3[stride];
		var drawn = new bool[segments];
		var builtUp = true;
		var largest = -1f;
		for ( var i = 0; i < segments; i++ )
		{
			var strip = Vector3.Cross( lefts[i + 1] - lefts[i], rights[i] - lefts[i] );
			if ( strip.LengthSquared < 1e-8f ) continue;
			drawn[i] = true;
			var flip = Vector3.Dot( strip, ups[i] + ups[i + 1] ) >= 0f ? 1f : -1f;
			if ( strip.LengthSquared > largest ) { largest = strip.LengthSquared; builtUp = flip > 0f; }
			var l = Vector3.Cross( lefts[i + 1] - lefts[i], mids[i] - lefts[i] );
			var r = Vector3.Cross( mids[i + 1] - mids[i], rights[i] - mids[i] );
			l = (l.LengthSquared < 1e-10f ? strip : l).Normal * flip;
			r = (r.LengthSquared < 1e-10f ? strip : r).Normal * flip;
			leftNormals[i] += l; leftNormals[i + 1] += l;
			rightNormals[i] += r; rightNormals[i + 1] += r;
		}
		if ( largest < 0f ) return;

		// The tangent runs along the blade (the texture's U); its handedness is the one the explicit-UV quad
		// would give the strip wound as built, flipped for the side wound the other way.
		float U( int i ) => region.Left + region.Width * stations[i];
		var top = region.Top; var bottom = region.Top + region.Height;
		var handedness = Handedness( lefts[0], lefts[1], rights[0], new Vector2( U( 0 ), top ), new Vector2( U( 1 ), top ), new Vector2( U( 0 ), bottom ) );

		var batch = For( skin.Material );
		for ( var side = 0; side < (oneSided ? 1 : 2); side++ )
		{
			// Side 0 faces the blade's Up; side 1, drawn back to back with it, faces away.
			var sign = side == 0 ? 1f : -1f;
			var asBuilt = (side == 0) == builtUp;
			var h = asBuilt ? handedness : -handedness;
			var start = batch.Vertices.Count;
			for ( var i = 0; i <= segments; i++ )
			{
				var tangent = (centres[Math.Min( i + 1, segments )] - centres[Math.Max( i - 1, 0 )]).Normal;
				var u = U( i );
				Vertex V( Vector3 p, Vector3 n, float v )
				{
					var vertex = new Vertex( p, n, tangent, new Vector4( u, v, 0f, 0f ) );
					vertex.Tangent = new Vector4( tangent.x, tangent.y, tangent.z, h );
					return vertex;
				}
				batch.Vertices.Add( V( lefts[i], leftNormals[i].Normal * sign, top ) );
				batch.Vertices.Add( V( rights[i], rightNormals[i].Normal * sign, bottom ) );
				Grow( lefts[i] ); Grow( rights[i] );
			}

			for ( var i = 0; i < segments; i++ )
			{
				if ( !drawn[i] ) continue;
				var s0 = start + i * 2;
				// As built: (left i, left i+1, right i+1, right i).
				Face( batch, s0, s0 + 2, s0 + 3, s0 + 1, asBuilt );
			}
		}
	}

	/// <summary>A quad a→b→c→d of shared vertices, as wound or (<paramref name="asWound"/> false) the other way round.</summary>
	static void Face( Batch batch, int a, int b, int c, int d, bool asWound )
	{
		if ( asWound ) { AddTriangle( batch, a, b, c ); AddTriangle( batch, a, c, d ); }
		else { AddTriangle( batch, d, c, b ); AddTriangle( batch, d, b, a ); }
	}

	/// <summary>The tangent handedness the explicit-UV <see cref="Quad(Material, Vector3, Vector3, Vector3, Vector3, Vector2, Vector2, Vector2, Vector2, Vector3?)"/> gives a face a→b…d with these UVs.</summary>
	static float Handedness( Vector3 a, Vector3 b, Vector3 d, Vector2 uvA, Vector2 uvB, Vector2 uvD )
	{
		var e1 = b - a; var e2 = d - a;
		var du1 = uvB.x - uvA.x; var dv1 = uvB.y - uvA.y;
		var du2 = uvD.x - uvA.x; var dv2 = uvD.y - uvA.y;
		var determinant = du1 * dv2 - du2 * dv1;
		var tangent = ((e1 * dv2 - e2 * dv1) / determinant).Normal;
		var bitangent = (e2 * du1 - e1 * du2) / determinant;
		return Vector3.Dot( Vector3.Cross( Vector3.Cross( e1, e2 ), tangent ), bitangent ) > 0f ? 1f : -1f;
	}

	/// <summary>
	/// A round tube swept along <paramref name="path"/> — a wire, a stem, a chain's run, a handle, a cane —
	/// shaded smooth round, its radius running from <paramref name="radius"/> at the start to
	/// <paramref name="endRadius"/> (negative: the same) at the end. The frame is carried along the path
	/// without twisting, so a curved tube keeps its girth. Open at both ends: tubes this thin end in
	/// something (a socket, a pot, a hook) or taper to nothing. The region wraps round and runs along.
	/// </summary>
	public void Tube( Skin skin, Vector3[] path, float radius, int sides, float endRadius = -1f )
	{
		var batch = For( skin.Material );
		var region = skin.Region;
		var count = path.Length;
		if ( endRadius < 0f ) endRadius = radius;

		var along = new float[count];
		for ( var i = 1; i < count; i++ ) along[i] = along[i - 1] + (path[i] - path[i - 1]).Length;
		var total = MathF.Max( along[^1], 0.0001f );

		var tangents = new Vector3[count];
		var normals = new Vector3[count];
		for ( var i = 0; i < count; i++ )
			tangents[i] = (path[Math.Min( i + 1, count - 1 )] - path[Math.Max( i - 1, 0 )]).Normal;
		var reference = MathF.Abs( tangents[0].z ) < 0.9f ? Vector3.Up : Vector3.Forward;
		normals[0] = Vector3.Cross( Vector3.Cross( tangents[0], reference ), tangents[0] ).Normal;
		for ( var i = 1; i < count; i++ )
			normals[i] = (normals[i - 1] - tangents[i] * Vector3.Dot( normals[i - 1], tangents[i] )).Normal;

		var start = batch.Vertices.Count;
		for ( var i = 0; i < count; i++ )
		{
			var binormal = Vector3.Cross( tangents[i], normals[i] );
			var r = radius + (endRadius - radius) * along[i] / total;
			for ( var step = 0; step <= sides; step++ )
			{
				var angle = MathF.Tau * step / sides;
				var outward = normals[i] * MathF.Cos( angle ) + binormal * MathF.Sin( angle );
				var around = binormal * MathF.Cos( angle ) - normals[i] * MathF.Sin( angle );
				Add( batch, path[i] + outward * r, outward, around,
					new Vector2( region.Left + region.Width * step / sides, region.Top + region.Height * (1f - along[i] / total) ) );
			}
		}

		// One ring of vertices per path point, shared by the lengths of tube either side of it.
		var row = sides + 1;
		for ( var i = 0; i + 1 < count; i++ )
		for ( var s = 0; s < sides; s++ )
		{
			var a = start + i * row + s;
			AddTriangle( batch, a, a + 1, a + row + 1 );
			AddTriangle( batch, a, a + row + 1, a + row );
		}
	}

	public Model Build( string name )
	{
		var builder = Model.Builder.WithName( name );

		foreach ( var (material, batch) in _batches )
		{
			var mesh = new Mesh( material );
			mesh.CreateVertexBuffer( batch.Vertices.Count, batch.Vertices );
			mesh.CreateIndexBuffer( batch.Indices.Count, batch.Indices );
			mesh.Bounds = _bounds;
			builder.AddMesh( mesh );
		}

		return builder.Create();
	}
}
