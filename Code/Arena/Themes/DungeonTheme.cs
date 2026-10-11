/// <summary>Floor 7 (index 6): a dungeon — granite flagstones underfoot, rough stone overhead.</summary>
public sealed class DungeonTheme : ArenaTheme
{
	public override string Name => "Dungeon";
	public override int Floor => 6;
	public override Color LightColor => new( 1.40f, 0.82f, 0.48f );

	// Full surface in Task 4b.
	protected override Material PaintFloor() => PaintPlainFloor( Palette.Hex( 0x8D8798 ), Palette.Hex( 0xA49FAE ), Palette.Hex( 0x6F6A7A ), 0.7f );

	// Full surface in Task 4b.
	protected override Material PaintCeiling() => PaintPlainCeiling( Palette.Hex( 0x5E5866 ), Palette.Hex( 0x706A78 ), Palette.Hex( 0x4A4552 ), 0.85f );

	public override void Dress( FloorContext room )
	{
		// Room dressing arrives in Task 6.
	}
}
