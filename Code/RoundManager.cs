using Sandbox.Network;

/// <summary>
/// Round state machine, lobby creation, and per-connection player spawning. Host-authoritative.
///
/// Waiting is a warm-up: the floor is intact and doesn't decay, and falling into the lava just
/// puts you back on top. It gates on <see cref="MinPlayers"/> and then runs a
/// <see cref="CountdownDuration"/> countdown. Both are written purely in terms of MinPlayers, so
/// 1, 2 and 24 all take the same path. Ended shows the results, then routes back through Waiting
/// rather than straight to Playing, so the gate and the countdown apply between rounds too.
/// </summary>
public sealed class RoundManager : Component, Component.INetworkListener
{
	public enum RoundState
	{
		Waiting,
		Playing,
		Ended
	}

	/// <summary>One line of the elimination feed, stamped locally when it arrives.</summary>
	public readonly struct FeedEntry
	{
		public readonly string Name;
		public readonly int Remaining;
		public readonly float Time;

		/// <summary>Who knocked them in, if anyone did within the credit window. Empty otherwise.</summary>
		public readonly string By;

		public FeedEntry( string name, int remaining, float time, string by )
		{
			Name = name;
			Remaining = remaining;
			Time = time;
			By = by;
		}
	}

	/// <summary>One row of the results table, set on every machine at Ended.</summary>
	public readonly struct ResultRow
	{
		public readonly string Name;
		public readonly int Points;
		public readonly int Knockdowns;
		public readonly int RankPoints;

		public ResultRow( string name, int points, int knockdowns, int rankPoints )
		{
			Name = name;
			Points = points;
			Knockdowns = knockdowns;
			RankPoints = rankPoints;
		}
	}

	/// <summary>
	/// Players required to start. Drop to 1 for solo testing — nothing below assumes 2, the
	/// countdown and the win condition are both written in terms of this number.
	/// </summary>
	[Property, Range( 1, 24 )] public int MinPlayers { get; set; } = 2;

	/// <summary>Seconds between reaching MinPlayers and the round starting. 0 starts instantly.</summary>
	[Property, Range( 0f, 30f )] public float CountdownDuration { get; set; } = 10f;

	/// <summary>How long the results stay up before the warm-up starts again.</summary>
	[Property, Range( 0.5f, 20f )] public float RestartDelay { get; set; } = 8f;

	[Property, Range( 2, 64 )] public int MaxPlayers { get; set; } = 24;

	public RoundState State { get; private set; } = RoundState.Waiting;

	/// <summary>Winner's display name, or empty for no survivors. Set on every machine.</summary>
	public string LastWinner { get; private set; } = "";

	/// <summary>
	/// Final standings of the last round, winner first, set on every machine at Ended. Names only:
	/// a placement outlives the player object if they leave while the results are up.
	/// </summary>
	public IReadOnlyList<string> LastResults => _lastResults;

	/// <summary>Points, knockdowns and rank for each row of <see cref="LastResults"/>, same order.</summary>
	public IReadOnlyList<ResultRow> LastRows => _lastRows;

	/// <summary>Whether the last round counted towards rank (it had at least two starters).</summary>
	public bool LastRoundRanked { get; private set; }

	/// <summary>How long the last round lasted, as the host measured it.</summary>
	public float LastRoundDuration { get; private set; }

	/// <summary>Recent eliminations, newest last. Every machine keeps its own copy.</summary>
	public IReadOnlyList<FeedEntry> Feed => _feed;

	/// <summary>Bumped on every feed change, so the HUD can fold it into its BuildHash.</summary>
	public int FeedRevision { get; private set; }

	/// <summary>True between pressing Host and the lobby coming up — the menu shows a spinner.</summary>
	public bool IsStartingLobby => _lobbyRequested && !Networking.IsActive;

	ArenaGenerator _arena;
	TileDecay _decay;
	MatchScore _score;
	AbilitySystem _abilities;


	TimeUntil _restartAt;

