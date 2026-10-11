using System;

/// <summary>
/// Floor 4 (index 3): an office. Underfoot, loop-pile carpet tiles with striated two-tone yarn,
/// laid quarter-turned, flattened along the walking paths, with the stains every office floor
/// collects. Overhead (the undersides of the beach floor), fissured acoustic ceiling tiles.
/// </summary>
public sealed class OfficeTheme : ArenaTheme
{
	public override string Name => "Office";
	public override int Floor => 3;
	public override Color LightColor => new( 0.95f, 1.00f, 1.10f );

	static Color Carpet => Palette.Hex( 0x8C97B8 );
	static Color CarpetLight => Palette.Hex( 0xA3ADCB );
	static Color CarpetShade => Palette.Hex( 0x76809F );
	static Color CarpetDeep => Palette.Hex( 0x5E6784 );
	static Color Fleck => Palette.Hex( 0x9FB0DA );
	static Color Seam => Palette.Hex( 0x474D63 );
	static Color Coffee => Palette.Hex( 0x6B5646 );

	static Color Panel => Palette.Hex( 0xE8E4DA );
	static Color Grid => Palette.Hex( 0xD2CEC5 );
	static Color Pinhole => Palette.Hex( 0x9C968A );
	static Color WaterStain => Palette.Hex( 0xB39A6E );

	/// <summary>Pile rows across the yarn, in pixels: about a third of a unit, close to real loop pile.</summary>
	const int PileRow = 3;

	public override void Dress( FloorContext room )
	{
		// Room dressing arrives in Task 6.
	}

	protected override Material PaintFloor()
	{
		const int cell = FloorSize / 2;
		var painter = new Painter( FloorSize, Seam, 0.95f );
		var random = new Random( Seed );
		float Next() => (float)random.NextDouble();

		// Cut carpet tiles are square, but never laid perfectly: a pixel or three of gap that varies,
		// a fraction of a degree of skew.
		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var tone = Next() * 2f - 1f;
			var carpet = tone < 0f ? Mix( Carpet, CarpetShade, -tone * 0.35f ) : Mix( Carpet, CarpetLight, tone * 0.3f );
			var left = 3f + Next() * 4f; var top = 3f + Next() * 4f;
			var right = 3f + Next() * 4f; var bottom = 3f + Next() * 4f;
			painter.Rect( min.x + left, min.y + top, cell - left - right, cell - top - bottom, carpet, radius: 3f, height: 1f, bevel: 8f, roughness: 0.95f, angle: (Next() - 0.5f) * 0.5f );
		}

		PaintPile( painter );

