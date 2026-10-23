namespace OpenNspw.AI;

// The clicks a player gives, one at a time, each taking as many frames as a click with the mouse: the press, then the
// release, two frames each (as the tests' CommandInput waits). A click is computed when its frame comes, from the
// game's state then, and may give up the clicks after it, when the game is not as the clicks before it should have left
// it.
public sealed class ClickQueue
{
	private const int FramesPerClick = 4;

	private readonly Queue<(Func<Nspw, bool> Click, int Frames)> _steps = new();
	private int _wait;

	public bool IsEmpty => _steps.Count == 0 && _wait == 0;

	// A click, which returns false to give up the clicks after it.
	public void Enqueue(Func<Nspw, bool> click)
	{
		_steps.Enqueue((click, FramesPerClick));
	}

	// A click that never gives up.
	public void Enqueue(Action<Nspw> click)
	{
		Enqueue(g =>
		{
			click(g);
			return true;
		});
	}

	// A wait of `frames` frames after the clicks before it.
	public void Wait(int frames)
	{
		_steps.Enqueue((_ => true, frames));
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
			if (step.Click(game))
			{
				_wait = step.Frames - 1;
			}
			else
			{
				_steps.Clear();
			}
		}
	}
}
