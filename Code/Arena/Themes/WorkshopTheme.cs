/// <summary>Floor 6 (index 5): a workshop — steel checker plate underfoot, galvanised sheet overhead.</summary>
public sealed class WorkshopTheme : ArenaTheme
{
	public override string Name => "Workshop";
	public override int Floor => 5;
	public override Color LightColor => new( 0.95f, 1.00f, 1.10f );

	// Full surface in Task 4b. Matte for now: on Painter.Metal both surfaces render black — the
	// scene has no environment for a metal to reflect (the skybox is off), and metal has no diffuse.
	protected override Material PaintFloor() => PaintPlainFloor( Palette.Hex( 0x9BA1A8 ), Palette.Hex( 0xC3C8CE ), Palette.Hex( 0x767C84 ), 0.45f );

	// Full surface in Task 4b.
	protected override Material PaintCeiling() => PaintPlainCeiling( Palette.Hex( 0x8E959C ), Palette.Hex( 0xA6ACB2 ), Palette.Hex( 0x737A81 ), 0.5f );

	public override void Dress( FloorContext room )
	{
		// Room dressing arrives in Task 6.
	}
}