		// Walking paths: pile crushed flat and a little lighter and shinier.
		painter.Blend( Ramp( painter.Noise( 3, Seed + 30, 4 ), 0.55f, 0.82f, 0.45f ), Mix( Carpet, CarpetLight, 0.35f ), height: 1f, roughness: 0.86f );

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );

			// A coffee spill on one tile: a faint irregular stain with a darker dried rim, and a drip beside it.
			if ( c == 2 )
			{
				var centre = min + new Vector2( 150f + Next() * 210f, 150f + Next() * 210f );
				var outline = Organic( centre, new Vector2( 26f + Next() * 14f, 20f + Next() * 12f ), 1f, Seed + 70, Next() * MathF.Tau );
				painter.Polygon( outline, Coffee, alpha: 0.12f, roughness: 0.9f );
				for ( var k = 0; k < outline.Length; k++ )
					painter.Line( outline[k], outline[(k + 1) % outline.Length], 1.8f, Coffee, 0.16f );
				var drip = centre + new Vector2( 40f + Next() * 20f, -20f + Next() * 40f );
				painter.Polygon( Organic( drip, new Vector2( 5f, 4f ), 0.6f, Seed + 71, 0f, 16 ), Coffee, alpha: 0.25f );
			}

			// Old chewing gum, flattened grey and shiny.
			if ( c == 0 )
			{
				var at = min + new Vector2( 80f + Next() * 350f, 80f + Next() * 350f );
				painter.Polygon( Organic( at, new Vector2( 3.2f, 2.7f ), 0.5f, Seed + 72, 0f, 16 ), Palette.Hex( 0x5A5A64 ), 0.7f, height: 0.98f, bevel: 1f, roughness: 0.5f );
			}

			// A patch scrubbed lighter than the rest.
			if ( c == 3 )
			{
				var at = min + new Vector2( 140f + Next() * 230f, 140f + Next() * 230f );
				painter.Polygon( Organic( at, new Vector2( 45f + Next() * 20f, 35f + Next() * 20f ), 0.8f, Seed + 73, Next() * MathF.Tau ), CarpetLight, alpha: 0.16f );
			}

			// Lint and grit.
			var specks = 15 + random.Next( 20 );
			for ( var s = 0; s < specks; s++ )
			{
				var at = min + new Vector2( 20f + Next() * 472f, 20f + Next() * 472f );
				painter.Circle( at.x, at.y, 0.6f + Next() * 0.9f, Next() < 0.7f ? Palette.Hex( 0xD8D6D0 ) : Palette.Hex( 0x3C3A40 ), 0.6f );
			}
		}

		painter.Mottle( 0.04f, 4, Seed + 40, 6 );
		painter.RoughnessNoise( 0.03f, 8, Seed + 41, 3 );
		painter.Cavity( 0.35f, 3 );
		painter.Cavity( 0.3f, 10 );
		painter.Grain( 0.02f, Seed + 42 );
		return painter.ToMaterial( "arena_3_floor", Painter.Matte, 4f );
	}

	/// <summary>
	/// Loop pile in rows, alternating direction per tile (the quarter-turn laying). Each row's yarn
	/// drifts between a dark and a light ply in streaks a hundred-odd pixels long — read from a noise
	/// field sampled stretched along the row — and the odd row carries a periwinkle fleck yarn.
	/// Loops are offset row to row like real tufting.
	/// </summary>
	void PaintPile( Painter painter )
	{
		const int cell = FloorSize / 2;
		const int Stretch = 3;
		var streaks = painter.Noise( 64, Seed + 20, 2 );
		var heather = painter.Noise( 512, Seed + 21, 1 );
		var dark = new float[FloorSize * FloorSize];
		var light = new float[FloorSize * FloorSize];
		var fleck = new float[FloorSize * FloorSize];
		var loops = new float[FloorSize * FloorSize];

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var alongX = c == 0 || c == 3;

			for ( var y = 0; y < cell; y++ )
			{
				var row = ((int)min.y + y) * FloorSize;
				for ( var x = 0; x < cell; x++ )
				{
					var i = row + (int)min.x + x;
					var along = alongX ? x : y;
					var across = alongX ? y : x;
					var pileRow = across / PileRow;

					// Stretched read: neighbouring rows sample far-apart noise rows, so each row streaks on its own.
					var sampleRow = (pileRow * 11 + c * 97) % FloorSize * FloorSize;
					var position = along / (float)Stretch + pileRow * 37;
					var p0 = (int)position;
					var f = position - p0;
					var s = streaks[sampleRow + p0 % FloorSize] * (1f - f) + streaks[sampleRow + (p0 + 1) % FloorSize] * f;

					// Heathered yarn: a tight speckle of a dark and a light ply, faintly streaked along the
					// row, and alternate pile rows a touch lighter — the fine striation of a tufted tile.
					var tone = (heather[i] - 0.5f) * 0.9f + (s - 0.5f) * 0.6f + ((pileRow & 1) == 0 ? 0.07f : -0.07f);
					dark[i] = tone < -0.08f ? MathF.Min( (-0.08f - tone) * 3f, 1f ) * 0.45f : 0f;
					light[i] = tone > 0.08f ? MathF.Min( (tone - 0.08f) * 3f, 1f ) * 0.35f : 0f;

					var rowHash = (uint)(pileRow * 2654435761u + (uint)c * 40503u);
					fleck[i] = (rowHash >> 24) > 238 && s > 0.5f ? 0.55f : 0f;

					var acrossPhase = across % PileRow;
					var alongPhase = (along + (pileRow & 1)) % PileRow;
					var acrossHeight = acrossPhase == 0 ? 0.15f : acrossPhase == 1 ? 1f : 0.6f;
					var alongHeight = alongPhase == 0 ? 0.35f : alongPhase == 1 ? 1f : 0.7f;
					loops[i] = acrossHeight * alongHeight - 0.5f;
				}
			}
		}

		painter.Blend( dark, CarpetDeep );
		painter.Blend( light, CarpetLight );
		painter.Blend( fleck, Fleck );
		painter.AddHeight( loops, 0.16f );
	}

	/// <summary>
	/// Lay-in acoustic tiles, two by two on every underside, in a light T-bar grid: random pinholes
	/// and short fissures (never on a grid), a slightly different tone per panel, a dusty one, and a
	/// water stain with its tide-mark rings on one.
	/// </summary>
	protected override Material PaintCeiling()
	{
		const int cell = CeilingSize / 2;
		const int panel = cell / 2;
		var painter = new Painter( CeilingSize, Grid, 0.5f );
		var random = new Random( Seed + 500 );
		float Next() => (float)random.NextDouble();

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );
			for ( var p = 0; p < 4; p++ )
			{
				var x0 = min.x + (p % 2) * panel + 3f + Next();
				var y0 = min.y + (p / 2) * panel + 3f + Next();
				var tone = Next() * 2f - 1f;
				var color = c == 3 && p == 1 ? Mix( Panel, Pinhole, 0.12f ) : Mix( Panel, tone < 0f ? Pinhole : Color.White, MathF.Abs( tone ) * 0.06f );
				painter.Rect( x0, y0, panel - 6f - Next(), panel - 6f - Next(), color, radius: 1f, height: 1f, bevel: 5f, roughness: 0.93f, angle: (Next() - 0.5f) * 0.3f );

				var holes = 170 + random.Next( 100 );
				for ( var h = 0; h < holes; h++ )
				{
					var hx = x0 + 6f + Next() * (panel - 18f); var hy = y0 + 6f + Next() * (panel - 18f);
					painter.Circle( hx, hy, 0.55f + Next() * 0.55f, Pinhole, 0.8f, height: 0.82f, roughness: 0.97f );
				}

				var fissures = 5 + random.Next( 7 );
				for ( var f = 0; f < fissures; f++ )
				{
					var start = new Vector2( x0 + 10f + Next() * (panel - 26f), y0 + 10f + Next() * (panel - 26f) );
					Polyline( painter, Wander( start, Next() * MathF.Tau, 2.5f, 2 + random.Next( 3 ), 0.9f, random ), 1.1f, Pinhole, 0.55f, 0.86f );
				}
			}

			if ( c == 1 )
			{
				var centre = min + new Vector2( panel * 0.5f + Next() * panel, panel * 0.5f + Next() * panel * 0.6f );
				var half = new Vector2( 26f + Next() * 14f, 22f + Next() * 10f );
				var outline = Organic( centre, half, 1f, Seed + 570 );
				painter.Polygon( outline, WaterStain, alpha: 0.16f );
				for ( var ring = 0; ring < 2; ring++ )
				{
					var shrunk = Organic( centre, half * (1f - ring * 0.35f), 1f, Seed + 570 );
					for ( var k = 0; k < shrunk.Length; k++ )
						painter.Line( shrunk[k], shrunk[(k + 1) % shrunk.Length], 2f, WaterStain, 0.3f - ring * 0.1f );
				}
			}
		}

		painter.Mottle( 0.03f, 3, Seed + 520, 5 );
		painter.Cavity( 0.35f, 3 );
		painter.Grain( 0.01f, Seed + 521 );
		return painter.ToMaterial( "arena_3_ceiling", Painter.Matte, 3f );
	}
}
