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
	/// The structural front layer — painted posts, a top plate, skirting — then the dressing. Along the
	/// top plate a rope swagged from hook to hook, fans of palm fronds tied to some of the posts, and five
	/// hurricane lanterns hung from the plate, each holding one of the room's warm lamps. Over most huts a
	/// striped awning in the hut's own colour. Every bay is dressed as a set, low and high: one bay a wall
	/// holds a life ring over a coil of rope or a bucket, with oars leaning beside it; the rest are dealt
	/// from three sets — surfboards leaning on the boards, folded deck chairs under a hung towel, a
	/// driftwood bench under a peg rail — with beach balls and sand blown up against the skirting, and
	/// over every set one high piece dealt the same way: a kite, boards on a rack, a shelf of finds, glass
	/// floats or crossed oars. A narrow bay gets a board (or a towel) and sand. Each wall draws from its
	/// own stream, so none match.
	/// </summary>
	public override void Dress( FloorContext room )
	{
		var bays = BuildFrontLayer( room, new FrontStyle { Post = 24f, Pier = 70f, Corner = 28f, Cornice = TopPlate, Skirting = Skirting, SkirtingDepth = 4f } );
		// Self-illumination is an exponent (2^b): at half the paint the lantern glass keeps its amber under bloom.
		Glow.Set( "g_flSelfIllumBrightness", -1f );
		var props = new Props( room, Trim, Glow );
		var lamps = LampSpots( 5, Seed + 900 );

		for ( var side = 0; side < 4; side++ )
		{
			props.Begin( side, Seed + 1000 + side * 137 );
			var wall = bays.FindAll( b => b.Side == side );
			props.Rope();

			foreach ( var bay in wall )
				if ( bay.Width > 60f && props.Next() < 0.85f ) props.Awning( bay, WallOf( bay ) );

			// Fronds on about half the posts — never the pier, where the plaque hangs, nor beside a lantern.
			for ( var k = 0; k + 1 < wall.Count; k++ )
			{
				var post = (wall[k].To + wall[k + 1].From) * 0.5f;
				var lit = lamps.Exists( l => l.Side == side && MathF.Abs( l.U - post ) < 40f );
				if ( MathF.Abs( post - FloorContext.HalfSpan ) < 60f || lit ) continue;
				if ( props.Next() < 0.5f ) props.Fronds( post );
			}

			// The plain bays in a shuffled order: the life ring's set in the first, then this wall's own
			// order of the other sets dealt round, surfboards twice in the deck.
			var plain = wall.FindAll( b => b.Width > 70f );
			Shuffle( plain, props );
			var deck = new List<Props.Look> { Props.Look.Surf, Props.Look.DeckChairs, Props.Look.Bench, Props.Look.Surf };
			Shuffle( deck, props );
			var upper = new List<Props.High> { Props.High.Kite, Props.High.Rack, Props.High.Shelf, Props.High.Floats, Props.High.Oars };
			Shuffle( upper, props );
			for ( var k = 0; k < plain.Count; k++ )
			{
				if ( k == 0 ) props.Lifeguard( plain[k] );
				else props.Furnish( deck[(k - 1) % deck.Count], plain[k] );
				props.Upper( upper[k % upper.Count], plain[k] );
			}
			foreach ( var bay in wall.FindAll( b => b.Width <= 70f && b.Width > 24f ) )
				props.Narrow( bay );
		}

		props.Begin( 0, Seed + 1900 );
		foreach ( var lamp in lamps )
		{
			props.Lantern( lamp.Side, lamp.U );
			AddLamp( room, lamp.Side, lamp.U, Props.LanternLight, Props.LanternDepth, LightColor * 3.6f * lamp.Brightness );
		}
	}

	/// <summary>Shuffles <paramref name="list"/> in place from the props' stream (Fisher–Yates).</summary>
	static void Shuffle<T>( List<T> list, Props props )
	{
		for ( var k = list.Count - 1; k > 0; k-- )
		{
			var j = (int)(props.Next() * (k + 1));
			(list[k], list[j]) = (list[j], list[k]);
		}
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

	/// <summary>Weathered white-painted timber, flaking to driftwood grey; the slab edge a worn deck board; the props' paints in the infill band.</summary>
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

			PaintSwatches( painter, new Random( Seed + 703 ) );
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

	/// <summary>
	/// The lit glass of the hurricane lanterns, on the self-lit base: four flame tints (old lanterns never
	/// burn alike), painted in the warm accent range so bloom leaves them amber rather than white.
	/// </summary>
	protected override PaintedSurface PaintGlow()
	{
		const int Size = 32;
		var painter = new Painter( Size, Palette.Hex( 0xFFC27A ), 0.3f );
		painter.Fill( 16, 0, 16, 16, Palette.Hex( 0xFFB86E ), roughness: 0.3f );
		painter.Fill( 0, 16, 16, 16, Palette.Hex( 0xF5C48E ), roughness: 0.3f );
		painter.Fill( 16, 16, 16, 16, Palette.Hex( 0xFFCF94 ), roughness: 0.3f );
		var glow = new float[Size * Size];
		Array.Fill( glow, 1f );
		painter.AddGlow( glow );
		return painter.Bake( "arena_2_glow", ArenaGenerator.GlowMaterialPath, 1f );
	}

	static Rect FlameGlow( int k ) => new( (k % 2) * 0.5f + 0.08f, (k / 2) * 0.5f + 0.08f, 0.34f, 0.34f );

	/// <summary>Awning canvas: each hut's pastel a shade deeper than its boards, and the same sun-faded.</summary>
	static Rect AwningSwatch( int hut, bool faded ) => Swatch( (faded ? 128 : 0) + hut * 32, 0, 32, 32 );
	static Rect CanvasSwatch( bool faded ) => Swatch( faded ? 288 : 256, 0, 32, 32 );
	static Rect RopeSwatch => Swatch( 320, 0, 64, 32 );
	static Rect SandSwatch => Swatch( 384, 0, 64, 32 );
	static Rect DriftwoodSwatch => Swatch( 448, 0, 64, 32 );
	static Rect SurfSwatch( int k ) => Swatch( k * 64, 32, 64, 32 );
	static Rect BallSwatch => Swatch( 256, 32, 64, 32 );
	static Rect LanternSwatch( int k ) => Swatch( 320 + k * 32, 32, 32, 32 );
	static Rect PalmSwatch => Swatch( 416, 32, 64, 32 );
	static Rect StrawSwatch => Swatch( 480, 32, 32, 32 );
	static Rect DeckCanvasSwatch( int k ) => Swatch( k * 32, 64, 32, 32 );
	static Rect TowelSwatch( int k ) => Swatch( 64 + k * 32, 64, 32, 32 );
	static Rect BucketSwatch( int k ) => Swatch( 160 + k * 32, 64, 32, 32 );
	static Rect OarSwatch => Swatch( 288, 64, 64, 32 );
	static Rect IronSwatch => Swatch( 352, 64, 32, 32 );
	static Rect PlasticSwatch => Swatch( 384, 64, 32, 32 );
	static Rect FloatSwatch( int k ) => Swatch( 416 + k * 32, 64, 32, 32 );
	static Rect ShellSwatch => Swatch( 32, 96, 32, 32 );
	static Rect LifeRingSwatch => Swatch( 0, 96, 32, 64 );

	/// <summary>Surfboard decks: peach, aqua, butter, lilac — the bottoms all a sun-yellowed white.</summary>
	static readonly uint[] Decks = { 0xF2C6B4, 0x9FD0D6, 0xF4E1A1, 0xC7B8E0 };

	/// <summary>Beach plastic: sky blue, sun yellow, coral, mint.</summary>
	static readonly uint[] Plastics = { 0x8FC3D9, 0xF0CB78, 0xE9A39A, 0xA9D3A0 };

	/// <summary>
	/// The props' materials, painted into the infill band (the beach hangs no infill pictures): awning
	/// canvas per hut, fresh and faded, and white and cream canvas; rope, blown sand, driftwood; four
	/// surfboards, a beach ball's six gores, three lantern paints, a palm frond, straw; deck-chair canvas,
	/// three towels, four buckets, a varnished oar, iron, white plastic, and a life ring's bands.
	/// </summary>
	static void PaintSwatches( Painter painter, Random random )
	{
		float Next() => (float)random.NextDouble();
		var band = TrimRows( TrimBand.Infill ).Top;

		// Canvas: one colour, a little mottled (a painted weave read as hatching on the small stripe faces).
		void Canvas( int x0, int y0, int w, int h, Color color )
		{
			painter.Fill( x0, y0, w, h, color, roughness: 0.9f );
			for ( var s = 0; s < 6; s++ )
				painter.Polygon( Organic( new Vector2( x0 + Next() * w, y0 + Next() * h ), new Vector2( 3f + Next() * 6f, 2f + Next() * 4f ), 0.8f, random.Next() ), HutWhite, alpha: 0.18f );
		}

		{
			var y0 = band;
			// Rope: three strands laid up at a slant, each with a light crown and a dark lay line. Painted
			// first: its slanting strands run over into the cells either side, which are painted over them.
			painter.Fill( 320, y0, 64, 32, Palette.Hex( 0xC2AC80 ), roughness: 0.95f );
			for ( var x = -32f; x < 64f; x += 5f )
			{
				painter.Line( new Vector2( 320f + x, y0 + 32f ), new Vector2( 320f + x + 22f, y0 ), 1.2f, Palette.Hex( 0x8E7A57 ), 0.8f );
				painter.Line( new Vector2( 322.5f + x, y0 + 32f ), new Vector2( 324.5f + x + 22f, y0 ), 1.4f, Palette.Hex( 0xD9C79F ), 0.6f );
			}

			for ( var hut = 0; hut < HutPastels.Length; hut++ )
			{
				var paint = Palette.Hex( HutPastels[hut] );
				Canvas( hut * 32, y0, 32, 32, Mix( paint, Palette.Ink, 0.16f ) );
				Canvas( 128 + hut * 32, y0, 32, 32, Mix( Mix( paint, Palette.Ink, 0.08f ), HutWhite, 0.3f ) );
			}
			Canvas( 256, y0, 32, 32, HutWhite );
			Canvas( 288, y0, 32, 32, Mix( HutWhite, Sand, 0.3f ) );

			// Sand blown into heaps: the floor's sand with grit and a few shell flecks.
			painter.Fill( 384, y0, 64, 32, Sand, roughness: 0.95f );
			for ( var s = 0; s < 60; s++ )
				painter.Circle( 384f + Next() * 64f, y0 + Next() * 32f, 0.5f + Next() * 0.8f, Mix( Palette.Hex( Grit[random.Next( Grit.Length )] ), Sand, 0.3f ), 0.8f );

			// Driftwood: silver-grey with grain running along it.
			painter.Fill( 448, y0, 64, 32, Driftwood, roughness: 0.9f );
			for ( var y = 1f; y < 32f; y += 1.5f + Next() * 2.5f )
				painter.Line( new Vector2( 448f, y0 + y ), new Vector2( 512f, y0 + y + Next() * 2f - 1f ), 0.6f, Mix( Driftwood, Palette.Ink, 0.25f ), 0.5f );
		}

		{
			var y0 = band + 32;
			// Surfboards, mapped round the board (U: rail, deck, rail, bottom) and nose (top) to tail: a deck
			// colour with a wooden stringer down its middle, a stripe across the nose, a white bottom.
			for ( var k = 0; k < 4; k++ )
			{
				var x0 = k * 64;
				var deck = Palette.Hex( Decks[k] );
				painter.Fill( x0, y0, 32, 32, deck, roughness: 0.25f );
				painter.Fill( x0 + 32, y0, 32, 32, Palette.Hex( 0xF4EEDF ), roughness: 0.3f );
				var stripe = Palette.Hex( Decks[(k + 2) % 4] );
				painter.Fill( x0, y0 + 5 + k % 2, 32, 3, Mix( stripe, Palette.Ink, 0.1f ), roughness: 0.25f );
				if ( k % 2 == 0 ) painter.Fill( x0, y0 + 10, 32, 1, Mix( stripe, Palette.Ink, 0.1f ) );
				painter.Fill( x0 + 17, y0, 1, 32, Palette.Hex( 0x9C7B5B ), roughness: 0.4f );
				painter.Fill( x0 + 46, y0, 1, 32, Palette.Hex( 0xC9B79C ) );
				// Wax on the deck's tail half, rubbed patchy.
				for ( var s = 0; s < 10; s++ )
					painter.Circle( x0 + 6f + Next() * 22f, y0 + 18f + Next() * 12f, 1.5f + Next() * 2.5f, Color.White, 0.12f, roughness: 0.6f );
			}

			// The beach ball's gores, mapped once round it, with white caps over the poles.
			var gores = new uint[] { 0xE9A39A, 0xF6F1E6, 0x8FC3D9, 0xF6F1E6, 0xF0CB78, 0xF6F1E6 };
			for ( var g = 0; g < 6; g++ )
			{
				var left = 256f + 3f + g * 58f / 6f;
				painter.Rect( g == 0 ? 256f : left, y0, (g == 0 ? 3f : 0f) + 58f / 6f + (g == 5 ? 3f : 0f) + 0.5f, 32f, Palette.Hex( gores[g] ), roughness: 0.2f );
			}
			painter.Fill( 256, y0, 64, 6, Palette.Hex( 0xF6F1E6 ), roughness: 0.2f );
			painter.Fill( 256, y0 + 26, 64, 6, Palette.Hex( 0xF6F1E6 ), roughness: 0.2f );

			// Lantern paint, chipped to bare metal: sea green, worn white enamel, blue-grey.
			var paints = new uint[] { 0x7FAEA4, 0xE8E3D8, 0x7C93AA };
			for ( var k = 0; k < 3; k++ )
			{
				var x0 = 320 + k * 32;
				painter.Fill( x0, y0, 32, 32, Palette.Hex( paints[k] ), roughness: 0.5f );
				for ( var s = 0; s < 8; s++ )
					painter.Polygon( Organic( new Vector2( x0 + Next() * 32f, y0 + Next() * 32f ), new Vector2( 1f + Next() * 3f, 1f + Next() * 2f ), 0.8f, random.Next() ), Palette.Hex( 0x6E625A ), alpha: 0.6f );
			}

			PaintPalm( painter, 416, y0, Palette.Hex( 0x6E9C58 ), random );

			// Straw: a tight plait.
			painter.Fill( 480, y0, 32, 32, Palette.Hex( 0xD8BE86 ), roughness: 0.9f );
			for ( var y = 0; y < 32; y += 3 )
			for ( var x = (y / 3) % 2 * 2; x < 32; x += 4 )
				painter.Rect( 480f + x, y0 + y, 2.5f, 2f, Palette.Hex( 0xB89C62 ), alpha: 0.6f );
		}

		{
			var y0 = band + 64;
			// Deck-chair canvas, striped down its length: coral and white; blue, butter and white.
			var canvases = new[] { new uint[] { 0xE9A39A, 0xF6F1E6 }, new uint[] { 0x8FB8D6, 0xF4E1A1, 0xF6F1E6 } };
			for ( var k = 0; k < 2; k++ )
				for ( var x = 0; x < 32; x += 4 )
					painter.Fill( k * 32 + x, y0, 4, 32, Palette.Hex( canvases[k][x / 4 % canvases[k].Length] ), roughness: 0.9f );

			// Towels, banded across: aqua, sun yellow and coral, lilac and white.
			var towels = new[] { new uint[] { 0x9FD0D6, 0xF6F1E6, 0x9FD0D6, 0x6FA7B8 }, new uint[] { 0xF0CB78, 0xE9A39A, 0xF0CB78, 0xF6F1E6 }, new uint[] { 0xC7B8E0, 0xF6F1E6 } };
			for ( var k = 0; k < 3; k++ )
			{
				for ( var y = 0; y < 32; y += 4 )
					painter.Fill( 64 + k * 32, y0 + y, 32, 4, Palette.Hex( towels[k][y / 4 % towels[k].Length] ), roughness: 0.97f );
				for ( var x = 0; x < 32; x += 2 ) painter.Rect( 64f + k * 32 + x, y0, 1f, 32f, Palette.Ink, alpha: 0.05f );
			}

			// Bucket plastic, a lighter rolled rim at the top.
			for ( var k = 0; k < 4; k++ )
			{
				var plastic = Palette.Hex( Plastics[k] );
				painter.Fill( 160 + k * 32, y0, 32, 32, plastic, roughness: 0.35f );
				painter.Fill( 160 + k * 32, y0, 32, 6, Mix( plastic, Color.White, 0.25f ), roughness: 0.3f );
			}

			// A varnished oar: grain along it, a painted band at the blade's tip (the bottom rows).
			painter.Fill( 288, y0, 64, 32, Palette.Hex( 0xBF915F ), roughness: 0.35f );
			for ( var x = 288f; x < 352f; x += 2f + Next() * 3f )
				painter.Line( new Vector2( x, y0 ), new Vector2( x + Next() * 2f - 1f, y0 + 32f ), 0.6f, Palette.Hex( 0x94683E ), 0.45f );
			painter.Fill( 288, y0 + 25, 64, 7, Palette.Hex( 0x8FC3D9 ), roughness: 0.4f );

			painter.Fill( 352, y0, 32, 32, Palette.Hex( 0x4D4846 ), roughness: 0.55f );
			for ( var s = 0; s < 10; s++ ) painter.Circle( 352f + Next() * 32f, y0 + Next() * 32f, 0.6f + Next(), Rust, 0.35f );
			painter.Fill( 384, y0, 32, 32, Palette.Hex( 0xF2EFE8 ), roughness: 0.4f );

			// Glass fishing floats: sea green, aqua and amber glass, a highlight and a darker seam band.
			var floats = new uint[] { 0x8DBFA6, 0x8CC6CF, 0xE2B56F };
			for ( var k = 0; k < 3; k++ )
			{
				var glass = Palette.Hex( floats[k] );
				painter.Fill( 416 + k * 32, y0, 32, 32, glass, roughness: 0.08f );
				painter.Fill( 416 + k * 32, y0 + 14, 32, 4, Mix( glass, Palette.Ink, 0.2f ), roughness: 0.1f );
				painter.Fill( 416 + k * 32, y0 + 4, 32, 5, Mix( glass, Color.White, 0.35f ), roughness: 0.05f );
			}
		}

		{
			// The life ring: four carrot bands and four white along its run, a little grubby.
			var y0 = band + 96;
			for ( var b = 0; b < 8; b++ )
				painter.Rect( 0f, y0 + (b == 0 ? 0f : 3f + b * 58f / 8f), 32f, (b == 0 ? 3f : 0f) + 58f / 8f + (b == 7 ? 3f : 0f) + 0.5f,
					b % 2 == 0 ? Palette.Hex( 0xE9A06A ) : Palette.Hex( 0xF6F1E6 ), roughness: 0.5f );
			for ( var s = 0; s < 12; s++ )
				painter.Polygon( Organic( new Vector2( Next() * 32f, y0 + Next() * 64f ), new Vector2( 2f + Next() * 4f, 1f + Next() * 2f ), 0.8f, random.Next() ), Palette.Hex( 0xB8AE9C ), alpha: 0.25f );

			// Shell: cream with pink growth bands round it.
			painter.Fill( 32, y0, 32, 32, ShellCream, roughness: 0.4f );
			for ( var y = 2f; y < 32f; y += 3f + Next() * 2f ) painter.Fill( 32, (int)(y0 + y), 32, 1, ShellRib, roughness: 0.4f );
		}
	}

	/// <summary>A palm frond's leaflets either side of a pale rib, each lighter at its centre than at its edges, dark gaps between.</summary>
	static void PaintPalm( Painter painter, int x0, int y0, Color leaf, Random random )
	{
		float Next() => (float)random.NextDouble();
		painter.Fill( x0, y0, 64, 32, Mix( leaf, Palette.Ink, 0.5f ), roughness: 0.6f );
		for ( var x = -6f; x < 66f; x += 3.4f * (0.85f + Next() * 0.3f) )
		{
			foreach ( var side in new[] { -1f, 1f } )
			{
				var root = new Vector2( x0 + x, y0 + 16f );
				var end = new Vector2( x0 + x + 18f, y0 + 16f + side * 15.5f );
				var tone = Mix( leaf, Color.White, Next() * 0.14f );
				painter.Line( root, end, 3.4f * 0.62f, Mix( tone, Palette.Ink, 0.12f ), 1f, roughness: 0.5f );
				painter.Line( root, end, 3.4f * 0.3f, tone, 1f, roughness: 0.45f );
			}
		}
		painter.Line( new Vector2( x0, y0 + 16f ), new Vector2( x0 + 64f, y0 + 16f ), 1.6f, Mix( leaf, Palette.Hex( 0xD9DDB0 ), 0.5f ), 0.9f );
	}

	/// <summary>
	/// Builds the beach room's props into the floor's kit, in wall space (see <see cref="WallProps"/>).
	/// What a player would use — boards, chairs, towels, buckets, the life ring — is at a player's size;
	/// each prop group draws from a stream of its own (<see cref="WallProps.Apart"/>), so retuning one does
	/// not reshuffle the wall.
	/// </summary>
	sealed class Props : WallProps
	{
		/// <summary>Where a lantern's lamp burns (inside its glass) and how far out from the top plate it hangs.</summary>
		public const float LanternLight = 216f;
		public const float LanternDepth = -9f;

		/// <summary>The underside of the top plate.</summary>
		const float Plate = FloorContext.ClearHeight - TopPlate;

		/// <summary>The face of the skirting, which things on the ledge stand against.</summary>
		const float Skirt = FloorContext.Recess - 4.5f;

		/// <summary>How low a frond's tip may droop: over the awnings, never into them.</summary>
		const float FrondFloor = 214f;

		const float Fold = 0.2f;

		static Vector3 ToRoom => new( 0f, -1f, 0f );

		readonly Material _trim;
		readonly Material _glow;

		public Props( FloorContext room, Material trim, Material glow ) : base( room )
		{
			_trim = trim;
			_glow = glow;
		}

		/// <summary>The set-ups a plain bay is dealt, after the life ring's.</summary>
		public enum Look { Surf, DeckChairs, Bench }

		/// <summary>What hangs high in a bay, over its set: a kite, a board on a rack, a shelf of finds, glass floats, crossed oars.</summary>
		public enum High { Kite, Rack, Shelf, Floats, Oars }

		Skin TrimSkin( Rect region ) => new( _trim, region );

		/// <summary>White-painted timber, the trim's stock band: awning frames, pegs.</summary>
		Skin Paint => TrimSkin( TrimRegion( TrimBand.Stock ) );

		/// <summary>A piece of cloth seen from both sides: a quad (anticlockwise from its front) and its back.</summary>
		void Sheet( Skin skin, Vector3 a, Vector3 b, Vector3 c, Vector3 d )
		{
			Kit.Quad( skin, At( a ), At( b ), At( c ), At( d ) );
			Kit.Quad( skin, At( d ), At( c ), At( b ), At( a ) );
		}

		void Pennant( Skin skin, Vector3 a, Vector3 b, Vector3 c )
		{
			Kit.Tri( skin, At( a ), At( b ), At( c ) );
			Kit.Tri( skin, At( a ), At( c ), At( b ) );
		}

		/// <summary>
		/// A rope swagged along the top plate from hook to hook, a metre or three apart, sagging between —
		/// the sail-loft's rigging, hung at the walls only (the ceiling is the floor above, R43).
		/// </summary>
		public void Rope() => Apart( () =>
		{
			const float Span = FloorContext.Span;
			const int Points = 7;
			var anchors = new List<float>();
			for ( var u = Range( 30f, 70f ); u < Span - 30f; u += Range( 80f, 150f ) ) anchors.Add( u );

			var rope = TrimSkin( RopeSwatch );
			foreach ( var u in anchors )
				Kit.Tube( TrimSkin( IronSwatch ), At( new[] { new Vector3( u, 0.5f, Plate - 0.4f ), new Vector3( u, -1.8f, Plate - 1.8f ) } ), 0.35f, 4 );

			for ( var a = 0; a + 1 < anchors.Count; a++ )
			{
				if ( Next() < 0.15f ) continue;
				var u0 = anchors[a];
				var u1 = anchors[a + 1];
				var sag = Range( 4f, 10f );
				var belly = Range( 0.4f, 2f );
				var path = new Vector3[Points];
				for ( var i = 0; i < Points; i++ )
				{
					var t = i / (Points - 1f);
					var bow = 4f * t * (1f - t);
					path[i] = new Vector3( u0 + (u1 - u0) * t, -1.8f - belly * bow, Plate - 1.8f - sag * bow );
				}
				Kit.Tube( rope, At( path ), 0.7f, 5 );

				// About half the swags carry bunting: pennants of the huts' canvases every hand or so.
				if ( Next() < 0.5f ) continue;
				var span = u1 - u0;
				var colour = Pick( 5 );
				for ( var d = Range( 3f, 6f ); d < span - 3f; d += Range( 7f, 9f ) )
				{
					var at = Along( path, d / span ) + new Vector3( 0f, -0.4f, -0.5f );
					var skin = TrimSkin( colour % 5 == 4 ? CanvasSwatch( false ) : AwningSwatch( colour % 5, false ) );
					colour++;
					var half = Range( 2.4f, 3f );
					Pennant( skin, at + new Vector3( -half, 0f, 0f ), at + new Vector3( Range( -0.6f, 0.6f ), Range( -0.8f, 0f ), -Range( 6f, 7.5f ) ), at + new Vector3( half, 0f, 0f ) );
				}
			}
		} );

		/// <summary>
		/// A striped awning over a hut's alcove, in the hut's own pastel and white (<paramref name="hut"/>):
		/// canvas sloping out from the wall over a front bar on two arms, a valance hanging off the bar,
		/// straight or cut in points. Stripes about a hand wide, so a wider hut gets more of them; about one
		/// in three is sun-faded. All of it above <see cref="WallProps.Clear"/>, reaching no further out
		/// than <see cref="WallProps.Overhang"/>.
		/// </summary>
		public void Awning( Bay bay, int hut ) => Apart( () =>
		{
			var width = bay.Width - Range( 6f, 14f );
			var u0 = bay.Middle - width * 0.5f + Range( -1.5f, 1.5f );
			var top = Range( 204f, 214f );
			var drop = Range( 32f, 44f );
			var front = -Range( 15f, 20.5f );
			var low = top - drop;
			const float Back = Face + 1.5f;
			var faded = Next() < 0.35f;
			var colour = TrimSkin( AwningSwatch( hut, faded ) );
			var white = TrimSkin( CanvasSwatch( faded ) );
			var count = Math.Max( 3, (int)MathF.Round( width / Range( 10f, 13.5f ) ) );
			var stripe = width / count;
			var odd = Pick( 2 );
			var valance = Range( 8f, 12f );
			var points = Next() < 0.55f;

			for ( var i = 0; i < count; i++ )
			{
				var skin = (i + odd) % 2 == 0 ? colour : white;
				var a = u0 + i * stripe;
				var b = a + stripe;
				Sheet( skin, new Vector3( a, front, low ), new Vector3( b, front, low ), new Vector3( b, Back, top ), new Vector3( a, Back, top ) );
				Sheet( skin, new Vector3( a, front, low - valance ), new Vector3( b, front, low - valance ), new Vector3( b, front, low ), new Vector3( a, front, low ) );
				if ( points )
					Pennant( skin, new Vector3( a, front, low - valance ), new Vector3( (a + b) * 0.5f, front, low - valance - stripe * 0.4f ), new Vector3( b, front, low - valance ) );
			}

			Kit.Tube( Paint, At( new[] { new Vector3( u0 - 0.5f, front + 0.7f, low + 0.2f ), new Vector3( u0 + width + 0.5f, front + 0.7f, low + 0.2f ) } ), 0.6f, 5 );
			foreach ( var u in new[] { u0 + 2.5f, u0 + width - 2.5f } )
				Kit.Tube( Paint, At( new[] { new Vector3( u, Face + 1f, top - Range( 28f, 38f ) ), new Vector3( u, front + 1.4f, low - 0.4f ) } ), 0.45f, 4 );
		} );

		/// <summary>
		/// A fan of palm fronds tied to the post at <paramref name="post"/> under the top plate: three to six,
		/// spread either way along the wall, rising a little and arching over, kept above the awnings and no
		/// further out over the room than <see cref="WallProps.Overhang"/>; a turn of rope where they meet.
		/// </summary>
		public void Fronds( float post ) => Apart( () =>
		{
			var root = new Vector3( post + Range( -2f, 2f ), -1.5f, Plate - Range( 4f, 10f ) );
			var count = 4 + Pick( 4 );
			for ( var f = 0; f < count; f++ )
			{
				var yaw = f % 2 == 0 ? Range( -0.55f, 0.3f ) : MathF.PI + Range( -0.3f, 0.55f );
				var rise = Range( 0.1f, 0.75f );
				Frond( root, yaw, rise, rise + Range( 0.8f, 1.5f ), Range( 44f, 64f ), Range( 0.3f, 0.38f ) );
			}
			Kit.Ellipsoid( TrimSkin( RopeSwatch ), At( root + new Vector3( 0f, 0.5f, 0f ) ), new Vector3( 2.2f, 1.5f, 3f ), Run, 3, 6 );
		} );

		/// <summary>
		/// One palm frond (see <see cref="MeshKit.Pinnate"/>): a rib from <paramref name="root"/> heading
		/// <paramref name="yaw"/>, rising at <paramref name="rise"/> radians and arching over by
		/// <paramref name="bend"/>. Grown shorter where the arch would carry it out of bounds: further than
		/// <see cref="WallProps.Overhang"/> over the room (its leaflets reach to the side as well), through the
		/// wall, into the ceiling, or down into the awnings (<see cref="FrondFloor"/>).
		/// </summary>
		void Frond( Vector3 root, float yaw, float rise, float bend, float length, float width )
		{
			var reach = (MathF.Sin( rise ) - MathF.Sin( rise - bend )) / bend;
			var fall = (MathF.Cos( rise - bend ) - MathF.Cos( rise )) / bend;
			var crest = rise > 0f ? (1f - MathF.Cos( MathF.Min( rise, bend ) )) / bend : 0f;
			var heading = Flat( yaw );
			length = MathF.Min( length, (root.y + Overhang - 2f) / (MathF.Max( 0f, -heading.y ) * reach + width * 0.5f * (1f + Fold)) );
			if ( heading.y > 0.05f ) length = MathF.Min( length, (Face - root.y - 1f) / (heading.y * reach) );
			if ( fall < 0f ) length = MathF.Min( length, (root.z - FrondFloor) / -fall );
			if ( crest > 0f ) length = MathF.Min( length, (FloorContext.ClearHeight - 3f - root.z) / crest );
			if ( length < 8f ) return;

			var direction = heading * MathF.Cos( rise ) + Vector3.Up * MathF.Sin( rise );
			Kit.Pinnate( TrimSkin( PalmSwatch ), At( root ), Turn( Aim( direction, Range( -12f, 12f ) ) ), length, length * width, bend,
				Math.Clamp( (int)(length / 3.2f), 8, 16 ), Fold, 0.8f, 0.7f, 0.08f, 0.35f );
		}

		/// <summary>
		/// A hurricane lantern hung from a hook in the top plate at <paramref name="u"/> of wall
		/// <paramref name="side"/> by a short cord: a wire bail, a vented cap, the glass chimney lit from
		/// inside (its lamp burns there: <see cref="LanternLight"/>), a wire guard and the fuel fount, in one
		/// of three chipped paints.
		/// </summary>
		public void Lantern( int side, float u )
		{
			OnSide( side );
			const float D = LanternDepth;
			const float S = 1.5f;
			var metal = TrimSkin( LanternSwatch( Pick( 3 ) ) );
			var iron = TrimSkin( IronSwatch );
			var turn = Turn( Rotation.FromYaw( Next() * 360f ) );
			const float Foot = 204f;
			Vector2[] Scaled( params Vector2[] profile ) => Array.ConvertAll( profile, p => p * S );

			Kit.Tube( iron, At( new[] { new Vector3( u, -0.5f, Plate + 1f ), new Vector3( u, D * 0.6f, Plate - 0.6f ), new Vector3( u, D, Plate - 2.2f ) } ), 0.35f, 4 );
			Kit.Tube( TrimSkin( RopeSwatch ), At( new[] { new Vector3( u, D, Plate - 2f ), new Vector3( u, D, Foot + 21f * S ) } ), 0.4f, 4 );

			var bail = new Vector3[7];
			for ( var i = 0; i < bail.Length; i++ )
			{
				var a = MathF.PI * i / (bail.Length - 1);
				bail[i] = new Vector3( u + MathF.Cos( a ) * 4.4f * S, D, Foot + (16.5f + MathF.Sin( a ) * 4.5f) * S );
			}
			Kit.Tube( iron, At( bail ), 0.3f, 4 );

			Kit.SmoothLathe( metal, At( new Vector3( u, D, Foot + 13f * S ) ), Scaled(
				new Vector2( 4.3f, 0f ), new Vector2( 4.3f, 0.6f ), new Vector2( 2.7f, 2.3f ), new Vector2( 1.3f, 3.3f ), new Vector2( 0f, 3.5f ) ), 10, turn, true, false, 40f );
			Kit.SmoothLathe( new Skin( _glow, FlameGlow( Pick( 4 ) ) ), At( new Vector3( u, D, Foot + 4f * S ) ), Scaled(
				new Vector2( 1.9f, 0f ), new Vector2( 3f, 2.5f ), new Vector2( 3.2f, 4.5f ), new Vector2( 2.6f, 7f ), new Vector2( 1.6f, 9f ) ), 10, turn, false, false );
			for ( var k = 0; k < 4; k++ )
			{
				var a = k * MathF.PI * 0.5f + 0.4f;
				var r = 3.9f * S;
				Kit.Tube( iron, At( new[] { new Vector3( u + MathF.Cos( a ) * r, D + MathF.Sin( a ) * r, Foot + 4f * S ), new Vector3( u + MathF.Cos( a ) * r, D + MathF.Sin( a ) * r, Foot + 13.2f * S ) } ), 0.22f, 3 );
			}
			Kit.SmoothLathe( metal, At( new Vector3( u, D, Foot ) ), Scaled(
				new Vector2( 0f, 0f ), new Vector2( 3.2f, 0f ), new Vector2( 4.2f, 1.2f ), new Vector2( 4.3f, 3f ), new Vector2( 3.5f, 4.2f ), new Vector2( 1.9f, 4.4f ) ), 10, turn, false, true, 40f );
		}

		// ---- Bays ------------------------------------------------------------------------------

		/// <summary>The life ring's bay: the ring on a peg, a coil of rope or a bucket under it, an oar or two leaning beside it.</summary>
		public void Lifeguard( Bay bay ) => Apart( () =>
		{
			var side = Next() < 0.5f ? -1f : 1f;
			var ring = bay.Middle + side * bay.Width * Range( 0.1f, 0.2f );
			LifeRing( ring, Range( 98f, 112f ) );
			if ( Next() < 0.55f ) Coil( new Vector3( ring + Range( -6f, 6f ), Skirt - 9f, Ledge ) );
			else Bucket( new Vector3( ring + Range( -8f, 8f ), Skirt - 7f, Ledge ), Range( 0.9f, 1.1f ), true );

			var oar = bay.Middle - side * bay.Width * Range( 0.2f, 0.3f );
			Oar( oar, Range( 9f, 15f ) * side * 0.6f );
			if ( Next() < 0.5f ) Oar( oar + side * Range( 9f, 13f ), Range( -6f, 6f ) );
			if ( Next() < 0.6f ) Sand( bay.From + 4f, bay.To - 4f );
		} );

		/// <summary>One of the three set-ups in <paramref name="bay"/>, most with sand blown against the skirting.</summary>
		public void Furnish( Look look, Bay bay ) => Apart( () =>
		{
			var side = Next() < 0.5f ? -1f : 1f;
			var near = side < 0f ? bay.From : bay.To;
			var far = side < 0f ? bay.To : bay.From;
			switch ( look )
			{
				case Look.Surf:
				{
					// Two or three boards leaning from one end, a ball or a towel at the other.
					var count = bay.Width > 150f ? 2 + Pick( 2 ) : 1 + Pick( 2 );
					var u = near - side * Range( 4f, 10f );
					var first = Pick( Decks.Length );
					for ( var k = 0; k < count; k++ )
					{
						var width = Range( 17f, 23f );
						u -= side * width * 0.5f;
						Board( u, Range( 68f, 102f ), width, (first + k) % Decks.Length );
						u -= side * (width * 0.5f + Range( 2f, 7f ));
					}
					if ( Next() < 0.6f ) Ball( far + side * Range( 18f, 30f ) );
					else Towel( far + side * Range( 16f, 24f ), Range( 92f, 102f ) );
					break;
				}
				case Look.DeckChairs:
				{
					// One or two deck chairs folded and leaning; a towel hung on a peg; a cool box or a bucket.
					var u = near - side * Range( 16f, 22f );
					DeckChair( u );
					if ( bay.Width > 150f && Next() < 0.6f ) DeckChair( u - side * Range( 26f, 30f ) );
					var free = far + side * Range( 22f, 34f );
					Towel( free, Range( 92f, 104f ) );
					if ( Next() < 0.55f ) CoolBox( free + side * Range( -4f, 6f ) );
					else Bucket( new Vector3( free + side * Range( 0f, 10f ), Skirt - 7f, Ledge ), Range( 0.9f, 1.15f ), true );
					break;
				}
				default:
				{
					// A driftwood bench, a peg rail over it with a hat and a towel, a ball or a bucket underneath.
					var length = MathF.Min( bay.Width - 24f, Range( 70f, 110f ) );
					var u = bay.Middle + side * Range( 0f, (bay.Width - 24f - length) * 0.5f );
					Bench( u, length );
					var rail = Range( 88f, 100f );
					Hat( u - length * Range( 0.15f, 0.3f ), rail );
					Towel( u + length * Range( 0.15f, 0.3f ), rail + Range( -2f, 4f ) );
					if ( Next() < 0.5f ) Ball( u + Range( -0.3f, 0.3f ) * length );
					break;
				}
			}
			if ( Next() < 0.7f ) Sand( bay.From + 4f, bay.To - 4f );
		} );

		/// <summary>A narrow bay: a board leaning in it (or a towel where even that will not fit), sand at its foot, now and then a ball.</summary>
		public void Narrow( Bay bay ) => Apart( () =>
		{
			if ( bay.Width >= 30f ) Board( bay.Middle + Range( -2f, 2f ), Range( 66f, 84f ), MathF.Min( bay.Width - 8f, Range( 17f, 21f ) ), Pick( Decks.Length ) );
			else Towel( bay.Middle, Range( 92f, 100f ) );
			Sand( bay.From + 2f, bay.To - 2f );
		} );

		/// <summary>
		/// What hangs high in <paramref name="bay"/>, over the set below it and under its awning: one of the
		/// <see cref="High"/> pieces, somewhere off the bay's middle.
		/// </summary>
		public void Upper( High kind, Bay bay ) => Apart( () =>
		{
			var u = bay.Middle + Range( -0.22f, 0.22f ) * bay.Width;
			switch ( kind )
			{
				case High.Kite: Kite( u, Range( 148f, 160f ) ); break;
				case High.Rack: Rack( u, Range( 128f, 146f ), MathF.Min( bay.Width - 30f, Range( 72f, 96f ) ) ); break;
				case High.Shelf: Shelf( u, MathF.Min( bay.Width - 40f, Range( 50f, 76f ) ), Range( 138f, 158f ) ); break;
				case High.Floats: Floats( u, Range( 182f, 192f ) ); break;
				default: CrossedOars( u, Range( 148f, 160f ) ); break;
			}
		} );

		/// <summary>
		/// A diamond kite pinned flat to the wall at <paramref name="u"/>, <paramref name="v"/>, a little askew:
		/// four panels in two of the huts' canvases, its spars, and a tail of bows trailing down the boards.
		/// </summary>
		void Kite( float u, float v )
		{
			var height = Range( 44f, 54f );
			var width = height * Range( 0.62f, 0.72f );
			var angle = Range( -18f, 18f ) * MathF.PI / 180f;
			var y = Face - 1.2f;
			Vector3 P( float x, float z ) => new( u + x * MathF.Cos( angle ) - z * MathF.Sin( angle ), y, v + x * MathF.Sin( angle ) + z * MathF.Cos( angle ) );

			var top = P( 0f, height * 0.5f );
			var bottom = P( 0f, -height * 0.5f );
			var left = P( -width * 0.5f, height * 0.2f );
			var right = P( width * 0.5f, height * 0.2f );
			var middle = P( 0f, height * 0.2f );
			var first = Pick( 4 );
			var a = TrimSkin( AwningSwatch( first, false ) );
			var b = TrimSkin( Next() < 0.5f ? CanvasSwatch( false ) : AwningSwatch( (first + 1 + Pick( 3 )) % 4, false ) );
			Pennant( a, top, left, middle );
			Pennant( b, top, middle, right );
			Pennant( b, left, bottom, middle );
			Pennant( a, middle, bottom, right );

			var spar = TrimSkin( DriftwoodSwatch );
			var proud = new Vector3( 0f, -0.4f, 0f );
			Kit.Tube( spar, At( new[] { top + proud, bottom + proud } ), 0.3f, 3 );
			Kit.Tube( spar, At( new[] { left + proud, right + proud } ), 0.3f, 3 );

			var tail = new Vector3[7];
			var phase = Next() * MathF.Tau;
			var drop = Range( 34f, 46f );
			var drift = Range( -1f, 1f );
			for ( var i = 0; i < tail.Length; i++ )
			{
				var t = i / (tail.Length - 1f);
				tail[i] = bottom + new Vector3( MathF.Sin( phase + t * 5f ) * 3.5f * t + t * drift * 4f, -0.2f, -drop * t );
			}
			Kit.Tube( TrimSkin( RopeSwatch ), At( tail ), 0.2f, 3 );
			for ( var k = 1; k < 5; k++ )
			{
				var at = Along( tail, k / 5f ) + new Vector3( 0f, -0.3f, 0f );
				var bow = TrimSkin( AwningSwatch( (first + k) % 4, false ) );
				Pennant( bow, at, at + new Vector3( -4f, 0f, 2.1f ), at + new Vector3( -4f, 0f, -2.1f ) );
				Pennant( bow, at, at + new Vector3( 4f, 0f, -2.1f ), at + new Vector3( 4f, 0f, 2.1f ) );
			}
		}

		/// <summary>
		/// A wall rack at <paramref name="u"/>: one or two surfboards laid on driftwood arms, one over the
		/// other, tipped so their bottoms face the room — all of it within the wall's thickness.
		/// </summary>
		void Rack( float u, float v, float length )
		{
			var count = 1 + Pick( 2 );
			var wood = TrimSkin( DriftwoodSwatch );
			var first = Pick( Decks.Length );
			for ( var k = 0; k < count; k++ )
			{
				var width = Range( 17f, 21f );
				var thick = Range( 2.6f, 3.2f );
				var tip = Range( 28f, 40f ) * MathF.PI / 180f;
				var dir = new Vector3( Next() < 0.5f ? 1f : -1f, 0f, Range( -0.03f, 0.03f ) ).Normal;
				// The deck faces up and out; across the board runs from its outer rail, low, up into the wall.
				var normal = new Vector3( 0f, -MathF.Sin( tip ), MathF.Cos( tip ) );
				var across = new Vector3( 0f, MathF.Cos( tip ), MathF.Sin( tip ) );
				var rotation = Rotation.LookAt( Vector3.Cross( normal, dir ), dir );
				var board = length * Range( 0.9f, 1.05f );
				var centre = new Vector3( u + Range( -3f, 3f ), Face - 1.5f, v + k * Range( 16f, 19f ) ) - across * (width * 0.5f);
				Kit.Ellipsoid( TrimSkin( SurfSwatch( (first + k) % Decks.Length ) ), At( centre ), new Vector3( width * 0.5f, thick * 0.5f, board * 0.5f ), Turn( rotation ), 8, 14 );
				foreach ( var s in new[] { -1f, 1f } )
					Kit.Box( wood, At( centre - normal * (thick * 0.5f + 1.1f) + dir * (s * board * 0.28f) + across * 1.5f ), new Vector3( width + 3f, 2.2f, 2.6f ), Turn( rotation ) );
			}
		}

		/// <summary>
		/// A driftwood shelf <paramref name="length"/> long at <paramref name="u"/>, <paramref name="v"/>, on two
		/// brackets, with the finds of a summer on it: jars of sand, a bottle, a shell, a child's bucket.
		/// </summary>
		void Shelf( float u, float length, float v )
		{
			var wood = TrimSkin( DriftwoodSwatch );
			const float Depth = 12f;
			// Wall decor, built a third over life size to hold the wall at this height (R41).
			const float K = 1.35f;
			Vector2[] Big( params Vector2[] profile ) => Array.ConvertAll( profile, p => p * K );
			var y = Face - Depth * 0.5f + 0.5f;
			Kit.BevelBox( wood, At( new Vector3( u, y, v - 1.2f ) ), new Vector3( length, Depth, 2.4f ), 0.4f, Turn( Rotation.FromYaw( Range( -0.6f, 0.6f ) ) ) );
			foreach ( var s in new[] { -1f, 1f } )
				Kit.Box( wood, At( new Vector3( u + s * (length * 0.5f - Range( 5f, 8f )), Face - 3.5f, v - 7f ) ), new Vector3( 2f, 7f, 9f ), Run );

			for ( var x = u - length * 0.5f + Range( 3f, 6f ); x < u + length * 0.5f - 4f; x += Range( 8f, 13f ) )
			{
				var foot = new Vector3( x, y + Range( -1.5f, 1.5f ), v );
				var turn = Turn( Rotation.FromYaw( Next() * 360f ) );
				switch ( Pick( 4 ) )
				{
					case 0:
						Kit.SmoothLathe( TrimSkin( SandSwatch ), At( foot ), Big( new Vector2( 2.2f, 0f ), new Vector2( 2.6f, 1f ), new Vector2( 2.6f, 6f ), new Vector2( 1.7f, 7.4f ), new Vector2( 1.7f, 8.6f ) ), 8, turn, true, true, 50f );
						break;
					case 1:
						Kit.SmoothLathe( TrimSkin( FloatSwatch( Pick( 3 ) ) ), At( foot ), Big( new Vector2( 2f, 0f ), new Vector2( 2.2f, 0.6f ), new Vector2( 2.2f, 7f ), new Vector2( 0.9f, 9.5f ), new Vector2( 0.8f, 12.5f ) ), 8, turn, true, true, 50f );
						break;
					case 2:
						Kit.Ellipsoid( TrimSkin( ShellSwatch ), At( foot + new Vector3( 0f, 0f, 2.2f * K ) ), new Vector3( Range( 4f, 5f ), Range( 3f, 3.6f ), 2.4f ) * K, turn, 4, 8 );
						Kit.SmoothLathe( TrimSkin( ShellSwatch ), At( foot + new Vector3( 0f, 0f, 3.2f * K ) ), Big( new Vector2( 2.4f, 0f ), new Vector2( 0f, Range( 4f, 6f ) ) ), 8, turn, false, false );
						break;
					default:
						Bucket( foot, Range( 0.68f, 0.8f ), false );
						break;
				}
			}
		}

		/// <summary>Glass fishing floats hung at <paramref name="u"/> from a hook at <paramref name="v"/>, each on its own drop of line, each in a net band.</summary>
		void Floats( float u, float v )
		{
			var count = 3 + Pick( 3 );
			var hook = new Vector3( u, Face - 0.5f, v );
			Kit.Tube( TrimSkin( IronSwatch ), At( new[] { hook + new Vector3( 0f, 1.5f, 0f ), hook + new Vector3( 0f, -2f, 0.8f ) } ), 0.35f, 4 );
			var gap = Range( 10f, 14f );
			for ( var k = 0; k < count; k++ )
			{
				var r = Range( 5f, 7f );
				var centre = new Vector3( u + (k - (count - 1) * 0.5f) * gap, Face - r - 1.2f, v - Range( 12f, 40f ) );
				Kit.Tube( TrimSkin( RopeSwatch ), At( new[] { hook + new Vector3( 0f, -1.6f, 0f ), centre + new Vector3( 0f, 0f, r ) } ), 0.22f, 3 );
				Kit.Ellipsoid( TrimSkin( FloatSwatch( Pick( 3 ) ) ), At( centre ), new Vector3( r, r, r ), Turn( Rotation.From( Range( -20f, 20f ), Next() * 360f, 0f ) ), 5, 10 );
				var band = new Vector3[9];
				for ( var i = 0; i < band.Length; i++ )
				{
					var a = i * MathF.Tau / (band.Length - 1);
					band[i] = centre + new Vector3( MathF.Cos( a ) * (r + 0.15f), MathF.Sin( a ) * (r + 0.15f), 0f );
				}
				Kit.Tube( TrimSkin( RopeSwatch ), At( band ), 0.25f, 3 );
			}
		}

		/// <summary>Two oars crossed flat on the wall at <paramref name="u"/>, <paramref name="v"/>, blades up, lashed where they cross.</summary>
		void CrossedOars( float u, float v )
		{
			var length = Range( 96f, 110f );
			var spread = Range( 26f, 36f );
			var wood = TrimSkin( OarSwatch );
			var centre = new Vector3( u, Face - 1.6f, v );
			for ( var s = -1; s <= 1; s += 2 )
			{
				var a = (s * spread + Range( -3f, 3f )) * MathF.PI / 180f;
				var dir = new Vector3( MathF.Sin( a ), 0f, MathF.Cos( a ) );
				var at = centre + new Vector3( 0f, s < 0 ? 0f : -2.2f, 0f );
				// The blade's painted tip is at its local foot, so it is laid pointing back down the loom.
				var blade = Turn( Rotation.LookAt( Vector3.Cross( ToRoom, -dir ), -dir ) );
				Kit.BevelBox( wood, At( at + dir * (length * 0.35f) ), new Vector3( Range( 8f, 10f ), 1.1f, length * 0.3f ), 0.4f, blade );
				Kit.Tube( wood, At( new[] { at - dir * (length * 0.42f), at + dir * (length * 0.22f) } ), 0.95f, 6 );
				Kit.Tube( wood, At( new[] { at - dir * (length * 0.5f), at - dir * (length * 0.42f) } ), 1.15f, 6 );
			}
			Kit.Ellipsoid( TrimSkin( RopeSwatch ), At( centre + new Vector3( 0f, -1.2f, 0f ) ), new Vector3( 2.4f, 2f, 2.4f ), Run, 3, 6 );
		}

		// ---- Things --------------------------------------------------------------------------

		/// <summary>
		/// A surfboard <paramref name="length"/> long standing on its tail at <paramref name="u"/> and leaning
		/// back on the wall, a little off plumb along it: a flattened ellipsoid (its outline and its thinning
		/// rails are a board's), the deck to the room, or now and then the bottom with its fin.
		/// </summary>
		void Board( float u, float length, float width, int deck )
		{
			var lean = Range( 9f, 16f ) * MathF.PI / 180f;
			var tilt = Range( -5f, 5f ) * MathF.PI / 180f;
			var dir = new Vector3( MathF.Sin( tilt ), MathF.Sin( lean ), MathF.Cos( lean ) ).Normal;
			var thick = Range( 2.6f, 3.4f );
			var foot = new Vector3( u, Face - 1.5f - dir.y * length, Ledge + 0.4f );
			var deckOut = Next() < 0.7f;
			var normal = (ToRoom - dir * Vector3.Dot( ToRoom, dir )).Normal * (deckOut ? 1f : -1f);
			var rotation = Rotation.LookAt( Vector3.Cross( normal, dir ), dir );
			Kit.Ellipsoid( TrimSkin( SurfSwatch( deck ) ), At( foot + dir * (length * 0.5f) ), new Vector3( width * 0.5f, thick * 0.5f, length * 0.5f ), Turn( rotation ), 8, 14 );
			if ( deckOut ) return;

			var bottom = -normal;
			var a = foot + dir * (length * 0.07f) + bottom * (thick * 0.25f);
			var b = foot + dir * (length * 0.17f) + bottom * (thick * 0.3f);
			Pennant( TrimSkin( PlasticSwatch ), a, b, foot + dir * (length * 0.08f) + bottom * (thick * 0.3f + 5f) );
		}

		/// <summary>A beach ball on the ledge at <paramref name="u"/>, against the skirting, turned any way: six gores and white caps.</summary>
		void Ball( float u )
		{
			var r = Range( 6.5f, 8.5f );
			var spin = Rotation.From( Range( -50f, 50f ), Next() * 360f, Range( -50f, 50f ) );
			Kit.Ellipsoid( TrimSkin( BallSwatch ), At( new Vector3( u, MathF.Max( Skirt - r - 0.3f, Front + r + 0.5f ), Ledge + r * 0.96f ) ), new Vector3( r, r, r * 0.96f ), Turn( spin ), 6, 12 );
		}

		/// <summary>Sand blown up against the skirting between <paramref name="from"/> and <paramref name="to"/>: one to three low heaps.</summary>
		void Sand( float from, float to )
		{
			var u = from + Range( 0f, (to - from) * 0.5f );
			for ( var k = 1 + Pick( 3 ); k > 0 && u < to - 8f; k-- )
			{
				var r = new Vector3( Range( 9f, 20f ), Range( 6f, 11f ), Range( 2.5f, 6f ) );
				var centre = new Vector3( MathF.Min( u + r.x * 0.5f, to - r.x * 0.5f ), MathF.Max( Face - r.y * 0.6f, Front + r.y + 0.5f ), Ledge - 0.5f );
				Kit.Dome( TrimSkin( SandSwatch ), At( centre ), r, Turn( Rotation.FromYaw( Range( -10f, 10f ) ) ), 3, 10 );
				u += r.x * Range( 0.9f, 1.5f );
			}
		}

		/// <summary>
		/// A life ring hung on a peg at <paramref name="u"/>, <paramref name="v"/>: a torus in carrot and white
		/// bands (turned any way round), its foot standing a little off the wall, and a grab line round it
		/// sagging between four clips.
		/// </summary>
		void LifeRing( float u, float v )
		{
			var major = Range( 9.5f, 11f );
			var minor = Range( 2.8f, 3.3f );
			var y = Face - minor - 0.8f;
			var lean = Range( 2f, 4f );
			var start = Next() * MathF.Tau;
			const int Steps = 16;
			Vector3 On( float a, float r ) => new( u + MathF.Cos( a ) * r, y - lean * (1f - MathF.Sin( a )) * 0.5f, v + MathF.Sin( a ) * r );

			var ring = new Vector3[Steps + 1];
			for ( var i = 0; i <= Steps; i++ ) ring[i] = On( start + i * MathF.Tau / Steps, major );
			Kit.Tube( TrimSkin( LifeRingSwatch ), At( ring ), minor, 8 );

			var line = new Vector3[25];
			for ( var i = 0; i < line.Length; i++ )
			{
				var t = i / (line.Length - 1f);
				var quarter = t * 4f - MathF.Floor( t * 4f );
				line[i] = On( start + MathF.PI * 0.25f + t * MathF.Tau, major + minor + 0.2f + 1.6f * 4f * quarter * (1f - quarter) ) + new Vector3( 0f, -minor * 0.3f, 0f );
			}
			Kit.Tube( TrimSkin( RopeSwatch ), At( line ), 0.35f, 3 );
			Kit.Tube( Paint, At( new[] { new Vector3( u, Face + 1f, v + major - minor ), new Vector3( u, y - minor - 1.2f, v + major - minor + 1.2f ) } ), 0.6f, 5 );
		}

		/// <summary>A coil of rope on the ledge: three turns, each a little smaller, slumped over the one below.</summary>
		void Coil( Vector3 at )
		{
			var r = Range( 6.5f, 8f );
			for ( var turn = 0; turn < 3; turn++ )
			{
				var path = new Vector3[13];
				var start = Next() * MathF.Tau;
				var radius = r * (1f - turn * 0.12f);
				for ( var i = 0; i < path.Length; i++ )
				{
					var a = start + i * MathF.Tau / (path.Length - 1);
					path[i] = at + new Vector3( MathF.Cos( a ) * radius, MathF.Sin( a ) * radius * 0.8f, 0.9f + turn * 1.5f + MathF.Sin( a * 2f ) * 0.3f );
				}
				Kit.Tube( TrimSkin( RopeSwatch ), At( path ), 0.95f, 5 );
			}
		}

		/// <summary>A bucket (open, with its inside) at <paramref name="foot"/>, a wire handle, and a spade stuck in it.</summary>
		void Bucket( Vector3 foot, float s, bool spade )
		{
			var tilt = Turn( Rotation.From( Range( -3f, 3f ), Next() * 360f, Range( -3f, 3f ) ) );
			Kit.SmoothLathe( TrimSkin( BucketSwatch( Pick( Plastics.Length ) ) ), At( foot ), new[]
			{
				new Vector2( 3.6f, 0f ) * s, new Vector2( 5.1f, 8.2f ) * s, new Vector2( 5.4f, 8.6f ) * s, new Vector2( 4.8f, 8.5f ) * s,
				new Vector2( 3.3f, 1f ) * s, new Vector2( 0f, 1f ) * s,
			}, 10, tilt, true, false, 50f );
			var handle = new Vector3[6];
			var swing = Range( -0.5f, 0.5f );
			for ( var i = 0; i < handle.Length; i++ )
			{
				var a = MathF.PI * i / (handle.Length - 1);
				handle[i] = foot + new Vector3( MathF.Cos( a ) * 5.3f * s, MathF.Sin( a ) * swing * 4f * s, (8.3f + MathF.Sin( a ) * 4.5f * (1f - MathF.Abs( swing ) * 0.6f)) * s );
			}
			Kit.Tube( TrimSkin( IronSwatch ), At( handle ), 0.18f, 3 );
			if ( !spade ) return;

			var lean = new Vector3( Range( -0.35f, 0.35f ), Range( 0.1f, 0.3f ), 1f ).Normal;
			var top = foot + new Vector3( 0f, 0f, 2f * s ) + lean * 16f * s;
			var paint = TrimSkin( BucketSwatch( Pick( Plastics.Length ) ) );
			Kit.Tube( paint, At( new[] { foot + new Vector3( 0f, 0f, 2f * s ), top } ), 0.55f * s, 5 );
			Kit.Box( paint, At( top + lean * 1.2f ), new Vector3( 4f, 1.2f, 1.2f ) * s, Turn( Rotation.LookAt( new Vector3( 1f, 0f, 0f ), lean ) ) );
		}

		/// <summary>
		/// A varnished oar standing on its blade at <paramref name="u"/>, leaning back on the wall and
		/// <paramref name="tilt"/> degrees off plumb along it: a round loom, a flat blade with a painted tip, a grip.
		/// </summary>
		void Oar( float u, float tilt )
		{
			var length = Range( 88f, 104f );
			var lean = Range( 8f, 13f ) * MathF.PI / 180f;
			var t = tilt * MathF.PI / 180f;
			var dir = new Vector3( MathF.Sin( t ), MathF.Sin( lean ), MathF.Cos( lean ) ).Normal;
			var foot = new Vector3( u, Face - 2.5f - dir.y * length, Ledge + 0.3f );
			var wood = TrimSkin( OarSwatch );
			var flat = Turn( Rotation.LookAt( Vector3.Cross( dir, new Vector3( 0f, 1f, 0f ) ).Normal, dir ) );
			Kit.BevelBox( wood, At( foot + dir * (length * 0.15f) ), new Vector3( Range( 8f, 10f ), 1.1f, length * 0.3f ), 0.4f, flat );
			Kit.Tube( wood, At( new[] { foot + dir * (length * 0.27f), foot + dir * (length * 0.92f) } ), 0.95f, 6 );
			Kit.Tube( wood, At( new[] { foot + dir * (length * 0.9f), foot + dir * length } ), 1.15f, 6 );
		}

		/// <summary>
		/// A deck chair folded flat and leaning on the wall at <paramref name="u"/>: the outer legs, the inner
		/// frame folded inside them, the cross rails, and its striped canvas lying slack along the frame.
		/// </summary>
		void DeckChair( float u )
		{
			var length = Range( 46f, 54f );
			var width = Range( 20f, 23f );
			var lean = Range( 12f, 18f ) * MathF.PI / 180f;
			var dir = new Vector3( Range( -0.04f, 0.04f ), MathF.Sin( lean ), MathF.Cos( lean ) ).Normal;
			var rotation = Rotation.LookAt( new Vector3( 1f, 0f, 0f ), dir );
			var foot = new Vector3( u, Face - 2.5f - dir.y * length, Ledge );
			Vector3 P( float x, float y, float z ) => foot + rotation * new Vector3( x, y, z );
			var turn = Turn( rotation );
			var wood = TrimSkin( OarSwatch );

			for ( var s = -1; s <= 1; s += 2 )
			{
				Kit.Box( wood, At( P( s * width * 0.5f, 0f, length * 0.5f ) ), new Vector3( 1.8f, 1.3f, length ), turn );
				Kit.Box( wood, At( P( s * (width * 0.5f - 2.2f), -1.3f, length * 0.45f + 1f ) ), new Vector3( 1.6f, 1.2f, length * 0.88f ), turn );
			}
			Kit.Box( wood, At( P( 0f, 0f, length - 1.5f ) ), new Vector3( width + 1.8f, 1.3f, 1.6f ), turn );
			Kit.Box( wood, At( P( 0f, 0f, 3f ) ), new Vector3( width + 1.8f, 1.3f, 1.6f ), turn );
			Kit.Box( wood, At( P( 0f, -1.3f, length * 0.62f ) ), new Vector3( width - 2.6f, 1.2f, 1.4f ), turn );
			Kit.Box( TrimSkin( DeckCanvasSwatch( Pick( 2 ) ) ), At( P( 0f, -2.3f, length * 0.55f ) ), new Vector3( width - 6f, 0.5f, length * 0.68f ), turn );
		}

		/// <summary>A towel hung from a peg at <paramref name="u"/>, <paramref name="hook"/>, swinging a little off plumb.</summary>
		void Towel( float u, float hook )
		{
			var length = Range( 34f, 44f );
			var width = Range( 15f, 20f );
			Kit.Tube( Paint, At( new[] { new Vector3( u, Face + 1f, hook ), new Vector3( u, Face - 4.5f, hook + 1f ) } ), 0.5f, 4 );
			Kit.BevelBox( TrimSkin( TowelSwatch( Pick( 3 ) ) ), At( new Vector3( u + Range( -1.5f, 1.5f ), Face - 2.6f, hook - length * 0.5f + 1.5f ) ), new Vector3( width, 1.6f, length ), 0.6f,
				Turn( Rotation.FromPitch( Range( -4f, 4f ) ) ) );
		}

		/// <summary>A straw sun hat hung flat on a peg at <paramref name="u"/>, <paramref name="v"/>, a coral ribbon round its crown.</summary>
		void Hat( float u, float v )
		{
			var facing = Turn( Rotation.FromToRotation( Vector3.Up, ToRoom ) * Rotation.FromYaw( Next() * 360f ) );
			var at = new Vector3( u, Face - 0.8f, v - 6f );
			Kit.Tube( Paint, At( new[] { new Vector3( u, Face + 1f, v ), new Vector3( u, Face - 4f, v + 0.8f ) } ), 0.5f, 4 );
			Kit.SmoothLathe( TrimSkin( StrawSwatch ), At( at ), new[]
			{
				new Vector2( 9.5f, 0f ), new Vector2( 9.7f, 0.5f ), new Vector2( 6f, 1.2f ), new Vector2( 4.8f, 1.6f ), new Vector2( 4.6f, 4.8f ), new Vector2( 3.6f, 6.3f ), new Vector2( 0f, 6.6f ),
			}, 12, facing, false, false, 40f );
			Kit.SmoothLathe( TrimSkin( AwningSwatch( 1, false ) ), At( at ), new[] { new Vector2( 4.85f, 1.7f ), new Vector2( 4.75f, 3.2f ) }, 12, facing, false, false );
		}

		/// <summary>A cool box on the ledge at <paramref name="u"/>: a pastel body, a white lid, a carrying handle.</summary>
		void CoolBox( float u )
		{
			var size = new Vector3( Range( 18f, 23f ), Range( 12f, 14f ), Range( 12f, 15f ) );
			var turn = Turn( Rotation.FromYaw( Range( -6f, 6f ) ) );
			var centre = new Vector3( u, Skirt - size.y * 0.5f - 0.5f, Ledge + (size.z - 2.5f) * 0.5f );
			Kit.BevelBox( TrimSkin( BucketSwatch( Pick( Plastics.Length ) ) ), At( centre ), size.WithZ( size.z - 2.5f ), 1.2f, turn );
			Kit.BevelBox( TrimSkin( PlasticSwatch ), At( centre + new Vector3( 0f, 0f, size.z * 0.5f ) ), new Vector3( size.x + 0.8f, size.y + 0.8f, 2.6f ), 1f, turn );
		}

		/// <summary>
		/// A driftwood bench <paramref name="length"/> long at <paramref name="u"/>: a thick plank on two
		/// slab legs, a stack of folded towels on it and now and then a bucket.
		/// </summary>
		void Bench( float u, float length )
		{
			var seat = Range( 16f, 19f );
			var depth = Range( 12f, 15f );
			var middle = Skirt - 0.5f - depth * 0.5f;
			var wood = TrimSkin( DriftwoodSwatch );
			Kit.BevelBox( wood, At( new Vector3( u, middle, seat - 1.5f ) ), new Vector3( length, depth, 3f ), 0.5f, Turn( Rotation.FromYaw( Range( -0.8f, 0.8f ) ) ) );
			var legs = length * 0.5f - Range( 6f, 10f );
			for ( var s = -1; s <= 1; s += 2 )
				Kit.Box( wood, At( new Vector3( u + s * legs, middle, (Ledge + seat - 3f) * 0.5f ) ), new Vector3( 3f, depth - 2f, seat - 3f - Ledge ), Run );

			var at = u + Range( -0.35f, 0.35f ) * length;
			var h = seat;
			for ( var k = 1 + Pick( 3 ); k > 0; k-- )
			{
				var t = Range( 2f, 2.8f );
				Kit.BevelBox( TrimSkin( TowelSwatch( Pick( 3 ) ) ), At( new Vector3( at + Range( -1f, 1f ), middle, h + t * 0.5f ) ), new Vector3( Range( 14f, 18f ), Range( 9f, 11f ), t ), 0.9f,
					Turn( Rotation.FromYaw( Range( -8f, 8f ) ) ) );
				h += t;
			}
			if ( Next() < 0.4f ) Bucket( new Vector3( at + (at > u ? -1f : 1f) * length * 0.3f, middle, seat ), 0.8f, false );
		}
	}
}
