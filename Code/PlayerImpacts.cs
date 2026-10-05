using System;

/// <summary>
/// Host-side judge for player-player collisions, and the one door every knockdown goes through —
/// shoves here, Shockwave and friends from <see cref="AbilitySystem"/>. CLAUDE.md: "the host
/// decides when a ragdoll starts".
///
/// Proximity plus closing speed, checked every fixed tick across all pairs — 276 pairs at 24
/// players, trivial. No collision listeners: two CharacterControllers never generate physics
/// contacts with each other, they just stop at each other's capsule.
///
/// The rule is the brief's (KONTEKST.md): each side's force is its mass times its own speed into
/// the other, and **whoever delivered the smaller force goes down**. A near-even head-on at speed
/// floors both — two sprinters meeting is the funniest thing in the game and should stay that way.
/// Iron Body and Giant raise mass; Iron Body and the post-ragdoll grace make you immune.
/// </summary>
public sealed class PlayerImpacts : Component
{
	/// <summary>
	/// Centre-to-centre distance that counts as touching. Two radii plus enough slack to catch the
	/// tick before contact, when the rammer is still at full speed.
	/// </summary>
	const float ContactDistance = TntPlayer.BodyRadius * 2f + 14f;

	const float ContactHeight = 48f;

	/// <summary>
	/// Closing speed needed for a knockdown. Above walk speed (190), so walking into someone just
	/// bumps; a sprint (280) into a standing player floors them.
	/// </summary>
	const float ImpactSpeed = 235f;

	/// <summary>Forces within this fraction of each other count as even — both go down.</summary>
	const float EvenMargin = 0.12f;

	/// <summary>An even collision only floors both if each side was really moving.</summary>
	const float MutualShare = 150f;

	const float LaunchScale = 1.15f;
	const float LaunchLift = 190f;

	/// <summary>Ragdoll length grows with the hit: a nudge is a stumble, a full sprint a real tumble.</summary>
	const float MinRagdoll = 1.0f;
	const float MaxRagdoll = 2.1f;

	/// <summary>Grace after getting up, so a player can't be chain-stunned. Also the dizzy stars' life.</summary>
	public const float Immunity = 1.2f;

	readonly Dictionary<TntPlayer, TimeUntil> _immuneUntil = new();

	readonly List<TntPlayer> _candidates = new();

	/// <summary>
	/// Each player's MoveVelocity from the previous tick. The controller clips velocity against the
	/// capsule it runs into, so on the tick two players touch, the rammer's speed already reads as
	/// near zero — and on the tick before, they weren't touching yet. Judging with the stronger of
	/// the two ticks catches the hit. Found by testing: a full-speed sprint into a standing player
	/// never knocked anyone down.
	/// </summary>
	readonly Dictionary<TntPlayer, Vector3> _previousVelocity = new();

	MatchScore _score;

	protected override void OnAwake()
	{
		_score = GetComponent<MatchScore>();
	}

	protected override void OnFixedUpdate()
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;

		_candidates.Clear();

		foreach ( var player in Scene.GetAll<TntPlayer>() )
		{
			if ( !player.IsAlive || player.IsRagdolled ) continue;
			_candidates.Add( player );
		}

		for ( var i = 0; i < _candidates.Count; i++ )
		{
			for ( var j = i + 1; j < _candidates.Count; j++ )
			{
				Judge( _candidates[i], _candidates[j] );
			}
		}

		_previousVelocity.Clear();
		foreach ( var player in _candidates )
		{
			_previousVelocity[player] = player.MoveVelocity;
		}
	}

	/// <summary>Host: can this player be floored right now?</summary>
	public bool CanKnockDown( TntPlayer player )
	{
		if ( player is null || !player.IsAlive || player.IsRagdolled ) return false;
		if ( player.Status.KnockdownImmune ) return false;
		if ( _immuneUntil.TryGetValue( player, out var until ) && !until ) return false;
		return true;
	}

	/// <summary>
	/// Host: floor <paramref name="victim"/>, launching them by <paramref name="launch"/> for
	/// <paramref name="duration"/> seconds, credited to <paramref name="attacker"/> (may be null).
	/// The single entry point for every knockdown in the game. Returns false if they were immune.
	/// </summary>
	public bool TryKnockDown( TntPlayer victim, Vector3 launch, float duration, TntPlayer attacker )
	{
		if ( Networking.IsActive && !Networking.IsHost ) return false;
		if ( !CanKnockDown( victim ) ) return false;

		victim.BeginRagdoll( launch, duration );
		_immuneUntil[victim] = duration + Immunity;

		_score?.RecordKnockdown( attacker, victim );
		return true;
	}

	/// <summary>A player's speed towards <paramref name="direction"/>, the stronger of this tick and the last.</summary>
	float ShareTowards( TntPlayer player, Vector3 direction )
	{
		var now = player.MoveVelocity.WithZ( 0f ).Dot( direction );
		if ( !_previousVelocity.TryGetValue( player, out var previous ) ) return now;

		var before = previous.WithZ( 0f ).Dot( direction );
		return before > now ? before : now;
	}

	void Judge( TntPlayer a, TntPlayer b )
	{
		if ( a.IsRagdolled || b.IsRagdolled ) return;

		var offset = b.WorldPosition - a.WorldPosition;
		if ( offset.z > ContactHeight || offset.z < -ContactHeight ) return;

		var flat = offset.WithZ( 0f );
		var distance = flat.Length;
		if ( distance > ContactDistance || distance < 0.01f ) return;

		var aToB = flat / distance;

		// Each player's own speed towards the other.
		var aShare = MathF.Max( 0f, ShareTowards( a, aToB ) );
		var bShare = MathF.Max( 0f, ShareTowards( b, -aToB ) );

		var closing = aShare + bShare;
		if ( closing < ImpactSpeed ) return;

		var aForce = aShare * a.Status.Mass;
		var bForce = bShare * b.Status.Mass;
		var stronger = MathF.Max( aForce, bForce );

		// How hard this was, 0..1 above the threshold — drives launch, ragdoll time and camera shake.
		var severity = ((closing - ImpactSpeed) / 400f).Clamp( 0f, 1f );
		var duration = MathX.Lerp( MinRagdoll, MaxRagdoll, severity );
		var launch = closing * LaunchScale;

		var even = MathF.Abs( aForce - bForce ) <= stronger * EvenMargin;
		if ( even && aShare > MutualShare && bShare > MutualShare )
		{
			TryKnockDown( a, -aToB * launch + Vector3.Up * LaunchLift, duration, b );
			TryKnockDown( b, aToB * launch + Vector3.Up * LaunchLift, duration, a );
			return;
		}

		// The brief's rule: the smaller force goes down. A heavier winner throws them further.
		if ( aForce >= bForce )
		{
			var ratio = MathF.Sqrt( a.Status.Mass / b.Status.Mass );
			TryKnockDown( b, aToB * launch * ratio + Vector3.Up * LaunchLift, duration, a );
			Protect( a, 0.4f );
		}
		else
		{
			var ratio = MathF.Sqrt( b.Status.Mass / a.Status.Mass );
			TryKnockDown( a, -aToB * launch * ratio + Vector3.Up * LaunchLift, duration, b );
			Protect( b, 0.4f );
		}
	}

	/// <summary>Brief cover for the winner, so the same contact can't flip and floor them next tick.</summary>
	void Protect( TntPlayer player, float seconds )
	{
		if ( _immuneUntil.TryGetValue( player, out var until ) && until.Relative > seconds ) return;
		_immuneUntil[player] = seconds;
	}
}
