using System;
using System.Collections.Generic;

/// <summary>
/// Floor 3 (index 2): a beach hut. Underfoot, blocks of sand with wind ripples — steep on the lee
/// side, darker heavy grains in the troughs — shell grit, a starfish, a scallop. Overhead (the
/// undersides of the winter floor), striped sailcloth. Round the walls, a row of weatherboard huts,
/// one pastel each.
/// </summary>
public sealed class BeachTheme : ArenaTheme
{
	public override string Name => "Beach";
	public override int Floor => 2;
	public override Color LightColor => new( 1.20f, 1.05f, 0.86f );

	static Color Sand => Palette.Hex( 0xF0D9A8 );
	static Color SandLight => Palette.Hex( 0xF8E8C4 );
	static Color SandShade => Palette.Hex( 0xD9BD86 );
	static Color SandDark => Palette.Hex( 0xB89A68 );
	static Color Damp => Palette.Hex( 0xCDB07E );
	static Color Coral => Palette.Hex( 0xE9A39A );
	static Color CoralLight => Palette.Hex( 0xF5C9BC );
	static Color ShellCream => Palette.Hex( 0xF4E2D2 );
	static Color ShellRib => Palette.Hex( 0xD99E8C );

	static Color Canvas => Palette.Hex( 0xF4ECDC );
	static Color Stripe => Palette.Hex( 0xE9A39A );
	static Color Thread => Palette.Hex( 0xB7A68C );

	/// <summary>Beach-hut paint: chalky pastels in the palette's band, and a sun-bleached white.</summary>
	static Color HutWhite => Palette.Hex( 0xF0EBE0 );
	static readonly uint[] HutPastels = { 0xA9D3C4, 0xEBB3A6, 0xF0D696, 0xA3C9D9 };

	/// <summary>Bare timber where the paint has worn through: silver-grey, sea-weathered.</summary>
	static Color Driftwood => Palette.Hex( 0xB9B2A6 );
	static Color NailHead => Palette.Hex( 0x6B6660 );
	static Color Rust => Palette.Hex( 0x9C6B4A );

	/// <summary>Grit on the sand: shell cream, slate, pink shell, driftwood brown, basalt.</summary>
	static readonly uint[] Grit = { 0xEFE3D0, 0xA9A39A, 0xE3B3A3, 0x9C7B5B, 0x5A5550 };

	/// <summary>
	/// The structural front layer: painted posts, a top plate, skirting; and five warm lamps under the
	/// top plate, a high-key summer light. Awnings, surfboards, the life ring and palm fronds come in Task 8.
	/// </summary>
	public override void Dress( FloorContext room )
	{
		BuildFrontLayer( room, new FrontStyle { Post = 24f, Pier = 70f, Corner = 28f, Cornice = TopPlate, Skirting = Skirting, SkirtingDepth = 4f } );
		AddLamps( room, 5, FloorContext.ClearHeight - 30f, -24f, LightColor * 3.6f, Seed + 900 );
	}

	/// <summary>Height of the top plate under the ceiling and of the skirting: between them is the wall a hut shows.</summary>
	const float TopPlate = 24f;
	const float Skirting = 12f;

	/// <summary>One hut per pastel: every length of wall between two posts is a hut of its own (ruling R26).</summary>
	public override int WallCount => HutPastels.Length;

