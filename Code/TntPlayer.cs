using System;
using Sandbox.Citizen;

/// <summary>
/// Player movement, third-person camera, spectating, ragdoll presentation and tile stepping.
/// Builds its own <see cref="CharacterController"/> and Citizen body at runtime — no prefabs,
/// plus its sibling components: <see cref="PlayerStatus"/> (timed effects),
/// <see cref="PlayerAbilities"/> (the two loadout slots) and <see cref="PlayerCosmetics"/>
/// (rank perks and status visuals).
/// Uses only the default input actions: forward, backward, left, right, jump, run (and
/// attack1/attack2, read by PlayerAbilities).
/// </summary>
public sealed class TntPlayer : Component, Component.INetworkSpawn
{
	[Property, Range( 50f, 500f )] public float WalkSpeed { get; set; } = 190f;
	[Property, Range( 50f, 600f )] public float SprintSpeed { get; set; } = 280f;
	[Property, Range( 100f, 600f )] public float JumpPower { get; set; } = 300f;

	/// <summary>Source-style accelerate factor. Higher = reaches top speed in fewer ticks.</summary>
	[Property, Range( 1f, 30f )] public float GroundAcceleration { get; set; } = 12f;

	[Property, Range( 1f, 20f )] public float GroundFriction { get; set; } = 9f;

	/// <summary>Friction floor. Higher = friction stays strong at low speed, so stops are sharper.</summary>
	[Property, Range( 50f, 400f )] public float StopSpeed { get; set; } = 250f;

	[Property, Range( 0f, 1f )] public float AirControl { get; set; } = 0.05f;

	[Property, Range( 80f, 600f )] public float CameraDistance { get; set; } = 220f;
	[Property, Range( 0f, 160f )] public float CameraTargetHeight { get; set; } = 56f;

	public const float BodyRadius = 16f;

	/// <summary>
	/// Half-size of the square that marks tiles under a grounded player. A little inside the
	/// capsule radius, so brushing a tile edge doesn't doom it but straddling a seam dooms both.
	/// </summary>
	public const float FootprintRadius = 12f;

	/// <summary>
	/// False once eliminated.
	///
	/// FromHost, not owner-synced: elimination is decided on the host, and the owning client has
	/// to be *told* it died. The setter is public because the sync generator assigns it — only
	/// the host may actually write it.
	/// </summary>
	[Sync( SyncFlags.FromHost ), Change( nameof( OnIsAliveChanged ) )]
	public bool IsAlive { get; set; } = true;

	/// <summary>
	/// Owner-synced. Proxies never run Move(), so their CharacterController's IsOnGround is
	/// permanently false — the animation needs the real value from whoever owns this player.
	/// </summary>
	[Sync] public bool IsGrounded { get; set; }

	/// <summary>
	/// Owner-synced intended velocity, sampled before the controller resolves collisions. Two
	/// uses: proxies animate from it, and the host judges shoves from it — a velocity read after
	/// Move() has already been clipped to zero by the very collision being judged.
	/// </summary>
	[Sync] public Vector3 MoveVelocity { get; set; }

	/// <summary>True while this player is a ragdoll, on every machine. Local timer, broadcast start.</summary>
	public bool IsRagdolled => _ragdoll.IsValid();

	/// <summary>
	/// Whether this player's feet should doom tiles right now. One definition shared by the
	/// owner's step reports and every machine's warning tint.
	/// </summary>
	public bool CanMarkTiles => IsAlive && IsGrounded && !IsRagdolled && !Status.Has( StatusKind.Ghost );

	/// <summary>
	/// Owner-synced rank points from the owner's local profile. Everyone else uses it for the
	/// nameplate emblem and the rank perks. Owner-written because the profile lives on the owner's
	/// machine (ranks are persisted locally).
	/// </summary>
	[Sync] public int RankPoints { get; set; }

	public RankTier RankTier => Ranks.Tier( RankPoints );

	/// <summary>The owning connection's name, or a placeholder before ownership lands.</summary>
	public string DisplayName => Network.Owner?.DisplayName ?? "Player";

	/// <summary>
	/// This machine's own player, or null on the menu, while spectating a join, or on a dedicated
	/// host. The one definition of "local player" every system shares.
	/// </summary>
	public static TntPlayer FindLocal( Scene scene ) => scene.GetAll<TntPlayer>().FirstOrDefault( p => !p.IsProxy );

	/// <summary>Timed effects from abilities. Same object, every machine.</summary>
	public PlayerStatus Status { get; private set; }

	public PlayerAbilities Abilities { get; private set; }

