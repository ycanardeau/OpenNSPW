namespace Aigamo.Saruhashi.Unity;

public sealed class UnityGraphicsFactory : GraphicsFactory
{
	// The graphics that paints gave back, closed, for the next paints: every control paints every frame. On the UI's
	// thread.
	private readonly Stack<UnityGraphics> _free = new();

	public override Graphics Create(Control control) => new UnityGraphics(control);

	public override Graphics Rent(Control control)
	{
		if (_free.Count == 0)
			return Create(control);

		var graphics = _free.Pop();
		graphics.Reopen(control);
		return graphics;
	}

	public override void Return(Graphics graphics)
	{
		if (graphics is UnityGraphics own)
		{
			own.Close();
			_free.Push(own);
		}
		else
		{
			graphics.Dispose();
		}
	}
}
