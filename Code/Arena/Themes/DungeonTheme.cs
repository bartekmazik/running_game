using System;

/// <summary>
/// Floor 7 (index 6): a dungeon. Underfoot, granite flagstones — two or three to a block, cut
/// irregular and laid in damp mortar, each stone its own tone and lean (slate, lilac, warm grey),
/// flecked with crystals, its edges worn round and chipped, moss in the joints. Overhead (the
/// undersides of the workshop floor), rough rubble stone, pillowed and pitted, set in deep lime
/// mortar, with damp and soot.
/// </summary>
public sealed class DungeonTheme : ArenaTheme
{
	public override string Name => "Dungeon";
	public override int Floor => 6;
	public override Color LightColor => new( 1.40f, 0.82f, 0.48f );

	static Color Granite => Palette.Hex( 0x8D8798 );
	static Color GraniteLight => Palette.Hex( 0xA49FAE );
	static Color GraniteShade => Palette.Hex( 0x6F6A7A );

	/// <summary>Stones that lean warm grey and stones that lean toward the floor's slate lilac — both close to grey.</summary>
	static Color GraniteWarm => Palette.Hex( 0x958C89 );
	static Color GraniteLilac => Palette.Hex( 0x8C849B );

	/// <summary>Feldspar and quartz (pale) and mica (dark) in the granite.</summary>
	static Color Feldspar => Palette.Hex( 0xBDB7BE );
	static Color Mica => Palette.Hex( 0x45414B );

	/// <summary>
	/// Lime mortar in the joints: warm and sandy against the cool granite, about as light — a filled
	/// joint reads as a band of another material, not as a dark crack.
	/// </summary>
	static Color Mortar => Palette.Hex( 0x8A847E );

	static Color Moss => Palette.Hex( 0x7FA06A );

	/// <summary>Damp patches on the stone.</summary>
	static Color Damp => Palette.Hex( 0x6D6874 );

	static Color Rubble => Palette.Hex( 0x5E5866 );
	static Color RubbleLight => Palette.Hex( 0x77707C );
	static Color RubbleDark => Palette.Hex( 0x46414C );
	static Color RubbleWarm => Palette.Hex( 0x665C5C );
	static Color Lime => Palette.Hex( 0x75707A );
	static Color Soot => Palette.Hex( 0x2E2B33 );

	/// <summary>Structural front layer only (arches, torches and lights come in Tasks 6b and 10).</summary>
	public override void Dress( FloorContext room )
	{
		BuildFrontLayer( room, new FrontStyle { Post = 34f, Pier = 80f, Corner = 36f, Cornice = 28f, Skirting = 12f, SkirtingDepth = 6f } );
	}

	/// <summary>
	/// Plain ashlar: courses of different heights, blocks of different lengths and tones (slate, lilac,
	/// warm grey), pillowed and pitted, set in deep mortar, damp toward the floor.
	/// </summary>
	protected override PaintedSurface PaintWall()
	{
		// Full surface in Task 6b.
		const float Ppu = WallSize / WallRepeat;
		var painter = new Painter( WallSize, Mix( Mortar, Soot, 0.35f ), 0.95f );
		var random = new Random( Seed + 600 );
		float Next() => (float)random.NextDouble();
		var tones = new[] { Granite, GraniteLight, GraniteShade, GraniteWarm, GraniteLilac };

		var y = 0f;
		while ( y < WallSize - 1f )
		{
			var course = (26f + Next() * 14f) * Ppu;
			if ( WallSize - (y + course) < 22f * Ppu ) course = WallSize - y;
			var x = Next() * WallSize;
			var end = x + WallSize;
			while ( x < end - 1f )
			{
				var length = (36f + Next() * 54f) * Ppu;
				if ( end - (x + length) < 30f * Ppu ) length = end - x;
				var joint = 6f + Next() * 6f;
				var stone = Mix( tones[random.Next( tones.Length )], tones[random.Next( tones.Length )], Next() );
				painter.Rect( x + joint * 0.5f, y + joint * 0.5f, length - joint, course - joint, stone, radius: 4f + Next() * 6f, height: 1f,
					bevel: 10f + Next() * 6f, roughness: 0.88f, angle: (Next() - 0.5f) * 0.8f );
				x += length;
			}
			y += course;
		}

		var damp = Ramp( painter.Noise( 6, Seed + 601, 4 ), 0.4f, 0.8f, 0.5f );
		for ( var py = 0; py < WallSize; py++ )
		{
			var low = Math.Clamp( (py / (float)WallSize - 0.6f) * 2.5f, 0f, 1f );
			for ( var px = 0; px < WallSize; px++ ) damp[py * WallSize + px] *= low;
		}
		painter.Blend( damp, Damp, roughness: 0.7f );

		painter.HeightNoise( 0.5f, 16, Seed + 602, 3 );
		painter.Mottle( 0.08f, 8, Seed + 603, 4 );
		painter.Cavity( 0.6f, 6 );
		painter.EdgeWear( 0.3f, 4, Seed + 604 );
		painter.Grain( 0.015f, Seed + 605 );
		return painter.Bake( "arena_6_wall", Painter.Matte, 6f );
	}

