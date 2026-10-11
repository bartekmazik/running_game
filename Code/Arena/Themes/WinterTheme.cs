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
	/// along it, two iron lanterns on posts, snow banked on the sills and drifted into bays, and in the
	/// plain bays a snowman in relief (one to a wall), a stack of firewood showing its cut ends, crossed
	/// skis on two walls, fir wreaths. Lights: two of the four warm lamps hang in the lanterns, two among
	/// the bulbs; two cool lights by the walls at the windows' height stand for the daylight off the snow.
	/// </summary>
	public override void Dress( FloorContext room )
	{
		var bays = BuildFrontLayer( room, new FrontStyle { Post = 30f, Pier = 74f, Corner = 34f, Cornice = 28f, Skirting = 12f, SkirtingDepth = 5f } );
		var windows = AddWindows( room, bays, InfillCell( 0 ), InfillCell( 1 ), WindowSill, WindowHeight, 84f, 0.45f, true, Seed + 800 );
		// The bulbs and lantern glass a little over their painted brightness, so they bloom like lit glass.
		Glow.Set( "g_flSelfIllumBrightness", 1.5f );
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
			{
				props.SillSnow( bay.Middle, width );
				if ( props.Next() < 0.3f ) props.Wreath( bay.Middle + props.Next() * 10f - 5f, WindowSill + WindowHeight + 28f, 10f );
				if ( props.Next() < 0.45f ) props.Drift( bay );
			}

			// The plain bays, in a shuffled order: a snowman in the first, skis next where this wall has
			// them, then firewood (two stacks at most to a wall) or a wreath, most with snow drifted at the foot.
			var plain = wall.FindAll( b => b.Width > 70f && !windows.Exists( w => w.Bay.From == b.From && w.Bay.Side == side ) );
			for ( var k = plain.Count - 1; k > 0; k-- )
			{
				var j = (int)(props.Next() * (k + 1));
				(plain[k], plain[j]) = (plain[j], plain[k]);
			}

			var stacks = 0;
			for ( var k = 0; k < plain.Count; k++ )
			{
				var bay = plain[k];
				if ( k == 0 ) props.Snowman( bay );
				else if ( k == 1 && (side == skiWall || side == otherSkiWall) ) props.Skis( bay );
				else if ( stacks < 2 && props.Next() < 0.55f )
				{
					stacks++;
					props.Firewood( bay );
					continue;
				}
				else props.Wreath( bay.Middle + props.Next() * 16f - 8f, 110f + props.Next() * 50f, 12f + props.Next() * 4f );
				if ( k != 0 && props.Next() < 0.6f ) props.Drift( bay );
			}
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
	/// The lit parts: bulbs in three warm whites (the odd one more amber, as old strings go), and the
	/// glass of a lantern lit from inside, a little softer.
	/// </summary>
	protected override PaintedSurface PaintGlow()
	{
		const int Size = 64;
		var painter = new Painter( Size, Palette.Hex( 0xFFE3B5 ), 0.3f );
		painter.Fill( 32, 0, 32, 32, Palette.Hex( 0xFFC27A ), roughness: 0.3f );
		painter.Fill( 0, 32, 32, 32, Palette.Hex( 0xFFD6A8 ), roughness: 0.3f );
		painter.Fill( 32, 32, 32, 32, Palette.Hex( 0xF7D2A0 ), roughness: 0.4f );
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

	/// <summary>
	/// The props' materials, painted into the infill band's two free cells (the first two hold the frosted
	/// pane and the frame paint): snow, ice, coal, carrot, twig, three firs, jute, two pairs of skis,
	/// bamboo, iron, firewood bark and two cut ends with their rings, two knits, wire, pine cone, felt, leather.
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

		Skin Trim( Rect region ) => new( _trim, region );

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
				for ( var u = start; u < end; u += Range( 2.2f, 5.5f ) )
				{
					var envelope = MathF.Sin( (u - start) / (end - start) * MathF.PI );
					var length = 2.5f + envelope * peak * Range( 6f, 30f ) * (Next() < 0.25f ? 1f : 0.45f);
					Icicle( u, Range( 1.2f, 4.5f ), length );
				}
				start = end + Range( 10f, 80f );
			}
		}

		/// <summary>One icicle: thickest at the root, tapering to a point, glossy, a little out of plumb.</summary>
		void Icicle( float u, float depth, float length )
		{
			var r = MathF.Min( 0.45f + length * 0.07f, 2.4f ) * Range( 0.8f, 1.2f );
			Kit.SmoothLathe( Trim( IceSwatch ), At( new Vector3( u, depth, Plate - length ) ), new[]
			{
				new Vector2( 0f, 0f ), new Vector2( r * 0.22f, length * 0.25f ), new Vector2( r * 0.5f, length * 0.55f ),
				new Vector2( r * 0.8f, length * 0.82f ), new Vector2( r, length ), new Vector2( r * 1.25f, length + 0.8f ),
			}, 6, Turn( Rotation.From( Range( -3f, 3f ), Next() * 360f, Range( -3f, 3f ) ) ), false, false, 60f, Next() );
		}

		/// <summary>
		/// A string of warm bulbs swagged along the top plate: hooked every metre or three, sagging between,
		/// a bulb on a short socket every foot or so, most warm white, a few amber or peach.
		/// </summary>
		public void BulbString()
		{
			const float Span = FloorContext.Span;
			const int Points = 9;
			var anchors = new List<float>();
			for ( var u = Range( 20f, 60f ); u < Span - 20f; u += Range( 75f, 135f ) ) anchors.Add( u );

			for ( var a = 0; a < anchors.Count; a++ )
				Kit.Tube( Trim( IronSwatch ), At( new[] { new Vector3( anchors[a], 0.5f, StringV + 1f ), new Vector3( anchors[a], -1.2f, StringV ) } ), 0.28f, 3 );

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
				Kit.Tube( Trim( WireSwatch ), At( wire ), 0.2f, 3 );

				var span = u1 - u0;
				for ( var d = Range( 3f, 7f ); d < span - 2f; d += Range( 9.5f, 13f ) )
				{
					var at = Along( wire, d / span );
					Kit.Tube( Trim( WireSwatch ), At( new[] { at + Vector3.Up * 0.2f, at - Vector3.Up * 1.7f } ), 0.5f, 5 );
					var tint = Next() < 0.7f ? 0 : 1 + Pick( 2 );
					Kit.Ellipsoid( new Skin( _glow, BulbGlow( tint ) ), At( at - Vector3.Up * 3.5f ), new Vector3( 1.3f, 1.3f, 1.8f ), Run, 4, 9 );
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
				Kit.Ellipsoid( Trim( SnowSwatch ), At( new Vector3( x, 35f, WindowSill - 0.5f ) ), r, Turn( Rotation.FromYaw( Range( -6f, 6f ) ) ), 5, 10 );
			}
		}

		/// <summary>A drift banked against the foot of the wall: a few overlapping heaps, high against the wall, sloping out.</summary>
		public void Drift( Bay bay )
		{
			var u = bay.From + Range( 4f, bay.Width * 0.4f );
			for ( var k = 2 + Pick( 4 ); k > 0 && u < bay.To - 6f; k-- )
			{
				var r = new Vector3( Range( 12f, 30f ), Range( 7f, 12f ), Range( 6f, 16f ) );
				var centre = new Vector3( u, MathF.Max( Face - r.y * 0.45f, Front + r.y ), Ledge + r.z * 0.15f );
				Kit.Ellipsoid( Trim( SnowSwatch ), At( centre ), r, Turn( Rotation.FromYaw( Range( -10f, 10f ) ) ), 5, 12 );
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
		/// A snowman in relief in the bay, half sunk into the wall: three balls of packed snow, each its
		/// own size, leaning a little; coal eyes, mouth and buttons, a carrot nose, twig arms with a fork,
		/// often a knitted scarf, a beanie or a felt hat. Every one looks a different way.
		/// </summary>
		public void Snowman( Bay bay )
		{
			var u = bay.Middle + Range( -bay.Width * 0.15f, bay.Width * 0.15f );
			var rb = Range( 19f, 24f );
			var rm = rb * Range( 0.66f, 0.74f );
			var rh = rm * Range( 0.64f, 0.72f );
			var lean = Range( -2.5f, 2.5f );
			var look = Range( -0.25f, 0.25f );
			var snow = Trim( SnowSwatch );
			var coal = Trim( CoalSwatch );
			Vector3 Ball( float v, float r ) => new( u + lean * (v / 60f), Face - r * 0.4f, v );

			var baseR = new Vector3( rb * Range( 1f, 1.08f ), rb * 0.92f, rb * 0.88f );
			var midR = new Vector3( rm, rm * 0.92f, rm * 0.9f );
			var headR = new Vector3( rh, rh * 0.95f, rh * 0.95f );
			var bottom = Ball( Ledge + rb * 0.78f, rb );
			var middle = Ball( bottom.z + rb * 0.75f + rm * 0.7f, rm );
			var head = Ball( middle.z + rm * 0.78f + rh * 0.72f, rh );
			Kit.Ellipsoid( snow, At( bottom ), baseR, Run, 8, 14 );
			Kit.Ellipsoid( snow, At( middle ), midR, Run, 7, 12 );
			Kit.Ellipsoid( snow, At( head ), headR, Run, 7, 12 );

			var size = rh / 8f;
			for ( var s = -1; s <= 1; s += 2 )
				Kit.Ellipsoid( coal, At( OnBall( head, headR, look + s * 0.32f, 0.28f ) ), new Vector3( 0.9f, 0.6f, 0.9f ) * (size * Range( 0.9f, 1.2f )), Run, 3, 6 );
			for ( var i = -2; i <= 2; i++ )
				Kit.Ellipsoid( coal, At( OnBall( head, headR, look + i * 0.13f, -0.2f - 0.08f * (1f - i * i / 4f) ) ), new Vector3( 0.55f, 0.45f, 0.55f ) * size, Run, 3, 5 );
			for ( var i = 0; i < 3; i++ )
				Kit.Ellipsoid( coal, At( OnBall( middle, midR, look * 0.5f + Range( -0.04f, 0.04f ), 0.38f - i * 0.36f ) ), new Vector3( 0.85f, 0.6f, 0.85f ) * size, Run, 3, 6 );

			var nose = new Vector3( look * 0.8f, -1f, Range( -0.2f, 0f ) ).Normal;
			var length = Range( 7f, 11f ) * size;
			Kit.SmoothLathe( Trim( CarrotSwatch ), At( OnBall( head, headR, look, 0.02f ) - nose * 1.2f ), new[]
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
				Kit.Tube( Trim( TwigSwatch ), At( arm ), 0.65f, 5, 0.22f );
				var fork = Along( arm, Range( 0.55f, 0.75f ) );
				Kit.Tube( Trim( TwigSwatch ), At( new[] { fork, fork + (reach + Vector3.Up * Range( 0.6f, 1.1f )).Normal * Range( 4f, 6.5f ) } ), 0.35f, 4, 0.12f );
			}

			// A knitted scarf round the neck, its end hanging over one shoulder.
			var knit = Trim( KnitSwatch( Pick( 2 ) ) );
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
				Kit.SmoothLathe( Trim( FeltSwatch ), At( head + new Vector3( 0f, 0f, rh * 0.62f ) ), new[]
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
			var count = 34 + Pick( 9 );
			var spin = Next() * MathF.Tau;
			var ring = new Vector3[count];
			var bulk = radius / 12f;
			for ( var i = 0; i < count; i++ )
			{
				// Two loose courses, inner and outer, so the ring is a fat irregular torus rather than a string of beads.
				var a = spin + i * MathF.Tau / count + Range( -0.08f, 0.08f );
				var reach = radius + (i % 2 == 0 ? -1.4f : 1.4f) * bulk + Range( -0.8f, 0.8f );
				ring[i] = centre + new Vector3( MathF.Cos( a ) * reach, Range( -0.8f, 0.4f ), MathF.Sin( a ) * reach );
				Kit.Ellipsoid( Trim( FirSwatch( Pick( 3 ) ) ), At( ring[i] ), new Vector3( Range( 3.6f, 4.6f ), Range( 2.2f, 2.8f ), Range( 2.8f, 3.6f ) ) * bulk,
					Turn( Rotation.FromAxis( new Vector3( 0f, 1f, 0f ), a * 57.3f + Range( -25f, 25f ) ) ), 3, 6 );
			}

			for ( var s = 0; s < 20; s++ )
			{
				var a = Next() * MathF.Tau;
				var root = centre + new Vector3( MathF.Cos( a ), 0f, MathF.Sin( a ) ) * (radius + Range( -2f, 2f ) * bulk) + ToRoom * 1.6f;
				var outward = new Vector3( MathF.Cos( a + Range( -0.9f, 0.9f ) ), 0f, MathF.Sin( a + Range( -0.9f, 0.9f ) ) );
				Kit.Blade( Trim( FirSwatch( Pick( 3 ) ) ), At( root ), Turn( Aim( outward, 0f, ToRoom ) ), Range( 4f, 7f ) * bulk, Range( 1.3f, 1.9f ) * bulk, 0.2f, 0.1f, 2, 0.1f, 0.4f );
			}

			for ( var c = 3 + Pick( 3 ); c > 0; c-- )
			{
				var at = ring[Pick( count )] + ToRoom * 2.2f;
				Kit.Ellipsoid( Trim( ConeSwatch ), At( at ), new Vector3( 1.1f, 1f, 1.6f ), Turn( Rotation.From( Next() * 360f, Next() * 360f, 0f ) ), 4, 6 );
			}

			for ( var k = 4 + Pick( 4 ); k > 0; k-- )
			{
				var a = Range( 0.35f, 2.8f );
				var at = centre + new Vector3( MathF.Cos( a ) * radius, -1.6f, MathF.Sin( a ) * radius + 2f );
				Kit.Ellipsoid( Trim( SnowSwatch ), At( at ), new Vector3( Range( 1.6f, 2.8f ), 1.4f, Range( 0.7f, 1.1f ) ), Run, 3, 7 );
			}

			// The bow, most often at the foot.
			var top = Next() < 0.3f ? 1f : -1f;
			var bow = centre + new Vector3( Range( -1.5f, 1.5f ), -2f, top * radius );
			var jute = Trim( JuteSwatch );
			for ( var s = -1; s <= 1; s += 2 )
				Kit.Ellipsoid( jute, At( bow + new Vector3( s * 2.8f, 0f, 0.8f ) ), new Vector3( 3.2f, 1f, 2f ), Turn( Rotation.FromAxis( new Vector3( 0f, 1f, 0f ), s * Range( 20f, 35f ) ) ), 3, 7 );
			Kit.Ellipsoid( jute, At( bow + ToRoom * 0.5f ), new Vector3( 1.3f, 1.1f, 1.3f ), Run, 3, 6 );
			for ( var s = -1; s <= 1; s += 2 )
				Kit.Blade( jute, At( bow ), Turn( Aim( new Vector3( s * Range( 0.3f, 0.6f ), 0f, -1f ), 0f, ToRoom ) ), Range( 8f, 11f ), 2.2f, 0.15f, 0f, 2, 0.9f, 0.5f );
		}

		/// <summary>A pair of wooden skis crossed on the wall, their tips curling out, with the poles crossed behind them.</summary>
		public void Skis( Bay bay )
		{
			var centre = new Vector3( bay.Middle + Range( -8f, 8f ), Face, Range( 98f, 116f ) );
			var lean = Range( 18f, 26f ) * MathF.PI / 180f;
			var skin = Trim( SkiSwatch( Pick( 2 ) ) );

			if ( Next() < 0.8f )
			{
				for ( var s = -1; s <= 1; s += 2 )
				{
					var a = s * Range( 10f, 16f ) * MathF.PI / 180f;
					var along = new Vector3( MathF.Sin( a ), 0f, MathF.Cos( a ) );
					var foot = centre + new Vector3( 0f, -0.6f, 0f ) - along * 31f;
					var head = foot + along * 62f;
					Kit.Tube( Trim( BambooSwatch ), At( new[] { foot, head } ), 0.45f, 5 );
					Kit.Tube( Trim( LeatherSwatch ), At( new[] { head - along * 7f, head } ), 0.7f, 6 );
					Kit.SmoothLathe( Trim( LeatherSwatch ), At( foot + along * 5f ), new[] { new Vector2( 0.5f, 0f ), new Vector2( 2.7f, 0.2f ), new Vector2( 2.7f, 0.7f ), new Vector2( 0.5f, 0.6f ) },
						10, Turn( Rotation.FromToRotation( Vector3.Up, along ) ), false, false );
				}
			}

			for ( var s = -1; s <= 1; s += 2 )
			{
				var along = new Vector3( MathF.Sin( lean * s ), 0f, MathF.Cos( lean * s ) );
				Ski( centre.WithY( Face - (s < 0 ? 1.2f : 2.6f) ), along, Range( 72f, 80f ), skin );
			}
		}

		/// <summary>One ski lying flat on the wall along <paramref name="along"/>, its shovel curling out toward the room.</summary>
		void Ski( Vector3 centre, Vector3 along, float length, Skin skin )
		{
			var body = length * 0.84f;
			var tail = centre - along * length * 0.5f;
			var flat = Turn( Rotation.LookAt( along, ToRoom ) );
			Kit.BevelBox( skin, At( tail + along * body * 0.5f ), new Vector3( body, 3.1f, 0.9f ), 0.25f, flat );

			var at = tail + along * body;
			var heading = along;
			for ( var k = 0; k < 3; k++ )
			{
				var piece = length * 0.055f;
				heading = (heading + ToRoom * 0.32f).Normal;
				var up = (ToRoom - heading * Vector3.Dot( ToRoom, heading )).Normal;
				Kit.BevelBox( skin, At( at + heading * piece * 0.5f ), new Vector3( piece + 0.4f, 3.1f - k * 0.25f, 0.9f ), 0.25f, Turn( Rotation.LookAt( heading, up ) ) );
				at += heading * piece;
			}

			Kit.BevelBox( Trim( IronSwatch ), At( tail + along * length * 0.42f + ToRoom * 0.9f ), new Vector3( 5f, 3.4f, 1.2f ), 0.3f, flat );
			Kit.BevelBox( Trim( LeatherSwatch ), At( tail + along * length * 0.47f + ToRoom * 1.2f ), new Vector3( 2.2f, 3.6f, 1.6f ), 0.3f, flat );
		}

		/// <summary>A stack of split-free firewood logs filling part of a bay, their sawn ends to the room, the top rows stepping in.</summary>
		public void Firewood( Bay bay )
		{
			var width = MathF.Min( bay.Width - 12f, Range( 50f, 95f ) );
			var u0 = bay.From + 6f + Next() * (bay.Width - 12f - width);
			var height = Range( 34f, 62f );
			var v = Ledge;
			for ( var row = 0; v < Ledge + height; row++ )
			{
				var inset = row * Range( 0f, 3f );
				var tallest = 0f;
				for ( var u = u0 + inset + (row % 2) * 2.5f; ; )
				{
					var r = Range( 3.2f, 5.4f );
					if ( u + 2f * r > u0 + width - inset ) break;
					Log( new Vector3( u + r, Face - 0.5f, v + r ), r, Face - 0.5f - Range( 4f, 9f ) );
					u += 2f * r + Range( 0.2f, 1.2f );
					tallest = MathF.Max( tallest, 2f * r );
				}
				if ( tallest == 0f ) break;
				v += tallest * Range( 0.82f, 0.92f );
			}
		}

		/// <summary>A log lying along the depth from <paramref name="back"/>, <paramref name="length"/> long: bark round it and the sawn end with its rings facing the room.</summary>
		void Log( Vector3 back, float r, float length )
		{
			var axis = Turn( Rotation.FromToRotation( Vector3.Up, new Vector3( Range( -0.04f, 0.04f ), -1f, Range( -0.04f, 0.04f ) ).Normal ) );
			var turn = Next();
			Kit.SmoothLathe( Trim( BarkSwatch ), At( back ), new[] { new Vector2( r, 0f ), new Vector2( r * 1.03f, length * 0.5f ), new Vector2( r * Range( 0.95f, 1.02f ), length ) },
				10, axis, false, false, 50f, turn );
			Kit.SmoothLathe( Trim( LogEndSwatch( Pick( 2 ) ) ), At( back ), new[] { new Vector2( r, length ), new Vector2( 0f, length + 0.25f ) }, 10, axis, false, false, 20f, turn );
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
			var iron = Trim( IronSwatch );

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
	}
}
