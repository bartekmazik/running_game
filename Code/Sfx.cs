/// <summary>
/// Every sound the game plays, in one place. Two sources, both shipped with the engine:
///
///   * `.sound` events (footsteps, impacts, explosions) — played by event path;
///   * Kenney's interface sounds, which the engine ships as bare `sounds/kenney/ui/*.vsnd` files.
///     The user's own `kenney_interface-sounds.zip` holds the identical files, so these are "their"
///     sounds without shipping a second copy. A bare .vsnd has no event, so one is built in code
///     around it — the same "generated, not authored" rule as the tile textures.
/// </summary>
public static class Sfx
{
	// World
	public const string Footstep = "sounds/footsteps/footstep-concrete.sound";
	public const string FootstepLand = "sounds/footsteps/footstep-concrete-land.sound";
	public const string FootstepJump = "sounds/footsteps/footstep-concrete-jump.sound";
	public const string TileCollapse = "sounds/impacts/melee/impact-melee-concrete.sound";
	public const string BodyHit = "sounds/impacts/melee/impact-melee-flesh.sound";
	public const string Blast = "sounds/effects/explosion/explosion_small.sound";

	// Kenney interface set (engine copy)
	public const string Click = "click_002";
	public const string Hover = "select_001";
	public const string Select = "switch_002";
	public const string Back = "back_001";
	public const string Confirm = "confirmation_001";
	public const string RankUp = "confirmation_004";
	public const string Deny = "error_004";
	public const string Cast = "maximize_006";
	public const string CastHarm = "glitch_002";
	public const string Ready = "pluck_001";
	public const string Tick = "tick_002";
	public const string Go = "bong_001";
	public const string Points = "drop_002";
	public const string Popup = "open_001";
	public const string Pop = "glass_002";
	public const string Status = "question_001";

	static readonly Dictionary<string, SoundEvent> _events = new();

	/// <summary>A Kenney interface sound, non-spatial — UI feedback heard only by this machine.</summary>
	public static SoundHandle Ui( string name, float volume = 0.6f )
	{
		var handle = Sound.Play( Kenney( name, true ) );
		handle.Volume = volume;
		return handle;
	}

	/// <summary>A Kenney interface sound placed in the world — ability casts, mines, pickups.</summary>
	public static SoundHandle KenneyAt( string name, Vector3 position, float volume = 0.8f, float pitch = 1f )
	{
		var handle = Sound.Play( Kenney( name, false ), position );
		handle.Volume = volume;
		handle.Pitch = pitch;
		return handle;
	}

	/// <summary>An engine sound event placed in the world.</summary>
	public static SoundHandle At( string soundEvent, Vector3 position, float volume = 1f, float pitch = 1f )
	{
		var handle = Sound.Play( soundEvent, position );
		handle.Volume = volume;
		handle.Pitch = pitch;
		return handle;
	}

	static SoundEvent Kenney( string name, bool ui )
	{
		var key = ui ? name : name + "@3d";
		if ( _events.TryGetValue( key, out var existing ) ) return existing;

		var created = new SoundEvent
		{
			Sounds = new List<SoundFile> { SoundFile.Load( $"sounds/kenney/ui/{name}.vsnd" ) },
			UI = ui
		};

		_events[key] = created;
		return created;
	}
}
