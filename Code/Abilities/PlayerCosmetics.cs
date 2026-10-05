using System;

/// <summary>
/// Everything a player wears that isn't clothing: rank perks, and the look of whatever ability
/// effect is on them. Runs on every machine for every player, from synced state only
/// (<see cref="TntPlayer.RankPoints"/>, <see cref="PlayerStatus"/>), and builds only local,
/// unnetworked objects — the same contract as the Body.
///
/// Rank perks are earned, not chosen (KONTEKST.md: higher ranks give visual benefits); see
/// <see cref="Ranks"/> for what each tier adds.
/// </summary>
public sealed class PlayerCosmetics : Component
{
	const string BoxPath = "models/dev/box.vmdl";
	const string SpherePath = "models/dev/sphere.vmdl";

	TntPlayer _player;
	PlayerStatus _status;

	/// <summary>Tier the perks were last built for; -1 forces a rebuild.</summary>
	int _builtTier = -1;

	GameObject _perks;
	readonly List<ModelRenderer> _ring = new();
	readonly List<GameObject> _sparks = new();
	GameObject _crown;

	TimeSince _sinceTrail;

	bool _hadShield;
	bool _hadStun;
	List<GameObject> _bubble;

	float _giantScale = 1f;

	protected override void OnAwake()
	{
		_player = GetComponent<TntPlayer>();
		_status = GetComponent<PlayerStatus>();
	}

	protected override void OnUpdate()
	{
		if ( _player.Body is null ) return;

		var visible = _player.IsAlive && !_player.IsRagdolled;

		UpdatePerks( visible );
		UpdateStatusLook( visible );
	}

	// ---------------------------------------------------------------------------------------------
	// Rank perks

	void UpdatePerks( bool visible )
	{
		var tier = (int)_player.RankTier;
		if ( tier != _builtTier ) BuildPerks( (RankTier)tier );

		if ( _perks.IsValid() && _perks.Enabled != visible )
			_perks.Enabled = visible;

		if ( !visible ) return;

		var colour = Ranks.TierColor( (RankTier)tier );
		var pulse = 0.75f + 0.25f * MathF.Sin( Time.Now * 3f );

		foreach ( var bead in _ring )
			bead.Tint = colour.Darken( 0.45f ) * pulse;

		if ( _crown.IsValid() && _player.Body.TryGetBoneTransform( "head", out var head ) )
		{
			_crown.WorldPosition = head.Position + Vector3.Up * 13f;
			_crown.WorldRotation = Rotation.FromYaw( _player.WorldRotation.Yaw() + Time.Now * 30f );
		}

		// Diamond and up: a trail of fading cubes at speed.
		if ( tier >= (int)RankTier.Diamond && _player.MoveVelocity.WithZ( 0f ).Length > 230f && _sinceTrail > 0.06f )
		{
			_sinceTrail = 0f;
			var jitter = new Vector3( Game.Random.NextSingle() - 0.5f, Game.Random.NextSingle() - 0.5f, 0f ) * 18f;
			var piece = AbilityFx.Piece( false, _player.WorldPosition + Vector3.Up * 14f + jitter, new Vector3( 9f ), new Vector3( 1f ), colour, Color.Black, 0.6f );
			piece.Spin = 200f;
		}
	}

	void BuildPerks( RankTier tier )
	{
		_builtTier = (int)tier;

		if ( _perks.IsValid() ) _perks.Destroy();
		_sparks.Clear();
		_ring.Clear();
		_crown = null;

		_perks = new GameObject( GameObject, true, "Rank Perks" );
		_perks.Flags |= GameObjectFlags.NotSaved;
		_perks.NetworkMode = NetworkMode.Never;

		var colour = Ranks.TierColor( tier );

		if ( tier >= RankTier.Gold )
		{
			// A slow halo of glowing beads at the feet. Glows, unlike most effects: a rank aura should
			// read across the floor. Beads rather than a flat disc — a squashed sphere z-fought with
			// the tiles into a dithered smear, and the cube vocabulary matches everything else.
			for ( var i = 0; i < 10; i++ )
			{
				var bead = Part( _perks, BoxPath, Vector3.Zero, new Vector3( 5f, 5f, 3f ), colour, true );
				var orbit = bead.AddComponent<FxOrbit>();
				orbit.Target = GameObject;
				orbit.Offset = Vector3.Up * 6f;
				orbit.Radius = 26f;
				orbit.Speed = 40f;
				orbit.Phase = i * 36f;
				orbit.Life = float.MaxValue;
				_ring.Add( bead.GetComponent<ModelRenderer>() );
			}
		}

		if ( tier >= RankTier.Platinum )
		{
			for ( var i = 0; i < 3; i++ )
			{
				var spark = Part( _perks, BoxPath, Vector3.Zero, new Vector3( 6f ), colour.Darken( 0.45f ), true );
				var orbit = spark.AddComponent<FxOrbit>();
				orbit.Target = GameObject;
				orbit.Offset = Vector3.Up * 46f;
				orbit.Radius = 30f;
				orbit.Speed = 160f;
				orbit.Phase = i * 120f;
				orbit.Bob = 10f;
				orbit.Life = float.MaxValue;
				_sparks.Add( spark );
			}
		}

		if ( tier >= RankTier.Master )
			_crown = BuildCrown( colour );
	}

