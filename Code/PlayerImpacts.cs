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
	/// tick before contact, when the rammer is still at full speed. Measured 2026-10-20: two
	/// CharacterControllers stop 48 units apart (radii plus the controller's skin), so the old
	/// +14 slack (46) was never reached and no collision ever floored anyone.
	/// </summary>
	const float ContactDistance = TntPlayer.BodyRadius * 2f + 28f;

	const float ContactHeight = 48f;

	/// <summary>
	/// Closing speed needed for a knockdown. Above walk speed (190), so walking into someone just
	/// bumps; a sprint (280) into a standing player floors them.
	/// </summary>
	const float ImpactSpeed = 235f;

	/// <summary>
	/// Forces within this fraction of each other count as a dead heat — both go down. Kept tight:
	/// the brief is that the weaker hit falls, and this only covers what nobody could call.
	/// </summary>
	const float EvenMargin = 0.05f;

	/// <summary>An even collision only floors both if each side was really moving.</summary>
	const float MutualShare = 150f;

	const float LaunchScale = 1.4f;
	const float LaunchLift = 240f;

	/// <summary>Speed the winner keeps going forward after a hit (on top of a third of their own).</summary>
	const float WinnerCarry = 45f;

	/// <summary>Ragdoll length grows with the hit: a nudge is a stumble, a full sprint a real tumble.</summary>
	const float MinRagdoll = 1.3f;
	const float MaxRagdoll = 2.4f;

	/// <summary>Grace after getting up, so a player can't be chain-stunned. Also the dizzy stars' life.</summary>
	public const float Immunity = 1.2f;

	readonly Dictionary<TntPlayer, TimeUntil> _immuneUntil = new();

	readonly List<TntPlayer> _candidates = new();

	/// <summary>
	/// Each close pair's run-up: every tick they are within <see cref="ApproachDistance"/>, each
	/// player's speed along the line between them is written down. Measured 2026-10-20 with two real
	/// clients: a controller starts sliding around the capsule ahead of it well before the centres
	/// meet — a 280 sprint read as 155 "towards" at 59 units and 12 at 48, where the controllers stop.
	/// Judging the contact by the strongest recent tick judges the run-up, which is what the brief
	/// means by force; judging the contact tick alone floored nobody.
	/// </summary>
	readonly Dictionary<(TntPlayer, TntPlayer), Approach> _approaches = new();

	sealed class Approach
	{
		public readonly float[] A = new float[RunUpTicks];
		public readonly float[] B = new float[RunUpTicks];
		public readonly int[] Tick = new int[RunUpTicks];
	}

	/// <summary>Within this centre distance a pair's run-up is recorded.</summary>
	const float ApproachDistance = 140f;

	/// <summary>Ticks of run-up that count — about 0.13 s at the default fixed rate.</summary>
	const int RunUpTicks = 8;

	int _tick;

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

		_tick++;

		for ( var i = 0; i < _candidates.Count; i++ )
		{
			for ( var j = i + 1; j < _candidates.Count; j++ )
			{
				// A stable order, so a pair keeps one memory however GetAll happens to list them.
				var a = _candidates[i];
				var b = _candidates[j];
				if ( a.GameObject.Id.CompareTo( b.GameObject.Id ) > 0 ) (a, b) = (b, a);

				Judge( a, b );
			}
		}

		// Forget pairs that drifted apart or whose players left, now and then.
		if ( _tick % 600 == 0 )
		{
			foreach ( var key in _approaches.Where( e => !e.Key.Item1.IsValid() || !e.Key.Item2.IsValid() || _tick - e.Value.Tick.Max() > 600 ).Select( e => e.Key ).ToList() )
				_approaches.Remove( key );
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

		_score.RecordKnockdown( attacker, victim );
		return true;
	}

	/// <summary>Write down this tick's run-up for a pair, and return the pair's memory.</summary>
	Approach Remember( TntPlayer a, TntPlayer b, float aShare, float bShare )
	{
		if ( !_approaches.TryGetValue( (a, b), out var memory ) )
			_approaches[(a, b)] = memory = new Approach();

		var slot = _tick % RunUpTicks;
		memory.A[slot] = aShare;
		memory.B[slot] = bShare;
		memory.Tick[slot] = _tick;
		return memory;
	}

	/// <summary>The strongest share in the last <see cref="RunUpTicks"/> ticks.</summary>
	float Strongest( float[] shares, int[] ticks )
	{
		var best = 0f;
		for ( var i = 0; i < RunUpTicks; i++ )
		{
			if ( _tick - ticks[i] < RunUpTicks && shares[i] > best ) best = shares[i];
		}

		return best;
	}

	void Judge( TntPlayer a, TntPlayer b )
	{
		if ( a.IsRagdolled || b.IsRagdolled ) return;

		var offset = b.WorldPosition - a.WorldPosition;
		if ( offset.z > ContactHeight || offset.z < -ContactHeight ) return;

		var flat = offset.WithZ( 0f );
		var distance = flat.Length;
		if ( distance > ApproachDistance || distance < 0.01f ) return;

		var aToB = flat / distance;

		// Each player's own speed towards the other, this tick — remembered through the run-up.
		var memory = Remember( a, b,
			MathF.Max( 0f, a.MoveVelocity.WithZ( 0f ).Dot( aToB ) ),
			MathF.Max( 0f, b.MoveVelocity.WithZ( 0f ).Dot( -aToB ) ) );

		if ( distance > ContactDistance ) return;

		var aShare = Strongest( memory.A, memory.Tick );
		var bShare = Strongest( memory.B, memory.Tick );

		var closing = aShare + bShare;
		if ( closing < ImpactSpeed ) return;

		var aForce = aShare * a.Status.Mass;
		var bForce = bShare * b.Status.Mass;
		var stronger = MathF.Max( aForce, bForce );

		// How hard this was, 0..1 above the threshold — drives launch, ragdoll time and camera shake.
		var severity = ((closing - ImpactSpeed) / 300f).Clamp( 0f, 1f );
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
		if ( aForce >= bForce ) Floor( b, a, aToB, launch, duration );
		else Floor( a, b, -aToB, launch, duration );
	}

	/// <summary>
	/// The winner of a collision throws the loser along <paramref name="direction"/> — further the
	/// heavier the winner — and pays for it: the hit eats most of their own speed, so a ram is a
	/// thump, not a free pass through.
	/// </summary>
	void Floor( TntPlayer loser, TntPlayer winner, Vector3 direction, float launch, float duration )
	{
		var ratio = MathF.Sqrt( winner.Status.Mass / loser.Status.Mass );
		if ( !TryKnockDown( loser, direction * launch * ratio + Vector3.Up * LaunchLift, duration, winner ) ) return;

		Protect( winner, 0.4f );
		winner.Launch( direction * WinnerCarry );
	}

	/// <summary>
	/// Host: make <paramref name="player"/> unfloorable for <paramref name="seconds"/> — brief cover for
	/// a collision's winner, and the grace after getting up.
	/// </summary>
	public void Protect( TntPlayer player, float seconds )
	{
		if ( _immuneUntil.TryGetValue( player, out var until ) && until.Relative > seconds ) return;
		_immuneUntil[player] = seconds;
	}
}
