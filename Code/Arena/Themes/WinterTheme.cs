using System;
using System.Collections.Generic;

/// <summary>
/// Floor 2 (index 1): a winter cabin. Underfoot, blocks of packed snow — soft drifts, lumpy rims,
/// a glossy sheen of old ice where it has worn thin, and frost that sparkles. Overhead (the
/// undersides of the greenhouse floor), knotty pine boards.
/// </summary>
public sealed class WinterTheme : ArenaTheme
{
	public override string Name => "Winter";
	public override int Floor => 1;
	public override Color LightColor => new( 0.90f, 0.97f, 1.10f );

	static Color Snow => Palette.Hex( 0xEEF3F8 );
	static Color SnowLight => Palette.Hex( 0xFFFFFF );
	static Color Ice => Palette.Hex( 0xA7CBE8 );

	/// <summary>Old ice seen through a skin of snow: barely blue, glossy.</summary>
	static Color IceSheen => Mix( Snow, Ice, 0.45f );

	/// <summary>Drift hollows: cool blue-white, never grey.</summary>
	static Color SnowCool => Palette.Hex( 0xD7E5F3 );

	/// <summary>The gap between snow blocks: blue shadow, not grey.</summary>
	static Color Crevice => Palette.Hex( 0xA6BCD4 );

	static Color Pine => Palette.Hex( 0xC9A27A );
	static Color PinePale => Palette.Hex( 0xDBBC95 );
	static Color PineDeep => Palette.Hex( 0xB7895D );
	static Color LateWood => Palette.Hex( 0xA0714A );
	static Color Knot => Palette.Hex( 0x6A4329 );
	static Color BoardGap => Palette.Hex( 0x5C4331 );

	/// <summary>Weathered log timber: silvered on the faces, warmer in the checks and knots.</summary>
	static Color Log => Palette.Hex( 0xAA845E );
	static Color LogLight => Palette.Hex( 0xC09B73 );
	static Color LogShade => Palette.Hex( 0x87654A );
	static Color LogGrain => Palette.Hex( 0x6C4F37 );
	static Color LogSilver => Palette.Hex( 0xA9A196 );

	/// <summary>Lime chinking between the logs.</summary>
	static Color Chinking => Palette.Hex( 0xB9AE9C );
	static Color ChinkingShade => Palette.Hex( 0x968B7A );

	/// <summary>Hewn beams and posts.</summary>
	static Color Beam => Palette.Hex( 0xA77F5A );
	static Color BeamShade => Palette.Hex( 0x5E4330 );

	static Color Frost => Palette.Hex( 0xDDEBF5 );
	static Color FramePaint => Palette.Hex( 0xF1F0EA );

	/// <summary>
	/// The structural front layer — hewn posts, a top plate, skirting — and frosted windows in about
	/// half the bays; then the dressing: icicles all along the top plate, a string of warm bulbs swagged
	/// along it, two iron lanterns on posts, snow banked on the sills and drifted into bays. Every bay is
	/// dressed as a set, low and high: under a window a bench or a low woodpile; in the plain bays a
	/// snowman in relief (one to a wall) with a sledge, crossed skis over a bench on two walls, and the
	/// rest dealt from four sets — a big woodpile with snowshoes or a wreath over it, a bench under a peg
	/// rail of coats and scarves, shelves of stoneware, a sledge with snowshoes. Lights: two of the four
	/// warm lamps hang in the lanterns, two among the bulbs; two cool lights by the walls at the windows'
	/// height stand for the daylight off the snow.
	/// </summary>
	public override void Dress( FloorContext room )
	{
		var bays = BuildFrontLayer( room, new FrontStyle { Post = 30f, Pier = 74f, Corner = 34f, Cornice = 28f, Skirting = 12f, SkirtingDepth = 5f } );
		var windows = AddWindows( room, bays, InfillCell( 0 ), InfillCell( 1 ), WindowSill, WindowHeight, 84f, 0.45f, true, Seed + 800 );
		// The shader lights the mask at 2^brightness times the paint. At 1.5 (2.8x) and still at 0 (1x) the
		// bulbs clipped to white or cream in the game camera; at half the paint they keep their amber and
		// still bloom against the dim logs.
		Glow.Set( "g_flSelfIllumBrightness", -1f );
		var props = new Props( room, Trim, Glow );

		var walls = new Random( Seed + 1700 );
		var skiWall = walls.Next( 4 );
		var otherSkiWall = (skiWall + 1 + walls.Next( 3 )) % 4;

		for ( var side = 0; side < 4; side++ )
		{
			props.Begin( side, Seed + 1000 + side * 137 );
			props.Icicles();
			props.BulbString();

			var wall = bays.FindAll( b => b.Side == side );
			foreach ( var (bay, width) in windows.FindAll( w => w.Bay.Side == side ) )
				props.WindowBay( bay, width );

			// The plain bays, in a shuffled order: the snowman's set in the first, the skis' next where this
			// wall has them, then this wall's own order of the four other sets dealt round, so neighbours in
			// the deal never match and a wall with four plain bays shows all four. A narrow bay gets a drift.
			var plain = wall.FindAll( b => b.Width > 70f && !windows.Exists( w => w.Bay.From == b.From && w.Bay.Side == side ) );
			props.Shuffle( plain );
			var deck = new List<Props.Look> { Props.Look.Woodpile, Props.Look.Mudroom, Props.Look.Shelves, Props.Look.Sledge };
			props.Shuffle( deck );

			var dealt = 0;
			for ( var k = 0; k < plain.Count; k++ )
			{
				var bay = plain[k];
				if ( k == 0 ) props.SnowmanBay( bay );
				else if ( k == 1 && (side == skiWall || side == otherSkiWall) ) props.SkiBay( bay );
				else props.Furnish( deck[dealt++ % deck.Count], bay );
			}
			foreach ( var bay in wall.FindAll( b => b.Width <= 70f && b.Width > 24f ) )
				props.Drift( bay );
		}

		// Two of the warm lamps hang in iron lanterns on posts (never on the pier, where the plaque is);
		// the other two burn among the bulbs.
		var warm = new Color( 1.25f, 0.95f, 0.66f ) * 3.6f;
		var lamps = LampSpots( 4, Seed + 900 );
		props.Begin( 0, Seed + 1900 );
		for ( var k = 0; k < lamps.Count; k++ )
		{
			var lamp = lamps[k];
			if ( k < 2 )
			{
				var post = NearestPost( bays, lamp.Side, lamp.U );
				props.Lantern( lamp.Side, post );
				AddLamp( room, lamp.Side, post, Props.LanternLight, Props.LanternDepth, warm * (0.7f * lamp.Brightness) );
			}
			else AddLamp( room, lamp.Side, lamp.U, Props.StringV - 8f, -6f, warm * lamp.Brightness );
		}
		AddLamps( room, 2, WindowSill + WindowHeight * 0.5f, -30f, LightColor * 3f, Seed + 901 );
	}

	/// <summary>The middle of the post nearest <paramref name="u"/> on wall <paramref name="side"/>, never the pier mid-wall.</summary>
	static float NearestPost( List<Bay> bays, int side, float u )
	{
		var wall = bays.FindAll( b => b.Side == side );
		var best = wall[0].From;
		for ( var k = 0; k + 1 < wall.Count; k++ )
		{
			var middle = (wall[k].To + wall[k + 1].From) * 0.5f;
			if ( MathF.Abs( middle - FloorContext.HalfSpan ) < 60f ) continue;
			if ( MathF.Abs( middle - u ) < MathF.Abs( best - u ) ) best = middle;
		}
		return best;
	}

	/// <summary>
	/// The lit parts: bulbs in a warm tungsten white, an amber and a peach (old strings mix them), and the
	/// glass of a lantern lit from inside, a little softer. Painted well into the warm range: bloom and
	/// tonemapping pushed the paler warm white the bulbs first had to plain white.
	/// </summary>
	protected override PaintedSurface PaintGlow()
	{
		const int Size = 64;
		var painter = new Painter( Size, Palette.Hex( 0xFFB86E ), 0.3f );
		painter.Fill( 32, 0, 32, 32, Palette.Hex( 0xFFA04E ), roughness: 0.3f );
		painter.Fill( 0, 32, 32, 32, Palette.Hex( 0xFFB27C ), roughness: 0.3f );
		painter.Fill( 32, 32, 32, 32, Palette.Hex( 0xF5C48E ), roughness: 0.4f );
		var glow = new float[Size * Size];
		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
			glow[y * Size + x] = x >= 32 && y >= 32 ? 0.62f + 0.2f * (y - 32) / 32f : 1f;
		painter.AddGlow( glow );
		return painter.Bake( "arena_1_glow", ArenaGenerator.GlowMaterialPath, 1f );
	}

	static Rect BulbGlow( int k ) => new( (k % 2) * 0.5f + 0.06f, (k / 2) * 0.5f + 0.06f, 0.38f, 0.38f );
	static Rect LanternGlow => new( 0.56f, 0.56f, 0.38f, 0.38f );

	static Rect SnowSwatch => Swatch( 256, 0, 64, 32 );
	static Rect IceSwatch => Swatch( 320, 0, 64, 32 );
	static Rect CoalSwatch => Swatch( 384, 0, 32, 32 );
	static Rect CarrotSwatch => Swatch( 416, 0, 32, 32 );
	static Rect TwigSwatch => Swatch( 448, 0, 64, 32 );
	static Rect FirSwatch( int k ) => Swatch( 256 + k * 64, 32, 64, 32 );
	static Rect JuteSwatch => Swatch( 448, 32, 64, 32 );
	static Rect SkiSwatch( int k ) => Swatch( 256 + k * 64, 64, 64, 32 );
	static Rect BambooSwatch => Swatch( 384, 64, 64, 32 );
	static Rect IronSwatch => Swatch( 448, 64, 64, 32 );
	static Rect BarkSwatch => Swatch( 256, 96, 64, 32 );
	static Rect LogEndSwatch( int k ) => Swatch( 320 + k * 64, 96, 64, 32 );
	static Rect KnitSwatch( int k ) => Swatch( 448 + k * 32, 96, 32, 32 );
	static Rect WireSwatch => Swatch( 256, 128, 32, 32 );
	static Rect ConeSwatch => Swatch( 288, 128, 32, 32 );
	static Rect FeltSwatch => Swatch( 320, 128, 32, 32 );
	static Rect LeatherSwatch => Swatch( 352, 128, 32, 32 );
	static Rect StonewareSwatch => Swatch( 384, 128, 32, 32 );
	static Rect MustardKnitSwatch => Swatch( 416, 128, 32, 32 );
	static Rect PlaidSwatch => Swatch( 448, 128, 32, 32 );
	static Rect CoatSwatch => Swatch( 480, 128, 32, 32 );