	/// <summary>A little toy crown: a band and five points, from dev boxes.</summary>
	GameObject BuildCrown( Color colour )
	{
		// Emissive, but a stop under full: a full-strength glow tint blows out to white.
		var gold = colour.Darken( 0.4f );

		var crown = new GameObject( _perks, true, "Crown" );
		crown.Flags |= GameObjectFlags.NotSaved;
		crown.NetworkMode = NetworkMode.Never;

		for ( var i = 0; i < 5; i++ )
		{
			var dir = Rotation.FromYaw( i * 72f ).Forward;
			Part( crown, BoxPath, dir * 7f, new Vector3( 4f, 7f, 3f ), gold, true ).LocalRotation = Rotation.FromYaw( i * 72f + 90f );
			Part( crown, BoxPath, dir * 7f + Vector3.Up * 4f, new Vector3( 3f, 3f, 6f ), gold, true );
		}

		Part( crown, SpherePath, Vector3.Up * 2f, new Vector3( 4f ), AbilityFx.Coral );
		return crown;
	}

	static GameObject Part( GameObject parent, string model, Vector3 localPosition, Vector3 size, Color colour, bool glow = false )
	{
		var go = new GameObject( parent, true, "Perk" );
		go.Flags |= GameObjectFlags.NotSaved;
		go.NetworkMode = NetworkMode.Never;
		go.LocalPosition = localPosition;

		var renderer = go.AddComponent<ModelRenderer>();
		renderer.Model = Model.Load( model );
		renderer.MaterialOverride = glow ? Material.Load( AbilityFx.GlowMaterialPath ) : AbilityFx.Matte;
		renderer.RenderType = ModelRenderer.ShadowRenderType.Off;
		renderer.Tint = colour;

		go.LocalScale = size / MathF.Max( renderer.Model.Bounds.Size.x, 1f );
		return go;
	}

	// ---------------------------------------------------------------------------------------------
	// Status look

	void UpdateStatusLook( bool visible )
	{
		var body = _player.Body;

		// Body tint: the strongest-reading effect wins. Pastel, ≤ 1 per channel.
		var tint = Color.White;
		if ( _status.Has( StatusKind.Iron ) ) tint = new Color( 0.62f, 0.68f, 0.80f );
		else if ( _status.Has( StatusKind.Ice ) ) tint = new Color( 0.70f, 0.92f, 1.00f );
		else if ( _status.Has( StatusKind.Glue ) ) tint = new Color( 0.74f, 0.94f, 0.56f );
		else if ( _status.Has( StatusKind.Ghost ) ) tint = MathF.Sin( Time.Now * 30f ) > 0f ? new Color( 0.92f, 0.96f, 1.00f ) : new Color( 0.70f, 0.78f, 1.00f );
		else if ( _status.Has( StatusKind.Turbo ) ) tint = new Color( 1.00f, 0.95f, 0.70f );

		// After a knockdown: blink while the grace period lasts.
		if ( _player.IsRecovering && MathF.Sin( Time.Now * 40f ) > 0.3f )
			tint = new Color( 1.00f, 0.86f, 0.55f );

		if ( body.Tint != tint ) body.Tint = tint;

		// Giant grows the body smoothly; the capsule stays put, it's the mass that wins collisions.
		var targetScale = _status.Has( StatusKind.Giant ) ? 1.35f : 1f;
		_giantScale = MathX.Approach( _giantScale, targetScale, Time.Delta * 2.5f );
		body.GameObject.LocalScale = new Vector3( _giantScale );

		// Turbo: a short streak at the feet.
		if ( visible && _status.Has( StatusKind.Turbo ) && _sinceTrail > 0.05f && _player.MoveVelocity.WithZ( 0f ).Length > 150f )
		{
			_sinceTrail = 0f;
			AbilityFx.Piece( false, _player.WorldPosition + Vector3.Up * 10f, new Vector3( 10f, 10f, 4f ), new Vector3( 2f ), AbilityFx.Butter, Color.Black, 0.35f );
		}

		var shield = _status.Has( StatusKind.Shield );
		if ( shield && !_hadShield )
			_bubble = AbilityFx.Bubble( GameObject, _status.Remaining( StatusKind.Shield ), AbilityFx.Sky );
		else if ( !shield && _hadShield )
			PopBubble();
		_hadShield = shield;

		var stun = _status.Has( StatusKind.Stunned );
		if ( stun && !_hadStun )
			AbilityFx.Dizzy( GameObject, _status.Remaining( StatusKind.Stunned ), AbilityFx.Sky );
		_hadStun = stun;
	}

	void PopBubble()
	{
		if ( _bubble is null ) return;

		foreach ( var mote in _bubble )
		{
			if ( mote.IsValid() ) mote.Destroy();
		}

		_bubble = null;
	}

	protected override void OnDestroy()
	{
		PopBubble();
	}
}
