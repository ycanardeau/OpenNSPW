namespace Aigamo.Saruhashi;

public sealed class KeyEventArgs
{
	public bool Alt => KeyData.HasFlag(Keys.Alt);
	public bool Control => KeyData.HasFlag(Keys.Control);
	public bool Handled { get; set; }
	public Keys KeyCode => KeyData & Keys.KeyCode;
	public Keys KeyData { get; }
	public int KeyValue => (int)KeyCode;
	public Keys Modifiers => KeyData & Keys.Modifiers;
	public bool Shift => KeyData.HasFlag(Keys.Shift);
	public bool SuppressKeyPress { get; set; }

	public KeyEventArgs(Keys keyData)
	{
		KeyData = keyData;
	}
}
