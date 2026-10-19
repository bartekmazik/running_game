using System;
using System.Collections.Generic;

/// <summary>
/// Floor 1 (index 0): a greenhouse under a glass roof. Underfoot, hand-made terracotta: each slab
/// its own size and tone, hand-cut edges, kiln-flashed patches, pitting, sealed in patches so the
/// sun catches part of it, chipped and nicked where pots were dragged, moss creeping in.
/// </summary>
public sealed class GreenhouseTheme : ArenaTheme
{
	public override string Name => "Greenhouse";
	public override int Floor => 0;
	public override Color LightColor => new( 1.10f, 1.00f, 0.86f );

	static Color Clay => Palette.Hex( 0xD4845F );
	static Color ClayLight => Palette.Hex( 0xE3A07E );
	static Color ClayShade => Palette.Hex( 0xB2663F );

	/// <summary>Darker kiln-flashed clay, where the flame touched the tile.</summary>
	static Color Flash => Palette.Hex( 0x9A5434 );

	/// <summary>A fresh break shows the unsealed body: paler and duller.</summary>
	static Color RawClay => Palette.Hex( 0xE8BC9C );

	static Color Moss => Palette.Hex( 0x6B7052 );
	public static Color MossLight => Palette.Hex( 0x8E9D74 );
	static Color Soil => Palette.Hex( 0x5A4434 );

	/// <summary>White-sage paint on the glazing bars and the timber trim.</summary>
	static Color BarPaint => Palette.Hex( 0xE4EBDD );
	static Color BarShade => Palette.Hex( 0xAEBBA6 );

	/// <summary>Opaque frosted glass, pale sky-tinted (controller ruling R6: no working translucency).</summary>
	static Color Glass => Palette.Hex( 0xCADDE0 );
	static Color GlassLight => Palette.Hex( 0xE0ECEC );
	static Color Putty => Palette.Hex( 0xC4C7B8 );
	static Color LapDirt => Palette.Hex( 0x98A391 );
	static Color Algae => Palette.Hex( 0x93AA82 );

	static Color Brick => Palette.Hex( 0xB46B4C );
	static Color BrickLight => Palette.Hex( 0xC78564 );
	static Color BrickDark => Palette.Hex( 0x8E5039 );
	static Color BrickMortar => Palette.Hex( 0xCDBFA8 );

	/// <summary>Bare timber where the paint has flaked: grey, weathered.</summary>
	static Color WeatheredWood => Palette.Hex( 0x9C9485 );

	/// <summary>
	/// The structural front layer — slender painted posts, a plate beam under the roof, skirting — and the
	/// planting in its bays: every bay holds a raised bed, a potting bench or a group of pots on the ledge,
	/// some a staging shelf on the glass above, some a fern in a basket hung from the plate beam; four
	/// enamel pendants hang on brackets from the plate beam, each with the dim warm lamp the room has always had
	/// (the sun through the glass does the rest). Each wall draws from its own stream, so no two match.
	/// </summary>
	public override void Dress( FloorContext room )
	{
		var bays = BuildFrontLayer( room, new FrontStyle { Post = 20f, Pier = 64f, Corner = 26f, Cornice = 22f, Skirting = 12f, SkirtingDepth = 4f } );
		var lamps = LampSpots( 4, Seed + 900 );
		// Leaves below eye height are seen from above: single faces on a copy of the trim that draws their
		// backs too, half the faces of a leaf modelled twice. Above it (Props.Leaf) they are modelled back
		// to back, since a back face drawn by that copy is lit as if it were the front, and an underside
		// seen from below caught the sun as a white glare.
		var leaves = Trim.CreateCopy( "arena_0_leaves" );
		leaves.SetFeature( "F_RENDER_BACKFACES", 1 );
		// The lamps burn in daylight: a little over the painted brightness, or they read as switched off.
		Glow.Set( "g_flSelfIllumBrightness", 1.3f );
		var props = new Props( room, Trim, leaves, Glow );

		for ( var side = 0; side < 4; side++ )
		{
			props.Begin( side, Seed + 1000 + side * 131 );
			var previous = Furniture.None;
			var benches = 0;
			var wall = bays.FindAll( b => b.Side == side );
			bool Lit( Bay bay ) => lamps.Exists( l => l.Side == side && l.U > bay.From - 30f && l.U < bay.To + 30f );

			// One palm on each wall, in a wide bay of its own where no pendant hangs, its crown clear of them.
			var open = new List<int>();
			for ( var k = 1; k + 1 < wall.Count; k++ )
				if ( wall[k].Width >= 90f && !Lit( wall[k] ) ) open.Add( k );
			var palm = open.Count > 0 ? open[(int)(props.Next() * open.Count)] : -1;

			for ( var k = 0; k < wall.Count; k++ )
			{
				var bay = wall[k];

				// No two neighbouring bays alike, and a bench on most walls: one is pushed into the second-last
				// bay of a wall that has none yet, unless that bay is narrow or holds the palm.
				var pick = props.Next();
				var kind = pick < 0.4f ? Furniture.Bed : pick < 0.7f ? Furniture.Bench : Furniture.Pots;
				if ( kind == previous ) kind = kind == Furniture.Bed ? (props.Next() < 0.5f ? Furniture.Bench : Furniture.Pots) : Furniture.Bed;
				if ( benches == 0 && k == wall.Count - 2 && previous != Furniture.Bench ) kind = Furniture.Bench;
				if ( bay.Width < 70f ) kind = Furniture.Pots;
				if ( palm == k ) kind = Furniture.Palm;
				if ( kind == Furniture.Bench ) benches++;
				previous = kind;

				switch ( kind )
				{
					case Furniture.Bed: props.RaisedBed( bay ); break;
					case Furniture.Bench: props.Bench( bay ); break;
					case Furniture.Palm: props.PalmTub( bay ); break;
					default: props.PotGroup( bay ); break;
				}

				if ( kind != Furniture.Palm && bay.Width > 60f && props.Next() < 0.35f ) props.Staging( bay );

				// Ferns where neither a pendant nor a palm's crown hangs in the bay.
				if ( kind != Furniture.Palm && !Lit( bay ) && props.Next() < 0.45f ) props.HangingFern( bay.From + bay.Width * (0.3f + props.Next() * 0.4f) );
			}
		}

		props.Begin( 0, Seed + 1900 );
		foreach ( var lamp in lamps )
		{
			props.Pendant( lamp.Side, lamp.U );
			AddLamp( room, lamp.Side, lamp.U, Props.PendantMouth - 2f, Props.PendantDepth, LightColor * 1.2f * lamp.Brightness );
		}
	}

	enum Furniture { None, Bed, Bench, Pots, Palm }

	/// <summary>
	/// The lit parts of the pendants, on the self-lit base: a bulb's warm glass, and the white enamel inside
	/// a shade lit by it — softer, as light bounced off enamel is.
	/// </summary>
	protected override PaintedSurface PaintGlow()
	{
		var painter = new Painter( GlowSize, Palette.Hex( 0xFFE6BC ), 0.3f );
		var glow = new float[GlowSize * GlowSize];
		for ( var y = 0; y < GlowSize; y++ )
		for ( var x = 0; x < GlowSize; x++ )
			glow[y * GlowSize + x] = x < GlowSize / 2 ? 1f : 0.42f + 0.18f * y / GlowSize;
		painter.Fill( GlowSize / 2, 0, GlowSize / 2, GlowSize, Palette.Hex( 0xF6EBD6 ), roughness: 0.4f );
		painter.AddGlow( glow );
		return painter.Bake( "arena_0_glow", ArenaGenerator.GlowMaterialPath, 1f );
	}

	const int GlowSize = 32;
	static Rect BulbGlow => new( 0.08f, 0.1f, 0.34f, 0.8f );
	static Rect ShadeGlow => new( 0.58f, 0.1f, 0.34f, 0.8f );

	/// <summary>
	/// A greenhouse side, floor to roof: a brick dwarf wall up to bench height, a painted timber sill,
	/// then glazing — white-sage bars about every 27 units (ten to a repeat) bedded in putty, a vent
	/// rail across them a little under the eaves, where top-hung vents open, and between them panes of
	/// opaque frosted glass lapped in uneven courses, each its own tint, with grime and a green bloom
	/// caught in the laps and condensation gathered at the foot of each pane. The dwarf wall keeps bench
	/// height and the panes their size whatever the room's height: a taller room has more courses of glass.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		const int Bars = 10;
		const float Column = WallSize / (float)Bars;
		const float Bar = 12f;
		const float VentBelowEaves = 55f;
		int Row( float v ) => (int)MathF.Round( (WallRepeat - v) * Ppu );
		var painter = new Painter( WallSize, Glass, 0.15f );
		var random = new Random( Seed + 600 );
		float Next() => (float)random.NextDouble();

		var sillTop = Row( 50f );
		var kneeTop = Row( 44f );

		// Panes, column by column, lapped in courses of uneven height that differ between columns.
		for ( var c = 0; c < Bars; c++ )
		{
			var x0 = c * Column + Bar * 0.5f;
			var width = Column - Bar;
			var y = 0f;
			while ( y < sillTop - 1f )
			{
				var pane = (19f + Next() * 11f) * Ppu;
				if ( sillTop - (y + pane) < 12f * Ppu ) pane = sillTop - y;
				var tint = Mix( Mix( Glass, GlassLight, Next() * 0.6f ), Algae, Next() * Next() * 0.12f );
				painter.Rect( x0, y, width, pane, tint, height: 0.5f, roughness: 0.1f + Next() * 0.08f );

				// A broad soft highlight across some panes, never the same slant twice.
				if ( Next() < 0.5f )
				{
					var at = x0 + Next() * width;
					var slant = 20f + Next() * 40f;
					var thick = 8f + Next() * 18f;
					painter.Polygon( new[] { new Vector2( at, y + pane ), new Vector2( at + thick, y + pane ), new Vector2( at + thick + slant, y ), new Vector2( at + slant, y ) },
						GlassLight, alpha: 0.18f + Next() * 0.12f );
				}

				// Condensation and dirt gather at the foot of the pane, above the lap.
				var drops = 6 + random.Next( 14 );
				for ( var d = 0; d < drops; d++ )
					painter.Circle( x0 + 4f + Next() * (width - 8f), y + pane - 3f - Next() * Next() * 30f, 0.8f + Next() * 1.4f, GlassLight, 0.35f, roughness: 0.05f );

				if ( y + pane < sillTop - 1f )
				{
					painter.Rect( x0, y + pane - 4f, width, 4f, Mix( LapDirt, Algae, Next() * 0.5f ), alpha: 0.55f + Next() * 0.25f, height: 0.42f, roughness: 0.6f );
					painter.Rect( x0, y + pane - 9f, width, 5f, LapDirt, alpha: 0.12f );
				}
				y += pane;
			}
		}

		// Putty fillets either side of each bar, the bars over them, the vent rail across.
		for ( var c = 0; c < Bars; c++ )
		{
			var x = c * Column;
			painter.Rect( x - Bar * 0.5f - 3f, 0f, Bar + 6f, sillTop, Putty, height: 0.72f, bevel: 2f, roughness: 0.7f );
			painter.Rect( x - Bar * 0.5f, 0f, Bar, sillTop, Mix( BarPaint, BarShade, Next() * 0.15f ), radius: 2f, height: 1f, bevel: 4f, roughness: 0.45f );
		}
		painter.Rect( 0f, Row( WallRepeat - VentBelowEaves ), WallSize, Row( WallRepeat - VentBelowEaves - 4f ) - Row( WallRepeat - VentBelowEaves ), BarPaint, height: 1.05f, bevel: 4f, roughness: 0.45f );

