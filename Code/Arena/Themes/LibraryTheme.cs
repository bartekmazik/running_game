using System;
using System.Collections.Generic;

/// <summary>
/// Floor 5 (index 4): a library. Underfoot, oak herringbone parquet blocks — every plank its own
/// tone, hue and grain, a polished finish worn dull along the walking paths, fine scratches, and a
/// small rosewood inlay pin on one block. Overhead (the undersides of the office floor), dark walnut
/// coffers: mitred beam frames, stepped mouldings, a fielded panel in every bay.
/// </summary>
public sealed class LibraryTheme : ArenaTheme
{
	public override string Name => "Library";
	public override int Floor => 4;
	public override Color LightColor => new( 1.30f, 0.95f, 0.66f );

	static Color Oak => Palette.Hex( 0xB98A62 );
	static Color OakLight => Palette.Hex( 0xCFA27A );
	static Color OakShade => Palette.Hex( 0x9C7050 );

	/// <summary>Late-wood lines of the grain: the same oak, denser and darker.</summary>
	static Color LateWood => Palette.Hex( 0x8A5F40 );

	/// <summary>Planks cut from yellower and from rosier stock — the rose leaning toward the floor's identity hue.</summary>
	static Color Honey => Palette.Hex( 0xC39556 );
	static Color Rosy => Palette.Hex( 0xB07A68 );

	/// <summary>Where the finish has worn through: paler, greyer, duller bare oak.</summary>
	static Color Bare => Palette.Hex( 0xC4A68A );

	/// <summary>Dirt packed in the gaps between planks and between blocks.</summary>
	static Color Gap => Palette.Hex( 0x4E3527 );

	/// <summary>Inlay: rosewood, worn toward the oak around it.</summary>
	static Color Rose => Palette.Hex( 0xC99BB0 );
	static Color Rosewood => Palette.Hex( 0x7A4A44 );

	static Color Walnut => Palette.Hex( 0x765443 );
	static Color WalnutLight => Palette.Hex( 0x8E6A52 );
	static Color WalnutDeep => Palette.Hex( 0x4D3528 );
	static Color WalnutGrain => Palette.Hex( 0x3E2A20 );

	/// <summary>The bookcases' boarded back, in shadow behind the books.</summary>
	static Color CaseBack => Palette.Hex( 0x3A2820 );

	/// <summary>Gilt tooling on the spines: old gold, worn soft.</summary>
	static Color Gilt => Palette.Hex( 0xB89D62 );

	/// <summary>Dust settled along the front of the shelves.</summary>
	static Color Dust => Palette.Hex( 0x9A8E80 );

	/// <summary>One bookcase per module, so every length of wall starts on a case's upright and no bay ends in a sliver of a case.</summary>
	protected override float WallModule => WallRepeat / Cases;

	/// <summary>Bookcases to a wall repeat: each about 54 units, a 1.35 m case.</summary>
	const int Cases = 5;

	/// <summary>
	/// Shelves in a case, the plinth to the crown rail divided among them: seven at about 32 units, a
	/// shelf of books at their real size. A taller room gets more shelves, never taller books.
	/// </summary>
	const int Shelves = 7;

	/// <summary>
	/// Three walls of five bookcases each, worn by the lengths of wall either way round (six looks, never
	/// the one the length before wore): with one, every bay — three and a half to four and a half cases
	/// wide — showed the same four cases in the same order, and a distinctive shelf came round about
	/// thirty times (6b review); with three and no mirror, the lengths ran A-B-C-A-B-C.
	/// </summary>
	public override int WallCount => 3;

	/// <summary>A bookcase has no handedness: mirrored, its books stand in the other order.</summary>
	protected override bool MirrorWalls => true;

	/// <summary>The deep shadow a shelf throws over the books under it, and the dark of a leather band.</summary>
	static Color ShelfShadow => Palette.Hex( 0x1E1512 );

	/// <summary>The faded colour a spine's head turns where the light has reached it for years.</summary>
	static Color Faded => Palette.Hex( 0xCFC3AE );

	/// <summary>Paper labels on cloth spines.</summary>
	static Color Label => Palette.Hex( 0xDCD0B6 );

	/// <summary>The dusty brown every binding drifts toward with age.</summary>
	static Color AgedBinding => Palette.Hex( 0x8A7A6A );

	/// <summary>Plank width in pixels (about 7 units, an 18 cm plank) and its length in widths.</summary>
	const int PlankWidth = 64;
	const int PlankRatio = 4;

	/// <summary>
	/// The structural front layer — fluted walnut pilasters, a dentilled cornice — and then the dressing.
	/// Two walls carry a brass rail along their bookcases with rolling ladders hooked on it; one wall is
	/// the gallery, a framed map or portrait hung on cords over the upper shelves of every bay. Every bay
	/// gets a brass classification plate on the cornice and a set of furniture in front of its cases,
	/// dealt so that no two neighbours match: a writing desk with its chair and green banker's lamp, a
	/// leather armchair by a lamp table under a standard lamp, a lectern by a floor globe, a plaster or
	/// bronze bust on a pedestal, a card catalogue with a kick stool, a book trolley, a painting on an easel —
	/// and on two walls a longcase clock (no two bays either side of a corner match either); whatever a set
	/// leaves bare is filled with something a head high (a standard lamp, and once a wall an armillary sphere
	/// or a hat stand), benches, kick stools, an umbrella stand and piles of books. Pale oak and painted pieces,
	/// parchment, white plaster and lit shades among the walnut, so the sets read against the walnut cases.
	/// Twin-armed brass sconces on the pilasters; the fourth wall, with neither rail nor gallery, has its own
	/// upper layer — busts, urns and globes on corbels, picture lights over the cases, a reading-room clock. The room's
	/// five lamps burn in two-tiered brass chandeliers hung from the cornice. Each wall draws from its own stream.
	/// </summary>
	public override void Dress( FloorContext room )
	{
		var bays = BuildFrontLayer( room, new FrontStyle { Post = 30f, Pier = 78f, Corner = 32f, Cornice = 30f, Skirting = 14f, SkirtingDepth = 6f } );

		// The flames are an exponent (2^b): at half the paint the warm glass keeps its amber under bloom.
		Glow.Set( "g_flSelfIllumBrightness", -1f );
		var props = new Props( room, Trim, Glow );
		var lamps = LampSpots( 5, Seed + 900 );

		var walls = new Random( Seed + 1700 );
		var order = new List<int> { 0, 1, 2, 3 };
		for ( var k = order.Count - 1; k > 0; k-- )
		{
			var j = walls.Next( k + 1 );
			(order[k], order[j]) = (order[j], order[k]);
		}
		var clock = walls.Next( 4 );
		var otherClock = (clock + 1 + walls.Next( 3 )) % 4;

		// The walls go round the room in the order their ends meet (+X, −Y, −X, +Y: each wall's far end is the
		// next one's start), so the deal can keep the two bays either side of a corner apart as well.
		var corners = new List<Props.Look>();
		foreach ( var side in new[] { 0, 3, 2, 1 } )
		{
			props.Begin( side, Seed + 1000 + side * 137 );
			props.StartWall();
			var all = bays.FindAll( b => b.Side == side );
			var wall = all.FindAll( b => b.Width > 60f );
			var ladders = side == order[0] || side == order[1];
			var sideLamps = lamps.FindAll( l => l.Side == side ).ConvertAll( l => l.U );
			props.Plates( wall, sideLamps );
			if ( ladders ) props.Rails( wall );
			if ( side == order[2] ) props.Gallery( wall, sideLamps );

			// Ladders in two or three bays of a rail wall, never in two bays running.
			var ladderBays = new List<int>();
			if ( ladders )
			{
				var places = new List<int>();
				for ( var k = 0; k < wall.Count; k++ ) places.Add( k );
				props.Shuffle( places );
				var want = 2 + (props.Next() < 0.5f ? 1 : 0);
				foreach ( var k in places )
					if ( ladderBays.Count < want && !ladderBays.Contains( k - 1 ) && !ladderBays.Contains( k + 1 ) ) ladderBays.Add( k );
			}

			// The sets, dealt in wall order so that no two neighbours match; on two walls one bay picked
			// from the wall's stream holds the longcase clock instead, which matches nothing either.
			var looks = props.Deal( new List<Props.Look>
			{
				Props.Look.Desk, Props.Look.Reading, Props.Look.Lectern, Props.Look.Bust, Props.Look.Catalogue, Props.Look.Trolley, Props.Look.Easel,
			}, wall.Count, ( slot, look ) => !(slot == 0 && corners.Count > 0 && look == corners[^1]) && !(slot == wall.Count - 1 && side == 1 && corners.Count > 0 && look == corners[0]) );
			if ( side == clock || side == otherClock ) looks[(int)(props.Next() * wall.Count)] = Props.Look.Clock;

			if ( corners.Count == 0 ) corners.Add( looks[0] );
			corners.Add( looks[^1] );

			for ( var k = 0; k < wall.Count; k++ )
				props.Furnish( looks[k], wall[k], ladderBays.Contains( k ) );

			props.Sconces( all, sideLamps );
			if ( side == order[3] ) props.UpperLayer( all, sideLamps, !looks.Contains( Props.Look.Clock ) );
		}

		props.Begin( 0, Seed + 1900 );
		foreach ( var lamp in lamps )
		{
			props.Chandelier( lamp.Side, lamp.U );
			AddLamp( room, lamp.Side, lamp.U, Props.FlameV, Props.HangDepth, LightColor * 4.2f * lamp.Brightness );
		}
	}

	/// <summary>
	/// The lit parts: a candle bulb in the flame's amber, a warmer bulb, the inside of a fabric shade lit by
	/// its bulb (softer, as light through linen is), and a parchment shade's warmer glow.
	/// </summary>
	protected override PaintedSurface PaintGlow()
	{
		const int Size = 64;
		var painter = new Painter( Size, Palette.Hex( 0xFFC27A ), 0.3f );
		painter.Fill( 32, 0, 32, 32, Palette.Hex( 0xFFB46A ), roughness: 0.3f );
		painter.Fill( 0, 32, 32, 32, Palette.Hex( 0xF6DDB4 ), roughness: 0.5f );
		painter.Fill( 32, 32, 32, 32, Palette.Hex( 0xF0C896 ), roughness: 0.5f );
		var glow = new float[Size * Size];
		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
			glow[y * Size + x] = y < 32 ? 1f : 0.55f + 0.25f * (Size - y) / 32f;
		painter.AddGlow( glow );
		return painter.Bake( "arena_4_glow", ArenaGenerator.GlowMaterialPath, 1f );
	}

	static Rect FlameGlow( int k ) => new( (k % 2) * 0.5f + 0.06f, 0.06f, 0.38f, 0.38f );
	static Rect ShadeGlow( int k ) => new( (k % 2) * 0.5f + 0.06f, 0.56f, 0.38f, 0.38f );

	// The props' paints in the trim atlas's infill band (see PaintSwatches).
	static Rect MapSwatch( int k ) => Swatch( k * 128, 0, 128, 96 );
	static Rect PortraitSwatch( int k ) => Swatch( 384 + k * 64, 0, 64, 96 );
	static Rect LandscapeSwatch( int k ) => Swatch( 352 + k * 80, 112, 80, 48 );
	static Rect PanoramaSwatch( int k ) => Swatch( k * 128, 128, 128, 32 );
	static Rect GlobeSwatch => Swatch( 256, 112, 64, 32 );
	static Rect DialSwatch => Swatch( 320, 112, 32, 32 );
	static Rect BookSwatch( int k ) => Swatch( k * 16, 112, 16, 16 );
	static Rect PlateSwatch( int k ) => Swatch( 336 + k * 32, 96, 32, 16 );

	/// <summary>One of the plain paints, sixteen pixels square in a row along the band (<see cref="Solids"/>).</summary>
	static Rect SolidSwatch( int k ) => Swatch( k * 16, 96, 16, 16 );
	static Rect BrassSwatch => SolidSwatch( 0 );
	static Rect IronSwatch => SolidSwatch( 1 );
	static Rect LeatherSwatch( int k ) => SolidSwatch( 2 + k );
	static Rect GreenGlassSwatch => SolidSwatch( 4 );
	static Rect LinenSwatch => SolidSwatch( 5 );
	static Rect WoodSwatch( int k ) => SolidSwatch( 6 + k );
	static Rect PagesSwatch => SolidSwatch( 9 );
	static Rect PlasterSwatch => SolidSwatch( 10 );
	static Rect VelvetSwatch => SolidSwatch( 11 );
	static Rect BaizeSwatch => SolidSwatch( 12 );
	static Rect GiltSwatch => SolidSwatch( 13 );
	static Rect ShadowSwatch => SolidSwatch( 14 );
	static Rect BronzeSwatch => SolidSwatch( 15 );
	static Rect MarbleSwatch => SolidSwatch( 16 );
	static Rect ParchmentSwatch => SolidSwatch( 17 );
	static Rect InkSwatch => SolidSwatch( 18 );
	static Rect PorcelainSwatch => SolidSwatch( 19 );
	static Rect WaxSwatch => SolidSwatch( 20 );

	/// <summary>The painted and pale finishes (pale oak, sage paint, cream paint), in the cells after the plates.</summary>
	static Rect FinishSwatch( int k ) => Swatch( 464 + k * 16, 96, 16, 16 );
	static readonly uint[] Finishes = { 0xD8BA8E, 0xA9B59C, 0xE6DCC4 };

	/// <summary>
	/// The plain paints in <see cref="SolidSwatch"/> order: brass, iron, oxblood and bottle-green leather, green
	/// glass, linen, light walnut, dark walnut, oak, page edges, plaster, rose velvet, green baize, gilt, shadow,
	/// bronze, marble, parchment, ink, porcelain, candle wax.
	/// </summary>
	static readonly uint[] Solids =
	{
		0xC6A76A, 0x3A3532, 0x7A3A34, 0x3F5A46, 0x3E7A58, 0xE8D9B8, 0x8E6A52, 0x4D3528, 0xB98A62, 0xE8DDC4, 0xE9E4DA,
		0x9C6878, 0x4E6A4E, 0xB89D62, 0x1E1714, 0x6E5A40, 0xD8D2C8, 0xE6D9BC, 0x2A2430, 0xF0EDE6, 0xEFE6D0,
	};

	/// <summary>The pictures the gallery wall deals from: three maps, two portraits, two landscapes and two panoramas.</summary>
	const int Pictures = 9;

	/// <summary>Book cloth on the 3D books: the wall's bindings, the last four a shade faded.</summary>
	const int BookCloths = 16;

	static Color Parchment => Palette.Hex( 0xE6D9BC );
	static Color ParchmentShade => Palette.Hex( 0xCDB992 );
	static Color MapSea => Palette.Hex( 0xA8BFBC );
	static Color MapLand => Palette.Hex( 0xDCCB9E );
	static Color MapInk => Palette.Hex( 0x5E4A3C );
	static Color Brass => Palette.Hex( 0xC6A76A );
	static Color BrassShade => Palette.Hex( 0x8E7446 );

