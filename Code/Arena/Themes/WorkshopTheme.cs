using System;

/// <summary>
/// Floor 6 (index 5): a workshop. Underfoot, steel checker plate — lens-shaped tread bosses in
/// alternating directions, their crowns polished bright by boots, grime packed around them, brass
/// rivets at the corners (one gone, some dull), scuffs, mill-scale patches and an old oil stain.
/// Overhead (the undersides of the library floor), galvanised sheet: zinc spangle, a cross-break
/// crease, a hemmed edge with screws, white rust blooming here and there.
///
/// The walls, corrugated zinc, are metal (<see cref="ArenaTheme.BakeMetal"/>), mirroring the room
/// through its reflection probe. The floor and ceiling stay on the matte base with a low roughness
/// (ruling R17): a room of nothing but metal mirrors itself — metal takes no ambient light — and
/// came out near black even after the probe's extra bounces, and the floor is what a player reads.
/// </summary>
public sealed class WorkshopTheme : ArenaTheme
{
	public override string Name => "Workshop";
	public override int Floor => 5;
	public override Color LightColor => new( 0.95f, 1.00f, 1.10f );

	static Color Steel => Palette.Hex( 0xA19F9B );
	static Color SteelLight => Palette.Hex( 0xC6C4BF );
	static Color SteelShade => Palette.Hex( 0x7A7874 );

	/// <summary>Grime: oil, dust and rust fines packed into every low spot, warm against the cool steel.</summary>
	static Color Grime => Palette.Hex( 0x544C42 );

	/// <summary>Mill scale and heat tint left from rolling: a darker, faintly blue-grey film.</summary>
	static Color MillScale => Palette.Hex( 0x787B80 );

	static Color Brass => Palette.Hex( 0xC9A26B );
	static Color BrassDull => Palette.Hex( 0x8C7550 );
	static Color Oil => Palette.Hex( 0x2F2D2E );

	/// <summary>Galvanised zinc, warm grey: a blue-grey read as cold under the room's lamps (4b review).</summary>
	static Color Zinc => Palette.Hex( 0x9A9993 );
	static Color ZincLight => Palette.Hex( 0xB5B4AD );
	static Color ZincDark => Palette.Hex( 0x7C7B76 );

	/// <summary>Old zinc gone dull and dark with age.</summary>
	static Color ZincDull => Palette.Hex( 0x83817A );

	/// <summary>Rust bleeding from the screws: a dull brown-orange, well away from the danger colours.</summary>
	static Color Rust => Palette.Hex( 0x8A5E42 );

	/// <summary>Machinery paint on the steelwork, a muted khaki near the floor's brass.</summary>
	static Color Khaki => Palette.Hex( 0x8E7E58 );

	/// <summary>White rust: the chalky bloom zinc grows where it stays damp.</summary>
	static Color WhiteRust => Palette.Hex( 0xC9CCC8 );

	/// <summary>Tread bosses per cell side (the lattice), and each boss's half-length and half-width in pixels.</summary>
	const int Lattice = 6;
	const float BossHalfLength = 27f;
	const float BossHalfWidth = 7f;

	/// <summary>
	/// The structural front layer, and cool-white light where the cage lamps will hang under the
	/// girders (pipes, gauges and the lamps themselves come in Task 9).
	/// </summary>
	public override void Dress( FloorContext room )
	{
		BuildFrontLayer( room, new FrontStyle { Post = 24f, Pier = 64f, Corner = 28f, Cornice = 24f, Skirting = 10f, SkirtingDepth = 4f } );
		AddLamps( room, 6, FloorContext.ClearHeight - 28f, -24f, LightColor * 4.2f, Seed + 900 );
	}

	/// <summary>
	/// Corrugated galvanised sheet, as clad on a shed: sheets of uneven width lapped over each other at
	/// a crest, in two lengths with an end lap at a different height on every sheet, each sheet its own
	/// age — fresher or duller zinc, more or less white rust. Screws with washers on every other crest
	/// along three girts, some haloed with rust and a few bleeding a thin streak down the sheet. Grime
	/// runs down the troughs and gathers in a splash band along the floor. The zinc is warm grey, and
	/// the spangle is a faint patchwork of crystal grains.
	///
	/// The corrugation lives in the relief and the reflection alone, never in the colour, and its
	/// period is a power-of-two number of pixels: at 25.6 px the crests fell between texels in every
	/// mip below the first few, which baked beating bands into the mips themselves, and crest shading
	/// in the colour plus a strong normal and a tight highlight beat against the screen as moiré
	/// (6b review). At 32 px each mip halves the period exactly until the crests average out flat.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		const int Crests = 32;
		const float Pitch = WallSize / (float)Crests;
		const int GrainSize = 16;
		float Row( float v ) => (WallRepeat - v) * Ppu;
		var painter = new Painter( WallSize, Zinc, 0.58f );
		var random = new Random( Seed + 600 );
		float Next() => (float)random.NextDouble();

