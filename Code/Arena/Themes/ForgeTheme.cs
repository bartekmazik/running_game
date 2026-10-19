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

	/// <summary>Weathered rock leans brown-grey (the walls' patches).</summary>
	static Color BasaltBrown => Palette.Hex( 0x51473F );

	/// <summary>The joints between columns: near black.</summary>
	static Color Fissure => Palette.Hex( 0x1F1B22 );

	/// <summary>Ash settled on the stone (the ceiling's dusting, the firebrick's).</summary>
	static Color Ash => Palette.Hex( 0x6E6770 );

	/// <summary>
	/// The column tops underfoot, a value step above the rock of the walls (R49). No lamp reaches the middle of the
	/// floor, so the tops are lit by the room's ambient alone; painted as dark as the walls they read near black from
	/// the room, and a hole further than a few tiles off was a dark slot on a dark floor — on the last floor above the
	/// lava, where seeing the holes is the game. The joints (<see cref="Fissure"/>) stay near black, so the columns
	/// still read as rock. Weathered columns lean brown-grey, fresh ones toward the floor's violet; ash drifts paler.
	/// </summary>
	static Color Basalt => Palette.Hex( 0x6A6274 );
	static Color BasaltLight => Palette.Hex( 0x7E7488 );
	static Color BasaltShade => Palette.Hex( 0x544C5C );
	static Color ColumnBrown => Palette.Hex( 0x6E6258 );
	static Color FloorAsh => Palette.Hex( 0x8A838C );

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

	/// <summary>
	/// Facet size of the wall rock in pixels: ten facets across the wall, so the rock tiles; 128 px, about 27 units,
	/// the approved rock scale. <see cref="ArenaTheme.WallSize"/> must stay divisible by ten, so a facet is a whole
	/// number of pixels and the facets tile across the wall's wrap.
	/// </summary>
	const int WallFacet = WallSize / 10;

	/// <summary>
	/// The structural front layer, then the smithy. One bay on every wall is a forge: a firebrick hearth with a
	/// glowing mouth under its lintel and a riveted iron hood rising to the cornice. The other bays get a set dealt
	/// so that no two neighbours match (nor the bays either side of a corner): an anvil on its stump by a stone quench
	/// trough, a smith's bench under a board of tongs and hammers, a great pair of bellows by a heap of coal, a rack
	/// of bar stock with ingots and a crate of horseshoes, a grindstone in its frame, or a quench tub under a rack of
	/// finished blades — the bellows, the grindstone and the quench tub once a wall. Every set carries a light of its
	/// own for the bays the lamps do not reach (task 10 review): a billet glowing on the anvil, a lantern on the bench
	/// or on a bracket over the grindstone, coal raked out hot by the bellows, a pile of fresh-cast ingots, a blade
	/// glowing in the quench tub. What a set leaves bare is filled: on the floor something a head high first where the
	/// bay has nothing tall, then barrels, crates, coal sacks, coal, ingots, chain and buckets; on the wall, at a
	/// player's chest and up to the heads of the room, chunky pieces that read from the middle of the room — tool
	/// boards, cupboards, pigeonholes, racks of tongs and of bar stock, shelf units, the smith's apron. Higher up,
	/// each bay gets two pieces dealt per wall — iron tyres on a peg, a board of horseshoes, a rail of hooks with tongs
	/// and ladles, a rack of blades, a cartwheel (one a wall) or a jib crane with its hook (one a wall); and beside it
	/// a high shelf of crocks, an iron vent, bar stock laid on brackets or the smithy's sign (one a wall) — and
	/// stalactites hang in clusters from the cornice everywhere. The room's four lamps burn in hanging cressets on
	/// brackets out of the cornice; the lava's glow is two warm lights aimed up the shaft at the rock overhead (R32).
	/// Each wall draws from its own stream.
	/// </summary>
	public override void Dress( FloorContext room )
	{
		var bays = BuildFrontLayer( room, new FrontStyle { Post = 36f, Pier = 80f, Corner = 38f, Cornice = 30f, Skirting = 12f, SkirtingDepth = 6f } );

		// Self-illumination is an exponent (2^b): a little under the paint, the forge mouths burn without blooming white.
		Glow.Set( "g_flSelfIllumBrightness", -0.7f );
		var props = new Props( room, Trim, Glow );
		var lamps = LampSpots( 4, Seed + 900 );

		var corners = new List<Props.Look>();
		foreach ( var side in new[] { 0, 3, 2, 1 } )
		{
			props.Begin( side, Seed + 1000 + side * 137 );
			var all = bays.FindAll( b => b.Side == side );
			var wall = all.FindAll( b => b.Width > 60f );
			props.Stalactites( all );

			var looks = props.Deal( new List<Props.Look>
			{
				Props.Look.Anvil, Props.Look.Bench, Props.Look.Bellows, Props.Look.Stock, Props.Look.Grind, Props.Look.Quench,
			}, wall.Count, ( slot, look ) => !(slot == 0 && corners.Count > 0 && look == corners[^1]) && !(slot == wall.Count - 1 && side == 1 && corners.Count > 0 && look == corners[0]) );
			var sets = new List<Props.Look> { Props.Look.Anvil, Props.Look.Stock, Props.Look.Bench };
			props.Once( looks, Props.Look.Grind, sets );
			props.Once( looks, Props.Look.Quench, sets );
			props.Once( looks, Props.Look.Bellows, sets );

			// The wall's forge in one of its wider bays away from the corners, clear of the lamps' cressets.
			var places = new List<int>();
			for ( var k = 1; k + 1 < wall.Count; k++ )
				if ( wall[k].Width > 150f ) places.Add( k );
			looks[places[(int)(props.Next() * places.Count)]] = Props.Look.Forge;
			if ( corners.Count == 0 ) corners.Add( looks[0] );
			corners.Add( looks[^1] );
			props.StartWall( looks.FindAll( l => l == Props.Look.Stock ).Count );

			// Two pieces over the heads of the room in every bay but the forge's, whose hood fills it, one from each
			// deck. Tyres and rails of hooks come twice in the first deck (a smithy has several); the wheel, the crane
			// and the blades once a wall — and no blades at all where the wall's quench set hangs its own rack over
			// the tub (the sentinel at the head of the hand counts that rack as the wall's one); the smithy's sign once
			// a wall in the second.
			var uppers = props.Deal( new List<Props.Upper>
			{
				Props.Upper.Hoops, Props.Upper.Horseshoes, Props.Upper.Hooks, Props.Upper.Blades, Props.Upper.Hoops, Props.Upper.Hooks, Props.Upper.Wheel, Props.Upper.Crane,
			}, wall.Count );
			var spares = new List<Props.Upper> { Props.Upper.Hoops, Props.Upper.Hooks, Props.Upper.Horseshoes };
			props.Once( uppers, Props.Upper.Wheel, spares );
			props.Once( uppers, Props.Upper.Crane, spares );
			var quench = looks.Contains( Props.Look.Quench );
			if ( quench ) uppers.Insert( 0, Props.Upper.Blades );
			props.Once( uppers, Props.Upper.Blades, spares );
			if ( quench ) uppers.RemoveAt( 0 );
			var seconds = props.Deal( new List<Props.Second> { Props.Second.Shelf, Props.Second.Vent, Props.Second.Sign, Props.Second.Bars, Props.Second.Shelf, Props.Second.Bars }, wall.Count );
			props.Once( seconds, Props.Second.Sign, new List<Props.Second> { Props.Second.Shelf, Props.Second.Bars } );

			for ( var k = 0; k < wall.Count; k++ )
			{
				props.Furnish( looks[k], wall[k] );
				if ( looks[k] != Props.Look.Forge ) props.Above( uppers[k], seconds[k], wall[k] );
			}
		}

		props.Begin( 0, Seed + 1900 );
		foreach ( var lamp in lamps )
		{
			props.Cresset( lamp.Side, lamp.U );
			AddLamp( room, lamp.Side, lamp.U, Props.FireV, Props.HangDepth, LightColor * 12f * lamp.Brightness );
		}
		foreach ( var sign in new[] { 1f, -1f } )
			room.AddLight( new Vector3( BounceAt.x * sign, BounceAt.y * sign, room.TileTopZ - BounceDepth ), LavaBounce * BounceStrength, BounceReach, Vector3.Up, BounceCone );
	}

	/// <summary>
	/// The lit parts: a forge mouth's back, deep lava red at its head to a white-hot core at the fire (the one place
	/// besides the seams the forge may glow in the lava's colour, R32); a flame in warm amber; embers in carrot.
	/// </summary>
	protected override PaintedSurface PaintGlow()
	{
		const int Size = 32;
		var painter = new Painter( Size, HotCore, 0.4f );
		for ( var y = 0; y < Size; y++ )
			painter.Fill( 0, y, 16, 1, Mix( Mix( Palette.Lava, Scorch, 0.35f ), Mix( Palette.Lava, HotCore, 0.75f ), y / (Size - 1f) ), roughness: 0.9f );
		painter.Fill( 16, 0, 16, 16, Palette.Hex( 0xFFC27A ), roughness: 0.3f );
		painter.Fill( 16, 16, 16, 16, Mix( Palette.Hex( 0xE9A06A ), Scorch, 0.25f ), roughness: 0.8f );
		var glow = new float[Size * Size];
		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
			glow[y * Size + x] = x < 16 ? 0.55f + 0.45f * y / (Size - 1f) : y < 16 ? 1f : 0.7f;
		painter.AddGlow( glow );
		return painter.Bake( "arena_7_glow", ArenaGenerator.GlowMaterialPath, 1f );
	}

	static Rect MouthGlow => new( 0.06f, 0.06f, 0.38f, 0.88f );
	static Rect FlameGlow => new( 0.56f, 0.06f, 0.38f, 0.38f );
	static Rect EmberGlow => new( 0.56f, 0.56f, 0.38f, 0.38f );

	// The props' paints in the trim atlas's infill band (see PaintSwatches).
	static Rect BrickSwatch => Swatch( 0, 0, 64, 64 );
	static Rect PlankSwatch => Swatch( 64, 0, 64, 32 );
	static Rect StaveSwatch => Swatch( 64, 32, 64, 32 );
	static Rect CoalSwatch => Swatch( 128, 0, 64, 32 );
	static Rect StoneSwatch => Swatch( 128, 32, 64, 32 );
	static Rect BarkSwatch => Swatch( 192, 0, 64, 32 );
	static Rect EndGrainSwatch => Swatch( 192, 32, 32, 32 );
	static Rect LeatherSwatch => Swatch( 224, 32, 32, 32 );
	static Rect SackSwatch => Swatch( 256, 0, 64, 32 );

	/// <summary>A cupboard door: boards in a painted frame, a ledge and brace, strap hinges; oxide red or slate blue.</summary>
	static Rect DoorSwatch( int k ) => Swatch( 320 + k * 64, 0, 64, 64 );
	static Rect SolidSwatch( int k ) => Swatch( (k % 32) * 16, 128 + (k / 32) * 16, 16, 16 );

	/// <summary>
	/// The plain paints in <see cref="SolidSwatch"/> order: iron, rubbed iron, bright steel, oak, charred wood, pine,
	/// leather, rope, brass, copper, coal, ash, water, firebrick, stone, soot, carrot, cream, wine, rust, slate,
	/// hot iron, tallow, horn, pewter, clay, dark wood, black, linen, sand, bronze, blued steel; then tan leather.
	/// Iron, charred wood and dark wood are a step lighter than they would be in daylight: the bays between the lamp
	/// pools have only the room's ambient, and painted true they went black against the rock (task 10 review).
	/// </summary>
	static readonly uint[] Solids =
	{
		0x8E8884, 0xA09A94, 0xC4C6C8, 0xA27C54, 0x6A5244, 0xAA8C64, 0x7E5A40, 0xB09A70, 0xB8955E, 0xA87050, 0x2A2628, 0x8A8480,
		0x2C3236, 0x8A5A48, 0x8C8480, 0x2A252C, 0xE9A06A, 0xE8E2D2, 0x7A4A52, 0x8A5E42, 0x5C5A66, 0xC87A50, 0xE6DCC4, 0xC8B89A,
		0x8C8C88, 0xA06A50, 0x6E5642, 0x26242A, 0xD8CFBC, 0xB8A27E, 0x8E7448, 0x4E5670,
		0xBC966E,
	};

	enum Paint { Iron, Rubbed, Steel, Oak, Charred, Pine, LeatherPaint, Rope, Brass, Copper, Coal, AshPaint, Water, Firebrick, StonePaint, SootPaint, Carrot, Cream, Wine, Rust, Slate, HotIron, Tallow, Horn, Pewter, Clay, DarkWood, Black, Linen, Sand, Bronze, Blued, Tan }

	/// <summary>
	/// The props' paints, in the trim's infill band (the forge hangs no pictures in it), painted over the finished
	/// iron so the wash that darkens the trim leaves them as they are: firebrick courses, boards, barrel staves, coal,
	/// dressed stone, bark, end grain, creased leather, sacking and the plain paints.
	/// </summary>
	static void PaintSwatches( Painter painter, Random random )
	{
		float Next() => (float)random.NextDouble();
		var band = TrimRows( TrimBand.Infill ).Top;
		painter.Fill( 0, band, TrimSize, TrimRows( TrimBand.Infill ).Height, Iron, roughness: 0.6f );

		// ---- Firebrick: four courses of two, broken bond, sooted toward the top ------------------------
		{
			var joint = Palette.Hex( 0x3A3236 );
			painter.Fill( 0, band, 64, 64, joint, roughness: 0.95f );
			for ( var c = 0; c < 4; c++ )
			{
				var x = c % 2 == 0 ? -16f : 0f;
				while ( x < 64f )
				{
					var tone = Mix( Palette.Hex( 0x8A5A48 ), Palette.Hex( 0x6E4A40 ), Next() );
					painter.Rect( x + 1f, band + c * 16f + 1f, 30f, 14f, Mix( tone, Ash, Next() * 0.2f ), radius: 1.5f, height: 0.9f, bevel: 1.5f, roughness: 0.9f );
					x += 32f;
				}
			}
			for ( var y = 0; y < 64; y++ ) painter.Rect( 0f, band + y, 64f, 1f, Soot, alpha: (1f - y / 64f) * 0.35f );
		}

		// ---- Boards across, and barrel staves up ----------------------------------------------------
		for ( var p = 0; p < 4; p++ )
		{
			var tone = Mix( Palette.Hex( 0xB89468 ), Palette.Hex( 0x8E6C4A ), Next() );
			painter.Fill( 64, band + p * 8, 64, 7, tone, roughness: 0.85f );
			for ( var g = 0; g < 3; g++ ) painter.Line( new Vector2( 64f, band + p * 8 + 1f + Next() * 5f ), new Vector2( 128f, band + p * 8 + 1f + Next() * 5f ), 0.6f, Mix( tone, Soot, 0.35f ), 0.4f );
			painter.Fill( 64, band + p * 8 + 7, 64, 1, Palette.Hex( 0x2E2420 ), roughness: 0.9f );
		}
		for ( var x = 0f; x < 64f; )
		{
			var w = 6f + Next() * 3f;
			var tone = Mix( Palette.Hex( 0x96724E ), Palette.Hex( 0x6A4E36 ), Next() );
			painter.Rect( 64f + x, band + 32f, MathF.Min( w, 64f - x ), 32f, tone, roughness: 0.85f );
			painter.Rect( 64f + x, band + 32f, 0.8f, 32f, Palette.Hex( 0x2E2420 ) );
			x += w;
		}

		// ---- Coal: lumps with a dull sheen; dressed stone with tooling ---------------------------------
		painter.Fill( 128, band, 64, 32, Palette.Hex( 0x1E1C20 ), roughness: 0.6f );
		for ( var k = 0; k < 70; k++ )
		{
			var c = new Vector2( 128f + Next() * 64f, band + Next() * 32f );
			var r = 1.5f + Next() * 3f;
			painter.Circle( c.x, c.y, r, Mix( Palette.Hex( 0x2C2A30 ), Palette.Hex( 0x4A4650 ), Next() ), roughness: 0.4f );
			painter.Circle( c.x - r * 0.3f, c.y - r * 0.3f, r * 0.35f, Palette.Hex( 0x6A6670 ), 0.5f );
		}
		painter.Fill( 128, band + 32, 64, 32, Palette.Hex( 0x8C8480 ), roughness: 0.9f );
		painter.Rect( 129f, band + 33f, 62f, 30f, Palette.Hex( 0x9A928C ), radius: 2f, height: 0.9f, bevel: 2f, roughness: 0.9f );
		for ( var k = 0; k < 24; k++ )
		{
			var x = 132f + Next() * 56f;
			painter.Line( new Vector2( x, band + 36f ), new Vector2( x + 4f, band + 60f ), 0.7f, Palette.Hex( 0x6E6662 ), 0.4f );
		}

		// ---- Bark, end grain, creased leather, sacking ---------------------------------------------------
		painter.Fill( 192, band, 64, 32, Palette.Hex( 0x4E3E32 ), roughness: 0.95f );
		for ( var k = 0; k < 18; k++ )
		{
			var x = 192f + Next() * 64f;
			painter.Line( new Vector2( x, band ), new Vector2( x + (Next() - 0.5f) * 4f, band + 32f ), 1f + Next() * 1.5f, Next() < 0.5f ? Palette.Hex( 0x34282A ) : Palette.Hex( 0x6E5A48 ), 0.7f );
		}
		{
			var c = new Vector2( 208f, band + 48f );
			painter.Fill( 192, band + 32, 32, 32, Palette.Hex( 0x4E3E32 ), roughness: 0.9f );
			painter.Circle( c.x, c.y, 13f, Palette.Hex( 0xB8986E ), roughness: 0.85f );
			for ( var r = 2; r < 13; r += 2 ) { painter.Circle( c.x, c.y, r + 0.5f, Palette.Hex( 0x8E6E4C ), 0.6f ); painter.Circle( c.x, c.y, r - 0.3f, Palette.Hex( 0xB8986E ), 0.7f ); }
			painter.Circle( c.x + 3f, c.y - 2f, 6f, Palette.Hex( 0x3A3030 ), 0.5f );
		}
		painter.Fill( 224, band + 32, 32, 32, Palette.Hex( 0x7E5A40 ), roughness: 0.6f );
		for ( var k = 0; k < 6; k++ ) painter.Rect( 224f, band + 34f + k * 5f, 32f, 1.6f, Palette.Hex( 0x4E3628 ), alpha: 0.6f );
		painter.Fill( 256, band, 64, 32, Palette.Hex( 0x6E6258 ), roughness: 0.95f );
		for ( var y = 0; y < 32; y += 2 ) painter.Rect( 256f, band + y, 64f, 0.8f, Palette.Hex( 0x4E4640 ), alpha: 0.5f );
		for ( var x = 0; x < 64; x += 2 ) painter.Rect( 256f + x, band, 0.8f, 32f, Palette.Hex( 0x4E4640 ), alpha: 0.35f );
		for ( var k = 0; k < 8; k++ ) painter.Circle( 256f + Next() * 64f, band + Next() * 32f, 2f + Next() * 4f, Palette.Hex( 0x2A2628 ), 0.3f );

		// ---- Cupboard doors: upright boards in a frame, ledge and brace, strap hinges, a ring; painted, worn -------
		for ( var k = 0; k < 2; k++ )
		{
			var x0 = 320f + k * 64f; var y0 = (float)band;
			var paint = k == 0 ? Palette.Hex( 0x9A5A48 ) : Palette.Hex( 0x5E7088 );
			var dark = Mix( paint, Soot, 0.55f );
			painter.Fill( (int)x0, band, 64, 64, dark, roughness: 0.85f );
			for ( var x = 6f; x < 58f; )
			{
				var w = MathF.Min( 9f + Next() * 4f, 58f - x );
				painter.Rect( x0 + x, y0 + 6f, w - 0.8f, 52f, Mix( paint, Palette.Hex( 0xE8E2D2 ), Next() * 0.12f ), roughness: 0.8f );
				x += w;
			}
			painter.Rect( x0 + 2f, y0 + 2f, 60f, 5f, paint, height: 0.9f, bevel: 1f, roughness: 0.8f );
			painter.Rect( x0 + 2f, y0 + 57f, 60f, 5f, paint, height: 0.9f, bevel: 1f, roughness: 0.8f );
			painter.Rect( x0 + 2f, y0 + 2f, 5f, 60f, paint, height: 0.9f, bevel: 1f, roughness: 0.8f );
			painter.Rect( x0 + 57f, y0 + 2f, 5f, 60f, paint, height: 0.9f, bevel: 1f, roughness: 0.8f );
			painter.Line( new Vector2( x0 + 9f, y0 + 52f ), new Vector2( x0 + 55f, y0 + 12f ), 4f, Mix( paint, Soot, 0.15f ), height: 0.95f, roughness: 0.8f );
			foreach ( var y in new[] { 14f, 50f } )
			{
				painter.Rect( x0 + 2f, y0 + y - 1.6f, 30f, 3.2f, Palette.Hex( 0x2E2A2C ), radius: 1f, height: 1f, roughness: 0.5f );
				for ( var n = 0; n < 3; n++ ) painter.Circle( x0 + 6f + n * 10f, y0 + y, 1f, Palette.Hex( 0x8A8480 ), height: 1f );
			}
			painter.Circle( x0 + 50f, y0 + 33f, 3.6f, Palette.Hex( 0x2E2A2C ), height: 1f );
			painter.Circle( x0 + 50f, y0 + 33f, 2.2f, dark );

			// Paint rubbed through to the wood at the edges and round the ring, soot from the forge over the top.
			for ( var s = 0; s < 26; s++ )
				painter.Circle( x0 + 3f + Next() * 58f, y0 + 3f + Next() * 58f, 1f + Next() * 2.6f, Palette.Hex( 0x8E6C4A ), 0.4f );
			for ( var y = 0; y < 22; y++ ) painter.Rect( x0, y0 + y, 64f, 1f, Soot, alpha: (1f - y / 22f) * 0.3f );
		}

		// ---- Plain paints, each with a little soot ----------------------------------------------------------
		for ( var k = 0; k < Solids.Length; k++ )
		{
			var color = Palette.Hex( Solids[k] );
			var roughness = k switch { 2 or 8 or 9 or 24 or 30 or 31 => 0.35f, 12 => 0.15f, _ => 0.8f };
			var x0 = (k % 32) * 16; var y0 = band + 128 + (k / 32) * 16;
			painter.Fill( x0, y0, 16, 16, color, roughness: roughness );
			for ( var d = 0; d < 6; d++ )
				painter.Circle( x0 + Next() * 16f, y0 + Next() * 16f, 0.4f + Next() * 0.5f, Mix( color, Soot, 0.35f ), 0.4f );
		}
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
		PaintSwatches( painter, new Random( Seed + 720 ) );
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
		painter.Blend( Ramp( painter.Noise( 5, Seed + 30, 2 ), 0.58f, 0.85f, 0.3f ), FloorAsh, roughness: 0.95f );

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
				palette[1 + c * 64 + k] = Mix( color, ColumnBrown, 0.15f + Bits( h, 12 ) * 0.6f );
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

	/// <summary>
	/// Builds the forge's props into the floor's kit, in wall space (see <see cref="WallProps"/>). What a smith would
	/// use — anvils, benches, bellows, troughs, racks, tools — is at a player's size; the tyres, the wheel and the crane
	/// up over the heads of the room may be half as big again. Each prop group draws from a stream of its own
	/// (<see cref="WallProps.Apart"/>).
	/// </summary>
	sealed class Props : WallProps
	{
		/// <summary>Where a cresset's fire burns, where its lamp is, and how far out from the cornice it hangs.</summary>
		public const float FireV = 202f;
		public const float HangDepth = -12f;

		/// <summary>Where furniture's back stands: a hair in front of the skirting's face.</summary>
		const float Back = FloorContext.Recess - 6.5f;

		/// <summary>The underside of the cornice lintel.</summary>
		const float Beam = FloorContext.ClearHeight - 30f;

		/// <summary>The wall face and a little past it, where what is built into the wall ends.</summary>
		const float Deep = FloorContext.Recess + FloorContext.Embed;

		/// <summary>The band of wall a player's chest and head pass, and the band over the heads of the room.</summary>
		const float ChestFoot = 46f;
		const float HeadFoot = 94f;

		static Vector3 ToRoom => new( 0f, -1f, 0f );
		static Vector3 Into => new( 0f, 1f, 0f );

		readonly Material _trim;
		readonly Material _glow;
		readonly List<(float From, float To)> _floor = new();
		readonly List<(float From, float To)> _chest = new();
		readonly List<(float From, float To)> _head = new();

		/// <summary>Whether the bay being furnished has something a head high yet.</summary>
		bool _tall;

		int _boards;
		int _cupboards;
		int _baskets;
		int _braziers;
		int _shelves;

		/// <summary>Racks of bar stock standing on the wall, the stock sets' own counted in from the start: two a wall.</summary>
		int _racks;

		/// <summary>The chest band's racks and hangings on the wall being dressed (see <see cref="Band"/>).</summary>
		int _pigeonholes;
		int _barRacks;
		int _tongRacks;
		int _aprons;

		public Props( FloorContext room, Material trim, Material glow ) : base( room )
		{
			_trim = trim;
			_glow = glow;
		}

		/// <summary>The sets the bays are dealt from; the forge is put in one bay of each wall by hand.</summary>
		public enum Look { Anvil, Bench, Bellows, Stock, Grind, Quench, Forge }

		/// <summary>The first piece over the heads of the room in each bay, toward one end of it.</summary>
		public enum Upper { Hoops, Horseshoes, Hooks, Blades, Wheel, Crane }

		/// <summary>The second piece over the heads of the room, in what the first leaves.</summary>
		public enum Second { Shelf, Vent, Sign, Bars }

		Skin T( Rect region ) => new( _trim, region );
		Skin P( Paint paint ) => T( SolidSwatch( (int)paint ) );
		Skin Any( params Paint[] paints ) => P( paints[Pick( paints.Length )] );
		Rotation Facing => Turn( Rotation.FromToRotation( Vector3.Up, ToRoom ) );
		Rotation Lying => Turn( Rotation.FromToRotation( Vector3.Up, Vector3.Forward ) );
		Rotation Yaw( float degrees ) => Turn( Rotation.FromYaw( degrees ) );

		/// <summary>A wall-space rotation that turns a lathe's axis (Up) onto <paramref name="direction"/>.</summary>
		Rotation Axis( Vector3 direction ) => Turn( Rotation.FromToRotation( Vector3.Up, direction ) );

		void Claim( float u, float width ) => _floor.Add( (u - width * 0.5f, u + width * 0.5f) );

		/// <summary>
		/// Claims a stretch of the chest band where the piece reaches <paramref name="top"/> well into it, and of the band
		/// over the heads of the room where it reaches well into that. A barrel stack or a pile of crates only tops out
		/// at the foot of the chest band; claiming the band over them left the rock bare from their tops to the heads of
		/// the room, half of most bays (task 10 review).
		/// </summary>
		void Hang( float u, float width, float top )
		{
			if ( top > ChestFoot + 12f ) _chest.Add( (u - width * 0.5f, u + width * 0.5f) );
			if ( top > HeadFoot + 8f ) _head.Add( (u - width * 0.5f, u + width * 0.5f) );
		}

		/// <summary>Starts the count of a wall's pieces afresh, its <paramref name="stockSets"/> racks of bar stock already counted.</summary>
		public void StartWall( int stockSets )
		{
			_boards = 0;
			_racks = stockSets;
			_cupboards = 0;
			_baskets = 0;
			_braziers = 0;
			_shelves = 0;
			_pigeonholes = 0;
			_barRacks = 0;
			_tongRacks = 0;
			_aprons = 0;
		}

		// ---- The cornice ------------------------------------------------------------------------------

		/// <summary>
		/// Stalactites under the cornice along the whole wall, in clusters of two to five at uneven spacing, each cone
		/// its own girth and length, the odd one long: mineral dripped out of the rock above and furred with ash — stone and
		/// slate grey, only the odd one pale, as a field of pale ones read as fangs or icicles under the iron (task 10 review).
		/// </summary>
		public void Stalactites( List<Bay> all ) => Apart( () =>
		{
			var down = Turn( Rotation.FromAxis( Vector3.Forward, 180f ) );
			var at = Range( 10f, 60f );
			while ( at < FloorContext.Span - 10f )
			{
				for ( var k = 2 + Pick( 3 ); k > 0; k-- )
				{
					var r = Range( 1.8f, 4.4f );
					var length = Next() < 0.15f ? Range( 22f, 30f ) : Range( 7f, 18f );
					var foot = new Vector3( at + Range( -12f, 12f ), Range( -14f, 30f ), Beam + 0.5f );
					Kit.SmoothLathe( Next() < 0.12f ? Any( Paint.Sand, Paint.Tallow ) : Any( Paint.StonePaint, Paint.Slate, Paint.AshPaint ), At( foot ), new[]
					{
						new Vector2( r, 0f ), new Vector2( r * Range( 0.5f, 0.7f ), length * Range( 0.35f, 0.55f ) ), new Vector2( 0f, length ),
					}, 4, down, false, false, 60f, Next() );
				}
				at += Range( 55f, 130f );
			}
		} );

		/// <summary>
		/// A cresset hung from an iron bracket out of the cornice at <paramref name="u"/> on wall <paramref name="side"/>:
		/// a chain to a ring, three rods down to an iron basket, the basket heaped with embers and burning (<see cref="FireV"/>,
		/// <see cref="HangDepth"/>).
		/// </summary>
		public void Cresset( int side, float u ) => Apart( () =>
		{
			OnSide( side );
			var iron = P( Paint.Iron );
			var arm = Beam + Range( 11f, 14f );
			Kit.BevelBox( iron, At( new Vector3( u, -0.6f, arm - 2f ) ), new Vector3( 6f, 1.2f, 12f ), 0.3f, Run );
			Kit.Tube( iron, At( new[] { new Vector3( u, -1f, arm ), new Vector3( u, HangDepth - 1.5f, arm ) } ), 0.75f, 4 );
			Kit.Tube( iron, At( new[] { new Vector3( u, -1f, arm - 7f ), new Vector3( u, HangDepth * 0.65f, arm - 0.5f ) } ), 0.5f, 4 );
			var ring = FireV + 14f;
			Chain( new[] { new Vector3( u, HangDepth, arm - 0.8f ), new Vector3( u, HangDepth, ring + 1.6f ) }, 4.2f );
			Kit.Tube( iron, At( Loop( new Vector3( u, HangDepth, ring ), Vector3.Forward, Into, 1.6f, 6 ) ), 0.4f, 3 );
			const float Rim = 7.2f;
			var rimV = FireV - 4f;
			var turn = Next() * MathF.Tau;
			for ( var k = 0; k < 3; k++ )
			{
				var a = turn + k * MathF.Tau / 3f;
				Kit.Tube( iron, At( new[] { new Vector3( u, HangDepth, ring ), new Vector3( u + MathF.Cos( a ) * Rim, HangDepth + MathF.Sin( a ) * Rim, rimV + 0.5f ) } ), 0.35f, 3 );
			}
			Kit.SmoothLathe( iron, At( new Vector3( u, HangDepth, FireV - 12f ) ), new[]
			{
				new Vector2( 1.4f, 0f ), new Vector2( 4.4f, 2.4f ), new Vector2( 6.4f, 5.4f ), new Vector2( Rim, 8f ), new Vector2( Rim - 0.6f, 8.4f ),
			}, 8, Run, true, false, 50f, Next() );
			Kit.SmoothLathe( P( Paint.Rubbed ), At( new Vector3( u, HangDepth, FireV - 12f ) ), new[] { new Vector2( Rim + 0.3f, 7.2f ), new Vector2( Rim + 0.3f, 8.6f ) }, 8, Run, false, false );
			Kit.Dome( new Skin( _glow, EmberGlow ), At( new Vector3( u, HangDepth, rimV - 0.4f ) ), new Vector3( Rim - 0.8f, Rim - 0.8f, 3.2f ), Run, 2, 8 );
			var flame = new Skin( _glow, FlameGlow );
			for ( var k = 0; k < 3; k++ )
			{
				var a = turn + 0.5f + k * MathF.Tau / 3f;
				var at = new Vector3( u + MathF.Cos( a ) * 2.6f, HangDepth + MathF.Sin( a ) * 2.6f, rimV + 0.8f );
				Kit.SmoothLathe( flame, At( at ), new[] { new Vector2( 2.6f, 0f ), new Vector2( 2.2f, 3f ), new Vector2( 1.1f, 6.5f ), new Vector2( 0f, Range( 9f, 13f ) ) }, 6, Turn( Rotation.FromAxis( Into, Range( -12f, 12f ) ) ), false, false, 60f, Next() );
			}
		} );

		// ---- Bays ---------------------------------------------------------------------------------

		/// <summary>
		/// Furnishes <paramref name="bay"/> as <paramref name="look"/> — each set claims most of the bay's width, with
		/// something at a player's chest or head in it — then fills what the set leaves bare (<see cref="Fill"/>).
		/// </summary>
		public void Furnish( Look look, Bay bay ) => Apart( () =>
		{
			_floor.Clear();
			_chest.Clear();
			_head.Clear();
			_tall = false;
			var (from, to) = (bay.From + 4f, bay.To - 4f);
			switch ( look )
			{
				case Look.Anvil:
				{
					var length = Range( 36f, 46f );
					Row( from, to, 5f, (26f, Anvil), (length + 2f, u => Trough( u, length )), (13f, Bucket) );
					break;
				}
				case Look.Bench:
				{
					var length = Range( 50f, 64f );
					Row( from, to, 4f, (length + 2f, u => { SmithBench( u, length ); ToolBoard( u + Range( -4f, 4f ), MathF.Min( length, Range( 48f, 60f ) ), Range( 58f, 64f ), Range( 38f, 44f ) ); }), (26f, Barrel) );
					break;
				}
				case Look.Bellows:
				{
					var heap = Range( 34f, 44f );
					Row( from, to, 4f, (52f, Bellows), (heap, u => CoalHeap( u, heap, true )), (13f, Bucket) );
					break;
				}
				case Look.Stock:
				{
					var width = Range( 36f, 46f );
					Row( from, to, 4f, (width + 2f, u => StockRack( u, width )), (24f, u => Ingots( u, true )), (26f, u => HorseshoeCrate( u )) );
					break;
				}
				case Look.Grind:
					Row( from, to, 4f, (34f, u => { Grindstone( u ); WallLantern( u + Range( -6f, 6f ), Range( 84f, 92f ) ); }), (26f, Barrel), (28f, u => CrateStack( u, false )) );
					break;
				case Look.Quench:
					Row( from, to, 4f, (30f, u => { Tub( u ); HotBlade( u ); Blades( u + Range( -4f, 4f ), Range( 40f, 48f ), Range( 58f, 66f ) ); }), (26f, Barrel), (30f, ChainCoil) );
					break;
				default:
				{
					var width = MathF.Min( bay.Width - 40f, Range( 84f, 96f ) );
					var u = bay.Middle + Range( -0.08f, 0.08f ) * bay.Width;
					Forge( u, width );
					break;
				}
			}
			Fill( bay );
		} );

		/// <summary>
		/// Fills what a bay's set left bare. On the floor, working along each gap from one end: something a head high
		/// first where the bay has nothing tall yet (a rack of bar stock, two water barrels and a third laid on them, or
		/// crates piled high), then a standing brazier (three a wall), barrels, crates, coal sacks, a heap of coal, ingots,
		/// coiled chain or a bucket. On the
		/// wall, at a player's chest over whatever stays low and again over the heads of the room (<see cref="Band"/>).
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
					if ( !_tall && room >= 30f )
					{
						if ( roll < 0.35f && room >= 40f && _racks < 2 )
						{
							var width = Range( 32f, MathF.Min( 40f, room - 4f ) );
							StockRack( Put( width + 2f ), width );
							_racks++;
						}
						else if ( roll < 0.7f && room >= 46f ) BarrelStack( Put( 46f ) );
						else CrateStack( Put( 28f ), true );
					}
					else if ( room >= 24f && roll < 0.16f && _braziers < 3 ) Brazier( Put( 22f ) );
					else if ( room >= 26f && roll < 0.26f ) Barrel( Put( 26f ) );
					else if ( room >= 28f && roll < 0.38f ) CrateStack( Put( 28f ), false );
					else if ( room >= 30f && roll < 0.52f ) Sacks( Put( 30f ) );
					else if ( room >= 30f && roll < 0.64f ) { var width = Range( 28f, MathF.Min( 40f, room - 2f ) ); CoalHeap( Put( width ), width, false ); }
					else if ( room >= 24f && roll < 0.76f ) Ingots( Put( 24f ), false );
					else if ( room >= 30f && roll < 0.86f ) ChainCoil( Put( 30f ) );
					else Bucket( Put( 13f ) );
				}
			}

			foreach ( var (from, to) in Gaps( _chest, bay.From + 5f, bay.To - 5f ) ) Band( from, to, false );
			foreach ( var (from, to) in Gaps( _head, bay.From + 5f, bay.To - 5f ) ) Band( from, to, true );
		}

		/// <summary>
		/// The pieces a bare stretch draws from, as <see cref="Band"/> numbers them: the chunky ones — boards, cupboards,
		/// pigeonholes, tongs — dealt more often than the shelf and the bar stock, which read as lines from the room.
		/// </summary>
		static readonly int[] Kinds = { 0, 0, 0, 2, 2, 2, 4, 4, 6, 6, 7, 3, 1, 5 };

		/// <summary>
		/// Fills a bare stretch of wall from one end, a piece at a time: a dark board of bright tools or a painted tool
		/// cupboard (five a wall each), a pigeonhole rack of ingots and bar ends or a rail of hanging tongs (four a wall
		/// each), a shelf unit of ingots, pots and horseshoes or bar stock laid on brackets (three a wall each), a fire
		/// basket burning on its bracket (five a wall), or the smith's leather apron and gloves on their pegs (two); at a
		/// player's chest, or (<paramref name="high"/>) over the heads of the room. The pieces are chunky and stand
		/// off the rock in value, so each reads as itself from the middle of the room: thin planks with small things on
		/// them read as lines from there, and plain boards as blank rectangles (task 10 review). A pick that does not
		/// fit leaves a little bare stone rather than repeat the last.
		/// </summary>
		void Band( float from, float to, bool high )
		{
			var at = from + Range( 0f, 5f );
			var last = -1;
			while ( to - at >= 20f )
			{
				var room = to - at;
				var foot = (high ? HeadFoot + 6f : ChestFoot + 4f) + Range( 0f, 6f );

				// At the chest, a board, a cupboard or a rack of pigeonholes is as often a tall one, up past the heads of
				// the room, which claims that band too: fewer, bigger pieces cover the rock where many small ones dotted it.
				var height = high ? 136f - foot : Next() < 0.55f ? Range( 70f, 82f ) : Range( 38f, 46f );
				var width = 0f;
				for ( var tries = 0; tries < 10 && width == 0f; tries++ )
				{
					var kind = Kinds[Pick( Kinds.Length )];
					if ( kind == last ) continue;
					switch ( kind )
					{
						case 0 when room >= 40f && _boards < 5: width = MathF.Min( room - 2f, Range( 44f, 62f ) ); ToolBoard( at + width * 0.5f, width, foot, height ); break;
						case 1 when room >= 38f && _shelves < 3: width = MathF.Min( room - 2f, Range( 40f, 60f ) ); Shelf( at + width * 0.5f, width, foot + 6f ); break;
						case 2 when room >= 32f && _cupboards < 5: width = MathF.Min( room - 2f, Range( 32f, 44f ) ); Cupboard( at + width * 0.5f, width, foot, MathF.Max( height, 30f ) ); break;
						case 3 when room >= 24f && _baskets < 5: width = 22f; FireBasket( at + 11f, foot + Range( 20f, 30f ) ); break;
						case 4 when room >= 32f && _pigeonholes < 4: width = MathF.Min( room - 2f, Range( 32f, 46f ) ); Pigeonholes( at + width * 0.5f, width, foot, MathF.Max( height, 30f ) ); break;
						case 5 when room >= 48f && _barRacks < 3: width = MathF.Min( room - 2f, Range( 50f, 80f ) ); BarRack( at + width * 0.5f, width, foot + 6f ); break;
						case 6 when room >= 34f && _tongRacks < 4: width = MathF.Min( room - 2f, Range( 36f, 52f ) ); TongRack( at + width * 0.5f, width, foot + Range( 34f, 40f ) ); break;
						case 7 when room >= 32f && _aprons < 2 && !high: width = MathF.Min( room - 2f, Range( 32f, 38f ) ); Apron( at + width * 0.5f, width, foot + Range( 44f, 50f ) ); break;
					}
					if ( width > 0f ) last = kind;
				}
				at += (width > 0f ? width : 10f) + Range( 3f, 7f );
			}
		}

		/// <summary>
		/// A tool cupboard hung on the wall: two painted doors (oxide red or slate blue, a swatch of their own, so they do
		/// not read as one more board) in a sooted carcass under a top board. Five a wall at most.
		/// </summary>
		void Cupboard( float u, float width, float v, float height )
		{
			_cupboards++;
			Hang( u, width + 2f, v + height + 2f );
			const float D = 12f;
			Kit.BevelBox( P( Paint.Charred ), At( new Vector3( u, Face - D * 0.5f, v + height * 0.5f ) ), new Vector3( width, D, height ), 0.6f, Run );
			Kit.Box( P( Paint.Charred ), At( new Vector3( u, Face - D * 0.5f - 0.8f, v + height + 1f ) ), new Vector3( width + 3f, D + 1.6f, 2f ), Run );
			var front = Face - D - 0.1f;
			var doors = DoorSwatch( Pick( 2 ) );
			foreach ( var s in new[] { -1f, 1f } )
			{
				var door = u + s * width * 0.25f;
				var region = s < 0f ? doors : new Rect( doors.Right, doors.Top, -doors.Width, doors.Height );
				Room.WallPicture( Side, _trim, region, door - width * 0.23f, door + width * 0.23f, v + 2f, v + height - 2f, front, false );
			}
		}

		/// <summary>A fire basket burning on an iron bracket in the wall, for the smith's tapers: glow without a lamp of its own. Five a wall at most.</summary>
		void FireBasket( float u, float v )
		{
			_baskets++;
			Hang( u, 22f, v + 14f );
			var iron = P( Paint.Iron );
			var y = Face - 9f;
			Kit.Box( iron, At( new Vector3( u, Face - 0.6f, v - 2f ) ), new Vector3( 5f, 1.2f, 10f ), Run );
			Kit.Tube( iron, At( new[] { new Vector3( u, Face - 1f, v - 5f ), new Vector3( u, y + 1f, v - 4f ), new Vector3( u, y, v - 1f ) } ), 0.5f, 4 );
			Kit.SmoothLathe( iron, At( new Vector3( u, y, v - 1f ) ), new[] { new Vector2( 1.2f, 0f ), new Vector2( 3.6f, 2f ), new Vector2( 5.4f, 5f ), new Vector2( 5.8f, 6.5f ) }, 7, Run, true, false, 50f, Next() );
			Kit.Dome( new Skin( _glow, EmberGlow ), At( new Vector3( u, y, v + 4.8f ) ), new Vector3( 5f, 5f, 2.6f ), Run, 2, 7 );
			var flame = new Skin( _glow, FlameGlow );
			for ( var k = 0; k < 2; k++ )
				Kit.SmoothLathe( flame, At( new Vector3( u + (k == 0 ? -1.6f : 1.8f), y + Range( -1f, 1f ), v + 5.6f ) ), new[] { new Vector2( 2f, 0f ), new Vector2( 1.4f, 3f ), new Vector2( 0f, Range( 6f, 9f ) ) }, 5, Turn( Rotation.FromAxis( Into, Range( -12f, 12f ) ) ), false, false, 60f, Next() );
		}

		/// <summary>
		/// A lantern standing at <paramref name="foot"/>: an iron base and cap round glowing panes, corner posts, a ring
		/// on top — one of the bays' own lights where the lamps do not reach (task 10 review).
		/// </summary>
		void Lantern( Vector3 foot, float scale )
		{
			var iron = P( Paint.Iron );
			var w = 5.6f * scale; var h = 8.4f * scale;
			var turn = Rotation.FromYaw( Range( -30f, 30f ) );
			var rotation = Turn( turn );
			Kit.Box( iron, At( foot + new Vector3( 0f, 0f, 0.7f * scale ) ), new Vector3( w + 1.6f, w + 1.6f, 1.4f * scale ), rotation );
			var glass = 1.4f * scale;
			Kit.Box( new Skin( _glow, FlameGlow ), At( foot + new Vector3( 0f, 0f, glass + h * 0.5f ) ), new Vector3( w, w, h ), rotation );
			foreach ( var sx in new[] { -1f, 1f } )
			foreach ( var sy in new[] { -1f, 1f } )
				Kit.Box( iron, At( foot + turn * new Vector3( sx * w * 0.5f, sy * w * 0.5f, glass + h * 0.5f ) ), new Vector3( 0.9f, 0.9f, h ), rotation );
			var top = glass + h;
			Kit.SmoothLathe( iron, At( foot + new Vector3( 0f, 0f, top ) ), new[] { new Vector2( w * 0.78f, 0f ), new Vector2( w * 0.5f, 2.4f * scale ), new Vector2( 0.9f, 4f * scale ) }, 4, Turn( turn * Rotation.FromYaw( 45f ) ), true, false, 30f );
			Kit.Tube( iron, At( Loop( foot + new Vector3( 0f, 0f, top + 5.6f * scale ), Vector3.Forward, Vector3.Up, 1.6f * scale, 6 ) ), 0.35f, 3 );
		}

		/// <summary>A lantern hung from an iron bracket in the wall, its ring at <paramref name="v"/>.</summary>
		void WallLantern( float u, float v )
		{
			const float Out = 11f;
			Hang( u, 16f, v + 4f );
			var iron = P( Paint.Iron );
			Kit.Box( iron, At( new Vector3( u, Face - 0.6f, v + 1f ) ), new Vector3( 4.6f, 1.2f, 11f ), Run );
			Kit.Tube( iron, At( new[] { new Vector3( u, Face - 1f, v + 4f ), new Vector3( u, Face - Out, v + 4f ), new Vector3( u, Face - Out, v + 2.4f ) } ), 0.6f, 4 );
			Kit.Tube( iron, At( new[] { new Vector3( u, Face - 1f, v - 4f ), new Vector3( u, Face - Out * 0.6f, v + 3.6f ) } ), 0.45f, 4 );
			Lantern( new Vector3( u, Face - Out, v - 15.4f ), 1.2f );
		}

		/// <summary>A blade just out of the fire, glowing, stood in the quench tub in a pair of tongs leant on its rim.</summary>
		void HotBlade( float u )
		{
			const float R = 13f;
			var centre = new Vector3( u + Range( -4f, 4f ), Back - R - 1.5f, Ledge + 4f );
			var tilt = Rotation.FromAxis( Into, Range( 14f, 22f ) * (Next() < 0.5f ? -1f : 1f) );
			var dir = tilt * Vector3.Up;
			var length = Range( 26f, 30f );
			var mid = centre + dir * (length * 0.5f);
			Kit.BevelBox( new Skin( _glow, EmberGlow ), At( mid ), new Vector3( 2.8f, 0.8f, length ), 0.3f, Turn( tilt ) );
			Tongs( centre + dir * (length - 3f), dir * Range( 16f, 20f ), new Vector3( 0f, 1.4f, 0f ), Paint.Rubbed, 0.5f );
		}

		/// <summary>
		/// A pigeonhole rack on the wall, two rows of cells or (a tall rack) four, a cell to a kind: ingots end on, bar ends
		/// of every gauge, a cell of horseshoes. Four a wall at most.
		/// </summary>
		void Pigeonholes( float u, float width, float v, float height )
		{
			_pigeonholes++;
			Hang( u, width + 2f, v + height );
			const float D = 13f;
			var y = Face - D * 0.5f;
			var wood = Any( Paint.Charred, Paint.DarkWood );
			Kit.Box( wood, At( new Vector3( u, Face - 0.6f, v + height * 0.5f ) ), new Vector3( width, 1.2f, height ), Run );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.Box( wood, At( new Vector3( u + s * (width * 0.5f - 1f), y, v + height * 0.5f ) ), new Vector3( 2f, D, height ), Run );
				Kit.Box( wood, At( new Vector3( u, y, v + height * 0.5f + s * (height * 0.5f - 1f) ) ), new Vector3( width, D, 2f ), Run );
			}
			var columns = width > 38f ? 3 : 2;
			var rows = height > 54f ? 4 : 2;
			var cell = (width - 2f) / columns;
			var tall = (height - 2f) / rows;
			for ( var c = 1; c < columns; c++ )
				Kit.Box( wood, At( new Vector3( u - width * 0.5f + 1f + c * cell, y, v + height * 0.5f ) ), new Vector3( 1.6f, D, height - 2f ), Run );
			for ( var r = 1; r < rows; r++ )
				Kit.Box( wood, At( new Vector3( u, y, v + 1f + r * tall ) ), new Vector3( width - 2f, D, 1.6f ), Run );
			for ( var r = 0; r < rows; r++ )
			for ( var c = 0; c < columns; c++ )
			{
				var x = u - width * 0.5f + 1f + (c + 0.5f) * cell;
				var floor = v + 1f + r * tall + 0.8f;
				switch ( Pick( 3 ) )
				{
					case 0:
						for ( var k = 0; k < 3 + Pick( 2 ); k++ )
							Kit.Box( Any( Paint.Rubbed, Paint.Bronze, Paint.Copper ), At( new Vector3( x + Range( -1f, 1f ), y - 0.5f, floor + 1.2f + k * 2.4f ) ), new Vector3( cell - 4f, D - 3f, 2.2f ), Run );
						break;
					case 1:
						for ( var k = 0; k < 5; k++ )
						{
							var r2 = Range( 0.6f, 1.2f );
							var end = new Vector3( x + (k % 3 - 1) * (cell * 0.25f), y - D * 0.5f + 1f, floor + r2 + (k / 3) * 2.6f );
							Kit.Tube( Any( Paint.Rubbed, Paint.Steel, Paint.Rust ), At( new[] { end, end + new Vector3( 0f, D - 2f, 0f ) } ), r2, 4 );
						}
						break;
					default:
						for ( var k = 0; k < 2; k++ )
							Horseshoe( new Vector3( x + (k - 0.5f) * cell * 0.4f, y - D * 0.5f + 1.4f, floor + 4.2f ), 0f, false, 1.1f );
						break;
				}
			}
		}

		/// <summary>Bar stock laid along the wall at a player's chest on three iron brackets, in three tiers. Three a wall at most.</summary>
		void BarRack( float u, float length, float v )
		{
			_barRacks++;
			Hang( u, length + 4f, v + 22f );
			var iron = P( Paint.Iron );
			var tiers = new[] { 0f, 9f, 18f };
			foreach ( var t in new[] { -0.42f, 0f, 0.42f } )
			{
				var x = u + t * length;
				Kit.Box( iron, At( new Vector3( x, Face - 0.7f, v + 9f ) ), new Vector3( 3f, 1.4f, 24f ), Run );
				foreach ( var tier in tiers )
					Kit.Box( iron, At( new Vector3( x, Face - 5f, v + tier - 1f ) ), new Vector3( 2f, 9f, 2f ), Run );
			}
			foreach ( var tier in tiers )
			for ( var k = 0; k < 3 + Pick( 3 ); k++ )
			{
				var r = Range( 0.8f, 1.6f );
				var y = Face - 1.8f - k * 1.6f;
				var z = v + tier + r;
				var shift = Range( -4f, 4f );
				Kit.Tube( Any( Paint.Rubbed, Paint.Steel, Paint.Rust, Paint.Rubbed ), At( new[] { new Vector3( u - length * 0.5f + shift, y, z ), new Vector3( u + length * 0.5f + shift, y, z ) } ), r, 4 );
			}
		}

		/// <summary>A rail on two brackets hung with tongs of every size, reins over the rail, jaws down. Four a wall at most.</summary>
		void TongRack( float u, float width, float v )
		{
			_tongRacks++;
			Hang( u, width + 2f, v + 3f );
			var wood = Any( Paint.Charred, Paint.DarkWood );
			Kit.BevelBox( wood, At( new Vector3( u, Face - 0.8f, v - 8f ) ), new Vector3( width, 1.6f, 22f ), 0.3f, Run );
			Kit.Tube( P( Paint.Iron ), At( new[] { new Vector3( u - width * 0.5f + 1f, Face - 4f, v ), new Vector3( u + width * 0.5f - 1f, Face - 4f, v ) } ), 0.8f, 4 );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( P( Paint.Iron ), At( new Vector3( u + s * (width * 0.5f - 2f), Face - 2f, v ) ), new Vector3( 1.4f, 4f, 1.4f ), Run );
			var at = u - width * 0.5f + Range( 4f, 6f );
			while ( at < u + width * 0.5f - 4f )
			{
				Tongs( new Vector3( at, Face - 5.4f, v - 3f ), new Vector3( Range( -1.5f, 1.5f ), 0f, -Range( 22f, 32f ) ), new Vector3( Range( 1.2f, 2.2f ), 0f, 0f ), Next() < 0.3f ? Paint.Steel : Paint.Rubbed, 0.55f );
				at += Range( 5f, 7f );
			}
		}

		/// <summary>The smith's leather apron hung on a peg by its neck strap, and his gloves on the next peg. Two a wall at most.</summary>
		void Apron( float u, float width, float v )
		{
			_aprons++;
			var apron = u - width * 0.5f + 11f;
			var gloves = u + width * 0.5f - 6f;
			var length = Range( 38f, 44f );
			Hang( u, width, v + 2f );
			foreach ( var x in new[] { apron, gloves } )
				Kit.Box( P( Paint.DarkWood ), At( new Vector3( x, Face - 2.4f, v ) ), new Vector3( 1.6f, 4.8f, 1.6f ), Run );
			var leather = Any( Paint.Tan, Paint.Tan, Paint.LeatherPaint );
			var top = v - 6f;
			Kit.Tube( P( Paint.LeatherPaint ), At( new[] { new Vector3( apron - 5f, Face - 2f, top - 2f ), new Vector3( apron, Face - 3.6f, v + 0.8f ), new Vector3( apron + 5f, Face - 2f, top - 2f ) } ), 0.5f, 3 );
			Kit.BevelBox( leather, At( new Vector3( apron, Face - 1.6f, top - 5f ) ), new Vector3( 11f, 1.6f, 10f ), 0.5f, Run );
			Kit.BevelBox( leather, At( new Vector3( apron, Face - 1.8f, top - 10f - (length - 10f) * 0.5f ) ), new Vector3( 19f, 1.8f, length - 10f ), 0.6f, Turn( Rotation.FromAxis( Into, Range( -3f, 3f ) ) ) );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Ellipsoid( leather, At( new Vector3( gloves + s * 2.4f, Face - 3.4f, v - 8f - Range( 0f, 2f ) ) ), new Vector3( 3f, 2.2f, 7.4f ), Turn( Rotation.FromAxis( Into, s * Range( 6f, 14f ) ) ), 2, 6 );
		}

		/// <summary>A standing brazier on a tripod, its bowl heaped with glowing coals. Three a wall at most.</summary>
		void Brazier( float u )
		{
			Claim( u, 22f );
			_braziers++;
			var y = Back - 11f;
			var bowl = Ledge + Range( 30f, 36f );
			var iron = P( Paint.Iron );
			var turn = Next() * MathF.Tau;
			for ( var k = 0; k < 3; k++ )
			{
				var a = turn + k * MathF.Tau / 3f;
				var dir = new Vector3( MathF.Cos( a ), MathF.Sin( a ) * 0.8f, 0f );
				Kit.Tube( iron, At( new[] { new Vector3( u, y, bowl ) + dir * 3f, new Vector3( u, y, Ledge ) + dir * 8.5f } ), 0.55f, 4 );
			}
			Kit.SmoothLathe( iron, At( new Vector3( u, y, bowl ) ), new[] { new Vector2( 1.5f, 0f ), new Vector2( 5f, 2.4f ), new Vector2( 8f, 5.4f ), new Vector2( 8.6f, 6.4f ) }, 8, Run, true, false, 50f, Next() );
			Kit.Dome( new Skin( _glow, EmberGlow ), At( new Vector3( u, y, bowl + 5f ) ), new Vector3( 7.8f, 7.8f, 3.6f ), Run, 2, 8 );
			var flame = new Skin( _glow, FlameGlow );
			for ( var k = 0; k < 2; k++ )
				Kit.SmoothLathe( flame, At( new Vector3( u + Range( -3f, 3f ), y + Range( -2f, 2f ), bowl + 6f ) ), new[] { new Vector2( 2.4f, 0f ), new Vector2( 1.6f, 3.6f ), new Vector2( 0f, Range( 7f, 11f ) ) }, 5, Turn( Rotation.FromAxis( Into, Range( -10f, 10f ) ) ), false, false, 60f, Next() );
		}

		// ---- The forge ----------------------------------------------------------------------------

		/// <summary>
		/// The forge, one a wall: a firebrick hearth a player's waist high under a stone slab, its mouth between brick jambs
		/// glowing from a bed of coals, a stone lintel with its keystone, and a riveted iron hood over it narrowing into a
		/// flue that runs up into the cornice; a poker and a shovel leant against the hearth.
		/// </summary>
		void Forge( float u, float width )
		{
			Claim( u, width );
			Hang( u, width, Beam );
			_tall = true;
			var brick = T( BrickSwatch );
			var stone = T( StoneSwatch );
			const float Front = 6f;
			var hearth = Ledge + Range( 30f, 34f );
			Kit.BevelBox( P( Paint.Firebrick ), At( new Vector3( u, (Front + Deep) * 0.5f, (Ledge + hearth) * 0.5f ) ), new Vector3( width, Deep - Front, hearth - Ledge ), 1f, Run );
			Bricks( u - width * 0.5f + 1f, u + width * 0.5f - 1f, Ledge + 1f, hearth - 1f, Front - 0.1f );
			Kit.BevelBox( stone, At( new Vector3( u, (Front - 1.5f + Deep) * 0.5f, hearth + 1.5f ) ), new Vector3( width + 4f, Deep - Front + 1.5f, 3f ), 0.6f, Run );

			var mouth = width - 30f;
			var floor = hearth + 3f;
			var top = floor + Range( 32f, 38f );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.BevelBox( brick, At( new Vector3( u + s * (mouth * 0.5f + 7.5f), (Front + 1f + Deep) * 0.5f, (floor + top) * 0.5f ) ), new Vector3( 15f, Deep - Front - 1f, top - floor ), 0.8f, Run );
			Room.WallPicture( Side, _glow, MouthGlow, u - mouth * 0.5f, u + mouth * 0.5f, floor, top, 30f, false );
			Kit.Dome( new Skin( _glow, EmberGlow ), At( new Vector3( u, 21f, floor ) ), new Vector3( mouth * 0.44f, 8f, 4.5f ), Run, 2, 8 );
			var flame = new Skin( _glow, FlameGlow );
			for ( var k = 0; k < 3; k++ )
				Kit.SmoothLathe( flame, At( new Vector3( u + (k - 1) * mouth * 0.25f + Range( -2f, 2f ), Range( 19f, 24f ), floor + 3f ) ), new[]
				{
					new Vector2( Range( 2.2f, 3.2f ), 0f ), new Vector2( 1.6f, 4.5f ), new Vector2( 0f, Range( 9f, 14f ) ),
				}, 5, Turn( Rotation.FromAxis( Into, Range( -14f, 14f ) ) ), false, false, 60f, Next() );

			Kit.BevelBox( stone, At( new Vector3( u, (Front + Deep) * 0.5f, top + 5f ) ), new Vector3( width, Deep - Front, 10f ), 0.8f, Run );
			Kit.BevelBox( stone, At( new Vector3( u, (Front - 2f + Deep) * 0.5f, top + 5.5f ) ), new Vector3( 9f, Deep - Front + 2f, 12f ), 0.6f, Run );

			var lower = top + 10f;
			const float Shoulder = 136f;
			Kit.BevelBox( P( Paint.Firebrick ), At( new Vector3( u, (Front - 1f + Deep) * 0.5f, (lower + Shoulder) * 0.5f ) ), new Vector3( width + 4f, Deep - Front + 1f, Shoulder - lower ), 0.8f, Run );
			Bricks( u - width * 0.5f - 1.5f, u + width * 0.5f + 1.5f, lower + 1f, Shoulder - 3f, Front - 1.1f );
			Kit.Box( P( Paint.Rubbed ), At( new Vector3( u, (Front - 2.2f + Deep) * 0.5f, lower + 2f ) ), new Vector3( width + 5f, Deep - Front + 2.2f, 4f ), Run );
			Kit.Box( P( Paint.Rubbed ), At( new Vector3( u, (Front - 2.6f + Deep) * 0.5f, Shoulder - 1.5f ) ), new Vector3( width + 6f, Deep - Front + 2.6f, 3f ), Run );
			var flue = width * Range( 0.5f, 0.6f );
			const float FlueFront = 14f;
			Kit.Box( P( Paint.Firebrick ), At( new Vector3( u, (FlueFront + Deep) * 0.5f, (Shoulder + Beam + 1f) * 0.5f ) ), new Vector3( flue, Deep - FlueFront, Beam + 1f - Shoulder ), Run );
			Bricks( u - flue * 0.5f, u + flue * 0.5f, Shoulder, Beam, FlueFront - 0.1f );
			Kit.Box( P( Paint.Rubbed ), At( new Vector3( u, (FlueFront - 1.2f + Deep) * 0.5f, Shoulder + (Beam - Shoulder) * 0.55f ) ), new Vector3( flue + 2f, Deep - FlueFront + 1.2f, 2.6f ), Run );

			var side = Next() < 0.5f ? -1f : 1f;
			var foot = new Vector3( u + side * (width * 0.5f - 6f), Front - 1f + 4f, Ledge );
			Kit.Tube( P( Paint.Iron ), At( new[] { foot, foot + new Vector3( side * 4f, 1.5f, 50f ) } ), 0.55f, 4 );
			var shovel = foot + new Vector3( -side * 7f, 0f, 0f );
			Kit.Tube( P( Paint.Oak ), At( new[] { shovel + new Vector3( 0f, 0f, 8f ), shovel + new Vector3( side * 3f, 1.5f, 44f ) } ), 0.6f, 4 );
			Kit.Box( P( Paint.Iron ), At( shovel + new Vector3( 0f, -0.5f, 4f ) ), new Vector3( 7f, 1f, 8f ), Run );
		}

		/// <summary>
		/// Firebrick laid over a face toward the room from <paramref name="u0"/> to <paramref name="u1"/> and <paramref name="v0"/> to
		/// <paramref name="v1"/> at <paramref name="depth"/>: the brick swatch in cells of a few courses each, the last cell of a
		/// row or column cut short, so the courses keep their size on a face of any size.
		/// </summary>
		void Bricks( float u0, float u1, float v0, float v1, float depth )
		{
			const float Cell = 30f;
			var region = BrickSwatch;
			for ( var v = v0; v < v1 - 0.5f; v += Cell )
			for ( var u = u0; u < u1 - 0.5f; u += Cell )
			{
				var w = MathF.Min( Cell, u1 - u );
				var h = MathF.Min( Cell, v1 - v );
				var cut = new Rect( region.Left, region.Top + region.Height * (1f - h / Cell), region.Width * w / Cell, region.Height * h / Cell );
				Room.WallPicture( Side, _trim, cut, u, u + w, v, v + h, depth, false );
			}
		}

		// ---- Smithing ---------------------------------------------------------------------------------

		/// <summary>An anvil on its stump: a waisted body, a flat face, a horn and a heel, a billet glowing on the face just out of the fire, the hammer by it.</summary>
		void Anvil( float u )
		{
			Claim( u, 26f );
			var r = Range( 8.5f, 9.5f );
			var h = Range( 13f, 15f );
			var foot = new Vector3( u, Back - r - 3f, Ledge );
			Kit.SmoothLathe( T( BarkSwatch ), At( foot ), new[] { new Vector2( r + 0.6f, 0f ), new Vector2( r, h ) }, 8, Run, false, false, 45f, Next() );
			Kit.SmoothLathe( T( EndGrainSwatch ), At( foot ), new[] { new Vector2( r - 0.1f, h ), new Vector2( r - 0.1f, h + 0.1f ) }, 8, Run, false, true );
			var yaw = Range( -8f, 8f );
			var turn = Rotation.FromYaw( yaw );
			var rotation = Turn( turn );
			Vector3 On( float x, float z ) => foot + turn * new Vector3( x, 0f, h + z );
			var iron = P( Paint.Iron );
			Kit.BevelBox( iron, At( On( 0f, 2f ) ), new Vector3( 16f, 11f, 4f ), 0.6f, rotation );
			Kit.BevelBox( iron, At( On( 0f, 7f ) ), new Vector3( 9f, 7f, 6f ), 0.4f, rotation );
			Kit.BevelBox( P( Paint.Rubbed ), At( On( 1f, 12.5f ) ), new Vector3( 21f, 9f, 5f ), 0.5f, rotation );
			Kit.Box( iron, At( On( -11f, 13f ) ), new Vector3( 4f, 7f, 3f ), rotation );
			Kit.SmoothLathe( iron, At( On( 11.4f, 12.5f ) ), new[] { new Vector2( 4f, 0f ), new Vector2( 3f, 4.5f ), new Vector2( 1.4f, 8.5f ), new Vector2( 0f, 11.5f ) }, 6, Turn( turn * Rotation.FromToRotation( Vector3.Up, Vector3.Forward ) ), false, false, 50f );
			Kit.BevelBox( new Skin( _glow, EmberGlow ), At( On( Range( 2f, 6f ), 16.6f ) ), new Vector3( Range( 13f, 16f ), 3.2f, 3.2f ), 0.4f, Turn( turn * Rotation.FromYaw( Range( -20f, 20f ) ) ) );
			Hammer( On( Range( -9f, -6f ), 15.2f ), yaw + Range( 60f, 120f ) );
		}

		/// <summary>A smith's hammer lying at <paramref name="at"/>, its handle turned <paramref name="yaw"/> degrees.</summary>
		void Hammer( Vector3 at, float yaw )
		{
			var turn = Rotation.FromYaw( yaw );
			Kit.Tube( P( Paint.Oak ), At( new[] { at + turn * new Vector3( 0f, 0f, 0.8f ), at + turn * new Vector3( 13f, 0f, 0.8f ) } ), 0.6f, 4 );
			Kit.BevelBox( P( Paint.Iron ), At( at + turn * new Vector3( -0.5f, 0f, 1.3f ) ), new Vector3( 2.6f, 6f, 2.6f ), 0.3f, Turn( turn ) );
		}

		/// <summary>A pair of tongs from <paramref name="pivot"/>, jaws one way and reins the other, open a little.</summary>
		void Tongs( Vector3 pivot, Vector3 reins, Vector3 open, Paint paint = Paint.Iron, float radius = 0.4f )
		{
			var iron = P( paint );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Tube( iron, At( new[] { pivot - reins * 0.3f + open * s * 0.6f, pivot, pivot + reins + open * s } ), radius, 3 );
		}

		/// <summary>A stone quench trough of water, tongs left standing in it.</summary>
		void Trough( float u, float length )
		{
			Claim( u, length + 2f );
			const float D = 16f;
			var y = Back - D * 0.5f - 0.5f;
			var h = Range( 17f, 20f );
			Kit.BevelBox( T( StoneSwatch ), At( new Vector3( u, y, Ledge + h * 0.5f ) ), new Vector3( length, D, h ), 1f, Run );
			var z = Ledge + h - 2f;
			var x0 = u - length * 0.5f + 2.5f; var x1 = u + length * 0.5f - 2.5f;
			Kit.Quad( P( Paint.Water ), At( new Vector3( x0, y - D * 0.5f + 2.5f, z ) ), At( new Vector3( x1, y - D * 0.5f + 2.5f, z ) ), At( new Vector3( x1, y + D * 0.5f - 2.5f, z ) ), At( new Vector3( x0, y + D * 0.5f - 2.5f, z ) ) );
			var at = new Vector3( u + Range( -0.3f, 0.3f ) * length, y, z - 2f );
			Tongs( at, new Vector3( Range( -4f, 4f ), -2f, 18f ), new Vector3( 1.6f, 0f, 0f ) );
		}

		/// <summary>A smith's bench: a thick top on four legs, a shelf under it with ingots, a leg vice at one end, horseshoes and tongs on top, a lantern burning at the other end.</summary>
		void SmithBench( float u, float length )
		{
			Claim( u, length + 2f );
			const float D = 20f;
			var y = Back - D * 0.5f - 0.5f;
			var top = Ledge + Range( 30f, 32f );
			var wood = Any( Paint.Oak, Paint.Charred, Paint.Oak );
			Kit.BevelBox( wood, At( new Vector3( u, y, top - 1.6f ) ), new Vector3( length, D, 3.2f ), 0.6f, Run );
			foreach ( var sx in new[] { -1f, 1f } )
			foreach ( var sy in new[] { -1f, 1f } )
				Kit.Box( wood, At( new Vector3( u + sx * (length * 0.5f - 3f), y + sy * (D * 0.5f - 2.5f), (Ledge + top - 3.2f) * 0.5f ) ), new Vector3( 3f, 3f, top - 3.2f - Ledge ), Run );
			Kit.Box( wood, At( new Vector3( u, y, Ledge + 8f ) ), new Vector3( length - 6f, D - 5f, 1.6f ), Run );
			for ( var k = 0; k < 3 + Pick( 3 ); k++ )
				Kit.BevelBox( P( Paint.Rubbed ), At( new Vector3( u - length * 0.3f + k * 5f + Range( -1f, 1f ), y + Range( -3f, 3f ), Ledge + 10f ) ), new Vector3( 4f, 9f, 2.2f ), 0.3f, Yaw( Range( -10f, 10f ) ) );
			var end = Next() < 0.5f ? -1f : 1f;
			var vice = new Vector3( u + end * (length * 0.5f - 5f), y - D * 0.5f + 3f, top );
			Kit.BevelBox( P( Paint.Iron ), At( vice + new Vector3( 0f, 0f, 3f ) ), new Vector3( 4f, 6f, 6f ), 0.4f, Run );
			Kit.Box( P( Paint.Iron ), At( vice + new Vector3( 0f, -3.6f, 3.6f ) ), new Vector3( 4f, 1.2f, 5f ), Run );
			Kit.Tube( P( Paint.Rubbed ), At( new[] { vice + new Vector3( -3f, -4.6f, 3f ), vice + new Vector3( 3f, -4.6f, 3f ) } ), 0.35f, 3 );
			Kit.Tube( P( Paint.Iron ), At( new[] { vice + new Vector3( 0f, 1f, 0f ), vice + new Vector3( 0f, 1f, Ledge - top ) } ), 0.8f, 4 );
			Horseshoe( new Vector3( u - end * length * 0.15f, y + Range( -4f, 2f ), top + 0.3f ), 0f, true );
			Horseshoe( new Vector3( u - end * length * 0.15f + Range( 5f, 8f ), y + Range( -2f, 4f ), top + 0.3f ), 0f, true );
			Tongs( new Vector3( u + Range( -6f, 6f ), y - 2f, top + 0.6f ), new Vector3( end * 14f, 2f, 0f ), new Vector3( 0f, 1.6f, 0f ) );
			Lantern( new Vector3( u - end * (length * 0.5f - 6f), y + Range( -1f, 2f ), top ), Range( 1.1f, 1.25f ) );
		}

		/// <summary>A horseshoe at <paramref name="at"/>: lying flat on a surface, or hung toe down on a nail in the wall.</summary>
		void Horseshoe( Vector3 at, float turn, bool flat, float scale = 1f )
		{
			var path = new Vector3[5];
			for ( var i = 0; i < 5; i++ )
			{
				var a = MathF.PI * (-0.12f + 1.24f * i / 4f) + turn;
				var p = new Vector3( MathF.Cos( a ) * 2.8f, 0f, MathF.Sin( a ) * 3.2f ) * scale;
				path[i] = flat ? at + new Vector3( p.x, p.z, 0.4f ) : at + new Vector3( p.x, 0f, -p.z );
			}
			Kit.Tube( P( Paint.Rubbed ), At( path ), 0.45f * scale, 3 );
		}

		/// <summary>
		/// A dark board of tools on the wall, in one row or (a tall board) two: tongs, hammers, a rasp and a horseshoe, each on its peg, the iron rubbed
		/// bright — the blade rack's idiom (bright steel on dark wood), the one that reads from across the room; a pale
		/// board with dark iron on it read as a blank plank there (task 10 review). Five a wall at most.
		/// </summary>
		void ToolBoard( float u, float width, float v, float h )
		{
			_boards++;
			Hang( u, width + 2f, v + h );
			Kit.BevelBox( Any( Paint.Charred, Paint.DarkWood, Paint.Charred ), At( new Vector3( u, Face - 0.9f, v + h * 0.5f ) ), new Vector3( width, 1.8f, h ), 0.4f, Run );
			var rows = h > 60f ? 2 : 1;
			for ( var row = 0; row < rows; row++ )
			{
				var top = v + h - row * h * 0.5f;
				ToolRow( u, width, top );
			}
		}

		/// <summary>One row of tools on a board, hung from pegs a little under <paramref name="top"/>.</summary>
		void ToolRow( float u, float width, float top )
		{
			var at = u - width * 0.5f + Range( 4f, 6f );
			var y = Face - 3f;
			while ( at < u + width * 0.5f - 5f )
			{
				var peg = top - Range( 3f, 5f );
				Kit.Box( P( Paint.Iron ), At( new Vector3( at, Face - 2.4f, peg ) ), new Vector3( 1.2f, 3f, 1.2f ), Run );
				switch ( Pick( 4 ) )
				{
					case 0:
						Tongs( new Vector3( at, y, peg - 6f ), new Vector3( 0f, 0f, -Range( 25f, 30f ) ), new Vector3( 1.8f, 0f, 0f ), Paint.Rubbed, 0.6f );
						at += 8f;
						break;
					case 1:
					{
						var length = Range( 18f, 22f );
						Kit.Tube( P( Paint.Oak ), At( new[] { new Vector3( at, y, peg - 1f ), new Vector3( at, y, peg - length ) } ), 0.8f, 4 );
						Kit.BevelBox( P( Paint.Steel ), At( new Vector3( at, y, peg - length - 2f ) ), new Vector3( 11f, 4f, 4.6f ), 0.5f, Run );
						at += 13f;
						break;
					}
					case 2:
						Kit.Box( P( Paint.Steel ), At( new Vector3( at, y, peg - 13f ) ), new Vector3( 2.6f, 0.8f, 20f ), Run );
						Kit.Tube( P( Paint.Oak ), At( new[] { new Vector3( at, y, peg - 1.5f ), new Vector3( at, y, peg - 4.5f ) } ), 0.8f, 4 );
						at += 6f;
						break;
					default:
						Horseshoe( new Vector3( at + 1f, y, peg - 0.5f ), 0f, false, 1.4f );
						at += 10f;
						break;
				}
				at += Range( 2f, 4f );
			}
		}

		/// <summary>A rack of finished blades on the wall, point down: swords and a long knife, brass guards, leather grips. One a wall.</summary>
		void Blades( float u, float width, float v )
		{
			Hang( u, width + 2f, v + 46f );
			Kit.BevelBox( P( Paint.DarkWood ), At( new Vector3( u, Face - 1.2f, v + 40f ) ), new Vector3( width, 2.4f, 4f ), 0.4f, Run );
			Kit.BevelBox( P( Paint.DarkWood ), At( new Vector3( u, Face - 1.2f, v + 6f ) ), new Vector3( width, 2.4f, 3f ), 0.4f, Run );
			var count = (int)(width / 10f);
			for ( var k = 0; k < count; k++ )
			{
				var x = u - width * 0.5f + (k + 0.5f) * width / count + Range( -1f, 1f );
				var y = Face - 3.6f;
				var blade = Range( 28f, 36f );
				var hilt = v + 42f + Range( -1f, 1f );
				Kit.BevelBox( P( Paint.Steel ), At( new Vector3( x, y, hilt - 2f - blade * 0.5f ) ), new Vector3( k % 3 == 2 ? 1.8f : 2.6f, 0.6f, blade ), 0.25f, Run );
				Kit.Box( P( Paint.Brass ), At( new Vector3( x, y, hilt - 1.4f ) ), new Vector3( 7f, 1.4f, 1.2f ), Run );
				Kit.Tube( P( Paint.LeatherPaint ), At( new[] { new Vector3( x, y, hilt - 0.8f ), new Vector3( x, y, hilt + 5.5f ) } ), 0.65f, 4 );
				Kit.Ellipsoid( P( Paint.Brass ), At( new Vector3( x, y, hilt + 6.3f ) ), new Vector3( 1.1f, 1.1f, 1.1f ), Run, 2, 5 );
			}
		}

		/// <summary>An iron rail on two brackets with S-hooks hanging tongs, a ladle and a pair of chains' worth of hooks.</summary>
		void HookRail( float u, float width, float v, bool high )
		{
			Hang( u, width, v + 2f );
			var iron = P( Paint.Iron );
			if ( high ) Kit.BevelBox( Any( Paint.Charred, Paint.DarkWood ), At( new Vector3( u, Face - 0.8f, v - 12f ) ), new Vector3( width + 6f, 1.6f, 30f ), 0.3f, Run );
			Kit.Tube( iron, At( new[] { new Vector3( u - width * 0.5f, Face - 3.6f, v ), new Vector3( u + width * 0.5f, Face - 3.6f, v ) } ), 0.6f, 4 );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( iron, At( new Vector3( u + s * (width * 0.5f - 2f), Face - 1.8f, v ) ), new Vector3( 1.2f, 3.6f, 1.2f ), Run );
			var at = u - width * 0.5f + Range( 5f, 8f );
			while ( at < u + width * 0.5f - 5f )
			{
				var y = Face - 3.6f;
				Kit.Tube( iron, At( new[] { new Vector3( at, y - 0.8f, v + 0.6f ), new Vector3( at, y - 1.2f, v - 1.4f ), new Vector3( at, y - 0.6f, v - 3f ) } ), 0.3f, 3 );
				var hook = v - 3.4f;
				switch ( Pick( 3 ) )
				{
					case 0:
						Tongs( new Vector3( at, y - 0.8f, hook - 6f ), new Vector3( 0f, 0f, -Range( 16f, 22f ) ), new Vector3( 1.4f, 0f, 0f ) );
						break;
					case 1:
					{
						var length = Range( 16f, 22f );
						Kit.Tube( iron, At( new[] { new Vector3( at, y - 0.8f, hook ), new Vector3( at, y - 0.8f, hook - length ) } ), 0.4f, 3 );
						Kit.SmoothLathe( iron, At( new Vector3( at, y - 3f, hook - length - 2.4f ) ), new[] { new Vector2( 0.8f, 0f ), new Vector2( 2.8f, 1f ), new Vector2( 3.2f, 2.6f ) }, 6, Run, true, false );
						break;
					}
					default:
						Hammer( new Vector3( at - 1f, y - 1f, hook - 15f ), 90f + Range( -10f, 10f ) );
						break;
				}
				at += Range( 9f, 13f );
			}
		}

		/// <summary>
		/// A great pair of bellows on a trestle: boards above and below a body of creased leather, the upper board tilted
		/// open, an iron nozzle, and the lever up to its rocking post.
		/// </summary>
		void Bellows( float u )
		{
			Claim( u, 52f );
			Hang( u, 52f, 82f );
			_tall = true;
			var y = Back - 10f;
			var wood = P( Paint.Oak );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( wood, At( new Vector3( u + s * 15f, y, Ledge + 13f ) ), new Vector3( 3f, 15f, 26f ), Run );
			Kit.Box( wood, At( new Vector3( u, y, Ledge + 27.5f ) ), new Vector3( 38f, 15f, 3f ), Run );
			var bed = Ledge + 29f;
			var dir = Next() < 0.5f ? 1f : -1f;
			Kit.BevelBox( P( Paint.DarkWood ), At( new Vector3( u - dir * 2f, y, bed + 0.8f ) ), new Vector3( 38f, 18f, 1.6f ), 0.3f, Run );
			Kit.Ellipsoid( T( LeatherSwatch ), At( new Vector3( u - dir * 4f, y, bed + 5f ) ), new Vector3( 17f, 8.4f, 4.6f ), Run, 3, 8 );
			var tilt = Range( 7f, 12f );
			Kit.BevelBox( P( Paint.DarkWood ), At( new Vector3( u - dir * 3f, y, bed + 9.6f ) ), new Vector3( 38f, 18f, 1.6f ), 0.3f, Turn( Rotation.FromAxis( Into, dir * tilt ) ) );
			Kit.SmoothLathe( P( Paint.Iron ), At( new Vector3( u + dir * 16f, y, bed + 4.8f ) ), new[] { new Vector2( 2.8f, 0f ), new Vector2( 1.7f, 6f ), new Vector2( 1.1f, 11f ) }, 6, Axis( new Vector3( dir, 0f, 0f ) ), false, true );
			var post = new Vector3( u - dir * 23f, y + 4f, Ledge );
			Kit.Box( wood, At( post + new Vector3( 0f, 0f, 40f ) ), new Vector3( 3f, 3f, 80f ), Run );
			Kit.Tube( P( Paint.DarkWood ), At( new[] { post + new Vector3( dir * 1f, -2.2f, 78f ), new Vector3( u + dir * 8f, y - 2.2f, bed + 11f ) } ), 0.7f, 4 );
			Kit.Tube( P( Paint.DarkWood ), At( new[] { post + new Vector3( dir * 1f, -2.2f, 78f ), post + new Vector3( -dir * 14f, -2.2f, 70f ) } ), 0.7f, 4 );
		}

		/// <summary>A heap of coal against the wall, a shovel stood in it; a <paramref name="hot"/> one glowing through at its foot, raked out of the fire.</summary>
		void CoalHeap( float u, float width, bool hot )
		{
			Claim( u, width );
			var h = Range( 8f, 13f );
			Kit.Dome( T( CoalSwatch ), At( new Vector3( u, Back - 13f, Ledge ) ), new Vector3( width * 0.5f, 12f, h ), Yaw( Range( -5f, 5f ) ), 3, 10 );
			if ( hot ) Kit.Dome( new Skin( _glow, EmberGlow ), At( new Vector3( u + Range( -0.12f, 0.12f ) * width, Back - 17f, Ledge ) ), new Vector3( width * 0.34f, 10f, h * 0.72f ), Yaw( Range( -10f, 10f ) ), 2, 8 );
			if ( Next() < 0.5f ) return;
			var foot = new Vector3( u + Range( -0.2f, 0.2f ) * width, Back - 12f, Ledge + h * 0.6f );
			Kit.Box( P( Paint.Iron ), At( foot ), new Vector3( 7f, 1f, 8f ), Turn( Rotation.FromAxis( Vector3.Forward, 20f ) ) );
			Kit.Tube( P( Paint.Oak ), At( new[] { foot + new Vector3( 0f, 0f, 4f ), foot + new Vector3( Range( -6f, 6f ), 6f, 40f ) } ), 0.6f, 4 );
		}

		/// <summary>A rack of bar stock a head high: a base with sockets, a rail on two uprights, bars of every gauge leant in it.</summary>
		void StockRack( float u, float width )
		{
			Claim( u, width + 2f );
			Hang( u, width + 2f, 96f );
			_tall = true;
			var y = Back - 5.5f;
			var wood = Any( Paint.Oak, Paint.Charred );
			Kit.BevelBox( wood, At( new Vector3( u, y, Ledge + 1.6f ) ), new Vector3( width, 10f, 3.2f ), 0.4f, Run );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( wood, At( new Vector3( u + s * (width * 0.5f - 1.4f), Back - 2f, Ledge + 38f ) ), new Vector3( 2.4f, 2.4f, 76f ), Run );
			Kit.Box( wood, At( new Vector3( u, Back - 2f, Ledge + 64f ) ), new Vector3( width, 2.6f, 3f ), Run );
			var count = (int)(width / 4.5f);
			for ( var k = 0; k < count; k++ )
			{
				var x = u - width * 0.5f + 3f + (width - 6f) * k / Math.Max( count - 1, 1 ) + Range( -1f, 1f );
				var foot = new Vector3( x, y - 1.5f + Range( -1.5f, 1.5f ), Ledge + 3f );
				var top = new Vector3( x + Range( -4f, 4f ), Back - 4f, Ledge + Range( 84f, 98f ) );
				Kit.Tube( Any( Paint.Iron, Paint.Rubbed, Paint.Rust, Paint.Iron ), At( new[] { foot, top } ), Range( 0.45f, 0.9f ), 4 );
			}
		}

		/// <summary>Ingots stacked in a criss-cross pile, three layers or four; a <paramref name="hot"/> pile freshly cast, its top layer still glowing.</summary>
		void Ingots( float u, bool hot )
		{
			Claim( u, 24f );
			var y = Back - 11f;
			var layers = 3 + Pick( 2 );
			for ( var l = 0; l < layers; l++ )
			for ( var k = 0; k < 3; k++ )
			{
				var across = l % 2 == 0;
				var offset = (k - 1) * 5.5f;
				var at = new Vector3( u + (across ? offset : 0f), y + (across ? 0f : offset), Ledge + 1.2f + l * 2.6f );
				var skin = hot && l == layers - 1 ? new Skin( _glow, EmberGlow ) : Any( Paint.Rubbed, Paint.Iron, Paint.Bronze );
				Kit.Box( skin, At( at ), across ? new Vector3( 4.4f, 15f, 2.4f ) : new Vector3( 15f, 4.4f, 2.4f ), Yaw( Range( -4f, 4f ) ) );
			}
		}

		/// <summary>A crate of new horseshoes, a few on its lid.</summary>
		void HorseshoeCrate( float u )
		{
			Claim( u, 26f );
			var size = Range( 20f, 23f );
			Crate( u, Ledge, size, Range( -8f, 8f ) );
			for ( var k = 0; k < 3; k++ )
				Horseshoe( new Vector3( u + Range( -6f, 6f ), Back - size * 0.5f - 1f + Range( -4f, 4f ), Ledge + size ), Range( 0f, MathF.Tau ), true );
		}

		/// <summary>A grindstone in its frame, face to the room: the wheel on an axle between two posts, a crank, a water trough under it, a treadle.</summary>
		void Grindstone( float u )
		{
			Claim( u, 34f );
			Hang( u, 34f, 48f );
			var y = Back - 13f;
			var r = Range( 12f, 14f );
			var axle = Ledge + r + 7f;
			var wood = P( Paint.Oak );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( wood, At( new Vector3( u, y + s * 6.5f, (Ledge + axle + 3f) * 0.5f ) ), new Vector3( 4f, 2.6f, axle + 3f - Ledge ), Run );
			Kit.Box( wood, At( new Vector3( u, y, Ledge + 1.2f ) ), new Vector3( r * 2f + 6f, 15f, 2.4f ), Run );
			Kit.SmoothLathe( T( StoneSwatch ), At( new Vector3( u, y - 2.6f, axle ) ), new[] { new Vector2( r, 0f ), new Vector2( r, 5.2f ) }, 12, Axis( Into ), true, true, 45f, Next() );
			Kit.Tube( P( Paint.Iron ), At( new[] { new Vector3( u, y - 9f, axle ), new Vector3( u, y + 8f, axle ) } ), 0.8f, 4 );
			Kit.Tube( P( Paint.Iron ), At( new[] { new Vector3( u, y - 9f, axle ), new Vector3( u + 6f, y - 9f, axle - 3f ), new Vector3( u + 6f, y - 13f, axle - 3f ) } ), 0.6f, 4 );
			Kit.BevelBox( T( StoneSwatch ), At( new Vector3( u, y, Ledge + 5.5f ) ), new Vector3( r * 1.6f, 9f, 6f ), 0.5f, Run );
			Kit.Box( P( Paint.DarkWood ), At( new Vector3( u + r * 0.4f, y - 5f, Ledge + 1.5f ) ), new Vector3( 4f, 14f, 1.4f ), Yaw( Range( -15f, 15f ) ) );
		}

		/// <summary>A quench tub: a cut-down barrel of dark water.</summary>
		void Tub( float u )
		{
			Claim( u, 30f );
			const float R = 13f;
			var h = Range( 15f, 18f );
			var foot = new Vector3( u, Back - R - 1.5f, Ledge );
			Kit.SmoothLathe( T( StaveSwatch ), At( foot ), new[] { new Vector2( R - 1f, 0f ), new Vector2( R, h * 0.5f ), new Vector2( R + 0.4f, h ), new Vector2( R - 0.6f, h ), new Vector2( R - 1.2f, h - 1.5f ) }, 10, Run, true, false, 45f, Next() );
			foreach ( var t in new[] { 0.2f, 0.8f } )
				Kit.SmoothLathe( P( Paint.Iron ), At( foot ), new[] { new Vector2( R - 0.6f + t * 1.2f, h * t - 0.9f ), new Vector2( R - 0.6f + t * 1.2f, h * t + 0.9f ) }, 10, Run, false, false );
			Kit.SmoothLathe( P( Paint.Water ), At( foot ), new[] { new Vector2( R - 1.3f, h - 2.4f ), new Vector2( R - 1.3f, h - 2.3f ) }, 10, Run, false, true );
		}

		/// <summary>A coil of heavy chain on the floor, three loops piled.</summary>
		void ChainCoil( float u )
		{
			Claim( u, 30f );
			var centre = new Vector3( u, Back - 13f, Ledge + 1.2f );
			for ( var k = 0; k < 3; k++ )
			{
				var r = 11f - k * 2.6f + Range( -0.6f, 0.6f );
				Kit.Tube( P( Paint.Iron ), At( Loop( centre + new Vector3( Range( -1f, 1f ), Range( -1f, 1f ), k * 2.2f ), Vector3.Forward, Into, r, 9 ) ), 1.2f, 3 );
			}
		}

		// ---- Stores ---------------------------------------------------------------------------------

		/// <summary>The bulge of a barrel's staves <paramref name="t"/> of the way up, over a radius of <paramref name="r"/>.</summary>
		static float Bulge( float r, float t ) => r - 1.4f + 1.9f * MathF.Sin( t * MathF.PI );

		void BarrelAt( Vector3 foot, float r, float h, Rotation rotation )
		{
			var profile = new[] { new Vector2( r - 1.4f, 0f ), new Vector2( Bulge( r, 0.25f ), h * 0.25f ), new Vector2( Bulge( r, 0.5f ), h * 0.5f ), new Vector2( Bulge( r, 0.75f ), h * 0.75f ), new Vector2( r - 1.4f, h ) };
			Kit.SmoothLathe( T( StaveSwatch ), At( foot ), profile, 7, rotation, false, true, 50f, Next() );
			foreach ( var t in new[] { 0.13f, 0.87f } )
			{
				var rr = Bulge( r, t ) + 0.3f;
				Kit.SmoothLathe( P( Paint.Iron ), At( foot ), new[] { new Vector2( rr, h * t - 0.9f ), new Vector2( rr, h * t + 0.9f ) }, 7, rotation, false, false );
			}
		}

		void Barrel( float u )
		{
			var r = Range( 10f, 12f );
			Claim( u, r * 2f + 2f );
			BarrelAt( new Vector3( u, Back - r - 1f, Ledge ), r, r * Range( 2.5f, 2.75f ), Yaw( Next() * 360f ) );
		}

		/// <summary>Two water barrels on end and a third laid along their tops: a player's chest high.</summary>
		void BarrelStack( float u )
		{
			const float R = 10.4f;
			var h = R * Range( 2.5f, 2.7f );
			Claim( u, 46f );
			Hang( u, 46f, h + R * 2f );
			_tall = true;
			var y = Back - R - 1f;
			foreach ( var s in new[] { -1f, 1f } )
				BarrelAt( new Vector3( u + s * (R + 0.8f) + Range( -0.6f, 0.6f ), y, Ledge ), R, h, Yaw( Next() * 360f ) );
			var length = R * Range( 2.4f, 2.6f );
			BarrelAt( new Vector3( u - length * 0.5f + Range( -2f, 2f ), y, Ledge + h + R - 1.6f ), R, length, Lying );
		}

		void Crate( float u, float v, float size, float yaw )
		{
			var a = yaw * MathF.PI / 180f;
			var reach = size * 0.5f * (MathF.Abs( MathF.Cos( a ) ) + MathF.Abs( MathF.Sin( a ) ));
			Kit.BevelBox( T( PlankSwatch ), At( new Vector3( u, Back - reach - 0.5f, v + size * 0.5f ) ), new Vector3( size, size, size ), 0.8f, Yaw( yaw ) );
		}

		void CrateStack( float u, bool high )
		{
			Claim( u, 28f );
			var low = Range( 21f, 24f );
			Crate( u + Range( -1.5f, 1.5f ), Ledge, low, Range( -8f, 8f ) );
			var second = Range( 16f, 20f );
			Crate( u + Range( -3f, 3f ), Ledge + low, second, Range( -12f, 12f ) );
			var top = low + second;
			if ( high )
			{
				var third = Range( 13f, 16f );
				Crate( u + Range( -3f, 3f ), Ledge + top, third, Range( -12f, 12f ) );
				top += third;
				_tall = true;
			}
			if ( top > 40f ) Hang( u, 26f, top );
		}

		/// <summary>Two or three sacks of charcoal slumped against the wall.</summary>
		void Sacks( float u )
		{
			Claim( u, 30f );
			var skin = T( SackSwatch );
			for ( var k = 0; k < 2; k++ )
			{
				var at = new Vector3( u + (k == 0 ? -7f : 7f) + Range( -1f, 1f ), Back - 8f, Ledge + 8.5f );
				Kit.Ellipsoid( skin, At( at ), new Vector3( Range( 6.5f, 7.5f ), 6.5f, Range( 8.5f, 10f ) ), Yaw( Next() * 360f ) * Rotation.FromPitch( Range( -8f, 8f ) ), 3, 6 );
				Kit.SmoothLathe( P( Paint.Rope ), At( at + new Vector3( 0f, 0f, 7.8f ) ), new[] { new Vector2( 2f, 0f ), new Vector2( 1.4f, 1.6f ), new Vector2( 2.2f, 3.4f ), new Vector2( 0f, 4.2f ) }, 5, Run, false, false );
			}
			if ( Next() < 0.7f )
				Kit.Ellipsoid( skin, At( new Vector3( u + Range( -3f, 3f ), Back - 9f, Ledge + 18f ) ), new Vector3( 10f, 6f, 5.5f ), Yaw( Range( -15f, 15f ) ), 3, 6 );
		}

		void Bucket( float u )
		{
			Claim( u, 13f );
			var foot = new Vector3( u, Back - 7f, Ledge );
			Kit.SmoothLathe( T( StaveSwatch ), At( foot ), new[] { new Vector2( 5f, 0f ), new Vector2( 5.7f, 6f ), new Vector2( 6.2f, 11f ), new Vector2( 5.6f, 11f ), new Vector2( 5.2f, 9.5f ) }, 8, Run, true, false, 40f, Next() );
			Kit.SmoothLathe( P( Paint.Iron ), At( foot ), new[] { new Vector2( 5.9f, 7.4f ), new Vector2( 6f, 8.6f ) }, 8, Run, false, false );
			var a = Next() * MathF.PI;
			var side = new Vector3( MathF.Cos( a ), MathF.Sin( a ) * 0.4f, 0f );
			var path = new Vector3[7];
			for ( var i = 0; i < 7; i++ )
			{
				var t = i / 6f;
				path[i] = foot + side * (6f * MathF.Cos( t * MathF.PI )) + new Vector3( 0f, 0f, 11f + MathF.Sin( t * MathF.PI ) * 5f );
			}
			Kit.Tube( P( Paint.Iron ), At( path ), 0.3f, 3 );
		}

		/// <summary>
		/// A shelf unit on the wall from <paramref name="v"/>: two plank shelves between side boards, set with ingots, pots
		/// and horseshoes — a box with things in it, which reads from the room where one plank on brackets read as a line.
		/// Three a wall at most.
		/// </summary>
		void Shelf( float u, float width, float v )
		{
			_shelves++;
			const float D = 10f;
			const float Tier = 17f;
			var y = Face - D * 0.5f;
			var wood = Any( Paint.Oak, Paint.Charred, Paint.DarkWood );
			Hang( u, width + 2f, v + Tier + 12f );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( wood, At( new Vector3( u + s * (width * 0.5f - 0.8f), y, v + Tier * 0.5f + 2f ) ), new Vector3( 1.6f, D, Tier + 8f ), Run );
			for ( var tier = 0; tier < 2; tier++ )
			{
				var level = v + tier * Tier;
				Kit.BevelBox( wood, At( new Vector3( u, y, level - 1.2f ) ), new Vector3( width, D, 2.4f ), 0.4f, Run );
				var at = u - width * 0.5f + Range( 3f, 5f );
				while ( at < u + width * 0.5f - 6f )
				{
					var foot = new Vector3( at, y + Range( -1f, 1f ), level );
					var roll = Next();
					if ( roll < 0.4f )
					{
						for ( var k = 0; k < 2 + Pick( 3 ); k++ )
							Kit.Box( Any( Paint.Rubbed, Paint.Bronze, Paint.Copper ), At( foot + new Vector3( 0f, 0f, 1.1f + k * 2.2f ) ), new Vector3( 8f, 4.4f, 2.2f ), Yaw( Range( -10f, 10f ) ) );
						at += 11f;
					}
					else if ( roll < 0.75f )
					{
						var size = Range( 1.3f, 1.7f );
						Kit.SmoothLathe( Any( Paint.Clay, Paint.Copper, Paint.Pewter ), At( foot ), new[]
						{
							new Vector2( 1.8f * size, 0f ), new Vector2( 3.4f * size, 2.6f * size ), new Vector2( 2.6f * size, 6.2f * size ), new Vector2( 2.2f * size, 7.4f * size ),
						}, 5, Run, true, false, 50f, Next() );
						at += 7f * size + 3f;
					}
					else { Horseshoe( foot, Range( 0f, MathF.Tau ), true, 1.3f ); at += 10f; }
				}
			}
		}

		/// <summary>
		/// A chain along <paramref name="path"/>: oval links of about <paramref name="link"/> each, every other one turned
		/// a quarter round — four straight runs of a three-sided rod per link.
		/// </summary>
		void Chain( Vector3[] path, float link )
		{
			var length = 0f;
			for ( var i = 1; i < path.Length; i++ ) length += (path[i] - path[i - 1]).Length;
			var count = Math.Max( 1, (int)(length / (link * 0.78f)) );
			var iron = P( Paint.Iron );
			for ( var k = 0; k < count; k++ )
			{
				var centre = Along( path, (k + 0.5f) / count );
				var dir = (Along( path, MathF.Min( (k + 1f) / count, 1f ) ) - Along( path, (float)k / count )).Normal;
				var side = MathF.Abs( dir.y ) < 0.9f ? Vector3.Cross( dir, Into ).Normal : Vector3.Forward;
				if ( k % 2 == 1 ) side = Vector3.Cross( dir, side ).Normal;
				var a = dir * (link * 0.5f); var b = side * (link * 0.28f);
				Kit.Tube( iron, At( new[] { centre - a - b, centre + a - b, centre + a + b, centre - a + b, centre - a - b } ), 0.32f, 3 );
			}
		}

		// ---- Over the heads of the room ---------------------------------------------------------------

		/// <summary>
		/// Dresses the stretch over the heads of the room in <paramref name="bay"/>: <paramref name="upper"/> toward one end,
		/// <paramref name="second"/> in the middle of what it leaves.
		/// </summary>
		public void Above( Upper upper, Second second, Bay bay ) => Apart( () =>
		{
			var left = Next() < 0.5f;
			var span = upper switch { Upper.Hoops => 52f, Upper.Horseshoes => 54f, Upper.Hooks => 70f, Upper.Blades => 58f, Upper.Wheel => 50f, _ => 0f };
			span = MathF.Min( span, bay.Width * 0.5f );
			float near, far;
			if ( upper == Upper.Crane )
			{
				var reach = Crane( bay, left );
				(near, far) = left ? (bay.From, bay.From + reach + 10f) : (bay.To - reach - 10f, bay.To);
			}
			else
			{
				var u = left ? bay.From + 8f + span * 0.5f + Range( 0f, 8f ) : bay.To - 8f - span * 0.5f - Range( 0f, 8f );
				switch ( upper )
				{
					case Upper.Hoops: Hoops( u ); break;
					case Upper.Horseshoes: ShoeBoard( u, span - 4f ); break;
					case Upper.Hooks: HookRail( u, span - 6f, Range( 206f, 214f ), true ); break;
					case Upper.Blades: Blades( u, span - 4f, Range( 158f, 166f ) ); break;
					default: Wheel( u ); break;
				}
				(near, far) = (u - span * 0.5f, u + span * 0.5f);
			}

			var (from, to) = left ? (far + 8f, bay.To - 8f) : (bay.From + 8f, near - 8f);
			var free = to - from;
			var at = (from + to) * 0.5f + Range( -0.1f, 0.1f ) * free;
			switch ( second )
			{
				case Second.Shelf when free >= 46f: HighShelf( at, MathF.Min( free - 4f, Range( 50f, 84f ) ), Range( 150f, 168f ) ); break;
				case Second.Sign when free >= 44f: Sign( at, Range( 190f, 200f ) ); break;
				case Second.Bars when free >= 50f: Bars( at, MathF.Min( free - 4f, Range( 60f, 100f ) ), Range( 160f, 176f ) ); break;
				default:
					if ( free >= 30f ) Vent( at, MathF.Min( free - 4f, Range( 30f, 40f ) ), Range( 166f, 186f ) );
					break;
			}
		} );

		/// <summary>A long plank shelf high on the wall on three brackets, set with crocks, ingots and a ladle.</summary>
		void HighShelf( float u, float length, float v )
		{
			const float D = 11f;
			var y = Face - D * 0.5f;
			Kit.BevelBox( Any( Paint.Oak, Paint.Pine ), At( new Vector3( u, y, v - 1f ) ), new Vector3( length, D, 2f ), 0.4f, Run );
			foreach ( var t in new[] { -0.42f, 0f, 0.42f } )
				Kit.Box( P( Paint.Iron ), At( new Vector3( u + t * length, Face - D * 0.45f, v - 3.2f ) ), new Vector3( 1.4f, D * 0.9f, 1.2f ), Turn( Rotation.FromAxis( Vector3.Forward, -28f ) ) );
			var at = u - length * 0.5f + Range( 4f, 7f );
			while ( at < u + length * 0.5f - 6f )
			{
				var foot = new Vector3( at, y + Range( -1f, 1f ), v );
				var scale = Range( 1.3f, 1.9f );
				if ( Next() < 0.7f )
				{
					Kit.SmoothLathe( Any( Paint.Clay, Paint.Copper, Paint.Pewter, Paint.Clay ), At( foot ), new[]
					{
						new Vector2( 1.8f * scale, 0f ), new Vector2( 3.2f * scale, 1.8f * scale ), new Vector2( 3.4f * scale, 4.4f * scale ), new Vector2( 2.2f * scale, 6.4f * scale ), new Vector2( 2.4f * scale, 7f * scale ),
					}, 6, Run, true, false, 50f, Next() );
					at += 7f * scale + Range( 2f, 5f );
				}
				else
				{
					for ( var k = 0; k < 2 + Pick( 2 ); k++ )
						Kit.Box( Any( Paint.Rubbed, Paint.Bronze, Paint.Copper ), At( foot + new Vector3( 0f, 0f, 1.1f + k * 2.2f ) ), new Vector3( 8f, 4f, 2.2f ), Yaw( Range( -10f, 10f ) ) );
					at += Range( 12f, 15f );
				}
			}
		}

		/// <summary>An iron vent high in the wall: a frame round louvred slats over a dark opening.</summary>
		void Vent( float u, float width, float v )
		{
			var h = width * Range( 0.7f, 0.9f );
			var iron = P( Paint.Iron );
			Room.WallPicture( Side, _trim, SolidSwatch( (int)Paint.Black ), u - width * 0.5f, u + width * 0.5f, v, v + h, Face - 0.3f, false );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.Box( iron, At( new Vector3( u + s * (width * 0.5f - 1.2f), Face - 1.5f, v + h * 0.5f ) ), new Vector3( 2.4f, 3f, h ), Run );
				Kit.Box( iron, At( new Vector3( u, Face - 1.5f, v + h * 0.5f + s * (h * 0.5f - 1.2f) ) ), new Vector3( width, 3f, 2.4f ), Run );
			}
			var slats = (int)(h / 5f);
			for ( var k = 1; k < slats; k++ )
				Kit.Box( P( Paint.Rubbed ), At( new Vector3( u, Face - 2f, v + k * h / slats ) ), new Vector3( width - 4f, 0.6f, 3.2f ), Turn( Rotation.FromAxis( Vector3.Forward, 35f ) ) );
		}

		/// <summary>The smithy's sign on an iron arm: a board hung on two rods, a horseshoe and a pair of crossed hammers on it. Once a wall.</summary>
		void Sign( float u, float v )
		{
			var iron = P( Paint.Iron );
			const float Y = 18f;
			Kit.Box( iron, At( new Vector3( u, Face - 0.6f, v + 2f ) ), new Vector3( 5f, 1.2f, 8f ), Run );
			Kit.Tube( iron, At( new[] { new Vector3( u, Face - 1f, v + 4f ), new Vector3( u, Y - 4f, v + 4f ) } ), 0.7f, 4 );
			Kit.Tube( iron, At( new[] { new Vector3( u, Face - 1f, v - 4f ), new Vector3( u, (Face + Y) * 0.5f, v + 3.5f ) } ), 0.5f, 4 );
			var board = v - 6f;
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Tube( iron, At( new[] { new Vector3( u + s * 14f, Y, v + 4f ), new Vector3( u + s * 14f, Y, board ) } ), 0.35f, 3 );
			Kit.BevelBox( T( PlankSwatch ), At( new Vector3( u, Y, board - 10f ) ), new Vector3( 36f, 2f, 20f ), 0.5f, Run );
			Horseshoe( new Vector3( u, Y - 1.4f, board - 5f ), 0f, false );
			foreach ( var s in new[] { -1f, 1f } )
			{
				var spin = Rotation.FromAxis( Into, s * 40f );
				Kit.Box( P( Paint.Charred ), At( new Vector3( u, Y - 1.3f, board - 13f ) + spin * new Vector3( 0f, 0f, -1f ) ), new Vector3( 1.2f, 0.6f, 14f ), Turn( spin ) );
				Kit.Box( iron, At( new Vector3( u, Y - 1.5f, board - 13f ) + spin * new Vector3( 0f, 0f, 6.4f ) ), new Vector3( 5f, 1f, 2.2f ), Turn( spin ) );
			}
		}

		/// <summary>Bar stock laid along the wall on iron brackets, five or six lengths of every gauge.</summary>
		void Bars( float u, float length, float v )
		{
			var iron = P( Paint.Iron );
			foreach ( var t in new[] { -0.4f, 0f, 0.4f } )
				Kit.Box( iron, At( new Vector3( u + t * length, Face - 4f, v - 1f ) ), new Vector3( 1.6f, 8f, 1.6f ), Run );
			for ( var k = 0; k < 5 + Pick( 2 ); k++ )
			{
				var r = Range( 0.5f, 1.1f );
				var y = Face - 1.6f - k * 1.3f;
				var z = v + r + Range( 0f, 1.5f ) + (k % 2) * 1.6f;
				var shift = Range( -6f, 6f );
				Kit.Tube( Any( Paint.Iron, Paint.Rubbed, Paint.Rust, Paint.Rubbed ), At( new[] { new Vector3( u - length * 0.5f + shift, y, z ), new Vector3( u + length * 0.5f + shift, y, z ) } ), r, 4 );
			}
		}

		/// <summary>Two or three iron tyres hung on a peg, overlapping, each its own size.</summary>
		void Hoops( float u )
		{
			var peg = Range( 210f, 218f );
			var iron = P( Paint.Iron );
			Kit.Box( iron, At( new Vector3( u, Face - 4f, peg ) ), new Vector3( 1.6f, 8f, 1.6f ), Run );
			var count = 2 + Pick( 2 );
			for ( var k = 0; k < count; k++ )
			{
				var r = Range( 16f, 25f );
				var lean = Range( -0.18f, 0.18f );
				var centre = new Vector3( u + lean * r, Face - 2.4f - k * 1.6f, peg - r + 0.6f );
				Kit.Tube( Any( Paint.Iron, Paint.Rubbed, Paint.Rust ), At( Loop( centre, Vector3.Forward, new Vector3( 0f, -0.08f, 1f ).Normal, r, 14 ) ), 0.9f, 4 );
			}
		}

		/// <summary>A board of horseshoes nailed in two rows, the smith's sign of his trade.</summary>
		void ShoeBoard( float u, float width )
		{
			var v = Range( 168f, 178f );
			Kit.BevelBox( Any( Paint.Charred, Paint.DarkWood ), At( new Vector3( u, Face - 0.8f, v + 13f ) ), new Vector3( width, 1.6f, 26f ), 0.3f, Run );
			for ( var row = 0; row < 2; row++ )
			{
				var at = u - width * 0.5f + Range( 4f, 6f );
				while ( at < u + width * 0.5f - 4f )
				{
					Horseshoe( new Vector3( at, Face - 2f, v + 20f - row * 11f ), Range( -0.2f, 0.2f ), false, 1.25f );
					at += Range( 9f, 11f );
				}
			}
		}

		/// <summary>An old cartwheel hung on the wall for its tyre: an iron-shod rim, spokes, a hub. One a wall.</summary>
		void Wheel( float u )
		{
			var r = Range( 20f, 24f );
			var centre = new Vector3( u, Face - 3f, Range( 182f, 192f ) );
			var wood = Any( Paint.Oak, Paint.Charred );
			var up = new Vector3( 0f, -0.06f, 1f ).Normal;
			Kit.Tube( P( Paint.Rubbed ), At( Loop( centre, Vector3.Forward, up, r, 14 ) ), 1.2f, 4 );
			Kit.Tube( wood, At( Loop( centre + new Vector3( 0f, 0.6f, 0f ), Vector3.Forward, up, r - 2f, 14 ) ), 1.2f, 4 );
			Kit.SmoothLathe( wood, At( centre + new Vector3( 0f, 1.5f, 0f ) ), new[] { new Vector2( 3.6f, 0f ), new Vector2( 3.8f, 3f ), new Vector2( 2.4f, 5.5f ) }, 8, Facing, false, true );
			var turn = Next() * MathF.Tau;
			for ( var k = 0; k < 10; k++ )
			{
				var a = turn + k * MathF.Tau / 10f;
				var dir = Vector3.Forward * MathF.Cos( a ) + up * MathF.Sin( a );
				Kit.Tube( wood, At( new[] { centre + dir * 3.4f, centre + dir * (r - 2.6f) } ), 0.7f, 4 );
			}
		}

		/// <summary>A timber jib crane at one end of the bay: a post from the floor to the cornice, an arm with its brace, a chain and hook. One a wall.</summary>
		float Crane( Bay bay, bool left )
		{
			var end = left ? -1f : 1f;
			var post = end < 0f ? bay.From + 2.6f : bay.To - 2.6f;
			var wood = P( Paint.Oak );
			const float Y = Deep - 3f;
			Kit.Box( wood, At( new Vector3( post, Y, (Ledge + Beam) * 0.5f ) ), new Vector3( 5f, 5f, Beam - Ledge ), Run );
			var arm = Range( 206f, 214f );
			var reach = MathF.Min( bay.Width - 30f, Range( 60f, 80f ) );
			var tip = post - end * reach;
			Kit.Box( wood, At( new Vector3( (post + tip) * 0.5f, Y - 6f, arm ) ), new Vector3( reach, 4f, 4f ), Run );
			var brace = new Vector3( post - end * reach * 0.45f, Y - 6f, arm - 2f );
			var foot = new Vector3( post, Y - 6f, arm - reach * 0.4f );
			var dir = (brace - foot).Normal;
			Kit.Box( wood, At( (brace + foot) * 0.5f ), new Vector3( 3f, 3f, (brace - foot).Length ), Axis( dir ) );
			Kit.Box( P( Paint.Iron ), At( new Vector3( tip + end * 6f, Y - 6f, arm - 3.4f ) ), new Vector3( 6f, 5f, 3f ), Run );
			var hang = new Vector3( tip + end * 6f, Y - 6f, arm - 5f );
			var low = arm - Range( 40f, 56f );
			Chain( new[] { hang, hang.WithZ( low + 4f ) }, 4.4f );
			Kit.Tube( P( Paint.Iron ), At( new[] { hang.WithZ( low + 4f ), hang.WithZ( low ), hang.WithZ( low ) + new Vector3( 2f, 0f, -2.6f ), hang.WithZ( low ) + new Vector3( 4f, 0f, -1.2f ), hang.WithZ( low ) + new Vector3( 4.2f, 0f, 1f ) } ), 0.7f, 4 );
			return reach;
		}
	}
}
