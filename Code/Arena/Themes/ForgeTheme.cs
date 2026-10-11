using System;
using System.Collections.Generic;

/// <summary>
/// Floor 8 (index 7): a forge over the lava. Underfoot, the tops of basalt columns — five-, six-
/// and seven-sided, each its own height, tone and weathering, dished or domed, pitted with gas
/// vesicles, dusted with ash — and, the one danger colour on any floor, thin ember cracks glowing in
/// a few of the joints, broken and short, with a scorched halo. Overhead (the undersides of the
/// dungeon floor, and of its own tiles where they face the lava shaft), fissured rock with a few
/// ember glints caught in the cracks.
/// </summary>
public sealed class ForgeTheme : ArenaTheme
{
	public override string Name => "Forge";
	public override int Floor => 7;
	public override Color LightColor => new( 1.60f, 0.62f, 0.36f );

	static Color Basalt => Palette.Hex( 0x4A4250 );
	static Color BasaltLight => Palette.Hex( 0x5E5566 );
	static Color BasaltShade => Palette.Hex( 0x352F3A );

	/// <summary>Weathered columns lean brown-grey; fresh ones toward the floor's dark violet.</summary>
	static Color BasaltBrown => Palette.Hex( 0x51473F );

	/// <summary>The joints between columns: near black.</summary>
	static Color Fissure => Palette.Hex( 0x1F1B22 );

	/// <summary>Ash settled on the stone.</summary>
	static Color Ash => Palette.Hex( 0x6E6770 );

	/// <summary>Scorched stone either side of a hot crack.</summary>
	static Color Scorch => Palette.Hex( 0x5A2E2A );

	static Color Rock => Palette.Hex( 0x3B3440 );
	static Color RockLight => Palette.Hex( 0x4D4553 );
	static Color RockDark => Palette.Hex( 0x2A2430 );
	static Color Soot => Palette.Hex( 0x1E1A20 );

	/// <summary>Wrought iron, sooted; its rubbed arrises and rivet heads a little brighter.</summary>
	static Color Iron => Palette.Hex( 0x57524F );
	static Color IronLight => Palette.Hex( 0x6E6863 );

	/// <summary>How strongly the ceiling glows on average with the light of the lava below (0..1 of its own colour).</summary>
	const float CeilingGlow = 0.3f;

	/// <summary>Column spacing in pixels: about 17 units, a 40 cm column.</summary>
	const float ColumnSpacing = 150f;

	/// <summary>Warm red of the light thrown up off the lava through the floor.</summary>
	static Color LavaBounce => new( 1.4f, 0.55f, 0.4f );

	/// <summary>
	/// Where the lava bounce lights stand: down the shaft just over the lava, a little way in from the
	/// middle of two opposite walls, mirrored.
	/// </summary>
	static readonly Vector2 BounceAt = new( 400f, 150f );

	/// <summary>How far under the forge's tile tops the bounce lights stand: a little over the lava, which lies about <see cref="Arena.LavaOffset"/> below.</summary>
	const float BounceDepth = Arena.LavaOffset - 30f;

	/// <summary>
	/// The bounce lights' cone and reach, which keep them out of the dungeon upstairs. Its walls start
	/// at least 488 above them (<see cref="BounceDepth"/> plus the forge's clear height) and 440 across,
	/// past the reach and well outside the cone. Its floor
	/// is only a slab's thickness above the ceiling they light, so the cone does it: a 45° chamfer
	/// facing a light below it is lit only from more than 45° off the vertical, and the cone stops well
	/// short of that (the dungeon keeps its chamfers free of painted relief that would tilt them
	/// further). The old fills lit those chamfers from below and drew every dungeon tile edge as a
	/// salmon line — the 6b review's "salmon seams". The forge's own walls lie outside the cone too,
	/// which leaves the ceiling as the one thing they light, and the undersides of the floor seen
	/// through holes. The reach runs <see cref="BounceOvershoot"/> past the ceiling, so the pools on
	/// the rock stay as bright whatever the room's height.
	/// </summary>
	const float BounceCone = 35f;
	const float BounceOvershoot = 132f;
	const float BounceReach = BounceDepth + FloorContext.ClearHeight + BounceOvershoot;

	/// <summary>
	/// Strength of each bounce light. Light falls off with the square of the distance it travels, so it
	/// scales with the throw from the lamp to the rock overhead, from the strength tuned (70) at a throw
	/// of 428 units, when the rooms were lower: the pools on the ceiling keep their brightness.
	/// </summary>
	static float BounceStrength => 70f * MathF.Pow( (BounceDepth + FloorContext.ClearHeight) / 428f, 2f );

	/// <summary>Facet size of the wall rock in pixels: ten facets across the wall, so the rock tiles; 128 px, about 27 units, the approved rock scale.</summary>
	const int WallFacet = WallSize / 10;

	/// <summary>
	/// The structural front layer, and the forge's light: four ember-orange lamps over the forge mouths
	/// (Task 10), thrown down onto the floor and the foot of the walls; and the glow of the lava below,
	/// two warm lights aimed up the shaft at the rock overhead (R32).
	/// </summary>
	public override void Dress( FloorContext room )
	{
		BuildFrontLayer( room, new FrontStyle { Post = 36f, Pier = 80f, Corner = 38f, Cornice = 30f, Skirting = 12f, SkirtingDepth = 6f } );
		AddLamps( room, 4, 150f, -40f, LightColor * 7f, Seed + 900 );
		foreach ( var sign in new[] { 1f, -1f } )
			room.AddLight( new Vector3( BounceAt.x * sign, BounceAt.y * sign, room.TileTopZ - BounceDepth ), LavaBounce * BounceStrength, BounceReach, Vector3.Up, BounceCone );
	}

