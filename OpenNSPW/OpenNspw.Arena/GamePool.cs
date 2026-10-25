using System.Collections.Concurrent;

namespace OpenNspw.Arena;

// Plays games on threads of their own, `parallel` at a time, each game on one, and writes a line as each ends.
internal static class GamePool
{
	public static List<ArenaResult> Play(IReadOnlyList<Func<ArenaResult>> games, int parallel)
	{
		var queue = new ConcurrentQueue<Func<ArenaResult>>(games);
		var results = new ConcurrentBag<ArenaResult>();
		var started = DateTime.Now;
		Console.WriteLine($"{games.Count} games, {parallel} at a time.");
		var workers = Enumerable
			.Range(0, Math.Min(parallel, games.Count))
			.Select(i => new Thread(() =>
			{
				while (queue.TryDequeue(out var game))
				{
					var result = game();
					results.Add(result);
					lock (results)
					{
						Console.WriteLine(
							$"[{results.Count}/{games.Count} {DateTime.Now - started:hh\\:mm\\:ss}] scenario {result.Scenario} date {result.StartDate} {result.Player}: {result.Outcome} after {result.Frames} frames, {result.TimeLeft} left ({result.Elapsed.TotalSeconds:F0} s)"
						);
						if (result.Error is { } error)
							Console.WriteLine(error);
					}
				}
			})
			{
				Name = $"Arena {i}",
				IsBackground = true,
			})
			.ToList();

		foreach (var worker in workers)
		{
			worker.Start();
		}

		foreach (var worker in workers)
		{
			worker.Join();
		}

		return [.. results];
	}
}