	/// <summary>The animated Citizen. Cosmetics hang their perks off it.</summary>
	public SkinnedModelRenderer Body => _body;

	/// <summary>Owner only: where this player is looking. Abilities aim along it.</summary>
	public Angles EyeAngles => _eyeAngles;

	/// <summary>Owner only: whether the sprint key is driving us right now.</summary>
	public bool IsSprinting => _isSprinting;

	/// <summary>
	/// Floor index under the player, or -1 outside the arena. Delegates to the one implementation
	/// of the index arithmetic in ArenaGenerator rather than repeating the formula.
	/// </summary>
	public int CurrentFloor =>
		_arena.TryGetTileIndex( WorldPosition, out var floor, out _, out _ ) ? floor : -1;

	/// <summary>The player this machine's camera is following while spectating, if any.</summary>
	public TntPlayer SpectateTarget { get; private set; }

	CharacterController _controller;
	CapsuleCollider _capsule;
	SkinnedModelRenderer _body;
	CitizenAnimationHelper _animation;
	ArenaGenerator _arena;
	TileDecay _decay;
	RoundManager _round;

	/// <summary>Kept so the ragdoll copy can wear the same outfit as the body.</summary>
	ClothingContainer _clothing;

	Angles _eyeAngles = new Angles( 20f, 0f, 0f );

	/// <summary>
	/// Tiles already reported this life. The host dedups too, but this stops a standing player
	/// re-sending the same four RPCs every tick.
	/// </summary>
	readonly HashSet<int> _reported = new();

	readonly int[] _footprint = new int[4];

	/// <summary>Where we're asking to go, for the air arm-swing animation.</summary>
	Vector3 _wishVelocity;

	float _lastYaw;
	float _yawSpeed;

	Vector3 _renderVelocity;

	bool _isSprinting;

	/// <summary>
	/// Jump is latched in OnUpdate and consumed in OnFixedUpdate. Input.Pressed is true for
	/// exactly one frame, and at 50Hz fixed / 60+ fps render most frames run no fixed update
	/// at all — reading it directly in OnFixedUpdate silently drops presses.
	/// </summary>
	bool _jumpQueued;

	GameObject _ragdoll;
	SkinnedModelRenderer _ragdollRenderer;
	TimeUntil _ragdollEnds;

	/// <summary>Distance walked since the last footstep sound.</summary>
	float _strideDistance;

	bool _wasGrounded = true;
	float _airborneFromZ;

	/// <summary>Slow orbit angle for the overview camera when nobody is left to follow.</summary>
	float _overviewYaw;

	TimeSince _sinceCreated;

	/// <summary>Owner only: Double Jump spends this until the next landing.</summary>
	bool _airJumpUsed;

	/// <summary>Owner only: where we were, sampled for Rewind.</summary>
	readonly Queue<(float Time, Vector3 Position)> _history = new();
	TimeSince _sinceSample;

	const float RewindSeconds = 3f;

	/// <summary>Camera shake amplitude in degrees, decaying. Every machine shakes its own camera.</summary>
	float _shake;

	/// <summary>Extra field of view from a hit, decaying back to zero.</summary>
	float _fovPunch;

	float _baseFov;

	/// <summary>Smoothed camera focus, so a tumbling ragdoll doesn't whip the view around.</summary>
	Vector3 _cameraFocus;
	bool _cameraFocusValid;

	protected override void OnAwake()
	{
		_sinceCreated = 0f;

		Status = GetOrAddComponent<PlayerStatus>();
		Abilities = GetOrAddComponent<PlayerAbilities>();

		_controller = GetOrAddComponent<CharacterController>();
		_controller.Height = 72f;
		_controller.Radius = BodyRadius;
		_controller.StepHeight = 18f;

		// CharacterController exposes no collider of its own, so triggers can't see it.
		// This capsule mirrors the controller so LavaZone gets an overlap event, and so other
		// players' controllers have something solid to bump into.
		_capsule = GetOrAddComponent<CapsuleCollider>();
		_capsule.Radius = BodyRadius;
		_capsule.Start = new Vector3( 0f, 0f, 16f );
		_capsule.End = new Vector3( 0f, 0f, 56f );

		var bodyObject = new GameObject( GameObject, true, "Body" );

		// Cosmetic, and every machine builds its own in this same OnAwake. Must not replicate:
		// otherwise a client receives the host's Body and then adds a second one itself.
		bodyObject.Flags |= GameObjectFlags.NotSaved;
		bodyObject.NetworkMode = NetworkMode.Never;

		_body = bodyObject.AddComponent<SkinnedModelRenderer>();
		_body.Model = Model.Load( "models/citizen/citizen.vmdl" );

		// CitizenAnimationHelper owns the animgraph parameter names and their types.
		// Never set "move_direction" and friends by hand — it drives six related params
		// from one velocity, projected through the body's own rotation.
		_animation = bodyObject.AddComponent<CitizenAnimationHelper>();
		_animation.Target = _body;

		// Safe in OnAwake: players are spawned at runtime by RoundManager, long after the
		// scene's own OnAwake pass. Storing refs only — nothing here reads arena geometry.
		_arena = Scene.Get<ArenaGenerator>();
		_decay = Scene.Get<TileDecay>();
		_round = Scene.Get<RoundManager>();

		// Presentation only, rebuilt on every machine like the Body — it reads synced state and
		// nothing reads it back.
		GetOrAddComponent<PlayerCosmetics>();
		CreateNameplate();
	}

