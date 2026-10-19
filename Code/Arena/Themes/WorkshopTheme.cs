using System;
using System.Collections.Generic;

/// <summary>
/// Floor 6 (index 5): a workshop. Underfoot, steel checker plate — lens-shaped tread bosses in
/// alternating directions, their crowns polished bright by boots, grime packed around them, brass
/// rivets at the corners (one gone, some dull), scuffs, mill-scale patches and an old oil stain.
/// Overhead (the undersides of the library floor), galvanised sheet: zinc spangle, a cross-break
/// crease, a hemmed edge with screws, white rust blooming here and there.
///
/// The walls, corrugated zinc, are metal (<see cref="ArenaTheme.BakeMetal"/>), mirroring the room
/// through its reflection probe. The floor and ceiling stay on the matte base with a low roughness
/// (ruling R17): a room of nothing but metal mirrors itself — metal takes no ambient light — and
/// came out near black even after the probe's extra bounces, and the floor is what a player reads.
/// </summary>
public sealed class WorkshopTheme : ArenaTheme
{
	public override string Name => "Workshop";
	public override int Floor => 5;
	public override Color LightColor => new( 0.95f, 1.00f, 1.10f );

	static Color Steel => Palette.Hex( 0xA19F9B );
	static Color SteelLight => Palette.Hex( 0xC6C4BF );
	static Color SteelShade => Palette.Hex( 0x7A7874 );

	/// <summary>Grime: oil, dust and rust fines packed into every low spot, warm against the cool steel.</summary>
	static Color Grime => Palette.Hex( 0x544C42 );

	/// <summary>Mill scale and heat tint left from rolling: a darker, faintly blue-grey film.</summary>
	static Color MillScale => Palette.Hex( 0x787B80 );

	static Color Brass => Palette.Hex( 0xC9A26B );
	static Color BrassDull => Palette.Hex( 0x8C7550 );
	static Color Oil => Palette.Hex( 0x2F2D2E );

	/// <summary>Galvanised zinc, warm grey: a blue-grey read as cold under the room's lamps (4b review).</summary>
	static Color Zinc => Palette.Hex( 0x9A9993 );
	static Color ZincLight => Palette.Hex( 0xB5B4AD );
	static Color ZincDark => Palette.Hex( 0x7C7B76 );

	/// <summary>Old zinc gone dull and dark with age.</summary>
	static Color ZincDull => Palette.Hex( 0x83817A );

	/// <summary>Rust bleeding from the screws: a dull brown-orange, well away from the danger colours.</summary>
	static Color Rust => Palette.Hex( 0x8A5E42 );

	/// <summary>Machinery paint on the steelwork, a muted khaki near the floor's brass.</summary>
	static Color Khaki => Palette.Hex( 0x8E7E58 );

	/// <summary>White rust: the chalky bloom zinc grows where it stays damp.</summary>
	static Color WhiteRust => Palette.Hex( 0xC9CCC8 );

	/// <summary>Tread bosses per cell side (the lattice), and each boss's half-length and half-width in pixels.</summary>
	const int Lattice = 6;
	const float BossHalfLength = 27f;
	const float BossHalfWidth = 7f;

	/// <summary>
	/// The structural front layer — riveted khaki columns under a girder, hazard-striped skirting — and then
	/// the dressing. Every wall carries a run of two or three pipes high up, clamped to each column, flanged
	/// along its length and turning into the wall at both ends, with a riser or two up from the floor and a
	/// drop or two down to a valve and a gauge; two walls have a low run along the bays with gauge risers and
	/// handwheels, the other two a cable tray over the heads of the room. Each bay gets a set dealt so that no
	/// two neighbours match (nor the bays either side of a corner), each set claiming most of its bay — a
	/// workbench under its tool board by a tall tool cabinet, a big tool board over a tool chest by a lathe, oil
	/// drums by a stock rack and crates, two steel shelving units full of bins, gas cylinders chained to the wall
	/// by a welding screen and a hose reel, the electrical boards by a switchgear cabinet, a pedestal grinder by
	/// a band saw and a bank of lockers, a pillar drill by an air compressor and lockers — then whatever it leaves
	/// bare is filled: on the floor, something a head high first and then drums, crates and pallets; on the wall,
	/// at a player's chest over whatever stays low (parts-bin panels, tool boards, cabinets, posters) and again
	/// over the heads of the room (long shelves of stock, bin panels, a rail of hi-vis vests and hard hats, clamp
	/// rails). Under the pipes every bay gets two pieces, one from each of two decks: meshing gears (one a wall),
	/// an extractor fan, a chain hoist, pinned blueprints, a chalkboard of jobs or a hose on its reel; and a run of
	/// duct, a rack of bar stock, a ladder hung on hooks, the works' signboard, a control panel or a high shelf.
	/// One clock a wall on a column. The room's six lamps burn in enamel cage lamps hung from the girder. Each
	/// wall draws from its own stream.
	/// </summary>
	public override void Dress( FloorContext room )
	{
		var bays = BuildFrontLayer( room, new FrontStyle { Post = 24f, Pier = 64f, Corner = 28f, Cornice = 24f, Skirting = 10f, SkirtingDepth = 4f } );

		// Self-illumination is an exponent (2^b): a little under the paint, the bulbs read lit and cool
		// without blooming their enamel to white.
		Glow.Set( "g_flSelfIllumBrightness", -0.6f );
		var props = new Props( room, Trim, Glow );
		var lamps = LampSpots( 6, Seed + 900 );

		var walls = new Random( Seed + 1700 );
		var low = walls.Next( 4 );
		var otherLow = (low + 1 + walls.Next( 3 )) % 4;

		// The walls go round the room in the order their ends meet (+X, −Y, −X, +Y: each wall's far end is the
		// next one's start), so the deal can keep the two bays either side of a corner apart as well.
		var corners = new List<Props.Look>();
		foreach ( var side in new[] { 0, 3, 2, 1 } )
		{
			props.Begin( side, Seed + 1000 + side * 137 );
			var all = bays.FindAll( b => b.Side == side );
			var wall = all.FindAll( b => b.Width > 60f );
			var lowRun = side == low || side == otherLow;
			props.StartWall( lowRun );
			props.HighPipes( all );
			if ( lowRun ) props.LowPipes( wall );
			else props.CableTray( all );

			var looks = props.Deal( new List<Props.Look>
			{
				Props.Look.Bench, Props.Look.Toolboard, Props.Look.Drums, Props.Look.Rack, Props.Look.Gas, Props.Look.Panel, Props.Look.Grinder, Props.Look.Drill,
			}, wall.Count, ( slot, look ) => !(slot == 0 && corners.Count > 0 && look == corners[^1]) && !(slot == wall.Count - 1 && side == 1 && corners.Count > 0 && look == corners[0]) );
			if ( corners.Count == 0 ) corners.Add( looks[0] );
			corners.Add( looks[^1] );

			// Two pieces over every set, one from each deck. Fans, hoists, hose reels, ducts and shelves come twice
			// in their decks (a workshop has several), the sheets and the hero pieces once a wall; the gas set's own
			// reel keeps a second one out of its bay.
			var uppers = props.Deal( new List<Props.Upper>
			{
				Props.Upper.Fan, Props.Upper.Hoist, Props.Upper.Blueprints, Props.Upper.Chalkboard, Props.Upper.Hose, Props.Upper.Fan, Props.Upper.Hoist, Props.Upper.Hose,
			}, wall.Count, ( slot, upper ) => !(upper == Props.Upper.Hose && looks[slot] == Props.Look.Gas) );
			uppers[(int)(props.Next() * wall.Count)] = Props.Upper.Gears;
			var spares = new List<Props.Upper> { Props.Upper.Fan, Props.Upper.Hoist };
			props.Once( uppers, Props.Upper.Chalkboard, spares );
			props.Once( uppers, Props.Upper.Blueprints, spares );
			var seconds = props.Deal( new List<Props.Second>
			{
				Props.Second.Duct, Props.Second.Bars, Props.Second.Ladder, Props.Second.Sign, Props.Second.Controls, Props.Second.Shelf, Props.Second.Duct, Props.Second.Shelf,
			}, wall.Count );
			var fillers = new List<Props.Second> { Props.Second.Duct, Props.Second.Shelf, Props.Second.Controls };
			foreach ( var once in new[] { Props.Second.Ladder, Props.Second.Sign, Props.Second.Bars } ) props.Once( seconds, once, fillers );

			for ( var k = 0; k < wall.Count; k++ )
			{
				props.Furnish( looks[k], wall[k] );
				props.Above( uppers[k], seconds[k], wall[k] );
			}
			props.Columns( all );
		}

		props.Begin( 0, Seed + 1900 );
		foreach ( var lamp in lamps )
		{
			props.CageLamp( lamp.Side, lamp.U );
			AddLamp( room, lamp.Side, lamp.U, Props.BulbV, Props.HangDepth, LightColor * 4.2f * lamp.Brightness );
		}
	}

	/// <summary>The lit parts of the cage lamps: a cool-white bulb, and the white enamel inside a shade lit by it.</summary>
	protected override PaintedSurface PaintGlow()
	{
		const int Size = 32;
		var painter = new Painter( Size, Palette.Hex( 0xF4F6FA ), 0.3f );
		painter.Fill( 16, 0, 16, 32, Palette.Hex( 0xF2EEE4 ), roughness: 0.4f );
		var glow = new float[Size * Size];
		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
			glow[y * Size + x] = x < 16 ? 1f : 0.42f + 0.2f * y / Size;
		painter.AddGlow( glow );
		return painter.Bake( "arena_5_glow", ArenaGenerator.GlowMaterialPath, 1f );
	}

	static Rect BulbGlow => new( 0.06f, 0.06f, 0.38f, 0.88f );
	static Rect EnamelGlow => new( 0.56f, 0.06f, 0.38f, 0.88f );

	// The props' paints in the trim atlas's infill band (see PaintSwatches).
	static Rect DialSwatch( int k ) => Swatch( k * 64, 0, 64, 64 );
	static Rect PegboardSwatch( int k ) => Swatch( 192 + k * 128, 0, 128, 64 );
	static Rect ClockFaceSwatch => Swatch( 448, 0, 64, 64 );
	static Rect SolidSwatch( int k ) => Swatch( k * 16, 64, 16, 16 );
	static Rect CrateSwatch => Swatch( 0, 80, 64, 32 );
	static Rect TimberSwatch => Swatch( 64, 80, 64, 32 );
	static Rect StripeSwatch => Swatch( 128, 80, 64, 32 );
	static Rect NoticeSwatch( int k ) => Swatch( 192 + k * 32, 80, 32, 32 );
	static Rect BoardFrontSwatch => Swatch( 288, 80, 32, 32 );
	static Rect BlueprintSwatch( int k ) => Swatch( k * 64, 112, 64, 48 );
	static Rect ChalkSwatch => Swatch( 128, 112, 96, 48 );
	static Rect PosterSwatch( int k ) => Swatch( 224 + k * 32, 112, 32, 48 );
	static Rect PlannerSwatch => Swatch( 320, 112, 96, 48 );
	static Rect CalendarSwatch => Swatch( 416, 112, 32, 48 );
	static Rect ShadowBoardSwatch => Swatch( 448, 112, 64, 48 );
	static Rect SignSwatch => Swatch( 320, 80, 32, 32 );
	static Rect BoardSwatch => Swatch( 352, 80, 128, 32 );

	/// <summary>
	/// The plain paints in <see cref="SolidSwatch"/> order: steel grey, dark iron, bright steel, rust, copper,
	/// brass, galvanised zinc, rubber, teal, olive, carrot, cream enamel, green enamel, khaki, handle wood,
	/// cardboard, machine blue-grey, flame, paper, concrete, black paint, wire, oil, leather, cylinder maroon,
	/// cylinder blue, lamp-black enamel, chalk white, hessian, hi-vis, hard-hat yellow, signboard navy.
	/// </summary>
	static readonly uint[] Solids =
	{
		0x8A8D90, 0x3A3A3C, 0xC8CACC, 0x8A5E42, 0xB87850, 0xC9A26B, 0xA4A49E, 0x2C2C2E, 0x5E8288, 0x7A7F55, 0xE9A06A, 0xE8E2D2,
		0x4E7A5A, 0x8E7E58, 0xA87A50, 0xB8956A, 0x6E7C8A, 0xFFC27A, 0xEDE8DC, 0x9A968E, 0x2A2A2E, 0x6A6A68, 0x302C28, 0x6E4E36,
		0x6E4A4C, 0x7E9CB0, 0x34363A, 0xE6E4DC, 0xA48A64, 0xCFD85C, 0xE2B64E, 0x3E5470,
	};

	enum Paint { Steel, Iron, Bright, RustPaint, Copper, BrassPaint, Galvanised, Rubber, Teal, Olive, Carrot, Cream, GreenEnamel, KhakiPaint, Handle, Cardboard, Machine, Flame, Paper, Concrete, Black, Wire, OilPaint, Leather, Maroon, CylinderBlue, LampBlack, Chalk, Hessian, HiVis, Hat, Navy }

	/// <summary>
	/// The props' paints, in the infill band (the workshop hangs no pictures in it): three pressure-gauge dials,
	/// two pegboards with the painted outlines of their tools, a clock face, the plain paints, crate planks, a
	/// worn bench top, hazard stripes, three pinned notices, an electrical board's front, two blueprints and a
	/// chalkboard of jobs.
	/// </summary>
	static void PaintSwatches( Painter painter, Random random )
	{
		float Next() => (float)random.NextDouble();
		var band = TrimRows( TrimBand.Infill ).Top;
		var ink = Palette.Ink;

		// ---- Gauges: a white dial, ticks round three quarters, a carrot sector, the needle ----------
		for ( var k = 0; k < 3; k++ )
		{
			var c = new Vector2( k * 64 + 32f, band + 32f );
			painter.Fill( k * 64, band, 64, 64, Palette.Hex( 0x2C2C2E ), roughness: 0.4f );
			painter.Circle( c.x, c.y, 28f, Palette.Hex( 0xF2EFE6 ), roughness: 0.25f );
			for ( var t = 0; t <= 40; t++ )
			{
				var a = MathF.PI * 0.75f + t / 40f * MathF.PI * 1.5f;
				var dir = new Vector2( MathF.Cos( a ), MathF.Sin( a ) );
				var major = t % 5 == 0;
				painter.Line( c + dir * (major ? 18f : 21f), c + dir * 24f, major ? 1.6f : 0.7f, ink, 0.9f );
				if ( t >= 32 - k * 4 ) painter.Line( c + dir * 24.5f, c + dir * 26.5f, 2.5f, Palette.Hex( 0xE9A06A ), 0.95f );
			}
			painter.Rect( c.x - 7f, c.y + 9f, 14f, 2f, ink, alpha: 0.7f );
			var needle = MathF.PI * 0.75f + (0.2f + Next() * 0.6f) * MathF.PI * 1.5f;
			painter.Line( c, c + new Vector2( MathF.Cos( needle ), MathF.Sin( needle ) ) * 21f, 1.4f, Palette.Hex( 0x2A2A2E ) );
			painter.Circle( c.x, c.y, 2.4f, Palette.Hex( 0x2A2A2E ) );
		}

		// ---- Pegboards: masonite and a grey-painted one, holes on a grid, outlines of the tools -------
		for ( var k = 0; k < 2; k++ )
		{
			var x0 = 192 + k * 128;
			var board = k == 0 ? Palette.Hex( 0xA27C54 ) : Palette.Hex( 0xC9C5BA );
			painter.Fill( x0, band, 128, 64, board, roughness: 0.8f );
			for ( var y = 3f; y < 64f; y += 5.33f )
			for ( var x = 3f; x < 128f; x += 5.33f )
				painter.Circle( x0 + x, band + y, 0.95f, Mix( board, Palette.Hex( 0x2A2420 ), 0.75f ) );
			var outline = k == 0 ? Palette.Hex( 0x3E3028 ) : Palette.Hex( 0x34363A );
			for ( var t = 0; t < 7; t++ )
			{
				var at = new Vector2( x0 + 10f + t * 17f + Next() * 4f, band + 12f + Next() * 10f );
				var length = 22f + Next() * 16f;
				painter.Line( at, at + new Vector2( (Next() - 0.5f) * 4f, length ), 2.6f + Next() * 2f, outline, 0.5f );
				if ( Next() < 0.6f ) painter.Rect( at.x - 4f, at.y - 2f, 8f, 4f, outline, alpha: 0.5f );
			}
		}

		// ---- The clock face: white, heavy ticks, a carrot second ring --------------------------------
		{
			var c = new Vector2( 480f, band + 32f );
			painter.Fill( 448, band, 64, 64, Palette.Hex( 0xF4F2EA ), roughness: 0.3f );
			for ( var t = 0; t < 60; t++ )
			{
				var a = t * MathF.Tau / 60f;
				var dir = new Vector2( MathF.Cos( a ), MathF.Sin( a ) );
				var hour = t % 5 == 0;
				painter.Line( c + dir * (hour ? 17f : 22f), c + dir * 25f, hour ? 2.6f : 0.7f, ink, 0.95f );
			}
			painter.Circle( c.x, c.y, 27f, Palette.Hex( 0xE9A06A ), 0.35f );
			painter.Circle( c.x, c.y, 26f, Palette.Hex( 0xF4F2EA ) );
			for ( var t = 0; t < 60; t++ )
			{
				var a = t * MathF.Tau / 60f;
				var dir = new Vector2( MathF.Cos( a ), MathF.Sin( a ) );
				var hour = t % 5 == 0;
				painter.Line( c + dir * (hour ? 17f : 22f), c + dir * 25f, hour ? 2.6f : 0.7f, ink, 0.95f );
			}
		}

		// ---- Plain paints, each with a little grime ---------------------------------------------------
		for ( var k = 0; k < Solids.Length; k++ )
		{
			var color = Palette.Hex( Solids[k] );
			var roughness = k switch { 2 or 4 or 5 => 0.3f, 7 or 15 or 18 or 19 or 27 or 28 => 0.9f, 11 or 12 or 26 or 30 => 0.35f, _ => 0.55f };
			painter.Fill( k * 16, band + 64, 16, 16, color, roughness: roughness );
			for ( var s = 0; s < 8; s++ )
				painter.Circle( k * 16 + Next() * 16f, band + 64 + Next() * 16f, 0.4f + Next() * 0.5f, Mix( color, Grime, 0.35f ), 0.4f );
		}

		// ---- Crate planks, stencilled ----------------------------------------------------------------
		{
			var y0 = band + 80;
			for ( var p = 0; p < 4; p++ )
			{
				var tone = Mix( Palette.Hex( 0xC2A276 ), Palette.Hex( 0xA6845C ), Next() );
				painter.Fill( 0, y0 + p * 8, 64, 7, tone, roughness: 0.85f );
				for ( var s = 0; s < 4; s++ ) painter.Line( new Vector2( 0f, y0 + p * 8 + 1f + Next() * 5f ), new Vector2( 64f, y0 + p * 8 + 1f + Next() * 5f ), 0.6f, Mix( tone, Grime, 0.4f ), 0.4f );
				painter.Fill( 0, y0 + p * 8 + 7, 64, 1, Palette.Hex( 0x4A3A2C ), roughness: 0.9f );
				foreach ( var x in new[] { 4f, 60f } ) painter.Circle( x, y0 + p * 8 + 3.5f, 0.8f, Palette.Hex( 0x4A4644 ) );
			}
			for ( var k = 0; k < 4; k++ ) painter.Rect( 22f + k * 5f, y0 + 12f, 3.5f, 7f, Palette.Hex( 0x3A3430 ), alpha: 0.7f );
		}

		// ---- A worn bench top: timber, grain, oil rings, a burn -----------------------------------------
		{
			var y0 = band + 80;
			painter.Fill( 64, y0, 64, 32, Palette.Hex( 0x9E7A52 ), roughness: 0.75f );
			for ( var s = 0; s < 14; s++ )
			{
				var y = y0 + 1f + Next() * 30f;
				painter.Line( new Vector2( 64f, y ), new Vector2( 128f, y + (Next() - 0.5f) * 3f ), 0.7f, Palette.Hex( 0x6E5236 ), 0.45f );
			}
			for ( var r = 0; r < 3; r++ )
			{
				var c = new Vector2( 70f + Next() * 52f, y0 + 6f + Next() * 20f );
				painter.Circle( c.x, c.y, 4f + Next() * 3f, Palette.Hex( 0x4A3A2A ), 0.3f, roughness: 0.4f );
				painter.Circle( c.x, c.y, 3f + Next() * 2f, Palette.Hex( 0x9E7A52 ), 0.6f );
			}
		}

		// ---- Hazard stripes in ink and paper (never the warning colour) ----------------------------------
		for ( var y = 0; y < 32; y++ )
		for ( var x = 0; x < 64; x++ )
			painter.Fill( 128 + x, band + 80 + y, 1, 1, ((x + y) / 8) % 2 == 0 ? Mix( ink, Grime, 0.2f ) : Mix( Palette.PaperShade, Khaki, 0.2f ), roughness: 0.6f );

		// ---- Notices: a title bar, lines of type, a diagram ----------------------------------------------
		for ( var k = 0; k < 3; k++ )
		{
			var x0 = 192 + k * 32; var y0 = band + 80;
			painter.Fill( x0, y0, 32, 32, Palette.Hex( 0xEFEADC ), roughness: 0.85f );
			painter.Fill( x0 + 4, y0 + 4, 24, 5, Palette.Hex( new uint[] { 0xE9A06A, 0x5E8288, 0x2F2A3B }[k] ), roughness: 0.8f );
			for ( var l = 0; l < 6; l++ ) painter.Rect( x0 + 5f, y0 + 12f + l * 2.6f, 10f + Next() * 12f, 0.9f, ink, alpha: 0.55f );
			if ( k != 1 ) painter.Circle( x0 + 23f, y0 + 22f, 4f, ink, 0.5f );
		}

		// ---- An electrical board's front: grey, a window over the breakers, a warning triangle in ink ------
		{
			var x0 = 288; var y0 = band + 80;
			painter.Fill( x0, y0, 32, 32, Palette.Hex( 0x9EA2A4 ), roughness: 0.5f );
			painter.Fill( x0 + 6, y0 + 8, 20, 9, Palette.Hex( 0x3A3C40 ), roughness: 0.3f );
			for ( var b = 0; b < 6; b++ ) painter.Fill( x0 + 7 + b * 3, y0 + 10, 2, 5, Palette.Hex( 0xE6E4DC ), roughness: 0.4f );
			painter.Polygon( new[] { new Vector2( x0 + 16f, y0 + 20f ), new Vector2( x0 + 21f, y0 + 28f ), new Vector2( x0 + 11f, y0 + 28f ) }, Palette.Hex( 0xEDE8DC ) );
			painter.Rect( x0 + 15.5f, y0 + 22.5f, 1f, 3.5f, ink );
		}

		// ---- Blueprints: white line drawings of parts on blue ---------------------------------------------
		for ( var k = 0; k < 2; k++ )
		{
			var x0 = k * 64; var y0 = band + 112;
			var blue = k == 0 ? Palette.Hex( 0x3E5E86 ) : Palette.Hex( 0x46688E );
			var line = Palette.Hex( 0xDCE6F0 );
			painter.Fill( x0, y0, 64, 48, blue, roughness: 0.85f );
			painter.Rect( x0 + 3f, y0 + 3f, 58f, 0.7f, line, alpha: 0.7f );
			painter.Rect( x0 + 3f, y0 + 44f, 58f, 0.7f, line, alpha: 0.7f );
			var c = new Vector2( x0 + 22f + Next() * 6f, y0 + 22f );
			var teeth = 10 + random.Next( 6 );
			for ( var t = 0; t < teeth * 2; t++ )
			{
				var a0 = t * MathF.PI / teeth; var a1 = (t + 1) * MathF.PI / teeth;
				var r0 = t % 2 == 0 ? 13f : 11f;
				painter.Line( c + new Vector2( MathF.Cos( a0 ), MathF.Sin( a0 ) ) * r0, c + new Vector2( MathF.Cos( a1 ), MathF.Sin( a1 ) ) * r0, 0.6f, line, 0.85f );
				painter.Line( c + new Vector2( MathF.Cos( a1 ), MathF.Sin( a1 ) ) * 11f, c + new Vector2( MathF.Cos( a1 ), MathF.Sin( a1 ) ) * 13f, 0.6f, line, 0.85f );
			}
			painter.Circle( c.x, c.y, 3f, line, 0.8f );
			painter.Circle( c.x, c.y, 2.3f, blue );
			painter.Rect( x0 + 42f, y0 + 10f, 16f, 22f, line, alpha: 0.25f );
			for ( var l = 0; l < 4; l++ ) painter.Rect( x0 + 44f, y0 + 12f + l * 5f, 10f, 0.6f, line, alpha: 0.8f );
			painter.Line( new Vector2( x0 + 8f, y0 + 40f ), new Vector2( x0 + 38f, y0 + 40f ), 0.5f, line, 0.8f );
		}

		// ---- A chalkboard: the week's jobs, ticked off, a sketch of a part ----------------------------------
		{
			var x0 = 128; var y0 = band + 112;
			painter.Fill( x0, y0, 96, 48, Palette.Hex( 0x343A38 ), roughness: 0.9f );
			for ( var s = 0; s < 6; s++ )
				painter.Polygon( Organic( new Vector2( x0 + 8f + Next() * 80f, y0 + 6f + Next() * 36f ), new Vector2( 6f + Next() * 10f, 2f + Next() * 3f ), 0.7f, random.Next() ), Palette.Hex( 0x4A504C ), alpha: 0.5f );
			var chalk = Palette.Hex( 0xE6E4DC );
			for ( var l = 0; l < 6; l++ )
			{
				var y = y0 + 8f + l * 6f;
				var x = x0 + 8f;
				while ( x < x0 + 30f + Next() * 24f ) { var w = 2f + Next() * 4f; painter.Rect( x, y, w, 1.2f, chalk, alpha: 0.8f ); x += w + 1.4f; }
				if ( Next() < 0.6f ) painter.Line( new Vector2( x0 + 4f, y ), new Vector2( x0 + 6f, y + 1.5f ), 0.8f, chalk, 0.8f );
			}
			painter.Circle( x0 + 76f, y0 + 22f, 10f, chalk, 0.7f );
			painter.Circle( x0 + 76f, y0 + 22f, 9f, Palette.Hex( 0x343A38 ) );
			painter.Circle( x0 + 76f, y0 + 22f, 3f, chalk, 0.7f );
			painter.Circle( x0 + 76f, y0 + 22f, 2.2f, Palette.Hex( 0x343A38 ) );
		}

		// ---- Safety posters: a coloured head band, a pictogram, lines of type ---------------------------
		for ( var k = 0; k < 3; k++ )
		{
			var x0 = 224 + k * 32; var y0 = band + 112;
			var head = Palette.Hex( new uint[] { 0x5E8288, 0xE9A06A, 0x4E7A5A }[k] );
			painter.Fill( x0, y0, 32, 48, Palette.Hex( 0xEFEADC ), roughness: 0.85f );
			painter.Fill( x0 + 3, y0 + 3, 26, 9, head, roughness: 0.8f );
			for ( var l = 0; l < 2; l++ ) painter.Rect( x0 + 6f, y0 + 5f + l * 3f, 14f + Next() * 6f, 1.2f, Palette.Hex( 0xF4F0E6 ) );
			var c = new Vector2( x0 + 16f, y0 + 25f );
			painter.Circle( c.x, c.y, 8.5f, head, roughness: 0.8f );
			painter.Circle( c.x, c.y, 7f, Palette.Hex( 0xF4F0E6 ) );
			switch ( k )
			{
				case 0:
					foreach ( var s in new[] { -1f, 1f } ) painter.Circle( c.x + s * 2.8f, c.y, 2.2f, ink );
					painter.Rect( c.x - 1f, c.y - 0.5f, 2f, 1f, ink );
					break;
				case 1:
					painter.Rect( c.x - 3f, c.y - 1f, 6f, 6f, ink, radius: 1.5f );
					for ( var f = 0; f < 4; f++ ) painter.Rect( c.x - 3f + f * 1.6f, c.y - 5f, 1.2f, 4.4f, ink, radius: 0.5f );
					break;
				default:
					painter.Circle( c.x, c.y - 4f, 1.6f, ink );
					painter.Line( c + new Vector2( 0f, -2.4f ), c + new Vector2( 0.5f, 2f ), 1.4f, ink );
					painter.Line( c + new Vector2( 0.5f, 2f ), c + new Vector2( -1.8f, 5f ), 1.2f, ink );
					painter.Line( c + new Vector2( 0.5f, 2f ), c + new Vector2( 2.6f, 5f ), 1.2f, ink );
					painter.Rect( c.x - 4.5f, c.y - 0.5f, 3f, 2.6f, Palette.Hex( 0xB8956A ) );
					break;
			}
			for ( var l = 0; l < 3; l++ ) painter.Rect( x0 + 5f, y0 + 37f + l * 2.6f, 14f + Next() * 8f, 0.9f, ink, alpha: 0.6f );
		}

		// ---- A planning board: columns of job cards under their headers --------------------------------
		{
			var x0 = 320; var y0 = band + 112;
			painter.Fill( x0, y0, 96, 48, Palette.Hex( 0xE8E6E0 ), roughness: 0.4f );
			var cards = new uint[] { 0xE9A06A, 0x8FB3B8, 0xF0D9A0, 0xA9C29A, 0xEDE8DC };
			for ( var col = 0; col < 5; col++ )
			{
				var cx = x0 + 4f + col * 18f;
				painter.Rect( cx, y0 + 3f, 16f, 4f, ink, alpha: 0.85f );
				for ( var row = 0; row < 3 + random.Next( 3 ); row++ )
					painter.Rect( cx + Next() * 1.5f, y0 + 10f + row * 7.2f, 14f, 5.6f, Palette.Hex( cards[random.Next( cards.Length )] ), roughness: 0.8f );
			}
			for ( var col = 1; col < 5; col++ ) painter.Rect( x0 + 3f + col * 18f, y0 + 3f, 0.6f, 42f, Palette.Hex( 0x9A968E ) );
		}

		// ---- A shop calendar: a picture of a lathe over the month -------------------------------------
		{
			var x0 = 416; var y0 = band + 112;
			painter.Fill( x0, y0, 32, 48, Palette.Hex( 0xF2EEE4 ), roughness: 0.8f );
			painter.Fill( x0 + 3, y0 + 3, 26, 18, Palette.Hex( 0x8FA6B0 ), roughness: 0.8f );
			painter.Rect( x0 + 6f, y0 + 12f, 20f, 5f, Palette.Hex( 0x4E5A62 ) );
			painter.Rect( x0 + 9f, y0 + 8f, 6f, 4f, Palette.Hex( 0x4E5A62 ) );
			painter.Fill( x0 + 3, y0 + 23, 26, 3, Palette.Hex( 0xE9A06A ), roughness: 0.8f );
			for ( var k = 0; k <= 5; k++ ) painter.Rect( x0 + 3f, y0 + 27f + k * 3.6f, 26f, 0.5f, Palette.Hex( 0xB9B7B0 ) );
			for ( var k = 0; k <= 7; k++ ) painter.Rect( x0 + 3f + k * 3.7f, y0 + 27f, 0.5f, 18f, Palette.Hex( 0xB9B7B0 ) );
		}

		// ---- A shadow board: tool outlines painted white on dark green ------------------------------------
		{
			var x0 = 448; var y0 = band + 112;
			painter.Fill( x0, y0, 64, 48, Palette.Hex( 0x3E5A48 ), roughness: 0.7f );
			var paint = Palette.Hex( 0xE6E4DC );
			for ( var t = 0; t < 6; t++ )
			{
				var x = x0 + 6f + t * 9.5f + Next() * 2f;
				var length = 18f + Next() * 16f;
				painter.Line( new Vector2( x, y0 + 8f ), new Vector2( x, y0 + 8f + length ), 2.4f, paint, 0.85f );
				painter.Line( new Vector2( x, y0 + 8f ), new Vector2( x, y0 + 8f + length ), 1.2f, Palette.Hex( 0x3E5A48 ) );
				if ( t % 2 == 0 ) { painter.Circle( x, y0 + 7f, 2.6f, paint, 0.85f ); painter.Circle( x, y0 + 7f, 1.6f, Palette.Hex( 0x3E5A48 ) ); }
			}
		}

		// ---- The works' signboard: a cog on navy, block lettering in two words, a carrot rule under it -------
		{
			var x0 = 352; var y0 = band + 80;
			var navy = Palette.Hex( 0x3E5470 );
			var cream = Palette.Hex( 0xEFEADC );
			painter.Fill( x0, y0, 128, 32, cream, roughness: 0.6f );
			painter.Fill( x0, y0, 30, 32, navy, roughness: 0.55f );
			var c = new Vector2( x0 + 15f, y0 + 16f );
			for ( var t = 0; t < 8; t++ )
			{
				var a = t * MathF.Tau / 8f;
				painter.Line( c, c + new Vector2( MathF.Cos( a ), MathF.Sin( a ) ) * 11.5f, 3.6f, cream );
			}
			painter.Circle( c.x, c.y, 9f, cream );
			painter.Circle( c.x, c.y, 3.6f, navy );
			var x = x0 + 36f;
			for ( var word = 0; word < 2; word++ )
			{
				for ( var g = 3 + random.Next( 3 ); g > 0 && x < x0 + 116f; g-- )
				{
					var w = 5f + Next() * 2.5f;
					painter.Rect( x, y0 + 6f, w, 14f, navy );
					if ( Next() < 0.7f ) painter.Rect( x + w * 0.35f, y0 + (Next() < 0.5f ? 6f : 13f), w * 0.3f, 7f, cream );
					x += w + 1.8f;
				}
				x += 5f;
			}
			painter.Fill( x0 + 34, y0 + 24, 90, 2, Palette.Hex( 0xE9A06A ), roughness: 0.6f );
		}

		// ---- A warning sign: an ink triangle on paper -------------------------------------------------------
		{
			var x0 = 320; var y0 = band + 80;
			painter.Fill( x0, y0, 32, 32, Palette.Hex( 0xEDE8DC ), roughness: 0.6f );
			painter.Polygon( new[] { new Vector2( x0 + 16f, y0 + 5f ), new Vector2( x0 + 28f, y0 + 26f ), new Vector2( x0 + 4f, y0 + 26f ) }, ink );
			painter.Polygon( new[] { new Vector2( x0 + 16f, y0 + 9.5f ), new Vector2( x0 + 24.2f, y0 + 23.6f ), new Vector2( x0 + 7.8f, y0 + 23.6f ) }, Palette.Hex( 0xEDE8DC ) );
			painter.Rect( x0 + 15f, y0 + 13f, 2f, 6f, ink );
			painter.Rect( x0 + 15f, y0 + 20.4f, 2f, 1.8f, ink );
		}
	}