	/// <summary>
	/// The props' paints, in the infill band the library hangs no pictures in: the gallery's nine pictures —
	/// three maps (a coastal chart with its compass rose, the world in two hemispheres, an estate plan with
	/// its river), two portraits in oils (a gentleman and a lady), two landscapes and two panoramas (a harbour
	/// with ships, a city across a river) — the globe's oceans and lands, a clock dial, book cloths with gilt
	/// bands, four classification plates, and the plain paints (<see cref="Solids"/>).
	/// </summary>
	static void PaintSwatches( Painter painter, Random random )
	{
		float Next() => (float)random.NextDouble();
		var band = TrimRows( TrimBand.Infill ).Top;

		// ---- Maps -----------------------------------------------------------------------------
		for ( var m = 0; m < 3; m++ )
		{
			var x0 = m * 128; var y0 = band;
			painter.Fill( x0, y0, 128, 96, Parchment, roughness: 0.8f );
			for ( var s = 0; s < 6; s++ )
				painter.Polygon( Organic( new Vector2( x0 + 10f + Next() * 108f, y0 + 10f + Next() * 76f ), new Vector2( 8f + Next() * 14f, 5f + Next() * 9f ), 0.8f, random.Next() ), ParchmentShade, alpha: 0.35f, roughness: 0.85f );
			switch ( m )
			{
				case 0:
				{
					// A coastal chart: sea with soundings, land with hills, a rhumb-line rose.
					painter.Fill( x0 + 6, y0 + 6, 116, 84, MapSea, roughness: 0.8f );
					for ( var k = 0; k < 3; k++ )
					{
						var at = new Vector2( x0 + 20f + k * 34f + Next() * 16f, y0 + 26f + Next() * 44f );
						var shape = Organic( at, new Vector2( 16f + Next() * 14f, 12f + Next() * 14f ), 0.9f, random.Next(), Next() * MathF.Tau );
						painter.Polygon( Organic( at, new Vector2( 19f + Next() * 2f, 15f + Next() * 2f ), 0.9f, random.Next(), Next() * MathF.Tau ), Mix( MapSea, MapInk, 0.25f ), alpha: 0.5f );
						painter.Polygon( shape, MapLand, roughness: 0.8f );
						for ( var h = 0; h < 4; h++ )
						{
							var hill = at + new Vector2( (Next() - 0.5f) * 14f, (Next() - 0.5f) * 10f );
							painter.Line( hill + new Vector2( -3f, 2f ), hill, 0.8f, MapInk, 0.7f );
							painter.Line( hill, hill + new Vector2( 3f, 2f ), 0.8f, MapInk, 0.7f );
						}
					}
					var rose = new Vector2( x0 + 96f + Next() * 10f, y0 + 66f );
					for ( var k = 0; k < 16; k++ )
					{
						var a = k * MathF.Tau / 16f;
						painter.Line( rose, rose + new Vector2( MathF.Cos( a ), MathF.Sin( a ) ) * 60f, 0.5f, MapInk, 0.25f );
					}
					for ( var k = 0; k < 4; k++ )
					{
						var a = k * MathF.PI * 0.5f;
						var tip = rose + new Vector2( MathF.Cos( a ), MathF.Sin( a ) ) * 9f;
						var wing = new Vector2( -MathF.Sin( a ), MathF.Cos( a ) ) * 2.2f;
						painter.Polygon( new[] { rose + wing, tip, rose - wing }, k == 3 ? Palette.Hex( 0x9C5A4E ) : MapInk, alpha: 0.85f );
					}
					for ( var k = 0; k < 20; k++ ) painter.Circle( x0 + 10f + Next() * 108f, y0 + 10f + Next() * 76f, 0.6f, MapInk, 0.5f );
					break;
				}
				case 1:
				{
					// The world in two hemispheres, a graticule over each, a cartouche between them.
					foreach ( var cx in new[] { x0 + 34f, x0 + 94f } )
					{
						var c = new Vector2( cx, y0 + 50f );
						painter.Circle( c.x, c.y, 28f, MapInk, 0.8f );
						painter.Circle( c.x, c.y, 27f, MapSea, roughness: 0.8f );
						for ( var k = 0; k < 3; k++ )
							painter.Polygon( Organic( c + new Vector2( (Next() - 0.5f) * 26f, (Next() - 0.5f) * 26f ), new Vector2( 6f + Next() * 7f, 6f + Next() * 9f ), 0.9f, random.Next(), Next() * MathF.Tau ), MapLand );
						for ( var k = -2; k <= 2; k++ )
						{
							painter.Line( new Vector2( c.x - MathF.Sqrt( 27f * 27f - (k * 9f) * (k * 9f) ), c.y + k * 9f ), new Vector2( c.x + MathF.Sqrt( 27f * 27f - (k * 9f) * (k * 9f) ), c.y + k * 9f ), 0.4f, MapInk, 0.35f );
							painter.Line( new Vector2( c.x + k * 9f, c.y - MathF.Sqrt( 27f * 27f - (k * 9f) * (k * 9f) ) ), new Vector2( c.x + k * 9f, c.y + MathF.Sqrt( 27f * 27f - (k * 9f) * (k * 9f) ) ), 0.4f, MapInk, 0.35f );
						}
					}
					painter.Rect( x0 + 44f, y0 + 6f, 40f, 12f, Mix( Parchment, Brass, 0.3f ), radius: 3f, roughness: 0.7f );
					for ( var k = 0; k < 2; k++ ) painter.Rect( x0 + 50f + k * 4f, y0 + 10f + k * 4f, 28f - k * 8f, 1f, MapInk, alpha: 0.7f );
					break;
				}
				default:
				{
					// An estate plan: a river, roads, field boundaries, the house and its outbuildings, trees.
					var river = new Vector2[9];
					for ( var k = 0; k < river.Length; k++ ) river[k] = new Vector2( x0 + 4f + k * 15f, y0 + 62f + MathF.Sin( k * 0.9f + Next() ) * 9f );
					for ( var k = 1; k < river.Length; k++ ) painter.Line( river[k - 1], river[k], 5f, MapSea, 0.9f );
					for ( var k = 0; k < 5; k++ )
					{
						var a = new Vector2( x0 + 8f + Next() * 112f, y0 + 8f );
						painter.Line( a, a + new Vector2( (Next() - 0.5f) * 40f, 50f ), 0.7f, MapInk, 0.45f );
					}
					painter.Line( new Vector2( x0 + 6f, y0 + 30f + Next() * 10f ), new Vector2( x0 + 122f, y0 + 26f + Next() * 10f ), 2f, Palette.Hex( 0xC2A878 ), 0.9f );
					for ( var k = 0; k < 6; k++ )
						painter.Rect( x0 + 30f + Next() * 70f, y0 + 12f + Next() * 34f, 5f + Next() * 9f, 4f + Next() * 6f, Palette.Hex( 0xA6705E ), alpha: 0.85f, angle: (Next() - 0.5f) * 20f );
					for ( var k = 0; k < 40; k++ ) painter.Circle( x0 + 8f + Next() * 112f, y0 + 8f + Next() * 80f, 1.4f + Next(), Palette.Hex( 0x7E9068 ), 0.75f );
					break;
				}
			}
			painter.Rect( x0 + 3f, y0 + 3f, 122f, 1f, MapInk, alpha: 0.6f );
			painter.Rect( x0 + 3f, y0 + 92f, 122f, 1f, MapInk, alpha: 0.6f );
			painter.Rect( x0 + 3f, y0 + 3f, 1f, 90f, MapInk, alpha: 0.6f );
			painter.Rect( x0 + 124f, y0 + 3f, 1f, 90f, MapInk, alpha: 0.6f );
		}

		// ---- Portraits: oils on a dark ground, the sitter lit from the left ------------------------
		for ( var p = 0; p < 2; p++ )
		{
			var x0 = 384 + p * 64; var y0 = band;
			var ground = p == 0 ? Palette.Hex( 0x3E3A2C ) : Palette.Hex( 0x40302E );
			for ( var row = 0; row < 96; row++ )
				painter.Fill( x0, y0 + row, 64, 1, Mix( Mix( ground, Palette.Hex( 0x7A6A4E ), 0.35f ), ground, row / 96f ), roughness: 0.6f );
			var face = new Vector2( x0 + 31f, y0 + 36f );
			var coat = p == 0 ? Palette.Hex( 0x2A2A30 ) : Palette.Hex( 0xB07A8E );
			painter.Polygon( new[] { new Vector2( x0 + 4f, y0 + 96f ), new Vector2( x0 + 10f, y0 + 62f ), new Vector2( x0 + 24f, y0 + 52f ), new Vector2( x0 + 40f, y0 + 52f ), new Vector2( x0 + 54f, y0 + 62f ), new Vector2( x0 + 60f, y0 + 96f ) }, coat, roughness: 0.6f );
			if ( p == 0 )
			{
				painter.Polygon( new[] { new Vector2( x0 + 25f, y0 + 52f ), new Vector2( x0 + 39f, y0 + 52f ), new Vector2( x0 + 32f, y0 + 72f ) }, Palette.Hex( 0xE8E2D2 ), roughness: 0.6f );
				painter.Circle( face.x, face.y - 9f, 13f, Palette.Hex( 0xB8B2A6 ), roughness: 0.7f );
			}
			else
			{
				painter.Polygon( new[] { new Vector2( x0 + 22f, y0 + 54f ), new Vector2( x0 + 42f, y0 + 54f ), new Vector2( x0 + 38f, y0 + 62f ), new Vector2( x0 + 26f, y0 + 62f ) }, Palette.Hex( 0xE0B49C ), roughness: 0.6f );
				painter.Circle( face.x, face.y - 8f, 13f, Palette.Hex( 0x4A3428 ), roughness: 0.7f );
				painter.Circle( face.x + 2f, face.y - 22f, 7f, Palette.Hex( 0x4A3428 ), roughness: 0.7f );
				for ( var k = 0; k < 9; k++ ) painter.Circle( x0 + 24f + k * 2f, y0 + 56f + MathF.Sin( k / 8f * MathF.PI ) * 3f, 0.9f, Palette.Hex( 0xEDE6D8 ) );
			}
			painter.Rect( face.x - 3.5f, face.y + 4f, 7f, 9f, Palette.Hex( 0xD4A88E ), roughness: 0.6f );
			painter.Polygon( Organic( face, new Vector2( 9f, 12f ), 0.15f, random.Next() ), Palette.Hex( 0xE2B89A ), roughness: 0.55f );
			painter.Polygon( Organic( face + new Vector2( 3.5f, 1f ), new Vector2( 4.5f, 9f ), 0.2f, random.Next() ), Palette.Hex( 0xC09078 ), alpha: 0.45f );
			foreach ( var s in new[] { -1f, 1f } ) painter.Circle( face.x + s * 3.6f, face.y - 1.5f, 1f, Palette.Hex( 0x3A2C26 ) );
			painter.Rect( face.x - 2.5f, face.y + 6f, 5f, 1f, Palette.Hex( 0x9A5A50 ), alpha: 0.8f );
		}

		// ---- Landscapes in oils: hills going blue into the distance, a lake or a river, trees ------
		for ( var l = 0; l < 2; l++ )
		{
			var x0 = 352 + l * 80; var y0 = band + 112;
			var sky = l == 0 ? Palette.Hex( 0xB8C8C4 ) : Palette.Hex( 0xD8C0A0 );
			for ( var row = 0; row < 48; row++ )
				painter.Fill( x0, y0 + row, 80, 1, Mix( sky, Palette.Hex( 0xEADCC0 ), row / 30f ), roughness: 0.6f );
			for ( var c = 0; c < 3; c++ )
				painter.Polygon( Organic( new Vector2( x0 + 10f + Next() * 60f, y0 + 8f + Next() * 8f ), new Vector2( 6f + Next() * 8f, 2f + Next() * 2f ), 0.6f, random.Next() ), Palette.Hex( 0xF2EADA ), alpha: 0.7f );
			var hills = new[] { Palette.Hex( 0x8E9CA4 ), Palette.Hex( 0x7E8C70 ), Palette.Hex( 0x5E6E48 ), Palette.Hex( 0x4A5236 ) };
			for ( var h = 0; h < hills.Length; h++ )
			{
				var top = y0 + 18f + h * 7f;
				var phase = Next() * MathF.Tau;
				var amp = 5f - h;
				var points = new List<Vector2> { new( x0, y0 + 48f ) };
				for ( var x = 0; x <= 80; x += 4 ) points.Add( new Vector2( x0 + x, top + MathF.Sin( x * 0.08f * (1f + h * 0.3f) + phase ) * amp ) );
				points.Add( new Vector2( x0 + 80f, y0 + 48f ) );
				painter.Polygon( points.ToArray(), hills[h], roughness: 0.6f );
			}
			if ( l == 0 ) painter.Polygon( Organic( new Vector2( x0 + 34f + Next() * 12f, y0 + 40f ), new Vector2( 18f, 3.5f ), 0.4f, random.Next() ), Palette.Hex( 0xA8BCC0 ) );
			else for ( var k = 1; k < 8; k++ ) painter.Line( new Vector2( x0 + 30f + k * 3f, y0 + 32f + k * 2.2f ), new Vector2( x0 + 32f + k * 3.6f, y0 + 34f + k * 2.2f ), 1.6f + k * 0.5f, Palette.Hex( 0xA8BCC0 ), 0.9f );
			for ( var t = 0; t < 5; t++ )
			{
				var at = new Vector2( x0 + 6f + Next() * 68f, y0 + 34f + Next() * 10f );
				painter.Polygon( Organic( at, new Vector2( 2.5f + Next() * 2f, 4f + Next() * 3f ), 0.5f, random.Next() ), Palette.Hex( 0x3A4630 ), roughness: 0.6f );
			}
		}

		// ---- Panoramas: a harbour with ships, a city across a river -------------------------------
		for ( var n = 0; n < 2; n++ )
		{
			var x0 = n * 128; var y0 = band + 128;
			for ( var row = 0; row < 32; row++ )
				painter.Fill( x0, y0 + row, 128, 1, Mix( Palette.Hex( 0xE4D8BC ), Palette.Hex( 0xBCC6C0 ), row < 18 ? row / 18f * 0.6f : 0.8f ), roughness: 0.7f );
			if ( n == 0 )
			{
				painter.Fill( x0, y0 + 20, 128, 12, Palette.Hex( 0x8EA4A6 ), roughness: 0.6f );
				for ( var k = 0; k < 3; k++ )
				{
					var x = x0 + 18f + k * 38f + Next() * 12f;
					painter.Polygon( new[] { new Vector2( x - 8f, y0 + 20f ), new Vector2( x + 9f, y0 + 20f ), new Vector2( x + 6f, y0 + 23f ), new Vector2( x - 6f, y0 + 23f ) }, MapInk );
					painter.Rect( x - 0.4f, y0 + 6f, 0.8f, 14f, MapInk );
					painter.Polygon( new[] { new Vector2( x + 0.6f, y0 + 7f ), new Vector2( x + 7f, y0 + 18f ), new Vector2( x + 0.6f, y0 + 18f ) }, Palette.Hex( 0xEFE6D2 ) );
					painter.Polygon( new[] { new Vector2( x - 0.6f, y0 + 9f ), new Vector2( x - 6f, y0 + 18f ), new Vector2( x - 0.6f, y0 + 18f ) }, Palette.Hex( 0xE2D6BE ) );
				}
			}
			else
			{
				for ( var x = x0 + 2f; x < x0 + 126f; )
				{
					var w = 4f + Next() * 7f;
					var h = 4f + Next() * 9f;
					painter.Rect( x, y0 + 20f - h, w, h, Mix( Palette.Hex( 0xA88C74 ), Palette.Hex( 0x8E8E96 ), Next() ), roughness: 0.7f );
					if ( Next() < 0.12f ) painter.Polygon( new[] { new Vector2( x + w * 0.5f - 1f, y0 + 20f - h ), new Vector2( x + w * 0.5f, y0 + 4f + Next() * 4f ), new Vector2( x + w * 0.5f + 1f, y0 + 20f - h ) }, Palette.Hex( 0x7A7480 ) );
					x += w + Next() * 1.5f;
				}
				painter.Fill( x0, y0 + 20, 128, 12, Palette.Hex( 0x98AAA8 ), roughness: 0.6f );
				for ( var k = 0; k < 10; k++ ) painter.Rect( x0 + Next() * 120f, y0 + 22f + Next() * 8f, 4f + Next() * 6f, 0.6f, Palette.Hex( 0xD8D8CC ), alpha: 0.6f );
			}
			painter.Rect( x0 + 3f, y0 + 3f, 122f, 0.8f, MapInk, alpha: 0.5f );
			painter.Rect( x0 + 3f, y0 + 28.2f, 122f, 0.8f, MapInk, alpha: 0.5f );
		}

		// ---- Globe: oceans and lands, an equator and tropics -------------------------------------
		{
			var (x0, y0) = (256, band + 112);
			painter.Fill( x0, y0, 64, 32, Palette.Hex( 0x9DB8B4 ), roughness: 0.4f );
			for ( var k = 0; k < 6; k++ )
				painter.Polygon( Organic( new Vector2( x0 + 6f + k * 10f + Next() * 4f, y0 + 9f + Next() * 14f ), new Vector2( 3f + Next() * 4f, 4f + Next() * 5f ), 0.9f, random.Next() ), Palette.Hex( 0xDCC99A ), roughness: 0.4f );
			painter.Fill( x0, y0 + 16, 64, 1, Palette.Hex( 0x8C5A48 ), roughness: 0.4f );
			foreach ( var row in new[] { 11, 21 } ) painter.Fill( x0, y0 + row, 64, 1, MapInk, roughness: 0.4f );
		}

		// ---- The dial: cream enamel, the hours as strokes inside a chapter ring ---------------------
		{
			var c = new Vector2( 336f, band + 128f );
			painter.Fill( 320, band + 112, 32, 32, Palette.Hex( 0xEDE4CE ), roughness: 0.35f );
			painter.Circle( c.x, c.y, 11.5f, Palette.Ink, 0.8f );
			painter.Circle( c.x, c.y, 10.9f, Palette.Hex( 0xEDE4CE ) );
			for ( var k = 0; k < 12; k++ )
			{
				var a = k * MathF.Tau / 12f;
				var dir = new Vector2( MathF.Cos( a ), MathF.Sin( a ) );
				painter.Line( c + dir * 7.5f, c + dir * 10f, k % 3 == 0 ? 1.4f : 0.8f, Palette.Ink, 0.9f );
			}
		}

		// ---- Solids, each with its own small texture ------------------------------------------------
		var y1 = band + 96;
		for ( var k = 0; k < Solids.Length; k++ )
		{
			var color = Palette.Hex( Solids[k] );
			var roughness = k switch { 0 or 13 or 15 => 0.3f, 4 => 0.12f, 19 => 0.25f, 2 or 3 => 0.45f, 11 or 12 or 5 or 9 => 0.88f, _ => 0.55f };
			painter.Fill( k * 16, y1, 16, 16, color, roughness: roughness );
			for ( var s = 0; s < 6; s++ )
				painter.Circle( k * 16 + Next() * 16f, y1 + Next() * 16f, 0.4f + Next() * 0.4f, Mix( color, Palette.Ink, 0.18f ), 0.35f );
		}
		for ( var y = 0; y < 16; y += 3 ) painter.Fill( 0, y1 + y, 16, 1, Mix( Brass, BrassShade, 0.4f ), roughness: 0.35f );
		painter.Fill( 64, y1 + 4, 16, 2, Palette.Hex( 0x8EBC9C ), roughness: 0.1f );
		for ( var k = 6; k <= 8; k++ )
			for ( var y = 2f; y < 16f; y += 3f + Next() * 1.5f )
				painter.Line( new Vector2( k * 16, y1 + y ), new Vector2( k * 16 + 16, y1 + y + (Next() - 0.5f) * 1.5f ), 0.7f, LateWood, 0.35f );
		for ( var y = 0; y < 16; y += 2 ) painter.Fill( 144, y1 + y, 16, 1, Palette.Hex( 0xD6C8AA ), roughness: 0.88f );
		painter.Fill( 304, y1 + 7, 16, 1, Palette.Hex( 0x8E9CC0 ), roughness: 0.25f );

		// ---- Pale finishes: pale oak with its grain, sage and cream paint with a little wear --------------
		for ( var k = 0; k < Finishes.Length; k++ )
		{
			var color = Palette.Hex( Finishes[k] );
			painter.Fill( 464 + k * 16, y1, 16, 16, color, roughness: k == 0 ? 0.55f : 0.7f );
			for ( var y = 2f; y < 16f; y += 3f + Next() * 1.5f )
				painter.Line( new Vector2( 464 + k * 16, y1 + y ), new Vector2( 480 + k * 16, y1 + y + (Next() - 0.5f) * 1.5f ), 0.6f, Mix( color, LateWood, k == 0 ? 0.35f : 0.12f ), 0.35f );
		}

		// ---- Book cloths: the wall's bindings, a gilt band near head and tail -----------------------
		for ( var k = 0; k < BookCloths; k++ )
		{
			var cloth = k < Bindings.Length ? Bindings[k] : Mix( Bindings[(k * 5) % Bindings.Length], Faded, 0.3f );
			painter.Fill( k * 16, band + 112, 16, 16, cloth, roughness: 0.7f );
			foreach ( var row in new[] { 4, 11 } ) painter.Fill( k * 16, band + 112 + row, 16, 1, Mix( Gilt, cloth, 0.25f ), roughness: 0.35f );
		}

		// ---- Classification plates: bold gilt lettering on a dark ground, each its own line ----------
		for ( var k = 0; k < 4; k++ )
		{
			var x0 = 336 + k * 32;
			painter.Fill( x0, band + 96, 32, 16, Palette.Hex( 0x2A211D ), roughness: 0.5f );
			var x = x0 + 4.5f + Next() * 1.5f;
			while ( x < x0 + 25f )
			{
				var w = 2f + Next() * 2f;
				painter.Rect( x, band + 100.5f, w, 6f, Gilt, roughness: 0.35f );
				x += w + 1.2f + (Next() < 0.25f ? 1.8f : 0f);
			}
		}
	}

	/// <summary>
	/// Built-in walnut bookcases, one per <see cref="WallModule"/>: a plinth, side uprights shared
	/// with the next case, <see cref="Shelves"/> shelves set at their own heights in every case (shelves are moved to
	/// fit what is on them), a dark boarded back. Books are generated per shelf: each its own width,
	/// height, colour from a muted set of bindings (oxblood, bottle green, navy, ochre, vellum, plum and
	/// the floor's own dusty rose), cloth or leather — leather with raised bands and gilt, cloth with a
	/// paper label or a pair of gilt rules — and a rounded spine. Now and then a short run of matching
	/// volumes, a few books lying flat, a gap, and in about half the rows the last book leaning on its
	/// neighbour. Each of the <see cref="WallCount"/> walls draws its cases from its own stream.
	/// Spines fade toward the head, where the light reaches; each shelf throws a shadow over the
	/// tops of the books under it.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		const float Case = WallSize / (float)Cases;
		const float Upright = 3.4f * Ppu;
		const float Shelf = 2.2f * Ppu;
		float Row( float v ) => (WallRepeat - v) * Ppu;
		var painter = new Painter( WallSize, CaseBack, 0.85f );
		var random = new Random( Seed + 600 + variant * 31 );
		float Next() => (float)random.NextDouble();

		// The case back: vertical boards, each its own shade, in deep shadow behind the books.
		for ( var x = 0f; x < WallSize; )
		{
			var board = (9f + Next() * 5f) * Ppu;
			var w = MathF.Min( board, WallSize - x );
			painter.Rect( x + 0.5f, 0f, w - 1f, WallSize, Mix( CaseBack, WalnutGrain, Next() * 0.5f ), height: 0.2f, roughness: 0.85f );
			x += board;
		}

