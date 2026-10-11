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

	/// <summary>Column spacing in pixels: about 17 units, a 40 cm column.</summary>
	const float ColumnSpacing = 150f;

	/// <summary>Structural front layer only (forge mouths, lava seams and lights come in Tasks 6b and 10).</summary>
	public override void Dress( FloorContext room )
	{
		BuildFrontLayer( room, new FrontStyle { Post = 36f, Pier = 80f, Corner = 38f, Cornice = 30f, Skirting = 12f, SkirtingDepth = 6f } );
	}

	/// <summary>
	/// Plain dark rock: broad lumps and hollows, faint strata, browner and lighter patches. Also the
	/// face of the lava shaft below the floor.
	/// </summary>
	protected override PaintedSurface PaintWall()
	{
		// Full surface in Task 6b.
		var painter = new Painter( WallSize, RockDark, 0.9f );

		// A jumble of broken faces, each its own tone and set at its own depth, so the rock reads as
		// rock even where only ambient light reaches it.
		var random = new Random( Seed + 610 );
		float Next() => (float)random.NextDouble();
		var tones = new[] { Rock, RockLight, RockDark, Basalt, Mix( Rock, BasaltBrown, 0.4f ) };
		for ( var f = 0; f < 260; f++ )
		{
			var centre = new Vector2( Next() * WallSize, Next() * WallSize );
			var half = new Vector2( 30f + Next() * 70f, 24f + Next() * 50f );
			var face = Mix( Mix( tones[random.Next( tones.Length )], tones[random.Next( tones.Length )], Next() ), Rock, 0.35f );
			painter.Polygon( Painter.Blob( centre, half, 5 + random.Next( 4 ), 1f, random.Next() ), face, height: 0.5f + Next() * 0.7f, bevel: 5f + Next() * 10f, roughness: 0.85f + Next() * 0.1f );
		}
		painter.Blend( Ramp( painter.Noise( 4, Seed + 600, 4 ), 0.5f, 0.8f, 0.3f ), RockLight );
		painter.Blend( Ramp( painter.Noise( 6, Seed + 601, 4 ), 0.55f, 0.85f, 0.3f ), RockDark );
		painter.Blend( Ramp( painter.Noise( 3, Seed + 602, 3 ), 0.6f, 0.9f, 0.3f ), BasaltBrown );

		var strata = painter.Noise( 4, Seed + 603, 2 );
		var bands = new float[WallSize * WallSize];
		for ( var y = 0; y < WallSize; y++ )
		for ( var x = 0; x < WallSize; x++ )
			bands[y * WallSize + x] = StreakX( strata, WallSize, x, y, 3 ) - 0.5f;
		painter.AddHeight( bands, 0.35f );

		painter.HeightNoise( 1.6f, 4, Seed + 604, 5 );
		painter.HeightNoise( 0.6f, 24, Seed + 605, 3 );
		painter.HeightNoise( 0.25f, 96, Seed + 607, 2 );
		painter.ShadeFromHeight( 0.35f, 0.4f );

		// Until the forge has lights of its own (6b) it is lit by ambient alone, where only colour reads:
		// grit and small facets in the colour as well as the relief.
		painter.Mottle( 0.12f, 32, Seed + 608, 3 );
		painter.Mottle( 0.08f, 128, Seed + 609, 2 );
		painter.Cavity( 0.6f, 4 );
		painter.Grain( 0.015f, Seed + 606 );
		return painter.Bake( "arena_7_wall", Painter.Matte, 9f );
	}

	/// <summary>Plain dark basalt blocks.</summary>
	protected override PaintedSurface PaintTrim()
	{
		// Full surface in Task 6b.
		return PaintTrimAtlas( "arena_7_trim", Basalt, Mix( BasaltShade, Soot, 0.5f ), 0.85f, 0f, Seed + 700 );
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
	/// Rough-hewn rock seen from below: each underside a slab with a broken outline, its face broken
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
			painter.Fill( (int)min.x, (int)min.y, cell, cell, Mix( rock, Soot, 0.5f ), height: 0.4f );
			painter.Polygon( SlabOutline( min, cell, 3f, 4f, 10f, Seed + 500 + c * 101 ), rock, height: 1f, bevel: 14f, roughness: 0.88f );
		}

		PaintFacets( painter );

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

		painter.Blend( Ramp( painter.Noise( 4, Seed + 511, 3 ), 0.55f, 0.85f, 0.5f ), Soot );
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
	/// Chipped facets over the whole ceiling: a jittered grid of points, each pixel belonging to the
	/// nearest; a facet is a plane tilted its own way (height for the normal map, and the same slope
	/// as a light-to-dark gradient in colour) with its own tone.
	/// </summary>
	void PaintFacets( Painter painter )
	{
		const int Facet = 34;
		const int grid = CeilingSize / Facet + 2;
		var pointX = new float[grid * grid];
		var pointY = new float[grid * grid];
		var slopeX = new float[grid * grid];
		var slopeY = new float[grid * grid];
		var tones = new float[grid * grid];
		for ( var gy = 0; gy < grid; gy++ )
		for ( var gx = 0; gx < grid; gx++ )
		{
			var k = gy * grid + gx;
			var h = Hash( gx, gy, Seed + 540 );
			pointX[k] = (gx + 0.1f + Bits( h, 0 ) * 0.8f) * Facet;
			pointY[k] = (gy + 0.1f + Bits( h, 8 ) * 0.8f) * Facet;
			var angle = Bits( h, 16 ) * MathF.Tau;
			slopeX[k] = MathF.Cos( angle ) / Facet;
			slopeY[k] = MathF.Sin( angle ) / Facet;
			tones[k] = Bits( h, 24 ) - 0.5f;
		}

		var relief = new float[CeilingSize * CeilingSize];
		var light = new float[CeilingSize * CeilingSize];
		var dark = new float[CeilingSize * CeilingSize];
		for ( var y = 0; y < CeilingSize; y++ )
		{
			var gy = y / Facet;
			for ( var x = 0; x < CeilingSize; x++ )
			{
				var gx = x / Facet;
				var px = x + 0.5f; var py = y + 0.5f;
				var best = float.MaxValue; var owner = 0;
				for ( var oy = Math.Max( gy - 1, 0 ); oy <= Math.Min( gy + 1, grid - 1 ); oy++ )
				for ( var ox = Math.Max( gx - 1, 0 ); ox <= Math.Min( gx + 1, grid - 1 ); ox++ )
				{
					var k = oy * grid + ox;
					var dx = px - pointX[k]; var dy = py - pointY[k];
					var dd = dx * dx + dy * dy;
					if ( dd < best ) { best = dd; owner = k; }
				}

				var tilt = (px - pointX[owner]) * slopeX[owner] + (py - pointY[owner]) * slopeY[owner];
				var i = y * CeilingSize + x;
				relief[i] = tilt * 0.35f;
				var tone = tones[owner] * 0.8f + tilt * 0.45f;
				if ( tone > 0f ) light[i] = MathF.Min( tone, 1f ) * 0.6f; else dark[i] = MathF.Min( -tone, 1f ) * 0.6f;
			}
		}

		painter.AddHeight( relief, 1f );
		painter.Blend( light, RockLight );
		painter.Blend( dark, RockDark );
	}
}
