using System;
using System.Collections.Generic;

/// <summary>What a theme needs to know about the room it dresses. Grows with the room builder (Task 6).</summary>
public sealed class FloorContext
{
	public int Floor { get; init; }

	/// <summary>World height of this floor's tile tops.</summary>
	public float TileTopZ { get; init; }
}

/// <summary>
/// One floor's room: the surfaces it paints at load and, later, the props it builds. The eight
/// themes run top to bottom from a greenhouse to a forge over the lava; each paints its own
/// materials in code (no files), in natural material colours near its floor's identity hue.
///
/// Instances are made per arena build and hold their generated materials — never cached in a
/// static, because statics survive hotload and would keep serving stale textures.
/// </summary>
public abstract class ArenaTheme
{
	/// <summary>Floor atlas resolution: 2x2 cells of 512 px, one cell per 56-unit tile top.</summary>
	protected const int FloorSize = 1024;

	/// <summary>Ceiling resolution: 2x2 cells of 256 px, one cell per tile underside, seen from 160+ units away.</summary>
	protected const int CeilingSize = 512;

	public abstract string Name { get; }
	public abstract int Floor { get; }

	/// <summary>Walkable surface, 2x2 atlas of four variants (each cell = one tile top).</summary>
	public Material FloorSurface { get; private set; }

	/// <summary>
	/// What you see looking up in this room: the underside of the floor above, as a 2x2 atlas of
	/// four tile undersides. Null where a room has no tiles above it (the greenhouse has a glass roof).
	/// </summary>
	public Material Ceiling { get; private set; }

	/// <summary>Interior light colour (HDR allowed) used by the room builder and props.</summary>
	public abstract Color LightColor { get; }

	/// <summary>How many textures <see cref="Paint"/> produced, for the one startup timing line.</summary>
	public int SurfaceCount => (FloorSurface is null ? 0 : 1) + (Ceiling is null ? 0 : 1);

	public void Paint()
	{
		FloorSurface = PaintFloor();
		Ceiling = PaintCeiling();
	}

	protected abstract Material PaintFloor();
	protected abstract Material PaintCeiling();

	/// <summary>Dress the room: wall face layer, alcove props, ceiling props, lights.</summary>
	public abstract void Dress( FloorContext room );

	public static Rect Cell( int column, int row, int cells ) => new( column / (float)cells, row / (float)cells, 1f / cells, 1f / cells );

	public static ArenaTheme Create( int floor ) => floor switch
	{
		0 => new GreenhouseTheme(), 1 => new WinterTheme(), 2 => new BeachTheme(), 3 => new OfficeTheme(),
		4 => new LibraryTheme(), 5 => new WorkshopTheme(), 6 => new DungeonTheme(), _ => new ForgeTheme(),
	};

	/// <summary>Seed base for this floor, so two themes never draw the same stone or the same noise.</summary>
	protected int Seed => 7919 * (Floor + 1);

	/// <summary>Top-left pixel of atlas cell 0..3 (column = cell % 2, row = cell / 2), matching <see cref="Cell"/>.</summary>
	protected static Vector2 CellOrigin( int cell, int atlasSize ) => new( (cell % 2) * atlasSize / 2, (cell / 2) * atlasSize / 2 );

	protected static Color Mix( Color a, Color b, float t ) => new( a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t );

