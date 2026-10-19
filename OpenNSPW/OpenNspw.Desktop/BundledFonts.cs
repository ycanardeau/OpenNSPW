namespace OpenNspw.Desktop;

// The fonts that come with the app (Fonts, copied beside it), so that its text is the same on every platform, under the
// SIL Open Font License (Fonts/OFL.txt): BIZ UDGothic, whose letters are all as wide, for the game's text, as the
// original's fonts are (SHIFTJIS_CHARSET, MS Gothic on Windows, which cannot be shipped); and its proportional twin,
// BIZ UDPGothic, for the menus.
internal static class BundledFonts
{
	private static byte[] Read(string file) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fonts", file));

	public static byte[] Game() => Read("BIZUDGothic-Regular.ttf");

	public static byte[] Menu() => Read("BIZUDPGothic-Regular.ttf");

	public static byte[] MenuBold() => Read("BIZUDPGothic-Bold.ttf");
}
