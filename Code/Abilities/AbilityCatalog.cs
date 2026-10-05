/// <summary>
/// The thirty abilities, as data. Order matters only for the loadout grid; the numeric value is
/// what travels on the wire and what the profile stores, so never renumber an existing entry.
/// </summary>
public enum AbilityId
{
	None = 0,

	// Help — move yourself, save yourself, or fix the floor.
	Dash = 1,
	SuperJump = 2,
	DoubleJump = 3,
	FeatherFall = 4,
	GhostStep = 5,
	Turbo = 6,
	IronBody = 7,
	BubbleShield = 8,
	Patch = 9,
	Bridge = 10,
	Blink = 11,
	Rewind = 12,
	Updraft = 13,
	Giant = 14,

	// Harm — knock people over, take the floor away, or mess with their controls.
	Shockwave = 15,
	Shove = 16,
	Hook = 17,
	TntBomb = 18,
	Sabotage = 19,
	Trench = 20,
	Glue = 21,
	Ice = 22,
	Confuse = 23,
	Blackout = 24,
	Swap = 25,
	HeavyBoots = 26,
	Stun = 27,
	Catapult = 28,
	Vortex = 29,
	Mine = 30,
}

public enum AbilityKind
{
	Help,
	Harm
}

/// <summary>One row of the catalogue. Icon is a Material Icons ligature — the font is proven in this install.</summary>
public readonly struct AbilityInfo
{
	public readonly AbilityId Id;
	public readonly string Name;
	public readonly string Description;
	public readonly AbilityKind Kind;
	public readonly float Cooldown;
	public readonly string Icon;

	public AbilityInfo( AbilityId id, string name, AbilityKind kind, float cooldown, string icon, string description )
	{
		Id = id;
		Name = name;
		Kind = kind;
		Cooldown = cooldown;
		Icon = icon;
		Description = description;
	}
}

/// <summary>
/// Static table of every ability. Pure data — what an ability *does* lives in
/// <see cref="AbilitySystem"/>, what it *looks like* in <see cref="AbilityFx"/>.
/// A switch rather than a static array, for the same hotload reason as ArenaGenerator.FloorColor.
/// </summary>
public static class AbilityCatalog
{
	public const int Count = 30;

	/// <summary>What a brand-new profile starts with: one escape, one weapon.</summary>
	public const AbilityId DefaultFirst = AbilityId.Dash;
	public const AbilityId DefaultSecond = AbilityId.Shove;

	public static IEnumerable<AbilityId> All
	{
		get
		{
			for ( var i = 1; i <= Count; i++ )
				yield return (AbilityId)i;
		}
	}

	public static bool IsValid( AbilityId id ) => (int)id >= 1 && (int)id <= Count;