	/// <summary>
	/// The props' materials, painted into the infill band's two free cells (the first two hold the frosted
	/// pane and the frame paint): snow, ice, coal, carrot, twig, three firs, jute, two pairs of skis,
	/// bamboo, iron, firewood bark and two cut ends with their rings, two knits, wire, pine cone, felt,
	/// leather, salt-glazed stoneware, a mustard knit, a plaid blanket and a wool coat.
	/// </summary>
	static void PaintSwatches( Painter painter, Random random )
	{
		float Next() => (float)random.NextDouble();
		var band = TrimRows( TrimBand.Infill ).Top;

		{
			var y0 = band;
			// Snow: white with cool blue hollows and a few glints.
			painter.Fill( 256, y0, 64, 32, Snow, roughness: 0.62f );
			for ( var s = 0; s < 22; s++ )
				painter.Polygon( Organic( new Vector2( 256f + Next() * 64f, y0 + Next() * 32f ), new Vector2( 4f + Next() * 8f, 2f + Next() * 4f ), 0.8f, random.Next() ), SnowCool, alpha: 0.35f );
			for ( var s = 0; s < 40; s++ )
				painter.Circle( 256f + Next() * 64f, y0 + Next() * 32f, 0.5f, SnowLight, 0.9f, roughness: 0.15f );

			// Ice: glossy and blue-clear round a milky core (the swatch runs round the icicle, so the core
			// is a band across it), frosted white at the root (top), bluest toward the tip, with drip ridges.
			for ( var y = 0; y < 32; y++ )
				painter.Fill( 320, y0 + y, 64, 1, Mix( Palette.Hex( 0xE4F0F8 ), Palette.Hex( 0xA9CAE4 ), y / 31f ), roughness: 0.05f + 0.1f * (1f - y / 31f) );
			painter.Rect( 340f, y0, 22f, 32f, Palette.Hex( 0xF6FAFD ), radius: 6f, alpha: 0.75f );
			for ( var s = 0; s < 16; s++ )
			{
				var x = 320f + Next() * 64f;
				painter.Line( new Vector2( x, y0 ), new Vector2( x + Next() * 2f - 1f, y0 + 32f ), 0.8f + Next(), Palette.Hex( 0xFFFFFF ), 0.45f );
			}

			painter.Fill( 384, y0, 32, 32, Palette.Hex( 0x2B2829 ), roughness: 0.75f );
			for ( var s = 0; s < 18; s++ ) painter.Circle( 384f + Next() * 32f, y0 + Next() * 32f, 0.5f + Next() * 0.6f, Palette.Hex( 0x4A4547 ), 0.7f, roughness: 0.4f );

			// Carrot: the accent orange, ringed across its length, the tip (top) paler.
			painter.Fill( 416, y0, 32, 32, Palette.Hex( 0xE9A06A ), roughness: 0.55f );
			for ( var y = 2f; y < 32f; y += 3f + Next() * 2f ) painter.Line( new Vector2( 416f, y0 + y ), new Vector2( 448f, y0 + y + Next() - 0.5f ), 0.6f, Palette.Hex( 0xC27A47 ), 0.5f );

			// Twig: grey-brown bark running along it.
			painter.Fill( 448, y0, 64, 32, Palette.Hex( 0x6B5848 ), roughness: 0.9f );
			for ( var x = 448f; x < 512f; x += 2f + Next() * 3f ) painter.Line( new Vector2( x, y0 ), new Vector2( x + Next() * 2f - 1f, y0 + 32f ), 0.7f, Palette.Hex( 0x51433A ), 0.5f );
		}

		{
			// Fir: crowded needles over a dark heart, three greens from blue-green to warm.
			var y0 = band + 32;
			var firs = new[] { Palette.Hex( 0x3F5E4C ), Palette.Hex( 0x4E6B4A ), Palette.Hex( 0x5B7454 ) };
			for ( var k = 0; k < 3; k++ )
			{
				var x0 = 256 + k * 64;
				painter.Fill( x0, y0, 64, 32, Mix( firs[k], Palette.Ink, 0.45f ), roughness: 0.75f );
				for ( var s = 0; s < 260; s++ )
				{
					var at = new Vector2( x0 + Next() * 64f, y0 + Next() * 32f );
					var angle = Next() * MathF.Tau;
					var tone = Mix( firs[k], Color.White, Next() * 0.15f );
					painter.Line( at, at + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * (2f + Next() * 2f), 0.8f, tone, 0.8f );
				}
			}

			// Jute ribbon: an open weave.
			painter.Fill( 448, y0, 64, 32, Palette.Hex( 0xB79E72 ), roughness: 0.95f );
			for ( var x = 0; x < 64; x += 3 ) painter.Fill( 448 + x, y0, 1, 32, Palette.Hex( 0x957D56 ) );
			for ( var y = 1; y < 32; y += 3 ) painter.Rect( 448f, y0 + y, 64f, 1f, Palette.Hex( 0xCDB88E ), alpha: 0.6f );
		}

		{
			// Skis: varnished hickory along their length with a painted stripe down the middle.
			var y0 = band + 64;
			var stripes = new[] { Palette.Hex( 0x6F8DA8 ), Palette.Hex( 0xE6DCC2 ) };
			for ( var k = 0; k < 2; k++ )
			{
				var x0 = 256 + k * 64;
				painter.Fill( x0, y0, 64, 32, Palette.Hex( 0xB4824F ), roughness: 0.3f );
				for ( var y = 1f; y < 32f; y += 2f + Next() * 2f )
					painter.Line( new Vector2( x0, y0 + y ), new Vector2( x0 + 64f, y0 + y + Next() * 2f - 1f ), 0.6f, Palette.Hex( 0x8C5F37 ), 0.4f );
				painter.Fill( x0, y0 + 12, 64, 8, stripes[k], roughness: 0.35f );
				painter.Fill( x0, y0 + 11, 64, 1, Mix( stripes[k], Palette.Ink, 0.4f ) );
				painter.Fill( x0, y0 + 20, 64, 1, Mix( stripes[k], Palette.Ink, 0.4f ) );
			}

			// Bamboo: pale cane with a node every few inches (across the run of the pole).
			painter.Fill( 384, y0, 64, 32, Palette.Hex( 0xCDB784 ), roughness: 0.4f );
			for ( var y = 4; y < 32; y += 10 ) painter.Fill( 384, y0 + y, 64, 2, Palette.Hex( 0x9E8858 ) );

			painter.Fill( 448, y0, 64, 32, Palette.Hex( 0x3A3634 ), roughness: 0.6f );
			for ( var s = 0; s < 24; s++ ) painter.Circle( 448f + Next() * 64f, y0 + Next() * 32f, 0.6f + Next() * 1.2f, Palette.Hex( 0x5A4436 ), 0.35f );
		}

		{
			var y0 = band + 96;
			// Firewood bark: grey-brown ridges and deep fissures along the log.
			painter.Fill( 256, y0, 64, 32, Palette.Hex( 0x948069 ), roughness: 0.95f );
			for ( var x = 256f; x < 320f; x += 3f + Next() * 4f )
				painter.Line( new Vector2( x, y0 ), new Vector2( x + Next() * 3f - 1.5f, y0 + 32f ), 0.7f + Next() * 0.8f, Palette.Hex( 0x4F4033 ), 0.5f );
			for ( var s = 0; s < 10; s++ )
				painter.Polygon( Organic( new Vector2( 256f + Next() * 64f, y0 + Next() * 32f ), new Vector2( 3f + Next() * 4f, 1.5f + Next() * 2f ), 0.8f, random.Next() ), Palette.Hex( 0x8B8A7C ), alpha: 0.4f );

			// Cut ends: mapped round the end from the bark (bottom row) to the pith (top), so rings are rows.
			for ( var k = 0; k < 2; k++ )
			{
				var x0 = 320 + k * 64;
				var wood = k == 0 ? Palette.Hex( 0xD9B98C ) : Palette.Hex( 0xCFAE80 );
				painter.Fill( x0, y0, 64, 32, wood, roughness: 0.85f );
				painter.Fill( x0, y0 + 28, 64, 4, Palette.Hex( 0x5E4D3F ) );
				painter.Fill( x0, y0 + 25, 64, 3, Mix( wood, Color.White, 0.15f ) );
				for ( var y = 24f; y > 2f; y -= 1.6f + Next() * 1.8f )
					painter.Line( new Vector2( x0, y0 + y ), new Vector2( x0 + 64f, y0 + y + Next() - 0.5f ), 0.5f, Mix( wood, Palette.Hex( 0x8A6440 ), 0.6f ), 0.5f );
				painter.Fill( x0, y0, 64, 3, Palette.Hex( 0x8A6440 ) );
				// Drying checks run from the bark toward the heart.
				for ( var c = 1 + random.Next( 3 ); c > 0; c-- )
				{
					var x = x0 + Next() * 64f;
					painter.Line( new Vector2( x, y0 + 28f ), new Vector2( x + Next() * 2f - 1f, y0 + 10f + Next() * 10f ), 0.9f, Palette.Hex( 0x4E3C2C ), 0.8f );
				}
				painter.Polygon( Organic( new Vector2( x0 + 32f, y0 + 14f ), new Vector2( 30f, 12f ), 0.5f, random.Next() ), Palette.Hex( 0x9C7A55 ), alpha: 0.12f );
			}

			// Knits: rows of little vees, a muted blue and an oatmeal.
			var knits = new[] { Palette.Hex( 0x8DA4BC ), Palette.Hex( 0xD6CAAE ) };
			for ( var k = 0; k < 2; k++ )
			{
				var x0 = 448 + k * 32;
				painter.Fill( x0, y0, 32, 32, Mix( knits[k], Palette.Ink, 0.25f ), roughness: 0.95f );
				for ( var y = 1f; y < 32f; y += 3f )
				for ( var x = 1f; x < 32f; x += 3f )
				{
					painter.Line( new Vector2( x0 + x, y0 + y ), new Vector2( x0 + x + 1.4f, y0 + y + 2.4f ), 1.1f, knits[k], 1f );
					painter.Line( new Vector2( x0 + x + 2.8f, y0 + y ), new Vector2( x0 + x + 1.4f, y0 + y + 2.4f ), 1.1f, knits[k], 1f );
				}
				if ( k == 1 ) painter.Fill( x0, y0 + 13, 32, 5, Palette.Hex( 0xA9776A ), roughness: 0.95f );
			}
		}

		{
			var y0 = band + 128;
			painter.Fill( 256, y0, 32, 32, Palette.Hex( 0x2F3A31 ), roughness: 0.5f );
			painter.Fill( 288, y0, 32, 32, Palette.Hex( 0x6E4F35 ), roughness: 0.85f );
			for ( var y = 2f; y < 32f; y += 4f )
			for ( var x = (y % 8f < 4f ? 0f : 2f); x < 32f; x += 4f )
				painter.Circle( 288f + x + 1.5f, y0 + y, 1.4f, Palette.Hex( 0x8E6A47 ), 0.8f );
			painter.Fill( 320, y0, 32, 32, Palette.Hex( 0x56535C ), roughness: 0.95f );
			painter.Fill( 352, y0, 32, 32, Palette.Hex( 0x5B3F2D ), roughness: 0.6f );

			// Stoneware, mapped base (bottom row) to lip (top): a cream glaze over the shoulder, the lower body
			// dipped in brown, the line between them running wet.
			painter.Fill( 384, y0, 32, 32, Palette.Hex( 0xE6DCC6 ), roughness: 0.3f );
			for ( var x = 0; x < 32; x++ )
			{
				var dip = 13 + (int)(MathF.Sin( x * 0.6f ) * 1.5f + Next() * 2f);
				painter.Fill( 384 + x, y0 + 32 - dip, 1, dip, Palette.Hex( 0x8A5C3B ), roughness: 0.25f );
			}
			for ( var s = 0; s < 14; s++ ) painter.Circle( 384f + Next() * 32f, y0 + Next() * 18f, 0.5f, Palette.Hex( 0xC9BCA2 ), 0.6f );

			// Mustard knit, and a plaid of slate blue on oatmeal.
			painter.Fill( 416, y0, 32, 32, Palette.Hex( 0xA88440 ), roughness: 0.95f );
			for ( var y = 1f; y < 32f; y += 3f )
			for ( var x = 1f; x < 32f; x += 3f )
			{
				painter.Line( new Vector2( 416f + x, y0 + y ), new Vector2( 417.4f + x, y0 + y + 2.4f ), 1.1f, Palette.Hex( 0xD3A955 ), 1f );
				painter.Line( new Vector2( 418.8f + x, y0 + y ), new Vector2( 417.4f + x, y0 + y + 2.4f ), 1.1f, Palette.Hex( 0xD3A955 ), 1f );
			}
			painter.Fill( 448, y0, 32, 32, Palette.Hex( 0xD9CFB8 ), roughness: 0.95f );
			for ( var k = 2; k < 32; k += 10 )
			{
				painter.Rect( 448f + k, y0, 4f, 32f, Palette.Hex( 0x6F86A0 ), alpha: 0.75f );
				painter.Rect( 448f, y0 + k, 32f, 4f, Palette.Hex( 0x6F86A0 ), alpha: 0.75f );
				painter.Rect( 448f + k + 6, y0, 1f, 32f, Palette.Hex( 0x4E5F74 ), alpha: 0.6f );
				painter.Rect( 448f, y0 + k + 6, 32f, 1f, Palette.Hex( 0x4E5F74 ), alpha: 0.6f );
			}

			// A slate-blue wool coat: felted, a faint twill running down it.
			painter.Fill( 480, y0, 32, 32, Palette.Hex( 0x56627A ), roughness: 0.95f );
			for ( var x = 0f; x < 32f; x += 1.5f )
				painter.Line( new Vector2( 480f + x, y0 ), new Vector2( 482f + x, y0 + 32f ), 0.5f, Palette.Hex( 0x4A5468 ), 0.5f );
		}
	}

	/// <summary>Cabin windows: sill a little over waist height, tall enough to leave a log or two above them under the plate.</summary>
	const float WindowSill = 70f;
	const float WindowHeight = 112f;

	/// <summary>
	/// Round logs to a repeat: one about every 26 units (a 65 cm log, its chinking included), so a
	/// taller room is more courses of the same logs, never fatter ones.
	/// </summary>
	const int Logs = (int)(WallRepeat / 26f + 0.5f);

