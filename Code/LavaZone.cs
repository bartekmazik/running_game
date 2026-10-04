/// <summary>
/// Host-authoritative lava volume. Attached by <see cref="ArenaGenerator"/> to the lava slab it
/// builds, so the trigger and the visual always share one size. What touching it means — out
/// during a round, back on top during the warm-up — belongs to <see cref="RoundManager.HandleLava"/>.
/// </summary>
public sealed class LavaZone : Component, Component.ITriggerListener
{
	public void OnTriggerEnter( Collider other )
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;
		if ( !other.GameObject.Tags.Has( "player" ) ) return;

		// The collider may sit on a child of the player root.
		var player = other.GameObject.Components.GetInAncestorsOrSelf<TntPlayer>();
		if ( player is null || !player.IsAlive ) return;

		Scene.Get<RoundManager>().HandleLava( player );
	}

	public void OnTriggerExit( Collider other )
	{
	}
}