	/// <summary>
	/// Weatherboard has no fixed height, so every hut's laps start at their own and never line up
	/// across a post. The height also decides, per hut, whether its replaced board shows: about half
	/// the huts put it somewhere between the skirting and the top plate, the rest behind one of them
	/// (the texture repeats every <see cref="ArenaTheme.WallRepeat"/>, so the band hidden behind the top
	/// plate runs on into the one behind the skirting). So a replaced board is a one-off of some huts,
	/// not a mark of every hut of a colour (6a re-review, ruling R28).
	/// </summary>
	protected override float WallLift( int wall, Random random )
	{
		const float Ppu = WallSize / WallRepeat;
		var (tops, replaced) = Boards( wall );
		var top = tops[replaced] / Ppu;
		var foot = tops[replaced + 1] / Ppu;
		var height = foot - top;

		var shows = random.NextDouble() < 0.5;
		var (low, high) = shows
			? (FloorContext.LedgeTop + Skirting + 2f, WallRepeat - TopPlate - 2f)
			: (WallRepeat - TopPlate + 1f, WallRepeat + FloorContext.LedgeTop + Skirting - 1f);
		var bottom = low + (float)random.NextDouble() * (high - height - low);

		// Texture rows run down from the top: a row at t units lands at v = WallRepeat + lift − t.
		return ((bottom - WallRepeat + foot) % WallRepeat + WallRepeat) % WallRepeat;
	}

	/// <summary>
	/// One hut's boards: the pixel row each starts at (plus the texture's foot) and which one was
	/// replaced. Its own stream, so the painter and <see cref="WallLift"/> agree on it.
	/// </summary>
	(List<float> Tops, int Replaced) Boards( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		var random = new Random( Seed + 600 + variant * 31 );
		var tops = new List<float>();
		var y = 0f;
		while ( y < WallSize - 1f )
		{
			tops.Add( y );
			var board = (16f + (float)random.NextDouble() * 4f) * Ppu;
			if ( WallSize - (y + board) < 15f * Ppu ) board = WallSize - y;
			y += board;
		}
		tops.Add( WallSize );
		return (tops, 1 + random.Next( tops.Count - 3 ));
	}

	/// <summary>
	/// One beach hut's weatherboard (ruling R26): lapped planks of uneven width running the whole
	/// length, all in the hut's one pastel (<paramref name="variant"/> picks it), each board only a
	/// shade off the next. Each board is butt-jointed once, anywhere along it, without changing colour.
	/// One board was replaced and painted later in another pastel, faded toward the hut's own; whether
	/// a hut shows it is up to its height (<see cref="WallLift"/>). The paint is weathered: thinned
	/// along the grain so the timber shows through, flaking to silver-grey driftwood along the laps and
	/// at the joints, salt-bleached in broad patches; a nail at each stud, rusting. Each hut lays its
	/// boards from its own stream, so no two share a board line.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		var painter = new Painter( WallSize, Driftwood, 0.85f );
		var random = new Random( Seed + 650 + variant * 31 );
		float Next() => (float)random.NextDouble();
		float Wrap( float x ) => (x % WallSize + WallSize) % WallSize;

		var (tops, replaced) = Boards( variant );
		var boards = tops.Count - 1;

		// The hut's paint, a shade different on every board; entry 0 leaves pixels alone, the last is the replaced board.
		var paint = Palette.Hex( HutPastels[variant] );
		var palette = new Color[boards + 2];
		palette[0] = new Color( 0f, 0f, 0f, 0f );
		for ( var b = 0; b < boards; b++ ) palette[b + 1] = Mix( paint, HutWhite, Next() * 0.1f );
		var other = (variant + 1 + random.Next( HutPastels.Length - 1 )) % HutPastels.Length;
		palette[boards + 1] = Mix( Mix( Palette.Hex( HutPastels[other] ), paint, 0.45f ), HutWhite, 0.3f + Next() * 0.15f );

		// One butt joint per board, anywhere along it. The replaced board runs the hut's whole length
		// like the rest: a stretch of one would come round again in every repeat, and so twice in a
		// bay wider than the gap between them.
		var joints = new float[boards];
		for ( var b = 0; b < boards; b++ ) joints[b] = Next() * WallSize;

		var wear = painter.Noise( 24, Seed + 601 + variant, 4 );
		var grainField = painter.Noise( 6, Seed + 602, 2 );
		var thin = painter.Noise( 4, Seed + 608 + variant, 3 );
		var index = new byte[WallSize * WallSize];
		var relief = new float[WallSize * WallSize];
		var bare = new float[WallSize * WallSize];
		var through = new float[WallSize * WallSize];
		var lap = new float[WallSize * WallSize];

