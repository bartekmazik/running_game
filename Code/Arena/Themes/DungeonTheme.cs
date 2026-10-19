using System;
using System.Collections.Generic;

/// <summary>
/// Floor 7 (index 6): a dungeon. Underfoot, granite flagstones — two or three to a block, cut
/// irregular and laid in damp mortar, each stone its own tone and lean (slate, lilac, warm grey),
/// flecked with crystals, its edges worn round and chipped, moss in the joints. Overhead (the
/// undersides of the workshop floor), rough rubble stone, pillowed and pitted, set in deep lime
/// mortar, with damp and soot.
/// </summary>
public sealed class DungeonTheme : ArenaTheme
{
	public override string Name => "Dungeon";
	public override int Floor => 6;
	public override Color LightColor => new( 1.40f, 0.82f, 0.48f );

	static Color Granite => Palette.Hex( 0x8D8798 );
	static Color GraniteLight => Palette.Hex( 0xA49FAE );
	static Color GraniteShade => Palette.Hex( 0x6F6A7A );

	/// <summary>Stones that lean warm grey and stones that lean toward the floor's slate lilac — both close to grey.</summary>
	static Color GraniteWarm => Palette.Hex( 0x958C89 );
	static Color GraniteLilac => Palette.Hex( 0x8C849B );

	/// <summary>Feldspar and quartz (pale) and mica (dark) in the granite.</summary>
	static Color Feldspar => Palette.Hex( 0xBDB7BE );
	static Color Mica => Palette.Hex( 0x45414B );

	/// <summary>
	/// Lime mortar in the walls' joints and the trim's: warm and sandy against the cool granite, about
	/// as light — a filled joint reads as a band of another material, not as a dark crack.
	/// </summary>
	static Color Mortar => Palette.Hex( 0x8A847E );

	/// <summary>
	/// The joints between the floor's flags: dark, neutral grime packed into the lime. The torches face
	/// the chamfers that carry the tile joints, and a light joint lit warm drew every tile edge as a
	/// salmon line; a dark one stays a dark line under any light (6b review).
	/// </summary>
	static Color FloorJoint => Palette.Hex( 0x4A464C );

	static Color Moss => Palette.Hex( 0x7FA06A );

	/// <summary>Salts left at the tide line of rising damp: chalky off-white.</summary>
	static Color Salt => Palette.Hex( 0xCFCBC4 );

	/// <summary>Damp patches on the stone.</summary>
	static Color Damp => Palette.Hex( 0x6D6874 );

	static Color Rubble => Palette.Hex( 0x5E5866 );
	static Color RubbleLight => Palette.Hex( 0x77707C );
	static Color RubbleDark => Palette.Hex( 0x46414C );
	static Color RubbleWarm => Palette.Hex( 0x665C5C );
	static Color Lime => Palette.Hex( 0x75707A );
	static Color Soot => Palette.Hex( 0x2E2B33 );

	/// <summary>
	/// The walls' stone, greyer and warmer than the floor's flags: under the torches a lilac granite
	/// wall read pink. A neutral grey, a warm grey and a slate, with lilac only as a lean in a few.
	/// </summary>
	static Color WallStoneGrey => Palette.Hex( 0x7E7A77 );
	static Color WallStoneWarm => Palette.Hex( 0x85796E );
	static Color WallStoneSlate => Palette.Hex( 0x67676F );

	/// <summary>Iron leaching out of the odd stone: a dull ochre-brown.</summary>
	static Color IronStain => Palette.Hex( 0x8A6A4E );

	/// <summary>
	/// The structural front layer, then the dressing. Over every bay a stone arch springs from imposts on
	/// the pilasters to just under the corbel course, its voussoirs cut one by one and its spandrels laid in
	/// the wall's own ashlar, so the bays read as the vaulted cells of an undercroft. Each bay gets a set
	/// dealt so that no two neighbours match (nor the bays either side of a corner): an iron-banded cell
	/// door, an armoury rack of spears and a halberd (once a wall), barrels and crates in store, shackles over
	/// a heap of straw, a trestle table laid with a candle and a jug, a woodpile and its chopping block, or a
	/// tall shelf of pots — and one bay in the room an ossuary niche of skulls. What a set leaves bare is
	/// filled: on the floor something a head high first where the bay has nothing tall (another cell door,
	/// barrels stacked, a shelf of pots or crates piled high), then barrels, crates, sacks, straw, stools and
	/// buckets; on the wall at a player's chest and again over the heads of the room shields, shelves of pots,
	/// cupboards, weapons laid on pegs, cloaks on a peg rail, proclamations and the gaol's keys. Under every
	/// arch hangs one piece dealt per wall, as wide as the arch lets it be: a lilac banner with its emblem in
	/// pale stone, a pair of narrower banners, a barred window between two shields, a tapestry, a cage on its
	/// chain over a chain swagged from wall rings (one a wall) or a trophy of crossed spears behind a shield
	/// (one a wall). Torches burn in iron brackets on about two in five of the pilasters; the room's six lamps
	/// burn in six of them. Each wall draws from its own stream.
	/// </summary>
	public override void Dress( FloorContext room )
	{
		var bays = BuildFrontLayer( room, new FrontStyle { Post = 34f, Pier = 80f, Corner = 36f, Cornice = 28f, Skirting = 12f, SkirtingDepth = 6f } );

		// Self-illumination is an exponent (2^b): at half the paint the flames keep their amber under bloom.
		Glow.Set( "g_flSelfIllumBrightness", -1f );
		var props = new Props( room, Trim, Glow, Wall );
		var lamps = LampSpots( 6, Seed + 900 );

		var walls = new Random( Seed + 1700 );
		var ossuary = walls.Next( 4 );

		// The walls go round the room in the order their ends meet (+X, −Y, −X, +Y: each wall's far end is the
		// next one's start), so the deal can keep the two bays either side of a corner apart as well.
		var corners = new List<Props.Look>();
		var lit = new List<(int Side, float U, float Brightness)>();
		foreach ( var side in new[] { 0, 3, 2, 1 } )
		{
			props.Begin( side, Seed + 1000 + side * 137 );
			var all = bays.FindAll( b => b.Side == side );
			var wall = all.FindAll( b => b.Width > 60f );
			props.StartWall();
			props.Arches( all );

			var looks = props.Deal( new List<Props.Look>
			{
				Props.Look.Cell, Props.Look.Armoury, Props.Look.Stores, Props.Look.Prisoner, Props.Look.Table, Props.Look.Woodpile, Props.Look.Pantry,
			}, wall.Count, ( slot, look ) => !(slot == 0 && corners.Count > 0 && look == corners[^1]) && !(slot == wall.Count - 1 && side == 1 && corners.Count > 0 && look == corners[0]) );
			props.Once( looks, Props.Look.Armoury, new List<Props.Look> { Props.Look.Stores, Props.Look.Table, Props.Look.Pantry } );
			if ( side == ossuary ) looks[1 + (int)(props.Next() * (wall.Count - 2))] = Props.Look.Ossuary;
			if ( corners.Count == 0 ) corners.Add( looks[0] );
			corners.Add( looks[^1] );

			// One piece under every arch, each as wide as the arch lets it be. Windows and tapestries come twice in
			// the deck (a dungeon has several); the cage and the trophy once a wall.
			var uppers = props.Deal( new List<Props.Upper>
			{
				Props.Upper.Banner, Props.Upper.Window, Props.Upper.Tapestry, Props.Upper.Banners, Props.Upper.Window, Props.Upper.Tapestry, Props.Upper.Cage, Props.Upper.Trophy,
			}, wall.Count );
			var spares = new List<Props.Upper> { Props.Upper.Banner, Props.Upper.Window, Props.Upper.Tapestry, Props.Upper.Banners };
			props.Once( uppers, Props.Upper.Cage, spares );
			props.Once( uppers, Props.Upper.Trophy, spares );
			props.ReserveShields( (looks.Contains( Props.Look.Armoury ) ? 1 : 0) + (uppers.Contains( Props.Upper.Trophy ) ? 1 : 0) );

			for ( var k = 0; k < wall.Count; k++ )
			{
				props.Furnish( looks[k], wall[k] );
				props.Above( uppers[k], wall[k] );
			}

			// Torches on about two pilasters in five, and always on the one nearest each of this wall's lamps.
			var posts = Props.Posts( all );
			var lampPosts = new List<int>();
			foreach ( var lamp in lamps.FindAll( l => l.Side == side ) )
			{
				var nearest = 0;
				for ( var p = 1; p < posts.Count; p++ )
					if ( MathF.Abs( posts[p] - lamp.U ) < MathF.Abs( posts[nearest] - lamp.U ) ) nearest = p;
				lampPosts.Add( nearest );
				lit.Add( (side, posts[nearest], lamp.Brightness) );
			}
			for ( var p = 0; p < posts.Count; p++ )
				if ( lampPosts.Contains( p ) || props.Next() < 0.4f ) props.Torch( posts[p] );
		}

		foreach ( var (side, u, brightness) in lit )
			AddLamp( room, side, u, Props.FlameV, Props.FlameDepth, LightColor * 4.6f * brightness );
	}

	/// <summary>The lit parts: a torch flame in warm amber, its paler core, and the glowing pitch of its head.</summary>
	protected override PaintedSurface PaintGlow()
	{
		const int Size = 32;
		var painter = new Painter( Size, Palette.Hex( 0xFFC27A ), 0.3f );
		painter.Fill( 16, 0, 16, 16, Palette.Hex( 0xFFE2B4 ), roughness: 0.3f );
		painter.Fill( 16, 16, 16, 16, Palette.Hex( 0xE9A06A ), roughness: 0.6f );
		var glow = new float[Size * Size];
		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
			glow[y * Size + x] = x < 16 || y < 16 ? 1f : 0.55f;
		painter.AddGlow( glow );
		return painter.Bake( "arena_6_glow", ArenaGenerator.GlowMaterialPath, 1f );
	}

	static Rect FlameGlow => new( 0.06f, 0.06f, 0.38f, 0.88f );
	static Rect CoreGlow => new( 0.56f, 0.06f, 0.38f, 0.38f );
	static Rect EmberGlow => new( 0.56f, 0.56f, 0.38f, 0.38f );

	// The props' paints in the trim atlas's infill band (see PaintSwatches).
	static Rect BannerSwatch( int k ) => Swatch( k * 48, 0, 48, 128 );
	static Rect WindowSwatch => Swatch( 144, 0, 48, 64 );
	static Rect NicheSwatch => Swatch( 144, 64, 48, 32 );
	static Rect NoticeSwatch( int k ) => Swatch( 144 + k * 24, 96, 24, 32 );
	static Rect DoorSwatch => Swatch( 192, 0, 64, 128 );
	static Rect StaveSwatch => Swatch( 256, 0, 64, 32 );
	static Rect CrateSwatch => Swatch( 256, 32, 64, 32 );
	static Rect StrawSwatch => Swatch( 256, 64, 64, 32 );
	static Rect SackSwatch => Swatch( 256, 96, 64, 32 );
	static Rect StoneSwatch( int k ) => Swatch( 320, k * 32, 64, 32 );
	static Rect TapestrySwatch( int k ) => k == 0 ? Swatch( 384, 0, 64, 64 ) : Swatch( 448, 64, 64, 64 );
	static Rect ShieldSwatch( int k ) => Swatch( 320 + k * 64, 64, 64, 64 );
	static Rect BarkSwatch => Swatch( 448, 0, 64, 32 );
	static Rect LogEndSwatch => Swatch( 448, 32, 32, 32 );
	static Rect PotSwatch => Swatch( 480, 32, 32, 32 );
	static Rect SolidSwatch( int k ) => Swatch( (k % 32) * 16, 128 + (k / 32) * 16, 16, 16 );

	/// <summary>
	/// The plain paints in <see cref="SolidSwatch"/> order: dark iron, rubbed iron, rust, oak, pine, dark wood,
	/// rope, leather, brass, bone, ivory, straw, sacking, pewter, clay, wax, water, lilac cloth, wine cloth,
	/// steel, coal, ash, pale stone, dark stone, moss, bread, green glass, carrot, cream, black, soot, copper.
	/// </summary>
	static readonly uint[] Solids =
	{
		0x3E3C40, 0x6A6866, 0x7E5A42, 0x8A6A48, 0xA88A62, 0x5A4636, 0xB09A70, 0x6E4E36, 0xB8955E, 0xD8CDB4, 0xE6DDC8, 0xC8B070,
		0xA89272, 0x8C8C88, 0xA06A50, 0xEDE4CC, 0x2E3438, 0x8F86AD, 0x7A4A52, 0xB4B6B8, 0x242226, 0x7A7470, 0xA49FAE, 0x6F6A7A,
		0x7FA06A, 0xB88A58, 0x5E7A62, 0xE9A06A, 0xE8E2D2, 0x2A2A2E, 0x2E2B33, 0xA87050,
	};

	enum Paint { Iron, Rubbed, Rust, Oak, Pine, DarkWood, Rope, Leather, Brass, Bone, Ivory, StrawPaint, Sacking, Pewter, Clay, Wax, Water, Lilac, Wine, Steel, Coal, Ash, PaleStone, DarkStone, MossPaint, Bread, Glass, Carrot, Cream, Black, SootPaint, Copper }

