using System;

/// <summary>
/// Floor 8 (index 7): a forge over the lava. Underfoot, the tops of basalt columns — five-, six-
/// and seven-sided, each its own height, tone and weathering, dished or domed, pitted with gas
/// vesicles, dusted with ash — and, the one danger colour on any floor, thin ember cracks glowing in
/// a few of the joints, broken and short, with a scorched halo. Overhead (the undersides of the
/// dungeon floor, and of its own tiles where they face the lava shaft), fissured rock with a few
/// ember glints caught in the cracks.
/// </summary>
public sealed class ForgeTheme : ArenaTheme
{
	public override string Name => "Forge";
	public override int Floor => 7;
	public override Color LightColor => new( 1.60f, 0.62f, 0.36f );

	static Color Basalt => Palette.Hex( 0x4A4250 );
	static Color BasaltLight => Palette.Hex( 0x5E5566 );
	static Color BasaltShade => Palette.Hex( 0x352F3A );

	/// <summary>Weathered columns lean brown-grey; fresh ones toward the floor's dark violet.</summary>
	static Color BasaltBrown => Palette.Hex( 0x51473F );

	/// <summary>The joints between columns: near black.</summary>
	static Color Fissure => Palette.Hex( 0x1F1B22 );

	/// <summary>Ash settled on the stone.</summary>
	static Color Ash => Palette.Hex( 0x6E6770 );

	/// <summary>Scorched stone either side of a hot crack.</summary>
	static Color Scorch => Palette.Hex( 0x5A2E2A );

	static Color Rock => Palette.Hex( 0x3B3440 );
	static Color RockLight => Palette.Hex( 0x4D4553 );
	static Color RockDark => Palette.Hex( 0x2A2430 );
	static Color Soot => Palette.Hex( 0x1E1A20 );

	/// <summary>Wrought iron, sooted; its rubbed arrises and rivet heads a little brighter.</summary>
	static Color Iron => Palette.Hex( 0x57524F );
	static Color IronLight => Palette.Hex( 0x6E6863 );

	/// <summary>Column spacing in pixels: about 17 units, a 40 cm column.</summary>
	const float ColumnSpacing = 150f;

	/// <summary>
	/// The structural front layer, and the forge's light: four low ember-orange lamps by the walls where
	/// the forge mouths will stand (Task 10), and two dim red fills out in the room for the glow off the
	/// lava below — the room has no floor under it to leak into, so they may reach further than the rest.
	/// </summary>
	public override void Dress( FloorContext room )
	{
		BuildFrontLayer( room, new FrontStyle { Post = 36f, Pier = 80f, Corner = 38f, Cornice = 30f, Skirting = 12f, SkirtingDepth = 6f } );
		AddLamps( room, 4, 40f, -12f, 640f, LightColor * 3.6f, Seed + 900 );
		AddLamps( room, 2, 110f, -360f, 900f, new Color( 1.4f, 0.55f, 0.4f ) * 2.6f, Seed + 901, 0.1f );
	}

	/// <summary>
	/// Dark rock with lava seams, the forge's walls and the shaft below them: broken basalt faces (a
	/// jittered Voronoi of tilted facets that meet in soft creases), faint strata, browner and paler
	/// patches, soot thickening toward the ceiling. Seams of lava — the one place the forge may use the
	/// danger colour — rise from the floor and branch, hot and wide at the foot, thinning to a thread
	/// higher up, each in a scorched halo with a dull glow around its core.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		var painter = new Painter( WallSize, Rock, 0.9f );
		var random = new Random( Seed + 610 );
		float Next() => (float)random.NextDouble();

		PaintFacets( painter, 128, Seed + 611, 0.35f, 0.4f );

		painter.Blend( Ramp( painter.Noise( 4, Seed + 600, 4 ), 0.5f, 0.8f, 0.3f ), RockLight );
		painter.Blend( Ramp( painter.Noise( 6, Seed + 601, 4 ), 0.55f, 0.85f, 0.35f ), RockDark );
		painter.Blend( Ramp( painter.Noise( 3, Seed + 602, 3 ), 0.6f, 0.9f, 0.3f ), BasaltBrown );

