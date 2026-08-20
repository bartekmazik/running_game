public static class Arena
{
	public const int   GridSize      = 30;    // tiles per side, per floor
	public const float TileSize      = 56f;   // world units (s&box unit ≈ 1 inch)
	public const float TileThickness = 12f;
	public const int   FloorCount    = 5;
	public const float FloorGap      = 220f;  // vertical spacing between floors
	public const float DecayDelay    = 0.6f;  // seconds from step-on to disappear
	public const float LavaOffset    = 250f;  // below the lowest floor
}