	/// <summary>
	/// The props' paints, in the trim's infill band (the dungeon hangs no infill pictures): three banners in the
	/// floor's lilac with their emblems in pale stone, a window's dark reveal, a niche's shadowed back, two
	/// proclamations, an oak
	/// door of boards, barrel staves, crate boards, straw, sacking, two dressed stones for the voussoirs, two
	/// painted shields, two tapestries, bark, a log's end grain, glazed clay and the plain paints.
	/// </summary>
	static void PaintSwatches( Painter painter, Random random )
	{
		float Next() => (float)random.NextDouble();
		var band = TrimRows( TrimBand.Infill ).Top;
		var pale = Mix( WallStoneGrey, Feldspar, 0.6f );
		var hue = Palette.FloorHue( 6 );

		// ---- Banners: lilac cloth, a pale border, an emblem, folds and a worn foot ---------------------
		for ( var k = 0; k < 3; k++ )
		{
			var x0 = k * 48; var y0 = band;
			var cloth = Mix( hue, k == 1 ? Soot : Palette.Hex( 0x6E5A8A ), 0.12f + k * 0.06f );
			painter.Fill( x0, y0, 48, 128, cloth, roughness: 0.95f );
			for ( var x = 0; x < 48; x++ )
			{
				var fold = 0.5f + 0.5f * MathF.Sin( (x + k * 5f) / 48f * MathF.Tau * 2.5f );
				painter.Rect( x0 + x, y0, 1f, 128f, Soot, alpha: fold * 0.12f );
			}
			painter.Rect( x0 + 4f, y0 + 4f, 40f, 2.5f, pale, alpha: 0.9f );
			painter.Rect( x0 + 4f, y0 + 4f, 2.5f, 118f, pale, alpha: 0.9f );
			painter.Rect( x0 + 41.5f, y0 + 4f, 2.5f, 118f, pale, alpha: 0.9f );
			painter.Rect( x0 + 4f, y0 + 16f, 40f, 1.5f, pale, alpha: 0.7f );
			var c = new Vector2( x0 + 24f, y0 + 54f );
			switch ( k )
			{
				case 0: // a tower
					painter.Rect( c.x - 8f, c.y - 8f, 16f, 22f, pale );
					for ( var m = 0; m < 3; m++ ) painter.Rect( c.x - 8f + m * 6f, c.y - 13f, 4f, 6f, pale );
					painter.Rect( c.x - 3f, c.y + 6f, 6f, 8f, cloth, radius: 2.5f );
					painter.Rect( c.x - 1.5f, c.y - 3f, 3f, 5f, cloth, radius: 1f );
					break;
				case 1: // crossed swords under a ring
					foreach ( var s in new[] { -1f, 1f } )
					{
						painter.Line( c + new Vector2( -s * 11f, 13f ), c + new Vector2( s * 10f, -13f ), 2.6f, pale );
						painter.Line( c + new Vector2( -s * 13f, 6f ), c + new Vector2( -s * 5f, 12f ), 2f, pale );
					}
					painter.Circle( c.x, c.y - 18f, 5f, pale );
					painter.Circle( c.x, c.y - 18f, 3f, cloth );
					break;
				default: // a key, bit down
					painter.Circle( c.x, c.y - 12f, 6.5f, pale );
					painter.Circle( c.x, c.y - 12f, 3.5f, cloth );
					painter.Rect( c.x - 1.6f, c.y - 6f, 3.2f, 24f, pale );
					painter.Rect( c.x, c.y + 10f, 7f, 3f, pale );
					painter.Rect( c.x, c.y + 15f, 5f, 3f, pale );
					break;
			}
			for ( var y = 0; y < 18; y++ )
				painter.Rect( x0, y0 + 110f + y, 48f, 1f, Soot, alpha: y / 18f * 0.3f );
			for ( var s = 0; s < 6; s++ )
				painter.Circle( x0 + 6f + Next() * 36f, y0 + 20f + Next() * 100f, 1f + Next() * 2f, Soot, 0.15f );
		}

		// ---- A barred window's reveal: dark, a cold glimmer at its head, stone reveals at the sides ------
		{
			var x0 = 144; var y0 = band;
			painter.Fill( x0, y0, 48, 64, Palette.Hex( 0x1E1C24 ), roughness: 0.95f );
			for ( var y = 0; y < 30; y++ ) painter.Rect( x0 + 8f, y0 + 6f + y, 32f, 1f, Palette.Hex( 0x3A4458 ), alpha: (1f - y / 30f) * 0.6f );
			painter.Rect( x0, y0, 7f, 64f, Mix( WallStoneSlate, Soot, 0.45f ) );
			painter.Rect( x0 + 41f, y0, 7f, 64f, Mix( WallStoneSlate, Soot, 0.55f ) );
			painter.Rect( x0, y0 + 56f, 48f, 8f, Mix( WallStoneGrey, Soot, 0.35f ) );
		}

		// ---- A niche's back: shadowed stone, darker toward its top ------------------------------------------
		{
			var x0 = 144; var y0 = band + 64;
			painter.Fill( x0, y0, 48, 32, Mix( WallStoneSlate, Soot, 0.55f ), roughness: 0.95f );
			for ( var y = 0; y < 32; y++ ) painter.Rect( x0, y0 + y, 48f, 1f, Soot, alpha: (1f - y / 32f) * 0.45f );
			for ( var r = 0; r < 3; r++ ) painter.Rect( x0, y0 + 8f + r * 9f, 48f, 1f, Soot, alpha: 0.4f );
		}

		// ---- Two proclamations nailed up: parchment, a heading, lines of black-letter, a seal ------------------
		for ( var k = 0; k < 2; k++ )
		{
			var x0 = 144 + k * 24; var y0 = band + 96;
			var paper = Mix( Palette.Paper, Palette.Hex( 0xC8B48A ), 0.35f + k * 0.15f );
			painter.Fill( x0, y0, 24, 32, Mix( paper, Soot, 0.3f ), roughness: 0.9f );
			painter.Rect( x0 + 1.5f, y0 + 1.5f, 21f, 29f, paper, roughness: 0.9f );
			painter.Rect( x0 + 5f, y0 + 4f, 14f, 2.4f, Palette.Ink, alpha: 0.85f );
			for ( var l = 0; l < 6; l++ ) painter.Rect( x0 + 4f, y0 + 9f + l * 2.6f, 9f + Next() * 7f, 1f, Palette.Ink, alpha: 0.6f );
			painter.Circle( x0 + (k == 0 ? 17f : 7f), y0 + 26f, 2.6f, Palette.Hex( 0x7A4A52 ) );
			for ( var s = 0; s < 4; s++ ) painter.Circle( x0 + 2f + Next() * 20f, y0 + 2f + Next() * 28f, 1f + Next() * 2f, Palette.Hex( 0x8A6A48 ), 0.25f );
		}

		// ---- An oak door of five boards, nail heads where the straps cross ----------------------------------
		{
			var x0 = 192; var y0 = band;
			var x = 0f;
			for ( var b = 0; b < 5; b++ )
			{
				var w = b == 4 ? 64f - x : 11.5f + Next() * 3f;
				var tone = Mix( Palette.Hex( 0x8A6A48 ), Palette.Hex( 0x6A4E36 ), Next() );
				painter.Rect( x0 + x, y0, w, 128f, tone, roughness: 0.85f );
				for ( var g = 0; g < 5; g++ )
				{
					var gx = x0 + x + 1.5f + Next() * (w - 3f);
					painter.Line( new Vector2( gx, y0 ), new Vector2( gx + (Next() - 0.5f) * 3f, y0 + 128f ), 0.6f, Mix( tone, Soot, 0.4f ), 0.5f );
				}
				painter.Rect( x0 + x + w - 0.8f, y0, 1.2f, 128f, Palette.Hex( 0x2A2220 ) );
				x += w;
			}
			for ( var y = 0; y < 20; y++ ) painter.Rect( x0, y0 + 108f + y, 64f, 1f, Soot, alpha: y / 20f * 0.35f );
			foreach ( var ny in new[] { 18f, 64f, 108f } )
				for ( var nx = 6f; nx < 64f; nx += 12f ) painter.Circle( x0 + nx, y0 + ny, 1.4f, Palette.Hex( 0x4A4644 ) );
		}

		// ---- Barrel staves (upright stripes, round the barrel) and crate boards (across) -------------------
		{
			var y0 = band;
			for ( var x = 0f; x < 64f; )
			{
				var w = 6f + Next() * 3f;
				var tone = Mix( Palette.Hex( 0x9A7650 ), Palette.Hex( 0x6E5236 ), Next() );
				painter.Rect( 256f + x, y0, MathF.Min( w, 64f - x ), 32f, tone, roughness: 0.85f );
				painter.Rect( 256f + x, y0, 0.8f, 32f, Palette.Hex( 0x3A2C22 ) );
				x += w;
			}
			for ( var p = 0; p < 4; p++ )
			{
				var tone = Mix( Palette.Hex( 0xB09068 ), Palette.Hex( 0x8A6A48 ), Next() );
				painter.Fill( 256, band + 32 + p * 8, 64, 7, tone, roughness: 0.85f );
				painter.Fill( 256, band + 32 + p * 8 + 7, 64, 1, Palette.Hex( 0x3A2C22 ), roughness: 0.9f );
				foreach ( var nx in new[] { 4f, 60f } ) painter.Circle( 256f + nx, band + 32 + p * 8 + 3.5f, 0.8f, Palette.Hex( 0x3A3836 ) );
			}
		}

		// ---- Straw: strands every way on a golden ground; sacking: a coarse weave -----------------------------
		{
			var y0 = band + 64;
			painter.Fill( 256, y0, 64, 32, Palette.Hex( 0xB89E62 ), roughness: 0.95f );
			for ( var s = 0; s < 160; s++ )
			{
				var a = new Vector2( 256f + Next() * 64f, y0 + Next() * 32f );
				var angle = (Next() - 0.5f) * 1.2f;
				var tone = Next() < 0.5f ? Palette.Hex( 0xD8C080 ) : Palette.Hex( 0x8E7646 );
				painter.Line( a, a + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * (5f + Next() * 9f), 0.7f, tone, 0.8f );
			}
			var y1 = band + 96;
			painter.Fill( 256, y1, 64, 32, Palette.Hex( 0xA08A68 ), roughness: 0.95f );
			for ( var y = 0; y < 32; y += 2 ) painter.Rect( 256f, y1 + y, 64f, 0.8f, Palette.Hex( 0x7E6A4E ), alpha: 0.5f );
			for ( var x = 0; x < 64; x += 2 ) painter.Rect( 256f + x, y1, 0.8f, 32f, Palette.Hex( 0x7E6A4E ), alpha: 0.35f );
		}

		// ---- Dressed stones: a smooth drafted margin round a pitched face, a mortar line all round -----------
		for ( var k = 0; k < 2; k++ )
		{
			var x0 = 320; var y0 = band + k * 32;
			var tone = Mix( Mix( WallStoneGrey, Feldspar, 0.25f ), k == 0 ? WallStoneWarm : WallStoneSlate, 0.18f );
			painter.Fill( x0, y0, 64, 32, Mix( Mortar, Soot, 0.3f ), roughness: 0.95f );
			painter.Rect( x0 + 2f, y0 + 2f, 60f, 28f, Mix( tone, Feldspar, 0.12f ), radius: 2f, height: 0.9f, bevel: 2f, roughness: 0.85f );
			painter.Rect( x0 + 7f, y0 + 6f, 50f, 20f, tone, radius: 1f, roughness: 0.92f );
			for ( var s = 0; s < 28; s++ )
				painter.Circle( x0 + 8f + Next() * 48f, y0 + 7f + Next() * 18f, 0.8f + Next() * 1.6f, Next() < 0.5f ? Feldspar : Mix( tone, Soot, 0.35f ), 0.35f );
		}

		// ---- Two tapestries: a wine field and a deep lilac one, pale borders, an emblem, a fringe -------------
		for ( var k = 0; k < 2; k++ )
		{
			var x0 = k == 0 ? 384 : 448; var y0 = band + (k == 0 ? 0 : 64);
			var field = k == 0 ? Palette.Hex( 0x7A4A52 ) : Mix( hue, Soot, 0.3f );
			var trim = k == 0 ? Mix( hue, pale, 0.3f ) : pale;
			painter.Fill( x0, y0, 64, 64, field, roughness: 0.95f );
			for ( var x = 0; x < 64; x++ ) painter.Rect( x0 + x, y0, 1f, 64f, Soot, alpha: (0.5f + 0.5f * MathF.Sin( (x + k * 9f) / 64f * MathF.Tau * 3f )) * 0.1f );
			painter.Rect( x0 + 3f, y0 + 3f, 58f, 52f, trim, alpha: 0.95f );
			painter.Rect( x0 + 6f, y0 + 6f, 52f, 46f, field );
			painter.Rect( x0 + 8f, y0 + 8f, 48f, 42f, trim, alpha: 0.4f );
			painter.Rect( x0 + 9f, y0 + 9f, 46f, 40f, field );
			var c = new Vector2( x0 + 32f, y0 + 29f );
			if ( k == 0 )
			{
				var star = new Vector2[16];
				for ( var i = 0; i < 16; i++ )
				{
					var a = i * MathF.Tau / 16f - MathF.PI * 0.5f;
					star[i] = c + new Vector2( MathF.Cos( a ), MathF.Sin( a ) ) * (i % 2 == 0 ? 15f : 6.5f);
				}
				painter.Polygon( star, trim );
				painter.Circle( c.x, c.y, 4f, field );
			}
			else
			{
				foreach ( var s in new[] { -1f, 1f } )
				{
					painter.Line( c + new Vector2( -s * 13f, 13f ), c + new Vector2( s * 9f, -9f ), 2.4f, trim );
					painter.Circle( c.x + s * 11f, c.y - 11f, 4.6f, trim );
					painter.Circle( c.x + s * 11f, c.y - 11f, 2.2f, field );
					painter.Rect( c.x - s * 13f - 2f, c.y + 10f, 4f, 3f, trim );
				}
			}
			for ( var x = 4; x < 60; x += 3 ) painter.Rect( x0 + x, y0 + 56f, 1.4f, 7f, trim, alpha: 0.8f );
			for ( var s = 0; s < 5; s++ ) painter.Circle( x0 + 8f + Next() * 48f, y0 + 10f + Next() * 40f, 1f + Next() * 2f, Soot, 0.15f );
		}

		// ---- Two painted shields: lilac with a pale chevron; wine and cream quarters --------------------------
		for ( var k = 0; k < 2; k++ )
		{
			var x0 = 320 + k * 64; var y0 = band + 64;
			var c = new Vector2( x0 + 32f, y0 + 32f );
			painter.Fill( x0, y0, 64, 64, Palette.Hex( 0x8A6A48 ), roughness: 0.8f );
			if ( k == 0 )
			{
				painter.Circle( c.x, c.y, 30f, Mix( hue, Soot, 0.1f ), roughness: 0.8f );
				painter.Polygon( new[] { c + new Vector2( -26f, 14f ), c + new Vector2( 0f, -10f ), c + new Vector2( 26f, 14f ), c + new Vector2( 26f, 24f ), c + new Vector2( 0f, 0f ), c + new Vector2( -26f, 24f ) }, pale );
			}
			else
			{
				var wine = Palette.Hex( 0x7A4A52 ); var cream = Palette.Hex( 0xE2D8C2 );
				painter.Circle( c.x, c.y, 30f, cream, roughness: 0.8f );
				painter.Polygon( new[] { c, c + new Vector2( 0f, -31f ), c + new Vector2( -22f, -22f ), c + new Vector2( -31f, 0f ) }, wine );
				painter.Polygon( new[] { c, c + new Vector2( 0f, 31f ), c + new Vector2( 22f, 22f ), c + new Vector2( 31f, 0f ) }, wine );
			}
			for ( var s = 0; s < 10; s++ )
				painter.Circle( c.x + (Next() - 0.5f) * 50f, c.y + (Next() - 0.5f) * 50f, 1.5f + Next() * 3f, Palette.Hex( 0x6A5240 ), 0.35f );
		}

		// ---- Bark down a log, its end grain, and glazed clay ------------------------------------------------
		{
			var y0 = band;
			painter.Fill( 448, y0, 64, 32, Palette.Hex( 0x5A4636 ), roughness: 0.95f );
			for ( var s = 0; s < 18; s++ )
			{
				var x = 448f + Next() * 64f;
				painter.Line( new Vector2( x, y0 ), new Vector2( x + (Next() - 0.5f) * 4f, y0 + 32f ), 1f + Next() * 1.5f, Next() < 0.5f ? Palette.Hex( 0x3E3028 ) : Palette.Hex( 0x7A6450 ), 0.7f );
			}
			var c = new Vector2( 464f, band + 48f );
			painter.Fill( 448, band + 32, 32, 32, Palette.Hex( 0x5A4636 ), roughness: 0.9f );
			painter.Circle( c.x, c.y, 13f, Palette.Hex( 0xC4A47A ), roughness: 0.85f );
			for ( var r = 2; r < 13; r += 2 ) { painter.Circle( c.x, c.y, r + 0.5f, Palette.Hex( 0x9A7A56 ), 0.6f ); painter.Circle( c.x, c.y, r - 0.3f, Palette.Hex( 0xC4A47A ), 0.7f ); }
			painter.Line( c, c + new Vector2( 9f, 5f ), 0.8f, Palette.Hex( 0x5A4636 ), 0.8f );
			painter.Fill( 480, band + 32, 32, 32, Palette.Hex( 0x9E6A50 ), roughness: 0.4f );
			painter.Rect( 480f, band + 40f, 32f, 3f, Palette.Hex( 0xD8C8A8 ), alpha: 0.8f );
			painter.Rect( 480f, band + 52f, 32f, 2f, Palette.Hex( 0x5E3E30 ), alpha: 0.7f );
		}

		// ---- Plain paints, each with a little grime ------------------------------------------------------------
		for ( var k = 0; k < Solids.Length; k++ )
		{
			var color = Palette.Hex( Solids[k] );
			var x0 = (k % 32) * 16; var y0 = band + 128 + (k / 32) * 16;
			var roughness = k switch { 8 or 13 or 19 or 31 => 0.35f, 15 or 16 or 26 => 0.25f, _ => 0.85f };
			painter.Fill( x0, y0, 16, 16, color, roughness: roughness );
			for ( var s = 0; s < 6; s++ )
				painter.Circle( x0 + Next() * 16f, y0 + Next() * 16f, 0.4f + Next() * 0.5f, Mix( color, Soot, 0.35f ), 0.4f );
		}
	}

	/// <summary>
	/// Coursed ashlar, old and damp: courses of different heights, blocks of different lengths and
	/// now and then two small stones stacked in one slot, every block cut a little out of true (its
	/// sides wander, its corners differ), pillowed, dressed with diagonal tooling that runs its own way
	/// on each stone, chipped along its arrises. Grey granite leaning slate, lilac or warm, a few
	/// stones iron-stained. Lime mortar sits deep and crumbled in the joints. Damp rises from the floor
	/// with a tide line of salts above it and moss in the low joints; soot hangs under the cornice.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		const float SootBand = 58f;
		var joints = Mix( Mortar, Soot, 0.3f );
		var painter = new Painter( WallSize, joints, 0.95f );
		var random = new Random( Seed + 600 );
		float Next() => (float)random.NextDouble();
		var tones = new[] { WallStoneGrey, WallStoneWarm, WallStoneSlate, Mix( WallStoneGrey, GraniteLilac, 0.35f ), Mix( WallStoneWarm, Rubble, 0.4f ) };

		// The mortar bed: sunk, rough, crumbled.
		var bed = painter.Noise( 48, Seed + 601, 3 );
		for ( var i = 0; i < bed.Length; i++ ) bed[i] = 0.18f + (bed[i] - 0.5f) * 0.25f;
		painter.AddHeight( bed, 1f );

		var stones = new List<(Vector2 Min, Vector2 Size)>();
		var y = 0f;
		while ( y < WallSize - 1f )
		{
			var course = (24f + Next() * 14f) * Ppu;
			if ( WallSize - (y + course) < 22f * Ppu ) course = WallSize - y;
			var x = Next() * WallSize;
			var end = x + WallSize;
			while ( x < end - 1f )
			{
				var length = (30f + Next() * 52f) * Ppu;
				if ( end - (x + length) < 26f * Ppu ) length = end - x;

				// Now and then a slot filled by two smaller stones, one over the other.
				if ( Next() < 0.1f && length < 55f * Ppu )
				{
					var split = course * (0.4f + Next() * 0.2f);
					stones.Add( (new Vector2( x, y ), new Vector2( length, split )) );
					stones.Add( (new Vector2( x, y + split ), new Vector2( length, course - split )) );
				}
				else stones.Add( (new Vector2( x, y ), new Vector2( length, course )) );
				x += length;
			}
			y += course;
		}