	TimeUntil _startAt;

	/// <summary>TimeUntil can't distinguish "unset" from "expired", hence the companion flag.</summary>
	bool _countingDown;

	TimeSince _roundElapsed;

	/// <summary>Headcount when the round began — decides what "last alive" means.</summary>
	int _playersAtRoundStart;

	/// <summary>
	/// Who fell this round, first out first, by player object id — not by name, since two players
	/// can share a Steam name — and when each fell. Kept on every machine, not just the host, so
	/// that if the host leaves mid-round and the session migrates, the new host can still write
	/// the standings. Same reason <see cref="_playersAtRoundStart"/> is broadcast.
	/// </summary>
	readonly List<System.Guid> _eliminatedIds = new();
	readonly Dictionary<System.Guid, float> _fellAt = new();

	string[] _lastResults = System.Array.Empty<string>();
	ResultRow[] _lastRows = System.Array.Empty<ResultRow>();

	readonly List<FeedEntry> _feed = new();

	const int MaxFeedEntries = 5;

	/// <summary>Host-only: when each player was last sent back up from the lava in the warm-up.</summary>
	readonly Dictionary<TntPlayer, TimeSince> _lavaRespawns = new();

	const float LavaRespawnCooldown = 1f;

	IEnumerable<TntPlayer> Players => Scene.GetAll<TntPlayer>();

	// Public for the HUD. Every client can count networked players in its own scene, so these
	// are correct everywhere, not just on the host.
	public int PlayerCount => Players.Count();

	public int AliveCount => Players.Count( p => p.IsAlive );

	public bool IsCountingDown => _countingDown;

	public float CountdownRemaining => _startAt.Relative;

	public float RoundElapsed => _roundElapsed.Relative;

	public float RestartRemaining => _restartAt.Relative;

	/// <summary>
	/// How many survivors the round can tolerate before it's over. With 2+ starters the round
	/// ends when one is left standing; solo, it ends only when that player is dead. Generalises
	/// to N without special-casing at the call site.
	/// </summary>
	int SurvivorsToEndRound => _playersAtRoundStart > 1 ? 1 : 0;

	bool _lobbyRequested;

	bool _wasActive;

	/// <summary>Last alive count pushed to the lobby, so we only republish when it moves.</summary>
	int _publishedAlive = -1;

	protected override void OnAwake()
	{
		// Refs only — nothing here reads arena geometry or tiles. Resolved in OnAwake rather
		// than OnStart because OnActive can fire before OnStart on a fast local connection.
		_arena = Scene.Get<ArenaGenerator>();
		_decay = Scene.Get<TileDecay>();

		// Runtime-built like everything else: the scene holds exactly three objects. Order matters
		// where one component's OnAwake looks up another: the profile and the score come first.
		GameObject.GetOrAddComponent<ProfileStore>();
		_score = GameObject.GetOrAddComponent<MatchScore>();
		GameObject.GetOrAddComponent<TileWarning>();
		GameObject.GetOrAddComponent<PlayerImpacts>();
		_abilities = GameObject.GetOrAddComponent<AbilitySystem>();
		GameObject.GetOrAddComponent<MenuCamera>();

		CreateUi();
	}

	/// <summary>
	/// Built in code rather than placed in the scene, same as the arena. Runs on every machine
	/// — the UI is purely local presentation, so it is never networked or saved.
	///
	/// One ScreenPanel for the whole game. GameUi is the only PanelComponent in the project;
	/// every screen under it is a plain Panel, because PanelComponents cannot be nested.
	/// </summary>
	void CreateUi()
	{
		var ui = new GameObject( true, "UI" );
		ui.Flags |= GameObjectFlags.NotSaved;
		ui.NetworkMode = NetworkMode.Never;

		ui.AddComponent<ScreenPanel>();
		ui.AddComponent<GameUi>();
	}