	/// <summary>
	/// Log-cabin wall: <see cref="Logs"/> round logs stacked floor to ceiling, each its own girth and tone, their
	/// edges wandering along the length; grain running with the log, drying checks along the upper
	/// shoulder that start and stop, knots in a halo of darker grain, the faces silvered by weather;
	/// pale lime chinking pressed between them, with a little snow lodged on the top of some logs.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		var painter = new Painter( WallSize, Chinking, 0.92f );
		var random = new Random( Seed + 600 );
		float Next() => (float)random.NextDouble();

		var heights = new float[Logs];
		var total = 0f;
		for ( var k = 0; k < Logs; k++ ) total += heights[k] = 0.85f + Next() * 0.3f;
		var centres = new float[Logs];
		var radii = new float[Logs];
		var palette = new Color[Logs + 1];
		palette[0] = new Color( 0f, 0f, 0f, 0f );
		var checkRow = new float[Logs]; var checkFrom = new float[Logs]; var checkLength = new float[Logs];
		var at = 0f;
		for ( var k = 0; k < Logs; k++ )
		{
			var h = heights[k] / total * WallSize;
			centres[k] = at + h * 0.5f;
			radii[k] = h * 0.5f - 4f - Next() * 4f;
			at += h;
			var tone = Next() * 2f - 1f;
			var wood = tone < 0f ? Mix( Log, LogShade, -tone * 0.6f ) : Mix( Log, LogLight, tone * 0.6f );
			palette[k + 1] = Mix( wood, LogSilver, Next() * 0.35f );
			checkRow[k] = -0.55f + Next() * 0.4f;
			checkFrom[k] = Next() * WallSize;
			checkLength[k] = 200f + Next() * 500f;
		}

		var grain = painter.Noise( 6, Seed + 601, 2 );
		var figure = painter.Noise( 3, Seed + 602, 2 );
		var wobble = painter.Noise( 8, Seed + 603, 3 );
		var lodged = painter.Noise( 24, Seed + 604, 3 );
		var index = new byte[WallSize * WallSize];
		var relief = new float[WallSize * WallSize];
		var late = new float[WallSize * WallSize];
		var snow = new float[WallSize * WallSize];
		var lit = new float[WallSize * WallSize];
		var dark = new float[WallSize * WallSize];
		var shadow = new float[WallSize * WallSize];
		var log = 0;

		for ( var y = 0; y < WallSize; y++ )
		{
			for ( var x = 0; x < WallSize; x++ )
			{
				var i = y * WallSize + x;
				var shift = (wobble[i] - 0.5f) * 10f;
				while ( log + 1 < Logs && y > (centres[log] + centres[log + 1]) * 0.5f ) log++;
				var k = log;
				if ( k > 0 && y + shift < (centres[k - 1] + centres[k]) * 0.5f ) k--;
				else if ( k + 1 < Logs && y + shift > (centres[k] + centres[k + 1]) * 0.5f ) k++;
				var t = (y + shift - centres[k]) / radii[k];
				var a = MathF.Abs( t );
				if ( a >= 1f )
				{
					// Chinking: rough lime, a little sunk, in shadow right under the log above it, with
					// snow lodged here and there where it meets the top of the log below.
					relief[i] = 0.25f + (lodged[i] - 0.5f) * 0.2f;
					if ( t > 0f ) shadow[i] = Math.Clamp( 1f - (t - 1f) * radii[k] / 9f, 0f, 1f ) * 0.6f;
					else snow[i] = lodged[i] > 0.66f ? MathF.Min( (lodged[i] - 0.66f) * 5f, 1f ) * 0.45f : 0f;
					continue;
				}

				index[i] = (byte)(k + 1);
				relief[i] = 0.2f + MathF.Sqrt( 1f - t * t ) * 1.1f;

				// A round log: its top catches the light, its underside turns into its own shadow.
				lit[i] = t < -0.3f ? (-0.3f - t) / 0.7f * 0.3f : 0f;
				dark[i] = t > 0.05f ? MathF.Pow( (t - 0.05f) / 0.95f, 1.4f ) * 0.6f : 0f;

				var s = StreakX( grain, WallSize, x, y + k * 97, 37 );
				var f = StreakX( figure, WallSize, x, y + k * 31, 5 );
				late[i] = (s > 0.6f ? MathF.Min( (s - 0.6f) * 7f, 1f ) * 0.45f : 0f) + (f > 0.55f ? (f - 0.55f) * 0.6f : 0f);

				// A drying check: a narrow groove along the log that starts and stops, tapering at both ends.
				var along = x - checkFrom[k];
				if ( along < 0f ) along += WallSize;
				if ( along < checkLength[k] )
				{
					var taper = MathF.Min( along, checkLength[k] - along ) / 60f;
					var width = 0.035f * MathF.Min( taper, 1f );
					var d = MathF.Abs( t - checkRow[k] - (wobble[i] - 0.5f) * 0.08f );
					if ( d < width )
					{
						relief[i] -= 0.35f * (1f - d / width);
						late[i] = MathF.Max( late[i], 0.8f );
					}
				}

				// Fresh snow along the top shoulder of some logs.
				if ( t < -0.85f && lodged[i] > 0.7f )
					snow[i] = MathF.Min( (lodged[i] - 0.7f) * 5f, 1f ) * ((-t - 0.85f) / 0.15f) * 0.5f;
			}
		}

		painter.Blend( index, palette );
		painter.AddHeight( relief, 1f );
		painter.Blend( late, LogGrain );
		painter.Blend( Ramp( painter.Noise( 128, Seed + 605, 2 ), 0.5f, 0.8f, 0.25f ), ChinkingShade );

		for ( var k = 0; k < Logs; k++ )
		{
			var knots = 1 + random.Next( 3 );
			for ( var n = 0; n < knots; n++ )
			{
				var kx = Next() * WallSize;
				var ky = centres[k] + (Next() - 0.5f) * radii[k] * 1.1f;
				var kr = 3f + Next() * 4f;
				var knotSeed = random.Next();
				painter.Polygon( Organic( new Vector2( kx, ky ), new Vector2( kr * 4f, kr * 1.8f ), 0.4f, knotSeed, 0f, 24 ), LogGrain, alpha: 0.3f );
				painter.Polygon( Organic( new Vector2( kx, ky ), new Vector2( kr * 1.3f, kr ), 0.5f, knotSeed + 1, 0f, 20 ), Knot, alpha: 0.85f, roughness: 0.6f );
			}
		}