		var tooling = new float[WallSize * WallSize];
		foreach ( var (min, size) in stones )
		{
			var joint = 8f + Next() * 6f;
			var outline = WallStone( min, size, joint, random.Next() );
			var tone = Mix( tones[random.Next( tones.Length )], tones[random.Next( tones.Length )], Next() );
			tone = Mix( tone, Next() < 0.5f ? Feldspar : Mix( Rubble, Soot, 0.3f ), Next() * 0.3f );
			painter.Polygon( outline, tone, height: 0.85f + Next() * 0.3f, bevel: 14f + Next() * 10f, roughness: 0.86f + Next() * 0.08f );

			// Tooling: fine parallel strokes across the face at the mason's angle for this stone.
			var angle = (Next() < 0.5f ? -1f : 1f) * (0.5f + Next() * 0.5f);
			var direction = new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) );
			var across = new Vector2( -direction.y, direction.x );
			var centre = min + size * 0.5f;
			var reach = (size.x + size.y) * 0.5f;
			for ( var s = -reach; s < reach; s += 4f + Next() * 4f )
			{
				var from = centre + across * s - direction * reach;
				var to = centre + across * s + direction * reach;
				ClipLine( tooling, from, to, min + new Vector2( joint + 6f, joint + 6f ), size - new Vector2( joint * 2f + 12f, joint * 2f + 12f ), 0.25f + Next() * 0.2f );
			}

			// Chips off the arrises: a bite or two from some edges, sunk to the joint.
			var chips = random.Next( 3 ) - 1;
			for ( var c = 0; c < chips; c++ )
			{
				var horizontal = Next() < 0.5f;
				var low = Next() < 0.5f;
				var at = horizontal
					? new Vector2( min.x + joint + Next() * (size.x - joint * 2f), low ? min.y + size.y - joint * 0.5f : min.y + joint * 0.5f )
					: new Vector2( low ? min.x + size.x - joint * 0.5f : min.x + joint * 0.5f, min.y + joint + Next() * (size.y - joint * 2f) );
				var along = horizontal ? new Vector2( 1f, 0f ) : new Vector2( 0f, 1f );
				var into = horizontal ? new Vector2( 0f, low ? -1f : 1f ) : new Vector2( low ? -1f : 1f, 0f );
				painter.Polygon( NickOutline( at, along, into, 6f + Next() * 10f, 4f + Next() * 5f, random.Next() ), Mix( tone, Soot, 0.25f ), height: 0.55f, bevel: 4f, roughness: 0.95f );
			}

			// An iron stain on the odd stone, soaked into its face.
			if ( Next() < 0.08f )
				painter.Polygon( Organic( centre + new Vector2( (Next() - 0.5f) * size.x * 0.4f, (Next() - 0.5f) * size.y * 0.3f ), size * new Vector2( 0.3f, 0.35f ), 0.9f, random.Next(), Next() * MathF.Tau ), IronStain, alpha: 0.18f );
		}

		painter.AddHeight( Scaled( tooling, -0.06f ), 1f );
		painter.Blend( Scaled( tooling, 0.25f ), WallStoneSlate );

		// Weathered faces: blotches of darker and paler stone, and pits where softer grains washed out.
		var blotch = painter.Noise( 24, Seed + 611, 3 );
		var pits = painter.Noise( 192, Seed + 612, 2 );
		var darker = new float[WallSize * WallSize];
		var paler = new float[WallSize * WallSize];
		var pitted = new float[WallSize * WallSize];
		for ( var i = 0; i < blotch.Length; i++ )
		{
			var b = (blotch[i] - 0.5f) * 2f;
			darker[i] = b < 0f ? -b * 0.22f : 0f;
			paler[i] = b > 0f ? b * 0.14f : 0f;
			pitted[i] = pits[i] < 0.3f ? (0.3f - pits[i]) * 3f : 0f;
		}
		painter.Blend( darker, Mix( Rubble, Soot, 0.4f ) );
		painter.Blend( paler, Feldspar );
		painter.Blend( Scaled( pitted, 0.4f ), Mix( Rubble, Soot, 0.5f ) );
		painter.AddHeight( Scaled( pitted, -0.12f ), 1f );

		// Rising damp: dark stone at the foot, a pale tide line of salts at its edge, moss in the low joints.
		var damp = painter.Noise( 6, Seed + 602, 3 );
		var mossNoise = painter.Noise( 24, Seed + 603, 3 );
		var soot = painter.Noise( 5, Seed + 604, 3 );
		var wet = new float[WallSize * WallSize];
		var salt = new float[WallSize * WallSize];
		var moss = new float[WallSize * WallSize];
		var smoke = new float[WallSize * WallSize];
		var height = new float[WallSize * WallSize];
		painter.CopyHeight( height );
		for ( var py = 0; py < WallSize; py++ )
		{
			var v = WallRepeat - py / Ppu;
			for ( var px = 0; px < WallSize; px++ )
			{
				var i = py * WallSize + px;
				var line = 34f + (damp[i] - 0.5f) * 40f;
				wet[i] = v < line ? MathF.Min( (line - v) / 12f, 1f ) * 0.45f : 0f;
				var edge = MathF.Abs( v - line - 2f );
				salt[i] = edge < 4f ? (1f - edge / 4f) * 0.3f * damp[i] : 0f;
				var low = v < 50f ? (50f - v) / 50f : 0f;
				moss[i] = height[i] < 0.5f && mossNoise[i] > 0.55f ? MathF.Min( (mossNoise[i] - 0.55f) * 5f, 1f ) * low * 0.6f : 0f;
				var high = v > WallRepeat - SootBand ? (v - WallRepeat + SootBand) / SootBand : 0f;
				smoke[i] = high * (0.25f + soot[i] * 0.35f);
			}
		}
		painter.Blend( wet, Damp, roughness: 0.6f );
		painter.Blend( salt, Salt, roughness: 0.95f );
		painter.Blend( moss, Mix( Moss, Granite, 0.35f ), roughness: 0.95f );
		painter.Blend( smoke, Soot );

		painter.HeightNoise( 0.35f, 16, Seed + 605, 3 );
		painter.HeightNoise( 0.12f, 96, Seed + 606, 2 );
		painter.Mottle( 0.07f, 8, Seed + 607, 4 );
		painter.RoughnessNoise( 0.05f, 12, Seed + 608, 2 );
		painter.Cavity( 0.6f, 6 );
		painter.EdgeWear( 0.2f, 4, Seed + 609 );
		painter.Grain( 0.015f, Seed + 610 );
		return painter.Bake( "arena_6_wall", Painter.Matte, 6f );
	}

	/// <summary>
	/// A dressed stone in its slot: a rounded rectangle inset by half the joint on every side, each
	/// side pushed in or out a little along its length, so no two stones are quite square.
	/// </summary>
	static Vector2[] WallStone( Vector2 min, Vector2 size, float joint, int seed )
	{
		var random = new Random( seed );
		float Next() => (float)random.NextDouble();
		float Inset() => joint * (0.4f + Next() * 0.3f);
		var left = min.x + Inset(); var right = min.x + size.x - Inset();
		var top = min.y + Inset(); var bottom = min.y + size.y - Inset();
		var corners = new[] { new Vector2( left, top ), new Vector2( right, top ), new Vector2( right, bottom ), new Vector2( left, bottom ) };
		var cut = new float[4];
		for ( var k = 0; k < 4; k++ ) cut[k] = 3f + Next() * 7f;

		var points = new List<Vector2>();
		for ( var k = 0; k < 4; k++ )
		{
			// Each side runs from just past its corner's cut to just before the next one's, bowed a
			// little and wandering a pixel or so, so the stone is never a true rectangle.
			var a = corners[k]; var b = corners[(k + 1) % 4];
			var direction = (b - a).Normal;
			var from = a + direction * cut[k];
			var to = b - direction * cut[(k + 1) % 4];
			var normal = new Vector2( direction.y, -direction.x );
			var bulge = (Next() - 0.5f) * 3f;
			var steps = Math.Max( 2, (int)((to - from).Length / 24f) );
			for ( var s = 0; s <= steps; s++ )
			{
				var t = s / (float)steps;
				var push = MathF.Sin( t * MathF.PI ) * bulge + (s == 0 || s == steps ? 0f : (Next() - 0.5f) * 1.5f);
				points.Add( from + (to - from) * t + normal * push );
			}
		}
		return points.ToArray();
	}

	/// <summary>A line clipped to a rectangle, written into a weight field (seamless), for tooling strokes.</summary>
	static void ClipLine( float[] field, Vector2 from, Vector2 to, Vector2 min, Vector2 size, float weight )
	{
		if ( size.x <= 0f || size.y <= 0f ) return;
		var length = (to - from).Length;
		var steps = (int)length;

		// Plain floats rather than vector maths in the loop: it runs a few million steps per wall.
		float dx = to.x - from.x, dy = to.y - from.y;
		float minX = min.x, minY = min.y, maxX = min.x + size.x, maxY = min.y + size.y;
		for ( var s = 0; s <= steps; s++ )
		{
			var t = s / (float)steps;
			var px = from.x + dx * t; var py = from.y + dy * t;
			if ( px < minX || py < minY || px > maxX || py > maxY ) continue;
			var x = ((int)px % WallSize + WallSize) % WallSize;
			var y = ((int)py % WallSize + WallSize) % WallSize;
			field[y * WallSize + x] = MathF.Max( field[y * WallSize + x], weight );
		}
	}

	/// <summary>A copy of a field scaled by <paramref name="amount"/>.</summary>
	static float[] Scaled( float[] field, float amount )
	{
		var copy = new float[field.Length];
		for ( var i = 0; i < field.Length; i++ ) copy[i] = field[i] * amount;
		return copy;
	}

	/// <summary>
	/// Dressed granite for the front layer: every moulding is laid in blocks, with a mortar joint across
	/// the run every 15 to 35 units, each block its own tone; the faces have a smooth drafted margin
	/// round a rougher, pitched middle, as quoins and lintels are cut. The cornice is a moulded corbel
	/// course, the skirting a plinth course, the slab edge a kerb.
	/// </summary>
	protected override PaintedSurface PaintTrim()
	{
		var body = Mix( WallStoneGrey, Feldspar, 0.25f );
		return PaintTrimAtlas( "arena_6_trim", body, Rubble, 0.85f, 0f, Seed + 700, painter =>
		{
			var random = new Random( Seed + 710 );
			float Next() => (float)random.NextDouble();
			var pitch = painter.Noise( 64, Seed + 711, 3 );
			var relief = new float[TrimSize * TrimSize];
			foreach ( var band in new[] { TrimBand.Cornice, TrimBand.Skirting, TrimBand.Pilaster, TrimBand.SlabEdge, TrimBand.Stock } )
			{
				var (top, height) = TrimRows( band );

				// Block lengths across the band's run, laid out to fill the repeat exactly.
				var x = 0f;
				while ( x < TrimSize - 1f )
				{
					var length = (15f + Next() * 20f) * 4f;
					if ( TrimSize - (x + length) < 50f ) length = TrimSize - x;
					var tone = Mix( body, Next() < 0.5f ? Feldspar : WallStoneSlate, Next() * 0.35f );
					tone = Mix( tone, Next() < 0.5f ? WallStoneWarm : GraniteLilac, Next() * 0.3f );
					painter.Rect( x + 2f, top, length - 4f, height, tone, alpha: 0.8f );

					// The drafted margin stays smooth; the middle is pitched rough.
					for ( var py = top + 6; py < top + height - 6; py++ )
					for ( var px = (int)x + 8; px < (int)(x + length) - 8; px++ )
						relief[py * TrimSize + px] = (pitch[py * TrimSize + px] - 0.5f) * 0.12f;

					painter.Rect( x - 2f, top, 4f, height, Mix( Mortar, Soot, 0.25f ), height: 0.35f, roughness: 0.95f );
					x += length;
				}
			}
			painter.AddHeight( relief, 1f );
			PaintSwatches( painter, new Random( Seed + 720 ) );
		} );
	}

	protected override PaintedSurface PaintFloor()
	{
		var painter = new Painter( FloorSize, Mortar, 0.5f );
		var random = new Random( Seed );

		PaintFlagstones( painter, random );

		// Damp patches, drawn over stone and mortar alike.
		painter.Blend( Ramp( painter.Noise( 3, Seed + 30, 2 ), 0.56f, 0.82f, 0.4f ), Damp, roughness: 0.55f );

		painter.HeightNoise( 0.5f, 8, Seed + 31, 2 );
		painter.Mottle( 0.07f, 4, Seed + 32, 3 );
		painter.RoughnessNoise( 0.08f, 6, Seed + 33, 2 );
		painter.Cavity( 0.5f, 5 );
		painter.EdgeWear( 0.2f, 4, Seed + 34 );
		painter.Grain( 0.015f, Seed + 35 );

		// The chamfers flat, dull and a shade darker. The forge's lava light shines up at the ceiling
		// under this floor from below the slab, and nothing casts shadows: a chamfer tilted further
		// still by a painted worn arris faced that light and drew every tile edge as a salmon line
		// (6b review). On the chamfer's own 45° the light's cone cannot reach it (ForgeTheme.BounceCone).
		const int Cell = FloorSize / 2;
		var rim = new float[FloorSize * FloorSize];
		for ( var y = 0; y < FloorSize; y++ )
		for ( var x = 0; x < FloorSize; x++ )
		{
			var toEdge = MathF.Min( MathF.Min( x % Cell, Cell - 1 - x % Cell ), MathF.Min( y % Cell, Cell - 1 - y % Cell ) ) + 0.5f;
			rim[y * FloorSize + x] = Step( Rim + 3f, Rim - 1f, toEdge );
		}
		painter.Level( rim, 1f );
		painter.Roughen( rim, 0.95f );
		painter.Blend( Scaled( rim, 0.2f ), Soot );
		return painter.Bake( "arena_6_floor", Painter.Matte, 7f );
	}

	/// <summary>
	/// The flagstones, per pixel. Each block is laid as two or three stones around seed points (a
	/// Voronoi cell per stone), read through a noise warp so the cut lines bend a little. The distance
	/// to the nearest split (exact, from the bisectors) or to the block's foot gives each stone a worn,
	/// rounded arris and the mortar joint its width: 4–7 px between stones, about the block joint's,
	/// varying slowly along its length. The block's foot stays clean — a straight cut, nicked only in a
	/// few places by larger, slowly varying chips — because a fine modulation there foreshortens into
	/// stitching along the chamfer. Stones sit clearly proud or sunk of each other, and each has its
	/// own colour from a small palette (one blend pass for all of them).
	/// </summary>
	void PaintFlagstones( Painter painter, Random random )
	{
		const int cell = FloorSize / 2;
		const int mask = FloorSize * FloorSize - 1;
		float Next() => (float)random.NextDouble();

		// One warp field read at two offsets serves both axes; the offset is far enough apart that the
		// two reads are unrelated.
		var warp = painter.Noise( 14, Seed + 20, 2 );
		var width = painter.Noise( 10, Seed + 22, 1 );
		var nicks = painter.Noise( 6, Seed + 25, 1 );
		var crystals = painter.Noise( FloorSize / 2, Seed + 23, 1 );
		var mossNoise = painter.Noise( 20, Seed + 24, 2 );

		// Outputs reuse the noise fields where a pixel's noise is read only at that pixel: crystals
		// become the pale-fleck weight, width the joint weight, mossNoise the moss weight.
		var relief = new float[FloorSize * FloorSize];
		var owners = new byte[FloorSize * FloorSize];
		var dark = new float[FloorSize * FloorSize];
		var pale = crystals; var joint = width; var moss = mossNoise;
		var palette = new Color[13];

		// Per block: the stone seeds, and for every ordered pair the bisector as a unit normal and an
		// offset, so the per-pixel distance to a split is one dot product.
		var seedX = new float[3]; var seedY = new float[3]; var lift = new float[3];
		var normalX = new float[9]; var normalY = new float[9]; var offset = new float[9];

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var count = c == 0 || c == 3 ? 2 : 3;
			var turn = Next() * MathF.Tau;
			var centreX = cell * 0.5f + (Next() - 0.5f) * 60f;
			var centreY = cell * 0.5f + (Next() - 0.5f) * 60f;
			for ( var k = 0; k < count; k++ )
			{
				var angle = turn + k * MathF.Tau / count + (Next() - 0.5f) * 0.5f;
				var radius = cell * 0.26f * (0.85f + Next() * 0.3f);
				seedX[k] = centreX + MathF.Cos( angle ) * radius;
				seedY[k] = centreY + MathF.Sin( angle ) * radius;
				lift[k] = (Next() - 0.5f) * 0.3f;

				var tone = Next() * 2f - 1f;
				var lean = Next() * 2f - 1f;
				var color = tone > 0f ? Mix( Granite, GraniteLight, tone * 0.45f ) : Mix( Granite, GraniteShade, -tone * 0.5f );
				palette[1 + c * 3 + k] = lean > 0f ? Mix( color, GraniteLilac, lean * 0.5f ) : Mix( color, GraniteWarm, -lean * 0.5f );
			}
			for ( var a = 0; a < count; a++ )
			for ( var b = 0; b < count; b++ )
			{
				if ( a == b ) continue;
				var nx = seedX[b] - seedX[a]; var ny = seedY[b] - seedY[a];
				var length = MathF.Sqrt( nx * nx + ny * ny );
				nx /= length; ny /= length;
				normalX[a * 3 + b] = nx; normalY[a * 3 + b] = ny;
				offset[a * 3 + b] = (seedX[a] + seedX[b]) * 0.5f * nx + (seedY[a] + seedY[b]) * 0.5f * ny;
			}
			var footInset = 6f + Next() * 2f;

			for ( var y = 0; y < cell; y++ )
			{
				var row = ((int)min.y + y) * FloorSize + (int)min.x;
				for ( var x = 0; x < cell; x++ )
				{
					var i = row + x;
					var px = x + 0.5f + (warp[i] - 0.5f) * 18f;
					var py = y + 0.5f + (warp[(i + 300 * FloorSize + 300) & mask] - 0.5f) * 18f;

					// Nearest seed, then the distance to its region's edge: the nearest bisector.
					var owner = 0; var best = float.MaxValue;
					for ( var k = 0; k < count; k++ )
					{
						var dx = px - seedX[k]; var dy = py - seedY[k];
						var dd = dx * dx + dy * dy;
						if ( dd < best ) { best = dd; owner = k; }
					}
					var split = float.MaxValue;
					for ( var k = 0; k < count; k++ )
					{
						if ( k == owner ) continue;
						var pair = owner * 3 + k;
						var e = offset[pair] - px * normalX[pair] - py * normalY[pair];
						if ( e < split ) split = e;
					}

					// Mortar between stones 4–7 px either side of the split; the block's own edge (the
					// chamfer's foot) is unwarped so neighbouring tiles still meet cleanly.
					var halfJoint = 4f + width[i] * 3f;
					var nick = nicks[i] > 0.68f ? (nicks[i] - 0.68f) * 45f : 0f;
					var mossy = mossNoise[i];
					var crystal = crystals[i];
					joint[i] = 0f; moss[i] = 0f; pale[i] = 0f;
					var toEdge = MathF.Min( MathF.Min( x + 0.5f, cell - x - 0.5f ), MathF.Min( y + 0.5f, cell - y - 0.5f ) );
					var foot = toEdge - footInset - nick;
					var edge = MathF.Min( split - halfJoint, foot );

					if ( edge <= 0f )
					{
						joint[i] = 0.9f;
						relief[i] = 0.3f;
						if ( crystal > 0.6f ) pale[i] = MathF.Min( (crystal - 0.6f) * 4f, 1f ) * 0.12f;
						var m = (mossy - 0.55f) * 4f;
						moss[i] = m > 0f ? MathF.Min( m, 1f ) * 0.4f : 0f;
						continue;
					}

					// A worn, rounded arris about twelve pixels wide, then the stone's own level.
					var t = edge >= 12f ? 1f : edge / 12f;
					var shoulder = t * t * (3f - 2f * t);
					relief[i] = 0.3f + shoulder * (0.7f + lift[owner]);
					owners[i] = (byte)(1 + c * 3 + owner);
					if ( edge < 3f )
					{
						var m = (mossy - 0.6f) * 4f;
						moss[i] = m > 0f ? MathF.Min( m, 1f ) * (1f - edge / 3f) * 0.4f : 0f;
					}

					// Crystals: sparse pale feldspar and dark mica, low contrast — granite, not asphalt. They
					// fade out over the chamfer: seen nearly face-on there, specks along every tile edge
					// would line up into a dotted seam.
					var fade = MathF.Min( MathF.Max( (toEdge - 10f) / 24f, 0f ), 1f );
					if ( crystal > 0.7f ) pale[i] = MathF.Min( (crystal - 0.7f) * 6f, 1f ) * 0.22f * fade;
					else if ( crystal < 0.29f ) dark[i] = MathF.Min( (0.29f - crystal) * 6f, 1f ) * 0.28f * fade;
				}
			}
		}

		painter.AddHeight( relief, 1f );
		painter.Blend( owners, palette );
		painter.Blend( pale, Feldspar );
		painter.Blend( dark, Mica );
		painter.Blend( joint, FloorJoint, roughness: 0.85f );
		painter.Blend( moss, Mix( Mix( Moss, Granite, 0.45f ), FloorJoint, 0.25f ), roughness: 0.9f );
	}

	/// <summary>
	/// Rubble masonry seen from below: five or six rough stones per underside around jittered points,
	/// each pillowed (rounded toward the room), pitted, its own tone, set in deep lime mortar whose
	/// width wanders; soot drifts across the lot.
	/// </summary>
	protected override PaintedSurface PaintCeiling()
	{
		const int cell = CeilingSize / 2;
		const int Stones = 6;
		var painter = new Painter( CeilingSize, Rubble, 0.9f );
		var random = new Random( Seed + 500 );
		float Next() => (float)random.NextDouble();
		var warpX = painter.Noise( 10, Seed + 510, 2 );
		var warpY = painter.Noise( 10, Seed + 511, 2 );
		var width = painter.Noise( 40, Seed + 512, 1 );
		var relief = new float[CeilingSize * CeilingSize];
		var owners = new byte[CeilingSize * CeilingSize];
		var mortar = width;
		var palette = new Color[1 + 4 * Stones];
		var seedX = new float[Stones]; var seedY = new float[Stones];
		var normalX = new float[Stones * Stones]; var normalY = new float[Stones * Stones]; var offset = new float[Stones * Stones];

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );
			var count = 5 + random.Next( 2 );
			for ( var k = 0; k < count; k++ )
			{
				// Jittered points on a loose 3 x 2 grid, so stones are neither tiny nor huge.
				seedX[k] = (k % 3 + 0.2f + Next() * 0.6f) * cell / 3f;
				seedY[k] = (k / 3 + 0.2f + Next() * 0.6f) * cell / 2f;
				var tone = Next() * 2f - 1f;
				var color = tone > 0f ? Mix( Rubble, RubbleLight, tone * 0.6f ) : Mix( Rubble, RubbleDark, -tone * 0.7f );
				palette[1 + c * Stones + k] = Mix( color, RubbleWarm, Next() * 0.6f );
			}
			for ( var a = 0; a < count; a++ )
			for ( var b = 0; b < count; b++ )
			{
				if ( a == b ) continue;
				var nx = seedX[b] - seedX[a]; var ny = seedY[b] - seedY[a];
				var length = MathF.Sqrt( nx * nx + ny * ny );
				nx /= length; ny /= length;
				normalX[a * Stones + b] = nx; normalY[a * Stones + b] = ny;
				offset[a * Stones + b] = (seedX[a] + seedX[b]) * 0.5f * nx + (seedY[a] + seedY[b]) * 0.5f * ny;
			}

			for ( var y = 0; y < cell; y++ )
			{
				var row = ((int)min.y + y) * CeilingSize + (int)min.x;
				for ( var x = 0; x < cell; x++ )
				{
					var i = row + x;
					var px = x + 0.5f + (warpX[i] - 0.5f) * 22f;
					var py = y + 0.5f + (warpY[i] - 0.5f) * 22f;
					var owner = 0; var best = float.MaxValue;
					for ( var k = 0; k < count; k++ )
					{
						var dx = px - seedX[k]; var dy = py - seedY[k];
						var dd = dx * dx + dy * dy;
						if ( dd < best ) { best = dd; owner = k; }
					}
					var split = float.MaxValue;
					for ( var k = 0; k < count; k++ )
					{
						if ( k == owner ) continue;
						var pair = owner * Stones + k;
						var e = offset[pair] - px * normalX[pair] - py * normalY[pair];
						if ( e < split ) split = e;
					}

					var foot = MathF.Min( MathF.Min( x + 0.5f, cell - x - 0.5f ), MathF.Min( y + 0.5f, cell - y - 0.5f ) ) - 3f;
					var edge = MathF.Min( split - 3f - width[i] * 6f, foot );
					mortar[i] = 0f;
					if ( edge <= 0f )
					{
						mortar[i] = 0.8f;
						relief[i] = 0.1f;
						continue;
					}

					// Pillowed: the stone bulges toward the room, steepest at its edge.
					var t = MathF.Min( edge / 26f, 1f );
					relief[i] = 0.3f + MathF.Sqrt( t ) * 0.7f;
					owners[i] = (byte)(1 + c * Stones + owner);
				}
			}
		}

		painter.AddHeight( relief, 1f );
		painter.Blend( owners, palette );
		painter.Blend( mortar, Lime, roughness: 0.95f );
		painter.Blend( Ramp( painter.Noise( 3, Seed + 520, 3 ), 0.5f, 0.85f, 0.55f ), Soot );
		painter.HeightNoise( 0.8f, 12, Seed + 521, 3 );
		painter.HeightNoise( 0.25f, 64, Seed + 522, 1 );
		painter.Mottle( 0.12f, 8, Seed + 523, 4 );
		painter.Cavity( 0.6f, 4 );
		painter.Grain( 0.015f, Seed + 524 );
		return painter.Bake( "arena_6_ceiling", Painter.Matte, 5f );
	}

	/// <summary>
	/// Builds the dungeon's props into the floor's kit, in wall space (see <see cref="WallProps"/>). What a
	/// gaoler would use — doors, benches, barrels, racks, buckets — is at a player's size; the banners and the
	/// trophies up under the arches may be half as big again. Each prop group draws from a stream of its own
	/// (<see cref="WallProps.Apart"/>).
	/// </summary>
	sealed class Props : WallProps
	{
		/// <summary>Where a torch's flame burns, where its lamp is, and how far out from the pilaster it stands.</summary>
		public const float FlameV = 178f;
		public const float FlameDepth = -9f;

		/// <summary>Where furniture's back stands: a hair in front of the skirting's face.</summary>
		const float Back = FloorContext.Recess - 6.5f;

		/// <summary>The underside of the corbel course, which an arch's spandrels run on into.</summary>
		const float Beam = FloorContext.ClearHeight - 28f;

		/// <summary>The band of wall a player's chest and head pass, and the band over the heads of the room under the arches.</summary>
		const float ChestFoot = 46f;
		const float HeadFoot = 94f;

		/// <summary>How far an arch's ring stands proud of the pilasters, and how deep it runs: back into the wall face, a vault over the bay.</summary>
		const float RingFront = -3f;
		const float RingBack = FloorContext.Recess + FloorContext.Embed;

		static Vector3 ToRoom => new( 0f, -1f, 0f );

		readonly Material _trim;
		readonly Material _glow;
		readonly Material _wall;
		readonly List<(float From, float To)> _floor = new();
		readonly List<(float From, float To)> _chest = new();
		readonly List<(float From, float To)> _head = new();
		readonly HashSet<string> _heroes = new();

		/// <summary>The arch over each bay of the wall being dressed, by the bay's index: its intrados crown, radius and centre height.</summary>
		readonly Dictionary<int, (float Crown, float Radius, float Centre)> _arches = new();

		/// <summary>Where the arches of the wall being dressed spring from their imposts.</summary>
		float _spring;

		/// <summary>Whether the bay being furnished has something a head high yet.</summary>
		bool _tall;

		int _shelfUnits;
		int _shelves;
		int _cupboards;
		int _cloaks;
		int _notices;
		int _racks;
		int _sconces;
		int _coils;
		int _sacks;

		/// <summary>Cell doors on the wall being dressed: a row of cells is a dungeon's own repeat, up to a point.</summary>
		int _doors;

		/// <summary>
		/// Shields on the wall being dressed, the armoury's and the trophy's counted in from the deal (<see cref="ReserveShields"/>):
		/// at most <see cref="MaxShields"/>, where one in every stretch of wall put a dozen on a wall (task 10 review).
		/// </summary>
		int _shields;
		const int MaxShields = 4;

		/// <summary>The next shield's design: the four come round in turn along a wall, so no two shields near each other match.</summary>
		int _design;

		public Props( FloorContext room, Material trim, Material glow, Material wall ) : base( room )
		{
			_trim = trim;
			_glow = glow;
			_wall = wall;
		}

		/// <summary>The sets the bays are dealt from; the ossuary is put in one bay of the room by hand.</summary>
		public enum Look { Cell, Armoury, Stores, Prisoner, Table, Woodpile, Pantry, Ossuary }

		/// <summary>The piece hung under each arch.</summary>
		public enum Upper { Banner, Banners, Window, Tapestry, Cage, Trophy }

		Skin T( Rect region ) => new( _trim, region );
		Skin P( Paint paint ) => T( SolidSwatch( (int)paint ) );
		Skin Any( params Paint[] paints ) => P( paints[Pick( paints.Length )] );
		Skin Stone() => T( StoneSwatch( Pick( 2 ) ) );
		Rotation Facing => Turn( Rotation.FromToRotation( Vector3.Up, ToRoom ) );
		Rotation Lying => Turn( Rotation.FromToRotation( Vector3.Up, Vector3.Forward ) );
		Rotation Yaw( float degrees ) => Turn( Rotation.FromYaw( degrees ) );

		void Claim( float u, float width ) => _floor.Add( (u - width * 0.5f, u + width * 0.5f) );

		/// <summary>Claims a stretch of the chest band, and of the band over the heads of the room as well where the piece reaches <paramref name="top"/> that high.</summary>
		void Hang( float u, float width, float top = 0f )
		{
			_chest.Add( (u - width * 0.5f, u + width * 0.5f) );
			if ( top > HeadFoot - 2f ) _head.Add( (u - width * 0.5f, u + width * 0.5f) );
		}

		/// <summary>The middles of a wall's pilasters between its bays, the pier (which carries the floor's plaque) left out.</summary>
		public static List<float> Posts( List<Bay> all )
		{
			var posts = new List<float>();
			for ( var k = 0; k + 1 < all.Count; k++ )
			{
				var post = (all[k].To + all[k + 1].From) * 0.5f;
				if ( MathF.Abs( post - FloorContext.HalfSpan ) > 50f ) posts.Add( post );
			}
			return posts;
		}

		public void StartWall()
		{
			_heroes.Clear();
			_arches.Clear();
			_shelfUnits = 0;
			_shelves = 0;
			_doors = 0;
			_shields = 0;
			_design = Side;
			_cupboards = 0;
			_cloaks = 0;
			_notices = 0;
			_racks = 0;
			_sconces = 0;
			_coils = 0;
			_sacks = 0;
		}

		/// <summary>Counts in the shields the wall's armoury and trophy will carry, before the bays are dressed in order.</summary>
		public void ReserveShields( int count ) => _shields = count;

		// ---- Arches -------------------------------------------------------------------------------

		/// <summary>
		/// An arch over every bay of the wall, springing at one height along the wall from an impost block on
		/// each pilaster: a segmental arch where the bay is wide, a full half-round (springing higher) where it
		/// is narrow, every crown its own height a hand under the corbel course. The ring is cut in voussoirs,
		/// each a stone of its own, the keystone deeper and prouder; it runs from in front of the pilasters back
		/// into the wall face, a short barrel vault over the bay. The spandrels between ring and course are laid
		/// in the wall's own ashlar, flush with the pilasters.
		/// </summary>
		public void Arches( List<Bay> all ) => Apart( () =>
		{
			_spring = Range( 146f, 151f );
			var bays = all.FindAll( b => b.Width > 40f );
			foreach ( var bay in bays ) Arch( bay );

			// The imposts: one block per pilaster under both arches' springing, and one at each corner.
			var edges = new List<(float From, float To)>();
			for ( var k = 0; k < bays.Count; k++ )
			{
				if ( k == 0 ) edges.Add( (bays[k].From - 8f, bays[k].From + 2f) );
				edges.Add( k + 1 < bays.Count && bays[k + 1].From - bays[k].To < 60f ? (bays[k].To - 2f, bays[k + 1].From + 2f) : (bays[k].To - 2f, bays[k].To + 8f) );
				if ( k + 1 < bays.Count && bays[k + 1].From - bays[k].To >= 60f ) edges.Add( (bays[k + 1].From - 8f, bays[k + 1].From + 2f) );
			}
			foreach ( var (from, to) in edges )
			{
				const float Front = -4.5f;
				Kit.BevelBox( Stone(), At( new Vector3( (from + to) * 0.5f, (Front + RingBack) * 0.5f, _spring - 2.6f ) ), new Vector3( to - from, RingBack - Front, 5.2f ), 1f, Run );
			}
		} );

		void Arch( Bay bay )
		{
			var half = bay.Width * 0.5f;
			var mid = bay.Middle;
			var crown = Range( 222f, 226.5f );
			var ring = Range( 10.5f, 12f );
			var spring = MathF.Max( _spring, crown - half );
			var rise = crown - spring;
			var radius = (half * half + rise * rise) / (2f * rise);
			var centre = crown - radius;
			var from = MathF.Asin( Math.Clamp( (spring - centre) / radius, -1f, 1f ) );
			var sweep = MathF.PI - 2f * from;
			var count = Math.Max( 5, (int)MathF.Round( sweep * (radius + ring * 0.5f) / Range( 15f, 18f ) ) ) | 1;
			_arches[bay.Index] = (crown, radius, centre);

			var outer = new Vector2[count + 1];
			for ( var i = 0; i <= count; i++ )
			{
				var a = from + sweep * i / count;
				outer[i] = new Vector2( mid + MathF.Cos( a ) * (radius + ring), centre + MathF.Sin( a ) * (radius + ring) );
			}

			for ( var i = 0; i < count; i++ )
			{
				var a0 = from + sweep * i / count;
				var a1 = from + sweep * (i + 1) / count;
				var a = (a0 + a1) * 0.5f;
				var key = i == count / 2;
				var depth = key ? ring + 2.5f : ring + Range( -0.8f, 0.6f );
				var front = key ? RingFront - 2f : RingFront + Range( -0.6f, 0.4f );
				var radial = new Vector3( MathF.Cos( a ), 0f, MathF.Sin( a ) );
				var tangent = new Vector3( -MathF.Sin( a ), 0f, MathF.Cos( a ) );
				var chord = 2f * (radius + depth * 0.5f) * MathF.Sin( (a1 - a0) * 0.5f ) - 1.1f;
				var at = new Vector3( mid, (front + RingBack) * 0.5f, centre ) + radial * (radius + depth * 0.5f);
				Kit.Box( Stone(), At( at ), new Vector3( chord, RingBack - front, depth ), Turn( Rotation.LookAt( tangent, radial ) ) );
			}

			// The spandrels: from the ring's back up into the course, a hair behind the pilasters' face.
			var offset = Next() * WallRepeat;
			Vector2 Uv( float u, float v ) => new( (u + offset) / WallRepeat, (WallRepeat - v) / WallRepeat );
			const float Plane = 0.6f;
			const float Top = Beam + 0.5f;
			for ( var i = 0; i < count; i++ )
			{
				var (l, r) = outer[i].x < outer[i + 1].x ? (outer[i], outer[i + 1]) : (outer[i + 1], outer[i]);
				Kit.Quad( _wall, At( new Vector3( l.x, Plane, l.y ) ), At( new Vector3( r.x, Plane, r.y ) ), At( new Vector3( r.x, Plane, Top ) ), At( new Vector3( l.x, Plane, Top ) ),
					Uv( l.x, l.y ), Uv( r.x, r.y ), Uv( r.x, Top ), Uv( l.x, Top ) );
			}
		}

		// ---- Torches ------------------------------------------------------------------------------

		/// <summary>
		/// A torch in an iron bracket on the pilaster at <paramref name="u"/>, over the imposts: a backplate, an arm
		/// out to a basket, a wooden stave with its pitch-soaked head glowing, and a flame of two tongues in warm
		/// amber (<see cref="FlameV"/>, <see cref="FlameDepth"/>).
		/// </summary>
		public void Torch( float u ) => Apart( () =>
		{
			var iron = P( Paint.Iron );
			Kit.BevelBox( iron, At( new Vector3( u, -0.6f, 158.5f ) ), new Vector3( 6f, 1.2f, 12f ), 0.3f, Run );
			var cup = new Vector3( u, FlameDepth, FlameV - 13f );
			Kit.Tube( iron, At( new[] { new Vector3( u, -1f, 156f ), new Vector3( u, -4.5f, 157f ), new Vector3( u, FlameDepth + 0.4f, cup.z - 3.5f ), new Vector3( u, FlameDepth, cup.z - 1f ) } ), 0.55f, 4 );
			Kit.SmoothLathe( iron, At( cup ), new[] { new Vector2( 1.3f, -1f ), new Vector2( 2.7f, 2.2f ), new Vector2( 3.1f, 4.2f ) }, 6, Run, true, false, 50f, Next() );
			var tilt = new Vector3( Range( -0.5f, 0.5f ), -0.4f, 0f );
			Kit.Tube( P( Paint.DarkWood ), At( new[] { cup + new Vector3( 0f, 0f, -6f ), cup + tilt + new Vector3( 0f, 0f, 7f ) } ), 1.05f, 5, 1.35f );
			var head = cup + tilt + new Vector3( 0f, 0f, 8.6f );
			Kit.Ellipsoid( new Skin( _glow, EmberGlow ), At( head ), new Vector3( 2.2f, 2.2f, 2.8f ), Run, 3, 6 );
			var flame = new Skin( _glow, FlameGlow );
			var lean = Range( -9f, 9f );
			Kit.SmoothLathe( flame, At( head + new Vector3( 0f, 0f, 1.2f ) ), new[]
			{
				new Vector2( 2.5f, 0f ), new Vector2( 2.8f, 2f ), new Vector2( 2.1f, 5.4f ), new Vector2( 1f, 8.6f ), new Vector2( 0f, 11.5f ),
			}, 7, Turn( Rotation.FromAxis( new Vector3( 0f, 1f, 0f ), lean ) ), false, false, 60f, Next() );
			Kit.SmoothLathe( new Skin( _glow, CoreGlow ), At( head + new Vector3( Range( -1f, 1f ), -0.6f, 3f ) ), new[]
			{
				new Vector2( 1.4f, 0f ), new Vector2( 1.2f, 3.5f ), new Vector2( 0f, 8f + Range( 0f, 2f ) ),
			}, 5, Turn( Rotation.FromAxis( new Vector3( 0f, 1f, 0f ), -lean * 1.6f ) ), false, false, 60f, Next() );
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
				case Look.Cell:
				{
					var width = Range( 36f, 42f );
					var bench = Range( 30f, 38f );
					Row( from, to, 5f, (width + 14f, u => Door( u, width )), (bench + 2f, u => Bench( u, bench )), (13f, Bucket) );
					break;
				}
				case Look.Armoury:
				{
					var width = Range( 40f, 50f );
					Row( from, to, 4f, (width + 2f, u => WeaponRack( u, width )), (26f, u => { Barrel( u ); var r = Range( 9f, 11f ); Shield( u, Range( 62f, 74f ), r ); }), (28f, u => CrateStack( u, false )) );
					break;
				}
				case Look.Stores:
					Row( from, to, 4f, (46f, BarrelStack), (28f, u => CrateStack( u, true )), (30f, Sacks) );
					break;
				case Look.Prisoner:
				{
					var straw = Range( 40f, 52f );
					Row( from, to, 4f, (straw, u => { Straw( u, straw ); Shackles( u + Range( -6f, 6f ) ); }), (13f, Bucket), (15f, Stool) );
					break;
				}
				case Look.Table:
				{
					var length = Range( 46f, 60f );
					Row( from, to, 4f, (length + 2f, u => { Table( u, length ); Shelf( u + Range( -6f, 6f ), MathF.Min( length, Range( 40f, 54f ) ), Range( 72f, 84f ) ); }), (15f, Stool), (26f, Barrel) );
					break;
				}
				case Look.Woodpile:
				{
					var width = Range( 34f, 44f );
					Row( from, to, 4f, (width, u => LogPile( u, width )), (18f, ChoppingBlock), (12f, Broom), (13f, Bucket) );
					break;
				}
				case Look.Pantry:
				{
					var width = Range( 36f, 44f );
					Row( from, to, 4f, (width + 2f, u => ShelfUnit( u, width )), (30f, Sacks), (26f, Barrel) );
					break;
				}
				default:
				{
					var width = Range( 44f, 52f );
					var u = bay.Middle + Range( -0.1f, 0.1f ) * bay.Width;
					Ossuary( u, width );
					break;
				}
			}
			Fill( bay );
		} );

		/// <summary>
		/// Fills what a bay's set left bare. On the floor, working along each gap from one end: something a head high
		/// first where the bay has nothing tall yet (a stack of barrels, a shelf of pots or crates piled high), then
		/// barrels, crates, sacks, straw, a stool or a bucket. On the wall, at a player's chest over whatever stays low
		/// and again over the heads of the room (<see cref="Band"/>).
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
						if ( roll < 0.3f && room >= 54f && _doors < 2 )
						{
							var width = Range( 34f, MathF.Min( 40f, room - 16f ) );
							Door( Put( width + 14f ), width );
						}
						else if ( roll < 0.55f && room >= 46f ) BarrelStack( Put( 46f ) );
						else if ( roll < 0.7f && room >= 40f && _shelfUnits < 2 )
						{
							var width = Range( 34f, MathF.Min( 42f, room - 4f ) );
							ShelfUnit( Put( width + 2f ), width );
						}
						else CrateStack( Put( 28f ), true );
					}
					else if ( room >= 26f && roll < 0.24f ) Barrel( Put( 26f ) );
					else if ( room >= 28f && roll < 0.42f ) CrateStack( Put( 28f ), false );
					else if ( room >= 30f && roll < 0.56f ) Sacks( Put( 30f ) );
					else if ( room >= 30f && roll < 0.68f ) { var width = Range( 28f, MathF.Min( 40f, room - 2f ) ); Straw( Put( width ), width ); }
					else if ( room >= 16f && roll < 0.82f ) Stool( Put( 15f ) );
					else Bucket( Put( 13f ) );
				}
			}

			foreach ( var (from, to) in Gaps( _chest, bay.From + 5f, bay.To - 5f ) ) Band( from, to, false );
			foreach ( var (from, to) in Gaps( _head, bay.From + 5f, bay.To - 5f ) ) Band( from, to, true );
		}

		/// <summary>
		/// Fills a bare stretch of wall from one end, a piece at a time: a painted shield (four a wall, counting the armoury's
		/// and the trophy's, and the pair beside a window), a shelf of pots and jugs (three a wall, counting the table's), a
		/// boarded cupboard, a rack of weapons laid on pegs, cloaks on a peg rail, proclamations nailed up, or once a wall
		/// a board of great keys, a pair of candle sconces (four pairs a wall), a coil of rope on its peg or sacks hung on
		/// pegs (two a wall each) — at a player's chest, or (<paramref name="high"/>) over the heads of the room.
		/// Cupboards come three a wall, half of those at the chest tall ones up past the heads of the room; proclamations
		/// four a wall.
		/// </summary>
		void Band( float from, float to, bool high )
		{
			var at = from + Range( 0f, 6f );
			var last = -1;
			var shield = false;
			while ( to - at >= 20f )
			{
				var room = to - at;
				var foot = (high ? HeadFoot + 8f : ChestFoot + 4f) + Range( 0f, 8f );
				var width = 0f;
				for ( var tries = 0; tries < 8 && width == 0f; tries++ )
				{
					var kind = Pick( 10 );
					if ( kind == last ) continue;
					switch ( kind )
					{
						case 0 when !shield && room >= 24f && _shields < MaxShields:
						{
							var r = MathF.Min( Range( 11f, 15f ), room * 0.5f - 1f );
							width = r * 2f + 2f;
							Shield( at + width * 0.5f, foot + r + 2f, r );
							_shields++;
							shield = true;
							break;
						}
						case 1 when room >= 40f && _shelves < 3: width = MathF.Min( room - 2f, Range( 40f, 70f ) ); Shelf( at + width * 0.5f, width, foot + 4f ); break;
						case 2 when room >= 34f && _cupboards < 3: width = MathF.Min( room - 2f, Range( 32f, 44f ) ); Cupboard( at + width * 0.5f, width, foot, high ? MathF.Min( width * Range( 0.85f, 1.15f ), 132f - foot ) : Next() < 0.5f ? Range( 62f, 78f ) : MathF.Min( width * Range( 0.85f, 1.15f ), 46f ) ); break;
						case 3 when room >= 48f && _racks < 3: width = MathF.Min( room - 2f, Range( 48f, 76f ) ); WallRack( at + width * 0.5f, width, foot + 8f ); break;
						case 4 when !high && room >= 36f && _cloaks < 2: width = MathF.Min( room - 2f, Range( 32f, 44f ) ); CloakRail( at + width * 0.5f, width ); break;
						case 5 when room >= 30f && _notices < 4: width = Range( 26f, 30f ); Notices( at + width * 0.5f, width, foot + Range( 6f, 14f ) ); break;
						case 6 when !high && room >= 24f && _heroes.Add( "keys" ): width = 22f; Keys( at + 11f, foot + 12f ); break;
						case 7 when room >= 30f && _sconces < 4: width = Range( 26f, 32f ); Sconces( at + width * 0.5f, width, foot + Range( 14f, 22f ) ); break;
						case 8 when room >= 28f && _coils < 2: width = 26f; RopeCoil( at + 13f, foot + Range( 30f, 36f ) ); break;
						case 9 when room >= 34f && _sacks < 2: width = MathF.Min( room - 2f, Range( 34f, 42f ) ); HungSacks( at + width * 0.5f, width, foot + Range( 34f, 40f ) ); break;
					}
					if ( width > 0f ) last = kind;
				}
				at += (width > 0f ? width : 10f) + Range( 4f, 9f );
			}
		}

		/// <summary>A cupboard hung on the wall: two doors of boards in a dark carcass, iron strap hinges, a top board. Three a wall at most.</summary>
		void Cupboard( float u, float width, float v, float height )
		{
			_cupboards++;
			Hang( u, width + 2f, v + height + 2f );
			const float D = 12f;
			Kit.BevelBox( P( Paint.DarkWood ), At( new Vector3( u, Face - D * 0.5f, v + height * 0.5f ) ), new Vector3( width, D, height ), 0.6f, Run );
			Kit.Box( P( Paint.DarkWood ), At( new Vector3( u, Face - D * 0.5f - 0.8f, v + height + 1f ) ), new Vector3( width + 3f, D + 1.6f, 2f ), Run );
			var front = Face - D - 0.1f;
			var iron = P( Paint.Iron );
			var boards = DoorSwatch;
			foreach ( var s in new[] { -1f, 1f } )
			{
				var door = u + s * width * 0.25f;
				var cut = new Rect( boards.Left + (s < 0f ? 0f : boards.Width * 0.5f), boards.Top + boards.Height * 0.3f, boards.Width * 0.5f, boards.Height * 0.4f );
				Room.WallPicture( Side, _trim, cut, door - width * 0.23f, door + width * 0.23f, v + 2f, v + height - 2f, front, false );
				foreach ( var t in new[] { 0.22f, 0.78f } )
					Kit.Box( iron, At( new Vector3( u + s * (width * 0.5f - 5f), front - 0.4f, v + height * t ) ), new Vector3( 8f, 0.8f, 1.6f ), Run );
			}
		}

		/// <summary>A pair of iron candle sconces in the wall, each an arm out to a drip dish with a burning candle. Four pairs a wall at most.</summary>
		void Sconces( float u, float width, float v )
		{
			_sconces++;
			Hang( u, width, v + 10f );
			var iron = P( Paint.Iron );
			foreach ( var s in new[] { -1f, 1f } )
			{
				var x = u + s * (width * 0.5f - 4f) + Range( -1f, 1f );
				var z = v + Range( -2f, 2f );
				Kit.Box( iron, At( new Vector3( x, Face - 0.5f, z - 1f ) ), new Vector3( 3.4f, 1f, 8f ), Run );
				Kit.Tube( iron, At( new[] { new Vector3( x, Face - 1f, z - 3f ), new Vector3( x, Face - 6f, z - 1.4f ), new Vector3( x, Face - 7f, z ) } ), 0.4f, 3 );
				Candle( new Vector3( x, Face - 7f, z ) );
			}
		}

		/// <summary>A coil of rope hung on a peg, its loose end hanging below. Two a wall at most.</summary>
		void RopeCoil( float u, float v )
		{
			_coils++;
			Hang( u, 24f, v + 2f );
			Kit.Box( P( Paint.DarkWood ), At( new Vector3( u, Face - 2.4f, v ) ), new Vector3( 1.6f, 4.8f, 1.6f ), Run );
			var rope = P( Paint.Rope );
			var r = Range( 8f, 10f );
			for ( var k = 0; k < 3; k++ )
			{
				var centre = new Vector3( u + Range( -1f, 1f ), Face - 2.6f - k * 0.9f, v - r + 0.8f - k * 0.6f );
				Kit.Tube( rope, At( Loop( centre, Vector3.Forward, Vector3.Up, r - k * 0.4f, 10 ) ), 0.9f, 4 );
			}
			Kit.Tube( rope, At( new[] { new Vector3( u + r * 0.5f, Face - 3f, v - r * 1.8f ), new Vector3( u + r * 0.6f, Face - 3.2f, v - r * 2f - Range( 8f, 14f ) ) } ), 0.9f, 4 );
		}

		/// <summary>Two or three sacks hung by their necks from pegs on a board, the stores kept off the damp floor. Two a wall at most.</summary>
		void HungSacks( float u, float width, float v )
		{
			_sacks++;
			Hang( u, width, v + 3f );
			Kit.BevelBox( P( Paint.DarkWood ), At( new Vector3( u, Face - 1f, v ) ), new Vector3( width, 2f, 5f ), 0.4f, Run );
			var count = width > 38f ? 3 : 2;
			var skin = T( SackSwatch );
			for ( var k = 0; k < count; k++ )
			{
				var x = u - width * 0.5f + (k + 0.5f) * width / count + Range( -1.5f, 1.5f );
				Kit.Box( P( Paint.DarkWood ), At( new Vector3( x, Face - 3.5f, v ) ), new Vector3( 1.4f, 5f, 1.4f ), Run );
				Kit.SmoothLathe( P( Paint.Rope ), At( new Vector3( x, Face - 5f, v - 4f ) ), new[] { new Vector2( 1.6f, 0f ), new Vector2( 1.1f, 2f ), new Vector2( 1.4f, 3.6f ) }, 5, Run, false, false );
				var h = Range( 9f, 12f );
				Kit.Ellipsoid( skin, At( new Vector3( x, Face - 6f, v - 4.5f - h ) ), new Vector3( Range( 5f, 6.4f ), 5f, h ), Yaw( Range( -30f, 30f ) ), 3, 6 );
			}
		}

		/// <summary>A peg rail with two or three cloaks hung on it, wine, lilac and brown, one with its hood up. Two a wall at most.</summary>
		void CloakRail( float u, float width )
		{
			_cloaks++;
			var rail = Range( 104f, 112f );
			Hang( u, width, rail + 4f );
			Kit.BevelBox( P( Paint.DarkWood ), At( new Vector3( u, Face - 1.2f, rail ) ), new Vector3( width, 2.4f, 4f ), 0.4f, Run );
			var count = width > 38f ? 3 : 2;
			for ( var k = 0; k < count; k++ )
			{
				var x = u - width * 0.5f + (k + 0.5f) * width / count + Range( -1.5f, 1.5f );
				Kit.Box( P( Paint.DarkWood ), At( new Vector3( x, Face - 4f, rail ) ), new Vector3( 1.4f, 5f, 1.4f ), Run );
				var cloth = Any( Paint.Wine, Paint.Lilac, Paint.Leather, Paint.Wine );
				var length = Range( 46f, 58f );
				var hem = Range( 6.5f, 7.5f );
				Kit.SmoothLathe( cloth, At( new Vector3( x, Face - hem - 1.5f, rail - 3f - length ) ), new[]
				{
					new Vector2( hem, 0f ), new Vector2( hem * 0.88f, length * 0.35f ), new Vector2( hem * 0.62f, length * 0.75f ), new Vector2( 2.6f, length ), new Vector2( 1.4f, length + 1.2f ),
				}, 6, Run, false, false, 50f, Next() );
				Kit.Ellipsoid( cloth, At( new Vector3( x, Face - 5f, rail - 2.5f ) ), new Vector3( 4f, 3.6f, 4.6f ), Run, 2, 6 );
			}
		}

		/// <summary>Proclamations nailed to the wall, two or three sheets of parchment overlapping. Three a wall at most.</summary>
		void Notices( float u, float width, float v )
		{
			_notices++;
			Hang( u, width, v + 24f );
			var at = u - width * 0.5f + 1f;
			for ( var k = 0; k < 2 + Pick( 2 ) && at < u + width * 0.5f - 8f; k++ )
			{
				var w = Range( 12f, 15f );
				var bottom = v + Range( -3f, 3f );
				Room.WallPicture( Side, _trim, NoticeSwatch( Pick( 2 ) ), at, at + w, bottom, bottom + w * 1.35f, Face - 0.3f - k * 0.15f, false );
				Kit.Ellipsoid( P( Paint.Iron ), At( new Vector3( at + w * 0.5f, Face - 0.6f - k * 0.15f, bottom + w * 1.35f - 1.4f ) ), new Vector3( 0.5f, 0.5f, 0.5f ), Run, 2, 4 );
				at += w * Range( 0.6f, 0.85f );
			}
		}

		/// <summary>Weapons laid on pegs along the wall: a spear, a sword, an axe, two or three of them one over another. Three a wall at most.</summary>
		void WallRack( float u, float length, float v )
		{
			_racks++;
			Hang( u, length + 2f, v + 16f );
			var wood = Any( Paint.Oak, Paint.DarkWood );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( wood, At( new Vector3( u + s * (length * 0.5f - 6f), Face - 2.5f, v + 6f ) ), new Vector3( 3f, 5f, 18f ), Run );
			var kinds = new List<int> { 0, 1, 2 };
			Shuffle( kinds );
			for ( var row = 0; row < 2 + Pick( 2 ); row++ )
			{
				var z = v + row * 7f;
				var y = Face - 4.5f;
				var left = u - length * 0.5f + Range( 0f, 4f );
				var right = u + length * 0.5f - Range( 0f, 4f );
				switch ( kinds[row % 3] )
				{
					case 0:
						Kit.Tube( P( Paint.DarkWood ), At( new[] { new Vector3( left, y, z ), new Vector3( right - 8f, y, z ) } ), 0.75f, 4 );
						Kit.SmoothLathe( P( Paint.Steel ), At( new Vector3( right - 8.5f, y, z ) ), new[] { new Vector2( 0.9f, 0f ), new Vector2( 1.8f, 2.6f ), new Vector2( 0f, 9f ) }, 4, Lying, false, false );
						break;
					case 1:
					{
						var hilt = left + Range( 2f, 8f );
						var blade = MathF.Min( right - hilt - 10f, Range( 30f, 38f ) );
						Kit.BevelBox( P( Paint.Steel ), At( new Vector3( hilt + 9f + blade * 0.5f, y, z ) ), new Vector3( blade, 0.6f, 2.6f ), 0.25f, Run );
						Kit.Box( P( Paint.Brass ), At( new Vector3( hilt + 8.4f, y, z ) ), new Vector3( 1.2f, 1.6f, 8f ), Run );
						Kit.Tube( P( Paint.Leather ), At( new[] { new Vector3( hilt + 1.5f, y, z ), new Vector3( hilt + 7.8f, y, z ) } ), 0.7f, 4 );
						Kit.Ellipsoid( P( Paint.Brass ), At( new Vector3( hilt + 1f, y, z ) ), new Vector3( 1.2f, 1.2f, 1.2f ), Run, 2, 5 );
						break;
					}
					default:
					{
						var head = right - Range( 4f, 10f );
						var start = MathF.Max( left, head - Range( 34f, 44f ) );
						Kit.Tube( P( Paint.Oak ), At( new[] { new Vector3( start, y, z ), new Vector3( head + 2f, y, z ) } ), 0.8f, 4 );
						Kit.BevelBox( P( Paint.Steel ), At( new Vector3( head, y, z - 3.2f ) ), new Vector3( 3.4f, 1f, 8f ), 0.3f, Run );
						break;
					}
				}
			}
		}

		// ---- Furniture and stores --------------------------------------------------------------------

		/// <summary>The bulge of a barrel's staves <paramref name="t"/> of the way up, over a radius of <paramref name="r"/>.</summary>
		static float Bulge( float r, float t ) => r - 1.4f + 1.9f * MathF.Sin( t * MathF.PI );

		/// <summary>A barrel standing on <paramref name="foot"/> along <paramref name="rotation"/>'s Up: staves bellied, two iron hoops, a lid of boards.</summary>
		void BarrelAt( Vector3 foot, float r, float h, Rotation rotation )
		{
			var profile = new[] { new Vector2( r - 1.4f, 0f ), new Vector2( Bulge( r, 0.25f ), h * 0.25f ), new Vector2( Bulge( r, 0.5f ), h * 0.5f ), new Vector2( Bulge( r, 0.75f ), h * 0.75f ), new Vector2( r - 1.4f, h ) };
			Kit.SmoothLathe( T( StaveSwatch ), At( foot ), profile, 8, rotation, false, true, 50f, Next() );
			foreach ( var t in new[] { 0.13f, 0.87f } )
			{
				var rr = Bulge( r, t ) + 0.3f;
				Kit.SmoothLathe( P( Paint.Iron ), At( foot ), new[] { new Vector2( rr, h * t - 0.9f ), new Vector2( rr, h * t + 0.9f ) }, 8, rotation, false, false );
			}
		}

		void Barrel( float u )
		{
			var r = Range( 10f, 12f );
			var h = r * Range( 2.5f, 2.75f );
			Claim( u, r * 2f + 2f );
			BarrelAt( new Vector3( u, Back - r - 1f, Ledge ), r, h, Yaw( Next() * 360f ) );
		}

		/// <summary>Two barrels on end and a third laid along their tops: a player's chest high.</summary>
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

		/// <summary>A boarded crate standing on <paramref name="v"/>, turned a little, kept inside the bay.</summary>
		void Crate( float u, float v, float size, float yaw )
		{
			var a = yaw * MathF.PI / 180f;
			var reach = size * 0.5f * (MathF.Abs( MathF.Cos( a ) ) + MathF.Abs( MathF.Sin( a ) ));
			Kit.BevelBox( T( CrateSwatch ), At( new Vector3( u, Back - reach - 0.5f, v + size * 0.5f ) ), new Vector3( size, size, size ), 0.8f, Yaw( yaw ) );
		}

		/// <summary>A crate with a smaller one on it, or (<paramref name="high"/>) three piled a head high.</summary>
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

		/// <summary>Two or three sacks of grain slumped against the wall, one laid over the others, tied at the neck.</summary>
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

		/// <summary>A wooden bucket: bound staves, an iron hoop, a rope handle.</summary>
		void Bucket( float u )
		{
			Claim( u, 13f );
			var y = Back - 7f;
			var foot = new Vector3( u, y, Ledge );
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
			Kit.Tube( P( Paint.Rope ), At( path ), 0.35f, 3 );
		}

		/// <summary>A heap of straw against the wall, in two mounds.</summary>
		void Straw( float u, float width )
		{
			Claim( u, width );
			var skin = T( StrawSwatch );
			Kit.Dome( skin, At( new Vector3( u, Back - 14f, Ledge ) ), new Vector3( width * 0.5f, 12f, Range( 6f, 9f ) ), Yaw( Range( -5f, 5f ) ), 3, 10 );
			Kit.Dome( skin, At( new Vector3( u + Range( -0.25f, 0.25f ) * width, Back - 12f, Ledge ) ), new Vector3( width * 0.3f, 10f, Range( 9f, 12f ) ), Yaw( Range( 0f, 360f ) ), 3, 8 );
		}

		/// <summary>A three-legged stool, its seat a turned disc.</summary>
		void Stool( float u )
		{
			Claim( u, 15f );
			var y = Back - 9f;
			var seat = Ledge + Range( 16f, 19f );
			var wood = Any( Paint.Oak, Paint.Pine, Paint.DarkWood );
			Kit.SmoothLathe( wood, At( new Vector3( u, y, seat ) ), new[] { new Vector2( 6.4f, 0f ), new Vector2( 6.6f, 1.2f ), new Vector2( 6.2f, 2f ) }, 8, Run, true, true );
			var turn = Next() * MathF.Tau;
			for ( var k = 0; k < 3; k++ )
			{
				var a = turn + k * MathF.Tau / 3f;
				var dir = new Vector3( MathF.Cos( a ), MathF.Sin( a ), 0f );
				Kit.Tube( wood, At( new[] { new Vector3( u, y, seat ) + dir * 3.5f, new Vector3( u, y, Ledge ) + dir * 6.5f } ), 0.8f, 4 );
			}
		}

		/// <summary>A plank bench on two slab legs, against the wall.</summary>
		void Bench( float u, float length )
		{
			Claim( u, length + 2f );
			var y = Back - 6.5f;
			var wood = Any( Paint.Oak, Paint.DarkWood );
			var top = Ledge + Range( 17f, 19f );
			Kit.BevelBox( wood, At( new Vector3( u, y, top ) ), new Vector3( length, 11f, 2.4f ), 0.5f, Run );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( wood, At( new Vector3( u + s * (length * 0.5f - 5f), y, (Ledge + top) * 0.5f - 0.6f ) ), new Vector3( 2.4f, 9.5f, top - Ledge - 1.2f ), Run );
			Kit.Box( wood, At( new Vector3( u, y + 2f, Ledge + 6f ) ), new Vector3( length - 10f, 1.6f, 2.4f ), Run );
		}

		/// <summary>A trestle table laid for the gaolers: a candle burning in a dish, a jug, a bowl, a tankard, a loaf.</summary>
		void Table( float u, float length )
		{
			Claim( u, length + 2f );
			const float D = 20f;
			var y = Back - D * 0.5f - 0.5f;
			var wood = Any( Paint.Oak, Paint.DarkWood, Paint.Pine );
			var top = Ledge + Range( 29f, 31f );
			Kit.BevelBox( wood, At( new Vector3( u, y, top - 1.3f ) ), new Vector3( length, D, 2.6f ), 0.6f, Run );
			foreach ( var s in new[] { -1f, 1f } )
			{
				var x = u + s * (length * 0.5f - 7f);
				Kit.Box( wood, At( new Vector3( x, y, (Ledge + top) * 0.5f - 1.3f ) ), new Vector3( 3f, D - 6f, top - Ledge - 2.6f ), Run );
				Kit.Box( wood, At( new Vector3( x, y, Ledge + 1.2f ) ), new Vector3( 4f, D - 2f, 2.4f ), Run );
			}
			Kit.Box( wood, At( new Vector3( u, y, Ledge + 9f ) ), new Vector3( length - 14f, 2.4f, 3f ), Run );

			var items = new List<Action<Vector3>> { Candle, Jug, Bowl, Tankard, Loaf };
			Shuffle( items );
			var at = u - length * 0.5f + Range( 5f, 8f );
			foreach ( var item in items )
			{
				if ( at > u + length * 0.5f - 5f ) break;
				item( new Vector3( at, y + Range( -3f, 3f ), top ) );
				at += Range( 9f, 14f );
			}
		}

		/// <summary>A tallow candle burning in a pewter dish.</summary>
		void Candle( Vector3 foot )
		{
			Kit.SmoothLathe( P( Paint.Pewter ), At( foot ), new[] { new Vector2( 2.6f, 0f ), new Vector2( 2.8f, 0.6f ) }, 7, Run, false, true );
			var h = Range( 3.5f, 7f );
			Kit.SmoothLathe( P( Paint.Wax ), At( foot + new Vector3( 0f, 0f, 0.6f ) ), new[] { new Vector2( 1f, 0f ), new Vector2( 1f, h ), new Vector2( 0.7f, h + 0.3f ) }, 6, Run, false, true );
			Kit.Ellipsoid( new Skin( _glow, FlameGlow ), At( foot + new Vector3( 0f, 0f, h + 1.8f ) ), new Vector3( 0.55f, 0.55f, 1.2f ), Run, 2, 5 );
		}

		void Jug( Vector3 foot )
		{
			Kit.SmoothLathe( T( PotSwatch ), At( foot ), new[]
			{
				new Vector2( 2f, 0f ), new Vector2( 3f, 1.6f ), new Vector2( 3.2f, 4.8f ), new Vector2( 2.1f, 7.8f ), new Vector2( 1.6f, 9.2f ), new Vector2( 2.1f, 10.4f ),
			}, 7, Run, true, false, 50f, Next() );
			var a = Next() * MathF.Tau;
			var dir = new Vector3( MathF.Cos( a ), MathF.Sin( a ) * 0.5f, 0f );
			Kit.Tube( T( PotSwatch ), At( new[] { foot + dir * 2.4f + new Vector3( 0f, 0f, 8.6f ), foot + dir * 4.4f + new Vector3( 0f, 0f, 7.6f ), foot + dir * 3.6f + new Vector3( 0f, 0f, 3.6f ) } ), 0.45f, 4 );
		}

		void Bowl( Vector3 foot ) =>
			Kit.SmoothLathe( Any( Paint.Oak, Paint.Clay, Paint.Pewter ), At( foot ), new[] { new Vector2( 2.2f, 0f ), new Vector2( 3.8f, 1.2f ), new Vector2( 4.4f, 2.4f ), new Vector2( 3.9f, 2.4f ), new Vector2( 3.2f, 1.4f ) }, 8, Run, true, false, 45f );

		void Tankard( Vector3 foot )
		{
			var skin = Any( Paint.Pewter, Paint.Oak );
			Kit.SmoothLathe( skin, At( foot ), new[] { new Vector2( 2.2f, 0f ), new Vector2( 2f, 5.6f ), new Vector2( 2.2f, 6f ) }, 7, Run, true, true );
			Kit.Tube( skin, At( new[] { foot + new Vector3( 2f, 0f, 5f ), foot + new Vector3( 3.8f, 0f, 4.4f ), foot + new Vector3( 3.6f, 0f, 1.6f ), foot + new Vector3( 2f, 0f, 1.2f ) } ), 0.4f, 3 );
		}

		void Loaf( Vector3 foot ) =>
			Kit.Ellipsoid( P( Paint.Bread ), At( foot + new Vector3( 0f, 0f, 1.6f ) ), new Vector3( 4.2f, 2.8f, 2.2f ), Yaw( Next() * 180f ), 3, 7 );

		/// <summary>A clay pot, glazed band round its shoulder, <paramref name="scale"/> of the usual size.</summary>
		void Pot( Vector3 foot, float scale )
		{
			var s = scale;
			Kit.SmoothLathe( Next() < 0.6f ? T( PotSwatch ) : Any( Paint.Clay, Paint.Ash, Paint.Glass ), At( foot ), new[]
			{
				new Vector2( 1.8f * s, 0f ), new Vector2( 3.3f * s, 1.6f * s ), new Vector2( 3.6f * s, 4f * s ), new Vector2( 2.5f * s, 6.4f * s ), new Vector2( 2.1f * s, 7f * s ), new Vector2( 2.5f * s, 7.6f * s ),
			}, 6, Run, true, false, 50f, Next() );
		}

		/// <summary>A plank shelf on two iron brackets at <paramref name="v"/>, set with pots, jugs, a candle stub and the odd skull.</summary>
		void Shelf( float u, float width, float v )
		{
			_shelves++;
			Hang( u, width + 2f, v + 12f );
			const float D = 9f;
			var y = Face - D * 0.5f;
			Kit.BevelBox( Any( Paint.Oak, Paint.DarkWood ), At( new Vector3( u, y, v - 0.8f ) ), new Vector3( width, D, 1.6f ), 0.4f, Run );
			foreach ( var s in new[] { -1f, 1f } )
			{
				var x = u + s * (width * 0.5f - 5f);
				Kit.Box( P( Paint.Iron ), At( new Vector3( x, Face - 0.6f, v - 4f ) ), new Vector3( 1.4f, 1.2f, 6f ), Run );
				Kit.Box( P( Paint.Iron ), At( new Vector3( x, Face - D * 0.45f, v - 2.2f ) ), new Vector3( 1.2f, D * 0.9f, 1f ), Turn( Rotation.FromAxis( Vector3.Forward, -28f ) ) );
			}
			var at = u - width * 0.5f + Range( 3f, 6f );
			while ( at < u + width * 0.5f - 5f )
			{
				var foot = new Vector3( at, y + Range( -1f, 1f ), v );
				var roll = Next();
				if ( roll < 0.5f ) { var scale = Range( 1.05f, 1.5f ); Pot( foot, scale ); at += 8f * scale + Range( 2f, 4f ); }
				else if ( roll < 0.68f ) { Jug( foot ); at += Range( 9f, 12f ); }
				else if ( roll < 0.78f ) { Kit.SmoothLathe( P( Paint.Wax ), At( foot ), new[] { new Vector2( 1f, 0f ), new Vector2( 1f, Range( 2f, 5f ) ) }, 6, Run, false, true ); at += 5f; }
				else if ( roll < 0.86f && _heroes.Add( "shelfskull" ) ) { Skull( foot, 0.9f, Range( -25f, 25f ) ); at += 10f; }
				else at += Range( 4f, 8f );
			}
		}

		/// <summary>A tall set of shelves a head high, its four shelves set with pots, jugs, sacks and candles.</summary>
		void ShelfUnit( float u, float width )
		{
			Claim( u, width + 2f );
			_shelfUnits++;
			const float D = 14f;
			var h = Range( 84f, 92f );
			Hang( u, width + 2f, h );
			_tall = true;
			var y = Back - D * 0.5f - 0.5f;
			var wood = Any( Paint.Oak, Paint.DarkWood, Paint.Pine );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( wood, At( new Vector3( u + s * (width * 0.5f - 0.8f), y, Ledge + h * 0.5f ) ), new Vector3( 1.6f, D, h ), Run );
			for ( var k = 0; k < 4; k++ )
			{
				var v = Ledge + 4f + k * (h - 6f) / 3.4f;
				Kit.Box( wood, At( new Vector3( u, y, v - 0.7f ) ), new Vector3( width - 3f, D, 1.4f ), Run );
				var at = u - width * 0.5f + Range( 4f, 6f );
				while ( at < u + width * 0.5f - 5f )
				{
					var foot = new Vector3( at, y + Range( -1.5f, 1.5f ), v );
					var roll = Next();
					if ( k == 0 && roll < 0.4f ) { Kit.Ellipsoid( T( SackSwatch ), At( foot + new Vector3( 2f, 0f, 5.5f ) ), new Vector3( 5.5f, 5f, 6f ), Yaw( Next() * 360f ), 3, 6 ); at += 13f; }
					else if ( roll < 0.5f ) { var scale = Range( 1.1f, 1.45f ); Pot( foot, scale ); at += 8f * scale + Range( 3f, 6f ); }
					else if ( roll < 0.68f ) { Jug( foot ); at += Range( 10f, 13f ); }
					else if ( roll < 0.78f ) { Bowl( foot ); at += 11f; }
					else at += Range( 6f, 10f );
				}
			}
		}

		/// <summary>
		/// An armoury rack <paramref name="width"/> wide: a base with sockets and a notched rail on two uprights, spears
		/// and a halberd stood in it leaning on the rail — taller than a man.
		/// </summary>
		void WeaponRack( float u, float width )
		{
			Claim( u, width + 2f );
			Hang( u, width + 2f, 100f );
			_tall = true;
			var y = Back - 5.5f;
			var wood = Any( Paint.Oak, Paint.DarkWood );
			Kit.BevelBox( wood, At( new Vector3( u, y, Ledge + 1.6f ) ), new Vector3( width, 10f, 3.2f ), 0.4f, Run );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( wood, At( new Vector3( u + s * (width * 0.5f - 1.4f), Back - 2f, Ledge + 36f ) ), new Vector3( 2.4f, 2.4f, 72f ), Run );
			Kit.Box( wood, At( new Vector3( u, Back - 2f, Ledge + 60f ) ), new Vector3( width, 2.6f, 3f ), Run );
			var count = 3 + Pick( 2 );
			var halberd = Pick( count );
			for ( var k = 0; k < count; k++ )
			{
				var x = u - width * 0.5f + 6f + (width - 12f) * k / Math.Max( count - 1, 1 ) + Range( -1.5f, 1.5f );
				var foot = new Vector3( x, y - 1.5f, Ledge + 3f );
				var top = new Vector3( x + Range( -3f, 3f ), Back - 4.5f, Ledge + Range( 92f, 102f ) );
				Kit.Tube( P( Paint.DarkWood ), At( new[] { foot, top } ), 0.75f, 4 );
				var dir = (top - foot).Normal;
				Kit.SmoothLathe( P( Paint.Steel ), At( top ), new[] { new Vector2( 0.9f, 0f ), new Vector2( 1.7f, 2.5f ), new Vector2( 0f, 9f ) }, 4, Turn( Rotation.FromToRotation( Vector3.Up, dir ) ), false, false );
				if ( k != halberd ) continue;
				var blade = top - dir * 6f;
				Kit.BevelBox( P( Paint.Steel ), At( blade + new Vector3( 3.6f, 0f, 0f ) ), new Vector3( 6f, 0.8f, 8.5f ), 0.3f, Run );
				Kit.Box( P( Paint.Steel ), At( blade + new Vector3( -2.6f, 0f, 0f ) ), new Vector3( 4f, 0.8f, 1.6f ), Run );
			}
		}

		/// <summary>
		/// A cell door set in a stone frame: oak boards, three iron straps, a ring pull, a little barred grille at
		/// eye height. The frame stands out of the wall face and the leaf sits back in it, so it reads as an opening.
		/// </summary>
		void Door( float u, float width )
		{
			var h = Range( 80f, 86f );
			_doors++;
			Claim( u, width + 14f );
			Hang( u, width + 14f, h + 10f );
			_tall = true;
			const float FrameFront = 30.5f;
			var stone = Stone();
			var frameY = (FrameFront + FloorContext.Recess + FloorContext.Embed) * 0.5f;
			var frameD = FloorContext.Recess + FloorContext.Embed - FrameFront;
			foreach ( var s in new[] { -1f, 1f } )
				Kit.BevelBox( stone, At( new Vector3( u + s * (width * 0.5f + 3.5f), frameY, Ledge + (h + 2f) * 0.5f ) ), new Vector3( 7f, frameD, h + 2f ), 1f, Run );
			Kit.BevelBox( stone, At( new Vector3( u, frameY, Ledge + h + 5.5f ) ), new Vector3( width + 18f, frameD, 9f ), 1f, Run );

			const float Leaf = 37.5f;
			Room.WallPicture( Side, _trim, DoorSwatch, u - width * 0.5f, u + width * 0.5f, Ledge, Ledge + h + 1f, Leaf, false );
			var iron = P( Paint.Iron );
			foreach ( var t in new[] { 0.16f, 0.5f, 0.84f } )
				Kit.Box( iron, At( new Vector3( u - 1.5f, Leaf - 0.5f, Ledge + h * t ) ), new Vector3( width - 3f, 1f, 3.2f ), Run );
			var pull = new Vector3( u + width * 0.3f, Leaf - 1.4f, Ledge + h * 0.42f );
			Kit.Tube( iron, At( Loop( pull + new Vector3( 0f, 0f, -2.6f ), Vector3.Forward, Vector3.Up, 2.4f, 6 ) ), 0.4f, 3 );
			Kit.Ellipsoid( iron, At( pull + new Vector3( 0f, 0.6f, 0f ) ), new Vector3( 1.2f, 0.8f, 1.2f ), Run, 2, 5 );
			var grille = Ledge + h * 0.66f;
			Room.WallPicture( Side, _trim, WindowSwatch, u - 6f, u + 6f, grille, grille + 10f, Leaf - 0.4f, false );
			for ( var b = -1; b <= 1; b++ )
				Kit.Tube( iron, At( new[] { new Vector3( u + b * 3.2f, Leaf - 0.9f, grille ), new Vector3( u + b * 3.2f, Leaf - 0.9f, grille + 10f ) } ), 0.4f, 4 );
		}

		/// <summary>Iron rings on plates in the wall, chains hanging from them to open manacles, over a prisoner's straw.</summary>
		void Shackles( float u )
		{
			var top = Range( 104f, 114f );
			Hang( u, 34f, top );
			var iron = P( Paint.Iron );
			var gap = Range( 9f, 13f );
			foreach ( var s in new[] { -1f, 1f } )
			{
				var x = u + s * gap;
				var ring = top + Range( -3f, 3f );
				WallRing( x, ring );
				var low = ring - Range( 28f, 40f );
				var hand = new Vector3( x + s * Range( 0f, 3f ), Face - 3f, low );
				Chain( new[] { new Vector3( x, Face - 2.4f, ring - 2.6f ), hand + new Vector3( 0f, 0f, 2.6f ) }, 3.6f );
				Kit.Tube( iron, At( Loop( hand, Vector3.Forward, new Vector3( 0f, -0.5f, 0.86f ), 2.6f, 7 ) ), 0.6f, 3 );
			}
		}

		/// <summary>An iron ring on a plate in the wall at (<paramref name="u"/>, <paramref name="v"/>), hanging from its staple.</summary>
		void WallRing( float u, float v )
		{
			var iron = P( Paint.Iron );
			Kit.Box( iron, At( new Vector3( u, Face - 0.5f, v ) ), new Vector3( 4f, 1.4f, 4f ), Run );
			Kit.Tube( iron, At( Loop( new Vector3( u, Face - 2.2f, v - 2.4f ), Vector3.Forward, new Vector3( 0f, -0.3f, 0.95f ), 2.2f, 6 ) ), 0.45f, 3 );
		}

		/// <summary>
		/// A chain along <paramref name="path"/>: oval links of about <paramref name="link"/> each, every other one turned
		/// a quarter round, so it reads as a chain from any side — four straight runs of a three-sided rod per link.
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
				var side = MathF.Abs( dir.y ) < 0.9f ? Vector3.Cross( dir, new Vector3( 0f, 1f, 0f ) ).Normal : Vector3.Forward;
				if ( k % 2 == 1 ) side = Vector3.Cross( dir, side ).Normal;
				var a = dir * (link * 0.5f); var b = side * (link * 0.28f);
				Kit.Tube( iron, At( new[] { centre - a - b, centre + a - b, centre + a + b, centre - a + b, centre - a - b } ), 0.32f, 3 );
			}
		}

		/// <summary>A stack of split logs, end grain to the room, and a chopping block with its axe beside it.</summary>
		void LogPile( float u, float width )
		{
			Claim( u, width );
			var rows = new List<int>();
			var r0 = Range( 3.6f, 4.2f );
			var across = Math.Max( 3, (int)(width / (r0 * 2.1f)) );
			for ( var n = across; n >= across - 2 && n > 0; n-- ) rows.Add( n );
			var top = 0f;
			for ( var row = 0; row < rows.Count; row++ )
			{
				var n = rows[row];
				for ( var k = 0; k < n; k++ )
				{
					var r = r0 * Range( 0.85f, 1.12f );
					var x = u + (k - (n - 1) * 0.5f) * r0 * 2.05f + Range( -0.6f, 0.6f );
					var v = Ledge + r0 + row * r0 * 1.75f + Range( -0.3f, 0.3f );
					var length = Range( 22f, 27f );
					var back = Back - Range( 0f, 2.5f );
					Kit.SmoothLathe( T( BarkSwatch ), At( new Vector3( x, back, v ) ), new[] { new Vector2( r, 0f ), new Vector2( r, length ) }, 6, Facing, false, false, 45f, Next() );
					Kit.SmoothLathe( T( LogEndSwatch ), At( new Vector3( x, back, v ) ), new[] { new Vector2( r - 0.15f, length ), new Vector2( r - 0.15f, length + 0.1f ) }, 6, Facing, false, true );
					top = MathF.Max( top, v + r );
				}
			}
			if ( top > 40f ) Hang( u, width, top );
		}

		void ChoppingBlock( float u )
		{
			Claim( u, 18f );
			var r = Range( 7f, 8f );
			var h = Range( 12f, 15f );
			var foot = new Vector3( u, Back - r - 2f, Ledge );
			Kit.SmoothLathe( T( BarkSwatch ), At( foot ), new[] { new Vector2( r + 0.6f, 0f ), new Vector2( r, h ) }, 8, Run, false, false, 45f, Next() );
			Kit.SmoothLathe( T( LogEndSwatch ), At( foot ), new[] { new Vector2( r - 0.1f, h ), new Vector2( r - 0.1f, h + 0.1f ) }, 8, Run, false, true );
			var bite = foot + new Vector3( Range( -2f, 2f ), 0f, h - 0.6f );
			var a = Next() < 0.5f ? 1f : -1f;
			var end = bite + new Vector3( a * 11f, -3f, 14f );
			Kit.Tube( P( Paint.Oak ), At( new[] { bite, end } ), 0.7f, 4 );
			Kit.BevelBox( P( Paint.Steel ), At( bite + new Vector3( -a * 1.4f, 0f, 1f ) ), new Vector3( 5f, 1.2f, 4f ), 0.3f, Yaw( 0f ) * Rotation.FromAxis( Vector3.Left, a * 35f ) );
		}

		/// <summary>A besom broom leant against the wall.</summary>
		void Broom( float u )
		{
			Claim( u, 12f );
			var foot = new Vector3( u, Back - 5f, Ledge );
			var top = new Vector3( u + Range( -9f, 9f ), Face - 1.5f, Ledge + Range( 56f, 62f ) );
			var dir = (top - foot).Normal;
			Kit.Tube( P( Paint.Pine ), At( new[] { foot + dir * 10f, top } ), 0.65f, 4 );
			Kit.SmoothLathe( P( Paint.StrawPaint ), At( foot ), new[] { new Vector2( 3.4f, 0f ), new Vector2( 2.6f, 6f ), new Vector2( 1f, 12f ) }, 6, Turn( Rotation.FromToRotation( Vector3.Up, dir ) ), true, false );
		}

		/// <summary>A round shield on the wall: painted boards, an iron rim and a boss.</summary>
		void Shield( float u, float v, float r )
		{
			Hang( u, r * 2f + 2f, v + r );
			ShieldAt( new Vector3( u, Face, v ), r );
		}

		/// <summary>The gaol's great keys on a board, once a wall.</summary>
		void Keys( float u, float v )
		{
			Hang( u, 22f, v + 8f );
			Kit.BevelBox( P( Paint.DarkWood ), At( new Vector3( u, Face - 0.6f, v + 2f ) ), new Vector3( 20f, 1.4f, 12f ), 0.3f, Run );
			for ( var k = 0; k < 3; k++ )
			{
				var x = u - 6f + k * 6f;
				var length = Range( 7f, 10f );
				Kit.Box( P( Paint.Iron ), At( new Vector3( x, Face - 2.6f, v + 6f ) ), new Vector3( 0.8f, 2.6f, 0.8f ), Run );
				Kit.Tube( P( Paint.Rubbed ), At( Loop( new Vector3( x, Face - 2.2f, v + 4f ), Vector3.Forward, Vector3.Up, 1.5f, 6 ) ), 0.35f, 3 );
				Kit.Box( P( Paint.Rubbed ), At( new Vector3( x, Face - 2.2f, v + 2.5f - length * 0.5f ) ), new Vector3( 0.7f, 0.7f, length ), Run );
				Kit.Box( P( Paint.Rubbed ), At( new Vector3( x + 1f, Face - 2.2f, v + 3f - length ) ), new Vector3( 2f, 0.7f, 1.6f ), Run );
			}
		}

		/// <summary>A skull of about life size resting at <paramref name="foot"/>, turned <paramref name="yaw"/> degrees from facing the room.</summary>
		void Skull( Vector3 foot, float scale, float yaw )
		{
			var s = scale;
			var turn = Rotation.FromYaw( yaw );
			var rotation = Turn( turn );
			Kit.Ellipsoid( P( Paint.Bone ), At( foot + turn * new Vector3( 0f, 0.4f * s, 3.6f * s ) ), new Vector3( 3f, 3.5f, 3.2f ) * s, rotation, 4, 7 );
			Kit.Box( P( Paint.Bone ), At( foot + turn * new Vector3( 0f, -2.4f * s, 1.4f * s ) ), new Vector3( 4f, 2.4f, 2.8f ) * s, rotation );
			foreach ( var e in new[] { -1f, 1f } )
				Kit.Ellipsoid( P( Paint.SootPaint ), At( foot + turn * new Vector3( e * 1.2f * s, -2.85f * s, 3.5f * s ) ), new Vector3( 0.9f, 0.5f, 0.75f ) * s, rotation, 2, 5 );
		}

		/// <summary>A long bone lying from <paramref name="a"/> to <paramref name="b"/>, knobbed at both ends.</summary>
		void Bone( Vector3 a, Vector3 b )
		{
			Kit.Tube( P( Paint.Ivory ), At( new[] { a, b } ), 0.55f, 4 );
			foreach ( var end in new[] { a, b } )
				Kit.Ellipsoid( P( Paint.Ivory ), At( end ), new Vector3( 1f, 0.9f, 0.9f ), Run, 2, 5 );
		}

		/// <summary>
		/// The ossuary, once in the room: a niche at a player's chest framed in dressed stone, its back in shadow, skulls
		/// in two rows on its sill and a shelf with long bones between them; on the floor under it candle stubs burning
		/// and a skull fallen from the rest.
		/// </summary>
		void Ossuary( float u, float width )
		{
			var sill = Range( 50f, 56f );
			var h = Range( 46f, 52f );
			Hang( u, width + 14f, sill + h + 10f );
			var stone = Stone();
			const float FrameFront = 28.5f;
			var frameY = (FrameFront + RingBack) * 0.5f;
			var frameD = RingBack - FrameFront;
			Kit.BevelBox( stone, At( new Vector3( u, frameY, sill - 3f ) ), new Vector3( width + 12f, frameD, 6f ), 1f, Run );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.BevelBox( stone, At( new Vector3( u + s * (width * 0.5f + 3f), frameY, sill + h * 0.5f ) ), new Vector3( 6f, frameD, h ), 1f, Run );
			Kit.BevelBox( stone, At( new Vector3( u, frameY, sill + h + 4f ) ), new Vector3( width + 14f, frameD, 8f ), 1f, Run );
			Room.WallPicture( Side, _trim, NicheSwatch, u - width * 0.5f, u + width * 0.5f, sill, sill + h, Face, false );
			var shelf = sill + h * 0.5f;
			Kit.Box( stone, At( new Vector3( u, (FrameFront + 2f + RingBack) * 0.5f, shelf - 1f ) ), new Vector3( width, RingBack - FrameFront - 2f, 2f ), Run );
			foreach ( var row in new[] { sill, shelf } )
			{
				var count = (int)(width / 11f);
				for ( var k = 0; k < count; k++ )
				{
					var x = u - width * 0.5f + (k + 0.5f) * width / count + Range( -1f, 1f );
					Skull( new Vector3( x, FrameFront + 6f + Range( -0.5f, 1f ), row ), Range( 0.85f, 1f ), Range( -20f, 20f ) );
				}
				Bone( new Vector3( u - width * 0.4f, FrameFront + 11.5f, row + 0.6f ), new Vector3( u + width * 0.4f, FrameFront + 12f, row + 0.8f ) );
			}

			Claim( u, width );
			for ( var k = 0; k < 3; k++ )
				Candle( new Vector3( u + Range( -0.4f, 0.4f ) * width, Back - Range( 4f, 10f ), Ledge ) );
			Skull( new Vector3( u + Range( -0.3f, 0.3f ) * width, Back - 9f, Ledge ), 1f, Range( -40f, 40f ) );
			Bone( new Vector3( u - 8f, Back - 14f, Ledge + 0.6f ), new Vector3( u + 6f, Back - 6f, Ledge + 0.6f ) );
		}

		// ---- Under the arches -----------------------------------------------------------------------

		/// <summary>Dresses the arch over <paramref name="bay"/> with <paramref name="upper"/>, kept under its intrados.</summary>
		public void Above( Upper upper, Bay bay ) => Apart( () =>
		{
			if ( !_arches.TryGetValue( bay.Index, out var arch ) ) return;
			float Under( float x ) => arch.Centre + MathF.Sqrt( MathF.Max( arch.Radius * arch.Radius - x * x, 0f ) );
			var u = bay.Middle + Range( -0.06f, 0.06f ) * bay.Width;
			switch ( upper )
			{
				case Upper.Banner: Banner( u, Under( 24f ) - Range( 9f, 13f ), Range( 30f, 38f ), Range( 86f, 104f ) ); break;
				case Upper.Banners:
				{
					var width = Range( 22f, 26f );
					var gap = width * 0.5f + Range( 7f, 12f );
					foreach ( var s in new[] { -1f, 1f } ) Banner( u + s * gap, Under( gap + width * 0.5f + 6f ) - Range( 9f, 12f ), width, Range( 70f, 90f ) );
					break;
				}
				case Upper.Window:
				{
					// Flanked, where the bay is wide enough, by a pair of shields while the wall has two to spare (they come up
					// in two designs), else by a pair of wall rings.
					var width = Window( u, Under( 20f ) );
					var r = Range( 9f, 11f );
					var v = Range( 176f, 184f );
					if ( bay.Width <= width + 4f * r + 30f ) break;
					var shields = _shields + 2 <= MaxShields;
					if ( shields ) _shields += 2;
					foreach ( var s in new[] { -1f, 1f } )
					{
						var x = u + s * (width * 0.5f + r + Range( 12f, 16f ));
						if ( shields ) Shield( x, v, r );
						else WallRing( x, v + Range( -2f, 2f ) );
					}
					break;
				}
				case Upper.Tapestry: Tapestry( u, Under ); break;
				case Upper.Cage: Cage( u, arch.Crown ); Swag( bay ); break;
				default: Trophy( u, Under( 28f ) ); break;
			}
		} );

		/// <summary>
		/// A banner hung from a pole on two brackets: lilac cloth with a pale border and one of three emblems, falling
		/// in gentle folds to a swallowtail.
		/// </summary>
		void Banner( float u, float pole, float width, float length )
		{
			const float Y = 34.5f;
			var iron = P( Paint.Iron );
			Kit.Tube( iron, At( new[] { new Vector3( u - width * 0.5f - 5f, Y, pole ), new Vector3( u + width * 0.5f + 5f, Y, pole ) } ), 0.8f, 5 );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.Ellipsoid( P( Paint.Brass ), At( new Vector3( u + s * (width * 0.5f + 5.8f), Y, pole ) ), new Vector3( 1.4f, 1.4f, 1.4f ), Run, 2, 6 );
				Kit.Tube( iron, At( new[] { new Vector3( u + s * (width * 0.5f + 2f), FloorContext.Recess + FloorContext.Embed, pole ), new Vector3( u + s * (width * 0.5f + 2f), Y, pole ) } ), 0.5f, 3 );
			}

			var region = BannerSwatch( Pick( 3 ) );
			const int Rows = 4;
			var phase = Next() * MathF.Tau;
			Vector3 Point( int column, int row )
			{
				var across = column / 2f;
				var down = row / (float)Rows;
				var drop = down * length - (row == Rows && column == 1 ? length * 0.16f : 0f);
				var wave = MathF.Sin( phase + column * 1.7f + down * 2.4f ) * 1.4f * down;
				return new Vector3( u + (across - 0.5f) * width * (1f - down * 0.05f), Y + 0.6f + wave + down * 1.2f, pole - 1f - drop );
			}
			Vector2 Uv( int column, int row )
			{
				var down = row / (float)Rows - (row == Rows && column == 1 ? 0.16f : 0f);
				return new Vector2( region.Left + region.Width * column / 2f, region.Top + region.Height * down );
			}
			for ( var row = 0; row < Rows; row++ )
			for ( var column = 0; column < 2; column++ )
				Kit.Quad( _trim, At( Point( column, row + 1 ) ), At( Point( column + 1, row + 1 ) ), At( Point( column + 1, row ) ), At( Point( column, row ) ),
					Uv( column, row + 1 ), Uv( column + 1, row + 1 ), Uv( column + 1, row ), Uv( column, row ) );
		}

		/// <summary>A barred window high under the arch: a dark reveal in a dressed-stone surround, four bars and a cross bar.</summary>
		float Window( float u, float under )
		{
			var width = Range( 28f, 36f );
			var height = Range( 46f, 56f );
			var top = MathF.Min( under - 9f, 216f );
			var sill = top - height;
			var stone = Stone();
			const float Front = 31.5f;
			var y = (Front + RingBack) * 0.5f;
			var d = RingBack - Front;
			Kit.Box( stone, At( new Vector3( u, y - 1f, sill - 2.5f ) ), new Vector3( width + 12f, d + 2f, 5f ), Run );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( stone, At( new Vector3( u + s * (width * 0.5f + 3f), y, sill + height * 0.5f ) ), new Vector3( 6f, d, height ), Run );
			Kit.Box( stone, At( new Vector3( u, y, top + 3.5f ) ), new Vector3( width + 14f, d, 7f ), Run );
			Room.WallPicture( Side, _trim, WindowSwatch, u - width * 0.5f, u + width * 0.5f, sill, top, Face - 0.3f, false );
			var iron = P( Paint.Iron );
			var bars = 3 + Pick( 2 );
			for ( var b = 0; b < bars; b++ )
			{
				var x = u - width * 0.5f + width * (b + 1f) / (bars + 1f);
				Kit.Tube( iron, At( new[] { new Vector3( x, Front + 3f, sill ), new Vector3( x, Front + 3f, top ) } ), 0.6f, 4 );
			}
			Kit.Tube( iron, At( new[] { new Vector3( u - width * 0.5f, Front + 2f, sill + height * 0.55f ), new Vector3( u + width * 0.5f, Front + 2f, sill + height * 0.55f ) } ), 0.55f, 4 );
			return width + 14f;
		}

		/// <summary>A chain swagged between two wall rings at the arch's springing.</summary>
		void Swag( Bay bay )
		{
			var left = bay.From + Range( 8f, 12f );
			var right = bay.To - Range( 8f, 12f );
			var v = Range( 156f, 164f );
			WallRing( left, v );
			WallRing( right, v + Range( -3f, 3f ) );
			var path = new Vector3[9];
			var sag = Range( 14f, 18f );
			for ( var i = 0; i < path.Length; i++ )
			{
				var t = i / (path.Length - 1f);
				path[i] = new Vector3( left + (right - left) * t, Face - 3f, v - 4.5f - MathF.Sin( t * MathF.PI ) * sag );
			}
			Chain( path, 7f );
		}

		/// <summary>A tapestry hung from a pole under the arch: a wide cloth in wine or deep lilac, a pale border, an emblem, a fringe.</summary>
		void Tapestry( float u, Func<float, float> under )
		{
			var width = Range( 52f, 64f );
			var height = width * Range( 0.95f, 1.1f );
			var pole = under( width * 0.5f + 6f ) - Range( 8f, 11f );
			const float Y = 35f;
			var iron = P( Paint.Iron );
			Kit.Tube( iron, At( new[] { new Vector3( u - width * 0.5f - 4f, Y, pole ), new Vector3( u + width * 0.5f + 4f, Y, pole ) } ), 0.8f, 5 );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.Ellipsoid( P( Paint.Brass ), At( new Vector3( u + s * (width * 0.5f + 4.8f), Y, pole ) ), new Vector3( 1.4f, 1.4f, 1.4f ), Run, 2, 6 );
				Kit.Tube( iron, At( new[] { new Vector3( u + s * (width * 0.5f + 1f), FloorContext.Recess + FloorContext.Embed, pole ), new Vector3( u + s * (width * 0.5f + 1f), Y, pole ) } ), 0.5f, 3 );
			}
			var region = TapestrySwatch( Pick( 2 ) );
			var phase = Next() * MathF.Tau;
			const int Columns = 3;
			const int Rows = 2;
			Vector3 Point( int column, int row )
			{
				var across = column / (float)Columns;
				var down = row / (float)Rows;
				return new Vector3( u + (across - 0.5f) * width, Y + 0.8f + MathF.Sin( phase + column * 2.1f ) * 1.1f * (0.4f + down), pole - 1f - down * height );
			}
			Vector2 Uv( int column, int row ) => new( region.Left + region.Width * column / Columns, region.Top + region.Height * row / Rows );
			for ( var row = 0; row < Rows; row++ )
			for ( var column = 0; column < Columns; column++ )
				Kit.Quad( _trim, At( Point( column, row + 1 ) ), At( Point( column + 1, row + 1 ) ), At( Point( column + 1, row ) ), At( Point( column, row ) ),
					Uv( column, row + 1 ), Uv( column + 1, row + 1 ), Uv( column + 1, row ), Uv( column, row ) );
		}

		/// <summary>A gibbet cage hung from the crown on a chain: bars between two hoops, a domed top, a plate floor with a skull on it. One a wall.</summary>
		void Cage( float u, float crown )
		{
			const float R = 12f;
			const float Y = 14f;
			var h = Range( 40f, 44f );
			var top = crown - Range( 14f, 18f );
			var bottom = top - h;
			var iron = P( Paint.Iron );
			Chain( new[] { new Vector3( u, Y, crown - 0.5f ), new Vector3( u, Y, top + 5.5f ) }, 3.8f );
			Kit.Dome( iron, At( new Vector3( u, Y, top ) ), new Vector3( R + 0.5f, R + 0.5f, 5f ), Run, 2, 8 );
			Kit.SmoothLathe( iron, At( new Vector3( u, Y, bottom - 1f ) ), new[] { new Vector2( R + 0.4f, 0f ), new Vector2( R + 0.4f, 1.2f ) }, 8, Run, true, true );
			foreach ( var v in new[] { top - 0.5f, bottom + h * 0.45f } )
				Kit.Tube( iron, At( Loop( new Vector3( u, Y, v ), Vector3.Forward, new Vector3( 0f, 1f, 0f ), R + 0.2f, 8 ) ), 0.5f, 3 );
			for ( var k = 0; k < 8; k++ )
			{
				var a = k * MathF.Tau / 8f + 0.2f;
				var dir = new Vector3( MathF.Cos( a ), MathF.Sin( a ), 0f );
				Kit.Tube( iron, At( new[] { new Vector3( u, Y, bottom ) + dir * R, new Vector3( u, Y, bottom + h * 0.5f ) + dir * (R + 0.6f), new Vector3( u, Y, top ) + dir * R } ), 0.4f, 3 );
			}
			Skull( new Vector3( u + Range( -3f, 3f ), Y + Range( -2f, 2f ), bottom + 0.2f ), 0.95f, Range( -50f, 50f ) );
		}

		/// <summary>A trophy on the wall under the arch: two spears crossed behind a round shield.</summary>
		void Trophy( float u, float under )
		{
			var r = Range( 13f, 15f );
			var v = MathF.Min( under - r - 24f, 184f );
			foreach ( var s in new[] { -1f, 1f } )
			{
				var foot = new Vector3( u - s * 26f, Face - 2f, v - 30f );
				var top = new Vector3( u + s * 26f, Face - 2f, v + 30f );
				Kit.Tube( P( Paint.DarkWood ), At( new[] { foot, top } ), 0.75f, 4 );
				var dir = (top - foot).Normal;
				Kit.SmoothLathe( P( Paint.Steel ), At( top ), new[] { new Vector2( 0.9f, 0f ), new Vector2( 1.8f, 2.6f ), new Vector2( 0f, 9.5f ) }, 4, Turn( Rotation.FromToRotation( Vector3.Up, dir ) ), false, false );
			}
			ShieldAt( new Vector3( u, Face - 3.2f, v ), r );
		}

		/// <summary>
		/// A shield with its back at <paramref name="back"/>, facing the room, in the wall's next design: a round shield
		/// painted lilac with a pale chevron, or quartered wine and cream, or pale stone with a wine band across it (all
		/// with an iron rim and a boss), or a heater — the pointed knight's shield — in wine with a cream cross.
		/// </summary>
		void ShieldAt( Vector3 back, float r )
		{
			var design = _design++ % 4;
			if ( design == 3 )
			{
				Heater( back, r );
				return;
			}
			// The face's texture turned half round: facing the room, the lathe's cap shows its picture upside down.
			var face = design == 2 ? P( Paint.PaleStone ) : T( ShieldSwatch( design ) );
			Kit.SmoothLathe( face, At( back ), new[] { new Vector2( r, 0f ), new Vector2( r, 1.2f ) }, 10, Facing, false, true, 45f, 0.5f + Range( -0.03f, 0.03f ) );
			Kit.SmoothLathe( P( Paint.Iron ), At( back ), new[] { new Vector2( r + 0.5f, 0.2f ), new Vector2( r + 0.2f, 1.7f ) }, 10, Facing, false, false );
			if ( design == 2 ) Kit.Box( P( Paint.Wine ), At( back + new Vector3( 0f, -1.4f, 0f ) ), new Vector3( r * 1.9f, 0.5f, r * 0.42f ), Run );
			Kit.Ellipsoid( P( Paint.Rubbed ), At( back + new Vector3( 0f, -1.4f, 0f ) ), new Vector3( r * 0.26f, r * 0.26f, r * 0.26f ), Facing, 2, 6 );
		}

		/// <summary>
		/// A heater shield, its back at <paramref name="back"/> and about <paramref name="r"/> in half-height: flat across the
		/// top, straight sides curving in to a point, a wine field with a cream cross, its edge in iron.
		/// </summary>
		void Heater( Vector3 back, float r )
		{
			var w = r * 0.86f;
			var outline = new[]
			{
				new Vector2( -w, r ), new Vector2( w, r ), new Vector2( w, -0.1f * r ), new Vector2( 0.8f * w, -0.55f * r ), new Vector2( 0.45f * w, -0.95f * r ),
				new Vector2( 0f, -1.2f * r ), new Vector2( -0.45f * w, -0.95f * r ), new Vector2( -0.8f * w, -0.55f * r ), new Vector2( -w, -0.1f * r ),
			};
			const float Thick = 1.2f;
			Vector3 Front( Vector2 p ) => back + new Vector3( p.x, -Thick, p.y );
			Vector3 Rear( Vector2 p ) => back + new Vector3( p.x, 0f, p.y );
			var field = P( Paint.Wine );
			var iron = P( Paint.Iron );
			var centre = Front( new Vector2( 0f, 0f ) );
			for ( var k = 0; k < outline.Length; k++ )
			{
				// Corners go round clockwise as the room sees them, so each face is wound to face the room (and the edge out).
				var a = outline[k]; var b = outline[(k + 1) % outline.Length];
				Kit.Tri( field, At( centre ), At( Front( b ) ), At( Front( a ) ) );
				Kit.Quad( iron, At( Rear( b ) ), At( Rear( a ) ), At( Front( a ) ), At( Front( b ) ) );
			}
			var cream = P( Paint.Cream );
			Kit.Box( cream, At( back + new Vector3( 0f, -Thick - 0.25f, -0.05f * r ) ), new Vector3( r * 0.26f, 0.5f, r * 1.85f ), Run );
			Kit.Box( cream, At( back + new Vector3( 0f, -Thick - 0.25f, 0.38f * r ) ), new Vector3( w * 1.7f, 0.5f, r * 0.26f ), Run );
		}
	}
}
