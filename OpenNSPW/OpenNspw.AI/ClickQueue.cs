namespace OpenNspw.AI;

// The clicks a player gives, one at a time, each taking as long as a click with the mouse at speed 1: the press, then
// the release, two frames each (as the tests' CommandInput waits), by the battle's time (UnitFacts.Now), so that at a
// higher speed, whose frames run more ticks, the clicks come as often in the battle's time, up to one a frame, which is
// as often as the game takes them. A click is computed when its frame comes, from the game's state then, and may give
// up the clicks after it, when the game is not as the clicks before it should have left it.
public sealed class ClickQueue
{
	private const int FramesPerClick = 4;

	private readonly Queue<(Func<Nspw, bool> Click, int Frames)> _steps = new();
	private int _readyAt;

	// Whether there is no click left, and the last has had its time.
	public bool IsEmpty(Nspw game) => _steps.Count == 0 && UnitFacts.Now(game) >= _readyAt;

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

	// A wait of `frames` frames of speed 1 after the clicks before it.
	public void Wait(int frames)
	{
		_steps.Enqueue((_ => true, frames));
	}

	public void Clear()
	{
		_steps.Clear();
	}

	// Called once a frame: gives the next click once the last step has had its time.
	public void Act(Nspw game)
	{
		var now = UnitFacts.Now(game);
		if (now < _readyAt || !_steps.TryDequeue(out var step))
			return;

		if (step.Click(game))
			_readyAt = now + step.Frames;
		else
			_steps.Clear();
	}
}
