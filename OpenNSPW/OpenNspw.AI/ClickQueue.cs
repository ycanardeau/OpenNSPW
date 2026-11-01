namespace OpenNspw.AI;

// The clicks a player gives, one at a time, each taking as long as a click with the mouse at speed 1: the press, then
// the release, two frames each (as the tests' CommandInput waits), by the battle's time (UnitFacts.Now), so that at a
// higher speed, whose frames run more ticks, the clicks come as often in the battle's time, up to one a frame, which is
// as often as the game takes them. A click is computed when its frame comes, from the game's state then, and may give
// up the clicks after it, when the game is not as the clicks before it should have left it. A step that finds no click
// to give, such as a deselection with nothing selected, may take no time (SkipsIdleClicks), the next one coming in the
// same frame.
public sealed class ClickQueue
{
	private readonly Queue<(Func<Nspw, bool> Click, int Frames, bool IsIdle)> _steps = new();
	private int _readyAt;
	private bool _wasIdle;

	// The frames of speed 1 a click takes: 4 by default, a press and a release of two frames each, as a hand gives them;
	// 1 is as fast as the game takes them.
	public int FramesPerClick { get; set; } = 4;

	// Whether a step that gave no click takes no time.
	public bool SkipsIdleClicks { get; set; }

	// Whether there is no click left, and the last has had its time.
	public bool IsEmpty(Nspw game) => _steps.Count == 0 && UnitFacts.Now(game) >= _readyAt;

	// A click, which returns false to give up the clicks after it.
	public void Enqueue(Func<Nspw, bool> click)
	{
		_steps.Enqueue((click, -1, false));
	}

	// A click that may find nothing to do, which it says by calling Idle: it then takes no time, with SkipsIdleClicks.
	public void EnqueueMaybe(Func<Nspw, Action, bool> click)
	{
		_steps.Enqueue((g => click(g, () => _wasIdle = true), -1, true));
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
		_steps.Enqueue((_ => true, frames, false));
	}

	public void Clear()
	{
		_steps.Clear();
	}

	// Called once a frame: gives the next click once the last step has had its time, and, with SkipsIdleClicks, the ones
	// after steps that gave none.
	public void Act(Nspw game)
	{
		var now = UnitFacts.Now(game);
		while (now >= _readyAt && _steps.TryDequeue(out var step))
		{
			_wasIdle = false;
			if (!step.Click(game))
			{
				_steps.Clear();
				return;
			}

			if (SkipsIdleClicks && step.IsIdle && _wasIdle)
				continue;

			_readyAt = now + (step.Frames < 0 ? FramesPerClick : step.Frames);
			return;
		}
	}
}