		painter.Blend( lit, LogLight );
		painter.Blend( dark, Mix( LogShade, LogGrain, 0.6f ) );
		painter.Blend( shadow, Mix( ChinkingShade, Palette.Ink, 0.35f ) );
		painter.Blend( snow, SnowLight, roughness: 0.5f );
		painter.ShadeFromHeight( 0.12f, 0.2f );
		painter.Mottle( 0.05f, 6, Seed + 606, 4 );
		painter.RoughnessNoise( 0.06f, 8, Seed + 607, 3 );
		painter.Cavity( 0.5f, 6 );
		painter.Grain( 0.012f, Seed + 609 );
		return painter.Bake( "arena_1_wall", Painter.Matte, 6f );
	}

	/// <summary>
	/// Hewn pine beams and posts, grain running with them; the infill band holds the frosted pane
	/// (frost feathering in from the corners) and the white paint of the window frames.
	/// </summary>
	protected override PaintedSurface PaintTrim()
	{
		return PaintTrimAtlas( "arena_1_trim", Beam, BeamShade, 0.75f, 0.4f, Seed + 700, painter =>
		{
			var (x, y, w, h) = InfillPixels( 0 );
			painter.Fill( x, y, w, h, Frost, roughness: 0.2f );
			var frost = painter.Noise( 32, Seed + 701, 4 );
			var weight = new float[TrimSize * TrimSize];
			for ( var py = y; py < y + h; py++ )
			for ( var px = x; px < x + w; px++ )
			{
				var cx = MathF.Min( px - x, x + w - 1 - px ) / (w * 0.5f);
				var cy = MathF.Min( py - y, y + h - 1 - py ) / (h * 0.5f);
				var corner = 1f - MathF.Sqrt( cx * cx + cy * cy ) * 0.9f;
				var i = py * TrimSize + px;
				weight[i] = Math.Clamp( (corner + (frost[i] - 0.5f) * 0.8f - 0.15f) * 2.5f, 0f, 0.9f );
			}
			painter.Blend( weight, SnowLight, roughness: 0.6f );

			var frame = InfillPixels( 1 );
			painter.Fill( frame.X, frame.Y, frame.Width, frame.Height, FramePaint, roughness: 0.5f );

			PaintSwatches( painter, new Random( Seed + 702 ) );
		} );
	}

	protected override PaintedSurface PaintFloor()
	{
		const int cell = FloorSize / 2;
		var painter = new Painter( FloorSize, Crevice, 0.7f );
		var random = new Random( Seed );
		float Next() => (float)random.NextDouble();

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var snow = Mix( Snow, SnowLight, Next() * 0.35f );
			var joint = 4f + Next() * 5f;

			// The snow runs on over the arris and slumps toward the joint at the chamfer's foot,
			// where it is in cool blue shadow.
			painter.Fill( (int)min.x, (int)min.y, cell, cell, Mix( Snow, Crevice, 0.55f ), height: 0.55f );
			painter.Polygon( SlabOutline( min, cell, joint, 11f, 20f + Next() * 20f, Seed + c * 101 ), snow, height: 1f, bevel: Rim, roughness: 0.55f );

			// Lumpy rims: clods of snow all along the edge, overhanging the arris, so no block ever
			// shows a clean rounded rectangle.
			var lumps = 8 + random.Next( 6 );
			for ( var l = 0; l < lumps; l++ )
			{
				var side = random.Next( 4 );
				var along = 30f + Next() * 452f;
				var half = new Vector2( 20f + Next() * 32f, 11f + Next() * 14f );
				var reach = MathF.Max( Rim * (0.5f + Next() * 0.9f), half.y + 2f );
				var at = side switch
				{
					0 => min + new Vector2( along, reach ), 1 => min + new Vector2( cell - reach, along ),
					2 => min + new Vector2( along, cell - reach ), _ => min + new Vector2( reach, along ),
				};
				var angle = side % 2 == 0 ? 0f : MathF.PI * 0.5f;
				painter.Polygon( Organic( at, half, 0.8f, Seed + c * 41 + l, angle, 24 ), Mix( snow, SnowLight, 0.3f ), height: 1.1f + Next() * 0.2f, bevel: 18f, roughness: 0.55f );
			}

			// A few soft clods on the face itself.
			var clods = 2 + random.Next( 3 );
			for ( var l = 0; l < clods; l++ )
			{
				var at = min + new Vector2( 90f + Next() * 332f, 90f + Next() * 332f );
				var half = new Vector2( 22f + Next() * 30f, 16f + Next() * 20f );
				painter.Polygon( Organic( at, half, 0.7f, Seed + c * 59 + l, Next() * MathF.Tau, 24 ), Mix( snow, SnowLight, 0.4f ), height: 1.08f, bevel: 20f, roughness: 0.55f );
			}

			// Frost: tiny facets that catch the light.
			for ( var s = 0; s < 320; s++ )
			{
				var at = min + new Vector2( 16f + Next() * 480f, 16f + Next() * 480f );
				painter.Circle( at.x, at.y, 0.55f + Next() * 0.75f, SnowLight, 0.9f, roughness: 0.12f );
			}
		}

		var ridges = PaintDrifts( painter, random );
		painter.HeightNoise( 0.06f, 128, Seed + 11, 2 );
		painter.HeightNoise( 0.6f, 16, Seed + 12, 3 );
		painter.RoughnessNoise( 0.06f, 8, Seed + 13, 3 );
		painter.Cavity( 0.25f, 6 );

		// Added after the cavity bake: wind ridges shape the sheen, but grey crevice shading in their
		// grooves read as dirt on fresh snow.
		painter.AddHeight( ridges, 0.15f );
		painter.Grain( 0.01f, Seed + 14 );
		return painter.Bake( "arena_1_floor", Painter.Matte, 7f );
	}

	/// <summary>
	/// The snow's large-scale life, all from noise rather than decals so nothing repeats tile to tile:
	///   * drifts about two undulations per tile, carried mostly by colour — hollows cool blue-white,
	///     crests pure white — because the lower floors are lit by ambient light, where colour reads
	///     and normals barely do;
	///   * old ice worn through on two of the four blocks: a faint, soft-edged, glossy sheen shaped by
	///     a thresholded noise, kept off the rim;
	///   * sastrugi, returned for the caller to add once crevices are baked: fine ridges scoured along
	///     one wind direction per block, read from a stretched noise field.
	/// </summary>
	float[] PaintDrifts( Painter painter, Random random )
	{
		const int cell = FloorSize / 2;
		var drift = painter.Noise( 8, Seed + 10, 4 );
		var hollows = new float[drift.Length];
		var crests = new float[drift.Length];
		for ( var i = 0; i < drift.Length; i++ )
		{
			var d = drift[i];
			hollows[i] = d < 0.52f ? MathF.Min( (0.52f - d) * 3.5f, 1f ) * 0.85f : 0f;
			crests[i] = d > 0.56f ? MathF.Min( (d - 0.56f) * 3.5f, 1f ) * 0.5f : 0f;
		}

		var ice = painter.Noise( 5, Seed + 20, 4 );
		var iceShare = new[] { 0f, 0.85f, 0.5f, 0f };
		var keepOff = Rim + 24f;
		for ( var y = 0; y < FloorSize; y++ )
		{
			var cy = y % cell;
			var rowEdge = MathF.Min( cy, cell - 1 - cy );
			for ( var x = 0; x < FloorSize; x++ )
			{
				var i = y * FloorSize + x;
				var cx = x % cell;
				var edge = MathF.Min( rowEdge, MathF.Min( cx, cell - 1 - cx ) );
				var share = iceShare[(y / cell) * 2 + x / cell];
				var t = (ice[i] - 0.66f) * 5f;
				var fade = (edge - keepOff) / 30f;
				ice[i] = share <= 0f || t <= 0f || fade <= 0f ? 0f : MathF.Min( t, 1f ) * MathF.Min( fade, 1f ) * share * 0.6f;
			}
		}

		var scour = painter.Noise( 64, Seed + 15, 2 );
		var ridges = new float[drift.Length];
		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var wind = ((float)random.NextDouble() - 0.5f) * 1.4f;
			var cos = MathF.Cos( wind ); var sin = MathF.Sin( wind );
			for ( var y = 0; y < cell; y++ )
			{
				var row = ((int)min.y + y) * FloorSize + (int)min.x;
				for ( var x = 0; x < cell; x++ )
				{
					var along = x * cos + y * sin;
					var across = y * cos - x * sin;
					ridges[row + x] = Streak( scour, FloorSize, (int)across + 512, along / 7f ) - 0.5f;
				}
			}
		}

		painter.AddHeight( drift, 2.2f );
		painter.Blend( hollows, SnowCool );
		painter.Blend( crests, SnowLight );

		// Snow grain: a fine cool speckle of crystals in shadow, visible from standing height.
		painter.Blend( Ramp( painter.Noise( 128, Seed + 16, 2 ), 0.52f, 0.75f, 0.35f ), SnowCool );
		painter.Blend( ice, IceSheen, roughness: 0.2f );
		painter.AddHeight( ice, -0.5f );
		return ridges;
	}

	/// <summary>
	/// Knotty pine boards, four to six per tile underside, each its own width, tone and length.
	/// Grain is a fine noise field read stretched along the board and nudged by a slow wave, so the
	/// late wood runs in thin, long, slightly wavy lines over a soft broad figure; knots sit in a halo
	/// of darker grain.
	/// </summary>
	protected override PaintedSurface PaintCeiling()
	{
		const int cell = CeilingSize / 2;
		var painter = new Painter( CeilingSize, BoardGap, 0.9f );
		var random = new Random( Seed + 500 );
		float Next() => (float)random.NextDouble();
		var grain = painter.Noise( 256, Seed + 501, 1 );
		var figure = painter.Noise( 32, Seed + 502, 2 );
		var lateWood = new float[CeilingSize * CeilingSize];
		var earlyWood = new float[CeilingSize * CeilingSize];

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );
			var y = min.y;
			var end = min.y + cell;

			while ( y < end - 1f )
			{
				var width = 36f + Next() * 26f;
				if ( end - (y + width) < 30f ) width = end - y;

				// Some boards are two lengths butted together.
				var split = Next() < 0.4f ? min.x + 50f + Next() * 156f : float.NaN;
				var pieces = float.IsNaN( split ) ? 1 : 2;
				for ( var piece = 0; piece < pieces; piece++ )
				{
					var x0 = piece == 0 ? min.x : split + 1f;
					var x1 = pieces == 1 || piece == 1 ? min.x + cell : split - 1f;
					var tone = Next() * 2f - 1f;
					var wood = tone < 0f ? Mix( Pine, PineDeep, -tone * 0.7f ) : Mix( Pine, PinePale, tone * 0.7f );
					painter.Rect( x0 + 0.5f, y + 0.8f, x1 - x0 - 1f, width - 1.6f, wood, radius: 1f, height: 1f, bevel: 3f, roughness: 0.6f, angle: (Next() - 0.5f) * 0.3f );

					var row = random.Next( CeilingSize );
					var shift = Next() * CeilingSize;
					var phase = Next() * MathF.Tau;
					var strength = 0.4f + Next() * 0.35f;
					for ( var py = (int)y + 2; py < (int)(y + width) - 2; py++ )
					{
						var offset = py * CeilingSize;
						for ( var px = (int)x0 + 1; px < (int)x1 - 1; px++ )
						{
							var wave = MathF.Sin( px * 0.035f + phase ) * 3f;
							// Thin late-wood lines run nearly the board's length; a broad, soft figure
							// under them shifts the tone, so neither reads as stripes on its own.
							var s = Streak( grain, CeilingSize, row + py + (int)wave, (px + shift) / 60f );
							var b = Streak( figure, CeilingSize, row + py, (px + shift) / 40f );
							lateWood[offset + px] = (s > 0.62f ? MathF.Min( (s - 0.62f) * 10f, 1f ) * strength : 0f) + (b > 0.5f ? (b - 0.5f) * 0.6f : 0f);
							earlyWood[offset + px] = b < 0.45f ? MathF.Min( (0.45f - b) * 3f, 1f ) * 0.2f : 0f;
						}
					}

					if ( Next() < 0.45f )
					{
						var kx = x0 + 14f + Next() * (x1 - x0 - 28f);
						var ky = y + width * (0.3f + Next() * 0.4f);
						var kr = 2.2f + Next() * 2.4f;
						var knotSeed = random.Next();
						painter.Polygon( Organic( new Vector2( kx, ky ), new Vector2( kr * 4f, kr * 2.2f ), 0.4f, knotSeed, 0f, 24 ), LateWood, alpha: 0.3f );
						painter.Polygon( Organic( new Vector2( kx, ky ), new Vector2( kr * 1.4f, kr ), 0.4f, knotSeed + 1, 0f, 20 ), Knot, alpha: 0.9f, height: 0.95f, bevel: kr, roughness: 0.5f );
					}
				}

				y += width;
			}
		}

		painter.Blend( lateWood, LateWood );
		painter.Blend( earlyWood, PinePale );
		painter.AddHeight( lateWood, -0.08f );

		painter.Mottle( 0.06f, 3, Seed + 510, 5 );
		painter.RoughnessNoise( 0.06f, 6, Seed + 511, 4 );
		painter.Cavity( 0.45f, 4 );
		painter.EdgeWear( 0.25f, 3, Seed + 512 );
		painter.Grain( 0.015f, Seed + 513 );
		return painter.Bake( "arena_1_ceiling", Painter.Matte, 4f );
	}
	/// <summary>
	/// Builds the winter room's props into the floor's kit, in wall space (see <see cref="WallProps"/>).
	/// Every snowman, wreath, drift and icicle run is grown from its wall's own stream.
	/// </summary>
	sealed class Props : WallProps
	{
		/// <summary>Where a lantern's lamp burns (inside its glass) and how far out from the post it hangs.</summary>
		public const float LanternLight = 169f;
		public const float LanternDepth = -11f;

		/// <summary>The height the bulb string is hooked to the top plate at.</summary>
		public const float StringV = 245f;

		/// <summary>The underside of the top plate, which the icicles hang from.</summary>
		const float Plate = FloorContext.ClearHeight - 28f;

		static Vector3 ToRoom => new( 0f, -1f, 0f );

		readonly Material _trim;
		readonly Material _glow;

		public Props( FloorContext room, Material trim, Material glow ) : base( room )
		{
			_trim = trim;
			_glow = glow;
		}

		/// <summary>A region of the trim atlas, as a skin for a prop.</summary>
		Skin TrimSkin( Rect region ) => new( _trim, region );

		/// <summary>
		/// Icicles along the whole top plate, in runs: a run grows its longest icicles in its middle and
		/// its shortest at its ends, as melt dripping off a beam does; gaps of bare timber between runs.
		/// </summary>
		public void Icicles()
		{
			const float Span = FloorContext.Span;
			for ( var start = Range( 0f, 30f ); start < Span - 10f; )
			{
				var end = MathF.Min( start + Range( 20f, 110f ), Span - 4f );
				var peak = Range( 0.35f, 1f );
				for ( var u = start; u < end; u += Range( 2.8f, 6.5f ) )
				{
					var envelope = MathF.Sin( (u - start) / (end - start) * MathF.PI );
					var length = 2.5f + envelope * peak * Range( 6f, 30f ) * (Next() < 0.25f ? 1f : 0.45f);
					Icicle( u, Range( 1.2f, 4.5f ), length );
				}
				start = end + Range( 10f, 80f );
			}
		}

		/// <summary>
		/// One icicle: thickest at the root, tapering to a point, glossy, a little out of plumb. Seen from
		/// the floor it is a few pixels wide, so it is built only as round as that shows: a stub is a plain
		/// four-sided cone, a longer one bends once in its taper, a long one twice. Its root ends flush with
		/// the plate (a flare there read as a spike above it).
		/// </summary>
		void Icicle( float u, float depth, float length )
		{
			var r = MathF.Min( 0.45f + length * 0.07f, 2.4f ) * Range( 0.8f, 1.2f );
			var profile = length > 14f
				? new[] { new Vector2( 0f, 0f ), new Vector2( r * 0.3f, length * 0.35f ), new Vector2( r * 0.72f, length * 0.75f ), new Vector2( r, length ) }
				: length > 7f
					? new[] { new Vector2( 0f, 0f ), new Vector2( r * 0.55f, length * 0.6f ), new Vector2( r, length ) }
					: new[] { new Vector2( 0f, 0f ), new Vector2( r, length ) };
			Kit.SmoothLathe( TrimSkin( IceSwatch ), At( new Vector3( u, depth, Plate - length ) ), profile, length > 7f ? 5 : 4,
				Turn( Rotation.From( Range( -3f, 3f ), Next() * 360f, Range( -3f, 3f ) ) ), false, false, 60f, Next() );
		}

		/// <summary>
		/// A string of warm bulbs swagged along the top plate: hooked every metre or three, sagging between,
		/// a bulb on a short socket every foot or so, most warm white, a few amber or peach.
		/// </summary>
		public void BulbString()
		{
			const float Span = FloorContext.Span;
			const int Points = 7;
			var anchors = new List<float>();
			for ( var u = Range( 20f, 60f ); u < Span - 20f; u += Range( 75f, 135f ) ) anchors.Add( u );

			for ( var a = 0; a < anchors.Count; a++ )
				Kit.Tube( TrimSkin( IronSwatch ), At( new[] { new Vector3( anchors[a], 0.5f, StringV + 1f ), new Vector3( anchors[a], -1.2f, StringV ) } ), 0.28f, 3 );

			for ( var a = 0; a + 1 < anchors.Count; a++ )
			{
				var u0 = anchors[a];
				var u1 = anchors[a + 1];
				var sag = Range( 7f, 15f );
				var belly = Range( 1.5f, 4f );
				var wire = new Vector3[Points];
				for ( var i = 0; i < Points; i++ )
				{
					var t = i / (Points - 1f);
					var bow = 4f * t * (1f - t);
					wire[i] = new Vector3( u0 + (u1 - u0) * t, -1.2f - belly * bow, StringV - sag * bow );
				}
				Kit.Tube( TrimSkin( WireSwatch ), At( wire ), 0.2f, 3 );

				var span = u1 - u0;
				for ( var d = Range( 3f, 7f ); d < span - 2f; d += Range( 12f, 16f ) )
				{
					var at = Along( wire, d / span );
					Kit.Tube( TrimSkin( WireSwatch ), At( new[] { at + Vector3.Up * 0.2f, at - Vector3.Up * 1.7f } ), 0.5f, 3 );
					var tint = Next() < 0.7f ? 0 : 1 + Pick( 2 );
					// Two hundred units over the floor a bulb is a few pixels across: a six-sided ellipsoid is round there.
					Kit.Ellipsoid( new Skin( _glow, BulbGlow( tint ) ), At( at - Vector3.Up * 3.5f ), new Vector3( 1.3f, 1.3f, 1.8f ), Run, 3, 6 );
				}
			}
		}

		/// <summary>Snow banked on a window's sill in two to four soft heaps.</summary>
		public void SillSnow( float u, float width )
		{
			var heaps = 2 + Pick( 3 );
			for ( var k = 0; k < heaps; k++ )
			{
				var r = new Vector3( Range( 7f, 14f ), Range( 5f, 6.5f ), Range( 2.6f, 4.8f ) );
				var x = u + Range( -width * 0.5f + r.x * 0.6f, width * 0.5f - r.x * 0.6f );
				Kit.Ellipsoid( TrimSkin( SnowSwatch ), At( new Vector3( x, 35f, WindowSill - 0.5f ) ), r, Turn( Rotation.FromYaw( Range( -6f, 6f ) ) ), 4, 8 );
			}
		}

		/// <summary>
		/// A drift banked against the foot of the wall: a few overlapping heaps, high against the wall, sloping
		/// out. Each is a dome rising off the ledge (its underside would be in the slab), so the faces a full
		/// ellipsoid spent there go on a rounder outline instead — soft snow, not a faceted rock.
		/// </summary>
		public void Drift( Bay bay )
		{
			var u = bay.From + Range( 4f, bay.Width * 0.4f );
			for ( var k = 2 + Pick( 4 ); k > 0 && u < bay.To - 6f; k-- )
			{
				var r = new Vector3( Range( 12f, 30f ), Range( 7f, 12f ), Range( 6f, 16f ) );
				var centre = new Vector3( u, MathF.Max( Face - r.y * 0.45f, Front + r.y ), Ledge - 0.5f );
				Kit.Dome( TrimSkin( SnowSwatch ), At( centre ), r.WithZ( r.z * 1.15f + 0.5f ), Turn( Rotation.FromYaw( Range( -10f, 10f ) ) ), 3, 12 );
				u += r.x * Range( 0.7f, 1.3f );
			}
		}

		/// <summary>A point on the front of an egg-shaped ball, <paramref name="across"/> to the side and <paramref name="up"/>, as directions off straight out.</summary>
		static Vector3 OnBall( Vector3 centre, Vector3 radii, float across, float up )
		{
			var direction = new Vector3( across, -1f, up ).Normal;
			return centre + new Vector3( direction.x * radii.x, direction.y * radii.y, direction.z * radii.z );
		}

		/// <summary>
		/// A snowman in relief at <paramref name="u"/>, half sunk into the wall: three balls of packed snow, each its
		/// own size, leaning a little; coal eyes, mouth and buttons, a carrot nose, twig arms with a fork,
		/// often a knitted scarf, a beanie or a felt hat. Every one looks a different way.
		/// </summary>
		public void Snowman( float u )
		{
			var rb = Range( 21f, 26f );
			var rm = rb * Range( 0.66f, 0.74f );
			var rh = rm * Range( 0.64f, 0.72f );
			var lean = Range( -2.5f, 2.5f );
			var look = Range( -0.25f, 0.25f );
			var snow = TrimSkin( SnowSwatch );
			var coal = TrimSkin( CoalSwatch );
			Vector3 Ball( float v, float r ) => new( u + lean * (v / 60f), Face - r * 0.4f, v );

			var baseR = new Vector3( rb * Range( 1f, 1.08f ), rb * 0.92f, rb * 0.88f );
			var midR = new Vector3( rm, rm * 0.92f, rm * 0.9f );
			var headR = new Vector3( rh, rh * 0.95f, rh * 0.95f );
			var bottom = Ball( Ledge + rb * 0.78f, rb );
			var middle = Ball( bottom.z + rb * 0.75f + rm * 0.7f, rm );
			var head = Ball( middle.z + rm * 0.78f + rh * 0.72f, rh );
			Kit.Ellipsoid( snow, At( bottom ), baseR, Run, 6, 12 );
			Kit.Ellipsoid( snow, At( middle ), midR, Run, 6, 11 );
			Kit.Ellipsoid( snow, At( head ), headR, Run, 5, 10 );

			var size = rh / 8f;
			for ( var s = -1; s <= 1; s += 2 )
				Kit.Ellipsoid( coal, At( OnBall( head, headR, look + s * 0.32f, 0.28f ) ), new Vector3( 0.9f, 0.6f, 0.9f ) * (size * Range( 0.9f, 1.2f )), Run, 3, 6 );
			for ( var i = -2; i <= 2; i++ )
				Kit.Ellipsoid( coal, At( OnBall( head, headR, look + i * 0.13f, -0.2f - 0.08f * (1f - i * i / 4f) ) ), new Vector3( 0.55f, 0.45f, 0.55f ) * size, Run, 3, 5 );
			for ( var i = 0; i < 3; i++ )
				Kit.Ellipsoid( coal, At( OnBall( middle, midR, look * 0.5f + Range( -0.04f, 0.04f ), 0.38f - i * 0.36f ) ), new Vector3( 0.85f, 0.6f, 0.85f ) * size, Run, 3, 6 );

			var nose = new Vector3( look * 0.8f, -1f, Range( -0.2f, 0f ) ).Normal;
			var length = Range( 7f, 11f ) * size;
			Kit.SmoothLathe( TrimSkin( CarrotSwatch ), At( OnBall( head, headR, look, 0.02f ) - nose * 1.2f ), new[]
			{
				new Vector2( 1.3f * size, 0f ), new Vector2( 1f * size, length * 0.35f ), new Vector2( 0.55f * size, length * 0.7f ), new Vector2( 0f, length ),
			}, 8, Turn( Rotation.FromToRotation( Vector3.Up, nose ) ), true, false );

			// Twig arms, each with a fork.
			for ( var s = -1; s <= 1; s += 2 )
			{
				var shoulder = OnBall( middle, midR, s * 1.6f, 0.25f );
				var angle = Range( -0.25f, 0.8f );
				var reach = new Vector3( s * MathF.Cos( angle ), 0f, MathF.Sin( angle ) );
				var elbow = shoulder + reach * Range( 8f, 12f ) + new Vector3( 0f, 0f, Range( -1.5f, 1.5f ) );
				var hand = elbow + (reach + new Vector3( 0f, 0f, Range( -0.3f, 0.4f ) )).Normal * Range( 8f, 12f );
				var arm = new[] { shoulder - reach * 2f, elbow, hand };
				Kit.Tube( TrimSkin( TwigSwatch ), At( arm ), 0.65f, 5, 0.22f );
				var fork = Along( arm, Range( 0.55f, 0.75f ) );
				Kit.Tube( TrimSkin( TwigSwatch ), At( new[] { fork, fork + (reach + Vector3.Up * Range( 0.6f, 1.1f )).Normal * Range( 4f, 6.5f ) } ), 0.35f, 4, 0.12f );
			}

			// A knitted scarf round the neck, its end hanging over one shoulder.
			var knit = TrimSkin( KnitSwatch( Pick( 2 ) ) );
			if ( Next() < 0.7f )
			{
				// The wrap: a thick soft roll round the front of the neck, a little lower on one side.
				var neck = new Vector3( head.x, head.y, middle.z + midR.z * 0.82f );
				var droop = Range( -1.2f, 1.2f );
				var wrap = new Vector3[11];
				for ( var i = 0; i < wrap.Length; i++ )
				{
					var a = (i / 10f - 0.5f) * 3.8f;
					wrap[i] = neck + new Vector3( MathF.Sin( a ) * rh * 0.98f, -MathF.Cos( a ) * rh * 0.94f, MathF.Sin( a ) * droop );
				}
				Kit.Tube( knit, At( wrap ), 1.7f * rh / 7f, 7 );
				var end = Next() < 0.5f ? -1f : 1f;
				var hang = OnBall( middle, midR, end * 0.45f, 0.62f );
				var scale = rh / 7f;
				Kit.Ellipsoid( knit, At( hang ), new Vector3( 1.7f, 1.2f, 3.6f ) * scale, Turn( Rotation.FromRoll( end * 8f ) ), 3, 6 );
				Kit.Ellipsoid( knit, At( hang + new Vector3( end * 0.6f, -0.4f, -5f ) * scale ), new Vector3( 1.6f, 1.1f, 3.2f ) * scale, Turn( Rotation.FromRoll( end * 14f ) ), 3, 6 );
			}

			var hat = Next();
			if ( hat < 0.45f )
			{
				// A beanie with a turned-up band and a bobble.
				var crown = head + new Vector3( 0f, 0f, rh * 0.5f );
				Kit.Ellipsoid( knit, At( crown ), new Vector3( rh * 0.98f, rh * 0.94f, rh * 0.62f ), Turn( Rotation.FromRoll( Range( -10f, 10f ) ) ), 5, 10 );
				Kit.Ellipsoid( knit, At( head + new Vector3( 0f, 0f, rh * 0.28f ) ), new Vector3( rh * 1.04f, rh * 1.0f, rh * 0.22f ), Run, 3, 10 );
				Kit.Ellipsoid( knit, At( crown + new Vector3( Range( -1f, 1f ), 0f, rh * 0.62f ) ), new Vector3( 2f, 2f, 1.9f ), Run, 4, 7 );
			}
			else if ( hat < 0.75f )
			{
				// An old felt hat, set a little crooked.
				Kit.SmoothLathe( TrimSkin( FeltSwatch ), At( head + new Vector3( 0f, 0f, rh * 0.62f ) ), new[]
				{
					new Vector2( rh * 1.45f, 0f ), new Vector2( rh * 1.45f, 0.5f ), new Vector2( rh * 0.78f, 0.7f ), new Vector2( rh * 0.74f, rh * 1.1f ), new Vector2( rh * 0.7f, rh * 1.15f ),
				}, 12, Turn( Rotation.FromRoll( Range( -12f, 12f ) ) ), true, true, 40f );
			}
		}

		/// <summary>
		/// A fir wreath of radius <paramref name="radius"/> hung on the wall at (<paramref name="u"/>,
		/// <paramref name="v"/>): clumps of three greens round a ring, sprigs breaking its outline, a few
		/// cones, snow caught along the top, and a jute bow with trailing ends.
		/// </summary>
		public void Wreath( float u, float v, float radius )
		{
			var centre = new Vector3( u, Face - 2.2f, v );
			var count = 20 + Pick( 7 );
			var spin = Next() * MathF.Tau;
			var ring = new Vector3[count];
			var bulk = radius / 12f;
			for ( var i = 0; i < count; i++ )
			{
				// Two loose courses, inner and outer, so the ring is a fat irregular torus rather than a string of beads.
				var a = spin + i * MathF.Tau / count + Range( -0.08f, 0.08f );
				var reach = radius + (i % 2 == 0 ? -1.4f : 1.4f) * bulk + Range( -0.8f, 0.8f );
				ring[i] = centre + new Vector3( MathF.Cos( a ) * reach, Range( -0.8f, 0.4f ), MathF.Sin( a ) * reach );
				Kit.Ellipsoid( TrimSkin( FirSwatch( Pick( 3 ) ) ), At( ring[i] ), new Vector3( Range( 4.6f, 5.8f ), Range( 2.4f, 3f ), Range( 3.4f, 4.3f ) ) * bulk,
					Turn( Rotation.FromAxis( new Vector3( 0f, 1f, 0f ), a * 57.3f + Range( -25f, 25f ) ) ), 3, 6 );
			}

			for ( var s = 0; s < 11; s++ )
			{
				var a = Next() * MathF.Tau;
				var root = centre + new Vector3( MathF.Cos( a ), 0f, MathF.Sin( a ) ) * (radius + Range( -2f, 2f ) * bulk) + ToRoom * 1.6f;
				var outward = new Vector3( MathF.Cos( a + Range( -0.9f, 0.9f ) ), 0f, MathF.Sin( a + Range( -0.9f, 0.9f ) ) );
				Kit.Blade( TrimSkin( FirSwatch( Pick( 3 ) ) ), At( root ), Turn( Aim( outward, 0f, ToRoom ) ), Range( 4f, 7f ) * bulk, Range( 1.3f, 1.9f ) * bulk, 0.2f, 0.1f, 2, 0.1f, 0.4f );
			}

			for ( var c = 3 + Pick( 3 ); c > 0; c-- )
			{
				var at = ring[Pick( count )] + ToRoom * 2.2f;
				Kit.Ellipsoid( TrimSkin( ConeSwatch ), At( at ), new Vector3( 1.1f, 1f, 1.6f ), Turn( Rotation.From( Next() * 360f, Next() * 360f, 0f ) ), 3, 5 );
			}

			for ( var k = 4 + Pick( 4 ); k > 0; k-- )
			{
				var a = Range( 0.35f, 2.8f );
				var at = centre + new Vector3( MathF.Cos( a ) * radius, -1.6f, MathF.Sin( a ) * radius + 2f );
				Kit.Ellipsoid( TrimSkin( SnowSwatch ), At( at ), new Vector3( Range( 1.6f, 2.8f ), 1.4f, Range( 0.7f, 1.1f ) ), Run, 3, 7 );
			}

			// The bow, most often at the foot.
			var top = Next() < 0.3f ? 1f : -1f;
			var bow = centre + new Vector3( Range( -1.5f, 1.5f ), -2f, top * radius );
			var jute = TrimSkin( JuteSwatch );
			for ( var s = -1; s <= 1; s += 2 )
				Kit.Ellipsoid( jute, At( bow + new Vector3( s * 2.8f, 0f, 0.8f ) ), new Vector3( 3.2f, 1f, 2f ), Turn( Rotation.FromAxis( new Vector3( 0f, 1f, 0f ), s * Range( 20f, 35f ) ) ), 3, 7 );
			Kit.Ellipsoid( jute, At( bow + ToRoom * 0.5f ), new Vector3( 1.3f, 1.1f, 1.3f ), Run, 3, 6 );
			for ( var s = -1; s <= 1; s += 2 )
				Kit.Blade( jute, At( bow ), Turn( Aim( new Vector3( s * Range( 0.3f, 0.6f ), 0f, -1f ), 0f, ToRoom ) ), Range( 8f, 11f ), 2.2f, 0.15f, 0f, 2, 0.9f, 0.5f );
		}

		/// <summary>A pair of wooden skis crossed on the wall, their tips curling out, with the poles crossed behind them.</summary>
		public void Skis( Bay bay )
		{
			var centre = new Vector3( bay.Middle + Range( -8f, 8f ), Face, Range( 112f, 128f ) );
			var lean = Range( 18f, 26f ) * MathF.PI / 180f;
			var skin = TrimSkin( SkiSwatch( Pick( 2 ) ) );

			if ( Next() < 0.8f )
			{
				for ( var s = -1; s <= 1; s += 2 )
				{
					var a = s * Range( 10f, 16f ) * MathF.PI / 180f;
					var along = new Vector3( MathF.Sin( a ), 0f, MathF.Cos( a ) );
					var foot = centre + new Vector3( 0f, -0.6f, 0f ) - along * 39f;
					var head = foot + along * 78f;
					Kit.Tube( TrimSkin( BambooSwatch ), At( new[] { foot, head } ), 0.6f, 5 );
					Kit.Tube( TrimSkin( LeatherSwatch ), At( new[] { head - along * 9f, head } ), 0.9f, 6 );
					Kit.SmoothLathe( TrimSkin( LeatherSwatch ), At( foot + along * 6f ), new[] { new Vector2( 0.6f, 0f ), new Vector2( 3.4f, 0.25f ), new Vector2( 3.4f, 0.9f ), new Vector2( 0.6f, 0.75f ) },
						10, Turn( Rotation.FromToRotation( Vector3.Up, along ) ), false, false );
				}
			}

			for ( var s = -1; s <= 1; s += 2 )
			{
				var along = new Vector3( MathF.Sin( lean * s ), 0f, MathF.Cos( lean * s ) );
				Ski( centre.WithY( Face - (s < 0 ? 1.4f : 3f) ), along, Range( 90f, 100f ), skin );
			}
		}

		/// <summary>One ski lying flat on the wall along <paramref name="along"/>, its shovel curling out toward the room.</summary>
		void Ski( Vector3 centre, Vector3 along, float length, Skin skin )
		{
			var body = length * 0.84f;
			var tail = centre - along * length * 0.5f;
			var flat = Turn( Rotation.LookAt( along, ToRoom ) );
			Kit.BevelBox( skin, At( tail + along * body * 0.5f ), new Vector3( body, 3.9f, 1.1f ), 0.3f, flat );

			var at = tail + along * body;
			var heading = along;
			for ( var k = 0; k < 3; k++ )
			{
				var piece = length * 0.055f;
				heading = (heading + ToRoom * 0.32f).Normal;
				var up = (ToRoom - heading * Vector3.Dot( ToRoom, heading )).Normal;
				Kit.BevelBox( skin, At( at + heading * piece * 0.5f ), new Vector3( piece + 0.4f, 3.9f - k * 0.3f, 1.1f ), 0.3f, Turn( Rotation.LookAt( heading, up ) ) );
				at += heading * piece;
			}

			Kit.BevelBox( TrimSkin( IronSwatch ), At( tail + along * length * 0.42f + ToRoom * 0.9f ), new Vector3( 5f, 3.4f, 1.2f ), 0.3f, flat );
			Kit.BevelBox( TrimSkin( LeatherSwatch ), At( tail + along * length * 0.47f + ToRoom * 1.2f ), new Vector3( 2.2f, 3.6f, 1.6f ), 0.3f, flat );
		}

		/// <summary>
		/// A stack of firewood <paramref name="width"/> wide from <paramref name="u0"/>, about
		/// <paramref name="height"/> high: rounds of every girth, their sawn ends to the room and each poking
		/// out its own way, the rows staggered and stepping in toward the top, now and then snow on top.
		/// Only the logs whose bark shows — the top row and the two ends of each row — are modelled whole;
		/// the rest are their sawn ends on a bark-faced block, which is all a packed stack shows between them.
		/// </summary>
		public void Firewood( float u0, float width, float height )
		{
			var logs = new List<(Vector3 Back, float R, float Length, int Row, bool End)>();
			var insets = new List<float>();
			var v = Ledge;
			for ( var row = 0; v < Ledge + height; row++ )
			{
				var inset = (insets.Count > 0 ? insets[^1] : 0f) + Range( 0f, 3.5f );
				var tallest = 0f;
				var first = logs.Count;
				for ( var u = u0 + inset + (row % 2) * 2.5f; ; )
				{
					var r = Range( 4.5f, 7f );
					if ( u + 2f * r > u0 + width - inset ) break;
					logs.Add( (new Vector3( u + r, Face - 0.5f, v + r ), r, Face - 0.5f - Range( 3f, 9f ), row, logs.Count == first) );
					u += 2f * r + Range( 0.2f, 1.2f );
					tallest = MathF.Max( tallest, 2f * r );
				}
				if ( tallest == 0f ) break;
				logs[^1] = logs[^1] with { End = true };
				insets.Add( inset );
				v += tallest * Range( 0.82f, 0.92f );
			}
			if ( logs.Count == 0 ) return;

			var top = logs[^1].Row;
			var bark = TrimSkin( BarkSwatch );
			foreach ( var log in logs )
				Log( log.Back, log.R, log.Length, log.End || log.Row == top );

			// The block behind the sawn ends, up to the middle of the row under the top one and as narrow as that row.
			if ( top >= 1 )
			{
				var under = logs.FindAll( l => l.Row == top - 1 );
				var left = under[0].Back.x - under[0].R * 0.5f;
				var right = under[^1].Back.x + under[^1].R * 0.5f;
				var high = under[0].Back.z;
				Kit.Box( bark, At( new Vector3( (left + right) * 0.5f, (Face + 12f) * 0.5f, (Ledge + high) * 0.5f ) ), new Vector3( right - left, Face - 12f, high - Ledge ), Run );
			}

			if ( Next() < 0.55f )
			{
				var crown = logs.FindAll( l => l.Row == top );
				var middle = (crown[0].Back.x + crown[^1].Back.x) * 0.5f;
				var spread = (crown[^1].Back.x - crown[0].Back.x) * 0.5f + crown[0].R;
				Kit.Ellipsoid( TrimSkin( SnowSwatch ), At( new Vector3( middle, Face - 16f, crown[0].Back.z + crown[0].R * 0.8f ) ), new Vector3( spread * Range( 0.75f, 0.95f ), 13f, Range( 2.2f, 3.6f ) ),
					Turn( Rotation.FromYaw( Range( -4f, 4f ) ) ), 3, 8 );
			}
		}

		/// <summary>
		/// A log lying along the depth from <paramref name="back"/>, <paramref name="length"/> long, the sawn end
		/// with its rings facing the room; with its bark round it where that shows (<paramref name="whole"/>).
		/// </summary>
		void Log( Vector3 back, float r, float length, bool whole )
		{
			var axis = Turn( Rotation.FromToRotation( Vector3.Up, new Vector3( Range( -0.04f, 0.04f ), -1f, Range( -0.04f, 0.04f ) ).Normal ) );
			var turn = Next();
			if ( whole )
				Kit.SmoothLathe( TrimSkin( BarkSwatch ), At( back ), new[] { new Vector2( r, 0f ), new Vector2( r * Range( 0.95f, 1.02f ), length ) },
					8, axis, false, false, 50f, turn );
			// The sawn ends face the room, so they get the sides: a ring of ten reads round from the floor.
			Kit.SmoothLathe( TrimSkin( LogEndSwatch( Pick( 2 ) ) ), At( back ), new[] { new Vector2( r, length ), new Vector2( 0f, length + 0.25f ) }, 10, axis, false, false, 20f, turn );
		}

		/// <summary>
		/// An iron lantern hung from a bracket on the post at <paramref name="u"/> of wall <paramref name="side"/>:
		/// a wall plate, a scrolled arm and brace, a ring, a four-sided roof, corner posts and a base, and
		/// glass lit warm from inside (its lamp burns there: <see cref="LanternLight"/>).
		/// </summary>
		public void Lantern( int side, float u )
		{
			OnSide( side );
			const float Top = 184f;
			const float Half = 3.8f;
			const float D = LanternDepth;
			var iron = TrimSkin( IronSwatch );

			Kit.BevelBox( iron, At( new Vector3( u, -0.6f, Top + 3f ) ), new Vector3( 4f, 1.2f, 12f ), 0.3f, Run );
			Kit.Tube( iron, At( new[] { new Vector3( u, -1f, Top + 7f ), new Vector3( u, D * 0.5f, Top + 7.6f ), new Vector3( u, D, Top + 7f ), new Vector3( u, D, Top + 3.6f ) } ), 0.45f, 5 );
			Kit.Tube( iron, At( new[] { new Vector3( u, -1f, Top - 2f ), new Vector3( u, D * 0.3f, Top + 2f ), new Vector3( u, D * 0.55f, Top + 6.8f ) } ), 0.35f, 4 );

			var loop = new Vector3[9];
			for ( var i = 0; i < loop.Length; i++ )
			{
				var a = i / 8f * MathF.Tau;
				loop[i] = new Vector3( u + MathF.Cos( a ) * 1.3f, D, Top + 2.2f + MathF.Sin( a ) * 1.3f );
			}
			Kit.Tube( iron, At( loop ), 0.3f, 4 );

			Kit.Lathe( iron, At( new Vector3( u, D, Top - 4.5f ) ), new[] { new Vector2( 6.2f, 0f ), new Vector2( 6.2f, 0.8f ), new Vector2( 0.6f, 4.2f ), new Vector2( 0f, 4.4f ) }, 4,
				Turn( Rotation.FromYaw( 45f ) ) );
			for ( var cx = -1; cx <= 1; cx += 2 )
			for ( var cy = -1; cy <= 1; cy += 2 )
				Kit.BevelBox( iron, At( new Vector3( u + cx * Half, D + cy * Half, Top - 9.75f ) ), new Vector3( 0.8f, 0.8f, 10.5f ), 0.15f, Run );
			Kit.BevelBox( iron, At( new Vector3( u, D, Top - 15.6f ) ), new Vector3( 8.8f, 8.8f, 1.4f ), 0.3f, Run );
			Kit.Lathe( iron, At( new Vector3( u, D, Top - 18f ) ), new[] { new Vector2( 0f, 0f ), new Vector2( 1.4f, 1.2f ), new Vector2( 2.6f, 2.3f ) }, 8, Run, false, false );
			Kit.BevelBox( new Skin( _glow, LanternGlow ), At( new Vector3( u, D, Top - 9.8f ) ), new Vector3( 7.2f, 7.2f, 9.6f ), 0.3f, Run );
		}

		// ---- Bays ------------------------------------------------------------------------------
		// A storey is 268 high and a bay about 180 wide, so a single prop at the foot of one left the logs
		// above it bare: each bay is dressed as a set — something on the floor and something hung on the
		// wall over it — and the plain bays of a wall deal out four different sets in turn. What a player
		// would use (benches, boots, coats on their pegs, the sledge) is at a player's size; only the
		// pure wall decor (woodpiles, shelves, skis, snowshoes, wreaths) is built large, to hold the logs.

		/// <summary>The set-ups a plain bay is dressed as, after the snowman's and the skis'.</summary>
		public enum Look { Woodpile, Mudroom, Shelves, Sledge }

		/// <summary>The cabin's hewn pine (the trim's stock band): benches, shelves, peg rails, the sledge.</summary>
		Skin Timber => TrimSkin( TrimRegion( TrimBand.Stock ) );

		/// <summary>
		/// A window bay: snow on the sill, now and then a wreath over the window, under it a bench, a low
		/// woodpile or only snow, and in the strips of wall beside the window, now and then a coat or a scarf on a peg.
		/// </summary>
		public void WindowBay( Bay bay, float width )
		{
			SillSnow( bay.Middle, width );
			var beside = (bay.Width - width) * 0.5f;
			for ( var s = -1; s <= 1 && beside > 38f; s += 2 )
				if ( Next() < 0.45f ) Peg( bay.Middle + s * (width * 0.5f + beside * Range( 0.45f, 0.55f )), Range( 104f, 118f ) );
			if ( Next() < 0.3f ) Wreath( bay.Middle + Range( -5f, 5f ), WindowSill + WindowHeight + 28f, Range( 10f, 13f ) );
			var under = Next();
			var room = MathF.Min( width + Range( -6f, 20f ), bay.Width - 16f );
			if ( under < 0.4f ) Bench( bay.Middle + Range( -8f, 8f ), room, Next() < 0.5f );
			else if ( under < 0.65f ) Firewood( bay.Middle - room * 0.5f, room, Range( 26f, 40f ) );
			if ( Next() < 0.65f ) Drift( bay );
		}

		/// <summary>The snowman's bay: the snowman to one side, a child's sledge hung on the wall at the other, and a drift.</summary>
		public void SnowmanBay( Bay bay )
		{
			var side = Next() < 0.5f ? -1f : 1f;
			Snowman( bay.Middle + side * bay.Width * Range( 0.12f, 0.22f ) );
			if ( bay.Width > 140f && Next() < 0.7f ) Sledge( bay.Middle - side * bay.Width * Range( 0.2f, 0.26f ), Range( 46f, 56f ), side );
			Drift( bay );
		}

		/// <summary>The skis' bay: the crossed skis, a bench under them or a drift.</summary>
		public void SkiBay( Bay bay )
		{
			Skis( bay );
			if ( Next() < 0.6f ) Bench( bay.Middle + Range( -10f, 10f ), MathF.Min( bay.Width - 20f, Range( 80f, 120f ) ), true );
			if ( Next() < 0.7f ) Drift( bay );
		}

		/// <summary>One of the four set-ups in <paramref name="bay"/>, most with snow drifted at the foot.</summary>
		public void Furnish( Look look, Bay bay )
		{
			var side = Next() < 0.5f ? -1f : 1f;
			switch ( look )
			{
				case Look.Woodpile:
				{
					// A big stack to one side; snowshoes hung beside it where there is room, or a wreath over it.
					var width = MathF.Min( bay.Width - 18f, Range( 96f, 140f ) );
					var u0 = side < 0f ? bay.From + Range( 6f, 12f ) : bay.To - Range( 6f, 12f ) - width;
					var height = Range( 84f, 124f );
					Firewood( u0, width, height );
					var free = bay.Width - width - 18f;
					if ( free > 50f ) Snowshoes( side < 0f ? bay.To - free * 0.5f - 6f : bay.From + free * 0.5f + 6f, Range( 140f, 176f ) );
					var crown = MathF.Min( Ledge + height + Range( 56f, 80f ), 205f );
					if ( free <= 50f || Next() < 0.5f ) Wreath( u0 + width * Range( 0.35f, 0.65f ), crown, Range( 17f, 25f ) );
					break;
				}
				case Look.Mudroom:
				{
					// A bench with a peg rail over it: coats, scarves and a hat on the pegs, boots under the bench.
					var length = MathF.Min( bay.Width - 20f, Range( 100f, 150f ) );
					var u = bay.Middle + side * Range( 0f, (bay.Width - 20f - length) * 0.5f );
					Bench( u, length, true );
					CoatPegs( u + Range( -8f, 8f ), MathF.Min( length + Range( -10f, 16f ), bay.Width - 24f ), Range( 102f, 114f ) );
					break;
				}
				case Look.Shelves:
				{
					// One or two shelves of stoneware, a low stack of kindling under them.
					var length = MathF.Min( bay.Width - 24f, Range( 80f, 130f ) );
					var u = bay.Middle + Range( -1f, 1f ) * (bay.Width - 24f - length) * 0.5f;
					var low = Range( 112f, 128f );
					Shelf( u, length, low );
					if ( Next() < 0.7f ) Shelf( u + Range( -10f, 10f ), length * Range( 0.7f, 1f ), low + Range( 50f, 62f ) );
					var stack = MathF.Min( length + Range( -10f, 20f ), bay.Width - 20f );
					if ( Next() < 0.7f ) Firewood( bay.Middle - stack * 0.5f + Range( -8f, 8f ), stack, Range( 34f, 56f ) );
					break;
				}
				default:
				{
					// A sledge hung on the wall, snowshoes hung beside it.
					var u = bay.Middle + side * bay.Width * Range( 0.14f, 0.22f );
					Sledge( u, Range( 46f, 56f ), -side );
					Snowshoes( bay.Middle - side * bay.Width * Range( 0.18f, 0.26f ), Range( 140f, 176f ) );
					break;
				}
			}
			if ( Next() < 0.75f ) Drift( bay );
		}

		// ---- Furniture and gear ----------------------------------------------------------------

		/// <summary>
		/// A plank bench <paramref name="length"/> long centred on <paramref name="u"/>: a thick seat on two
		/// slab ends and a stretcher; on it a folded blanket, a jug or a couple of mugs; under it, sometimes,
		/// a pair of boots (<paramref name="boots"/>).
		/// </summary>
		void Bench( float u, float length, bool boots )
		{
			var seat = Range( 20f, 24f );
			var depth = Range( 15f, 18f );
			var front = Face - 0.5f - depth;
			var middle = front + depth * 0.5f;
			Kit.BevelBox( Timber, At( new Vector3( u, middle, seat - 1.8f ) ), new Vector3( length, depth, 3.6f ), 0.6f, Turn( Rotation.FromYaw( Range( -0.6f, 0.6f ) ) ) );
			var legs = length * 0.5f - Range( 7f, 13f );
			for ( var s = -1; s <= 1; s += 2 )
				Kit.Box( Timber, At( new Vector3( u + s * legs, middle, (Ledge + seat - 3.6f) * 0.5f ) ), new Vector3( 3.4f, depth - 3f, seat - 3.6f - Ledge ), Run );
			Kit.Box( Timber, At( new Vector3( u, middle + 2.5f, Ledge + (seat - Ledge) * 0.4f ) ), new Vector3( legs * 2f, 2.4f, 3f ), Run );

			// On the seat, left to right, as many things as fit, none of them twice running.
			var previous = -1;
			for ( var at = u - length * 0.5f + Range( 4f, 14f ); at < u + length * 0.5f - 10f; )
			{
				var kind = Pick( 4 );
				if ( kind == previous ) kind = (kind + 1) % 4;
				previous = kind;
				var foot = new Vector3( at, front + Range( 5f, depth - 4f ), seat );
				if ( kind == 0 )
				{
					var w = Range( 24f, 32f );
					var h = 0f;
					for ( var k = 1 + Pick( 2 ); k > 0; k-- )
					{
						var t = Range( 4f, 5.5f );
						Kit.BevelBox( TrimSkin( k == 1 ? PlaidSwatch : KnitSwatch( 1 ) ), At( foot + new Vector3( w * 0.5f + Range( -1f, 1f ), 0f, h + t * 0.5f ) ), new Vector3( w, Range( 12f, 14f ), t ), 1.2f, Turn( Rotation.FromYaw( Range( -8f, 8f ) ) ) );
						h += t;
					}
					at += w + Range( 4f, 10f );
				}
				else if ( kind == 1 ) { var s = Range( 1.2f, 1.5f ); Jug( foot + Vector3.Forward * 4.4f * s, s ); at += 9f * s + Range( 6f, 12f ); }
				else if ( kind == 2 ) { Mug( foot + Vector3.Forward * 2.5f ); Mug( foot + new Vector3( Range( 6f, 7.5f ), Range( -2f, 2f ), 0f ) ); at += Range( 16f, 24f ); }
				else at += Range( 10f, 22f );
			}

			if ( boots && Next() < 0.65f ) Boots( u + Range( -legs + 10f, legs - 10f ), front + 6f );
		}

		/// <summary>A pair of leather boots standing side by side on the ledge, toes to the room, one a little turned.</summary>
		void Boots( float u, float depth )
		{
			var leather = TrimSkin( LeatherSwatch );
			for ( var s = -1; s <= 1; s += 2 )
			{
				var turn = Turn( Rotation.FromYaw( Range( -12f, 12f ) ) );
				var heel = new Vector3( u + s * 4f, depth + 5f, Ledge );
				Kit.Box( leather, At( heel + new Vector3( 0f, 0f, 7.5f ) ), new Vector3( 5.4f, 5.6f, 15f ), turn );
				Kit.Box( leather, At( heel + new Vector3( 0f, -4.2f, 2f ) ), new Vector3( 5.6f, 11f, 4f ), turn );
			}
		}

		/// <summary>
		/// A peg rail <paramref name="length"/> long at height <paramref name="v"/>: turned pegs, most of them
		/// holding something — a wool coat with its hood, a knitted scarf hanging in two tails, a beanie.
		/// </summary>
		void CoatPegs( float u, float length, float v )
		{
			Kit.BevelBox( Timber, At( new Vector3( u, Face - 1.3f, v ) ), new Vector3( length, 2.6f, 6.5f ), 0.5f, Run );
			var pegs = Math.Max( 3, (int)(length / 24f) );
			var pitch = length / pegs;
			var previous = -1;
			for ( var k = 0; k < pegs; k++ )
			{
				var at = u - length * 0.5f + pitch * (k + 0.5f) + Range( -2f, 2f );
				var tip = new Vector3( at, Face - 7.5f, v + 2.5f );
				Kit.Tube( Timber, At( new[] { new Vector3( at, Face - 1.5f, v ), tip } ), 0.95f, 4, 0.75f );

				var kind = Pick( 5 );
				if ( kind == previous && kind < 3 ) kind = 3;
				previous = kind;
				if ( kind == 0 || kind == 1 ) Coat( tip );
				else if ( kind == 2 || kind == 3 ) Scarf( tip );
				else if ( Next() < 0.6f ) Kit.Ellipsoid( TrimSkin( Next() < 0.5f ? MustardKnitSwatch : KnitSwatch( 0 ) ), At( tip + new Vector3( 0f, 1.5f, -4f ) ), new Vector3( 6.4f, 4.6f, 7f ), Run, 4, 8 );
			}
		}

		/// <summary>A single peg on a little block at (<paramref name="u"/>, <paramref name="v"/>), a coat or a scarf on it.</summary>
		void Peg( float u, float v )
		{
			Kit.BevelBox( Timber, At( new Vector3( u, Face - 1f, v ) ), new Vector3( 5f, 2f, 7f ), 0.4f, Run );
			var tip = new Vector3( u, Face - 7.5f, v + 2.5f );
			Kit.Tube( Timber, At( new[] { new Vector3( u, Face - 1.5f, v ), tip } ), 0.95f, 4, 0.75f );
			if ( Next() < 0.5f ) Coat( tip );
			else Scarf( tip );
		}

		/// <summary>
		/// A wool coat hung by its loop from the peg at <paramref name="tip"/>, at a player's size — 40 to 48
		/// long, so its hem clears a bench seat under the rail: a soft slab of a body flat to the wall, rounded
		/// shoulders, the sleeves hanging down its sides and the hood bunched over the peg.
		/// </summary>
		void Coat( Vector3 tip )
		{
			var cloth = TrimSkin( new[] { CoatSwatch, CoatSwatch, KnitSwatch( 1 ), PlaidSwatch, FeltSwatch, MustardKnitSwatch }[Pick( 6 )] );
			var length = Range( 40f, 48f );
			var width = Range( 14f, 17f );
			var depth = Range( 3.4f, 4.4f );
			var sleeve = length * Range( 0.58f, 0.66f );
			// Hung a little askew, turned about the peg in the wall's plane.
			var roll = Rotation.FromAxis( new Vector3( 0f, 1f, 0f ), Range( -4f, 4f ) );
			var collar = new Vector3( tip.x, Face - 1.2f - depth * 0.5f, tip.z - 1f );
			Vector3 Below( float across, float outward, float down ) => collar + roll * new Vector3( across, -outward, -down );

			Kit.BevelBox( cloth, At( Below( 0f, 0f, length * 0.5f + 1.5f ) ), new Vector3( width, depth, length - 3f ), 1.2f, Turn( roll ) );
			Kit.Ellipsoid( cloth, At( Below( 0f, 0.2f, 4f ) ), new Vector3( width * 0.56f, depth * 0.62f, 4.5f ), Turn( roll ), 3, 8 );
			for ( var s = -1; s <= 1; s += 2 )
				Kit.BevelBox( cloth, At( Below( s * (width * 0.5f - 1.4f), depth * 0.5f + 0.9f, 3.5f + sleeve * 0.5f ) ), new Vector3( 3.6f, 2f, sleeve ), 0.8f,
					Turn( roll * Rotation.FromAxis( new Vector3( 0f, 1f, 0f ), s * Range( 2f, 5f ) ) ) );
			Kit.Ellipsoid( cloth, At( Below( Range( -1f, 1f ), depth * 0.5f + 1.2f, 0.5f ) ), new Vector3( 5.2f, 3.4f, 4.6f ), Turn( roll ), 3, 7 );
		}

		/// <summary>A knitted scarf over the peg at <paramref name="tip"/>, its two tails hanging, one longer.</summary>
		void Scarf( Vector3 tip )
		{
			var knit = TrimSkin( new[] { KnitSwatch( 0 ), KnitSwatch( 1 ), MustardKnitSwatch }[Pick( 3 )] );
			var width = Range( 5f, 6.6f );
			Kit.Ellipsoid( knit, At( tip + new Vector3( 0f, 0.8f, 0.6f ) ), new Vector3( width * 0.55f, 2f, 1.7f ), Run, 3, 6 );
			for ( var s = -1; s <= 1; s += 2 )
			{
				var root = tip + new Vector3( s * width * 0.45f, s * 0.6f, -0.5f );
				var down = new Vector3( s * Range( 0.02f, 0.1f ), Range( 0.08f, 0.16f ), -1f );
				Kit.Blade( knit, At( root ), Turn( Aim( down, 0f, ToRoom ) ), (s < 0 ? Range( 32f, 43f ) : Range( 23f, 33f )), width, Range( 0.05f, 0.18f ), 0.15f, 3, 0.9f, 0.5f, true );
			}
		}

		/// <summary>
		/// A shelf <paramref name="length"/> long at <paramref name="v"/> on two iron brackets, set with
		/// stoneware: jugs, mugs in twos and threes, a wide crock — or a stack of folded blankets.
		/// </summary>
		void Shelf( float u, float length, float v )
		{
			var depth = Range( 13f, 16f );
			var front = Face - 0.5f - depth;
			Kit.BevelBox( Timber, At( new Vector3( u, front + depth * 0.5f, v - 1.4f ) ), new Vector3( length, depth, 2.8f ), 0.45f, Turn( Rotation.FromYaw( Range( -0.5f, 0.5f ) ) ) );
			foreach ( var bu in new[] { u - length * Range( 0.28f, 0.36f ), u + length * Range( 0.28f, 0.36f ) } )
			{
				Kit.Box( TrimSkin( IronSwatch ), At( new Vector3( bu, Face - 0.5f, v - 6.5f ) ), new Vector3( 1f, 1f, 11f ), Run );
				Kit.Tube( TrimSkin( IronSwatch ), At( new[] { new Vector3( bu, Face - 0.5f, v - 11f ), new Vector3( bu, front + 2f, v - 2f ) } ), 0.4f, 3 );
			}

			var previous = -1;
			for ( var at = u - length * 0.5f + Range( 3f, 9f ); at < u + length * 0.5f - 7f; )
			{
				var kind = Pick( 4 );
				if ( kind == previous ) kind = (kind + 1) % 4;
				previous = kind;
				var foot = new Vector3( at, front + depth * Range( 0.4f, 0.6f ), v );
				switch ( kind )
				{
					case 0: { var s = Range( 1.1f, 1.6f ); Jug( foot + Vector3.Forward * 3.8f * s, s ); at += 8f * s + Range( 3f, 7f ); break; }
					case 1:
					{
						var count = 2 + Pick( 2 );
						for ( var m = 0; m < count; m++ ) Mug( foot + new Vector3( 2.5f + m * Range( 5f, 6f ), Range( -1.5f, 1.5f ), 0f ) );
						at += count * 5.6f + Range( 3f, 7f );
						break;
					}
					case 2:
					{
						var s = Range( 1.1f, 1.45f );
						Kit.SmoothLathe( TrimSkin( StonewareSwatch ), At( foot + Vector3.Forward * 4.4f * s ), Scale( s, new Vector2( 3.6f, 0f ), new Vector2( 4.3f, 1.2f ), new Vector2( 4.4f, 5.6f ),
							new Vector2( 3.9f, 6.6f ), new Vector2( 3.5f, 6.4f ), new Vector2( 3.4f, 1.6f ) ), 8, Turn( Rotation.FromYaw( Next() * 360f ) ), false, false, 40f, Next() );
						at += 9f * s + Range( 2f, 6f );
						break;
					}
					default:
					{
						var w = Range( 18f, 26f );
						var h = 0f;
						for ( var k = 1 + Pick( 3 ); k > 0; k-- )
						{
							var t = Range( 2.4f, 3.4f );
							Kit.Box( TrimSkin( k % 2 == 0 ? PlaidSwatch : KnitSwatch( 1 ) ), At( foot + new Vector3( w * 0.5f + Range( -1f, 1f ), 0f, h + t * 0.5f ) ), new Vector3( w, depth - 2.5f, t ), Turn( Rotation.FromYaw( Range( -4f, 4f ) ) ) );
							h += t;
						}
						at += w + Range( 3f, 7f );
						break;
					}
				}
			}
		}

		static Vector2[] Scale( float s, params Vector2[] profile ) => Array.ConvertAll( profile, p => p * s );

		/// <summary>A salt-glazed stoneware jug on <paramref name="foot"/>: bellied, a narrow neck with a lip, a loop handle.</summary>
		void Jug( Vector3 foot, float s )
		{
			var turn = Next() < 0.5f ? 1f : -1f;
			Kit.SmoothLathe( TrimSkin( StonewareSwatch ), At( foot ), Scale( s, new Vector2( 2.6f, 0f ), new Vector2( 3.5f, 1.8f ), new Vector2( 3.7f, 4.8f ), new Vector2( 2.8f, 8f ),
				new Vector2( 1.3f, 9.6f ), new Vector2( 1.2f, 11f ), new Vector2( 1.55f, 11.6f ) ), 8, Run, false, true, 40f, Next() );
			Kit.Tube( TrimSkin( StonewareSwatch ), At( new[]
			{
				foot + new Vector3( turn * 1.2f * s, 0f, 10.4f * s ), foot + new Vector3( turn * 3.6f * s, 0f, 10.2f * s ),
				foot + new Vector3( turn * 4.4f * s, 0f, 7.4f * s ), foot + new Vector3( turn * 3.4f * s, 0f, 5.4f * s ),
			} ), 0.45f * s, 3 );
		}

		/// <summary>A stoneware mug on <paramref name="foot"/>, open, its inside showing.</summary>
		void Mug( Vector3 foot )
		{
			var s = Range( 1.25f, 1.5f );
			Kit.SmoothLathe( TrimSkin( StonewareSwatch ), At( foot ), Scale( s, new Vector2( 1.6f, 0f ), new Vector2( 1.75f, 3.6f ), new Vector2( 1.5f, 3.6f ), new Vector2( 1.4f, 0.8f ) ),
				6, Run, false, false, 40f, Next() );
		}

		/// <summary>
		/// A wooden sledge <paramref name="length"/> long — a real one, about two thirds of a player — hung on
		/// two pegs at <paramref name="u"/> at about a player's waist, side on to the room with its front toward
		/// <paramref name="facing"/> (−1 or 1 along the wall): two bent-wood runners curling up and back at the
		/// front to meet the seat, the seat's slats laid along it on short posts, the towing rope hanging in a
		/// loop under the curls. Stood on its tail it read as a ladder, and lying on the ledge it sank behind
		/// the tiles; hung side on, its profile is the shape everyone knows.
		/// </summary>
		void Sledge( float u, float length, float facing )
		{
			var width = Range( 17f, 20f );
			var seat = length * Range( 0.2f, 0.24f );
			var half = length * 0.5f;
			var tilt = Rotation.FromAxis( new Vector3( 0f, 1f, 0f ), Range( -3f, 3f ) );
			var centre = new Vector3( u, Face - 1.2f - width * 0.5f, Range( 44f, 54f ) );
			// Along the sledge (to its front), across it (toward the wall), up.
			Vector3 P( float along, float across, float up ) => centre + tilt * new Vector3( along * facing, across, up );
			var turn = Turn( tilt );
			// Varnished hickory, darker than the pine walls, as the skis are; the seat painted with their stripe.
			var paint = SkiSwatch( Pick( 2 ) );
			var hickory = TrimSkin( new Rect( paint.Left, paint.Top, paint.Width, paint.Height * 0.34f ) );

			for ( var s = -1; s <= 1; s += 2 )
			{
				var a = s * width * 0.5f;
				Kit.Tube( hickory, At( new[]
				{
					P( -half, a, 1.8f ), P( -half + 3f, a, 1f ), P( half - 14f, a, 1f ), P( half - 5f, a, 2.8f ),
					P( half - 0.5f, a, 7f ), P( half - 2f, a, seat - 1.5f ), P( half - 7f, a, seat - 0.8f ),
				} ), 1.1f, 4 );
				foreach ( var post in new[] { -half + 6f, -2f, half - 12f } )
					Kit.Box( hickory, At( P( post, a, seat * 0.5f ) ), new Vector3( 1.8f, 1.8f, seat - 2.4f ), turn );
			}

			// The battens under the seat rest on the two pegs.
			foreach ( var batten in new[] { -half + 7.5f, half - 13.5f } )
			{
				Kit.Box( hickory, At( P( batten, 0f, seat - 2.4f ) ), new Vector3( 2.4f, width + 1.5f, 1.6f ), turn );
				var peg = P( batten, 0f, seat - 4f );
				Kit.Tube( Timber, At( new[] { new Vector3( peg.x, Face - 1.5f, peg.z - 0.6f ), new Vector3( peg.x, Face - width - 3f, peg.z ) } ), 0.8f, 4, 0.7f );
			}

			var across = (width + 2f) / 3f;
			for ( var k = -1; k <= 1; k++ )
				Kit.BevelBox( TrimSkin( paint ), At( P( -3f + Range( -0.5f, 0.5f ), k * across, seat - 0.8f ) ), new Vector3( length - 10f, across - Range( 0.6f, 1f ), 1.6f ), 0.3f, turn );

			var drop = Range( 8f, 16f );
			Kit.Tube( TrimSkin( JuteSwatch ), At( new[]
			{
				P( half - 1f, -width * 0.4f, 6.5f ), P( half + 1.5f, -width * 0.25f, -drop * 0.5f ), P( half + 0.5f, Range( -1f, 1f ), -drop ),
				P( half + 1.5f, width * 0.25f, -drop * 0.5f ), P( half - 1f, width * 0.4f, 6.5f ),
			} ), 0.5f, 3 );
		}

		/// <summary>
		/// A pair of snowshoes hung on the wall at (<paramref name="u"/>, <paramref name="v"/>), side by side and
		/// each a little askew: a bent-wood teardrop frame, rawhide webbing laced across it, a leather binding.
		/// </summary>
		void Snowshoes( float u, float v )
		{
			var length = Range( 50f, 62f );
			var width = length * Range( 0.34f, 0.4f );
			for ( var s = -1; s <= 1; s += 2 )
			{
				var centre = new Vector3( u + s * width * Range( 0.55f, 0.7f ), Face - 1.6f, v + Range( -3f, 3f ) );
				var turn = Rotation.FromAxis( new Vector3( 0f, 1f, 0f ), s * Range( 4f, 14f ) );
				var frame = new Vector3[11];
				for ( var i = 0; i < frame.Length; i++ )
				{
					var a = i / 10f * MathF.Tau;
					var narrow = 1f - 0.7f * MathF.Pow( MathF.Max( 0f, -MathF.Cos( a ) ), 1.4f );
					frame[i] = centre + turn * new Vector3( MathF.Sin( a ) * width * 0.5f * narrow, 0f, MathF.Cos( a ) * length * 0.5f + length * 0.08f );
				}
				Kit.Tube( Timber, At( frame ), 1.05f, 3 );
				Kit.Ellipsoid( TrimSkin( JuteSwatch ), At( centre + turn * new Vector3( 0f, 0.3f, length * 0.12f ) ), new Vector3( width * 0.42f, 0.35f, length * 0.36f ), Turn( turn ), 3, 8 );
				Kit.Box( TrimSkin( LeatherSwatch ), At( centre + turn * new Vector3( 0f, -0.6f, length * 0.1f ) ), new Vector3( width * 0.62f, 1.4f, 2.4f ), Turn( turn ) );
			}
		}
	}
}
