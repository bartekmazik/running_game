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
	/// panels in it, six of them holding the room's lamps, and a smoke detector here and there. Under each
	/// window a radiator, a credenza, a desk or a row of low cabinets, often a tall plant beside it. Every
	/// plain bay is dressed low, at head height and up on the fabric over it: one bay a wall holds its
	/// meeting corner (a screen on the wall, the projector screen pulled part-way down over the
	/// clerestory), two walls a water cooler, and the rest are dealt from nine sets — a big floor plant, a
	/// whiteboard, a pinboard by a tall filing cabinet, a row of filing cabinets under a year planner, a desk
	/// with its chair, bookcases, tall stationery cupboards, lockers, a photocopier — never the same set next
	/// door, and whatever a set leaves bare filled.
	/// Over the partitions, cable trays, an exit sign, air-conditioners and speakers, never two bare bays
	/// running; one clock a wall, on a column. Each wall draws from its own stream.
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

			var wall = bays.FindAll( b => b.Side == side );
			var windowed = windows.FindAll( w => w.Bay.Side == side );
			foreach ( var (bay, width) in windowed )
				props.WindowBay( bay, width );

			// The plain bays in wall order, dealt from the nine sets so that no two neighbours match; then
			// bays picked from the wall's stream are given over to its meeting corner and, on two walls, the
			// water cooler (one of a kind each, so they match nothing either).
			var plain = wall.FindAll( b => b.Width > 60f && !windowed.Exists( w => w.Bay.Index == b.Index ) );
			var looks = props.Deal( new List<Props.Look>
			{
				Props.Look.Plant, Props.Look.Whiteboard, Props.Look.Pinboard, Props.Look.Cabinets, Props.Look.Desk,
				Props.Look.Shelving, Props.Look.Storage, Props.Look.Lockers, Props.Look.Copier,
			}, plain.Count );
			var places = new List<int>();
			for ( var k = 0; k < plain.Count; k++ ) places.Add( k );
			props.Shuffle( places );
			var specials = new List<Props.Look> { Props.Look.Meeting };
			if ( side == cooler || side == otherCooler ) specials.Add( Props.Look.Cooler );
			for ( var k = 0; k < Math.Min( specials.Count, places.Count ); k++ )
				looks[places[k]] = specials[k];
			for ( var k = 0; k < plain.Count; k++ )
				props.Furnish( looks[k], plain[k] );

			props.Overhead( wall, places.Count > 0 ? plain[places[0]].Index : -1 );
		}

		foreach ( var lamp in lamps )
			AddLamp( room, lamp.Side, Props.Cell( lamp.Side, lamp.U ), Props.PanelLight, Props.PanelDepth, LightColor * 3.6f * lamp.Brightness );
	}

	/// <summary>Height of the bulkhead beam under the ceiling; the grid strip hangs under it.</summary>
	const float Bulkhead = 26f;

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
	/// The lit parts of the fittings, on the self-lit base. In the top left quarter, as it always was, the
	/// light panels: a cool-white prismatic diffuser (a fine grid of lenses, a little brighter in the
	/// middle) and a dimmer, greyer one for the odd panel whose tubes are failing — every office has one.
	/// Beside them an exit sign, white on green; under them two wall screens mid-meeting, a bar chart and a
	/// line graph with a pie, pastel on navy.
	/// </summary>
	protected override PaintedSurface PaintGlow()
	{
		const int Size = 128;
		var painter = new Painter( Size, Palette.Hex( 0xEEF3FA ), 0.3f );
		painter.Fill( 32, 0, 32, 64, Palette.Hex( 0xCDD2D8 ), roughness: 0.35f );
		var glow = new float[Size * Size];
		for ( var y = 0; y < 64; y++ )
		for ( var x = 0; x < 64; x++ )
		{
			var lens = (x % 4 == 0 || y % 4 == 0) ? 0.88f : 1f;
			var middle = 1f - MathF.Abs( (x % 32) - 15.5f ) / 15.5f * 0.12f;
			glow[y * Size + x] = x < 32 ? lens * middle : 0.22f * lens;
		}

		// The exit sign: a running figure through a doorway, an arrow, in white on safety green.
		var white = Palette.Hex( 0xF4F8F4 );
		painter.Fill( 64, 0, 64, 32, Palette.Hex( 0x2E9E5E ), roughness: 0.3f );
		painter.Circle( 79f, 8f, 2.6f, white );
		painter.Line( new Vector2( 78.5f, 11f ), new Vector2( 76f, 19f ), 2.4f, white );
		painter.Line( new Vector2( 78f, 13f ), new Vector2( 83f, 15.5f ), 1.6f, white );
		painter.Line( new Vector2( 78f, 13f ), new Vector2( 73.5f, 15f ), 1.6f, white );
		painter.Line( new Vector2( 76f, 19f ), new Vector2( 81f, 22f ), 1.8f, white );
		painter.Line( new Vector2( 81f, 22f ), new Vector2( 80f, 27f ), 1.8f, white );
		painter.Line( new Vector2( 76f, 19f ), new Vector2( 72.5f, 26f ), 1.8f, white );
		painter.Line( new Vector2( 88f, 5f ), new Vector2( 98f, 5f ), 1.6f, white );
		painter.Line( new Vector2( 98f, 5f ), new Vector2( 98f, 27f ), 1.6f, white );
		painter.Line( new Vector2( 88f, 5f ), new Vector2( 88f, 27f ), 1.6f, white );
		painter.Line( new Vector2( 104f, 16f ), new Vector2( 122f, 16f ), 2.4f, white );
		painter.Line( new Vector2( 122f, 16f ), new Vector2( 116f, 10.5f ), 2.4f, white );
		painter.Line( new Vector2( 122f, 16f ), new Vector2( 116f, 21.5f ), 2.4f, white );
		for ( var y = 0; y < 32; y++ )
		for ( var x = 64; x < 128; x++ )
			glow[y * Size + x] = 1f;

		// Two screens, each 64 by 36: a title bar, then a bar chart, or a line graph with a pie beside it.
		var navy = Palette.Hex( 0x1F2A44 );
		var axis = Palette.Hex( 0x8C97B8 );
		var random = new Random( Seed + 750 );
		float Next() => (float)random.NextDouble();
		const int Top = 64;
		for ( var k = 0; k < 2; k++ )
		{
			var x0 = k * 64;
			painter.Fill( x0, Top, 64, 36, navy, roughness: 0.15f );
			painter.Fill( x0, Top, 64, 6, Palette.Hex( 0x2E3D60 ), roughness: 0.15f );
			painter.Fill( x0 + 4, Top + 2, 18 + random.Next( 14 ), 2, Palette.Hex( 0xDDE3F0 ), roughness: 0.15f );
			painter.Line( new Vector2( x0 + 6f, Top + 10f ), new Vector2( x0 + 6f, Top + 32f ), 0.8f, axis );
			painter.Line( new Vector2( x0 + 6f, Top + 32f ), new Vector2( x0 + (k == 0 ? 60f : 40f), Top + 32f ), 0.8f, axis );
			if ( k == 0 )
			{
				for ( var bar = 0; bar < 7; bar++ )
				{
					var h = 6f + Next() * 18f + bar * 0.8f;
					painter.Rect( x0 + 9f + bar * 7f, Top + 32f - h, 4.5f, h, Palette.Hex( Art[bar % 5] ) );
				}
			}
			else
			{
				foreach ( var (ink, rise) in new[] { (Palette.Hex( 0xE9A06A ), 0.6f), (Palette.Hex( 0xBFE3CF ), 0.25f) } )
				{
					var at = new Vector2( x0 + 7f, Top + 28f - Next() * 4f );
					for ( var step = 1; step <= 6; step++ )
					{
						var to = new Vector2( x0 + 7f + step * 5.5f, Top + 29f - step * rise * 3.4f - Next() * 6f );
						painter.Line( at, to, 1.2f, ink );
						at = to;
					}
				}
				var centre = new Vector2( x0 + 52f, Top + 21f );
				painter.Circle( centre.x, centre.y, 8f, Palette.Hex( 0xA9B6D8 ) );
				painter.Polygon( new[] { centre, centre + new Vector2( 0f, -8f ), centre + new Vector2( 6.4f, -4.8f ), centre + new Vector2( 8f, 0f ), centre + new Vector2( 5.7f, 5.7f ) }, Palette.Hex( 0xF2C6C2 ) );
				painter.Polygon( new[] { centre, centre + new Vector2( 5.7f, 5.7f ), centre + new Vector2( 0f, 8f ), centre + new Vector2( -4f, 6.9f ) }, Palette.Hex( 0xF4E1A1 ) );
			}
			for ( var y = Top; y < Top + 36; y++ )
			for ( var x = x0; x < x0 + 64; x++ )
				glow[y * Size + x] = 0.8f;
		}

		painter.AddGlow( glow );
		return painter.Bake( "arena_3_glow", ArenaGenerator.GlowMaterialPath, 1f );
	}

	static Rect PanelGlow => new( 0.02f, 0.02f, 0.21f, 0.46f );
	static Rect DeadGlow => new( 0.27f, 0.02f, 0.21f, 0.46f );
	static Rect ExitGlow => new( 66f / 128f, 2f / 128f, 60f / 128f, 28f / 128f );
	static Rect ScreenGlow( int k ) => new( (k * 64 + 1f) / 128f, 65f / 128f, 62f / 128f, 34f / 128f );

	/// <summary>
	/// The whiteboard sheet: eight motifs, a <see cref="MotifSize"/>-pixel cell each with a clear margin,
	/// so a board shows two or three of them whole, side by side, each board its own pick.
	/// </summary>
	const int SheetX = 256, SheetWidth = 192, SheetHeight = 96, MotifSize = 48, Motifs = 8;
	static Rect MotifSwatch( int k ) => Swatch( SheetX + (k % 4) * MotifSize, (k / 4) * MotifSize, MotifSize, MotifSize );

	/// <summary>The whiteboard's own white, blank: the sheet's ground, which the motifs are laid on without a seam.</summary>
	static Color Board => Palette.Hex( 0xF3F4F2 );
	static Rect BoardSwatch => Swatch( 480, 64, 32, 32 );

	/// <summary>Two clock faces: ticks only, and ticks with the quarters in numerals.</summary>
	static Rect ClockSwatch( int k ) => k == 0 ? Swatch( 448, 0, 64, 64 ) : Swatch( 128, 0, 64, 64 );

	/// <summary>A year planner, a calendar's month and cardboard, in the frame cell's spare half.</summary>
	static Rect PlannerSwatch => Swatch( 192, 64, 64, 48 );
	static Rect MonthSwatch => Swatch( 224, 112, 32, 32 );
	static Rect KraftSwatch => Swatch( 192, 112, 32, 32 );

	/// <summary>Print colours: eight solid pastels and accents in the frame cell's spare corner.</summary>
	static Rect ArtSwatch( int k ) => Swatch( 192 + (k % 4) * 16, (k / 4) * 32, 16, 32 );
	static readonly uint[] Art = { 0xF4E1A1, 0xF2C6C2, 0xBFE3CF, 0xA9B6D8, 0x9FC6E0, 0xE9A06A, 0x9DB89A, 0xC7B8E0 };

	/// <summary>The window frames' white powder coat: a solid part of the frame cell.</summary>
	static Rect FrameSwatch => Swatch( 128, 64, 64, 96 );
	static Rect MarkerSwatch( int k ) => Swatch( 448 + k * 8, 64, 8, 32 );
	static Rect CabinetSwatch( int k ) => Swatch( 256 + k * 32, 96, 32, 32 );
	static Rect CorkSwatch => Swatch( 352, 96, 32, 32 );
	static Rect AluminiumSwatch => Swatch( 416, 96, 32, 32 );
	static Rect BottleSwatch => Swatch( 448, 96, 32, 32 );
	static Rect WhiteSwatch => Swatch( 480, 96, 32, 32 );
	static Rect LeafSwatch( int k ) => Swatch( 256 + k * 64, 128, 64, 32 );

	/// <summary>A golden pothos leaf: fresh green streaked with butter, under its pale midrib.</summary>
	static Rect PothosSwatch => Swatch( 192, 144, 64, 16 );
	static Rect PotSwatch => Swatch( 384, 128, 16, 32 );
	static Rect CharcoalSwatch => Swatch( 400, 128, 16, 32 );
	static Rect SoilSwatch => Swatch( 416, 128, 32, 32 );

	/// <summary>One of the paper swatch's four quarters: white, butter, pink and mint notes, ruled.</summary>
	static Rect PaperSwatch( int k ) => Swatch( 384 + (k % 2) * 16, 96 + (k / 2) * 16, 16, 16 );

	/// <summary>A binder's spine: six colours side by side, each a column of the swatch.</summary>
	static Rect BinderSwatch( int k ) => Swatch( 448 + (k % 6) * 10f + 1f, 128, 10f, 32 );

	/// <summary>Filing cabinet paint: putty, a slate grey that holds against the pale fabric, and a periwinkle (the floor's hue).</summary>
	static readonly uint[] CabinetPaints = { 0xC9C4B8, 0x7F889C, 0xA9B6D8 };

	/// <summary>Marker inks: black, blue, a carrot red and green.</summary>
	static readonly uint[] Inks = { 0x2F2A3B, 0x3F63A8, 0xD0704F, 0x3F8A5A };

	/// <summary>
	/// The props' materials, painted into the infill band's free cells (the first holds the view out, half
	/// the second the frame paint): eight whiteboard motifs — the ghosts of wiped meetings under a flow
	/// chart, a bar chart, a pie, a list, sticky notes, a line graph, a mind map, a table — and the board's
	/// blank white, two clock faces, marker inks, three cabinet paints, cork, ruled notes, aluminium, the
	/// cooler's blue bottle and white plastic, two leaves, a white and a charcoal pot, soil, a row of binder
	/// spines, a year planner, a calendar month and cardboard; and eight pastels for the prints.
	/// </summary>
	static void PaintSwatches( Painter painter, Random random )
	{
		float Next() => (float)random.NextDouble();
		var band = TrimRows( TrimBand.Infill ).Top;

		for ( var k = 0; k < Art.Length; k++ )
			painter.Fill( 192 + (k % 4) * 16, band + (k / 4) * 32, 16, 32, Palette.Hex( Art[k] ), roughness: 0.85f );

		{
			var y0 = band;
			painter.Fill( SheetX, y0, SheetWidth, SheetHeight, Board, roughness: 0.12f );
			painter.Fill( 480, y0 + 64, 32, 32, Board, roughness: 0.12f );

			Color Ink() => Palette.Hex( Inks[Next() < 0.55f ? 0 : 1 + random.Next( 3 )] );
			void Scrawl( Vector2 at, float width, Color ink )
			{
				var points = new List<Vector2>();
				for ( var x = 0f; x < width; x += 1.2f + Next() * 0.8f )
					points.Add( at + new Vector2( x, (Next() - 0.5f) * 2.2f ) );
				for ( var k = 0; k + 1 < points.Count; k++ ) painter.Line( points[k], points[k + 1], 0.9f, ink, 0.85f );
			}
			void Stroke( Vector2 a, Vector2 b, Color ink ) => painter.Line( a, b, 1f, ink, 0.85f );
			void Arrow( Vector2 from, Vector2 to, Color ink )
			{
				Stroke( from, to, ink );
				var back = (from - to).Normal * 3f;
				var across = new Vector2( -back.y, back.x ) * 0.6f;
				Stroke( to, to + back + across, ink );
				Stroke( to, to + back - across, ink );
			}
			void Outline( Vector2 at, float w, float h, Color ink )
			{
				Stroke( at, at + new Vector2( w, 0f ), ink );
				Stroke( at + new Vector2( w, 0f ), at + new Vector2( w, h ), ink );
				Stroke( at + new Vector2( w, h ), at + new Vector2( 0f, h ), ink );
				Stroke( at + new Vector2( 0f, h ), at, ink );
			}
			void Ring( Vector2 centre, float r, Color ink )
			{
				for ( var k = 0; k < 20; k++ )
				{
					var a0 = k * MathF.Tau / 20f; var a1 = (k + 1) * MathF.Tau / 20f;
					Stroke( centre + new Vector2( MathF.Cos( a0 ), MathF.Sin( a0 ) ) * r, centre + new Vector2( MathF.Cos( a1 ), MathF.Sin( a1 ) ) * r, ink );
				}
			}

			// Every motif in its own cell, five pixels clear of the cell's edges, so a board can lay any of
			// them on its blank white whole and seamless.
			for ( var m = 0; m < Motifs; m++ )
			{
				var c = new Vector2( SheetX + (m % 4) * MotifSize, y0 + (m / 4) * MotifSize );
				for ( var s = 0; s < 3; s++ )
					painter.Polygon( Organic( c + new Vector2( 12f + Next() * 24f, 12f + Next() * 24f ), new Vector2( 4f + Next() * 6f, 1.5f + Next() * 2.5f ), 0.7f, random.Next(), Next() * 0.5f ),
						Palette.Hex( 0xC9CBCE ), alpha: 0.18f, roughness: 0.2f );

				var ink = Ink();
				switch ( m )
				{
					case 0:
						// A flow chart: three boxes, arrows between.
						foreach ( var box in new[] { new Vector2( 6f, 6f ), new Vector2( 25f, 19f ), new Vector2( 7f, 32f ) } )
						{
							Outline( c + box, 16f, 9f, ink );
							Scrawl( c + box + new Vector2( 2.5f, 4.5f ), 11f, ink );
						}
						Arrow( c + new Vector2( 22f, 10.5f ), c + new Vector2( 32f, 18.5f ), ink );
						Arrow( c + new Vector2( 30f, 28.5f ), c + new Vector2( 22f, 32f ), ink );
						break;
					case 1:
						// A bar chart on two axes, a title over it.
						Scrawl( c + new Vector2( 10f, 7f ), 22f, ink );
						Stroke( c + new Vector2( 8f, 12f ), c + new Vector2( 8f, 41f ), ink );
						Stroke( c + new Vector2( 8f, 41f ), c + new Vector2( 42f, 41f ), ink );
						for ( var k = 0; k < 5; k++ )
						{
							var h = 7f + Next() * 20f;
							painter.Rect( c.x + 11f + k * 6f, c.y + 41f - h, 4f, h, Palette.Hex( Inks[1 + (k + m) % 3] ), alpha: 0.75f );
						}
						break;
					case 2:
					{
						// A pie and its slices, a legend under it.
						var centre = c + new Vector2( 22f, 19f );
						Ring( centre, 12f, ink );
						for ( var k = 0; k < 3; k++ )
						{
							var a = Next() * MathF.Tau;
							Stroke( centre, centre + new Vector2( MathF.Cos( a ), MathF.Sin( a ) ) * 12f, ink );
						}
						for ( var k = 0; k < 2; k++ )
						{
							painter.Rect( c.x + 7f + k * 18f, c.y + 37f, 3f, 3f, Palette.Hex( Inks[1 + k] ), alpha: 0.8f );
							Scrawl( c + new Vector2( 12f + k * 18f, 38.5f ), 9f, ink );
						}
						break;
					}
					case 3:
						// A list: a heading underlined, bullets under it.
						Scrawl( c + new Vector2( 7f, 8f ), 24f, ink );
						Stroke( c + new Vector2( 7f, 11f ), c + new Vector2( 33f, 11.5f ), Palette.Hex( Inks[2] ) );
						for ( var k = 0; k < 5; k++ )
						{
							var line = c + new Vector2( 8f, 17f + k * 5.5f );
							painter.Circle( line.x, line.y, 0.8f, ink, 0.85f );
							Scrawl( line + new Vector2( 3f, 0f ), 14f + Next() * 18f, ink );
						}
						break;
					case 4:
						// Sticky notes stuck about, a scrawl on each.
						for ( var k = 0; k < 4; k++ )
						{
							var at = c + new Vector2( 7f + (k % 2) * 17f + Next() * 4f, 7f + (k / 2) * 17f + Next() * 4f );
							painter.Rect( at.x, at.y, 12f, 12f, Palette.Hex( new uint[] { 0xF4E1A1, 0xF2C6C2, 0xBFE3CF }[(k + random.Next( 2 )) % 3] ), roughness: 0.8f, angle: (Next() - 0.5f) * 0.35f );
							Scrawl( at + new Vector2( 2f, 4f ), 8f, Palette.Ink );
							Scrawl( at + new Vector2( 2f, 7.5f ), 6f, Palette.Ink );
						}
						break;
					case 5:
					{
						// A line graph, two runs and a ring round the peak.
						Stroke( c + new Vector2( 7f, 8f ), c + new Vector2( 7f, 40f ), ink );
						Stroke( c + new Vector2( 7f, 40f ), c + new Vector2( 42f, 40f ), ink );
						for ( var run = 0; run < 2; run++ )
						{
							var colour = Palette.Hex( Inks[1 + run * 2] );
							var at = c + new Vector2( 9f, 34f - run * 6f );
							var peak = at;
							for ( var k = 1; k <= 6; k++ )
							{
								var to = c + new Vector2( 9f + k * 5.5f, 36f - k * (2.5f + run) - Next() * 8f );
								painter.Line( at, to, 1.1f, colour, 0.85f );
								if ( to.y < peak.y ) peak = to;
								at = to;
							}
							if ( run == 0 ) Ring( peak, 3f, Palette.Hex( Inks[2] ) );
						}
						break;
					}
					case 6:
					{
						// A mind map: a ringed word in the middle, spokes out to the rest.
						var centre = c + new Vector2( 24f, 24f );
						Ring( centre, 6f, ink );
						Scrawl( centre + new Vector2( -4f, 0f ), 8f, ink );
						var turn = Next() * MathF.Tau;
						for ( var k = 0; k < 5; k++ )
						{
							var a = turn + k * MathF.Tau / 5f + (Next() - 0.5f) * 0.4f;
							var dir = new Vector2( MathF.Cos( a ), MathF.Sin( a ) );
							var end = centre + dir * 13f;
							Stroke( centre + dir * 6f, end, ink );
							var word = 7f + Next() * 3f;
							Scrawl( end + new Vector2( dir.x < 0f ? -word - 1f : 1f, dir.y * 2f ), word, ink );
						}
						break;
					}
					default:
					{
						// A table: a ruled grid, ticks and crosses in it, a total ringed.
						for ( var k = 0; k <= 4; k++ ) Stroke( c + new Vector2( 6f, 8f + k * 8f ), c + new Vector2( 42f, 8f + k * 8f ), ink );
						foreach ( var x in new[] { 6f, 20f, 31f, 42f } ) Stroke( c + new Vector2( x, 8f ), c + new Vector2( x, 40f ), ink );
						for ( var k = 0; k < 4; k++ )
						{
							Scrawl( c + new Vector2( 8f, 12f + k * 8f ), 9f, ink );
							var tick = c + new Vector2( 24f + (Next() < 0.5f ? 0f : 11f), 12f + k * 8f );
							Stroke( tick + new Vector2( -1.5f, 0f ), tick + new Vector2( 0f, 1.5f ), Palette.Hex( Inks[3] ) );
							Stroke( tick + new Vector2( 0f, 1.5f ), tick + new Vector2( 2.5f, -2f ), Palette.Hex( Inks[3] ) );
						}
						Ring( c + new Vector2( 36.5f, 36f ), 3.2f, Palette.Hex( Inks[2] ) );
						break;
					}
				}
			}

			// The clock faces, white, mapped inside the cap's circle: sixty ticks with the hours heavier, and
			// the same with 12, 3, 6 and 9 in numerals of a few strokes each.
			for ( var face = 0; face < 2; face++ )
			{
				var x0 = face == 0 ? 448 : 128;
				painter.Fill( x0, y0, 64, 64, Palette.Hex( 0xF7F6F1 ), roughness: 0.2f );
				var hub = new Vector2( x0 + 32f, y0 + 32f );
				for ( var k = 0; k < 60; k++ )
				{
					var a = k * MathF.Tau / 60f;
					var dir = new Vector2( MathF.Cos( a ), MathF.Sin( a ) );
					var hour = k % 5 == 0;
					var quarter = k % 15 == 0;
					if ( face == 1 && quarter ) continue;
					painter.Line( hub + dir * (hour ? (quarter ? 17f : 19.5f) : 22f), hub + dir * 24f, quarter ? 2.2f : hour ? 1.4f : 0.5f, Palette.Ink, 0.9f );
				}
				if ( face == 1 )
				{
					Digits( painter, "12", hub + new Vector2( 0f, -17.5f ) );
					Digits( painter, "3", hub + new Vector2( 18f, 0f ) );
					Digits( painter, "6", hub + new Vector2( 0f, 17.5f ) );
					Digits( painter, "9", hub + new Vector2( -18f, 0f ) );
				}
			}

			for ( var k = 0; k < 4; k++ ) painter.Fill( 448 + k * 8, y0 + 64, 8, 32, Palette.Hex( Inks[k] ), roughness: 0.4f );

			// A year planner: twelve month rows ruled under a blue header, weeks faint across, a project or a
			// holiday blocked in here and there.
			{
				var (px, py) = (192, y0 + 64);
				painter.Fill( px, py, 64, 48, Palette.Hex( 0xF6F5F0 ), roughness: 0.6f );
				painter.Fill( px, py, 64, 5, Palette.Hex( Inks[1] ), roughness: 0.5f );
				for ( var x = px + 11f; x < px + 62f; x += 4.3f ) painter.Rect( x, py + 6f, 0.6f, 40f, Palette.Hex( 0xD9D7D0 ) );
				for ( var row = 0; row < 12; row++ )
				{
					var y = py + 6.5f + row * 3.4f;
					painter.Rect( px + 3f, y + 3f, 59f, 0.5f, Palette.Hex( 0xB9B7B0 ) );
					painter.Rect( px + 4f, y + 1f, 5f, 1.2f, Palette.Hex( 0x8C8A92 ) );
					for ( var block = random.Next( 3 ); block > 0; block-- )
						painter.Rect( px + 12f + Next() * 38f, y + 0.6f, 3f + Next() * 12f, 2.2f, Palette.Hex( block == 2 ? Inks[2] : Art[random.Next( Art.Length )] ), alpha: 0.9f );
				}
			}

			// A calendar's month: a carrot header over seven days by five weeks, one day ringed.
			{
				var (mx, my) = (224, y0 + 112);
				painter.Fill( mx, my, 32, 32, Palette.Hex( 0xF6F5F0 ), roughness: 0.7f );
				painter.Fill( mx + 3, my + 3, 26, 4, Palette.Hex( 0xE9A06A ), roughness: 0.6f );
				for ( var k = 0; k <= 5; k++ ) painter.Rect( mx + 3f, my + 9f + k * 4f, 26f, 0.5f, Palette.Hex( 0xB9B7B0 ) );
				for ( var k = 0; k <= 7; k++ ) painter.Rect( mx + 3f + k * 3.7f, my + 9f, 0.5f, 20f, Palette.Hex( 0xB9B7B0 ) );
				painter.Circle( mx + 3f + (1 + random.Next( 6 )) * 3.7f - 1.85f, my + 9f + (1 + random.Next( 4 )) * 4f - 2f, 1.8f, Palette.Hex( Inks[2] ), 0.6f );
			}

			// A golden pothos leaf, streaked.
			painter.Fill( 192, y0 + 144, 64, 16, Palette.Hex( 0x6E9E58 ), roughness: 0.35f );
			for ( var s = 0; s < 14; s++ )
			{
				var x = 194f + Next() * 60f;
				painter.Line( new Vector2( x, y0 + 147f + Next() * 3f ), new Vector2( x + 3f + Next() * 6f, y0 + 150f + Next() * 7f ), 0.9f, Palette.Hex( 0xD8D58E ), 0.6f );
			}
			painter.Fill( 192, y0 + 151, 64, 1, Palette.Hex( 0xA9C58E ), roughness: 0.35f );

			// Cardboard: kraft with its fibres.
			painter.Fill( 192, y0 + 112, 32, 32, Palette.Hex( 0xC4A57A ), roughness: 0.9f );
			for ( var s = 0; s < 50; s++ )
				painter.Circle( 192f + Next() * 32f, y0 + 112f + Next() * 32f, 0.4f + Next() * 0.6f, Next() < 0.5f ? Palette.Hex( 0xA88B62 ) : Palette.Hex( 0xD4BA92 ), 0.6f );
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
	/// Numerals of a few strokes each (seven-segment, as a station clock's), 3 by 5 pixels, centred on
	/// <paramref name="centre"/>: enough for a clock face's 12, 3, 6 and 9.
	/// </summary>
	static void Digits( Painter painter, string text, Vector2 centre )
	{
		// Segments a–g as from/to corners on a 0..1 by 0..2 glyph: top, upper right, lower right, bottom, lower left, upper left, middle.
		var segments = new (Vector2 From, Vector2 To)[]
		{
			(new( 0f, 0f ), new( 1f, 0f )), (new( 1f, 0f ), new( 1f, 1f )), (new( 1f, 1f ), new( 1f, 2f )), (new( 0f, 2f ), new( 1f, 2f )),
			(new( 0f, 1f ), new( 0f, 2f )), (new( 0f, 0f ), new( 0f, 1f )), (new( 0f, 1f ), new( 1f, 1f )),
		};
		var lit = new Dictionary<char, string> { ['1'] = "bc", ['2'] = "abged", ['3'] = "abgcd", ['6'] = "afgedc", ['9'] = "abfgcd" };
		const float Width = 3f, Height = 2.5f, Gap = 1.6f;
		var left = centre.x - (text.Length * Width + (text.Length - 1) * Gap) * 0.5f;
		for ( var k = 0; k < text.Length; k++ )
		{
			var origin = new Vector2( left + k * (Width + Gap), centre.y - Height );
			foreach ( var segment in lit[text[k]] )
			{
				var (from, to) = segments[segment - 'a'];
				painter.Line( origin + new Vector2( from.x * Width, from.y * Height ), origin + new Vector2( to.x * Width, to.y * Height ), 1.1f, Palette.Ink, 0.9f );
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

		/// <summary>How far the room's ceiling texture runs before it repeats: a tile's underside wears one of its four cells.</summary>
		const float CeilingSpan = 2f * Arena.TileSize;

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

		/// <summary>
		/// What a plain bay is furnished as: the wall's meeting corner, a cooler, or one of the nine sets the
		/// rest are dealt from.
		/// </summary>
		public enum Look { Plant, Meeting, Cooler, Whiteboard, Pinboard, Cabinets, Desk, Shelving, Storage, Lockers, Copier }

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
		/// irregularly, with now and then a panel whose tubes are failing and a tile pushed askew. Under
		/// some of the plain tiles, a smoke detector; under one near a corner, a camera dome.
		/// </summary>
		public void Grid( List<float> lamps ) => Apart( () =>
		{
			var first = First( Side );
			var last = FloorContext.Span - first;
			var count = Cells( Side );
			var length = Size( Side );
			Kit.Box( Metal, At( new Vector3( (first + last) * 0.5f, -Reach - 0.6f, Strip - 0.9f ) ), new Vector3( last - first, 1.2f, 1.8f ), Run );

			var gap = 0;
			var plain = new List<float>();
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
				if ( !askew ) plain.Add( middle );
				var lift = askew ? new Vector3( 0f, 0f, 1.2f ) : Vector3.Zero;
				var tile = new Skin( _ceiling, new Rect( Next() * 0.4f, Next() * 0.4f, length / CeilingSpan, Reach / CeilingSpan ) );
				Kit.Quad( tile, At( a + lift ), At( d + lift * (askew ? 0f : 1f) ), At( cc + lift * (askew ? 0f : 1f) ), At( b + lift ) );
			}
			Kit.Box( Metal, At( new Vector3( first + count * length, -Reach * 0.5f, Strip - 0.75f ) ), new Vector3( 1.2f, Reach, 1.5f ), Run );

			// Smoke detectors every few plain tiles, irregularly; a camera dome in a tile near one end.
			var white = TrimSkin( WhiteSwatch );
			for ( var k = Pick( 4 ); k < plain.Count; k += 5 + Pick( 6 ) )
			{
				var at = new Vector3( plain[k] + Range( -8f, 8f ), -Reach * 0.5f + Range( -2f, 2f ), Strip - 0.4f );
				Kit.SmoothLathe( white, At( at - new Vector3( 0f, 0f, 1.8f ) ), new[] { new Vector2( 3.4f, 0f ), new Vector2( 3.4f, 1.2f ), new Vector2( 2.6f, 1.8f ) }, 12, Run, true, false, 50f );
				Kit.Ellipsoid( TrimSkin( MarkerSwatch( 2 ) ), At( at - new Vector3( -1.6f, 0f, 1.9f ) ), new Vector3( 0.35f, 0.35f, 0.2f ), Run, 2, 4 );
			}
			if ( plain.Count > 0 )
			{
				var dome = plain[Next() < 0.5f ? 0 : plain.Count - 1];
				var at = new Vector3( dome, -Reach * 0.5f, Strip - 0.4f );
				Kit.SmoothLathe( white, At( at - new Vector3( 0f, 0f, 1f ) ), new[] { new Vector2( 4.4f, 0f ), new Vector2( 4.4f, 1f ) }, 12, Run, true, false );
				Kit.Dome( TrimSkin( CharcoalSwatch ), At( at - new Vector3( 0f, 0f, 1f ) ), new Vector3( 3.6f, 3.6f, 3.4f ), Turn( Rotation.FromRoll( 180f ) ), 3, 12 );
			}
		} );

		// ---- Bays ------------------------------------------------------------------------------

		/// <summary>
		/// A window bay: a venetian blind in the window, now and then a pothos hung in front of it; under the
		/// sill a radiator, a credenza with files and a pot plant on it, a desk with its chair, or a row of
		/// two-drawer cabinets; often a tall plant by the window; now and then a plant, a mug or a stack of
		/// papers on the sill; then whatever the bay leaves bare beside the window filled (<see cref="Fill"/>).
		/// </summary>
		public void WindowBay( Bay bay, float width ) => Apart( () =>
		{
			_floor.Clear();
			_wall.Clear();
			_low.Clear();
			Hang( bay.Middle, width + 6f, WindowSill, UpperRail );
			Blind( bay.Middle, width );
			if ( Next() < 0.35f ) HangingPlant( bay.Middle + Range( -0.3f, 0.3f ) * width );

			var under = Next();
			if ( under < 0.22f ) Radiator( bay.Middle + Range( -4f, 4f ), MathF.Min( width - Range( 8f, 20f ), bay.Width - 20f ) );
			else if ( under < 0.5f ) Cupboard( bay.Middle + Range( -6f, 6f ), MathF.Min( width + Range( -10f, 10f ), bay.Width - 16f ), Range( 26f, 30f ), Next() < 0.7f );
			else if ( under < 0.85f ) Desk( bay.Middle + Range( -6f, 6f ), MathF.Min( width - 6f, Range( 62f, 78f ) ) );
			else
			{
				// Two or three two-drawer cabinets in a row, things on them.
				var count = width > 100f ? 2 + Pick( 2 ) : 2;
				var u = bay.Middle - (count - 1) * 10f + Range( -10f, 10f );
				for ( var k = 0; k < count; k++, u += Range( 19.5f, 21f ) )
					Cabinet( u, 2, Next() < 0.3f ? 1 : 0 );
			}
			// Now and then a tall plant by the window, where the floor under the sill leaves room for its pot
			// (its leaves before the edge of the glass, as a plant by a window does); nothing else taller
			// than the sill stands in front of the glass.
			if ( Next() < 0.55f )
			{
				var room = Gaps( _floor, bay.From + 3f, bay.To - 3f ).FindAll( g => g.To - g.From >= 24f );
				if ( room.Count > 0 )
				{
					var (from, to) = room[Pick( room.Count )];
					TallPlant( Next() < 0.5f ? from + 12f : to - 12f );
				}
			}
			Claim( _floor, bay.Middle, width );

			var sill = Next();
			var at = bay.Middle + Range( -0.35f, 0.35f ) * width;
			if ( sill < 0.35f )
			{
				SmallPlant( new Vector3( at, Face - 6f, WindowSill ) );
				if ( Next() < 0.5f ) SmallPlant( new Vector3( at + (at > bay.Middle ? -1f : 1f) * Range( 8f, 12f ), Face - 6f, WindowSill ) );
			}
			else if ( sill < 0.5f ) Mug( new Vector3( at, Face - 6f, WindowSill ) );
			else if ( sill < 0.7f ) Papers( new Vector3( at, Face - 6.5f, WindowSill ), 2 + Pick( 4 ) );
			Fill( bay );
		} );

		/// <summary>
		/// The stretches along a bay taken, for <see cref="Fill"/>: by what stands on the floor, by what hangs
		/// on the upper fabric (v 104–156), and by what hangs low enough that nothing tall may stand under it.
		/// </summary>
		readonly List<(float From, float To)> _floor = new();
		readonly List<(float From, float To)> _wall = new();
		readonly List<(float From, float To)> _low = new();

		static void Claim( List<(float From, float To)> taken, float u, float width ) => taken.Add( (u - width * 0.5f, u + width * 0.5f) );

		/// <summary>Claims the stretch a wall piece from <paramref name="bottom"/> to <paramref name="top"/> covers: on the upper fabric if it reaches it, over the floor if it hangs low.</summary>
		void Hang( float u, float width, float bottom, float top )
		{
			if ( top > 104f && bottom < 156f ) Claim( _wall, u, width );
			if ( bottom < 100f ) Claim( _low, u, width );
		}

		/// <summary>
		/// Fills what a bay's set left bare, so no stretch of it reads empty from the room. Along the skirting,
		/// where nothing hangs low over the floor: a bookcase or a stationery cupboard where there is room
		/// for one, a tall plant or a filing cabinet with boxes on it where there is less — each against one
		/// end of its gap, so a long gap takes two. Under a board or a screen hung low, only what stays under
		/// it: a two-drawer cabinet, a bin or a box. Up on the fabric, a print or a calendar wherever the wall
		/// is bare.
		/// </summary>
		void Fill( Bay bay )
		{
			for ( var pass = 0; pass < 2; pass++ )
			{
				var taken = new List<(float From, float To)>( _floor );
				taken.AddRange( _low );
				foreach ( var (from, to) in Gaps( taken, bay.From + 3f, bay.To - 3f ) )
				{
					var gap = to - from;
					var end = Next() < 0.5f;
					float Place( float width ) => end ? from + width * 0.5f + 1f : to - width * 0.5f - 1f;
					var roll = Next();
					if ( gap >= 52f && roll < 0.55f )
					{
						var width = MathF.Min( gap - 4f, Range( 36f, 46f ) );
						if ( Next() < 0.6f ) Shelving( Place( width ), width );
						else TallCupboard( Place( width ), width );
					}
					else if ( gap >= 28f && roll < 0.85f )
					{
						if ( Next() < 0.55f ) Cabinet( Place( 19f ), 3 + Pick( 2 ), 1 + Pick( 2 ) );
						else TallPlant( Place( 26f ) );
					}
				}
			}

			foreach ( var (from, to) in Gaps( _floor, bay.From + 3f, bay.To - 3f ) )
			{
				var gap = to - from;
				var end = Next() < 0.5f;
				float Place( float width ) => end ? from + width * 0.5f + 1f : to - width * 0.5f - 1f;
				var roll = Next();
				if ( gap >= 22f && roll < 0.5f ) Cabinet( Place( 19f ), 2 );
				else if ( gap >= 15f && roll < 0.75f )
				{
					if ( Next() < 0.5f ) Bin( Place( 12f ) );
					else ArchiveBoxes( new Vector3( Place( 15f ), Skirt - 7f, Ledge ), 1 + Pick( 2 ), Range( 13f, 15f ), Range( 10f, 11f ) );
				}
			}

			foreach ( var (from, to) in Gaps( _wall, bay.From + 4f, bay.To - 4f ) )
			{
				var gap = to - from;
				var roll = Next();
				if ( gap < 36f || roll < 0.15f ) continue;
				var u = (from + to) * 0.5f + Range( -0.25f, 0.25f ) * (gap - 36f);
				if ( gap > 52f && roll < 0.8f ) Print( u, Range( 128f, 138f ), MathF.Min( gap - 14f, Range( 36f, 60f ) ), Range( 30f, 38f ) );
				else if ( roll < 0.6f ) Calendar( u, Range( 124f, 132f ) );
			}
		}

		/// <summary>
		/// Furnishes <paramref name="bay"/> as <paramref name="look"/>: low by the skirting, something at a
		/// player's height or taller, and wall decor up on the upper fabric panel — nothing in a bay stops
		/// short at the lower rail. Things a player would use are at a player's size; decor up high is up to
		/// half as big again, to hold a 268-high wall.
		/// </summary>
		public void Furnish( Look look, Bay bay ) => Apart( () =>
		{
			_floor.Clear();
			_wall.Clear();
			_low.Clear();
			var side = Next() < 0.5f ? -1f : 1f;
			var near = bay.Middle + side * bay.Width * 0.5f;
			switch ( look )
			{
				case Look.Plant:
				{
					// The wall's big plant to one side; a year planner on the other, or a pinboard under a print.
					var u = bay.Middle + side * bay.Width * Range( 0.2f, 0.28f );
					TallPlant( u );
					if ( Next() < 0.6f ) Print( u + Range( -4f, 4f ), Range( 132f, 140f ), Range( 36f, 46f ), Range( 30f, 38f ) );
					var other = bay.Middle - side * bay.Width * Range( 0.12f, 0.2f );
					if ( Next() < 0.5f ) Planner( other, Range( 98f, 104f ), MathF.Min( bay.Width * 0.5f, Range( 64f, 84f ) ) );
					else
					{
						Pinboard( other, Range( 52f, 66f ), Range( 38f, 46f ), Range( 56f, 62f ) );
						Print( other + Range( -6f, 6f ), Range( 132f, 138f ), Range( 40f, 56f ), Range( 32f, 40f ) );
					}
					break;
				}
				case Look.Meeting:
				{
					// The meeting corner: a screen on the wall over a credenza, the projector screen pulled down
					// over the clerestory, a tall plant at one end where there is room.
					var screen = MathF.Min( bay.Width - 40f, Range( 52f, 62f ) );
					var u = bay.Middle + Range( -0.12f, 0.12f ) * bay.Width;
					Screen( u, Range( 116f, 124f ), screen );
					Cupboard( u + Range( -6f, 6f ), MathF.Min( bay.Width - 30f, screen + Range( 6f, 24f ) ), Range( 26f, 30f ) );
					Projector( bay );
					if ( bay.Width - screen > 90f ) TallPlant( near - side * Range( 16f, 22f ) );
					break;
				}
				case Look.Cooler:
				{
					// The water cooler with a notice board hung high over it; a tall cabinet with boxes on it, or a
					// stationery cupboard, beside it under a print.
					var u = bay.Middle + side * bay.Width * Range( 0.15f, 0.25f );
					Cooler( u );
					Pinboard( u + Range( -4f, 4f ), Range( 40f, 50f ), Range( 34f, 40f ), Range( 108f, 114f ) );
					var other = bay.Middle - side * bay.Width * Range( 0.15f, 0.25f );
					if ( Next() < 0.5f ) Cabinet( other, 4, 1 + Pick( 2 ) );
					else TallCupboard( other, Range( 34f, 40f ) );
					Print( other + Range( -4f, 4f ), Range( 132f, 140f ), Range( 36f, 46f ), Range( 30f, 38f ) );
					break;
				}
				case Look.Whiteboard:
				{
					// A whiteboard over a low cupboard of binders; a stationery cupboard beside it where the bay
					// has room, otherwise a high shelf of binders over it.
					var width = MathF.Min( bay.Width - 24f, Range( 84f, 110f ) );
					var room = bay.Width - width - 24f;
					var u = room > 46f ? near - side * (12f + width * 0.5f) : bay.Middle + Range( -0.5f, 0.5f ) * room;
					Whiteboard( u, width, Range( 46f, 56f ), Range( 50f, 55f ) );
					Cupboard( u + Range( -4f, 4f ), width + Range( -24f, -10f ), Range( 24f, 28f ) );
					if ( room > 46f ) TallCupboard( u - side * (width * 0.5f + 4f + 20f), MathF.Min( room - 6f, Range( 36f, 42f ) ) );
					else WallShelf( u + Range( -8f, 8f ), width * Range( 0.6f, 0.8f ), Range( 128f, 136f ) );
					break;
				}
				case Look.Pinboard:
				{
					// A pinboard of notices by a tall filing cabinet with archive boxes on it, a big print over the
					// cabinet and a calendar over the board.
					var cabinet = bay.Middle + side * bay.Width * Range( 0.24f, 0.3f );
					Cabinet( cabinet, 4, 1 + Pick( 3 ) );
					if ( Next() < 0.4f ) Cabinet( cabinet - side * Range( 20f, 22f ), 2 );
					var board = bay.Middle - side * bay.Width * Range( 0.08f, 0.16f );
					Pinboard( board, Range( 56f, 74f ), Range( 40f, 48f ), Range( 56f, 60f ) );
					var calendar = board - side * Range( 0f, 8f );
					Calendar( calendar, Range( 132f, 136f ) );
					Print( cabinet, Range( 130f, 140f ), MathF.Max( 26f, MathF.Min( Range( 40f, 54f ), (MathF.Abs( calendar - cabinet ) - 22f) * 2f ) ), Range( 32f, 40f ) );
					break;
				}
				case Look.Cabinets:
				{
					// A row of filing cabinets, every one its own height, colour and state, archive boxes on some;
					// a year planner over them, or a shelf of binders under a pair of prints.
					var count = bay.Width > 150f ? 3 + Pick( 2 ) : 2 + Pick( 2 );
					var u = bay.Middle - (count - 1) * 10f + Range( -8f, 8f );
					var middle = u + (count - 1) * 10f;
					for ( var k = 0; k < count; k++ )
					{
						Cabinet( u, 2 + Pick( 3 ), Next() < 0.4f ? 1 + Pick( 2 ) : 0 );
						u += Range( 19.5f, 21f );
					}
					if ( Next() < 0.6f ) Planner( middle + Range( -6f, 6f ), Range( 98f, 104f ), MathF.Min( bay.Width - 30f, Range( 64f, 88f ) ) );
					else
					{
						WallShelf( middle + Range( -6f, 6f ), MathF.Min( bay.Width - 30f, count * 20f + Range( -4f, 12f ) ), Range( 92f, 98f ) );
						var gap = Range( 20f, 28f );
						Print( middle - gap, Range( 134f, 140f ), Range( 30f, 38f ), Range( 30f, 36f ) );
						Print( middle + gap, Range( 134f, 140f ), Range( 30f, 38f ), Range( 30f, 36f ) );
					}
					break;
				}
				case Look.Desk:
				{
					// A desk against the wall, its chair tucked in, a high shelf of binders over it; beside it a
					// bookcase or a cupboard where there is room, a tall plant or a cabinet where there is less.
					var length = MathF.Min( bay.Width - 44f, Range( 62f, 80f ) );
					var u = bay.Middle + side * Range( 0f, (bay.Width - 44f - length) * 0.5f );
					Desk( u, length );
					WallShelf( u + Range( -8f, 8f ), length * Range( 0.6f, 0.85f ), Range( 126f, 136f ) );
					var free = bay.Width * 0.5f - (u - bay.Middle) * -side - length * 0.5f;
					var at = u - side * (length * 0.5f + 2f);
					if ( free > 52f && Next() < 0.6f ) Shelving( at - side * 24f, Range( 42f, 48f ) );
					else if ( free > 46f ) TallCupboard( at - side * 21f, Range( 36f, 40f ) );
					else if ( free > 30f ) TallPlant( at - side * 14f );
					else if ( free > 22f ) Cabinet( at - side * 11f, 2 + Pick( 3 ) );
					Bin( u + side * length * Range( 0.3f, 0.45f ) );
					break;
				}
				case Look.Shelving:
				{
					// Bookcases full of files, two side by side in a wide bay; a print over them, a plant or a
					// cabinet beside them.
					var width = Range( 44f, 56f );
					var u = bay.Middle + side * Range( 0f, MathF.Max( 0f, bay.Width * 0.5f - width - 14f ) );
					Shelving( u, width );
					var pair = bay.Width > 130f && Next() < 0.7f;
					var second = Range( 40f, 52f );
					if ( pair ) Shelving( u - side * ((width + second) * 0.5f + Range( 0.5f, 2f )), second );
					Print( u - (pair ? side * (width + second) * 0.25f : 0f) + Range( -6f, 6f ), Range( 128f, 138f ), Range( 50f, 66f ), Range( 34f, 42f ) );
					var reach = pair ? width * 0.5f + second + 2f : width * 0.5f;
					var free = bay.Middle - side * bay.Width * Range( 0.3f, 0.36f );
					if ( MathF.Abs( free - u ) > reach + 14f )
					{
						if ( Next() < 0.6f ) TallPlant( free );
						else Cabinet( free, 2 + Pick( 3 ), Pick( 3 ) );
					}
					break;
				}
				case Look.Storage:
				{
					// Tall stationery cupboards, one or two, boxes or a trailing plant on top; a calendar and a
					// two-drawer cabinet beside them.
					var width = Range( 36f, 42f );
					var u = bay.Middle + side * bay.Width * Range( 0.1f, 0.2f );
					TallCupboard( u, width );
					var pair = bay.Width > 150f && Next() < 0.6f;
					if ( pair ) TallCupboard( u - side * (width + Range( 1f, 3f )), Range( 34f, 42f ) );
					var free = u - side * (width * (pair ? 1.5f : 0.5f) + Range( 16f, 24f ));
					if ( MathF.Abs( free - bay.Middle ) < bay.Width * 0.5f - 14f )
					{
						Cabinet( free, 2, Pick( 2 ) );
						Calendar( free + Range( -4f, 4f ), Range( 112f, 124f ) );
					}
					break;
				}
				case Look.Lockers:
				{
					// A bank of lockers, boxes on top here and there, a poster over it; a tall plant or a bin by it.
					var columns = Math.Clamp( (int)((bay.Width - 50f) / 14f), 2, 5 );
					var width = Range( 12.5f, 14f );
					var u = bay.Middle + side * Range( 0f, MathF.Max( 0f, (bay.Width - 50f - columns * width) * 0.5f ) );
					Lockers( u, columns, width );
					Print( u + Range( -8f, 8f ), Range( 126f, 136f ), MathF.Min( columns * width, Range( 50f, 64f ) ), Range( 34f, 42f ) );
					var free = u - side * (columns * width * 0.5f + Range( 14f, 18f ));
					if ( MathF.Abs( free - bay.Middle ) < bay.Width * 0.5f - 12f )
					{
						if ( Next() < 0.6f ) TallPlant( free );
						else Bin( free );
					}
					break;
				}
				default:
				{
					// A photocopier with boxes of paper beside it and a notice over it; a stationery cupboard at
					// the bay's other end.
					var u = bay.Middle + side * bay.Width * Range( 0.1f, 0.18f );
					Copier( u, -side );
					Pinboard( u + Range( -4f, 4f ), Range( 40f, 52f ), Range( 32f, 40f ), Range( 66f, 72f ) );
					var boxes = u + side * Range( 26f, 30f );
					if ( MathF.Abs( boxes - bay.Middle ) < bay.Width * 0.5f - 12f )
						ArchiveBoxes( new Vector3( boxes, Skirt - 8f, Ledge ), 1 + Pick( 3 ), 17f, 11f );
					var cupboard = bay.Middle - side * bay.Width * Range( 0.24f, 0.3f );
					if ( MathF.Abs( cupboard - u ) > 50f )
					{
						TallCupboard( cupboard, Range( 34f, 40f ) );
						Print( cupboard + Range( -4f, 4f ), Range( 128f, 136f ), Range( 36f, 46f ), Range( 30f, 38f ) );
					}
					break;
				}
			}
			Fill( bay );
		} );

		/// <summary>
		/// Up over the partitions on one wall, in wall order: a run or two of cable trunking along the plaster
		/// with a drop down the clerestory, an exit sign over one bay, and over the others an air-conditioner
		/// or a speaker — never two bare bays running (<paramref name="meeting"/>'s bay has its projector
		/// screen). On the columns, one clock a wall and a speaker here and there.
		/// </summary>
		public void Overhead( List<Bay> wall, int meeting )
		{
			Apart( () => Trunking( wall ) );
			var exit = Pick( wall.Count );
			if ( wall[exit].Index == meeting ) exit = (exit + 1) % wall.Count;
			var bare = 0;
			for ( var k = 0; k < wall.Count; k++ )
			{
				var bay = wall[k];
				if ( bay.Index == meeting || bay.Width < 60f ) bare = 0;
				else if ( k == exit ) { ExitSign( bay ); bare = 0; }
				else if ( bare > 0 || Next() < 0.65f )
				{
					if ( Next() < 0.6f ) AirCon( bay );
					else Speaker( bay );
					bare = 0;
				}
				else bare++;
			}
			Apart( () => Columns( wall ) );
		}

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
			var v = Range( 223.5f, 226.5f );
			var y = Face - Depth * 0.5f + 0.5f;
			var white = TrimSkin( WhiteSwatch );
			Kit.BevelBox( white, At( new Vector3( u, y, v + 6f ) ), new Vector3( length, Depth, 12f ), 1.6f, Run );
			Kit.Box( TrimSkin( CharcoalSwatch ), At( new Vector3( u, y - Depth * 0.5f + 1.2f, v + 0.6f ) ), new Vector3( length - 6f, 2.6f, 1f ), Turn( Rotation.FromRoll( Range( 10f, 30f ) ) ) );
			Kit.Box( TrimSkin( MarkerSwatch( 3 ) ), At( new Vector3( u + length * 0.4f, y - Depth * 0.5f - 0.05f, v + 3f ) ), new Vector3( 0.8f, 0.2f, 0.5f ), Run );
			var end = u + (Next() < 0.5f ? -1f : 1f) * (length * 0.5f - 3f);
			Kit.BevelBox( white, At( new Vector3( end, Face - 1.8f, (PartitionTop + v) * 0.5f ) ), new Vector3( 4f, 3.6f, v - PartitionTop + 1f ), 0.4f, Run );
		} );

		/// <summary>
		/// Runs of cable tray along the plaster just over the partition head, each two to four bays long
		/// (passing behind the columns), one or two a wall: a wire tray with its front lip, two to four cables
		/// laid in it in their own colours, and from somewhere along each a white trunking drop running down
		/// the clerestory glass to the rail.
		/// </summary>
		void Trunking( List<Bay> wall )
		{
			const float V = PartitionTop + 1.2f;
			const float Tray = 8f;
			var start = Pick( wall.Count );
			for ( var run = 1 + Pick( 2 ); run > 0 && start < wall.Count; run-- )
			{
				var end = Math.Min( wall.Count, start + 2 + Pick( 3 ) );
				var drop = start + Pick( end - start );
				var cables = new List<(Skin Skin, float Y, float Radius)>();
				for ( var c = 2 + Pick( 3 ); c > 0; c-- )
					cables.Add( (TrimSkin( Next() < 0.4f ? CharcoalSwatch : Next() < 0.5f ? MarkerSwatch( 1 ) : CabinetSwatch( 1 ) ), Face - Range( 1.5f, Tray - 1.5f ), Range( 0.45f, 0.8f )) );
				for ( var k = start; k < end; k++ )
				{
					var bay = wall[k];
					Kit.Box( Metal, At( new Vector3( bay.Middle, Face - Tray * 0.5f, V ) ), new Vector3( bay.Width + 2f, Tray, 0.5f ), Run );
					Kit.Box( Metal, At( new Vector3( bay.Middle, Face - Tray - 0.2f, V + 1.1f ) ), new Vector3( bay.Width + 2f, 0.4f, 2.6f ), Run );
					foreach ( var (skin, y, radius) in cables )
						Kit.Tube( skin, At( new[] { new Vector3( bay.From - 1f, y, V + 0.25f + radius ), new Vector3( bay.To + 1f, y, V + 0.25f + radius ) } ), radius, 4 );
					if ( k != drop || bay.Width < 40f ) continue;
					var u = Next() < 0.5f ? bay.From + Range( 6f, 14f ) : bay.To - Range( 6f, 14f );
					Kit.BevelBox( TrimSkin( WhiteSwatch ), At( new Vector3( u, Face - 1.4f, (UpperRail + RailHeight + V) * 0.5f ) ), new Vector3( 3.4f, 2.6f, V - UpperRail - RailHeight ), 0.4f, Run );
				}
				start = end + 1 + Pick( 3 );
			}
		}

		/// <summary>
		/// An exit sign on the plaster over <paramref name="bay"/>: a white box with its lit face, a running
		/// figure and an arrow in white on green — half as big again as life, to read across the room.
		/// </summary>
		void ExitSign( Bay bay ) => Apart( () =>
		{
			var u = bay.Middle + Range( -0.3f, 0.3f ) * MathF.Max( 0f, bay.Width - 40f );
			var v = Range( 228f, 231f );
			const float Width = 21f, Height = 9.5f, Depth = 3f;
			Kit.BevelBox( TrimSkin( WhiteSwatch ), At( new Vector3( u, Face - Depth * 0.5f + 0.5f, v ) ), new Vector3( Width + 2f, Depth, Height + 2f ), 0.5f, Run );
			Room.WallPicture( Side, _glow, ExitGlow, u - Width * 0.5f, u + Width * 0.5f, v - Height * 0.5f, v + Height * 0.5f, Face - Depth + 0.45f, false );
		} );

		/// <summary>A wall speaker on the plaster over <paramref name="bay"/>, tipped down at the room.</summary>
		void Speaker( Bay bay ) => Apart( () =>
		{
			var u = bay.Middle + Range( -0.35f, 0.35f ) * MathF.Max( 0f, bay.Width - 30f );
			Speaker( u, Range( 224f, 228f ), Face );
		} );

		/// <summary>
		/// A speaker box a third over life size hung on the surface at depth <paramref name="wall"/>, its foot at
		/// <paramref name="v"/>: white, a charcoal grille, tipped down at the room on its bracket.
		/// </summary>
		void Speaker( float u, float v, float wall )
		{
			var tip = Turn( Rotation.FromRoll( -Range( 8f, 14f ) ) );
			var centre = new Vector3( u, wall - 4.5f, v + 7f );
			Kit.Box( Metal, At( new Vector3( u, wall - 1f, v + 7f ) ), new Vector3( 2f, 2f, 4f ), Run );
			Kit.BevelBox( TrimSkin( WhiteSwatch ), At( centre ), new Vector3( 9.5f, 6f, 14f ), 1f, tip );
			Kit.Box( TrimSkin( CharcoalSwatch ), At( centre ) + tip * new Vector3( 0f, -3.05f, 0f ), new Vector3( 8f, 0.2f, 12f ), tip );
		}

		/// <summary>
		/// On the columns between the bays (never the pier, which carries the floor's plaque): the wall's one
		/// clock, high, where it reads from anywhere in the room, and a speaker on one column in three.
		/// </summary>
		void Columns( List<Bay> wall )
		{
			var posts = new List<float>();
			for ( var k = 0; k + 1 < wall.Count; k++ )
			{
				var post = (wall[k].To + wall[k + 1].From) * 0.5f;
				if ( MathF.Abs( post - FloorContext.HalfSpan ) > 50f ) posts.Add( post );
			}
			if ( posts.Count == 0 ) return;
			var clock = Pick( posts.Count );
			for ( var k = 0; k < posts.Count; k++ )
			{
				if ( k == clock ) Clock( posts[k], Range( 158f, 168f ), 0f );
				else if ( Next() < 0.33f ) Speaker( posts[k] + Range( -2f, 2f ), Range( 196f, 210f ), 0f );
			}
		}

		/// <summary>
		/// A projector screen over <paramref name="bay"/>: its case on the plaster under the bulkhead, the
		/// screen pulled part-way down in front of the clerestory glass, black-bordered at the top, its weighted
		/// bar at the foot with a pull ring.
		/// </summary>
		void Projector( Bay bay )
		{
			var length = MathF.Min( bay.Width - 30f, Range( 84f, 104f ) );
			var u = bay.Middle + Range( -0.5f, 0.5f ) * (bay.Width - 30f - length);
			var top = Strip - Range( 2f, 3f );
			var foot = Range( 172f, 196f );
			const float Y = Face - 3.2f;
			Kit.BevelBox( TrimSkin( WhiteSwatch ), At( new Vector3( u, Face - 3f, top - 3f ) ), new Vector3( length + 6f, 6f, 6f ), 1.4f, Run );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( TrimSkin( CharcoalSwatch ), At( new Vector3( u + s * (length * 0.5f + 3.1f), Face - 3f, top - 3f ) ), new Vector3( 0.4f, 5f, 5f ), Run );
			Kit.Box( TrimSkin( CharcoalSwatch ), At( new Vector3( u, Y, top - 8f ) ), new Vector3( length - 4f, 0.2f, 4f ), Run );
			Kit.Box( TrimSkin( BoardSwatch ), At( new Vector3( u, Y + 0.05f, (top - 10f + foot) * 0.5f ) ), new Vector3( length - 4f, 0.2f, top - 10f - foot ), Run );
			Kit.BevelBox( TrimSkin( CharcoalSwatch ), At( new Vector3( u, Y, foot - 0.6f ) ), new Vector3( length - 2f, 1.4f, 1.6f ), 0.4f, Run );
			var ring = u + Range( -0.1f, 0.1f ) * length;
			Kit.Tube( TrimSkin( CharcoalSwatch ), At( new[] { new Vector3( ring, Y - 0.4f, foot - 1.4f ), new Vector3( ring, Y - 0.6f, foot - 6f ) } ), 0.2f, 3 );
			Kit.SmoothLathe( TrimSkin( CharcoalSwatch ), At( new Vector3( ring, Y - 0.6f, foot - 8f ) ), new[] { new Vector2( 1.2f, 0f ), new Vector2( 1.2f, 0.4f ) }, 8, Turn( Rotation.FromRoll( 90f ) ), true, true );
		}

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
			Claim( _floor, u, length );
			var height = Range( 22f, 27f );
			var foot = Range( 6f, 9f );
			var y = Face - 4f;
			var white = TrimSkin( FrameSwatch );
			Kit.BevelBox( white, At( new Vector3( u, y, foot + height * 0.5f ) ), new Vector3( length, 4f, height ), 0.6f, Run );
			var pitch = Range( 3.6f, 4.2f );
			var channels = (int)((length - 3f) / pitch);
			for ( var k = 0; k < channels; k++ )
				Kit.Box( white, At( new Vector3( u - (channels - 1) * pitch * 0.5f + k * pitch, y - 2.3f, foot + height * 0.5f ) ), new Vector3( pitch * 0.7f, 1f, height - 2.4f ), Run );
			Kit.Box( TrimSkin( CharcoalSwatch ), At( new Vector3( u, y, foot + height + 0.05f ) ), new Vector3( length - 2f, 2.4f, 0.2f ), Run );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( TrimSkin( WhiteSwatch ), At( new Vector3( u + s * length * 0.35f, y + 2f, foot - 1f ) ), new Vector3( 2f, 3f, 4f ), Run );
			var valve = u + (Next() < 0.5f ? -1f : 1f) * (length * 0.5f + 1.5f);
			Kit.Tube( Metal, At( new[] { new Vector3( valve, y, Ledge ), new Vector3( valve, y, foot + 4f ) } ), 0.8f, 6 );
			Kit.SmoothLathe( TrimSkin( WhiteSwatch ), At( new Vector3( valve, y, foot + 4f ) ), new[] { new Vector2( 1.6f, 0f ), new Vector2( 1.6f, 3.2f ), new Vector2( 0f, 3.4f ) }, 8, Run, true, false );
		}

		/// <summary>
		/// A low cupboard <paramref name="length"/> long and <paramref name="height"/> high at <paramref name="u"/>:
		/// a carcass in one of the cabinet paints, two or three doors with pulls, binders on top and a box, papers
		/// or a small plant — or, with <paramref name="plant"/>, a pot plant big enough to read from the room.
		/// </summary>
		void Cupboard( float u, float length, float height, bool plant = false )
		{
			Claim( _floor, u, length );
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
			if ( plant )
			{
				MediumPlant( new Vector3( rest, y + Range( -1f, 1f ), Ledge + height ) );
				return;
			}
			var item = Next();
			if ( item < 0.35f ) SmallPlant( new Vector3( rest, y, Ledge + height ) );
			else if ( item < 0.6f ) Kit.BevelBox( TrimSkin( WhiteSwatch ), At( new Vector3( rest, y, Ledge + height + 5f ) ), new Vector3( Range( 12f, 16f ), Range( 10f, 13f ), 10f ), 0.5f, Turn( Rotation.FromYaw( Range( -8f, 8f ) ) ) );
			else if ( item < 0.8f ) Papers( new Vector3( rest, y, Ledge + height ), 3 + Pick( 6 ) );
		}

		/// <summary>
		/// An open bookcase <paramref name="width"/> wide at <paramref name="u"/>, a head taller than a man:
		/// two ends, a top, a plinth and three or four shelves, each holding its own mix — a run of binders
		/// leaning where it ends, box files, a stack of papers, a small plant.
		/// </summary>
		void Shelving( float u, float width )
		{
			Claim( _floor, u, width );
			var height = Range( 80f, 90f );
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
		/// and then a drawer left open; on top, <paramref name="boxes"/> archive boxes, or binders, papers or a small plant.
		/// </summary>
		void Cabinet( float u, int drawers, int boxes = 0 )
		{
			Claim( _floor, u, 19f );
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
			if ( boxes > 0 )
			{
				ArchiveBoxes( new Vector3( u + Range( -1f, 1f ), y + Range( -2f, 1f ), top ), boxes, Range( 15f, 17f ), Range( 10f, 11.5f ) );
				return;
			}
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
			Claim( _floor, u, length );
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
			Hang( u, width + 4f, v - height * 0.5f, v + height * 0.5f );
			var face = Face - 1.2f;
			Kit.Box( TrimSkin( WhiteSwatch ), At( new Vector3( u, (face + Face + 1f) * 0.5f, v ) ), new Vector3( width, Face + 1f - face, height ), Run );
			var frame = TrimSkin( Next() < 0.55f ? CharcoalSwatch : CabinetSwatch( Pick( 3 ) ) );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.Box( frame, At( new Vector3( u, face - 0.3f, v + s * (height * 0.5f + 0.8f) ) ), new Vector3( width + 3.2f, 2.2f, 1.6f ), Run );
				Kit.Box( frame, At( new Vector3( u + s * (width * 0.5f + 0.8f), face - 0.3f, v ) ), new Vector3( 1.6f, 2.2f, height ), Run );
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
			Hang( u, length, v - 8f, v + 14f );
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
		/// <paramref name="v"/>: blank white, with two or three of the sheet's motifs on it — each whole, each
		/// board its own pick, place and size, so no two boards carry the same marks — in an aluminium frame,
		/// a tray with markers and a duster.
		/// </summary>
		void Whiteboard( float u, float width, float height, float v )
		{
			Hang( u, width + 3f, v, v + height );
			var face = Face - 1.6f;
			Kit.Box( TrimSkin( BoardSwatch ), At( new Vector3( u, (face + 0.2f + Face + 1f) * 0.5f, v + height * 0.5f ) ), new Vector3( width, Face + 0.8f - face, height ), Run );
			var count = width > 96f && Next() < 0.6f ? 3 : 2;
			var slot = width / count;
			var first = Pick( Motifs );
			var step = 1 + 2 * Pick( Motifs / 2 );
			for ( var k = 0; k < count; k++ )
			{
				var size = MathF.Min( slot - 3f, height * Range( 0.6f, 0.86f ) );
				var mu = u - width * 0.5f + slot * (k + 0.5f) + Range( -0.5f, 0.5f ) * (slot - 3f - size);
				var mv = v + height * 0.5f + Range( -0.5f, 0.5f ) * (height - 3f - size);
				Room.WallPicture( Side, _trim, MotifSwatch( (first + k * step) % Motifs ), mu - size * 0.5f, mu + size * 0.5f, mv - size * 0.5f, mv + size * 0.5f, face, false );
			}

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
			Hang( u, width + 3f, v, v + height );
			var face = Face - 1.4f;
			Kit.Box( TrimSkin( CorkSwatch ), At( new Vector3( u, (face + Face + 1f) * 0.5f, v + height * 0.5f ) ), new Vector3( width, Face + 1f - face, height ), Run );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.Box( Metal, At( new Vector3( u, face - 0.2f, v + height * 0.5f + s * (height * 0.5f + 0.6f) ) ), new Vector3( width + 2.4f, 2f, 1.2f ), Run );
				Kit.Box( Metal, At( new Vector3( u + s * (width * 0.5f + 0.6f), face - 0.2f, v + height * 0.5f ) ), new Vector3( 1.2f, 2f, height ), Run );
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
		/// A wall clock at <paramref name="u"/>, <paramref name="v"/> on the surface at depth <paramref name="wall"/>:
		/// a moulded rim in one of the cabinet paints round a ticked face or one with numerals, its hands at
		/// whatever time it is in this room, a carrot second hand.
		/// </summary>
		void Clock( float u, float v, float wall )
		{
			var r = Range( 9.5f, 11f );
			var facing = Turn( Rotation.FromToRotation( Vector3.Up, ToRoom ) * Rotation.FromYaw( Next() * 360f ) );
			var back = new Vector3( u, wall + 0.5f, v );
			Kit.SmoothLathe( TrimSkin( Next() < 0.5f ? CharcoalSwatch : CabinetSwatch( Pick( 3 ) ) ), At( back ), new[]
			{
				new Vector2( r, 0f ), new Vector2( r + 0.4f, 0.8f ), new Vector2( r + 0.4f, 2.4f ), new Vector2( r - 0.3f, 3.2f ), new Vector2( r - 1f, 3f ),
			}, 20, facing, false, false, 40f );
			Kit.SmoothLathe( TrimSkin( ClockSwatch( Pick( 2 ) ) ), At( back ), new[] { new Vector2( r - 0.9f, 1.6f ), new Vector2( r - 0.9f, 1.8f ) }, 20, facing, false, true );

			// The hands, on the face at its own time; hour and minute agree with each other.
			var minutes = Next() * 720f;
			void Hand( float turns, float length, float width, Rect ink, float lift )
			{
				var a = turns * MathF.Tau;
				var dir = new Vector3( MathF.Sin( a ), 0f, MathF.Cos( a ) );
				Kit.Box( TrimSkin( ink ), At( new Vector3( u, wall + 0.5f - lift, v ) + dir * (length * 0.35f) ), new Vector3( width, 0.3f, length * 1.1f ),
					Turn( Rotation.LookAt( Vector3.Cross( ToRoom, dir ), dir ) ) );
			}
			Hand( minutes / 720f, r * 0.55f, 0.9f, MarkerSwatch( 0 ), 2.2f );
			Hand( minutes % 60f / 60f, r * 0.82f, 0.6f, MarkerSwatch( 0 ), 2.5f );
			Hand( Next(), r * 0.88f, 0.25f, MarkerSwatch( 2 ), 2.8f );
			Kit.Ellipsoid( TrimSkin( MarkerSwatch( 0 ) ), At( new Vector3( u, wall + 0.5f - 2.9f, v ) ), new Vector3( 0.6f, 0.4f, 0.6f ), Run, 3, 6 );
		}

		/// <summary>
		/// A water cooler at <paramref name="u"/>: a white cabinet with a recessed tap panel, a blue and a carrot
		/// tap over a drip tray, a stack of cups clipped to its side, and the blue bottle upended on top.
		/// </summary>
		void Cooler( float u )
		{
			Claim( _floor, u, 15f );
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
		/// A big floor plant at <paramref name="u"/>, 80 to 100 tall in a tall white or charcoal pot: a snake
		/// plant's swords, a rubber plant's glossy leaves up its stems, a fiddle-leaf fig, a dracaena's canes
		/// with their arching tufts, or a yucca's trunks with stiff rosettes — every leaf kept in front of the
		/// wall and behind the edge of the play space (<see cref="Fit"/>).
		/// </summary>
		void TallPlant( float u )
		{
			Claim( _floor, u, 34f );
			var r = Range( 9f, 11f );
			var pot = Range( 18f, 24f );
			var foot = new Vector3( u, Skirt - r - 0.5f, Ledge );
			Kit.SmoothLathe( TrimSkin( Next() < 0.6f ? PotSwatch : CharcoalSwatch ), At( foot ), new[]
			{
				new Vector2( r * 0.78f, 0f ), new Vector2( r, pot ), new Vector2( r * 0.92f, pot + 0.2f ), new Vector2( r * 0.9f, pot - 1.5f ),
			}, 14, Run, true, false, 40f );
			Kit.Dome( TrimSkin( SoilSwatch ), At( foot + new Vector3( 0f, 0f, pot - 1.6f ) ), new Vector3( r * 0.9f, r * 0.9f, 1.2f ), Run, 2, 12 );

			var soil = foot + new Vector3( 0f, 0f, pot - 1f );
			switch ( Pick( 5 ) )
			{
				case 0:
					// Snake plant: stiff swords, near upright, each a little twisted.
					for ( var k = 18 + Pick( 9 ); k > 0; k-- )
					{
						var root = soil + new Vector3( Range( -0.5f, 0.5f ) * r, Range( -0.5f, 0.5f ) * r, 0f );
						var lean = new Vector3( Range( -0.25f, 0.25f ), Range( -0.12f, 0.2f ), 1f ).Normal;
						Kit.Blade( TrimSkin( LeafSwatch( 0 ) ), At( root ), Turn( Aim( lean, Range( 0f, 180f ) ) ), Range( 50f, 72f ), Range( 4.4f, 5.8f ), Range( 0.05f, 0.3f ), 0.3f, 3, 0.05f, 0.55f );
					}
					break;
				case 1:
					// Rubber plant: two or three stems, broad leaves alternating up them, held out and up.
					for ( var s = 2 + Pick( 2 ); s > 0; s-- )
					{
						var height = Range( 58f, 76f );
						var stem = new[] { soil, soil + new Vector3( Range( -4f, 4f ), Range( -1f, 2f ), height * 0.5f ), soil + new Vector3( Range( -7f, 7f ), Range( 0f, 3f ), height ) };
						Inside( stem, 0.7f );
						Kit.Tube( TrimSkin( SoilSwatch ), At( stem ), 0.7f, 4, 0.35f );
						var yaw = Next() * MathF.Tau;
						for ( var h = 0.25f; h <= 1f; h += Range( 0.045f, 0.07f ) )
						{
							yaw += 2.4f + Range( -0.3f, 0.3f );
							var node = Along( stem, h );
							var heading = Flat( yaw );
							var length = Fit( node, heading, Range( 17f, 23f ), 0.35f );
							if ( length < 6f ) continue;
							Kit.Blade( TrimSkin( LeafSwatch( 1 ) ), At( node ), Turn( Aim( (heading + Vector3.Up * Range( 0.3f, 0.8f )).Normal, Range( -15f, 15f ) ) ), length, length * Range( 0.48f, 0.6f ), Range( 0.3f, 0.6f ), 0.2f, 2, 0.15f, 0.5f );
						}
					}
					break;
				case 2:
				{
					// Fiddle-leaf fig: a trunk or two, big leaves broadest near the tip, crowded toward the top.
					var tallest = Range( 64f, 80f );
					for ( var t = 1 + Pick( 2 ); t > 0; t-- )
					{
						var height = tallest * (t == 1 ? 1f : Range( 0.6f, 0.8f ));
						var stem = new[] { soil, soil + new Vector3( Range( -3f, 3f ), Range( -1f, 2f ), height * 0.5f ), soil + new Vector3( Range( -6f, 6f ), Range( 0f, 3f ), height ) };
						Inside( stem, 0.9f );
						Kit.Tube( TrimSkin( SoilSwatch ), At( stem ), 0.9f, 5, 0.45f );
						var yaw = Next() * MathF.Tau;
						for ( var h = 0.3f; h <= 1.01f; h += Range( 0.026f, 0.042f ) * (1.4f - h * 0.6f) )
						{
							yaw += 2.4f + Range( -0.3f, 0.3f );
							var node = Along( stem, MathF.Min( h, 1f ) );
							var heading = Flat( yaw );
							var length = Fit( node, heading, Range( 15f, 21f ), 0.45f );
							if ( length < 6f ) continue;
							Kit.Blade( TrimSkin( LeafSwatch( 1 ) ), At( node ), Turn( Aim( (heading + Vector3.Up * Range( 0.2f, 0.7f )).Normal, Range( -20f, 20f ) ) ), length, length * Range( 0.62f, 0.78f ), Range( 0.2f, 0.5f ), 0.2f, 2, 0.3f, 0.62f );
						}
					}
					break;
				}
				case 3:
				{
					// Dracaena: two or three canes of different heights, a tuft of long arching straps on each.
					var tallest = Range( 60f, 76f );
					for ( var c = 3 + Pick( 2 ); c > 0; c-- )
					{
						var height = tallest * (c == 1 ? 1f : Range( 0.5f, 0.8f ));
						var cane = new[] { soil + new Vector3( Range( -2.5f, 2.5f ), Range( -2f, 2f ), 0f ), soil + new Vector3( Range( -5f, 5f ), Range( -1f, 3f ), height ) };
						Inside( cane, 1.3f );
						Kit.Tube( TrimSkin( SoilSwatch ), At( cane ), 1.3f, 5, 1f );
						for ( var k = 14 + Pick( 7 ); k > 0; k-- )
						{
							var heading = Flat( Next() * MathF.Tau );
							var rise = Range( 0.45f, 1.3f );
							var length = Fit( cane[1], heading, Range( 20f, 30f ), 0.3f );
							if ( length < 6f ) continue;
							Kit.Blade( TrimSkin( LeafSwatch( 0 ) ), At( cane[1] ), Turn( Aim( heading * MathF.Cos( rise ) + Vector3.Up * MathF.Sin( rise ), Range( -15f, 15f ) ) ), length, Range( 2.4f, 3.2f ), Range( 0.6f, 1.2f ), 0.3f, 3, 0.05f, 0.5f );
						}
					}
					break;
				}
				default:
				{
					// Yucca: two or three thick trunks, a rosette of stiff, pointed leaves on each.
					var tallest = Range( 50f, 62f );
					for ( var c = 2 + Pick( 2 ); c > 0; c-- )
					{
						var height = tallest * (c == 1 ? 1f : Range( 0.55f, 0.85f ));
						var trunk = new[] { soil + new Vector3( Range( -2.5f, 2.5f ), Range( -2f, 2f ), 0f ), soil + new Vector3( Range( -6f, 6f ), Range( -1f, 3f ), height ) };
						Inside( trunk, 2.2f );
						Kit.Tube( TrimSkin( SoilSwatch ), At( trunk ), 2.2f, 6, 1.7f );
						for ( var k = 20 + Pick( 9 ); k > 0; k-- )
						{
							var heading = Flat( Next() * MathF.Tau );
							var rise = Range( -0.1f, 1.4f );
							var length = Fit( trunk[1], heading, Range( 16f, 24f ), 0.2f );
							if ( length < 6f ) continue;
							Kit.Blade( TrimSkin( LeafSwatch( 1 ) ), At( trunk[1] ), Turn( Aim( heading * MathF.Cos( rise ) + Vector3.Up * MathF.Sin( rise ), Range( -10f, 10f ) ) ), length, Range( 1.8f, 2.4f ), Range( 0.1f, 0.35f ), 0.35f, 2, 0f, 0.35f );
						}
					}
					break;
				}
			}
		}

		/// <summary>
		/// The longest a leaf from <paramref name="node"/> heading out along <paramref name="heading"/> may grow
		/// and stay between the wall face and the edge of the play space, <paramref name="spread"/> of its
		/// length allowed either way for its width and its droop.
		/// </summary>
		float Fit( Vector3 node, Vector3 heading, float length, float spread )
		{
			var front = node.y - Nearest( node.z ) - 1f;
			var back = Face - node.y - 0.5f;
			length = MathF.Min( length, front / (MathF.Max( 0f, -heading.y ) + spread) );
			return MathF.Min( length, back / (MathF.Max( 0f, heading.y ) + spread) );
		}

		/// <summary>
		/// A pothos on top of something at <paramref name="foot"/> whose front edge is at depth
		/// <paramref name="front"/>: a small pot, a few leaves on it, and three to five vines trailing over the
		/// edge and down its face, heart-shaped leaves along them.
		/// </summary>
		void Pothos( Vector3 foot, float front )
		{
			var r = Range( 3.4f, 4.4f );
			var h = Range( 4.5f, 6f );
			Kit.SmoothLathe( TrimSkin( Next() < 0.6f ? PotSwatch : CharcoalSwatch ), At( foot ), new[] { new Vector2( r * 0.8f, 0f ), new Vector2( r, h ), new Vector2( r * 0.9f, h ) }, 10, Run, true, true );
			var rim = foot + new Vector3( 0f, 0f, h );
			var leaf = TrimSkin( PothosSwatch );
			for ( var k = 6 + Pick( 4 ); k > 0; k-- )
				Kit.Blade( leaf, At( rim ), Turn( Aim( (Flat( Next() * MathF.Tau ) * 0.8f + Vector3.Up * 0.6f).Normal, Range( -20f, 20f ) ) ), Range( 4.5f, 6f ), Range( 3.4f, 4.4f ), 0.4f, 0.3f, 2, 0.15f, 0.45f );

			for ( var vine = 3 + Pick( 3 ); vine > 0; vine-- )
			{
				var along = Range( -1f, 1f );
				var drop = Range( 18f, 44f );
				var path = new Vector3[6];
				path[0] = rim + new Vector3( along * r * 0.7f, -r * 0.5f, -0.5f );
				path[1] = new Vector3( foot.x + along * (r + 2f), front - 1.2f, foot.z + 0.5f );
				for ( var i = 2; i < path.Length; i++ )
				{
					var t = (i - 1) / (path.Length - 2f);
					path[i] = new Vector3( path[1].x + (Range( -2.5f, 2.5f ) + along * 3f) * t, front - 1.4f - Range( 0f, 1.5f ) * t, foot.z - drop * t );
				}
				Inside( path, 0.3f );
				Kit.Tube( TrimSkin( LeafSwatch( 1 ) ), At( path ), 0.3f, 3 );
				for ( var t = Range( 0.22f, 0.3f ); t < 1f; t += Range( 0.07f, 0.1f ) )
				{
					var node = Along( path, t );
					var outward = Flat( Next() < 0.5f ? Range( -0.6f, 0.6f ) : MathF.PI + Range( -0.6f, 0.6f ) ) * 0.6f + new Vector3( 0f, -0.7f, -0.35f );
					var length = Range( 4.2f, 5.6f );
					Kit.Blade( leaf, At( node ), Turn( Aim( outward.Normal, Range( -25f, 25f ) ) ), length, length * Range( 0.7f, 0.85f ), 0.3f, 0.25f, 2, 0.1f, 0.4f );
				}
			}
		}

		/// <summary>
		/// Archive boxes stacked at <paramref name="foot"/>, <paramref name="count"/> high: kraft cardboard,
		/// each a little out of line, its lid proud, a label and a hand hole on the end facing the room.
		/// </summary>
		void ArchiveBoxes( Vector3 foot, int count, float width, float height )
		{
			if ( foot.z <= Ledge ) Claim( _floor, foot.x, width + 2f );
			var kraft = TrimSkin( KraftSwatch );
			var at = foot;
			for ( var k = 0; k < count; k++ )
			{
				var w = width * Range( 0.94f, 1.04f );
				var d = Range( 11f, 12.5f );
				var h = height * Range( 0.95f, 1.05f );
				var turn = Turn( Rotation.FromYaw( Range( -4f, 4f ) ) );
				var floor = At( at + new Vector3( Range( -1.2f, 1.2f ), Range( -0.6f, 0.6f ), 0f ) );
				Kit.Box( kraft, floor + turn * new Vector3( 0f, 0f, (h - 1.5f) * 0.5f ), new Vector3( w, d, h - 1.5f ), turn );
				Kit.Box( kraft, floor + turn * new Vector3( 0f, 0f, h - 1.3f ), new Vector3( w + 0.5f, d + 0.5f, 2.6f ), turn );
				Kit.Box( TrimSkin( PaperSwatch( Pick( 4 ) ) ), floor + turn * new Vector3( Range( -0.15f, 0.15f ) * w, -d * 0.5f - 0.1f, h * 0.38f ), new Vector3( w * 0.42f, 0.2f, h * 0.3f ), turn );
				Kit.Box( TrimSkin( CharcoalSwatch ), floor + turn * new Vector3( 0f, -d * 0.5f - 0.1f, h - 4.6f ), new Vector3( 4f, 0.2f, 1.4f ), turn );
				at += new Vector3( 0f, 0f, h + 0.3f );
			}
		}

		/// <summary>
		/// A tall two-door stationery cupboard <paramref name="width"/> wide at <paramref name="u"/>, a little
		/// taller than a man: a carcass in white or a cabinet paint on a dark plinth, two doors with pulls;
		/// now and then a door stands ajar on shelves of binders and paper. On top, archive boxes, a trailing
		/// pothos or a pile of papers.
		/// </summary>
		void TallCupboard( float u, float width )
		{
			Claim( _floor, u, width );
			var height = Range( 78f, 84f );
			var depth = Range( 17f, 19f );
			var y = Skirt - depth * 0.5f;
			var front = y - depth * 0.5f;
			var paint = TrimSkin( Next() < 0.35f ? WhiteSwatch : CabinetSwatch( Pick( 3 ) ) );
			const float Plinth = 3f;
			const float T = 0.9f;
			var bottom = Ledge + Plinth;
			var body = height - Plinth;
			Kit.Box( TrimSkin( CharcoalSwatch ), At( new Vector3( u, y + 1f, Ledge + Plinth * 0.5f ) ), new Vector3( width - 1.2f, depth - 2f, Plinth ), Run );

			var ajar = Next() < 0.3f ? (Next() < 0.5f ? -1f : 1f) : 0f;
			if ( ajar == 0f ) Kit.BevelBox( paint, At( new Vector3( u, y, bottom + body * 0.5f ) ), new Vector3( width, depth, body ), 0.6f, Run );
			else
			{
				// Open: sides, top, bottom, back and the middle upright, so the shelves show behind the open door.
				foreach ( var s in new[] { -1f, 1f } )
					Kit.Box( paint, At( new Vector3( u + s * (width - T) * 0.5f, y, bottom + body * 0.5f ) ), new Vector3( T, depth, body ), Run );
				Kit.Box( paint, At( new Vector3( u, y, bottom + body - T * 0.5f ) ), new Vector3( width, depth, T ), Run );
				Kit.Box( paint, At( new Vector3( u, y, bottom + T * 0.5f ) ), new Vector3( width, depth, T ), Run );
				Kit.Box( paint, At( new Vector3( u, y + (depth - T) * 0.5f, bottom + body * 0.5f ) ), new Vector3( width, T, body ), Run );
				Kit.Box( paint, At( new Vector3( u, y + 0.5f, bottom + body * 0.5f ) ), new Vector3( T, depth - 1f, body - T * 2f ), Run );

				var near = u + ajar * T * 0.5f;
				var far = u + ajar * (width * 0.5f - T);
				var (lo, hi) = (MathF.Min( near, far ), MathF.Max( near, far ));
				const int Shelves = 4;
				var pitch = (body - T * 2f) / Shelves;
				for ( var k = 0; k < Shelves; k++ )
				{
					var v = bottom + T + k * pitch;
					if ( k > 0 ) Kit.Box( paint, At( new Vector3( (lo + hi) * 0.5f, y + 0.5f, v - 0.4f ) ), new Vector3( hi - lo, depth - 1f, 0.8f ), Run );
					if ( Next() < 0.3f )
					{
						Papers( new Vector3( (lo + hi) * 0.5f, y + 1f, v ), 3 + Pick( 8 ) );
						continue;
					}
					var at = lo + Range( 0.5f, 2f );
					var lean = 0f;
					while ( at < hi - 3.5f )
					{
						var bw = Range( 2.4f, 3.4f );
						Binder( new Vector3( at + bw * 0.5f, y + Range( -0.5f, 0.5f ), v ), bw, lean, MathF.Min( pitch - 1.5f, 12.5f ) );
						at += bw + 0.15f;
						if ( Next() < 0.12f ) lean = Range( 10f, 20f );
					}
				}
			}

			// The doors: flush in front, or swung out on the hinge at the cupboard's side, its pull by the meeting edge.
			var door = (width - 1.2f) * 0.5f;
			foreach ( var s in new[] { -1f, 1f } )
			{
				var hinge = new Vector3( u + s * (width * 0.5f - 0.3f), front - 0.4f, bottom + body * 0.5f );
				var angle = s == ajar ? Range( 25f, 40f ) : 0f;
				var a = angle * MathF.PI / 180f;
				var along = new Vector3( -s * MathF.Cos( a ), -MathF.Sin( a ), 0f );
				var face = new Vector3( MathF.Sin( s * a ), -MathF.Cos( s * a ), 0f );
				var turn = Turn( Rotation.FromYaw( s * angle ) );
				Kit.Box( paint, At( hinge + along * (door * 0.5f) ), new Vector3( door, 0.8f, body - 1.2f ), turn );
				Kit.Box( Metal, At( hinge + along * (door - 2.4f) + face * 0.9f + new Vector3( 0f, 0f, body * 0.06f ) ), new Vector3( 0.8f, 1f, 6f ), turn );
			}

			var top = Ledge + height;
			var item = Next();
			if ( item < 0.45f ) ArchiveBoxes( new Vector3( u + Range( -0.2f, 0.2f ) * width, y, top ), 1 + Pick( 2 ), Range( 14f, 16f ), Range( 10f, 11f ) );
			else if ( item < 0.75f ) Pothos( new Vector3( u + Range( -0.25f, 0.25f ) * width, y + Range( -1f, 1f ), top ), front );
			else if ( item < 0.9f ) Papers( new Vector3( u + Range( -0.2f, 0.2f ) * width, y, top ), 2 + Pick( 6 ) );
		}

		/// <summary>
		/// A bank of <paramref name="columns"/> steel lockers at <paramref name="u"/>, each <paramref name="width"/>
		/// wide, a little taller than a man: full-height doors or two tiers, each door with its vents, a handle
		/// and a number tag; archive boxes on top now and then.
		/// </summary>
		void Lockers( float u, int columns, float width )
		{
			Claim( _floor, u, columns * width );
			var height = Range( 72f, 80f );
			const float Depth = 18f;
			const float Plinth = 3f;
			var y = Skirt - Depth * 0.5f;
			var front = y - Depth * 0.5f;
			var paint = TrimSkin( CabinetSwatch( Pick( 3 ) ) );
			var total = columns * width;
			Kit.Box( TrimSkin( CharcoalSwatch ), At( new Vector3( u, y + 1f, Ledge + Plinth * 0.5f ) ), new Vector3( total - 1f, Depth - 2f, Plinth ), Run );
			Kit.BevelBox( paint, At( new Vector3( u, y, Ledge + Plinth + (height - Plinth) * 0.5f ) ), new Vector3( total, Depth, height - Plinth ), 0.5f, Run );
			var tiers = Next() < 0.5f ? 1 : 2;
			var tall = (height - Plinth - 1f) / tiers;
			for ( var c = 0; c < columns; c++ )
			for ( var t = 0; t < tiers; t++ )
			{
				var x = u - total * 0.5f + width * (c + 0.5f);
				var v0 = Ledge + Plinth + 0.5f + t * tall;
				Kit.Box( paint, At( new Vector3( x, front - 0.3f, v0 + tall * 0.5f ) ), new Vector3( width - 1f, 0.6f, tall - 1f ), Run );
				for ( var k = 0; k < 3; k++ )
					Kit.Box( TrimSkin( CharcoalSwatch ), At( new Vector3( x, front - 0.65f, v0 + tall - 4f - k * 1.6f ) ), new Vector3( width * 0.5f, 0.15f, 0.6f ), Run );
				Kit.Box( Metal, At( new Vector3( x + width * 0.3f, front - 1f, v0 + tall * 0.5f ) ), new Vector3( 1f, 1.2f, 3.5f ), Run );
				Kit.Box( TrimSkin( PaperSwatch( 0 ) ), At( new Vector3( x - width * 0.15f, front - 0.65f, v0 + tall - 8.5f ) ), new Vector3( 2.4f, 0.15f, 1.2f ), Run );
			}
			if ( Next() < 0.45f ) ArchiveBoxes( new Vector3( u + Range( -0.3f, 0.3f ) * total, y, Ledge + height ), 1 + Pick( 2 ), Range( 14f, 16f ), Range( 9.5f, 11f ) );
		}

		/// <summary>
		/// A photocopier at <paramref name="u"/> on castors: a grey pedestal of paper drawers, the white engine
		/// over it, the scanner lid, a control panel tipped toward whoever stands there, an output tray out to
		/// the <paramref name="tray"/> side with copies in it, and now and then a ream of paper on the lid.
		/// </summary>
		void Copier( float u, float tray )
		{
			Claim( _floor, u + tray * 4f, 37f );
			const float Width = 28f, Depth = 22f, Castor = 2f, Pedestal = 20f;
			var y = Skirt - Depth * 0.5f - 0.5f;
			var front = y - Depth * 0.5f;
			var grey = TrimSkin( CabinetSwatch( 1 ) );
			var charcoal = TrimSkin( CharcoalSwatch );
			foreach ( var sx in new[] { -1f, 1f } )
			foreach ( var sy in new[] { -1f, 1f } )
				Kit.Ellipsoid( charcoal, At( new Vector3( u + sx * (Width * 0.5f - 2.5f), y + sy * (Depth * 0.5f - 2.5f), Ledge + 1f ) ), new Vector3( 1f, 1f, 1f ), Run, 2, 5 );
			Kit.BevelBox( grey, At( new Vector3( u, y, Ledge + Castor + Pedestal * 0.5f ) ), new Vector3( Width, Depth, Pedestal ), 0.6f, Run );
			for ( var d = 0; d < 3; d++ )
			{
				var v = Ledge + Castor + Pedestal * (d + 0.5f) / 3f;
				Kit.Box( charcoal, At( new Vector3( u, front - 0.1f, v + Pedestal / 6f - 0.2f ) ), new Vector3( Width - 2f, 0.3f, 0.4f ), Run );
				Kit.Box( charcoal, At( new Vector3( u, front - 0.3f, v + 1.5f ) ), new Vector3( 8f, 0.6f, 1.2f ), Run );
			}

			var body = Ledge + Castor + Pedestal;
			Kit.BevelBox( TrimSkin( WhiteSwatch ), At( new Vector3( u, y, body + 7f ) ), new Vector3( Width, Depth, 14f ), 0.8f, Run );
			Kit.Box( charcoal, At( new Vector3( u, front - 0.1f, body + 4f ) ), new Vector3( Width - 6f, 0.3f, 0.4f ), Run );
			Kit.BevelBox( grey, At( new Vector3( u, y + 1f, body + 15.2f ) ), new Vector3( Width - 2f, Depth - 3f, 2.4f ), 0.4f, Run );
			Kit.BevelBox( charcoal, At( new Vector3( u - tray * 6f, front + 1f, body + 14.2f ) ), new Vector3( 11f, 5f, 1.2f ), 0.3f, Turn( Rotation.FromRoll( -18f ) ) );
			Kit.Box( TrimSkin( MarkerSwatch( 3 ) ), At( new Vector3( u - tray * 2.5f, front - 0.2f, body + 14.6f ) ), new Vector3( 0.8f, 0.4f, 0.4f ), Run );
			Kit.Box( grey, At( new Vector3( u + tray * (Width * 0.5f + 4f), y, body + 9f ) ), new Vector3( 8f, Depth - 6f, 0.6f ), Run );
			Papers( new Vector3( u + tray * (Width * 0.5f + 4f), y, body + 9.3f ), 1 + Pick( 3 ) );
			if ( Next() < 0.6f )
				Kit.Box( TrimSkin( PaperSwatch( 0 ) ), At( new Vector3( u + Range( -4f, 4f ), y + 1f, body + 17.5f ) ), new Vector3( 11f, 8.5f, 2.2f ), Turn( Rotation.FromYaw( Range( -12f, 12f ) ) ) );
		}

		/// <summary>
		/// A year planner <paramref name="width"/> wide on the upper fabric at <paramref name="u"/>, its foot at
		/// <paramref name="foot"/>, as tall as fits under the rail: the planner sheet in a thin aluminium frame,
		/// a few magnets stuck on it.
		/// </summary>
		void Planner( float u, float foot, float width )
		{
			Hang( u, width + 2f, foot, UpperRail );
			var height = MathF.Min( width * 0.66f, UpperRail - 4f - foot );
			var face = Face - 1.2f;
			Kit.Box( TrimSkin( WhiteSwatch ), At( new Vector3( u, (face + 0.2f + Face + 1f) * 0.5f, foot + height * 0.5f ) ), new Vector3( width, Face + 0.8f - face, height ), Run );
			Room.WallPicture( Side, _trim, PlannerSwatch, u - width * 0.5f, u + width * 0.5f, foot, foot + height, face, false );
			foreach ( var s in new[] { -1f, 1f } )
			{
				Kit.Box( Metal, At( new Vector3( u, face - 0.2f, foot + height * 0.5f + s * (height * 0.5f + 0.5f) ) ), new Vector3( width + 2f, 1.6f, 1f ), Run );
				Kit.Box( Metal, At( new Vector3( u + s * (width * 0.5f + 0.5f), face - 0.2f, foot + height * 0.5f ) ), new Vector3( 1f, 1.6f, height ), Run );
			}
			for ( var k = 2 + Pick( 3 ); k > 0; k-- )
				Kit.Ellipsoid( TrimSkin( MarkerSwatch( Pick( 4 ) ) ), At( new Vector3( u + Range( -0.45f, 0.45f ) * width, face - 0.4f, foot + Range( 0.1f, 0.85f ) * height ) ), new Vector3( 0.9f, 0.5f, 0.9f ), Run, 2, 6 );
		}

		/// <summary>
		/// A wall calendar a third over life size centred at <paramref name="u"/>, <paramref name="v"/>, hung
		/// on a nail: a pastel picture of a hill under a sun over the month's grid, the binding along its top.
		/// </summary>
		void Calendar( float u, float v )
		{
			Hang( u, 32f, v - 24f, v + 24f );
			var width = Range( 26f, 30f );
			var height = width * Range( 1.45f, 1.6f );
			var top = v + height * 0.5f;
			var bottom = v - height * 0.5f;
			var picture = height * 0.48f;
			Kit.Box( TrimSkin( WhiteSwatch ), At( new Vector3( u, Face - 0.05f, v ) ), new Vector3( width, 1.1f, height ), Run );
			Room.WallPicture( Side, _trim, MonthSwatch, u - width * 0.45f, u + width * 0.45f, bottom + 1.2f, bottom + height * 0.47f, Face - 0.75f, false );
			var sky = Pick( Art.Length );
			Kit.Box( TrimSkin( ArtSwatch( sky ) ), At( new Vector3( u, Face - 0.8f, top - 2.2f - picture * 0.5f ) ), new Vector3( width * 0.9f, 0.2f, picture ), Run );
			Kit.Box( TrimSkin( ArtSwatch( (sky + 2 + Pick( 5 )) % Art.Length ) ), At( new Vector3( u, Face - 1f, top - 2.2f - picture * 0.78f ) ), new Vector3( width * 0.9f, 0.2f, picture * 0.44f ), Run );
			var r = width * Range( 0.1f, 0.14f );
			Kit.SmoothLathe( TrimSkin( ArtSwatch( (sky + 1) % Art.Length ) ), At( new Vector3( u + Range( -0.25f, 0.25f ) * width, Face - 0.9f, top - 2.2f - picture * Range( 0.25f, 0.35f ) ) ),
				new[] { new Vector2( r, 0f ), new Vector2( r, 0.2f ) }, 8, Turn( Rotation.FromToRotation( Vector3.Up, ToRoom ) ), false, true );
			Kit.Box( TrimSkin( CharcoalSwatch ), At( new Vector3( u, Face - 0.9f, top - 0.8f ) ), new Vector3( width * 0.92f, 0.6f, 1.2f ), Run );
			Kit.Ellipsoid( Metal, At( new Vector3( u, Face - 0.4f, top + 1.2f ) ), new Vector3( 0.5f, 0.6f, 0.5f ), Run, 2, 5 );
		}

		/// <summary>
		/// A wall-mounted screen <paramref name="width"/> wide (16:9, a third over life size) centred at
		/// <paramref name="u"/>, <paramref name="v"/>, on its bracket, showing a chart from the meeting.
		/// </summary>
		void Screen( float u, float v, float width )
		{
			Hang( u, width + 2f, v - 20f, v + 20f );
			var height = width * 9f / 16f;
			Kit.Box( Metal, At( new Vector3( u, Face - 1.5f, v ) ), new Vector3( 10f, 3f, 8f ), Run );
			Kit.BevelBox( TrimSkin( CharcoalSwatch ), At( new Vector3( u, Face - 4f, v ) ), new Vector3( width + 2f, 2f, height + 2f ), 0.5f, Run );
			Room.WallPicture( Side, _glow, ScreenGlow( Pick( 2 ) ), u - width * 0.5f, u + width * 0.5f, v - height * 0.5f, v + height * 0.5f, Face - 5.15f, false );
		}

		/// <summary>A wastepaper bin at <paramref name="u"/> against the skirting, open-topped, charcoal or brushed metal.</summary>
		void Bin( float u )
		{
			Claim( _floor, u, 12f );
			var r = Range( 4.5f, 5.5f );
			var h = Range( 11f, 13f );
			Kit.SmoothLathe( TrimSkin( Next() < 0.5f ? CharcoalSwatch : AluminiumSwatch ), At( new Vector3( u, Skirt - r - 1f, Ledge ) ), new[]
			{
				new Vector2( r * 0.8f, 0f ), new Vector2( r, h ), new Vector2( r - 0.3f, h ), new Vector2( r * 0.8f - 0.3f, 0.6f ), new Vector2( 0f, 0.6f ),
			}, 10, Run, true, false, 50f );
		}

		/// <summary>
		/// A pot plant to stand on a credenza at <paramref name="foot"/>, big enough to read from across the
		/// room: a young snake plant's swords, or a peace lily's arching leaves.
		/// </summary>
		void MediumPlant( Vector3 foot )
		{
			var r = Range( 4.5f, 5.5f );
			var h = Range( 8f, 10f );
			Kit.SmoothLathe( TrimSkin( Next() < 0.6f ? PotSwatch : CharcoalSwatch ), At( foot ), new[] { new Vector2( r * 0.8f, 0f ), new Vector2( r, h ), new Vector2( r * 0.9f, h ) }, 12, Run, true, true );
			var top = foot + new Vector3( 0f, 0f, h );
			if ( Next() < 0.5f )
			{
				for ( var k = 8 + Pick( 5 ); k > 0; k-- )
				{
					var lean = new Vector3( Range( -0.3f, 0.3f ), Range( -0.15f, 0.2f ), 1f ).Normal;
					Kit.Blade( TrimSkin( LeafSwatch( 0 ) ), At( top + new Vector3( Range( -0.5f, 0.5f ) * r, Range( -0.4f, 0.4f ) * r, 0f ) ), Turn( Aim( lean, Range( 0f, 180f ) ) ), Range( 18f, 28f ), Range( 2.6f, 3.4f ), Range( 0.05f, 0.25f ), 0.3f, 3, 0.05f, 0.55f );
				}
				return;
			}
			for ( var k = 10 + Pick( 5 ); k > 0; k-- )
			{
				var heading = Flat( Next() * MathF.Tau );
				var rise = Range( 0.6f, 1.3f );
				var length = Fit( top, heading, Range( 11f, 15f ), 0.35f );
				Kit.Blade( TrimSkin( LeafSwatch( 1 ) ), At( top ), Turn( Aim( heading * MathF.Cos( rise ) + Vector3.Up * MathF.Sin( rise ), Range( -15f, 15f ) ) ), length, length * Range( 0.38f, 0.46f ), Range( 0.5f, 0.9f ), 0.25f, 3, 0.1f, 0.45f );
			}
		}

		/// <summary>A pothos in a pot hung on three cords from the window head at <paramref name="u"/>, in front of the blind, its vines trailing.</summary>
		void HangingPlant( float u )
		{
			var y = Face - 9f;
			var hook = UpperRail - 3f;
			var foot = hook - Range( 14f, 20f );
			foreach ( var a in new[] { 0.3f, 2.4f, 4.5f } )
				Kit.Tube( TrimSkin( SoilSwatch ), At( new[] { new Vector3( u, y, hook ), new Vector3( u + MathF.Cos( a ) * 3.6f, y + MathF.Sin( a ) * 3.6f, foot + 5f ) } ), 0.12f, 3 );
			Pothos( new Vector3( u, y, foot ), y - 4.5f );
		}

		/// <summary>A small succulent in a white pot, standing at <paramref name="foot"/>.</summary>
		void SmallPlant( Vector3 foot )
		{
			var r = Range( 2.4f, 3.2f );
			Kit.SmoothLathe( TrimSkin( Next() < 0.6f ? PotSwatch : CharcoalSwatch ), At( foot ), new[] { new Vector2( r * 0.8f, 0f ), new Vector2( r, r * 1.5f ), new Vector2( r * 0.85f, r * 1.5f ) }, 8, Run, true, true );
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