	/// <summary>
	/// A world-space name tag over the head: name, rank emblem, status emote. Local, unsaved and
	/// unnetworked like the Body, and hidden for this machine's own player by the panel itself.
	/// </summary>
	void CreateNameplate()
	{
		var tag = new GameObject( GameObject, true, "Nameplate" );
		tag.Flags |= GameObjectFlags.NotSaved;
		tag.NetworkMode = NetworkMode.Never;
		tag.LocalPosition = Vector3.Up * 96f;

		var world = tag.AddComponent<WorldPanel>();
		world.PanelSize = new Vector2( 900f, 260f );
		world.RenderScale = 1.5f;
		world.LookAtCamera = true;

		tag.AddComponent<Nameplate>().Player = this;
	}

	/// <summary>
	/// Dress the Citizen in the owner's own Steam avatar outfit.
	///
	/// Fires on every machine that receives this object, and each one dresses its own local
	/// Body — which is what we want, because Body is NetworkMode.Never and built per machine.
	///
	/// Not OnAwake: RoundManager calls NetworkSpawn *after* AddComponent, so Network.Owner is
	/// still null while OnAwake runs. This hook hands us the connection directly.
	///
	/// ApplyClothing, not the async variant: it skips clothing that isn't already downloaded, which keeps
	/// us inside CLAUDE.md's no-external-downloads rule.
	///
	/// removeUnowned is false, and must stay false. The engine's own doc on
	/// RemoveUnownedItems(Connection) — which is what passing true runs — says: "Must be called
	/// from the host or from the local player, as clients don't have access to other player
	/// inventory data." This hook runs on *every* machine, so on one that is neither the host
	/// nor that player's owner the check has no inventory to verify against and drops every item
	/// gated on Steam ownership. Hair needs no entitlement, so it survived — which is exactly
	/// the "hair changes, clothing doesn't" symptom.
	/// </summary>
	public void OnNetworkSpawn( Connection owner )
	{
		_clothing = ClothingContainer.CreateFromConnection( owner, false );
		Dress( _body );

		// Our own player: publish the rank from this machine's profile. Everyone else reads it.
		if ( !IsProxy )
			RankPoints = Scene.Get<ProfileStore>().Profile.RankPoints;
	}

	/// <summary>
	/// Appearance (skin, height) then outfit, through a transient Dresser on that renderer.
	/// Dresser.Apply skips anything not already downloaded — no downloads, per CLAUDE.md.
	/// </summary>
	void Dress( SkinnedModelRenderer renderer )
	{
		if ( _clothing is null ) return;

		var dresser = Dresser.GetOrCreate( renderer );
		dresser.UpdateAppearance( _clothing );
		dresser.Apply( _clothing );
	}

	protected override void OnUpdate()
	{
		// Presentation runs before the proxy guard: remote players need moving legs, sounds and
		// ragdolls too, and everything these read is either synced or derived from the transform.
		UpdateBodyVisibility();
		UpdateRagdoll();
		UpdateRenderVelocity();
		UpdateAnimation();
		UpdateFootsteps();

		UpdateCameraEffects();

		if ( IsProxy ) return;

		if ( Input.Pressed( "jump" ) )
		{
			if ( IsAlive ) _jumpQueued = true;
			else CycleSpectateTarget();
		}

		var look = _eyeAngles;
		look += Input.AnalogLook;
		look.pitch = look.pitch.Clamp( -80f, 80f );
		look.roll = 0f;
		_eyeAngles = look;

		// Degrees per second of turn, for the animgraph's foot shuffle.
		var yawDelta = MathX.DeltaDegrees( _lastYaw, _eyeAngles.yaw );
		_yawSpeed = Time.Delta > 0f ? -yawDelta / Time.Delta : 0f;
		_lastYaw = _eyeAngles.yaw;

		if ( IsAlive ) UpdateCamera();
		else UpdateSpectateCamera();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		// Skipping Move() entirely is the freeze: no gravity, no input, no tile marking.
		// Look and camera keep running in OnUpdate so you can watch the rest of the round.
		if ( !IsAlive ) return;

		if ( IsRagdolled )
		{
			FollowRagdoll();
			return;
		}

		Move();

		IsGrounded = _controller.IsOnGround;
		if ( IsGrounded ) _airJumpUsed = false;

		SampleHistory();
		UpdateTiles();
	}

