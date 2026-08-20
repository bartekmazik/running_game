using Sandbox.Citizen;

/// <summary>
/// Player movement, third-person camera, and tile stepping. Builds its own
/// <see cref="CharacterController"/> and Citizen body at runtime — no prefabs.
/// Uses only the default input actions: forward, backward, left, right, jump.
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

	/// <summary>
	/// False once eliminated.
	///
	/// FromHost, not owner-synced: elimination is decided by LavaZone on the host, and the
	/// owning client has to be *told* it died. The setter is public because the sync generator
	/// assigns it — only the host may actually write it.
	/// </summary>
	[Sync( SyncFlags.FromHost ), Change( nameof( OnIsAliveChanged ) )]
	public bool IsAlive { get; set; } = true;

	/// <summary>
	/// Owner-synced. Proxies never run Move(), so their CharacterController's IsOnGround is
	/// permanently false — the animation needs the real value from whoever owns this player.
	/// </summary>
	[Sync] public bool IsGrounded { get; set; }

	/// <summary>
	/// Floor index under the player, or -1 outside the arena. Delegates to the one implementation
	/// of the index arithmetic in ArenaGenerator rather than repeating the formula.
	/// </summary>
	public int CurrentFloor =>
		_arena.TryGetTileIndex( WorldPosition, out var floor, out _, out _ ) ? floor : -1;

	[Property, Range( 80f, 600f )] public float CameraDistance { get; set; } = 220f;
	[Property, Range( 0f, 160f )] public float CameraTargetHeight { get; set; } = 56f;

	CharacterController _controller;
	SkinnedModelRenderer _body;
	CitizenAnimationHelper _animation;
	ArenaGenerator _arena;
	TileDecay _decay;

	Angles _eyeAngles = new Angles( 20f, 0f, 0f );

	/// <summary>Packed index of the tile we last reported, or -1 for none.</summary>
	int _lastTile = -1;

	/// <summary>Where we're asking to go, for the air arm-swing animation.</summary>
	Vector3 _wishVelocity;

	float _lastYaw;
	float _yawSpeed;

	Vector3 _lastPosition;
	Vector3 _renderVelocity;

	bool _isSprinting;

	/// <summary>
	/// Jump is latched in OnUpdate and consumed in OnFixedUpdate. Input.Pressed is true for
	/// exactly one frame, and at 50Hz fixed / 60+ fps render most frames run no fixed update
	/// at all — reading it directly in OnFixedUpdate silently drops presses.
	/// </summary>
	bool _jumpQueued;

	protected override void OnAwake()
	{
		_controller = GetOrAddComponent<CharacterController>();
		_controller.Height = 72f;
		_controller.Radius = 16f;
		_controller.StepHeight = 18f;

		// CharacterController exposes no collider of its own, so triggers can't see it.
		// This capsule mirrors the controller purely so LavaZone gets an overlap event.
		var capsule = GetOrAddComponent<CapsuleCollider>();
		capsule.Radius = 16f;
		capsule.Start = new Vector3( 0f, 0f, 16f );
		capsule.End = new Vector3( 0f, 0f, 56f );

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

		// Safe in OnAwake now: players are spawned at runtime by RoundManager, long after the
		// scene's own OnAwake pass, so ArenaGenerator.GridOrigin is already set. Storing refs
		// only — nothing here reads arena geometry.
		_arena = Scene.Get<ArenaGenerator>();
		_decay = Scene.Get<TileDecay>();
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
	/// Apply, not ApplyAsync: Apply skips clothing that isn't already downloaded, which keeps
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
		ClothingContainer.CreateFromConnection( owner, false ).Apply( _body );
	}

	protected override void OnUpdate()
	{
		// Animation runs before the proxy guard: remote players need moving legs too, and
		// everything it reads is either synced or derived from the replicated transform.
		UpdateRenderVelocity();
		UpdateAnimation();

		if ( IsProxy ) return;

		if ( IsAlive && Input.Pressed( "jump" ) )
			_jumpQueued = true;

		var look = _eyeAngles;
		look += Input.AnalogLook;
		look.pitch = look.pitch.Clamp( -80f, 80f );
		look.roll = 0f;
		_eyeAngles = look;

		// Degrees per second of turn, for the animgraph's foot shuffle.
		var yawDelta = MathX.DeltaDegrees( _lastYaw, _eyeAngles.yaw );
		_yawSpeed = Time.Delta > 0f ? -yawDelta / Time.Delta : 0f;
		_lastYaw = _eyeAngles.yaw;

		UpdateCamera();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		// Skipping Move() entirely is the freeze: no gravity, no input, no tile marking.
		// Look and camera keep running in OnUpdate so you can watch the rest of the round.
		if ( !IsAlive ) return;

		Move();

		IsGrounded = _controller.IsOnGround;

		UpdateTile();
	}

	/// <summary>Called by <see cref="LavaZone"/> on the host. Idempotent.</summary>
	public void Eliminate()
	{
		if ( !IsAlive ) return;

		// Host-only write. Motion is cleared by OnIsAliveChanged, which is the only mechanism
		// that reaches the owning client — clearing it here would only affect the host's copy.
		IsAlive = false;

		Log.Info( "TntPlayer: eliminated by lava" );
	}

	/// <summary>
	/// Fires wherever the synced value lands, so the owner stops itself the moment the host's
	/// elimination arrives.
	/// </summary>
	void OnIsAliveChanged( bool oldValue, bool newValue )
	{
		if ( newValue ) return;

		ClearMotion();
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
		WorldPosition = _arena.GetSpawnPosition( index, total );
		Transform.ClearInterpolation();

		ClearMotion();

		// Must reset, or landing on the tile we last reported would skip its NotifyStep and
		// that tile would never decay again.
		_lastTile = -1;
	}

	void ClearMotion()
	{
		_controller.Velocity = Vector3.Zero;
		_wishVelocity = Vector3.Zero;
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
			}
		}
		else
		{
			// Don't bank a mid-air press — it would fire the instant we land.
			_jumpQueued = false;

			_controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
			_controller.Accelerate( _wishVelocity * AirControl );
		}

		_controller.Move();

		WorldRotation = yaw;
	}

	/// <summary>
	/// Arithmetic tile lookup, then one NotifyStep per tile change (CLAUDE.md, decision 2).
	/// Only reports while grounded, so falling through a hole doesn't mark tiles in mid-air.
	/// </summary>
	void UpdateTile()
	{
		if ( !_controller.IsOnGround ) return;
		if ( !_arena.TryGetTileIndex( WorldPosition, out var floor, out var x, out var y ) ) return;

		var packed = TileDecay.PackIndex( floor, x, y );
		if ( packed == _lastTile ) return;

		_lastTile = packed;
		_decay.NotifyStep( floor, x, y );
	}

	void UpdateCamera()
	{
		var camera = Scene.Camera;
		if ( !camera.IsValid() ) return;

		var rot = _eyeAngles.ToRotation();
		var target = WorldPosition + Vector3.Up * CameraTargetHeight;

		camera.WorldPosition = target - rot.Forward * CameraDistance;
		camera.WorldRotation = rot;
	}

	/// <summary>
	/// The velocity the animgraph should see. The owner has an authoritative one on the
	/// controller; a proxy never runs Move(), so its controller velocity is permanently zero
	/// and the only signal available is how far the replicated transform moved this frame.
	/// </summary>
	void UpdateRenderVelocity()
	{
		if ( IsProxy )
		{
			var delta = WorldPosition - _lastPosition;
			_renderVelocity = Time.Delta > 0f ? delta / Time.Delta : Vector3.Zero;
		}
		else
		{
			_renderVelocity = _controller.Velocity;
		}

		_lastPosition = WorldPosition;
	}

	void UpdateAnimation()
	{
		_animation.WithVelocity( _renderVelocity );
		_animation.WithWishVelocity( _wishVelocity );
		// Frozen over the lava, so report grounded — otherwise the Citizen holds a falling
		// pose while stationary, which reads as a bug. IsGrounded is synced, so this is
		// correct for remote players too.
		_animation.IsGrounded = !IsAlive || IsGrounded;
		_animation.MoveRotationSpeed = _yawSpeed;

		// MoveStyles is { Auto, Walk, Run } — there is no sprint tier. Sprint is the top of
		// the run blend, so pin Run rather than let Auto blend back toward walk.
		_animation.MoveStyle = _isSprinting
			? CitizenAnimationHelper.MoveStyles.Run
			: CitizenAnimationHelper.MoveStyles.Auto;
	}

}