	/// <summary>Plain dressed granite.</summary>
	protected override PaintedSurface PaintTrim()
	{
		// Full surface in Task 6b.
		return PaintTrimAtlas( "arena_6_trim", Mix( Granite, GraniteShade, 0.3f ), Rubble, 0.85f, 0f, Seed + 700 );
	}

	protected override PaintedSurface PaintFloor()
	{
		var painter = new Painter( FloorSize, Mortar, 0.5f );
		var random = new Random( Seed );

		PaintFlagstones( painter, random );

		// Damp patches, drawn over stone and mortar alike.
		painter.Blend( Ramp( painter.Noise( 3, Seed + 30, 2 ), 0.56f, 0.82f, 0.4f ), Damp, roughness: 0.55f );

		painter.HeightNoise( 0.5f, 8, Seed + 31, 2 );
		painter.Mottle( 0.07f, 4, Seed + 32, 3 );
		painter.RoughnessNoise( 0.08f, 6, Seed + 33, 2 );
		painter.Cavity( 0.5f, 5 );
		painter.EdgeWear( 0.2f, 4, Seed + 34 );
		painter.Grain( 0.015f, Seed + 35 );
		return painter.Bake( "arena_6_floor", Painter.Matte, 7f );
	}

	/// <summary>
	/// The flagstones, per pixel. Each block is laid as two or three stones around seed points (a
	/// Voronoi cell per stone), read through a noise warp so the cut lines bend a little. The distance
	/// to the nearest split (exact, from the bisectors) or to the block's foot gives each stone a worn,
	/// rounded arris and the mortar joint its width: 4–7 px between stones, about the block joint's,
	/// varying slowly along its length. The block's foot stays clean — a straight cut, nicked only in a
	/// few places by larger, slowly varying chips — because a fine modulation there foreshortens into
	/// stitching along the chamfer. Stones sit clearly proud or sunk of each other, and each has its
	/// own colour from a small palette (one blend pass for all of them).
	/// </summary>
	void PaintFlagstones( Painter painter, Random random )
	{
		const int cell = FloorSize / 2;
		const int mask = FloorSize * FloorSize - 1;
		float Next() => (float)random.NextDouble();

		// One warp field read at two offsets serves both axes; the offset is far enough apart that the
		// two reads are unrelated.
		var warp = painter.Noise( 14, Seed + 20, 2 );
		var width = painter.Noise( 10, Seed + 22, 1 );
		var nicks = painter.Noise( 6, Seed + 25, 1 );
		var crystals = painter.Noise( FloorSize / 2, Seed + 23, 1 );
		var mossNoise = painter.Noise( 20, Seed + 24, 2 );

		// Outputs reuse the noise fields where a pixel's noise is read only at that pixel: crystals
		// become the pale-fleck weight, width the joint weight, mossNoise the moss weight.
		var relief = new float[FloorSize * FloorSize];
		var owners = new byte[FloorSize * FloorSize];
		var dark = new float[FloorSize * FloorSize];
		var pale = crystals; var joint = width; var moss = mossNoise;
		var palette = new Color[13];

		// Per block: the stone seeds, and for every ordered pair the bisector as a unit normal and an
		// offset, so the per-pixel distance to a split is one dot product.
		var seedX = new float[3]; var seedY = new float[3]; var lift = new float[3];
		var normalX = new float[9]; var normalY = new float[9]; var offset = new float[9];

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var count = c == 0 || c == 3 ? 2 : 3;
			var turn = Next() * MathF.Tau;
			var centreX = cell * 0.5f + (Next() - 0.5f) * 60f;
			var centreY = cell * 0.5f + (Next() - 0.5f) * 60f;
			for ( var k = 0; k < count; k++ )
			{
				var angle = turn + k * MathF.Tau / count + (Next() - 0.5f) * 0.5f;
				var radius = cell * 0.26f * (0.85f + Next() * 0.3f);
				seedX[k] = centreX + MathF.Cos( angle ) * radius;
				seedY[k] = centreY + MathF.Sin( angle ) * radius;
				lift[k] = (Next() - 0.5f) * 0.3f;

				var tone = Next() * 2f - 1f;
				var lean = Next() * 2f - 1f;
				var color = tone > 0f ? Mix( Granite, GraniteLight, tone * 0.45f ) : Mix( Granite, GraniteShade, -tone * 0.5f );
				palette[1 + c * 3 + k] = lean > 0f ? Mix( color, GraniteLilac, lean * 0.5f ) : Mix( color, GraniteWarm, -lean * 0.5f );
			}
			for ( var a = 0; a < count; a++ )
			for ( var b = 0; b < count; b++ )
			{
				if ( a == b ) continue;
				var nx = seedX[b] - seedX[a]; var ny = seedY[b] - seedY[a];
				var length = MathF.Sqrt( nx * nx + ny * ny );
				nx /= length; ny /= length;
				normalX[a * 3 + b] = nx; normalY[a * 3 + b] = ny;
				offset[a * 3 + b] = (seedX[a] + seedX[b]) * 0.5f * nx + (seedY[a] + seedY[b]) * 0.5f * ny;
			}
			var footInset = 6f + Next() * 2f;

			for ( var y = 0; y < cell; y++ )
			{
				var row = ((int)min.y + y) * FloorSize + (int)min.x;
				for ( var x = 0; x < cell; x++ )
				{
					var i = row + x;
					var px = x + 0.5f + (warp[i] - 0.5f) * 18f;
					var py = y + 0.5f + (warp[(i + 300 * FloorSize + 300) & mask] - 0.5f) * 18f;

					// Nearest seed, then the distance to its region's edge: the nearest bisector.
					var owner = 0; var best = float.MaxValue;
					for ( var k = 0; k < count; k++ )
					{
						var dx = px - seedX[k]; var dy = py - seedY[k];
						var dd = dx * dx + dy * dy;
						if ( dd < best ) { best = dd; owner = k; }
					}
					var split = float.MaxValue;
					for ( var k = 0; k < count; k++ )
					{
						if ( k == owner ) continue;
						var pair = owner * 3 + k;
						var e = offset[pair] - px * normalX[pair] - py * normalY[pair];
						if ( e < split ) split = e;
					}

					// Mortar between stones 4–7 px either side of the split; the block's own edge (the
					// chamfer's foot) is unwarped so neighbouring tiles still meet cleanly.
					var halfJoint = 4f + width[i] * 3f;
					var nick = nicks[i] > 0.68f ? (nicks[i] - 0.68f) * 45f : 0f;
					var mossy = mossNoise[i];
					var crystal = crystals[i];
					joint[i] = 0f; moss[i] = 0f; pale[i] = 0f;
					var toEdge = MathF.Min( MathF.Min( x + 0.5f, cell - x - 0.5f ), MathF.Min( y + 0.5f, cell - y - 0.5f ) );
					var foot = toEdge - footInset - nick;
					var edge = MathF.Min( split - halfJoint, foot );

					if ( edge <= 0f )
					{
						joint[i] = 0.9f;
						relief[i] = 0.3f;
						if ( crystal > 0.6f ) pale[i] = MathF.Min( (crystal - 0.6f) * 4f, 1f ) * 0.3f;
						var m = (mossy - 0.55f) * 4f;
						moss[i] = m > 0f ? MathF.Min( m, 1f ) * 0.4f : 0f;
						continue;
					}

					// A worn, rounded arris about twelve pixels wide, then the stone's own level.
					var t = edge >= 12f ? 1f : edge / 12f;
					var shoulder = t * t * (3f - 2f * t);
					relief[i] = 0.3f + shoulder * (0.7f + lift[owner]);
					owners[i] = (byte)(1 + c * 3 + owner);
					if ( edge < 3f )
					{
						var m = (mossy - 0.6f) * 4f;
						moss[i] = m > 0f ? MathF.Min( m, 1f ) * (1f - edge / 3f) * 0.4f : 0f;
					}

					// Crystals: sparse pale feldspar and dark mica, low contrast — granite, not asphalt. They
					// fade out over the chamfer: seen nearly face-on there, specks along every tile edge
					// would line up into a dotted seam.
					var fade = MathF.Min( MathF.Max( (toEdge - 10f) / 24f, 0f ), 1f );
					if ( crystal > 0.7f ) pale[i] = MathF.Min( (crystal - 0.7f) * 6f, 1f ) * 0.22f * fade;
					else if ( crystal < 0.29f ) dark[i] = MathF.Min( (0.29f - crystal) * 6f, 1f ) * 0.28f * fade;
				}
			}
		}

