namespace OpenNspw.AI;

// Chooses among orders by playing each forward in copies of the battle (GameFork), on random numbers of their own, and
// scoring how the battle stands after: the choice that scores best on average over the seeds is the one to give. The
// copies run in parallel, each on its own thread; a game keeps no state that its copies share. A copy that fails is
// left out of its candidate's average, and a candidate whose copies all fail scores the least.
public static class Lookahead
{
	// The average score of each candidate: a copy of the game for each candidate and seed, which `play` gives, for the
	// candidate and the copy, what plays a frame of it with the candidate's orders (GameFork.Step, with its clicks, or a
	// player's Act before it); played `frames` frames, and scored by `score`. `play` is called for every copy before any
	// is played, on the calling thread.
	public static double[] Evaluate(
		Nspw game,
		int candidates,
		Func<int, GameFork, Action<GameFork>> play,
		int frames,
		int seeds,
		Func<Nspw, double> score,
		uint seed = 1
	)
	{
		var forks = new GameFork[candidates * seeds];
		var plays = new Action<GameFork>[forks.Length];
		for (var i = 0; i < forks.Length; i++)
		{
			forks[i] = new GameFork(game);
			forks[i].Reseed(seed + (uint)(i % seeds) * 7919);
			plays[i] = play(i / seeds, forks[i]);
		}

		var scores = new double[forks.Length];
		Parallel.For(
			0,
			forks.Length,
			i =>
			{
				var fork = forks[i];
				try
				{
					for (var f = 0; f < frames && fork.Game.Result == GameResult.None; f++)
					{
						plays[i](fork);
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
