using Sandbox.Network;

/// <summary>
/// Round state machine, lobby creation, and per-connection player spawning. Host-authoritative.
///
/// Waiting gates on <see cref="MinPlayers"/> and then runs a <see cref="CountdownDuration"/>
/// countdown. Both are written purely in terms of MinPlayers, so 1, 2 and 24 all take the same
/// path — there is no 2-player special case. Ended routes back through Waiting rather than
/// straight to Playing, so the gate and the countdown apply between rounds too.
/// </summary>
public sealed class RoundManager : Component, Component.INetworkListener
{
	public enum RoundState
	{
		Waiting,
		Playing,
		Ended
	}

	/// <summary>
	/// Players required to start. Drop to 1 for solo testing — nothing below assumes 2, the
	/// countdown and the win condition are both written in terms of this number.
	/// </summary>
	[Property, Range( 1, 24 )] public int MinPlayers { get; set; } = 2;

	/// <summary>Seconds between reaching MinPlayers and the round starting. 0 starts instantly.</summary>
	[Property, Range( 0f, 30f )] public float CountdownDuration { get; set; } = 5f;

	[Property, Range( 0.5f, 10f )] public float RestartDelay { get; set; } = 3f;

	[Property, Range( 2, 64 )] public int MaxPlayers { get; set; } = 24;

	/// <summary>
	/// Turn this off to browse instead of host. Without it every machine creates its own lobby
	/// on the first tick and there is nothing to discover.
	/// </summary>
	[Property] public bool AutoHost { get; set; } = true;

	public RoundState State { get; private set; } = RoundState.Waiting;

	/// <summary>Winner's object name, or empty for no survivors. Set on every machine.</summary>
	public string LastWinner { get; private set; } = "";

	ArenaGenerator _arena;
	TileDecay _decay;

	TimeUntil _restartAt;

	/// <summary>TimeUntil can't distinguish "unset" from "expired", hence the companion flag.</summary>
	TimeUntil _startAt;

	bool _countingDown;

	TimeSince _roundElapsed;

	/// <summary>Headcount when the round began — decides what "last alive" means.</summary>
	int _playersAtRoundStart;

	IEnumerable<TntPlayer> Players => Scene.GetAll<TntPlayer>();

	// Public for the HUD. Every client can count networked players in its own scene, so these
	// are correct everywhere, not just on the host.
	public int PlayerCount => Players.Count();

	public int AliveCount => Players.Count( p => p.IsAlive );

	public bool IsCountingDown => _countingDown;

	public float CountdownRemaining => _startAt.Relative;

	public float RoundElapsed => _roundElapsed.Relative;

	/// <summary>
	/// How many survivors the round can tolerate before it's over. With 2+ starters the round
	/// ends when one is left standing; solo, it ends only when that player is dead. Generalises
	/// to N without special-casing at the call site.
	/// </summary>
	int SurvivorsToEndRound => _playersAtRoundStart > 1 ? 1 : 0;

	bool _lobbyRequested;

	/// <summary>Last alive count pushed to the lobby, so we only republish when it moves.</summary>
	int _publishedAlive = -1;

	protected override void OnAwake()
	{
		// Refs only — nothing here reads arena geometry or tiles. Resolved in OnAwake rather
		// than OnStart because OnActive can fire before OnStart on a fast local connection.
		_arena = Scene.Get<ArenaGenerator>();
		_decay = Scene.Get<TileDecay>();

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
		player.AddComponent<TntPlayer>();
		player.NetworkSpawn( connection );

		// Their arena is generated locally and starts pristine, so hand them the current
		// collapsed set. Sent unconditionally rather than only during Playing: tiles stay down
		// through Ended and the following Waiting too, and an unconditional send is both
		// smaller and strictly more correct than a state check. Costs nothing when the arena
		// is intact — the snapshot is empty and no message goes out.
		_decay.SendSnapshotTo( connection );

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
		if ( Networking.IsActive && !Networking.IsHost ) return;

		if ( AliveCount != _publishedAlive ) PublishLobbyData();

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

	void TickWaiting()
	{
		if ( !EnsureLobby() ) return;

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

	/// <summary>
	/// A lobby has to exist before OnActive fires and spawns anyone — including in solo editor
	/// Play. Created from here rather than ISceneStartup.OnHostInitialize: OnUpdate is a path
	/// already proven in this project, and a scene-event that silently never fires would leave
	/// no lobby, no player, and no log to diagnose it by.
	/// </summary>
	bool EnsureLobby()
	{
		if ( Networking.IsActive ) return true;
		if ( !AutoHost ) return false;
		if ( _lobbyRequested ) return false;

		_lobbyRequested = true;

		Networking.CreateLobby( new LobbyConfig
		{
			MaxPlayers = MaxPlayers,
			Privacy = LobbyPrivacy.Public,
			Name = "TNT Run"
		} );

		Log.Info( "Round: creating lobby" );
		return false;
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
	}

	void StartRound()
	{
		// Snapshot the list so index and total agree, and so ring slots are stable within
		// this call even though Scene.GetAll order isn't guaranteed.
		var players = Players.ToList();

		// Broadcast first: every machine resets its own arena as part of entering Playing,
		// so nobody respawns onto tiles that are still collapsed locally.
		SetState( RoundState.Playing );

		for ( var i = 0; i < players.Count; i++ )
		{
			// IsAlive is FromHost, so it has to be written here, not inside Respawn.
			players[i].IsAlive = true;
			players[i].Respawn( i, players.Count );
		}

		_playersAtRoundStart = players.Count;

		Log.Info( $"Round: started with {_playersAtRoundStart} player(s)" );
	}

	void EndRound()
	{
		var winner = Players.FirstOrDefault( p => p.IsAlive );

		SetState( RoundState.Ended );
		AnnounceWinner( winner?.GameObject.Name ?? "" );

		_restartAt = RestartDelay;
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
	}

	void SetState( RoundState next )
	{
		if ( State == next ) return;

		ApplyRoundState( next );
	}

	/// <summary>
	/// Transitions are discrete events, so they broadcast rather than sync. An RPC also avoids
	/// needing NetworkMode.Object on the Arena object, which [Sync] would require.
	///
	/// Arena reset rides along with entering Playing: tiles are NetworkMode.Never, so there is
	/// no tile state to replicate — each machine just needs the instruction to rebuild its own.
	/// </summary>
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
	void AnnounceWinner( string winner )
	{
		LastWinner = winner;

		Log.Info( string.IsNullOrEmpty( winner )
			? "Round: no survivors"
			: $"Round: winner is {winner}" );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void ApplyRoundState( RoundState next )
	{
		State = next;

		// Any transition ends the countdown — this is what clears the flag on clients, which
		// never run TickWaiting.
		_countingDown = false;

		Log.Info( $"Round: {next}" );

		PublishLobbyData();

		if ( next != RoundState.Playing ) return;

		_roundElapsed = 0f;
		ResetArena();
	}
}
