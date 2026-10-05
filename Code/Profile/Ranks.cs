using System;

/// <summary>The six rank tiers, Bronze to Master. Each but Master has three divisions.</summary>
public enum RankTier
{
	Bronze = 0,
	Silver = 1,
	Gold = 2,
	Platinum = 3,
	Diamond = 4,
	Master = 5
}

/// <summary>
/// Rank points → rank, and everything a rank looks like. Ranks are earned, never chosen; what a
/// higher rank buys is purely visual (KONTEKST.md: "każda wyższa ranga ma dawać benefity wizualne").
///
///   Bronze   — emblem on your nameplate
///   Silver   — + name in your rank colour
///   Gold     — + a glowing ring under your feet
///   Platinum — + three orbiting sparks
///   Diamond  — + a trail of fading cubes while you sprint
///   Master   — + a crown, and your name in gold everywhere it appears
/// </summary>
public static class Ranks
{
	/// <summary>RP needed for each of the 16 steps, lowest first. Index = step.</summary>
	static int Threshold( int step ) => step switch
	{
		0 => 0,
		1 => 100,
		2 => 200,
		3 => 300,
		4 => 425,
		5 => 550,
		6 => 700,
		7 => 850,
		8 => 1000,
		9 => 1200,
		10 => 1400,
		11 => 1600,
		12 => 1850,
		13 => 2100,
		14 => 2350,
		_ => 2650,
	};

	public const int StepCount = 16;

	/// <summary>0..15 — 0 is Bronze I, 15 is Master.</summary>
	public static int Step( int rankPoints )
	{
		for ( var step = StepCount - 1; step > 0; step-- )
		{
			if ( rankPoints >= Threshold( step ) ) return step;
		}

		return 0;
	}

	public static RankTier Tier( int rankPoints ) => TierOfStep( Step( rankPoints ) );

	public static RankTier TierOfStep( int step ) => step >= 15 ? RankTier.Master : (RankTier)(step / 3);

	/// <summary>1, 2 or 3 — the division within a tier. Master has only one.</summary>
	public static int Division( int step ) => step >= 15 ? 1 : step % 3 + 1;

	public static string Name( int rankPoints ) => NameOfStep( Step( rankPoints ) );

	public static string NameOfStep( int step )
	{
		var tier = TierOfStep( step );
		if ( tier == RankTier.Master ) return "Master";

		var numeral = Division( step ) switch { 1 => "I", 2 => "II", _ => "III" };
		return $"{tier} {numeral}";
	}

	/// <summary>Kenney emblem for a step, sliced from the ranks pack into Assets/ui/kenney/ranks.</summary>
	public static string EmblemOfStep( int step )
	{
		var tier = TierOfStep( step ).ToString().ToLowerInvariant();
		return $"/ui/kenney/ranks/{tier}_{Division( step )}.svg";
	}

	public static string Emblem( int rankPoints ) => EmblemOfStep( Step( rankPoints ) );

	/// <summary>RP at the bottom of this step, and the RP needed for the next one (same if maxed).</summary>
	public static (int Floor, int Next) Bounds( int rankPoints )
	{
		var step = Step( rankPoints );
		var floor = Threshold( step );
		var next = step >= StepCount - 1 ? floor : Threshold( step + 1 );
		return (floor, next);
	}

	/// <summary>0..1 progress through the current step, 1 at Master.</summary>
	public static float Progress( int rankPoints )
	{
		var (floor, next) = Bounds( rankPoints );
		if ( next <= floor ) return 1f;
		return ((rankPoints - floor) / (float)(next - floor)).Clamp( 0f, 1f );
	}

	/// <summary>Identity colour per tier — nameplates, the ring, the sparks. All channels ≤ 1 (Tint wraps).</summary>
	public static Color TierColor( RankTier tier ) => tier switch
	{
		RankTier.Bronze => new Color( 0.80f, 0.52f, 0.34f ),
		RankTier.Silver => new Color( 0.70f, 0.80f, 0.92f ),
		RankTier.Gold => new Color( 1.00f, 0.76f, 0.26f ),
		RankTier.Platinum => new Color( 0.42f, 0.88f, 0.84f ),
		RankTier.Diamond => new Color( 0.66f, 0.56f, 1.00f ),
		_ => new Color( 1.00f, 0.84f, 0.30f ),
	};

	/// <summary>Same colours as CSS hex, for Razor inline styles.</summary>
	public static string TierHex( RankTier tier )
	{
		var c = TierColor( tier );
		return $"#{(int)(c.r * 255):X2}{(int)(c.g * 255):X2}{(int)(c.b * 255):X2}";
	}

	/// <summary>The perk each tier adds, for the profile screen's rank ladder.</summary>
	public static string Perk( RankTier tier ) => tier switch
	{
		RankTier.Bronze => "Rank emblem on your nameplate",
		RankTier.Silver => "Your name in your rank colour",
		RankTier.Gold => "A glowing ring under your feet",
		RankTier.Platinum => "Three sparks orbit you",
		RankTier.Diamond => "A trail of cubes when you sprint",
		_ => "A crown and a golden name",
	};

	/// <summary>
	/// RP change for finishing <paramref name="place"/> (1 = winner) of <paramref name="players"/>,
	/// plus a share of the round's points. Placement runs +40 for first to −30 for last, scaled
	/// linearly between; points add up to +30 more, so an aggressive player who falls early still
	/// loses less than a passive one. Solo rounds never reach here.
	/// </summary>
	public static int Delta( int place, int players, int points )
	{
		var span = players - 1;
		var t = span <= 0 ? 0f : (place - 1) / (float)span; // 0 for first, 1 for last
		var placement = MathX.Lerp( 40f, -30f, t );
		var bonus = MathX.Clamp( points / 25f, 0f, 30f );
		return (int)MathF.Round( placement + bonus );
	}
}