		var shade = new float[WallSize * WallSize];
		for ( var c = 0; c < Cases; c++ )
		{
			var left = c * Case + Upright * 0.5f;
			var right = (c + 1) * Case - Upright * 0.5f;

			// Plinth top and the shelves, every case set at its own heights.
			var plinth = 13f + Next() * 3f;
			var levels = new float[Shelves + 1];
			levels[0] = plinth;
			var pitch = (WallRepeat - 33f - plinth) / Shelves;
			for ( var k = 1; k <= Shelves; k++ ) levels[k] = plinth + k * pitch + (k < Shelves ? (Next() - 0.5f) * 4f : 0f);

			painter.Rect( left, Row( plinth ), right - left, WallSize - Row( plinth ), Walnut, height: 1.2f, bevel: 3f, roughness: 0.55f );

			for ( var k = 0; k < Shelves; k++ )
			{
				var floorV = levels[k] + (k == 0 ? 0f : Shelf / Ppu);
				var clear = levels[k + 1] - floorV;
				ShelfOfBooks( painter, random, left + 2f, right - 2f, Row( floorV ), clear * Ppu );

				// The shelf above throws its shadow over the tops of the books.
				var under = Row( levels[k + 1] ) + Shelf;
				for ( var y = (int)under; y < (int)under + 34 && y < WallSize; y++ )
				{
					var w = 0.55f * (1f - (y - under) / 34f);
					for ( var x = (int)left; x < (int)right; x++ ) shade[y * WallSize + x] = MathF.Max( shade[y * WallSize + x], w * w );
				}
			}

			// Shelves: walnut boards with a lit front arris and a grey line of dust along it.
			for ( var k = 1; k <= Shelves; k++ )
			{
				var top = Row( levels[k] );
				var tone = Mix( Walnut, Next() < 0.5f ? WalnutLight : WalnutDeep, Next() * 0.25f );
				painter.Rect( left - 1f, top, right - left + 2f, Shelf, tone, height: 1.25f, bevel: 3f, roughness: 0.5f );
				painter.Rect( left, top + 1f, right - left, 1.5f, Mix( tone, Dust, 0.5f ), alpha: 0.6f );
			}

			// The crown rail over the top shelf, hidden for the most part behind the cornice beam.
			painter.Rect( left - 1f, 0f, right - left + 2f, Row( WallRepeat - 28f ), WalnutDeep, height: 1.1f, bevel: 3f, roughness: 0.6f );
		}

		painter.Blend( shade, ShelfShadow );

		// The uprights, shared by neighbouring cases, their grain running down them.
		var grain = painter.Noise( 8, Seed + 601 + variant, 2 );
		var lines = new float[WallSize * WallSize];
		for ( var c = 0; c < Cases; c++ )
		{
			var x0 = c * Case - Upright * 0.5f;
			var tone = Mix( Walnut, WalnutLight, Next() * 0.3f );
			painter.Rect( x0, 0f, Upright, WallSize, tone, height: 1.45f, bevel: 4f, roughness: 0.5f );
			for ( var x = (int)x0 + 2; x < (int)(x0 + Upright) - 2; x++ )
			{
				var px = (x + WallSize) % WallSize;
				for ( var y = 0; y < WallSize; y++ )
				{
					// Read down the upright: thin across it, long along it.
					var g = grain[(px * 37 % WallSize) * WallSize + y];
					lines[y * WallSize + px] = g > 0.58f ? MathF.Min( (g - 0.58f) * 3f, 1f ) * 0.35f : 0f;
				}
			}
		}
		painter.Blend( lines, WalnutGrain );