	/// <summary>
	/// The menu's Host button. Lobby creation used to be an editor toggle that fired on the first
	/// tick; it is an explicit choice now, so a player who wants to browse never hosts by accident.
	/// </summary>
	public void Host()
	{
		if ( Networking.IsActive ) return;
		if ( _lobbyRequested ) return;

		_lobbyRequested = true;

		Networking.CreateLobby( new LobbyConfig
		{
			MaxPlayers = MaxPlayers,
			Privacy = LobbyPrivacy.Public,
			Name = $"{Connection.Local?.DisplayName ?? "TNT Run"}'s TNT Run"
		} );

		Log.Info( "Round: creating lobby" );
	}

	/// <summary>Leave the session and go back to the menu. Works for host and client alike.</summary>
	public void Leave()
	{
		_lobbyRequested = false;
		Networking.Disconnect();
	}

	/// <summary>
	/// Spawn one player per connection, owned by that connection. Host-only by contract —
	/// INetworkListener only fires on the host.
	///
	/// No prefab: CLAUDE.md forbids prefab files, so the GameObject is assembled in code and
	/// TntPlayer.OnAwake builds its own controller, collider and body on every machine.
	/// </summary>
	public void OnActive( Connection connection )
	{
		// This connection isn't counted yet, so index == current count.
		var index = PlayerCount;

		var player = new GameObject( true, $"Player - {connection.DisplayName}" );
		player.Tags.Add( "player" );
		player.WorldPosition = _arena.GetSpawnPosition( index, index + 1 );

		var tnt = player.AddComponent<TntPlayer>();

		// Joining a round already under way makes you a spectator until the next warm-up —
		// otherwise a late joiner would count towards "last alive" in a round they never started.
		if ( State != RoundState.Waiting )
			tnt.IsAlive = false;

		player.NetworkSpawn( connection );

		// Round state is broadcast, not synced, so a joiner hears nothing until the next change.
		// Must go before the tile snapshot: nothing in it resets the arena, but keeping state
		// first means a future reset-on-state can't silently wipe the snapshot.
		// The host's own connection already holds the live state. Sending it to itself would land a
		// frame later and could stomp a countdown that started in between — seen in testing as the
		// countdown announcing twice.
		if ( connection != Connection.Local )
		{
			using ( Rpc.FilterInclude( connection ) )
			{
				ApplyJoinState( State, _roundElapsed.Relative, _countingDown, _startAt.Relative );

				// Joining on the results screen: they'd otherwise see an empty table.
				if ( State == RoundState.Ended )
				{
					AnnounceResults( _lastResults,
						_lastRows.Select( r => r.Points ).ToArray(),
						_lastRows.Select( r => r.Knockdowns ).ToArray(),
						_lastRows.Select( r => r.RankPoints ).ToArray(),
						LastRoundDuration, LastWinner, LastRoundRanked );
				}
			}
		}

		// Their arena is generated locally and starts pristine, so hand them the current
		// collapsed set. Costs nothing when the arena is intact — no message goes out.
		// Same for the scoreboard and any mines already planted.
		_decay.SendSnapshotTo( connection );
		_score.SendSnapshotTo( connection );
		_abilities.SendSnapshotTo( connection );

		Log.Info( $"Round: spawned player for {connection.DisplayName}" );
	}

	public void OnDisconnected( Connection connection )
	{
		// Their player object is cleaned up automatically — NetworkOrphaned defaults to
		// Destroy, so an orphaned owned object goes away on its own.
		Log.Info( $"Round: {connection.DisplayName} left" );
	}

