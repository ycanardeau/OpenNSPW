namespace OpenNspw.App;

// The fonts that come with the app (Fonts, embedded in it), so that its text is the same on every platform, under the
// SIL Open Font License (Fonts/OFL.txt, beside the app): BIZ UDGothic, whose letters are all as wide, for the game's text,
// as the original's fonts are (SHIFTJIS_CHARSET, MS Gothic on Windows, which cannot be shipped); and its proportional
// twin, BIZ UDPGothic, for the menus.
internal static class BundledFonts
{
	private static byte[] Read(string file)
	{
		using var stream =
			typeof(BundledFonts).Assembly.GetManifestResourceStream($"Fonts/{file}")
			?? throw new FileNotFoundException($"The font {file} is not in the app.", file);
		var data = new byte[stream.Length];
		stream.ReadExactly(data);
		return data;
	}

	public static byte[] Game() => Read("BIZUDGothic-Regular.ttf");

	public static byte[] Menu() => Read("BIZUDPGothic-Regular.ttf");

	public static byte[] MenuBold() => Read("BIZUDPGothic-Bold.ttf");
}
