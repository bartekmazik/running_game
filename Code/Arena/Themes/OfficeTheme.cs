using System;
using System.Collections.Generic;

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

	/// <summary>The partition module, about 54 units (a 1.35 m panel): five per wall repeat, so bays start on a whole panel.</summary>
	protected override float WallModule => WallRepeat / Modules;

	/// <summary>Partition modules per wall repeat: 256 px each, so every post and panel edge falls on a whole pixel.</summary>
	const int Modules = 5;

	// The partition's tiers, in units above the tile top: a vinyl kick, two fabric panels split by a
	// rail, a clerestory of frosted glass, and the top rail; plaster above it to the bulkhead. Panels
	// keep a real height, so the taller room gets the glazed tier rather than taller fabric.

	/// <summary>Top of the vinyl kick.</summary>
	const float Kick = 10f;

	/// <summary>Foot of the rail between the two fabric panels.</summary>
	const float LowerRail = 90f;

	/// <summary>Foot of the rail over the fabric, under the clerestory; the windows' heads meet it.</summary>
	const float UpperRail = 160f;

	/// <summary>Head of the clerestory glass, under the top rail.</summary>
	const float GlassTop = 214f;

	/// <summary>Top of the partition's head rail; plaster runs on above it to the bulkhead.</summary>
	const float PartitionTop = 218f;

	/// <summary>Height of the lower and upper rails.</summary>
	const float RailHeight = 3f;

	/// <summary>
	/// Window sill. The window fills the fabric tier from here up to <see cref="UpperRail"/>, so its
	/// head is flush with the rail and the clerestory runs on unbroken above every window, as office
	/// glazing does (with the head mid-clerestory the frame cut across the milky band, 6c review).
	/// </summary>
	const float WindowSill = 52f;

	static Color Frosted => Palette.Hex( 0xD5DBDF );

	/// <summary>Pile rows across the yarn, in pixels: about a third of a unit, close to real loop pile.</summary>
	const int PileRow = 3;

	/// <summary>
	/// The structural front layer — plastered columns, a bulkhead beam, a vinyl cove skirting — and
	/// windows in about half the bays, each with a venetian blind let down to its own height; then the
	/// dressing. Under the bulkhead all round, a strip of suspended ceiling grid with cool-white light
	/// panels in it, six of them holding the room's lamps. Under each window a radiator, a low cupboard
	/// or a two-drawer cabinet, and now and then something on the sill. Every plain bay is dressed low and
	/// high: one bay a wall holds its potted plant, two walls a water cooler, the rest are dealt from five
	/// sets — a whiteboard over a low cupboard, a pinboard by a filing cabinet, a row of filing cabinets, a
	/// desk against the wall with its chair tucked in, open shelving full of files — with prints, shelves of
	/// binders and wall clocks over them, and an air-conditioner high on the plaster here and there. Each
	/// wall draws from its own stream.
	/// </summary>
	public override void Dress( FloorContext room )
	{
		var bays = BuildFrontLayer( room, new FrontStyle { Post = 28f, Pier = 72f, Corner = 30f, Cornice = Bulkhead, Skirting = 10f, SkirtingDepth = 3f } );
		var windows = AddWindows( room, bays, InfillCell( 0 ), FrameSwatch, WindowSill, UpperRail - WindowSill, 124f, 0.5f, false, Seed + 800 );
		// Self-illumination is an exponent (2^b): a little under the paint, the panels read lit and cool
		// without blooming the bulkhead around them to white.
		Glow.Set( "g_flSelfIllumBrightness", -0.4f );
		var props = new Props( room, Trim, Glow, Ceiling );
		var lamps = LampSpots( 6, Seed + 900 );

		var walls = new Random( Seed + 1700 );
		var cooler = walls.Next( 4 );
		var otherCooler = (cooler + 1 + walls.Next( 3 )) % 4;

		for ( var side = 0; side < 4; side++ )
		{
			props.Begin( side, Seed + 1000 + side * 137 );
			props.Grid( lamps.FindAll( l => l.Side == side ).ConvertAll( l => l.U ) );

			foreach ( var (bay, width) in windows.FindAll( w => w.Bay.Side == side ) )
				props.WindowBay( bay, width );

			// The plain bays in a shuffled order: the wall's plant in the first, a cooler in the next on two
			// walls, then this wall's own order of the five sets dealt round.
			var wall = bays.FindAll( b => b.Side == side );
			var plain = wall.FindAll( b => b.Width > 60f && !windows.Exists( w => w.Bay.Side == side && w.Bay.From == b.From ) );
			Shuffle( plain, props );
			var deck = new List<Props.Look> { Props.Look.Whiteboard, Props.Look.Pinboard, Props.Look.Cabinets, Props.Look.Desk, Props.Look.Shelving };
			Shuffle( deck, props );
			var dealt = 0;
			for ( var k = 0; k < plain.Count; k++ )
			{
				if ( k == 0 ) props.PlantBay( plain[k] );
				else if ( k == 1 && (side == cooler || side == otherCooler) ) props.CoolerBay( plain[k] );
				else props.Furnish( deck[dealt++ % deck.Count], plain[k] );
			}

			// A split air-conditioner high on the plaster over one or two of the plain bays.
			for ( var k = 0; k < plain.Count; k++ )
				if ( k % 3 == 1 ) props.AirCon( plain[k] );
		}

		foreach ( var lamp in lamps )
			AddLamp( room, lamp.Side, Props.Cell( lamp.Side, lamp.U ), Props.PanelLight, Props.PanelDepth, LightColor * 3.6f * lamp.Brightness );
	}

	/// <summary>Height of the bulkhead beam under the ceiling; the grid strip hangs under it.</summary>
	const float Bulkhead = 26f;

	/// <summary>Shuffles <paramref name="list"/> in place from the props' stream (Fisher–Yates).</summary>
	static void Shuffle<T>( List<T> list, Props props )
	{
		for ( var k = list.Count - 1; k > 0; k-- )
		{
			var j = (int)(props.Next() * (k + 1));
			(list[k], list[j]) = (list[j], list[k]);
		}
	}

	/// <summary>
	/// System partitions in <see cref="WallModule"/> modules: aluminium posts with a shadow groove, a
	/// dark vinyl kick, a lower and an upper fabric panel split by a rail, a clerestory of frosted glass
	/// over them, plaster above to the bulkhead. The fabric is a fine weave with a heathered yarn, every
	/// panel its own small drift in tone and hue (no single odd panel: anything one-off in one repeat
	/// comes back in every bay, 6a review / R18), with the faint ghosts of notices that hung there and
	/// the pinholes that held them; the glass is milky, a shade lighter toward its head, each pane its
	/// own faint tint.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		const float Module = WallSize / (float)Modules;
		int Row( float v ) => (int)MathF.Round( (WallRepeat - v) * Ppu );
		var painter = new Painter( WallSize, Plaster, 0.85f );
		var random = new Random( Seed + 600 );
		float Next() => (float)random.NextDouble();

		var head = Row( PartitionTop );
		painter.Fill( 0, 0, WallSize, head, Plaster, height: 0.8f, roughness: 0.85f );
		painter.Fill( 0, head, WallSize, WallSize - head, Aluminium, height: 0.9f, roughness: 0.35f );

		var panels = new[] { (Top: Row( UpperRail ), Bottom: Row( LowerRail + RailHeight )), (Top: Row( LowerRail ), Bottom: Row( Kick )) };
		for ( var m = 0; m < Modules; m++ )
		{
			for ( var p = 0; p < 2; p++ )
			{
				var (panelTop, panelBottom) = panels[p];
				var x0 = m * Module + 9f;
				var tone = Next() * 2f - 1f;
				var fabric = tone < 0f ? Mix( Fabric, FabricDark, -tone * 0.18f ) : Mix( Fabric, FabricLight, tone * 0.2f );
				fabric = Mix( fabric, FabricBlue, Next() * 0.12f );
				painter.Rect( x0, panelTop + 1f, Module - 18f, panelBottom - panelTop - 2f, fabric, radius: 2f, height: 0.7f, bevel: 3f, roughness: 0.95f );

				// The ghost of a notice: a paler rectangle where the fabric was kept out of the light, and its pinholes.
				if ( Next() < 0.3f )
				{
					var w = 40f + Next() * 60f; var h = 50f + Next() * 70f;
					var nx = x0 + 15f + Next() * (Module - 48f - w); var ny = panelTop + 20f + Next() * MathF.Max( 1f, panelBottom - panelTop - 40f - h );
					painter.Rect( nx, ny, w, h, FabricLight, alpha: 0.06f );
					foreach ( var (cx, cy) in new[] { (nx + 3f, ny + 3f), (nx + w - 3f, ny + 3f), (nx + 3f, ny + h - 3f), (nx + w - 3f, ny + h - 3f) } )
						painter.Circle( cx + (Next() - 0.5f) * 2f, cy + (Next() - 0.5f) * 2f, 0.8f, FabricDark, 0.6f );
				}
			}

			// The clerestory pane: milky glass set back in its frame, lighter toward the head.
			{
				var glassTop = Row( GlassTop ); var glassBottom = Row( UpperRail + RailHeight );
				var x0 = m * Module + 9f;
				var pane = Mix( Frosted, FabricBlue, Next() * 0.1f );
				painter.Rect( x0, glassTop + 1f, Module - 18f, glassBottom - glassTop - 2f, pane, radius: 1f, height: 0.6f, bevel: 2f, roughness: 0.25f );
				var span = glassBottom - glassTop;
				for ( var y = 0; y < span * 0.6f; y++ )
					painter.Rect( x0, glassTop + 1f + y, Module - 18f, 1f, Color.White, alpha: 0.12f * (1f - y / (span * 0.6f)) );
			}

			// The post between modules, with its shadow groove.
			var post = m * Module;
			painter.Rect( post - 9f, head, 18f, WallSize - head, Aluminium, height: 1f, bevel: 3f, roughness: 0.3f );
			painter.Rect( post - 1.5f, head, 3f, WallSize - head, AluminiumShade, height: 0.6f, roughness: 0.5f );
		}

		foreach ( var rail in new[] { LowerRail, UpperRail } )
			painter.Rect( 0f, Row( rail + RailHeight ), WallSize, Row( rail ) - Row( rail + RailHeight ), Aluminium, height: 1f, bevel: 2f, roughness: 0.3f );
		painter.Rect( 0f, head, WallSize, Row( GlassTop ) - head, Aluminium, height: 1f, bevel: 2f, roughness: 0.3f );
		painter.Fill( 0, Row( Kick ), WallSize, WallSize - Row( Kick ), Vinyl, height: 0.95f, roughness: 0.6f );

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
				var inModule = x - MathF.Floor( x / Module ) * Module;
				if ( inModule < 12f || inModule > Module - 12f ) continue;
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
	/// on the slab edge; the infill band holds the view out (pale sky over a hazy skyline — the blinds
	/// in front of it are props now, each let down to its own height), the white powder-coat of the
	/// frames, and the props' materials.
	/// </summary>
	protected override PaintedSurface PaintTrim()
	{
		return PaintTrimAtlas( "arena_3_trim", Plaster, PlasterShade, 0.85f, 0f, Seed + 700, painter =>
		{
			TintBand( painter, TrimBand.Skirting, Vinyl, 0.6f, SkirtingProfile, Mix( Vinyl, Palette.Ink, 0.5f ), 0.5f );
			TintBand( painter, TrimBand.SlabEdge, Aluminium, 0.35f, NosingProfile, AluminiumShade, 0.6f );

			// The view out: pale sky over a hazy skyline.
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
			var frame = InfillPixels( 1 );
			painter.Fill( frame.X, frame.Y, frame.Width, frame.Height, PowderCoat, roughness: 0.4f );

			PaintSwatches( painter, new Random( Seed + 702 ) );
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

	/// <summary>
	/// The lit parts of the light panels, on the self-lit base: a cool-white prismatic diffuser (a fine
	/// grid of lenses, a little brighter in the middle), and a dimmer, greyer one for the odd panel whose
	/// tubes are failing — every office has one.
	/// </summary>
	protected override PaintedSurface PaintGlow()
	{
		const int Size = 64;
		var painter = new Painter( Size, Palette.Hex( 0xEEF3FA ), 0.3f );
		painter.Fill( 32, 0, 32, 64, Palette.Hex( 0xCDD2D8 ), roughness: 0.35f );
		var glow = new float[Size * Size];
		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
		{
			var lens = (x % 4 == 0 || y % 4 == 0) ? 0.88f : 1f;
			var middle = 1f - MathF.Abs( (x % 32) - 15.5f ) / 15.5f * 0.12f;
			glow[y * Size + x] = x < 32 ? lens * middle : 0.22f * lens;
		}
		painter.AddGlow( glow );
		return painter.Bake( "arena_3_glow", ArenaGenerator.GlowMaterialPath, 1f );
	}

	static Rect PanelGlow => new( 0.04f, 0.04f, 0.42f, 0.92f );
	static Rect DeadGlow => new( 0.54f, 0.04f, 0.42f, 0.92f );

	/// <summary>The whiteboard sheet: a crop of it is each board's face.</summary>
	const int SheetX = 256, SheetWidth = 192, SheetHeight = 96;

	static Rect ClockSwatch => Swatch( 448, 0, 64, 64 );

	/// <summary>Print colours: eight solid pastels and accents in the frame cell's spare corner.</summary>
	static Rect ArtSwatch( int k ) => Swatch( 192 + (k % 4) * 16, (k / 4) * 32, 16, 32 );
	static readonly uint[] Art = { 0xF4E1A1, 0xF2C6C2, 0xBFE3CF, 0xA9B6D8, 0x9FC6E0, 0xE9A06A, 0x9DB89A, 0xC7B8E0 };

	/// <summary>The window frames' white powder coat: a solid part of the frame cell.</summary>
	static Rect FrameSwatch => Swatch( 128, 64, 128, 96 );
	static Rect MarkerSwatch( int k ) => Swatch( 448 + k * 8, 64, 8, 32 );
	static Rect CabinetSwatch( int k ) => Swatch( 256 + k * 32, 96, 32, 32 );
	static Rect CorkSwatch => Swatch( 352, 96, 32, 32 );
	static Rect AluminiumSwatch => Swatch( 416, 96, 32, 32 );
	static Rect BottleSwatch => Swatch( 448, 96, 32, 32 );
	static Rect WhiteSwatch => Swatch( 480, 96, 32, 32 );
	static Rect LeafSwatch( int k ) => Swatch( 256 + k * 64, 128, 64, 32 );
	static Rect PotSwatch => Swatch( 384, 128, 16, 32 );
	static Rect CharcoalSwatch => Swatch( 400, 128, 16, 32 );
	static Rect SoilSwatch => Swatch( 416, 128, 32, 32 );

	/// <summary>One of the paper swatch's four quarters: white, butter, pink and mint notes, ruled.</summary>
	static Rect PaperSwatch( int k ) => Swatch( 384 + (k % 2) * 16, 96 + (k / 2) * 16, 16, 16 );

	/// <summary>A binder's spine: six colours side by side, each a column of the swatch.</summary>
	static Rect BinderSwatch( int k ) => Swatch( 448 + (k % 6) * 10f + 1f, 128, 10f, 32 );

	/// <summary>Filing cabinet paint: putty, cool grey and a periwinkle (the floor's hue).</summary>
	static readonly uint[] CabinetPaints = { 0xC9C4B8, 0xB2B6BD, 0xA9B6D8 };

	/// <summary>Marker inks: black, blue, a carrot red and green.</summary>
	static readonly uint[] Inks = { 0x2F2A3B, 0x3F63A8, 0xD0704F, 0x3F8A5A };

	/// <summary>
	/// The props' materials, painted into the infill band's two free cells (the first two hold the view
	/// out and the frame paint): the whiteboard sheet — the ghosts of wiped meetings under boxes and
	/// arrows, a chart, lines of handwriting, sticky notes — a clock face, marker inks, three cabinet
	/// paints, cork, ruled notes, aluminium, the cooler's blue bottle and white plastic, two leaves, a
	/// white and a charcoal pot, soil and a row of binder spines; and eight pastels for the prints.
	/// </summary>
	static void PaintSwatches( Painter painter, Random random )
	{
		float Next() => (float)random.NextDouble();
		var band = TrimRows( TrimBand.Infill ).Top;

		for ( var k = 0; k < Art.Length; k++ )
			painter.Fill( 192 + (k % 4) * 16, band + (k / 4) * 32, 16, 32, Palette.Hex( Art[k] ), roughness: 0.85f );

		{
			var y0 = band;
			painter.Fill( SheetX, y0, SheetWidth, SheetHeight, Palette.Hex( 0xF3F4F2 ), roughness: 0.12f );
			// Ghosts of wiped writing: faint grey smears.
			for ( var s = 0; s < 16; s++ )
				painter.Polygon( Organic( new Vector2( SheetX + Next() * SheetWidth, y0 + Next() * SheetHeight ), new Vector2( 6f + Next() * 16f, 2f + Next() * 5f ), 0.7f, random.Next(), Next() * 0.5f ),
					Palette.Hex( 0xC9CBCE ), alpha: 0.18f, roughness: 0.2f );

			Color Ink() => Palette.Hex( Inks[Next() < 0.55f ? 0 : 1 + random.Next( 3 )] );
			void Scrawl( Vector2 at, float width, Color ink )
			{
				var points = new List<Vector2>();
				for ( var x = 0f; x < width; x += 1.2f + Next() * 0.8f )
					points.Add( at + new Vector2( x, (Next() - 0.5f) * 2.6f ) );
				for ( var k = 0; k + 1 < points.Count; k++ ) painter.Line( points[k], points[k + 1], 0.9f, ink, 0.85f );
			}

			// Boxes joined by arrows, chart, handwriting and notes scattered over the sheet, no two crops alike.
			for ( var b = 0; b < 9; b++ )
			{
				var at = new Vector2( SheetX + 6f + Next() * (SheetWidth - 40f), y0 + 6f + Next() * (SheetHeight - 30f) );
				var ink = Ink();
				switch ( random.Next( 5 ) )
				{
					case 0:
					{
						var w = 18f + Next() * 16f; var h = 9f + Next() * 7f;
						painter.Line( at, at + new Vector2( w, 0f ), 1f, ink, 0.85f );
						painter.Line( at + new Vector2( w, 0f ), at + new Vector2( w, h ), 1f, ink, 0.85f );
						painter.Line( at + new Vector2( w, h ), at + new Vector2( 0f, h ), 1f, ink, 0.85f );
						painter.Line( at + new Vector2( 0f, h ), at, 1f, ink, 0.85f );
						Scrawl( at + new Vector2( 3f, h * 0.5f ), w - 6f, ink );
						var to = at + new Vector2( w + 6f + Next() * 14f, (Next() - 0.5f) * 20f );
						painter.Line( at + new Vector2( w, h * 0.5f ), to, 0.9f, ink, 0.8f );
						painter.Line( to, to + new Vector2( -3f, -2f ), 0.9f, ink, 0.8f );
						painter.Line( to, to + new Vector2( -3f, 2f ), 0.9f, ink, 0.8f );
						break;
					}
					case 1:
					{
						// A bar chart on two axes.
						painter.Line( at, at + new Vector2( 0f, 20f ), 1f, ink, 0.85f );
						painter.Line( at + new Vector2( 0f, 20f ), at + new Vector2( 26f, 20f ), 1f, ink, 0.85f );
						for ( var k = 0; k < 4; k++ )
						{
							var h = 5f + Next() * 13f;
							painter.Rect( at.x + 3f + k * 6f, at.y + 20f - h, 3.5f, h, Palette.Hex( Inks[1 + (k + b) % 3] ), alpha: 0.75f );
						}
						break;
					}
					case 2:
					{
						// A pie with its slices.
						var r = 7f + Next() * 4f;
						var centre = at + new Vector2( r, r );
						for ( var k = 0; k < 24; k++ )
						{
							var a0 = k * MathF.Tau / 24f; var a1 = (k + 1) * MathF.Tau / 24f;
							painter.Line( centre + new Vector2( MathF.Cos( a0 ), MathF.Sin( a0 ) ) * r, centre + new Vector2( MathF.Cos( a1 ), MathF.Sin( a1 ) ) * r, 1f, ink, 0.85f );
						}
						for ( var k = 0; k < 3; k++ )
						{
							var a = Next() * MathF.Tau;
							painter.Line( centre, centre + new Vector2( MathF.Cos( a ), MathF.Sin( a ) ) * r, 0.9f, ink, 0.8f );
						}
						break;
					}
					case 3:
					{
						// Lines of handwriting, a bullet before each, one underlined.
						var lines = 2 + random.Next( 4 );
						var width = 24f + Next() * 30f;
						for ( var k = 0; k < lines; k++ )
						{
							var line = at + new Vector2( 0f, k * 5.5f );
							painter.Circle( line.x, line.y, 0.8f, ink, 0.85f );
							Scrawl( line + new Vector2( 3f, 0f ), width * (0.6f + Next() * 0.4f), ink );
						}
						painter.Line( at + new Vector2( 3f, 2.8f ), at + new Vector2( 3f + width * 0.7f, 3.2f ), 0.8f, Palette.Hex( Inks[2] ), 0.8f );
						break;
					}
					default:
					{
						// A sticky note or two.
						for ( var k = 1 + random.Next( 2 ); k > 0; k-- )
						{
							var note = new[] { 0xF4E1A1, 0xF2C6C2, 0xBFE3CF }[random.Next( 3 )];
							painter.Rect( at.x + Next() * 6f, at.y + Next() * 6f, 9f, 9f, Palette.Hex( (uint)note ), roughness: 0.8f, angle: (Next() - 0.5f) * 0.3f );
						}
						break;
					}
				}
			}

			// The clock face: white, twelve ticks (the quarters heavier) round a ring, mapped inside the cap's circle.
			painter.Fill( 448, y0, 64, 64, Palette.Hex( 0xF7F6F1 ), roughness: 0.2f );
			var hub = new Vector2( 480f, y0 + 32f );
			for ( var k = 0; k < 60; k++ )
			{
				var a = k * MathF.Tau / 60f;
				var dir = new Vector2( MathF.Cos( a ), MathF.Sin( a ) );
				var hour = k % 5 == 0;
				var quarter = k % 15 == 0;
				painter.Line( hub + dir * (hour ? (quarter ? 17f : 19.5f) : 22f), hub + dir * 24f, quarter ? 2.2f : hour ? 1.4f : 0.5f, Palette.Ink, 0.9f );
			}

			for ( var k = 0; k < 4; k++ ) painter.Fill( 448 + k * 8, y0 + 64, 8, 32, Palette.Hex( Inks[k] ), roughness: 0.4f );

		}

		{
			var y0 = band + 96;
			for ( var k = 0; k < 3; k++ )
			{
				var paint = Palette.Hex( CabinetPaints[k] );
				painter.Fill( 256 + k * 32, y0, 32, 32, paint, roughness: 0.45f );
				for ( var s = 0; s < 6; s++ ) painter.Circle( 256f + k * 32 + Next() * 32f, y0 + Next() * 32f, 0.6f + Next(), Mix( paint, Palette.Ink, 0.25f ), 0.3f );
			}

			// Cork: speckled brown, old pinholes.
			painter.Fill( 352, y0, 32, 32, Palette.Hex( 0xB98E62 ), roughness: 0.95f );
			for ( var s = 0; s < 120; s++ )
				painter.Circle( 352f + Next() * 32f, y0 + Next() * 32f, 0.4f + Next() * 0.6f, Next() < 0.5f ? Palette.Hex( 0x8E6A46 ) : Palette.Hex( 0xD3AE80 ), 0.8f );

			// Notes, ruled: white, butter, pink, mint.
			var notes = new uint[] { 0xF6F4EE, 0xF4E1A1, 0xF2C6C2, 0xBFE3CF };
			for ( var k = 0; k < 4; k++ )
			{
				var x0 = 384 + (k % 2) * 16; var ny = y0 + (k / 2) * 16;
				painter.Fill( x0, ny, 16, 16, Palette.Hex( notes[k] ), roughness: 0.85f );
				for ( var y = 5; y < 14; y += 2 ) painter.Fill( x0 + 4, ny + y, 8 - (y * 3) % 5, 1, Palette.Hex( 0x8C8A92 ) );
			}

			painter.Fill( 416, y0, 32, 32, Aluminium, roughness: 0.3f );
			for ( var y = 0; y < 32; y += 2 ) painter.Fill( 416, y0 + y, 32, 1, AluminiumShade, roughness: 0.35f );

			// The cooler bottle: pale blue plastic, ribbed, a highlight down one side.
			painter.Fill( 448, y0, 32, 32, Palette.Hex( 0xBFD8E8 ), roughness: 0.1f );
			for ( var y = 4; y < 32; y += 7 ) painter.Fill( 448, y0 + y, 32, 2, Palette.Hex( 0xA6C6DB ), roughness: 0.1f );
			painter.Fill( 455, y0, 4, 32, Palette.Hex( 0xE4F0F7 ), roughness: 0.05f );
			painter.Fill( 480, y0, 32, 32, Palette.Hex( 0xF1F0EC ), roughness: 0.45f );
		}

		{
			var y0 = band + 128;
			// Leaves: a snake plant's banded sword (dark green with grey-green bands and a butter margin),
			// and a rubber plant's glossy deep green with a pale midrib.
			painter.Fill( 256, y0, 64, 32, Palette.Hex( 0x3E6447 ), roughness: 0.4f );
			for ( var x = 256f; x < 320f; x += 3f + Next() * 4f )
				painter.Line( new Vector2( x, y0 + 4f ), new Vector2( x + 2f, y0 + 28f ), 1.4f, Palette.Hex( 0x7E9C7E ), 0.5f );
			painter.Fill( 256, y0, 64, 4, Palette.Hex( 0xD8D58E ), roughness: 0.4f );
			painter.Fill( 256, y0 + 28, 64, 4, Palette.Hex( 0xD8D58E ), roughness: 0.4f );
			for ( var x = 0; x < 64; x++ )
				painter.Fill( 320 + x, y0, 1, 32, Mix( Palette.Hex( 0x46704A ), Palette.Hex( 0x2F5238 ), x / 63f ), roughness: 0.25f );
			painter.Fill( 320, y0 + 15, 64, 2, Palette.Hex( 0x8FA87F ), roughness: 0.3f );

			painter.Fill( 384, y0, 16, 32, Palette.Hex( 0xEDEBE5 ), roughness: 0.3f );
			painter.Fill( 400, y0, 16, 32, Palette.Hex( 0x4A4B50 ), roughness: 0.5f );
			painter.Fill( 416, y0, 32, 32, Palette.Hex( 0x4F3E31 ), roughness: 0.95f );
			for ( var s = 0; s < 30; s++ ) painter.Circle( 416f + Next() * 32f, y0 + Next() * 32f, 0.5f + Next() * 0.8f, Palette.Hex( 0x6F5A47 ), 0.7f );

			var spines = new uint[] { 0x3F63A8, 0x2F2A3B, 0xC9C4B8, 0x7A9E7E, 0xD0704F, 0xA9B6D8 };
			for ( var k = 0; k < 6; k++ )
			{
				painter.Fill( 448 + k * 10 + 1, y0, 10, 32, Palette.Hex( spines[k] ), roughness: 0.4f );
				painter.Fill( 448 + k * 10 + 3, y0 + 8, 6, 9, Palette.Hex( 0xF6F4EE ), roughness: 0.8f );
				painter.Circle( 448 + k * 10 + 6f, y0 + 25f, 1.6f, Mix( Palette.Hex( spines[k] ), Palette.Ink, 0.4f ), 1f );
			}
		}
	}

	/// <summary>
	/// Builds the office's props into the floor's kit, in wall space (see <see cref="WallProps"/>). What
	/// a player would use — cabinets, the cooler, boards, the desk and its chair — is at a player's
	/// size; each prop group draws from a stream of its own (<see cref="WallProps.Apart"/>).
	/// </summary>
	sealed class Props : WallProps
	{
		/// <summary>Where a panel's lamp burns, just under the grid strip, and how far out from the bulkhead.</summary>
		public const float PanelLight = Strip - 3f;
		public const float PanelDepth = -9f;

		/// <summary>The underside of the bulkhead, which the grid strip hangs under.</summary>
		const float Strip = FloorContext.ClearHeight - Bulkhead;

		/// <summary>How far the grid strip reaches out over the room.</summary>
		const float Reach = 18f;

		/// <summary>One cell of the grid strip along the wall: regular, as a real grid is.</summary>
		const float CellLength = 48f;

		/// <summary>The face of the skirting, which things on the ledge stand against.</summary>
		const float Skirt = FloorContext.Recess - 3.5f;

		static Vector3 ToRoom => new( 0f, -1f, 0f );

		readonly Material _trim;
		readonly Material _glow;
		readonly Material _ceiling;

		public Props( FloorContext room, Material trim, Material glow, Material ceiling ) : base( room )
		{
			_trim = trim;
			_glow = glow;
			_ceiling = ceiling;
		}

		/// <summary>The set-ups a plain bay is dealt, after the plant's and the cooler's.</summary>
		public enum Look { Whiteboard, Pinboard, Cabinets, Desk, Shelving }

		Skin TrimSkin( Rect region ) => new( _trim, region );
		Skin Metal => TrimSkin( AluminiumSwatch );

		/// <summary>The middle of the grid cell holding <paramref name="u"/> on wall <paramref name="side"/>: where a panel and its lamp go.</summary>
		public static float Cell( int side, float u ) => First( side ) + (MathF.Floor( (u - First( side )) / Size( side ) ) + 0.5f) * Size( side );

		/// <summary>How many cells wall <paramref name="side"/>'s strip holds, and how long each is: as near <see cref="CellLength"/> as fills it exactly.</summary>
		static int Cells( int side ) => (int)MathF.Round( (FloorContext.Span - 2f * First( side )) / CellLength );
		static float Size( int side ) => (FloorContext.Span - 2f * First( side )) / Cells( side );

		/// <summary>Where a wall's strip starts: walls 1 and 3 stop at the corner squares the other two run through.</summary>
		static float First( int side ) => side % 2 == 0 ? 0f : Reach;

		/// <summary>
		/// The strip of suspended ceiling under the bulkhead: a main tee along its front and a cross tee
		/// every cell, acoustic tiles in the cells (the room's own ceiling tile) and, in some, a light panel
		/// — always in the cells over the wall's lamps (<paramref name="lamps"/>), otherwise every few cells,
		/// irregularly, with now and then a panel whose tubes are failing and a tile pushed askew.
		/// </summary>
		public void Grid( List<float> lamps ) => Apart( () =>
		{
			var first = First( Side );
			var last = FloorContext.Span - first;
			var count = Cells( Side );
			var length = Size( Side );
			Kit.Box( Metal, At( new Vector3( (first + last) * 0.5f, -Reach - 0.6f, Strip - 0.9f ) ), new Vector3( last - first, 1.2f, 1.8f ), Run );

			var gap = 0;
			for ( var c = 0; c < count; c++ )
			{
				var u0 = first + c * length;
				var middle = u0 + length * 0.5f;
				Kit.Box( Metal, At( new Vector3( u0, -Reach * 0.5f, Strip - 0.75f ) ), new Vector3( 1.2f, Reach, 1.5f ), Run );

				var lit = lamps.Exists( l => MathF.Abs( Cell( Side, l ) - middle ) < 1f );
				gap++;
				if ( !lit && gap >= 3 && Next() < 0.45f ) lit = true;
				if ( lit ) gap = 0;

				var inset = 0.6f;
				var a = new Vector3( u0 + inset, -Reach + inset, Strip - 0.4f );
				var b = new Vector3( u0 + length - inset, -Reach + inset, Strip - 0.4f );
				var cc = new Vector3( u0 + length - inset, -inset, Strip - 0.4f );
				var d = new Vector3( u0 + inset, -inset, Strip - 0.4f );
				if ( lit )
				{
					// A lay-in troffer: the diffuser a little below the grid, in a white frame.
					var dead = !lamps.Exists( l => MathF.Abs( Cell( Side, l ) - middle ) < 1f ) && Next() < 0.12f;
					Kit.BevelBox( TrimSkin( WhiteSwatch ), At( new Vector3( middle, -Reach * 0.5f, Strip - 1.3f ) ), new Vector3( length - 3f, Reach - 3f, 1.6f ), 0.4f, Run );
					var glow = new Skin( _glow, dead ? DeadGlow : PanelGlow );
					Kit.Quad( glow, At( a.WithZ( Strip - 2.35f ) + new Vector3( 2.2f, 2.2f, 0f ) ), At( d.WithZ( Strip - 2.35f ) + new Vector3( 2.2f, -2.2f, 0f ) ),
						At( cc.WithZ( Strip - 2.35f ) + new Vector3( -2.2f, -2.2f, 0f ) ), At( b.WithZ( Strip - 2.35f ) + new Vector3( -2.2f, 2.2f, 0f ) ) );
					continue;
				}

				// An acoustic tile from the room's own ceiling, one in a while pushed up askew, its edge dark.
				var askew = Next() < 0.06f;
				var lift = askew ? new Vector3( 0f, 0f, 1.2f ) : Vector3.Zero;
				var tile = new Skin( _ceiling, new Rect( Next() * 0.4f, Next() * 0.4f, length / 112f, Reach / 112f ) );
				Kit.Quad( tile, At( a + lift ), At( d + lift * (askew ? 0f : 1f) ), At( cc + lift * (askew ? 0f : 1f) ), At( b + lift ) );
			}
			Kit.Box( Metal, At( new Vector3( first + count * length, -Reach * 0.5f, Strip - 0.75f ) ), new Vector3( 1.2f, Reach, 1.5f ), Run );
		} );

		// ---- Bays ------------------------------------------------------------------------------

		/// <summary>
		/// A window bay: a venetian blind in the window; under the sill a radiator, a low cupboard or a
		/// two-drawer cabinet; now and then a plant, a mug or a stack of papers on the sill.
		/// </summary>
		public void WindowBay( Bay bay, float width ) => Apart( () =>
		{
			Blind( bay.Middle, width );
			var under = Next();
			if ( under < 0.45f ) Radiator( bay.Middle + Range( -4f, 4f ), MathF.Min( width - Range( 8f, 20f ), bay.Width - 20f ) );
			else if ( under < 0.75f ) Cupboard( bay.Middle + Range( -6f, 6f ), MathF.Min( width + Range( -10f, 10f ), bay.Width - 16f ), Range( 26f, 30f ) );
			else
			{
				var side = Next() < 0.5f ? -1f : 1f;
				Cabinet( bay.Middle + side * Range( 10f, width * 0.3f ), 2 );
			}

			var sill = Next();
			var at = bay.Middle + Range( -0.35f, 0.35f ) * width;
			if ( sill < 0.35f )
			{
				SmallPlant( new Vector3( at, Face - 6f, WindowSill ) );
				if ( Next() < 0.5f ) SmallPlant( new Vector3( at + (at > bay.Middle ? -1f : 1f) * Range( 8f, 12f ), Face - 6f, WindowSill ) );
			}
			else if ( sill < 0.5f ) Mug( new Vector3( at, Face - 6f, WindowSill ) );
			else if ( sill < 0.7f ) Papers( new Vector3( at, Face - 6.5f, WindowSill ), 2 + Pick( 4 ) );
		} );

		/// <summary>The wall's plant bay: a big potted plant to one side with a print over it, a pinboard or a whiteboard beside it.</summary>
		public void PlantBay( Bay bay ) => Apart( () =>
		{
			var side = Next() < 0.5f ? -1f : 1f;
			var u = bay.Middle + side * bay.Width * Range( 0.2f, 0.28f );
			Plant( u );
			if ( Next() < 0.7f ) Print( u + Range( -4f, 4f ), Range( 118f, 132f ), Range( 30f, 40f ), Range( 38f, 48f ) );
			var board = bay.Middle - side * bay.Width * Range( 0.1f, 0.18f );
			if ( Next() < 0.5f ) Pinboard( board, Range( 54f, 70f ), Range( 40f, 50f ), Range( 58f, 66f ) );
			else Whiteboard( board, MathF.Min( bay.Width * 0.55f, Range( 70f, 90f ) ), Range( 44f, 52f ), Range( 50f, 56f ) );
			if ( Next() < 0.35f ) Clock( board + Range( -8f, 8f ), Range( 132f, 146f ) );
		} );

		/// <summary>A cooler bay: the water cooler with a notice over it; a cupboard or a cabinet beside it under a print or a shelf.</summary>
		public void CoolerBay( Bay bay ) => Apart( () =>
		{
			var side = Next() < 0.5f ? -1f : 1f;
			var u = bay.Middle + side * bay.Width * Range( 0.15f, 0.25f );
			Cooler( u );
			Pinboard( u + Range( -4f, 4f ), Range( 36f, 46f ), Range( 28f, 34f ), Range( 72f, 80f ) );
			var other = bay.Middle - side * bay.Width * Range( 0.15f, 0.25f );
			if ( Next() < 0.5f ) Cabinet( other, 4 );
			else Cupboard( other, MathF.Min( bay.Width * 0.4f, Range( 44f, 60f ) ), Range( 26f, 30f ) );
			if ( Next() < 0.6f ) Print( other + Range( -4f, 4f ), Range( 112f, 128f ), Range( 30f, 42f ), Range( 38f, 50f ) );
			else WallShelf( other, Range( 46f, 60f ), Range( 96f, 106f ) );
			if ( Next() < 0.4f ) Clock( u + Range( -6f, 6f ), Range( 134f, 148f ) );
		} );

		/// <summary>One of the five set-ups in <paramref name="bay"/>, about a third with a wall clock over it.</summary>
		public void Furnish( Look look, Bay bay ) => Apart( () =>
		{
			var side = Next() < 0.5f ? -1f : 1f;
			var clockU = bay.Middle + Range( -0.25f, 0.25f ) * bay.Width;
			switch ( look )
			{
				case Look.Whiteboard:
				{
					// A whiteboard over a low cupboard of binders.
					var width = MathF.Min( bay.Width - 24f, Range( 84f, 110f ) );
					var u = bay.Middle + Range( -1f, 1f ) * (bay.Width - 24f - width) * 0.5f;
					Whiteboard( u, width, Range( 46f, 56f ), Range( 50f, 55f ) );
					Cupboard( u + Range( -8f, 8f ), MathF.Min( bay.Width - 20f, width + Range( -20f, 0f ) ), Range( 24f, 28f ) );
					clockU = u + side * Range( 0f, width * 0.3f );
					break;
				}
				case Look.Pinboard:
				{
					// A pinboard of notices by a tall filing cabinet (a two-drawer one now and then beside it),
					// a print or a shelf of binders over the cabinet.
					var cabinet = bay.Middle + side * bay.Width * Range( 0.22f, 0.3f );
					Cabinet( cabinet, 3 + Pick( 2 ) );
					if ( Next() < 0.4f ) Cabinet( cabinet - side * Range( 20f, 22f ), 2 );
					if ( Next() < 0.6f ) Print( cabinet + Range( -4f, 4f ), Range( 112f, 128f ), Range( 28f, 38f ), Range( 36f, 48f ) );
					else WallShelf( cabinet, Range( 40f, 54f ), Range( 92f, 104f ) );
					var board = bay.Middle - side * bay.Width * Range( 0.08f, 0.16f );
					Pinboard( board, Range( 56f, 74f ), Range( 40f, 50f ), Range( 58f, 66f ) );
					clockU = board + Range( -10f, 10f );
					break;
				}
				case Look.Desk:
				{
					// A desk against the wall, its chair tucked in, a shelf or a print over it, a cabinet beside it.
					var length = MathF.Min( bay.Width - 44f, Range( 62f, 80f ) );
					var u = bay.Middle + side * Range( 0f, (bay.Width - 44f - length) * 0.5f );
					Desk( u, length );
					if ( Next() < 0.55f ) WallShelf( u + Range( -8f, 8f ), length * Range( 0.6f, 0.85f ), Range( 82f, 92f ) );
					else Print( u + Range( -10f, 10f ), Range( 100f, 116f ), Range( 30f, 40f ), Range( 36f, 46f ) );
					if ( bay.Width - length > 64f ) Cabinet( u - side * (length * 0.5f + 13f), 2 + Pick( 3 ) );
					clockU = u + side * length * Range( 0.25f, 0.4f );
					break;
				}
				case Look.Shelving:
				{
					// One or two open shelving units full of files, a floor plant or a two-drawer cabinet beside them.
					var width = Range( 44f, 58f );
					var u = bay.Middle + side * Range( 0f, MathF.Max( 0f, bay.Width * 0.5f - width - 14f ) );
					Shelving( u, width );
					var free = bay.Middle - side * bay.Width * Range( 0.28f, 0.34f );
					var beside = Next();
					if ( bay.Width > 150f && beside < 0.4f ) Shelving( u - side * (width + Range( 0.5f, 2f )), Range( 40f, 52f ) );
					else if ( beside < 0.6f ) Cabinet( u - side * (width * 0.5f + 12f), 2 );
					else if ( beside < 0.8f ) Plant( free );
					else Print( free, Range( 96f, 112f ), Range( 26f, 34f ), Range( 34f, 44f ) );
					clockU = u + Range( -8f, 8f );
					break;
				}
				default:
				{
					// A row of filing cabinets, every one its own height, colour and state, a shelf of binders
					// or a pair of prints over them.
					var count = bay.Width > 150f ? 3 + Pick( 2 ) : 2 + Pick( 2 );
					var u = bay.Middle - (count - 1) * 10f + Range( -8f, 8f );
					var middle = u + (count - 1) * 10f;
					for ( var k = 0; k < count; k++ )
					{
						Cabinet( u, 2 + Pick( 3 ) );
						u += Range( 19.5f, 21f );
					}
					if ( Next() < 0.55f ) WallShelf( middle + Range( -6f, 6f ), MathF.Min( bay.Width - 30f, count * 20f + Range( -4f, 12f ) ), Range( 90f, 102f ) );
					else
					{
						var gap = Range( 18f, 26f );
						Print( middle - gap, Range( 108f, 120f ), Range( 26f, 34f ), Range( 34f, 44f ) );
						Print( middle + gap, Range( 108f, 120f ), Range( 26f, 34f ), Range( 34f, 44f ) );
					}
					clockU = Next() < 0.5f ? middle + Range( -10f, 10f ) : -1f;
					break;
				}
			}
			if ( clockU > 0f && Next() < 0.35f ) Clock( clockU, Range( 134f, 150f ) );
		} );

		/// <summary>
		/// A split air-conditioner on the plaster between the partition head and the bulkhead, somewhere
		/// over <paramref name="bay"/>: a white casing with a rounded front, its louvre a dark slot along the
		/// bottom, a status light, and the pipe run dropping from one end into the partition.
		/// </summary>
		public void AirCon( Bay bay ) => Apart( () =>
		{
			var length = MathF.Min( bay.Width - 30f, Range( 40f, 52f ) );
			var u = bay.Middle + Range( -0.5f, 0.5f ) * (bay.Width - 30f - length);
			const float Depth = 11f;
			var v = Range( 222f, 226f );
			var y = Face - Depth * 0.5f + 0.5f;
			var white = TrimSkin( WhiteSwatch );
			Kit.BevelBox( white, At( new Vector3( u, y, v + 6f ) ), new Vector3( length, Depth, 12f ), 1.6f, Run );
			Kit.Box( TrimSkin( CharcoalSwatch ), At( new Vector3( u, y - Depth * 0.5f + 1.2f, v + 0.6f ) ), new Vector3( length - 6f, 2.6f, 1f ), Turn( Rotation.FromRoll( Range( 10f, 30f ) ) ) );
			Kit.Box( TrimSkin( MarkerSwatch( 3 ) ), At( new Vector3( u + length * 0.4f, y - Depth * 0.5f - 0.05f, v + 3f ) ), new Vector3( 0.8f, 0.2f, 0.5f ), Run );
			var end = u + (Next() < 0.5f ? -1f : 1f) * (length * 0.5f - 3f);
			Kit.BevelBox( white, At( new Vector3( end, Face - 1.8f, (PartitionTop + v) * 0.5f ) ), new Vector3( 4f, 3.6f, v - PartitionTop + 1f ), 0.4f, Run );
		} );

		// ---- Things --------------------------------------------------------------------------

		/// <summary>
		/// A venetian blind in the window <paramref name="width"/> wide at <paramref name="u"/>: a head rail,
		/// slats let down to its own height (often half way, sometimes all the way, now and then barely),
		/// tilted its own way with a little play slat to slat and the odd bent one, the slat stack bunched
		/// under the head rail, ladder tapes, a bottom rail, now and then hanging askew, and the pull cord.
		/// </summary>
		void Blind( float u, float width )
		{
			const float Pitch = 6.6f;
			const float Depth = FloorContext.Recess - 2.6f;
			var inner = width - 8.5f;
			var head = UpperRail - 4f;
			var paint = TrimSkin( Next() < 0.7f ? WhiteSwatch : AluminiumSwatch );
			Kit.Box( paint, At( new Vector3( u, Depth, head - 1.6f ) ), new Vector3( inner + 0.5f, 3.2f, 3.2f ), Run );

			var drop = (head - WindowSill - 6f) * (Next() < 0.2f ? Range( 0.85f, 1f ) : Next() < 0.25f ? Range( 0.12f, 0.25f ) : Range( 0.35f, 0.75f ));
			var count = Math.Max( 1, (int)(drop / Pitch) );
			var tilt = Next() < 0.35f ? Range( 62f, 80f ) : Range( 8f, 35f );
			var askew = Next() < 0.2f ? Range( -2.5f, 2.5f ) : 0f;
			var bottom = head - 3.2f - count * Pitch;

			// The slat stack: raised slats bunched tight under the head rail.
			var raised = Math.Max( 0, (int)((head - WindowSill - 6f - drop) / Pitch) );
			if ( raised > 0 )
				Kit.Box( paint, At( new Vector3( u, Depth, head - 3.2f - raised * 0.35f - 0.4f ) ), new Vector3( inner, 2.6f, raised * 0.7f + 0.8f ), Run );
			var top = head - 3.2f - raised * 0.7f - 0.8f;

			for ( var k = 0; k < count; k++ )
			{
				var v = top - (k + 1) * Pitch + Pitch * 0.5f;
				var fall = askew * (k + 1) / count;
				var angle = tilt + Range( -4f, 4f ) + (Next() < 0.06f ? Range( 25f, 45f ) : 0f);
				Kit.Box( paint, At( new Vector3( u, Depth, v ) ), new Vector3( inner, 2.6f, 0.3f ), Turn( Rotation.FromPitch( fall ) * Rotation.FromRoll( angle ) ) );
			}
			var rail = top - count * Pitch - 1f;
			Kit.Box( paint, At( new Vector3( u, Depth, rail ) ), new Vector3( inner, 2.8f, 1.2f ), Turn( Rotation.FromPitch( askew ) ) );

			foreach ( var s in new[] { -1f, 1f } )
			foreach ( var d in new[] { -1.3f, 1.3f } )
				Kit.Box( paint, At( new Vector3( u + s * (inner * 0.5f - Range( 9f, 13f )), Depth + d, (top + rail) * 0.5f ) ), new Vector3( 0.25f, 0.25f, top - rail ), Run );

			var cord = u + (Next() < 0.5f ? -1f : 1f) * (inner * 0.5f - 3f);
			var end = MathF.Max( WindowSill + Range( 6f, 30f ), rail - Range( 10f, 40f ) );
			Kit.Tube( paint, At( new[] { new Vector3( cord, Depth - 1.6f, head - 2f ), new Vector3( cord, Depth - 1.8f, end ) } ), 0.18f, 3 );
			Kit.Ellipsoid( paint, At( new Vector3( cord, Depth - 1.8f, end - 1.2f ) ), new Vector3( 0.6f, 0.6f, 1.4f ), Run, 3, 5 );
		}

		/// <summary>
		/// A panel radiator under a window, <paramref name="length"/> long: a white enamel panel with its
		/// vertical channels standing proud, a top grille, two brackets, a valve at one end.
		/// </summary>
		void Radiator( float u, float length )
		{
			var height = Range( 22f, 27f );
			var foot = Range( 6f, 9f );
			var y = Face - 4f;
			var white = TrimSkin( FrameSwatch );
			Kit.BevelBox( white, At( new Vector3( u, y, foot + height * 0.5f ) ), new Vector3( length, 4f, height ), 0.6f, Run );
			var pitch = Range( 3.6f, 4.2f );
			var channels = (int)((length - 3f) / pitch);
			for ( var k = 0; k < channels; k++ )
				Kit.BevelBox( white, At( new Vector3( u - (channels - 1) * pitch * 0.5f + k * pitch, y - 2.3f, foot + height * 0.5f ) ), new Vector3( pitch * 0.7f, 1f, height - 2.4f ), 0.3f, Run );
			Kit.Box( TrimSkin( CharcoalSwatch ), At( new Vector3( u, y, foot + height + 0.05f ) ), new Vector3( length - 2f, 2.4f, 0.2f ), Run );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( TrimSkin( WhiteSwatch ), At( new Vector3( u + s * length * 0.35f, y + 2f, foot - 1f ) ), new Vector3( 2f, 3f, 4f ), Run );
			var valve = u + (Next() < 0.5f ? -1f : 1f) * (length * 0.5f + 1.5f);
			Kit.Tube( Metal, At( new[] { new Vector3( valve, y, Ledge ), new Vector3( valve, y, foot + 4f ) } ), 0.8f, 6 );
			Kit.SmoothLathe( TrimSkin( WhiteSwatch ), At( new Vector3( valve, y, foot + 4f ) ), new[] { new Vector2( 1.6f, 0f ), new Vector2( 1.6f, 3.2f ), new Vector2( 0f, 3.4f ) }, 8, Run, true, false );
		}

		/// <summary>
		/// A low cupboard <paramref name="length"/> long and <paramref name="height"/> high at <paramref name="u"/>:
		/// a carcass in one of the cabinet paints, two or three doors with pulls, binders and a box on top.
		/// </summary>
		void Cupboard( float u, float length, float height )
		{
			var depth = Range( 16f, 19f );
			var y = Skirt - depth * 0.5f;
			var paint = TrimSkin( CabinetSwatch( Pick( 3 ) ) );
			var turn = Turn( Rotation.FromYaw( Range( -1f, 1f ) ) );
			Kit.BevelBox( paint, At( new Vector3( u, y, Ledge + height * 0.5f ) ), new Vector3( length, depth, height ), 0.6f, turn );
			var doors = length > 60f ? 3 : 2;
			var door = (length - 2f) / doors;
			for ( var k = 0; k < doors; k++ )
			{
				var x = u - length * 0.5f + 1f + door * (k + 0.5f);
				Kit.Box( paint, At( new Vector3( x, y - depth * 0.5f - 0.3f, Ledge + height * 0.5f + 0.5f ) ), new Vector3( door - 1f, 0.6f, height - 4f ), turn );
				Kit.Box( Metal, At( new Vector3( x + door * 0.36f * (k % 2 == 0 ? 1f : -1f), y - depth * 0.5f - 1f, Ledge + height - 6f ) ), new Vector3( 0.8f, 1f, 5f ), turn );
			}

			// On top: a run of binders, leaning where the run ends, and sometimes a box or a small plant.
			var at = u - length * 0.5f + Range( 3f, 10f );
			var stop = at + Range( length * 0.3f, length * 0.6f );
			var lean = 0f;
			while ( at < stop )
			{
				var w = Range( 2.4f, 3.4f );
				Binder( new Vector3( at + w * 0.5f, y + Range( -1f, 1f ), Ledge + height ), w, lean );
				at += w + 0.15f;
				if ( Next() < 0.12f ) lean = Range( 12f, 25f );
			}
			var rest = (u + length * 0.5f - stop) * 0.5f + stop;
			var item = Next();
			if ( item < 0.35f ) SmallPlant( new Vector3( rest, y, Ledge + height ) );
			else if ( item < 0.6f ) Kit.BevelBox( TrimSkin( WhiteSwatch ), At( new Vector3( rest, y, Ledge + height + 5f ) ), new Vector3( Range( 12f, 16f ), Range( 10f, 13f ), 10f ), 0.5f, Turn( Rotation.FromYaw( Range( -8f, 8f ) ) ) );
			else if ( item < 0.8f ) Papers( new Vector3( rest, y, Ledge + height ), 3 + Pick( 6 ) );
		}

		/// <summary>
		/// An open shelving unit <paramref name="width"/> wide at <paramref name="u"/>, about a man's height:
		/// two ends, a top, a plinth and three or four shelves, each holding its own mix — a run of binders
		/// leaning where it ends, box files, a stack of papers, a small plant.
		/// </summary>
		void Shelving( float u, float width )
		{
			var height = Range( 66f, 78f );
			const float Depth = 14f;
			var y = Skirt - Depth * 0.5f;
			var paint = TrimSkin( Next() < 0.5f ? WhiteSwatch : CabinetSwatch( Pick( 3 ) ) );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( paint, At( new Vector3( u + s * (width * 0.5f - 0.8f), y, Ledge + height * 0.5f ) ), new Vector3( 1.6f, Depth, height ), Run );
			var shelves = 3 + Pick( 2 );
			var pitch = (height - 6f) / shelves;
			for ( var k = 0; k <= shelves; k++ )
			{
				var v = Ledge + 4f + k * pitch;
				Kit.Box( paint, At( new Vector3( u, y, v - 0.8f ) ), new Vector3( width - 3.2f, Depth, 1.6f ), Run );
				if ( k == shelves ) break;

				var at = u - width * 0.5f + 2.2f + Range( 0f, 4f );
				var stop = u + width * 0.5f - 2.2f;
				var lean = 0f;
				var room = pitch - 2f;
				while ( at < stop - 3f )
				{
					var what = Next();
					if ( what < 0.65f )
					{
						var bw = Range( 2.4f, 3.4f );
						Binder( new Vector3( at + bw * 0.5f, y + Range( -0.6f, 0.6f ), v ), bw, lean, MathF.Min( room, 12.5f ) );
						at += bw + 0.15f;
						if ( Next() < 0.1f ) lean = Range( 10f, 22f );
					}
					else if ( what < 0.8f && stop - at > 12f )
					{
						Papers( new Vector3( at + 6f, y, v ), 2 + Pick( 6 ) );
						at += 13f;
						lean = 0f;
					}
					else if ( what < 0.9f && stop - at > 8f && room > 9f )
					{
						SmallPlant( new Vector3( at + 4f, y, v ) );
						at += 9f;
						lean = 0f;
					}
					else at += Range( 3f, 8f );
				}
			}
		}

		/// <summary>A binder standing on its foot at <paramref name="foot"/>, its spine to the room, leaning <paramref name="lean"/> degrees, no taller than <paramref name="tallest"/>.</summary>
		void Binder( Vector3 foot, float width, float lean, float tallest = 12.5f )
		{
			var height = MathF.Min( Range( 11f, 12.5f ), tallest );
			var turn = Turn( Rotation.FromPitch( lean ) );
			Kit.Box( TrimSkin( BinderSwatch( Pick( 6 ) ) ), At( foot + new Vector3( MathF.Sin( lean * MathF.PI / 180f ) * height * 0.5f, 0f, height * 0.5f * MathF.Cos( lean * MathF.PI / 180f ) ) ),
				new Vector3( width, Range( 9.5f, 10.5f ), height ), turn );
		}

		/// <summary>
		/// A steel filing cabinet of <paramref name="drawers"/> drawers at <paramref name="u"/>, against the wall:
		/// a carcass in one of the cabinet paints, proud drawer fronts with a pull and a label holder each, now
		/// and then a drawer left open; binders, papers or a small plant on top.
		/// </summary>
		void Cabinet( float u, int drawers )
		{
			var width = Range( 17.5f, 19f );
			var depth = Range( 22f, 25f );
			var drawer = Range( 12.5f, 13.5f );
			var height = drawers * drawer + 1.6f;
			var y = Skirt - depth * 0.5f;
			var paint = TrimSkin( CabinetSwatch( Pick( 3 ) ) );
			var turn = Turn( Rotation.FromYaw( Range( -1.5f, 1.5f ) ) );
			Kit.BevelBox( paint, At( new Vector3( u, y, Ledge + height * 0.5f ) ), new Vector3( width, depth, height ), 0.5f, turn );

			var open = Next() < 0.25f ? Pick( drawers ) : -1;
			for ( var k = 0; k < drawers; k++ )
			{
				var v = Ledge + 0.8f + drawer * (k + 0.5f);
				var pulled = k == open ? Range( 3f, 6f ) : 0f;
				var front = y - depth * 0.5f - 0.35f - pulled;
				Kit.Box( paint, At( new Vector3( u, front + pulled * 0.5f, v ) ), new Vector3( width - 1.6f, 0.7f + pulled, drawer - 1.2f ), turn );
				Kit.Box( Metal, At( new Vector3( u, front - 0.9f, v + drawer * 0.12f ) ), new Vector3( 6f, 1.2f, 1f ), turn );
				Kit.Box( TrimSkin( PaperSwatch( 0 ) ), At( new Vector3( u, front - 0.45f, v + drawer * 0.3f ) ), new Vector3( 3.4f, 0.3f, 1.8f ), turn );
			}

			var top = Ledge + height;
			var item = Next();
			if ( item < 0.3f ) SmallPlant( new Vector3( u + Range( -3f, 3f ), y, top ) );
			else if ( item < 0.55f ) Papers( new Vector3( u + Range( -2f, 2f ), y, top ), 2 + Pick( 8 ) );
			else if ( item < 0.75f )
			{
				var binders = 2 + Pick( 3 );
				for ( var k = 0; k < binders; k++ )
					Kit.Box( TrimSkin( BinderSwatch( Pick( 6 ) ) ), At( new Vector3( u + Range( -1f, 1f ), y + Range( -1f, 1f ), top + 1.3f + k * 2.6f ) ), new Vector3( 11.5f, 10f, 2.6f ), Turn( Rotation.FromYaw( Range( -12f, 12f ) ) ) );
			}
		}

		/// <summary>
		/// A desk <paramref name="length"/> long against the wall at <paramref name="u"/>: a laminate top on
		/// two panel ends and a modesty panel, a monitor on its stand with a keyboard and mouse before it,
		/// papers, a mug or a small plant; under it, its swivel chair, pushed in and turned a little.
		/// </summary>
		void Desk( float u, float length )
		{
			const float Top = 29f;
			const float Depth = 24f;
			var y = Skirt - Depth * 0.5f;
			var top = TrimSkin( Next() < 0.5f ? WhiteSwatch : CabinetSwatch( Pick( 3 ) ) );
			var frame = TrimSkin( Next() < 0.6f ? CharcoalSwatch : AluminiumSwatch );
			Kit.BevelBox( top, At( new Vector3( u, y, Top - 0.8f ) ), new Vector3( length, Depth, 1.6f ), 0.3f, Run );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( frame, At( new Vector3( u + s * (length * 0.5f - 1.5f), y, (Ledge + Top - 1.6f) * 0.5f ) ), new Vector3( 2f, Depth - 2f, Top - 1.6f - Ledge ), Run );
			Kit.Box( frame, At( new Vector3( u, Skirt - 2f, Top - 12f ) ), new Vector3( length - 5f, 1f, 18f ), Run );

			// The monitor: a stand, a neck, the screen turned a little toward the chair.
			var screen = u + Range( -0.2f, 0.2f ) * length;
			var turn = Turn( Rotation.FromYaw( Range( -10f, 10f ) ) );
			var charcoal = TrimSkin( CharcoalSwatch );
			Kit.BevelBox( charcoal, At( new Vector3( screen, y + 3f, Top + 0.3f ) ), new Vector3( 8f, 6f, 0.6f ), 0.2f, turn );
			Kit.Box( charcoal, At( new Vector3( screen, y + 4f, Top + 5f ) ), new Vector3( 2f, 1f, 9f ), turn );
			var width = Range( 22f, 27f );
			Kit.BevelBox( charcoal, At( new Vector3( screen, y + 2.6f, Top + 10.5f ) ), new Vector3( width, 1.4f, width * 0.58f ), 0.4f, turn );
			Kit.Box( TrimSkin( MarkerSwatch( 0 ) ), At( new Vector3( screen, y + 1.85f, Top + 10.5f ) ), new Vector3( width - 1.6f, 0.1f, width * 0.58f - 1.6f ), turn );
			Kit.BevelBox( TrimSkin( Next() < 0.5f ? WhiteSwatch : CharcoalSwatch ), At( new Vector3( screen + Range( -2f, 2f ), y - 5f, Top + 0.4f ) ), new Vector3( 16f, 5.5f, 0.8f ), 0.2f,
				Turn( Rotation.FromYaw( Range( -6f, 6f ) ) ) );
			Kit.Ellipsoid( charcoal, At( new Vector3( screen + 11f + Range( 0f, 3f ), y - 5f + Range( -1f, 1f ), Top + 0.5f ) ), new Vector3( 1.1f, 1.7f, 0.7f ), Run, 3, 6 );

			var free = u + (screen > u ? -1f : 1f) * length * Range( 0.28f, 0.38f );
			var item = Next();
			if ( item < 0.4f ) Papers( new Vector3( free, y + Range( -3f, 3f ), Top ), 2 + Pick( 6 ) );
			else if ( item < 0.7f ) SmallPlant( new Vector3( free, y + Range( 0f, 6f ), Top ) );
			if ( Next() < 0.6f ) Mug( new Vector3( free + Range( -6f, 6f ), y - Range( 3f, 7f ), Top ) );

			Chair( u + Range( -0.15f, 0.15f ) * length, y - Depth * 0.5f + Range( 3.5f, 6.5f ) );
		}

		/// <summary>
		/// A swivel chair with its seat at <paramref name="seatY"/> (depth), pushed in under a desk: five-star
		/// base on castors, a gas lift, an upholstered seat and back, turned a little off square.
		/// </summary>
		void Chair( float u, float seatY )
		{
			var fabric = TrimSkin( Next() < 0.5f ? CharcoalSwatch : ArtSwatch( 3 ) );
			var frame = TrimSkin( CharcoalSwatch );
			var yaw = Range( -14f, 14f );
			var turn = Turn( Rotation.FromYaw( yaw ) );
			var centre = new Vector3( u, seatY, 0f );
			var back = Rotation.FromYaw( yaw ) * new Vector3( 0f, -8.5f, 0f );
			const float Seat = 19f;
			Kit.BevelBox( fabric, At( centre.WithZ( Seat ) ), new Vector3( 17f, 16f, 3f ), 1.2f, turn );
			Kit.BevelBox( fabric, At( centre + back + new Vector3( 0f, 0f, Seat + 12f ) ), new Vector3( 16f, 2.8f, 15f ), 1.2f, Turn( Rotation.FromYaw( yaw ) * Rotation.FromRoll( -8f ) ) );
			Kit.Box( frame, At( centre + back * 0.95f + new Vector3( 0f, 0f, Seat + 3f ) ), new Vector3( 2.4f, 1.2f, 7f ), turn );
			Kit.Tube( TrimSkin( AluminiumSwatch ), At( new[] { centre.WithZ( Ledge + 3f ), centre.WithZ( Seat - 1.5f ) } ), 1f, 6 );
			var spin = Next() * MathF.Tau;
			for ( var k = 0; k < 5; k++ )
			{
				var a = spin + k * MathF.Tau / 5f;
				var dir = new Vector3( MathF.Cos( a ), MathF.Sin( a ), 0f );
				var tip = centre + dir * 10f;
				// A leg reaching toward the room is kept behind the edge of the play space.
				if ( tip.y < Front + 2f ) tip = tip.WithY( Front + 2f );
				Kit.Tube( frame, At( new[] { centre.WithZ( Ledge + 3.2f ), tip.WithZ( Ledge + 2.4f ) } ), 0.8f, 4 );
				Kit.Ellipsoid( frame, At( tip.WithZ( Ledge + 1.2f ) ), new Vector3( 1.2f, 1.2f, 1.2f ), Run, 3, 6 );
			}
		}

		/// <summary>A stack of <paramref name="count"/> sheets' worth of paper, each a little out of line.</summary>
		void Papers( Vector3 foot, int count )
		{
			for ( var k = 0; k < count; k++ )
				Kit.Box( TrimSkin( PaperSwatch( Next() < 0.8f ? 0 : 1 + Pick( 3 ) ) ), At( foot + new Vector3( Range( -0.6f, 0.6f ), Range( -0.6f, 0.6f ), 0.35f + k * 0.7f ) ),
					new Vector3( 11.5f, 8.5f, 0.7f ), Turn( Rotation.FromYaw( Range( -10f, 10f ) ) ) );
		}

		/// <summary>
		/// A framed print <paramref name="width"/> by <paramref name="height"/> centred at <paramref name="u"/>,
		/// <paramref name="v"/>: a white mat in a charcoal or painted frame, and on it a pastel abstract — a few
		/// overlapping blocks, a sun over bands of colour, or a row of bars — every one its own.
		/// </summary>
		void Print( float u, float v, float width, float height )
		{
			var face = Face - 1.2f;
			Kit.Box( TrimSkin( WhiteSwatch ), At( new Vector3( u, (face + Face + 1f) * 0.5f, v ) ), new Vector3( width, Face + 1f - face, height ), Run );
			var frame = TrimSkin( Next() < 0.55f ? CharcoalSwatch : CabinetSwatch( Pick( 3 ) ) );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.BevelBox( frame, At( new Vector3( u, face - 0.3f, v + s * (height * 0.5f + 0.8f) ) ), new Vector3( width + 3.2f, 2.2f, 1.6f ), 0.3f, Run );
				Kit.BevelBox( frame, At( new Vector3( u + s * (width * 0.5f + 0.8f), face - 0.3f, v ) ), new Vector3( 1.6f, 2.2f, height ), 0.3f, Run );
			}

			var w = width - Range( 7f, 10f );
			var h = height - Range( 7f, 10f );
			var colour = Pick( Art.Length );
			var layer = 0;
			Skin Paint() => TrimSkin( ArtSwatch( colour++ % Art.Length ) );
			void Block( float x, float z, float bw, float bh ) => Kit.Box( Paint(), At( new Vector3( u + x, face - 0.15f - 0.08f * layer++, v + z ) ), new Vector3( bw, 0.2f, bh ), Run );
			switch ( Pick( 3 ) )
			{
				case 0:
					for ( var k = 2 + Pick( 3 ); k > 0; k-- )
					{
						var bw = w * Range( 0.3f, 0.65f );
						var bh = h * Range( 0.25f, 0.6f );
						Block( Range( -0.5f, 0.5f ) * (w - bw), Range( -0.5f, 0.5f ) * (h - bh), bw, bh );
					}
					break;
				case 1:
				{
					var bands = 2 + Pick( 3 );
					var z = -h * 0.5f;
					for ( var k = 0; k < bands; k++ )
					{
						var bh = k == bands - 1 ? h * 0.5f - z : h * Range( 0.12f, 0.25f );
						Block( 0f, z + bh * 0.5f, w, bh );
						z += bh;
					}
					var r = MathF.Min( w, h ) * Range( 0.16f, 0.24f );
					Kit.SmoothLathe( Paint(), At( new Vector3( u + Range( -0.3f, 0.3f ) * w, face - 0.2f - 0.08f * layer, v + h * Range( 0.1f, 0.3f ) ) ),
						new[] { new Vector2( r, 0f ), new Vector2( r, 0.2f ) }, 16, Turn( Rotation.FromToRotation( Vector3.Up, ToRoom ) ), false, true );
					break;
				}
				default:
				{
					var bars = 3 + Pick( 3 );
					var pitch = w / bars;
					for ( var k = 0; k < bars; k++ )
					{
						var bh = h * Range( 0.25f, 0.95f );
						Block( -w * 0.5f + pitch * (k + 0.5f), -h * 0.5f + bh * 0.5f, pitch * Range( 0.55f, 0.8f ), bh );
					}
					break;
				}
			}
		}

		/// <summary>A white wall shelf <paramref name="length"/> long at <paramref name="u"/>, <paramref name="v"/>, on two brackets: binders along it, a box or a small plant.</summary>
		void WallShelf( float u, float length, float v )
		{
			const float Depth = 11f;
			var y = Face - Depth * 0.5f + 0.5f;
			Kit.BevelBox( TrimSkin( WhiteSwatch ), At( new Vector3( u, y, v - 0.9f ) ), new Vector3( length, Depth, 1.8f ), 0.3f, Run );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( Metal, At( new Vector3( u + s * (length * 0.5f - Range( 6f, 9f )), Face - 4f, v - 5f ) ), new Vector3( 1.2f, 8f, 8f ), Run );

			var at = u - length * 0.5f + Range( 2f, 8f );
			var stop = at + length * Range( 0.45f, 0.75f );
			var lean = 0f;
			while ( at < stop )
			{
				var bw = Range( 2.4f, 3.4f );
				Binder( new Vector3( at + bw * 0.5f, y + Range( -0.8f, 0.8f ), v ), bw, lean );
				at += bw + 0.15f;
				if ( Next() < 0.12f ) lean = Range( 12f, 25f );
			}
			var rest = (u + length * 0.5f + stop) * 0.5f;
			if ( Next() < 0.5f ) SmallPlant( new Vector3( rest, y, v ) );
			else Kit.BevelBox( TrimSkin( CabinetSwatch( Pick( 3 ) ) ), At( new Vector3( rest, y, v + 4f ) ), new Vector3( Range( 9f, 13f ), 9f, 8f ), 0.4f, Turn( Rotation.FromYaw( Range( -6f, 6f ) ) ) );
		}

		/// <summary>
		/// A whiteboard <paramref name="width"/> by <paramref name="height"/> at <paramref name="u"/>, its foot at
		/// <paramref name="v"/>: its own crop of the scribbled sheet, an aluminium frame, a tray with markers and a duster.
		/// </summary>
		void Whiteboard( float u, float width, float height, float v )
		{
			var face = Face - 1.6f;
			var rows = MathF.Min( SheetHeight, Range( 64f, SheetHeight ) );
			var columns = MathF.Min( SheetWidth, rows * width / height );
			rows = columns * height / width;
			var x = SheetX + Next() * (SheetWidth - columns);
			var y = Next() * (SheetHeight - rows);
			var top = TrimRows( TrimBand.Infill ).Top;
			var crop = new Rect( x / TrimSize, (top + y) / TrimSize, columns / TrimSize, rows / TrimSize );
			Room.WallPicture( Side, _trim, crop, u - width * 0.5f, u + width * 0.5f, v, v + height, face, false );
			Kit.Box( TrimSkin( WhiteSwatch ), At( new Vector3( u, (face + 0.2f + Face + 1f) * 0.5f, v + height * 0.5f ) ), new Vector3( width, Face + 0.8f - face, height ), Run );

			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.BevelBox( Metal, At( new Vector3( u, face - 0.2f, v + height * 0.5f + s * (height * 0.5f + 0.6f) ) ), new Vector3( width + 2.4f, 2.4f, 1.2f ), 0.3f, Run );
				Kit.BevelBox( Metal, At( new Vector3( u + s * (width * 0.5f + 0.6f), face - 0.2f, v + height * 0.5f ) ), new Vector3( 1.2f, 2.4f, height ), 0.3f, Run );
			}
			Kit.BevelBox( Metal, At( new Vector3( u, face - 2.4f, v - 1.2f ) ), new Vector3( width * Range( 0.4f, 0.7f ), 4.2f, 1.2f ), 0.3f, Run );
			var at = u - width * 0.15f;
			for ( var k = 0; k < 1 + Pick( 4 ); k++ )
			{
				at += Range( 2f, 5f );
				Kit.Tube( TrimSkin( MarkerSwatch( Pick( 4 ) ) ), At( new[] { new Vector3( at, face - 2.6f, v - 0.2f ), new Vector3( at + Range( 5f, 6f ), face - 2.8f + Range( -0.5f, 0.5f ), v - 0.2f ) } ), 0.45f, 5 );
			}
			if ( Next() < 0.7f )
				Kit.BevelBox( TrimSkin( CharcoalSwatch ), At( new Vector3( u + width * Range( 0.05f, 0.2f ), face - 2.6f, v + 0.4f ) ), new Vector3( 5.5f, 2.2f, 1.6f ), 0.4f, Turn( Rotation.FromYaw( Range( -10f, 10f ) ) ) );
		}

		/// <summary>
		/// A cork pinboard <paramref name="width"/> by <paramref name="height"/> at <paramref name="u"/>, its foot
		/// at <paramref name="v"/>, in a timber frame, with notices pinned on it every which way.
		/// </summary>
		void Pinboard( float u, float width, float height, float v )
		{
			var face = Face - 1.4f;
			Kit.Box( TrimSkin( CorkSwatch ), At( new Vector3( u, (face + Face + 1f) * 0.5f, v + height * 0.5f ) ), new Vector3( width, Face + 1f - face, height ), Run );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.BevelBox( Metal, At( new Vector3( u, face - 0.2f, v + height * 0.5f + s * (height * 0.5f + 0.6f) ) ), new Vector3( width + 2.4f, 2f, 1.2f ), 0.3f, Run );
				Kit.BevelBox( Metal, At( new Vector3( u + s * (width * 0.5f + 0.6f), face - 0.2f, v + height * 0.5f ) ), new Vector3( 1.2f, 2f, height ), 0.3f, Run );
			}

			var notes = 3 + Pick( 5 );
			for ( var k = 0; k < notes; k++ )
			{
				var size = new Vector3( Range( 7f, 11f ), 0.2f, Range( 9f, 13f ) );
				if ( Next() < 0.3f ) size = new Vector3( 6f, 0.2f, 6f );
				var at = new Vector3( u + Range( -0.5f, 0.5f ) * (width - size.x), face - 0.15f - k * 0.05f, v + height * 0.5f + Range( -0.5f, 0.5f ) * (height - size.z) );
				Kit.Box( TrimSkin( PaperSwatch( Pick( 4 ) ) ), At( at ), size, Turn( Rotation.FromPitch( Range( -7f, 7f ) ) ) );
				Kit.Ellipsoid( TrimSkin( MarkerSwatch( 1 + Pick( 3 ) ) ), At( at + new Vector3( Range( -1f, 1f ), -0.6f, size.z * 0.5f - 1.2f ) ), new Vector3( 0.7f, 0.6f, 0.7f ), Run, 3, 5 );
			}
		}

		/// <summary>
		/// A wall clock at <paramref name="u"/>, <paramref name="v"/>: a moulded rim in one of the cabinet paints
		/// round the ticked face, its hands at whatever time it is in this room, a carrot second hand.
		/// </summary>
		void Clock( float u, float v )
		{
			var r = Range( 10f, 12.5f );
			var facing = Turn( Rotation.FromToRotation( Vector3.Up, ToRoom ) * Rotation.FromYaw( Next() * 360f ) );
			var back = new Vector3( u, Face + 0.5f, v );
			Kit.SmoothLathe( TrimSkin( Next() < 0.5f ? CharcoalSwatch : CabinetSwatch( Pick( 3 ) ) ), At( back ), new[]
			{
				new Vector2( r, 0f ), new Vector2( r + 0.4f, 0.8f ), new Vector2( r + 0.4f, 2.4f ), new Vector2( r - 0.3f, 3.2f ), new Vector2( r - 1f, 3f ),
			}, 20, facing, false, false, 40f );
			Kit.SmoothLathe( TrimSkin( ClockSwatch ), At( back ), new[] { new Vector2( r - 0.9f, 1.6f ), new Vector2( r - 0.9f, 1.8f ) }, 20, facing, false, true );

			// The hands, on the face at its own time; hour and minute agree with each other.
			var minutes = Next() * 720f;
			void Hand( float turns, float length, float width, Rect ink, float lift )
			{
				var a = turns * MathF.Tau;
				var dir = new Vector3( MathF.Sin( a ), 0f, MathF.Cos( a ) );
				Kit.Box( TrimSkin( ink ), At( new Vector3( u, Face + 0.5f - lift, v ) + dir * (length * 0.35f) ), new Vector3( width, 0.3f, length * 1.1f ),
					Turn( Rotation.LookAt( Vector3.Cross( ToRoom, dir ), dir ) ) );
			}
			Hand( minutes / 720f, r * 0.55f, 0.9f, MarkerSwatch( 0 ), 2.2f );
			Hand( minutes % 60f / 60f, r * 0.82f, 0.6f, MarkerSwatch( 0 ), 2.5f );
			Hand( Next(), r * 0.88f, 0.25f, MarkerSwatch( 2 ), 2.8f );
			Kit.Ellipsoid( TrimSkin( MarkerSwatch( 0 ) ), At( new Vector3( u, Face + 0.5f - 2.9f, v ) ), new Vector3( 0.6f, 0.4f, 0.6f ), Run, 3, 6 );
		}

		/// <summary>
		/// A water cooler at <paramref name="u"/>: a white cabinet with a recessed tap panel, a blue and a carrot
		/// tap over a drip tray, a stack of cups clipped to its side, and the blue bottle upended on top.
		/// </summary>
		void Cooler( float u )
		{
			const float Body = 36f;
			var y = Skirt - 7.5f;
			var white = TrimSkin( WhiteSwatch );
			var turn = Turn( Rotation.FromYaw( Range( -4f, 4f ) ) );
			Kit.BevelBox( white, At( new Vector3( u, y, Ledge + Body * 0.5f ) ), new Vector3( 13f, 14f, Body ), 1f, turn );
			Kit.Box( TrimSkin( CharcoalSwatch ), At( new Vector3( u, y - 7.1f, Ledge + Body - 12f ) ), new Vector3( 9f, 0.4f, 10f ), turn );
			Kit.Box( TrimSkin( CharcoalSwatch ), At( new Vector3( u, y - 8.5f, Ledge + Body - 17.5f ) ), new Vector3( 8f, 3f, 1f ), turn );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.BevelBox( TrimSkin( MarkerSwatch( s < 0f ? 1 : 2 ) ), At( new Vector3( u + s * 2.4f, y - 8f, Ledge + Body - 9f ) ), new Vector3( 1.6f, 2.2f, 2.4f ), 0.3f, turn );
			Kit.Tube( white, At( new[] { new Vector3( u + 7.6f, y - 3f, Ledge + Body - 26f ), new Vector3( u + 7.6f, y - 3f, Ledge + Body - 8f ) } ), 1.4f, 8 );

			Kit.SmoothLathe( TrimSkin( BottleSwatch ), At( new Vector3( u, y, Ledge + Body - 0.5f ) ), new[]
			{
				new Vector2( 1.7f, 0f ), new Vector2( 1.7f, 2.2f ), new Vector2( 4.6f, 4.4f ), new Vector2( 5.6f, 6.5f ), new Vector2( 5.7f, 14f ), new Vector2( 5.2f, 16f ), new Vector2( 0f, 16.6f ),
			}, 14, Run, false, false, 40f, Next() );
		}

		/// <summary>
		/// The wall's big plant at <paramref name="u"/>, in a white or charcoal pot: a snake plant's upright
		/// swords, a rubber plant's glossy leaves up a stem, or a parlour palm's arching fronds — every leaf
		/// kept in front of the wall and behind the edge of the play space.
		/// </summary>
		void Plant( float u )
		{
			var r = Range( 8f, 10f );
			var pot = Range( 15f, 19f );
			var foot = new Vector3( u, Skirt - r - 0.5f, Ledge );
			Kit.SmoothLathe( TrimSkin( Next() < 0.6f ? PotSwatch : CharcoalSwatch ), At( foot ), new[]
			{
				new Vector2( r * 0.8f, 0f ), new Vector2( r, pot ), new Vector2( r * 0.92f, pot + 0.2f ), new Vector2( r * 0.9f, pot - 1.5f ),
			}, 14, Run, true, false, 40f );
			Kit.Dome( TrimSkin( SoilSwatch ), At( foot + new Vector3( 0f, 0f, pot - 1.6f ) ), new Vector3( r * 0.9f, r * 0.9f, 1.2f ), Run, 2, 12 );

			var soil = foot + new Vector3( 0f, 0f, pot - 1f );
			var kind = Pick( 3 );
			if ( kind == 0 )
			{
				// Snake plant: stiff swords, near upright, each a little twisted.
				for ( var k = 12 + Pick( 7 ); k > 0; k-- )
				{
					var root = soil + new Vector3( Range( -0.5f, 0.5f ) * r, Range( -0.5f, 0.5f ) * r, 0f );
					var lean = new Vector3( Range( -0.25f, 0.25f ), Range( -0.12f, 0.2f ), 1f ).Normal;
					Kit.Blade( TrimSkin( LeafSwatch( 0 ) ), At( root ), Turn( Aim( lean, Range( 0f, 180f ) ) ), Range( 40f, 62f ), Range( 4.2f, 5.6f ), Range( 0.05f, 0.3f ), 0.3f, 4, 0.05f, 0.55f );
				}
			}
			else if ( kind == 1 )
			{
				// Rubber plant: a stem or two, broad leaves alternating up them, held out and up.
				for ( var s = 2 + Pick( 2 ); s > 0; s-- )
				{
					var height = Range( 46f, 64f );
					var stem = new[] { soil, soil + new Vector3( Range( -4f, 4f ), Range( -1f, 2f ), height * 0.5f ), soil + new Vector3( Range( -6f, 6f ), Range( 0f, 3f ), height ) };
					Inside( stem, 0.6f );
					Kit.Tube( TrimSkin( SoilSwatch ), At( stem ), 0.6f, 4, 0.35f );
					var yaw = Next() * MathF.Tau;
					for ( var h = 0.25f; h <= 1f; h += Range( 0.07f, 0.11f ) )
					{
						yaw += 2.4f + Range( -0.3f, 0.3f );
						var node = Along( stem, h );
						var heading = Flat( yaw );
						heading = heading.WithY( MathF.Max( heading.y, -0.2f ) ).Normal;
						var length = Range( 14f, 19f );
						var room = node.y - Front - 2f;
						length = MathF.Min( length, room / MathF.Max( 0.2f, -heading.y + 0.35f ) );
						Kit.Blade( TrimSkin( LeafSwatch( 1 ) ), At( node ), Turn( Aim( (heading + Vector3.Up * Range( 0.3f, 0.8f )).Normal, Range( -15f, 15f ) ) ), length, length * Range( 0.48f, 0.6f ), Range( 0.3f, 0.6f ), 0.2f, 3, 0.15f, 0.5f );
					}
				}
			}
			else
			{
				// Parlour palm: fronds arching out of the pot, kept in front of the wall and off the play space.
				for ( var k = 8 + Pick( 5 ); k > 0; k-- )
				{
					var yaw = Next() * MathF.Tau;
					var heading = Flat( yaw );
					if ( heading.y < -0.3f ) heading = heading.WithY( -0.3f ).Normal;
					var rise = Range( 0.7f, 1.2f );
					var bend = rise + Range( 0.4f, 1f );
					var reach = (MathF.Sin( rise ) - MathF.Sin( rise - bend )) / bend;
					var length = Range( 34f, 50f );
					var root = soil + new Vector3( Range( -2f, 2f ), Range( -2f, 2f ), 0f );
					if ( heading.y < 0f ) length = MathF.Min( length, (root.y - Front - 2f) / (-heading.y * reach + 0.18f) );
					else length = MathF.Min( length, (Face - root.y - 1f) / MathF.Max( 0.05f, heading.y * reach + 0.18f ) );
					var direction = heading * MathF.Cos( rise ) + Vector3.Up * MathF.Sin( rise );
					Kit.Pinnate( TrimSkin( LeafSwatch( 1 ) ), At( root ), Turn( Aim( direction, Range( -10f, 10f ) ) ), length, length * 0.3f, bend, Math.Clamp( (int)(length / 2.6f), 8, 14 ), 0.2f, 0.7f, 0.65f, 0.08f, 0.4f );
				}
			}
		}

		/// <summary>A small succulent in a white pot, standing at <paramref name="foot"/>.</summary>
		void SmallPlant( Vector3 foot )
		{
			var r = Range( 2.4f, 3.2f );
			Kit.SmoothLathe( TrimSkin( Next() < 0.6f ? PotSwatch : CharcoalSwatch ), At( foot ), new[] { new Vector2( r * 0.8f, 0f ), new Vector2( r, r * 1.5f ), new Vector2( r * 0.85f, r * 1.5f ) }, 10, Run, true, true );
			var top = foot + new Vector3( 0f, 0f, r * 1.5f );
			for ( var k = 5 + Pick( 4 ); k > 0; k-- )
			{
				var yaw = Next() * MathF.Tau;
				Kit.Blade( TrimSkin( LeafSwatch( 1 ) ), At( top ), Turn( Aim( (Flat( yaw ) * 0.6f + Vector3.Up).Normal, Range( -20f, 20f ) ) ), Range( 3.5f, 6f ), Range( 1.6f, 2.4f ), Range( 0.3f, 0.7f ), 0.3f, 2, 0.2f, 0.5f );
			}
		}

		/// <summary>A mug at <paramref name="foot"/>, in a marker colour or white, its handle turned any way.</summary>
		void Mug( Vector3 foot )
		{
			var skin = TrimSkin( Next() < 0.5f ? WhiteSwatch : MarkerSwatch( 1 + Pick( 3 ) ) );
			var yaw = Next() * MathF.Tau;
			var turn = Turn( Rotation.FromYaw( yaw * 180f / MathF.PI ) );
			Kit.SmoothLathe( skin, At( foot ), new[] { new Vector2( 1.6f, 0f ), new Vector2( 1.7f, 3.8f ), new Vector2( 1.45f, 3.8f ), new Vector2( 1.4f, 0.6f ), new Vector2( 0f, 0.6f ) }, 10, turn, true, false, 50f );
			var side = Flat( yaw );
			Kit.Tube( skin, At( new[] { foot + side * 1.6f + Vector3.Up * 3.1f, foot + side * 2.9f + Vector3.Up * 2.4f, foot + side * 1.6f + Vector3.Up * 0.9f } ), 0.3f, 4 );
		}
	}
}
