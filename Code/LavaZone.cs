/// <summary>
/// Host-authoritative elimination volume. Attached by <see cref="ArenaGenerator"/> to the
/// lava slab it builds, so the trigger and the visual always share one size.
/// Elimination only — respawn and round reset belong to RoundManager.
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

		player.Eliminate();
	}

	public void OnTriggerExit( Collider other )
	{
	}
}