		// Sheet widths in crests, 5 to 8, adding up to the repeat; each sheet's end lap at its own height.
		var widths = new[] { 7, 7, 6, 6, 6 };
		for ( var k = 0; k < 6; k++ )
		{
			var a = random.Next( 5 ); var b = random.Next( 5 );
			if ( a != b && widths[a] < 8 && widths[b] > 5 ) { widths[a]++; widths[b]--; }
		}
		var starts = new int[6];
		starts[0] = random.Next( Crests );
		for ( var k = 0; k < 5; k++ ) starts[k + 1] = starts[k] + widths[k];
		var laps = new float[5];
		for ( var k = 0; k < 5; k++ ) laps[k] = Row( 98f + Next() * 22f );

		// Ten sheets (five columns, two lengths), each its own zinc and its own weathering.
		var palette = new Color[11];
		var weather = new float[11];
		for ( var k = 1; k <= 10; k++ )
		{
			var tone = Next() * 2f - 1f;
			var zinc = tone < 0f ? Mix( Zinc, ZincDark, -tone * 0.45f ) : Mix( Zinc, ZincLight, tone * 0.45f );
			weather[k] = Next();
			palette[k] = Mix( zinc, ZincDull, weather[k] * 0.55f );
		}

		var warp = painter.Noise( 16, Seed + 601, 2 );
		var rustNoise = painter.Noise( 7, Seed + 602, 3 );
		var runs = painter.Noise( 48, Seed + 603, 2 );
		var owner = new byte[WallSize * WallSize];
		var relief = new float[WallSize * WallSize];
		var corrugation = new float[WallSize * WallSize];
		var spangle = new float[WallSize * WallSize];
		var bloom = new float[WallSize * WallSize];
		var grime = new float[WallSize * WallSize];
		var edge = new float[WallSize * WallSize];
		var wave = new float[WallSize];
		for ( var x = 0; x < WallSize; x++ ) wave[x] = 0.5f + 0.5f * MathF.Cos( x * MathF.Tau / Pitch );

		for ( var y = 0; y < WallSize; y++ )
		{
			var v = WallRepeat - y / Ppu;
			var splash = v < 30f ? (30f - v) / 30f : 0f;
			for ( var x = 0; x < WallSize; x++ )
			{
				var i = y * WallSize + x;

				// Which sheet: the column by crest (wrapping), the length by that column's lap.
				var crest = (x / Pitch - starts[0] + Crests * 2) % Crests;
				var column = 0;
				while ( column < 4 && crest >= starts[column + 1] - starts[0] ) column++;
				var upper = y < laps[column];
				var sheet = 1 + column * 2 + (upper ? 1 : 0);
				owner[i] = (byte)sheet;

				// Corrugation, a hair proud where a sheet laps over its neighbour and over the length below.
				var intoColumn = crest - (starts[column] - starts[0]);
				var side = intoColumn < 0.5f ? 0.07f : 0f;
				var end = upper && laps[column] - y < 5f ? 0.08f : 0f;
				corrugation[i] = wave[x] * 0.8f;
				relief[i] = side + end;
				if ( !upper && y - laps[column] < 7f ) edge[i] = (1f - (y - laps[column]) / 7f) * 0.55f;
				if ( intoColumn > 0.48f && intoColumn < 0.56f ) edge[i] = MathF.Max( edge[i], 0.4f );

				// Spangle: crystal grains as a warped patchwork, each a little brighter or duller; grains
				// a whole number of times across the texture, so the patchwork runs on over its wrap.
				const int Grains = WallSize / GrainSize;
				var gx = ((int)MathF.Floor( (x + (warp[i] - 0.5f) * 30f) / GrainSize ) + Grains) % Grains;
				var gy = ((int)MathF.Floor( (y + (warp[(i + 512 * WallSize + 512) % (WallSize * WallSize)] - 0.5f) * 30f) / GrainSize ) + Grains) % Grains;
				spangle[i] = (Bits( Hash( gx, gy, Seed ), 0 ) - 0.5f) * (1f - weather[sheet]) * 0.5f;

				var r = rustNoise[i];
				bloom[i] = r > 0.62f ? MathF.Min( (r - 0.62f) * 4f, 1f ) * (0.15f + weather[sheet] * 0.45f) : 0f;

				// Grime down the troughs, read as long thin streaks; and the splash band at the foot.
				var s = StreakX( runs, WallSize, y, x, 37 );
				grime[i] = (s > 0.55f ? MathF.Min( (s - 0.55f) * 3f, 1f ) * (1f - wave[x]) * 0.35f : 0f) + splash * splash * 0.5f;
			}
		}