	/// <summary>
	/// Corrugated galvanised sheet, as clad on a shed: sheets of uneven width lapped over each other at
	/// a crest, in two lengths with an end lap at a different height on every sheet, each sheet its own
	/// age — fresher or duller zinc, more or less white rust. Screws with washers on every other crest
	/// along three girts, some haloed with rust and a few bleeding a thin streak down the sheet. Grime
	/// runs down the troughs and gathers in a splash band along the floor. The zinc is warm grey, and
	/// the spangle is a faint patchwork of crystal grains.
	///
	/// The corrugation lives in the relief and the reflection alone, never in the colour, and its
	/// period is a power-of-two number of pixels: at 25.6 px the crests fell between texels in every
	/// mip below the first few, which baked beating bands into the mips themselves, and crest shading
	/// in the colour plus a strong normal and a tight highlight beat against the screen as moiré
	/// (6b review). At 32 px each mip halves the period exactly until the crests average out flat.
	///
	/// Six sheets across a repeat, in two lengths: the end lap and the middle girt sit about half way
	/// up, the top girt under the cornice, so a taller room is longer sheets, not wider corrugation.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		const int Crests = WallSize / 32;
		const int Sheets = 6;
		const float Pitch = WallSize / (float)Crests;
		const int GrainSize = 16;
		// How many times a crest is traded between two random sheets to unsettle the even split.
		const int Swaps = 6;
		float Row( float v ) => (WallRepeat - v) * Ppu;
		var painter = new Painter( WallSize, Zinc, 0.58f );
		var random = new Random( Seed + 600 );
		float Next() => (float)random.NextDouble();

		// Sheet widths in crests, 5 to 8, adding up to the repeat; each sheet's end lap at its own height.
		var widths = new int[Sheets];
		for ( var k = 0; k < Sheets; k++ ) widths[k] = Crests / Sheets + (k < Crests % Sheets ? 1 : 0);
		for ( var k = 0; k < Swaps; k++ )
		{
			var a = random.Next( Sheets ); var b = random.Next( Sheets );
			if ( a != b && widths[a] < 8 && widths[b] > 5 ) { widths[a]++; widths[b]--; }
		}
		var starts = new int[Sheets + 1];
		starts[0] = random.Next( Crests );
		for ( var k = 0; k < Sheets; k++ ) starts[k + 1] = starts[k] + widths[k];
		var laps = new float[Sheets];
		for ( var k = 0; k < Sheets; k++ ) laps[k] = Row( WallRepeat * 0.47f + Next() * 28f );

		// Twelve sheets (six columns, two lengths), each its own zinc and its own weathering.
		var palette = new Color[Sheets * 2 + 1];
		var weather = new float[Sheets * 2 + 1];
		for ( var k = 1; k <= Sheets * 2; k++ )
		{
			var tone = Next() * 2f - 1f;
			var zinc = tone < 0f ? Mix( Zinc, ZincDark, -tone * 0.45f ) : Mix( Zinc, ZincLight, tone * 0.45f );
			weather[k] = Next();
			palette[k] = Mix( zinc, ZincDull, weather[k] * 0.55f );
		}

		var warp = painter.Noise( 16, Seed + 601, 2 );
		var rustNoise = painter.Noise( 7, Seed + 602, 3 );
		var runs = painter.Noise( 48, Seed + 603, 2 );
		var owner = new byte[WallSize * WallSize];
		var relief = new float[WallSize * WallSize];
		var corrugation = new float[WallSize * WallSize];
		var spangle = new float[WallSize * WallSize];
		var bloom = new float[WallSize * WallSize];
		var grime = new float[WallSize * WallSize];
		var edge = new float[WallSize * WallSize];
		var wave = new float[WallSize];
		for ( var x = 0; x < WallSize; x++ ) wave[x] = 0.5f + 0.5f * MathF.Cos( x * MathF.Tau / Pitch );

		for ( var y = 0; y < WallSize; y++ )
		{
			var v = WallRepeat - y / Ppu;
			var splash = v < 30f ? (30f - v) / 30f : 0f;
			for ( var x = 0; x < WallSize; x++ )
			{
				var i = y * WallSize + x;

				// Which sheet: the column by crest (wrapping), the length by that column's lap.
				var crest = (x / Pitch - starts[0] + Crests * 2) % Crests;
				var column = 0;
				while ( column < Sheets - 1 && crest >= starts[column + 1] - starts[0] ) column++;
				var upper = y < laps[column];
				var sheet = 1 + column * 2 + (upper ? 1 : 0);
				owner[i] = (byte)sheet;

				// Corrugation, a hair proud where a sheet laps over its neighbour and over the length below.
				var intoColumn = crest - (starts[column] - starts[0]);
				var side = intoColumn < 0.5f ? 0.07f : 0f;
				var end = upper && laps[column] - y < 5f ? 0.08f : 0f;
				corrugation[i] = wave[x] * 0.8f;
				relief[i] = side + end;
				if ( !upper && y - laps[column] < 7f ) edge[i] = (1f - (y - laps[column]) / 7f) * 0.55f;
				if ( intoColumn > 0.48f && intoColumn < 0.56f ) edge[i] = MathF.Max( edge[i], 0.4f );

				// Spangle: crystal grains as a warped patchwork, each a little brighter or duller; grains
				// a whole number of times across the texture, so the patchwork runs on over its wrap.
				const int Grains = WallSize / GrainSize;
				var gx = ((int)MathF.Floor( (x + (warp[i] - 0.5f) * 30f) / GrainSize ) + Grains) % Grains;
				var gy = ((int)MathF.Floor( (y + (warp[(i + 512 * WallSize + 512) % (WallSize * WallSize)] - 0.5f) * 30f) / GrainSize ) + Grains) % Grains;
				spangle[i] = (Bits( Hash( gx, gy, Seed ), 0 ) - 0.5f) * (1f - weather[sheet]) * 0.5f;

				var r = rustNoise[i];
				bloom[i] = r > 0.62f ? MathF.Min( (r - 0.62f) * 4f, 1f ) * (0.15f + weather[sheet] * 0.45f) : 0f;

				// Grime down the troughs, read as long thin streaks; and the splash band at the foot.
				var s = StreakX( runs, WallSize, y, x, 37 );
				grime[i] = (s > 0.55f ? MathF.Min( (s - 0.55f) * 3f, 1f ) * (1f - wave[x]) * 0.35f : 0f) + splash * splash * 0.5f;
			}
		}

		painter.AddHeight( relief, 1f );
		painter.AddHeight( corrugation, 1f );
		painter.Blend( owner, palette );
		var lighter = new float[WallSize * WallSize];
		for ( var i = 0; i < spangle.Length; i++ ) { lighter[i] = spangle[i] > 0f ? spangle[i] : 0f; spangle[i] = spangle[i] < 0f ? -spangle[i] : 0f; }
		painter.Blend( lighter, ZincLight );
		painter.Blend( spangle, ZincDark );
		painter.Blend( bloom, WhiteRust, roughness: 0.85f );
		painter.Blend( grime, Grime, roughness: 0.7f );
		painter.Blend( edge, Mix( ZincDark, Grime, 0.5f ) );

		// Screws with washers on every other crest along three girts; some rusty, a few bleeding.
		for ( var c = 0; c < Sheets; c++ )
		{
			var girts = new[] { Row( 24f ), laps[c] + 10f, Row( WallRepeat - 38f ) };
			for ( var k = starts[c] + 1; k < starts[c + 1]; k += 2 )
			foreach ( var gy in girts )
			{
				var x = (k % Crests) * Pitch + (Next() - 0.5f) * 1.5f;
				var y = gy + (Next() - 0.5f) * 3f;
				var rusty = Next();
				if ( rusty < 0.35f ) painter.Circle( x, y, 7f, Rust, 0.18f + rusty * 0.4f );
				if ( rusty < 0.12f )
				{
					var length = 40f + Next() * 110f;
					var width = 2.5f + Next() * 2.5f;
					for ( var s = 0; s < 6; s++ )
						painter.Line( new Vector2( x, y + length * s / 6f ), new Vector2( x + (Next() - 0.5f) * 2f, y + length * (s + 1) / 6f ), width * (1f - s * 0.12f), Rust, 0.22f * (1f - s / 7f) );
				}
				painter.Circle( x, y, 4.6f, ZincLight, height: 1.05f, bevel: 2f, roughness: 0.4f );
				painter.Circle( x, y, 2.3f, Mix( ZincDark, rusty < 0.35f ? Rust : ZincDark, 0.5f ), height: 1.25f, bevel: 1.6f, roughness: 0.5f );
			}
		}

		painter.HeightNoise( 0.15f, 3, Seed + 604, 2 );
		painter.Mottle( 0.04f, 8, Seed + 605, 3 );
		painter.RoughnessNoise( 0.08f, 6, Seed + 606, 3 );

