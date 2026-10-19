/// <summary>
/// Host-authoritative tile collapse. A stepped-on tile counts down <see cref="Arena.DecayDelay"/>
/// seconds, then disables itself — never destroyed. Collapsed indices accumulate in a buffer
/// for the batched broadcast added in the networking pass (CLAUDE.md, decision 3).
/// Lives on the "Arena" GameObject next to <see cref="ArenaGenerator"/>.
/// </summary>
public sealed class TileDecay : Component
{
	readonly struct Pending
	{
		public readonly int Floor;
		public readonly int X;
		public readonly int Y;
		public readonly float DueTime;

		public Pending( int floor, int x, int y, float dueTime )
		{
			Floor = floor;
			X = x;
			Y = y;
			DueTime = dueTime;
		}
	}

	/// <summary>
	/// Slack on a validated step, in tiles. At sprint speed (280 u/s) a 150ms round trip is
	/// ~0.75 tiles, plus the capsule half-width and transform interpolation lag on the host.
	/// 2 tiles is generous for latency while still bounding a client to its own vicinity.
	/// </summary>
	const int TileTolerance = 2;

	/// <summary>
	/// Floors are <see cref="Arena.FloorGap"/> (280) apart and a fall between them takes ~0.84s, so one floor of slack covers
	/// a player the host already sees dropping while the client reported itself grounded.
	/// </summary>
	const int FloorTolerance = 1;

	ArenaGenerator _arena;

	/// <summary>True once a tile has been stepped on — covers counting-down and collapsed alike.</summary>
	bool[,,] _marked;

	/// <summary>
	/// Kept sorted by due time, so the head is always the next tile to collapse and the tick never
	/// scans the whole list. Delays vary since abilities arrived (Heavy Boots halves a player's
	/// delay, Trench and Sabotage set their own), so plain insertion order is no longer due order;
	/// a sorted insert keeps the cheap head check. The list is short — tens of tiles, not thousands.
	/// </summary>
	readonly List<Pending> _pending = new();

	readonly List<int> _collapsed = new();

	/// <summary>How often the collapse buffer is broadcast. Decision 3 calls for ~100ms.</summary>
	[Property, Range( 0.02f, 0.5f )] public float FlushInterval { get; set; } = 0.1f;

	/// <summary>Packed indices of tiles collapsed since the last flush. Read-only view.</summary>
	public IReadOnlyList<int> CollapseBuffer => _collapsed;

	/// <summary>
	/// Host-side: whether steps start countdowns at all. RoundManager arms this for Playing only,
	/// so the warm-up in Waiting and the results screen in Ended leave the floor alone.
	/// </summary>
	public bool Armed { get; set; }

	/// <summary>Collapse sounds per flush, at most. A flush can carry dozens of tiles.</summary>
	const int MaxCollapseSounds = 3;

	/// <summary>Only tiles this close to the listener are worth a sound.</summary>
	const float CollapseSoundRange = 900f;

	TimeUntil _nextFlush;

	protected override void OnAwake()
	{
		// OnAwake, not OnStart: component order within a GameObject isn't guaranteed, and
		// NotifyStep must be safe to call from the first frame onwards.
		_arena = GetComponent<ArenaGenerator>();
		_marked = new bool[Arena.FloorCount, Arena.GridSize, Arena.GridSize];
		_nextFlush = FlushInterval;
	}

	protected override void OnFixedUpdate()
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;

		while ( _pending.Count > 0 && Time.Now >= _pending[0].DueTime )
		{
			var due = _pending[0];
			_pending.RemoveAt( 0 );
			Collapse( due );
		}

		if ( !_nextFlush ) return;