	/// <summary>
	/// Three faces of rock (<see cref="ArenaTheme.Walls"/>), each with its own seams: a quiet face with a
	/// short crack or two low down, a face split by one seam the full height of the wall that branches on
	/// its way up to the cornice, and a face with two or three seams of middling height. One face for
	/// every length put the same few red lines in the same band of every bay (6b review). The lengths
	/// wear them either way round (<see cref="MirrorWalls"/>), so the tall seam does not come round in
	/// every third bay.
	/// </summary>
	public override int WallCount => 3;

	/// <summary>Rock has no handedness.</summary>
	protected override bool MirrorWalls => true;

	/// <summary>Rough rock overhead has no direction: its undersides turn with the tiles, sixteen looks instead of four.</summary>
	public override bool CeilingTurns => true;

	/// <summary>The wall's rock: a step lighter than the ceiling's, so its facets read in the forge's low light.</summary>
	static Color RockFace => Mix( Rock, RockLight, 0.5f );

	/// <summary>The white-hot middle of a seam, toward which its core is mixed.</summary>
	static Color HotCore => Palette.Hex( 0xFFC27A );

	/// <summary>Heat tarnish on the iron: a faint bronze and a faint blue.</summary>
	static Color TarnishBronze => Palette.Hex( 0x6B5638 );
	static Color TarnishBlue => Palette.Hex( 0x464C5E );

	/// <summary>
	/// Dark rock with lava seams, the forge's walls and the shaft below them: broken basalt faces (a
	/// jittered Voronoi of tilted facets that meet in soft creases, deep enough to catch the low lamps
	/// along their edges), faint strata, browner and paler patches, soot thickening toward the ceiling.
	/// Seams of lava — the one place the forge may use the danger colour — rise from the floor and
	/// branch, hot and wide at the foot, cooling and thinning higher up. They glow: the wall is painted
	/// onto the self-lit base with a glow mask over every seam (a faint wide ember halo, a brighter glow
	/// round the core, the core itself brightest), so the seams light themselves however dark the room
	/// is, where paint alone read as thin red lines drawn on black (6b review).
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		const float SootBand = 88f;
		var stream = Seed + 610 + variant * 31;
		var painter = new Painter( WallSize, RockFace, 0.9f );
		var random = new Random( stream );
		float Next() => (float)random.NextDouble();

		PaintFacets( painter, WallFacet, stream + 1, 1.1f, 0.7f );

		painter.Blend( Ramp( painter.Noise( 4, stream + 2, 4 ), 0.5f, 0.8f, 0.3f ), RockLight );
		painter.Blend( Ramp( painter.Noise( 6, stream + 3, 4 ), 0.55f, 0.85f, 0.3f ), RockDark );
		painter.Blend( Ramp( painter.Noise( 3, stream + 4, 3 ), 0.6f, 0.9f, 0.3f ), BasaltBrown );

		var strata = painter.Noise( 4, stream + 5, 2 );
		var bands = new float[WallSize * WallSize];
		for ( var y = 0; y < WallSize; y++ )
		for ( var x = 0; x < WallSize; x++ )
			bands[y * WallSize + x] = StreakX( strata, WallSize, x, y, 3 ) - 0.5f;
		painter.AddHeight( bands, 0.3f );
		painter.HeightNoise( 0.7f, 4, stream + 6, 4 );
		painter.HeightNoise( 0.2f, 96, stream + 7, 2 );

		// Soot from the forge hangs under the ceiling.
		var smoke = painter.Noise( 5, stream + 8, 3 );
		for ( var y = 0; y < WallSize; y++ )
		{
			var v = WallRepeat - y / Ppu;
			var high = v > WallRepeat - SootBand ? (v - WallRepeat + SootBand) / SootBand : 0f;
			for ( var x = 0; x < WallSize; x++ ) smoke[y * WallSize + x] = high * (0.25f + smoke[y * WallSize + x] * 0.35f);
		}
		painter.Blend( smoke, Soot );

		// This face's seams: how high each climbs (units), its core width at the foot (px), its branches.
		var layout = variant switch
		{
			0 => Next() < 0.6f
				? new[] { (25f + Next() * 30f, 2.4f + Next(), random.Next( 2 )), (15f + Next() * 20f, 2f + Next() * 0.8f, 0) }
				: new[] { (25f + Next() * 30f, 2.4f + Next(), random.Next( 2 )) },
			1 => new[] { (WallRepeat - 22f, 6f + Next(), 3 + random.Next( 2 )), (20f + Next() * 25f, 2.2f + Next(), 0) },
			_ => new[] { (70f + Next() * 70f, 4f + Next(), 1 + random.Next( 2 )), (60f + Next() * 60f, 3.4f + Next(), random.Next( 2 )), (30f + Next() * 40f, 2.6f + Next(), random.Next( 2 )) },
		};

		var seams = new List<Seam>();
		for ( var s = 0; s < layout.Length; s++ )
		{
			var (reach, width, branches) = layout[s];
			var foot = new Vector2( (s + 0.15f + Next() * 0.7f) * WallSize / layout.Length, WallSize - Next() * 20f );
			var path = Rise( foot, WallSize - reach * Ppu, 9f, random );
			seams.Add( new Seam( path, width, 1f, random ) );
			for ( var b = 0; b < branches && path.Length > 10; b++ )
			{
				var at = 3 + random.Next( path.Length * 3 / 4 );
				var heading = -MathF.PI * 0.5f + (Next() < 0.5f ? -1f : 1f) * (0.5f + Next() * 0.6f);
				var branch = Crack( path[at], heading, 7f, 6 + random.Next( 14 ), random );
				var t = at / (float)path.Length;
				seams.Add( new Seam( branch, width * (0.35f + Next() * 0.15f) * (1f - t * 0.5f), 0.8f - t * 0.3f, random ) );
			}
		}
		foreach ( var seam in seams ) seam.Cut( painter );

		painter.ShadeFromHeight( 0.45f, 0.5f );
		painter.Mottle( 0.1f, 32, stream + 9, 3 );
		painter.Cavity( 0.55f, 4 );
		painter.Grain( 0.015f, stream + 10 );

