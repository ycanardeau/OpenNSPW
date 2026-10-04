namespace Aigamo.Saruhashi;

public sealed class ControlEventArgs : EventArgs
{
	public Control Control { get; }

	public ControlEventArgs(Control control)
	{
		Control = control;
	}
}