		painter.AddHeight( relief, 1f );
		painter.Blend( owners, palette );
		painter.Blend( pale, Feldspar );
		painter.Blend( dark, Mica );
		painter.Blend( joint, Mortar, roughness: 0.5f );
		painter.Blend( moss, Mix( Mix( Moss, Granite, 0.45f ), Mortar, 0.25f ), roughness: 0.9f );
	}

	/// <summary>
	/// Rubble masonry seen from below: five or six rough stones per underside around jittered points,
	/// each pillowed (rounded toward the room), pitted, its own tone, set in deep lime mortar whose
	/// width wanders; soot drifts across the lot.
	/// </summary>
	protected override PaintedSurface PaintCeiling()
	{
		const int cell = CeilingSize / 2;
		const int Stones = 6;
		var painter = new Painter( CeilingSize, Rubble, 0.9f );
		var random = new Random( Seed + 500 );
		float Next() => (float)random.NextDouble();
		var warpX = painter.Noise( 10, Seed + 510, 2 );
		var warpY = painter.Noise( 10, Seed + 511, 2 );
		var width = painter.Noise( 40, Seed + 512, 1 );
		var relief = new float[CeilingSize * CeilingSize];
		var owners = new byte[CeilingSize * CeilingSize];
		var mortar = width;
		var palette = new Color[1 + 4 * Stones];
		var seedX = new float[Stones]; var seedY = new float[Stones];
		var normalX = new float[Stones * Stones]; var normalY = new float[Stones * Stones]; var offset = new float[Stones * Stones];

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );
			var count = 5 + random.Next( 2 );
			for ( var k = 0; k < count; k++ )
			{
				// Jittered points on a loose 3 x 2 grid, so stones are neither tiny nor huge.
				seedX[k] = (k % 3 + 0.2f + Next() * 0.6f) * cell / 3f;
				seedY[k] = (k / 3 + 0.2f + Next() * 0.6f) * cell / 2f;
				var tone = Next() * 2f - 1f;
				var color = tone > 0f ? Mix( Rubble, RubbleLight, tone * 0.6f ) : Mix( Rubble, RubbleDark, -tone * 0.7f );
				palette[1 + c * Stones + k] = Mix( color, RubbleWarm, Next() * 0.6f );
			}
			for ( var a = 0; a < count; a++ )
			for ( var b = 0; b < count; b++ )
			{
				if ( a == b ) continue;
				var nx = seedX[b] - seedX[a]; var ny = seedY[b] - seedY[a];
				var length = MathF.Sqrt( nx * nx + ny * ny );
				nx /= length; ny /= length;
				normalX[a * Stones + b] = nx; normalY[a * Stones + b] = ny;
				offset[a * Stones + b] = (seedX[a] + seedX[b]) * 0.5f * nx + (seedY[a] + seedY[b]) * 0.5f * ny;
			}

			for ( var y = 0; y < cell; y++ )
			{
				var row = ((int)min.y + y) * CeilingSize + (int)min.x;
				for ( var x = 0; x < cell; x++ )
				{
					var i = row + x;
					var px = x + 0.5f + (warpX[i] - 0.5f) * 22f;
					var py = y + 0.5f + (warpY[i] - 0.5f) * 22f;
					var owner = 0; var best = float.MaxValue;
					for ( var k = 0; k < count; k++ )
					{
						var dx = px - seedX[k]; var dy = py - seedY[k];
						var dd = dx * dx + dy * dy;
						if ( dd < best ) { best = dd; owner = k; }
					}
					var split = float.MaxValue;
					for ( var k = 0; k < count; k++ )
					{
						if ( k == owner ) continue;
						var pair = owner * Stones + k;
						var e = offset[pair] - px * normalX[pair] - py * normalY[pair];
						if ( e < split ) split = e;
					}

					var foot = MathF.Min( MathF.Min( x + 0.5f, cell - x - 0.5f ), MathF.Min( y + 0.5f, cell - y - 0.5f ) ) - 3f;
					var edge = MathF.Min( split - 3f - width[i] * 6f, foot );
					mortar[i] = 0f;
					if ( edge <= 0f )
					{
						mortar[i] = 0.8f;
						relief[i] = 0.1f;
						continue;
					}

					// Pillowed: the stone bulges toward the room, steepest at its edge.
					var t = MathF.Min( edge / 26f, 1f );
					relief[i] = 0.3f + MathF.Sqrt( t ) * 0.7f;
					owners[i] = (byte)(1 + c * Stones + owner);
				}
			}
		}

		painter.AddHeight( relief, 1f );
		painter.Blend( owners, palette );
		painter.Blend( mortar, Lime, roughness: 0.95f );
		painter.Blend( Ramp( painter.Noise( 3, Seed + 520, 3 ), 0.5f, 0.85f, 0.55f ), Soot );
		painter.HeightNoise( 0.8f, 12, Seed + 521, 3 );
		painter.HeightNoise( 0.25f, 64, Seed + 522, 1 );
		painter.Mottle( 0.12f, 8, Seed + 523, 4 );
		painter.Cavity( 0.6f, 4 );
		painter.Grain( 0.015f, Seed + 524 );
		return painter.Bake( "arena_6_ceiling", Painter.Matte, 5f );
	}
}