		for ( var b = 0; b < boards; b++ )
		{
			var top = (int)tops[b]; var bottom = (int)tops[b + 1];
			var height = bottom - top;
			for ( var py = top; py < bottom; py++ )
			{
				var t = (py - top + 0.5f) / height;
				for ( var px = 0; px < WallSize; px++ )
				{
					var i = py * WallSize + px;
					var patched = b == replaced;
					index[i] = (byte)(patched ? boards + 1 : b + 1);

					// Lapped board: thin at the top, thick at the foot, then a shadowed step down to the next.
					var shadow = t > 0.93f;
					var g = StreakX( grainField, WallSize, px, py + b * 53, 37 ) - 0.5f;
					relief[i] = shadow ? 0.25f : 0.5f + t * 0.5f + g * 0.05f;
					if ( shadow ) lap[i] = 0.55f;

					// Paint thinned along the grain, so the timber's figure shows through it.
					var s = StreakX( thin, WallSize, px, py + b * 29, 61 );
					through[i] = s > 0.42f ? MathF.Min( (s - 0.42f) * 2.2f, 1f ) * 0.34f : 0f;

					// Flaking: crisp-edged patches, more along the laps and around the joints, where water sits;
					// the newer replaced board has had less time to weather.
					// Distance to the board's butt joint, round the wrap (inline: this runs per pixel).
					var dj = ((px - joints[b]) % WallSize + WallSize) % WallSize;
					dj = MathF.Min( dj, WallSize - dj );
					var exposure = wear[i] + MathF.Max( 0f, t - 0.6f ) * 0.45f + MathF.Max( 0f, 1f - dj / 30f ) * 0.3f - (patched ? 0.08f : 0f);
					bare[i] = exposure > 0.745f ? MathF.Min( (exposure - 0.745f) * 7f, 1f ) * 0.65f : 0f;
				}
			}
		}

		painter.Blend( index, palette );
		painter.AddHeight( relief, 1f );
		painter.Blend( through, Driftwood );
		painter.Blend( bare, Mix( Driftwood, HutWhite, 0.3f ), roughness: 0.9f );
		painter.Blend( lap, Mix( Driftwood, Palette.Ink, 0.3f ) );

		// The butt joints: a dark hairline gap where two lengths meet.
		for ( var b = 0; b < boards; b++ )
			painter.Line( new Vector2( Wrap( joints[b] ), tops[b] ), new Vector2( Wrap( joints[b] ), tops[b + 1] - 4f ), 1.6f, Mix( Driftwood, Palette.Ink, 0.3f ), 0.45f, 0.3f );

		// Salt bleaching in broad patches.
		painter.Blend( Ramp( painter.Noise( 4, Seed + 603 + variant, 4 ), 0.55f, 0.85f, 0.25f ), HutWhite );

		// Nails at every stud, about 24 units apart but never on a grid, each with a faint rust halo.
		var studs = new List<float>();
		for ( var x = Next() * 40f; x < WallSize - 30f; x += (24f + (Next() - 0.5f) * 8f) * Ppu ) studs.Add( x );
		for ( var b = 0; b < boards; b++ )
		{
			var ny = tops[b + 1] - (tops[b + 1] - tops[b]) * 0.22f;
			foreach ( var x in studs )
			{
				var nx = x + (Next() - 0.5f) * 4f;
				if ( Next() < 0.4f ) painter.Circle( nx, ny + (Next() - 0.5f) * 3f, 3f + Next() * 3f, Rust, 0.08f );
				painter.Circle( nx, ny, 1.8f, NailHead, 0.9f, height: 1.05f, bevel: 1f, roughness: 0.5f );
			}
			painter.Circle( Wrap( joints[b] ) - 6f, ny, 1.8f, NailHead, 0.9f );
			painter.Circle( Wrap( joints[b] ) + 6f, ny, 1.8f, NailHead, 0.9f );
		}

