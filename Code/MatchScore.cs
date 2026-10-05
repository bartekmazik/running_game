using System;

/// <summary>
/// Points earned during one round. Host-authoritative: the host awards, and one broadcast per
/// award carries the new total to every machine, so a scoreboard anywhere is just a lookup.
///
/// Points feed two things: the results screen, and the rank points each player takes home
/// (<see cref="RoundManager"/> folds them into the RP delta at the end of a round).
///
/// Also the referee's memory of "who hit whom last": an elimination within
/// <see cref="CreditWindow"/> seconds of a knockdown or a harmful ability is credited to whoever
/// caused it, which is what makes shoving someone off the edge worth more than the shove.
/// </summary>
public sealed class MatchScore : Component
{
	public const int KnockdownPoints = 50;
	public const int AbilityHitPoints = 20;
	public const int EliminationPoints = 100;
	public const int WinPoints = 300;
	public const int SecondPoints = 150;
	public const int ThirdPoints = 75;

	/// <summary>Survival is worth this much per second alive, added once at the end of the round.</summary>
	public const float SurvivalPointsPerSecond = 2f;

	/// <summary>How long a hit keeps you on the hook for the victim's fall.</summary>
	const float CreditWindow = 6f;

	/// <summary>One "+50 KNOCKDOWN" popup for the local player's HUD.</summary>
	public readonly struct Popup
	{
		public readonly int Amount;
		public readonly string Reason;
		public readonly float Time;

		public Popup( int amount, string reason, float time )
		{
			Amount = amount;
			Reason = reason;
			Time = time;
		}
	}

	readonly Dictionary<Guid, int> _points = new();
	readonly Dictionary<Guid, int> _knockdowns = new();

	/// <summary>Host only: victim → (attacker, when).</summary>
	readonly Dictionary<Guid, (TntPlayer Attacker, float Time)> _lastHit = new();

	readonly List<Popup> _popups = new();

	public IReadOnlyList<Popup> Popups => _popups;

	/// <summary>Bumped on every change, for HUD BuildHash.</summary>
	public int Revision { get; private set; }

	public int PointsOf( TntPlayer player ) => player is not null && _points.TryGetValue( player.GameObject.Id, out var p ) ? p : 0;

	public int KnockdownsOf( TntPlayer player ) => player is not null && _knockdowns.TryGetValue( player.GameObject.Id, out var k ) ? k : 0;

	/// <summary>
	/// Host only. Adds points and tells every machine the new total. Only a live round scores —
	/// shoving people around in the warm-up is practice.
	/// </summary>
	public void Award( TntPlayer player, int amount, string reason )
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;
		if ( player is null || !player.IsValid() || amount == 0 ) return;
		if ( GetComponent<RoundManager>()?.State == RoundManager.RoundState.Waiting ) return;

		var total = PointsOf( player ) + amount;
		BroadcastAward( player.GameObject.Id, total, amount, reason );
	}

	/// <summary>Host only. A knockdown: points for the attacker, a tally for the results.</summary>
	public void RecordKnockdown( TntPlayer attacker, TntPlayer victim )
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;

		RecordHit( attacker, victim );
		if ( attacker is null ) return;

		BroadcastKnockdown( attacker.GameObject.Id );
		Award( attacker, KnockdownPoints, "Knockdown" );
	}

	/// <summary>Host only. Remember who last hurt <paramref name="victim"/>, for the elimination credit.</summary>
	public void RecordHit( TntPlayer attacker, TntPlayer victim )
	{
		if ( attacker is null || victim is null || attacker == victim ) return;
		_lastHit[victim.GameObject.Id] = (attacker, Time.Now);
	}

	/// <summary>Host only. Who gets the credit for <paramref name="victim"/> falling, if anyone.</summary>
	public TntPlayer ClaimElimination( TntPlayer victim )
	{
		if ( !_lastHit.TryGetValue( victim.GameObject.Id, out var hit ) ) return null;
		_lastHit.Remove( victim.GameObject.Id );

		if ( Time.Now - hit.Time > CreditWindow ) return null;
		if ( !hit.Attacker.IsValid() ) return null;
		return hit.Attacker;
	}

	/// <summary>Host only. Wipe the board for a new round.</summary>
	public void ResetRound()
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;
		BroadcastReset();
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void BroadcastAward( Guid playerId, int total, int amount, string reason )
	{
		_points[playerId] = total;
		Revision++;

		var local = TntPlayer.FindLocal( Scene );
		if ( local is null || local.GameObject.Id != playerId ) return;

		_popups.Add( new Popup( amount, reason, Time.Now ) );
		if ( _popups.Count > 4 ) _popups.RemoveAt( 0 );

		Sfx.Ui( Sfx.Points, 0.5f );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void BroadcastKnockdown( Guid playerId )
	{
		_knockdowns[playerId] = (_knockdowns.TryGetValue( playerId, out var k ) ? k : 0) + 1;
		Revision++;
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	void BroadcastReset()
	{
		_points.Clear();
		_knockdowns.Clear();
		_lastHit.Clear();
		_popups.Clear();
		Revision++;
	}
}
