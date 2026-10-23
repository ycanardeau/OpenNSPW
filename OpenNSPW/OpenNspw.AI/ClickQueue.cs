namespace OpenNspw.AI;

// The clicks a player gives, one at a time, each taking as many frames as a click with the mouse: the press, then the
// release, two frames each (as the tests' CommandInput waits). A click is computed when its frame comes, from the game's
// state then.
public sealed class ClickQueue
{
	private const int FramesPerClick = 4;

	private readonly Queue<(Action<Nspw> Click, int Frames)> _steps = new();
	private int _wait;

	public bool IsEmpty => _steps.Count == 0 && _wait == 0;

	// A click, which may give no command once its frame comes.
	public void Enqueue(Action<Nspw> click)
	{
		_steps.Enqueue((click, FramesPerClick));
	}

	// A wait of `frames` frames after the clicks before it.
	public void Wait(int frames)
	{
		_steps.Enqueue((_ => { }, frames));
	}

	public void Clear()
	{
		_steps.Clear();
	}

	// Called once a frame: gives the next click once the last step has had its frames.
	public void Act(Nspw game)
	{
		if (_wait > 0)
		{
			_wait--;
			return;
		}

		if (_steps.TryDequeue(out var step))
		{
			step.Click(game);
			_wait = step.Frames - 1;
		}
	}
}