	/// <summary>Owner: a position every 0.1s for the last few seconds, for Rewind.</summary>
	void SampleHistory()
	{
		if ( _sinceSample < 0.1f ) return;
		_sinceSample = 0f;

		_history.Enqueue( (Time.Now, WorldPosition) );
		while ( _history.Count > 0 && Time.Now - _history.Peek().Time > RewindSeconds + 0.5f )
			_history.Dequeue();
	}

	/// <summary>Called by RoundManager on the host. Idempotent.</summary>
	public void Eliminate()
	{
		if ( !IsAlive ) return;

		// Host-only write. Motion is cleared by OnIsAliveChanged, which is the only mechanism
		// that reaches the owning client — clearing it here would only affect the host's copy.
		IsAlive = false;
	}

	/// <summary>
	/// Fires wherever the synced value lands, so the owner stops itself the moment the host's
	/// elimination arrives — and every machine hears the blast.
	/// </summary>
	void OnIsAliveChanged( bool oldValue, bool newValue )
	{
		if ( newValue ) return;

		EndRagdoll();
		ClearMotion();

		// A late joiner receives IsAlive=false as initial state, which lands here too. That isn't
		// a death anyone saw, so it gets no blast.
		if ( _sinceCreated < 1f ) return;

		Sfx.At( Sfx.Blast, WorldPosition, 0.8f );
		AbilityFx.Burst( WorldPosition + Vector3.Up * 20f, AbilityFx.Coral, 14, 340f, 12f );
	}

	/// <summary>
	/// Put the player back on floor 0, alive and stationary.
	///
	/// [Rpc.Owner] because transforms are owner-authoritative: if the host wrote WorldPosition
	/// on a client-owned player directly, the owner's own transform would immediately overwrite
	/// it. The host calls this, it executes on the owner, and the owner moves itself. Solo (no
	/// session) it just runs locally.
	///
	/// Deliberately does NOT touch IsAlive — that's a FromHost property, so a write from the
	/// owner would be rejected and the player would respawn permanently dead. RoundManager
	/// sets it host-side alongside this call.
	/// </summary>
	[Rpc.Owner]
	public void Respawn( int index, int total )
	{
		EndRagdoll();

		var spawn = _arena.GetSpawnPosition( index, total );
		WorldPosition = spawn;
		Transform.ClearInterpolation();

		_eyeAngles = new Angles( 20f, _arena.GetSpawnYaw( spawn ), 0f );
		_lastYaw = _eyeAngles.yaw;

		ClearMotion();

		// A new life starts with a clean slate, or tiles walked over in a previous one would
		// never be reported again.
		_reported.Clear();
		_history.Clear();
		_airJumpUsed = false;
		SpectateTarget = null;
		_cameraFocusValid = false;
	}

	/// <summary>A rebuilt tile (Patch, Bridge) must be reportable again by whoever stands on it.</summary>
	public void ForgetReported( int packed ) => _reported.Remove( packed );

	/// <summary>
	/// Host → owner: add velocity. Dash, Hook, Super Jump and friends. Movement is owner-authoritative,
	/// so the host can't move the body itself — it asks the owner to, like Respawn.
	/// </summary>
	[Rpc.Owner]
	public void Launch( Vector3 velocity )
	{
		if ( !IsAlive || IsRagdolled ) return;

		var horizontal = velocity.WithZ( 0f );
		_controller.Velocity = _controller.Velocity.WithZ( 0f ) * 0.35f + horizontal;

		// Punch lifts the controller off the ground so it doesn't immediately re-stick.
		if ( velocity.z > 0f )
			_controller.Punch( Vector3.Up * velocity.z );
	}

	/// <summary>
	/// Host → owner: put the body somewhere else. Blink, Swap and Rewind. Ignored mid-ragdoll: the
	/// ragdoll is a local physics copy on every machine, and only the owner would see it end early.
	/// </summary>
	[Rpc.Owner]
	public void TeleportTo( Vector3 position )
	{
		if ( !IsAlive || IsRagdolled ) return;

		WorldPosition = position;
		Transform.ClearInterpolation();
		_controller.Velocity = Vector3.Zero;
		_cameraFocusValid = false;
	}

