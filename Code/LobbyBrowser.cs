using System.Threading.Tasks;
using Sandbox.Network;

/// <summary>
/// TURN 2 — lobby discovery with a UI.
///
/// Owns the query loop and the result set; <c>BrowserPanel</c> renders them. Polls on an
/// interval and on demand, and connects to a chosen lobby.
///
/// Stands down on a machine that hosts: discovery is only meaningful before you are in a lobby,
/// and the platform filters your own lobby out of the results anyway (established in turn 1 —
/// machine A hosted an active lobby for the whole window machine B polled, and B saw nothing,
/// because both ran the same Steam account).
/// </summary>
public sealed class LobbyBrowser : Component
{
	[Property, Range( 1f, 30f )] public float RefreshInterval { get; set; } = 5f;

	/// <summary>Latest query result. Empty until the first one lands.</summary>
	public List<LobbyInformation> Lobbies { get; private set; } = new();

	public bool IsQuerying { get; private set; }

	/// <summary>Seconds since the last result landed — the panel's staleness readout.</summary>
	public float SinceResult => _sinceResult.Relative;

	/// <summary>
	/// Bumped whenever anything the panel draws changes. The panel folds this into its
	/// BuildHash, so a lobby whose alive-count moved redraws without the list length changing.
	/// </summary>
	public int Revision { get; private set; }

	/// <summary>False on a hosting machine, where the browser never runs.</summary>
	public bool IsBrowsing { get; private set; }

	TimeUntil _nextQuery;

	TimeSince _sinceResult;

	/// <summary>One query in flight at a time — the poll interval is shorter than a slow round trip.</summary>
	bool _querying;

	protected override void OnStart()
	{
		IsBrowsing = !Networking.IsActive && Scene.Get<RoundManager>()?.AutoHost != true;

		// Says so explicitly, because silence is otherwise ambiguous: a browser that is standing
		// down looks identical to a component that never loaded.
		if ( !IsBrowsing )
		{
			Log.Info( "LobbyBrowser: standing down — this machine hosts. Untick RoundManager.AutoHost to browse." );
			return;
		}

		Log.Info( $"LobbyBrowser: polling every {RefreshInterval:0.#}s for ident '{Game.Ident}'" );
	}

	protected override void OnUpdate()
	{
		if ( !IsBrowsing ) return;

		// Discovery is only meaningful before you're in a lobby.
		if ( Networking.IsActive ) return;
		if ( !_nextQuery ) return;

		Refresh();
	}

	/// <summary>Queries now and restarts the interval. Called by the poll loop and the button.</summary>
	public void Refresh()
	{
		if ( _querying ) return;

		_nextQuery = RefreshInterval;
		_ = QueryAsync();
	}

	async Task QueryAsync()
	{
		_querying = true;
		IsQuerying = true;
		Revision++;

		var lobbies = await Networking.QueryLobbies();

		// Joining mid-query: the results are for a screen that is already gone, and writing
		// them would flash a stale list over the connect.
		if ( !Networking.IsActive )
		{
			Lobbies = lobbies;
			_sinceResult = 0;

			Log.Info( $"LobbyBrowser: {lobbies.Count} lobby(s) visible — ident '{Game.Ident}'" );
		}

		_querying = false;
		IsQuerying = false;
		Revision++;
	}

	/// <summary>
	/// Connects to the chosen lobby. The panel hides itself once the connection is active, and
	/// RoundManager takes over from there — it spawns this connection a player on the host.
	/// </summary>
	public void Join( LobbyInformation lobby )
	{
		if ( Networking.IsActive ) return;
		if ( lobby.IsFull ) return;

		Log.Info( $"LobbyBrowser: connecting to '{lobby.Name}' ({lobby.LobbyId})" );

		Networking.Connect( lobby.LobbyId );
	}
}
