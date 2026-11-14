namespace Aigamo.Saruhashi;

public sealed class KeyEventArgs(Keys keyData)
{
	public Keys KeyData { get; } = keyData;
	public bool Alt => KeyData.HasFlag(Keys.Alt);
	public bool Command => KeyData.HasFlag(Keys.Command);
	public bool Control => KeyData.HasFlag(Keys.Control);
	public bool Handled { get; set; }
	public Keys KeyCode => KeyData & Keys.KeyCode;
	public int KeyValue => (int)KeyCode;
	public Keys Modifiers => KeyData & Keys.Modifiers;
	public bool Shift => KeyData.HasFlag(Keys.Shift);
	public bool SuppressKeyPress { get; set; }
}