	public static AbilityInfo Get( AbilityId id ) => id switch
	{
		AbilityId.Dash => new( id, "Dash", AbilityKind.Help, 7f, "fast_forward", "Burst forward along your view." ),
		AbilityId.SuperJump => new( id, "Super Jump", AbilityKind.Help, 10f, "keyboard_double_arrow_up", "Leap twice as high as a normal jump." ),
		AbilityId.DoubleJump => new( id, "Double Jump", AbilityKind.Help, 14f, "stairs", "For 6s you can jump once more in mid-air." ),
		AbilityId.FeatherFall => new( id, "Feather Fall", AbilityKind.Help, 14f, "air", "For 4s you fall slowly and steer freely." ),
		AbilityId.GhostStep => new( id, "Ghost Step", AbilityKind.Help, 18f, "blur_on", "For 2.5s the floor ignores your feet." ),
		AbilityId.Turbo => new( id, "Turbo", AbilityKind.Help, 12f, "bolt", "Run 40% faster for 4s." ),
		AbilityId.IronBody => new( id, "Iron Body", AbilityKind.Help, 16f, "shield", "For 5s nothing knocks you down and you hit three times as hard." ),
		AbilityId.BubbleShield => new( id, "Bubble Shield", AbilityKind.Help, 15f, "bubble_chart", "Blocks the next enemy ability within 8s." ),
		AbilityId.Patch => new( id, "Patch", AbilityKind.Help, 18f, "grid_on", "Rebuild a 3x3 patch of floor under you." ),
		AbilityId.Bridge => new( id, "Bridge", AbilityKind.Help, 16f, "linear_scale", "Rebuild a six-tile path ahead of you." ),
		AbilityId.Blink => new( id, "Blink", AbilityKind.Help, 12f, "flash_on", "Teleport five tiles forward." ),
		AbilityId.Rewind => new( id, "Rewind", AbilityKind.Help, 20f, "history", "Snap back to where you stood 3s ago." ),
		AbilityId.Updraft => new( id, "Updraft", AbilityKind.Help, 22f, "north", "A gust strong enough to reach the floor above." ),
		AbilityId.Giant => new( id, "Giant", AbilityKind.Help, 20f, "open_in_full", "Grow for 5s: you win every collision." ),

		AbilityId.Shockwave => new( id, "Shockwave", AbilityKind.Harm, 18f, "radio_button_checked", "Knock down everyone close around you." ),
		AbilityId.Shove => new( id, "Shove", AbilityKind.Harm, 9f, "front_hand", "Knock down whoever is right in front of you." ),
		AbilityId.Hook => new( id, "Hook", AbilityKind.Harm, 14f, "phishing", "Yank the nearest enemy you aim at towards you." ),
		AbilityId.TntBomb => new( id, "TNT Bomb", AbilityKind.Harm, 16f, "local_fire_department", "Lob a block of TNT that blasts a 3x3 hole." ),
		AbilityId.Sabotage => new( id, "Sabotage", AbilityKind.Harm, 18f, "dangerous", "The floor under the nearest enemy gives way." ),
		AbilityId.Trench => new( id, "Trench", AbilityKind.Harm, 14f, "horizontal_rule", "Tear an eight-tile trench ahead of you." ),
		AbilityId.Glue => new( id, "Glue", AbilityKind.Harm, 14f, "water_drop", "Enemies near you move at half speed for 3s." ),
		AbilityId.Ice => new( id, "Ice", AbilityKind.Harm, 14f, "ac_unit", "Enemies near you slide on ice for 3s." ),
		AbilityId.Confuse => new( id, "Confuse", AbilityKind.Harm, 16f, "help_center", "The nearest enemy's controls flip for 3s." ),
		AbilityId.Blackout => new( id, "Blackout", AbilityKind.Harm, 16f, "visibility_off", "Enemies near you can barely see for 3s." ),
		AbilityId.Swap => new( id, "Swap", AbilityKind.Harm, 20f, "swap_horiz", "Trade places with the nearest enemy." ),
		AbilityId.HeavyBoots => new( id, "Heavy Boots", AbilityKind.Harm, 16f, "fitness_center", "The nearest enemy's tiles drop twice as fast for 4s." ),
		AbilityId.Stun => new( id, "Stun", AbilityKind.Harm, 16f, "pause_circle", "Freeze the nearest enemy for 1.2s." ),
		AbilityId.Catapult => new( id, "Catapult", AbilityKind.Harm, 18f, "rocket_launch", "Fling the nearest enemy into the air." ),
		AbilityId.Vortex => new( id, "Vortex", AbilityKind.Harm, 20f, "cyclone", "A whirl ahead of you drags enemies in for 2s." ),
		AbilityId.Mine => new( id, "Mine", AbilityKind.Harm, 14f, "adjust", "Plant a mine. The first enemy to touch it goes flying." ),

		_ => new( AbilityId.None, "None", AbilityKind.Help, 0f, "block", "" ),
	};

	/// <summary>Accent per kind, for HUD slots and loadout cards (8-bit safe, ≤ 1).</summary>
	public static Color KindColor( AbilityKind kind ) => kind == AbilityKind.Help
		? new Color( 0.40f, 0.82f, 0.66f )
		: new Color( 1.00f, 0.50f, 0.52f );
}