		// The cavity bake sees the laps, girts and screws but not the corrugation, which would put the
		// crests back into the colour.
		painter.AddHeight( corrugation, -1f );
		painter.Cavity( 0.3f, 5 );
		painter.AddHeight( corrugation, 1f );
		painter.Grain( 0.01f, Seed + 607 );
		return BakeMetal( painter, "arena_5_wall", 2.5f, 0.65f );
	}

	/// <summary>
	/// The steelwork in machinery khaki, chipped to bare steel at the arrises: the cornice is a girder
	/// (flanges proud of the web, rivets along both), the posts riveted columns with flange plates down
	/// their edges, the skirting a band of worn hazard stripes in ink and paper (never the warning
	/// colour), the slab edge a bolted steel angle.
	/// </summary>
	protected override PaintedSurface PaintTrim()
	{
		return PaintTrimAtlas( "arena_5_trim", Khaki, Grime, 0.55f, 0f, Seed + 700, painter =>
		{
			PaintBand( painter, TrimBand.Cornice, Khaki, GirderProfile, 0.55f );
			ShadeBand( painter, TrimBand.Cornice, GirderProfile, Grime, 0.6f );
			PaintBand( painter, TrimBand.Pilaster, Khaki, ColumnProfile, 0.55f );
			ShadeBand( painter, TrimBand.Pilaster, ColumnProfile, Grime, 0.5f );

			// Rivets along both flanges of the girder and both plates of the columns, at an even rivet
			// pitch that divides the band, so they run on seamlessly along the run.
			const float RivetPitch = TrimSize / 24f;
			foreach ( var (band, rows) in new[] { (TrimBand.Cornice, new[] { 0.08f, 0.92f }), (TrimBand.Pilaster, new[] { 0.07f, 0.93f }) } )
			{
				var (top, height) = TrimRows( band );
				for ( var k = 0; k < 24; k++ )
				foreach ( var t in rows )
					painter.Circle( k * RivetPitch + RivetPitch * 0.5f, top + height * t, 3f, Khaki, height: 1.3f, bevel: 2.4f, roughness: 0.45f );
			}

			// Hazard stripes on the skirting, worn through to steel where boots and trolleys hit them.
			{
				var (top, height) = TrimRows( TrimBand.Skirting );
				for ( var y = top; y < top + height; y++ )
				for ( var x = 0; x < TrimSize; x++ )
				{
					// A 64 px period, which divides the band's 512 px run, so the stripes carry on across the wrap.
					var stripe = ((x + y) / 32) % 2 == 0;
					painter.Fill( x, y, 1, 1, stripe ? Mix( Palette.Ink, Grime, 0.2f ) : Mix( Palette.PaperShade, Khaki, 0.25f ) );
				}
				var scuffs = Ramp( painter.Noise( 32, Seed + 710, 3 ), 0.6f, 0.8f, 0.6f );
				for ( var i = 0; i < scuffs.Length; i++ )
					if ( i / TrimSize < top || i / TrimSize >= top + height ) scuffs[i] = 0f;
				painter.Blend( scuffs, SteelShade, roughness: 0.4f );
			}

			// The slab edge: a steel angle, bolted.
			{
				var (top, height) = TrimRows( TrimBand.SlabEdge );
				TintBand( painter, TrimBand.SlabEdge, SteelShade, 0.4f, NosingProfile, Grime, 0.5f );
				for ( var x = 16; x < TrimSize; x += 64 )
				{
					painter.Circle( x, top + height * 0.6f, 5f, Steel, height: 1.25f, bevel: 3f, roughness: 0.35f );
					painter.Circle( x, top + height * 0.6f, 2.2f, SteelShade, height: 1.35f, bevel: 1f, roughness: 0.4f );
				}
			}

			// Paint chipped to steel in small flakes over all the khaki.
			var chips = Ramp( painter.Noise( 48, Seed + 711, 3 ), 0.7f, 0.8f, 0.7f );
			var (skirt, skirtHeight) = TrimRows( TrimBand.Skirting );
			var infill = TrimRows( TrimBand.Infill ).Top;
			for ( var i = 0; i < chips.Length; i++ )
			{
				var row = i / TrimSize;
				if ( row >= infill || row >= skirt && row < skirt + skirtHeight ) chips[i] = 0f;
			}
			painter.Blend( chips, SteelShade, roughness: 0.35f );

			PaintSwatches( painter, new Random( Seed + 702 ) );
		} );
	}

	/// <summary>A girder seen from the side: the flanges at top and bottom proud of the web between them.</summary>
	static float GirderProfile( float t )
	{
		var flange = MathF.Min( t, 1f - t );
		return flange < 0.16f ? 1f : flange < 0.2f ? 1f - (flange - 0.16f) / 0.04f * 0.4f : 0.6f;
	}

	/// <summary>A built-up column's face: riveted flange plates down both edges, proud of the plate between.</summary>
	static float ColumnProfile( float t )
	{
		var edge = MathF.Min( t, 1f - t );
		return edge < 0.02f ? 0.7f + edge * 15f : edge < 0.14f ? 1f : edge < 0.16f ? 1f - (edge - 0.14f) / 0.02f * 0.15f : 0.85f;
	}

	protected override PaintedSurface PaintFloor()
	{
		const int cell = FloorSize / 2;
		const float pitch = cell / (float)Lattice;
		var painter = new Painter( FloorSize, Grime, 0.6f );
		var random = new Random( Seed );
		float Next() => (float)random.NextDouble();

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var tone = Next() * 2f - 1f;
			var plate = tone < 0f ? Mix( Steel, SteelShade, -tone * 0.35f ) : Mix( Steel, SteelLight, tone * 0.2f );

			// Sheared plate: straighter than stone, but a cut edge is never quite true, and the plate
			// bends down over the arris to the joint at the chamfer's foot.
			painter.Fill( (int)min.x, (int)min.y, cell, cell, Mix( SteelShade, Grime, 0.55f ), height: 0.5f, roughness: 0.7f );
			painter.Polygon( SlabOutline( min, cell, 6f + Next() * 3f, 1.2f, 3f + Next() * 4f, Seed + c * 101 ), plate, height: 1f, bevel: Rim, roughness: 0.42f );
		}

		PaintTread( painter );

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );

			// Rivets where the corner bosses would be: domed brass, some dulled with grime, one hole
			// on the plate where a rivet has gone.
			for ( var k = 0; k < 4; k++ )
			{
				var at = min + new Vector2( k % 2 == 0 ? pitch * 0.5f : cell - pitch * 0.5f, k < 2 ? pitch * 0.5f : cell - pitch * 0.5f )
					+ new Vector2( Next() - 0.5f, Next() - 0.5f ) * 3f;
				var radius = 6.5f + Next() * 1.5f;
				painter.Circle( at.x, at.y, radius + 3f, Grime, 0.35f, roughness: 0.7f );
				if ( c == 1 && k == 2 )
				{
					painter.Circle( at.x, at.y, radius * 0.75f, Grime, 0.95f, height: 0.55f, bevel: 1.5f, roughness: 0.9f );
					continue;
				}
				var dull = Next();
				var metal = Mix( Mix( Brass, BrassDull, dull * 0.8f ), Grime, dull * 0.25f );
				painter.Circle( at.x, at.y, radius, metal, height: 1.55f, bevel: radius * 0.9f, roughness: 0.35f + dull * 0.3f );
			}

			// Scuffs: short bright scratches from dragged crates and boots, every which way.
			var scuffs = 35 + random.Next( 25 );
			for ( var s = 0; s < scuffs; s++ )
			{
				var at = min + new Vector2( 34f + Next() * 444f, 34f + Next() * 444f );
				var angle = Next() * MathF.Tau;
				var length = 5f + Next() * Next() * 34f;
				var to = at + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * length;
				painter.Line( at, to, 0.7f + Next() * 0.5f, SteelLight, 0.12f + Next() * 0.2f, roughness: 0.3f );
			}

			// An old oil stain on variant 2: soaked into the grime, glossy, soft-edged, small.
			if ( c == 2 )
			{
				var centre = min + new Vector2( 160f + Next() * 190f, 160f + Next() * 190f );
				var half = new Vector2( 42f + Next() * 14f, 32f + Next() * 12f );
				var angle = Next() * MathF.Tau;
				for ( var layer = 0; layer < 3; layer++ )
					painter.Polygon( Organic( centre, half * (1f - layer * 0.3f), 0.9f, Seed + 70 + layer, angle ), Oil, alpha: 0.07f, roughness: 0.22f );
			}
		}

		// Mill scale and heat tint in broad patches, laid out differently on each of the four plates.
		painter.Blend( Ramp( painter.Noise( 3, Seed + 30, 2 ), 0.5f, 0.8f, 0.4f ), MillScale );

		// Grime worked into the plate in drifts, thicker away from the walking lines.
		painter.Blend( Ramp( painter.Noise( 6, Seed + 36, 2 ), 0.6f, 0.9f, 0.2f ), Grime, roughness: 0.7f );
		painter.HeightNoise( 0.35f, 4, Seed + 31, 2 );
		painter.Mottle( 0.05f, 4, Seed + 32, 3 );
		painter.RoughnessNoise( 0.08f, 6, Seed + 33, 3 );
		painter.Cavity( 0.55f, 3 );
		painter.EdgeWear( 0.75f, 3, Seed + 34 );
		painter.Grain( 0.012f, Seed + 35 );
		return painter.Bake( "arena_5_floor", Painter.Matte, 6f );
	}

	/// <summary>
	/// Diamond tread, computed per pixel: a lens (two intersecting circles) at every lattice point,
	/// turned +45° and −45° in a checkerboard, as rolled plate is. The rolls are exact, the wear is not:
	/// each boss sits a hair off its point, a few percent longer or shorter, and its crown is polished
	/// by its own amount. The four corner points are left flat for the rivets.
	/// </summary>
	void PaintTread( Painter painter )
	{
		const int cell = FloorSize / 2;
		const float pitch = cell / (float)Lattice;
		var relief = new float[FloorSize * FloorSize];
		var crown = new float[FloorSize * FloorSize];
		var reach = (int)MathF.Ceiling( BossHalfLength * 0.72f + 2f );

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			for ( var j = 0; j < Lattice; j++ )
			for ( var i = 0; i < Lattice; i++ )
			{
				if ( (i == 0 || i == Lattice - 1) && (j == 0 || j == Lattice - 1) ) continue;

				var h = Hash( i, j, Seed + c * 31 );
				var cx = min.x + (i + 0.5f) * pitch + (Bits( h, 0 ) - 0.5f) * 2.4f;
				var cy = min.y + (j + 0.5f) * pitch + (Bits( h, 8 ) - 0.5f) * 2.4f;
				var halfLength = BossHalfLength * (0.95f + Bits( h, 16 ) * 0.1f);
				var polish = 0.15f + Bits( h, 24 ) * 0.35f;

				// Lens of half-length b and half-width w: circles of radius r at ±d across it.
				var w = BossHalfWidth;
				var r = (halfLength * halfLength / w + w) * 0.5f;
				var d = r - w;
				var flip = (i + j) % 2 == 0 ? 1f : -1f;

				for ( var y = (int)cy - reach; y <= (int)cy + reach; y++ )
				{
					var dy = y + 0.5f - cy;
					var row = y * FloorSize;
					for ( var x = (int)cx - reach; x <= (int)cx + reach; x++ )
					{
						var dx = x + 0.5f - cx;
						var along = MathF.Abs( (dx + dy * flip) * 0.70710678f );
						var across = MathF.Abs( (dx - dy * flip) * 0.70710678f );
						float sd;
						if ( (along - halfLength) * d > across * halfLength )
						{
							var ex = across; var ey = along - halfLength;
							sd = MathF.Sqrt( ex * ex + ey * ey );
						}
						else
						{
							var ex = across + d;
							sd = MathF.Sqrt( ex * ex + along * along ) - r;
						}
						if ( sd >= 1f ) continue;

						// Rounded shoulders a few pixels wide, then a low crown along the ridge.
						var t = MathF.Min( MathF.Max( (1f - sd) / 5f, 0f ), 1f );
						var shoulder = t * t * (3f - 2f * t);
						var ridge = 1f - along / halfLength;
						var i0 = row + x;
						relief[i0] = shoulder * (0.5f + ridge * 0.12f);
						crown[i0] = shoulder * ridge * polish;
					}
				}
			}
		}

		painter.AddHeight( relief, 1f );
		painter.Blend( crown, SteelLight, roughness: 0.32f );
	}

	/// <summary>
	/// Galvanised sheet, one per tile underside. Spangle — the zinc's crystal grains — is a jittered
	/// Voronoi pattern, each grain its own brightness and finish with a slight sheen gradient across
	/// it, as real spangle catches light half by half. A cross-break crease runs corner to corner
	/// (sheet-metal panels are stiffened that way), the edge is hemmed and screwed, and white rust
	/// blooms in soft patches.
	/// </summary>
	protected override PaintedSurface PaintCeiling()
	{
		const int cell = CeilingSize / 2;
		const int Hem = 7;
		var painter = new Painter( CeilingSize, ZincDark, 0.45f );
		var random = new Random( Seed + 500 );
		float Next() => (float)random.NextDouble();

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );
			var tone = Next() * 2f - 1f;
			var zinc = tone < 0f ? Mix( Zinc, ZincDark, -tone * 0.4f ) : Mix( Zinc, ZincLight, tone * 0.4f );
			painter.Fill( (int)min.x, (int)min.y, cell, cell, Mix( zinc, ZincLight, 0.15f ), height: 1.15f );
			painter.Polygon( new[]
			{
				min + new Vector2( Hem, Hem ), min + new Vector2( cell - Hem, Hem ),
				min + new Vector2( cell - Hem, cell - Hem ), min + new Vector2( Hem, cell - Hem ),
			}, zinc, height: 0.9f, bevel: 3f, roughness: 0.45f );
		}

		PaintSpangle( painter );

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );

			// Screws along the hem, spaced by hand.
			for ( var side = 0; side < 4; side++ )
			{
				for ( var along = 18f + Next() * 10f; along < cell - 14f; along += 52f + Next() * 16f )
				{
					var at = side switch
					{
						0 => min + new Vector2( along, Hem * 0.5f ), 1 => min + new Vector2( cell - Hem * 0.5f, along ),
						2 => min + new Vector2( cell - along, cell - Hem * 0.5f ), _ => min + new Vector2( Hem * 0.5f, cell - along ),
					};
					painter.Circle( at.x, at.y, 2.6f, ZincDark, height: 1.45f, bevel: 2f, roughness: 0.4f );
					var slot = Next() * MathF.PI;
					var dir = new Vector2( MathF.Cos( slot ), MathF.Sin( slot ) ) * 1.8f;
					painter.Line( at - dir, at + dir, 0.7f, Grime, 0.6f );
				}
			}
		}

		painter.Blend( Ramp( painter.Noise( 5, Seed + 520, 3 ), 0.62f, 0.85f, 0.35f ), WhiteRust, roughness: 0.85f );
		painter.Mottle( 0.04f, 3, Seed + 521, 4 );
		painter.Cavity( 0.4f, 3 );
		painter.Grain( 0.01f, Seed + 522 );
		return painter.Bake( "arena_5_ceiling", Painter.Matte, 3f );
	}

	/// <summary>Spangle grains and the cross-break crease, in one pass over the ceiling.</summary>
	void PaintSpangle( Painter painter )
	{
		const int cell = CeilingSize / 2;
		const int Spangle = 13;
		const int grid = CeilingSize / Spangle + 2;
		var bright = new float[CeilingSize * CeilingSize];
		var dark = new float[CeilingSize * CeilingSize];
		var crease = new float[CeilingSize * CeilingSize];

		// One jittered seed point per grid square, with its grain's tone and sheen direction.
		var pointX = new float[grid * grid];
		var pointY = new float[grid * grid];
		var sheenX = new float[grid * grid];
		var sheenY = new float[grid * grid];
		var tones = new float[grid * grid];
		for ( var gy = 0; gy < grid; gy++ )
		for ( var gx = 0; gx < grid; gx++ )
		{
			var k = gy * grid + gx;
			var h = Hash( gx, gy, Seed + 540 );
			pointX[k] = (gx + 0.1f + Bits( h, 0 ) * 0.8f) * Spangle;
			pointY[k] = (gy + 0.1f + Bits( h, 8 ) * 0.8f) * Spangle;
			var angle = Bits( h, 16 ) * MathF.Tau;
			sheenX[k] = MathF.Cos( angle ) * 0.35f / Spangle;
			sheenY[k] = MathF.Sin( angle ) * 0.35f / Spangle;
			tones[k] = (Bits( h, 24 ) - 0.5f) * 0.7f;
		}

		for ( var y = 0; y < CeilingSize; y++ )
		{
			var gy = y / Spangle;
			for ( var x = 0; x < CeilingSize; x++ )
			{
				var gx = x / Spangle;
				var px = x + 0.5f; var py = y + 0.5f;
				var best = float.MaxValue; var owner = 0;
				for ( var oy = Math.Max( gy - 1, 0 ); oy <= Math.Min( gy + 1, grid - 1 ); oy++ )
				for ( var ox = Math.Max( gx - 1, 0 ); ox <= Math.Min( gx + 1, grid - 1 ); ox++ )
				{
					var k = oy * grid + ox;
					var ddx = px - pointX[k]; var ddy = py - pointY[k];
					var dd = ddx * ddx + ddy * ddy;
					if ( dd < best ) { best = dd; owner = k; }
				}

				// Each grain: its own tone, and a sheen gradient across it in its own direction.
				var tone = tones[owner] + (px - pointX[owner]) * sheenX[owner] + (py - pointY[owner]) * sheenY[owner];
				var i = y * CeilingSize + x;
				if ( tone > 0f ) bright[i] = MathF.Min( tone, 1f ) * 0.55f; else dark[i] = MathF.Min( -tone, 1f ) * 0.45f;

				// The cross-break: a soft ridge along both diagonals of the sheet.
				var lx = x % cell + 0.5f; var ly = y % cell + 0.5f;
				var toDiagonal = MathF.Min( MathF.Abs( lx - ly ), MathF.Abs( lx + ly - cell ) ) * 0.70710678f;
				var t = MathF.Max( 1f - toDiagonal / 14f, 0f );
				crease[i] = t * t * 0.18f;
			}
		}

		painter.Blend( bright, ZincLight );
		painter.Blend( dark, ZincDark );
		painter.AddHeight( crease, 1f );
	}

	/// <summary>
	/// Builds the workshop's props into the floor's kit, in wall space (see <see cref="WallProps"/>). What a
	/// worker would use — benches, vices, drums, cylinders, shelving, tools — is at a player's size; decor
	/// up high (gears, the fan) may be half as big again. Each prop group draws from a stream of its own
	/// (<see cref="WallProps.Apart"/>).
	/// </summary>
	sealed class Props : WallProps
	{
		/// <summary>Where a cage lamp's bulb burns, where its lamp is, and how far out from the girder it hangs.</summary>
		public const float BulbV = 227f;
		public const float HangDepth = -12f;

		/// <summary>The face of the skirting.</summary>
		const float Skirt = FloorContext.Recess - 4.5f;

		/// <summary>The underside of the girder.</summary>
		const float Girder = FloorContext.ClearHeight - 24f;

		/// <summary>Where the high pipe runs end, short of the corners, turning into the wall.</summary>
		const float End = 34f;

		/// <summary>The band of wall a player's chest and head pass (what stands a head high claims it), and the band over the heads of the room up to the cable tray or the clear height.</summary>
		const float ChestFoot = 46f;
		const float HeadFoot = 94f;

		/// <summary>The band under the pipes where the two pieces over each set hang.</summary>
		const float UpperFoot = 144f;
		const float UpperTop = 180f;

		static Vector3 ToRoom => new( 0f, -1f, 0f );
		static Vector3 Across => new( 0f, 1f, 0f );

		readonly Material _trim;
		readonly Material _glow;
		readonly List<(float From, float To)> _floor = new();
		readonly HashSet<string> _heroes = new();

		/// <summary>How deep furniture stands on the wall being dressed: against the skirting, or clear of a low pipe run.</summary>
		float _back;

		/// <summary>The top of the band over the heads of the room: under the cable tray where the wall has one.</summary>
		float _headTop;

		/// <summary>Whether the bay being furnished has something at a player's chest or head yet.</summary>
		bool _tall;

		public Props( FloorContext room, Material trim, Material glow ) : base( room )
		{
			_trim = trim;
			_glow = glow;
		}

		/// <summary>The sets the bays are dealt from.</summary>
		public enum Look { Bench, Toolboard, Drums, Rack, Gas, Panel, Grinder, Drill }

		/// <summary>The first piece under the pipes over a bay's set, on the corrugation or hung from the girder.</summary>
		public enum Upper { Gears, Fan, Hoist, Blueprints, Chalkboard, Hose }

		/// <summary>The second piece under the pipes, beside the first: long, wide or boxy, to fill the rest of the bay.</summary>
		public enum Second { Duct, Bars, Ladder, Sign, Controls, Shelf }

		Skin TrimSkin( Rect region ) => new( _trim, region );
		Skin P( Paint paint ) => TrimSkin( SolidSwatch( (int)paint ) );
		Skin Any( params Paint[] paints ) => P( paints[Pick( paints.Length )] );
		Rotation Facing => Turn( Rotation.FromToRotation( Vector3.Up, ToRoom ) );
		Rotation Lying => Turn( Rotation.FromToRotation( Vector3.Up, Vector3.Forward ) );

		/// <summary>A wall-space rotation of <paramref name="degrees"/> in the wall's plane, about the axis into it.</summary>
		static Rotation Spin( float degrees ) => Rotation.FromAxis( Across, degrees );

		void Claim( float u, float width ) => _floor.Add( (u - width * 0.5f, u + width * 0.5f) );

		/// <summary>
		/// A flat panel of <paramref name="skin"/> facing the room, <paramref name="width"/> wide from <paramref name="v0"/>
		/// to <paramref name="v1"/> at <paramref name="depth"/>: a door seam, a louvre, a pull, a label — two triangles where
		/// a box would take twelve, for detail seen only from the front.
		/// </summary>
		void Flat( Skin skin, float u, float width, float v0, float v1, float depth ) =>
			Room.WallPicture( Side, skin.Material, skin.Region, u - width * 0.5f, u + width * 0.5f, v0, v1, depth, false );

		/// <summary>Starts dressing a wall; on a wall with a low pipe run the furniture stands clear of it.</summary>
		public void StartWall( bool lowRun )
		{
			_back = lowRun ? 30.5f : Skirt;
			_headTop = lowRun ? 136f : 124f;
			_heroes.Clear();
			_risers.Clear();
			_drops.Clear();
			_gauges.Clear();
		}

		/// <summary>Where the wall being dressed has a riser up it, which the bays' furniture keeps clear of.</summary>
		readonly List<float> _risers = new();

		/// <summary>Where the wall's drops come down from the high run, and where the low run's gauges stand up from it or a cable drops from the tray: the wall's pieces keep clear of them.</summary>
		readonly List<float> _drops = new();
		readonly List<float> _gauges = new();

		/// <summary>How far into the room the high run's frontmost pipe reaches: what hangs past the run hangs in front of it.</summary>
		float _pipeFront;

		// ---- Pipework ----------------------------------------------------------------------------

		/// <summary>
		/// The high run along the whole wall: one to three pipes stacked, each its own bore and finish, coming
		/// out of the wall short of one corner on an elbow and going back in short of the other; clamped to a
		/// strut on every column, flanged here and there; a drop or two from the lowest down to a gate valve
		/// with its handwheel and a gauge, turning back into the wall over the head of the room.
		/// </summary>
		public void HighPipes( List<Bay> all ) => Apart( () =>
		{
			var pipes = new List<(float V, float Y, float R, Skin Skin)>();
			var v = Range( 184f, 194f );
			for ( var k = 2 + Pick( 2 ); k > 0; k-- )
			{
				var r = Range( 1.8f, 3.4f );
				v += r;
				pipes.Add( (v, -(3.5f + r + Range( 0f, 4f )), r, Any( Paint.Copper, Paint.Galvanised, Paint.KhakiPaint, Paint.Steel, Paint.Teal, Paint.Iron )) );
				v += r + Range( 3f, 6f );
			}
			_pipeFront = 0f;
			foreach ( var (_, py, r, _) in pipes ) _pipeFront = MathF.Min( _pipeFront, py - r );

			const float Far = FloorContext.Span - End;
			foreach ( var (pv, py, r, skin) in pipes )
			{
				var bend = 4f + r;
				var path = new List<Vector3> { new( End, Face + 1f, pv ) };
				for ( var i = 0; i <= 4; i++ )
				{
					var t = MathF.PI * 0.5f * i / 4f;
					path.Add( new Vector3( End + bend - bend * MathF.Cos( t ), py + bend - bend * MathF.Sin( t ), pv ) );
				}
				for ( var i = 0; i <= 4; i++ )
				{
					var t = MathF.PI * 0.5f * i / 4f;
					path.Add( new Vector3( Far - bend + bend * MathF.Sin( t ), py + bend - bend * MathF.Cos( t ), pv ) );
				}
				path.Add( new Vector3( Far, Face + 1f, pv ) );
				Kit.Tube( skin, At( path.ToArray() ), r, 6 );
			}

			// A strut on every column but the pier (whose plaque hangs just under the pipes), a saddle bracket
			// from it round each pipe.
			var posts = new List<float>();
			for ( var k = 0; k + 1 < all.Count; k++ ) posts.Add( (all[k].To + all[k + 1].From) * 0.5f );
			var low = pipes[0].V - pipes[0].R - 3f;
			var high = pipes[^1].V + pipes[^1].R + 3f;
			var iron = P( Paint.Iron );
			foreach ( var post in posts )
			{
				if ( MathF.Abs( post - FloorContext.HalfSpan ) < 50f ) continue;
				Kit.Box( P( Paint.Steel ), At( new Vector3( post, -0.6f, (low + high) * 0.5f ) ), new Vector3( 2.6f, 1.2f, high - low ), Run );
				foreach ( var (pv, py, r, _) in pipes )
				{
					Kit.Box( iron, At( new Vector3( post, (py - r - 0.45f - 1.2f) * 0.5f, pv ) ), new Vector3( 1.2f, -1.2f - py + r + 0.45f, r * 2f + 0.9f ), Run );
				}
			}

			// Flanges along each pipe, clear of the clamps.
			foreach ( var (pv, py, r, skin) in pipes )
				for ( var k = 2 + Pick( 3 ); k > 0; k-- )
				{
					var u = Range( End + 20f, Far - 20f );
					if ( posts.Exists( p => MathF.Abs( p - u ) < 8f ) ) continue;
					Kit.SmoothLathe( skin, At( new Vector3( u - 0.8f, py, pv ) ), new[] { new Vector2( r + 1.3f, 0f ), new Vector2( r + 1.3f, 1.6f ) }, 6, Lying, true, true );
				}

			// Risers up the corrugation from the floor into the lowest pipe, jogging out to it over the heads
			// of the room, each with a valve at a player's chest.
			var risers = all.FindAll( b => b.Width > 80f );
			Shuffle( risers );
			for ( var k = 0; k < Math.Min( risers.Count, 1 + Pick( 2 ) ); k++ )
			{
				var bay = risers[k];
				var u = Next() < 0.5f ? bay.From + Range( 7f, 16f ) : bay.To - Range( 7f, 16f );
				_risers.Add( u );
				var (pv, py, pr, pskin) = pipes[0];
				var rr = MathF.Min( pr, Range( 1.2f, 2f ) );
				var wall = Face - rr - 0.4f;
				Kit.Tube( pskin, At( new[]
				{
					new Vector3( u, wall, Ledge ), new Vector3( u, wall, 146f ), new Vector3( u, wall - 3f, 151f ), new Vector3( u, (wall + py) * 0.5f, 156f ),
					new Vector3( u, py + 3f, 161f ), new Vector3( u, py, 166f ), new Vector3( u, py, pv ),
				} ), rr, 6 );
				var valve = Range( 52f, 80f );
				Kit.SmoothLathe( P( Paint.Iron ), At( new Vector3( u, wall, valve - 3f ) ), new[]
				{
					new Vector2( rr + 0.6f, 0f ), new Vector2( rr + 2f, 1.2f ), new Vector2( rr + 2.3f, 3f ), new Vector2( rr + 2f, 4.8f ), new Vector2( rr + 0.6f, 6f ),
				}, 8, Run, false, false, 50f );
				Kit.Tube( P( Paint.Iron ), At( new[] { new Vector3( u, wall - rr - 1.6f, valve ), new Vector3( u, wall - rr - 4.6f, valve ) } ), 0.4f, 4 );
				Handwheel( new Vector3( u, wall - rr - 4.8f, valve ), Range( 3f, 3.8f ), ToRoom );
			}

			// Drops from the lowest pipe.
			var wide = all.FindAll( b => b.Width > 90f );
			Shuffle( wide );
			var (dv, dy, dr, dskin) = pipes[0];
			for ( var k = 0; k < Math.Min( wide.Count, 1 + Pick( 2 ) ); k++ )
			{
				var bay = wide[k];
				var u = bay.From + Range( 18f, bay.Width - 18f );
				_drops.Add( u );
				var rd = MathF.Min( dr, Range( 1.1f, 1.8f ) );
				var valve = Range( 154f, 164f );
				var elbow = 142.5f + rd;
				var b2 = 3f + rd;
				var down = new List<Vector3> { new( u, dy, dv ), new( u, dy, elbow + b2 ) };
				for ( var i = 0; i <= 4; i++ )
				{
					var t = MathF.PI * 0.5f * i / 4f;
					down.Add( new Vector3( u, dy + b2 - b2 * MathF.Cos( t ), elbow + b2 - b2 * MathF.Sin( t ) ) );
				}
				down.Add( new Vector3( u, Face + 1f, elbow ) );
				Kit.Tube( dskin, At( down.ToArray() ), rd, 6 );
				Kit.SmoothLathe( dskin, At( new Vector3( u, dy, dv - dr - 1.6f ) ), new[] { new Vector2( rd + 0.8f, 0f ), new Vector2( rd + 0.8f, 1.6f ) }, 8, Run, true, true );
				GateValve( new Vector3( u, dy, valve ), rd );
				var gauge = (dv + valve) * 0.5f + 2f;
				Kit.Tube( P( Paint.BrassPaint ), At( new[] { new Vector3( u, dy - rd + 0.2f, gauge ), new Vector3( u, dy - rd - 1.6f, gauge ) } ), 0.5f, 5 );
				Gauge( new Vector3( u, dy - rd - 1.6f, gauge ), Range( 2.8f, 3.4f ) );
			}
		} );

		/// <summary>A gate valve on a vertical pipe of bore <paramref name="r"/>: a cast body, a stem toward the room, a spoked handwheel.</summary>
		void GateValve( Vector3 at, float r )
		{
			Kit.SmoothLathe( P( Paint.Iron ), At( at - new Vector3( 0f, 0f, 3f ) ), new[]
			{
				new Vector2( r + 0.6f, 0f ), new Vector2( r + 2f, 1.2f ), new Vector2( r + 2.4f, 3f ), new Vector2( r + 2f, 4.8f ), new Vector2( r + 0.6f, 6f ),
			}, 8, Run, false, false, 50f );
			var hub = at + new Vector3( 0f, -r - 5f, 0f );
			Kit.SmoothLathe( P( Paint.Iron ), At( at + new Vector3( 0f, -r - 1.6f, 0f ) ), new[] { new Vector2( 1.4f, 0f ), new Vector2( 1f, 2f ), new Vector2( 0.35f, 2.2f ), new Vector2( 0.35f, 3.6f ) }, 6, Facing, false, true );
			Handwheel( hub, Range( 3.2f, 4.2f ), ToRoom );
		}

		/// <summary>A spoked handwheel of <paramref name="radius"/> at <paramref name="hub"/>, its face toward <paramref name="axis"/>, painted carrot or teal.</summary>
		void Handwheel( Vector3 hub, float radius, Vector3 axis )
		{
			var paint = Any( Paint.Carrot, Paint.Teal, Paint.Carrot );
			var a = MathF.Abs( axis.z ) > 0.9f ? Vector3.Forward : Vector3.Up;
			var b = Vector3.Cross( axis, a ).Normal;
			Kit.Tube( paint, At( Loop( hub, a, b, radius, 8 ) ), 0.5f, 3 );
			var spin = Next() * MathF.Tau;
			for ( var k = 0; k < 3; k++ )
			{
				var t = spin + k * MathF.Tau / 3f;
				Kit.Tube( paint, At( new[] { hub, hub + (a * MathF.Cos( t ) + b * MathF.Sin( t )) * radius } ), 0.3f, 3 );
			}
			Kit.SmoothLathe( paint, At( hub - axis * 0.6f ), new[] { new Vector2( 0.9f, 0f ), new Vector2( 0.9f, 1.2f ) }, 6, Turn( Rotation.FromToRotation( Vector3.Up, axis ) ), false, true );
		}

		/// <summary>A pressure gauge facing the room, its back at <paramref name="at"/>: a bright bezel round one of the three dials (painted flat, a square inside the bezel showing the round dial on its dark ground).</summary>
		void Gauge( Vector3 at, float r )
		{
			Kit.SmoothLathe( P( Next() < 0.5f ? Paint.Bright : Paint.BrassPaint ), At( at ), new[] { new Vector2( r, 0f ), new Vector2( r + 0.3f, 0.6f ), new Vector2( r + 0.3f, 1.6f ), new Vector2( r - 0.2f, 2f ) }, 8, Facing, true, false, 45f );
			Flat( TrimSkin( DialSwatch( Pick( 3 ) ) ), at.x, r * 1.4f, at.z - r * 0.7f, at.z + r * 0.7f, at.y - 1.5f );
		}

		/// <summary>
		/// A cable tray along the corrugation of every bay, through the columns, just under the heads of the room:
		/// a perforated steel channel with three or four cables laid in it, their own colours, and here and there
		/// a cable dropping out of it down the wall.
		/// </summary>
		public void CableTray( List<Bay> all ) => Apart( () =>
		{
			var v = Range( 128f, 134f );
			const float D = 7f;
			var y = Face - D * 0.5f;
			var tray = Any( Paint.Galvanised, Paint.Steel );
			var cables = new List<(Skin Skin, float Y, float R)>();
			for ( var c = 3 + Pick( 2 ); c > 0; c-- )
				cables.Add( (Any( Paint.Black, Paint.Carrot, Paint.CylinderBlue, Paint.Black ), Face - Range( 1.5f, D - 1.5f ), Range( 0.5f, 0.9f )) );
			foreach ( var bay in all )
			{
				Kit.Box( tray, At( new Vector3( bay.Middle, y, v ) ), new Vector3( bay.Width + 2f, D, 0.5f ), Run );
				Kit.Box( tray, At( new Vector3( bay.Middle, Face - D - 0.2f, v + 1.5f ) ), new Vector3( bay.Width + 2f, 0.4f, 3.4f ), Run );
				foreach ( var (skin, cy, r) in cables )
					Kit.Tube( skin, At( new[] { new Vector3( bay.From - 1f, cy, v + 0.25f + r ), new Vector3( bay.To + 1f, cy, v + 0.25f + r ) } ), r, 4 );
				if ( Next() < 0.35f )
				{
					var u = bay.From + Range( 10f, bay.Width - 10f );
					_gauges.Add( u );
					var (skin, cy, r) = cables[Pick( cables.Count )];
					Kit.Tube( skin, At( new[] { new Vector3( u, cy, v + 0.5f ), new Vector3( u, Face - 1.2f, v - 4f ), new Vector3( u + Range( -3f, 3f ), Face - 1.2f, Range( 70f, 100f ) ) } ), r, 4 );
				}
			}
		} );

		/// <summary>
		/// The low run along the bays, a hand above the skirting, through the columns: on some bays a riser up to
		/// a gauge at a player's chest, on others a valve with its handwheel on top.
		/// </summary>
		public void LowPipes( List<Bay> wall ) => Apart( () =>
		{
			var r = Range( 1.8f, 2.6f );
			var v = Range( 20f, 30f );
			var y = Face - r - 0.3f;
			var skin = Any( Paint.Galvanised, Paint.Copper, Paint.RustPaint, Paint.KhakiPaint );
			foreach ( var bay in wall )
			{
				Kit.Tube( skin, At( new[] { new Vector3( bay.From - 1f, y, v ), new Vector3( bay.To + 1f, y, v ) } ), r, 8 );
				var u = bay.From + Range( 14f, bay.Width - 14f );
				var roll = Next();
				if ( roll < 0.4f )
				{
					_gauges.Add( u );
					var top = Range( 56f, 68f );
					Kit.Tube( P( Paint.BrassPaint ), At( new[] { new Vector3( u, y, v + r ), new Vector3( u, y, top ) } ), 0.7f, 5 );
					Kit.Tube( P( Paint.BrassPaint ), At( new[] { new Vector3( u, y, top ), new Vector3( u, y - 1.6f, top ) } ), 0.6f, 5 );
					Gauge( new Vector3( u, y - 1.6f, top ), Range( 3.2f, 4.2f ) );
				}
				else if ( roll < 0.7f )
				{
					Kit.SmoothLathe( P( Paint.Iron ), At( new Vector3( u - 3f, y, v ) ), new[]
					{
						new Vector2( r + 0.4f, 0f ), new Vector2( r + 1.6f, 1f ), new Vector2( r + 1.8f, 3f ), new Vector2( r + 1.6f, 5f ), new Vector2( r + 0.4f, 6f ),
					}, 8, Lying, false, false, 50f );
					Kit.Tube( P( Paint.Iron ), At( new[] { new Vector3( u, y, v + r + 1.4f ), new Vector3( u, y, v + r + 6.5f ) } ), 0.45f, 5 );
					Handwheel( new Vector3( u, y, v + r + 6.8f ), Range( 3f, 3.6f ), Vector3.Up );
				}
			}
		} );

		// ---- Bays ---------------------------------------------------------------------------------

		/// <summary>
		/// Furnishes <paramref name="bay"/> as <paramref name="look"/> — each set claims most of the bay's width, with
		/// something a head high in it — then fills what the set leaves bare (<see cref="Fill"/>), so every bay ends up
		/// with something low, something at a player's chest or head, and pieces over the heads of the room.
		/// </summary>
		public void Furnish( Look look, Bay bay ) => Apart( () =>
		{
			_floor.Clear();
			_wall.Clear();
			_head.Clear();
			_upperTaken.Clear();
			_tall = false;
			foreach ( var riser in _risers.FindAll( r => r > bay.From && r < bay.To ) )
			{
				Claim( riser, 12f );
				Hang( riser, 14f );
				Head( riser, 14f );
			}
			foreach ( var gauge in _gauges.FindAll( g => g > bay.From && g < bay.To ) )
			{
				Hang( gauge, 12f );
				Head( gauge, 10f );
			}
			var (from, to) = (bay.From + 4f, bay.To - 4f);
			var widest = 0f;
			foreach ( var gap in Gaps( _floor, bay.From + 4f, bay.To - 4f ) )
				if ( gap.To - gap.From > widest ) { widest = gap.To - gap.From; (from, to) = gap; }
			switch ( look )
			{
				case Look.Bench:
				{
					var length = MathF.Min( to - from - 4f, Range( 70f, 86f ) );
					Row( from, to, 3f, (length + 2f, u => Workbench( u, length )), (32f, ToolCabinet) );
					break;
				}
				case Look.Toolboard:
				{
					var length = Range( 58f, 70f );
					Row( from, to, 3f, (64f, u => { Claim( u, 64f ); BigBoard( u ); ToolChest( u + Range( -14f, 14f ) ); }), (length + 2f, u => Lathe( u, length )) );
					break;
				}
				case Look.Drums:
					Row( from, to, 3f, (24f, u => Drum( u, false )), (32f, StockOrCrates), (24f, u => Drum( u, false )), (28f, CrateStack), (38f, u => Drum( u, true )) );
					break;
				case Look.Rack:
				{
					var width = Range( 50f, 60f );
					var other = Range( 40f, 50f );
					Row( from, to, 3f, (width + 2f, u => Rack( u, width )), (other + 2f, u => Rack( u, other )), (24f, u => Drum( u, false )) );
					break;
				}
				case Look.Gas:
				{
					var screen = Range( 46f, 56f );
					Row( from, to, 3f, (36f, Cylinders), (screen + 2f, u => WeldingScreen( u, screen )), (30f, u => HoseReel( u, Range( 62f, 74f ), 1f, true )) );
					break;
				}
				case Look.Panel:
					Row( from, to, 3f, (44f, Panel), (36f, SwitchCabinet), (20f, CableDrum) );
					break;
				case Look.Grinder:
				{
					var count = 2 + Pick( 2 );
					Row( from, to, 3f, (20f, Grinder), (16f, Stool), (30f, BandSaw), (15f * count + 2f, u => Lockers( u, count )) );
					break;
				}
				default:
				{
					var count = 2 + Pick( 2 );
					Row( from, to, 3f, (24f, PillarDrill), (46f, Compressor), (15f * count + 2f, u => Lockers( u, count )) );
					break;
				}
			}
			Fill( bay );
		} );

		/// <summary>
		/// Fills what a bay's set left bare. On the floor, working along each gap from one end: something a head high
		/// first where the bay has nothing tall yet (a folded stepladder, a sack truck or the stock rack once a wall,
		/// otherwise a bank of lockers, a tool cabinet, a shelving unit or a stack of crates), then a pallet, drums,
		/// crates, a bucket, tins or a broom. On the wall, at a player's chest over whatever stays low, and over the
		/// heads of the room along the whole bay (<see cref="Band"/>).
		/// </summary>
		void Fill( Bay bay )
		{
			foreach ( var (from, to) in Gaps( _floor, bay.From + 4f, bay.To - 4f ) )
			{
				var forward = Next() < 0.5f;
				var at = forward ? from : to;
				var room = to - from;
				for ( var n = 0; n < 3 && room >= 12f; n++ )
				{
					float Put( float width )
					{
						var centre = forward ? at + width * 0.5f + 1f : at - width * 0.5f - 1f;
						at += (forward ? 1f : -1f) * (width + 4f);
						room -= width + 4f;
						return centre;
					}
					var roll = Next();
					if ( !_tall && room >= 26f )
					{
						if ( roll < 0.14f && _heroes.Add( "ladder" ) ) StepLadder( Put( 24f ) );
						else if ( roll < 0.26f && _heroes.Add( "truck" ) ) SackTruck( Put( 20f ) );
						else if ( roll < 0.36f && room >= 34f && _heroes.Add( "stock" ) ) StockRack( Put( 32f ) );
						else if ( roll < 0.6f && room >= 34f )
						{
							var count = Math.Min( 2 + Pick( 3 ), (int)((room - 2f) / 15f) );
							Lockers( Put( 15f * count + 2f ), count );
						}
						else if ( roll < 0.8f && room >= 34f ) ToolCabinet( Put( 32f ) );
						else if ( room >= 46f )
						{
							var width = Range( 40f, MathF.Min( 50f, room - 4f ) );
							Rack( Put( width + 2f ), width );
						}
						else CrateStack( Put( 28f ) );
					}
					else if ( room >= 40f && roll < 0.22f ) Pallet( Put( 38f ) );
					else if ( room >= 26f && roll < 0.42f ) Drum( Put( 24f ), false );
					else if ( room >= 28f && roll < 0.56f ) CrateStack( Put( 28f ) );
					else if ( room >= 20f && roll < 0.62f && _heroes.Add( "bin" ) ) Bin( Put( 18f ) );
					else if ( room >= 14f && roll < 0.66f && _heroes.Add( "broom" ) ) Broom( Put( 12f ) );
					else if ( room >= 14f && roll < 0.84f ) Bucket( Put( 13f ) );
					else Tins( Put( 12f ) );
				}
			}

			foreach ( var (from, to) in Gaps( _wall, bay.From + 5f, bay.To - 5f ) ) Band( from, to, false );
			foreach ( var (from, to) in Gaps( _head, bay.From + 5f, bay.To - 5f ) ) Band( from, to, true );
		}

		/// <summary>
		/// Fills a bare stretch of wall from one end, a piece at a time: at a player's chest (from <see cref="ChestFoot"/>)
		/// a panel of parts bins, a tool board, a steel cabinet, a safety poster, or once a wall the planning board, the
		/// shadow board, the first-aid box or the calendar; over the heads of the room (<paramref name="high"/>, from
		/// <see cref="HeadFoot"/> to the cable tray) a long shelf of stock, a bin panel, a cabinet, a rail of clamps, or
		/// once a wall a rail of hi-vis vests and hard hats.
		/// </summary>
		void Band( float from, float to, bool high )
		{
			var at = from + Range( 0f, 4f );
			var last = -1;
			while ( to - at >= 18f )
			{
				var room = to - at;
				const int Kinds = 6;
				var kind = Pick( Kinds );
				if ( kind == last ) kind = (kind + 1 + Pick( Kinds - 1 )) % Kinds;
				last = kind;
				float width;
				if ( high )
				{
					var foot = HeadFoot + Range( 0f, 4f );
					if ( kind <= 1 && room >= 44f ) { width = MathF.Min( room - 2f, Range( 54f, 92f ) ); WallShelf( at + width * 0.5f, width, foot + 6f ); }
					else if ( kind == 2 && room >= 34f ) { width = MathF.Min( room - 2f, Range( 32f, 48f ) ); BinPanel( at + width * 0.5f, width, foot, _headTop - foot ); }
					else if ( kind == 3 && room >= 30f ) { width = MathF.Min( room - 2f, Range( 28f, 40f ) ); WallCabinet( at + width * 0.5f, width, foot, _headTop - foot ); }
					else if ( kind == 4 && room >= 40f && _heroes.Add( "vests" ) ) { width = MathF.Min( room - 2f, Range( 40f, 54f ) ); VestRail( at + width * 0.5f, width, _headTop - 4f ); }
					else if ( kind == 4 && room >= 44f ) { width = MathF.Min( room - 2f, Range( 48f, 80f ) ); WallShelf( at + width * 0.5f, width, foot + 6f ); }
					else if ( kind != 5 && room >= 34f ) { width = MathF.Min( room - 2f, Range( 32f, 44f ) ); BinPanel( at + width * 0.5f, width, foot, _headTop - foot ); }
					else if ( room >= 26f ) { width = MathF.Min( room - 2f, Range( 24f, 34f ) ); ClampRail( at + width * 0.5f, width, _headTop - 3f ); }
					else { width = 16f; Notice( at + 8f, foot + Range( 10f, 20f ), Pick( 3 ) ); }
				}
				else
				{
					var foot = ChestFoot + Range( 2f, 8f );
					if ( kind <= 1 && room >= 36f ) { width = MathF.Min( room - 2f, Range( 36f, 54f ) ); BinPanel( at + width * 0.5f, width, foot, Range( 30f, 38f ) ); }
					else if ( kind == 2 && room >= 40f ) { width = MathF.Min( room - 2f, Range( 40f, 58f ) ); Pegboard( at + width * 0.5f, width, foot, width * Range( 0.55f, 0.7f ) ); }
					else if ( kind == 3 && room >= 32f ) { width = MathF.Min( room - 2f, Range( 30f, 42f ) ); WallCabinet( at + width * 0.5f, width, foot + 4f, width * Range( 1f, 1.2f ) ); }
					else if ( kind == 4 && room >= 56f && _heroes.Add( "planner" ) ) { width = MathF.Min( room - 2f, Range( 56f, 76f ) ); Picture( at + width * 0.5f, width, width * 0.5f, foot + 8f, PlannerSwatch, true ); }
					else if ( kind == 4 && room >= 46f && _heroes.Add( "shadow" ) ) { width = MathF.Min( room - 2f, Range( 44f, 58f ) ); ShadowBoard( at + width * 0.5f, width, foot + 4f ); }
					else if ( kind == 5 && _heroes.Add( "aid" ) ) { width = 15f; FirstAid( at + width * 0.5f, foot + 14f ); }
					else if ( kind == 5 && room >= 20f && _heroes.Add( "calendar" ) ) { width = 18f; Picture( at + width * 0.5f, 16f, 24f, foot + 14f, CalendarSwatch, false ); }
					else if ( room >= 26f ) { width = Range( 22f, 28f ); Poster( at + width * 0.5f, width, foot + Range( 6f, 12f ) ); }
					else { width = 16f; Notice( at + 8f, foot + Range( 16f, 26f ), Pick( 3 ) ); }
				}
				at += width + Range( 4f, 9f );
			}
		}

		/// <summary>The stretches along the bay that something hangs over at a player's chest (from v 46), or stands that tall.</summary>
		readonly List<(float From, float To)> _wall = new();

		/// <summary>The stretches along the bay that something reaches over the heads of the room at (from v 94).</summary>
		readonly List<(float From, float To)> _head = new();

		/// <summary>Claims a stretch of the chest band, and of the band over the heads of the room as well where the piece reaches <paramref name="top"/> that high.</summary>
		void Hang( float u, float width, float top = 0f )
		{
			_wall.Add( (u - width * 0.5f, u + width * 0.5f) );
			if ( top > HeadFoot - 2f ) Head( u, width );
		}
		void Head( float u, float width ) => _head.Add( (u - width * 0.5f, u + width * 0.5f) );

		/// <summary>
		/// A bank of <paramref name="count"/> steel lockers a head high, one colour, a seam between the doors, louvres at
		/// the top and a bright handle on each (flat panels: only their fronts are ever seen); a carton on top now and then.
		/// </summary>
		void Lockers( float u, int count )
		{
			const float W = 15f;
			Claim( u, W * count + 2f );
			Hang( u, W * count + 2f );
			_tall = true;
			const float D = 15f;
			var y = _back - D * 0.5f - 0.5f;
			var paint = Any( Paint.Machine, Paint.Olive, Paint.KhakiPaint, Paint.Teal, Paint.Steel, Paint.CylinderBlue );
			var height = Range( 68f, 76f );
			var front = y - D * 0.5f - 0.05f;
			var top = Ledge + height;
			Kit.Box( paint, At( new Vector3( u, y, Ledge + height * 0.5f ) ), new Vector3( W * count, D, height ), Run );
			for ( var k = 0; k < count; k++ )
			{
				var x = u - W * count * 0.5f + W * (k + 0.5f);
				if ( k > 0 ) Flat( P( Paint.Iron ), x - W * 0.5f, 0.5f, Ledge + 1.5f, top - 1.5f, front );
				Flat( P( Paint.Iron ), x, 7f, top - 12f, top - 5f, front );
				Flat( P( Paint.Bright ), x + W * 0.3f, 1f, top - height * 0.5f - 2.5f, top - height * 0.5f + 2.5f, front - 0.05f );
			}
			if ( Next() < 0.45f )
			{
				var w = Range( 12f, 18f );
				var h = Range( 7f, 11f );
				Kit.Box( P( Paint.Cardboard ), At( new Vector3( u + Range( -0.3f, 0.3f ) * W * (count - 1), y + Range( -1f, 1f ), top + h * 0.5f ) ), new Vector3( w, D - 3f, h ), Turn( Rotation.FromYaw( Range( -6f, 6f ) ) ) );
			}
		}

		/// <summary>
		/// A stock rack: two steel arms out of the wall, and lengths of pipe, bar and plank stood on end against
		/// it, leaning, each its own length and finish — the tallest thing in a bay.
		/// </summary>
		void StockRack( float u )
		{
			Claim( u, 32f );
			Hang( u, 32f );
			Head( u, 32f );
			_tall = true;
			const float Foot = 9f;
			foreach ( var v in new[] { Range( 50f, 58f ), Range( 96f, 106f ) } )
			{
				Kit.Box( P( Paint.Iron ), At( new Vector3( u, Face - 6f, v ) ), new Vector3( 30f, 1.4f, 1.6f ), Run );
				foreach ( var s in new[] { -1f, 1f } )
					Kit.Box( P( Paint.Iron ), At( new Vector3( u + s * 14f, Face - 3f, v ) ), new Vector3( 1.4f, 6f, 1.6f ), Run );
			}
			var at = u - 13f;
			while ( at < u + 13f )
			{
				var top = new Vector3( at + Range( -1f, 1f ), Face - Range( 7.5f, 9.5f ), Range( 96f, 128f ) );
				var foot = new Vector3( at + Range( -3f, 3f ), Foot + Range( 0f, 4f ), Ledge );
				if ( Next() < 0.3f )
				{
					var axis = (top - foot).Normal;
					Kit.Box( TrimSkin( TimberSwatch ), At( (top + foot) * 0.5f ), new Vector3( Range( 3f, 5f ), 1.2f, (top - foot).Length ), Turn( Rotation.LookAt( Vector3.Forward, axis ) ) );
					at += Range( 4f, 6f );
				}
				else
				{
					var r = Range( 0.6f, 1.6f );
					Kit.Tube( Any( Paint.Bright, Paint.Galvanised, Paint.Copper, Paint.RustPaint, Paint.Steel ), At( new[] { foot + new Vector3( 0f, 0f, r ), top } ), r, 5 );
					at += r * 2f + Range( 0.6f, 2f );
				}
			}
		}

		/// <summary>The stock rack once a wall, crates where the wall has had it already.</summary>
		void StockOrCrates( float u )
		{
			if ( _heroes.Add( "stock" ) ) StockRack( u );
			else CrateStack( u );
		}

		/// <summary>A tall roller cabinet of drawers, chest to head high: carrot, teal or blue-grey, a bright pull along every drawer, a rubber plinth, a toolbox on top now and then.</summary>
		void ToolCabinet( float u )
		{
			const float W = 30f, D = 18f;
			Claim( u, W + 2f );
			Hang( u, W + 2f );
			_tall = true;
			var y = _back - D * 0.5f - 0.5f;
			var paint = Any( Paint.Carrot, Paint.Teal, Paint.Machine, Paint.CylinderBlue, Paint.Navy );
			var height = Range( 56f, 66f );
			var front = y - D * 0.5f - 0.05f;
			var foot = Ledge + 3f;
			Kit.Box( P( Paint.Rubber ), At( new Vector3( u, y, Ledge + 1.5f ) ), new Vector3( W - 3f, D - 3f, 3f ), Run );
			Kit.Box( paint, At( new Vector3( u, y, foot + height * 0.5f ) ), new Vector3( W, D, height ), Run );
			var drawers = 5 + Pick( 3 );
			var shares = drawers - 0.6f;
			var z = foot + height - 2f;
			for ( var k = 0; k < drawers; k++ )
			{
				var h = (height - 4f) * (k < 2 ? 0.7f : 1f) / shares;
				Flat( P( Paint.Bright ), u, W * 0.55f, z - h * 0.3f - 0.4f, z - h * 0.3f + 0.4f, front - 0.05f );
				z -= h;
				if ( k + 1 < drawers ) Flat( P( Paint.Iron ), u, W - 2f, z - 0.25f, z + 0.25f, front );
			}
			if ( Next() < 0.5f ) Toolbox( new Vector3( u + Range( -4f, 4f ), y + Range( 0f, 2f ), foot + height ), Range( -10f, 10f ) );
		}

		/// <summary>
		/// A lathe along the wall on its cabinet stand, a player's waist high: two pedestal cabinets, a chip tray, the
		/// bright bed, the headstock with its chuck at one end, the carriage and tool post, the tailstock with its
		/// handwheel, a splash guard behind.
		/// </summary>
		void Lathe( float u, float length )
		{
			Claim( u, length + 2f );
			const float D = 18f;
			const float Bed = 32f;
			var y = _back - D * 0.5f - 0.5f;
			var paint = Any( Paint.GreenEnamel, Paint.Machine, Paint.Cream, Paint.Teal );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.Box( paint, At( new Vector3( u + s * (length * 0.5f - 8f), y, (Ledge + Bed - 6f) * 0.5f ) ), new Vector3( 16f, D - 2f, Bed - 6f - Ledge ), Run );
				Flat( P( Paint.Iron ), u + s * (length * 0.5f - 8f), 12f, Bed - 12f, Bed - 11.4f, y - D * 0.5f + 0.9f );
			}
			Kit.Box( P( Paint.Iron ), At( new Vector3( u, y, Bed - 5f ) ), new Vector3( length, D, 2f ), Run );
			Kit.Box( P( Paint.Bright ), At( new Vector3( u, y + 2f, Bed - 1.5f ) ), new Vector3( length - 8f, 7f, 5f ), Run );
			Kit.Box( P( Paint.Galvanised ), At( new Vector3( u, _back - 1.5f, Bed + 5f ) ), new Vector3( length - 4f, 1f, 16f ), Run );
			var end = Next() < 0.5f ? -1f : 1f;
			var head = u + end * (length * 0.5f - 9f);
			Kit.BevelBox( paint, At( new Vector3( head, y + 1f, Bed + 7f ) ), new Vector3( 16f, 14f, 14f ), 0.8f, Run );
			var chuck = head - end * 8f;
			Kit.SmoothLathe( P( Paint.Bright ), At( new Vector3( end > 0f ? chuck - 3f : chuck, y, Bed + 5f ) ), new[] { new Vector2( 3.8f, 0f ), new Vector2( 3.8f, 3f ) }, 8, Lying, true, true );
			var tail = u - end * (length * 0.5f - 10f);
			Kit.Box( paint, At( new Vector3( tail, y + 1f, Bed + 4.5f ) ), new Vector3( 9f, 9f, 9f ), Run );
			Kit.Tube( P( Paint.Bright ), At( new[] { new Vector3( tail - end * 4.5f, y, Bed + 5f ), new Vector3( tail - end * 9f, y, Bed + 5f ) } ), 1f, 5 );
			var carriage = u + Range( -0.15f, 0.15f ) * length;
			Kit.Box( paint, At( new Vector3( carriage, y - 1f, Bed + 2.5f ) ), new Vector3( 11f, 13f, 5f ), Run );
			Kit.Box( P( Paint.Iron ), At( new Vector3( carriage, y, Bed + 6.5f ) ), new Vector3( 4f, 4f, 3f ), Run );
			Flat( P( Paint.Carrot ), carriage, 3f, Bed - 1f, Bed + 2f, y - 7.6f );
		}

		/// <summary>A band saw a head high: a cabinet base, the spine of the C-frame, the upper and lower wheel housings, the table at a player's waist, the blade between.</summary>
		void BandSaw( float u )
		{
			Claim( u, 28f );
			Hang( u, 28f );
			_tall = true;
			var y = _back - 9f;
			var paint = Any( Paint.Machine, Paint.GreenEnamel, Paint.Cream, Paint.Teal );
			var s = Next() < 0.5f ? -1f : 1f;
			Kit.Box( paint, At( new Vector3( u, y, Ledge + 12f ) ), new Vector3( 20f, 16f, 24f ), Run );
			Kit.Box( paint, At( new Vector3( u + s * 7f, y + 2f, Ledge + 47f ) ), new Vector3( 6f, 9f, 46f ), Run );
			Kit.BevelBox( paint, At( new Vector3( u, y - 1f, Ledge + 63f ) ), new Vector3( 24f, 8f, 20f ), 2.4f, Run );
			Kit.BevelBox( paint, At( new Vector3( u, y - 1f, Ledge + 32f ) ), new Vector3( 24f, 8f, 14f ), 2f, Run );
			Kit.Box( P( Paint.Bright ), At( new Vector3( u, y - 2f, Ledge + 40.5f ) ), new Vector3( 22f, 18f, 1.6f ), Run );
			Kit.Box( P( Paint.Bright ), At( new Vector3( u - s * 6f, y - 3f, Ledge + 47f ) ), new Vector3( 0.6f, 0.4f, 12f ), Run );
			Kit.Box( P( Paint.Carrot ), At( new Vector3( u - s * 6f, y - 3.5f, Ledge + 51.5f ) ), new Vector3( 2.4f, 2f, 3f ), Run );
			Flat( P( Paint.Iron ), u, 12f, Ledge + 4f, Ledge + 5f, y - 8.05f );
		}

		/// <summary>An air compressor on the floor: the tank lying on its feet, the pump and motor on top behind a belt guard, a gauge, the air line coiled on a hook on the wall above.</summary>
		void Compressor( float u )
		{
			const float L = 44f, R = 9f;
			Claim( u, L + 2f );
			var y = _back - R - 1.5f;
			var paint = Any( Paint.Carrot, Paint.Teal, Paint.Machine, Paint.Olive, Paint.Navy );
			var axis = Ledge + 4f + R;
			Kit.SmoothLathe( paint, At( new Vector3( u - L * 0.5f, y, axis ) ), new[]
			{
				new Vector2( 0f, 0f ), new Vector2( R * 0.75f, 1.2f ), new Vector2( R, 4f ), new Vector2( R, L - 4f ), new Vector2( R * 0.75f, L - 1.2f ), new Vector2( 0f, L ),
			}, 10, Lying, false, false, 50f, Next() );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( P( Paint.Iron ), At( new Vector3( u + s * (L * 0.5f - 9f), y, Ledge + 2.5f ) ), new Vector3( 3f, R * 1.6f, 5f ), Run );
			var side = Next() < 0.5f ? -1f : 1f;
			Kit.Box( P( Paint.Steel ), At( new Vector3( u + side * 8f, y + 1f, axis + R + 4.5f ) ), new Vector3( 14f, 11f, 9f ), Run );
			Kit.BevelBox( paint, At( new Vector3( u - side * 7f, y + 1f, axis + R + 5f ) ), new Vector3( 9f, 9f, 10f ), 1f, Run );
			Kit.Box( P( Paint.Black ), At( new Vector3( u, y - 4.5f, axis + R + 5f ) ), new Vector3( 22f, 2f, 9f ), Run );
			Kit.Box( P( Paint.Iron ), At( new Vector3( u - side * 15f, y - R + 0.5f, axis + R - 3f ) ), new Vector3( 5f, 2f, 5f ), Run );
			Flat( TrimSkin( DialSwatch( Pick( 3 ) ) ), u - side * 15f, 4.4f, axis + R - 5.2f, axis + R - 0.8f, y - R - 0.6f );
			var coil = new Vector3( u + side * 12f, Face - 2.6f, Range( 54f, 62f ) );
			Kit.Box( P( Paint.Iron ), At( coil + new Vector3( 0f, 1f, 6.6f ) ), new Vector3( 1f, 3f, 1f ), Run );
			Kit.Tube( Any( Paint.Carrot, Paint.CylinderBlue, Paint.Black ), At( Loop( coil, Vector3.Forward, Vector3.Up, 6f, 10 ) ), 0.9f, 4 );
			Hang( coil.x, 14f );
		}

		/// <summary>A welding screen stood against the wall: a tube frame on two feet, a tinted curtain hung in it with its folds.</summary>
		void WeldingScreen( float u, float width )
		{
			Claim( u, width + 2f );
			Hang( u, width + 2f );
			_tall = true;
			var y = _back - 7f;
			var height = Range( 64f, 72f );
			var frame = Any( Paint.Steel, Paint.Galvanised, Paint.Carrot );
			var top = Ledge + height;
			foreach ( var s in new[] { -1f, 1f } )
			{
				var x = u + s * width * 0.5f;
				Kit.Tube( frame, At( new[] { new Vector3( x, y, Ledge + 1.4f ), new Vector3( x, y, top ) } ), 0.8f, 4 );
				Kit.Box( frame, At( new Vector3( x, y, Ledge + 0.7f ) ), new Vector3( 2f, 13f, 1.4f ), Run );
			}
			Kit.Tube( frame, At( new[] { new Vector3( u - width * 0.5f, y, top ), new Vector3( u + width * 0.5f, y, top ) } ), 0.8f, 4 );
			var curtain = Any( Paint.Teal, Paint.GreenEnamel, Paint.Olive, Paint.Navy );
			Kit.Box( curtain, At( new Vector3( u, y, (Ledge + 7f + top - 1.5f) * 0.5f ) ), new Vector3( width - 2.4f, 0.6f, top - 1.5f - Ledge - 7f ), Run );
			for ( var k = 1; k < 4; k++ )
				Flat( P( Paint.Black ), u - width * 0.5f + width * k / 4f + Range( -2f, 2f ), 0.5f, Ledge + 8f, top - 2.5f, y - 0.35f );
		}

		/// <summary>A switchgear cabinet a head high: grey steel, two doors with their handles, a warning plate, louvres at the foot, conduits from its top up the wall to the girder.</summary>
		void SwitchCabinet( float u )
		{
			const float W = 34f, D = 16f;
			Claim( u, W + 2f );
			Hang( u, W + 2f );
			_tall = true;
			var y = _back - D * 0.5f - 0.5f;
			var height = Range( 70f, 78f );
			var front = y - D * 0.5f - 0.05f;
			var top = Ledge + height;
			Kit.Box( Any( Paint.Steel, Paint.Machine, Paint.Cream ), At( new Vector3( u, y, Ledge + height * 0.5f ) ), new Vector3( W, D, height ), Run );
			Flat( P( Paint.Iron ), u, 0.5f, Ledge + 2f, top - 2f, front );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Flat( P( Paint.Black ), u + s * 2.4f, 1.2f, top - height * 0.55f - 3f, top - height * 0.55f + 3f, front - 0.05f );
				Flat( P( Paint.Iron ), u + s * 8.5f, 10f, Ledge + 4f, Ledge + 9f, front );
			}
			Flat( TrimSkin( SignSwatch ), u - 8.5f, 8f, top - 16f, top - 8f, front - 0.05f );
			foreach ( var x in new[] { u - 9f, u + 3f, u + 10f } )
				Kit.Tube( P( Paint.Galvanised ), At( new[] { new Vector3( x, y, top - 0.5f ), new Vector3( x, y, top + 3f ), new Vector3( x, y + 2.5f, top + 5.5f ), new Vector3( x, Face + 1f, top + 5.5f ) } ), 0.9f, 4 );
		}

		/// <summary>
		/// A louvred panel of parts bins <paramref name="width"/> by <paramref name="height"/>, its foot at <paramref name="v0"/>:
		/// a steel backing, a louvre rail under each row, and a row of plastic bins on each, one colour a row.
		/// </summary>
		void BinPanel( float u, float width, float v0, float height )
		{
			Hang( u, width, v0 + height );
			const float Y = Face - 1f;
			Flat( Any( Paint.Steel, Paint.Galvanised, Paint.Machine ), u, width, v0, v0 + height, Face - 0.4f );
			var rows = Math.Clamp( (int)(height / 10f), 2, 4 );
			var pitch = height / rows;
			for ( var r = 0; r < rows; r++ )
			{
				var v = v0 + 1.5f + r * pitch;
				Flat( P( Paint.Iron ), u, width - 1f, v - 0.6f, v + 0.2f, Face - 0.6f );
				var bin = Range( 8f, 11f );
				var count = Math.Max( 2, (int)((width - 2f) / (bin + 0.8f)) );
				var paint = Any( Paint.Teal, Paint.Carrot, Paint.CylinderBlue, Paint.Olive, Paint.Navy, Paint.HiVis );
				var h = MathF.Min( pitch - 2f, Range( 5.5f, 7f ) );
				Kit.Box( paint, At( new Vector3( u, Y - 4.5f, v + h * 0.5f ) ), new Vector3( width - 2f, 9f, h ), Run );
				for ( var k = 1; k < count; k++ )
					Flat( P( Paint.Iron ), u - width * 0.5f + 1f + (width - 2f) * k / count, 0.7f, v, v + h, Y - 9.05f );
			}
		}

		/// <summary>A rail of hooks over the heads of the room: two or three hi-vis vests hung by their shoulders, their reflective bands, and a hard hat or two on hooks of their own.</summary>
		void VestRail( float u, float length, float v )
		{
			Hang( u, length, v );
			Kit.Box( P( Paint.Handle ), At( new Vector3( u, Face - 1f, v ) ), new Vector3( length, 2f, 2.6f ), Run );
			var at = u - length * 0.5f + Range( 3f, 5f );
			var vests = 2 + Pick( 2 );
			for ( var k = 0; k < vests && at < u + length * 0.5f - 12f; k++ )
			{
				var w = Range( 13f, 15f );
				var drop = Range( 22f, 26f );
				var x = at + w * 0.5f;
				Kit.Box( P( Paint.HiVis ), At( new Vector3( x, Face - 2.8f, v - 2f - drop * 0.5f ) ), new Vector3( w, 2f, drop ), Turn( Spin( Range( -4f, 4f ) ) ) );
				foreach ( var band in new[] { 0.45f, 0.75f } )
					Flat( P( Paint.Bright ), x, w - 0.6f, v - 2f - drop * band - 0.9f, v - 2f - drop * band + 0.9f, Face - 3.85f );
				at += w + Range( 1.5f, 3f );
			}
			for ( var k = 0; k < 2 && at < u + length * 0.5f - 6f; k++ )
			{
				var x = at + 6f;
				Kit.Box( P( Paint.Iron ), At( new Vector3( x, Face - 2.5f, v - 1.5f ) ), new Vector3( 1f, 3f, 1f ), Run );
				Kit.SmoothLathe( Any( Paint.Hat, Paint.Chalk, Paint.Hat ), At( new Vector3( x, Face - 5.5f, v - 9f ) ), new[]
				{
					new Vector2( 6.4f, 0f ), new Vector2( 6.4f, 0.6f ), new Vector2( 4.8f, 1.2f ), new Vector2( 4.4f, 4f ), new Vector2( 2.6f, 6.2f ), new Vector2( 0f, 6.8f ),
				}, 8, Facing, true, false, 50f );
				at += 13f;
			}
		}

		/// <summary>
		/// A pillar drill a head high: a base, the column, the table on its arm, the head with its belt guard and
		/// motor at the back, the quill and chuck, the three-spoked feed handle.
		/// </summary>
		void PillarDrill( float u )
		{
			Claim( u, 24f );
			Hang( u, 24f );
			_tall = true;
			var y = _back - 11f;
			var paint = Any( Paint.GreenEnamel, Paint.Machine, Paint.Teal );
			Kit.BevelBox( paint, At( new Vector3( u, y, Ledge + 2f ) ), new Vector3( 20f, 18f, 4f ), 0.6f, Run );
			Kit.Tube( P( Paint.Bright ), At( new[] { new Vector3( u, y + 5f, Ledge + 4f ), new Vector3( u, y + 5f, Ledge + 64f ) } ), 1.6f, 8 );
			Kit.BevelBox( paint, At( new Vector3( u, y + 1f, Ledge + 30f ) ), new Vector3( 4f, 10f, 3f ), 0.4f, Run );
			Kit.SmoothLathe( paint, At( new Vector3( u, y - 2f, Ledge + 31.5f ) ), new[] { new Vector2( 6f, 0f ), new Vector2( 6f, 1.4f ) }, 12, Run, true, true );
			var head = Ledge + 62f;
			Kit.BevelBox( paint, At( new Vector3( u, y + 1f, head ) ), new Vector3( 12f, 18f, 9f ), 1.2f, Run );
			Kit.BevelBox( paint, At( new Vector3( u, y + 1f, head + 7f ) ), new Vector3( 13f, 20f, 5f ), 1.4f, Run );
			Kit.SmoothLathe( paint, At( new Vector3( u - 3f, y + 9f, head - 6f ) ), new[] { new Vector2( 4f, 0f ), new Vector2( 4.2f, 1f ), new Vector2( 4.2f, 9f ), new Vector2( 3f, 10f ) }, 10, Run, true, true, 40f );
			Kit.Tube( P( Paint.Bright ), At( new[] { new Vector3( u, y - 5f, head - 4.5f ), new Vector3( u, y - 5f, head - 12f ) } ), 0.9f, 6 );
			Kit.SmoothLathe( P( Paint.Iron ), At( new Vector3( u, y - 5f, head - 16f ) ), new[] { new Vector2( 0.2f, 0f ), new Vector2( 1.4f, 1.6f ), new Vector2( 1.4f, 4f ) }, 6, Run, false, true );
			var hub = new Vector3( u + 6.6f, y - 1f, head - 1f );
			for ( var k = 0; k < 3; k++ )
			{
				var t = k * MathF.Tau / 3f + 0.3f;
				var dir = new Vector3( 0f, MathF.Cos( t ), MathF.Sin( t ) );
				Kit.Tube( P( Paint.Bright ), At( new[] { hub, hub + dir * 7f } ), 0.3f, 3 );
				Kit.Ellipsoid( P( Paint.Black ), At( hub + dir * 7.4f ), new Vector3( 0.8f, 0.8f, 0.8f ), Run, 2, 5 );
			}
		}

		/// <summary>A steel wall cabinet <paramref name="width"/> by <paramref name="height"/>, its foot at <paramref name="v"/>: two doors with their seam and bright handles (flat, as only their fronts are ever seen), a padlock on some.</summary>
		void WallCabinet( float u, float width, float v, float height )
		{
			Hang( u, width, v + height );
			const float D = 9f;
			const float Door = Face - D - 0.05f;
			var middle = v + height * 0.5f;
			Kit.Box( Any( Paint.Machine, Paint.Olive, Paint.KhakiPaint, Paint.Steel, Paint.Navy ), At( new Vector3( u, Face - D * 0.5f, middle ) ), new Vector3( width, D, height ), Run );
			Flat( P( Paint.Iron ), u, 0.5f, v + 1f, v + height - 1f, Door );
			foreach ( var s in new[] { -1f, 1f } )
				Flat( P( Paint.Bright ), u + s * 1.8f, 0.8f, middle - 1.8f, middle + 1.8f, Door - 0.05f );
			if ( Next() < 0.4f ) Flat( P( Paint.BrassPaint ), u, 2f, middle - 4.6f, middle - 2.2f, Door - 0.1f );
		}

		/// <summary>A safety poster <paramref name="width"/> wide, its foot at <paramref name="v"/>, pinned at its corners.</summary>
		void Poster( float u, float width, float v )
		{
			var height = width * 1.5f;
			Hang( u, width, v + height );
			_poster = (_poster + 1 + Pick( 2 )) % 3;
			Room.WallPicture( Side, _trim, PosterSwatch( _poster ), u - width * 0.5f, u + width * 0.5f, v, v + height, Face - 0.3f, false );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Ellipsoid( P( Paint.Carrot ), At( new Vector3( u + s * (width * 0.5f - 1.5f), Face - 0.6f, v + height - 1.5f ) ), new Vector3( 0.5f, 0.5f, 0.5f ), Run, 2, 4 );
		}

		/// <summary>The last poster hung on this wall: the next is always another.</summary>
		int _poster;

		/// <summary>A pallet on the floor, cartons or sacks stacked on it.</summary>
		void Pallet( float u )
		{
			Claim( u, 38f );
			const float D = 24f;
			var y = _back - D * 0.5f - 0.5f;
			var wood = TrimSkin( TimberSwatch );
			foreach ( var s in new[] { -1f, 0f, 1f } )
				Kit.Box( wood, At( new Vector3( u, y + s * (D * 0.5f - 2f), Ledge + 1.6f ) ), new Vector3( 36f, 3.4f, 3.2f ), Run );
			foreach ( var x in new[] { -15f, -7.5f, 0f, 7.5f, 15f } )
				Kit.Box( wood, At( new Vector3( u + x, y, Ledge + 3.8f ) ), new Vector3( 5f, D, 1.2f ), Run );
			var v = Ledge + 4.4f;
			if ( Next() < 0.5f )
			{
				for ( var layer = 0; layer < 2 + Pick( 2 ); layer++ )
				{
					var h = Range( 8f, 12f );
					var w = Range( 14f, 17f );
					foreach ( var s in new[] { -1f, 1f } )
						if ( layer == 0 || Next() < 0.75f )
							Kit.Box( P( Paint.Cardboard ), At( new Vector3( u + s * (w * 0.5f + 0.3f) + Range( -1f, 1f ), y + Range( -1f, 1f ), v + h * 0.5f ) ), new Vector3( w, D - 3f, h ), Turn( Rotation.FromYaw( Range( -4f, 4f ) ) ) );
					v += h;
				}
				return;
			}
			var sacking = Any( Paint.Hessian, Paint.Cardboard, Paint.Hessian );
			for ( var layer = 0; layer < 3; layer++ )
				foreach ( var s in new[] { -1f, 1f } )
					Kit.BevelBox( sacking, At( new Vector3( u + s * 8.6f + Range( -1f, 1f ), y + Range( -1f, 1f ), v + 2.4f + layer * 4.6f ) ), new Vector3( 16f, 19f, 4.8f ), 1.8f, Turn( Rotation.FromYaw( Range( -8f, 8f ) ) ) );
		}

		/// <summary>A wheeled bin, dark green, its lid shut.</summary>
		void Bin( float u )
		{
			Claim( u, 18f );
			const float D = 18f;
			var y = _back - D * 0.5f - 0.5f;
			Kit.BevelBox( P( Paint.GreenEnamel ), At( new Vector3( u, y, Ledge + 20f ) ), new Vector3( 16f, D, 36f ), 1f, Run );
			Kit.BevelBox( P( Paint.GreenEnamel ), At( new Vector3( u, y - 0.4f, Ledge + 38.6f ) ), new Vector3( 17f, D + 1.4f, 1.6f ), 0.5f, Run );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.SmoothLathe( P( Paint.Rubber ), At( new Vector3( u + s * 8f - (s > 0f ? 0f : 1.6f), y + D * 0.5f - 3f, Ledge + 3.2f ) ), new[] { new Vector2( 3.2f, 0f ), new Vector2( 3.2f, 1.6f ) }, 10, Lying, true, true );
		}

		/// <summary>A board on battens: the planning board, the calendar — a painted sheet, <paramref name="framed"/> in aluminium or just pinned.</summary>
		void Picture( float u, float width, float height, float v, Rect sheet, bool framed )
		{
			Hang( u, width + 4f, v + height );
			Room.WallPicture( Side, _trim, sheet, u - width * 0.5f, u + width * 0.5f, v, v + height, Face - (framed ? 1.6f : 0.3f), false );
			if ( !framed )
			{
				Kit.Ellipsoid( P( Paint.Iron ), At( new Vector3( u, Face - 0.6f, v + height + 0.6f ) ), new Vector3( 0.5f, 0.5f, 0.5f ), Run, 2, 4 );
				return;
			}
			Kit.Box( P( Paint.Steel ), At( new Vector3( u, (Face - 1.55f + Face + 1f) * 0.5f, v + height * 0.5f ) ), new Vector3( width, 2.55f, height ), Run );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.Box( P( Paint.Bright ), At( new Vector3( u, Face - 2f, v + height * 0.5f + s * (height * 0.5f + 0.6f) ) ), new Vector3( width + 2.4f, 1.6f, 1.2f ), Run );
				Kit.Box( P( Paint.Bright ), At( new Vector3( u + s * (width * 0.5f + 0.6f), Face - 2f, v + height * 0.5f ) ), new Vector3( 1.2f, 1.6f, height ), Run );
			}
		}

		/// <summary>A shadow board <paramref name="width"/> wide at <paramref name="v"/>: tool outlines painted on dark green, a few tools hung back in theirs.</summary>
		void ShadowBoard( float u, float width, float v )
		{
			var height = width * 0.75f;
			Hang( u, width + 4f, v + height );
			Room.WallPicture( Side, _trim, ShadowBoardSwatch, u - width * 0.5f, u + width * 0.5f, v, v + height, Face - 1.45f, false );
			Kit.Box( P( Paint.GreenEnamel ), At( new Vector3( u, (Face - 1.4f + Face + 1f) * 0.5f, v + height * 0.5f ) ), new Vector3( width, 2.4f, height ), Run );
			var at = u - width * 0.5f + Range( 4f, 8f );
			for ( var k = 0; k < 3 && at < u + width * 0.5f - 6f; k++ )
				at += Tool( Pick( 8 ), at, v + height - 4f, Face - 2.6f, height - 8f ) + Range( 6f, 12f );
		}

		/// <summary>A first-aid box on the wall: white, a green cross.</summary>
		void FirstAid( float u, float v )
		{
			Hang( u, 14f );
			Kit.BevelBox( P( Paint.Chalk ), At( new Vector3( u, Face - 3f, v + 7f ) ), new Vector3( 12f, 6f, 14f ), 0.6f, Run );
			Kit.Box( P( Paint.GreenEnamel ), At( new Vector3( u, Face - 6.1f, v + 8f ) ), new Vector3( 6f, 0.2f, 1.8f ), Run );
			Kit.Box( P( Paint.GreenEnamel ), At( new Vector3( u, Face - 6.1f, v + 8f ) ), new Vector3( 1.8f, 0.2f, 6f ), Run );
		}

		/// <summary>A rail at <paramref name="v"/> with three or more sash clamps hanging from it, bars of their own lengths, carrot or teal jaws.</summary>
		void ClampRail( float u, float length, float v )
		{
			Hang( u, length, v );
			Kit.Box( P( Paint.Handle ), At( new Vector3( u, Face - 1f, v ) ), new Vector3( length, 2f, 2.4f ), Run );
			var count = Math.Max( 3, (int)(length / 7f) );
			for ( var k = 0; k < count; k++ )
			{
				var x = u - length * 0.5f + length * (k + 0.5f) / count + Range( -0.8f, 0.8f );
				var bar = Range( 14f, 26f );
				var jaw = Any( Paint.Carrot, Paint.Teal );
				Kit.Box( P( Paint.Bright ), At( new Vector3( x, Face - 2.6f, v - bar * 0.5f ) ), new Vector3( 0.9f, 0.5f, bar ), Run );
				Kit.Box( jaw, At( new Vector3( x + 1f, Face - 2.6f, v - 2f ) ), new Vector3( 2.4f, 1.2f, 2.6f ), Run );
				Kit.Box( jaw, At( new Vector3( x + 1f, Face - 2.6f, v - bar + Range( 2f, 6f ) ) ), new Vector3( 2.4f, 1.2f, 2.6f ), Run );
			}
		}

		/// <summary>A wall shelf <paramref name="length"/> long at <paramref name="v"/> on steel brackets: tins, boxes, a coil, a fuel can.</summary>
		void WallShelf( float u, float length, float v )
		{
			Hang( u, length, v + 12f );
			HighShelf( u, length, v );
		}

		/// <summary>
		/// A workbench <paramref name="length"/> long: a worn timber top on a painted steel frame, a shelf under it
		/// with a crate or a box, a vice at one end with its tommy bar, a toolbox, a hammer, a spanner and an oil can
		/// on it, and a pegboard of tools over it.
		/// </summary>
		void Workbench( float u, float length )
		{
			Claim( u, length + 2f );
			const float D = 24f;
			const float Top = 30f;
			var y = _back - D * 0.5f - 0.5f;
			var frame = Any( Paint.KhakiPaint, Paint.Machine, Paint.Olive );
			Kit.BevelBox( TrimSkin( TimberSwatch ), At( new Vector3( u, y, Top - 1.5f ) ), new Vector3( length, D, 3f ), 0.4f, Run );
			foreach ( var sx in new[] { -1f, 1f } )
			foreach ( var sy in new[] { -1f, 1f } )
				Kit.Box( frame, At( new Vector3( u + sx * (length * 0.5f - 2f), y + sy * (D * 0.5f - 2f), (Ledge + Top - 3f) * 0.5f ) ), new Vector3( 2.2f, 2.2f, Top - 3f - Ledge ), Run );
			Kit.Box( frame, At( new Vector3( u, y + D * 0.5f - 2f, Top - 5f ) ), new Vector3( length - 4f, 1.6f, 3f ), Run );
			Kit.Box( TrimSkin( TimberSwatch ), At( new Vector3( u, y, Ledge + 6f ) ), new Vector3( length - 4f, D - 4f, 1.2f ), Run );
			if ( Next() < 0.5f ) Crate( new Vector3( u + Range( -0.25f, 0.25f ) * length, y, Ledge + 6.6f ), Range( 14f, 17f ), Range( -6f, 6f ) );
			else Toolbox( new Vector3( u + Range( -0.25f, 0.25f ) * length, y, Ledge + 6.6f ), Range( -8f, 8f ) );

			var end = Next() < 0.5f ? -1f : 1f;
			var vx = u + end * (length * 0.5f - 6f);
			var front = y - D * 0.5f;
			var body = Any( Paint.Teal, Paint.Machine, Paint.Olive );
			Kit.BevelBox( body, At( new Vector3( vx, front + 3.5f, Top + 3f ) ), new Vector3( 7f, 9f, 6f ), 0.7f, Run );
			Kit.BevelBox( body, At( new Vector3( vx, front - 1f, Top + 2.6f ) ), new Vector3( 7f, 2f, 5.2f ), 0.5f, Run );
			foreach ( var j in new[] { front - 0.2f, front + 0.6f } )
				Kit.Box( P( Paint.Bright ), At( new Vector3( vx, j, Top + 5.4f ) ), new Vector3( 7.4f, 0.5f, 1.6f ), Run );
			Kit.Tube( P( Paint.Bright ), At( new[] { new Vector3( vx, front - 2f, Top + 2.4f ), new Vector3( vx, front - 3.2f, Top + 2.4f ) } ), 0.6f, 5 );
			var bar = Range( -4f, 4f );
			Kit.Tube( P( Paint.Bright ), At( new[] { new Vector3( vx - 5f, front - 3.4f, Top + 2.4f + bar * 0.2f ), new Vector3( vx + 5f, front - 3.4f, Top + 2.4f - bar * 0.2f ) } ), 0.35f, 4 );

			Toolbox( new Vector3( u - end * (length * 0.5f - 10f), y + Range( 1f, 4f ), Top ), Range( -12f, 12f ) );
			Hammer( new Vector3( u + Range( -0.15f, 0.15f ) * length, y - Range( 2f, 6f ), Top ), Range( -40f, 40f ) );
			if ( Next() < 0.6f ) Spanner( new Vector3( u + Range( -0.1f, 0.25f ) * length * -end, y - Range( 0f, 4f ), Top ), Range( -50f, 50f ) );
			OilCan( new Vector3( u + end * Range( 0.05f, 0.2f ) * length, y + Range( 3f, 7f ), Top ) );

			var width = MathF.Min( length - 6f, Range( 50f, 72f ) );
			Pegboard( u + Range( -5f, 5f ), width, Range( 40f, 46f ), width * Range( 0.48f, 0.58f ) );
		}

		/// <summary>A big pegboard over a tool chest: a head high, full of tools.</summary>
		void BigBoard( float u )
		{
			var width = Range( 56f, 64f );
			Pegboard( u, width, Range( 46f, 52f ), width * Range( 0.85f, 1f ) );
		}

		/// <summary>A pegboard <paramref name="width"/> by <paramref name="height"/> on battens, its foot at <paramref name="v0"/>, and tools hung on it in a row or two.</summary>
		void Pegboard( float u, float width, float v0, float height )
		{
			const float BoardY = Face - 1.45f;
			var top = v0 + height;
			Hang( u, width + 4f, top );
			Room.WallPicture( Side, _trim, PegboardSwatch( Pick( 2 ) ), u - width * 0.5f, u + width * 0.5f, v0, top, BoardY, false );
			Kit.Box( P( Paint.Handle ), At( new Vector3( u, (BoardY + 0.05f + Face + 1f) * 0.5f, (v0 + top) * 0.5f ) ), new Vector3( width, Face + 0.95f - BoardY, height ), Run );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.Box( P( Paint.Handle ), At( new Vector3( u, BoardY - 0.5f, (v0 + top) * 0.5f + s * (height * 0.5f + 0.8f) ) ), new Vector3( width + 3.2f, 1.6f, 1.6f ), Run );
				Kit.Box( P( Paint.Handle ), At( new Vector3( u + s * (width * 0.5f + 0.8f), BoardY - 0.5f, (v0 + top) * 0.5f ) ), new Vector3( 1.6f, 1.6f, height ), Run );
			}

			var rows = height > 40f ? 2 : 1;
			var y = BoardY - 1.4f;
			for ( var row = 0; row < rows; row++ )
			{
				var hook = top - 4f - row * (height * 0.5f);
				var reach = (rows == 2 ? height * 0.5f : height) - 6f;
				var at = u - width * 0.5f + Range( 3f, 6f );
				while ( at < u + width * 0.5f - 5f )
				{
					var used = Tool( Pick( 8 ), at, hook, y, reach );
					at += used + Range( 7f, 12f );
				}
			}
		}

		/// <summary>One tool hung on the pegboard from its hook at (<paramref name="u"/>, <paramref name="hook"/>), no longer than <paramref name="reach"/>; returns how wide it is.</summary>
		float Tool( int kind, float u, float hook, float y, float reach )
		{
			var steel = P( Paint.Bright );
			var grip = Any( Paint.Carrot, Paint.Teal, Paint.Handle, Paint.Black );
			switch ( kind )
			{
				case 0:
				{
					var length = MathF.Min( reach, Range( 10f, 16f ) );
					Kit.Box( steel, At( new Vector3( u + 1.5f, y, hook - length * 0.5f ) ), new Vector3( 1f, 0.35f, length ), Run );
					Kit.Tube( steel, At( Loop( new Vector3( u + 1.5f, y, hook ), Vector3.Forward, Vector3.Up, 1.1f, 4 ) ), 0.3f, 3 );
					foreach ( var s in new[] { -1f, 1f } ) Kit.Box( steel, At( new Vector3( u + 1.5f + s * 0.9f, y, hook - length - 0.8f ) ), new Vector3( 0.6f, 0.4f, 2f ), Run );
					return 3f;
				}
				case 1:
				{
					var length = MathF.Min( reach, Range( 10f, 13f ) );
					Kit.Box( P( Paint.Handle ), At( new Vector3( u + 2.3f, y, hook - length * 0.5f ) ), new Vector3( 0.9f, 0.8f, length ), Run );
					Kit.Box( P( Paint.Iron ), At( new Vector3( u + 2.3f, y, hook - 0.4f ) ), new Vector3( 4.6f, 1.4f, 1.5f ), Run );
					return 4.6f;
				}
				case 2:
				{
					Kit.SmoothLathe( grip, At( new Vector3( u + 1f, y, hook - 4.5f ) ), new[] { new Vector2( 0.5f, 0f ), new Vector2( 0.85f, 0.6f ), new Vector2( 0.8f, 4.2f ), new Vector2( 0.5f, 4.5f ) }, 6, Run, false, true );
					Kit.Tube( steel, At( new[] { new Vector3( u + 1f, y, hook - 4.5f ), new Vector3( u + 1f, y, hook - 4.5f - MathF.Min( reach - 5f, Range( 4f, 7f ) ) ) } ), 0.2f, 3 );
					return 2f;
				}
				case 3:
				{
					var length = MathF.Min( reach - 4f, Range( 14f, 18f ) );
					Kit.Box( P( Paint.Handle ), At( new Vector3( u + 2f, y, hook - 2f ) ), new Vector3( 3.6f, 0.9f, 4f ), Run );
					Kit.Box( steel, At( new Vector3( u + 2f, y, hook - 4f - length * 0.5f ) ), new Vector3( 4f, 0.15f, length ), Run );
					return 4.2f;
				}
				case 4:
				{
					foreach ( var s in new[] { -1f, 1f } )
					{
						Kit.Box( steel, At( new Vector3( u + 1.8f, y - s * 0.15f, hook - 3f ) ), new Vector3( 0.5f, 0.3f, 6f ), Turn( Spin( s * 9f ) ) );
						Kit.Box( grip, At( new Vector3( u + 1.8f + s * 0.9f, y - s * 0.15f, hook - 6.5f ) ), new Vector3( 0.7f, 0.5f, 4f ), Turn( Spin( s * 9f ) ) );
					}
					return 3.6f;
				}
				case 5:
				{
					var paint = Any( Paint.Carrot, Paint.Teal, Paint.Black );
					Kit.Tube( paint, At( Loop( new Vector3( u + 4f, y - 0.6f, hook - 4f ), Vector3.Forward, Vector3.Up, 3.6f, 8 ) ), 0.7f, 3 );
					return 8.2f;
				}
				case 6:
				{
					var length = MathF.Min( reach, Range( 10f, 13f ) );
					Kit.Box( steel, At( new Vector3( u + 0.5f, y, hook - length * 0.5f ) ), new Vector3( 0.8f, 0.2f, length ), Run );
					Kit.Box( P( Paint.Handle ), At( new Vector3( u + 3.3f, y, hook - length + 0.6f ) ), new Vector3( 6f, 0.5f, 1.2f ), Run );
					return 6.4f;
				}
				default:
				{
					var paint = Any( Paint.Carrot, Paint.Teal );
					Kit.Box( paint, At( new Vector3( u + 0.6f, y, hook - 4f ) ), new Vector3( 1f, 0.6f, 8f ), Run );
					foreach ( var s in new[] { -1f, 1f } ) Kit.Box( paint, At( new Vector3( u + 2.6f, y, hook - 4f + s * 3.5f ) ), new Vector3( 4f, 0.6f, 1f ), Run );
					Kit.Tube( steel, At( new[] { new Vector3( u + 4f, y, hook - 7.5f ), new Vector3( u + 4f, y, hook - 2f ) } ), 0.25f, 3 );
					return 4.6f;
				}
			}
		}

		/// <summary>A toolbox: a steel box in carrot, teal or khaki, a ridged lid, a handle.</summary>
		void Toolbox( Vector3 foot, float yaw )
		{
			var turn = Rotation.FromYaw( yaw );
			var paint = Any( Paint.Carrot, Paint.Teal, Paint.KhakiPaint, Paint.Machine );
			Kit.Box( paint, At( foot + new Vector3( 0f, 0f, 3f ) ), new Vector3( 15f, 7f, 6f ), Turn( turn ) );
			Kit.Box( paint, At( foot + new Vector3( 0f, 0f, 6.6f ) ), new Vector3( 15.4f, 7.2f, 1.2f ), Turn( turn ) );
			Kit.Tube( P( Paint.Black ), At( new[] { foot + turn * new Vector3( -3f, 0f, 7f ), foot + turn * new Vector3( -2.4f, 0f, 9f ), foot + turn * new Vector3( 2.4f, 0f, 9f ), foot + turn * new Vector3( 3f, 0f, 7f ) } ), 0.35f, 4 );
		}

		void Hammer( Vector3 foot, float yaw )
		{
			var turn = Rotation.FromYaw( yaw );
			Kit.Box( P( Paint.Handle ), At( foot + turn * new Vector3( 0f, 0f, 0.5f ) ), new Vector3( 11f, 1f, 0.9f ), Turn( turn ) );
			Kit.Box( P( Paint.Iron ), At( foot + turn * new Vector3( 5.6f, 0f, 0.8f ) ), new Vector3( 1.5f, 4.6f, 1.5f ), Turn( turn ) );
		}

		void Spanner( Vector3 foot, float yaw )
		{
			var turn = Rotation.FromYaw( yaw );
			Kit.Box( P( Paint.Bright ), At( foot + new Vector3( 0f, 0f, 0.2f ) ), new Vector3( 12f, 1.2f, 0.35f ), Turn( turn ) );
			Kit.Tube( P( Paint.Bright ), At( Loop( foot + turn * new Vector3( 6.6f, 0f, 0.25f ), turn * Vector3.Forward, turn * Across, 1.2f, 6 ) ), 0.3f, 3 );
		}

		void OilCan( Vector3 foot )
		{
			var paint = Any( Paint.Carrot, Paint.Olive, Paint.Bright );
			Kit.SmoothLathe( paint, At( foot ), new[] { new Vector2( 2.4f, 0f ), new Vector2( 2.6f, 0.6f ), new Vector2( 2.6f, 3f ), new Vector2( 1f, 4.6f ), new Vector2( 0.5f, 5f ) }, 10, Run, false, true, 45f );
			var dir = Flat( Next() * MathF.Tau );
			Kit.Tube( P( Paint.BrassPaint ), At( new[] { foot + new Vector3( 0f, 0f, 4.6f ), foot + dir * 3f + new Vector3( 0f, 0f, 8f ) } ), 0.35f, 4, 0.15f );
		}

		/// <summary>A wooden crate, <paramref name="size"/> across, battened at its corners, turned <paramref name="yaw"/> degrees.</summary>
		void Crate( Vector3 foot, float size, float yaw )
		{
			var turn = Rotation.FromYaw( yaw );
			var height = size * Range( 0.75f, 0.95f );
			Kit.Box( TrimSkin( CrateSwatch ), At( foot + new Vector3( 0f, 0f, height * 0.5f ) ), new Vector3( size, size * 0.85f, height ), Turn( turn ) );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( P( Paint.Handle ), At( foot + turn * new Vector3( s * (size * 0.5f - 1.2f), -size * 0.425f - 0.3f, height * 0.5f ) ), new Vector3( 2.2f, 0.6f, height - 0.4f ), Turn( turn ) );
		}

		/// <summary>Two or three crates stacked a head high, each a little askew, the top one smaller.</summary>
		void CrateStack( float u )
		{
			Claim( u, 28f );
			var y = _back - 12.5f;
			var v = Ledge;
			var size = Range( 22f, 25f );
			for ( var k = 2 + Pick( 2 ); k > 0; k-- )
			{
				var yaw = Range( -7f, 7f );
				var depth = MathF.Min( y, _back - size * 0.425f - 0.5f );
				depth = MathF.Max( depth, Front + 2f + size * 0.5f );
				Crate( new Vector3( u + Range( -1.5f, 1.5f ), depth, v ), size, yaw );
				v += size * 0.85f;
				size *= Range( 0.78f, 0.92f );
			}
			_tall |= v > 48f;
			if ( v > 48f ) Hang( u, 28f );
		}

		/// <summary>An oil drum: upright with rolling hoops and a bung, or lying on two chocks; a funnel on some.</summary>
		void Drum( float u, bool lying )
		{
			const float R = 10.6f;
			const float H = 34f;
			var paint = Any( Paint.Teal, Paint.Olive, Paint.Carrot, Paint.KhakiPaint, Paint.RustPaint, Paint.Machine );
			var y = _back - R - 0.5f;
			var profile = new[]
			{
				new Vector2( R - 0.3f, 0f ), new Vector2( R - 0.2f, 10.8f ), new Vector2( R + 0.3f, 11.4f ), new Vector2( R - 0.2f, 12f ),
				new Vector2( R - 0.2f, 22f ), new Vector2( R + 0.3f, 22.6f ), new Vector2( R - 0.2f, 23.2f ), new Vector2( R - 0.3f, H ),
			};
			if ( lying )
			{
				Claim( u, 38f );
				foreach ( var s in new[] { -1f, 1f } )
					Kit.Box( TrimSkin( TimberSwatch ), At( new Vector3( u + s * 11f, y, Ledge + 1.2f ) ), new Vector3( 4f, R * 1.6f, 2.4f ), Run );
				Kit.SmoothLathe( paint, At( new Vector3( u - H * 0.5f, y, Ledge + R + 1.6f ) ), profile, 8, Lying, true, true, 50f, Next() );
				return;
			}
			Claim( u, 24f );
			Kit.SmoothLathe( paint, At( new Vector3( u, y, Ledge ) ), profile, 8, Run, false, true, 50f, Next() );
			Kit.SmoothLathe( P( Paint.Iron ), At( new Vector3( u + Range( -5f, 5f ), y + Range( -4f, 2f ), Ledge + H ) ), new[] { new Vector2( 1.4f, 0f ), new Vector2( 1.4f, 0.6f ) }, 6, Run, false, true );
			if ( Next() < 0.35f )
				Kit.SmoothLathe( P( Paint.Galvanised ), At( new Vector3( u + Range( -3f, 3f ), y, Ledge + H ) ), new[] { new Vector2( 0.6f, 0f ), new Vector2( 0.6f, 3f ), new Vector2( 4.6f, 7f ), new Vector2( 4.8f, 8f ) }, 8, Run, false, false );
		}

		/// <summary>A rolling tool chest: a cabinet of drawers on castors, a top chest with its lid, carrot, teal or blue-grey.</summary>
		void ToolChest( float u )
		{
			Claim( u, 30f );
			const float D = 18f;
			var y = _back - D * 0.5f - 0.5f;
			var paint = Any( Paint.Carrot, Paint.Teal, Paint.Machine );
			var front = y - D * 0.5f;
			Kit.BevelBox( paint, At( new Vector3( u, y, Ledge + 4f + 15f ) ), new Vector3( 28f, D, 30f ), 0.8f, Run );
			for ( var k = 0; k < 5; k++ )
			{
				var z = Ledge + 4f + 3f + k * 5.6f;
				Kit.Box( P( Paint.Bright ), At( new Vector3( u, front - 0.4f, z + 3.6f ) ), new Vector3( 18f, 0.8f, 0.8f ), Run );
			}
			foreach ( var sx in new[] { -1f, 1f } )
			foreach ( var sy in new[] { -1f, 1f } )
				Kit.Ellipsoid( P( Paint.Rubber ), At( new Vector3( u + sx * 11f, y + sy * 6f, Ledge + 1.8f ) ), new Vector3( 1.8f, 1.2f, 1.8f ), Run, 3, 6 );
			Kit.BevelBox( paint, At( new Vector3( u, y + 1f, Ledge + 34f + 4f ) ), new Vector3( 26f, D - 3f, 8f ), 0.8f, Run );
			Kit.BevelBox( paint, At( new Vector3( u, y + 1f, Ledge + 42.6f ) ), new Vector3( 26.4f, D - 2.6f, 1.6f ), 0.5f, Run );
			Kit.Box( P( Paint.Bright ), At( new Vector3( u, front + 0.8f, Ledge + 40f ) ), new Vector3( 10f, 0.8f, 0.8f ), Run );
		}

		/// <summary>Steel shelving <paramref name="width"/> wide, a head high: four shelves of parts bins, boxes, tins and coils.</summary>
		void Rack( float u, float width )
		{
			Claim( u, width + 2f );
			Hang( u, width + 2f );
			_tall = true;
			const float D = 16f;
			const float H = 84f;
			var y = _back - D * 0.5f - 0.5f;
			var steel = Any( Paint.Steel, Paint.Machine, Paint.KhakiPaint );
			foreach ( var sx in new[] { -1f, 1f } )
			foreach ( var sy in new[] { -1f, 1f } )
				Kit.Box( steel, At( new Vector3( u + sx * (width * 0.5f - 0.8f), y + sy * (D * 0.5f - 0.8f), Ledge + H * 0.5f ) ), new Vector3( 1.6f, 1.6f, H ), Run );
			for ( var k = 0; k < 4; k++ )
			{
				var v = Ledge + 4f + k * 25f;
				Kit.Box( steel, At( new Vector3( u, y, v - 0.4f ) ), new Vector3( width - 1f, D, 0.8f ), Run );
				var at = u - width * 0.5f + 2f;
				while ( at < u + width * 0.5f - 4f )
				{
					var room = u + width * 0.5f - 2f - at;
					var roll = Next();
					if ( roll < 0.4f && room > 14f )
					{
						var w = MathF.Min( room, Range( 14f, 24f ) );
						var bins = (int)(w / 7f);
						Kit.Box( Any( Paint.Teal, Paint.Carrot, Paint.Olive, Paint.Machine, Paint.Navy ), At( new Vector3( at + w * 0.5f, y, v + 2.6f ) ), new Vector3( w - 0.4f, 11f, 5.2f ), Run );
						for ( var b = 1; b < bins; b++ ) Flat( P( Paint.Iron ), at + w * b / bins, 0.6f, v, v + 5.2f, y - 5.55f );
						at += w + 0.6f;
					}
					else if ( roll < 0.65f && room > 12f )
					{
						var w = MathF.Min( room, Range( 10f, 16f ) );
						var h = Range( 8f, 14f );
						Kit.Box( P( Paint.Cardboard ), At( new Vector3( at + w * 0.5f, y + Range( -1f, 1f ), v + h * 0.5f ) ), new Vector3( w - 0.6f, 12f, h ), Turn( Rotation.FromYaw( Range( -4f, 4f ) ) ) );
						at += w + 0.5f;
					}
					else if ( roll < 0.78f && room > 6f )
					{
						Kit.SmoothLathe( Any( Paint.Carrot, Paint.Teal, Paint.Cream, Paint.Olive ), At( new Vector3( at + 2.8f, y - Range( 0f, 3f ), v ) ), new[] { new Vector2( 2.6f, 0f ), new Vector2( 2.6f, 4.8f ) }, 6, Run, false, true );
						at += 6f;
					}
					else at += Range( 2f, 5f );
				}
			}
			Kit.Box( steel, At( new Vector3( u, y, Ledge + H - 0.4f ) ), new Vector3( width - 1f, D, 0.8f ), Run );
		}

		/// <summary>Two or three gas cylinders chained to the wall, each its own colour with a band at its shoulder, a valve and a cap.</summary>
		void Cylinders( float u )
		{
			Claim( u, 36f );
			Hang( u, 36f );
			_tall = true;
			var count = 2 + Pick( 2 );
			var y = _back - 5.5f;
			var span = (count - 1) * 10f;
			for ( var k = 0; k < count; k++ )
			{
				var x = u - span * 0.5f + k * 10f + Range( -0.6f, 0.6f );
				var height = Range( 48f, 56f );
				var paint = Any( Paint.Maroon, Paint.CylinderBlue, Paint.Black, Paint.Olive, Paint.Teal );
				Kit.SmoothLathe( paint, At( new Vector3( x, y, Ledge ) ), new[]
				{
					new Vector2( 4.2f, 0f ), new Vector2( 4.7f, 0.8f ), new Vector2( 4.7f, height - 6f ), new Vector2( 4f, height - 2.6f ), new Vector2( 2.2f, height - 0.8f ), new Vector2( 1.4f, height ),
				}, 8, Run, true, false, 40f, Next() );
				Kit.SmoothLathe( Any( Paint.Cream, Paint.Carrot, Paint.Chalk ), At( new Vector3( x, y, Ledge + height - 9f ) ), new[] { new Vector2( 4.75f, 0f ), new Vector2( 4.75f, 2.4f ) }, 8, Run, false, false );
				Kit.SmoothLathe( P( Paint.BrassPaint ), At( new Vector3( x, y, Ledge + height ) ), new[] { new Vector2( 1f, 0f ), new Vector2( 1f, 2.2f ), new Vector2( 1.6f, 2.6f ), new Vector2( 1.6f, 3.4f ), new Vector2( 0f, 3.6f ) }, 6, Run, false, false );
				Kit.Box( P( Paint.BrassPaint ), At( new Vector3( x + 1.6f, y, Ledge + height + 1.4f ) ), new Vector3( 2.4f, 0.8f, 0.8f ), Run );
			}
			var chain = Ledge + Range( 34f, 40f );
			var left = u - span * 0.5f - 6f;
			var right = u + span * 0.5f + 6f;
			Kit.Tube( P( Paint.Iron ), At( new[]
			{
				new Vector3( left, Face - 0.5f, chain ), new Vector3( left + 2f, y - 4.6f, chain ), new Vector3( right - 2f, y - 4.6f, chain - 1f ), new Vector3( right, Face - 0.5f, chain ),
			} ), 0.4f, 4 );
			foreach ( var x in new[] { left, right } )
				Kit.Box( P( Paint.Iron ), At( new Vector3( x, Face - 0.8f, chain ) ), new Vector3( 2f, 1.6f, 4f ), Run );
		}

		/// <summary>A hose reel on the wall at <paramref name="v"/>, <paramref name="scale"/> times life size: a back plate, a drum, two turns of hose and its end hanging; standing over a gap on the floor when <paramref name="claims"/>.</summary>
		void HoseReel( float u, float v, float scale, bool claims )
		{
			if ( claims ) Claim( u, 24f );
			Hang( u, 20f * scale, v + 9f * scale );
			Kit.Box( P( Paint.Steel ), At( new Vector3( u, Face - 0.5f, v ) ), new Vector3( 10f * scale, 1.2f, 14f * scale ), Run );
			var drum = new[] { new Vector2( 8.5f, 0f ), new Vector2( 8.5f, 0.6f ), new Vector2( 5f, 0.6f ), new Vector2( 5f, 6.4f ), new Vector2( 8.5f, 6.4f ), new Vector2( 8.5f, 7f ) };
			Kit.SmoothLathe( Any( Paint.Carrot, Paint.Teal, Paint.CylinderBlue, Paint.Steel ), At( new Vector3( u, Face - 1f, v ) ), Array.ConvertAll( drum, p => p * scale ), 10, Facing, false, true, 30f );
			var hose = Any( Paint.GreenEnamel, Paint.Carrot, Paint.CylinderBlue, Paint.Black );
			var path = new List<Vector3>();
			for ( var i = 0; i <= 16; i++ )
			{
				var t = i / 8f * MathF.Tau;
				var r = (6.1f + (i / 8) * 0.9f) * scale;
				path.Add( new Vector3( u + MathF.Cos( t ) * r, Face - (1.8f + i * 0.2f) * scale, v + MathF.Sin( t ) * r ) );
			}
			var tail = path[^1];
			path.Add( tail + new Vector3( 1f, -0.4f, -6f ) * scale );
			path.Add( tail + new Vector3( 1.4f, -0.6f, -16f ) * scale );
			Kit.Tube( hose, At( path.ToArray() ), 0.85f * scale, 4 );
			Kit.SmoothLathe( P( Paint.BrassPaint ), At( tail + new Vector3( 1.4f, -0.6f, -19f ) * scale ), new[] { new Vector2( 0.5f, 0f ), new Vector2( 1f, 2.4f ), new Vector2( 1f, 3f ) }, 6, Run, true, false );
			if ( claims ) _tall = true;
		}

		/// <summary>
		/// The electrical corner: a distribution board with its window and a warning sign, an isolator with its lever
		/// beside it, conduits from both up the wall to the girder and one down to the floor.
		/// </summary>
		void Panel( float u )
		{
			Claim( u, 44f );
			Hang( u, 46f );
			_tall = true;
			var board = u + Range( -8f, 0f );
			var bv = Range( 70f, 76f );
			Kit.BevelBox( P( Paint.Steel ), At( new Vector3( board, Face - 3.5f, bv + 15f ) ), new Vector3( 22f, 7f, 30f ), 0.6f, Run );
			Room.WallPicture( Side, _trim, BoardFrontSwatch, board - 10f, board + 10f, bv + 1f, bv + 29f, Face - 7.05f, false );
			Kit.Box( P( Paint.Black ), At( new Vector3( board + 9f, Face - 7.4f, bv + 15f ) ), new Vector3( 0.8f, 0.8f, 4f ), Run );
			var isolator = board + 18f;
			Kit.BevelBox( P( Paint.Machine ), At( new Vector3( isolator, Face - 3f, bv + 7f ) ), new Vector3( 10f, 6f, 14f ), 0.6f, Run );
			Kit.Box( P( Paint.Carrot ), At( new Vector3( isolator + 5.6f, Face - 3f, bv + 9f ) ), new Vector3( 1.2f, 1.4f, 6f ), Turn( Spin( Next() < 0.5f ? 30f : -30f ) ) );
			var conduit = P( Paint.Galvanised );
			foreach ( var (x, from) in new[] { (board - 6f, bv + 30f), (board + 5f, bv + 30f), (isolator, bv + 14f) } )
			{
				Kit.Tube( conduit, At( new[] { new Vector3( x, Face - 1.2f, from ), new Vector3( x, Face - 1.2f, Girder + 1f ) } ), 0.9f, 6 );
				for ( var c = from + Range( 20f, 30f ); c < Girder - 6f; c += Range( 38f, 50f ) )
					Kit.Box( P( Paint.Iron ), At( new Vector3( x, Face - 1f, c ) ), new Vector3( 2.8f, 1.6f, 1f ), Run );
			}
			Kit.Tube( conduit, At( new[] { new Vector3( board, Face - 1.2f, bv ), new Vector3( board, Face - 1.2f, Ledge + 10f ) } ), 0.9f, 6 );
			Head( board + 4f, 30f );
			_upperTaken.Add( (board - 9f, isolator + 3f) );
			Notice( isolator + Range( -1f, 1f ), bv + Range( 22f, 28f ), Pick( 3 ) );
		}

		/// <summary>A notice pinned to the corrugation, a little askew, a pin at its head.</summary>
		void Notice( float u, float v, int kind )
		{
			Hang( u, 13f );
			var size = Range( 10f, 12f );
			var tilt = Turn( Spin( Range( -5f, 5f ) ) );
			Kit.Box( TrimSkin( NoticeSwatch( kind ) ), At( new Vector3( u, Face - 0.4f, v ) ), new Vector3( size, 0.15f, size * 1.3f ), tilt );
			Kit.Ellipsoid( P( Paint.Carrot ), At( new Vector3( u, Face - 0.8f, v + size * 0.55f ) ), new Vector3( 0.5f, 0.5f, 0.5f ), Run, 2, 4 );
		}

		/// <summary>A cable drum lying on the floor: two wooden flanges and the black cable wound between them.</summary>
		void CableDrum( float u )
		{
			Claim( u, 20f );
			const float R = 9f;
			var y = _back - R - 0.5f;
			Kit.SmoothLathe( TrimSkin( CrateSwatch ), At( new Vector3( u - 7f, y, Ledge + R ) ), new[]
			{
				new Vector2( R, 0f ), new Vector2( R, 1.4f ), new Vector2( 6.2f, 1.4f ), new Vector2( 6.2f, 12.6f ), new Vector2( R, 12.6f ), new Vector2( R, 14f ),
			}, 14, Lying, true, true, 30f );
			Kit.SmoothLathe( P( Paint.Black ), At( new Vector3( u - 5.6f, y, Ledge + R ) ), new[] { new Vector2( 7.2f, 0f ), new Vector2( 7.4f, 5.6f ), new Vector2( 7.2f, 11.2f ) }, 14, Lying, false, false, 60f );
		}

		/// <summary>A pedestal grinder: a base plate, a column, the motor lying across with a wheel and its guard at each end; two notices over it.</summary>
		void Grinder( float u )
		{
			Claim( u, 20f );
			_tall = true;
			var y = _back - 9f;
			var paint = Any( Paint.Machine, Paint.Teal, Paint.GreenEnamel );
			Kit.BevelBox( paint, At( new Vector3( u, y, Ledge + 0.6f ) ), new Vector3( 14f, 12f, 1.2f ), 0.3f, Run );
			Kit.SmoothLathe( paint, At( new Vector3( u, y, Ledge + 1.2f ) ), new[] { new Vector2( 3.2f, 0f ), new Vector2( 2.2f, 2f ), new Vector2( 1.8f, 28f ), new Vector2( 3f, 30f ) }, 8, Run, false, true, 40f );
			var axle = Ledge + 35f;
			Kit.SmoothLathe( paint, At( new Vector3( u - 6f, y, axle ) ), new[] { new Vector2( 3f, 0f ), new Vector2( 4.4f, 1.6f ), new Vector2( 4.4f, 10.4f ), new Vector2( 3f, 12f ) }, 12, Lying, true, true, 40f );
			foreach ( var s in new[] { -1f, 1f } )
			{
				var x = u + s * 8.5f;
				Kit.SmoothLathe( P( Paint.Concrete ), At( new Vector3( x - 1f, y, axle ) ), new[] { new Vector2( 5f, 0f ), new Vector2( 5f, 2f ) }, 14, Lying, true, true );
				Kit.BevelBox( paint, At( new Vector3( x, y + 0.5f, axle + 2.4f ) ), new Vector3( 3f, 11.5f, 6f ), 0.6f, Run );
				Kit.Box( P( Paint.Chalk ), At( new Vector3( x, y - 6.5f, axle + 6.4f ) ), new Vector3( 3.4f, 0.2f, 3f ), Turn( Rotation.FromRoll( 20f ) ) );
			}
			Notice( u + Range( -8f, -3f ), Range( 74f, 84f ), Pick( 3 ) );
			Notice( u + Range( 3f, 8f ), Range( 70f, 90f ), Pick( 3 ) );
		}

		/// <summary>A shop stool: a round seat on three splayed legs with a foot ring.</summary>
		void Stool( float u )
		{
			Claim( u, 16f );
			var y = _back - 9.5f;
			var seat = new Vector3( u, y, Ledge + 26f );
			Kit.SmoothLathe( Any( Paint.Teal, Paint.Black, Paint.Machine ), At( seat ), new[] { new Vector2( 5.6f, 0f ), new Vector2( 6f, 0.8f ), new Vector2( 5.6f, 1.8f ) }, 12, Run, true, true, 50f );
			var spin = Next() * MathF.Tau;
			for ( var k = 0; k < 3; k++ )
			{
				var a = spin + k * MathF.Tau / 3f;
				var dir = new Vector3( MathF.Cos( a ), MathF.Sin( a ), 0f );
				var foot = new Vector3( u, y, Ledge ) + dir * 7f;
				if ( foot.y < Front + 1f ) foot = foot.WithY( Front + 1f );
				Kit.Tube( P( Paint.Steel ), At( new[] { seat + dir * 3.6f, foot } ), 0.5f, 4 );
			}
			Kit.Tube( P( Paint.Steel ), At( Loop( new Vector3( u, y, Ledge + 9f ), Vector3.Forward, Across, 5.2f, 12 ) ), 0.35f, 4 );
		}

		/// <summary>A folded stepladder leaning on the wall: two rails and flat treads, aluminium or timber.</summary>
		void StepLadder( float u )
		{
			Claim( u, 24f );
			Hang( u, 24f );
			_tall = true;
			var foot = new Vector3( u, 7f, Ledge );
			var top = new Vector3( u, Face - 3f, Ledge + Range( 74f, 82f ) );
			var length = (top - foot).Length;
			var axis = (top - foot) / length;
			var turn = Turn( Rotation.LookAt( Vector3.Forward, axis ) );
			var skin = Any( Paint.Bright, Paint.Galvanised, Paint.Handle );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( skin, At( foot + axis * (length * 0.5f) + new Vector3( s * 8f, 0f, 0f ) ), new Vector3( 1.4f, 3f, length ), turn );
			for ( var d = 10f; d < length - 6f; d += 11f )
				Kit.Box( skin, At( foot + axis * d + new Vector3( 0f, -0.4f, 0f ) ), new Vector3( 15f, 4f, 0.8f ), turn );
			Kit.BevelBox( skin, At( top + new Vector3( 0f, -1f, -2f ) ), new Vector3( 18f, 4f, 3f ), 0.4f, turn );
		}

		/// <summary>A sack truck leaning on the wall: two handles, a frame of tubes, a nose plate and two wheels.</summary>
		void SackTruck( float u )
		{
			Claim( u, 20f );
			Hang( u, 20f );
			_tall = true;
			var steel = Any( Paint.Carrot, Paint.Teal, Paint.Steel );
			var foot = new Vector3( u, 10f, Ledge + 4.5f );
			var top = new Vector3( u, Face - 4f, Ledge + 52f );
			foreach ( var s in new[] { -1f, 1f } )
			{
				var side = new Vector3( s * 6f, 0f, 0f );
				Kit.Tube( steel, At( new[] { foot + side, top + side, top + side * 0.7f + new Vector3( 0f, -3f, 4f ) } ), 0.6f, 5 );
				Kit.SmoothLathe( P( Paint.Rubber ), At( new Vector3( u + s * 9.5f - 1f, foot.y + 2f, Ledge + 4.5f ) ), new[] { new Vector2( 4.5f, 0f ), new Vector2( 4.5f, 2f ) }, 12, Lying, true, true );
			}
			for ( var k = 1; k <= 3; k++ )
				Kit.Tube( steel, At( new[] { Vector3.Lerp( foot, top, k / 4f ) - new Vector3( 6f, 0f, 0f ), Vector3.Lerp( foot, top, k / 4f ) + new Vector3( 6f, 0f, 0f ) } ), 0.45f, 4 );
			Kit.Box( steel, At( new Vector3( u, foot.y - 3.5f, Ledge + 0.6f ) ), new Vector3( 13f, 7f, 1f ), Run );
		}

		/// <summary>A galvanised bucket, a rag hanging over its rim.</summary>
		void Bucket( float u )
		{
			Claim( u, 13f );
			var foot = new Vector3( u, _back - 7.5f, Ledge );
			Kit.SmoothLathe( P( Paint.Galvanised ), At( foot ), new[] { new Vector2( 4.4f, 0f ), new Vector2( 5.8f, 11f ), new Vector2( 6.1f, 11.6f ) }, 12, Run, true, false, 40f );
			Kit.SmoothLathe( P( Paint.OilPaint ), At( foot + new Vector3( 0f, 0f, 9f ) ), new[] { new Vector2( 5.6f, 0f ), new Vector2( 5.6f, 0.1f ) }, 12, Run, false, true );
			var a = Next() * MathF.PI;
			Kit.Tube( P( Paint.Wire ), At( new[] { foot + new Vector3( -6f, 0f, 11f ), foot + new Vector3( -4f, MathF.Cos( a ) * 3f, 16f ), foot + new Vector3( 4f, MathF.Cos( a ) * 3f, 16f ), foot + new Vector3( 6f, 0f, 11f ) } ), 0.2f, 3 );
		}

		/// <summary>A few paint tins, stacked and side by side.</summary>
		void Tins( float u )
		{
			Claim( u, 12f );
			var v = Ledge;
			for ( var k = 1 + Pick( 3 ); k > 0; k-- )
			{
				var r = Range( 3f, 4f );
				var h = r * Range( 1.1f, 1.4f );
				Kit.SmoothLathe( Any( Paint.Carrot, Paint.Teal, Paint.Cream, Paint.Olive, Paint.Bright ), At( new Vector3( u + Range( -1f, 1f ), _back - 6f + Range( -1f, 1f ), v ) ), new[] { new Vector2( r, 0f ), new Vector2( r, h ), new Vector2( r - 0.4f, h + 0.3f ) }, 10, Run, false, true );
				v += h + 0.3f;
			}
		}

		/// <summary>A broom leaning on the wall, its head on the floor.</summary>
		void Broom( float u )
		{
			Claim( u, 12f );
			var foot = new Vector3( u, 6f, Ledge + 2.5f );
			var top = new Vector3( u + Range( -6f, 6f ), Face - 2f, Ledge + 58f );
			Kit.Tube( P( Paint.Handle ), At( new[] { foot, top } ), 0.5f, 4 );
			Kit.BevelBox( P( Paint.Handle ), At( foot ), new Vector3( 12f, 3f, 2f ), 0.3f, Run );
			Kit.Box( P( Paint.Black ), At( foot + new Vector3( 0f, 0f, -1.8f ) ), new Vector3( 11f, 2.6f, 1.8f ), Run );
		}

		// ---- Over the bays -------------------------------------------------------------------------

		/// <summary>
		/// Dresses the corrugation under the pipes over <paramref name="bay"/>'s set: <paramref name="upper"/> toward one
		/// end of the bay, <paramref name="second"/> along the widest stretch it leaves — both clear of the risers, the
		/// drops and the conduits coming up from the set — and a warning sign where a stretch is still bare.
		/// </summary>
		public void Above( Upper upper, Second second, Bay bay ) => Apart( () =>
		{
			foreach ( var x in _risers ) if ( x > bay.From && x < bay.To ) _upperTaken.Add( (x - 7f, x + 7f) );
			foreach ( var x in _drops ) if ( x > bay.From && x < bay.To ) _upperTaken.Add( (x - 9f, x + 9f) );

			(float From, float To) Widest()
			{
				var best = (From: bay.Middle, To: bay.Middle);
				foreach ( var gap in Gaps( _upperTaken, bay.From + 6f, bay.To - 6f ) )
					if ( gap.To - gap.From > best.To - best.From ) best = gap;
				return best;
			}

			var (from, to) = Widest();
			var span = upper switch { Upper.Gears => 84f, Upper.Fan => 48f, Upper.Hoist => 22f, Upper.Blueprints => 96f, Upper.Chalkboard => 82f, _ => 30f };
			var room = to - from;
			var u = room <= span ? (from + to) * 0.5f : Next() < 0.5f ? from + span * 0.5f + Range( 0f, MathF.Min( 12f, room - span ) ) : to - span * 0.5f - Range( 0f, MathF.Min( 12f, room - span ) );
			switch ( upper )
			{
				case Upper.Gears: Gears( u ); break;
				case Upper.Fan: Fan( u, Range( 146f, 158f ) ); break;
				case Upper.Hoist: Hoist( u ); break;
				case Upper.Blueprints: Blueprints( u, Range( 146f, 156f ) ); break;
				case Upper.Chalkboard: Chalkboard( u, Range( 146f, 154f ) ); break;
				default: HoseReel( u, Range( 160f, 166f ), Range( 1.35f, 1.5f ), false ); break;
			}
			_upperTaken.Add( (u - span * 0.5f, u + span * 0.5f) );

			(from, to) = Widest();
			room = to - from - 6f;
			var at = (from + to) * 0.5f;
			var width = 0f;
			switch ( second )
			{
				case Second.Duct when room >= 50f: Duct( at, width = MathF.Min( room, Range( 70f, 150f ) ) ); break;
				case Second.Bars when room >= 54f: Bars( at, width = MathF.Min( room, Range( 70f, 120f ) ) ); break;
				case Second.Ladder when room >= 74f: HungLadder( at, width = MathF.Min( room, Range( 90f, 130f ) ) ); break;
				case Second.Sign when room >= 56f: Signboard( at, width = MathF.Min( room, Range( 72f, 100f ) ) ); break;
				case Second.Shelf when room >= 44f: HighShelf( at, width = MathF.Min( room, Range( 56f, 90f ) ), Range( 150f, 156f ) ); break;
				default:
					if ( room >= 36f ) ControlPanel( at, width = MathF.Min( room, Range( 36f, 46f ) ) );
					break;
			}
			_upperTaken.Add( (at - width * 0.5f - 2f, at + width * 0.5f + 2f) );

			(from, to) = Widest();
			if ( to - from >= 22f && Next() < 0.6f )
			{
				var size = Range( 13f, 16f );
				var x = Range( from + size * 0.5f + 2f, to - size * 0.5f - 2f );
				var v0 = Range( 152f, 166f );
				Room.WallPicture( Side, _trim, SignSwatch, x - size * 0.5f, x + size * 0.5f, v0, v0 + size, Face - 0.3f, false );
			}
		} );

		/// <summary>What the bay being dressed has under its pipes already: the set's conduits, the risers, the drops, the pieces placed.</summary>
		readonly List<(float From, float To)> _upperTaken = new();

		/// <summary>Two or three gears on the wall about <paramref name="u"/>, half as big again as life, meshing tooth into gap, each its own colour.</summary>
		void Gears( float u )
		{
			var r1 = Range( 16f, 21f );
			var a = Range( -0.5f, 0.5f ) + (Next() < 0.5f ? 0f : MathF.PI);
			var r2 = r1 * Range( 0.5f, 0.75f );
			var dir = new Vector3( MathF.Cos( a ), 0f, MathF.Sin( a ) );
			var c1 = new Vector3( u, Face - 0.6f, Range( 178f, 196f ) ) - dir * (r2 + 0.7f);
			Gear( c1, r1, a, Any( Paint.KhakiPaint, Paint.RustPaint, Paint.BrassPaint ) );
			var c2 = c1 + dir * (r1 + r2 + 1.4f);
			Gear( c2, r2, a + MathF.PI + MathF.PI / Teeth( r2 ), Any( Paint.Machine, Paint.Teal, Paint.Steel ) );
			if ( Next() < 0.6f )
			{
				var r3 = r1 * Range( 0.35f, 0.5f );
				var b = a + (Next() < 0.5f ? 1.6f : -1.6f);
				var c3 = c1 + new Vector3( MathF.Cos( b ), 0f, MathF.Sin( b ) ) * (r1 + r3 + 1.4f);
				if ( c3.z - r3 > 146f && c3.z + r3 < Girder - 4f ) Gear( c3, r3, b + MathF.PI + MathF.PI / Teeth( r3 ), Any( Paint.Olive, Paint.Copper ) );
			}
		}

		static int Teeth( float r ) => Math.Max( 8, (int)MathF.Round( r * 0.75f ) );

		/// <summary>One gear facing the room, its back at <paramref name="centre"/>: a disc, a tooth pointing at <paramref name="phase"/> radians and the rest round it, a hub, four spokes cut as ribs.</summary>
		void Gear( Vector3 centre, float r, float phase, Skin paint )
		{
			var n = Teeth( r );
			Kit.SmoothLathe( paint, At( centre ), new[] { new Vector2( r, 0f ), new Vector2( r, 2.4f ) }, 16, Facing, false, true );
			Kit.SmoothLathe( paint, At( centre + new Vector3( 0f, -2.4f, 0f ) ), new[] { new Vector2( r * 0.28f, 0f ), new Vector2( r * 0.24f, 1.6f ), new Vector2( r * 0.12f, 1.8f ) }, 10, Facing, false, true );
			for ( var k = 0; k < n; k++ )
			{
				var t = phase + k * MathF.Tau / n;
				var dir = new Vector3( MathF.Cos( t ), 0f, MathF.Sin( t ) );
				Kit.Box( paint, At( centre + dir * (r + 1.1f) + new Vector3( 0f, -1.2f, 0f ) ), new Vector3( MathF.Tau * r / n * 0.5f, 2.4f, 2.4f ), Turn( Spin( -t * 180f / MathF.PI + 90f ) ) );
			}
			for ( var k = 0; k < 4; k++ )
			{
				var t = phase + 0.3f + k * MathF.PI * 0.5f;
				var dir = new Vector3( MathF.Cos( t ), 0f, MathF.Sin( t ) );
				Kit.Box( paint, At( centre + dir * (r * 0.55f) + new Vector3( 0f, -2.7f, 0f ) ), new Vector3( r * 0.6f, 0.6f, r * 0.12f ), Turn( Spin( -t * 180f / MathF.PI ) ) );
			}
		}

		/// <summary>An extractor fan in the corrugation, decor half as big again as a real one: a square housing, its blades turned, a wire guard ring and cross.</summary>
		void Fan( float u, float v )
		{
			var size = Range( 40f, 48f );
			var c = new Vector3( u, Face - 2.5f, v + size * 0.5f );
			Kit.BevelBox( P( Paint.Steel ), At( c ), new Vector3( size, 5f, size ), 0.8f, Run );
			Kit.SmoothLathe( P( Paint.LampBlack ), At( c + new Vector3( 0f, -2.55f, 0f ) ), new[] { new Vector2( size * 0.42f, 0f ), new Vector2( size * 0.42f, 0.05f ) }, 14, Facing, false, true );
			var spin = Next() * 360f;
			for ( var k = 0; k < 5; k++ )
			{
				var t = spin + k * 72f;
				var dir = Spin( t ) * Vector3.Up;
				Kit.Box( P( Paint.Galvanised ), At( c + new Vector3( 0f, -3.2f, 0f ) + dir * (size * 0.22f) ), new Vector3( size * 0.16f, 0.4f, size * 0.34f ), Turn( Spin( t ) * Rotation.FromAxis( Vector3.Up, 25f ) ) );
			}
			Kit.SmoothLathe( P( Paint.Iron ), At( c + new Vector3( 0f, -2.6f, 0f ) ), new[] { new Vector2( size * 0.08f, 0f ), new Vector2( size * 0.07f, 1.6f ), new Vector2( 0f, 1.9f ) }, 8, Facing, false, false );
			Kit.Tube( P( Paint.Wire ), At( Loop( c + new Vector3( 0f, -4.4f, 0f ), Vector3.Forward, Vector3.Up, size * 0.42f, 14 ) ), 0.35f, 3 );
			foreach ( var t in new[] { 45f, -45f } )
				Kit.Box( P( Paint.Wire ), At( c + new Vector3( 0f, -4.4f, 0f ) ), new Vector3( 0.5f, 0.4f, size * 0.84f ), Turn( Spin( t ) ) );
		}

		/// <summary>
		/// A chain hoist hung from a bracket out of the girder, in front of the pipes so its chains clear them: a heavy
		/// painted block with its hand wheel, the load chain down to a hook block over the bay, the hand chain hanging
		/// beside it.
		/// </summary>
		void Hoist( float u )
		{
			var y = MathF.Min( -10f, _pipeFront - 2.5f );
			var iron = P( Paint.Iron );
			Kit.Box( iron, At( new Vector3( u, (y - 1.5f) * 0.5f, Girder - 1.6f ) ), new Vector3( 3f, 1.5f - y, 3.2f ), Run );
			Kit.Box( iron, At( new Vector3( u, y, Girder - 4.4f ) ), new Vector3( 4f, 2.4f, 3f ), Run );
			var body = Any( Paint.Carrot, Paint.Teal, Paint.KhakiPaint, Paint.HiVis );
			var block = Girder - 13f;
			Kit.BevelBox( body, At( new Vector3( u, y, block ) ), new Vector3( 11f, 6f, 12f ), 1.2f, Run );
			Kit.SmoothLathe( body, At( new Vector3( u + 5.5f, y, block + 1f ) ), new[] { new Vector2( 4f, 0f ), new Vector2( 4f, 1.6f ) }, 12, Lying, true, true );
			var hook = Range( 150f, 164f );
			Kit.Tube( iron, At( new[] { new Vector3( u - 2f, y, block - 6f ), new Vector3( u - 2f, y, hook + 10f ) } ), 0.55f, 4 );
			Kit.BevelBox( Any( Paint.Black, Paint.Carrot, Paint.Iron ), At( new Vector3( u - 2f, y, hook + 7f ) ), new Vector3( 5f, 4f, 7f ), 0.6f, Run );
			Kit.SmoothLathe( TrimSkin( StripeSwatch ), At( new Vector3( u - 2f, y, hook + 1f ) ), new[] { new Vector2( 1.2f, 0f ), new Vector2( 2f, 1f ), new Vector2( 2f, 3f ), new Vector2( 1f, 4f ) }, 6, Run, true, true );
			var curl = new List<Vector3>();
			for ( var i = 0; i <= 6; i++ )
			{
				var t = MathF.PI * 1.3f * i / 6f;
				curl.Add( new Vector3( u - 2f + MathF.Sin( t ) * 2.6f, y, hook + 1f - 2.6f + MathF.Cos( t ) * 2.6f ) );
			}
			Kit.Tube( iron, At( curl.ToArray() ), 0.6f, 4, 0.3f );
			var hand = Range( 166f, 180f );
			foreach ( var s in new[] { 0f, 1.6f } )
				Kit.Tube( iron, At( new[] { new Vector3( u + 6.5f + s, y, block - 3f ), new Vector3( u + 6.5f + s * 0.6f, y, hand ) } ), 0.3f, 3 );
		}

		/// <summary>The two blueprints pinned side by side about <paramref name="u"/>, their feet near <paramref name="v"/>, each its own sheet.</summary>
		void Blueprints( float u, float v )
		{
			var w0 = Range( 38f, 46f );
			var w1 = Range( 38f, 46f );
			var gap = Range( 3f, 8f );
			var left = u - (w0 + w1 + gap) * 0.5f;
			var first = Pick( 2 );
			Blueprint( left + w0 * 0.5f, v + Range( 0f, 6f ), w0, first );
			Blueprint( left + w0 + gap + w1 * 0.5f, v + Range( 0f, 8f ), w1, 1 - first );
		}

		/// <summary>
		/// A run of galvanised duct along the bay under the pipes, <paramref name="length"/> long about <paramref name="u"/>:
		/// a square trunk filling the bay's depth, flanged at every joint, hung on straps, turning up behind the pipes to the
		/// roof at one end and closed by a grille at the other.
		/// </summary>
		void Duct( float u, float length )
		{
			const float Near = -2f;
			var h = Range( 14f, 18f );
			var v = Range( 150f, 158f );
			var depth = Face - Near;
			var y = (Face + Near) * 0.5f;
			var skin = Any( Paint.Galvanised, Paint.Bright, Paint.Galvanised );
			Kit.Box( skin, At( new Vector3( u, y, v + h * 0.5f ) ), new Vector3( length, depth, h ), Run );
			var joints = Math.Max( 1, (int)(length / 34f) );
			for ( var k = 1; k <= joints; k++ )
			{
				var x = u - length * 0.5f + length * k / (joints + 1f);
				Kit.Box( P( Paint.Steel ), At( new Vector3( x, y - 0.5f, v + h * 0.5f ) ), new Vector3( 1.6f, depth + 1f, h + 1.4f ), Run );
			}
			var up = Next() < 0.5f ? -1f : 1f;
			var riser = u + up * (length * 0.5f - 7f);
			Kit.Box( skin, At( new Vector3( riser, Face - 7f, (v + h + Girder) * 0.5f ) ), new Vector3( 14f, 14f, Girder - v - h ), Run );
			Kit.Box( P( Paint.Steel ), At( new Vector3( riser, Face - 7.5f, v + h + 1f ) ), new Vector3( 15.4f, 15f, 1.6f ), Run );
			var end = u - up * length * 0.5f;
			Kit.Box( P( Paint.Iron ), At( new Vector3( end - up * 0.4f, y, v + h * 0.5f ) ), new Vector3( 0.8f, depth - 2f, h - 2f ), Run );
		}

		/// <summary>
		/// A rack of bar stock over the bay, <paramref name="length"/> long: steel arms out of the corrugation on two
		/// tiers, and lengths of round bar, tube, conduit and plank laid on them, each its own length and finish.
		/// </summary>
		void Bars( float u, float length )
		{
			var arms = Math.Max( 2, (int)(length / 40f) + 1 );
			var iron = P( Paint.Iron );
			foreach ( var tier in new[] { Range( 148f, 152f ), Range( 164f, 170f ) } )
			{
				for ( var k = 0; k < arms; k++ )
				{
					var x = u - length * 0.5f + 6f + (length - 12f) * k / (arms - 1f);
					Kit.Box( iron, At( new Vector3( x, (Face + 6f) * 0.5f, tier - 1f ) ), new Vector3( 1.6f, Face - 6f, 2f ), Run );
					Kit.Box( iron, At( new Vector3( x, 6.8f, tier + 1.5f ) ), new Vector3( 1.6f, 1.6f, 5f ), Run );
				}
				var at = 8f;
				while ( at < Face - 4f )
				{
					var reach = length * Range( 0.75f, 1f );
					var x = u + Range( -0.1f, 0.1f ) * length;
					if ( Next() < 0.25f )
					{
						var w = Range( 4f, 6f );
						Kit.Box( TrimSkin( TimberSwatch ), At( new Vector3( x, at + w * 0.5f, tier + 0.6f ) ), new Vector3( reach, w, 1.2f ), Run );
						at += w + Range( 0.5f, 1.5f );
					}
					else
					{
						var r = Range( 0.7f, 1.8f );
						Kit.Tube( Any( Paint.Bright, Paint.Galvanised, Paint.Copper, Paint.RustPaint, Paint.Steel, Paint.Black ), At( new[] { new Vector3( x - reach * 0.5f, at + r, tier + r ), new Vector3( x + reach * 0.5f, at + r, tier + r ) } ), r, 5 );
						at += r * 2f + Range( 0.4f, 1.6f );
					}
				}
			}
		}

		/// <summary>A long aluminium or timber ladder hung on its side on two hooks, <paramref name="length"/> long: two stiles and the rungs between.</summary>
		void HungLadder( float u, float length )
		{
			var skin = Any( Paint.Bright, Paint.Galvanised, Paint.Handle );
			var v = Range( 150f, 158f );
			const float Width = 13f;
			var y = Face - 4f;
			foreach ( var s in new[] { 0f, 1f } )
				Kit.Box( skin, At( new Vector3( u, y, v + s * Width ) ), new Vector3( length, 3f, 2f ), Run );
			for ( var x = u - length * 0.5f + 6f; x < u + length * 0.5f - 3f; x += Range( 10f, 11f ) )
				Kit.Box( skin, At( new Vector3( x, y, v + Width * 0.5f ) ), new Vector3( 1.2f, 1.2f, Width ), Run );
			foreach ( var s in new[] { -1f, 1f } )
			{
				var x = u + s * length * 0.3f;
				Kit.Box( P( Paint.Iron ), At( new Vector3( x, (Face + y) * 0.5f, v - 1.4f ) ), new Vector3( 1.4f, Face - y + 1f, 1.2f ), Run );
				Kit.Box( P( Paint.Iron ), At( new Vector3( x, y - 2f, v + 0.4f ) ), new Vector3( 1.4f, 1f, 3.6f ), Run );
			}
		}

		/// <summary>The works' signboard over the bay, <paramref name="width"/> wide: a painted board in a steel frame on two brackets.</summary>
		void Signboard( float u, float width )
		{
			var height = width * 0.25f;
			var v = Range( 150f, 160f );
			const float Board = Face - 3f;
			Room.WallPicture( Side, _trim, BoardSwatch, u - width * 0.5f, u + width * 0.5f, v, v + height, Board, false );
			Kit.Box( P( Paint.Navy ), At( new Vector3( u, (Board + Face) * 0.5f, v + height * 0.5f ) ), new Vector3( width + 3f, Face - Board, height + 3f ), Run );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( P( Paint.Iron ), At( new Vector3( u + s * width * 0.32f, Face - 1f, v + height + 2.5f ) ), new Vector3( 1.6f, 2f, 4f ), Run );
		}

		/// <summary>
		/// A control panel over the bay, <paramref name="width"/> wide: a steel box with a white label plate along its
		/// top, a row of gauges, a row of pilot lamps in their colours and the switches under them, and a conduit from
		/// its top to the girder.
		/// </summary>
		void ControlPanel( float u, float width )
		{
			var height = width * Range( 0.66f, 0.78f );
			var v = Range( 146f, 150f );
			const float D = 8f;
			var front = Face - D - 0.05f;
			Kit.Box( Any( Paint.Machine, Paint.Steel, Paint.GreenEnamel, Paint.Navy ), At( new Vector3( u, Face - D * 0.5f, v + height * 0.5f ) ), new Vector3( width, D, height ), Run );
			Flat( P( Paint.Chalk ), u, width * 0.62f, v + height - 5.5f, v + height - 2f, front );
			var gauges = Math.Max( 2, (int)(width / 15f) );
			var first = Pick( 3 );
			for ( var k = 0; k < gauges; k++ )
				Flat( TrimSkin( DialSwatch( (first + k) % 3 ) ), u - width * 0.5f + width * (k + 0.5f) / gauges, 10f, v + height - 17.5f, v + height - 7.5f, front );
			var lamps = gauges + 1;
			for ( var k = 0; k < lamps; k++ )
			{
				var x = u - width * 0.5f + width * (k + 0.5f) / lamps;
				Kit.Box( Any( Paint.Carrot, Paint.Teal, Paint.GreenEnamel, Paint.Chalk, Paint.HiVis ), At( new Vector3( x, front - 0.6f, v + 10.5f ) ), new Vector3( 2.6f, 1.2f, 2.6f ), Run );
				Flat( P( Paint.Black ), x, 2.2f, v + 3f, v + 7f, front );
			}
			Kit.Tube( P( Paint.Galvanised ), At( new[] { new Vector3( u + width * 0.3f, Face - 1.2f, v + height ), new Vector3( u + width * 0.3f, Face - 1.2f, Girder + 1f ) } ), 0.9f, 5 );
		}

		/// <summary>A blueprint <paramref name="width"/> wide pinned to the corrugation, a little askew, a pin in each top corner.</summary>
		void Blueprint( float u, float v, float width, int kind )
		{
			var height = width * 0.75f;
			var tilt = Range( -4f, 4f );
			Kit.Box( TrimSkin( BlueprintSwatch( kind ) ), At( new Vector3( u, Face - 0.4f, v + height * 0.5f ) ), new Vector3( width, 0.15f, height ), Turn( Spin( tilt ) ) );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Ellipsoid( P( Paint.Carrot ), At( new Vector3( u + s * width * 0.45f, Face - 0.8f, v + height * 0.95f + s * tilt * 0.35f ) ), new Vector3( 0.6f, 0.6f, 0.6f ), Run, 2, 4 );
		}

		/// <summary>A shelf on steel brackets at <paramref name="v"/>, <paramref name="length"/> long: tins, boxes, a coil of rope, a fuel can.</summary>
		void HighShelf( float u, float length, float v )
		{
			const float D = 13f;
			var y = Face - D * 0.5f;
			Kit.Box( TrimSkin( TimberSwatch ), At( new Vector3( u, y, v - 0.7f ) ), new Vector3( length, D, 1.4f ), Run );
			foreach ( var s in new[] { -1f, 1f } )
			{
				var x = u + s * (length * 0.5f - Range( 6f, 10f ));
				Kit.Box( P( Paint.Iron ), At( new Vector3( x, Face - 0.6f, v - 6f ) ), new Vector3( 1.2f, 1.2f, 12f ), Run );
				Kit.Box( P( Paint.Iron ), At( new Vector3( x, y, v - 1.9f ) ), new Vector3( 1.2f, D - 1f, 1.2f ), Run );
			}
			var at = u - length * 0.5f + Range( 2f, 5f );
			while ( at < u + length * 0.5f - 6f )
			{
				var roll = Next();
				if ( roll < 0.4f )
				{
					Kit.SmoothLathe( Any( Paint.Carrot, Paint.Teal, Paint.Cream, Paint.Olive, Paint.Bright ), At( new Vector3( at + 3f, y + Range( -2f, 2f ), v ) ), new[] { new Vector2( 2.8f, 0f ), new Vector2( 2.8f, 5f ) }, 6, Run, false, true );
					at += 6.4f;
				}
				else if ( roll < 0.7f )
				{
					var w = Range( 9f, 14f );
					var h = Range( 6f, 11f );
					Kit.Box( P( Paint.Cardboard ), At( new Vector3( at + w * 0.5f, y, v + h * 0.5f ) ), new Vector3( w, D - 2f, h ), Turn( Rotation.FromYaw( Range( -5f, 5f ) ) ) );
					at += w + 1f;
				}
				else if ( roll < 0.85f )
				{
					Kit.Tube( Any( Paint.Leather, Paint.Cardboard ), At( Loop( new Vector3( at + 4.5f, y, v + 1f ), Vector3.Forward, Across, 3.8f, 8 ) ), 1f, 3 );
					at += 9.5f;
				}
				else
				{
					Kit.Box( Any( Paint.Olive, Paint.Carrot ), At( new Vector3( at + 4f, y, v + 6f ) ), new Vector3( 8f, 5f, 12f ), Run );
					Kit.Tube( P( Paint.Black ), At( new[] { new Vector3( at + 6f, y, v + 12f ), new Vector3( at + 7f, y, v + 14f ) } ), 0.7f, 5 );
					at += 9f;
				}
			}
		}

		/// <summary>A chalkboard of the week's jobs in a timber frame, a chalk ledge under it.</summary>
		void Chalkboard( float u, float v )
		{
			var width = Range( 64f, 80f );
			var height = width * 0.5f;
			const float Board = Face - 1.2f;
			Room.WallPicture( Side, _trim, ChalkSwatch, u - width * 0.5f, u + width * 0.5f, v, v + height, Board, false );
			Kit.Box( P( Paint.Handle ), At( new Vector3( u, (Board + 0.05f + Face + 1f) * 0.5f, v + height * 0.5f ) ), new Vector3( width, Face + 0.95f - Board, height ), Run );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.Box( P( Paint.Handle ), At( new Vector3( u, Board - 0.6f, v + height * 0.5f + s * (height * 0.5f + 1f) ) ), new Vector3( width + 4f, 1.8f, 2f ), Run );
				Kit.Box( P( Paint.Handle ), At( new Vector3( u + s * (width * 0.5f + 1f), Board - 0.6f, v + height * 0.5f ) ), new Vector3( 2f, 1.8f, height ), Run );
			}
			Kit.Box( P( Paint.Handle ), At( new Vector3( u, Board - 2f, v - 2.4f ) ), new Vector3( width * 0.6f, 3f, 0.8f ), Run );
			Kit.Box( P( Paint.Chalk ), At( new Vector3( u + Range( -8f, 8f ), Board - 2.4f, v - 1.6f ) ), new Vector3( 3f, 0.6f, 0.6f ), Run );
		}

		// ---- Columns and lamps -----------------------------------------------------------------------

		/// <summary>
		/// On the columns (never the pier, which carries the floor's plaque): the wall's one clock, and a junction box
		/// with its conduit up to the girder on some others — all over the heads of the room.
		/// </summary>
		public void Columns( List<Bay> wall ) => Apart( () =>
		{
			var posts = new List<float>();
			for ( var k = 0; k + 1 < wall.Count; k++ )
			{
				var post = (wall[k].To + wall[k + 1].From) * 0.5f;
				if ( MathF.Abs( post - FloorContext.HalfSpan ) > 50f ) posts.Add( post );
			}
			var clock = Pick( posts.Count );
			for ( var k = 0; k < posts.Count; k++ )
			{
				if ( k == clock ) Clock( posts[k], Range( 158f, 166f ) );
				else if ( Next() < 0.35f )
				{
					var v = Range( 146f, 160f );
					Kit.BevelBox( P( Paint.Steel ), At( new Vector3( posts[k], -2f, v ) ), new Vector3( 7f, 4f, 7f ), 0.4f, Run );
					Kit.Tube( P( Paint.Galvanised ), At( new[] { new Vector3( posts[k], -2f, v + 3.5f ), new Vector3( posts[k], -2f, Girder + 1f ) } ), 0.8f, 6 );
				}
			}
		} );

		/// <summary>A works clock on a column: a black steel rim round a white face, heavy hands.</summary>
		void Clock( float u, float v )
		{
			const float R = 9.5f;
			var back = new Vector3( u, 0f, v );
			Kit.SmoothLathe( P( Paint.LampBlack ), At( back ), new[] { new Vector2( R, 0f ), new Vector2( R + 0.4f, 1f ), new Vector2( R + 0.4f, 3f ), new Vector2( R - 0.4f, 3.4f ) }, 20, Facing, true, false, 40f );
			Kit.SmoothLathe( TrimSkin( ClockFaceSwatch ), At( back + new Vector3( 0f, -2.2f, 0f ) ), new[] { new Vector2( R - 0.5f, 0f ), new Vector2( R - 0.5f, 0.2f ) }, 20, Facing, false, true );
			var minutes = Next() * 720f;
			foreach ( var (turns, length, width) in new[] { (minutes / 720f, R * 0.55f, 1f), (minutes % 60f / 60f, R * 0.8f, 0.7f) } )
			{
				var a = turns * MathF.Tau;
				var dir = new Vector3( MathF.Sin( a ), 0f, MathF.Cos( a ) );
				Kit.Box( P( Paint.Black ), At( back + new Vector3( 0f, -2.7f, 0f ) + dir * (length * 0.35f) ), new Vector3( width, 0.3f, length * 1.1f ), Turn( Rotation.LookAt( Vector3.Cross( ToRoom, dir ), dir ) ) );
			}
		}

		/// <summary>
		/// An enamel cage lamp at <paramref name="u"/> on wall <paramref name="side"/>, on a conduit arm out of the
		/// girder: a green or black enamel shade lit white inside, the bulb in a wire cage under its rim. The room's
		/// lamp burns in the bulb (<see cref="BulbV"/>, <see cref="HangDepth"/>).
		/// </summary>
		public void CageLamp( int side, float u ) => Apart( () =>
		{
			OnSide( side );
			const float Y = HangDepth;
			var conduit = P( Paint.Galvanised );
			var arm = Girder + Range( 8f, 12f );
			var mouth = BulbV + 1.5f;
			Kit.BevelBox( P( Paint.Steel ), At( new Vector3( u, -0.6f, arm ) ), new Vector3( 5f, 1.2f, 6f ), 0.3f, Run );
			Kit.Tube( conduit, At( new[] { new Vector3( u, -1f, arm ), new Vector3( u, Y + 2.4f, arm ), new Vector3( u, Y, arm - 2.4f ), new Vector3( u, Y, mouth + 9f ) } ), 0.7f, 6 );
			var shade = Any( Paint.GreenEnamel, Paint.LampBlack, Paint.GreenEnamel );
			Kit.SmoothLathe( shade, At( new Vector3( u, Y, mouth ) ), new[]
			{
				new Vector2( 8.2f, 0.4f ), new Vector2( 8.6f, 0f ), new Vector2( 8.2f, 1f ), new Vector2( 5f, 4.4f ), new Vector2( 2.2f, 7f ), new Vector2( 1.6f, 9f ),
			}, 14, Run, false, true, 40f, Next() );
			Kit.SmoothLathe( new Skin( _glow, EnamelGlow ), At( new Vector3( u, Y, mouth ) ), new[]
			{
				new Vector2( 1.5f, 8.6f ), new Vector2( 2f, 6.8f ), new Vector2( 4.8f, 4.2f ), new Vector2( 7.9f, 1f ), new Vector2( 8.1f, 0.4f ),
			}, 14, Run, false, false, 50f );
			Kit.Ellipsoid( new Skin( _glow, BulbGlow ), At( new Vector3( u, Y, BulbV ) ), new Vector3( 2f, 2f, 2.6f ), Run, 4, 8 );
			var wire = P( Paint.Wire );
			var cageFoot = BulbV - 5f;
			for ( var k = 0; k < 4; k++ )
			{
				var t = k * MathF.PI * 0.5f + 0.4f;
				var dir = new Vector3( MathF.Cos( t ), MathF.Sin( t ), 0f );
				Kit.Tube( wire, At( new[] { new Vector3( u, Y, mouth + 0.2f ) + dir * 4.6f, new Vector3( u, Y, BulbV ) + dir * 4.2f, new Vector3( u, Y, cageFoot ) + dir * 1.2f } ), 0.18f, 3 );
			}
			Kit.Tube( wire, At( Loop( new Vector3( u, Y, BulbV - 1f ), Vector3.Forward, Across, 4f, 10 ) ), 0.18f, 3 );
			Kit.SmoothLathe( wire, At( new Vector3( u, Y, cageFoot - 0.6f ) ), new[] { new Vector2( 1.4f, 0f ), new Vector2( 1.4f, 0.6f ) }, 6, Run, true, true );
		} );
	}
}
