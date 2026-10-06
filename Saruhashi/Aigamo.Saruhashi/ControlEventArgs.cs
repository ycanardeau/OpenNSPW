namespace Aigamo.Saruhashi;

public sealed class ControlEventArgs(Control control) : EventArgs
{
	public Control Control { get; } = control;
}