		// Sill, then the dwarf wall: a running bond of soft red bricks, every brick its own length and tone.
		painter.Rect( 0f, sillTop, WallSize, kneeTop - sillTop + 3f, BarPaint, height: 1.2f, bevel: 6f, roughness: 0.5f );
		painter.Fill( 0, kneeTop + 3, WallSize, WallSize - kneeTop - 3, BrickMortar, height: 0.55f, roughness: 0.95f );
		var course = 2.9f * Ppu;
		var row = 0;
		for ( var by = kneeTop + 3f; by < WallSize - 1f; by += course, row++ )
		{
			var x = (row % 2) * 21f + Next() * 6f;
			var end = x + WallSize;
			while ( x < end - 1f )
			{
				var length = (8f + Next() * 1.4f) * Ppu;
				if ( end - (x + length) < 20f ) length = end - x;
				var tone = Next() * 2f - 1f;
				var brick = tone < 0f ? Mix( Brick, BrickDark, -tone * 0.6f ) : Mix( Brick, BrickLight, tone * 0.6f );
				painter.Rect( x + 1.2f, by + 1.2f, length - 2.4f, MathF.Min( course, WallSize - by ) - 2.4f, brick, radius: 1.5f, height: 1f, bevel: 2.5f,
					roughness: 0.88f, angle: (Next() - 0.5f) * 0.5f );
				x += length;
			}
		}

		// Moss creeping up from the foot of the dwarf wall.
		var moss = Ramp( painter.Noise( 16, Seed + 601, 4 ), 0.45f, 0.7f, 0.8f );
		for ( var y = 0; y < WallSize; y++ )
		{
			var low = Math.Clamp( (y - (WallSize - 70f)) / 60f, 0f, 1f );
			for ( var x = 0; x < WallSize; x++ ) moss[y * WallSize + x] *= low;
		}
		painter.Blend( moss, Mix( Moss, MossLight, 0.3f ), roughness: 0.95f );

