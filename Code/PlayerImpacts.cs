/// <summary>
/// Host-side judge for player-player collisions. Decides when a shove is hard enough to knock
/// someone into a ragdoll, and who goes down (CLAUDE.md: "the host decides when a ragdoll starts").
///
/// Proximity plus closing speed, checked every fixed tick across all pairs — 276 pairs at 24
/// players, trivial. No collision listeners: two CharacterControllers never generate physics
/// contacts with each other, they just stop at each other's capsule.
///
/// Rule: whoever was moving into the other harder stays up; the other one is launched away from
/// them. A head-on collision where both were sprinting floors both.
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

	/// <summary>Each side's own share of the closing speed above which a head-on floors both.</summary>
	const float MutualShare = 190f;

	const float LaunchScale = 1.15f;
	const float LaunchLift = 190f;

	/// <summary>Grace after getting up, so a player can't be chain-stunned.</summary>
	const float Immunity = 1.2f;

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

	protected override void OnFixedUpdate()
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;

		_candidates.Clear();

		foreach ( var player in Scene.GetAll<TntPlayer>() )
		{
			if ( !player.IsAlive || player.IsRagdolled ) continue;
			if ( _immuneUntil.TryGetValue( player, out var until ) && !until ) continue;

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
		var aShare = ShareTowards( a, aToB );
		var bShare = ShareTowards( b, -aToB );

		var closing = aShare + bShare;
		if ( closing < ImpactSpeed ) return;

		var launch = closing * LaunchScale;

		if ( aShare > MutualShare && bShare > MutualShare )
		{
			KnockDown( a, -aToB * launch );
			KnockDown( b, aToB * launch );
			return;
		}

		if ( aShare >= bShare )
		{
			KnockDown( b, aToB * launch );
			Protect( a, 0.4f );
		}
		else
		{
			KnockDown( a, -aToB * launch );
			Protect( b, 0.4f );
		}
	}

	void KnockDown( TntPlayer victim, Vector3 horizontal )
	{
		victim.BeginRagdoll( horizontal + Vector3.Up * LaunchLift );
		Protect( victim, TntPlayer.RagdollDuration + Immunity );
	}

	void Protect( TntPlayer player, float seconds )
	{
		_immuneUntil[player] = seconds;
	}
}
