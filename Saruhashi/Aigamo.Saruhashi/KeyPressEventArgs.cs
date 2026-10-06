namespace Aigamo.Saruhashi;

public sealed class KeyPressEventArgs(char keyChar) : EventArgs
{
	public char KeyChar { get; set; } = keyChar;
	public bool Handled { get; set; }
}
