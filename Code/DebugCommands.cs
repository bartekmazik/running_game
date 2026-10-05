// TEMP-DEBUG — delete before commit.
public static class DebugCommands
{
	static TntPlayer Remote => Game.ActiveScene.GetAll<TntPlayer>().FirstOrDefault( p => p.IsProxy );
	static TntPlayer Local => TntPlayer.FindLocal( Game.ActiveScene );

	static Angles AimAtRemote() => Rotation.LookAt( (Remote.WorldPosition - Local.WorldPosition).WithZ( 0 ).Normal ).Angles();

	[ConCmd( "tnt_rcast" )]
	public static void CastAtRemote( int id )
	{
		var ok = Game.ActiveScene.Get<AbilitySystem>().Execute( Local, (AbilityId)id, AimAtRemote() );
		Log.Info( $"rcast {(AbilityId)id}: {ok} remote at {Remote.WorldPosition} ragdoll={Remote.IsRagdolled} alive={Remote.IsAlive}" );
	}

	[ConCmd( "tnt_near" )]
	public static void Near( float distance )
	{
		var r = Remote;
		Local.TeleportTo( r.WorldPosition - Rotation.FromYaw( 0 ).Forward * distance + Vector3.Up * 4 );
	}

	[ConCmd( "tnt_ram" )]
	public static void Ram()
	{
		var dir = (Remote.WorldPosition - Local.WorldPosition).WithZ( 0 ).Normal;
		Local.Launch( dir * 600f );
	}

	[ConCmd( "tnt_state" )]
	public static void State()
	{
		foreach ( var p in Game.ActiveScene.GetAll<TntPlayer>() )
			Log.Info( $"{p.DisplayName} proxy={p.IsProxy} alive={p.IsAlive} ragdoll={p.IsRagdolled} rp={p.RankPoints} pos={p.WorldPosition} loadout={p.Abilities.First}/{p.Abilities.Second} statuses={string.Join( ",", p.Status.Current )}" );
	}
}