	protected override void OnUpdate()
	{
		// The session ended under us — we left, the host left, or the connection dropped.
		// Reloading the scene is the one reset that can't miss anything: arena, players,
		// round state, UI all come back exactly as a fresh launch has them, menu up.
		if ( _wasActive && !Networking.IsActive && !Networking.IsConnecting )
		{
			Log.Info( "Round: session ended, back to menu" );
			Scene.LoadFromFile( "scenes/minimal.scene" );
			return;
		}

		_wasActive = Networking.IsActive;

		// No session yet: the menu is up and there is nothing to run.
		if ( !Networking.IsActive ) return;
		if ( !Networking.IsHost ) return;

		if ( AliveCount != _publishedAlive ) PublishLobbyData();

		CheckLava();

		switch ( State )
		{
			case RoundState.Waiting:
				TickWaiting();
				break;

			case RoundState.Playing:
				TickPlaying();
				break;

			case RoundState.Ended:
				TickEnded();
				break;
		}
	}

	/// <summary>
	/// Height check on top of LavaZone's trigger. The trigger needs a solid capsule to overlap,
	/// and a ragdolled player has switched theirs off — so anything below the lava's surface is
	/// in the lava, whatever its collider is doing. Arithmetic, like the tile lookup.
	/// </summary>
	void CheckLava()
	{
		var lavaZ = _arena.LavaTopZ + 4f;

		foreach ( var player in Players )
		{
			if ( player.IsAlive && player.WorldPosition.z < lavaZ )
				HandleLava( player );
		}
	}

	/// <summary>
	/// The one path for touching the lava, from the trigger or the height check. During a round
	/// it eliminates; in the warm-up and on the results screen it just puts you back on top.
	/// </summary>
	public void HandleLava( TntPlayer player )
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;
		if ( !player.IsAlive ) return;

		if ( State != RoundState.Playing )
		{
			// Respawn is an RPC to the owner, and the host keeps seeing the old position for a round
			// trip — without this, CheckLava would re-send it every frame until the move lands.
			if ( _lavaRespawns.TryGetValue( player, out var since ) && since < LavaRespawnCooldown ) return;
			_lavaRespawns[player] = 0f;

			var players = Players.ToList();
			player.Respawn( players.IndexOf( player ), players.Count );
			return;
		}

		player.Eliminate();
		player.Status.ClearAll();

		// Pushed, shoved or sabotaged in the last few seconds? Whoever did it gets the credit.
		var by = _score.ClaimElimination( player );
		if ( by is not null )
			_score.Award( by, MatchScore.EliminationPoints, "Elimination" );

