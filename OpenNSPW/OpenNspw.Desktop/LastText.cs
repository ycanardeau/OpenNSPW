namespace OpenNspw.Desktop;

// A text that the UI paints, made from a value, and made again only when the value or the language changes: the UI paints
// every frame, and a text made each time is garbage. One for each text painted, on the UI's thread.
internal sealed class LastText<T>(Func<T, string> make)
	where T : IEquatable<T>
{
	private readonly Func<T, string> _make = make;
	private (T Value, Language Language, string Text)? _last;

	public string Of(T value)
	{
		if (_last is not { } last || !last.Value.Equals(value) || last.Language != Texts.Language)
			_last = last = (value, Texts.Language, _make(value));
		return last.Text;
	}
}