		painter.Mottle( 0.03f, 8, Seed + 602, 4 );
		painter.RoughnessNoise( 0.05f, 8, Seed + 603, 3 );
		painter.Cavity( 0.4f, 4 );
		painter.EdgeWear( 0.25f, 3, Seed + 604 );
		painter.Grain( 0.01f, Seed + 605 );
		return painter.Bake( "arena_0_wall", Painter.Matte, 5f );
	}

	/// <summary>
	/// Painted timber in white-sage, the grain showing faintly through the paint, flaking to bare
	/// grey wood in places; the slab edge is a course of terracotta brick-on-edge, the floor's clay.
	/// </summary>
	protected override PaintedSurface PaintTrim()
	{
		return PaintTrimAtlas( "arena_0_trim", BarPaint, BarShade, 0.45f, 0.08f, Seed + 700, painter =>
		{
			var random = new Random( Seed + 701 );
			float Next() => (float)random.NextDouble();

			var (slabTop, slabHeight) = TrimRows( TrimBand.SlabEdge );
			painter.Fill( 0, slabTop, TrimSize, slabHeight, Mix( ClayShade, Flash, 0.5f ), roughness: 0.9f );
			for ( var x = 0f; x < TrimSize - 1f; )
			{
				var length = 34f + Next() * 16f;
				if ( TrimSize - (x + length) < 26f ) length = TrimSize - x;
				var tone = Next() * 2f - 1f;
				painter.Rect( x + 1.5f, slabTop + 2f, length - 3f, slabHeight - 4f, tone < 0f ? Mix( Clay, ClayShade, -tone * 0.6f ) : Mix( Clay, ClayLight, tone * 0.5f ),
					radius: 2f, height: 0.95f, bevel: 3f, roughness: 0.85f );
				x += length;
			}

			// Flaking: small patches of bare grey timber on the posts, beams and skirting.
			foreach ( var band in new[] { TrimBand.Cornice, TrimBand.Pilaster, TrimBand.Skirting } )
			{
				var (top, height) = TrimRows( band );
				var flakes = 4 + random.Next( 4 );
				for ( var f = 0; f < flakes; f++ )
				{
					var at = new Vector2( Next() * TrimSize, top + 4f + Next() * (height - 8f) );
					painter.Polygon( Organic( at, new Vector2( 2f + Next() * 7f, 1.5f + Next() * 3f ), 0.9f, random.Next(), Next() * 0.4f, 16 ), Mix( WeatheredWood, BarPaint, 0.4f ),
						alpha: 0.5f, roughness: 0.9f );
				}
			}

			PaintSwatches( painter, new Random( Seed + 702 ) );
		} );
	}

	/// <summary>Leaf colours, one per swatch in the first infill row: fresh, deep, yellow-green, glaucous, variegated, fern, glossy dark, olive.</summary>
	static readonly uint[] LeafColors = { 0x7A9E55, 0x4D7444, 0x9AB362, 0x8BA58C, 0x6C9452, 0x6A9449, 0x3E6141, 0x87986A };

	/// <summary>Pot bodies, one per swatch in the second row: five clays, an old mossy one, and two glazes.</summary>
	static readonly uint[] PotColors = { 0xCB7E58, 0xBD6F4C, 0xA9603F, 0xC98762, 0xB8714F, 0x9E6448, 0x9DB4A0, 0xE4DCC6 };

	static Rect LeafSwatch( int k ) => Swatch( k * 64, 0, 64, 32 );
	static Rect PotSwatch( int k ) => Swatch( k * 64, 32, 64, 32 );
	static Rect SoilSwatch( int k ) => Swatch( k * 64, 64, 64, 32 );
	static Rect CedarSwatch => Swatch( 128, 64, 64, 32 );
	static Rect GreyWoodSwatch => Swatch( 192, 64, 64, 32 );
	static Rect ZincSwatch => Swatch( 256, 64, 64, 32 );
	static Rect CanPaintSwatch => Swatch( 320, 64, 64, 32 );
	static Rect JuteSwatch => Swatch( 384, 64, 64, 32 );
	static Rect IronSwatch => Swatch( 448, 64, 64, 32 );
	static Rect BarkSwatch => Swatch( 0, 96, 64, 32 );
	static Rect CaneSwatch => Swatch( 64, 96, 64, 32 );
	static Rect EnamelSwatch => Swatch( 128, 96, 64, 32 );
	static Rect CoirSwatch => Swatch( 192, 96, 64, 32 );
	static Rect CreamEnamelSwatch => Swatch( 256, 96, 64, 32 );
	static Rect FlowerSwatch( int k ) => Swatch( 320 + k * 32, 96, 32, 32 );
	static Rect LabelSwatch => Swatch( 384, 96, 32, 32 );
	static Rect StemSwatch => Swatch( 416, 96, 32, 32 );

	/// <summary>Massed foliage for clumps (a box ball, a cushion of thyme): deep, fresh, silvery.</summary>
	static Rect FoliageSwatch( int k ) => Swatch( k * 64, 128, 64, 32 );

	/// <summary>A palm's frond: long narrow leaflets either side of the rib.</summary>
	static Rect PalmSwatch => Swatch( 192, 128, 64, 32 );

	/// <summary>
	/// The props' materials, painted into the infill band (the greenhouse hangs no infill pictures):
	/// leaves with a pale midrib and veins running to the tip, clay pots darker inside and bloomed with
	/// lime under the rim or green at the foot, soil and peat, cedar and silvered timber, galvanised and
	/// painted zinc, jute, iron, bark, cane, enamel, a coir basket lining, flowers, a plant label.
	/// </summary>
	static void PaintSwatches( Painter painter, Random random )
	{
		float Next() => (float)random.NextDouble();
		var band = TrimRows( TrimBand.Infill ).Top;
		painter.Fill( 0, band, TrimSize, TrimRows( TrimBand.Infill ).Height, Soil, height: 0.8f, roughness: 0.9f );

		for ( var k = 0; k < 8; k++ )
		{
			var x0 = k * 64; var y0 = band;
			var leaf = Palette.Hex( LeafColors[k] );
			var gloss = k == 6 ? 0.32f : k == 3 ? 0.62f : 0.48f;
			// Paler toward the stalk, deeper toward the tip; the rim a shade darker.
			for ( var x = 0; x < 64; x++ )
				painter.Fill( x0 + x, y0, 1, 32, Mix( Mix( leaf, Color.White, 0.12f ), Mix( leaf, Palette.Ink, 0.12f ), x / 63f ), roughness: gloss );
			painter.Rect( x0, y0, 64, 3, Mix( leaf, Palette.Ink, 0.25f ), alpha: 0.5f );
			painter.Rect( x0, y0 + 29, 64, 3, Mix( leaf, Palette.Ink, 0.25f ), alpha: 0.5f );
			if ( k == 4 )
			{
				// Variegated: a cream margin either side of a green heart.
				painter.Rect( x0, y0, 64, 9, Palette.Hex( 0xE6E5C4 ), alpha: 0.85f );
				painter.Rect( x0, y0 + 23, 64, 9, Palette.Hex( 0xE6E5C4 ), alpha: 0.85f );
			}
			if ( k == 5 )
			{
				PaintPinnate( painter, x0, y0, leaf, 4.2f, 13f, random );
				continue;
			}
			painter.Line( new Vector2( x0 + 1f, y0 + 16f ), new Vector2( x0 + 63f, y0 + 16f ), 1.4f, Mix( leaf, Palette.Hex( 0xE8EBC8 ), 0.45f ), 0.75f );
			for ( var v = 0; v < 7; v++ )
			{
				var at = x0 + 4f + v * 8.5f + Next() * 2f;
				var reach = 8f + Next() * 4f;
				painter.Line( new Vector2( at, y0 + 16f ), new Vector2( at + reach, y0 + 3f ), 0.8f, Mix( leaf, Color.White, 0.3f ), 0.3f );
				painter.Line( new Vector2( at, y0 + 16f ), new Vector2( at + reach, y0 + 29f ), 0.8f, Mix( leaf, Color.White, 0.3f ), 0.3f );
			}
		}

		for ( var k = 0; k < 8; k++ )
		{
			var x0 = k * 64; var y0 = band + 32;
			var clay = Palette.Hex( PotColors[k] );
			var glazed = k >= 6;
			painter.Fill( x0, y0, 64, 32, clay, roughness: glazed ? 0.22f : 0.85f );
			// The top fifth is the inside of the pot, the rim's lip just under it.
			painter.Fill( x0, y0, 64, 6, Mix( clay, Palette.Ink, glazed ? 0.3f : 0.45f ), roughness: 0.9f );
			painter.Fill( x0, y0 + 6, 64, 2, Mix( clay, Color.White, 0.08f ) );
			for ( var s = 0; s < 70; s++ )
				painter.Circle( x0 + Next() * 64f, y0 + 6f + Next() * 26f, 0.4f + Next() * 0.6f, Mix( clay, Next() < 0.5f ? Flash : RawClay, 0.5f ), 0.4f );
			if ( k == 3 || k == 5 )
			{
				// Lime bloom wicked up through the clay, heaviest under the rim.
				for ( var s = 0; s < 18; s++ )
					painter.Polygon( Organic( new Vector2( x0 + Next() * 64f, y0 + 9f + Next() * 6f ), new Vector2( 3f + Next() * 6f, 1.5f + Next() * 2.5f ), 0.8f, random.Next() ),
						Palette.Hex( 0xE9E3D3 ), alpha: 0.35f + Next() * 0.25f, roughness: 0.95f );
			}
			if ( k == 4 || k == 5 )
			{
				for ( var s = 0; s < 14; s++ )
					painter.Polygon( Organic( new Vector2( x0 + Next() * 64f, y0 + 27f + Next() * 4f ), new Vector2( 3f + Next() * 5f, 1.5f + Next() * 3f ), 0.8f, random.Next() ),
						Mix( Moss, MossLight, Next() ), alpha: 0.5f, roughness: 0.95f );
			}
			if ( glazed )
			{
				// Glaze pools darker toward the foot and stops short of it, showing the clay.
				painter.Rect( x0, y0 + 22f, 64, 8, Mix( clay, Palette.Ink, 0.12f ), alpha: 0.6f );
				painter.Fill( x0, y0 + 29, 64, 3, Mix( Clay, ClayShade, 0.4f ), roughness: 0.85f );
			}
		}

		{
			var y0 = band + 64;
			for ( var k = 0; k < 2; k++ )
			{
				var x0 = k * 64;
				var soil = k == 0 ? Soil : Mix( Soil, Palette.Hex( 0x3D2E26 ), 0.6f );
				painter.Fill( x0, y0, 64, 32, soil, roughness: 0.96f );
				for ( var s = 0; s < 160; s++ )
					painter.Circle( x0 + Next() * 64f, y0 + Next() * 32f, 0.4f + Next() * 0.9f, Next() < 0.5f ? Mix( soil, Palette.Ink, 0.4f ) : Mix( soil, Palette.Hex( 0x8C7259 ), 0.5f ), 0.6f );
				// Perlite in the potting mix: a few white grains.
				for ( var s = 0; s < (k == 1 ? 26 : 8); s++ )
					painter.Circle( x0 + Next() * 64f, y0 + Next() * 32f, 0.5f + Next() * 0.4f, Palette.Hex( 0xE8E6DE ), 0.8f );
			}

			PaintWood( painter, 128, y0, Palette.Hex( 0xB48660 ), Palette.Hex( 0x7E5A3E ), random );
			PaintWood( painter, 192, y0, Palette.Hex( 0xA29A8B ), Palette.Hex( 0x6F6A60 ), random );

			// Galvanised zinc: a pale patchwork of spangle.
			painter.Fill( 256, y0, 64, 32, Palette.Hex( 0xB3B7B1 ), roughness: 0.42f );
			for ( var s = 0; s < 30; s++ )
				painter.Rect( 256f + Next() * 60f, y0 + Next() * 28f, 3f + Next() * 6f, 2f + Next() * 5f, Next() < 0.5f ? Palette.Hex( 0xC4C8C1 ) : Palette.Hex( 0x9EA39D ), alpha: 0.5f, roughness: 0.35f + Next() * 0.15f );

			// Painted can: sage-green enamel, worn to zinc in a few rubs.
			painter.Fill( 320, y0, 64, 32, Palette.Hex( 0x5F7F5B ), roughness: 0.5f );
			for ( var s = 0; s < 8; s++ )
				painter.Polygon( Organic( new Vector2( 320f + Next() * 64f, y0 + Next() * 32f ), new Vector2( 2f + Next() * 4f, 1f + Next() * 2f ), 0.9f, random.Next() ), Palette.Hex( 0xA4A9A2 ), alpha: 0.7f, roughness: 0.4f );

			// Jute: an open weave.
			painter.Fill( 384, y0, 64, 32, Palette.Hex( 0xB19A70 ), roughness: 0.95f );
			for ( var x = 0; x < 64; x += 3 ) painter.Fill( 384 + x, y0, 1, 32, Palette.Hex( 0x8F7955 ) );
			for ( var y = 1; y < 32; y += 3 ) painter.Rect( 384f, y0 + y, 64f, 1f, Palette.Hex( 0xC8B48B ), alpha: 0.6f );

			painter.Fill( 448, y0, 64, 32, Palette.Hex( 0x403B38 ), roughness: 0.6f );
			for ( var s = 0; s < 20; s++ )
				painter.Circle( 448f + Next() * 64f, y0 + Next() * 32f, 0.6f + Next() * 1.4f, Palette.Hex( 0x6A4A3A ), 0.4f );
		}

		{
			var y0 = band + 96;
			// Bark and stems: the run of the stem goes down the swatch.
			painter.Fill( 0, y0, 64, 32, Palette.Hex( 0x6F5F47 ), roughness: 0.9f );
			for ( var x = 0; x < 64; x += 2 + random.Next( 3 ) ) painter.Rect( x, y0, 1f, 32f, Palette.Hex( 0x544634 ), alpha: 0.6f );
			painter.Fill( 64, y0, 64, 32, Palette.Hex( 0xC6AE78 ), roughness: 0.45f );
			for ( var y = 3; y < 32; y += 9 ) painter.Fill( 64, y0 + y, 64, 1, Palette.Hex( 0x9C8455 ) );
			painter.Fill( 128, y0, 64, 32, Palette.Hex( 0x476B4D ), roughness: 0.22f );
			for ( var s = 0; s < 6; s++ ) painter.Circle( 128f + Next() * 64f, y0 + 26f + Next() * 6f, 0.5f + Next() * 0.8f, Palette.Hex( 0x2E2B2A ), 0.8f );
			painter.Fill( 192, y0, 64, 32, Palette.Hex( 0x76593E ), roughness: 0.97f );
			for ( var s = 0; s < 120; s++ )
				painter.Line( new Vector2( 192f + Next() * 64f, y0 + Next() * 32f ), new Vector2( 192f + Next() * 64f, y0 + Next() * 32f ), 0.5f, Palette.Hex( 0x93724F ), 0.2f );
			for ( var s = 0; s < 10; s++ )
				painter.Polygon( Organic( new Vector2( 192f + Next() * 64f, y0 + Next() * 14f ), new Vector2( 4f + Next() * 6f, 2f + Next() * 3f ), 0.8f, random.Next() ), Mix( Moss, MossLight, Next() * 0.6f ), alpha: 0.7f );
			painter.Fill( 256, y0, 64, 32, Palette.Hex( 0xE7E2D0 ), roughness: 0.25f );
			painter.Fill( 320, y0, 32, 32, Palette.Hex( 0xF1EAD2 ), roughness: 0.7f );
			painter.Fill( 352, y0, 32, 32, Palette.Hex( 0xE6C4BE ), roughness: 0.7f );
			painter.Fill( 384, y0, 32, 32, Palette.Hex( 0xEDEBE3 ), roughness: 0.6f );
			painter.Fill( 416, y0, 32, 32, Palette.Hex( 0x6D8650 ), roughness: 0.6f );
		}

		{
			// Massed foliage: small leaves crowded over a dark heart, lit ones on top of shaded ones.
			var y0 = band + 128;
			PaintPinnate( painter, 192, y0, Palette.Hex( 0x56804A ), 3.4f, 18f, random );
			var foliage = new[] { Palette.Hex( 0x4B7044 ), Palette.Hex( 0x6E964F ), Palette.Hex( 0x8A9C78 ) };
			for ( var k = 0; k < 3; k++ )
			{
				var x0 = k * 64;
				var green = foliage[k];
				painter.Fill( x0, y0, 64, 32, Mix( green, Palette.Ink, 0.45f ), roughness: 0.7f );
				for ( var layer = 0; layer < 3; layer++ )
				for ( var s = 0; s < 70; s++ )
				{
					var tone = Mix( Mix( green, Palette.Ink, 0.3f - layer * 0.15f ), Color.White, layer * 0.06f * Next() );
					painter.Polygon( Organic( new Vector2( x0 + 2f + Next() * 60f, y0 + 2f + Next() * 28f ), new Vector2( 2.2f + Next() * 1.6f, 1.2f + Next() * 0.8f ), 0.5f, random.Next(), Next() * MathF.Tau, 10 ),
						tone, roughness: 0.55f );
				}
			}
		}
	}

	/// <summary>
	/// A pinnate leaf along a swatch (base left, tip right): a pale rib down the middle and leaflets
	/// angled toward the tip either side of it, every <paramref name="pitch"/> px, each a little lighter at
	/// its centre than at its edges, dark gaps between — the frond's toothed outline does the rest.
	/// </summary>
	static void PaintPinnate( Painter painter, int x0, int y0, Color leaf, float pitch, float lean, Random random )
	{
		float Next() => (float)random.NextDouble();
		painter.Fill( x0, y0, 64, 32, Mix( leaf, Palette.Ink, 0.5f ), roughness: 0.6f );
		for ( var x = -6f; x < 66f; x += pitch * (0.85f + Next() * 0.3f) )
		{
			foreach ( var side in new[] { -1f, 1f } )
			{
				var root = new Vector2( x0 + x, y0 + 16f );
				var end = new Vector2( x0 + x + lean, y0 + 16f + side * 15.5f );
				var tone = Mix( leaf, Color.White, Next() * 0.12f );
				painter.Line( root, end, pitch * 0.62f, Mix( tone, Palette.Ink, 0.12f ), 1f, roughness: 0.5f );
				painter.Line( root, end, pitch * 0.3f, tone, 1f, roughness: 0.45f );
			}
		}
		painter.Line( new Vector2( x0, y0 + 16f ), new Vector2( x0 + 64f, y0 + 16f ), 1.6f, Mix( leaf, Palette.Hex( 0xD9DDB0 ), 0.5f ), 0.9f );
	}

	/// <summary>Sawn timber along the swatch: long late-wood lines that wander a little, the odd knot.</summary>
	static void PaintWood( Painter painter, int x0, int y0, Color wood, Color late, Random random )
	{
		float Next() => (float)random.NextDouble();
		painter.Fill( x0, y0, 64, 32, wood, roughness: 0.8f );
		for ( var y = 1f; y < 32f; y += 2f + Next() * 3f )
		{
			var drift = (Next() - 0.5f) * 3f;
			painter.Line( new Vector2( x0, y0 + y ), new Vector2( x0 + 64f, y0 + y + drift ), 0.6f + Next() * 0.6f, late, 0.25f + Next() * 0.3f );
		}
		painter.Circle( x0 + 10f + Next() * 44f, y0 + 6f + Next() * 20f, 1.2f + Next(), late, 0.7f );
	}

	protected override PaintedSurface PaintFloor()
	{
		const int cell = FloorSize / 2;
		var painter = new Painter( FloorSize, Mix( ClayShade, Flash, 0.6f ), 0.9f );
		var random = new Random( Seed );
		float Next() => (float)random.NextDouble();

		var cornerOffsets = new[] { new Vector2( 0, 0 ), new Vector2( cell, 0 ), new Vector2( cell, cell ), new Vector2( 0, cell ) };
		var sideA = new[] { new Vector2( 1, 0 ), new Vector2( -1, 0 ), new Vector2( -1, 0 ), new Vector2( 1, 0 ) };
		var sideB = new[] { new Vector2( 0, 1 ), new Vector2( 0, 1 ), new Vector2( 0, -1 ), new Vector2( 0, -1 ) };

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var tone = Next() * 2f - 1f;
			var body = tone < -0.2f ? Mix( Clay, ClayShade, (-tone - 0.2f) * 0.75f ) : Mix( Clay, ClayLight, (tone + 0.2f) * 0.3f );
			var joint = 3f + Next() * 4f;

			// The clay runs on over the arris (the outer Rim px land on the chamfer) and rolls down a
			// little toward the joint, which sits at the chamfer's foot in the clay's own deeper tone.
			painter.Fill( (int)min.x, (int)min.y, cell, cell, Mix( body, Flash, 0.45f ), height: 0.6f );
			painter.Polygon( SlabOutline( min, cell, joint, 2.5f, 6f + Next() * 12f, Seed + c * 101 ), body, height: 1f, bevel: Rim, roughness: 0.8f );

			// Grog: grains of sand and crushed fired clay in the body, darker and lighter than it.
			for ( var s = 0; s < 700; s++ )
			{
				var at = min + new Vector2( 10f + Next() * 492f, 10f + Next() * 492f );
				painter.Circle( at.x, at.y, 0.5f + Next() * 0.8f, Next() < 0.55f ? Flash : RawClay, 0.35f + Next() * 0.3f );
			}

			// Two sealed pools where the glaze went on thicker: deeper colour, much smoother, soft-edged
			// (three shrinking layers), so the sun puts a highlight on part of the tile, not all of it.
			for ( var g = 0; g < 2; g++ )
			{
				var centre = min + new Vector2( 150f + Next() * 212f, 150f + Next() * 212f );
				var half = new Vector2( 55f + Next() * 60f, 45f + Next() * 50f );
				var angle = Next() * MathF.Tau;
				var shapeSeed = Seed + c * 101 + g * 7 + 1;
				for ( var layer = 0; layer < 3; layer++ )
					painter.Polygon( Organic( centre, half * (1f - layer * 0.28f), 0.9f, shapeSeed, angle ), Mix( body, Flash, 0.1f ), alpha: 0.3f, roughness: 0.3f );
			}

			// Pitting: air pockets in hand-pressed clay.
			var pits = 25 + random.Next( 35 );
			for ( var p = 0; p < pits; p++ )
			{
				var at = min + new Vector2( 30f + Next() * 452f, 30f + Next() * 452f );
				var radius = 0.8f + Next() * Next() * 3f;
				painter.Circle( at.x, at.y, radius, Mix( body, Flash, 0.7f ), 0.8f, height: 0.8f, bevel: radius, roughness: 0.95f );
			}

			// A hairline crack across one of the four.
			if ( c == 0 )
			{
				var start = min + new Vector2( 14f, 150f + Next() * 220f );
				var crack = Wander( start, (Next() - 0.5f) * 0.8f, 16f, 12 + random.Next( 6 ), 0.35f, random );
				Polyline( painter, crack, 1.3f, Mix( body, Flash, 0.85f ), 0.8f, 0.82f, 0.95f );
				var branch = Wander( crack[crack.Length / 2], Next() * MathF.Tau, 10f, 4 + random.Next( 3 ), 0.5f, random );
				Polyline( painter, branch, 0.9f, Mix( body, Flash, 0.85f ), 0.65f, 0.86f, 0.95f );
			}

			// Chipped corners on variants 1 and 3 (a big one each, a second smaller one on 3).
			if ( c == 1 || c == 3 )
			{
				var k = c == 1 ? 2 : 0;
				// A break: sunk well below the face, raw and rough, with a sharp step the cavity bake darkens.
				var broken = Mix( Mix( body, RawClay, 0.3f ), Flash, 0.12f );
				painter.Polygon( ChipOutline( min + cornerOffsets[k], sideA[k], sideB[k], 70f + Next() * 45f, 60f + Next() * 50f, Seed + c * 13 ),
					broken, height: 0.35f, bevel: 2.5f, roughness: 0.96f );
				if ( c == 3 )
				{
					k = 1;
					painter.Polygon( ChipOutline( min + cornerOffsets[k], sideA[k], sideB[k], 36f + Next() * 16f, 32f + Next() * 16f, Seed + c * 17 ),
						broken, height: 0.4f, bevel: 2f, roughness: 0.96f );
				}
			}

			// Small nicks along the edges, a different count and place on every block.
			var nicks = 1 + random.Next( 3 );
			for ( var n = 0; n < nicks; n++ )
			{
				var side = random.Next( 4 );
				var along = side % 2 == 0 ? new Vector2( 1, 0 ) : new Vector2( 0, 1 );
				var into = side switch { 0 => new Vector2( 0, 1 ), 1 => new Vector2( -1, 0 ), 2 => new Vector2( 0, -1 ), _ => new Vector2( 1, 0 ) };
				var edge = side switch { 0 => min, 1 => min + new Vector2( cell, 0 ), 2 => min + new Vector2( 0, cell ), _ => min };
				var at = edge + along * (90f + Next() * 330f) + into * (joint + 6f);
				painter.Polygon( NickOutline( at, along, into, 9f + Next() * 14f, 7f + Next() * 10f, Seed + c * 29 + n ),
					Mix( Mix( body, RawClay, 0.3f ), Flash, 0.12f ), height: 0.45f, bevel: 2f, roughness: 0.96f );
			}

			// Moss and soil lodged at the block's foot, on the chamfer: soft cushions, not green dots.
			var tufts = c == 2 ? 0 : random.Next( 2 );
			for ( var t = 0; t < tufts; t++ )
			{
				var side = random.Next( 4 );
				var along = 40f + Next() * 430f;
				var size = 8f + Next() * 7f;
				var inset = size * 1.45f + 1f;
				var at = side switch
				{
					0 => min + new Vector2( along, inset ), 1 => min + new Vector2( cell - inset, along ),
					2 => min + new Vector2( along, cell - inset ), _ => min + new Vector2( inset, along ),
				};
				PaintTuft( painter, at, size, random );
			}
		}

		// Moss creeping out of one corner of variant 2, fuzzy and clumped.
		var mossCorner = CellOrigin( 2, FloorSize ) + new Vector2( 4f, 4f );
		var moss = Ramp( painter.Noise( 20, Seed + 40, 4 ), 0.3f, 0.55f );
		KeepNear( moss, FloorSize, mossCorner, 200f );
		var clumps = Ramp( painter.Noise( 96, Seed + 41, 2 ), 0.4f, 0.75f );
		for ( var i = 0; i < clumps.Length; i++ ) clumps[i] *= moss[i];
		painter.Blend( moss, Mix( Moss, Soil, 0.2f ), roughness: 0.97f );
		painter.Blend( clumps, Mix( MossLight, Moss, 0.4f ) );
		painter.AddHeight( moss, 0.15f );
		painter.AddHeight( clumps, 0.1f );

		// Kiln flashing and pale lime bloom: the colour drift of fired clay, a different map per tile.
		painter.Blend( Ramp( painter.Noise( 3, Seed + 10, 4 ), 0.46f, 0.8f, 0.55f ), Flash );
		painter.Blend( Ramp( painter.Noise( 5, Seed + 11, 4 ), 0.62f, 0.85f, 0.35f ), RawClay );

		painter.HeightNoise( 1f, 4, Seed + 12, 5 );
		painter.HeightNoise( 0.14f, 64, Seed + 13, 2 );
		painter.Mottle( 0.07f, 4, Seed + 14, 6 );
		painter.RoughnessNoise( 0.1f, 6, Seed + 15, 4 );
		painter.Cavity( 0.5f, 6 );
		painter.EdgeWear( 0.5f, 4, Seed + 16 );
		painter.Grain( 0.015f, Seed + 17 );
		return painter.Bake( "arena_0_floor", Painter.Matte, 8f );
	}

	/// <summary>A tuft of moss (or a crumb of soil): a few overlapping round cushions, each its own green.</summary>
	static void PaintTuft( Painter painter, Vector2 at, float size, Random random )
	{
		float Next() => (float)random.NextDouble();
		if ( Next() < 0.35f )
		{
			painter.Polygon( Organic( at, new Vector2( size, size * 0.7f ), 0.8f, random.Next(), Next() * MathF.Tau, 16 ), Soil, height: 0.95f, bevel: 2f, roughness: 0.97f );
			return;
		}

		var cushions = 6 + random.Next( 5 );
		for ( var k = 0; k < cushions; k++ )
		{
			var offset = new Vector2( Next() - 0.5f, Next() - 0.5f ) * size * 1.4f;
			var r = size * (0.35f + Next() * 0.35f);
			painter.Circle( at.x + offset.x, at.y + offset.y, r, Mix( Mix( Moss, Soil, 0.3f ), MossLight, Next() * 0.25f ), 0.7f, height: 1f + Next() * 0.08f, bevel: r, roughness: 0.97f );
		}
	}

	/// <summary>
	/// None: the greenhouse is roofed in glass (built as geometry with the room), and no tile
	/// underside ever shows this room's ceiling — floor 0's undersides wear the winter room's.
	/// </summary>
	protected override PaintedSurface PaintCeiling() => null;

	/// <summary>
	/// Builds the greenhouse's props into the floor's kit, in wall space: a point is (u along the wall,
	/// depth into it, v up), as <see cref="FloorContext.OnWall"/> takes them, and a direction likewise.
	/// Below <see cref="Clear"/> nothing reaches in front of <see cref="Front"/> (just inside the collider
	/// plane, which is the play volume's edge); above it a leaf or a lamp may reach <see cref="Overhang"/>
	/// out over the room. Every plant is grown from the wall's own stream, so none repeats another.
	/// </summary>
	sealed class Props : WallProps
	{
		/// <summary>Height of a pendant shade's mouth and how far out from the wall it hangs (its lamp hangs inside it).</summary>
		public const float PendantMouth = 228f;
		public const float PendantDepth = -9.5f;

		/// <summary>The skirting's face, which pots stand in front of.</summary>
		const float Skirt = FloorContext.Recess - 4.5f;

		readonly Material _trim;
		readonly Material _leaves;
		readonly Material _glow;

		public Props( FloorContext room, Material trim, Material leaves, Material glow ) : base( room )
		{
			_trim = trim;
			_leaves = leaves;
			_glow = glow;
		}

		/// <summary>A region of the trim atlas, as a skin for a prop.</summary>
		Skin TrimSkin( Rect region ) => new( _trim, region );

		/// <summary>A little under a standing player's eyes (64 over the tiles): foliage rooted lower is seen from above.</summary>
		const float EyeLevel = 56f;

		/// <summary>A leaf's skin: one-sided on the back-drawing copy below <see cref="EyeLevel"/>, the trim itself (modelled back to back) above it.</summary>
		Skin Foliage( Rect region, float v ) => new( v < EyeLevel ? _leaves : _trim, region );

		/// <summary>
		/// A leaf from <paramref name="root"/> toward <paramref name="direction"/>, its face toward the sky —
		/// or, <paramref name="onGlass"/>, laid against the glass facing the room, where its back is never
		/// seen: one-sided on the back-drawing copy at any height, half the faces. One that would reach out of
		/// bounds — in front of the bay below <see cref="WallProps.Clear"/>, too far over the room above it —
		/// is turned back toward the glass, and if its curl or its width still carry it out, grown smaller; one that would go through
		/// the glass is shortened. A broad leaf gets a third segment, to curve.
		/// </summary>
		void Leaf( Vector3 root, Vector3 direction, float length, float width, float bend, float fold, Rect skin, float tip = 0.1f, float widest = 0.45f, bool onGlass = false )
		{
			direction = direction.Normal;
			var up = onGlass ? new Vector3( 0f, -1f, 0f ) : default;
			var roll = Range( -25f, 25f );
			// A little over a unit spare for the curl between the points Reach samples and the roll of the edges.
			var nearest = Nearest( MathF.Min( root.z, root.z + direction.z * length ) ) + 1.2f;
			var aim = Aim( direction, roll, up );
			if ( Reach( root, aim, length, width, bend ) < nearest )
			{
				direction = direction.WithY( MathF.Abs( direction.y ) );
				aim = Aim( direction, roll, up );
			}
			var low = Reach( root, aim, length, width, bend );
			if ( low < nearest )
			{
				var scale = Math.Clamp( (root.y - nearest) / MathF.Max( root.y - low, 0.01f ), 0.15f, 1f );
				length *= scale;
				width *= scale;
			}
			if ( direction.y > 0f && root.y + direction.y * length > Face ) length = MathF.Max( 0.6f, (Face - root.y) / direction.y );
			// A third segment only where a leaf is big enough for its curl to show.
			var segments = length > 14f ? 3 : 2;
			var single = onGlass || root.z < EyeLevel;
			Kit.Blade( new Skin( single ? _leaves : _trim, skin ), At( root ), Turn( aim ), length, width, bend, fold, segments, tip, widest, single );
		}

		/// <summary>
		/// The nearest depth a blade aimed by <paramref name="aim"/> comes to: its midline follows the same
		/// arc <see cref="MeshKit.Blade"/> bends it along, and either edge sits half its width to the side.
		/// </summary>
		static float Reach( Vector3 root, Rotation aim, float length, float width, float bend )
		{
			var nearest = root.y;
			foreach ( var t in new[] { 0.3f, 0.5f, 0.75f, 1f } )
			{
				var angle = bend * t;
				var along = MathF.Abs( bend ) < 0.001f ? length * t : length * MathF.Sin( angle ) / bend;
				var drop = MathF.Abs( bend ) < 0.001f ? 0f : length * (1f - MathF.Cos( angle )) / bend;
				var centre = root + aim.Forward * along - aim.Up * drop;
				var side = MathF.Abs( aim.Left.y ) * width * 0.5f;
				nearest = MathF.Min( nearest, centre.y - side );
			}
			return nearest;
		}

		// ---- Pots ------------------------------------------------------------------------------

		/// <summary>
		/// A flowerpot on <paramref name="foot"/> with a rim of radius <paramref name="r"/>: the classic
		/// tapered clay pot with a thick rolled rim, or (glazed) a bellied pot with a small lip. Returns the
		/// middle of its soil — or, empty, of its floor. Its texture's seam is turned to the glass. A pot
		/// nested under another in a stack (<paramref name="open"/> false) has no inside: none of it shows.
		/// </summary>
		Vector3 Pot( Vector3 foot, float r, int clay, bool soil, bool open = true )
		{
			var glazed = clay >= 6;
			var h = r * (glazed ? Range( 1.15f, 1.4f ) : Range( 1.4f, 1.8f ));
			var t = r * 0.09f;
			var rim = h * Range( 0.14f, 0.19f );
			var fill = h - r * Range( 0.14f, 0.28f );
			var floor = h * 0.12f;
			var inner = (glazed ? 0.88f : 1.04f) * r - t * 1.3f;
			var profile = glazed
				? new[]
				{
					new Vector2( r * 0.62f, 0f ), new Vector2( r * 0.95f, h * 0.36f ), new Vector2( r * 1.0f, h * 0.56f ),
					new Vector2( r * 0.86f, h * 0.9f ), new Vector2( r * 0.9f, h ), new Vector2( r * 0.9f - t, h ),
					new Vector2( inner, soil ? fill : floor ),
				}
				: new[]
				{
					new Vector2( r * 0.72f, 0f ), new Vector2( r * 0.95f, h - rim ), new Vector2( r * 1.04f, h - rim + h * 0.03f ),
					new Vector2( r * 1.04f, h ), new Vector2( r * 1.04f - t, h ), new Vector2( inner, soil ? fill : floor ),
				};

			// Smooth-shaded, so a pot reads round on few sides; a big tub gets a few more for its silhouette.
			var sides = Math.Clamp( (int)(r * 0.7f), 6, 11 );
			if ( !open ) profile = profile[..^1];
			// A small pot's rim is under a unit thick: from the floor its top and its inside read as one.
			else if ( r < 8f ) { profile[^2] = profile[^1]; profile = profile[..^1]; }
			Kit.SmoothLathe( TrimSkin( PotSwatch( clay ) ), At( foot ), profile, sides, Run, false, false, 40f, 0.25f + Range( -0.1f, 0.1f ) );

			var middle = foot + Vector3.Up * (soil ? fill : floor);
			if ( open ) Kit.Dome( TrimSkin( SoilSwatch( soil ? Pick( 2 ) : 1 ) ), At( middle ), soil ? new Vector3( inner, inner, r * 0.07f ) : new Vector3( r * 0.66f, r * 0.66f, 0.3f ), Run, 1, sides );
			return middle + Vector3.Up * (soil ? r * 0.05f : 0.2f);
		}

		/// <summary>A clay colour for a pot: mostly plain clays, some bloomed or mossy, a few glazed.</summary>
		int Clay( bool allowGlaze = true )
		{
			var p = Next();
			return allowGlaze && p > 0.88f ? 6 + Pick( 2 ) : p < 0.55f ? Pick( 3 ) : 3 + Pick( 3 );
		}

		/// <summary>Empty pots nested in a stack, each a little crooked on the one below.</summary>
		void PotStack( Vector3 foot, float r, int count )
		{
			var clay = Clay( false );
			for ( var i = 0; i < count; i++ )
				Pot( foot + new Vector3( Range( -0.3f, 0.3f ), Range( -0.3f, 0.3f ), i * r * 0.36f ), r, clay, false, i == count - 1 );
		}

		// ---- Plants ----------------------------------------------------------------------------

		enum Plant { Bushy, Strap, Shrub, Fern, Tall, BigLeaf, Succulent }

		void Grow( Plant plant, Vector3 at, float size )
		{
			Apart( () =>
			{
				switch ( plant )
				{
					case Plant.Bushy: Bushy( at, size, Next() < 0.35f ); break;
					case Plant.Strap: Strap( at, size ); break;
					case Plant.Shrub: Shrub( at, size, size > 18f && Next() < 0.6f ); break;
					case Plant.Fern: Fern( at, size, false ); break;
					case Plant.Tall: Tall( at, size ); break;
					case Plant.BigLeaf: BigLeaf( at, size ); break;
					default: Succulent( at, size ); break;
				}
			} );
		}

		/// <summary>A soft-stemmed leafy plant (a pelargonium, basil): several leaning stems with broad leaves at the nodes, some in flower.</summary>
		void Bushy( Vector3 at, float size, bool flowers )
		{
			var leaf = LeafSwatch( Pick( 3 ) );
			var flower = FlowerSwatch( Next() < 0.7f ? 1 : 0 );
			var stems = 3 + Pick( 4 );
			var spin = Next() * MathF.Tau;
			for ( var s = 0; s < stems; s++ )
			{
				var yaw = spin + s * MathF.Tau / stems + Range( -0.4f, 0.4f );
				var lean = Range( 0.25f, 0.75f );
				var height = size * Range( 0.55f, 1f );
				var path = new Vector3[3];
				for ( var i = 0; i < 3; i++ )
				{
					var t = i / 2f;
					path[i] = at + Flat( yaw ) * (lean * height * t * t) + Vector3.Up * (height * t);
				}
				Inside( path, 0.2f + size * 0.012f );
				Kit.Tube( TrimSkin( StemSwatch ), At( path ), 0.2f + size * 0.012f, 3, 0.12f );

				var nodes = 2 + Pick( 3 );
				for ( var n = 0; n < nodes; n++ )
				{
					var node = Along( path, 0.25f + 0.75f * (n + 1) / nodes );
					var length = size * Range( 0.3f, 0.45f );
					Leaf( node, Flat( yaw + Range( -1.3f, 1.3f ) ) + Vector3.Up * Range( 0.1f, 0.7f ), length, length * Range( 0.75f, 0.95f ), Range( 0.3f, 0.7f ), -0.15f, leaf, 0.35f, 0.5f );
				}

				if ( !flowers || Next() > 0.7f ) continue;
				var head = Range( 1.2f, 1.8f ) * (0.6f + size * 0.02f);
				// The head kept as far behind the play volume's edge as its own radius (the stem tip may sit right on it).
				var bloom = path[^1] + Vector3.Up * head * 0.6f;
				bloom = bloom.WithY( MathF.Max( bloom.y, Nearest( bloom.z ) + head ) );
				Kit.Ellipsoid( TrimSkin( flower ), At( bloom ), new Vector3( head, head * Range( 0.8f, 1f ), head * 0.7f ),
					Turn( Rotation.FromYaw( Next() * 180f ) ), 3, 6 );
			}
		}

		/// <summary>A rosette of arching strap leaves: a spider plant (variegated), a clivia, a young leek.</summary>
		void Strap( Vector3 at, float size )
		{
			var leaf = LeafSwatch( Next() < 0.4f ? 4 : Next() < 0.5f ? 1 : 7 );
			var count = 7 + Pick( 6 );
			var spin = Next() * MathF.Tau;
			for ( var i = 0; i < count; i++ )
			{
				var yaw = spin + i * 2.4f + Range( -0.2f, 0.2f );
				var pitch = Range( 0.45f, 1.2f );
				var direction = Flat( yaw ) * MathF.Cos( pitch ) + Vector3.Up * MathF.Sin( pitch );
				Leaf( at + Vector3.Up * 0.4f, direction, size * Range( 0.7f, 1.2f ), size * Range( 0.08f, 0.13f ), Range( 0.7f, 1.5f ), 0.3f, leaf, 0f, 0.25f );
			}
		}

		/// <summary>
		/// A clipped box ball, or (as a <paramref name="tree"/>) a small standard on a trunk: clumps of
		/// flattened spheres in two greens, with single leaves breaking the outline.
		/// </summary>
		void Shrub( Vector3 at, float size, bool tree )
		{
			var radius = size * (tree ? 0.3f : 0.38f);
			radius = MathF.Min( radius, MathF.Min( at.y - Front, Face - at.y ) * 1.1f );
			var trunk = tree ? size - radius * 1.7f : radius * 0.35f;
			var centre = at + Vector3.Up * (trunk + radius * 0.8f);
			if ( tree )
				Kit.Tube( TrimSkin( BarkSwatch ), At( new[] { at - Vector3.Up, at + new Vector3( Range( -0.6f, 0.6f ), Range( -0.6f, 0.6f ), trunk * 0.5f ), centre } ), 0.55f + size * 0.012f, 5, 0.35f );

			var greens = new[] { FoliageSwatch( 0 ), FoliageSwatch( tree ? 2 : 1 ) };
			var sprigLeaves = new[] { LeafSwatch( 1 ), LeafSwatch( tree ? 6 : 0 ) };
			var clumps = 7 + Pick( 4 );
			for ( var c = 0; c < clumps; c++ )
			{
				var direction = new Vector3( Range( -1f, 1f ), Range( -1f, 1f ), Range( -0.5f, 1f ) ).Normal;
				var r = radius * Range( 0.38f, 0.55f );
				var place = centre + direction * radius * Range( 0.35f, 0.7f );
				place = place.WithY( Math.Clamp( place.y, Front + r, Face - r * 0.5f ) );
				Kit.Ellipsoid( TrimSkin( greens[c % 2] ), At( place ), new Vector3( r, r * Range( 0.8f, 1f ), r * Range( 0.6f, 0.85f ) ),
					Turn( Rotation.From( Range( -20f, 20f ), Next() * 360f, Range( -20f, 20f ) ) ), 3, 6 );
			}

			var sprigs = 5 + Pick( 5 );
			for ( var s = 0; s < sprigs; s++ )
			{
				var direction = new Vector3( Range( -1f, 1f ), Range( -1f, 1f ), Range( -0.3f, 1f ) ).Normal;
				Leaf( centre + direction * radius * 0.75f, direction + Vector3.Up * 0.3f, size * Range( 0.1f, 0.15f ), size * Range( 0.05f, 0.08f ), 0.3f, 0.2f, sprigLeaves[s % 2], 0.1f, 0.45f );
			}
		}

		/// <summary>How far a frond's leaflets are lifted off its rib, as a share of their length: a shallow V.</summary>
		const float Fold = 0.3f;

		/// <summary>
		/// A fern: fronds rising and arching over (or, <paramref name="hanging"/> from a basket, spilling down
		/// over its rim) — see <see cref="Frond"/>.
		/// </summary>
		void Fern( Vector3 at, float size, bool hanging )
		{
			var fronds = (hanging ? 8 : 7) + Pick( 3 );
			var spin = Next() * MathF.Tau;
			for ( var f = 0; f < fronds; f++ )
			{
				var rise = hanging ? Range( 0.2f, 0.9f ) : Range( 0.7f, 1.3f );
				Frond( at, spin + f * 2.4f + Range( -0.25f, 0.25f ), rise, rise + Range( 1.1f, 1.9f ), size * Range( 0.7f, 1.15f ), Range( 0.24f, 0.3f ), LeafSwatch( 5 ), 1.5f, 0.6f, 1f );
			}
		}

		/// <summary>
		/// One pinnate frond — a fern's, a palm's: a rib from <paramref name="root"/> heading
		/// <paramref name="yaw"/>, rising at <paramref name="rise"/> radians and arching over by
		/// <paramref name="bend"/>, with a pair of leaflets every 2 × <paramref name="leaflet"/> units or so,
		/// swept toward the tip by <paramref name="sweep"/> of their reach and standing on
		/// <paramref name="spread"/> of their share of the rib (see <see cref="MeshKit.Pinnate"/>; a pinnate
		/// texture does the rest). Grown shorter where the arch would carry it out of bounds: in front of the
		/// bay below <see cref="Clear"/>, further than <see cref="Overhang"/> over the room above it, through
		/// the glass, or down past <see cref="Clear"/> while out over the room.
		/// </summary>
		void Frond( Vector3 root, float yaw, float rise, float bend, float length, float width, Rect skin, float leaflet, float sweep, float spread )
		{
			// The arch is a circle: its reach and its drop per unit of length follow from where it starts and how far it turns.
			var reach = (MathF.Sin( rise ) - MathF.Sin( rise - bend )) / bend;
			var drop = MathF.Max( 0f, -(MathF.Cos( rise - bend ) - MathF.Cos( rise )) / bend );
			var heading = Flat( yaw );
			var high = root.z >= Clear;
			var nearest = high ? -Overhang : Front;
			// A leaflet's point stands half the frond's width (a share of its length) to the side of the rib, and
			// a little more lifted off it by the fold.
			if ( heading.y < -0.05f ) length = MathF.Min( length, (root.y - nearest - 2f) / (-heading.y * reach + width * 0.5f * (1f + Fold)) );
			if ( heading.y > 0.05f ) length = MathF.Min( length, (Face - root.y - 1f) / (heading.y * reach) );
			if ( high && drop > 0f ) length = MathF.Min( length, (root.z - Clear - 2f) / drop );
			if ( length < 4f ) return;

			var direction = heading * MathF.Cos( rise ) + Vector3.Up * MathF.Sin( rise );
			Kit.Pinnate( Foliage( skin, root.z ), At( root ), Turn( Aim( direction, Range( -12f, 12f ) ) ), length, length * width, bend,
				Math.Clamp( (int)(length / (leaflet * 2f)), 6, 16 ), Fold, sweep, spread, 0.08f, 0.35f, root.z < EyeLevel );
		}

		/// <summary>A tall leafy plant tied to a cane (a young tomato, a fig cutting): a stem winding up beside the cane, leaves alternating up it.</summary>
		void Tall( Vector3 at, float size )
		{
			var cane = at + new Vector3( Range( -1.5f, 1.5f ), 0f, 0f );
			Kit.Tube( TrimSkin( CaneSwatch ), At( new[] { cane - Vector3.Up, cane + new Vector3( Range( -0.8f, 0.8f ), Range( -0.5f, 0.5f ), size * 1.15f ) } ), 0.3f, 5 );
			var path = new Vector3[4];
			for ( var i = 0; i < path.Length; i++ )
			{
				var t = i / (path.Length - 1f);
				path[i] = at + new Vector3( MathF.Sin( t * 5f ) * 1.2f, MathF.Cos( t * 4f ) * 0.8f, size * t );
			}
			Inside( path, 0.32f );
			Kit.Tube( TrimSkin( StemSwatch ), At( path ), 0.32f, 3, 0.18f );

			var leaf = LeafSwatch( Pick( 2 ) == 0 ? 1 : 0 );
			var yaw = Next() * MathF.Tau;
			for ( var height = size * 0.12f; height < size; height += Range( 3.2f, 4.6f ) )
			{
				yaw += MathF.PI + Range( -0.6f, 0.6f );
				var node = Along( path, height / size );
				var length = size * Range( 0.29f, 0.4f ) * (1.15f - height / size * 0.45f);
				Leaf( node, Flat( yaw ) + Vector3.Up * Range( 0f, 0.5f ), length, length * Range( 0.45f, 0.6f ), Range( 0.5f, 0.9f ), 0.2f, leaf, 0.1f, 0.4f );
			}
		}

		/// <summary>
		/// A big-leaved plant (a canna, a hosta, a young banana): a handful of broad cupped leaves held out on
		/// long stalks — most of a bed's green mass for very few faces.
		/// </summary>
		void BigLeaf( Vector3 at, float size )
		{
			var leaf = LeafSwatch( new[] { 0, 1, 6, 4, 2 }[Pick( 5 )] );
			var count = 5 + Pick( 5 );
			var spin = Next() * MathF.Tau;
			for ( var i = 0; i < count; i++ )
			{
				var yaw = spin + i * 2.4f + Range( -0.3f, 0.3f );
				var rise = Range( 0.7f, 1.35f );
				var stalk = size * Range( 0.2f, 0.45f );
				var path = new[] { at, at + Flat( yaw ) * (stalk * MathF.Cos( rise )) + Vector3.Up * (stalk * MathF.Sin( rise )) };
				Inside( path, 0.25f + size * 0.008f );
				Kit.Tube( TrimSkin( StemSwatch ), At( path ), 0.25f + size * 0.008f, 3, 0.18f );
				var length = size * Range( 0.4f, 0.6f );
				Leaf( path[^1], Flat( yaw + Range( -0.3f, 0.3f ) ) + Vector3.Up * Range( -0.1f, 0.6f ), length, length * Range( 0.5f, 0.68f ), Range( 0.4f, 0.9f ), -0.25f, leaf, 0.1f, 0.45f );
			}
		}

		/// <summary>
		/// A clump palm (a bamboo palm, a kentia) in a tub: two to four ringed stems of different heights,
		/// each crowned with arching feather fronds high up the glass, where they may lean out over the room.
		/// </summary>
		void Palm( Vector3 at )
		{
			var stems = 2 + Pick( 2 );
			for ( var k = 0; k < stems; k++ )
			{
				var crown = at + new Vector3( Range( -14f, 14f ), Range( -3f, 3f ), 0f );
				crown = crown.WithZ( Range( 185f, 220f ) - k * Range( 10f, 25f ) );
				crown = crown.WithY( Math.Clamp( crown.y, Front + 2f, Face - 3f ) );
				var bow = new Vector3( Range( -4f, 4f ), Range( -1.5f, 1.5f ), 0f );
				var stem = new[] { at + new Vector3( Range( -2f, 2f ), Range( -1f, 1f ), -1f ), Vector3.Lerp( at, crown, 0.5f ) + bow, crown };
				var girth = Range( 1.6f, 2.1f );
				Inside( stem, girth );
				Kit.Tube( TrimSkin( CaneSwatch ), At( stem ), girth, 6, 1f );

				// Older fronds lower down the stem, held out level and kept inside the bay.
				for ( var low = 1 + Pick( 2 ); low > 0; low-- )
				{
					var rise = Range( 0.3f, 0.8f );
					Frond( Along( stem, Range( 0.45f, 0.75f ) ), Next() * MathF.Tau, rise, rise + Range( 1f, 1.6f ), Range( 26f, 36f ), Range( 0.3f, 0.36f ), PalmSwatch, 1.6f, 0.8f, 0.7f );
				}

				var fronds = 8 + Pick( 4 );
				var spin = Next() * MathF.Tau;
				for ( var f = 0; f < fronds; f++ )
				{
					var rise = Range( 0.6f, 1.3f );
					Frond( crown, spin + f * MathF.Tau / fronds + Range( -0.3f, 0.3f ), rise, rise + Range( 0.9f, 1.6f ), Range( 38f, 52f ), Range( 0.3f, 0.36f ), PalmSwatch, 1.6f, 0.8f, 0.7f );
				}
			}
		}

		/// <summary>A succulent rosette (an echeveria): fleshy glaucous leaves, upright in the heart and opening outward.</summary>
		void Succulent( Vector3 at, float size )
		{
			var count = 10 + Pick( 6 );
			var spin = Next() * MathF.Tau;
			for ( var i = 0; i < count; i++ )
			{
				var t = i / (float)count;
				var pitch = 1.35f - t * 1.05f;
				var length = size * (0.25f + 0.35f * (1f - t));
				var direction = Flat( spin + i * 2.4f ) * MathF.Cos( pitch ) + Vector3.Up * MathF.Sin( pitch );
				Leaf( at + Vector3.Up * (0.2f + t * 0.6f), direction, length, length * Range( 0.55f, 0.7f ), -0.35f, 0.45f, LeafSwatch( 3 ), 0.2f, 0.45f );
			}
		}

		/// <summary>Trailing ivy spilling over a pot's rim and down the front of a shelf, small leaves along each strand.</summary>
		void Trail( Vector3 rim, float length )
		{
			var strands = 2 + Pick( 3 );
			var leaf = LeafSwatch( Next() < 0.5f ? 1 : 6 );
			for ( var s = 0; s < strands; s++ )
			{
				var out1 = new Vector3( Range( -1.5f, 1.5f ), -Range( 1f, 2.5f ), 0.4f );
				var drop = length * Range( 0.5f, 1f );
				var sway = Range( -4f, 4f );
				var path = new Vector3[5];
				path[0] = rim + new Vector3( Range( -1f, 1f ), 0f, 0f );
				path[1] = path[0] + out1;
				for ( var i = 2; i < path.Length; i++ )
				{
					var t = (i - 1) / (path.Length - 2f);
					path[i] = path[1] + new Vector3( sway * t * t + Range( -0.4f, 0.4f ), -t * 2f, -drop * t );
				}
				Inside( path, 0.13f );
				Kit.Tube( TrimSkin( StemSwatch ), At( path ), 0.13f, 3 );
				for ( var d = 1.2f; d < drop; d += Range( 2.8f, 3.8f ) )
				{
					var node = Along( path, 0.15f + 0.85f * d / drop );
					Leaf( node, new Vector3( Range( -1f, 1f ), -Range( 0.2f, 1f ), Range( -0.3f, 0.4f ) ), Range( 1.9f, 2.8f ), Range( 1.9f, 2.6f ), 0.2f, -0.1f, leaf, 0.15f, 0.35f );
				}
			}
		}

		/// <summary>
		/// A climber trained up the glass (a passion flower, a grape): a stem wandering up between the bars
		/// to somewhere under the eaves, a side shoot or two, broad leaves laid flat to the glass.
		/// </summary>
		void Vine( Vector3 foot, float top )
		{
			var leaf = LeafSwatch( Next() < 0.5f ? 1 : 0 );
			var height = top - foot.z;
			var phase = Next() * MathF.Tau;
			var sway = Range( 6f, 16f );
			var drift = Range( -30f, 30f );
			var count = Math.Max( 4, (int)(height / 18f) );
			var stem = new Vector3[count + 1];
			for ( var i = 0; i <= count; i++ )
			{
				var t = i / (float)count;
				stem[i] = new Vector3( foot.x + MathF.Sin( t * 5f + phase ) * sway * t + drift * t, MathF.Min( foot.y + 4f * t, Face - 0.8f ), foot.z + height * t );
			}

			var shoots = new List<Vector3[]> { stem };
			for ( var b = Pick( 3 ); b > 0; b-- )
			{
				var from = Along( stem, Range( 0.3f, 0.75f ) );
				var heading = Next() < 0.5f ? -1f : 1f;
				var length = Range( 25f, 55f );
				var shoot = new Vector3[5];
				for ( var i = 0; i < 5; i++ )
				{
					var t = i / 4f;
					shoot[i] = from + new Vector3( heading * length * t, 0f, length * (0.7f * t - 0.25f * t * t) );
				}
				shoots.Add( shoot );
			}

			foreach ( var path in shoots )
			{
				Kit.Tube( TrimSkin( StemSwatch ), At( path ), path == stem ? 0.5f : 0.35f, 3, 0.2f );
				var span = 0f;
				for ( var i = 1; i < path.Length; i++ ) span += (path[i] - path[i - 1]).Length;
				var flip = 1f;
				for ( var d = Range( 3f, 6f ); d < span; d += Range( 7.5f, 10.5f ) )
				{
					flip = -flip;
					var length = Range( 7f, 11.5f ) * (path == stem ? 1f : 0.8f);
					Leaf( Along( path, d / span ), new Vector3( flip * Range( 0.6f, 1f ), -Range( 0.05f, 0.2f ), Range( -0.7f, 0.2f ) ), length, length * Range( 0.8f, 0.95f ),
						0.25f, -0.1f, leaf, 0.25f, 0.45f, true );
				}
			}
		}

		/// <summary>A seedling: two seed leaves opening from the soil, one a little ahead of the other.</summary>
		void Seedling( Vector3 at, float size, Rect leaf )
		{
			var yaw = Next() * MathF.Tau;
			for ( var s = 0; s < 2; s++ )
				Leaf( at, Flat( yaw + s * MathF.PI + Range( -0.3f, 0.3f ) ) + Vector3.Up * Range( 0.8f, 1.6f ), size * Range( 0.8f, 1.2f ), size * 0.6f, 0.9f, 0.1f, leaf, 0.3f, 0.6f );
		}

		/// <summary>A white plant label pushed into the soil at a slant.</summary>
		void Label( Vector3 at )
		{
			Kit.Box( TrimSkin( LabelSwatch ), At( at + Vector3.Up * 1.4f ), new Vector3( 0.2f, 1.1f, 3.4f ),
				Turn( Rotation.From( Range( -14f, 14f ), Next() * 360f, Range( -10f, 10f ) ) ) );
		}

		// ---- Bays ------------------------------------------------------------------------------

		/// <summary>
		/// A raised bed the length of the bay: a painted timber trough (the trim's skirting board for its
		/// face, beaded along the top) with a cap rail, soil heaped unevenly, and a row of mixed planting.
		/// </summary>
		public void RaisedBed( Bay bay )
		{
			var u0 = bay.From + Range( 2f, 5f );
			var u1 = bay.To - Range( 2f, 5f );
			var top = Range( 15f, 23f );
			var front = Front + Range( 0f, 3f );
			Room.WallBox( Side, _trim, TrimRegion( TrimBand.Skirting ), TrimRegion( TrimBand.Stock ), false, u0, u1, Ledge, top, front, Skirt, BoxFaces.Front | BoxFaces.Ends );
			var stock = TrimSkin( TrimRegion( TrimBand.Stock ) );
			Kit.BevelBox( stock, At( new Vector3( (u0 + u1) * 0.5f, front + 1.3f, top + 0.5f ) ), new Vector3( u1 - u0 + 1f, 2.6f, 1.2f ), 0.3f, Run );

			var soil = top - 1.6f;
			Kit.BevelBox( TrimSkin( SoilSwatch( 0 ) ), At( new Vector3( (u0 + u1) * 0.5f, (front + Skirt) * 0.5f, soil - 1f ) ), new Vector3( u1 - u0 - 0.6f, Skirt - front - 0.4f, 2f ), 0.4f, Run );
			for ( var l = (int)((u1 - u0) / 22f); l > 0; l-- )
			{
				var r = Range( 2f, 5f );
				Kit.Dome( TrimSkin( SoilSwatch( Pick( 2 ) ) ), At( new Vector3( Range( u0 + r, u1 - r ), Range( front + r + 1f, Skirt - r ), soil ) ), new Vector3( r, r * Range( 0.6f, 1f ), r * 0.22f ),
					Turn( Rotation.FromYaw( Next() * 180f ) ), 1, 6 );
			}

			var previous = (Plant)(-1);
			for ( var u = u0 + Range( 6f, 12f ); u < u1 - 8f; )
			{
				var plant = (Plant)Pick( 6 );
				if ( plant == previous ) plant = (Plant)(((int)plant + 1 + Pick( 5 )) % 6);
				previous = plant;
				var size = plant switch { Plant.Tall => Range( 44f, 66f ), Plant.BigLeaf => Range( 40f, 56f ), _ => Range( 26f, 40f ) };
				Grow( plant, new Vector3( u, Range( 15f, 23f ), soil ), size );
				if ( Next() < 0.3f ) Label( new Vector3( u + Range( 3f, 5f ), front + Range( 3f, 6f ), soil ) );
				u += size * Range( 0.35f, 0.55f ) + Range( 1f, 3f );
			}

			// Low cushions along the front, some spilling over the rail.
			for ( var u = u0 + Range( 3f, 10f ); u < u1 - 4f; u += Range( 17f, 32f ) )
			{
				var r = new Vector3( Range( 4f, 8f ), Range( 3f, 5f ), Range( 2.2f, 3.6f ) );
				Kit.Ellipsoid( TrimSkin( FoliageSwatch( Pick( 3 ) ) ), At( new Vector3( u, front + r.y + 0.3f, soil + r.z * 0.4f ) ), r, Turn( Rotation.FromYaw( Range( -15f, 15f ) ) ), 3, 7 );
			}

			var vine = new Vector3( Range( u0 + 8f, u1 - 8f ), Face - 3f, soil );
			var reach = Range( 150f, 235f );
			Apart( () => Vine( vine, reach ) );
		}

		/// <summary>
		/// A slatted potting bench: four slats on legs with an apron, a lower shelf of slats; on top pots,
		/// seed trays, a stack of empties, now and then the watering can; under it spare pots or a sack.
		/// </summary>
		public void Bench( Bay bay )
		{
			var u0 = bay.From + Range( 3f, 9f );
			var u1 = bay.To - Range( 3f, 9f );
			var length = u1 - u0;
			var mid = (u0 + u1) * 0.5f;
			var top = Range( 29f, 35f );
			var wood = TrimSkin( Next() < 0.5f ? CedarSwatch : GreyWoodSwatch );
			const float D0 = 4f;
			const float D1 = 36f;
			const int Slats = 4;
			const float Pitch = (D1 - D0) / Slats;

			for ( var s = 0; s < Slats; s++ )
				Kit.BevelBox( wood, At( new Vector3( mid + Range( -0.6f, 0.6f ), D0 + Pitch * (s + 0.5f), top - 0.9f ) ), new Vector3( length + Range( -1f, 1f ), Pitch - 0.8f, 1.8f ), 0.35f,
					Turn( Rotation.FromYaw( Range( -0.4f, 0.4f ) ) ) );
			Kit.BevelBox( wood, At( new Vector3( mid, D0 + 0.8f, top - 3.6f ) ), new Vector3( length - 1f, 1.6f, 3.6f ), 0.3f, Run );
			foreach ( var lu in new[] { u0 + 2f, u1 - 2f } )
			foreach ( var ld in new[] { D0 + 1.5f, D1 - 1.5f } )
				Kit.Box( wood, At( new Vector3( lu, ld, (Ledge + top - 1.8f) * 0.5f ) ), new Vector3( 2.6f, 2.6f, top - 1.8f - Ledge ), Run );
			for ( var s = 0; s < 3; s++ )
				Kit.Box( wood, At( new Vector3( mid, D0 + 4f + s * 9.6f, 5f ) ), new Vector3( length - 4f, 8.8f, 1.5f ), Run );

			var surface = top;
			var can = Next() < 0.6f;
			for ( var u = u0 + Range( 2f, 6f ); u < u1 - 6f; )
			{
				var p = Next();
				if ( can && u < u1 - 18f && p < 0.35f )
				{
					can = false;
					var scale = Range( 0.85f, 1.1f );
					WateringCan( new Vector3( u + 6f * scale, Range( 18f, 22f ), surface ), scale, Next() < 0.5f ? Range( -35f, 35f ) : 180f + Range( -35f, 35f ) );
					u += 18f * scale + Range( 2f, 5f );
				}
				else if ( p < 0.6f && u < u1 - 14f )
				{
					var trayLength = Math.Min( Range( 14f, 20f ), u1 - u - 1f );
					SeedTray( new Vector3( u + trayLength * 0.5f, Range( 16f, 24f ), surface ), trayLength );
					u += trayLength + Range( 1f, 4f );
				}
				else if ( p < 0.75f )
				{
					var r = Range( 3.5f, 5f );
					PotStack( new Vector3( u + r, Range( 14f, 28f ), surface ), r, 2 + Pick( 3 ) );
					u += 2f * r + Range( 1f, 4f );
				}
				else
				{
					var r = Range( 4.5f, 7.5f );
					var soil = Pot( new Vector3( u + r, Range( 12f + r, 30f - r * 0.5f ), surface ), r, Clay(), true );
					var plant = (Plant)new[] { 0, 1, 3, 6 }[Pick( 4 )];
					Grow( plant, soil, r * (plant == Plant.Succulent ? Range( 1.5f, 2f ) : Range( 2.4f, 3.4f )) );
					u += 2f * r + Range( 1f, 4f );
				}
			}

			// Under the bench: spare pots on the shelf, or a sack of compost slumped on the ledge.
			if ( Next() < 0.45f )
			{
				var r = Range( 4f, 6f );
				PotStack( new Vector3( u0 + Range( 6f, length * 0.5f ), Range( 16f, 24f ), 5.75f ), r, 2 + Pick( 2 ) );
			}
			if ( Next() < 0.5f ) Sack( new Vector3( u1 - Range( 10f, length * 0.4f ), Range( 18f, 22f ), 5.75f ), Range( 13f, 17f ) );
		}

		/// <summary>Pots of all sizes standing on the ledge in the bay, the big ones with a shrub, a fern or a cane plant.</summary>
		public void PotGroup( Bay bay )
		{
			var count = 2 + Pick( 4 );
			var big = Pick( count );
			var u = bay.From + Range( 3f, 12f );
			for ( var i = 0; i < count; i++ )
			{
				var r = i == big ? Range( 13f, 15.5f ) : Range( 6.5f, 11f );
				if ( u + 2f * r > bay.To - 3f ) break;
				var depth = Math.Clamp( Range( 14f, 24f ), Front + r * 1.05f + 0.5f, Skirt - r * 1.05f );
				var soil = Pot( new Vector3( u + r, depth, Ledge ), r, Clay(), true );
				var plant = r > 12f ? (Plant)new[] { 2, 3, 4, 5, 5 }[Pick( 5 )] : (Plant)new[] { 0, 1, 6, 0, 3, 5 }[Pick( 6 )];
				var size = plant switch
				{
					Plant.Shrub => r * Range( 4f, 5f ),
					Plant.Tall => r * Range( 3.5f, 4.5f ),
					Plant.BigLeaf => r * Range( 3.2f, 4f ),
					Plant.Succulent => r * Range( 1.4f, 1.8f ),
					_ => r * Range( 2.2f, 2.9f ),
				};
				Grow( plant, soil, size );
				if ( i == big && Next() < 0.5f )
				{
					var reach = Range( 140f, 220f );
					Apart( () => Vine( soil + new Vector3( 0f, r * 0.6f, 0f ), reach ) );
				}
				u += 2f * r + Range( 1f, 6f );
			}

			if ( u < bay.To - 22f && Next() < 0.25f ) WateringCan( new Vector3( u + 8f, Range( 18f, 22f ), Ledge ), Range( 0.9f, 1.1f ), Range( -30f, 30f ) );
			else if ( u < bay.To - 18f && Next() < 0.4f ) Sack( new Vector3( u + 9f, Range( 18f, 20f ), Ledge ), Range( 18f, 26f ) );
		}

		/// <summary>A palm in a big tub in the middle of the bay, with a pot or two at its foot.</summary>
		public void PalmTub( Bay bay )
		{
			const float R = 15.5f;
			var middle = bay.Middle + Range( -bay.Width * 0.15f, bay.Width * 0.15f );
			var soil = Pot( new Vector3( middle, Front + R * 1.05f + 0.8f, Ledge ), R, Next() < 0.5f ? 6 + Pick( 2 ) : Clay( false ), true );
			Apart( () => Palm( soil ) );
			for ( var side = -1; side <= 1; side += 2 )
			{
				if ( Next() < 0.4f ) continue;
				var r = Range( 5f, 8f );
				var u = middle + side * (R + r + Range( 2f, 8f ));
				if ( u - r < bay.From + 2f || u + r > bay.To - 2f ) continue;
				var foot = Pot( new Vector3( u, Range( 14f, 22f ), Ledge ), r, Clay(), true );
				Grow( Next() < 0.5f ? Plant.Bushy : Plant.Strap, foot, r * Range( 2.2f, 2.8f ) );
			}
		}

		/// <summary>A slatted staging shelf on iron brackets against the glass, with small pots — succulents, seedlings, ivy trailing over the edge.</summary>
		public void Staging( Bay bay )
		{
			var v = Range( 96f, 116f );
			var length = MathF.Min( bay.Width - 10f, Range( 40f, 90f ) );
			var u0 = bay.From + 5f + Next() * (bay.Width - 10f - length);
			var mid = u0 + length * 0.5f;
			Kit.BevelBox( TrimSkin( Next() < 0.5f ? CedarSwatch : GreyWoodSwatch ), At( new Vector3( mid, 30.5f, v - 0.75f ) ), new Vector3( length, 16.5f, 1.5f ), 0.3f, Run );
			foreach ( var bu in new[] { u0 + length * 0.18f, u0 + length * 0.82f } )
			{
				Kit.BevelBox( TrimSkin( IronSwatch ), At( new Vector3( bu, Face - 0.5f, v - 6f ) ), new Vector3( 1f, 1f, 10.5f ), 0.15f, Run );
				Kit.Tube( TrimSkin( IronSwatch ), At( new[] { new Vector3( bu, Face - 0.5f, v - 10.5f ), new Vector3( bu, 25f, v - 1.5f ) } ), 0.4f, 4 );
			}

			for ( var u = u0 + Range( 2f, 5f ); u < u0 + length - 5f; )
			{
				var r = Range( 4f, 6.5f );
				var soil = Pot( new Vector3( u + r, Math.Clamp( Range( 28f, 32f ), 22.5f + r, Face - r * 1.05f ), v ), r, Clay(), true );
				var p = Next();
				Apart( () =>
				{
					if ( p < 0.35f ) Trail( soil + new Vector3( 0f, -r * 0.9f, 0f ), Range( 16f, 40f ) );
					else if ( p < 0.6f ) Succulent( soil, r * Range( 1.4f, 1.9f ) );
					else if ( p < 0.85f ) Bushy( soil, r * Range( 2.4f, 3.2f ), Next() < 0.4f );
					else for ( var k = 0; k < 2; k++ ) Seedling( soil + new Vector3( Range( -r, r ) * 0.5f, Range( -r, r ) * 0.5f, 0f ), Range( 1.8f, 2.8f ), LeafSwatch( 2 ) );
				} );
				u += 2f * r + Range( 2.5f, 6f );
			}
		}

		/// <summary>A wire basket lined with coir, hung on three chains from the plate beam, a fern spilling over its rim.</summary>
		public void HangingFern( float u )
		{
			var depth = Range( 12f, 17f );
			var rim = Range( 200f, 216f );
			var r = Range( 9f, 12f );
			var centre = new Vector3( u, depth, rim );
			var profile = new Vector2[6];
			for ( var i = 0; i < 5; i++ )
			{
				var a = i / 4f * MathF.PI * 0.5f;
				profile[i] = new Vector2( r * MathF.Sin( a ), r * 0.85f * (1f - MathF.Cos( a )) );
			}
			profile[5] = new Vector2( r + 0.5f, r * 0.85f + 0.4f );
			Kit.SmoothLathe( TrimSkin( CoirSwatch ), At( centre - Vector3.Up * r * 0.85f ), profile, 10, Run, false, false, 50f, Next() );
			Kit.Dome( TrimSkin( SoilSwatch( 1 ) ), At( centre ), new Vector3( r, r, r * 0.12f ), Run, 1, 10 );

			var hook = new Vector3( u, depth, FloorContext.ClearHeight - 22f );
			var spin = Next() * MathF.Tau;
			for ( var c = 0; c < 3; c++ )
			{
				var point = centre + Flat( spin + c * MathF.Tau / 3f ) * (r + 0.3f) + Vector3.Up * 0.4f;
				Kit.Tube( TrimSkin( IronSwatch ), At( new[] { point, hook } ), 0.16f, 3 );
			}
			Apart( () => Fern( centre + Vector3.Up * 0.5f, Range( 30f, 42f ), true ) );
		}

		// ---- Things ----------------------------------------------------------------------------

		/// <summary>A wooden seed tray of potting mix with rows of seedlings, some rows patchy, some not yet up.</summary>
		void SeedTray( Vector3 at, float length )
		{
			var width = Range( 10f, 12f );
			const float Height = 2.6f;
			var rotation = Rotation.FromYaw( Range( -4f, 4f ) );
			Kit.Box( TrimSkin( Next() < 0.5f ? CedarSwatch : GreyWoodSwatch ), At( at + Vector3.Up * Height * 0.5f ), new Vector3( length, width, Height ), Turn( rotation ) );
			var surface = at + Vector3.Up * Height;
			Kit.Dome( TrimSkin( SoilSwatch( 1 ) ), At( surface ), new Vector3( length * 0.47f, width * 0.45f, 0.5f ), Turn( rotation ), 1, 8 );

			var leaf = LeafSwatch( Next() < 0.5f ? 2 : 0 );
			var rows = 2 + Pick( 2 );
			var sown = Next();
			for ( var row = 0; row < rows; row++ )
			{
				if ( sown < 0.25f && row >= rows / 2 ) break;
				var across = (row + 0.5f) / rows - 0.5f;
				for ( var a = -0.42f; a <= 0.42f; a += Range( 0.24f, 0.34f ) )
				{
					if ( Next() < 0.25f ) continue;
					var spot = surface + rotation * new Vector3( a * length, across * width * 0.85f, 0.2f );
					Seedling( spot, Range( 2f, 3.4f ), leaf );
				}
			}
			if ( Next() < 0.6f ) Label( surface + rotation * new Vector3( -length * 0.38f, width * 0.3f, 0f ) );
		}

		/// <summary>
		/// A watering can: a galvanised or painted body with a filler on top, a long spout rising from its
		/// foot to a rose, the carrying handle arched over the top and a second handle at the back.
		/// </summary>
		void WateringCan( Vector3 foot, float scale, float yaw )
		{
			var skin = TrimSkin( Next() < 0.6f ? ZincSwatch : CanPaintSwatch );
			var turn = Rotation.FromYaw( yaw );
			Vector3 L( float x, float y, float z ) => foot + turn * new Vector3( x, y, z ) * scale;
			Vector2[] Scaled( params Vector2[] profile ) => Array.ConvertAll( profile, p => p * scale );

			Kit.SmoothLathe( skin, At( foot ), Scaled( new Vector2( 5.2f, 0f ), new Vector2( 5.5f, 0.5f ), new Vector2( 5.5f, 8.6f ), new Vector2( 5.1f, 9.8f ), new Vector2( 3.6f, 10.6f ), new Vector2( 2.4f, 10.9f ) ),
				10, Turn( turn ), false, false, 40f, 0.25f );
			Kit.SmoothLathe( skin, At( L( 0f, 0f, 10.9f ) ), Scaled( new Vector2( 2.4f, 0f ), new Vector2( 2.6f, 0.5f ), new Vector2( 2.6f, 1.3f ), new Vector2( 2.1f, 1.3f ), new Vector2( 2.0f, 0.4f ) ),
				8, Turn( turn ), false, true );

			var spout = new[] { L( 4.6f, 0f, 1.5f ), L( 8.5f, 0f, 5f ), L( 12.5f, 0f, 9.5f ), L( 16f, 0f, 13.5f ) };
			Kit.Tube( skin, At( spout ), 0.85f * scale, 6, 0.55f * scale );
			var rise = Rotation.FromToRotation( Vector3.Up, new Vector3( 0.66f, 0f, 0.75f ).Normal );
			Kit.SmoothLathe( skin, At( spout[^1] ), Scaled( new Vector2( 0.55f, 0f ), new Vector2( 1.7f, 1.4f ), new Vector2( 1.7f, 1.9f ) ), 8, Turn( turn * rise ), false, true );

			var arch = new Vector3[5];
			for ( var i = 0; i < arch.Length; i++ )
			{
				var a = i / (arch.Length - 1f) * MathF.PI;
				arch[i] = L( -MathF.Cos( a ) * 3.4f - 0.6f, 0f, 10.4f + MathF.Sin( a ) * 5.6f );
			}
			Kit.Tube( skin, At( arch ), 0.5f * scale, 5 );
			Kit.Tube( skin, At( new[] { L( -5.3f, 0f, 9f ), L( -8f, 0f, 8.4f ), L( -8.5f, 0f, 5.6f ), L( -5.3f, 0f, 3f ) } ), 0.5f * scale, 5 );
		}

		/// <summary>A jute sack of compost <paramref name="height"/> tall, settled on its base, bellied, its neck tied off and the ends splayed.</summary>
		void Sack( Vector3 foot, float height )
		{
			var r = height * Range( 0.4f, 0.48f );
			var h = height;
			Kit.SmoothLathe( TrimSkin( JuteSwatch ), At( foot ), new[]
			{
				new Vector2( r * 0.8f, 0f ), new Vector2( r, h * 0.12f ), new Vector2( r * 1.02f, h * 0.45f ), new Vector2( r * 0.85f, h * 0.72f ), new Vector2( r * 0.42f, h * 0.86f ),
				new Vector2( r * 0.28f, h * 0.9f ), new Vector2( r * 0.4f, h * 0.97f ), new Vector2( r * 0.18f, h ), new Vector2( 0f, h * 0.98f ),
			}, 8, Turn( Rotation.From( Range( -6f, 6f ), Next() * 360f, Range( -6f, 6f ) ) ), true, false, 50f, Next() );
		}

		/// <summary>
		/// An enamel pendant on a swan-neck bracket screwed to the plate beam (a rod down to a roof bar
		/// would land mid-pane wherever the lamp's u fell): a shallow dome shade, enamelled outside and lit
		/// white inside, the bulb showing under its rim. The shade of each is its own colour.
		/// </summary>
		public void Pendant( int side, float u )
		{
			OnSide( side );

			// The bracket: up from the shade, over in a curve and back into the plate beam's face (depth 0,
			// from Bracket - 11 to the ceiling), on a small iron plate.
			const float Bracket = FloorContext.ClearHeight - 11f;
			var mouth = new Vector3( u, PendantDepth, PendantMouth );
			Kit.Tube( TrimSkin( IronSwatch ), At( new[]
			{
				mouth + Vector3.Up * 15f, new Vector3( u, PendantDepth, Bracket - 4f ), new Vector3( u, PendantDepth * 0.85f, Bracket - 0.6f ),
				new Vector3( u, PendantDepth * 0.5f, Bracket + 0.4f ), new Vector3( u, 0.5f, Bracket ),
			} ), 0.35f, 4 );
			Kit.BevelBox( TrimSkin( IronSwatch ), At( new Vector3( u, -0.4f, Bracket ) ), new Vector3( 3.6f, 0.8f, 6f ), 0.2f, Run );

			var shade = TrimSkin( new[] { EnamelSwatch, CreamEnamelSwatch, ZincSwatch }[Pick( 3 )] );
			const float Scale = 1.3f;
			Vector2[] Scaled( params Vector2[] profile ) => Array.ConvertAll( profile, p => p * Scale );
			Kit.SmoothLathe( shade, At( mouth ), Scaled(
				new Vector2( 8.6f, 0.35f ), new Vector2( 9.4f, 0f ), new Vector2( 9.1f, 1f ), new Vector2( 7.6f, 4f ), new Vector2( 4.8f, 7.2f ),
				new Vector2( 2.4f, 9f ), new Vector2( 1.6f, 10.2f ), new Vector2( 1.6f, 12.5f ) ), 16, Run, false, true, 35f, Next() );
			Kit.SmoothLathe( new Skin( _glow, ShadeGlow ), At( mouth ), Scaled(
				new Vector2( 1.3f, 9.6f ), new Vector2( 2.2f, 8.5f ), new Vector2( 4.5f, 6.7f ), new Vector2( 7.2f, 3.7f ), new Vector2( 8.6f, 0.9f ), new Vector2( 8.6f, 0.35f ) ), 16, Run, false, false, 50f );
			Kit.Ellipsoid( new Skin( _glow, BulbGlow ), At( mouth + Vector3.Up * 3.4f ), new Vector3( 2.7f, 2.7f, 3.4f ), Run, 5, 10 );
		}
	}
}