		AnnounceElimination( player.DisplayName, player.GameObject.Id, AliveCount, by?.DisplayName ?? "" );
	}

	void TickWaiting()
	{
		// Everything here is expressed in terms of MinPlayers, so this behaves identically at
		// 1, 2 or 24 — a lone player at MinPlayers 1 counts down and starts exactly the same way.
		if ( PlayerCount < MinPlayers )
		{
			CancelCountdown();
			return;
		}

		if ( !_countingDown )
		{
			AnnounceCountdown( CountdownDuration, PlayerCount, MinPlayers );
		}

		// Players joining mid-countdown are welcome and get picked up by StartRound's snapshot;
		// only dropping back below MinPlayers aborts.
		if ( !_startAt ) return;

		StartRound();
	}

	void CancelCountdown()
	{
		if ( !_countingDown ) return;

		AnnounceCountdownCancelled( PlayerCount, MinPlayers );
	}

	void TickPlaying()
	{
		if ( AliveCount > SurvivorsToEndRound ) return;

		EndRound();
	}

	void TickEnded()
	{
		if ( !_restartAt ) return;

		SetState( RoundState.Waiting );
		RespawnEveryone();
		_abilities.ResetRound();
	}

	void StartRound()
	{
		// Broadcast first: every machine resets its own arena as part of entering Playing,
		// so nobody respawns onto tiles that are still collapsed locally.
		SetState( RoundState.Playing );

		_score.ResetRound();
		_abilities.ResetRound();

		AnnounceRoundStart( RespawnEveryone() );
	}

	/// <summary>
	/// Everyone back on floor 0, alive, spread around the ring. Used for both the warm-up and the
	/// round proper, so a spectator who joined mid-round gets a body back at the next warm-up.
	/// Returns the headcount.
	/// </summary>
	int RespawnEveryone()
	{
		// Snapshot the list so index and total agree, and so ring slots are stable within
		// this call even though Scene.GetAll order isn't guaranteed.
		var players = Players.ToList();

		for ( var i = 0; i < players.Count; i++ )
		{
			// IsAlive is FromHost, so it has to be written here, not inside Respawn.
			players[i].IsAlive = true;
			players[i].Status.ClearAll();
			players[i].Respawn( i, players.Count );
		}

		return players.Count;
	}

	void EndRound()
	{
		// Winner first, then everyone else in reverse order of falling — last out is second.
		// Late joiners spectate and were never in the round, so they're in neither list.
		var fallen = Enumerable.Reverse( _eliminatedIds )
			.Select( id => Players.FirstOrDefault( p => p.GameObject.Id == id ) )
			.Where( p => p is not null );

		var order = Players.Where( p => p.IsAlive ).Concat( fallen ).ToList();
		var ranked = _playersAtRoundStart > 1;
		var duration = _roundElapsed.Relative;

		// Placement and survival points land before the table is built, so it shows final totals.
		for ( var i = 0; i < order.Count; i++ )
		{
			var place = i + 1;
			var player = order[i];

			var bonus = place switch
			{
				1 => MatchScore.WinPoints,
				2 => MatchScore.SecondPoints,
				3 => MatchScore.ThirdPoints,
				_ => 0
			};

			if ( ranked && bonus > 0 ) _score.Award( player, bonus, place == 1 ? "Winner" : $"#{place}" );

			var alive = player.IsAlive ? duration : SurvivedFor( player, duration );
			_score.Award( player, (int)(alive * MatchScore.SurvivalPointsPerSecond), "Survival" );
		}

		var results = order.Select( p => p.DisplayName ).ToArray();
		var points = order.Select( p => _score.PointsOf( p ) ).ToArray();
		var knockdowns = order.Select( p => _score.KnockdownsOf( p ) ).ToArray();
		var deltas = order.Select( ( p, i ) => ranked ? Ranks.Delta( i + 1, order.Count, points[i] ) : 0 ).ToArray();

		// The table shows everyone's rank after this round, the same number their owner will save.
		var rank = order.Select( ( p, i ) => System.Math.Max( 0, p.RankPoints + deltas[i] ) ).ToArray();

		SetState( RoundState.Ended );
		AnnounceResults( results, points, knockdowns, rank, duration, ranked && results.Length > 0 ? results[0] : "", ranked );

		// Rank points travel to each owner, whose profile is the only copy of their rank.
		if ( ranked )
		{
			for ( var i = 0; i < order.Count; i++ )
				order[i].ReceiveRoundResult( deltas[i], i + 1, knockdowns[i] );
		}

		_restartAt = RestartDelay;
	}

	/// <summary>How long an eliminated player lasted, in seconds into the round.</summary>
	float SurvivedFor( TntPlayer player, float duration )
	{
		return _fellAt.TryGetValue( player.GameObject.Id, out var at ) ? at : duration;
	}

	/// <summary>
	/// Advertise round status on the lobby so a browsing client can show it without joining.
	/// LobbyInformation.Get is the read side of this.
	/// </summary>
	void PublishLobbyData()
	{
		if ( !Networking.IsActive ) return;
		if ( !Networking.IsHost ) return;

		_publishedAlive = AliveCount;

		Networking.SetData( "host", Connection.Local?.DisplayName ?? "?" );
		Networking.SetData( "state", State.ToString() );
		Networking.SetData( "alive", _publishedAlive.ToString() );
		Networking.SetData( "players", PlayerCount.ToString() );
	}

	void ResetArena()
	{
		_arena.ResetAll();
		_decay.ResetAll();
		GetComponent<TileWarning>().Clear();
	}

	void SetState( RoundState next )
	{
		if ( State == next ) return;

		ApplyRoundState( next );
	}

	/// <summary>
	/// Broadcast so both screens show the countdown. Clients don't run the state machine — the
	/// host guard stops them at OnUpdate — so without this they'd just see Playing appear from
	/// nowhere. Counts are passed rather than recomputed locally so the log reflects the host's
	/// authoritative view.
	/// </summary>
	[Rpc.Broadcast( NetFlags.HostOnly )]
	void AnnounceCountdown( float seconds, int players, int needed )
	{
		// Assigning _startAt on every machine means each one ticks its own countdown for the
		// HUD — no need to stream the remaining time. The host reads the same field to decide
		// when to start, so there is one source of truth per machine.
		_countingDown = true;
		_startAt = seconds;

		Log.Info( $"Round: starting in {seconds:0.#}s ({players}/{needed} players)" );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void AnnounceCountdownCancelled( int players, int needed )
	{
		_countingDown = false;

		Log.Info( $"Round: countdown cancelled ({players}/{needed} players)" );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void AnnounceRoundStart( int players )
	{
		_playersAtRoundStart = players;
		_eliminatedIds.Clear();
		_fellAt.Clear();
		Scene.Get<ProfileStore>().ClearLastDelta();

		Log.Info( $"Round: started with {players} player(s)" );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void AnnounceElimination( string name, System.Guid id, int remaining, string by )
	{
		_eliminatedIds.Add( id );
		_fellAt[id] = _roundElapsed.Relative;

		_feed.Add( new FeedEntry( name, remaining, Time.Now, by ) );
		if ( _feed.Count > MaxFeedEntries ) _feed.RemoveAt( 0 );
		FeedRevision++;

		Log.Info( $"Round: {name} fell ({remaining} left)" );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void AnnounceResults( string[] results, int[] points, int[] knockdowns, int[] rank, float duration, string winner, bool ranked )
	{
		_lastResults = results;
		_lastRows = new ResultRow[results.Length];
		for ( var i = 0; i < results.Length; i++ )
			_lastRows[i] = new ResultRow( results[i], points[i], knockdowns[i], rank[i] );

		LastRoundDuration = duration;
		LastWinner = winner;
		LastRoundRanked = ranked;

		// Each machine runs its own restart clock, like the countdown, for the results screen.
		_restartAt = RestartDelay;

		Log.Info( string.IsNullOrEmpty( winner )
			? "Round: no winner"
			: $"Round: winner is {winner}" );
	}

	/// <summary>
	/// Everything a joiner missed, in one message: the state without the arena reset that a live
	/// transition carries, and the clocks as the host has them right now.
	/// </summary>
	[Rpc.Broadcast( NetFlags.HostOnly )]
	void ApplyJoinState( RoundState state, float elapsed, bool countingDown, float countdownLeft )
	{
		State = state;
		_roundElapsed = elapsed;
		_countingDown = countingDown;
		_startAt = countdownLeft;
	}

	/// <summary>
	/// Transitions are discrete events, so they broadcast rather than sync. An RPC also avoids
	/// needing NetworkMode.Object on the Arena object, which [Sync] would require.
	///
	/// Arena reset rides along with entering Waiting and Playing: tiles are NetworkMode.Never, so
	/// there is no tile state to replicate — each machine just needs the instruction to rebuild
	/// its own.
	/// </summary>
	[Rpc.Broadcast( NetFlags.HostOnly )]
	void ApplyRoundState( RoundState next )
	{
		var changed = State != next;
		State = next;

		// Any transition ends the countdown — this is what clears the flag on clients, which
		// never run TickWaiting.
		_countingDown = false;

		// Only the host's copy matters; set everywhere so the flag never disagrees with State.
		_decay.Armed = next == RoundState.Playing;

		if ( !changed ) return;

		Log.Info( $"Round: {next}" );

		PublishLobbyData();

		if ( next == RoundState.Ended ) return;

		if ( next == RoundState.Playing )
		{
			_roundElapsed = 0f;
			_feed.Clear();
			FeedRevision++;
		}

		ResetArena();
	}
}
