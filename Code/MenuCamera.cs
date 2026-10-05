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
	const float OrbitDistance = 3300f;
	const float OrbitPitch = 24f;

	/// <summary>Degrees per second. Slow enough to read as ambience rather than motion.</summary>
	const float OrbitSpeed = 4f;

	/// <summary>Sideways offset of the aim point, in world units.</summary>
	const float FrameShift = 620f;

	float _yaw = 35f;

	protected override void OnUpdate()
	{
		if ( TntPlayer.FindLocal( Scene ) is not null ) return;

		var camera = Scene.Camera;
		if ( !camera.IsValid() ) return;

		_yaw += OrbitSpeed * Time.Delta;

		// Aimed a little above the tower's middle, so the whole stack and the lava glow sit in frame
		// below the top floor rather than the camera skimming across it. Derived from FloorCount,
		// so a taller or shorter tower stays framed.
		var focus = WorldPosition + Vector3.Down * ((Arena.FloorCount - 1) * Arena.FloorGap * 0.42f);
		var rotation = new Angles( OrbitPitch, _yaw, 0f ).ToRotation();

		// Look past the tower's left side, so it stands in the right two thirds of the frame and
		// the menu column on the left has open sky behind it.
		focus += rotation.Left * FrameShift;

		camera.WorldPosition = focus - rotation.Forward * OrbitDistance;
		camera.WorldRotation = rotation;
	}
}
