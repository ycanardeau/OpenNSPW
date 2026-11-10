namespace Aigamo.Saruhashi;

/// <summary>
/// Where text that is cut or copied goes, and where pasted text comes from (<see cref="WindowManager.Clipboard"/>): the
/// system's clipboard, which a backend reaches in its own way, or one of the window manager's own.
/// </summary>
public interface IClipboard
{
	/// <summary>
	/// The text on the clipboard, or an empty string if it holds none.
	/// </summary>
	string GetText();

	/// <summary>
	/// Puts <paramref name="text"/> on the clipboard, in place of what it held.
	/// </summary>
	void SetText(string text);
}

// A clipboard of the window manager's own, for a backend that does not reach the system's: text is pasted only where it
// was copied, in the same app.
internal sealed class LocalClipboard : IClipboard
{
	private string _text = string.Empty;

	public string GetText() => _text;

	public void SetText(string text) => _text = text;
}