		_nextFlush = FlushInterval;
		FlushCollapses();
	}

	/// <summary>
	/// Decision 3: one message per flush window, never one per tile. At 24 players stepping
	/// ~4 tiles/sec each that's ~100 collapses/sec — roughly 10 indices in one message every
	/// 100ms, instead of 100 separate messages.
	/// </summary>
	void FlushCollapses()
	{
		if ( _collapsed.Count == 0 ) return;

		ApplyCollapses( _collapsed.ToArray() );
		_collapsed.Clear();
	}

	/// <summary>
	/// Reliable by default — a dropped collapse would leave a client standing on a tile the
	/// host has removed, permanently. HostOnly so a client cannot forge collapses.
	/// This also runs on the host, where every tile is already disabled; re-disabling is a
	/// no-op, which keeps host and client on a single code path.
	/// </summary>
	[Rpc.Broadcast( NetFlags.HostOnly )]
	void ApplyCollapses( int[] packed )
	{
		var dropped = 0;
		var sounds = 0;

		foreach ( var index in packed )
		{
			UnpackIndex( index, out var floor, out var x, out var y );

			var tile = _arena.GetTile( floor, x, y );
			if ( tile is null )
			{
				dropped++;
				continue;
			}

			if ( sounds < MaxCollapseSounds &&
				 tile.WorldPosition.Distance( Sound.Listener.Position ) < CollapseSoundRange )
			{
				Sfx.At( Sfx.TileCollapse, tile.WorldPosition, 0.45f, 0.8f + 0.1f * sounds );
				sounds++;
			}

			tile.Enabled = false;
		}

		// Silent in normal operation. Non-zero means collapses landed before this machine had
		// generated its arena — the one ordering risk in the join snapshot below.
		if ( dropped > 0 )
			Log.Warning( $"TileDecay: dropped {dropped} collapse(s) — arena not generated yet" );
	}

	/// <summary>
	/// Send the currently-collapsed set to a single connection, for someone joining mid-round.
	/// Every machine generates its own arena locally, so a late joiner starts with a pristine
	/// one and would otherwise stand on tiles that are already gone for everyone else.
	///
	/// One snapshot, not a replay of individual collapses, and it reuses the same packed array
	/// and the same apply path as the 100ms flush — only the recipient filter differs.
	/// </summary>
	public void SendSnapshotTo( Connection connection )
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;

		var collapsed = CollectCollapsed();
		if ( collapsed.Length == 0 ) return;

		using ( Rpc.FilterInclude( connection ) )
		{
			ApplyCollapses( collapsed );
		}

		Log.Info( $"TileDecay: sent {collapsed.Length} collapsed tile(s) to {connection?.DisplayName}" );
	}

	/// <summary>
	/// Derived from live tile state rather than a cumulative list, so there is no second copy of
	/// the truth to keep in step with ResetAll(). A disabled tile is a collapsed tile — nothing else
	/// disables them.
	/// </summary>
	int[] CollectCollapsed()
	{
		var collapsed = new List<int>();

		for ( var floor = 0; floor < Arena.FloorCount; floor++ )
		{
			for ( var x = 0; x < Arena.GridSize; x++ )
			{
				for ( var y = 0; y < Arena.GridSize; y++ )
				{
					var tile = _arena.GetTile( floor, x, y );
					if ( tile is null ) continue;
					if ( tile.Enabled ) continue;

					collapsed.Add( PackIndex( floor, x, y ) );
				}
			}
		}

		return collapsed.ToArray();
	}

	/// <summary>
	/// Mark a tile as stepped on and start its countdown. Ignored if the tile is out of range,
	/// already counting down, or already collapsed.
	///
	/// [Rpc.Host] because TntPlayer.UpdateTile runs on whoever owns that player, which for a
	/// remote client is not the host. The step is a request; the host owns the decision. The
	/// attribute replaces the explicit host guard — the body only ever runs on the host, and
	/// runs locally when there's no session.
	/// </summary>
	[Rpc.Host]
	public void NotifyStep( int floor, int x, int y )
	{
		if ( !Armed ) return;
		if ( !IsStepPlausible( floor, x, y, out var player ) ) return;
		if ( _arena.GetTile( floor, x, y ) is null ) return;
		if ( _marked[floor, x, y] ) return;

		Schedule( floor, x, y, StepDelay( player ) );
	}

	/// <summary>
	/// How long a tile under <paramref name="player"/> holds. Heavy Boots halves it — the one
	/// ability that changes how fast your own floor goes. TileWarning uses the same rule so the
	/// glow on every screen finishes when the tile really drops.
	/// </summary>
	public static float StepDelay( TntPlayer player )
	{
		var heavy = player is not null && player.Status.Has( StatusKind.HeavyBoots );
		return heavy ? Arena.DecayDelay * 0.5f : Arena.DecayDelay;
	}

	void Schedule( int floor, int x, int y, float delay )
	{
		_marked[floor, x, y] = true;

		var pending = new Pending( floor, x, y, Time.Now + delay );

		var index = _pending.Count;
		while ( index > 0 && _pending[index - 1].DueTime > pending.DueTime ) index--;
		_pending.Insert( index, pending );
	}

	/// <summary>
	/// Host only. Doom a set of tiles from an ability — Trench, Sabotage, the TNT bomb — with their
	/// own delay, and tell every machine to glow them for exactly that long. Unlike steps these
	/// aren't anyone's footprint, so TileWarning can't predict them and needs the broadcast.
	/// Armed or not: an ability is a deliberate act, and the warm-up resets the arena anyway.
	/// </summary>
	public void ForceCollapse( IReadOnlyList<int> packed, float delay )
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;

		var doomed = new List<int>();

		foreach ( var index in packed )
		{
			UnpackIndex( index, out var floor, out var x, out var y );

			var tile = _arena.GetTile( floor, x, y );
			if ( tile is null || !tile.Enabled ) continue;
			if ( _marked[floor, x, y] ) continue;

			Schedule( floor, x, y, delay );
			doomed.Add( index );
		}

		if ( doomed.Count > 0 )
			BroadcastWarnings( doomed.ToArray(), delay );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void BroadcastWarnings( int[] packed, float delay )
	{
		var warning = GetComponent<TileWarning>();
		foreach ( var index in packed )
			warning.Warn( index, delay );
	}

	/// <summary>
	/// Host only. Rebuild tiles — Patch and Bridge. Brings back collapsed tiles and cancels ones
	/// still counting down, then one broadcast puts them back on every machine.
	/// </summary>
	public void Restore( IReadOnlyList<int> packed )
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;

		var restored = new List<int>();

		foreach ( var index in packed )
		{
			UnpackIndex( index, out var floor, out var x, out var y );

			var tile = _arena.GetTile( floor, x, y );
			if ( tile is null ) continue;
			if ( tile.Enabled && !_marked[floor, x, y] ) continue;

			_marked[floor, x, y] = false;
			_pending.RemoveAll( p => p.Floor == floor && p.X == x && p.Y == y );
			_collapsed.Remove( index );
			restored.Add( index );
		}

		if ( restored.Count > 0 )
			ApplyRestores( restored.ToArray() );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void ApplyRestores( int[] packed )
	{
		var warning = GetComponent<TileWarning>();
		var local = TntPlayer.FindLocal( Scene );

		foreach ( var index in packed )
		{
			UnpackIndex( index, out var floor, out var x, out var y );

			var tile = _arena.GetTile( floor, x, y );
			if ( tile is null ) continue;

			warning.Forget( index );
			_arena.RestoreTile( floor, x, y );
			tile.Enabled = true;

			// The owner dedups its step reports per life; a rebuilt tile has to be reportable again,
			// or standing on it would never doom it.
			local?.ForgetReported( index );
		}
	}

	/// <summary>
	/// A step is a claim about where the caller is standing, so check it against where the host
	/// actually sees that player. Transforms are owner-authoritative and replicate to the host,
	/// so the host already holds an independent copy of the truth.
	///
	/// This bounds an abusive client to tiles it is genuinely next to, rather than any of the
	/// 4500 in the arena. It is not anti-cheat against collapsing your own neighbours — that is
	/// just the game — it stops remote collapses under other players.
	/// </summary>
	bool IsStepPlausible( int floor, int x, int y, out TntPlayer player )
	{
		// Not a remote call: solo play, or the host reporting its own player. Nothing to check
		// it against, and the host is authoritative by definition.
		if ( !Rpc.Calling )
		{
			player = TntPlayer.FindLocal( Scene );
			return true;
		}

		var caller = Rpc.Caller;

		player = Scene.GetAll<TntPlayer>()
			.FirstOrDefault( p => p.Network.Owner == caller );

		if ( player is null )
		{
			Log.Warning( $"TileDecay: step from {caller?.DisplayName} with no player" );
			return false;
		}

		if ( !player.IsAlive )
		{
			Log.Warning( $"TileDecay: step from eliminated player {caller?.DisplayName}" );
			return false;
		}

		// CLAUDE.md: a ragdolled body does not collapse tiles — only an actively controlled player.
		// Ghost Step is the same rule by ability: the owner already skips reporting, this backs it up.
		if ( player.IsRagdolled ) return false;
		if ( player.Status.Has( StatusKind.Ghost ) ) return false;

		if ( !_arena.TryGetTileIndex( player.WorldPosition, out var onFloor, out var onX, out var onY ) )
		{
			Log.Warning( $"TileDecay: step from {caller?.DisplayName} outside the arena" );
			return false;
		}

		if ( Within( floor, onFloor, FloorTolerance ) &&
			 Within( x, onX, TileTolerance ) &&
			 Within( y, onY, TileTolerance ) )
			return true;

		Log.Warning( $"TileDecay: rejected step [{floor},{x},{y}] from {caller?.DisplayName} " +
			$"standing at [{onFloor},{onX},{onY}]" );

		return false;
	}

	/// <summary>Written out rather than using System.Math — System is not a global using here.</summary>
	static bool Within( int reported, int actual, int tolerance )
	{
		var diff = reported - actual;
		if ( diff < 0 ) diff = -diff;

		return diff <= tolerance;
	}

	void Collapse( Pending tile )
	{
		_arena.GetTile( tile.Floor, tile.X, tile.Y ).Enabled = false;
		_collapsed.Add( PackIndex( tile.Floor, tile.X, tile.Y ) );
	}

	/// <summary>
	/// Drop all decay state. Pair with <see cref="ArenaGenerator.ResetAll"/> on round restart.
	///
	/// Named ResetAll rather than Reset because Component has its own unrelated Reset, and the
	/// shorter name collided with it.
	/// </summary>
	public void ResetAll()
	{
		_marked = new bool[Arena.FloorCount, Arena.GridSize, Arena.GridSize];
		_pending.Clear();
		_collapsed.Clear();
	}

	/// <summary>Flatten a tile index into one int. Max value is 7199, so this fits a ushort on the wire.</summary>
	public static int PackIndex( int floor, int x, int y )
	{
		return (floor * Arena.GridSize + x) * Arena.GridSize + y;
	}

	public static void UnpackIndex( int packed, out int floor, out int x, out int y )
	{
		y = packed % Arena.GridSize;
		x = packed / Arena.GridSize % Arena.GridSize;
		floor = packed / (Arena.GridSize * Arena.GridSize);
	}
}
