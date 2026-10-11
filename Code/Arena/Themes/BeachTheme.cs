using System;
using System.Collections.Generic;

/// <summary>
/// Floor 3 (index 2): a beach hut. Underfoot, blocks of sand with wind ripples — steep on the lee
/// side, darker heavy grains in the troughs — shell grit, a starfish, a scallop. Overhead (the
/// undersides of the winter floor), striped sailcloth.
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

	/// <summary>Grit on the sand: shell cream, slate, pink shell, driftwood brown, basalt.</summary>
	static readonly uint[] Grit = { 0xEFE3D0, 0xA9A39A, 0xE3B3A3, 0x9C7B5B, 0x5A5550 };

	public override void Dress( FloorContext room )
	{
		// Room dressing arrives in Task 6.
	}

	protected override Material PaintFloor()
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
			painter.Polygon( SlabOutline( min, cell, 5f + Next() * 5f, 4f, 12f + Next() * 18f, Seed + c * 101 ), sand, height: 1f, bevel: 18f, roughness: 0.9f );

			// Grit: shell fragments and pebbles, each its own size, outline and colour. Painted before
			// the ripples are added, so they ride the sand instead of sinking into the crests.
			var bits = 22 + random.Next( 20 );
			for ( var b = 0; b < bits; b++ )
			{
				var at = min + new Vector2( 30f + Next() * 452f, 30f + Next() * 452f );
				var size = 1.2f + Next() * Next() * 4f;
				var half = new Vector2( size, size * (0.5f + Next() * 0.5f) );
				var color = Mix( Palette.Hex( Grit[random.Next( Grit.Length )] ), Sand, 0.25f );
				painter.Polygon( Organic( at, half, 0.7f, Seed + c * 61 + b, Next() * MathF.Tau, 10 ), color,
					height: 1.1f + Next() * 0.12f, bevel: 1.2f, roughness: 0.45f + Next() * 0.35f );
			}

			if ( c == 1 ) PaintStarfish( painter, min + new Vector2( 150f + Next() * 210f, 150f + Next() * 210f ), 44f + Next() * 10f, Next() * MathF.Tau, random );
			if ( c == 2 ) PaintScallop( painter, min + new Vector2( 160f + Next() * 190f, 180f + Next() * 170f ), 38f + Next() * 10f, Next() * MathF.Tau, random );
		}

		PaintRipples( painter, random );

		// Damp patches, darker and less matte, wandering across tiles.
		painter.Blend( Ramp( painter.Noise( 3, Seed + 30, 4 ), 0.6f, 0.85f, 0.35f ), Damp, roughness: 0.75f );

		painter.HeightNoise( 0.8f, 4, Seed + 31, 4 );
		painter.HeightNoise( 0.08f, 128, Seed + 32, 2 );
		painter.Mottle( 0.05f, 4, Seed + 33, 6 );
		painter.RoughnessNoise( 0.05f, 8, Seed + 34, 3 );
		painter.Cavity( 0.3f, 8 );
		painter.Grain( 0.02f, Seed + 35 );
		return painter.ToMaterial( "arena_2_floor", Painter.Matte, 6f );
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
					var profile = t < 0.72f ? t / 0.72f : (1f - t) / 0.28f;
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
		painter.Blend( troughs, SandShade );
		painter.Blend( crests, SandLight );
	}

	/// <summary>A five-armed starfish, every arm its own length and curl, studded with tubercles and dusted with sand.</summary>
	static void PaintStarfish( Painter painter, Vector2 centre, float radius, float rotation, Random random )
	{
		float Next() => (float)random.NextDouble();
		Vector2 Toward( float angle, float distance ) => centre + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * distance;

		var points = new List<Vector2>();
		var arms = new (float angle, float length, float curl)[5];
		for ( var a = 0; a < 5; a++ )
		{
			var angle = rotation + a * MathF.Tau / 5f + (Next() - 0.5f) * 0.18f;
			var length = radius * (0.82f + Next() * 0.3f);
			var curl = (Next() - 0.5f) * 0.3f;
			arms[a] = (angle, length, curl);

			points.Add( Toward( angle - MathF.PI / 5f, radius * (0.34f + Next() * 0.06f) ) );
			points.Add( Toward( angle - 0.24f + curl * 0.4f, length * 0.6f ) );
			points.Add( Toward( angle - 0.08f + curl, length * 0.94f ) );
			points.Add( Toward( angle + curl * 1.1f, length ) );
			points.Add( Toward( angle + 0.08f + curl, length * 0.94f ) );
			points.Add( Toward( angle + 0.24f + curl * 0.4f, length * 0.6f ) );
		}

		// Sun-bleached: a dried starfish fades toward the sand it lies on.
		painter.Polygon( points.ToArray(), Mix( Coral, Sand, 0.3f ), height: 1.4f, bevel: 12f, roughness: 0.7f );
		painter.Circle( centre.x, centre.y, radius * 0.16f, CoralLight, 0.5f );

		foreach ( var (angle, length, curl) in arms )
		{
			var studs = 6 + random.Next( 4 );
			for ( var s = 0; s < studs; s++ )
			{
				var t = 0.12f + s / (float)studs * 0.75f;
				var at = Toward( angle + curl * t + (Next() - 0.5f) * 0.12f, length * t );
				var r = 1.4f + Next() * 1.2f;
				painter.Circle( at.x, at.y, r, CoralLight, 0.85f, height: 1.48f, bevel: r, roughness: 0.55f );
			}
		}

		for ( var d = 0; d < 18; d++ )
		{
			var at = Toward( Next() * MathF.Tau, Next() * radius * 0.8f );
			painter.Circle( at.x, at.y, 1f + Next() * 2.5f, Sand, 0.55f );
		}
	}

	/// <summary>A scallop: fan of ribs with a scalloped rim, two ears at the hinge, pink growth bands.</summary>
	static void PaintScallop( Painter painter, Vector2 hinge, float radius, float heading, Random random )
	{
		var shapeSeed = random.Next();
		painter.Polygon( ScallopOutline( hinge, radius, heading, shapeSeed ), ShellCream, height: 1.3f, bevel: 10f, roughness: 0.45f );
		painter.Polygon( ScallopOutline( hinge, radius * 0.74f, heading, shapeSeed ), Coral, alpha: 0.28f );
		painter.Polygon( ScallopOutline( hinge, radius * 0.46f, heading, shapeSeed ), Coral, alpha: 0.22f );

		const int Ribs = 13;
		for ( var r = 0; r <= Ribs; r++ )
		{
			var angle = heading + (r / (float)Ribs - 0.5f) * 2.1f;
			var direction = new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) );
			painter.Line( hinge + direction * radius * 0.16f, hinge + direction * radius * 0.94f, 2f, ShellRib, 0.5f, 1.24f );
		}
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
	/// patches, stitched panel seams, and one patched repair.
	/// </summary>
	protected override Material PaintCeiling()
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

			// A patched repair on one underside, sewn on slightly askew.
			if ( c == 3 )
			{
				var px = min.x + 60f + Next() * 100f; var py = min.y + 50f + Next() * 110f;
				var pw = 26f + Next() * 14f; var ph = 20f + Next() * 10f;
				var patchAngle = (Next() - 0.5f) * 8f;
				painter.Rect( px, py, pw, ph, Mix( Canvas, Thread, 0.15f ), radius: 1f, alpha: 0.45f, height: 0.5f, bevel: 2f, roughness: 0.9f, angle: patchAngle );
				var turn = patchAngle * MathF.PI / 180f;
				var across = new Vector2( MathF.Cos( turn ), MathF.Sin( turn ) );
				var down = new Vector2( -across.y, across.x );
				var middle = new Vector2( px + pw * 0.5f, py + ph * 0.5f );
				var corners = new[]
				{
					middle - across * (pw * 0.5f - 2f) - down * (ph * 0.5f - 2f), middle + across * (pw * 0.5f - 2f) - down * (ph * 0.5f - 2f),
					middle + across * (pw * 0.5f - 2f) + down * (ph * 0.5f - 2f), middle - across * (pw * 0.5f - 2f) + down * (ph * 0.5f - 2f),
				};
				for ( var k = 0; k < 4; k++ )
				{
					var from = corners[k]; var to = corners[(k + 1) % 4];
					var length = (to - from).Length; var direction = (to - from) / length;
					for ( var d = 1f; d < length - 2f; d += 5f )
						painter.Line( from + direction * d, from + direction * (d + 2.4f), 1f, Thread, 0.6f, 0.75f );
				}
			}
		}

		// Sun-fading: the coral bleaches toward the canvas in broad patches.
		painter.Blend( Ramp( painter.Noise( 3, Seed + 520, 4 ), 0.5f, 0.85f, 0.3f ), Canvas );
		painter.HeightNoise( 0.6f, 3, Seed + 521, 4 );
		painter.Mottle( 0.05f, 3, Seed + 522, 5 );
		painter.Cavity( 0.3f, 3 );
		painter.Grain( 0.015f, Seed + 523 );
		return painter.ToMaterial( "arena_2_ceiling", Painter.Matte, 3f );
	}
}