	/// <summary>
	/// The outline of one hand-laid block inside an atlas cell, in pixels: a rounded rectangle whose
	/// four sides sit different distances in from the cell edge (so the joint changes width around the
	/// block), whose corners have different radii, and whose sides wander by up to
	/// <paramref name="wobble"/> px along their length. Nothing about it is a perfect square.
	/// </summary>
	protected static Vector2[] SlabOutline( Vector2 min, float size, float joint, float wobble, float corner, int seed )
	{
		var random = new Random( seed );
		float Next() => (float)random.NextDouble();

		var insets = new float[4];
		for ( var k = 0; k < 4; k++ ) insets[k] = joint * (0.7f + 0.6f * Next());

		// Corners clockwise in the image (y down): top-left, top-right, bottom-right, bottom-left.
		var left = min.x + insets[3]; var right = min.x + size - insets[1];
		var top = min.y + insets[0]; var bottom = min.y + size - insets[2];
		var corners = new[] { new Vector2( left, top ), new Vector2( right, top ), new Vector2( right, bottom ), new Vector2( left, bottom ) };
		var inward = new[] { new Vector2( 1, 1 ), new Vector2( -1, 1 ), new Vector2( -1, -1 ), new Vector2( 1, -1 ) };

		var radii = new float[4];
		for ( var k = 0; k < 4; k++ ) radii[k] = corner * (0.55f + 0.9f * Next());

		var points = new List<Vector2>();
		for ( var k = 0; k < 4; k++ )
		{
			// Arc around corner k, from the side before it to the side after it.
			var centre = corners[k] + inward[k] * radii[k];
			var start = MathF.PI * (1f + 0.5f * k);
			for ( var j = 0; j <= 3; j++ )
			{
				var angle = start + MathF.PI * 0.5f * j / 3f;
				points.Add( centre + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * radii[k] );
			}

			// The side to the next corner, subdivided and pushed in or out along a smooth random curve
			// that is zero at both ends so it meets the arcs cleanly.
			var next = (k + 1) % 4;
			var from = points[^1];
			var nextCentre = corners[next] + inward[next] * radii[next];
			var nextStart = MathF.PI * (1f + 0.5f * next);
			var to = nextCentre + new Vector2( MathF.Cos( nextStart ), MathF.Sin( nextStart ) ) * radii[next];

			var along = to - from;
			var length = along.Length;
			var direction = along / length;
			var normal = new Vector2( -direction.y, direction.x );
			var controls = new float[5];
			for ( var c = 1; c < 4; c++ ) controls[c] = Next() * 2f - 1f;

			var steps = Math.Max( 2, (int)(length / 16f) );
			for ( var s = 1; s < steps; s++ )
			{
				var t = s / (float)steps;
				var g = t * 4f; var c0 = Math.Min( (int)g, 3 ); var f = g - c0;
				f = f * f * (3f - 2f * f);
				var offset = (controls[c0] + (controls[c0 + 1] - controls[c0]) * f) * wobble + (Next() - 0.5f) * wobble * 0.25f;
				offset = MathF.Max( offset, -insets[k] * 0.6f );
				points.Add( from + direction * (length * t) + normal * offset );
			}
		}

		return points.ToArray();
	}

	/// <summary>
	/// A natural closed outline — ice patch, puddle, stain, moss clump, lump of snow — inside an
	/// ellipse of half-size <paramref name="half"/> turned by <paramref name="angle"/> radians. The
	/// radius wanders with a few random low harmonics (amplitudes falling with frequency) plus a
	/// little per-point jitter, so it has no corners and is never a circle; it is scaled so its
	/// widest point touches the ellipse and never leaves it. <paramref name="roughness"/> 0 gives the
	/// ellipse, 1 a bean or a splash.
	/// </summary>
	protected static Vector2[] Organic( Vector2 centre, Vector2 half, float roughness, int seed, float angle = 0f, int count = 40 )
	{
		var random = new Random( seed );
		var amplitudes = new float[5];
		var phases = new float[5];
		for ( var k = 0; k < 5; k++ )
		{
			amplitudes[k] = roughness * 0.32f / (k + 1) * (0.4f + (float)random.NextDouble() * 0.6f);
			phases[k] = (float)random.NextDouble() * MathF.Tau;
		}

		var radii = new float[count];
		var widest = 0f;
		for ( var i = 0; i < count; i++ )
		{
			var theta = i * MathF.Tau / count;
			var r = 1f + ((float)random.NextDouble() - 0.5f) * 0.05f * roughness;
			for ( var k = 0; k < 5; k++ ) r += amplitudes[k] * MathF.Cos( (k + 2) * theta + phases[k] );
			radii[i] = r;
			widest = MathF.Max( widest, r );
		}

		var cos = MathF.Cos( angle ); var sin = MathF.Sin( angle );
		var points = new Vector2[count];
		for ( var i = 0; i < count; i++ )
		{
			var theta = i * MathF.Tau / count;
			var r = MathF.Max( radii[i] / widest, 0.3f );
			var x = MathF.Cos( theta ) * half.x * r; var y = MathF.Sin( theta ) * half.y * r;
			points[i] = centre + new Vector2( x * cos - y * sin, x * sin + y * cos );
		}

		return points;
	}