		var strata = painter.Noise( 4, Seed + 603, 2 );
		var bands = new float[WallSize * WallSize];
		for ( var y = 0; y < WallSize; y++ )
		for ( var x = 0; x < WallSize; x++ )
			bands[y * WallSize + x] = StreakX( strata, WallSize, x, y, 3 ) - 0.5f;
		painter.AddHeight( bands, 0.3f );
		painter.HeightNoise( 0.7f, 4, Seed + 604, 4 );
		painter.HeightNoise( 0.2f, 96, Seed + 605, 2 );

		// Soot from the forge hangs under the ceiling.
		var smoke = painter.Noise( 5, Seed + 606, 3 );
		for ( var y = 0; y < WallSize; y++ )
		{
			var v = WallRepeat - y / Ppu;
			var high = v > 120f ? (v - 120f) / 88f : 0f;
			for ( var x = 0; x < WallSize; x++ ) smoke[y * WallSize + x] = high * (0.3f + smoke[y * WallSize + x] * 0.4f);
		}
		painter.Blend( smoke, Soot );

		// Seams: from the floor up, branching. The danger colour is only in their cores.
		var seams = 4 + random.Next( 2 );
		for ( var s = 0; s < seams; s++ )
		{
			var start = new Vector2( (s + Next() * 0.7f) * WallSize / seams, WallSize - Next() * 30f );
			var path = Crack( start, -MathF.PI * 0.5f + (Next() - 0.5f) * 0.6f, 9f, 24 + random.Next( 30 ), random );
			PaintSeam( painter, path, 3.4f + Next() * 1.4f, random );
			for ( var b = random.Next( 3 ); b > 0 && path.Length > 10; b-- )
			{
				var from = path[3 + random.Next( path.Length * 2 / 3 )];
				var branch = Crack( from, -MathF.PI * 0.5f + (Next() < 0.5f ? -1f : 1f) * (0.5f + Next() * 0.6f), 7f, 5 + random.Next( 10 ), random );
				PaintSeam( painter, branch, 1.3f + Next() * 0.9f, random );
			}
		}