		painter.AddHeight( relief, 1f );
		painter.AddHeight( corrugation, 1f );
		painter.Blend( owner, palette );
		var lighter = new float[WallSize * WallSize];
		for ( var i = 0; i < spangle.Length; i++ ) { lighter[i] = spangle[i] > 0f ? spangle[i] : 0f; spangle[i] = spangle[i] < 0f ? -spangle[i] : 0f; }
		painter.Blend( lighter, ZincLight );
		painter.Blend( spangle, ZincDark );
		painter.Blend( bloom, WhiteRust, roughness: 0.85f );
		painter.Blend( grime, Grime, roughness: 0.7f );
		painter.Blend( edge, Mix( ZincDark, Grime, 0.5f ) );

		// Screws with washers on every other crest along three girts; some rusty, a few bleeding.
		for ( var c = 0; c < 5; c++ )
		{
			var girts = new[] { Row( 24f ), laps[c] + 10f, Row( 170f ) };
			for ( var k = starts[c] + 1; k < starts[c + 1]; k += 2 )
			foreach ( var gy in girts )
			{
				var x = (k % Crests) * Pitch + (Next() - 0.5f) * 1.5f;
				var y = gy + (Next() - 0.5f) * 3f;
				var rusty = Next();
				if ( rusty < 0.35f ) painter.Circle( x, y, 7f, Rust, 0.18f + rusty * 0.4f );
				if ( rusty < 0.12f )
				{
					var length = 40f + Next() * 110f;
					var width = 2.5f + Next() * 2.5f;
					for ( var s = 0; s < 6; s++ )
						painter.Line( new Vector2( x, y + length * s / 6f ), new Vector2( x + (Next() - 0.5f) * 2f, y + length * (s + 1) / 6f ), width * (1f - s * 0.12f), Rust, 0.22f * (1f - s / 7f) );
				}
				painter.Circle( x, y, 4.6f, ZincLight, height: 1.05f, bevel: 2f, roughness: 0.4f );
				painter.Circle( x, y, 2.3f, Mix( ZincDark, rusty < 0.35f ? Rust : ZincDark, 0.5f ), height: 1.25f, bevel: 1.6f, roughness: 0.5f );
			}
		}

		painter.HeightNoise( 0.15f, 3, Seed + 604, 2 );
		painter.Mottle( 0.04f, 8, Seed + 605, 3 );
		painter.RoughnessNoise( 0.08f, 6, Seed + 606, 3 );