	/// <summary>Host → owner: Rewind. Only the owner remembers where it has been.</summary>
	[Rpc.Owner]
	public void RewindNow()
	{
		if ( !IsAlive || _history.Count == 0 ) return;

		var target = _history.Peek();
		foreach ( var sample in _history )
		{
			if ( Time.Now - sample.Time <= RewindSeconds )
			{
				target = sample;
				break;
			}
		}

		AbilityFx.Line( WorldPosition + Vector3.Up * 36f, target.Position + Vector3.Up * 36f, AbilityFx.Lilac, 40f, 10f );
		TeleportTo( target.Position + Vector3.Up * 4f );
		_history.Clear();
	}

	/// <summary>
	/// Host → owner, once per ranked round: how it went. The owner's profile is the only copy of
	/// its rank, so this is where rank points actually change hands.
	/// </summary>
	[Rpc.Owner]
	public void ReceiveRoundResult( int delta, int place, int knockdowns )
	{
		var store = Scene.Get<ProfileStore>();
		store.ApplyRoundResult( delta, place, knockdowns );
		RankPoints = store.Profile.RankPoints;
	}

	/// <summary>
	/// Start a ragdoll on every machine. The host decides when (CLAUDE.md: "the host decides when
	/// a ragdoll starts"); each machine then simulates its own cosmetic copy from the same pose
	/// and the same launch velocity, and the owner's copy is the one the player's root follows.
	/// </summary>
	[Rpc.Broadcast( NetFlags.HostOnly )]
	public void BeginRagdoll( Vector3 launchVelocity, float duration )
	{
		if ( !IsAlive ) return;
		if ( IsRagdolled ) return;

		_ragdoll = new GameObject( true, $"Ragdoll - {DisplayName}" );
		_ragdoll.Flags |= GameObjectFlags.NotSaved;
		_ragdoll.NetworkMode = NetworkMode.Never;
		_ragdoll.WorldTransform = _body.GameObject.WorldTransform;

		_ragdollRenderer = _ragdoll.AddComponent<SkinnedModelRenderer>();
		_ragdollRenderer.Model = _body.Model;
		_ragdollRenderer.UseAnimGraph = false;
		Dress( _ragdollRenderer );

		var physics = _ragdoll.AddComponent<ModelPhysics>();
		physics.Renderer = _ragdollRenderer;
		physics.Model = _ragdollRenderer.Model;
		physics.CopyBonesFrom( _body, true );
		physics.MotionEnabled = true;

		var velocity = launchVelocity + _renderVelocity.WithZ( 0f );

		foreach ( var body in _ragdoll.GetComponentsInChildren<Rigidbody>( true, true ) )
		{
			body.Velocity = velocity;
		}

		// Our own capsule sits exactly where the ragdoll spawns; left solid it would launch the
		// ragdoll off itself. Lava detection doesn't depend on it — RoundManager checks height.
		_capsule.Enabled = false;

		_ragdollEnds = duration;

		// Feel: the harder the hit, the louder and lower the thud, and the more nearby cameras shake.
		var strength = (launchVelocity.Length / 700f).Clamp( 0.3f, 1.2f );
		Sfx.At( Sfx.BodyHit, WorldPosition + Vector3.Up * 40f, 0.6f + 0.4f * strength, 1.15f - 0.25f * strength );
		AbilityFx.Flash( WorldPosition + Vector3.Up * 40f, Color.White, 50f + 40f * strength );
		AbilityFx.Burst( WorldPosition + Vector3.Up * 40f, AbilityFx.Butter, 6, 160f + 120f * strength, 7f );

		ShakeNearby( WorldPosition, strength );
	}

	/// <summary>
	/// Shake whichever camera this machine is driving, scaled by distance from the hit. Our own
	/// knockdown shakes hardest; a crash across the floor barely registers.
	/// </summary>
	void ShakeNearby( Vector3 position, float strength )
	{
		var camera = Scene.Camera;
		if ( !camera.IsValid() ) return;

		var distance = camera.WorldPosition.Distance( position );
		var falloff = (1f - distance / 900f).Clamp( 0f, 1f );
		if ( falloff <= 0f ) return;

		FindLocal( Scene )?.Shake( strength * falloff );
	}

