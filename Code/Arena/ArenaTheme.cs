using System;
using System.Collections.Generic;

/// <summary>
/// The surfaces a theme paints, each an independent job for <see cref="ArenaTheme.Bake"/>: the four every
/// theme has, and <see cref="Glow"/> for the few that light their own lamps.
/// </summary>
public enum ThemeSurface { Floor, Ceiling, Wall, Trim, Glow }

/// <summary>
/// The rows of a theme's trim atlas. Every band but <see cref="Infill"/> spans the full width and is
/// painted seamless across it, so a moulding runs any length: faces tile the band along their run at
/// <see cref="ArenaTheme.TrimRepeat"/> and fit it across. Infill is four pictures side by side
/// (window panes, blinds) that are fitted to one face each.
/// </summary>
public enum TrimBand { Cornice, Skirting, Pilaster, SlabEdge, Stock, Infill }

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

	/// <summary>
	/// Wall resolution: one <see cref="WallRepeat"/> square, floor to ceiling, about 4.8 pixels per
	/// unit. 20 × 64, so a 32 px or 64 px period (corrugation, spangle grains) divides it and halves
	/// cleanly through the first mips, and every wall's module comes out in whole pixels: ten 128 px
	/// greenhouse bays and forge facets, five 256 px partitions and bookcases, forty zinc crests. It
	/// grew with the room when the floors were raised (it was 1024 px): about the same detail per unit,
	/// more of it, never stretched. (1344 was tried first; this is 9 % fewer pixels on the slowest jobs.)
	/// </summary>
	protected const int WallSize = 1280;

	/// <summary>Trim atlas resolution: <see cref="TrimRepeat"/> units of moulding per 512 px, four pixels per unit.</summary>
	protected const int TrimSize = 512;

	/// <summary>World units one horizontal repeat of the trim atlas spans along a moulding.</summary>
	public const float TrimRepeat = 128f;

	/// <summary>
	/// World units one floor cell spans: the tile's top face plus both chamfers laid flat at their
	/// true slant length. The tile model maps a cell over exactly this, so texture density is the
	/// same on the top and on the chamfers.
	/// </summary>
	public static float UnfoldedSpan => Arena.TileSize - 2f * ArenaGenerator.TileBevel + 2f * ArenaGenerator.TileBevel * MathF.Sqrt( 2f );

	/// <summary>
	/// Pixels along each edge of a 512 px floor cell that land on the tile's chamfer (about 25). The
	/// joint belongs at the outer edge of this rim — the chamfer's foot, where neighbours meet — and
	/// no decal may sit in it, because the chamfer's foot also feeds the tile's sides.
	/// </summary>
	protected static float Rim => FloorSize / 2 * ArenaGenerator.TileBevel * MathF.Sqrt( 2f ) / UnfoldedSpan;

	public abstract string Name { get; }
	public abstract int Floor { get; }

	/// <summary>Walkable surface, 2x2 atlas of four variants (each cell = one tile top).</summary>
	public Material FloorSurface { get; private set; }

	/// <summary>
	/// What you see looking up in this room: the underside of the floor above, as a 2x2 atlas of
	/// four tile undersides. Null where a room has no tiles above it (the greenhouse has a glass roof).
	/// </summary>
	public Material Ceiling { get; private set; }

	/// <summary>
	/// Whether a tile's underside turns with its top (the tile's quarter-turn), so each of the four
	/// ceiling cells is seen four ways round. For a ceiling with no direction — rough rock — that is
	/// sixteen looks instead of four; boards, panels and stripes overhead keep one direction across the
	/// room, so the default is off.
	/// </summary>
	public virtual bool CeilingTurns => false;

	/// <summary>
	/// The room's wall face behind the pilasters, tiled every <see cref="WallRepeat"/> units along the
	/// wall; vertically one repeat spans tile top to ceiling, so a wall can be composed floor to ceiling.
	/// One per <see cref="WallCount"/>: a theme whose bays are painted differently (a row of beach huts)
	/// paints several and each length of wall wears one. The face is laid only by
	/// <see cref="BuildFrontLayer"/>, so a theme's <see cref="Dress"/> must call it or the room has no wall.
	/// </summary>
	public Material[] Walls { get; private set; }

	/// <summary>The first (for most themes the only) wall surface.</summary>
	public Material Wall => Walls[0];

	/// <summary>How many wall surfaces the theme paints; each is its own job for <see cref="Bake"/>.</summary>
	public virtual int WallCount => 1;

	/// <summary>
	/// Whether a length of wall may wear its surface mirrored along the wall, doubling the looks of a
	/// pattern with no handedness (rock, a bookcase) without painting more: with three surfaces, "never
	/// one of the previous two" leaves exactly one choice and the lengths ran A-B-C-A-B-C. With six
	/// looks the next length need only differ from the one before (<see cref="PickLook"/>). Off by
	/// default: a beach hut's colour is its look, and a mirror would not make a second hut of it.
	/// </summary>
	protected virtual bool MirrorWalls => false;

	/// <summary>
	/// Width of the wall pattern's module in world units (partition panels, bookcase bays), or 0 for a
	/// pattern without one. With a module, every length of wall face starts its pattern on a module
	/// edge chosen so that no bay ends in a sliver of a module against a post.
	/// </summary>
	protected virtual float WallModule => 0f;

	/// <summary>
	/// How far up a length of wall wearing <see cref="Walls"/>[<paramref name="wall"/>] starts its
	/// pattern, in world units, drawn from <paramref name="random"/>. 0 (and no draw) by default: most
	/// patterns are composed floor to ceiling (a dwarf wall, a partition rail, shelves). A pattern with
	/// no fixed height (weatherboard) returns its own per length, so laps never line up across a post —
	/// and can use it to place or hide a feature of the texture behind the cornice or the skirting.
	/// </summary>
	protected virtual float WallLift( int wall, Random random ) => 0f;

	/// <summary>
	/// The widest bay <see cref="AddPosts"/> lets a non-modular wall have, kept under
	/// <see cref="WallRepeat"/> so no bay shows its pattern's first strip again at its far end. It was
	/// the lower rooms' repeat stretched 10 % before the floors were raised (when a wider bay stretched
	/// its pattern to fit); the repeat is now wider than any bay, so nothing is stretched, and the cap
	/// keeps its value, so every wall keeps the post layout the user approved.
	/// </summary>
	const float MaxBay = 228.8f;

	/// <summary>Mouldings, posts, skirting, slab edge and infill pictures, one atlas laid out by <see cref="TrimBand"/>.</summary>
	public Material Trim { get; private set; }

	/// <summary>
	/// The lit parts of the room's lamp fittings — bulbs, a shade's lit inside, a lantern's panes — on the
	/// self-lit base (<see cref="ArenaGenerator.GlowMaterialPath"/>) with a painted mask, so they glow in their
	/// own colour and light the ceiling around them the way no unshadowed lamp may (ruling R31). Null where
	/// a theme paints none. Everything else a prop wears comes from <see cref="Trim"/>'s infill band.
	/// </summary>
	public Material Glow { get; private set; }

	/// <summary>
	/// World units per horizontal repeat of <see cref="Wall"/>, and the height its one vertical repeat
	/// spans: the room's clear height, so a wall is painted floor to ceiling in one repeat.
	/// </summary>
	public const float WallRepeat = FloorContext.ClearHeight;

	/// <summary>Interior light colour (HDR allowed) used by the room builder and props.</summary>
	public abstract Color LightColor { get; }

	/// <summary>
	/// Paints one surface into plain bytes. Safe on any thread: every surface paints its own canvas
	/// from its own seeded random streams and touches no engine resource, so the arena paints all of
	/// them side by side and gets the same pixels whatever the order. Null where a room has none.
	/// </summary>
	public PaintedSurface Bake( ThemeSurface surface, int variant = 0 ) => surface switch
	{
		ThemeSurface.Floor => PaintFloor(),
		ThemeSurface.Ceiling => PaintCeiling(),
		ThemeSurface.Wall => PaintWall( variant ),
		ThemeSurface.Trim => PaintTrim(),
		_ => PaintGlow(),
	};

	/// <summary>Uploads a baked surface and keeps its material. Main thread only.</summary>
	public void Apply( ThemeSurface surface, PaintedSurface baked, int variant = 0 )
	{
		var material = baked?.Upload();
		switch ( surface )
		{
			case ThemeSurface.Floor: FloorSurface = material; break;
			case ThemeSurface.Ceiling: Ceiling = material; break;
			case ThemeSurface.Wall: (Walls ??= new Material[WallCount])[variant] = material; break;
			case ThemeSurface.Trim: Trim = material; break;
			default: Glow = material; break;
		}
	}

	protected abstract PaintedSurface PaintFloor();
	protected abstract PaintedSurface PaintCeiling();

	/// <summary>Wall surface <paramref name="variant"/> of <see cref="WallCount"/>; a theme with one wall only ever gets 0.</summary>
	protected abstract PaintedSurface PaintWall( int variant );
	protected abstract PaintedSurface PaintTrim();

	/// <summary>The lamp fittings' lit parts (<see cref="Glow"/>), or null for a theme whose lamps show none.</summary>
	protected virtual PaintedSurface PaintGlow() => null;

	/// <summary>
	/// Dress the room: wall face layer, alcove props, ceiling props, lights. Must call
	/// <see cref="BuildFrontLayer"/>, which also lays the wall face behind the posts.
	/// </summary>
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

	public static Color Mix( Color a, Color b, float t ) => new( a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t );

	/// <summary>
	/// A well-mixed hash of three integers, for properties of things a surface computes per pixel —
	/// one plank, one stone, one boss — where a <see cref="Random"/> stream cannot be used because the
	/// same thing is met again on every pixel it covers. Read different bit ranges for independent values.
	/// </summary>
	protected static uint Hash( int a, int b, int c )
	{
		var h = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ (uint)(c * 83492791);
		h ^= h >> 16; h *= 0x7feb352du;
		h ^= h >> 15; h *= 0x846ca68bu;
		h ^= h >> 16;
		return h;
	}

	/// <summary>Eight bits of a hash as 0..1: <paramref name="shift"/> picks which eight.</summary>
	protected static float Bits( uint hash, int shift ) => ((hash >> shift) & 0xFF) / 255f;

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
	public static float[] Ramp( float[] field, float from, float to, float scale = 1f )
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

	/// <summary>Pixel rows of a trim band: top row and height.</summary>
	public static (int Top, int Height) TrimRows( TrimBand band ) => band switch
	{
		TrimBand.Cornice => (0, 96),
		TrimBand.Skirting => (96, 64),
		TrimBand.Pilaster => (160, 96),
		TrimBand.SlabEdge => (256, 48),
		TrimBand.Stock => (304, 48),
		_ => (352, 160),
	};

	/// <summary>
	/// A band as a UV region, inset from its top and bottom rows so the first few mips never bleed the
	/// neighbouring band in (a sage bar picked up the terracotta slab edge as a pink fringe at two pixels).
	/// </summary>
	public static Rect TrimRegion( TrimBand band )
	{
		const float Inset = 5f;
		var (top, height) = TrimRows( band );
		return new Rect( 0f, (top + Inset) / TrimSize, 1f, (height - Inset * 2f) / TrimSize );
	}

	/// <summary>Infill picture 0..3: a 128 × 160 px cell of the infill band, inset two pixels all round.</summary>
	public static Rect InfillCell( int index )
	{
		var (top, height) = TrimRows( TrimBand.Infill );
		return new Rect( (index * 128 + 2f) / TrimSize, (top + 2f) / TrimSize, 124f / TrimSize, (height - 4f) / TrimSize );
	}

	/// <summary>
	/// Paints a band's base: one colour over its rows, a height profile across it (t = 0 at the band's
	/// top row, 1 at its bottom — a moulding's cross-section) and a roughness. The profile is what
	/// makes a flat box face read as a moulding: the normal map lights its steps and coves.
	/// </summary>
	protected static void PaintBand( Painter painter, TrimBand band, Color color, Func<float, float> profile, float roughness )
	{
		var (top, height) = TrimRows( band );
		painter.Fill( 0, top, TrimSize, height, color, roughness: roughness );
		var relief = new float[TrimSize * TrimSize];
		for ( var y = 0; y < height; y++ )
		{
			var h = profile( (y + 0.5f) / height );
			var row = (top + y) * TrimSize;
			for ( var x = 0; x < TrimSize; x++ ) relief[row + x] = h;
		}
		painter.AddHeight( relief, 1f );
	}

	/// <summary>
	/// Shades a band by its own profile: rows sitting low in it (a cove, the shadow under a bead) move
	/// toward <paramref name="shade"/>, so mouldings read even under flat ambient light.
	/// </summary>
	protected static void ShadeBand( Painter painter, TrimBand band, Func<float, float> profile, Color shade, float amount )
	{
		var (top, height) = TrimRows( band );
		var weight = new float[TrimSize * TrimSize];
		for ( var y = 0; y < height; y++ )
		{
			var w = Math.Clamp( (1f - profile( (y + 0.5f) / height )) * amount, 0f, 1f );
			var row = (top + y) * TrimSize;
			for ( var x = 0; x < TrimSize; x++ ) weight[row + x] = w;
		}
		painter.Blend( weight, shade );
	}

	/// <summary>
	/// Wood grain along rows <paramref name="top"/>..+<paramref name="height"/>: growth rings as seen on
	/// a sawn face — continuous thin late-wood lines running the length of the piece. Rings are laid
	/// across the band at random widths (a slow random walk between about 4 and 12 px, so tight and
	/// open years come in runs, as in a real log), every line its own weight and thickness, and the
	/// whole set drifts a few pixels along the run on one slow octave, so lines wander gently instead
	/// of rippling in step. Each line is sharp on one side and fades into the early wood on the other.
	/// Never dashes: thresholded noise read as tiger stripes (4a review); evenly spaced rings bent by
	/// fast noise read as a wavy pattern (6a review). Seamless across the texture's wrap, since
	/// mouldings tile along their run.
	/// </summary>
	protected static void GrainRows( Painter painter, int top, int height, Color lateWood, float strength, int seed )
	{
		const float Drift = 4f;
		var size = painter.Size;
		var bend = painter.Noise( 2, seed, 1 );
		var fade = painter.Noise( 4, seed + 1, 2 );
		var random = new Random( seed + 2 );
		float Next() => (float)random.NextDouble();

		// Ring starts over the band plus the drift on both sides, with each ring's line width and weight.
		var starts = new List<float>();
		var widths = new List<float>();
		var weights = new List<float>();
		var period = 5f + Next() * 5f;
		var start = top - Drift - 1f - Next() * 12f;
		while ( start < top + height + Drift + 1f )
		{
			starts.Add( start );
			widths.Add( 1.2f + Next() * 1.4f );
			weights.Add( 0.5f + Next() * 0.8f );
			period = Math.Clamp( period + (Next() - 0.5f) * 3f, 4f, 12f );
			start += period * (0.8f + Next() * 0.4f);
		}

		// Which ring a whole-pixel position falls in, so each pixel finds its ring in a step or two.
		var origin = (int)MathF.Floor( starts[0] );
		var lookup = new int[(int)MathF.Ceiling( start ) - origin + 2];
		for ( int p = 0, k = 0; p < lookup.Length; p++ )
		{
			while ( k + 1 < starts.Count && starts[k + 1] <= origin + p ) k++;
			lookup[p] = k;
		}

		var weight = new float[size * size];
		for ( var y = top; y < top + height; y++ )
		{
			var row = y * size;
			for ( var x = 0; x < size; x++ )
			{
				var i = row + x;
				var c = y + (bend[i] - 0.5f) * 2f * Drift;
				var k = lookup[(int)MathF.Floor( c ) - origin];
				while ( k + 1 < starts.Count && starts[k + 1] <= c ) k++;
				// Sharp side: a one-pixel ramp up to the next ring's line, so a line drifting across pixel
				// rows never drops out between them (a hard edge there broke lines into dashes).
				var d = c - starts[k];
				var line = d < widths[k] ? (1f - d / widths[k]) * weights[k] : 0f;
				if ( k + 1 < starts.Count ) line = MathF.Max( line, MathF.Max( 0f, 1f - (starts[k + 1] - c) ) * weights[k + 1] );
				weight[i] = line * strength * (0.6f + fade[i] * 0.6f);
			}
		}
		painter.Blend( weight, lateWood );
	}

	/// <summary>
	/// A noise field read as streaks along x that stay seamless across the texture's wrap: straight
	/// along x, so the field's own wrap carries over the edge (its period sets the streak length), and
	/// each pixel row from a row <paramref name="spread"/> apart, so features stay thin across. For
	/// surfaces that tile — walls and trims — where <see cref="Streak"/>'s stretch would leave a seam.
	/// </summary>
	public static float StreakX( float[] noise, int size, int x, int y, int spread )
	{
		var column = ((x % size) + size) % size;
		var row = (int)((uint)(y * spread) % (uint)size);
		return noise[row * size + column];
	}

	/// <summary>
	/// Crown moulding across a cornice band, top (t = 0, against the ceiling) to bottom: a flat fillet,
	/// a deep cove, a round bead, then the fascia with a small drip step at its foot.
	/// </summary>
	protected static float CorniceProfile( float t )
	{
		if ( t < 0.14f ) return 1f;
		if ( t < 0.42f ) { var c = (t - 0.14f) / 0.28f; return 0.55f + 0.45f * (1f - MathF.Sin( c * MathF.PI )) * (c < 0.5f ? 1f : 0.85f); }
		if ( t < 0.56f ) { var b = (t - 0.42f) / 0.14f; return 0.75f + 0.25f * MathF.Sin( b * MathF.PI ); }
		if ( t < 0.62f ) return 0.62f;
		if ( t < 0.92f ) return 0.82f;
		return 0.7f;
	}

	/// <summary>Skirting board across its band: a rounded top bead, a shallow step, then the flat board.</summary>
	protected static float SkirtingProfile( float t )
	{
		if ( t < 0.22f ) return 0.75f + 0.25f * MathF.Sin( t / 0.22f * MathF.PI );
		if ( t < 0.3f ) return 0.68f;
		return 0.8f;
	}

	/// <summary>A post's face across its width: chamfered arrises and a shallow sunk panel down the middle.</summary>
	protected static float PostProfile( float t )
	{
		var edge = Step( 0f, 0.1f, MathF.Min( t, 1f - t ) );
		var panel = Step( 0.24f, 0.3f, MathF.Min( t, 1f - t ) );
		return 0.55f + 0.45f * edge - 0.12f * panel;
	}

	/// <summary>The slab edge: a rounded nosing along the top, then the flat edge of the floor.</summary>
	protected static float NosingProfile( float t ) => t < 0.3f ? 0.7f + 0.3f * MathF.Sin( (t / 0.3f) * MathF.PI * 0.5f + MathF.PI * 0.5f ) : 0.75f;

	/// <summary>
	/// A trim atlas in one material: every band in <paramref name="body"/> with its moulding profile
	/// and the profile's coves shaded toward <paramref name="shade"/>, then <paramref name="grain"/>
	/// (0 for none) of wood grain along the run of every moulding band, then <paramref name="details"/>
	/// (a theme's own paint, nail heads, windows in the infill band), then mottle, cavity and worn arrises.
	/// </summary>
	protected PaintedSurface PaintTrimAtlas( string name, Color body, Color shade, float roughness, float grain, int seed, Action<Painter> details = null )
	{
		var painter = new Painter( TrimSize, body, roughness );
		PaintBand( painter, TrimBand.Cornice, body, CorniceProfile, roughness );
		PaintBand( painter, TrimBand.Skirting, Mix( body, shade, 0.15f ), SkirtingProfile, roughness );
		PaintBand( painter, TrimBand.Pilaster, body, PostProfile, roughness );
		PaintBand( painter, TrimBand.SlabEdge, Mix( body, shade, 0.3f ), NosingProfile, roughness );
		PaintBand( painter, TrimBand.Stock, body, _ => 0.8f, roughness );
		PaintBand( painter, TrimBand.Infill, body, _ => 0.8f, roughness );
		ShadeBand( painter, TrimBand.Cornice, CorniceProfile, shade, 0.7f );
		ShadeBand( painter, TrimBand.Skirting, SkirtingProfile, shade, 0.5f );
		ShadeBand( painter, TrimBand.Pilaster, PostProfile, shade, 0.4f );

		// The generic grain goes under the theme's own paint, so pictures and band repaints sit on top of it.
		if ( grain > 0f ) GrainRows( painter, 0, TrimRows( TrimBand.Infill ).Top, Mix( body, shade, 0.8f ), grain, seed + 1 );
		details?.Invoke( painter );

		painter.HeightNoise( 0.08f, 32, seed + 2, 3 );
		painter.Mottle( 0.05f, 6, seed + 3, 4 );
		painter.RoughnessNoise( 0.06f, 8, seed + 4, 3 );
		painter.Cavity( 0.4f, 3 );
		painter.EdgeWear( 0.35f, 3, seed + 5 );
		painter.Grain( 0.012f, seed + 6 );
		return painter.Bake( name, Painter.Matte, 4f );
	}

	/// <summary>Recolours a band without touching its relief, then re-shades it by its profile.</summary>
	protected static void TintBand( Painter painter, TrimBand band, Color color, float roughness, Func<float, float> profile, Color shade, float amount )
	{
		var (top, height) = TrimRows( band );
		painter.Fill( 0, top, TrimSize, height, color, roughness: roughness );
		ShadeBand( painter, band, profile, shade, amount );
	}

	/// <summary>The pixel rectangle of infill picture <paramref name="index"/> (128 × 160).</summary>
	protected static (int X, int Y, int Width, int Height) InfillPixels( int index ) => (index * 128, TrimRows( TrimBand.Infill ).Top, 128, 160);

	/// <summary>
	/// Windows set into the wall face of some bays: a picture from the trim's infill band (frosted
	/// glass, blinds) fitted to the opening, a frame and a sill in another infill cell, an optional
	/// glazing cross. All of it stands inside the recess, in front of the wall face and behind the
	/// collider plane. Which bays get one is seeded per room, so every machine agrees. Returns those
	/// bays with each window's width, for a theme that dresses round its windows.
	/// </summary>
	protected List<(Bay Bay, float Width)> AddWindows( FloorContext room, List<Bay> bays, Rect glass, Rect frame, float sill, float height, float maxWidth, float chance, bool cross, int seed )
	{
		var random = new Random( seed );
		var windows = new List<(Bay, float)>();
		var skin = new Skin( Trim, frame );
		const float Rail = 4f;
		const float Depth = 5f;
		const float Face = FloorContext.Recess;

		foreach ( var bay in bays )
		{
			if ( bay.Width < 90f || random.NextDouble() > chance ) continue;

			var side = bay.Side;
			var facing = room.WallFacing( side );
			var width = MathF.Min( maxWidth, bay.Width - 36f );
			windows.Add( (bay, width) );
			var u = bay.Middle;
			var top = sill + height;
			// Every piece reaches Embed behind the wall face, so it never meets the face along a crack.
			Vector3 At( float along, float v, float depth ) => room.OnWall( side, u + along, v, Face + FloorContext.Embed - depth * 0.5f );

			room.WallPicture( side, Trim, glass, u - width * 0.5f, u + width * 0.5f, sill, top, Face - 0.5f, false );
			const float Frame = Depth + FloorContext.Embed;
			room.Kit.BevelBox( skin, At( 0f, top - Rail * 0.5f, Frame ), new Vector3( Frame, width, Rail ), 0.6f, facing );
			room.Kit.BevelBox( skin, At( 0f, sill + Rail * 0.5f, Frame ), new Vector3( Frame, width, Rail ), 0.6f, facing );
			room.Kit.BevelBox( skin, At( -(width - Rail) * 0.5f, sill + height * 0.5f, Frame ), new Vector3( Frame, Rail, height - Rail * 2f ), 0.6f, facing );
			room.Kit.BevelBox( skin, At( (width - Rail) * 0.5f, sill + height * 0.5f, Frame ), new Vector3( Frame, Rail, height - Rail * 2f ), 0.6f, facing );
			room.Kit.BevelBox( skin, At( 0f, sill - 1.5f, Frame + 5f ), new Vector3( Frame + 5f, width + 8f, 3f ), 0.6f, facing );

			if ( !cross ) continue;
			room.Kit.BevelBox( skin, At( 0f, sill + height * 0.5f, 3f + FloorContext.Embed ), new Vector3( 3f + FloorContext.Embed, 2.5f, height - Rail * 2f ), 0.4f, facing );
			room.Kit.BevelBox( skin, At( 0f, sill + height * 0.55f, 3f + FloorContext.Embed ), new Vector3( 3f + FloorContext.Embed, width - Rail * 2f, 2.5f ), 0.4f, facing );
		}

		return windows;
	}

	/// <summary>
	/// The room's own lamps: <paramref name="count"/> unshadowed lights (<see cref="FloorContext.AddLight"/>)
	/// spread round the room's perimeter in corner order from a seeded start, each slid along its wall
	/// by up to <paramref name="jitter"/> of the spacing and kept clear of the corners, at height
	/// <paramref name="v"/> and <paramref name="depth"/> (negative: out in the room, in front of the
	/// wall plane). Every lamp is a few percent brighter or dimmer than <paramref name="color"/> — real
	/// lamps never match.
	///
	/// Each is a cone thrown straight down (<see cref="FloorContext.LampCone"/>), as a fitting under a
	/// cornice throws its light, reaching no further than <see cref="FloorContext.LampReach"/>: the
	/// lights cast no shadows, so a lamp shining level or upward lit the room above through the slab,
	/// and one reaching too far down lit the room below.
	/// </summary>
	protected static void AddLamps( FloorContext room, int count, float v, float depth, Color color, int seed, float jitter = 0.25f )
	{
		foreach ( var spot in LampSpots( count, seed, jitter ) )
			AddLamp( room, spot.Side, spot.U, v, depth, color * spot.Brightness );
	}

	/// <summary>One of <see cref="AddLamps"/>'s lamps: a cone thrown straight down from (side, u, v, depth), reaching <see cref="FloorContext.LampReach"/>.</summary>
	protected static void AddLamp( FloorContext room, int side, float u, float v, float depth, Color color )
		=> room.AddLight( room.OnWall( side, u, v, depth ), color, FloorContext.LampReach( v ), Vector3.Down, FloorContext.LampCone );

	/// <summary>Where a lamp of <see cref="AddLamps"/> goes along the walls, and how much brighter or dimmer than the rest it burns.</summary>
	protected readonly struct LampSpot
	{
		public int Side { get; init; }
		public float U { get; init; }
		public float Brightness { get; init; }
	}

	/// <summary>
	/// <see cref="AddLamps"/>'s placement on its own, for a theme that hangs a fitting where each lamp
	/// goes: the same draws from the same <paramref name="seed"/>, so the lamps land where they always have.
	/// </summary>
	protected static List<LampSpot> LampSpots( int count, int seed, float jitter = 0.25f )
	{
		const float Span = FloorContext.Span;
		const float Margin = 160f;
		var order = new[] { 0, 3, 2, 1 };
		var random = new Random( seed );
		var spacing = 4f * Span / count;
		var start = (float)random.NextDouble() * spacing;
		var spots = new List<LampSpot>();

		for ( var k = 0; k < count; k++ )
		{
			var along = start + (k + ((float)random.NextDouble() - 0.5f) * 2f * jitter) * spacing;
			along = (along % (4f * Span) + 4f * Span) % (4f * Span);
			var side = order[(int)(along / Span)];
			var u = Math.Clamp( along % Span, Margin, Span - Margin );
			spots.Add( new LampSpot { Side = side, U = u, Brightness = 0.94f + (float)random.NextDouble() * 0.12f } );
		}

		return spots;
	}

	/// <summary>
	/// A swatch of the trim atlas's infill band as a UV region, from pixel (<paramref name="x"/>,
	/// <paramref name="y"/>) — y counted from the band's top row — <paramref name="width"/> by
	/// <paramref name="height"/>, inset three pixels so the first mips never pull in the swatch beside it.
	/// Props wear these: a theme that hangs no infill pictures paints its props' materials there.
	/// </summary>
	public static Rect Swatch( float x, float y, float width, float height )
	{
		const float Inset = 3f;
		var top = TrimRows( TrimBand.Infill ).Top;
		return new Rect( (x + Inset) / TrimSize, (top + y + Inset) / TrimSize, (width - Inset * 2f) / TrimSize, (height - Inset * 2f) / TrimSize );
	}

	/// <summary>
	/// Bakes a surface painted as it looks onto the metal base. A metal's colour is its reflectance,
	/// and real metals reflect far more than their apparent grey suggests (zinc and steel about two
	/// thirds of the light): the grey comes from the room they mirror (the floor's reflection probe).
	/// So the painted colours are lifted <paramref name="lift"/> of the way toward white first; painted
	/// as-is, the metal mirrored the room at a third of its brightness and read near black.
	/// </summary>
	protected static PaintedSurface BakeMetal( Painter painter, string name, float normalStrength, float lift )
	{
		painter.Wash( Color.White, lift );
		return painter.Bake( name, Painter.Metal, normalStrength );
	}

	/// <summary>Smooth step from 0 at <paramref name="a"/> to 1 at <paramref name="b"/> (either order).</summary>
	protected static float Step( float a, float b, float t )
	{
		var x = Math.Clamp( (t - a) / (b - a), 0f, 1f );
		return x * x * (3f - 2f * x);
	}

	/// <summary>The dimensions of a room's structural front layer, in world units.</summary>
	protected readonly struct FrontStyle
	{
		/// <summary>Average post width (each post varies ±15 %).</summary>
		public float Post { get; init; }

		/// <summary>The wider pier in the middle of every wall, where the floor plaque hangs.</summary>
		public float Pier { get; init; }

		/// <summary>Width of a corner post's face on each of its two walls.</summary>
		public float Corner { get; init; }

		/// <summary>Height of the cornice beam under the ceiling.</summary>
		public float Cornice { get; init; }

		/// <summary>Skirting height above the ledge, and how far it stands off the wall face.</summary>
		public float Skirting { get; init; }
		public float SkirtingDepth { get; init; }
	}

	/// <summary>A recess between two posts on one wall: u from <see cref="From"/> to <see cref="To"/>.</summary>
	public readonly struct Bay
	{
		public int Side { get; init; }
		public float From { get; init; }
		public float To { get; init; }
		public float Width => To - From;
		public float Middle => (From + To) * 0.5f;
	}

	/// <summary>Average post spacing, centre to centre; the irregularity rule jitters every gap ±30 %.</summary>
	const float PostSpacing = 210f;

	/// <summary>
	/// The structural front layer every room shares, in the theme's own trim: a post at every corner,
	/// a wider pier mid-wall, posts between them at jittered spacing — a different stream per wall and
	/// per half, so no two walls and no two halves mirror — a cornice beam along the top, skirting in
	/// every bay. Posts and cornice stand in the <see cref="FloorContext.Recess"/> between the wall
	/// face and the collider plane; the bays between them are the alcoves later props sit in.
	/// Returns the bays, so a theme can fill some (windows, panels).
	/// </summary>
	protected List<Bay> BuildFrontLayer( FloorContext room, FrontStyle style )
	{
		var bays = new List<Bay>();
		var postsBySide = new List<(float From, float To)>[4];
		var post = TrimRegion( TrimBand.Pilaster );
		var stock = TrimRegion( TrimBand.Stock );
		var cornice = TrimRegion( TrimBand.Cornice );
		var skirting = TrimRegion( TrimBand.Skirting );
		var beam = FloorContext.ClearHeight - style.Cornice;
		const float Span = FloorContext.Span;
		const float Half = FloorContext.HalfSpan;
		const float Recess = FloorContext.Recess;
		const float Deep = Recess + FloorContext.Embed;

		// A bay wider than the repeat would show its first strip again at its far end; a modular
		// pattern repeats by design, so it takes any width.
		var widest = WallModule > 0f ? float.MaxValue : MaxBay;

		for ( var side = 0; side < 4; side++ )
		{
			var posts = new List<(float From, float To)> { (0f, style.Corner) };
			var pier = style.Pier * 0.5f;
			AddPosts( posts, style.Corner, Half - pier, style.Post, widest, new Random( Seed * 13 + side * 7919 + 3 ) );
			posts.Add( (Half - pier, Half + pier) );
			AddPosts( posts, Half + pier, Span - style.Corner, style.Post, widest, new Random( Seed * 17 + side * 6151 + 11 ) );

			// The corner this wall owns runs on to the wall face of the next, filling the corner square.
			posts.Add( (Span - style.Corner, Span + Deep) );

			foreach ( var (from, to) in posts )
				room.WallBox( side, Trim, post, stock, true, from, to, FloorContext.LedgeTop, beam, 0f, Deep, BoxFaces.Front | BoxFaces.Ends );

			postsBySide[side] = posts;

			for ( var k = 0; k + 1 < posts.Count; k++ )
			{
				var bay = new Bay { Side = side, From = posts[k].To, To = posts[k + 1].From };
				bays.Add( bay );
				room.WallBox( side, Trim, skirting, stock, false, bay.From, bay.To, FloorContext.LedgeTop, FloorContext.LedgeTop + style.Skirting,
					Recess - style.SkirtingDepth, Deep, BoxFaces.Front | BoxFaces.Top );
			}

			// Sides 0 and 2 run through both corner squares, 1 and 3 stop at them, so no two beams overlap.
			var (u0, u1) = side % 2 == 0 ? (-Deep, Span + Deep) : (0f, Span);
			room.WallBox( side, Trim, cornice, stock, false, u0, u1, beam, FloorContext.ClearHeight, 0f, Deep, BoxFaces.Front | BoxFaces.Bottom );
		}

		// The wall faces go on in order round the room — each wall's far end meets the next one's
		// start: +X, −Y, −X, +Y — with the walls each length wears carried over the corners.
		var huts = new List<int>();
		foreach ( var side in new[] { 0, 3, 2, 1 } )
			BuildWallFace( room, side, postsBySide[side], huts, side == 1 );

		return bays;
	}

	/// <summary>
	/// Which of the <see cref="Walls"/> each bay's length of wall face wears, by side and the bay's start,
	/// as <see cref="BuildWallFace"/> picked it: for a theme that dresses a bay to match its wall (a beach
	/// hut's awning in the hut's own colour).
	/// </summary>
	readonly Dictionary<(int Side, float From), int> _bayWalls = new();

	/// <summary>Which of the <see cref="Walls"/> the length of wall face behind <paramref name="bay"/> wears.</summary>
	protected int WallOf( Bay bay ) => _bayWalls[(bay.Side, bay.From)];

	/// <summary>
	/// The wall face behind the posts, from the segment's bottom to the ceiling, in lengths that meet
	/// at the middle of a post — hidden inside it, since a post's front and ends run from the collider
	/// plane to behind the face. Every length starts the wall's pattern somewhere else, so a wall that
	/// repeats every <see cref="WallRepeat"/> units never shows the same strip in two bays (with one
	/// start per wall, bays about a repeat wide all showed the same bands, 6a review): on a module
	/// edge where the pattern has one (<see cref="WallModule"/>), at its own height where it may
	/// (<see cref="WallLift"/>), never stretched (a non-modular bay is narrower than a repeat,
	/// <see cref="MaxBay"/>; a modular one repeats by design), and in its own one
	/// of the <see cref="Walls"/> (<see cref="PickWall"/>), or one of them either way round
	/// (<see cref="PickLook"/>) where the theme allows it. Faces of neighbouring walls meet only along
	/// the corner edges, so the end lengths run through both corners.
	/// </summary>
	void BuildWallFace( FloorContext room, int side, List<(float From, float To)> posts, List<int> huts, bool closing )
	{
		const float Recess = FloorContext.Recess;
		var random = new Random( Seed * 19 + room.Floor * 211 + side * 4099 + 5 );
		var bottom = room.BottomZ - room.TileTopZ;
		var start = -Recess;
		for ( var k = 1; k < posts.Count; k++ )
		{
			var end = k < posts.Count - 1 ? (posts[k].From + posts[k].To) * 0.5f : FloorContext.Span + Recess;
			var offset = WallModule > 0f
				? ModuleOffset( posts[k - 1].To, posts[k].From, random )
				: (float)random.NextDouble() * WallRepeat;
			var look = MirrorWalls ? PickLook( huts, closing && k == posts.Count - 1, random ) : PickWall( huts, closing ? posts.Count - k : 0, random ) * 2;
			var wall = look / 2;
			var mirror = look % 2 == 1 ? -1f : 1f;
			var lift = WallLift( wall, random );
			_bayWalls[(side, posts[k - 1].To)] = wall;

			// A mirrored length runs its pattern the other way; a module edge stays a module edge.
			room.WallFace( side, Walls[wall], start, end, bottom, FloorContext.ClearHeight, Recess, false,
				( u, v ) => new Vector2( mirror * (u + offset) / WallRepeat, (WallRepeat - v + lift) / WallRepeat ) );
			start = end;
		}
	}

	/// <summary>
	/// Which of the <see cref="Walls"/> the next length wears (no draw when there is one): never one
	/// of the previous two lengths', which run on round the corners in <paramref name="huts"/>, so
	/// neither a corner nor an A-B-A-B run repeats a wall. The last two lengths round the room
	/// (<paramref name="beforeEnd"/> 1 and 2) also keep clear of the first two, which they meet
	/// round the closing corner; and no choice may complete a second full round of a cycle of three
	/// or four (A-B-C-D-A-B-C-D reads as a pattern, not a row of huts). Those two give way, cycle
	/// first, when nothing else is left.
	/// </summary>
	int PickWall( List<int> huts, int beforeEnd, Random random )
	{
		if ( Walls.Length == 1 ) return 0;

		bool Recent( int wall, int back ) => huts.Count >= back && huts[^back] == wall;
		bool Closing( int wall ) => huts.Count > 1 && (beforeEnd == 1 && (huts[0] == wall || huts[1] == wall) || beforeEnd == 2 && huts[0] == wall);
		bool Cycle( int wall )
		{
			for ( var period = 3; period <= 4; period++ )
			{
				if ( huts.Count < period * 2 - 1 ) continue;
				var repeats = huts[^period] == wall;
				for ( var back = 1; repeats && back < period; back++ ) repeats = huts[^back] == huts[^(back + period)];
				if ( repeats ) return true;
			}
			return false;
		}

		var candidates = new List<int>();
		for ( var strict = 3; strict >= 0 && candidates.Count == 0; strict-- )
			for ( var wall = 0; wall < Walls.Length; wall++ )
			{
				if ( Recent( wall, 1 ) || strict >= 1 && Recent( wall, 2 ) ) continue;
				if ( strict >= 2 && Closing( wall ) || strict >= 3 && Cycle( wall ) ) continue;
				candidates.Add( wall );
			}

		var pick = candidates[random.Next( candidates.Count )];
		huts.Add( pick );
		return pick;
	}

	/// <summary>
	/// Which look the next length wears when walls may be mirrored (<see cref="MirrorWalls"/>): one of
	/// the <see cref="Walls"/> times two, plus one if mirrored. Any of them but the one the length before
	/// wore (carried round the corners in <paramref name="looks"/>), and for the length that closes the
	/// room (<paramref name="last"/>) not the first length's either, which it meets round the corner.
	/// Five choices at every step, so no run settles into a cycle.
	/// </summary>
	int PickLook( List<int> looks, bool last, Random random )
	{
		var candidates = new List<int>();
		for ( var look = 0; look < Walls.Length * 2; look++ )
			if ( looks.Count == 0 || look != looks[^1] && !(last && look == looks[0]) )
				candidates.Add( look );

		var pick = candidates[random.Next( candidates.Count )];
		looks.Add( pick );
		return pick;
	}

	/// <summary>
	/// A pattern start for the bay <paramref name="from"/>..<paramref name="to"/> that puts a module
	/// edge where no module is cut to a sliver: whole modules from the bay's start when the part left
	/// over is at least half a module (it ends the bay), otherwise the leftover plus one module split
	/// between the two ends. Which of the repeat's modules comes first is random.
	/// </summary>
	float ModuleOffset( float from, float to, Random random )
	{
		var module = WallModule;
		var width = to - from;
		var whole = MathF.Floor( width / module );
		var left = width - whole * module;
		var edge = from + (left >= module * 0.5f || whole < 1f ? 0f : (left + module) * 0.5f);
		var first = random.Next( (int)MathF.Round( WallRepeat / module ) ) * module;
		return ((first - edge) % WallRepeat + WallRepeat) % WallRepeat;
	}

	/// <summary>
	/// Posts between <paramref name="from"/> and <paramref name="to"/>: gaps jittered ±30 % and rescaled
	/// to fit exactly, widths ±15 %, and no bay wider than <paramref name="widest"/>. A draw that breaks
	/// that is drawn again from the same stream (a layout that keeps it is the same as ever), and after
	/// a few tries the run gets one more post.
	/// </summary>
	static void AddPosts( List<(float From, float To)> posts, float from, float to, float width, float widest, Random random )
	{
		var length = to - from;
		var segments = Math.Max( 1, (int)MathF.Round( length / PostSpacing ) );
		var placed = new List<(float From, float To)>();
		for ( var attempt = 1; ; attempt++ )
		{
			var weights = new float[segments];
			var sum = 0f;
			for ( var k = 0; k < segments; k++ ) sum += weights[k] = 1f + ((float)random.NextDouble() - 0.5f) * 0.6f;

			placed.Clear();
			var at = from;
			var fits = true;
			for ( var k = 0; k + 1 < segments; k++ )
			{
				at += length * weights[k] / sum;
				var half = width * (0.85f + (float)random.NextDouble() * 0.3f) * 0.5f;
				fits &= at - half - (k == 0 ? from : placed[k - 1].To) <= widest;
				placed.Add( (at - half, at + half) );
			}
			fits &= to - (segments > 1 ? placed[^1].To : from) <= widest;

			if ( fits ) break;
			if ( attempt % 8 == 0 ) segments++;
		}
		posts.AddRange( placed );
	}
}
