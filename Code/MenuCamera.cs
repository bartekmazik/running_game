/// <summary>
/// Drives the camera whenever this machine has no player of its own to follow: on the title
/// screen, in the lobby list, and in the moment between joining and being spawned. A slow orbit
/// around the stacked floors, so the menu sits on top of the actual arena instead of a backdrop.
///
/// Local presentation only. Added at runtime by <see cref="RoundManager"/>, like every other
/// component the scene doesn't contain.
/// </summary>
public sealed class MenuCamera : Component
{
	const float OrbitDistance = 2600f;
	const float OrbitPitch = 24f;

	/// <summary>Degrees per second. Slow enough to read as ambience rather than motion.</summary>
	const float OrbitSpeed = 4f;

	float _yaw = 35f;

	protected override void OnUpdate()
	{
		if ( Scene.GetAll<TntPlayer>().Any( p => !p.IsProxy ) ) return;

		var camera = Scene.Camera;
		if ( !camera.IsValid() ) return;

		_yaw += OrbitSpeed * Time.Delta;

		// Aimed a floor and a half down, so the shaft and the lava glow sit in frame below the
		// top floor rather than the camera skimming across it.
		var focus = WorldPosition + Vector3.Down * (Arena.FloorGap * 2.4f);
		var rotation = new Angles( OrbitPitch, _yaw, 0f ).ToRotation();

		camera.WorldPosition = focus - rotation.Forward * OrbitDistance;
		camera.WorldRotation = rotation;
	}
}
