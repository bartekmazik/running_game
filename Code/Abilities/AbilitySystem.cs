using System;

/// <summary>
/// What every ability actually does. Host-authoritative: <see cref="PlayerAbilities"/> asks,
/// <see cref="Execute"/> decides and acts, through the channels that already exist —
/// <see cref="PlayerImpacts.TryKnockDown"/> for ragdolls, <see cref="PlayerStatus"/> for timed
/// effects, <see cref="TntPlayer"/>'s owner RPCs for movement, <see cref="TileDecay"/> for the floor.
/// Presentation goes out as one broadcast per cast (<see cref="PlayFx"/>), so every machine draws
/// it locally from dev models.
///
/// Lives on the Arena object, created by RoundManager. Mines and bombs are host-side timers here.
/// </summary>
public sealed class AbilitySystem : Component
{
	/// <summary>"Nearest enemy" abilities look this far.</summary>
	const float TargetRange = 700f;

	/// <summary>Aim cone for targeted abilities (cosine) — roughly ±60°.</summary>
	const float AimCone = 0.5f;

	const float AreaRadius = 300f;

	const int MaxMinesPerPlayer = 2;
	const float MineLife = 25f;
	const float MineTriggerRadius = 44f;
	const float MineArmTime = 0.6f;

	const float BombFuse = 1.2f;
	const float BombRadius = 120f;

	sealed class MinePlacement
	{
		public int Id;
		public TntPlayer Owner;
		public Vector3 Position;
		public TimeSince Age;
	}

	sealed class BombFlight
	{
		public TntPlayer Owner;
		public Vector3 Target;
		public int Floor;
		public int X;
		public int Y;
		public TimeUntil Detonate;
	}

	readonly List<MinePlacement> _mines = new();
	readonly List<BombFlight> _bombs = new();
	int _nextMineId;

	/// <summary>Every machine: mine visuals by id, so the host can remove one everywhere.</summary>
	readonly Dictionary<int, GameObject> _mineVisuals = new();

	ArenaGenerator _arena;
	TileDecay _decay;
	PlayerImpacts _impacts;
	MatchScore _score;

	protected override void OnAwake()
	{
		_arena = GetComponent<ArenaGenerator>();
		_decay = GetComponent<TileDecay>();
		_impacts = GetComponent<PlayerImpacts>();
		_score = GetComponent<MatchScore>();
	}

	/// <summary>Host: clear mines and bombs, ready every slot. Called when a round or warm-up starts.</summary>
	public void ResetRound()
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;

		_mines.Clear();
		_bombs.Clear();
		ClearMineVisuals();

