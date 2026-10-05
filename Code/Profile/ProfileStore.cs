using System;

/// <summary>What this machine's player carries between sessions. Serialised as JSON.</summary>
public sealed class PlayerProfile
{
	public int RankPoints { get; set; }
	public int Rounds { get; set; }
	public int Wins { get; set; }
	public int Knockdowns { get; set; }
	public int BestPlace { get; set; }

	/// <summary>Loadout as raw AbilityId values, so an unknown id from a newer build can't break loading.</summary>
	public int FirstAbility { get; set; } = (int)AbilityCatalog.DefaultFirst;
	public int SecondAbility { get; set; } = (int)AbilityCatalog.DefaultSecond;
}

/// <summary>
/// The local player's profile: rank points, lifetime stats and ability loadout.
///
/// Persisted in FileSystem.Data (works offline, in the editor, everywhere) and mirrored to
/// Sandbox.Services.Stats, which starts recording once the game is published under a real
/// organisation. Lives on the Arena object, created by RoundManager like everything else.
///
/// A component instance rather than a static: s&box hotload keeps statics alive across code
/// changes (see memory note on TileTextures), and a stale profile would be worse than a stale texture.
/// </summary>
public sealed class ProfileStore : Component
{
	const string FileName = "profile.json";

	public PlayerProfile Profile { get; private set; }

	/// <summary>Bumped on every change so panels can fold it into BuildHash.</summary>
	public int Revision { get; private set; }

	/// <summary>The most recent rank-up, for the results screen's "rank up!" card. Cleared when shown.</summary>
	public int RankUpToStep { get; set; } = -1;

	/// <summary>RP change from the last round, shown on the results screen.</summary>
	public int LastDelta { get; private set; }

	public bool HasLastDelta { get; private set; }

	protected override void OnAwake()
	{
		Profile = FileSystem.Data.ReadJson<PlayerProfile>( FileName, null ) ?? new PlayerProfile();
		Sanitise();
	}

	public AbilityId First => (AbilityId)Profile.FirstAbility;
	public AbilityId Second => (AbilityId)Profile.SecondAbility;

	public void SetLoadout( AbilityId first, AbilityId second )
	{
		Profile.FirstAbility = (int)first;
		Profile.SecondAbility = (int)second;
		Sanitise();
		Save();
	}

	/// <summary>
	/// Called on the owner when the host reports the end of a ranked round. Applies the RP change,
	/// updates lifetime stats, saves, and notes a rank-up for the results screen.
	/// </summary>
	public void ApplyRoundResult( int delta, int place, int knockdowns )
	{
		var before = Ranks.Step( Profile.RankPoints );

		Profile.RankPoints = Math.Max( 0, Profile.RankPoints + delta );
		Profile.Rounds++;
		Profile.Knockdowns += knockdowns;
		if ( place == 1 ) Profile.Wins++;
		if ( Profile.BestPlace == 0 || place < Profile.BestPlace ) Profile.BestPlace = place;

		LastDelta = delta;
		HasLastDelta = true;

		var after = Ranks.Step( Profile.RankPoints );
		if ( after > before ) RankUpToStep = after;

		Save();
		Publish( place, knockdowns );
	}

	/// <summary>A fresh results screen shouldn't show last round's delta until the new one lands.</summary>
	public void ClearLastDelta()
	{
		HasLastDelta = false;
		Revision++;
	}

	void Save()
	{
		FileSystem.Data.WriteJson( FileName, Profile );
		Revision++;
	}

	/// <summary>
	/// Backend mirror. Starts recording once the game is published under a real organisation; until
	/// then it is skipped, since an unpublished package can only fail to authorise.
	/// </summary>
	void Publish( int place, int knockdowns )
	{
		// An unpublished build ("local.*") has no backend to write to; every flush would log a 401.
		if ( Game.Ident.StartsWith( "local." ) ) return;

		Sandbox.Services.Stats.SetValue( "rank_points", Profile.RankPoints );
		Sandbox.Services.Stats.Increment( "rounds", 1 );
		Sandbox.Services.Stats.Increment( "knockdowns", knockdowns );
		if ( place == 1 ) Sandbox.Services.Stats.Increment( "wins", 1 );
	}

	/// <summary>Repair a loadout that's invalid or doubled — a hand-edited file, or an older build.</summary>
	void Sanitise()
	{
		if ( !AbilityCatalog.IsValid( First ) ) Profile.FirstAbility = (int)AbilityCatalog.DefaultFirst;
		if ( !AbilityCatalog.IsValid( Second ) ) Profile.SecondAbility = (int)AbilityCatalog.DefaultSecond;

		if ( Profile.FirstAbility == Profile.SecondAbility )
		{
			Profile.SecondAbility = Profile.FirstAbility == (int)AbilityCatalog.DefaultSecond
				? (int)AbilityCatalog.DefaultFirst
				: (int)AbilityCatalog.DefaultSecond;
		}

		Profile.RankPoints = Math.Max( 0, Profile.RankPoints );
	}
}
