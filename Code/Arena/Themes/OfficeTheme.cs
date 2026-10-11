using System;

/// <summary>
/// Floor 4 (index 3): an office. Underfoot, loop-pile carpet tiles in heathered yarn with fleck
/// dashes, laid quarter-turned so each tile shades differently, flattened along the walking paths,
/// with the faint stains every office floor collects. Overhead (the undersides of the beach floor),
/// fissured acoustic ceiling tiles.
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

	/// <summary>Partition fabric: a warm grey weave, and the slightly bluer grey of a replaced panel.</summary>
	static Color Fabric => Palette.Hex( 0xB4B1AA );
	static Color FabricBlue => Palette.Hex( 0xA6ABB8 );
	static Color FabricDark => Palette.Hex( 0x8E8B86 );
	static Color FabricLight => Palette.Hex( 0xC8C5BE );

	static Color Aluminium => Palette.Hex( 0xC7CACE );
	static Color AluminiumShade => Palette.Hex( 0x8F949B );
	static Color Plaster => Palette.Hex( 0xE9E5DC );
	static Color PlasterShade => Palette.Hex( 0xB7B2A8 );
	static Color Vinyl => Palette.Hex( 0x545966 );
	static Color PowderCoat => Palette.Hex( 0xEFEFEC );
	static Color Sky => Palette.Sky;
	static Color Slat => Palette.Hex( 0xE4E4DF );

	/// <summary>The partition module: four per wall repeat, so bays start on a whole panel.</summary>
	protected override float WallModule => WallRepeat / 4f;

	/// <summary>Pile rows across the yarn, in pixels: about a third of a unit, close to real loop pile.</summary>
	const int PileRow = 3;

	/// <summary>
	/// The structural front layer — plastered columns, a bulkhead beam, a vinyl cove skirting — and
	/// windows with blinds in about half the bays; neutral-cool light from six ceiling panels set in from
	/// the walls. Whiteboards, clocks, cabinets and the cooler come in Task 8.
	/// </summary>
	public override void Dress( FloorContext room )
	{
		var bays = BuildFrontLayer( room, new FrontStyle { Post = 28f, Pier = 72f, Corner = 30f, Cornice = 26f, Skirting = 10f, SkirtingDepth = 3f } );
		AddWindows( room, bays, InfillCell( 0 ), InfillCell( 1 ), 48f, 104f, 124f, 0.5f, false, Seed + 800 );
		AddLamps( room, 6, FloorContext.ClearHeight - 28f, -160f, 480f, LightColor * 2.6f, Seed + 900 );
	}

	/// <summary>
	/// System partitions, 52-unit modules: aluminium posts with a shadow groove, a dark vinyl kick, a
	/// lower and an upper fabric panel split by a rail, plaster above to the bulkhead. The fabric is a
	/// fine weave with a heathered yarn, every panel its own small drift in tone and hue (no single odd
	/// panel: anything one-off in a 208-unit repeat comes back in every bay, 6a review / R18), with
	/// the faint ghosts of notices that hung there and the pinholes that held them.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		const int Module = WallSize / 4;
		int Row( float v ) => (int)MathF.Round( (WallRepeat - v) * Ppu );
		var painter = new Painter( WallSize, Plaster, 0.85f );
		var random = new Random( Seed + 600 );
		float Next() => (float)random.NextDouble();

		painter.Fill( 0, 0, WallSize, Row( 153f ), Plaster, height: 0.8f, roughness: 0.85f );
		painter.Fill( 0, Row( 153f ), WallSize, WallSize - Row( 153f ), Aluminium, height: 0.9f, roughness: 0.35f );

		var panels = new[] { (Top: Row( 150f ), Bottom: Row( 99f )), (Top: Row( 96f ), Bottom: Row( 10f )) };
		for ( var m = 0; m < 4; m++ )
		{
			for ( var p = 0; p < 2; p++ )
			{
				var (top, bottom) = panels[p];
				var x0 = m * Module + 9f;
				var tone = Next() * 2f - 1f;
				var fabric = tone < 0f ? Mix( Fabric, FabricDark, -tone * 0.18f ) : Mix( Fabric, FabricLight, tone * 0.2f );
				fabric = Mix( fabric, FabricBlue, Next() * 0.12f );
				painter.Rect( x0, top + 1f, Module - 18f, bottom - top - 2f, fabric, radius: 2f, height: 0.7f, bevel: 3f, roughness: 0.95f );

				// The ghost of a notice: a paler rectangle where the fabric was kept out of the light, and its pinholes.
				if ( Next() < 0.3f )
				{
					var w = 40f + Next() * 60f; var h = 50f + Next() * 70f;
					var nx = x0 + 15f + Next() * (Module - 48f - w); var ny = top + 20f + Next() * MathF.Max( 1f, bottom - top - 40f - h );
					painter.Rect( nx, ny, w, h, FabricLight, alpha: 0.06f );
					foreach ( var (cx, cy) in new[] { (nx + 3f, ny + 3f), (nx + w - 3f, ny + 3f), (nx + 3f, ny + h - 3f), (nx + w - 3f, ny + h - 3f) } )
						painter.Circle( cx + (Next() - 0.5f) * 2f, cy + (Next() - 0.5f) * 2f, 0.8f, FabricDark, 0.6f );
				}
			}

			// The post between modules, with its shadow groove.
			var post = m * Module;
			painter.Rect( post - 9f, Row( 153f ), 18f, WallSize - Row( 153f ), Aluminium, height: 1f, bevel: 3f, roughness: 0.3f );
			painter.Rect( post - 1.5f, Row( 153f ), 3f, WallSize - Row( 153f ), AluminiumShade, height: 0.6f, roughness: 0.5f );
		}

		painter.Rect( 0f, Row( 99f ), WallSize, Row( 96f ) - Row( 99f ), Aluminium, height: 1f, bevel: 2f, roughness: 0.3f );
		painter.Rect( 0f, Row( 153f ), WallSize, Row( 150f ) - Row( 153f ), Aluminium, height: 1f, bevel: 2f, roughness: 0.3f );
		painter.Fill( 0, Row( 10f ), WallSize, WallSize - Row( 10f ), Vinyl, height: 0.95f, roughness: 0.6f );

		// The weave and its heathered yarn, only on the fabric (the weave pattern is two pixels a thread).
		var heather = painter.Noise( 256, Seed + 601, 2 );
		var pills = painter.Noise( 64, Seed + 602, 2 );
		var weave = new float[WallSize * WallSize];
		var dark = new float[WallSize * WallSize];
		var light = new float[WallSize * WallSize];
		foreach ( var (top, bottom) in panels )
		{
			for ( var y = top + 3; y < bottom - 3; y++ )
			for ( var x = 0; x < WallSize; x++ )
			{
				var inModule = x % Module;
				if ( inModule < 12 || inModule > Module - 12 ) continue;
				var i = y * WallSize + x;
				var over = ((x >> 1) + (y >> 1)) & 1;
				weave[i] = over == 0 ? 0.04f : -0.04f;
				var tone = (heather[i] - 0.5f) * 1.4f + (pills[i] - 0.5f) * 0.4f;
				dark[i] = tone < 0f ? MathF.Min( -tone, 1f ) * 0.35f : 0f;
				light[i] = tone > 0f ? MathF.Min( tone, 1f ) * 0.3f : 0f;
			}
		}
		painter.AddHeight( weave, 1f );
		painter.Blend( dark, FabricDark );
		painter.Blend( light, FabricLight );

		painter.HeightNoise( 0.05f, 128, Seed + 603, 2 );
		painter.Mottle( 0.03f, 6, Seed + 604, 4 );
		painter.Cavity( 0.35f, 3 );
		painter.EdgeWear( 0.15f, 3, Seed + 605 );
		painter.Grain( 0.01f, Seed + 606 );
		return painter.Bake( "arena_3_wall", Painter.Matte, 4f );
	}

	/// <summary>
	/// Plastered columns and bulkhead in off-white, a dark vinyl cove skirting, an aluminium nosing
	/// on the slab edge; the infill band holds the window blind (twelve slats, pale sky between) and
	/// the white powder-coat of the frames.
	/// </summary>
	protected override PaintedSurface PaintTrim()
	{
		return PaintTrimAtlas( "arena_3_trim", Plaster, PlasterShade, 0.85f, 0f, Seed + 700, painter =>
		{
			TintBand( painter, TrimBand.Skirting, Vinyl, 0.6f, SkirtingProfile, Mix( Vinyl, Palette.Ink, 0.5f ), 0.5f );
			TintBand( painter, TrimBand.SlabEdge, Aluminium, 0.35f, NosingProfile, AluminiumShade, 0.6f );

			// The view out — pale sky over a hazy skyline — with the blind let down over the top half.
			var (x, y, w, h) = InfillPixels( 0 );
			for ( var row = 0; row < h; row++ )
				painter.Fill( x, y + row, w, 1, Mix( Color.White, Sky, 0.35f + row / (float)h * 0.65f ), height: 0.2f, roughness: 0.15f );
			var random = new Random( Seed + 701 );
			for ( var bx = x; bx < x + w; )
			{
				var bw = 8 + random.Next( 18 );
				var bh = 10 + random.Next( 26 );
				painter.Fill( bx, y + h - bh, Math.Min( bw, x + w - bx ), bh, Mix( Sky, PlasterShade, 0.45f + (float)random.NextDouble() * 0.2f ), height: 0.2f, roughness: 0.2f );
				bx += bw + random.Next( 4 );
			}
			const int Slats = 12;
			var blind = h * 0.55f;
			for ( var k = 0; k < Slats; k++ )
			{
				var pitch = blind / Slats;
				var sy = y + k * pitch + 1f;
				var slat = Mix( Slat, Color.White, (float)random.NextDouble() * 0.3f );
				painter.Rect( x, sy, w, pitch - 2f, slat, radius: 1f, height: 1f, bevel: 2f, roughness: 0.35f );
				painter.Rect( x, sy + (pitch - 2f) * 0.6f, w, (pitch - 2f) * 0.4f, Mix( Slat, PlasterShade, 0.5f ), alpha: 0.35f );
			}
			painter.Rect( x, y + blind, w, 5f, Mix( Slat, PlasterShade, 0.3f ), radius: 1f, height: 1.1f, bevel: 2f, roughness: 0.4f );
			foreach ( var cord in new[] { 30, 98 } )
				painter.Rect( x + cord, y, 1.5f, blind + 3f, Mix( Slat, PlasterShade, 0.4f ), height: 1.1f, roughness: 0.6f );

			var frame = InfillPixels( 1 );
			painter.Fill( frame.X, frame.Y, frame.Width, frame.Height, PowderCoat, roughness: 0.4f );
		} );
	}

	protected override PaintedSurface PaintFloor()
	{
		const int cell = FloorSize / 2;
		var painter = new Painter( FloorSize, Seam, 0.95f );
		var random = new Random( Seed );
		float Next() => (float)random.NextDouble();

		// Pile-direction shading, the hallmark of quarter-turned carpet tiles: the same carpet reads
		// clearly lighter or darker per tile depending on which way its pile lies to the eye.
		var tones = new[] { 0.55f, -0.45f, 0.15f, -0.75f };

		// Cut tiles are square but never laid perfectly: the seam at the chamfer's foot varies by a
		// pixel or two, and each tile sits a fraction of a degree askew. The pile runs on over the arris.
		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var tone = tones[c] + (Next() - 0.5f) * 0.1f;
			var carpet = tone < 0f ? Mix( Carpet, CarpetShade, -tone * 0.7f ) : Mix( Carpet, CarpetLight, tone * 0.6f );
			var left = 2f + Next() * 2.5f; var top = 2f + Next() * 2.5f;
			var right = 2f + Next() * 2.5f; var bottom = 2f + Next() * 2.5f;
			painter.Fill( (int)min.x, (int)min.y, cell, cell, Mix( carpet, Seam, 0.5f ), height: 0.75f );
			var middle = new Vector2( min.x + (left + cell - right) * 0.5f, min.y + (top + cell - bottom) * 0.5f );
			var halfSize = new Vector2( (cell - left - right) * 0.5f, (cell - top - bottom) * 0.5f );
			var skew = (Next() - 0.5f) * 0.4f * MathF.PI / 180f;
			var across = new Vector2( MathF.Cos( skew ), MathF.Sin( skew ) ) * halfSize.x;
			var down = new Vector2( -MathF.Sin( skew ), MathF.Cos( skew ) ) * halfSize.y;
			painter.Polygon( new[] { middle - across - down, middle + across - down, middle + across + down, middle - across + down }, carpet, height: 1f, bevel: 7f, roughness: 0.95f );
		}

		PaintPile( painter );

		// Fleck yarn: short dashes of periwinkle and of a deep ply along the pile rows, at low contrast.
		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var direction = c == 0 || c == 3 ? new Vector2( 1, 0 ) : new Vector2( 0, 1 );
			for ( var f = 0; f < 1300; f++ )
			{
				var at = min + new Vector2( 30f + Next() * 452f, 30f + Next() * 452f );
				var length = 2f + Next() * 4f;
				var deep = Next() < 0.4f;
				painter.Line( at, at + direction * length, 1.3f, deep ? Mix( Carpet, CarpetDeep, 0.8f ) : Mix( Carpet, Fleck, 0.7f ), deep ? 0.4f : 0.45f );
			}
		}

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
		return painter.Bake( "arena_3_floor", Painter.Matte, 4f );
	}

	/// <summary>
	/// Loop pile in rows, across on two tiles and down on the other two (the quarter-turn laying).
	/// Its colour is three scales of yarn variation blended softly, never thresholded into lines:
	///   * heather, a 2–4 px speckle of a dark and a light ply;
	///   * tufting, soft clouds a few units across where the pile sits denser or looser — also in
	///     height, so the pile still looks matte and uneven where mips have averaged the loops away;
	///   * faint streaks along each row, and alternate rows a hair lighter.
	/// Loops are offset row to row like real tufting.
	/// </summary>
	void PaintPile( Painter painter )
	{
		const int cell = FloorSize / 2;
		const int Stretch = 3;
		var streaks = painter.Noise( 64, Seed + 20, 2 );
		var heather = painter.Noise( 256, Seed + 21, 2 );
		var tufting = painter.Noise( 24, Seed + 22, 3 );
		var dark = new float[FloorSize * FloorSize];
		var light = new float[FloorSize * FloorSize];
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

					var tone = (heather[i] - 0.5f) * 1.3f + (tufting[i] - 0.5f) * 0.7f + (s - 0.5f) * 0.25f + ((pileRow & 1) == 0 ? 0.04f : -0.04f);
					dark[i] = tone < 0f ? MathF.Min( -tone * 1.3f, 1f ) * 0.5f : 0f;
					light[i] = tone > 0f ? MathF.Min( tone * 1.3f, 1f ) * 0.4f : 0f;

					var acrossPhase = across % PileRow;
					var alongPhase = (along + (pileRow & 1)) % PileRow;
					var acrossHeight = acrossPhase == 0 ? 0.15f : acrossPhase == 1 ? 1f : 0.6f;
					var alongHeight = alongPhase == 0 ? 0.35f : alongPhase == 1 ? 1f : 0.7f;
					loops[i] = (acrossHeight * alongHeight - 0.5f) * 0.16f + (tufting[i] - 0.5f) * 0.5f;
				}
			}
		}

		painter.Blend( dark, CarpetDeep );
		painter.Blend( light, CarpetLight );
		painter.AddHeight( loops, 1f );
	}

	/// <summary>
	/// Lay-in acoustic tiles, two by two on every underside, in a light T-bar grid: random pinholes
	/// and short fissures (never on a grid), a slightly different tone per panel, a dusty one, and a
	/// water stain with its tide-mark rings on one.
	/// </summary>
	protected override PaintedSurface PaintCeiling()
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
					var shrunk = Organic( centre, half * (1f - ring * 0.35f), 1f, Seed + 571 + ring );
					for ( var k = 0; k < shrunk.Length; k++ )
						painter.Line( shrunk[k], shrunk[(k + 1) % shrunk.Length], 2f, WaterStain, 0.3f - ring * 0.1f );
				}
			}
		}

		painter.Mottle( 0.03f, 3, Seed + 520, 5 );
		painter.Cavity( 0.35f, 3 );
		painter.Grain( 0.01f, Seed + 521 );
		return painter.Bake( "arena_3_ceiling", Painter.Matte, 3f );
	}
}