		foreach ( var player in Scene.GetAll<TntPlayer>() )
			player.Abilities.ResetCooldowns();
	}

	/// <summary>
	/// Host: run <paramref name="id"/> for <paramref name="caster"/> aiming along <paramref name="aim"/>.
	/// False means nothing happened (no target in range, nothing to rebuild) — no cooldown is spent.
	/// </summary>
	public bool Execute( TntPlayer caster, AbilityId id, Angles aim )
	{
		var forward = Rotation.FromYaw( aim.yaw ).Forward;
		var origin = caster.WorldPosition;
		var status = caster.Status;

		switch ( id )
		{
			case AbilityId.Dash:
				caster.Launch( forward * 720f + Vector3.Up * 140f );
				PlayFx( id, origin, forward, origin );
				return true;

			case AbilityId.SuperJump:
				caster.Launch( Vector3.Up * 560f );
				PlayFx( id, origin, forward, origin );
				return true;

			case AbilityId.DoubleJump:
				status.Apply( StatusKind.DoubleJump, 6f );
				PlayFx( id, origin, forward, origin );
				return true;

			case AbilityId.FeatherFall:
				status.Apply( StatusKind.FeatherFall, 4f );
				PlayFx( id, origin, forward, origin );
				return true;

			case AbilityId.GhostStep:
				status.Apply( StatusKind.Ghost, 2.5f );
				PlayFx( id, origin, forward, origin );
				return true;

			case AbilityId.Turbo:
				status.Apply( StatusKind.Turbo, 4f );
				PlayFx( id, origin, forward, origin );
				return true;

			case AbilityId.IronBody:
				status.Apply( StatusKind.Iron, 5f );
				PlayFx( id, origin, forward, origin );
				return true;

			case AbilityId.BubbleShield:
				status.Apply( StatusKind.Shield, 8f );
				PlayFx( id, origin, forward, origin );
				return true;

			case AbilityId.Giant:
				status.Apply( StatusKind.Giant, 5f );
				PlayFx( id, origin, forward, origin );
				return true;

			case AbilityId.Patch:
				return Patch( caster, forward );

			case AbilityId.Bridge:
				return Bridge( caster, forward );

			case AbilityId.Blink:
				return Blink( caster, forward );

			case AbilityId.Rewind:
				caster.RewindNow();
				PlayFx( id, origin, forward, origin );
				return true;

			case AbilityId.Updraft:
				caster.Launch( Vector3.Up * 760f );
				status.Apply( StatusKind.FeatherFall, 1.6f );
				PlayFx( id, origin, forward, origin );
				return true;

			case AbilityId.Shockwave:
				return Shockwave( caster, forward );

			case AbilityId.Shove:
				return Shove( caster, forward );

			case AbilityId.Hook:
				return Hook( caster, forward );

			case AbilityId.TntBomb:
				return ThrowBomb( caster, forward );

			case AbilityId.Sabotage:
				return Sabotage( caster, forward );

			case AbilityId.Trench:
				return Trench( caster, forward );

			case AbilityId.Glue:
				return AreaStatus( caster, id, StatusKind.Glue, 3f, AreaRadius, forward );

			case AbilityId.Ice:
				return AreaStatus( caster, id, StatusKind.Ice, 3f, AreaRadius, forward );

			case AbilityId.Blackout:
				return AreaStatus( caster, id, StatusKind.Blackout, 3f, AreaRadius + 150f, forward );

			case AbilityId.Confuse:
				return TargetStatus( caster, id, StatusKind.Confused, 3f, forward );

			case AbilityId.HeavyBoots:
				return TargetStatus( caster, id, StatusKind.HeavyBoots, 4f, forward );

			case AbilityId.Stun:
				return TargetStatus( caster, id, StatusKind.Stunned, 1.2f, forward );

			case AbilityId.Swap:
				return Swap( caster, forward );

			case AbilityId.Catapult:
				return Catapult( caster, forward );

			case AbilityId.Vortex:
				return Vortex( caster, forward );

			case AbilityId.Mine:
				return PlantMine( caster, forward );
		}

		return false;
	}

	// ---------------------------------------------------------------------------------------------
	// Targeting

	IEnumerable<TntPlayer> Enemies( TntPlayer caster ) =>
		Scene.GetAll<TntPlayer>().Where( p => p != caster && p.IsAlive );

	/// <summary>
	/// The enemy this ability should hit: the closest one inside the aim cone, or failing that the
	/// closest one at all. Being able to land a cast without pixel aim matters in a game where
	/// you're also sprinting for your life.
	/// </summary>
	TntPlayer NearestEnemy( TntPlayer caster, Vector3 forward, float range )
	{
		TntPlayer best = null;
		var bestScore = float.MaxValue;

		foreach ( var enemy in Enemies( caster ) )
		{
			var offset = enemy.WorldPosition - caster.WorldPosition;
			var distance = offset.Length;
			if ( distance > range ) continue;

			var inCone = distance > 1f && offset.WithZ( 0f ).Normal.Dot( forward ) > AimCone;
			var score = inCone ? distance : distance + range;

			if ( score >= bestScore ) continue;
			bestScore = score;
			best = enemy;
		}

		return best;
	}

	/// <summary>
	/// A harmful ability reaching <paramref name="target"/>. A Bubble Shield eats it — the shield
	/// pops and nothing else happens. Otherwise the hit is remembered for elimination credit and
	/// worth a few points.
	/// </summary>
	bool Lands( TntPlayer caster, TntPlayer target )
	{
		if ( target.Status.Has( StatusKind.Shield ) )
		{
			target.Status.Clear( StatusKind.Shield );
			PlayFx( AbilityId.BubbleShield, target.WorldPosition, Vector3.Forward, target.WorldPosition, true );
			return false;
		}

		_score?.RecordHit( caster, target );
		_score?.Award( caster, MatchScore.AbilityHitPoints, "Hit" );
		return true;
	}

	// ---------------------------------------------------------------------------------------------
	// Help

	bool Patch( TntPlayer caster, Vector3 forward )
	{
		if ( !_arena.TryGetFloorBelow( caster.WorldPosition, out var floor ) ) return false;
		if ( !_arena.TryGetColumn( caster.WorldPosition, out var cx, out var cy ) ) return false;

		var tiles = new List<int>();
		for ( var dx = -1; dx <= 1; dx++ )
			for ( var dy = -1; dy <= 1; dy++ )
				AddTile( tiles, floor, cx + dx, cy + dy );

		_decay.Restore( tiles );
		var centre = _arena.TileTop( floor, cx, cy );
		PlayFx( AbilityId.Patch, centre, forward, centre );
		return true;
	}

	bool Bridge( TntPlayer caster, Vector3 forward )
	{
		if ( !_arena.TryGetFloorBelow( caster.WorldPosition, out var floor ) ) return false;

		var tiles = TilesAlong( caster.WorldPosition, forward, floor, 1, 6 );
		if ( tiles.Count == 0 ) return false;

		_decay.Restore( tiles );

		var start = caster.WorldPosition.WithZ( _arena.TileTop( floor, 0, 0 ).z );
		PlayFx( AbilityId.Bridge, start, forward, start + forward * Arena.TileSize * 6f );
		return true;
	}

	bool Blink( TntPlayer caster, Vector3 forward )
	{
		var from = caster.WorldPosition;
		var to = from + forward * Arena.TileSize * 5f;

		// Stay inside the arena's footprint: a blink into the void would be an own goal, not a dodge.
		var half = Arena.GridSize * Arena.TileSize * 0.5f - Arena.TileSize * 0.5f;
		var centre = _arena.WorldPosition;
		to = new Vector3( to.x.Clamp( centre.x - half, centre.x + half ), to.y.Clamp( centre.y - half, centre.y + half ), from.z + 6f );

		caster.TeleportTo( to );
		PlayFx( AbilityId.Blink, from, forward, to );
		return true;
	}

	// ---------------------------------------------------------------------------------------------
	// Harm

	bool Shockwave( TntPlayer caster, Vector3 forward )
	{
		var origin = caster.WorldPosition;

		// An area blast always goes off, target or not — the shape of the threat is the point.
		foreach ( var enemy in Enemies( caster ).ToList() )
		{
			var offset = enemy.WorldPosition - origin;
			if ( offset.Length > 240f || MathF.Abs( offset.z ) > 80f ) continue;
			if ( !Lands( caster, enemy ) ) continue;

			var away = offset.WithZ( 0f ).IsNearZeroLength ? forward : offset.WithZ( 0f ).Normal;
			_impacts.TryKnockDown( enemy, away * 480f + Vector3.Up * 260f, 1.5f, caster );
		}

		PlayFx( AbilityId.Shockwave, origin, forward, origin );
		return true;
	}

	bool Shove( TntPlayer caster, Vector3 forward )
	{
		var origin = caster.WorldPosition;
		var hitAny = false;

		foreach ( var enemy in Enemies( caster ).ToList() )
		{
			var offset = enemy.WorldPosition - origin;
			if ( offset.Length > 170f || MathF.Abs( offset.z ) > 70f ) continue;
			if ( offset.WithZ( 0f ).Normal.Dot( forward ) < AimCone ) continue;
			if ( !Lands( caster, enemy ) ) continue;

			_impacts.TryKnockDown( enemy, forward * 560f + Vector3.Up * 220f, 1.4f, caster );
			hitAny = true;
		}

		if ( !hitAny ) return false;

		PlayFx( AbilityId.Shove, origin, forward, origin + forward * 120f );
		return true;
	}

	bool Hook( TntPlayer caster, Vector3 forward )
	{
		var target = NearestEnemy( caster, forward, TargetRange );
		if ( target is null ) return false;

		var from = target.WorldPosition;
		if ( Lands( caster, target ) )
		{
			var toCaster = (caster.WorldPosition - from).WithZ( 0f );
			var pull = MathF.Min( toCaster.Length * 1.6f, 640f );
			target.Launch( toCaster.Normal * pull + Vector3.Up * 220f );
		}

		PlayFx( AbilityId.Hook, caster.WorldPosition, forward, from );
		return true;
	}

	bool ThrowBomb( TntPlayer caster, Vector3 forward )
	{
		if ( !_arena.TryGetFloorBelow( caster.WorldPosition, out var floor ) ) return false;

		var landing = caster.WorldPosition + forward * Arena.TileSize * 5f;
		if ( !_arena.TryGetColumn( landing, out var x, out var y ) ) return false;

		var target = _arena.TileTop( floor, x, y );

		_bombs.Add( new BombFlight
		{
			Owner = caster,
			Target = target,
			Floor = floor,
			X = x,
			Y = y,
			Detonate = BombFuse
		} );

		PlayFx( AbilityId.TntBomb, caster.WorldPosition + Vector3.Up * 50f, forward, target );
		return true;
	}

	void Detonate( BombFlight bomb )
	{
		var tiles = new List<int>();
		for ( var dx = -1; dx <= 1; dx++ )
			for ( var dy = -1; dy <= 1; dy++ )
				AddTile( tiles, bomb.Floor, bomb.X + dx, bomb.Y + dy );

		_decay.ForceCollapse( tiles, 0.05f );

		foreach ( var enemy in Scene.GetAll<TntPlayer>().Where( p => p.IsAlive ).ToList() )
		{
			var offset = enemy.WorldPosition - bomb.Target;
			if ( offset.Length > BombRadius ) continue;
			if ( enemy != bomb.Owner && !Lands( bomb.Owner, enemy ) ) continue;

			var away = offset.WithZ( 0f ).IsNearZeroLength ? Vector3.Forward : offset.WithZ( 0f ).Normal;
			_impacts.TryKnockDown( enemy, away * 380f + Vector3.Up * 340f, 1.5f, enemy == bomb.Owner ? null : bomb.Owner );
		}
	}

	bool Sabotage( TntPlayer caster, Vector3 forward )
	{
		var target = NearestEnemy( caster, forward, 650f );
		if ( target is null ) return false;
		if ( !_arena.TryGetFloorBelow( target.WorldPosition, out var floor ) ) return false;
		if ( !_arena.TryGetColumn( target.WorldPosition, out var cx, out var cy ) ) return false;

		if ( Lands( caster, target ) )
		{
			var tiles = new List<int>();
			for ( var dx = -1; dx <= 1; dx++ )
				for ( var dy = -1; dy <= 1; dy++ )
					AddTile( tiles, floor, cx + dx, cy + dy );

			_decay.ForceCollapse( tiles, 0.6f );
		}

		PlayFx( AbilityId.Sabotage, caster.WorldPosition, forward, target.WorldPosition );
		return true;
	}

	bool Trench( TntPlayer caster, Vector3 forward )
	{
		if ( !_arena.TryGetFloorBelow( caster.WorldPosition, out var floor ) ) return false;

		var tiles = TilesAlong( caster.WorldPosition, forward, floor, 2, 9 );
		if ( tiles.Count == 0 ) return false;

		// Rolling collapse, nearest first, so it reads as a crack running away from you.
		for ( var i = 0; i < tiles.Count; i++ )
			_decay.ForceCollapse( new[] { tiles[i] }, 0.4f + i * 0.05f );

		var start = caster.WorldPosition + forward * Arena.TileSize * 2f;
		PlayFx( AbilityId.Trench, start, forward, caster.WorldPosition + forward * Arena.TileSize * 9f );
		return true;
	}

	bool AreaStatus( TntPlayer caster, AbilityId id, StatusKind kind, float seconds, float radius, Vector3 forward )
	{
		var origin = caster.WorldPosition;

		foreach ( var enemy in Enemies( caster ).ToList() )
		{
			if ( enemy.WorldPosition.Distance( origin ) > radius ) continue;
			if ( !Lands( caster, enemy ) ) continue;

			enemy.Status.Apply( kind, seconds );
		}

		PlayFx( id, origin, forward, origin + Vector3.Up * radius );
		return true;
	}

	bool TargetStatus( TntPlayer caster, AbilityId id, StatusKind kind, float seconds, Vector3 forward )
	{
		var target = NearestEnemy( caster, forward, TargetRange );
		if ( target is null ) return false;

		if ( Lands( caster, target ) )
			target.Status.Apply( kind, seconds );

		PlayFx( id, caster.WorldPosition, forward, target.WorldPosition );
		return true;
	}

	bool Swap( TntPlayer caster, Vector3 forward )
	{
		var target = NearestEnemy( caster, forward, 900f );
		if ( target is null ) return false;

		var mine = caster.WorldPosition;
		var theirs = target.WorldPosition;

		if ( Lands( caster, target ) )
		{
			caster.TeleportTo( theirs + Vector3.Up * 4f );
			target.TeleportTo( mine + Vector3.Up * 4f );
		}

		PlayFx( AbilityId.Swap, mine, forward, theirs );
		return true;
	}

	bool Catapult( TntPlayer caster, Vector3 forward )
	{
		var target = NearestEnemy( caster, forward, TargetRange );
		if ( target is null ) return false;

		if ( Lands( caster, target ) )
		{
			var sideways = (target.WorldPosition - caster.WorldPosition).WithZ( 0f );
			var push = sideways.IsNearZeroLength ? forward : sideways.Normal;
			_impacts.TryKnockDown( target, push * 220f + Vector3.Up * 720f, 1.8f, caster );
		}

		PlayFx( AbilityId.Catapult, caster.WorldPosition, forward, target.WorldPosition );
		return true;
	}

	bool Vortex( TntPlayer caster, Vector3 forward )
	{
		var centre = caster.WorldPosition + forward * Arena.TileSize * 4f;

		foreach ( var enemy in Enemies( caster ).ToList() )
		{
			var offset = enemy.WorldPosition - centre;
			if ( offset.WithZ( 0f ).Length > 380f || MathF.Abs( offset.z ) > 90f ) continue;
			if ( !Lands( caster, enemy ) ) continue;

			enemy.Status.Apply( StatusKind.Vortex, 2f, centre );
		}

		PlayFx( AbilityId.Vortex, centre, forward, centre );
		return true;
	}

	bool PlantMine( TntPlayer caster, Vector3 forward )
	{
		if ( !caster.IsGrounded ) return false;

		var owned = _mines.Where( m => m.Owner == caster ).ToList();
		if ( owned.Count >= MaxMinesPerPlayer )
			RemoveMine( owned[0] );

		var mine = new MinePlacement
		{
			Id = ++_nextMineId,
			Owner = caster,
			Position = caster.WorldPosition,
			Age = 0f
		};

		_mines.Add( mine );
		ShowMine( mine.Id, mine.Position );
		PlayFx( AbilityId.Mine, mine.Position, forward, mine.Position );
		return true;
	}

	void RemoveMine( MinePlacement mine )
	{
		_mines.Remove( mine );
		HideMine( mine.Id );
	}

	// ---------------------------------------------------------------------------------------------
	// Host tick: mines and bombs

	protected override void OnFixedUpdate()
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;

		for ( var i = _bombs.Count - 1; i >= 0; i-- )
		{
			if ( !_bombs[i].Detonate ) continue;

			var bomb = _bombs[i];
			_bombs.RemoveAt( i );
			Detonate( bomb );
		}

		for ( var i = _mines.Count - 1; i >= 0; i-- )
		{
			var mine = _mines[i];

			if ( mine.Age > MineLife || !mine.Owner.IsValid() || !_arena.TryGetFloorBelow( mine.Position, out var floor )
				|| !_arena.TryGetColumn( mine.Position, out var x, out var y ) || !_arena.IsTileStanding( floor, x, y ) )
			{
				RemoveMine( mine );
				continue;
			}

			if ( mine.Age < MineArmTime ) continue;

			var victim = Enemies( mine.Owner ).FirstOrDefault( p =>
				p.WorldPosition.WithZ( 0f ).Distance( mine.Position.WithZ( 0f ) ) < MineTriggerRadius &&
				MathF.Abs( p.WorldPosition.z - mine.Position.z ) < 60f );

			if ( victim is null ) continue;

			RemoveMine( mine );

			if ( Lands( mine.Owner, victim ) )
			{
				var away = (victim.WorldPosition - mine.Position).WithZ( 0f );
				var push = away.IsNearZeroLength ? Vector3.Forward : away.Normal;
				_impacts.TryKnockDown( victim, push * 260f + Vector3.Up * 560f, 1.7f, mine.Owner );
			}

			PlayFx( AbilityId.Mine, mine.Position, Vector3.Forward, mine.Position, true );
		}
	}

	// ---------------------------------------------------------------------------------------------
	// Tile helpers

	void AddTile( List<int> tiles, int floor, int x, int y )
	{
		if ( x < 0 || y < 0 || x >= Arena.GridSize || y >= Arena.GridSize ) return;
		tiles.Add( TileDecay.PackIndex( floor, x, y ) );
	}

	/// <summary>Distinct tiles under a straight line from a player, from step <paramref name="first"/> to <paramref name="last"/> tiles out.</summary>
	List<int> TilesAlong( Vector3 origin, Vector3 forward, int floor, int first, int last )
	{
		var tiles = new List<int>();

		for ( var step = first; step <= last; step++ )
		{
			var point = origin + forward * Arena.TileSize * step;
			if ( !_arena.TryGetColumn( point, out var x, out var y ) ) continue;

			var packed = TileDecay.PackIndex( floor, x, y );
			if ( !tiles.Contains( packed ) ) tiles.Add( packed );
		}

		return tiles;
	}

	// ---------------------------------------------------------------------------------------------
	// Presentation — broadcast once, drawn locally everywhere

	/// <summary>
	/// One message per cast. <paramref name="from"/> and <paramref name="to"/> mean whatever the
	/// ability needs: caster and target, line start and end, or the same point twice.
	/// <paramref name="secondary"/> picks the follow-up look — a shield popping, a mine going off.
	/// </summary>
	void PlayFx( AbilityId id, Vector3 from, Vector3 forward, Vector3 to, bool secondary = false )
	{
		BroadcastFx( id, from, forward, to, secondary );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void BroadcastFx( AbilityId id, Vector3 from, Vector3 forward, Vector3 to, bool secondary )
	{
		var info = AbilityCatalog.Get( id );
		var colour = AbilityCatalog.KindColor( info.Kind );
		var chest = Vector3.Up * 40f;

		if ( secondary )
		{
			if ( id == AbilityId.BubbleShield )
			{
				AbilityFx.Flash( from + chest, AbilityFx.Sky, 110f );
				AbilityFx.Burst( from + chest, AbilityFx.Sky, 10, 200f, 8f );
				Sfx.KenneyAt( Sfx.Pop, from, 0.9f );
			}
			else if ( id == AbilityId.Mine )
			{
				AbilityFx.Burst( from, AbilityFx.Coral, 12, 320f, 11f );
				AbilityFx.Ring( from, 90f, AbilityFx.Butter );
				Sfx.At( Sfx.Blast, from, 0.8f, 1.3f );
			}

			return;
		}

		Sfx.KenneyAt( info.Kind == AbilityKind.Help ? Sfx.Cast : Sfx.CastHarm, from, 0.8f );

		switch ( id )
		{
			case AbilityId.Dash:
				AbilityFx.Line( from + chest, from + chest + forward * 140f, AbilityFx.Mint, 24f, 10f );
				break;

			case AbilityId.SuperJump:
			case AbilityId.Updraft:
				AbilityFx.Column( from, AbilityFx.Sky, id == AbilityId.Updraft ? 300f : 200f );
				AbilityFx.Ring( from, 70f, AbilityFx.Sky );
				break;

			case AbilityId.DoubleJump:
			case AbilityId.FeatherFall:
			case AbilityId.GhostStep:
			case AbilityId.Turbo:
			case AbilityId.IronBody:
			case AbilityId.Giant:
			case AbilityId.Rewind:
				AbilityFx.Ring( from, 80f, colour );
				AbilityFx.Burst( from + chest, colour, 6, 160f, 7f );
				break;

			case AbilityId.BubbleShield:
				AbilityFx.Flash( from + chest, AbilityFx.Sky, 90f );
				break;

			case AbilityId.Patch:
				AbilityFx.Ring( from, 100f, AbilityFx.Mint );
				AbilityFx.Burst( from, AbilityFx.Mint, 10, 180f, 8f );
				break;

			case AbilityId.Bridge:
				AbilityFx.Line( from, to, AbilityFx.Mint, 30f, 14f );
				break;

			case AbilityId.Blink:
				AbilityFx.Flash( from + chest, AbilityFx.Lilac, 70f );
				AbilityFx.Line( from + chest, to + chest, AbilityFx.Lilac, 40f, 8f );
				AbilityFx.Flash( to + chest, AbilityFx.Lilac, 70f );
				break;

			case AbilityId.Shockwave:
				AbilityFx.Ring( from, 240f, AbilityFx.Coral, 0.5f );
				AbilityFx.Ring( from, 160f, AbilityFx.Butter, 0.35f );
				break;

			case AbilityId.Shove:
				AbilityFx.Line( from + chest, to + chest, AbilityFx.Coral, 26f, 16f );
				break;

			case AbilityId.Hook:
				AbilityFx.Line( from + chest, to + chest, AbilityFx.Coral, 30f, 7f );
				break;

			case AbilityId.TntBomb:
				AbilityFx.Bomb( from, to, BombFuse );
				break;

			case AbilityId.Sabotage:
			case AbilityId.Confuse:
			case AbilityId.HeavyBoots:
			case AbilityId.Stun:
			case AbilityId.Catapult:
				AbilityFx.Line( from + chest, to + chest, colour, 36f, 7f );
				AbilityFx.Flash( to + chest, colour, 80f );
				if ( id == AbilityId.Catapult ) AbilityFx.Column( to, AbilityFx.Coral, 260f );
				break;

			case AbilityId.Trench:
				AbilityFx.Line( from, to, AbilityFx.Coral, 28f, 12f );
				break;

			case AbilityId.Glue:
				AbilityFx.Ring( from, AreaRadius, AbilityFx.Glue, 0.6f );
				break;

			case AbilityId.Ice:
				AbilityFx.Ring( from, AreaRadius, AbilityFx.Ice, 0.6f );
				AbilityFx.Burst( from + chest, AbilityFx.Ice, 10, 220f, 8f );
				break;

			case AbilityId.Blackout:
				AbilityFx.Ring( from, AreaRadius + 150f, AbilityFx.Ink, 0.6f );
				break;

			case AbilityId.Swap:
				AbilityFx.Flash( from + chest, AbilityFx.Lilac, 70f );
				AbilityFx.Flash( to + chest, AbilityFx.Lilac, 70f );
				AbilityFx.Line( from + chest, to + chest, AbilityFx.Lilac, 40f, 8f );
				break;

			case AbilityId.Vortex:
				AbilityFx.Whirl( from, 140f, AbilityFx.Lilac, 2f );
				AbilityFx.Ring( from, 380f, AbilityFx.Lilac, 0.6f );
				break;

			case AbilityId.Mine:
				AbilityFx.Ring( from, 40f, AbilityFx.Coral, 0.3f );
				break;
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void ShowMine( int id, Vector3 position )
	{
		_mineVisuals[id] = AbilityFx.Mine( position );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void HideMine( int id )
	{
		if ( !_mineVisuals.TryGetValue( id, out var visual ) ) return;
		_mineVisuals.Remove( id );
		if ( visual.IsValid() ) visual.Destroy();
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void ClearMineVisuals()
	{
		foreach ( var visual in _mineVisuals.Values )
		{
			if ( visual.IsValid() ) visual.Destroy();
		}

		_mineVisuals.Clear();
	}
}
