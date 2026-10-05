/// <summary>
/// The two ability slots on one player: the loadout (owner-synced from the owner's profile),
/// input on attack1 / attack2, and cooldowns.
///
/// Casting is a request, never an action: the owner asks, the host validates and runs it through
/// <see cref="AbilitySystem"/>, then tells the owner whether it went off. The host keeps its own
/// cooldown clock as the truth; the owner's copy is only for the HUD and to avoid pointless asks.
/// </summary>
public sealed class PlayerAbilities : Component
{
	public const int SlotCount = 2;

	/// <summary>Input action per slot. CLAUDE.md opened attack1/attack2 for exactly this.</summary>
	static string ActionFor( int slot ) => slot == 0 ? "attack1" : "attack2";

	[Sync] public AbilityId First { get; set; } = AbilityCatalog.DefaultFirst;
	[Sync] public AbilityId Second { get; set; } = AbilityCatalog.DefaultSecond;

	public AbilityId InSlot( int slot ) => slot == 0 ? First : Second;

	/// <summary>Owner: when each slot is ready again, as the host last confirmed.</summary>
	readonly TimeUntil[] _ready = new TimeUntil[SlotCount];
	readonly float[] _cooldown = new float[SlotCount];

	/// <summary>Host: the authoritative cooldowns.</summary>
	readonly TimeUntil[] _hostReady = new TimeUntil[SlotCount];

	/// <summary>Owner: latched in OnUpdate, like jump — Pressed is true for exactly one frame.</summary>
	readonly bool[] _queued = new bool[SlotCount];

	/// <summary>Owner: a request is in flight, so a held mouse button doesn't spam the host.</summary>
	readonly TimeUntil[] _awaiting = new TimeUntil[SlotCount];

	/// <summary>Owner: when a slot came off cooldown, for the HUD's "ready" flash.</summary>
	readonly bool[] _wasCooling = new bool[SlotCount];

	/// <summary>Bumped on every change the HUD shows.</summary>
	public int Revision { get; private set; }

	TntPlayer _player;
	RoundManager _round;
	ProfileStore _profile;

	protected override void OnAwake()
	{
		_player = GetComponent<TntPlayer>();
		_round = Scene.Get<RoundManager>();
		_profile = Scene.Get<ProfileStore>();
	}

	/// <summary>Seconds until a slot is ready, 0 if ready. Owner's view.</summary>
	public float CooldownRemaining( int slot ) => _ready[slot] ? 0f : _ready[slot].Relative;

	/// <summary>0..1, 1 = ready. Owner's view.</summary>
	public float ReadyFraction( int slot )
	{
		if ( _cooldown[slot] <= 0f || _ready[slot] ) return 1f;
		return 1f - (_ready[slot].Relative / _cooldown[slot]).Clamp( 0f, 1f );
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		SyncLoadout();

		for ( var slot = 0; slot < SlotCount; slot++ )
		{
			if ( Input.Pressed( ActionFor( slot ) ) ) _queued[slot] = true;

			var cooling = !_ready[slot];
			if ( _wasCooling[slot] && !cooling )
			{
				Sfx.Ui( Sfx.Ready, 0.35f );
				Revision++;
			}

			_wasCooling[slot] = cooling;
		}

		for ( var slot = 0; slot < SlotCount; slot++ )
		{
			if ( !_queued[slot] ) continue;
			_queued[slot] = false;
			TryCast( slot );
		}
	}

	/// <summary>Owner: follow the profile — a loadout changed on the results screen applies next cast.</summary>
	void SyncLoadout()
	{
		if ( _profile is null ) return;

		if ( First != _profile.First ) First = _profile.First;
		if ( Second != _profile.Second ) Second = _profile.Second;
	}

	/// <summary>Whether casting makes sense at all right now, before bothering the host.</summary>
	public bool CanCastNow =>
		_player.IsAlive && !_player.IsRagdolled && !_player.Status.Has( StatusKind.Stunned )
		&& _round.State != RoundManager.RoundState.Ended;

	void TryCast( int slot )
	{
		if ( !CanCastNow ) return;
		if ( !_awaiting[slot] ) return;

		if ( !_ready[slot] )
		{
			Sfx.Ui( Sfx.Deny, 0.35f );
			return;
		}

		_awaiting[slot] = 0.5f;
		RequestCast( slot, _player.EyeAngles );
	}

	/// <summary>
	/// Owner → host. Runs on the host; the body validates everything the owner claimed, since a
	/// modified client could send anything here.
	/// </summary>
	[Rpc.Host]
	void RequestCast( int slot, Angles aim )
	{
		if ( Rpc.Calling && Rpc.Caller != Network.Owner ) return;
		if ( slot < 0 || slot >= SlotCount ) return;
		if ( !CanCastNow || !_hostReady[slot] )
		{
			DenyCast( slot );
			return;
		}

		var id = InSlot( slot );
		if ( !AbilityCatalog.IsValid( id ) ) return;

		var system = Scene.Get<AbilitySystem>();
		if ( system is null || !system.Execute( _player, id, aim ) )
		{
			DenyCast( slot );
			return;
		}

		var cooldown = AbilityCatalog.Get( id ).Cooldown;
		_hostReady[slot] = cooldown;
		ConfirmCast( slot, cooldown );
	}

	[Rpc.Owner]
	void ConfirmCast( int slot, float cooldown )
	{
		_ready[slot] = cooldown;
		_cooldown[slot] = cooldown;
		_awaiting[slot] = 0f;
		_wasCooling[slot] = true;
		Revision++;
	}

	/// <summary>Nothing in range, stunned on arrival, or still cooling: a dull click, no cooldown.</summary>
	[Rpc.Owner]
	void DenyCast( int slot )
	{
		_awaiting[slot] = 0f;
		Sfx.Ui( Sfx.Deny, 0.35f );
	}

	/// <summary>Host: every slot ready — a new round starts everyone even.</summary>
	public void ResetCooldowns()
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;

		for ( var slot = 0; slot < SlotCount; slot++ ) _hostReady[slot] = 0f;
		ClearCooldowns();
	}

	[Rpc.Owner]
	void ClearCooldowns()
	{
		for ( var slot = 0; slot < SlotCount; slot++ )
		{
			_ready[slot] = 0f;
			_cooldown[slot] = 0f;
			_wasCooling[slot] = false;
		}

		Revision++;
	}
}