	/// <summary>Kick this machine's camera: <paramref name="amount"/> 0..1+, the stronger kick wins.</summary>
	public void Shake( float amount )
	{
		_shake = MathF.Max( _shake, 5f * amount );
		_fovPunch = MathF.Max( _fovPunch, 9f * amount );
	}

	/// <summary>Owner: decay shake and FOV punch on the camera this machine is driving.</summary>
	void UpdateCameraEffects()
	{
		if ( IsProxy ) return;

		_shake = MathX.Approach( _shake, 0f, Time.Delta * 14f );
		_fovPunch = MathX.Approach( _fovPunch, 0f, Time.Delta * 30f );
	}

	/// <summary>
	/// The straight snap back to control CLAUDE.md asks for: the ragdoll copy goes away and the
	/// animated body reappears at the root, which the owner has been dragging along with it.
	/// </summary>
	void EndRagdoll()
	{
		if ( !_ragdoll.IsValid() ) return;

		_ragdoll.Destroy();
		_ragdoll = null;
		_ragdollRenderer = null;

		_capsule.Enabled = IsAlive;

		if ( !IsProxy )
		{
			_controller.Velocity = Vector3.Zero;
			Transform.ClearInterpolation();
		}

		// Dazed for a moment after getting up — cartoon stars, and the body blinks (PlayerCosmetics)
		// while PlayerImpacts' grace makes it impossible to floor again straight away.
		if ( IsAlive )
		{
			AbilityFx.Dizzy( GameObject, PlayerImpacts.Immunity, AbilityFx.Butter );
			_recoveredAt = 0f;
		}
	}

	TimeSince _recoveredAt = 999f;

	/// <summary>True for a moment after getting up from a ragdoll, on every machine — the body blinks.</summary>
	public bool IsRecovering => _recoveredAt < PlayerImpacts.Immunity;

	void UpdateRagdoll()
	{
		if ( !IsRagdolled ) return;
		if ( _ragdollEnds ) EndRagdoll();
	}

	/// <summary>
	/// Owner only: drag the root along with the ragdoll's pelvis, so the replicated position,
	/// the camera, the host's lava check and the floor ladder all track where the body really is.
	/// </summary>
	void FollowRagdoll()
	{
		IsGrounded = false;
		MoveVelocity = Vector3.Zero;

		if ( !_ragdollRenderer.IsValid() ) return;
		if ( !_ragdollRenderer.TryGetBoneTransform( "pelvis", out var pelvis ) ) return;

		// At the pelvis, not below it: a body lying on a tile has its pelvis only a few units up,
		// and a root placed under that sinks the controller's capsule into the tile on recovery.
		WorldPosition = pelvis.Position;
	}

	/// <summary>
	/// The animated body is hidden while ragdolled (the copy stands in) and while eliminated (a
	/// spectator has no body). Polled rather than event-driven so a late joiner, who never sees
	/// the change happen, still lands on the right state.
	/// </summary>
	void UpdateBodyVisibility()
	{
		var visible = IsAlive && !IsRagdolled;
		if ( _body.GameObject.Enabled != visible )
			_body.GameObject.Enabled = visible;

		var solid = IsAlive && !IsRagdolled;
		if ( _capsule.Enabled != solid )
			_capsule.Enabled = solid;
	}

	void ClearMotion()
	{
		_controller.Velocity = Vector3.Zero;
		_wishVelocity = Vector3.Zero;
		MoveVelocity = Vector3.Zero;
		_isSprinting = false;
		_jumpQueued = false;
	}