		painter.ShadeFromHeight( 0.25f, 0.3f );
		painter.Mottle( 0.1f, 32, Seed + 608, 3 );
		painter.Cavity( 0.55f, 4 );
		painter.Grain( 0.015f, Seed + 609 );
		return painter.Bake( "arena_7_wall", Painter.Matte, 7f );
	}

	/// <summary>
	/// One lava seam along a polyline, its core <paramref name="width"/> px at the start (the foot of
	/// the wall) thinning to a third at its end: a broad scorched halo, a dull ember glow, then the
	/// thin hot core cut into the rock.
	/// </summary>
	static void PaintSeam( Painter painter, Vector2[] path, float width, Random random )
	{
		// Every segment its own width, pinching nearly shut here and there, so the seam is a broken
		// crack rather than a drawn line.
		var widths = new float[path.Length];
		for ( var k = 1; k < path.Length; k++ )
		{
			var t = (k - 0.5f) / (path.Length - 1);
			var pinch = random.NextDouble() < 0.15 ? 0.25f : 0.7f + (float)random.NextDouble() * 0.6f;
			widths[k] = width * (1f - t * 0.7f) * pinch;
		}
		for ( var k = 1; k < path.Length; k++ )
		{
			painter.Line( path[k - 1], path[k], widths[k] * 2f + 18f, Scorch, 0.1f );
			painter.Line( path[k - 1], path[k], widths[k] + 7f, Mix( Scorch, Palette.Lava, 0.4f ), 0.22f );
		}
		for ( var k = 1; k < path.Length; k++ )
		{
			painter.Line( path[k - 1], path[k], widths[k] + 2f, Mix( Palette.Lava, Scorch, 0.35f ), 0.8f, height: 0.25f, roughness: 0.9f );
			painter.Line( path[k - 1], path[k], widths[k] * 0.55f, Mix( Palette.Lava, Palette.Hex( 0xFFC27A ), 0.35f ), 0.95f, height: 0.15f, roughness: 0.9f );
		}
	}

	/// <summary>A jagged crack: short straight runs that kink sharply, drifting about a heading.</summary>
	static Vector2[] Crack( Vector2 start, float heading, float step, int steps, Random random )
	{
		var points = new Vector2[steps + 1];
		points[0] = start;
		var angle = heading;
		for ( var s = 1; s <= steps; s++ )
		{
			angle = heading + (angle - heading) * 0.6f + ((float)random.NextDouble() - 0.5f) * 1.6f;
			points[s] = points[s - 1] + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * step * (0.5f + (float)random.NextDouble());
		}
		return points;
	}

	/// <summary>
	/// Wrought iron for the front layer, smith's work: riveted plate posts with edge straps, a lintel
	/// with a row of rivets under the ceiling, an iron kerb at the skirting, a bolted angle on the slab
	/// edge. Soot in the hollows, heat tarnish — a faint bronze and a faint blue — in patches, the
	/// arrises rubbed bright. Baked as metal, so it carries the glints of the forge's fires.
	/// </summary>
	protected override PaintedSurface PaintTrim()
	{
		var painter = new Painter( TrimSize, Iron, 0.5f );
		PaintBand( painter, TrimBand.Cornice, Iron, CorniceProfile, 0.5f );
		PaintBand( painter, TrimBand.Skirting, Mix( Iron, Soot, 0.2f ), SkirtingProfile, 0.55f );
		PaintBand( painter, TrimBand.Pilaster, Iron, StrapProfile, 0.5f );
		PaintBand( painter, TrimBand.SlabEdge, Mix( Iron, Soot, 0.15f ), NosingProfile, 0.5f );
		PaintBand( painter, TrimBand.Stock, Iron, _ => 0.8f, 0.5f );
		PaintBand( painter, TrimBand.Infill, Iron, _ => 0.8f, 0.5f );
		ShadeBand( painter, TrimBand.Cornice, CorniceProfile, Soot, 0.6f );
		ShadeBand( painter, TrimBand.Pilaster, StrapProfile, Soot, 0.5f );
		ShadeBand( painter, TrimBand.Skirting, SkirtingProfile, Soot, 0.5f );

		// Rivets: along both straps of the posts, a row under the lintel's fillet, bolts on the angle.
		const float Pitch = TrimSize / 16f;
		var (ctop, cheight) = TrimRows( TrimBand.Cornice );
		var (ptop, pheight) = TrimRows( TrimBand.Pilaster );
		var (stop, sheight) = TrimRows( TrimBand.SlabEdge );
		for ( var k = 0; k < 16; k++ )
		{
			var x = k * Pitch + Pitch * 0.5f;
			painter.Circle( x, ctop + cheight * 0.76f, 3.4f, IronLight, height: 1.25f, bevel: 2.8f, roughness: 0.4f );
			painter.Circle( x, ptop + pheight * 0.08f, 3.2f, IronLight, height: 1.25f, bevel: 2.6f, roughness: 0.4f );
			painter.Circle( x + Pitch * 0.5f, ptop + pheight * 0.92f, 3.2f, IronLight, height: 1.25f, bevel: 2.6f, roughness: 0.4f );
			if ( k % 3 == 0 ) painter.Circle( x, stop + sheight * 0.6f, 4.5f, IronLight, height: 1.3f, bevel: 3f, roughness: 0.4f );
		}

		// Heat tarnish and soot in broad, faint patches.
		painter.Blend( Ramp( painter.Noise( 6, Seed + 710, 3 ), 0.6f, 0.85f, 0.25f ), Palette.Hex( 0x6B5638 ) );
		painter.Blend( Ramp( painter.Noise( 5, Seed + 711, 3 ), 0.65f, 0.9f, 0.2f ), Palette.Hex( 0x464C5E ) );
		painter.Blend( Ramp( painter.Noise( 4, Seed + 712, 3 ), 0.5f, 0.85f, 0.4f ), Soot, roughness: 0.8f );

		painter.HeightNoise( 0.1f, 32, Seed + 713, 3 );
		painter.Mottle( 0.06f, 8, Seed + 714, 3 );
		painter.RoughnessNoise( 0.08f, 12, Seed + 715, 2 );
		painter.Cavity( 0.45f, 3 );
		painter.EdgeWear( 0.45f, 3, Seed + 716 );
		painter.Grain( 0.012f, Seed + 717 );
		return BakeMetal( painter, "arena_7_trim", 4f, 0.3f );
	}

	/// <summary>An iron post's face: straps down both edges, a hair proud of the plate between, their arrises rounded.</summary>
	static float StrapProfile( float t )
	{
		var edge = MathF.Min( t, 1f - t );
		return edge < 0.03f ? 0.75f + edge * 8f : edge < 0.17f ? 1f : edge < 0.19f ? 1f - (edge - 0.17f) / 0.02f * 0.18f : 0.82f;
	}

	protected override PaintedSurface PaintFloor()
	{
		var painter = new Painter( FloorSize, Fissure, 0.85f );
		var random = new Random( Seed );
		float Next() => (float)random.NextDouble();

		PaintColumns( painter, random );

		// Vesicles: gas bubbles frozen in the lava, small pits scattered over the tops.
		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var pits = 110 + random.Next( 60 );
			for ( var p = 0; p < pits; p++ )
			{
				var at = min + new Vector2( 30f + Next() * 452f, 30f + Next() * 452f );
				var radius = 0.6f + Next() * Next() * 2.2f;
				painter.Circle( at.x, at.y, radius, Mix( Basalt, Fissure, 0.6f ), 0.75f, height: 0.82f, bevel: radius, roughness: 0.95f );
			}
		}

		// Ash in drifts across the floor.
		painter.Blend( Ramp( painter.Noise( 5, Seed + 30, 2 ), 0.58f, 0.85f, 0.3f ), Ash, roughness: 0.95f );

		painter.HeightNoise( 0.45f, 8, Seed + 31, 2 );
		painter.Mottle( 0.08f, 4, Seed + 32, 3 );
		painter.RoughnessNoise( 0.06f, 6, Seed + 33, 2 );
		painter.Cavity( 0.5f, 4 );
		painter.EdgeWear( 0.3f, 3, Seed + 34 );
		painter.Grain( 0.015f, Seed + 35 );
		return painter.Bake( "arena_7_floor", Painter.Matte, 7f );
	}

	/// <summary>
	/// The column tops, per pixel: a jittered hexagonal lattice of seed points (so cells come out
	/// with five to seven sides), the nearest seed owning the pixel and the distance to the bisector
	/// with the second nearest giving the joint and a weathered shoulder. Columns stand at their own
	/// heights; some tops are dished, some domed. A few joints — chosen per pair of columns — carry a
	/// thin ember crack, broken by noise so it glows in short runs, with scorched stone beside it.
	/// The block's edge is a cut, unwarped, so neighbouring tiles meet at the chamfer's foot.
	/// </summary>
	void PaintColumns( Painter painter, Random random )
	{
		const int cell = FloorSize / 2;
		const float rowSpacing = ColumnSpacing * 0.866f;
		var rows = (int)(cell / rowSpacing) + 3;
		var columns = (int)(cell / ColumnSpacing) + 3;
		var chips = painter.Noise( 40, Seed + 20, 1 );
		var glow = painter.Noise( 24, Seed + 21, 2 );

		// Joint and ember weights reuse the chips and glow fields: each is read only at its own pixel.
		var relief = new float[FloorSize * FloorSize];
		var owners = new byte[FloorSize * FloorSize];
		var scorch = new float[FloorSize * FloorSize];
		var joint = chips; var ember = glow;
		var palette = new Color[256];
		var pointX = new float[rows * columns];
		var pointY = new float[rows * columns];
		var lift = new float[rows * columns];
		var dish = new float[rows * columns];

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );

			// Lattice points from one row and one column before the cell, so columns run off its edge.
			for ( var r = 0; r < rows; r++ )
			for ( var q = 0; q < columns; q++ )
			{
				var k = r * columns + q;
				var h = Hash( r, q, Seed + c * 13 );
				pointX[k] = (q - 1 + (r % 2 == 0 ? 0f : 0.5f) + (Bits( h, 0 ) - 0.5f) * 0.55f) * ColumnSpacing + (c * 37 % 60);
				pointY[k] = (r - 1 + (Bits( h, 8 ) - 0.5f) * 0.5f) * rowSpacing + (c * 53 % 50);
				lift[k] = (Bits( h, 16 ) - 0.5f) * 0.24f;
				dish[k] = (Bits( h, 24 ) - 0.45f) * 0.3f;

				var tone = Bits( h, 4 ) * 2f - 1f;
				var color = tone > 0f ? Mix( Basalt, BasaltLight, tone * 0.5f ) : Mix( Basalt, BasaltShade, -tone * 0.55f );
				palette[1 + c * 64 + k] = Mix( color, BasaltBrown, 0.15f + Bits( h, 12 ) * 0.6f );
			}

			var footInset = 6f + (float)random.NextDouble() * 2f;
			for ( var y = 0; y < cell; y++ )
			{
				var row = ((int)min.y + y) * FloorSize + (int)min.x;
				var py = y + 0.5f;
				var r0 = (int)(py / rowSpacing) + 1;
				for ( var x = 0; x < cell; x++ )
				{
					var i = row + x;
					var px = x + 0.5f;
					var q0 = (int)(px / ColumnSpacing) + 1;

					// Nearest and second-nearest seeds among the 3 x 3 lattice neighbours.
					int first = 0, second = 0;
					float best = float.MaxValue, next = float.MaxValue;
					for ( var r = Math.Max( r0 - 1, 0 ); r <= Math.Min( r0 + 1, rows - 1 ); r++ )
					for ( var q = Math.Max( q0 - 1, 0 ); q <= Math.Min( q0 + 1, columns - 1 ); q++ )
					{
						var k = r * columns + q;
						var dx = px - pointX[k]; var dy = py - pointY[k];
						var dd = dx * dx + dy * dy;
						if ( dd < best ) { next = best; second = first; best = dd; first = k; }
						else if ( dd < next ) { next = dd; second = k; }
					}

					// Distance to the bisector between the two: exact for the shared edge.
					var sx = pointX[second] - pointX[first]; var sy = pointY[second] - pointY[first];
					var split = (next - best) / (2f * MathF.Sqrt( sx * sx + sy * sy ));
					var chip = chips[i];
					var halfJoint = 1f + chip * 2.2f;
					var foot = MathF.Min( MathF.Min( px, cell - px ), MathF.Min( py, cell - py ) ) - footInset - (chip - 0.5f) * 3f;
					var edge = MathF.Min( split - halfJoint, foot );

					// Hot joints: about one pair in six, and only where the glow noise lets it show.
					var hot = 0f;
					if ( split < 10f && foot > 4f && (Hash( Math.Min( first, second ), Math.Max( first, second ), Seed + c * 17 ) & 0xFF) < 44 )
						hot = MathF.Min( MathF.Max( (glow[i] - 0.42f) * 5f, 0f ), 1f );
					joint[i] = 0f; ember[i] = 0f;

					if ( edge <= 0f )
					{
						joint[i] = 1f;
						relief[i] = 0.35f;
						ember[i] = split < 1.1f ? hot * 0.6f : split < 2f ? hot * 0.6f * (2f - split) / 0.9f * 0.5f : 0f;
						continue;
					}

					// A weathered shoulder, then the column's own top: its height, dished or domed.
					var t = edge >= 7f ? 1f : edge / 7f;
					var shoulder = t * t * (3f - 2f * t);
					var toCentre = MathF.Min( MathF.Sqrt( best ) / (ColumnSpacing * 0.5f), 1f );
					relief[i] = 0.4f + shoulder * (0.6f + lift[first] - dish[first] * (1f - toCentre * toCentre));
					owners[i] = (byte)(1 + c * 64 + first);
					if ( split < 10f ) scorch[i] = hot * (1f - split / 10f) * 0.5f;
				}
			}
		}

		painter.AddHeight( relief, 1f );
		painter.Blend( owners, palette );
		painter.Blend( joint, Fissure );
		painter.Blend( scorch, Scorch );
		painter.Blend( ember, Palette.Lava, roughness: 0.9f );
	}

	/// <summary>
	/// Rough-hewn rock seen from below, dusted pale with ash (the forge's low lamps barely reach it, and
	/// bare basalt overhead read black): each underside a slab with a broken outline, its face broken
	/// into chipped facets (a jittered Voronoi, every facet a tilted plane with its own tone, so the
	/// rock reads faceted even in flat light), a few long tapered fissures cut mostly in height, soot,
	/// and a handful of ember glints lodged in the fissures — tiny, soft and few.
	/// </summary>
	protected override PaintedSurface PaintCeiling()
	{
		const int cell = CeilingSize / 2;
		var painter = new Painter( CeilingSize, Soot, 0.9f );
		var random = new Random( Seed + 500 );
		float Next() => (float)random.NextDouble();

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );
			var tone = Next() * 2f - 1f;
			var rock = tone < 0f ? Mix( Rock, RockDark, -tone * 0.5f ) : Mix( Rock, RockLight, tone * 0.5f );
			rock = Mix( rock, Ash, 0.3f );
			painter.Fill( (int)min.x, (int)min.y, cell, cell, Mix( rock, Soot, 0.3f ), height: 0.4f );
			painter.Polygon( SlabOutline( min, cell, 3f, 4f, 10f, Seed + 500 + c * 101 ), rock, height: 1f, bevel: 14f, roughness: 0.88f );
		}

		PaintFacets( painter, 32, Seed + 540, 0.35f, 0.5f );

		// Fissures: one or two long cracks per underside with a shorter branch, cut mostly in height,
		// with an ember caught here and there in the widest part.
		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );
			var fissures = 1 + random.Next( 2 );
			for ( var f = 0; f < fissures; f++ )
			{
				var start = min + new Vector2( 40f + Next() * (cell - 80f), 40f + Next() * (cell - 80f) );
				var crack = Inside( Wander( start, Next() * MathF.Tau, 12f, 12 + random.Next( 8 ), 0.35f, random ), min, cell );
				var width = 3.2f + Next() * 1.6f;
				PaintFissure( painter, crack, width, random );
				if ( crack.Length > 4 )
				{
					var branch = Inside( Wander( crack[crack.Length / 3], Next() * MathF.Tau, 9f, 4 + random.Next( 4 ), 0.45f, random ), min, cell );
					PaintFissure( painter, branch, width * 0.55f, random );
				}

				for ( var g = 0; g < 2 && crack.Length > 4; g++ )
				{
					var at = crack[crack.Length / 4 + random.Next( crack.Length / 2 )];
					painter.Circle( at.x, at.y, 2.6f + Next() * 1.5f, Scorch, 0.25f );
					painter.Circle( at.x, at.y, 0.7f + Next() * 0.6f, Palette.Lava, 0.6f, roughness: 0.9f );
				}
			}
		}

		// Soot in drifts, lighter than it was under ambient light alone: overhead, where the forge's low
		// lamps reach least, the full drifts turned the rock black.
		painter.Blend( Ramp( painter.Noise( 4, Seed + 511, 3 ), 0.55f, 0.85f, 0.3f ), Soot );
		painter.HeightNoise( 0.5f, 8, Seed + 512, 3 );
		painter.Mottle( 0.08f, 6, Seed + 513, 3 );
		painter.Cavity( 0.5f, 4 );
		painter.Grain( 0.015f, Seed + 514 );
		return painter.Bake( "arena_7_ceiling", Painter.Matte, 5f );
	}

	/// <summary>
	/// A fissure as relief, not ink: along the polyline its width swells and tapers to nothing at both
	/// ends, each segment a little wider or narrower than the last. A slightly raised, faintly lighter
	/// lip either side catches the light; the cut itself drops deep, and its colour is only a soft
	/// darkening — the cavity bake and the normal map do the rest.
	/// </summary>
	static void PaintFissure( Painter painter, Vector2[] points, float width, Random random )
	{
		for ( var s = 1; s < points.Length; s++ )
		{
			var t = (s - 0.5f) / (points.Length - 1);
			var w = width * (0.12f + 0.88f * MathF.Sin( t * MathF.PI )) * (0.75f + (float)random.NextDouble() * 0.5f);
			painter.Line( points[s - 1], points[s], w + 3f, RockLight, 0.08f, height: 1.08f );
		}
		for ( var s = 1; s < points.Length; s++ )
		{
			var t = (s - 0.5f) / (points.Length - 1);
			var w = width * (0.12f + 0.88f * MathF.Sin( t * MathF.PI ));
			painter.Line( points[s - 1], points[s], w, Mix( Rock, Soot, 0.4f ), 0.22f, height: 0.5f );
			painter.Line( points[s - 1], points[s], w * 0.4f, Soot, 0.2f, height: 0.25f );
		}
	}

	/// <summary>A polyline cut off where it first leaves its underside (inset 14 px), so a crack never runs onto a neighbouring cell.</summary>
	static Vector2[] Inside( Vector2[] points, Vector2 min, float size )
	{
		var count = 1;
		while ( count < points.Length )
		{
			var p = points[count];
			if ( p.x < min.x + 14f || p.y < min.y + 14f || p.x > min.x + size - 14f || p.y > min.y + size - 14f ) break;
			count++;
		}
		return points[..count];
	}

	/// <summary>
	/// Chipped facets over a whole surface: a jittered grid of points <paramref name="facet"/> px apart,
	/// each pixel belonging to the nearest; a facet is a plane tilted its own way (height, scaled by
	/// <paramref name="relief"/>, and the same slope as a light-to-dark gradient in colour) with its own
	/// tone. Near the border with the next-nearest facet both planes blend to their mean, and the border
	/// sinks into a slight crease: two planes meeting in a hard height step made a cliff one pixel wide
	/// in the normal map, which shimmered as a saw-tooth along every border under direct light.
	/// </summary>
	void PaintFacets( Painter painter, int facet, int seed, float relief, float tone )
	{
		var size = painter.Size;
		var grid = size / facet + 2;
		var pointX = new float[grid * grid];
		var pointY = new float[grid * grid];
		var slopeX = new float[grid * grid];
		var slopeY = new float[grid * grid];
		var tones = new float[grid * grid];
		var cells = size / facet;
		for ( var gy = 0; gy < grid; gy++ )
		for ( var gx = 0; gx < grid; gx++ )
		{
			// Points one cell beyond each edge repeat the cells on the far side, so facets tile.
			var k = gy * grid + gx;
			var h = Hash( (gx + cells - 1) % cells, (gy + cells - 1) % cells, seed );
			pointX[k] = (gx - 1 + 0.1f + Bits( h, 0 ) * 0.8f) * facet;
			pointY[k] = (gy - 1 + 0.1f + Bits( h, 8 ) * 0.8f) * facet;
			var angle = Bits( h, 16 ) * MathF.Tau;
			slopeX[k] = MathF.Cos( angle ) / facet;
			slopeY[k] = MathF.Sin( angle ) / facet;
			tones[k] = Bits( h, 24 ) - 0.5f;
		}

		var height = new float[size * size];
		var light = new float[size * size];
		var dark = new float[size * size];
		var blend = facet * 0.18f;
		for ( var y = 0; y < size; y++ )
		{
			var gy = y / facet + 1;
			for ( var x = 0; x < size; x++ )
			{
				var gx = x / facet + 1;
				var px = x + 0.5f; var py = y + 0.5f;
				float best = float.MaxValue, next = float.MaxValue;
				int first = 0, second = 0;
				for ( var oy = gy - 1; oy <= gy + 1; oy++ )
				for ( var ox = gx - 1; ox <= gx + 1; ox++ )
				{
					var k = oy * grid + ox;
					var dx = px - pointX[k]; var dy = py - pointY[k];
					var dd = dx * dx + dy * dy;
					if ( dd < best ) { next = best; second = first; best = dd; first = k; }
					else if ( dd < next ) { next = dd; second = k; }
				}

				// Distance to the border with the second facet, and each facet's plane at this pixel.
				var sx = pointX[second] - pointX[first]; var sy = pointY[second] - pointY[first];
				var split = (next - best) / (2f * MathF.Sqrt( sx * sx + sy * sy ));
				var own = (px - pointX[first]) * slopeX[first] + (py - pointY[first]) * slopeY[first];
				var other = (px - pointX[second]) * slopeX[second] + (py - pointY[second]) * slopeY[second];
				var w = split >= blend ? 1f : split / blend;
				w = w * w * (3f - 2f * w);
				var tilt = (own + other) * 0.5f + (own - other) * 0.5f * w;
				var shade = (tones[first] + tones[second]) * 0.5f + (tones[first] - tones[second]) * 0.5f * w;

				var i = y * size + x;
				height[i] = tilt * relief - (1f - w) * 0.06f;
				var t = shade * 0.8f + tilt * 0.45f * tone / 0.5f;
				if ( t > 0f ) light[i] = MathF.Min( t, 1f ) * 0.6f; else dark[i] = MathF.Min( -t, 1f ) * 0.6f;
			}
		}

		painter.AddHeight( height, 1f );
		painter.Blend( light, RockLight );
		painter.Blend( dark, RockDark );
	}
}
