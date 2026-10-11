using System;

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
		return painter.ToMaterial( "arena_1_floor", Painter.Matte, 7f );
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
	protected override Material PaintCeiling()
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
		return painter.ToMaterial( "arena_1_ceiling", Painter.Matte, 4f );
	}
}
