/// <summary>Floor 8 (index 7): a forge over the lava — basalt underfoot, rock overhead; its undersides also face the lava shaft.</summary>
public sealed class ForgeTheme : ArenaTheme
{
	public override string Name => "Forge";
	public override int Floor => 7;
	public override Color LightColor => new( 1.60f, 0.62f, 0.36f );

	// Full surface in Task 4b.
	protected override Material PaintFloor() => PaintPlainFloor( Palette.Hex( 0x4A4250 ), Palette.Hex( 0x5E5566 ), Palette.Hex( 0x352F3A ), 0.8f );

	// Full surface in Task 4b.
	protected override Material PaintCeiling() => PaintPlainCeiling( Palette.Hex( 0x3B3440 ), Palette.Hex( 0x4A424F ), Palette.Hex( 0x2C2631 ), 0.85f );

	public override void Dress( FloorContext room )
	{
		// Room dressing arrives in Task 6.
	}
}
