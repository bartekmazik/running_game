/// <summary>Floor 5 (index 4): a library — oak parquet underfoot, walnut coffers overhead.</summary>
public sealed class LibraryTheme : ArenaTheme
{
	public override string Name => "Library";
	public override int Floor => 4;
	public override Color LightColor => new( 1.30f, 0.95f, 0.66f );

	// Full surface in Task 4b.
	protected override Material PaintFloor() => PaintPlainFloor( Palette.Hex( 0xB98A62 ), Palette.Hex( 0xCFA27A ), Palette.Hex( 0x9C7050 ), 0.4f );

	// Full surface in Task 4b.
	protected override Material PaintCeiling() => PaintPlainCeiling( Palette.Hex( 0x6E4F3E ), Palette.Hex( 0x82604C ), Palette.Hex( 0x5A3F31 ), 0.5f );

	public override void Dress( FloorContext room )
	{
		// Room dressing arrives in Task 6.
	}
}