		// The cores once more over the cavity bake, which had darkened them as the lowest thing on the wall.
		foreach ( var seam in seams ) seam.Core( painter );
		return painter.Bake( $"arena_7_wall_{variant}", ArenaGenerator.GlowMaterialPath, 7f );
	}

	/// <summary>
	/// One lava seam along a polyline: its core is the given width at the start (the foot of the wall,
	/// or where a branch leaves its seam) and thins to a third at its end, pinching nearly shut here and
	/// there; its heat falls off along it the same way, so a seam cools as it climbs.
	/// </summary>
	sealed class Seam
	{
		readonly Vector2[] _path;
		readonly float[] _widths;
		readonly float[] _heats;

		public Seam( Vector2[] path, float width, float heat, Random random )
		{
			_path = path;
			_widths = new float[path.Length];
			_heats = new float[path.Length];
			for ( var k = 1; k < path.Length; k++ )
			{
				var t = (k - 0.5f) / (path.Length - 1);
				var pinch = random.NextDouble() < 0.15 ? 0.25f : 0.7f + (float)random.NextDouble() * 0.6f;
				_widths[k] = width * (1f - t * 0.7f) * pinch;
				_heats[k] = heat * (1f - t * 0.55f) * (0.6f + pinch * 0.4f);
			}
		}

		/// <summary>
		/// Cuts the seam into the rock: a broad scorched halo with a faint ember glow, a brighter glow
		/// round the core, then the core, sunk into the rock and glowing hottest.
		/// </summary>
		public void Cut( Painter painter )
		{
			for ( var k = 1; k < _path.Length; k++ )
			{
				var w = _widths[k]; var h = _heats[k];
				painter.Line( _path[k - 1], _path[k], w * 2.5f + 40f, Mix( Scorch, Palette.Lava, 0.2f ), 0.4f, glow: 0.6f * h );
				painter.Line( _path[k - 1], _path[k], w + 14f, Mix( Scorch, Palette.Lava, 0.6f ), 0.6f, glow: 0.9f * h );
			}
			for ( var k = 1; k < _path.Length; k++ )
			{
				painter.Line( _path[k - 1], _path[k], _widths[k] + 2f, Mix( Palette.Lava, Scorch, 0.3f ), 0.85f, height: 0.25f, roughness: 0.9f );
				painter.Line( _path[k - 1], _path[k], _widths[k] * 0.55f, Mix( Palette.Lava, HotCore, 0.35f ), 0.95f, height: 0.15f, roughness: 0.9f );
			}
			Core( painter );
		}

		/// <summary>The core's colour and glow alone, leaving the height as it is.</summary>
		public void Core( Painter painter )
		{
			for ( var k = 1; k < _path.Length; k++ )
			{
				var h = _heats[k];
				painter.Line( _path[k - 1], _path[k], _widths[k] + 2f, Mix( Palette.Lava, Scorch, 0.3f ), 0.85f, glow: 0.75f * h );
				painter.Line( _path[k - 1], _path[k], _widths[k] * 0.55f, Mix( Palette.Lava, HotCore, 0.35f ), 0.95f, glow: 0.9f * h );
			}
		}
	}

	/// <summary>
	/// A crack climbing from <paramref name="foot"/> until it passes pixel row <paramref name="top"/>:
	/// short straight runs that kink sharply, drifting about straight up.
	/// </summary>
	static Vector2[] Rise( Vector2 foot, float top, float step, Random random )
	{
		var points = new List<Vector2> { foot };
		const float Heading = -MathF.PI * 0.5f;
		var angle = Heading;
		while ( points[^1].y > top )
		{
			angle = Heading + (angle - Heading) * 0.6f + ((float)random.NextDouble() - 0.5f) * 1.6f;
			points.Add( points[^1] + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * step * (0.5f + (float)random.NextDouble()) );
		}
		return points.ToArray();
	}

	/// <summary>A jagged crack: short straight runs that kink sharply, drifting about a heading.</summary>
	static Vector2[] Crack( Vector2 start, float heading, float step, int steps, Random random )
	{
		var points = new Vector2[steps + 1];
		points[0] = start;
		var angle = heading;
		for ( var s = 1; s <= steps; s++ )
		{
			angle = heading + (angle - heading) * 0.6f + ((float)random.NextDouble() - 0.5f) * 1.6f;
			points[s] = points[s - 1] + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * step * (0.5f + (float)random.NextDouble());
		}
		return points;
	}

	/// <summary>
	/// Wrought iron for the front layer, smith's work: riveted plate posts with edge straps, a lintel
	/// with a row of rivets under the ceiling, an iron kerb at the skirting, a bolted angle on the slab
	/// edge. Soot in the hollows, heat tarnish — a faint bronze and a faint blue — in patches, the
	/// arrises rubbed bright. Baked matte, as sooted iron is: on the metal base it showed only what the
	/// forge's dark reflection probe gave it and read pure black away from the lamps (6b re-review);
	/// matte, it reads as dark oxide in the room's ambient light and still takes a sheen from the lamps.
	/// </summary>
	protected override PaintedSurface PaintTrim()
	{
		var painter = new Painter( TrimSize, Iron, 0.5f );
		PaintBand( painter, TrimBand.Cornice, Iron, CorniceProfile, 0.5f );
		PaintBand( painter, TrimBand.Skirting, Mix( Iron, Soot, 0.2f ), SkirtingProfile, 0.55f );
		PaintBand( painter, TrimBand.Pilaster, Iron, StrapProfile, 0.5f );
		PaintBand( painter, TrimBand.SlabEdge, Mix( Iron, Soot, 0.15f ), NosingProfile, 0.5f );
		PaintBand( painter, TrimBand.Stock, Iron, _ => 0.8f, 0.5f );
		PaintBand( painter, TrimBand.Infill, Iron, _ => 0.8f, 0.5f );
		ShadeBand( painter, TrimBand.Cornice, CorniceProfile, Soot, 0.6f );
		ShadeBand( painter, TrimBand.Pilaster, StrapProfile, Soot, 0.5f );
		ShadeBand( painter, TrimBand.Skirting, SkirtingProfile, Soot, 0.5f );

		// Rivets: along both straps of the posts, a row under the lintel's fillet, bolts on the angle.
		const float Pitch = TrimSize / 16f;
		var (ctop, cheight) = TrimRows( TrimBand.Cornice );
		var (ptop, pheight) = TrimRows( TrimBand.Pilaster );
		var (stop, sheight) = TrimRows( TrimBand.SlabEdge );
		for ( var k = 0; k < 16; k++ )
		{
			var x = k * Pitch + Pitch * 0.5f;
			painter.Circle( x, ctop + cheight * 0.76f, 3.4f, IronLight, height: 1.25f, bevel: 2.8f, roughness: 0.4f );
			painter.Circle( x, ptop + pheight * 0.08f, 3.2f, IronLight, height: 1.25f, bevel: 2.6f, roughness: 0.4f );
			painter.Circle( x + Pitch * 0.5f, ptop + pheight * 0.92f, 3.2f, IronLight, height: 1.25f, bevel: 2.6f, roughness: 0.4f );
			if ( k % 3 == 0 ) painter.Circle( x, stop + sheight * 0.6f, 4.5f, IronLight, height: 1.3f, bevel: 3f, roughness: 0.4f );
		}

		// Heat tarnish and soot in broad, faint patches.
		painter.Blend( Ramp( painter.Noise( 6, Seed + 710, 3 ), 0.6f, 0.85f, 0.25f ), TarnishBronze );
		painter.Blend( Ramp( painter.Noise( 5, Seed + 711, 3 ), 0.65f, 0.9f, 0.2f ), TarnishBlue );
		painter.Blend( Ramp( painter.Noise( 4, Seed + 712, 3 ), 0.5f, 0.85f, 0.28f ), Soot, roughness: 0.8f );

		painter.HeightNoise( 0.1f, 32, Seed + 713, 3 );
		painter.Mottle( 0.06f, 8, Seed + 714, 3 );
		painter.RoughnessNoise( 0.08f, 12, Seed + 715, 2 );
		painter.Cavity( 0.45f, 3 );
		painter.EdgeWear( 0.45f, 3, Seed + 716 );
		painter.Grain( 0.012f, Seed + 717 );
		painter.Wash( Soot, 0.18f );
		return painter.Bake( "arena_7_trim", Painter.Matte, 4f );
	}

	/// <summary>An iron post's face: straps down both edges, a hair proud of the plate between, their arrises rounded.</summary>
	static float StrapProfile( float t )
	{
		var edge = MathF.Min( t, 1f - t );
		return edge < 0.03f ? 0.75f + edge * 8f : edge < 0.17f ? 1f : edge < 0.19f ? 1f - (edge - 0.17f) / 0.02f * 0.18f : 0.82f;
	}

	protected override PaintedSurface PaintFloor()
	{
		var painter = new Painter( FloorSize, Fissure, 0.85f );
		var random = new Random( Seed );
		float Next() => (float)random.NextDouble();

		PaintColumns( painter, random );

		// Vesicles: gas bubbles frozen in the lava, small pits scattered over the tops.
		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var pits = 110 + random.Next( 60 );
			for ( var p = 0; p < pits; p++ )
			{
				var at = min + new Vector2( 30f + Next() * 452f, 30f + Next() * 452f );
				var radius = 0.6f + Next() * Next() * 2.2f;
				painter.Circle( at.x, at.y, radius, Mix( Basalt, Fissure, 0.6f ), 0.75f, height: 0.82f, bevel: radius, roughness: 0.95f );
			}
		}

		// Ash in drifts across the floor.
		painter.Blend( Ramp( painter.Noise( 5, Seed + 30, 2 ), 0.58f, 0.85f, 0.3f ), Ash, roughness: 0.95f );

		painter.HeightNoise( 0.45f, 8, Seed + 31, 2 );
		painter.Mottle( 0.08f, 4, Seed + 32, 3 );
		painter.RoughnessNoise( 0.06f, 6, Seed + 33, 2 );
		painter.Cavity( 0.5f, 4 );
		painter.EdgeWear( 0.3f, 3, Seed + 34 );
		painter.Grain( 0.015f, Seed + 35 );
		return painter.Bake( "arena_7_floor", Painter.Matte, 7f );
	}

	/// <summary>
	/// The column tops, per pixel: a jittered hexagonal lattice of seed points (so cells come out
	/// with five to seven sides), the nearest seed owning the pixel and the distance to the bisector
	/// with the second nearest giving the joint and a weathered shoulder. Columns stand at their own
	/// heights; some tops are dished, some domed. A few joints — chosen per pair of columns — carry a
	/// thin ember crack, broken by noise so it glows in short runs, with scorched stone beside it.
	/// The block's edge is a cut, unwarped, so neighbouring tiles meet at the chamfer's foot.
	/// </summary>
	void PaintColumns( Painter painter, Random random )
	{
		const int cell = FloorSize / 2;
		const float rowSpacing = ColumnSpacing * 0.866f;
		var rows = (int)(cell / rowSpacing) + 3;
		var columns = (int)(cell / ColumnSpacing) + 3;
		var chips = painter.Noise( 40, Seed + 20, 1 );
		var glow = painter.Noise( 24, Seed + 21, 2 );

		// Joint and ember weights reuse the chips and glow fields: each is read only at its own pixel.
		var relief = new float[FloorSize * FloorSize];
		var owners = new byte[FloorSize * FloorSize];
		var scorch = new float[FloorSize * FloorSize];
		var joint = chips; var ember = glow;
		var palette = new Color[256];
		var pointX = new float[rows * columns];
		var pointY = new float[rows * columns];
		var lift = new float[rows * columns];
		var dish = new float[rows * columns];

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );

			// Lattice points from one row and one column before the cell, so columns run off its edge.
			for ( var r = 0; r < rows; r++ )
			for ( var q = 0; q < columns; q++ )
			{
				var k = r * columns + q;
				var h = Hash( r, q, Seed + c * 13 );
				pointX[k] = (q - 1 + (r % 2 == 0 ? 0f : 0.5f) + (Bits( h, 0 ) - 0.5f) * 0.55f) * ColumnSpacing + (c * 37 % 60);
				pointY[k] = (r - 1 + (Bits( h, 8 ) - 0.5f) * 0.5f) * rowSpacing + (c * 53 % 50);
				lift[k] = (Bits( h, 16 ) - 0.5f) * 0.24f;
				dish[k] = (Bits( h, 24 ) - 0.45f) * 0.3f;

				var tone = Bits( h, 4 ) * 2f - 1f;
				var color = tone > 0f ? Mix( Basalt, BasaltLight, tone * 0.5f ) : Mix( Basalt, BasaltShade, -tone * 0.55f );
				palette[1 + c * 64 + k] = Mix( color, BasaltBrown, 0.15f + Bits( h, 12 ) * 0.6f );
			}

			var footInset = 6f + (float)random.NextDouble() * 2f;
			for ( var y = 0; y < cell; y++ )
			{
				var row = ((int)min.y + y) * FloorSize + (int)min.x;
				var py = y + 0.5f;
				var r0 = (int)(py / rowSpacing) + 1;
				for ( var x = 0; x < cell; x++ )
				{
					var i = row + x;
					var px = x + 0.5f;
					var q0 = (int)(px / ColumnSpacing) + 1;

					// Nearest and second-nearest seeds among the 3 x 3 lattice neighbours.
					int first = 0, second = 0;
					float best = float.MaxValue, next = float.MaxValue;
					for ( var r = Math.Max( r0 - 1, 0 ); r <= Math.Min( r0 + 1, rows - 1 ); r++ )
					for ( var q = Math.Max( q0 - 1, 0 ); q <= Math.Min( q0 + 1, columns - 1 ); q++ )
					{
						var k = r * columns + q;
						var dx = px - pointX[k]; var dy = py - pointY[k];
						var dd = dx * dx + dy * dy;
						if ( dd < best ) { next = best; second = first; best = dd; first = k; }
						else if ( dd < next ) { next = dd; second = k; }
					}

					// Distance to the bisector between the two: exact for the shared edge.
					var sx = pointX[second] - pointX[first]; var sy = pointY[second] - pointY[first];
					var split = (next - best) / (2f * MathF.Sqrt( sx * sx + sy * sy ));
					var chip = chips[i];
					var halfJoint = 1f + chip * 2.2f;
					var foot = MathF.Min( MathF.Min( px, cell - px ), MathF.Min( py, cell - py ) ) - footInset - (chip - 0.5f) * 3f;
					var edge = MathF.Min( split - halfJoint, foot );

					// Hot joints: about one pair in six, and only where the glow noise lets it show.
					var hot = 0f;
					if ( split < 10f && foot > 4f && (Hash( Math.Min( first, second ), Math.Max( first, second ), Seed + c * 17 ) & 0xFF) < 44 )
						hot = MathF.Min( MathF.Max( (glow[i] - 0.42f) * 5f, 0f ), 1f );
					joint[i] = 0f; ember[i] = 0f;

					if ( edge <= 0f )
					{
						joint[i] = 1f;
						relief[i] = 0.35f;
						ember[i] = split < 1.1f ? hot * 0.6f : split < 2f ? hot * 0.6f * (2f - split) / 0.9f * 0.5f : 0f;
						continue;
					}

					// A weathered shoulder, then the column's own top: its height, dished or domed.
					var t = edge >= 7f ? 1f : edge / 7f;
					var shoulder = t * t * (3f - 2f * t);
					var toCentre = MathF.Min( MathF.Sqrt( best ) / (ColumnSpacing * 0.5f), 1f );
					relief[i] = 0.4f + shoulder * (0.6f + lift[first] - dish[first] * (1f - toCentre * toCentre));
					owners[i] = (byte)(1 + c * 64 + first);
					if ( split < 10f ) scorch[i] = hot * (1f - split / 10f) * 0.5f;
				}
			}
		}

		painter.AddHeight( relief, 1f );
		painter.Blend( owners, palette );
		painter.Blend( joint, Fissure );
		painter.Blend( scorch, Scorch );
		painter.Blend( ember, Palette.Lava, roughness: 0.9f );
	}

	/// <summary>
	/// Rough-hewn rock seen from below, dusted pale with ash (bare basalt overhead read black): each
	/// underside a slab with a broken outline, its face broken into chipped facets (a jittered Voronoi,
	/// every facet a tilted plane with its own tone), and across most of them one long fissure that runs
	/// along the facets' borders, the way rock splits, opening in the middle and closing to nothing at
	/// both ends. Nothing on a slab is meant to be found again on the next: the undersides turn with the
	/// tiles (<see cref="CeilingTurns"/>) and no cell holds a compact mark.
	///
	/// The ceiling lights itself — the lava's glow from far below (R33) — and self-illumination is flat:
	/// it shows the colour as painted, with none of the normal map's shading. So the shading the relief
	/// would get from that low light is baked into the glow (<see cref="ReliefGlow"/>): facets turn
	/// lighter and darker, a fissure's near wall and lip catch it and its far wall falls into shade,
	/// where the colour alone drew the cut as a dark ink line (6b re-review).
	/// </summary>
	protected override PaintedSurface PaintCeiling()
	{
		const int cell = CeilingSize / 2;
		var painter = new Painter( CeilingSize, Soot, 0.9f );
		var random = new Random( Seed + 500 );
		float Next() => (float)random.NextDouble();

		// Each underside its own slab, a little lighter or darker, catching a little more or less of the glow.
		var slabGlow = new float[4];
		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );
			var tone = Next() * 2f - 1f;
			var rock = tone < 0f ? Mix( Rock, RockDark, -tone * 0.4f ) : Mix( Rock, RockLight, tone * 0.4f );
			rock = Mix( rock, Ash, 0.3f );
			painter.Fill( (int)min.x, (int)min.y, cell, cell, Mix( rock, Soot, 0.3f ), height: 0.4f );
			painter.Polygon( SlabOutline( min, cell, 3f, 4f, 10f, Seed + 500 + c * 101 ), rock, height: 1f, bevel: 14f, roughness: 0.88f );
			slabGlow[c] = 0.88f + Next() * 0.24f;
		}

		// The slabs' own relief is kept out of the baked glow shading: their rims would light up on
		// whichever side the tile happens to be turned to.
		var slabHeight = new float[CeilingSize * CeilingSize];
		painter.CopyHeight( slabHeight );

		var borders = new float[CeilingSize * CeilingSize];
		PaintFacets( painter, CeilingFacet, Seed + 540, 0.8f, 0.3f, 0.45f, borders );
		PaintFacets( painter, CeilingFacet / 2, Seed + 541, 0.4f, 0.1f, 0.2f );

		// One fissure across most undersides, now and then a second, shorter one; one underside left whole.
		var cut = new float[CeilingSize * CeilingSize];
		var lip = new float[CeilingSize * CeilingSize];
		var whole = random.Next( 4 );
		for ( var c = 0; c < 4; c++ )
		{
			if ( c == whole ) continue;
			var min = CellOrigin( c, CeilingSize );
			var fissures = Next() < 0.35f ? 2 : 1;
			for ( var f = 0; f < fissures; f++ )
			{
				// From near one side of the slab across it, bending gently.
				var side = random.Next( 4 );
				var along = 50f + Next() * (cell - 100f);
				var start = min + side switch
				{
					0 => new Vector2( along, FissureInset ),
					1 => new Vector2( cell - FissureInset, along ),
					2 => new Vector2( along, cell - FissureInset ),
					_ => new Vector2( FissureInset, along ),
				};
				var heading = side * MathF.PI * 0.5f + MathF.PI * 0.5f + (Next() - 0.5f) * 1.1f;
				var steps = f == 0 ? 8 + random.Next( 3 ) : 4 + random.Next( 3 );
				var guide = Clip( Wander( start, heading, 24f, steps, 0.3f, random ), min, cell, FissureInset );
				var glint = OpenFissure( cut, lip, borders, guide, (f == 0 ? 3.4f : 2.2f) + Next() * 0.9f, min, cell, random );
				if ( glint >= 0 && Next() < 0.5f )
				{
					var (gx, gy) = (glint % CeilingSize + 0.5f, glint / CeilingSize + 0.5f);
					painter.Circle( gx, gy, 2.4f, Scorch, 0.2f );
					painter.Circle( gx, gy, 0.8f, Palette.Lava, 0.55f, roughness: 0.9f, glow: 0.9f );
				}
			}
		}

		// The cut sinks deep with a faint raised lip; its colour darkens only a little and the lip pales.
		for ( var i = 0; i < lip.Length; i++ ) if ( cut[i] > 0f ) lip[i] = 0f;
		painter.AddHeight( cut, -0.55f );
		painter.AddHeight( lip, 0.12f );
		for ( var i = 0; i < cut.Length; i++ ) cut[i] *= 0.1f;
		painter.Blend( cut, Soot );
		for ( var i = 0; i < lip.Length; i++ ) lip[i] *= 0.1f;
		painter.Blend( lip, RockLight );

		// Soot in faint drifts; overhead, where the forge's lamps reach least, full drifts turned the rock black.
		painter.Blend( Ramp( painter.Noise( 4, Seed + 511, 3 ), 0.6f, 0.85f, 0.18f ), Soot );
		painter.HeightNoise( 0.3f, 8, Seed + 512, 3 );
		painter.Mottle( 0.05f, 6, Seed + 513, 3 );
		painter.Cavity( 0.25f, 3 );
		painter.Grain( 0.012f, Seed + 514 );

		// The glow of the lava far below: low, a little uneven from slab to slab and across a slab, and
		// shaded by the rock's relief. The bounce lights warm two pools of it; a light shining up through
		// the slab to warm the rest would light the dungeon's floor as well.
		var glow = ReliefGlow( painter, slabHeight );
		var drift = painter.Noise( 3, Seed + 515, 2 );
		for ( var y = 0; y < CeilingSize; y++ )
		for ( var x = 0; x < CeilingSize; x++ )
		{
			var i = y * CeilingSize + x;
			glow[i] *= CeilingGlow * slabGlow[(y / cell) * 2 + x / cell] * (0.9f + 0.2f * drift[i]);
		}
		painter.AddGlow( glow );
		return painter.Bake( "arena_7_ceiling", ArenaGenerator.GlowMaterialPath, 6f );
	}

	/// <summary>The ceiling's facets in pixels: about 14 units, a fist-to-forearm chip of rock.</summary>
	const int CeilingFacet = 64;

	/// <summary>How far in from its underside's edge a fissure stays, in pixels: clear of the slab's bevelled rim.</summary>
	const float FissureInset = 30f;

	/// <summary>
	/// Paints one fissure into <paramref name="cut"/> (depth 0..1) and <paramref name="lip"/> (0..1). The
	/// crack runs from the <paramref name="guide"/>'s start to its end the way rock splits, along the
	/// facets' borders: the cheapest path across the underside, where a pixel on a border
	/// (<paramref name="border"/>, distance in px) costs little, one inside a facet a lot, and straying
	/// far from the guide more and more. It opens up to <paramref name="halfWidth"/> px either side in its
	/// middle, swells and pinches along the way and closes to nothing at both ends. Returns the pixel
	/// where it is widest (for an ember caught there), or −1 if the guide was too short to open.
	/// </summary>
	static int OpenFissure( float[] cut, float[] lip, float[] border, Vector2[] guide, float halfWidth, Vector2 min, float cell, Random random )
	{
		if ( guide.Length < 3 ) return -1;
		const int size = CeilingSize;
		const float LipWidth = 2.5f;

		// The search runs over the underside inside its rim.
		var x0 = (int)min.x + 16; var y0 = (int)min.y + 16;
		var span = (int)cell - 32;
		var count = span * span;
		var step = new float[count];
		for ( var ly = 0; ly < span; ly++ )
		for ( var lx = 0; lx < span; lx++ )
		{
			var px = x0 + lx + 0.5f; var py = y0 + ly + 0.5f;
			var near = float.MaxValue;
			for ( var k = 1; k < guide.Length; k++ )
			{
				var ax = guide[k - 1].x; var ay = guide[k - 1].y;
				var ex = guide[k].x - ax; var ey = guide[k].y - ay;
				var t = Math.Clamp( ((px - ax) * ex + (py - ay) * ey) / MathF.Max( ex * ex + ey * ey, 0.0001f ), 0f, 1f );
				var dx = px - ax - ex * t; var dy = py - ay - ey * t;
				near = MathF.Min( near, dx * dx + dy * dy );
			}
			var stray = MathF.Max( MathF.Sqrt( near ) - 10f, 0f ) / 14f;
			var inside = Math.Clamp( border[(y0 + ly) * size + x0 + lx] / 2.5f, 0f, 1f );
			step[ly * span + lx] = 1f + 6f * inside * inside * (3f - 2f * inside) + stray * stray;
		}

		int Node( Vector2 p ) => Math.Clamp( (int)p.y - y0, 0, span - 1 ) * span + Math.Clamp( (int)p.x - x0, 0, span - 1 );
		var source = Node( guide[0] ); var target = Node( guide[^1] );

		// Dijkstra over the eight neighbours, on a binary heap of (cost, node) big enough for one push per
		// neighbour of every pixel: a pixel is settled once, so each of its eight steps improves a cost once at most.
		var cost = new float[count]; Array.Fill( cost, float.MaxValue );
		var from = new int[count]; Array.Fill( from, -1 );
		var heapCost = new float[count * 8]; var heapNode = new int[count * 8]; var heapSize = 0;
		void Push( float c, int n )
		{
			var k = heapSize++;
			while ( k > 0 && heapCost[(k - 1) / 2] > c ) { heapCost[k] = heapCost[(k - 1) / 2]; heapNode[k] = heapNode[(k - 1) / 2]; k = (k - 1) / 2; }
			heapCost[k] = c; heapNode[k] = n;
		}
		cost[source] = 0f; Push( 0f, source );
		while ( heapSize > 0 )
		{
			var c = heapCost[0]; var n = heapNode[0];
			var lastCost = heapCost[--heapSize]; var lastNode = heapNode[heapSize];
			var k = 0;
			while ( true )
			{
				var child = k * 2 + 1;
				if ( child >= heapSize ) break;
				if ( child + 1 < heapSize && heapCost[child + 1] < heapCost[child] ) child++;
				if ( heapCost[child] >= lastCost ) break;
				heapCost[k] = heapCost[child]; heapNode[k] = heapNode[child]; k = child;
			}
			heapCost[k] = lastCost; heapNode[k] = lastNode;

			if ( c > cost[n] ) continue;
			if ( n == target ) break;
			var nx = n % span; var ny = n / span;
			for ( var oy = -1; oy <= 1; oy++ )
			for ( var ox = -1; ox <= 1; ox++ )
			{
				if ( ox == 0 && oy == 0 ) continue;
				var mx = nx + ox; var my = ny + oy;
				if ( (uint)mx >= (uint)span || (uint)my >= (uint)span ) continue;
				var m = my * span + mx;
				var next = c + (step[n] + step[m]) * (ox != 0 && oy != 0 ? 0.7071f : 0.5f);
				if ( next >= cost[m] ) continue;
				cost[m] = next; from[m] = n; Push( next, m );
			}
		}
		if ( from[target] < 0 ) return -1;

		var path = new List<int>();
		for ( var n = target; n >= 0; n = from[n] ) path.Add( n );
		path.Reverse();

		// Its width along the way: open in the middle, closed at both ends, swelling and pinching.
		var swell = new float[path.Count];
		var level = 1f; var aim = 1f;
		for ( var s = 0; s < path.Count; s++ )
		{
			if ( s % 12 == 0 ) aim = 0.55f + (float)random.NextDouble() * 0.7f;
			level += (aim - level) * 0.15f;
			swell[s] = level;
		}

		var widest = -1; var best = 0f;
		for ( var s = 0; s < path.Count; s++ )
		{
			var taper = MathF.Pow( MathF.Sin( (s + 0.5f) / path.Count * MathF.PI ), 0.8f );
			var w = halfWidth * taper * swell[s];
			if ( w < 0.35f ) continue;
			var cx = x0 + path[s] % span + 0.5f; var cy = y0 + path[s] / span + 0.5f;
			var reach = (int)MathF.Ceiling( w + LipWidth );
			for ( var y = (int)cy - reach; y <= (int)cy + reach; y++ )
			for ( var x = (int)cx - reach; x <= (int)cx + reach; x++ )
			{
				var dx = x + 0.5f - cx; var dy = y + 0.5f - cy;
				var d = MathF.Sqrt( dx * dx + dy * dy );
				var i = y * size + x;
				if ( d < w )
				{
					var depth = (1f - d * d / (w * w)) * MathF.Min( w / 2f, 1f );
					if ( depth > cut[i] ) cut[i] = depth;
				}
				else if ( d < w + LipWidth )
				{
					var rise = (1f - (d - w) / LipWidth) * MathF.Min( w / 2f, 1f );
					if ( rise > lip[i] ) lip[i] = rise;
				}
			}
			if ( w > best ) { best = w; widest = (int)cy * size + (int)cx; }
		}

		return widest;
	}

	/// <summary>A polyline cut off where it first comes within <paramref name="inset"/> px of its underside's edge.</summary>
	static Vector2[] Clip( Vector2[] points, Vector2 min, float size, float inset )
	{
		var count = 1;
		while ( count < points.Length )
		{
			var p = points[count];
			if ( p.x < min.x + inset || p.y < min.y + inset || p.x > min.x + size - inset || p.y > min.y + size - inset ) break;
			count++;
		}
		return points[..count];
	}

	/// <summary>Direction of the low light whose shading the ceiling's glow carries, in texture space (x right, y down, z out of the rock).</summary>
	static readonly Vector3 GlowLight = new Vector3( -0.5f, -0.35f, 0.8f ).Normal;

	/// <summary>
	/// The relief shading of a low light, per pixel, as a glow factor around 1 on flat rock: from the
	/// height's slope over a few pixels (facets, the walls of a fissure and its lip, not the grain),
	/// minus <paramref name="exclude"/> (the slab rims, which would light up on whatever side a tile
	/// is turned to). Shaded slopes keep a little glow, so nothing goes black.
	/// </summary>
	static float[] ReliefGlow( Painter painter, float[] exclude )
	{
		const int size = CeilingSize;
		const float Steepness = 8f;
		var height = new float[size * size];
		painter.CopyHeight( height );
		for ( var i = 0; i < height.Length; i++ ) height[i] -= exclude[i];

		var shade = new float[size * size];
		for ( var y = 0; y < size; y++ )
		{
			var row = y * size;
			var above = ((y - 2 + size) % size) * size; var below = ((y + 2) % size) * size;
			for ( var x = 0; x < size; x++ )
			{
				var nx = -(height[row + (x + 2) % size] - height[row + (x - 2 + size) % size]) * Steepness;
				var ny = -(height[below + x] - height[above + x]) * Steepness;
				var lit = (nx * GlowLight.x + ny * GlowLight.y + GlowLight.z) / MathF.Sqrt( nx * nx + ny * ny + 1f ) / GlowLight.z;
				shade[row + x] = 0.3f + 0.7f * Math.Clamp( lit, 0f, 1.7f );
			}
		}
		return shade;
	}

	/// <summary>
	/// Chipped facets over a whole surface: a jittered grid of points <paramref name="facet"/> px apart,
	/// each pixel belonging to the nearest; a facet is a plane tilted its own way (height, scaled by
	/// <paramref name="relief"/>, and the same slope as a light-to-dark gradient in colour) with its own
	/// tone, spread by <paramref name="spread"/>. Near the border with the next-nearest facet both planes blend to their mean, and the border
	/// sinks into a slight crease: two planes meeting in a hard height step made a cliff one pixel wide
	/// in the normal map, which shimmered as a saw-tooth along every border under direct light.
	/// Given <paramref name="borders"/>, it also records per pixel the distance to the nearest border,
	/// so cracks can be run along them.
	/// </summary>
	void PaintFacets( Painter painter, int facet, int seed, float relief, float tone, float spread = 0.8f, float[] borders = null )
	{
		var size = painter.Size;
		var grid = size / facet + 2;
		var pointX = new float[grid * grid];
		var pointY = new float[grid * grid];
		var slopeX = new float[grid * grid];
		var slopeY = new float[grid * grid];
		var tones = new float[grid * grid];
		var cells = size / facet;
		for ( var gy = 0; gy < grid; gy++ )
		for ( var gx = 0; gx < grid; gx++ )
		{
			// Points one cell beyond each edge repeat the cells on the far side, so facets tile.
			var k = gy * grid + gx;
			var h = Hash( (gx + cells - 1) % cells, (gy + cells - 1) % cells, seed );
			pointX[k] = (gx - 1 + 0.1f + Bits( h, 0 ) * 0.8f) * facet;
			pointY[k] = (gy - 1 + 0.1f + Bits( h, 8 ) * 0.8f) * facet;
			var angle = Bits( h, 16 ) * MathF.Tau;
			slopeX[k] = MathF.Cos( angle ) / facet;
			slopeY[k] = MathF.Sin( angle ) / facet;
			tones[k] = Bits( h, 24 ) - 0.5f;
		}

		var height = new float[size * size];
		var light = new float[size * size];
		var dark = new float[size * size];
		var blend = facet * 0.18f;
		for ( var y = 0; y < size; y++ )
		{
			var gy = y / facet + 1;
			for ( var x = 0; x < size; x++ )
			{
				var gx = x / facet + 1;
				var px = x + 0.5f; var py = y + 0.5f;
				float best = float.MaxValue, next = float.MaxValue;
				int first = 0, second = 0;
				for ( var oy = gy - 1; oy <= gy + 1; oy++ )
				for ( var ox = gx - 1; ox <= gx + 1; ox++ )
				{
					var k = oy * grid + ox;
					var dx = px - pointX[k]; var dy = py - pointY[k];
					var dd = dx * dx + dy * dy;
					if ( dd < best ) { next = best; second = first; best = dd; first = k; }
					else if ( dd < next ) { next = dd; second = k; }
				}

				// Distance to the border with the second facet, and each facet's plane at this pixel.
				var sx = pointX[second] - pointX[first]; var sy = pointY[second] - pointY[first];
				var split = (next - best) / (2f * MathF.Sqrt( sx * sx + sy * sy ));
				var own = (px - pointX[first]) * slopeX[first] + (py - pointY[first]) * slopeY[first];
				var other = (px - pointX[second]) * slopeX[second] + (py - pointY[second]) * slopeY[second];
				var w = split >= blend ? 1f : split / blend;
				w = w * w * (3f - 2f * w);
				var tilt = (own + other) * 0.5f + (own - other) * 0.5f * w;
				var shade = (tones[first] + tones[second]) * 0.5f + (tones[first] - tones[second]) * 0.5f * w;

				var i = y * size + x;
				height[i] = tilt * relief - (1f - w) * 0.06f;
				if ( borders is not null ) borders[i] = split;
				var t = shade * spread + tilt * 0.45f * tone / 0.5f;
				if ( t > 0f ) light[i] = MathF.Min( t, 1f ) * 0.6f; else dark[i] = MathF.Min( -t, 1f ) * 0.6f;
			}
		}

		painter.AddHeight( height, 1f );
		painter.Blend( light, RockLight );
		painter.Blend( dark, RockDark );
	}
}
