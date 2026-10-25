using System.Drawing;

namespace OpenNspw.Desktop.Controls;

// The menus' colors: the navy of a chart table, with brass for what can be chosen.
internal static class Theme
{
	public static readonly Color Ink = Color.FromArgb(6, 12, 22);
	public static readonly Color Navy = Color.FromArgb(12, 25, 43);
	public static readonly Color Panel = Color.FromArgb(16, 32, 54);
	public static readonly Color Line = Color.FromArgb(48, 72, 102);
	public static readonly Color Brass = Color.FromArgb(214, 176, 92);
	public static readonly Color BrassLight = Color.FromArgb(244, 214, 140);
	public static readonly Color Text = Color.FromArgb(232, 236, 240);
	public static readonly Color Muted = Color.FromArgb(136, 155, 178);
	public static readonly Color Alert = Color.FromArgb(236, 112, 99);
	public static readonly Color Japan = Color.FromArgb(206, 46, 56);
	public static readonly Color UnitedStates = Color.FromArgb(74, 130, 226);

	// The color with an opacity, premultiplied, as the sprite batch blends it.
	public static Color Alpha(Color color, float alpha)
	{
		var a = Math.Clamp(alpha, 0, 1);
		return Color.FromArgb(
			(int)(255 * a),
			(int)(color.R * a),
			(int)(color.G * a),
			(int)(color.B * a)
		);
	}

	public static Color Lerp(Color from, Color to, float amount)
	{
		var t = Math.Clamp(amount, 0, 1);
		return Color.FromArgb(
			(int)(from.A + (to.A - from.A) * t),
			(int)(from.R + (to.R - from.R) * t),
			(int)(from.G + (to.G - from.G) * t),
			(int)(from.B + (to.B - from.B) * t)
		);
	}
}