		painter.Mottle( 0.05f, 6, Seed + 602 + variant, 4 );
		painter.RoughnessNoise( 0.05f, 16, Seed + 603 + variant, 2 );
		painter.Cavity( 0.5f, 4 );
		painter.EdgeWear( 0.15f, 2, Seed + 604 + variant );
		painter.Grain( 0.012f, Seed + 605 + variant );
		return painter.Bake( $"arena_4_wall_{variant}", Painter.Matte, 5f );
	}

	/// <summary>
	/// One shelf's books from <paramref name="left"/> to <paramref name="right"/>, standing on pixel
	/// row <paramref name="foot"/> with <paramref name="clear"/> pixels of headroom.
	/// </summary>
	static void ShelfOfBooks( Painter painter, Random random, float left, float right, float foot, float clear )
	{
		const float Ppu = WallSize / WallRepeat;
		float Next() => (float)random.NextDouble();
		var x = left + Next() * 4f;
		var run = 0;
		var runColor = Bindings[0];
		float runHeight = 0f, runWidth = 0f;
		var runLeather = false;

		// About half the rows end in a book leaning on its neighbour where there is room for it to fall;
		// the rest stand straight to the end.
		var leanAtEnd = Next() < 0.5f;

		while ( x < right - 4f )
		{
			// A gap now and then, where a book is out.
			if ( Next() < 0.035f ) { x += (4f + Next() * 9f) * Ppu; continue; }

			// A few books lying flat, the longest at the bottom.
			if ( Next() < 0.04f && right - x > 30f * Ppu )
			{
				var length = (19f + Next() * 6f) * Ppu;
				var y = foot;
				var count = 2 + random.Next( 3 );
				for ( var k = 0; k < count && foot - y < clear - 10f; k++ )
				{
					var thick = (2.6f + Next() * 2.2f) * Ppu;
					var l = length * (1f - k * 0.08f) - Next() * 6f;
					painter.Rect( x + (length - l) * Next(), y - thick, l, thick - 1f, Binding( random ), radius: 1.5f, height: 0.9f, bevel: thick * 0.4f, roughness: 0.7f );
					y -= thick;
				}
				x += length + 2f;
				continue;
			}

			// A short run of matching volumes now and then, otherwise a single book: long runs made
			// landmarks that recurred in every bay.
			if ( run == 0 )
			{
				run = Next() < 0.12f ? 3 + random.Next( 3 ) : 1;
				runColor = Binding( random );
				runHeight = MathF.Min( clear * (0.62f + Next() * 0.3f), clear - 6f );
				runWidth = (3.2f + Next() * Next() * 4.5f) * Ppu;
				runLeather = Next() < 0.3f;
			}
			run--;

			var width = runWidth * (0.94f + Next() * 0.12f);
			var height = MathF.Min( runHeight + (Next() - 0.5f) * 3f, clear - 4f );
			if ( x + width > right ) break;

			// The last book of a row, with room to fall, leans on its neighbour.
			var lean = 0f;
			var room = right - (x + width);
			if ( leanAtEnd && room < width * 1.6f && room > 4f ) lean = 8f + Next() * 12f;
			else if ( Next() < 0.03f ) lean = (Next() - 0.5f) * 10f;

			var tone = Mix( runColor, Next() < 0.5f ? Color.Black : Color.White, Next() * 0.1f );
			PaintBook( painter, random, x, foot, width, height, tone, runLeather, lean );
			x += width + (lean != 0f ? MathF.Abs( MathF.Sin( lean * MathF.PI / 180f ) ) * height : 0.6f + Next() * 1.2f);
		}
	}

	/// <summary>
	/// One book seen by its spine: a rounded spine (the bevel across its width is the curve), faded
	/// toward its head, leather with raised bands and a gilt panel or cloth with a label or gilt
	/// rules. <paramref name="lean"/> in degrees tips its head toward the book on its left (Painter's
	/// angle turns clockwise in the image), the foot sliding out to the right.
	/// </summary>
	static void PaintBook( Painter painter, Random random, float x, float foot, float width, float height, Color color, bool leather, float lean )
	{
		float Next() => (float)random.NextDouble();
		var radians = lean * MathF.PI / 180f;
		var sin = MathF.Abs( MathF.Sin( radians ) ); var cos = MathF.Cos( radians );

		// Centre placed so the lowest corner of the tipped book rests on the shelf.
		var cx = x + width * 0.5f + (lean > 0f ? height * sin * 0.5f : 0f);
		var cy = foot - (sin * width + cos * height) * 0.5f;
		var rough = leather ? 0.55f : 0.78f;
		painter.Rect( cx - width * 0.5f, cy - height * 0.5f, width - 1f, height, color, radius: 1.2f, height: 1f, bevel: MathF.Min( width * 0.45f, 7f ), roughness: rough, angle: lean );

		// The sun-faded head of the spine.
		painter.Rect( cx - width * 0.5f, cy - height * 0.5f, width - 1f, height * 0.3f, Mix( color, Faded, 0.35f ), alpha: 0.25f + Next() * 0.15f, angle: lean );

		if ( lean != 0f ) return;
		var top = cy - height * 0.5f;
		if ( leather )
		{
			var bands = 3 + random.Next( 2 );
			for ( var b = 0; b < bands; b++ )
			{
				var y = top + height * (0.14f + 0.72f * b / (bands - 1));
				painter.Rect( x + 1f, y, width - 3f, 2.4f, Mix( color, ShelfShadow, 0.35f ), height: 1.15f, bevel: 1f, roughness: 0.5f );
			}
			painter.Rect( x + 2f, top + height * 0.22f, width - 5f, height * 0.13f, Gilt, alpha: 0.35f + Next() * 0.2f, roughness: 0.35f );
		}
		else if ( Next() < 0.45f )
		{
			painter.Rect( x + 2f, top + height * (0.12f + Next() * 0.1f), width - 5f, height * 0.12f, Label, alpha: 0.55f + Next() * 0.2f, roughness: 0.85f );
		}
		else
		{
			foreach ( var at in new[] { 0.08f, 0.9f } )
				painter.Rect( x + 1.5f, top + height * at, width - 4f, 1.6f, Gilt, alpha: 0.4f + Next() * 0.2f, roughness: 0.35f );
		}
	}

	/// <summary>A binding from the muted set, a few percent off its neighbours.</summary>
	static Color Binding( Random random ) => Mix( Bindings[random.Next( Bindings.Length )], AgedBinding, (float)random.NextDouble() * 0.15f );

	/// <summary>Muted bindings: oxblood, bottle green, navy, ochre, tan, plum, the floor's dusty rose, vellum, brown calf, teal, maroon, sage.</summary>
	static readonly Color[] Bindings =
	{
		Palette.Hex( 0x7A3E3E ), Palette.Hex( 0x3F5B49 ), Palette.Hex( 0x3E4A66 ), Palette.Hex( 0xA88452 ),
		Palette.Hex( 0xB59A72 ), Palette.Hex( 0x6A4B66 ), Palette.Hex( 0x9C6F7C ), Palette.Hex( 0xD6C8AA ),
		Palette.Hex( 0x6E4C36 ), Palette.Hex( 0x4D6B6C ), Palette.Hex( 0x5E3A34 ), Palette.Hex( 0x7E8A6A ),
	};

	/// <summary>
	/// Walnut mouldings and posts with continuous grain: the cornice's fascia carries a row of dentils,
	/// the posts are fluted pilasters, the skirting a plinth with a bead, the slab edge a walnut nosing.
	/// </summary>
	protected override PaintedSurface PaintTrim()
	{
		return PaintTrimAtlas( "arena_4_trim", WalnutLight, WalnutDeep, 0.5f, 0.3f, Seed + 700, painter =>
		{
			// Dentils along the fascia: blocks stood proud, the gaps between them in shadow.
			var (top, height) = TrimRows( TrimBand.Cornice );
			var y0 = top + height * 0.6f; var y1 = top + height * 0.9f;
			for ( var x = 0; x < TrimSize; x += 16 )
			{
				painter.Rect( x + 3f, y0, 10f, y1 - y0, WalnutLight, alpha: 0f, height: 0.98f, bevel: 1.5f );
				painter.Rect( x - 3f, y0, 6f, y1 - y0, WalnutDeep, alpha: 0.55f, height: 0.62f );
			}

			// Flutes down the posts: five narrow grooves between wider flat fillets, each a round-bottomed
			// cut with crisp shoulders and a dark hollow. Wide sine grooves with no flat between them read
			// as the folds of a curtain (6b review).
			var (ptop, pheight) = TrimRows( TrimBand.Pilaster );
			var relief = new float[TrimSize * TrimSize];
			var hollow = new float[TrimSize * TrimSize];
			for ( var y = 0; y < pheight; y++ )
			{
				var t = (y + 0.5f) / pheight;
				var groove = 0f;
				for ( var f = 0; f < 5; f++ )
				{
					var u = (t - (0.24f + f * 0.115f)) / 0.06f;
					if ( u > 0f && u < 1f ) groove = MathF.Sqrt( MathF.Sin( u * MathF.PI ) );
				}
				for ( var x = 0; x < TrimSize; x++ )
				{
					relief[(ptop + y) * TrimSize + x] = -groove * 0.3f;
					hollow[(ptop + y) * TrimSize + x] = groove * 0.4f;
				}
			}
			painter.AddHeight( relief, 1f );
			painter.Blend( hollow, WalnutDeep );

			PaintSwatches( painter, new Random( Seed + 702 ) );
		} );
	}

	protected override PaintedSurface PaintFloor()
	{
		const int cell = FloorSize / 2;
		var painter = new Painter( FloorSize, Oak, 0.35f );
		var random = new Random( Seed );
		float Next() => (float)random.NextDouble();

		// Each block's outline is set in height first (the parquet's own relief is added on top of it),
		// and the joint at the chamfer's foot is painted last, as the ring between the cell edge and
		// that same outline, so colour and height agree on where the block ends.
		var outlines = new Vector2[4][];
		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			painter.Fill( (int)min.x, (int)min.y, cell, cell, Oak, height: 0.5f );
			outlines[c] = SlabOutline( min, cell, 6f + Next() * 3f, 2f, 5f + Next() * 6f, Seed + c * 101 );
			painter.Polygon( outlines[c], Oak, alpha: 0f, height: 1f, bevel: Rim );
		}

		PaintParquet( painter, random );

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );

			// Fine scratches from chair legs and grit, pale where they cut through the finish.
			var scratches = 18 + random.Next( 18 );
			for ( var s = 0; s < scratches; s++ )
			{
				var at = min + new Vector2( 40f + Next() * 432f, 40f + Next() * 432f );
				var angle = Next() * MathF.Tau;
				var length = 8f + Next() * Next() * 46f;
				var to = at + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * length;
				painter.Line( at, to, 0.7f + Next() * 0.6f, Bare, 0.18f + Next() * 0.2f, roughness: 0.55f );
			}

			// The rosewood inlay pin on variant 3: small, set into a plank, worn into the oak around it.
			if ( c == 3 )
			{
				var at = min + new Vector2( 170f + Next() * 170f, 170f + Next() * 170f );
				var radius = 7f + Next() * 2f;
				painter.Polygon( Organic( at, new Vector2( radius, radius * 0.96f ), 0.1f, Seed + 81, 0f, 24 ), Mix( Mix( Rose, Rosewood, 0.25f ), Oak, 0.35f ), alpha: 0.6f, roughness: 0.4f );
				painter.Polygon( Organic( at + new Vector2( 1.5f, -1f ), new Vector2( radius * 0.6f, radius * 0.45f ), 0.6f, Seed + 82, Next() * MathF.Tau, 16 ), Bare, alpha: 0.25f );
			}

			// The joint between blocks: the ring outside the block outline, dark with packed dirt.
			painter.Polygon( JointRing( min, cell, outlines[c] ), Mix( Gap, OakShade, 0.25f ), alpha: 0.85f, roughness: 0.8f );
		}

		// Traffic: the finish worn dull and paler in broad patches, different on each of the four blocks.
		painter.Blend( Ramp( painter.Noise( 3, Seed + 30, 2 ), 0.55f, 0.85f, 0.45f ), Bare, roughness: 0.62f );

		painter.Mottle( 0.06f, 4, Seed + 31, 3 );
		painter.RoughnessNoise( 0.07f, 8, Seed + 32, 2 );
		painter.Cavity( 0.4f, 4 );
		painter.EdgeWear( 0.3f, 3, Seed + 33 );
		painter.Grain( 0.012f, Seed + 34 );
		return painter.Bake( "arena_4_floor", Painter.Matte, 6f );
	}

	/// <summary>
	/// Herringbone, computed per pixel. In a frame turned 45° the pattern is axis-aligned planks of
	/// <see cref="PlankRatio"/> by 1 (units of a plank width): a plank along U sits where
	/// (U − ⌊V⌋) mod 2n &lt; n, every other point lies on a plank along V. Each plank gets its own tone,
	/// hue lean, slight lift (planks are never perfectly flush), and grain read along its length from
	/// a stretched noise row of its own; a fine gap with a micro-bevel separates the planks. The
	/// pattern's phase differs per block and two blocks are mirrored, so no block is a turned copy of another.
	/// </summary>
	void PaintParquet( Painter painter, Random random )
	{
		const int cell = FloorSize / 2;
		const float Period = PlankRatio * 2f;
		var grain = painter.Noise( FloorSize / 2, Seed + 20, 1 );
		var figure = painter.Noise( 24, Seed + 21, 1 );
		var planks = new byte[FloorSize * FloorSize];

		// 256 plank colours, ±6 % in value with the hue oak really has: paler planks yellower,
		// darker ones rosier. A plank's hash picks its entry.
		var palette = new Color[256];
		for ( var k = 0; k < 256; k++ )
		{
			var tone = k / 255f * 2f - 1f;
			palette[k] = tone > 0f ? Mix( Oak, Mix( OakLight, Honey, 0.45f ), tone * 0.55f ) : Mix( Oak, Mix( OakShade, Rosy, 0.5f ), -tone * 0.55f );
		}
		var late = new float[FloorSize * FloorSize];
		var gap = new float[FloorSize * FloorSize];
		var relief = new float[FloorSize * FloorSize];
		var scale = 1f / (MathF.Sqrt( 2f ) * PlankWidth);

		// One period of a sine in 256 steps: the grain's waver without a Sin per pixel.
		var sine = new float[256];
		for ( var k = 0; k < 256; k++ ) sine[k] = MathF.Sin( k * MathF.Tau / 256f );
		int lastKey = int.MinValue, lastB = int.MinValue, plankPhase = 0, plankRow = 0;
		byte plankColor = 0;
		float plankLift = 0f, plankStrength = 0f, plankWave = 0f;
		uint plankShift = 0;

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var mirror = c == 1 || c == 2;
			var offsetU = (float)random.NextDouble() * Period;
			var offsetV = (float)random.NextDouble() * Period;
			lastKey = int.MinValue; lastB = int.MinValue;

			for ( var y = 0; y < cell; y++ )
			{
				var py = y + 0.5f;
				var row = ((int)min.y + y) * FloorSize + (int)min.x;
				for ( var x = 0; x < cell; x++ )
				{
					var px = mirror ? cell - x - 0.5f : x + 0.5f;
					var u = (px + py) * scale + offsetU;
					var v = (py - px) * scale + offsetV;

					float along, across; int idA, idB, family;
					var r = MathF.Floor( v );
					var a = u - r;
					var m = a - MathF.Floor( a / Period ) * Period;
					if ( m < PlankRatio )
					{
						along = m; across = v - r; idA = (int)r; idB = (int)MathF.Floor( a / Period ); family = 0;
					}
					else
					{
						var q = MathF.Floor( u );
						var b = v - q - 1f;
						along = b - MathF.Floor( b / Period ) * Period; across = u - q; idA = (int)q; idB = (int)MathF.Floor( b / Period ); family = 1;
					}

					// Per-plank values, recomputed only when the plank changes (runs of pixels share one).
					var key = idA * 2 + family;
					if ( key != lastKey || idB != lastB )
					{
						lastKey = key; lastB = idB;
						var h = Hash( key, idB, Seed + c * 7 );
						var g = Hash( key, idB, Seed + c * 7 + 3 );
						plankColor = (byte)(h & 0xFF);
						plankLift = (Bits( h, 12 ) - 0.5f) * 0.06f;
						plankStrength = 0.22f + Bits( h, 20 ) * 0.28f;
						plankPhase = (int)(g & 0xFF);
						plankWave = 1f + Bits( g, 8 ) * 2f;
						plankRow = (int)((g >> 16) & 511);
						plankShift = (g >> 25) & 127;
					}

					var i = row + x;
					planks[i] = plankColor;

					// Grain: thin late-wood lines along the plank, wavering slightly, read from a stretched
					// noise row (Streak, inlined: this loop runs a million times), over a soft figure.
					var acrossPx = across * PlankWidth;
					var alongPx = along * PlankWidth;
					var wave = sine[((int)(alongPx * 0.65f) + plankPhase) & 255] * plankWave;
					var grainRow = (((int)(acrossPx + wave) + plankRow) & (FloorSize - 1)) * FloorSize;
					var position = (alongPx + plankShift) * (1f / 70f);
					var p0 = (int)position;
					var g0 = grain[grainRow + (p0 & (FloorSize - 1))];
					var s = g0 + (grain[grainRow + ((p0 + 1) & (FloorSize - 1))] - g0) * (position - p0);
					var f = figure[i];
					var lateWood = (s > 0.62f ? MathF.Min( (s - 0.62f) * 6f, 1f ) * plankStrength : 0f) + (f > 0.55f ? (f - 0.55f) * 0.5f : 0f);
					late[i] = lateWood;

					// Gap and micro-bevel between planks.
					var edgeAcross = MathF.Min( across, 1f - across ) * PlankWidth;
					var edgeAlong = MathF.Min( along, PlankRatio - along ) * PlankWidth;
					var edge = MathF.Min( edgeAcross, edgeAlong );
					var shoulder = edge >= 3f ? 1f : edge / 3f;
					shoulder = shoulder * shoulder * (3f - 2f * shoulder);
					gap[i] = edge < 1.1f ? (1f - edge / 1.1f) * 0.45f : 0f;
					relief[i] = (shoulder - 1f) * 0.28f + plankLift - lateWood * 0.04f;
				}
			}
		}

		painter.Blend( planks, palette );
		painter.Blend( late, LateWood );
		painter.Blend( gap, Gap, roughness: 0.75f );
		painter.AddHeight( relief, 1f );
	}

	/// <summary>
	/// The ring between a cell's edge (pushed a pixel and a half out, so the outermost pixels are fully
	/// covered) and a block outline inside it, as one polygon: the cell square, a zero-width bridge to the
	/// outline, the outline, and back. Painter.Polygon fills by crossing count, so the bridge cancels
	/// itself and the outline becomes a hole.
	/// </summary>
	static Vector2[] JointRing( Vector2 min, float size, Vector2[] outline )
	{
		var points = new Vector2[outline.Length + 6];
		var lo = min - new Vector2( 1.5f, 1.5f );
		var hi = min + new Vector2( size + 1.5f, size + 1.5f );
		points[0] = lo; points[1] = new Vector2( hi.x, lo.y ); points[2] = hi; points[3] = new Vector2( lo.x, hi.y ); points[4] = lo;
		for ( var k = 0; k < outline.Length; k++ ) points[5 + k] = outline[k];
		points[^1] = outline[0];
		return points;
	}

	/// <summary>
	/// Dark walnut coffers, one bay per tile underside: half a beam along every edge (neighbouring
	/// undersides complete the beams, with a fine groove where they meet), a two-step moulding down into
	/// the recess, and a fielded panel. Beam grain runs along each beam and meets its neighbour in a
	/// mitre at the corners; panel grain runs one way or the other per bay, with its own figure.
	/// </summary>
	protected override PaintedSurface PaintCeiling()
	{
		const int cell = CeilingSize / 2;
		const int Beam = 24;
		var painter = new Painter( CeilingSize, Walnut, 0.45f );
		var random = new Random( Seed + 500 );
		float Next() => (float)random.NextDouble();

		var panelAlongX = new bool[4];
		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );
			var tone = Next() * 2f - 1f;
			var wood = tone < 0f ? Mix( Walnut, WalnutDeep, -tone * 0.4f ) : Mix( Walnut, WalnutLight, tone * 0.4f );
			panelAlongX[c] = Next() < 0.5f;

			painter.Fill( (int)min.x, (int)min.y, cell, cell, Mix( wood, WalnutLight, 0.2f ), height: 1f );
			painter.Polygon( Square( min, Beam, cell ), Mix( wood, WalnutDeep, 0.25f ), height: 0.72f, bevel: 2.5f );
			painter.Polygon( Square( min, Beam + 9, cell ), Mix( wood, WalnutDeep, 0.15f ), height: 0.5f, bevel: 2.5f );
			painter.Polygon( Square( min, Beam + 16, cell ), Mix( wood, WalnutDeep, 0.35f ), height: 0.22f, bevel: 2f );

			// The fielded panel: raised, its edges bevelled wide, never quite centred or square.
			var inset = Beam + 30 + Next() * 4f;
			var panel = Square( min, inset, cell );
			for ( var k = 0; k < 4; k++ ) panel[k] += new Vector2( Next() - 0.5f, Next() - 0.5f ) * 2.5f;
			painter.Polygon( panel, wood, height: 0.48f, bevel: 13f );
		}

		PaintWalnutGrain( painter, panelAlongX, Beam );

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );

			// Where neighbouring undersides meet: the beam's centre groove.
			painter.Polygon( JointRing( min, cell, Square( min, 1.5f, cell ) ), WalnutGrain, alpha: 0.7f, height: 0.85f );

			// A shrinkage check along the panel's grain on one bay; a dark heartwood streak on another.
			if ( c == 0 )
			{
				var start = min + new Vector2( 90f + Next() * 60f, 80f + Next() * 30f );
				var crack = Wander( start, panelAlongX[c] ? 0f : MathF.PI * 0.5f, 9f, 6 + random.Next( 3 ), 0.08f, random );
				Polyline( painter, crack, 0.9f, WalnutGrain, 0.7f, 0.4f );
			}
			if ( c == 3 )
			{
				var at = min + new Vector2( 128f + (Next() - 0.5f) * 40f, 128f + (Next() - 0.5f) * 40f );
				var half = panelAlongX[c] ? new Vector2( 50f, 9f ) : new Vector2( 9f, 50f );
				painter.Polygon( Organic( at, half, 0.6f, Seed + 560, 0f, 24 ), WalnutDeep, alpha: 0.3f );
			}
		}

		painter.Mottle( 0.06f, 3, Seed + 520, 4 );
		painter.RoughnessNoise( 0.06f, 6, Seed + 521, 2 );
		painter.Cavity( 0.5f, 4 );
		painter.Grain( 0.012f, Seed + 523 );
		return painter.Bake( "arena_4_ceiling", Painter.Matte, 4f );
	}

	/// <summary>A square inset by <paramref name="inset"/> px from a cell's edges, corners clockwise in the image.</summary>
	static Vector2[] Square( Vector2 min, float inset, float size ) => new[]
	{
		min + new Vector2( inset, inset ), min + new Vector2( size - inset, inset ),
		min + new Vector2( size - inset, size - inset ), min + new Vector2( inset, size - inset ),
	};

	/// <summary>
	/// Walnut grain over the whole ceiling in one pass: in the beam and moulding band, along the
	/// nearest edge (the diagonal where two edges are equally near is the mitre); in the bay, along the
	/// panel's direction. Late-wood lines darken, a broad figure shifts the tone, and the odd pale
	/// sapwood streak shows on the beams.
	/// </summary>
	void PaintWalnutGrain( Painter painter, bool[] panelAlongX, int beam )
	{
		const int cell = CeilingSize / 2;
		var grain = painter.Noise( CeilingSize / 2, Seed + 530, 1 );
		var figure = painter.Noise( 24, Seed + 531, 2 );
		var dark = new float[CeilingSize * CeilingSize];
		var light = new float[CeilingSize * CeilingSize];
		var groove = new float[CeilingSize * CeilingSize];
		var band = beam + 16;

		for ( var y = 0; y < CeilingSize; y++ )
		{
			var ly = y % cell;
			for ( var x = 0; x < CeilingSize; x++ )
			{
				var lx = x % cell;
				var c = (y / cell) * 2 + x / cell;
				var toLeft = lx; var toRight = cell - 1 - lx; var toTop = ly; var toBottom = cell - 1 - ly;
				var nearestX = Math.Min( toLeft, toRight ); var nearestY = Math.Min( toTop, toBottom );

				int along, across, zone;
				if ( Math.Min( nearestX, nearestY ) < band )
				{
					// A beam along a vertical edge when that edge is nearer, along a horizontal one otherwise.
					if ( nearestX < nearestY ) { along = ly; across = nearestX; zone = toLeft < toRight ? 1 : 2; }
					else { along = lx; across = nearestY; zone = toTop < toBottom ? 3 : 4; }
				}
				else
				{
					along = panelAlongX[c] ? lx : ly; across = panelAlongX[c] ? ly : lx; zone = 5;
				}

				var offset = zone * 97 + c * 211;
				var s = Streak( grain, CeilingSize, across + offset, (along + offset) / 45f );
				var f = Streak( figure, CeilingSize, across + offset, along / 28f );
				var i = y * CeilingSize + x;
				dark[i] = (s > 0.6f ? MathF.Min( (s - 0.6f) * 6f, 1f ) * 0.4f : 0f) + (f > 0.6f ? (f - 0.6f) * 0.6f : 0f);
				light[i] = f < 0.32f ? (0.32f - f) * 0.7f : 0f;
				groove[i] = -dark[i] * 0.05f;
			}
		}

		painter.Blend( dark, WalnutGrain );
		painter.Blend( light, WalnutLight );
		painter.AddHeight( groove, 1f );
	}

	/// <summary>
	/// Builds the library's props into the floor's kit, in wall space (see <see cref="WallProps"/>). What a
	/// reader would use — desks, chairs, steps, ladders, lamps — is at a player's size; each prop group
	/// draws from a stream of its own (<see cref="WallProps.Apart"/>).
	/// </summary>
	sealed class Props : WallProps
	{
		/// <summary>Height of a chandelier's candle flames, where its lamp burns, and how far out from the cornice it hangs.</summary>
		public const float FlameV = 224f;
		public const float HangDepth = -11f;

		/// <summary>The face of the skirting, which furniture stands against.</summary>
		const float Skirt = FloorContext.Recess - 6.5f;

		/// <summary>The underside of the cornice beam.</summary>
		const float Beam = FloorContext.ClearHeight - 30f;

		/// <summary>Depth of the ladder rail, and of a ladder's foot on the bay floor.</summary>
		const float RailY = 27f;
		const float FootY = 5f;

		/// <summary>Desk and table tops: a player's waist.</summary>
		const float DeskTop = 29f;

		static Vector3 ToRoom => new( 0f, -1f, 0f );

		readonly Material _trim;
		readonly Material _glow;

		/// <summary>The stretches along the bay being furnished that something already stands on.</summary>
		readonly List<(float From, float To)> _floor = new();

		/// <summary>The rail's height on the wall being dressed.</summary>
		float _railV;

		/// <summary>Whether the bay being furnished has something at a player's chest or head yet.</summary>
		bool _tall;

		public Props( FloorContext room, Material trim, Material glow ) : base( room )
		{
			_trim = trim;
			_glow = glow;
		}

		/// <summary>What a bay is furnished as: one of the seven sets the bays are dealt from, or the longcase clock.</summary>
		public enum Look { Desk, Reading, Lectern, Bust, Catalogue, Trolley, Easel, Clock }

		Skin TrimSkin( Rect region ) => new( _trim, region );
		Skin Brass => TrimSkin( BrassSwatch );
		Skin Wood() => TrimSkin( WoodSwatch( Pick( 3 ) ) );
		Skin Book() => TrimSkin( BookSwatch( Pick( BookCloths ) ) );

		/// <summary>A pale or painted finish half the time, else one of the walnuts: what lifts a piece off the walnut cases behind it.</summary>
		Skin Finish() => Next() < 0.5f ? TrimSkin( FinishSwatch( Pick( 3 ) ) ) : Wood();

		/// <summary>A flat panel of <paramref name="skin"/> facing the room: a drawer front, a pull — two triangles, for detail only ever seen from the front.</summary>
		void Flat( Skin skin, float u, float width, float v0, float v1, float depth ) =>
			Room.WallPicture( Side, skin.Material, skin.Region, u - width * 0.5f, u + width * 0.5f, v0, v1, depth, false );
		static Rotation Yaw( float degrees ) => Rotation.FromYaw( degrees );
		Rotation Facing => Turn( Rotation.FromToRotation( Vector3.Up, ToRoom ) );

		void Claim( float u, float width ) => _floor.Add( (u - width * 0.5f, u + width * 0.5f) );


		/// <summary>
		/// The depth a piece <paramref name="halfWidth"/> by <paramref name="halfDepth"/>, turned <paramref name="yaw"/>
		/// degrees, stands at: <paramref name="wanted"/>, or further in where a corner would come nearer the room
		/// than <see cref="WallProps.Front"/>.
		/// </summary>
		static float Fit( float halfWidth, float halfDepth, float yaw, float wanted )
		{
			var a = yaw * MathF.PI / 180f;
			return MathF.Max( wanted, Front + 1f + MathF.Abs( MathF.Sin( a ) ) * halfWidth + MathF.Cos( a ) * halfDepth );
		}



		// ---- Walls ------------------------------------------------------------------------------

		/// <summary>
		/// A brass classification plate on the cornice over every bay, its lettering dealt so that no two
		/// neighbours read alike, slid aside where a chandelier's bracket comes out of the cornice.
		/// </summary>
		public void Plates( List<Bay> wall, List<float> lamps ) => Apart( () =>
		{
			var labels = Deal( new List<int> { 0, 1, 2, 3 }, wall.Count );
			for ( var k = 0; k < wall.Count; k++ )
			{
				var bay = wall[k];
				var u = bay.Middle + Range( -6f, 6f );
				var lamp = lamps.FindIndex( l => MathF.Abs( l - u ) < 20f );
				if ( lamp >= 0 ) u = lamps[lamp] + (u < lamps[lamp] ? -22f : 22f);
				var v = Beam + Range( 12.5f, 14f );
				var w = Range( 21f, 25f );
				Kit.BevelBox( TrimSkin( BronzeSwatch ), At( new Vector3( u, -0.4f, v ) ), new Vector3( w, 1.6f, 10f ), 0.4f, Run );
				Room.WallPicture( Side, _trim, PlateSwatch( labels[k] ), u - w * 0.5f + 1.3f, u + w * 0.5f - 1.3f, v - 3.7f, v + 3.7f, -1.25f, false );
			}
		} );

		/// <summary>A brass rail along the bookcases of every bay, on brackets screwed to the shelf fronts, for the rolling ladders.</summary>
		public void Rails( List<Bay> wall ) => Apart( () =>
		{
			_railV = Range( 192f, 202f );
			foreach ( var bay in wall )
			{
				Kit.Tube( Brass, At( new[] { new Vector3( bay.From - 1f, RailY, _railV ), new Vector3( bay.To + 1f, RailY, _railV ) } ), 0.8f, 6 );
				var brackets = Math.Max( 2, (int)(bay.Width / 60f) + 1 );
				for ( var k = 0; k < brackets; k++ )
				{
					var u = bay.From + 10f + (bay.Width - 20f) * k / (brackets - 1f) + Range( -3f, 3f );
					Kit.Box( Brass, At( new Vector3( u, (RailY + Face + 2f) * 0.5f, _railV ) ), new Vector3( 1.2f, Face + 2f - RailY, 1.6f ), Run );
					Kit.BevelBox( Brass, At( new Vector3( u, Face + 0.5f, _railV ) ), new Vector3( 3f, 1f, 4f ), 0.2f, Run );
				}
			}
		} );

		/// <summary>
		/// The gallery wall: in every bay a framed map or portrait hung on two cords from a brass hook under the
		/// cornice, over the upper shelves — dealt so that no two neighbours hang the same picture.
		/// </summary>
		public void Gallery( List<Bay> wall, List<float> lamps ) => Apart( () =>
		{
			var deck = new List<int>();
			for ( var k = 0; k < Pictures; k++ ) deck.Add( k );
			var pictures = Deal( deck, wall.Count );
			for ( var k = 0; k < wall.Count; k++ )
			{
				var bay = wall[k];
				var room = bay.Width - 40f;
				var (canvas, width, aspect, mirror) = pictures[k] switch
				{
					< 3 => (MapSwatch( pictures[k] ), MathF.Min( room, Range( 64f, 82f ) ), 0.75f, false),
					< 5 => (PortraitSwatch( pictures[k] - 3 ), Range( 40f, 48f ), 1.5f, Next() < 0.5f),
					< 7 => (LandscapeSwatch( pictures[k] - 5 ), MathF.Min( room, Range( 64f, 80f ) ), 0.6f, Next() < 0.5f),
					_ => (PanoramaSwatch( pictures[k] - 7 ), MathF.Min( room, Range( 92f, 112f ) ), 0.25f, false),
				};
				var height = width * aspect;
				var slack = MathF.Max( 0f, room - width ) * 0.5f;
				var u = bay.Middle + Range( -1f, 1f ) * slack;
				var bottom = MathF.Min( Range( 150f, 166f ), 226f - height );

				// A chandelier's arms reach about fifteen either way along the wall and hang to v 207: slide the
				// picture as far from one as the bay allows, and where it still comes under the arms, hang it lower.
				var reach = width * 0.5f + 20f;
				var lamp = lamps.FindIndex( l => MathF.Abs( l - u ) < reach );
				if ( lamp >= 0 )
				{
					u = bay.Middle + (lamps[lamp] < bay.Middle ? slack : -slack);
					if ( MathF.Abs( lamps[lamp] - u ) < reach ) bottom = MathF.Min( bottom, 203f - height );
				}
				Picture( u, bottom, width, height, canvas, mirror );
			}
		} );

		/// <summary>
		/// A picture <paramref name="width"/> by <paramref name="height"/> over the shelves, in a gilt or walnut
		/// frame, on cords; a portrait or a landscape may hang <paramref name="mirror"/>ed, a second sitter of the first.
		/// </summary>
		void Picture( float u, float bottom, float width, float height, Rect canvas, bool mirror )
		{
			const float Y = Face - 2.2f;
			const float Moulding = 3.2f;
			var top = bottom + height;
			var middle = (bottom + top) * 0.5f;
			var frame = TrimSkin( Next() < 0.6f ? GiltSwatch : WoodSwatch( 1 ) );
			if ( mirror ) canvas = new Rect( canvas.Left + canvas.Width, canvas.Top, -canvas.Width, canvas.Height );
			Room.WallPicture( Side, _trim, canvas, u - width * 0.5f, u + width * 0.5f, bottom, top, Y, false );
			Kit.Box( TrimSkin( ShadowSwatch ), At( new Vector3( u, (Y + 0.1f + Face + 1f) * 0.5f, middle ) ), new Vector3( width, Face + 0.9f - Y, height ), Run );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.BevelBox( frame, At( new Vector3( u, Y - 0.6f, middle + s * (height + Moulding) * 0.5f ) ), new Vector3( width + Moulding * 2f, 2.4f, Moulding ), 0.6f, Run );
				Kit.BevelBox( frame, At( new Vector3( u + s * (width + Moulding) * 0.5f, Y - 0.6f, middle ) ), new Vector3( Moulding, 2.4f, height ), 0.6f, Run );
			}
			var hook = new Vector3( u + Range( -3f, 3f ), Face - 0.5f, Beam - 2f );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Tube( TrimSkin( IronSwatch ), At( new[] { new Vector3( u + s * width * 0.3f, Y + 0.6f, top + Moulding ), hook } ), 0.18f, 3 );
			Kit.BevelBox( Brass, At( hook + new Vector3( 0f, 0.5f, 0.6f ) ), new Vector3( 3f, 2f, 1.6f ), 0.3f, Run );
		}

		/// <summary>
		/// Twin-armed brass sconces on about half the pilasters between the bays (never the pier, which carries
		/// the floor's plaque, nor beside a chandelier): a backplate, two scrolled arms, candle cups, wax candles
		/// with lit bulbs, small linen shades.
		/// </summary>
		public void Sconces( List<Bay> wall, List<float> lamps ) => Apart( () =>
		{
			for ( var k = 0; k + 1 < wall.Count; k++ )
			{
				var u = (wall[k].To + wall[k + 1].From) * 0.5f;
				if ( MathF.Abs( u - FloorContext.HalfSpan ) < 50f || lamps.Exists( l => MathF.Abs( l - u ) < 45f ) || Next() < 0.4f ) continue;
				Sconce( u, Range( 148f, 158f ) );
			}
		} );

		void Sconce( float u, float v )
		{
			Kit.BevelBox( Brass, At( new Vector3( u, -0.5f, v ) ), new Vector3( 4f, 1.4f, 9f ), 0.4f, Run );
			Kit.SmoothLathe( Brass, At( new Vector3( u, -1.2f, v - 7f ) ), new[] { new Vector2( 0f, 0f ), new Vector2( 0.9f, 1.4f ), new Vector2( 0.5f, 2.6f ) }, 6, Run, false, true );
			var reach = Range( 5f, 6.5f );
			foreach ( var s in new[] { -1f, 1f } )
			{
				var cup = new Vector3( u + s * reach, -6f, v + 3.5f );
				Kit.Tube( Brass, At( new[] { new Vector3( u, -1.2f, v - 1f ), new Vector3( u + s * reach * 0.45f, -3f, v - 2.2f ), new Vector3( u + s * reach * 0.9f, -5.2f, v - 0.8f ), cup + new Vector3( 0f, 0f, -0.4f ) } ), 0.35f, 4 );
				Kit.SmoothLathe( Brass, At( cup ), new[] { new Vector2( 0.5f, -0.6f ), new Vector2( 1.4f, 0.2f ), new Vector2( 1.5f, 0.8f ) }, 6, Run, true, false );
				Kit.SmoothLathe( TrimSkin( WaxSwatch ), At( cup + new Vector3( 0f, 0f, 0.8f ) ), new[] { new Vector2( 0.6f, 0f ), new Vector2( 0.6f, 2.8f ) }, 6, Run, false, true );
				Kit.Ellipsoid( new Skin( _glow, FlameGlow( Pick( 2 ) ) ), At( cup + new Vector3( 0f, 0f, 4.4f ) ), new Vector3( 0.6f, 0.6f, 1.1f ), Run, 3, 6 );
				var shade = cup + new Vector3( 0f, 0f, 3.2f );
				Kit.SmoothLathe( TrimSkin( LinenSwatch ), At( shade ), new[] { new Vector2( 2.8f, 0f ), new Vector2( 1.7f, 3.4f ) }, 8, Run, false, false );
				Kit.SmoothLathe( new Skin( _glow, ShadeGlow( 0 ) ), At( shade ), new[] { new Vector2( 1.6f, 3.35f ), new Vector2( 2.7f, 0.05f ) }, 8, Run, false, false );
			}
		}

		/// <summary>
		/// The plain wall's upper layer, so it is not a flat picture over the furniture: a walnut corbel on every
		/// pilaster but the pier's and the chandeliers', each carrying a white plaster bust, a porcelain urn or a table
		/// globe, half as big again as life and dealt so no two neighbours match — pale things that carry against the
		/// walnut from across the room; a brass picture light over the cases of every bay, lit underneath; and, unless
		/// the wall has its longcase clock, a big reading-room clock hung on the cases of one bay.
		/// </summary>
		public void UpperLayer( List<Bay> wall, List<float> lamps, bool clock ) => Apart( () =>
		{
			var posts = new List<float>();
			for ( var k = 0; k + 1 < wall.Count; k++ )
			{
				var u = (wall[k].To + wall[k + 1].From) * 0.5f;
				if ( MathF.Abs( u - FloorContext.HalfSpan ) > 50f && !lamps.Exists( l => MathF.Abs( l - u ) < 30f ) ) posts.Add( u );
			}
			var pieces = Deal( new List<int> { 0, 1, 2 }, posts.Count );
			for ( var k = 0; k < posts.Count; k++ )
			{
				var v = Range( 186f, 192f );
				Corbel( posts[k], v );
				var foot = new Vector3( posts[k], -5.5f, v );
				if ( pieces[k] == 0 ) Bust( foot, 1.3f, TrimSkin( Next() < 0.85f ? PlasterSwatch : BronzeSwatch ) );
				else if ( pieces[k] == 1 ) Urn( foot, 1.3f );
				else TableGlobe( foot, 1.3f );
			}

			foreach ( var bay in wall )
			{
				var length = MathF.Min( bay.Width * 0.6f, Range( 64f, 90f ) );
				var u = bay.Middle + Range( -6f, 6f );
				var lamp = lamps.FindIndex( l => MathF.Abs( l - u ) < length * 0.5f + 18f );
				if ( lamp >= 0 ) u = lamps[lamp] + (u < lamps[lamp] ? -1f : 1f) * (length * 0.5f + 18f);
				if ( u - length * 0.5f > bay.From + 4f && u + length * 0.5f < bay.To - 4f ) PictureLight( u, length );
			}

			if ( !clock ) return;
			var bays = wall.FindAll( b => !lamps.Exists( l => l > b.From - 10f && l < b.To + 10f ) );
			var at = bays[Pick( bays.Count )];
			ReadingClock( at.Middle + Range( -0.15f, 0.15f ) * at.Width, Range( 176f, 184f ) );
		} );

		/// <summary>A walnut corbel on a pilaster at <paramref name="v"/>: a moulded shelf on a scrolled bracket, a brass rim.</summary>
		void Corbel( float u, float v )
		{
			var wood = TrimSkin( WoodSwatch( Next() < 0.5f ? 0 : 1 ) );
			Kit.BevelBox( wood, At( new Vector3( u, -5f, v - 1.5f ) ), new Vector3( 15f, 10f, 3f ), 0.6f, Run );
			Kit.Box( TrimSkin( GiltSwatch ), At( new Vector3( u, -5f, v - 3.4f ) ), new Vector3( 14f, 9.4f, 0.8f ), Run );
			Kit.Box( wood, At( new Vector3( u, -3f, v - 8.5f ) ), new Vector3( 8f, 6f, 9f ), Run );
			Kit.Box( wood, At( new Vector3( u, -1.5f, v - 15f ) ), new Vector3( 5f, 3f, 5f ), Run );
		}

		/// <summary>A white porcelain urn on its foot at <paramref name="foot"/>, <paramref name="scale"/> times life: a turned body with a neck and lip, two handles.</summary>
		void Urn( Vector3 foot, float scale )
		{
			var skin = TrimSkin( PorcelainSwatch );
			var tall = Range( 0.9f, 1.15f );
			var profile = new[]
			{
				new Vector2( 3.2f, 0f ), new Vector2( 3.2f, 1.2f ), new Vector2( 1.5f, 2.6f ), new Vector2( 1.6f, 4.6f ), new Vector2( 5.4f, 9f * tall ),
				new Vector2( 5.6f, 12f * tall ), new Vector2( 3.6f, 16.5f * tall ), new Vector2( 2.6f, 18f * tall ), new Vector2( 3.6f, 19.6f * tall ), new Vector2( 3.4f, 20.4f * tall ),
			};
			Kit.SmoothLathe( skin, At( foot ), Array.ConvertAll( profile, p => p * scale ), 10, Run, true, true, 40f, Next() );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Vector3 P( float x, float z ) => foot + new Vector3( s * x * scale, 0f, z * tall * scale );
				Kit.Tube( skin, At( new[] { P( 3.4f, 17.6f ), P( 6.2f, 18.4f ), P( 6.6f, 14.6f ), P( 5.2f, 11.6f ) } ), 0.45f * scale, 4 );
			}
		}

		/// <summary>A table globe at <paramref name="foot"/>, <paramref name="scale"/> times life: a turned brass stand, the globe tilted on its axis in a brass meridian.</summary>
		void TableGlobe( Vector3 foot, float scale )
		{
			Kit.SmoothLathe( Brass, At( foot ), new[] { new Vector2( 3.6f, 0f ), new Vector2( 3.6f, 0.8f ), new Vector2( 1f, 2f ), new Vector2( 0.8f, 5f ), new Vector2( 1.4f, 6f ) }, 8, Run, true, false, 45f );
			var r = 6f * scale;
			var centre = foot + new Vector3( 0f, 0f, 6f + r );
			var tilt = Yaw( Range( -40f, 40f ) ) * Rotation.FromRoll( 23.5f );
			Kit.Ellipsoid( TrimSkin( GlobeSwatch ), At( centre ), new Vector3( r, r, r ), Turn( tilt * Yaw( Next() * 360f ) ), 5, 12 );
			Kit.Tube( Brass, At( Loop( centre, tilt * Vector3.Up, tilt * Vector3.Forward, r + 0.8f, 14 ) ), 0.35f, 3 );
		}

		/// <summary>A brass picture light <paramref name="length"/> long over the cases: two arms from the top shelf and a hood lit underneath, as a gallery lights its paintings.</summary>
		void PictureLight( float u, float length )
		{
			const float V = 228f;
			const float Y = Face - 9f;
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Tube( Brass, At( new[] { new Vector3( u + s * length * 0.3f, Face - 0.5f, V - 6f ), new Vector3( u + s * length * 0.3f, Face - 4f, V - 1f ), new Vector3( u + s * length * 0.3f, Y + 1f, V ) } ), 0.35f, 4 );
			Kit.SmoothLathe( Brass, At( new Vector3( u - length * 0.5f, Y, V ) ), new[] { new Vector2( 2.2f, 0f ), new Vector2( 2.6f, 0.8f ), new Vector2( 2.6f, length - 0.8f ), new Vector2( 2.2f, length ) }, 6, Turn( Rotation.FromToRotation( Vector3.Up, Vector3.Forward ) ), true, true, 50f );
			Kit.Box( new Skin( _glow, ShadeGlow( 1 ) ), At( new Vector3( u, Y + 0.4f, V - 2.9f ) ), new Vector3( length - 2f, 3f, 0.5f ), Run );
		}

		/// <summary>A reading-room clock hung on the cases, half as big again as a wall clock: a brass bezel round a cream enamel dial, black hands, on a bracket from the shelf above.</summary>
		void ReadingClock( float u, float v )
		{
			var r = Range( 15f, 17f );
			var back = new Vector3( u, Face - 2.5f, v );
			Kit.Box( TrimSkin( WoodSwatch( 1 ) ), At( new Vector3( u, Face - 1.5f, v + r + 3f ) ), new Vector3( 4f, 3f, 8f ), Run );
			Kit.SmoothLathe( Brass, At( back ), new[] { new Vector2( r, 0f ), new Vector2( r + 0.8f, 0.8f ), new Vector2( r + 0.8f, 2.6f ), new Vector2( r - 0.6f, 3.2f ) }, 16, Facing, true, false, 40f );
			Kit.SmoothLathe( TrimSkin( DialSwatch ), At( back + new Vector3( 0f, -2.4f, 0f ) ), new[] { new Vector2( r - 0.6f, 0f ), new Vector2( r - 0.6f, 0.2f ) }, 16, Facing, false, true );
			var minutes = Next() * 720f;
			foreach ( var (turns, length, width) in new[] { (minutes / 720f, r * 0.55f, 1.4f), (minutes % 60f / 60f, r * 0.8f, 1f) } )
			{
				var a = turns * MathF.Tau;
				var dir = new Vector3( MathF.Sin( a ), 0f, MathF.Cos( a ) );
				Kit.Box( TrimSkin( InkSwatch ), At( back + new Vector3( 0f, -2.9f, 0f ) + dir * (length * 0.35f) ), new Vector3( width, 0.3f, length * 1.1f ), Turn( Rotation.LookAt( Vector3.Cross( ToRoom, dir ), dir ) ) );
			}
		}

		/// <summary>
		/// A brass chandelier at <paramref name="u"/> on wall <paramref name="side"/>, hung on a chain from a scrolled
		/// bracket out of the cornice: a turned stem with a bowl, five or six arms curling up to candle cups, and
		/// a lit candle in each. The room's lamp burns among the flames (<see cref="FlameV"/>, <see cref="HangDepth"/>).
		/// </summary>
		public void Chandelier( int side, float u ) => Apart( () =>
		{
			OnSide( side );
			const float Y = HangDepth;
			var metal = TrimSkin( Next() < 0.7f ? BrassSwatch : BronzeSwatch );
			var bracket = Beam + Range( 9f, 12f );
			Kit.BevelBox( metal, At( new Vector3( u, -0.5f, bracket ) ), new Vector3( 5f, 1.4f, 8f ), 0.3f, Run );
			Kit.Tube( metal, At( new[]
			{
				new Vector3( u, -1f, bracket - 2f ), new Vector3( u, -4f, bracket - 3.2f ), new Vector3( u, -8f, bracket - 1.6f ),
				new Vector3( u, Y, bracket + 1f ), new Vector3( u, Y + 1.4f, bracket + 3.4f ),
			} ), 0.45f, 5 );

			const float Crown = FlameV + 13f;
			Kit.Tube( metal, At( new[] { new Vector3( u, Y, bracket + 2.4f ), new Vector3( u, Y, Crown + 0.6f ) } ), 0.45f, 4 );

			Kit.SmoothLathe( metal, At( new Vector3( u, Y, FlameV - 17f ) ), new[]
			{
				new Vector2( 0f, 0f ), new Vector2( 0.6f, 0.8f ), new Vector2( 0.5f, 2.4f ), new Vector2( 2.4f, 4.6f ), new Vector2( 2.8f, 6.4f ),
				new Vector2( 1f, 8.2f ), new Vector2( 0.8f, 9f ), new Vector2( 3.6f, 10.4f ), new Vector2( 3.9f, 11.4f ), new Vector2( 1.2f, 12.6f ),
				new Vector2( 0.7f, 14f ), new Vector2( 0.7f, 26f ), new Vector2( 1.8f, 27.6f ), new Vector2( 1.8f, 29f ), new Vector2( 0.6f, 30f ),
			}, 10, Run, false, false, 40f, Next() );

			// Two tiers of arms, spread wider along the wall than out over the room (the overhang allows no
			// more), so from the room it reads a chandelier's full width: six or seven below, four above.
			var wax = TrimSkin( WaxSwatch );
			foreach ( var (arms, along, across, rise) in new[] { (6 + Pick( 2 ), Range( 13f, 15f ), 8.2f, 0f), (4, Range( 7f, 8.5f ), 5f, 9f) } )
			{
				var spin = Next() / arms;
				for ( var k = 0; k < arms; k++ )
				{
					var a = (k / (float)arms + spin) * MathF.Tau;
					var dir = new Vector3( MathF.Cos( a ) * along, MathF.Sin( a ) * across, 0f );
					Vector3 P( float t, float v ) => new Vector3( u, Y, v + rise ) + dir * t;
					var cup = P( 1f, FlameV - 4.8f );
					Kit.Tube( metal, At( new[] { P( 0.1f, FlameV - 7f ), P( 0.4f, FlameV - 9.6f ), P( 0.78f, FlameV - 9f ), P( 0.97f, FlameV - 6.6f ), cup } ), 0.35f, 4 );
					Kit.SmoothLathe( metal, At( cup ), new[] { new Vector2( 0.4f, -0.5f ), new Vector2( 1.3f, 0.2f ), new Vector2( 1.4f, 0.7f ) }, 6, Run, true, false );
					Kit.SmoothLathe( wax, At( cup + new Vector3( 0f, 0f, 0.7f ) ), new[] { new Vector2( 0.55f, 0f ), new Vector2( 0.55f, Range( 2.2f, 3f ) ) }, 5, Run, false, true );
					Kit.Ellipsoid( new Skin( _glow, FlameGlow( Pick( 2 ) ) ), At( cup + new Vector3( 0f, 0f, 4.4f ) ), new Vector3( 0.65f, 0.65f, 1.2f ), Run, 3, 6 );
				}
			}

			// Glass drops hanging from the bowl's rim, catching the flames.
			for ( var k = 0; k < 8; k++ )
			{
				var a = k * MathF.Tau / 8f;
				var drop = new Vector3( u + MathF.Cos( a ) * 3.8f, Y + MathF.Sin( a ) * 3.8f, FlameV - 8.6f );
				Kit.Ellipsoid( new Skin( _glow, ShadeGlow( 1 ) ), At( drop ), new Vector3( 0.45f, 0.45f, 1.1f ), Run, 2, 4 );
			}
		} );

		// ---- Bays -------------------------------------------------------------------------------

		/// <summary>
		/// Furnishes <paramref name="bay"/> as <paramref name="look"/> — after its rolling ladder, where it has one, in
		/// the widest stretch the ladder leaves — then fills what the set leaves bare (<see cref="Fill"/>), so every
		/// bay ends up with something low and something at a player's chest or head.
		/// </summary>
		public void Furnish( Look look, Bay bay, bool ladder ) => Apart( () =>
		{
			_floor.Clear();
			_tall = false;
			_stool = false;
			if ( ladder ) Ladder( Next() < 0.5f ? bay.From + Range( 15f, 28f ) : bay.To - Range( 15f, 28f ) );

			var (from, to) = (bay.From + 3f, bay.To - 3f);
			var widest = 0f;
			foreach ( var gap in Gaps( _floor, bay.From + 3f, bay.To - 3f ) )
				if ( gap.To - gap.From > widest ) { widest = gap.To - gap.From; (from, to) = gap; }

			switch ( look )
			{
				case Look.Desk:
				{
					var length = MathF.Min( to - from - 6f, Range( 46f, 60f ) );
					if ( length > 36f ) Row( from, to, 2.5f, (length + 2f, u => Desk( u, length )) );
					break;
				}
				case Look.Reading:
					Row( from, to, 2.5f, (36f, Armchair), (17f, u => SideTable( u, Next() < 0.6f )), (19f, FloorLamp) );
					break;
				case Look.Lectern:
					Row( from, to, 2.5f, (22f, Lectern), (26f, Globe), (19f, FloorLamp) );
					break;
				case Look.Bust:
				{
					var bench = Range( 34f, 44f );
					Row( from, to, 2.5f, (18f, Bust), (bench + 2f, u => Bench( u, bench )), (17f, u => SideTable( u, true )) );
					break;
				}
				case Look.Catalogue:
				{
					var width = Range( 30f, 36f );
					Row( from, to, 2.5f, (width + 2f, u => Catalogue( u, width )), (15f, KickStool) );
					break;
				}
				case Look.Trolley:
				{
					var length = Range( 34f, 42f );
					Row( from, to, 2.5f, (length + 2f, u => Trolley( u, length )), (19f, FloorLamp) );
					break;
				}
				case Look.Easel:
					Row( from, to, 2.5f, (28f, EaselOrSphere), (17f, SideChair) );
					break;
				default:
					Row( from, to, 2.5f, (25f, LongcaseClock), (17f, SideChair) );
					break;
			}
			Fill( bay );
		} );

		/// <summary>
		/// Fills what a bay's set left bare, so no stretch of it reads empty from the room: a standard lamp first
		/// where the bay has nothing tall yet, then a bench, a kick stool, an umbrella stand or piles of books,
		/// each against one end of its gap, so a long gap takes two.
		/// </summary>
		void Fill( Bay bay )
		{
			for ( var pass = 0; pass < 2; pass++ )
				foreach ( var (from, to) in Gaps( _floor, bay.From + 3f, bay.To - 3f ) )
				{
					var gap = to - from;
					var end = Next() < 0.5f;
					float Place( float width ) => end ? from + width * 0.5f + 1f : to - width * 0.5f - 1f;
					var roll = Next();
					if ( !_tall && gap >= 21f )
					{
						// Something a head high: most often a standard lamp; now and then, once a wall each, an
						// armillary sphere on its pedestal or a hat stand.
						var pick = Next();
						if ( pick < 0.38f && _heroes.Add( "armillary" ) ) Armillary( Place( 19f ) );
						else if ( pick < 0.56f && _heroes.Add( "hats" ) ) HatStand( Place( 19f ) );
						else FloorLamp( Place( 19f ) );
					}
					else if ( pass == 0 && gap >= 46f && roll < 0.45f )
					{
						var length = MathF.Min( gap - 6f, Range( 34f, 44f ) );
						Bench( Place( length + 2f ), length );
					}
					else if ( gap >= 16f && roll < 0.3f && !_stool ) KickStool( Place( 15f ) );
					else if ( gap >= 14f && roll < 0.45f && !_umbrellas ) Umbrellas( Place( 12f ) );
					else if ( gap >= 16f ) Piles( Place( 16f ) );
				}
		}

		/// <summary>Whether the bay being furnished has its kick stool, and the wall its umbrella stand: one of each is plenty.</summary>
		bool _stool;
		bool _umbrellas;

		/// <summary>The one-a-wall pieces the wall being dressed has had already.</summary>
		readonly HashSet<string> _heroes = new();

		/// <summary>Starts dressing a wall: nothing on it yet.</summary>
		public void StartWall()
		{
			_umbrellas = false;
			_heroes.Clear();
		}

		// ---- Things -----------------------------------------------------------------------------

		/// <summary>
		/// A rolling library ladder at <paramref name="u"/>: two stiles leaning from the bay floor up to the rail,
		/// flat treads, brass hooks over the rail and castors at its feet.
		/// </summary>
		void Ladder( float u )
		{
			Claim( u, 24f );
			var foot = new Vector3( u, FootY, Ledge );
			var top = new Vector3( u, RailY - 2.6f, _railV + 3f );
			var length = (top - foot).Length;
			var axis = (top - foot) / length;
			var turn = Turn( Rotation.LookAt( Vector3.Forward, axis ) );
			var wood = TrimSkin( WoodSwatch( Next() < 0.5f ? 0 : 2 ) );
			var half = Range( 8.5f, 10f );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.BevelBox( wood, At( foot + axis * (length * 0.5f) + new Vector3( s * half, 0f, 0f ) ), new Vector3( 1.8f, 2.4f, length ), 0.3f, turn );
				var hook = new Vector3( u + s * half, RailY, _railV );
				Kit.Tube( Brass, At( new[]
				{
					top + new Vector3( s * half, 0f, -1f ), hook + new Vector3( 0f, -1.9f, 1.8f ), hook + new Vector3( 0f, 0f, 2.3f ),
					hook + new Vector3( 0f, 1.9f, 1.4f ), hook + new Vector3( 0f, 2.1f, -0.6f ),
				} ), 0.45f, 5 );
				Kit.Ellipsoid( TrimSkin( IronSwatch ), At( foot + new Vector3( s * half, -0.3f, 1.4f ) ), new Vector3( 1.4f, 1.4f, 1.4f ), Run, 3, 6 );
			}
			var pitch = Range( 10.5f, 12f );
			for ( var d = pitch; d < length - 4f; d += pitch )
				Kit.Box( wood, At( foot + axis * d ), new Vector3( half * 2f - 0.6f, 3.2f, 0.9f ), turn );
			_tall = true;
		}

		/// <summary>A turned leg from <paramref name="foot"/> up <paramref name="height"/>: a taper to the floor, a swell, a bead under its block.</summary>
		void Leg( Skin wood, Vector3 foot, float height, float radius )
		{
			Kit.SmoothLathe( wood, At( foot ), new[]
			{
				new Vector2( radius * 0.6f, 0f ), new Vector2( radius * 0.75f, height * 0.35f ), new Vector2( radius * 1.15f, height * 0.7f ),
				new Vector2( radius * 0.8f, height * 0.8f ), new Vector2( radius * 1.1f, height * 0.86f ), new Vector2( radius * 1.1f, height ),
			}, 6, Run, false, false, 50f );
		}

		/// <summary>
		/// A writing desk <paramref name="length"/> long against the cases: a walnut top with a baize or leather
		/// inlay on turned legs, two drawers in its apron; on it a green banker's lamp at one end, a pile of books at
		/// the other, an open book or an inkstand between; its chair pushed in, back to the room.
		/// </summary>
		void Desk( float u, float length )
		{
			Claim( u, length + 2f );
			const float Depth = 22f;
			var y = Skirt - Depth * 0.5f - 0.5f;
			var wood = Finish();
			Kit.BevelBox( wood, At( new Vector3( u, y, DeskTop - 0.9f ) ), new Vector3( length, Depth, 1.8f ), 0.5f, Run );
			Kit.Box( TrimSkin( Next() < 0.6f ? BaizeSwatch : LeatherSwatch( Pick( 2 ) ) ), At( new Vector3( u, y + 1f, DeskTop + 0.06f ) ), new Vector3( length - 7f, Depth - 8f, 0.1f ), Run );
			Kit.Box( wood, At( new Vector3( u, y, DeskTop - 3.8f ) ), new Vector3( length - 3f, Depth - 3f, 4f ), Run );
			var apron = y - (Depth - 3f) * 0.5f;
			foreach ( var s in new[] { -1f, 1f } )
			{
				var x = u + s * length * 0.24f;
				Kit.Box( wood, At( new Vector3( x, apron - 0.25f, DeskTop - 3.8f ) ), new Vector3( length * 0.36f, 0.5f, 3f ), Run );
				Kit.Ellipsoid( Brass, At( new Vector3( x, apron - 0.9f, DeskTop - 3.8f ) ), new Vector3( 0.6f, 0.6f, 0.6f ), Run, 3, 6 );
			}
			foreach ( var sx in new[] { -1f, 1f } )
			foreach ( var sy in new[] { -1f, 1f } )
				Leg( wood, new Vector3( u + sx * (length * 0.5f - 2.2f), y + sy * (Depth * 0.5f - 2.2f), Ledge ), DeskTop - 5.8f - Ledge, 1.1f );

			var end = Next() < 0.5f ? -1f : 1f;
			BankerLamp( new Vector3( u + end * (length * 0.5f - 8f), y + 4f, DeskTop ) );
			Stack( new Vector3( u - end * (length * 0.5f - 9f), y + Range( 0f, 3f ), DeskTop ), 2 + Pick( 4 ), true );
			var middle = Next();
			if ( middle < 0.4f ) OpenBook( new Vector3( u + Range( -4f, 4f ), y - 3f, DeskTop + 0.1f ), Range( -12f, 12f ) );
			else if ( middle < 0.75f ) Chart( new Vector3( u + Range( -3f, 3f ), y - 1f, DeskTop + 0.1f ), MathF.Min( length - 22f, Range( 20f, 26f ) ) );
			else Inkstand( new Vector3( u + Range( -5f, 5f ), y + 3f, DeskTop ) );
			Chair( u + Range( -0.15f, 0.15f ) * length, y - Depth * 0.5f - 0.5f, false );
		}

		/// <summary>
		/// A side chair: turned legs, a seat in velvet or leather, a back of spindles under a top rail. Pushed in
		/// under a desk with its back to the room, or (<paramref name="facingRoom"/>) standing against the cases
		/// facing out; <paramref name="y"/> is the seat's middle, kept clear of the room and of the skirting.
		/// </summary>
		void Chair( float u, float y, bool facingRoom )
		{
			const float Seat = 17f;
			const float Half = 7.5f;
			var wood = Wood();
			var seat = TrimSkin( Next() < 0.5f ? VelvetSwatch : LeatherSwatch( Pick( 2 ) ) );
			y = Math.Clamp( y, Front + 1.5f + Half, Skirt - Half - 0.5f );
			var back = y + (facingRoom ? 6.4f : -6.4f);
			Kit.BevelBox( seat, At( new Vector3( u, y, Seat ) ), new Vector3( 15f, 15f, 1.8f ), 0.6f, Run );
			foreach ( var sx in new[] { -1f, 1f } )
			foreach ( var sy in new[] { -1f, 1f } )
				Kit.Box( wood, At( new Vector3( u + sx * 6.2f, y + sy * 6.2f, (Ledge + Seat - 0.9f) * 0.5f ) ), new Vector3( 1.3f, 1.3f, Seat - 0.9f - Ledge ), Run );
			foreach ( var sx in new[] { -1f, 1f } )
				Kit.Box( wood, At( new Vector3( u + sx * 6.2f, back, Seat + 10.5f ) ), new Vector3( 1.4f, 1.4f, 21f ), Run );
			Kit.BevelBox( wood, At( new Vector3( u, back, Seat + 20f ) ), new Vector3( 14.4f, 1.8f, 3f ), 0.5f, Run );
			for ( var k = -1; k <= 1; k++ )
				Kit.Tube( wood, At( new[] { new Vector3( u + k * 3f, back, Seat + 1f ), new Vector3( u + k * 3f, back, Seat + 18.5f ) } ), 0.45f, 4 );
		}

		/// <summary>A side chair standing against the cases, facing the room.</summary>
		void SideChair( float u )
		{
			Claim( u, 17f );
			Chair( u, Skirt - 8f, true );
		}

		/// <summary>A banker's lamp on <paramref name="foot"/>: a brass base and stem, a green glass shade lit underneath, its pull chain.</summary>
		void BankerLamp( Vector3 foot )
		{
			Kit.SmoothLathe( Brass, At( foot ), new[] { new Vector2( 3.4f, 0f ), new Vector2( 3.4f, 0.7f ), new Vector2( 2.2f, 1.4f ), new Vector2( 0.7f, 1.9f ) }, 12, Run, true, true, 40f );
			Kit.Tube( Brass, At( new[] { foot + new Vector3( 0f, 0f, 1.6f ), foot + new Vector3( 0f, 0f, 10.5f ) } ), 0.35f, 5 );
			var shade = foot + new Vector3( 0f, -1.5f, 11.4f );
			Kit.Tube( Brass, At( new[] { foot + new Vector3( 0f, 0f, 10.4f ), shade + new Vector3( 0f, 0.6f, 0.6f ) } ), 0.3f, 4 );
			Kit.Dome( TrimSkin( GreenGlassSwatch ), At( shade ), new Vector3( 6.2f, 3f, 2.6f ), Run, 3, 14 );
			Kit.Ellipsoid( new Skin( _glow, FlameGlow( 1 ) ), At( shade ), new Vector3( 5.7f, 2.5f, 0.35f ), Run, 2, 12 );
			Kit.Tube( Brass, At( new[] { shade + new Vector3( 3f, -1.4f, 0f ), shade + new Vector3( 3.2f, -1.6f, -4f ) } ), 0.12f, 3 );
		}

		/// <summary>A book lying open, its pages lifting toward the spine.</summary>
		void OpenBook( Vector3 foot, float yaw )
		{
			var turn = Yaw( yaw );
			var w = Range( 7f, 9f );
			var d = Range( 10f, 12f );
			Kit.Box( Book(), At( foot + new Vector3( 0f, 0f, 0.2f ) ), new Vector3( w * 2f + 0.8f, d + 0.6f, 0.4f ), Turn( turn ) );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( TrimSkin( ParchmentSwatch ), At( foot + turn * new Vector3( s * (w * 0.5f + 0.1f), 0f, 0.9f ) ), new Vector3( w, d, 1f ), Turn( turn * Rotation.FromPitch( s * 5f ) ) );
		}

		/// <summary>A parchment chart <paramref name="length"/> wide unrolled on a desk: the sheet, curling up into a roll at either end.</summary>
		void Chart( Vector3 foot, float length )
		{
			var turn = Yaw( Range( -8f, 8f ) );
			var parchment = TrimSkin( ParchmentSwatch );
			Kit.Box( parchment, At( foot + new Vector3( 0f, 0f, 0.1f ) ), new Vector3( length - 3f, 13f, 0.2f ), Turn( turn ) );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.SmoothLathe( parchment, At( foot + turn * new Vector3( s * (length * 0.5f - 1.5f), -7f, 1.2f ) ), new[] { new Vector2( 1.2f, 0f ), new Vector2( 1.2f, 14f ) }, 6, Turn( turn * Rotation.FromToRotation( Vector3.Up, new Vector3( 0f, 1f, 0f ) ) ), true, true );
		}

		/// <summary>A brass inkstand: a tray, two ink pots and a quill.</summary>
		void Inkstand( Vector3 foot )
		{
			Kit.BevelBox( Brass, At( foot + new Vector3( 0f, 0f, 0.4f ) ), new Vector3( 9f, 5f, 0.8f ), 0.2f, Turn( Yaw( Range( -15f, 15f ) ) ) );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.SmoothLathe( TrimSkin( InkSwatch ), At( foot + new Vector3( s * 2.4f, 0f, 0.8f ) ), new[] { new Vector2( 1.3f, 0f ), new Vector2( 1.4f, 1.6f ), new Vector2( 0.6f, 2.2f ), new Vector2( 0.6f, 2.6f ) }, 8, Run, false, true );
			Kit.Tube( TrimSkin( PorcelainSwatch ), At( new[] { foot + new Vector3( 2.4f, 0f, 2.4f ), foot + new Vector3( 4.4f, 1.5f, 9.5f ) } ), 0.3f, 4, 0.05f );
		}

		/// <summary>A cup on its saucer.</summary>
		void Cup( Vector3 foot )
		{
			var china = TrimSkin( PorcelainSwatch );
			Kit.SmoothLathe( china, At( foot ), new[] { new Vector2( 1.4f, 0f ), new Vector2( 2.4f, 0.3f ), new Vector2( 2.4f, 0.45f ) }, 10, Run, true, true );
			Kit.SmoothLathe( china, At( foot + new Vector3( 0f, 0f, 0.4f ) ), new[] { new Vector2( 0.8f, 0f ), new Vector2( 1.5f, 0.6f ), new Vector2( 1.6f, 2f ) }, 10, Run, true, false );
		}

		/// <summary>One book lying flat, its page edges showing on one side, the spine on the other.</summary>
		void FlatBook( Vector3 foot, float w, float d, float h, float yaw )
		{
			var turn = Yaw( yaw );
			var centre = foot + new Vector3( 0f, 0f, h * 0.5f );
			Kit.Box( Book(), At( centre ), new Vector3( w, d, h ), Turn( turn ) );
			if ( Next() < 0.5f ) return;

			// The fore-edge, toward the room: one face of page edges just proud of the cover's.
			Vector3 P( float x, float z ) => At( centre + turn * new Vector3( x, -d * 0.5f - 0.05f, z ) );
			var (x0, z0) = (w * 0.5f - 0.35f, h * 0.5f - 0.3f);
			Kit.Quad( TrimSkin( PagesSwatch ), P( -x0, -z0 ), P( x0, -z0 ), P( x0, z0 ), P( -x0, z0 ) );
		}

		/// <summary>A stack of <paramref name="count"/> books lying flat, the biggest at the bottom, each a little off square; <paramref name="small"/> for octavos on a table.</summary>
		void Stack( Vector3 foot, int count, bool small = false )
		{
			var scale = small ? 0.75f : 1f;
			var w = Range( 13f, 17f ) * scale;
			var d = Range( 9.5f, 12f ) * scale;
			var yaw = Range( -10f, 10f );
			var v = foot.z;
			for ( var k = 0; k < count; k++ )
			{
				var h = Range( 1.8f, 3.4f ) * scale;
				FlatBook( new Vector3( foot.x, foot.y, v ), w, d, h, yaw );
				v += h;
				w = MathF.Max( 7f, w - Range( -0.5f, 1.5f ) );
				d = MathF.Max( 6f, d - Range( -0.3f, 0.9f ) );
				yaw += Range( -9f, 9f );
			}
		}

		/// <summary>Books piled on the bay floor at <paramref name="u"/>: a tall stack, now and then a short one beside it.</summary>
		void Piles( float u )
		{
			Claim( u, 16f );
			Stack( new Vector3( u + Range( -1f, 1f ), Skirt - 7.5f, Ledge ), 3 + Pick( 6 ) );
			if ( Next() < 0.4f ) Stack( new Vector3( u + Range( -2f, 2f ), Skirt - 16f, Ledge ), 1 + Pick( 3 ), true );
		}

		/// <summary>
		/// Books standing in a row from <paramref name="u0"/> to <paramref name="u1"/>, spines to the room along depth
		/// <paramref name="y"/>, on <paramref name="v"/>, no taller than <paramref name="tallest"/>; a gap now and then,
		/// and toward the end one leaning on its neighbour.
		/// </summary>
		void BookRow( float u0, float u1, float y, float v, float tallest )
		{
			var at = u0 + Range( 0f, 2f );
			var lean = 0f;
			while ( at < u1 - 2f )
			{
				var w = Range( 1.6f, 3.6f );
				var h = tallest * Range( 0.7f, 1f );
				if ( at + w > u1 ) break;
				var d = Range( 8.5f, 11f );
				var rad = lean * MathF.PI / 180f;
				Kit.Box( Book(), At( new Vector3( at + w * 0.5f + MathF.Sin( rad ) * h * 0.5f, y + d * 0.5f, v + MathF.Cos( rad ) * h * 0.5f ) ), new Vector3( w, d, h ), Turn( Rotation.FromPitch( lean ) ) );
				at += w + (lean != 0f ? MathF.Abs( MathF.Sin( rad ) ) * h : 0.12f);
				if ( lean != 0f ) break;
				if ( u1 - at < 14f && Next() < 0.3f ) lean = Range( 14f, 26f );
				if ( Next() < 0.05f ) at += Range( 2f, 5f );
			}
		}

		/// <summary>
		/// A leather club armchair facing the room: a deep body on short feet, a seat cushion, a back leaning
		/// a little, rolled arms; oxblood or bottle green, turned a few degrees off square.
		/// </summary>
		void Armchair( float u )
		{
			Claim( u, 34f );
			const float W = 32f, D = 27f;
			var yaw = Range( -6f, 6f );
			var y = Fit( W * 0.5f, D * 0.5f, yaw, Skirt - D * 0.5f - 0.5f );
			var r = Yaw( yaw );
			var turn = Turn( r );
			Vector3 P( float x, float yy, float z ) => new Vector3( u, y, Ledge ) + r * new Vector3( x, yy, z );
			var leather = TrimSkin( LeatherSwatch( Pick( 2 ) ) );
			foreach ( var sx in new[] { -1f, 1f } )
			foreach ( var sy in new[] { -1f, 1f } )
				Kit.SmoothLathe( TrimSkin( WoodSwatch( 1 ) ), At( P( sx * (W * 0.5f - 3f), sy * (D * 0.5f - 3f), 0f ) ), new[] { new Vector2( 0.9f, 0f ), new Vector2( 1.3f, 3.2f ) }, 6, turn, false, false );
			Kit.BevelBox( leather, At( P( 0f, 0.5f, 3f + 7f ) ), new Vector3( W, D - 1f, 14f ), 2.2f, turn );
			Kit.BevelBox( leather, At( P( 0f, -2f, 17f + 2.4f ) ), new Vector3( W - 11f, D - 8f, 4.8f ), 1.9f, turn );
			Kit.BevelBox( leather, At( P( 0f, D * 0.5f - 4.5f, 17f + 11f ) ), new Vector3( W - 1f, 8f, 22f ), 2.6f, Turn( Rotation.LookAt( r * Vector3.Forward, r * new Vector3( 0f, 0.1f, 1f ) ) ) );
			foreach ( var sx in new[] { -1f, 1f } )
				Kit.BevelBox( leather, At( P( sx * (W * 0.5f - 3f), 0.5f, 17f + 4.5f ) ), new Vector3( 6f, D - 1f, 9f ), 2.6f, turn );
		}

		/// <summary>A round lamp table: a turned pedestal on three splayed feet; a table lamp on it, or a few books and a cup.</summary>
		void SideTable( float u, bool lamp )
		{
			Claim( u, 17f );
			var y = Skirt - 9f;
			var wood = Wood();
			const float Top = 24f;
			Kit.SmoothLathe( wood, At( new Vector3( u, y, Top - 1.2f ) ), new[] { new Vector2( 7.2f, 0f ), new Vector2( 7.6f, 0.6f ), new Vector2( 7.2f, 1.2f ) }, 14, Run, true, true, 60f );
			Kit.SmoothLathe( wood, At( new Vector3( u, y, Ledge + 4f ) ), new[]
			{
				new Vector2( 1.6f, 0f ), new Vector2( 1.1f, 6f ), new Vector2( 1.8f, 12f ), new Vector2( 1f, 18f ), new Vector2( 1.4f, Top - 1.2f - Ledge - 4f ),
			}, 8, Run, false, false, 50f );
			var spin = Next() * MathF.Tau;
			for ( var k = 0; k < 3; k++ )
			{
				var a = spin + k * MathF.Tau / 3f;
				var dir = new Vector3( MathF.Cos( a ), MathF.Sin( a ), 0f );
				var tip = new Vector3( u, y, Ledge ) + dir * 6f;
				if ( tip.y < Front + 1f ) tip = tip.WithY( Front + 1f );
				Kit.Tube( wood, At( new[] { new Vector3( u, y, Ledge + 5f ) + dir * 1.2f, new Vector3( u, y, Ledge + 2.2f ) + dir * 4f, tip + new Vector3( 0f, 0f, 0.4f ) } ), 0.6f, 4 );
			}
			var top = new Vector3( u, y, Top );
			if ( lamp ) TableLamp( top + new Vector3( Range( -1.5f, 1.5f ), 1f, 0f ) );
			else
			{
				Stack( top + new Vector3( -1.5f, 0.5f, 0f ), 1 + Pick( 3 ), true );
				Cup( top + new Vector3( 3.5f, -3f, 0f ) );
			}
		}

		/// <summary>A table lamp on <paramref name="foot"/>: a china, brass or bronze vase body, a linen shade lit inside.</summary>
		void TableLamp( Vector3 foot )
		{
			var body = TrimSkin( Next() < 0.5f ? PorcelainSwatch : Next() < 0.5f ? BrassSwatch : BronzeSwatch );
			Kit.SmoothLathe( body, At( foot ), new[]
			{
				new Vector2( 2.6f, 0f ), new Vector2( 2.8f, 0.6f ), new Vector2( 1.6f, 1.4f ), new Vector2( 3.2f, 5f ), new Vector2( 2.8f, 7.5f ), new Vector2( 1f, 8.6f ), new Vector2( 0.5f, 9.4f ),
			}, 10, Run, true, true, 50f, Next() );
			Kit.Tube( Brass, At( new[] { foot + new Vector3( 0f, 0f, 9f ), foot + new Vector3( 0f, 0f, 13.5f ) } ), 0.25f, 4 );
			Shade( foot + new Vector3( 0f, 0f, 11.5f ), 5.4f, 3.6f, 6.5f );
		}

		/// <summary>A linen shade from <paramref name="foot"/>, <paramref name="bottom"/> across its rim and <paramref name="top"/> across its crown, lit inside, its bulb showing under the rim.</summary>
		void Shade( Vector3 foot, float bottom, float top, float height )
		{
			Kit.SmoothLathe( TrimSkin( LinenSwatch ), At( foot ), new[] { new Vector2( bottom, 0f ), new Vector2( top, height ) }, 10, Run, false, false );
			Kit.SmoothLathe( new Skin( _glow, ShadeGlow( Pick( 2 ) ) ), At( foot ), new[] { new Vector2( top - 0.12f, height - 0.05f ), new Vector2( bottom - 0.12f, 0.05f ) }, 10, Run, false, false );
			Kit.Ellipsoid( new Skin( _glow, FlameGlow( 1 ) ), At( foot + new Vector3( 0f, 0f, height * 0.35f ) ), new Vector3( 1.3f, 1.3f, 1.7f ), Run, 3, 6 );
		}

		/// <summary>A standard lamp, a head tall: a weighted brass or bronze foot, a pole with a turned knop, a linen shade lit inside.</summary>
		void FloorLamp( float u )
		{
			Claim( u, 19f );
			var y = Skirt - Range( 9.5f, 12f );
			var foot = new Vector3( u, y, Ledge );
			var metal = TrimSkin( Next() < 0.6f ? BrassSwatch : BronzeSwatch );
			Kit.SmoothLathe( metal, At( foot ), new[] { new Vector2( 6.4f, 0f ), new Vector2( 6.4f, 0.8f ), new Vector2( 4f, 2f ), new Vector2( 1.2f, 3f ) }, 10, Run, false, false, 45f );
			var height = Range( 62f, 68f );
			Kit.Tube( metal, At( new[] { foot + new Vector3( 0f, 0f, 2.5f ), foot + new Vector3( 0f, 0f, height + 1f ) } ), 0.45f, 5 );
			Kit.SmoothLathe( metal, At( foot + new Vector3( 0f, 0f, height * 0.45f ) ), new[] { new Vector2( 0.5f, 0f ), new Vector2( 1f, 1f ), new Vector2( 0.5f, 2f ) }, 6, Run, false, false );
			Shade( foot + new Vector3( 0f, 0f, height - 3f ), Range( 8.5f, 9.5f ), Range( 5.5f, 6.5f ), Range( 9f, 11f ) );
			_tall = true;
		}

		/// <summary>A lectern: a plinth, a turned pillar, a sloped desk with a lip, a great volume lying open on it.</summary>
		void Lectern( float u )
		{
			Claim( u, 22f );
			const float Height = 42f;
			var y = Skirt - 10f;
			var wood = Finish();
			Kit.BevelBox( wood, At( new Vector3( u, y, Ledge + 1.5f ) ), new Vector3( 16f, 13f, 3f ), 0.6f, Run );
			Kit.SmoothLathe( wood, At( new Vector3( u, y, Ledge + 3f ) ), new[]
			{
				new Vector2( 3.4f, 0f ), new Vector2( 3.4f, 1.5f ), new Vector2( 2f, 3f ), new Vector2( 1.6f, 12f ), new Vector2( 2.6f, 18f ),
				new Vector2( 1.6f, 24f ), new Vector2( 1.6f, Height - 9f ), new Vector2( 3f, Height - 7f ),
			}, 8, Run, false, true, 45f );
			var desk = Rotation.LookAt( Vector3.Forward, new Vector3( 0f, -0.42f, 0.91f ) );
			var centre = new Vector3( u, y + 0.5f, Ledge + Height - 1f );
			Kit.BevelBox( wood, At( centre ), new Vector3( 20f, 15f, 1.6f ), 0.4f, Turn( desk ) );
			Kit.Box( wood, At( centre + desk * new Vector3( 0f, -7.2f, 1.4f ) ), new Vector3( 20f, 1f, 2f ), Turn( desk ) );
			var book = centre + desk * new Vector3( 0f, 0.5f, 1.1f );
			Kit.Box( Book(), At( book ), new Vector3( 19f, 13f, 0.6f ), Turn( desk ) );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( TrimSkin( ParchmentSwatch ), At( book + desk * new Vector3( s * 4.6f, 0f, 1.1f ) ), new Vector3( 8.8f, 12f, 1.6f ), Turn( desk * Rotation.FromPitch( s * 4f ) ) );
		}

		/// <summary>A floor globe: three splayed legs up to a horizon ring, the globe tilted on its axis inside a brass meridian.</summary>
		void Globe( float u )
		{
			Claim( u, 26f );
			const float R = 9f;
			var y = Skirt - 12f;
			var wood = Wood();
			var centre = new Vector3( u, y, Ledge + 31f );
			var spin = Next() * MathF.Tau;
			for ( var k = 0; k < 3; k++ )
			{
				var a = spin + k * MathF.Tau / 3f;
				var dir = new Vector3( MathF.Cos( a ), MathF.Sin( a ), 0f );
				var foot = centre.WithZ( Ledge ) + dir * 9.5f;
				if ( foot.y < Front + 1.5f ) foot = foot.WithY( Front + 1.5f );
				Kit.Tube( wood, At( new[] { foot + new Vector3( 0f, 0f, 0.5f ), centre.WithZ( Ledge + 12f ) + dir * 6.5f, centre + dir * (R + 1.6f) + new Vector3( 0f, 0f, -0.6f ) } ), 0.75f, 5 );
			}
			Kit.Tube( wood, At( Loop( centre, Vector3.Forward, new Vector3( 0f, 1f, 0f ), R + 1.6f, 20 ) ), 0.7f, 4 );
			var tilt = Rotation.FromRoll( 23.5f );
			Kit.Ellipsoid( TrimSkin( GlobeSwatch ), At( centre ), new Vector3( R, R, R ), Turn( tilt * Yaw( Next() * 360f ) ), 8, 16 );
			Kit.Tube( Brass, At( Loop( centre, tilt * Vector3.Up, tilt * new Vector3( 0f, 1f, 0f ), R + 0.8f, 22 ) ), 0.35f, 4 );
		}

		/// <summary>A bust, white plaster as a rule and bronze now and then, on a marble or walnut pedestal, a head high.</summary>
		void Bust( float u )
		{
			Claim( u, 18f );
			const float Shaft = 40f;
			var y = Skirt - 8f;
			var stone = TrimSkin( Next() < 0.75f ? MarbleSwatch : WoodSwatch( 1 ) );
			Kit.BevelBox( stone, At( new Vector3( u, y, Ledge + 2f ) ), new Vector3( 14f, 14f, 4f ), 0.6f, Run );
			Kit.SmoothLathe( stone, At( new Vector3( u, y, Ledge + 4f ) ), new[]
			{
				new Vector2( 5.6f, 0f ), new Vector2( 5.6f, 1.2f ), new Vector2( 4.6f, 2.4f ), new Vector2( 4.2f, Shaft - 3f ), new Vector2( 5.2f, Shaft - 1.6f ), new Vector2( 5.2f, Shaft ),
			}, 10, Run, false, false, 40f );
			var top = Ledge + 4f + Shaft;
			Kit.BevelBox( stone, At( new Vector3( u, y, top + 1.2f ) ), new Vector3( 13f, 13f, 2.4f ), 0.5f, Run );
			top += 2.4f;
			Bust( new Vector3( u, y, top ), 1f, TrimSkin( Next() < 0.85f ? PlasterSwatch : BronzeSwatch ) );
			_tall = true;
		}

		/// <summary>A bust on its socle at <paramref name="foot"/>, <paramref name="scale"/> times life, cast in <paramref name="cast"/>: shoulders, neck, head, nose and hair, turned to look along the room.</summary>
		void Bust( Vector3 foot, float scale, Skin cast )
		{
			var turn = Yaw( Range( -28f, 28f ) );
			Vector2[] Scaled( params Vector2[] profile ) => Array.ConvertAll( profile, p => p * scale );
			Vector3 Up( float y, float z ) => foot + new Vector3( 0f, y, z ) * scale;
			Kit.SmoothLathe( cast, At( foot ), Scaled( new Vector2( 3.2f, 0f ), new Vector2( 3.2f, 0.8f ), new Vector2( 2.2f, 1.6f ), new Vector2( 1.8f, 3f ) ), 8, Run, true, false, 45f );
			Kit.Ellipsoid( cast, At( Up( 0f, 7f ) ), new Vector3( 8f, 4.6f, 5f ) * scale, Turn( turn ), 4, 10 );
			Kit.SmoothLathe( cast, At( Up( -0.3f, 10.5f ) ), Scaled( new Vector2( 2.1f, 0f ), new Vector2( 1.9f, 4f ) ), 6, Run, false, false );
			var head = Up( -0.5f, 18.4f );
			Kit.Ellipsoid( cast, At( head ), new Vector3( 3.6f, 4.2f, 5f ) * scale, Turn( turn ), 5, 10 );
			Kit.Ellipsoid( cast, At( head + turn * new Vector3( 0f, -4f, -0.6f ) * scale ), new Vector3( 0.7f, 1.1f, 1.3f ) * scale, Turn( turn ), 2, 5 );
			Kit.Ellipsoid( cast, At( head + turn * new Vector3( 0f, 0.7f, 1.6f ) * scale ), new Vector3( 3.9f, 4f, 4.1f ) * scale, Turn( turn ), 4, 8 );
		}

		/// <summary>
		/// A longcase clock: a plinth, a trunk with its door and the pendulum swinging behind a window, a hood
		/// with a brass-ringed dial, a crest and brass finials — a little taller than a man.
		/// </summary>
		void LongcaseClock( float u )
		{
			Claim( u, 25f );
			const float D = 12f;
			var y = Skirt - D * 0.5f - 1.5f;
			var front = y - D * 0.5f;
			var wood = TrimSkin( WoodSwatch( Next() < 0.5f ? 1 : 2 ) );
			Kit.BevelBox( wood, At( new Vector3( u, y, Ledge + 5f ) ), new Vector3( 21f, D + 2f, 10f ), 0.6f, Run );
			Kit.Box( wood, At( new Vector3( u, y, Ledge + 32f ) ), new Vector3( 16f, D, 44f ), Run );
			Kit.BevelBox( wood, At( new Vector3( u, front - 0.3f, Ledge + 31f ) ), new Vector3( 10f, 0.8f, 32f ), 0.4f, Run );
			Kit.Box( TrimSkin( ShadowSwatch ), At( new Vector3( u, front - 0.75f, Ledge + 34f ) ), new Vector3( 6f, 0.2f, 20f ), Run );
			var swing = Range( -6f, 6f );
			Kit.Tube( Brass, At( new[] { new Vector3( u, front - 0.9f, Ledge + 43f ), new Vector3( u + swing * 0.25f, front - 0.9f, Ledge + 28.5f ) } ), 0.2f, 3 );
			Kit.SmoothLathe( Brass, At( new Vector3( u + swing * 0.28f, front - 0.8f, Ledge + 27f ) ), new[] { new Vector2( 2.4f, 0f ), new Vector2( 2.4f, 0.4f ) }, 12, Facing, false, true );
			Kit.BevelBox( wood, At( new Vector3( u, y, Ledge + 55f ) ), new Vector3( 20f, D + 2f, 2f ), 0.4f, Run );
			Kit.BevelBox( wood, At( new Vector3( u, y, Ledge + 66f ) ), new Vector3( 18f, D + 1f, 20f ), 0.5f, Run );
			var dial = new Vector3( u, front - 0.5f, Ledge + 66f );
			Kit.SmoothLathe( Brass, At( dial ), new[] { new Vector2( 7f, 0f ), new Vector2( 7f, 0.3f ) }, 18, Facing, false, true );
			Kit.SmoothLathe( TrimSkin( DialSwatch ), At( dial + new Vector3( 0f, -0.3f, 0f ) ), new[] { new Vector2( 6.2f, 0f ), new Vector2( 6.2f, 0.3f ) }, 18, Facing, false, true );
			var minutes = Next() * 720f;
			foreach ( var (turns, length, width) in new[] { (minutes / 720f, 3.4f, 0.6f), (minutes % 60f / 60f, 5f, 0.4f) } )
			{
				var a = turns * MathF.Tau;
				var dir = new Vector3( MathF.Sin( a ), 0f, MathF.Cos( a ) );
				Kit.Box( TrimSkin( InkSwatch ), At( dial + new Vector3( 0f, -0.75f, 0f ) + dir * (length * 0.4f) ), new Vector3( width, 0.15f, length ), Turn( Rotation.LookAt( Vector3.Cross( ToRoom, dir ), dir ) ) );
			}
			Kit.BevelBox( wood, At( new Vector3( u, y, Ledge + 77.2f ) ), new Vector3( 21f, D + 2.5f, 2.4f ), 0.5f, Run );
			foreach ( var x in new[] { -8f, 0f, 8f } )
				Kit.SmoothLathe( Brass, At( new Vector3( u + x, y - 1f, Ledge + 78.4f ) ), new[]
				{
					new Vector2( 1f, 0f ), new Vector2( 1.3f, 0.8f ), new Vector2( 0.5f, 1.6f ), new Vector2( 0.25f, x == 0f ? 4.5f : 3.2f ), new Vector2( 0f, x == 0f ? 5f : 3.6f ),
				}, 8, Run, true, false, 40f );
			_tall = true;
		}

		/// <summary>A card catalogue <paramref name="width"/> wide on a stand: a grid of small drawers with brass pulls and paper labels, one or two left out; a lamp or books on top.</summary>
		void Catalogue( float u, float width )
		{
			Claim( u, width + 2f );
			const float D = 17f;
			const float Stand = 14f;
			const float Body = 27f;
			const int Rows = 5;
			var y = Skirt - D * 0.5f - 0.5f;
			var wood = Finish();
			foreach ( var sx in new[] { -1f, 1f } )
			foreach ( var sy in new[] { -1f, 1f } )
				Leg( wood, new Vector3( u + sx * (width * 0.5f - 2f), y + sy * (D * 0.5f - 2f), Ledge ), Stand, 1f );
			Kit.Box( wood, At( new Vector3( u, y, Ledge + Stand - 1.5f ) ), new Vector3( width - 2f, D - 2f, 3f ), Run );
			Kit.BevelBox( wood, At( new Vector3( u, y, Ledge + Stand + Body * 0.5f ) ), new Vector3( width, D, Body ), 0.6f, Run );
			Kit.BevelBox( wood, At( new Vector3( u, y, Ledge + Stand + Body + 0.8f ) ), new Vector3( width + 2f, D + 1.5f, 1.6f ), 0.4f, Run );
			var columns = Math.Max( 3, (int)(width / 6.6f) );
			var cw = (width - 2f) / columns;
			var rh = (Body - 2f) / Rows;
			var front = y - D * 0.5f;
			var drawers = Next() < 0.5f ? TrimSkin( FinishSwatch( Pick( 3 ) ) ) : TrimSkin( WoodSwatch( Pick( 3 ) ) );
			for ( var c = 0; c < columns; c++ )
			for ( var r = 0; r < Rows; r++ )
			{
				var x = u - width * 0.5f + 1f + cw * (c + 0.5f);
				var z = Ledge + Stand + 1f + rh * (r + 0.5f);
				if ( Next() < 0.04f )
				{
					var pulled = Range( 2f, 5f );
					Kit.Box( drawers, At( new Vector3( x, front - 0.25f - pulled * 0.5f, z ) ), new Vector3( cw - 0.7f, 0.5f + pulled, rh - 0.7f ), Run );
					Flat( Brass, x, 1.8f, z - rh * 0.18f - 0.35f, z - rh * 0.18f + 0.35f, front - 0.55f - pulled );
					continue;
				}
				Flat( drawers, x, cw - 0.7f, z - (rh - 0.7f) * 0.5f, z + (rh - 0.7f) * 0.5f, front - 0.1f );
				Flat( Brass, x, 1.8f, z - rh * 0.18f - 0.35f, z - rh * 0.18f + 0.35f, front - 0.2f );
			}
			var top = new Vector3( u, y, Ledge + Stand + Body + 1.6f );
			var side = Next() < 0.5f ? -1f : 1f;
			TableLamp( top + new Vector3( side * width * 0.28f, 1.5f, 0f ) );
			if ( Next() < 0.5f ) Bookends( top + new Vector3( -side * width * 0.12f, 0f, 0f ), width * 0.4f );
			else Stack( top + new Vector3( -side * width * 0.22f, 0f, 0f ), 2 + Pick( 3 ), true );
			_tall = true;
		}

		/// <summary>A short row of standing books between two brass bookends, <paramref name="length"/> long.</summary>
		void Bookends( Vector3 foot, float length )
		{
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.Box( Brass, At( foot + new Vector3( s * length * 0.5f, 0f, 0.2f ) ), new Vector3( 4f, 7f, 0.4f ), Run );
				Kit.BevelBox( Brass, At( foot + new Vector3( s * (length * 0.5f + 0.6f), 0f, 4.5f ) ), new Vector3( 1.2f, 7f, 9f ), 0.3f, Run );
			}
			BookRow( foot.x - length * 0.5f, foot.x + length * 0.5f, foot.y - 4.5f, foot.z + 0.4f, 11f );
		}

		/// <summary>A book trolley <paramref name="length"/> long on castors, books standing along both shelves, a few lying on top.</summary>
		void Trolley( float u, float length )
		{
			Claim( u, length + 2f );
			const float D = 15f;
			var y = Skirt - D * 0.5f - 0.5f;
			var wood = Finish();
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.BevelBox( wood, At( new Vector3( u + s * (length * 0.5f - 0.7f), y, Ledge + 18f ) ), new Vector3( 1.4f, D, 30f ), 0.3f, Run );
				foreach ( var sy in new[] { -1f, 1f } )
					Kit.Ellipsoid( TrimSkin( IronSwatch ), At( new Vector3( u + s * (length * 0.5f - 2f), y + sy * (D * 0.5f - 2f), Ledge + 1.5f ) ), new Vector3( 1.5f, 1.5f, 1.5f ), Run, 3, 6 );
			}
			foreach ( var z in new[] { 4.5f, 18.5f, 32.5f } )
				Kit.Box( wood, At( new Vector3( u, y, Ledge + z ) ), new Vector3( length - 2.8f, D, 1.2f ), Run );
			foreach ( var z in new[] { 5.1f, 19.1f } )
				BookRow( u - length * 0.5f + 1.6f, u + length * 0.5f - 1.6f, y - D * 0.5f + 1f, Ledge + z, 12.5f );
			Stack( new Vector3( u + Range( -6f, 6f ), y, Ledge + 33.1f ), 1 + Pick( 3 ), true );
		}

		/// <summary>An upholstered bench <paramref name="length"/> long on turned legs, now and then a pile of books left on it.</summary>
		void Bench( float u, float length )
		{
			Claim( u, length + 2f );
			const float D = 14f;
			const float Seat = 17f;
			var y = Skirt - D * 0.5f - 0.5f;
			var wood = Wood();
			Kit.BevelBox( wood, At( new Vector3( u, y, Seat - 1.5f ) ), new Vector3( length, D, 3f ), 0.4f, Run );
			Kit.BevelBox( TrimSkin( Next() < 0.6f ? VelvetSwatch : LeatherSwatch( Pick( 2 ) ) ), At( new Vector3( u, y, Seat + 1.6f ) ), new Vector3( length - 1.2f, D - 1.2f, 3.2f ), 1.3f, Run );
			foreach ( var sx in new[] { -1f, 1f } )
			foreach ( var sy in new[] { -1f, 1f } )
				Leg( wood, new Vector3( u + sx * (length * 0.5f - 2f), y + sy * (D * 0.5f - 2f), Ledge ), Seat - 3f - Ledge, 1.1f );
			if ( Next() < 0.55f ) Stack( new Vector3( u + Range( -0.3f, 0.3f ) * length, y + Range( -1f, 1f ), Seat + 3.2f ), 1 + Pick( 3 ) );
		}

		/// <summary>A kick stool, the round step every library rolls about: a waisted drum with turned rings and a leather tread.</summary>
		void KickStool( float u )
		{
			Claim( u, 15f );
			_stool = true;
			var foot = new Vector3( u, Skirt - Range( 8f, 10f ), Ledge );
			var body = TrimSkin( Next() < 0.6f ? WoodSwatch( Pick( 3 ) ) : BronzeSwatch );
			Kit.SmoothLathe( body, At( foot ), new[]
			{
				new Vector2( 6.6f, 0f ), new Vector2( 6.8f, 1.2f ), new Vector2( 6.2f, 2f ), new Vector2( 5.4f, 8f ), new Vector2( 5.8f, 8.6f ), new Vector2( 5.2f, 9.2f ),
				new Vector2( 4.9f, 15f ), new Vector2( 5.4f, 15.6f ),
			}, 12, Run, true, false, 40f, Next() );
			Kit.SmoothLathe( TrimSkin( LeatherSwatch( Pick( 2 ) ) ), At( foot + new Vector3( 0f, 0f, 15.6f ) ), new[] { new Vector2( 5.4f, 0f ), new Vector2( 5.3f, 0.9f ), new Vector2( 4.4f, 1.3f ) }, 12, Run, false, true, 50f );
		}

		/// <summary>The easel once a wall; where the wall has had it, an armillary sphere (or a standard lamp) in its place.</summary>
		void EaselOrSphere( float u )
		{
			if ( _heroes.Add( "easel" ) ) Easel( u );
			else if ( _heroes.Add( "armillary" ) ) Armillary( u );
			else FloorLamp( u );
		}

		/// <summary>
		/// A painter's easel a head high: two front legs to an apex, a back leg, a ledge across; on it a study in hand
		/// on its stretcher, leaning back with the legs — a primed canvas, a wash of ground, a still life blocked in (a
		/// jug, fruit, the table's edge), the last strip still bare but for its charcoal lines. Its own picture, never
		/// one of the gallery's.
		/// </summary>
		void Easel( float u )
		{
			Claim( u, 28f );
			_tall = true;
			var wood = Wood();
			const float FrontY = 7f;
			const float Apex = 80f;
			const float Lean = 8f;
			var up = new Vector3( 0f, Lean, Apex - Ledge ).Normal;
			var turn = Turn( Rotation.LookAt( Vector3.Forward, up ) );
			Vector3 OnLegs( float x, float v ) => new Vector3( u + x, FrontY + (v - Ledge) / (Apex - Ledge) * Lean, v );
			foreach ( var s in new[] { -1f, 1f } )
			{
				var foot = OnLegs( s * 10f, Ledge );
				var top = OnLegs( s * 1.2f, Apex );
				var leg = top - foot;
				Kit.Box( wood, At( (foot + top) * 0.5f ), new Vector3( 1.4f, 1.2f, leg.Length ), Turn( Rotation.LookAt( leg.Normal, new Vector3( 0f, -1f, 0f ) ) * Rotation.FromPitch( 90f ) ) );
			}
			var back = new Vector3( u, FrontY + 26f, Ledge );
			var apex = OnLegs( 0f, Apex - 3f ) + new Vector3( 0f, 1f, 0f );
			Kit.Tube( wood, At( new[] { back, apex } ), 0.65f, 4 );
			const float Shelf = 32f;
			Kit.Box( wood, At( OnLegs( 0f, Shelf ) + new Vector3( 0f, -1.4f, 0f ) ), new Vector3( 22f, 3.4f, 1.2f ), turn );

			var portrait = Next() < 0.45f;
			var (w, h) = portrait ? (Range( 24f, 28f ), Range( 34f, 40f )) : (Range( 36f, 42f ), Range( 24f, 28f ));
			var foot0 = OnLegs( 0f, Shelf + 0.8f ) + new Vector3( 0f, -2.4f, 0f );
			var toRoom = Vector3.Cross( Vector3.Forward, up );
			void Patch( Skin skin, float x0, float y0, float x1, float y1, float lift )
			{
				Vector3 C( float x, float y ) => foot0 + Vector3.Forward * ((x - 0.5f) * w) + up * (y * h) + toRoom * lift;
				Kit.Quad( skin, At( C( x0, y0 ) ), At( C( x1, y0 ) ), At( C( x1, y1 ) ), At( C( x0, y1 ) ) );
			}
			Patch( TrimSkin( LinenSwatch ), 0f, 0f, 1f, 1f, 0f );
			var done = Range( 0.62f, 0.8f );
			Patch( TrimSkin( Next() < 0.5f ? VelvetSwatch : Next() < 0.5f ? BaizeSwatch : FinishSwatch( Pick( 3 ) ) ), 0.05f, 0.05f, done, 0.95f, 0.04f );
			Patch( TrimSkin( WoodSwatch( Pick( 3 ) ) ), 0.05f, 0.05f, done, Range( 0.24f, 0.32f ), 0.08f );
			var jug = Range( 0.14f, 0.24f );
			Patch( TrimSkin( Next() < 0.5f ? PorcelainSwatch : GreenGlassSwatch ), jug, 0.28f, jug + Range( 0.12f, 0.16f ), Range( 0.56f, 0.68f ), 0.12f );
			var at = jug + 0.2f;
			for ( var k = 2 + Pick( 2 ); k > 0 && at < done - 0.08f; k-- )
			{
				var size = Range( 0.07f, 0.1f );
				Patch( TrimSkin( Next() < 0.4f ? GiltSwatch : Next() < 0.5f ? VelvetSwatch : BaizeSwatch ), at, 0.28f, at + size, 0.28f + size * w / h, 0.12f );
				at += size + Range( 0.02f, 0.05f );
			}
			Patch( TrimSkin( InkSwatch ), done + 0.04f, Range( 0.25f, 0.3f ), 0.95f, Range( 0.31f, 0.33f ), 0.04f );
			var line = Range( done + 0.06f, 0.88f );
			Patch( TrimSkin( InkSwatch ), line, 0.3f, line + 0.015f, Range( 0.55f, 0.7f ), 0.04f );
			Kit.Box( wood, At( foot0 + up * (h * 0.5f) + new Vector3( 0f, 0.9f, 0f ) ), new Vector3( w, 1.6f, h ), turn );
		}

		/// <summary>A pedestal like a bust's, and on it a brass armillary sphere: rings for the equator, two meridians and the ecliptic, tilted on its axis round a small earth.</summary>
		void Armillary( float u )
		{
			Claim( u, 19f );
			_tall = true;
			const float Shaft = 34f;
			var y = Skirt - 9f;
			var stone = TrimSkin( Next() < 0.5f ? MarbleSwatch : WoodSwatch( 1 ) );
			Kit.BevelBox( stone, At( new Vector3( u, y, Ledge + 2f ) ), new Vector3( 14f, 14f, 4f ), 0.6f, Run );
			Kit.SmoothLathe( stone, At( new Vector3( u, y, Ledge + 4f ) ), new[]
			{
				new Vector2( 5.4f, 0f ), new Vector2( 5.4f, 1.2f ), new Vector2( 4.4f, 2.4f ), new Vector2( 3.8f, Shaft - 3f ), new Vector2( 5f, Shaft - 1.6f ), new Vector2( 5f, Shaft ),
			}, 10, Run, false, true, 40f );
			var top = Ledge + 4f + Shaft;
			Kit.SmoothLathe( Brass, At( new Vector3( u, y, top ) ), new[] { new Vector2( 3.6f, 0f ), new Vector2( 3.6f, 0.8f ), new Vector2( 1f, 2f ), new Vector2( 0.7f, 5f ), new Vector2( 1.4f, 6f ) }, 10, Run, false, false, 45f );
			const float R = 7.5f;
			var centre = new Vector3( u, y, top + 6f + R );
			var tilt = Yaw( Range( -40f, 40f ) ) * Rotation.FromRoll( 23.5f );
			var axis = tilt * Vector3.Up;
			Kit.Tube( Brass, At( new[] { centre - axis * (R + 2f), centre + axis * (R + 2f) } ), 0.25f, 4 );
			Kit.Ellipsoid( TrimSkin( GlobeSwatch ), At( centre ), new Vector3( 1.8f, 1.8f, 1.8f ), Turn( tilt ), 4, 8 );
			Kit.Tube( Brass, At( Loop( centre, tilt * Vector3.Forward, tilt * new Vector3( 0f, 1f, 0f ), R, 20 ) ), 0.3f, 4 );
			Kit.Tube( Brass, At( Loop( centre, axis, tilt * Vector3.Forward, R + 0.4f, 20 ) ), 0.3f, 4 );
			Kit.Tube( Brass, At( Loop( centre, axis, tilt * new Vector3( 0f, 1f, 0f ), R + 0.8f, 20 ) ), 0.35f, 4 );
			var ecliptic = tilt * Rotation.FromPitch( 23.5f );
			Kit.Tube( TrimSkin( GiltSwatch ), At( Loop( centre, ecliptic * Vector3.Forward, ecliptic * new Vector3( 0f, 1f, 0f ), R - 0.4f, 20 ) ), 0.55f, 4 );
		}

		/// <summary>A bentwood hat stand: splayed feet, a turned pole, curled hooks; a bowler on one, a scarf and an umbrella hanging from others.</summary>
		void HatStand( float u )
		{
			Claim( u, 19f );
			_tall = true;
			var y = Skirt - 9.5f;
			var wood = TrimSkin( WoodSwatch( Next() < 0.5f ? 1 : 0 ) );
			var spin = Next() * MathF.Tau;
			for ( var k = 0; k < 4; k++ )
			{
				var a = spin + k * MathF.Tau / 4f;
				var dir = new Vector3( MathF.Cos( a ), MathF.Sin( a ), 0f );
				var tip = new Vector3( u, y, Ledge + 0.6f ) + dir * 8f;
				if ( tip.y < Front + 1f ) tip = tip.WithY( Front + 1f );
				Kit.Tube( wood, At( new[] { new Vector3( u, y, Ledge + 9f ) + dir * 0.8f, new Vector3( u, y, Ledge + 4f ) + dir * 3f, tip + Vector3.Up * 0.8f, tip } ), 0.6f, 4 );
			}
			const float Pole = 70f;
			Kit.SmoothLathe( wood, At( new Vector3( u, y, Ledge ) ), new[]
			{
				new Vector2( 1.3f, 4f ), new Vector2( 1.6f, 10f ), new Vector2( 1f, 14f ), new Vector2( 0.9f, Pole - 6f ), new Vector2( 1.3f, Pole - 1f ), new Vector2( 0.5f, Pole + 1f ),
			}, 8, Run, false, true, 45f );
			var hooks = new List<Vector3>();
			for ( var k = 0; k < 6; k++ )
			{
				var a = spin + 0.4f + k * MathF.Tau / 6f;
				var dir = new Vector3( MathF.Cos( a ), MathF.Sin( a ) * 0.8f, 0f );
				var v = Ledge + Pole - 4f - (k % 2) * 4f;
				var root = new Vector3( u, y, v - 6f );
				Kit.Tube( wood, At( new[] { root, root + dir * 2.4f + Vector3.Up * 3f, root + dir * 4.6f + Vector3.Up * 4.4f, root + dir * 5.6f + Vector3.Up * 2.6f } ), 0.4f, 4 );
				hooks.Add( root + dir * 4.6f + Vector3.Up * 4.4f );
			}

			// A bowler on the highest hook, a scarf hanging from another, an umbrella by its crook from a third.
			var hat = hooks[0] + Vector3.Up * 0.6f;
			Kit.SmoothLathe( TrimSkin( InkSwatch ), At( hat ), new[] { new Vector2( 5.2f, 0f ), new Vector2( 5.4f, 0.4f ), new Vector2( 3.4f, 0.8f ), new Vector2( 3.6f, 3.2f ), new Vector2( 2.4f, 5.2f ), new Vector2( 0f, 5.6f ) }, 12, Turn( Rotation.FromRoll( Range( -12f, 12f ) ) ), true, false, 50f );
			var scarf = hooks[3];
			var fall = Range( 22f, 30f );
			var cloth = TrimSkin( Next() < 0.5f ? VelvetSwatch : LeatherSwatch( 1 ) );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Tube( cloth, At( new[] { scarf, scarf + new Vector3( s * 1.6f, -0.6f, -fall * 0.4f ), scarf + new Vector3( s * 2.2f, -0.8f, -fall * (s > 0f ? 1f : 0.8f) ) } ), 1.2f, 5, 1.6f );
			var crook = hooks[4];
			Kit.Tube( TrimSkin( WoodSwatch( 1 ) ), At( new[] { crook + new Vector3( 1.2f, 0f, -1.6f ), crook + new Vector3( 0f, 0f, 0.2f ), crook + new Vector3( -1.2f, 0f, -1.6f ), crook + new Vector3( -1.2f, 0f, -4f ) } ), 0.4f, 4 );
			Kit.SmoothLathe( TrimSkin( InkSwatch ), At( crook + new Vector3( -1.2f, 0f, -30f ) ), new[] { new Vector2( 0.2f, 0f ), new Vector2( 1.8f, 6f ), new Vector2( 1.3f, 22f ), new Vector2( 0.4f, 26f ) }, 8, Run, false, false, 50f );
		}

		/// <summary>A brass umbrella stand, a furled umbrella or two in it, crook handles up.</summary>
		void Umbrellas( float u )
		{
			Claim( u, 12f );
			_umbrellas = true;
			var foot = new Vector3( u, Skirt - 6f, Ledge );
			Kit.SmoothLathe( Brass, At( foot ), new[] { new Vector2( 4.4f, 0f ), new Vector2( 4.6f, 1f ), new Vector2( 4.1f, 3f ), new Vector2( 4.1f, 19f ), new Vector2( 4.6f, 20f ) }, 12, Run, true, false, 40f );
			Kit.SmoothLathe( TrimSkin( ShadowSwatch ), At( foot + new Vector3( 0f, 0f, 18f ) ), new[] { new Vector2( 4f, 0f ), new Vector2( 4f, 0.1f ) }, 12, Run, false, true );
			for ( var k = 1 + Pick( 2 ); k > 0; k-- )
			{
				var at = foot + new Vector3( Range( -1.6f, 1.6f ), Range( -1f, 1f ), 2f );
				var lean = new Vector3( Range( -0.25f, 0.25f ), Range( 0.02f, 0.15f ), 1f ).Normal;
				var cloth = TrimSkin( Next() < 0.5f ? InkSwatch : LeatherSwatch( 1 ) );
				Kit.SmoothLathe( cloth, At( at ), new[] { new Vector2( 0.3f, 0f ), new Vector2( 2f, 6f ), new Vector2( 1.4f, 20f ), new Vector2( 0.4f, 24f ) }, 8, Turn( Rotation.FromToRotation( Vector3.Up, lean ) ), false, false, 50f );
				var top = at + lean * 24f;
				var hook = top + lean * 6f;
				var bend = new Vector3( Range( -1f, 1f ), -1f, 0f ).Normal;
				Kit.Tube( TrimSkin( WoodSwatch( 1 ) ), At( new[] { top, hook, hook + bend * 1.6f + Vector3.Up * 1.4f, hook + bend * 3.2f, hook + bend * 3.4f - Vector3.Up * 1.4f } ), 0.45f, 5 );
			}
		}
	}
}
