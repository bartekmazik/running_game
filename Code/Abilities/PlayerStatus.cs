/// <summary>Timed effects an ability can put on a player. Values travel on the wire; don't renumber.</summary>
public enum StatusKind
{
	DoubleJump = 0,
	FeatherFall = 1,
	Ghost = 2,
	Turbo = 3,
	Iron = 4,
	Shield = 5,
	Giant = 6,
	Glue = 7,
	Ice = 8,
	Confused = 9,
	Blackout = 10,
	HeavyBoots = 11,
	Stunned = 12,
	Vortex = 13,
}

/// <summary>
/// Every timed effect on one player, on every machine.
///
/// The host decides (CLAUDE.md: host authority); <see cref="Apply"/> broadcasts, and each machine
/// then runs its own clock — the same pattern RoundManager uses for its countdown. Nothing streams
/// per tick. A late joiner misses effects already running, which is harmless: none lasts long.
///
/// The owner reads these in TntPlayer.Move; every machine reads them for presentation.
/// </summary>
public sealed class PlayerStatus : Component
{
	const int KindCount = (int)StatusKind.Vortex + 1;

	readonly TimeUntil[] _until = new TimeUntil[KindCount];
	readonly bool[] _active = new bool[KindCount];

	/// <summary>Where a Vortex pulls towards, set alongside the status.</summary>
	public Vector3 VortexCentre { get; private set; }

	/// <summary>Bumped on every change, so the HUD can fold it into a BuildHash.</summary>
	public int Revision { get; private set; }

	public bool Has( StatusKind kind )
	{
		var i = (int)kind;
		return _active[i] && !_until[i];
	}

	/// <summary>Retire expired statuses once, here, so reading one never changes anything.</summary>
	protected override void OnUpdate()
	{
		for ( var i = 0; i < KindCount; i++ )
		{
			if ( !_active[i] || !_until[i] ) continue;

			_active[i] = false;
			Revision++;
		}
	}

	/// <summary>Seconds left on a status, 0 if it isn't active.</summary>
	public float Remaining( StatusKind kind ) => Has( kind ) ? _until[(int)kind].Relative : 0f;

	/// <summary>
	/// Weight in a collision (PlayerImpacts): the brief says the weaker hit goes down, and these are
	/// the two abilities that make you the heavier party.
	/// </summary>
	public float Mass => Has( StatusKind.Iron ) ? 3f : Has( StatusKind.Giant ) ? 2f : 1f;

	/// <summary>Iron Body makes you impossible to floor. (The post-ragdoll grace lives in PlayerImpacts.)</summary>
	public bool KnockdownImmune => Has( StatusKind.Iron );

	public float SpeedScale
	{
		get
		{
			var scale = 1f;
			if ( Has( StatusKind.Turbo ) ) scale *= 1.4f;
			if ( Has( StatusKind.Glue ) ) scale *= 0.5f;
			if ( Has( StatusKind.Giant ) ) scale *= 1.1f;
			return scale;
		}
	}

	/// <summary>Host only. Starts (or refreshes) a status on every machine.</summary>
	public void Apply( StatusKind kind, float seconds ) => Apply( kind, seconds, Vector3.Zero );

	/// <summary>Host only. <paramref name="point"/> is the Vortex centre; ignored otherwise.</summary>
	public void Apply( StatusKind kind, float seconds, Vector3 point )
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;
		BroadcastApply( kind, seconds, point );
	}

	/// <summary>Host only. Ends a status early everywhere — a Bubble Shield popping, say.</summary>
	public void Clear( StatusKind kind )
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;
		BroadcastClear( kind );
	}

	/// <summary>Host only. Everything off — a respawn or an elimination.</summary>
	public void ClearAll()
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;
		BroadcastClearAll();
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void BroadcastApply( StatusKind kind, float seconds, Vector3 point )
	{
		var i = (int)kind;
		_active[i] = true;
		_until[i] = seconds;

		if ( kind == StatusKind.Vortex ) VortexCentre = point;

		Revision++;
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void BroadcastClear( StatusKind kind )
	{
		_active[(int)kind] = false;
		Revision++;
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void BroadcastClearAll()
	{
		for ( var i = 0; i < KindCount; i++ ) _active[i] = false;
		Revision++;
	}

	/// <summary>The active statuses, for the HUD's chips and the nameplate's emote.</summary>
	public IEnumerable<StatusKind> Current
	{
		get
		{
			for ( var i = 0; i < KindCount; i++ )
			{
				if ( Has( (StatusKind)i ) ) yield return (StatusKind)i;
			}
		}
	}

	/// <summary>A short label for each status, shown to the affected player.</summary>
	public static string Label( StatusKind kind ) => kind switch
	{
		StatusKind.DoubleJump => "Double jump",
		StatusKind.FeatherFall => "Feather fall",
		StatusKind.Ghost => "Ghost step",
		StatusKind.Turbo => "Turbo",
		StatusKind.Iron => "Iron body",
		StatusKind.Shield => "Shield",
		StatusKind.Giant => "Giant",
		StatusKind.Glue => "Glued",
		StatusKind.Ice => "Iced",
		StatusKind.Confused => "Confused",
		StatusKind.Blackout => "Blackout",
		StatusKind.HeavyBoots => "Heavy boots",
		StatusKind.Stunned => "Stunned",
		StatusKind.Vortex => "Vortex",
		_ => ""
	};

	/// <summary>The status shows the icon of the ability that causes it.</summary>
	public static string Icon( StatusKind kind ) => AbilityCatalog.Get( kind switch
	{
		StatusKind.DoubleJump => AbilityId.DoubleJump,
		StatusKind.FeatherFall => AbilityId.FeatherFall,
		StatusKind.Ghost => AbilityId.GhostStep,
		StatusKind.Turbo => AbilityId.Turbo,
		StatusKind.Iron => AbilityId.IronBody,
		StatusKind.Shield => AbilityId.BubbleShield,
		StatusKind.Giant => AbilityId.Giant,
		StatusKind.Glue => AbilityId.Glue,
		StatusKind.Ice => AbilityId.Ice,
		StatusKind.Confused => AbilityId.Confuse,
		StatusKind.Blackout => AbilityId.Blackout,
		StatusKind.HeavyBoots => AbilityId.HeavyBoots,
		StatusKind.Stunned => AbilityId.Stun,
		StatusKind.Vortex => AbilityId.Vortex,
		_ => AbilityId.None
	} ).Icon;

	/// <summary>Debuffs read as bad news in the HUD; everything else is a buff.</summary>
	public static bool IsDebuff( StatusKind kind ) => kind is StatusKind.Glue or StatusKind.Ice
		or StatusKind.Confused or StatusKind.Blackout or StatusKind.HeavyBoots or StatusKind.Stunned
		or StatusKind.Vortex;
}
