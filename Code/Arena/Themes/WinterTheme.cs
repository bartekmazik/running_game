using System;

/// <summary>
/// Floor 2 (index 1): a winter cabin. Underfoot, blocks of packed snow — soft drifts, lumpy rims,
/// old blue ice showing through where it has worn thin, and frost that sparkles. Overhead (the
/// undersides of the greenhouse floor), knotty pine boards.
/// </summary>
public sealed class WinterTheme : ArenaTheme
{
	public override string Name => "Winter";
	public override int Floor => 1;
	public override Color LightColor => new( 0.90f, 0.97f, 1.10f );

	static Color Snow => Palette.Hex( 0xEEF3F8 );
	static Color SnowLight => Palette.Hex( 0xFFFFFF );
	static Color SnowShade => Palette.Hex( 0xC8D6E6 );
	static Color Ice => Palette.Hex( 0xA7CBE8 );
	static Color IceDeep => Palette.Hex( 0x7FA8CE );

	/// <summary>The gap between snow blocks: blue shadow, not grey.</summary>
	static Color Crevice => Palette.Hex( 0x9DB2CA );

	static Color Pine => Palette.Hex( 0xC9A27A );
	static Color PinePale => Palette.Hex( 0xDBBC95 );
	static Color PineDeep => Palette.Hex( 0xB7895D );
	static Color LateWood => Palette.Hex( 0xA0714A );
	static Color Knot => Palette.Hex( 0x6A4329 );
	static Color BoardGap => Palette.Hex( 0x5C4331 );

	public override void Dress( FloorContext room )
	{
		// Room dressing arrives in Task 6.
	}

	protected override Material PaintFloor()
	{
		const int cell = FloorSize / 2;
		var painter = new Painter( FloorSize, Crevice, 0.7f );
		var random = new Random( Seed );
		float Next() => (float)random.NextDouble();

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var snow = Mix( Snow, Next() < 0.5f ? SnowShade : SnowLight, Next() * 0.3f );
			var joint = 6f + Next() * 5f;

			painter.Polygon( SlabOutline( min, cell, joint, 5f, 14f + Next() * 18f, Seed + c * 101 ), snow, height: 1f, bevel: 26f, roughness: 0.55f );

			// Lumps of snow along the rim, so no block edge is a clean line.
			var lumps = 3 + random.Next( 4 );
			for ( var l = 0; l < lumps; l++ )
			{
				var side = random.Next( 4 );
				var along = 60f + Next() * 392f;
				var half = new Vector2( 14f + Next() * 22f, 7f + Next() * 7f );
				var reach = half.y + 2f + Next() * 8f;
				var at = side switch
				{
					0 => min + new Vector2( along, reach ), 1 => min + new Vector2( cell - reach, along ),
					2 => min + new Vector2( along, cell - reach ), _ => min + new Vector2( reach, along ),
				};
				var angle = side % 2 == 0 ? 0f : MathF.PI * 0.5f;
				painter.Polygon( Organic( at, half, 0.8f, Seed + c * 41 + l, angle, 24 ), snow, height: 1.06f, bevel: 12f, roughness: 0.55f );
			}

			// Old ice showing through where the snow has worn thin — on three of the four blocks, so the
			// floor is not polka-dotted: two small patches, one large, one near the rim. A frosted halo
			// blends each into the snow around it.
			var patches = c switch { 0 => 0, 1 => 2, _ => 1 };
			for ( var p = 0; p < patches; p++ )
			{
				var scale = c == 2 ? 1.25f : c == 1 ? (p == 0 ? 0.5f : 0.75f) : 0.8f;
				var half = new Vector2( 45f + Next() * 60f, 30f + Next() * 40f ) * scale;

				// The halo is the widest part; keep it whole inside this block's cell.
				var margin = MathF.Max( half.x, half.y ) * 1.25f + 8f;
				var span = cell - 2f * margin;
				var centre = c == 3 ? min + new Vector2( margin + Next() * 30f, margin + Next() * span )
					: c == 1 ? min + new Vector2( margin + (p + Next() * 0.4f) * span * 0.6f, margin + (p == 0 ? Next() * 0.3f : 0.7f + Next() * 0.3f) * span )
					: min + new Vector2( margin + Next() * span, margin + Next() * span );
				var angle = Next() * MathF.Tau;
				var shapeSeed = Seed + c * 53 + p;
				painter.Polygon( Organic( centre, half * 1.25f, 0.55f, shapeSeed, angle, 48 ), Mix( Snow, Ice, 0.5f ), alpha: 0.35f );
				painter.Polygon( Organic( centre, half, 0.55f, shapeSeed, angle, 48 ), Ice, height: 0.45f, bevel: 18f, roughness: 0.18f );
				painter.Polygon( Organic( centre, half * 0.55f, 0.6f, shapeSeed + 1, angle, 32 ), IceDeep, alpha: 0.4f );

				var reach = MathF.Min( half.x, half.y );
				var cracks = 2 + random.Next( 2 );
				for ( var k = 0; k < cracks; k++ )
				{
					var start = centre + new Vector2( Next() - 0.5f, Next() - 0.5f ) * reach * 0.6f;
					Polyline( painter, Wander( start, Next() * MathF.Tau, reach * 0.15f, 4 + random.Next( 3 ), 0.6f, random ), 1f, SnowLight, 0.55f );
				}

				var bubbles = 8 + random.Next( 10 );
				for ( var b = 0; b < bubbles; b++ )
				{
					var theta = Next() * MathF.Tau; var distance = MathF.Sqrt( Next() ) * 0.5f;
					var at = centre + new Vector2( MathF.Cos( theta ), MathF.Sin( theta ) ) * reach * distance;
					painter.Circle( at.x, at.y, 0.8f + Next() * 1.5f, Mix( Ice, SnowLight, 0.7f ), 0.7f, roughness: 0.15f );
				}

				// Snow dust blown back over the ice.
				for ( var d = 0; d < 14; d++ )
				{
					var theta = Next() * MathF.Tau; var distance = 0.3f + Next() * 0.6f;
					var at = centre + new Vector2( MathF.Cos( theta ) * half.x, MathF.Sin( theta ) * half.y ) * distance;
					painter.Circle( at.x, at.y, 1.5f + Next() * 3f, Snow, 0.35f );
				}
			}

			// Frost: tiny facets that catch the sun.
			for ( var s = 0; s < 320; s++ )
			{
				var at = min + new Vector2( 16f + Next() * 480f, 16f + Next() * 480f );
				painter.Circle( at.x, at.y, 0.55f + Next() * 0.75f, SnowLight, 0.9f, roughness: 0.12f );
			}
		}