	/// <summary>
	/// Reads a noise field stretched along one axis: <paramref name="row"/> picks the noise row,
	/// <paramref name="along"/> moves along it (divide by the stretch before passing it), linear
	/// between samples. Neighbouring rows read neighbouring noise rows, so features keep their width
	/// across and become streaks along — wood grain, yarn, brushed metal.
	/// </summary>
	protected static float Streak( float[] noise, int size, int row, float along )
	{
		var offset = ((row % size) + size) % size * size;
		var p0 = (int)MathF.Floor( along );
		var f = along - p0;
		p0 = ((p0 % size) + size) % size;
		var p1 = p0 + 1 == size ? 0 : p0 + 1;
		return noise[offset + p0] + (noise[offset + p1] - noise[offset + p0]) * f;
	}

	/// <summary>
	/// A chipped-off corner: covers the block's corner from the cell corner and ends in a jagged
	/// break line from <paramref name="a"/> px along one side to <paramref name="b"/> px along the other.
	/// <paramref name="sideA"/>/<paramref name="sideB"/> point from the cell corner into the cell.
	/// </summary>
	protected static Vector2[] ChipOutline( Vector2 corner, Vector2 sideA, Vector2 sideB, float a, float b, int seed )
	{
		var random = new Random( seed );
		var bulge = (sideA + sideB).Normal;
		var points = new List<Vector2> { corner };

		var from = corner + sideA * a;
		var to = corner + sideB * b;
		var reach = MathF.Min( a, b );
		for ( var s = 0; s <= 6; s++ )
		{
			var t = s / 6f;
			var push = s == 0 || s == 6 ? 0f : ((float)random.NextDouble() * 0.5f - 0.12f) * reach * 0.45f;
			points.Add( from + (to - from) * t + bulge * push );
		}

		return points.ToArray();
	}

	/// <summary>
	/// A small bite out of a block's edge: starts 6 px outside it, breaks <paramref name="depth"/> px
	/// into the block over <paramref name="halfLength"/> px either side of <paramref name="at"/>.
	/// <paramref name="along"/> runs with the edge, <paramref name="into"/> points into the block.
	/// </summary>
	protected static Vector2[] NickOutline( Vector2 at, Vector2 along, Vector2 into, float halfLength, float depth, int seed )
	{
		var random = new Random( seed );
		float Jitter() => 0.6f + (float)random.NextDouble() * 0.6f;
		return new[]
		{
			at - along * halfLength - into * 6f,
			at - along * halfLength,
			at - along * (halfLength * 0.45f) + into * (depth * 0.7f * Jitter()),
			at + along * (halfLength * 0.1f) + into * depth,
			at + along * (halfLength * 0.55f) + into * (depth * 0.6f * Jitter()),
			at + along * halfLength,
			at + along * halfLength - into * 6f,
		};
	}

	/// <summary>A jagged polyline from <paramref name="start"/>, heading <paramref name="angle"/> radians and turning a little at each step — cracks, grain, fissures.</summary>
	protected static Vector2[] Wander( Vector2 start, float angle, float step, int steps, float turn, Random random )
	{
		var points = new Vector2[steps + 1];
		points[0] = start;
		for ( var s = 1; s <= steps; s++ )
		{
			angle += ((float)random.NextDouble() - 0.5f) * 2f * turn;
			points[s] = points[s - 1] + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * step * (0.6f + (float)random.NextDouble() * 0.8f);
		}
		return points;
	}

	/// <summary>Draws a polyline from <see cref="Wander"/> as joined segments.</summary>
	protected static void Polyline( Painter painter, Vector2[] points, float width, Color color, float alpha, float height = float.NaN, float roughness = float.NaN )
	{
		for ( var s = 1; s < points.Length; s++ )
			painter.Line( points[s - 1], points[s], width, color, alpha, height, roughness );
	}

	/// <summary>Turns a noise field into a soft mask in place: 0 below <paramref name="from"/>, <paramref name="scale"/> above <paramref name="to"/>, smooth between.</summary>
	protected static float[] Ramp( float[] field, float from, float to, float scale = 1f )
	{
		var inverse = 1f / (to - from);
		for ( var i = 0; i < field.Length; i++ )
		{
			var t = (field[i] - from) * inverse;
			t = t <= 0f ? 0f : t >= 1f ? 1f : t * t * (3f - 2f * t);
			field[i] = t * scale;
		}
		return field;
	}