		// The cavity bake sees the laps, girts and screws but not the corrugation, which would put the
		// crests back into the colour.
		painter.AddHeight( corrugation, -1f );
		painter.Cavity( 0.3f, 5 );
		painter.AddHeight( corrugation, 1f );
		painter.Grain( 0.01f, Seed + 607 );
		return BakeMetal( painter, "arena_5_wall", 2.5f, 0.65f );
	}

	/// <summary>
	/// The steelwork in machinery khaki, chipped to bare steel at the arrises: the cornice is a girder
	/// (flanges proud of the web, rivets along both), the posts riveted columns with flange plates down
	/// their edges, the skirting a band of worn hazard stripes in ink and paper (never the warning
	/// colour), the slab edge a bolted steel angle.
	/// </summary>
	protected override PaintedSurface PaintTrim()
	{
		return PaintTrimAtlas( "arena_5_trim", Khaki, Grime, 0.55f, 0f, Seed + 700, painter =>
		{
			PaintBand( painter, TrimBand.Cornice, Khaki, GirderProfile, 0.55f );
			ShadeBand( painter, TrimBand.Cornice, GirderProfile, Grime, 0.6f );
			PaintBand( painter, TrimBand.Pilaster, Khaki, ColumnProfile, 0.55f );
			ShadeBand( painter, TrimBand.Pilaster, ColumnProfile, Grime, 0.5f );

			// Rivets along both flanges of the girder and both plates of the columns, at an even rivet
			// pitch that divides the band, so they run on seamlessly along the run.
			const float RivetPitch = TrimSize / 24f;
			foreach ( var (band, rows) in new[] { (TrimBand.Cornice, new[] { 0.08f, 0.92f }), (TrimBand.Pilaster, new[] { 0.07f, 0.93f }) } )
			{
				var (top, height) = TrimRows( band );
				for ( var k = 0; k < 24; k++ )
				foreach ( var t in rows )
					painter.Circle( k * RivetPitch + RivetPitch * 0.5f, top + height * t, 3f, Khaki, height: 1.3f, bevel: 2.4f, roughness: 0.45f );
			}

			// Hazard stripes on the skirting, worn through to steel where boots and trolleys hit them.
			{
				var (top, height) = TrimRows( TrimBand.Skirting );
				for ( var y = top; y < top + height; y++ )
				for ( var x = 0; x < TrimSize; x++ )
				{
					// A 64 px period, which divides the band's 512 px run, so the stripes carry on across the wrap.
					var stripe = ((x + y) / 32) % 2 == 0;
					painter.Fill( x, y, 1, 1, stripe ? Mix( Palette.Ink, Grime, 0.2f ) : Mix( Palette.PaperShade, Khaki, 0.25f ) );
				}
				var scuffs = Ramp( painter.Noise( 32, Seed + 710, 3 ), 0.6f, 0.8f, 0.6f );
				for ( var i = 0; i < scuffs.Length; i++ )
					if ( i / TrimSize < top || i / TrimSize >= top + height ) scuffs[i] = 0f;
				painter.Blend( scuffs, SteelShade, roughness: 0.4f );
			}

			// The slab edge: a steel angle, bolted.
			{
				var (top, height) = TrimRows( TrimBand.SlabEdge );
				TintBand( painter, TrimBand.SlabEdge, SteelShade, 0.4f, NosingProfile, Grime, 0.5f );
				for ( var x = 16; x < TrimSize; x += 64 )
				{
					painter.Circle( x, top + height * 0.6f, 5f, Steel, height: 1.25f, bevel: 3f, roughness: 0.35f );
					painter.Circle( x, top + height * 0.6f, 2.2f, SteelShade, height: 1.35f, bevel: 1f, roughness: 0.4f );
				}
			}

			// Paint chipped to steel in small flakes over all the khaki.
			var chips = Ramp( painter.Noise( 48, Seed + 711, 3 ), 0.7f, 0.8f, 0.7f );
			var (skirt, skirtHeight) = TrimRows( TrimBand.Skirting );
			var infill = TrimRows( TrimBand.Infill ).Top;
			for ( var i = 0; i < chips.Length; i++ )
			{
				var row = i / TrimSize;
				if ( row >= infill || row >= skirt && row < skirt + skirtHeight ) chips[i] = 0f;
			}
			painter.Blend( chips, SteelShade, roughness: 0.35f );
		} );
	}

	/// <summary>A girder seen from the side: the flanges at top and bottom proud of the web between them.</summary>
	static float GirderProfile( float t )
	{
		var flange = MathF.Min( t, 1f - t );
		return flange < 0.16f ? 1f : flange < 0.2f ? 1f - (flange - 0.16f) / 0.04f * 0.4f : 0.6f;
	}

	/// <summary>A built-up column's face: riveted flange plates down both edges, proud of the plate between.</summary>
	static float ColumnProfile( float t )
	{
		var edge = MathF.Min( t, 1f - t );
		return edge < 0.02f ? 0.7f + edge * 15f : edge < 0.14f ? 1f : edge < 0.16f ? 1f - (edge - 0.14f) / 0.02f * 0.15f : 0.85f;
	}

	protected override PaintedSurface PaintFloor()
	{
		const int cell = FloorSize / 2;
		const float pitch = cell / (float)Lattice;
		var painter = new Painter( FloorSize, Grime, 0.6f );
		var random = new Random( Seed );
		float Next() => (float)random.NextDouble();

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var tone = Next() * 2f - 1f;
			var plate = tone < 0f ? Mix( Steel, SteelShade, -tone * 0.35f ) : Mix( Steel, SteelLight, tone * 0.2f );

			// Sheared plate: straighter than stone, but a cut edge is never quite true, and the plate
			// bends down over the arris to the joint at the chamfer's foot.
			painter.Fill( (int)min.x, (int)min.y, cell, cell, Mix( SteelShade, Grime, 0.55f ), height: 0.5f, roughness: 0.7f );
			painter.Polygon( SlabOutline( min, cell, 6f + Next() * 3f, 1.2f, 3f + Next() * 4f, Seed + c * 101 ), plate, height: 1f, bevel: Rim, roughness: 0.42f );
		}

		PaintTread( painter );

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );

			// Rivets where the corner bosses would be: domed brass, some dulled with grime, one hole
			// on the plate where a rivet has gone.
			for ( var k = 0; k < 4; k++ )
			{
				var at = min + new Vector2( k % 2 == 0 ? pitch * 0.5f : cell - pitch * 0.5f, k < 2 ? pitch * 0.5f : cell - pitch * 0.5f )
					+ new Vector2( Next() - 0.5f, Next() - 0.5f ) * 3f;
				var radius = 6.5f + Next() * 1.5f;
				painter.Circle( at.x, at.y, radius + 3f, Grime, 0.35f, roughness: 0.7f );
				if ( c == 1 && k == 2 )
				{
					painter.Circle( at.x, at.y, radius * 0.75f, Grime, 0.95f, height: 0.55f, bevel: 1.5f, roughness: 0.9f );
					continue;
				}
				var dull = Next();
				var metal = Mix( Mix( Brass, BrassDull, dull * 0.8f ), Grime, dull * 0.25f );
				painter.Circle( at.x, at.y, radius, metal, height: 1.55f, bevel: radius * 0.9f, roughness: 0.35f + dull * 0.3f );
			}

			// Scuffs: short bright scratches from dragged crates and boots, every which way.
			var scuffs = 35 + random.Next( 25 );
			for ( var s = 0; s < scuffs; s++ )
			{
				var at = min + new Vector2( 34f + Next() * 444f, 34f + Next() * 444f );
				var angle = Next() * MathF.Tau;
				var length = 5f + Next() * Next() * 34f;
				var to = at + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * length;
				painter.Line( at, to, 0.7f + Next() * 0.5f, SteelLight, 0.12f + Next() * 0.2f, roughness: 0.3f );
			}

			// An old oil stain on variant 2: soaked into the grime, glossy, soft-edged, small.
			if ( c == 2 )
			{
				var centre = min + new Vector2( 160f + Next() * 190f, 160f + Next() * 190f );
				var half = new Vector2( 42f + Next() * 14f, 32f + Next() * 12f );
				var angle = Next() * MathF.Tau;
				for ( var layer = 0; layer < 3; layer++ )
					painter.Polygon( Organic( centre, half * (1f - layer * 0.3f), 0.9f, Seed + 70 + layer, angle ), Oil, alpha: 0.07f, roughness: 0.22f );
			}
		}

		// Mill scale and heat tint in broad patches, laid out differently on each of the four plates.
		painter.Blend( Ramp( painter.Noise( 3, Seed + 30, 2 ), 0.5f, 0.8f, 0.4f ), MillScale );

		// Grime worked into the plate in drifts, thicker away from the walking lines.
		painter.Blend( Ramp( painter.Noise( 6, Seed + 36, 2 ), 0.6f, 0.9f, 0.2f ), Grime, roughness: 0.7f );
		painter.HeightNoise( 0.35f, 4, Seed + 31, 2 );
		painter.Mottle( 0.05f, 4, Seed + 32, 3 );
		painter.RoughnessNoise( 0.08f, 6, Seed + 33, 3 );
		painter.Cavity( 0.55f, 3 );
		painter.EdgeWear( 0.75f, 3, Seed + 34 );
		painter.Grain( 0.012f, Seed + 35 );
		return painter.Bake( "arena_5_floor", Painter.Matte, 6f );
	}

	/// <summary>
	/// Diamond tread, computed per pixel: a lens (two intersecting circles) at every lattice point,
	/// turned +45° and −45° in a checkerboard, as rolled plate is. The rolls are exact, the wear is not:
	/// each boss sits a hair off its point, a few percent longer or shorter, and its crown is polished
	/// by its own amount. The four corner points are left flat for the rivets.
	/// </summary>
	void PaintTread( Painter painter )
	{
		const int cell = FloorSize / 2;
		const float pitch = cell / (float)Lattice;
		var relief = new float[FloorSize * FloorSize];
		var crown = new float[FloorSize * FloorSize];
		var reach = (int)MathF.Ceiling( BossHalfLength * 0.72f + 2f );

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			for ( var j = 0; j < Lattice; j++ )
			for ( var i = 0; i < Lattice; i++ )
			{
				if ( (i == 0 || i == Lattice - 1) && (j == 0 || j == Lattice - 1) ) continue;

				var h = Hash( i, j, Seed + c * 31 );
				var cx = min.x + (i + 0.5f) * pitch + (Bits( h, 0 ) - 0.5f) * 2.4f;
				var cy = min.y + (j + 0.5f) * pitch + (Bits( h, 8 ) - 0.5f) * 2.4f;
				var halfLength = BossHalfLength * (0.95f + Bits( h, 16 ) * 0.1f);
				var polish = 0.15f + Bits( h, 24 ) * 0.35f;

				// Lens of half-length b and half-width w: circles of radius r at ±d across it.
				var w = BossHalfWidth;
				var r = (halfLength * halfLength / w + w) * 0.5f;
				var d = r - w;
				var flip = (i + j) % 2 == 0 ? 1f : -1f;

				for ( var y = (int)cy - reach; y <= (int)cy + reach; y++ )
				{
					var dy = y + 0.5f - cy;
					var row = y * FloorSize;
					for ( var x = (int)cx - reach; x <= (int)cx + reach; x++ )
					{
						var dx = x + 0.5f - cx;
						var along = MathF.Abs( (dx + dy * flip) * 0.70710678f );
						var across = MathF.Abs( (dx - dy * flip) * 0.70710678f );
						float sd;
						if ( (along - halfLength) * d > across * halfLength )
						{
							var ex = across; var ey = along - halfLength;
							sd = MathF.Sqrt( ex * ex + ey * ey );
						}
						else
						{
							var ex = across + d;
							sd = MathF.Sqrt( ex * ex + along * along ) - r;
						}
						if ( sd >= 1f ) continue;

						// Rounded shoulders a few pixels wide, then a low crown along the ridge.
						var t = MathF.Min( MathF.Max( (1f - sd) / 5f, 0f ), 1f );
						var shoulder = t * t * (3f - 2f * t);
						var ridge = 1f - along / halfLength;
						var i0 = row + x;
						relief[i0] = shoulder * (0.5f + ridge * 0.12f);
						crown[i0] = shoulder * ridge * polish;
					}
				}
			}
		}

		painter.AddHeight( relief, 1f );
		painter.Blend( crown, SteelLight, roughness: 0.32f );
	}

	/// <summary>
	/// Galvanised sheet, one per tile underside. Spangle — the zinc's crystal grains — is a jittered
	/// Voronoi pattern, each grain its own brightness and finish with a slight sheen gradient across
	/// it, as real spangle catches light half by half. A cross-break crease runs corner to corner
	/// (sheet-metal panels are stiffened that way), the edge is hemmed and screwed, and white rust
	/// blooms in soft patches.
	/// </summary>
	protected override PaintedSurface PaintCeiling()
	{
		const int cell = CeilingSize / 2;
		const int Hem = 7;
		var painter = new Painter( CeilingSize, ZincDark, 0.45f );
		var random = new Random( Seed + 500 );
		float Next() => (float)random.NextDouble();

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );
			var tone = Next() * 2f - 1f;
			var zinc = tone < 0f ? Mix( Zinc, ZincDark, -tone * 0.4f ) : Mix( Zinc, ZincLight, tone * 0.4f );
			painter.Fill( (int)min.x, (int)min.y, cell, cell, Mix( zinc, ZincLight, 0.15f ), height: 1.15f );
			painter.Polygon( new[]
			{
				min + new Vector2( Hem, Hem ), min + new Vector2( cell - Hem, Hem ),
				min + new Vector2( cell - Hem, cell - Hem ), min + new Vector2( Hem, cell - Hem ),
			}, zinc, height: 0.9f, bevel: 3f, roughness: 0.45f );
		}

		PaintSpangle( painter );

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );

			// Screws along the hem, spaced by hand.
			for ( var side = 0; side < 4; side++ )
			{
				for ( var along = 18f + Next() * 10f; along < cell - 14f; along += 52f + Next() * 16f )
				{
					var at = side switch
					{
						0 => min + new Vector2( along, Hem * 0.5f ), 1 => min + new Vector2( cell - Hem * 0.5f, along ),
						2 => min + new Vector2( cell - along, cell - Hem * 0.5f ), _ => min + new Vector2( Hem * 0.5f, cell - along ),
					};
					painter.Circle( at.x, at.y, 2.6f, ZincDark, height: 1.45f, bevel: 2f, roughness: 0.4f );
					var slot = Next() * MathF.PI;
					var dir = new Vector2( MathF.Cos( slot ), MathF.Sin( slot ) ) * 1.8f;
					painter.Line( at - dir, at + dir, 0.7f, Grime, 0.6f );
				}
			}
		}

		painter.Blend( Ramp( painter.Noise( 5, Seed + 520, 3 ), 0.62f, 0.85f, 0.35f ), WhiteRust, roughness: 0.85f );
		painter.Mottle( 0.04f, 3, Seed + 521, 4 );
		painter.Cavity( 0.4f, 3 );
		painter.Grain( 0.01f, Seed + 522 );
		return painter.Bake( "arena_5_ceiling", Painter.Matte, 3f );
	}

	/// <summary>Spangle grains and the cross-break crease, in one pass over the ceiling.</summary>
	void PaintSpangle( Painter painter )
	{
		const int cell = CeilingSize / 2;
		const int Spangle = 13;
		const int grid = CeilingSize / Spangle + 2;
		var bright = new float[CeilingSize * CeilingSize];
		var dark = new float[CeilingSize * CeilingSize];
		var crease = new float[CeilingSize * CeilingSize];

		// One jittered seed point per grid square, with its grain's tone and sheen direction.
		var pointX = new float[grid * grid];
		var pointY = new float[grid * grid];
		var sheenX = new float[grid * grid];
		var sheenY = new float[grid * grid];
		var tones = new float[grid * grid];
		for ( var gy = 0; gy < grid; gy++ )
		for ( var gx = 0; gx < grid; gx++ )
		{
			var k = gy * grid + gx;
			var h = Hash( gx, gy, Seed + 540 );
			pointX[k] = (gx + 0.1f + Bits( h, 0 ) * 0.8f) * Spangle;
			pointY[k] = (gy + 0.1f + Bits( h, 8 ) * 0.8f) * Spangle;
			var angle = Bits( h, 16 ) * MathF.Tau;
			sheenX[k] = MathF.Cos( angle ) * 0.35f / Spangle;
			sheenY[k] = MathF.Sin( angle ) * 0.35f / Spangle;
			tones[k] = (Bits( h, 24 ) - 0.5f) * 0.7f;
		}

		for ( var y = 0; y < CeilingSize; y++ )
		{
			var gy = y / Spangle;
			for ( var x = 0; x < CeilingSize; x++ )
			{
				var gx = x / Spangle;
				var px = x + 0.5f; var py = y + 0.5f;
				var best = float.MaxValue; var owner = 0;
				for ( var oy = Math.Max( gy - 1, 0 ); oy <= Math.Min( gy + 1, grid - 1 ); oy++ )
				for ( var ox = Math.Max( gx - 1, 0 ); ox <= Math.Min( gx + 1, grid - 1 ); ox++ )
				{
					var k = oy * grid + ox;
					var ddx = px - pointX[k]; var ddy = py - pointY[k];
					var dd = ddx * ddx + ddy * ddy;
					if ( dd < best ) { best = dd; owner = k; }
				}

				// Each grain: its own tone, and a sheen gradient across it in its own direction.
				var tone = tones[owner] + (px - pointX[owner]) * sheenX[owner] + (py - pointY[owner]) * sheenY[owner];
				var i = y * CeilingSize + x;
				if ( tone > 0f ) bright[i] = MathF.Min( tone, 1f ) * 0.55f; else dark[i] = MathF.Min( -tone, 1f ) * 0.45f;

				// The cross-break: a soft ridge along both diagonals of the sheet.
				var lx = x % cell + 0.5f; var ly = y % cell + 0.5f;
				var toDiagonal = MathF.Min( MathF.Abs( lx - ly ), MathF.Abs( lx + ly - cell ) ) * 0.70710678f;
				var t = MathF.Max( 1f - toDiagonal / 14f, 0f );
				crease[i] = t * t * 0.18f;
			}
		}

		painter.Blend( bright, ZincLight );
		painter.Blend( dark, ZincDark );
		painter.AddHeight( crease, 1f );
	}
}