		var ridges = PaintDrifts( painter, random );
		painter.HeightNoise( 0.06f, 64, Seed + 11, 2 );
		painter.Mottle( 0.03f, 4, Seed + 12, 5 );
		painter.RoughnessNoise( 0.08f, 8, Seed + 13, 4 );
		painter.Cavity( 0.35f, 8 );
		painter.Cavity( 0.2f, 32 );

		// Added after the cavity bake: wind ridges shape the sheen, but grey crevice shading in their
		// grooves read as dirt on fresh snow.
		painter.AddHeight( ridges, 0.18f );
		painter.Grain( 0.01f, Seed + 14 );
		return painter.ToMaterial( "arena_1_floor", Painter.Matte, 6f );
	}

	/// <summary>
	/// Wind-shaped snow: broad soft drifts whose hollows turn a cooler blue (the lower floors are lit
	/// mostly by ambient light, where colour carries relief better than normals). Returns sastrugi —
	/// fine ridges scoured along one wind direction per block, read from a stretched noise field —
	/// for the caller to add once crevices are baked.
	/// </summary>
	float[] PaintDrifts( Painter painter, Random random )
	{
		const int cell = FloorSize / 2;
		var drift = painter.Noise( 3, Seed + 10, 5 );
		var hollows = new float[drift.Length];
		for ( var i = 0; i < drift.Length; i++ )
		{
			var t = (0.48f - drift[i]) * 4f;
			hollows[i] = t <= 0f ? 0f : t >= 1f ? 0.45f : t * 0.45f;
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

		painter.AddHeight( drift, 3f );
		painter.Blend( hollows, SnowShade );
		return ridges;
	}

	/// <summary>
	/// Knotty pine boards, four to six per tile underside, each its own width, tone and length.
	/// Grain is a noise field read stretched along the board and nudged by a slow wave, so the late
	/// wood runs in long, slightly wavy streaks; knots sit in a halo of darker grain.
	/// </summary>
	protected override Material PaintCeiling()
	{
		const int cell = CeilingSize / 2;
		var painter = new Painter( CeilingSize, BoardGap, 0.9f );
		var random = new Random( Seed + 500 );
		float Next() => (float)random.NextDouble();
		var grain = painter.Noise( 128, Seed + 501, 2 );
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
							var s = Streak( grain, CeilingSize, row + py + (int)wave, (px + shift) / 14f );
							lateWood[offset + px] = s > 0.55f ? MathF.Min( (s - 0.55f) * 8f, 1f ) * strength : 0f;
							earlyWood[offset + px] = s < 0.4f ? MathF.Min( (0.4f - s) * 6f, 1f ) * 0.25f : 0f;
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
		return painter.ToMaterial( "arena_1_ceiling", Painter.Matte, 4f );
	}
}
