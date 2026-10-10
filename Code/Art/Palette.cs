/// <summary>
/// The colours of the game, in one place. Arena builders, texture painters and (mirrored by hand in
/// Code/ui/_ui.scss) the UI all read from here, so a hue can't drift between the HUD and the world.
///
/// Rule: neutrals carry structure, one identity hue per floor, and the warm saturated pair Warn/Lava
/// means danger — nothing decorative may use them.
///
/// Expression-bodied properties rather than static fields: hotload keeps statics alive, and an edited
/// colour would otherwise not show until the editor restarts.
/// </summary>
public static class Palette
{
	public static Color Hex( uint rgb ) => Color.FromBytes( (int)(rgb >> 16) & 0xFF, (int)(rgb >> 8) & 0xFF, (int)rgb & 0xFF, 255 );

	public static Color Paper => Hex( 0xF4ECDC );
	public static Color PaperShade => Hex( 0xE2D3B8 );
	public static Color Ink => Hex( 0x2F2A3B );
	public static Color InkSoft => Hex( 0x5D5669 );

	public static Color Warn => Hex( 0xFF8A4C );
	public static Color Lava => Hex( 0xF25C54 );

	/// <summary>
	/// Identity hue per floor, top (0) to bottom (7): light and cool at the top, darker and warm near
	/// the lava, all in one lightness/chroma band. Mirrored as $floor0..$floor7 in _ui.scss.
	/// </summary>
	public static Color FloorHue( int floor ) => floor switch
	{
		0 => Hex( 0xA9CF95 ), // greenhouse sage
		1 => Hex( 0xA7CBE8 ), // winter ice
		2 => Hex( 0xF0CF8A ), // beach sand
		3 => Hex( 0x9FB0DA ), // office periwinkle
		4 => Hex( 0xC99BB0 ), // library rose
		5 => Hex( 0xC9A26B ), // workshop brass
		6 => Hex( 0x8F86AD ), // dungeon slate lilac
		_ => Hex( 0xE07E58 ), // forge ember
	};
}
