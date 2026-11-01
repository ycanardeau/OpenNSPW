namespace OpenNspw.AI;

// Chooses among orders by playing each forward in copies of the battle (GameFork), on random numbers of their own, and
// scoring how the battle stands after: the choice that scores best on average over the seeds is the one to give. The
// copies run in parallel, each on its own thread; a game keeps no state that its copies share. A copy that fails is
// left out of its candidate's average, and a candidate whose copies all fail scores the least.
public static class Lookahead
{
	// The average score of each candidate: a copy of the game for each candidate and seed, given the candidate's orders,
	// then played `frames` frames, each frame giving the candidate's clicks as they go on (`step`, at the point of the
	// frame where the game takes them), and scored by `score`.
	public static double[] Evaluate(
		Nspw game,
		int candidates,
		Func<int, Action<GameFork>> step,
		int frames,
		int seeds,
		Func<Nspw, double> score,
		uint seed = 1
	)
	{
		var forks = new GameFork[candidates * seeds];
		for (var i = 0; i < forks.Length; i++)
		{
			forks[i] = new GameFork(game);
			forks[i].Reseed(seed + (uint)(i % seeds) * 7919);
		}

		var scores = new double[forks.Length];
		Parallel.For(
			0,
			forks.Length,
			i =>
			{
				var fork = forks[i];
				var act = step(i / seeds);
				try
				{
					for (var f = 0; f < frames && fork.Game.Result == GameResult.None; f++)
					{
						fork.Step(act);
					}

					scores[i] = score(fork.Game);
				}
				catch (Exception)
				{
					// A copy on random numbers of its own may meet a failure of the game's that the battle itself would not
					// (such as the computer's route overrunning its path): that copy tells nothing.
					scores[i] = double.NaN;
				}
			}
		);

		return
		[
			.. Enumerable
				.Range(0, candidates)
				.Select(c =>
					Enumerable.Range(c * seeds, seeds).Where(i => !double.IsNaN(scores[i])).ToList()
						is { Count: > 0 } played
						? played.Average(i => scores[i])
						: double.NegativeInfinity
				),
		];
	}
}
