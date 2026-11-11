using UnityEngine;

namespace Aigamo.Saruhashi.Unity;

/// <summary>
/// A Unity font at a size, drawn and measured as IMGUI draws a label.
/// </summary>
/// <param name="font">The font, such as one of <see cref="Font.CreateDynamicFontFromOSFont(string, int)"/>.</param>
/// <param name="size">The size in pixels, for a dynamic font; 0 for the font's own.</param>
public sealed class UnityFont(Font font, int size) : IUnityFont
{
	// The text drawn or measured, in one content, as a paint draws every frame.
	private readonly GUIContent _content = new();

	// Clipped by the graphics, which clips to the control.
	private readonly GUIStyle _style = new()
	{
		font = font,
		fontSize = size,
		alignment = TextAnchor.UpperLeft,
		clipping = TextClipping.Overflow,
		richText = false,
		wordWrap = false,
	};

	public Font Font { get; } = font;
	public int Size { get; } = size;

	public Vector2 MeasureString(string? text)
	{
		_content.text = text;
		return _style.CalcSize(_content);
	}

	public void Draw(string text, Vector2 position, Color color)
	{
		_style.normal.textColor = color;
		GUI.Label(new Rect(position, MeasureString(text)), _content, _style);
	}
}