	/// <summary>
	/// Keeps a mask only around <paramref name="centre"/>: full strength at the centre, fading to zero
	/// at <paramref name="radius"/>, and zero outside the atlas cell the centre lies in — so a patch
	/// meant for one tile never bleeds onto the neighbouring cell's tile.
	/// </summary>
	protected static void KeepNear( float[] mask, int size, Vector2 centre, float radius )
	{
		var half = size / 2;
		var cellX = (int)(centre.x / half) * half;
		var cellY = (int)(centre.y / half) * half;
		var inverse = 1f / radius;

		for ( var y = 0; y < size; y++ )
		{
			var row = y * size;
			var insideRow = y >= cellY && y < cellY + half;
			var dy = y + 0.5f - centre.y;
			for ( var x = 0; x < size; x++ )
			{
				if ( !insideRow || x < cellX || x >= cellX + half ) { mask[row + x] = 0f; continue; }
				var dx = x + 0.5f - centre.x;
				var t = 1f - MathF.Sqrt( dx * dx + dy * dy ) * inverse;
				mask[row + x] *= t <= 0f ? 0f : t * t * (3f - 2f * t);
			}
		}
	}

	/// <summary>
	/// A plain but honest block surface: four blocks per atlas with uneven joints, a bevelled edge in
	/// height, a tone shift per block, crevice darkening and soft colour drift. Floors 4–7 wear this
	/// until their full surfaces are painted (Task 4b).
	/// </summary>
	protected Material PaintPlainFloor( Color baseColor, Color light, Color shade, float roughness, string baseMaterial = Painter.Matte )
	{
		var painter = new Painter( FloorSize, Mix( shade, Color.Black, 0.25f ), 0.95f );
		var random = new Random( Seed );
		var cell = FloorSize / 2;

		for ( var c = 0; c < 4; c++ )
		{
			var tone = (float)random.NextDouble() * 2f - 1f;
			var color = tone < 0f ? Mix( baseColor, shade, -tone * 0.5f ) : Mix( baseColor, light, tone * 0.5f );
			var outline = SlabOutline( CellOrigin( c, FloorSize ), cell, 7f, 2.5f, 14f, Seed + c * 101 );
			painter.Polygon( outline, color, height: 1f, bevel: 14f, roughness: roughness );
		}

		painter.HeightNoise( 0.8f, 4, Seed + 1, 5 );
		painter.Mottle( 0.07f, 4, Seed + 2, 5 );
		painter.Cavity( 0.45f, 8 );
		painter.Grain( 0.015f, Seed + 3 );
		return painter.ToMaterial( $"arena_{Floor}_floor", baseMaterial, 8f );
	}

	/// <summary>The plain counterpart for ceilings: four undersides per atlas, same treatment at half the resolution.</summary>
	protected Material PaintPlainCeiling( Color baseColor, Color light, Color shade, float roughness, string baseMaterial = Painter.Matte )
	{
		var painter = new Painter( CeilingSize, Mix( shade, Color.Black, 0.25f ), 0.95f );
		var random = new Random( Seed + 500 );
		var cell = CeilingSize / 2;

		for ( var c = 0; c < 4; c++ )
		{
			var tone = (float)random.NextDouble() * 2f - 1f;
			var color = tone < 0f ? Mix( baseColor, shade, -tone * 0.5f ) : Mix( baseColor, light, tone * 0.5f );
			var outline = SlabOutline( CellOrigin( c, CeilingSize ), cell, 3f, 1f, 6f, Seed + 500 + c * 101 );
			painter.Polygon( outline, color, height: 1f, bevel: 6f, roughness: roughness );
		}

		painter.HeightNoise( 0.5f, 4, Seed + 501, 4 );
		painter.Mottle( 0.07f, 4, Seed + 502, 5 );
		painter.Cavity( 0.45f, 4 );
		painter.Grain( 0.015f, Seed + 503 );
		return painter.ToMaterial( $"arena_{Floor}_ceiling", baseMaterial, 5f );
	}
}