		painter.Mottle( 0.04f, 6, Seed + 604 + variant, 4 );
		painter.RoughnessNoise( 0.05f, 8, Seed + 605, 3 );
		painter.Cavity( 0.35f, 4 );
		painter.EdgeWear( 0.3f, 3, Seed + 606 + variant );
		painter.Grain( 0.012f, Seed + 607 + variant );
		return painter.Bake( $"arena_2_wall_{variant}", Painter.Matte, 5f );
	}

	/// <summary>Weathered white-painted timber, flaking to driftwood grey; the slab edge a worn deck board.</summary>
	protected override PaintedSurface PaintTrim()
	{
		return PaintTrimAtlas( "arena_2_trim", HutWhite, Driftwood, 0.7f, 0.12f, Seed + 700, painter =>
		{
			var random = new Random( Seed + 701 );
			float Next() => (float)random.NextDouble();
			TintBand( painter, TrimBand.SlabEdge, Mix( Driftwood, SandDark, 0.3f ), 0.85f, NosingProfile, Mix( Driftwood, Palette.Ink, 0.4f ), 0.5f );
			GrainRows( painter, TrimRows( TrimBand.SlabEdge ).Top, TrimRows( TrimBand.SlabEdge ).Height, Mix( Driftwood, Palette.Ink, 0.3f ), 0.5f, Seed + 702 );

			foreach ( var band in new[] { TrimBand.Cornice, TrimBand.Pilaster, TrimBand.Skirting } )
			{
				var (top, height) = TrimRows( band );
				var flakes = 5 + random.Next( 5 );
				for ( var f = 0; f < flakes; f++ )
				{
					var at = new Vector2( Next() * TrimSize, top + 4f + Next() * (height - 8f) );
					painter.Polygon( Organic( at, new Vector2( 3f + Next() * 9f, 1.5f + Next() * 4f ), 0.9f, random.Next(), Next() * 0.4f, 16 ), Driftwood,
						alpha: 0.45f, roughness: 0.9f );
				}
			}
		} );
	}

	protected override PaintedSurface PaintFloor()
	{
		const int cell = FloorSize / 2;
		var painter = new Painter( FloorSize, Mix( SandShade, SandDark, 0.6f ), 0.95f );
		var random = new Random( Seed );
		float Next() => (float)random.NextDouble();

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var tone = Next() * 2f - 1f;
			var sand = tone < 0f ? Mix( Sand, SandShade, -tone * 0.4f ) : Mix( Sand, SandLight, tone * 0.4f );
			// The sand runs on over the arris and slumps toward the joint at the chamfer's foot.
			painter.Fill( (int)min.x, (int)min.y, cell, cell, Mix( sand, SandDark, 0.5f ), height: 0.6f );
			painter.Polygon( SlabOutline( min, cell, 3f + Next() * 4f, 4f, 12f + Next() * 18f, Seed + c * 101 ), sand, height: 1f, bevel: Rim, roughness: 0.9f );

			// Grit: shell fragments and pebbles, each its own size, outline and colour. Painted before
			// the ripples are added, so they ride the sand instead of sinking into the crests.
			var bits = 22 + random.Next( 20 );
			for ( var b = 0; b < bits; b++ )
			{
				var at = min + new Vector2( 34f + Next() * 444f, 34f + Next() * 444f );
				var size = 1.2f + Next() * Next() * 4f;
				var half = new Vector2( size, size * (0.5f + Next() * 0.5f) );
				var color = Mix( Palette.Hex( Grit[random.Next( Grit.Length )] ), Sand, 0.25f );
				painter.Polygon( Organic( at, half, 0.7f, Seed + c * 61 + b, Next() * MathF.Tau, 10 ), color,
					height: 1.1f + Next() * 0.12f, bevel: 1.2f, roughness: 0.45f + Next() * 0.35f );
			}

			// One half-buried find per variant 1 and 2 — small, bleached and broken, because each of
			// them lies on a quarter of the floor's 900 tiles and must never read as a sticker.
			if ( c == 1 ) PaintStarfish( painter, min + new Vector2( 150f + Next() * 210f, 150f + Next() * 210f ), 30f + Next() * 8f, Next() * MathF.Tau, random );
			if ( c == 2 ) PaintScallop( painter, min + new Vector2( 160f + Next() * 190f, 180f + Next() * 170f ), 26f + Next() * 6f, Next() * MathF.Tau, random );
		}

		PaintRipples( painter, random );

		// Damp patches, darker and less matte, wandering across tiles.
		painter.Blend( Ramp( painter.Noise( 3, Seed + 30, 4 ), 0.6f, 0.85f, 0.35f ), Damp, roughness: 0.75f );

		painter.HeightNoise( 0.8f, 4, Seed + 31, 4 );
		painter.HeightNoise( 0.08f, 128, Seed + 32, 2 );
		painter.Mottle( 0.05f, 4, Seed + 33, 6 );
		painter.RoughnessNoise( 0.05f, 8, Seed + 34, 3 );
		painter.Cavity( 0.2f, 8 );
		painter.Grain( 0.02f, Seed + 35 );
		return painter.Bake( "arena_2_floor", Painter.Matte, 6f );
	}

	/// <summary>
	/// Wind ripples, a different heading and wavelength per block. Each crest is warped by noise and
	/// skewed (phase plus a fraction of its own sine), so the lee side is steeper than the stoss side
	/// like real ripples; amplitude fades in and out. Heavy dark grains settle in the troughs, the
	/// dry crests bleach lighter.
	/// </summary>
	void PaintRipples( Painter painter, Random random )
	{
		const int cell = FloorSize / 2;
		var warp = painter.Noise( 6, Seed + 20, 3 );
		var warpFine = painter.Noise( 14, Seed + 21, 2 );
		var strength = painter.Noise( 3, Seed + 22, 3 );
		var ripple = new float[FloorSize * FloorSize];
		var troughs = new float[FloorSize * FloorSize];
		var crests = new float[FloorSize * FloorSize];

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var heading = ((float)random.NextDouble() - 0.5f) * 1.1f;
			var dx = MathF.Cos( heading ); var dy = MathF.Sin( heading );
			var wavelength = cell / (4.5f + (float)random.NextDouble() * 2f);

			for ( var y = (int)min.y; y < (int)min.y + cell; y++ )
			{
				var row = y * FloorSize;
				for ( var x = (int)min.x; x < (int)min.x + cell; x++ )
				{
					var i = row + x;
					// Position within one ripple, 0..1: a long gentle rise (stoss), a crest, a short steep fall (lee).
					var phase = ((x - min.x) * dx + (y - min.y) * dy + (warp[i] - 0.5f) * 50f + (warpFine[i] - 0.5f) * 14f) / wavelength;
					var t = phase - MathF.Floor( phase );
					var profile = t < 0.66f ? t / 0.66f : (1f - t) / 0.34f;
					profile = profile * profile * (3f - 2f * profile);
					var amplitude = 0.3f + strength[i] * 0.9f;
					var h = (profile - 0.5f) * 2f * amplitude;
					ripple[i] = h;
					troughs[i] = h < -0.4f ? MathF.Min( (-h - 0.4f) * 1.5f, 1f ) * 0.22f : 0f;
					crests[i] = h > 0.5f ? MathF.Min( (h - 0.5f) * 2f, 1f ) * 0.3f : 0f;
				}
			}
		}

		painter.AddHeight( ripple, 1.2f );
		painter.Blend( troughs, Mix( SandShade, SandDark, 0.5f ) );
		painter.Blend( crests, SandLight );
	}

	/// <summary>
	/// A dried starfish half sunk in the sand: five arms of their own length and curl, one snapped
	/// short, bleached most of the way to the sand's colour, faint tubercles, and sand drifted over
	/// part of it.
	/// </summary>
	static void PaintStarfish( Painter painter, Vector2 centre, float radius, float rotation, Random random )
	{
		float Next() => (float)random.NextDouble();
		Vector2 Toward( float angle, float distance ) => centre + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * distance;

		var points = new List<Vector2>();
		var arms = new (float angle, float length, float curl)[5];
		for ( var a = 0; a < 5; a++ )
		{
			var angle = rotation + a * MathF.Tau / 5f + (Next() - 0.5f) * 0.18f;
			var length = radius * (a == 0 ? 0.5f : 0.82f + Next() * 0.3f);
			var curl = (Next() - 0.5f) * 0.3f;
			arms[a] = (angle, length, curl);

			points.Add( Toward( angle - MathF.PI / 5f, radius * (0.34f + Next() * 0.06f) ) );
			points.Add( Toward( angle - 0.24f + curl * 0.4f, length * 0.6f ) );
			points.Add( Toward( angle - 0.08f + curl, length * 0.94f ) );
			points.Add( Toward( angle + curl * 1.1f, length ) );
			points.Add( Toward( angle + 0.08f + curl, length * 0.94f ) );
			points.Add( Toward( angle + 0.24f + curl * 0.4f, length * 0.6f ) );
		}

		var body = Mix( Coral, Sand, 0.55f );
		painter.Polygon( points.ToArray(), body, height: 1.22f, bevel: 7f, roughness: 0.8f );

		foreach ( var (angle, length, curl) in arms )
		{
			var studs = 6 + random.Next( 4 );
			for ( var s = 0; s < studs; s++ )
			{
				var t = 0.12f + s / (float)studs * 0.75f;
				var at = Toward( angle + curl * t + (Next() - 0.5f) * 0.12f, length * t );
				var r = 1f + Next() * 0.9f;
				painter.Circle( at.x, at.y, r, Mix( CoralLight, Sand, 0.4f ), 0.5f, height: 1.27f, bevel: r, roughness: 0.75f );
			}
		}

		// Sand drifted over two of the arms and the edge of the disc.
		for ( var d = 0; d < 3; d++ )
		{
			var arm = arms[1 + random.Next( 4 )];
			var at = Toward( arm.angle + (Next() - 0.5f) * 0.6f, arm.length * (0.4f + Next() * 0.5f) );
			painter.Polygon( Organic( at, new Vector2( radius * 0.45f, radius * 0.3f ), 0.8f, random.Next(), Next() * MathF.Tau, 20 ), Sand, alpha: 0.85f, height: 1.1f, bevel: 6f, roughness: 0.9f );
		}
	}

	/// <summary>A small scallop, hinge-first in the sand: fan of ribs, scalloped rim, ears, faint pink growth bands.</summary>
	static void PaintScallop( Painter painter, Vector2 hinge, float radius, float heading, Random random )
	{
		var shapeSeed = random.Next();
		var shell = Mix( ShellCream, Sand, 0.3f );
		painter.Polygon( ScallopOutline( hinge, radius, heading, shapeSeed ), shell, height: 1.18f, bevel: 7f, roughness: 0.5f );
		painter.Polygon( ScallopOutline( hinge, radius * 0.74f, heading, shapeSeed ), Coral, alpha: 0.14f );
		painter.Polygon( ScallopOutline( hinge, radius * 0.46f, heading, shapeSeed ), Coral, alpha: 0.12f );

		const int Ribs = 13;
		for ( var r = 0; r <= Ribs; r++ )
		{
			var angle = heading + (r / (float)Ribs - 0.5f) * 2.1f;
			var direction = new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) );
			painter.Line( hinge + direction * radius * 0.16f, hinge + direction * radius * 0.94f, 1.6f, ShellRib, 0.3f, 1.14f );
		}

		// The hinge end is under the sand.
		painter.Polygon( Organic( hinge, new Vector2( radius * 0.55f, radius * 0.4f ), 0.8f, shapeSeed + 1, heading, 20 ), Sand, alpha: 0.9f, height: 1.08f, bevel: 6f, roughness: 0.9f );
	}

	static Vector2[] ScallopOutline( Vector2 hinge, float radius, float heading, int seed )
	{
		var random = new Random( seed );
		var forward = new Vector2( MathF.Cos( heading ), MathF.Sin( heading ) );
		var side = new Vector2( -forward.y, forward.x );
		var points = new List<Vector2>
		{
			hinge - side * radius * 0.3f - forward * radius * 0.03f,
			hinge - side * radius * 0.32f + forward * radius * 0.15f,
		};

		const int Ribs = 13;
		for ( var k = 0; k <= Ribs * 2; k++ )
		{
			var angle = heading + (k / (float)(Ribs * 2) - 0.5f) * 2.16f;
			var reach = radius * (k % 2 == 0 ? 1f : 0.95f) * (0.98f + (float)random.NextDouble() * 0.04f);
			points.Add( hinge + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * reach );
		}

		points.Add( hinge + side * radius * 0.32f + forward * radius * 0.15f );
		points.Add( hinge + side * radius * 0.3f - forward * radius * 0.03f );
		return points.ToArray();
	}

	/// <summary>
	/// Striped sailcloth: 32 px stripes, so all four undersides start on the same stripe and the
	/// pattern runs on from tile to tile. Basket weave in height, soft sagging folds, sun-faded
	/// patches and stitched panel seams.
	/// </summary>
	protected override PaintedSurface PaintCeiling()
	{
		const int cell = CeilingSize / 2;
		const int StripeWidth = 32;
		var painter = new Painter( CeilingSize, Canvas, 0.85f );
		var random = new Random( Seed + 500 );
		float Next() => (float)random.NextDouble();

		for ( var y = StripeWidth; y < CeilingSize; y += StripeWidth * 2 )
			painter.Rect( 0f, y, CeilingSize, StripeWidth, Stripe, roughness: 0.82f );

		var weave = new float[CeilingSize * CeilingSize];
		for ( var y = 0; y < CeilingSize; y++ )
		for ( var x = 0; x < CeilingSize; x++ )
		{
			var over = ((x >> 2) + (y >> 2)) % 2 == 0;
			var t = over ? x & 3 : y & 3;
			weave[y * CeilingSize + x] = t == 0 ? 0f : t == 2 ? 1f : 0.5f;
		}
		painter.AddHeight( weave, 0.14f );

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );

			// Panel seams on two stripe edges per tile, stitched.
			for ( var s = 0; s < 2; s++ )
			{
				var seamY = min.y + StripeWidth * (1 + random.Next( 7 )) + (s == 0 ? -3f : 3f);
				for ( var x = min.x + 3f + Next() * 4f; x < min.x + cell - 5f; x += 5.5f + Next() * 1.2f )
					painter.Line( new Vector2( x, seamY ), new Vector2( x + 2.6f, seamY + (Next() - 0.5f) * 0.6f ), 1.1f, Thread, 0.55f, 0.35f );
			}
		}

		// Sun-fading: the coral bleaches toward the canvas in broad patches.
		painter.Blend( Ramp( painter.Noise( 3, Seed + 520, 4 ), 0.5f, 0.85f, 0.3f ), Canvas );
		painter.HeightNoise( 0.6f, 3, Seed + 521, 4 );
		painter.Mottle( 0.05f, 3, Seed + 522, 5 );
		painter.Cavity( 0.3f, 3 );
		painter.Grain( 0.015f, Seed + 523 );
		return painter.Bake( "arena_2_ceiling", Painter.Matte, 3f );
	}
}
