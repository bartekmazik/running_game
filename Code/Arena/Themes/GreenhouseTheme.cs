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

	static Color Moss => Palette.Hex( 0x5C7A43 );
	static Color MossLight => Palette.Hex( 0xA9CF95 );
	static Color Soil => Palette.Hex( 0x5A4434 );

	public override void Dress( FloorContext room )
	{
		// Room dressing arrives in Task 6.
	}

	protected override Material PaintFloor()
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
			var joint = 1.5f + Next() * 2.5f;

			// The arris under each slab is its own clay a shade deeper, not a grey grout line: the
			// geometric chamfer carries the joint, the texture only darkens toward it.
			painter.Rect( min.x, min.y, cell, cell, Mix( body, Flash, 0.3f ), height: 0.5f );

			painter.Polygon( SlabOutline( min, cell, joint, 2f, 6f + Next() * 12f, Seed + c * 101 ), body, height: 1f, bevel: 9f, roughness: 0.8f );

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
				painter.Polygon( ChipOutline( min + cornerOffsets[k], sideA[k], sideB[k], 70f + Next() * 45f, 60f + Next() * 50f, Seed + c * 13 ),
					Mix( body, RawClay, 0.55f ), height: 0.55f, bevel: 5f, roughness: 0.92f );
				if ( c == 3 )
				{
					k = 1;
					painter.Polygon( ChipOutline( min + cornerOffsets[k], sideA[k], sideB[k], 36f + Next() * 16f, 32f + Next() * 16f, Seed + c * 17 ),
						Mix( body, RawClay, 0.55f ), height: 0.6f, bevel: 4f, roughness: 0.92f );
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
					Mix( body, RawClay, 0.5f ), height: 0.62f, bevel: 3f, roughness: 0.92f );
			}

			// Moss and soil lodged at the block's edge: small clusters of round tufts.
			var tufts = c == 2 ? 0 : random.Next( 3 );
			for ( var t = 0; t < tufts; t++ )
			{
				var side = random.Next( 4 );
				var along = 40f + Next() * 430f;
				var size = 5f + Next() * 7f;
				var inset = joint + size * 1.6f;
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
		var moss = Ramp( painter.Noise( 20, Seed + 40, 4 ), 0.36f, 0.6f );
		KeepNear( moss, FloorSize, mossCorner, 175f );
		var clumps = Ramp( painter.Noise( 96, Seed + 41, 2 ), 0.4f, 0.75f );
		for ( var i = 0; i < clumps.Length; i++ ) clumps[i] *= moss[i];
		painter.Blend( moss, Moss, roughness: 0.97f );
		painter.Blend( clumps, MossLight );
		painter.AddHeight( moss, 0.4f );
		painter.AddHeight( clumps, 0.25f );

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
		return painter.ToMaterial( "arena_0_floor", Painter.Matte, 8f );
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

		var cushions = 4 + random.Next( 5 );
		for ( var k = 0; k < cushions; k++ )
		{
			var offset = new Vector2( Next() - 0.5f, Next() - 0.5f ) * size * 1.6f;
			var r = size * (0.3f + Next() * 0.4f);
			painter.Circle( at.x + offset.x, at.y + offset.y, r, Mix( Moss, MossLight, Next() * 0.6f ), 0.9f, height: 1.05f + Next() * 0.1f, bevel: r, roughness: 0.97f );
		}
	}

	/// <summary>
	/// None: the greenhouse is roofed in glass (built as geometry with the room), and no tile
	/// underside ever shows this room's ceiling — floor 0's undersides wear the winter room's.
	/// </summary>
	protected override Material PaintCeiling() => null;
}