	void Move()
	{
		var stunned = Status.Has( StatusKind.Stunned );
		var iced = Status.Has( StatusKind.Ice );
		var feather = Status.Has( StatusKind.FeatherFall );

		var yaw = Rotation.FromYaw( _eyeAngles.yaw );
		var input = stunned ? Vector3.Zero : Input.AnalogMove;

		// Confuse flips the stick both ways — forward walks you back, left goes right.
		if ( Status.Has( StatusKind.Confused ) )
			input = new Vector3( -input.x, -input.y, input.z );

		var wishDir = input * yaw;
		if ( !wishDir.IsNearZeroLength )
			wishDir = wishDir.Normal;

		_isSprinting = Input.Down( "run" ) && !wishDir.IsNearZeroLength;
		_wishVelocity = wishDir * (_isSprinting ? SprintSpeed : WalkSpeed) * Status.SpeedScale;

		if ( stunned ) _jumpQueued = false;

		// Assigned every tick so inspector edits take effect live during Play. Ice barely grips.
		_controller.Acceleration = iced ? GroundAcceleration * 0.18f : GroundAcceleration;

		if ( _controller.IsOnGround )
		{
			_controller.ApplyFriction( iced ? GroundFriction * 0.08f : GroundFriction, StopSpeed );
			_controller.Accelerate( _wishVelocity );

			if ( _jumpQueued )
			{
				_jumpQueued = false;
				_controller.Punch( Vector3.Up * JumpPower );
				_animation.TriggerJump();
				PlayJumpSound();
			}
		}
		else
		{
			// Double Jump spends the press in mid-air; otherwise don't bank it, or it would fire
			// the instant we land.
			if ( _jumpQueued && !_airJumpUsed && Status.Has( StatusKind.DoubleJump ) )
			{
				_airJumpUsed = true;
				_controller.Velocity = _controller.Velocity.WithZ( 0f );
				_controller.Punch( Vector3.Up * JumpPower * 1.05f );
				_animation.TriggerJump();
				PlayJumpSound();
				AbilityFx.Ring( WorldPosition, 50f, AbilityFx.Mint, 0.3f );
			}

			_jumpQueued = false;

			var gravity = Scene.PhysicsWorld.Gravity * (feather ? 0.22f : 1f);
			_controller.Velocity += gravity * Time.Delta;

			// Feather Fall also caps the drop and hands back real steering.
			if ( feather && _controller.Velocity.z < -140f )
				_controller.Velocity = _controller.Velocity.WithZ( -140f );

			_controller.Accelerate( _wishVelocity * (feather ? 0.6f : AirControl) );
		}

		// Vortex: a steady pull towards the whirl's centre, owner-side so it's smooth at any ping.
		if ( Status.Has( StatusKind.Vortex ) )
		{
			var toCentre = (Status.VortexCentre - WorldPosition).WithZ( 0f );
			var distance = toCentre.Length;
			if ( distance > 8f )
				_controller.Velocity += toCentre / distance * 900f * Time.Delta;
		}

		MoveVelocity = _controller.Velocity;

		_controller.Move();

		WorldRotation = yaw;
	}

	/// <summary>
	/// Arithmetic tile lookup over the footprint, then one NotifyStep per newly touched tile
	/// (CLAUDE.md, decision 2). Only while the round is live and the player can mark at all.
	/// </summary>
	void UpdateTiles()
	{
		if ( !CanMarkTiles ) return;
		if ( _round.State != RoundManager.RoundState.Playing ) return;

		var count = _arena.GetFootprintTiles( WorldPosition, FootprintRadius, _footprint );

		for ( var i = 0; i < count; i++ )
		{
			var packed = _footprint[i];
			if ( !_reported.Add( packed ) ) continue;

			TileDecay.UnpackIndex( packed, out var floor, out var x, out var y );
			_decay.NotifyStep( floor, x, y );
		}
	}

	/// <summary>
	/// Over-the-shoulder camera, pulled in when something solid sits between the player and
	/// where the camera wants to be — looking down from a lower floor otherwise puts the lens
	/// inside the floor above. A trace for the camera only, never for tile lookup.
	/// </summary>
	void UpdateCamera()
	{
		var target = WorldPosition + Vector3.Up * CameraTargetHeight;

		// Ease the focus while ragdolled: the root follows a tumbling pelvis, and a camera glued to
		// it whips with every bounce. Snapped when controlled, so running never feels floaty.
		if ( !_cameraFocusValid || !IsRagdolled )
			_cameraFocus = target;
		else
			_cameraFocus = Vector3.Lerp( _cameraFocus, target, (Time.Delta * 8f).Clamp( 0f, 1f ) );

		_cameraFocusValid = true;

		// Pull back a little while down, so you can see where you're about to land.
		var distance = IsRagdolled ? CameraDistance * 1.25f : CameraDistance;
		PlaceCamera( _cameraFocus, _eyeAngles.ToRotation(), distance );
	}

	void PlaceCamera( Vector3 target, Rotation rotation, float distance )
	{
		var camera = Scene.Camera;
		if ( !camera.IsValid() ) return;

		if ( _baseFov <= 0f ) _baseFov = camera.FieldOfView;
		camera.FieldOfView = _baseFov + _fovPunch;

		if ( _shake > 0.01f )
		{
			var jitter = new Angles(
				(Game.Random.NextSingle() - 0.5f) * 2f * _shake,
				(Game.Random.NextSingle() - 0.5f) * 2f * _shake,
				(Game.Random.NextSingle() - 0.5f) * _shake );
			rotation *= jitter.ToRotation();
		}

		var desired = target - rotation.Forward * distance;

		var trace = Scene.Trace.Ray( target, desired )
			.Radius( 8f )
			.WithoutTags( new[] { "player" } )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		camera.WorldPosition = trace.Hit ? trace.EndPosition : desired;
		camera.WorldRotation = rotation;
	}

