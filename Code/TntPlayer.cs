using Sandbox.Citizen;

/// <summary>
/// Player movement, third-person camera, spectating, ragdoll presentation and tile stepping.
/// Builds its own <see cref="CharacterController"/> and Citizen body at runtime — no prefabs.
/// Uses only the default input actions: forward, backward, left, right, jump, run.
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

	/// <summary>How long a shoved player stays a ragdoll before snapping back to control.</summary>
	public const float RagdollDuration = 1.3f;

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
	public bool CanMarkTiles => IsAlive && IsGrounded && !IsRagdolled;

	/// <summary>The owning connection's name, or a placeholder before ownership lands.</summary>
	public string DisplayName => Network.Owner?.DisplayName ?? "Player";

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

	protected override void OnAwake()
	{
		_sinceCreated = 0f;

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

		UpdateTiles();
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

		var blast = Sound.Play( "sounds/effects/explosion/explosion_small.sound", WorldPosition );
		blast.Volume = 0.8f;
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
		SpectateTarget = null;
	}

	/// <summary>
	/// Start a ragdoll on every machine. The host decides when (CLAUDE.md: "the host decides when
	/// a ragdoll starts"); each machine then simulates its own cosmetic copy from the same pose
	/// and the same launch velocity, and the owner's copy is the one the player's root follows.
	/// </summary>
	[Rpc.Broadcast( NetFlags.HostOnly )]
	public void BeginRagdoll( Vector3 launchVelocity )
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

		_ragdollEnds = RagdollDuration;

		var thud = Sound.Play( "sounds/impacts/melee/impact-melee-flesh.sound", WorldPosition + Vector3.Up * 40f );
		thud.Volume = 0.9f;
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
	}

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
		var yaw = Rotation.FromYaw( _eyeAngles.yaw );
		var wishDir = Input.AnalogMove * yaw;
		if ( !wishDir.IsNearZeroLength )
			wishDir = wishDir.Normal;

		_isSprinting = Input.Down( "run" ) && !wishDir.IsNearZeroLength;
		_wishVelocity = wishDir * (_isSprinting ? SprintSpeed : WalkSpeed);

		// Assigned every tick so inspector edits take effect live during Play.
		_controller.Acceleration = GroundAcceleration;

		if ( _controller.IsOnGround )
		{
			_controller.ApplyFriction( GroundFriction, StopSpeed );
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
			// Don't bank a mid-air press — it would fire the instant we land.
			_jumpQueued = false;

			_controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
			_controller.Accelerate( _wishVelocity * AirControl );
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
		PlaceCamera( target, _eyeAngles.ToRotation(), CameraDistance );
	}

	void PlaceCamera( Vector3 target, Rotation rotation, float distance )
	{
		var camera = Scene.Camera;
		if ( !camera.IsValid() ) return;

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
			var land = Sound.Play( "sounds/footsteps/footstep-concrete-land.sound", WorldPosition );
			land.Volume = 0.9f;
			_strideDistance = 0f;
		}

		_wasGrounded = grounded;

		if ( !grounded ) return;

		var speed = _renderVelocity.WithZ( 0f ).Length;
		if ( speed < 60f ) return;

		_strideDistance += speed * Time.Delta;
		if ( _strideDistance < 72f ) return;

		_strideDistance = 0f;

		var step = Sound.Play( "sounds/footsteps/footstep-concrete.sound", WorldPosition );
		step.Volume = IsProxy ? 0.35f : 0.5f;
	}

	void PlayJumpSound()
	{
		var jump = Sound.Play( "sounds/footsteps/footstep-concrete-jump.sound", WorldPosition );
		jump.Volume = 0.6f;
	}

	protected override void OnDestroy()
	{
		if ( _ragdoll.IsValid() ) _ragdoll.Destroy();
	}
}
