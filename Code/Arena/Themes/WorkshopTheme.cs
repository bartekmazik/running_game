using System;

/// <summary>
/// Floor 6 (index 5): a workshop. Underfoot, steel checker plate — lens-shaped tread bosses in
/// alternating directions, their crowns polished bright by boots, grime packed around them, brass
/// rivets at the corners (one gone, some dull), scuffs, mill-scale patches and an old oil stain.
/// Overhead (the undersides of the library floor), galvanised sheet: zinc spangle, a cross-break
/// crease, a hemmed edge with screws, white rust blooming here and there.
///
/// Steel is painted on the matte base with a low roughness: <see cref="Painter.Metal"/> renders
/// black until the rooms have something to reflect (controller ruling R17, revisited in Task 6).
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

	static Color Zinc => Palette.Hex( 0x8E959C );
	static Color ZincLight => Palette.Hex( 0xA9AFB5 );
	static Color ZincDark => Palette.Hex( 0x777E86 );

	/// <summary>White rust: the chalky bloom zinc grows where it stays damp.</summary>
	static Color WhiteRust => Palette.Hex( 0xC9CCC8 );

	/// <summary>Tread bosses per cell side (the lattice), and each boss's half-length and half-width in pixels.</summary>
	const int Lattice = 6;
	const float BossHalfLength = 27f;
	const float BossHalfWidth = 7f;

	/// <summary>Structural front layer only (pipes, gauges, cage lamps and lights come in Tasks 6b and 9).</summary>
	public override void Dress( FloorContext room )
	{
		BuildFrontLayer( room, new FrontStyle { Post = 24f, Pier = 64f, Corner = 28f, Cornice = 24f, Skirting = 10f, SkirtingDepth = 4f } );
	}

	/// <summary>
	/// Plain corrugated sheet: galvanised zinc with spangle, a lapped seam riveted at uneven spacing,
	/// grime running down the troughs, white rust here and there.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		// Full surface in Task 6b.
		const int Pitch = 32;
		var painter = new Painter( WallSize, Zinc, 0.45f );
		var random = new Random( Seed + 600 );
		var relief = new float[WallSize * WallSize];
		for ( var y = 0; y < WallSize; y++ )
		for ( var x = 0; x < WallSize; x++ )
			relief[y * WallSize + x] = 0.5f + 0.5f * MathF.Cos( x * MathF.Tau / Pitch );
		painter.AddHeight( relief, 1f );

		painter.Mottle( 0.05f, 64, Seed + 601, 2 );
		painter.Blend( Ramp( painter.Noise( 5, Seed + 602, 4 ), 0.6f, 0.85f, 0.5f ), ZincLight );

		var streaks = painter.Noise( 64, Seed + 603, 2 );
		var grime = new float[WallSize * WallSize];
		for ( var y = 0; y < WallSize; y++ )
		for ( var x = 0; x < WallSize; x++ )
		{
			var s = Streak( streaks, WallSize, x, y / 40f );
			var trough = 1f - relief[y * WallSize + x];
			grime[y * WallSize + x] = s > 0.55f ? MathF.Min( (s - 0.55f) * 3f, 1f ) * trough * 0.35f : 0f;
		}
		painter.Blend( grime, Grime );

		var seam = WallSize / 2 + 20;
		painter.Rect( 0f, seam, WallSize, 6f, ZincDark, height: 1.3f, bevel: 2f, roughness: 0.5f );
		for ( var x = random.Next( 40 ); x < WallSize - 20; x += 120 + random.Next( 60 ) )
			painter.Circle( x + 0.5f, seam + 12f, 4f, ZincLight, height: 1.6f, bevel: 3f, roughness: 0.3f );

		painter.Blend( Ramp( painter.Noise( 7, Seed + 604, 4 ), 0.72f, 0.9f, 0.25f ), WhiteRust, roughness: 0.85f );
		painter.ShadeFromHeight( 0.12f, 0.18f );
		painter.Cavity( 0.3f, 6 );
		painter.Grain( 0.01f, Seed + 605 );
		return painter.Bake( "arena_5_wall", Painter.Matte, 4f );
	}

	/// <summary>Plain painted steel: posts and beams with rivets along the post edges.</summary>
	protected override PaintedSurface PaintTrim()
	{
		// Full surface in Task 6b.
		return PaintTrimAtlas( "arena_5_trim", SteelShade, Grime, 0.5f, 0f, Seed + 700, painter =>
		{
			var (top, height) = TrimRows( TrimBand.Pilaster );
			for ( var x = 12; x < TrimSize; x += 42 )
			{
				painter.Circle( x, top + height * 0.16f, 3.2f, Steel, height: 1.25f, bevel: 2.5f, roughness: 0.35f );
				painter.Circle( x + 21, top + height * 0.84f, 3.2f, Steel, height: 1.25f, bevel: 2.5f, roughness: 0.35f );
			}
		} );
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