	/// <summary>
	/// After elimination: follow a survivor, jump to switch between them. With nobody left
	/// standing, a slow orbit over the whole arena instead.
	/// </summary>
	void UpdateSpectateCamera()
	{
		if ( SpectateTarget is null || !SpectateTarget.IsValid() || !SpectateTarget.IsAlive )
			SpectateTarget = FindSpectateTarget( null );

		if ( SpectateTarget is not null )
		{
			var target = SpectateTarget.WorldPosition + Vector3.Up * CameraTargetHeight;
			PlaceCamera( target, _eyeAngles.ToRotation(), CameraDistance * 1.3f );
			return;
		}

		_overviewYaw += Time.Delta * 6f;
		var orbit = new Angles( 38f, _overviewYaw, 0f ).ToRotation();
		var centre = _arena.WorldPosition + Vector3.Down * ((Arena.FloorCount - 1) * Arena.FloorGap * 0.5f);

		var camera = Scene.Camera;
		if ( !camera.IsValid() ) return;

		camera.WorldPosition = centre - orbit.Forward * 3000f;
		camera.WorldRotation = orbit;
	}

	void CycleSpectateTarget()
	{
		SpectateTarget = FindSpectateTarget( SpectateTarget );
	}

	/// <summary>
	/// The next survivor after <paramref name="current"/>, in a stable order so repeated presses
	/// walk through everyone instead of bouncing between whoever Scene.GetAll returns first.
	/// </summary>
	TntPlayer FindSpectateTarget( TntPlayer current )
	{
		var alive = Scene.GetAll<TntPlayer>()
			.Where( p => p.IsAlive && p != this )
			.OrderBy( p => p.GameObject.Id )
			.ToList();

		if ( alive.Count == 0 ) return null;

		var index = current is null ? -1 : alive.IndexOf( current );
		return alive[(index + 1) % alive.Count];
	}

	/// <summary>
	/// The velocity the animgraph should see. The owner has an authoritative one on the
	/// controller; a proxy reads the owner-synced MoveVelocity, which is steadier than
	/// differencing an interpolated transform frame to frame.
	/// </summary>
	void UpdateRenderVelocity()
	{
		_renderVelocity = IsProxy ? MoveVelocity : _controller.Velocity;
	}

	void UpdateAnimation()
	{
		if ( !_body.GameObject.Enabled ) return;

		_animation.WithVelocity( _renderVelocity );
		_animation.WithWishVelocity( IsProxy ? MoveVelocity : _wishVelocity );
		_animation.IsGrounded = IsGrounded;
		_animation.MoveRotationSpeed = _yawSpeed;

		// MoveStyles is { Auto, Walk, Run } — there is no sprint tier. Sprint is the top of
		// the run blend, so pin Run rather than let Auto blend back toward walk.
		var sprinting = IsProxy ? MoveVelocity.WithZ( 0f ).Length > WalkSpeed + 20f : _isSprinting;
		_animation.MoveStyle = sprinting
			? CitizenAnimationHelper.MoveStyles.Run
			: CitizenAnimationHelper.MoveStyles.Auto;
	}

	/// <summary>
	/// Footsteps every stride, plus a landing thump after a real drop. Runs on every machine for
	/// every player, from synced state only, so everybody hears everybody.
	/// </summary>
	void UpdateFootsteps()
	{
		if ( !IsAlive || IsRagdolled )
		{
			_wasGrounded = true;
			return;
		}

		var grounded = IsGrounded;

		if ( !grounded && _wasGrounded )
			_airborneFromZ = WorldPosition.z;

		if ( grounded && !_wasGrounded && _airborneFromZ - WorldPosition.z > 40f )
		{
			Sfx.At( Sfx.FootstepLand, WorldPosition, 0.9f );
			_strideDistance = 0f;
		}

		_wasGrounded = grounded;

		if ( !grounded ) return;

		var speed = _renderVelocity.WithZ( 0f ).Length;
		if ( speed < 60f ) return;

		_strideDistance += speed * Time.Delta;
		if ( _strideDistance < 72f ) return;

		_strideDistance = 0f;

		Sfx.At( Sfx.Footstep, WorldPosition, IsProxy ? 0.35f : 0.5f );
	}

	void PlayJumpSound()
	{
		Sfx.At( Sfx.FootstepJump, WorldPosition, 0.6f );
	}

	protected override void OnDestroy()
	{
		if ( _ragdoll.IsValid() ) _ragdoll.Destroy();
	}
}
