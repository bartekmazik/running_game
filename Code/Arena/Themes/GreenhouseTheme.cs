using System;

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
	/// The structural front layer: slender painted posts, a plate beam under the roof, skirting; and a
	/// warm fill of four dim lamps under the plate beam — the sun through the glass does the rest. The
	/// planting beds, ferns and potting shelves come in Task 7.
	/// </summary>
	public override void Dress( FloorContext room )
	{
		BuildFrontLayer( room, new FrontStyle { Post = 20f, Pier = 64f, Corner = 26f, Cornice = 22f, Skirting = 12f, SkirtingDepth = 4f } );
		AddLamps( room, 4, FloorContext.ClearHeight - 30f, -24f, 480f, LightColor * 1.2f, Seed + 900 );
	}

	/// <summary>
	/// A greenhouse side, floor to roof: a brick dwarf wall up to bench height, a painted timber sill,
	/// then glazing — white-sage bars every 26 units bedded in putty, a vent rail across them, and
	/// between them panes of opaque frosted glass lapped in uneven courses, each its own tint, with
	/// grime and a green bloom caught in the laps and condensation gathered at the foot of each pane.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		const int Column = WallSize / 8;
		const float Bar = 12f;
		int Row( float v ) => (int)MathF.Round( (WallRepeat - v) * Ppu );
		var painter = new Painter( WallSize, Glass, 0.15f );
		var random = new Random( Seed + 600 );
		float Next() => (float)random.NextDouble();

		var sillTop = Row( 50f );
		var kneeTop = Row( 44f );

		// Panes, column by column, lapped in courses of uneven height that differ between columns.
		for ( var c = 0; c < 8; c++ )
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
		for ( var c = 0; c < 8; c++ )
		{
			var x = c * Column;
			painter.Rect( x - Bar * 0.5f - 3f, 0f, Bar + 6f, sillTop, Putty, height: 0.72f, bevel: 2f, roughness: 0.7f );
			painter.Rect( x - Bar * 0.5f, 0f, Bar, sillTop, Mix( BarPaint, BarShade, Next() * 0.15f ), radius: 2f, height: 1f, bevel: 4f, roughness: 0.45f );
		}
		painter.Rect( 0f, Row( 153f ), WallSize, Row( 149f ) - Row( 153f ), BarPaint, height: 1.05f, bevel: 4f, roughness: 0.45f );

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
		} );
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
}
