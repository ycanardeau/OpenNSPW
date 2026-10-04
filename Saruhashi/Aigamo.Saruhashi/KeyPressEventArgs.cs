namespace Aigamo.Saruhashi;

public sealed class KeyPressEventArgs : EventArgs
{
	public char KeyChar { get; set; }
	public bool Handled { get; set; }

	public KeyPressEventArgs(char keyChar)
	{
		KeyChar = keyChar;
	}
}
