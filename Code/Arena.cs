public static class Arena
{
	public const int   GridSize      = 30;    // tiles per side, per floor
	public const float TileSize      = 56f;   // world units (s&box unit ≈ 1 inch)
	public const float TileThickness = 12f;
	public const int   FloorCount    = 8;
	public const float FloorGap      = 280f;  // vertical spacing between floors (220 until 2026-10-11: taller rooms)
	public const float DecayDelay    = 0.6f;  // seconds from step-on to disappear
	public const float LavaOffset    = 250f;  // below the lowest floor
}
