using Aigamo.Otsuki.Actors;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// How a <see cref="ManualScheduler"/> chooses the order: at random, or by PCT's priorities of depth 2 or 3, each of
/// which finds a bug that takes that many messages in one order with a probability that has a lower bound.
/// </summary>
internal enum SimulationOrder
{
	Random,
	PrioritiesOfDepth2,
	PrioritiesOfDepth3,
}

/// <summary>
/// What a simulation runs in: how its scheduler orders the messages, and what its network does to the datagrams. A
/// seed and its conditions take the same run again.
/// </summary>
internal sealed record SimulationConditions
{
	// About the most steps a scenario takes, among which PCT draws the steps where it changes its priorities.
	private const int Steps = 1000;

	// When partitions start, from the start of the faults, and the shortest.
	private static readonly TimeSpan PartitionWindow = TimeSpan.FromSeconds(10);

	private static readonly TimeSpan ShortestPartition = TimeSpan.FromMilliseconds(100);

	/// <summary>
	/// No fault, in the order a random scheduler takes.
	/// </summary>
	public static SimulationConditions Plain { get; } = new();

	public SimulationOrder Order { get; init; }

	/// <summary>
	/// The share of datagrams lost, from 0 to 1.
	/// </summary>
	public double LossRate { get; init; }

	/// <summary>
	/// The share of datagrams delivered twice, from 0 to 1.
	/// </summary>
	public double DuplicateRate { get; init; }

	/// <summary>
	/// The least time a datagram takes. With no latency and no jitter, a datagram arrives at the time it was sent, as a
	/// step of its own that the scheduler orders among the others.
	/// </summary>
	public TimeSpan Latency { get; init; }

	/// <summary>
	/// Up to how much longer than <see cref="Latency"/> each datagram takes, drawn for each: datagrams overtake one
	/// another.
	/// </summary>
	public TimeSpan Jitter { get; init; }

	/// <summary>
	/// How many times the network splits, each time between two machines or around one, at a time and for a length that
	/// the seed draws.
	/// </summary>
	public int Partitions { get; init; }

	public TimeSpan LongestPartition { get; init; } = TimeSpan.FromSeconds(3);

	/// <summary>
	/// Whether the faults can be more than the protocol is meant to ride out, so that connections drop and the session
	/// may split: a scenario then checks only what holds whatever the network did.
	/// </summary>
	public bool IsRough { get; init; }

	/// <summary>
	/// Whether the network keeps its trace (<see cref="SimulatedNetwork.Trace"/>), which costs time.
	/// </summary>
	public bool KeepsTrace { get; init; }

	/// <summary>
	/// Called with each simulation as it starts, such as by a replay that prints what it did once it fails.
	/// </summary>
	public Action<Simulation>? OnStart { get; init; }

	public ManualScheduler CreateScheduler(int seed) =>
		Order switch
		{
			SimulationOrder.Random => new ManualScheduler(seed),
			SimulationOrder.PrioritiesOfDepth2 => ManualScheduler.WithPriorities(
				seed,
				depth: 2,
				Steps
			),
			_ => ManualScheduler.WithPriorities(seed, depth: 3, Steps),
		};

	/// <summary>
	/// When each partition starts, from the start of the faults, and how long it lasts.
	/// </summary>
	public IEnumerable<(TimeSpan Start, TimeSpan Length)> DrawPartitions(Random random)
	{
		for (var i = 0; i < Partitions; i++)
		{
			var start = TimeSpan.FromTicks(random.NextInt64(PartitionWindow.Ticks));
			var length = TimeSpan.FromTicks(
				random.NextInt64(
					ShortestPartition.Ticks,
					Math.Max(ShortestPartition.Ticks + 1, LongestPartition.Ticks)
				)
			);
			yield return (start, length);
		}
	}

	private static TimeSpan Milliseconds(Random random, int most) =>
		TimeSpan.FromMilliseconds(random.Next(most + 1));

	/// <summary>
	/// Draws the conditions of a seed: any order, and faults that the protocol is meant to ride out, losing and
	/// doubling up to a tenth of the datagrams, delaying them by up to 200 ms, and splitting the network for up to 3 s;
	/// with <paramref name="mayBeRough"/>, half the seeds far worse (<see cref="IsRough"/>).
	/// </summary>
	public static SimulationConditions Draw(int seed, bool mayBeRough = false)
	{
		var random = new Random(seed ^ 0x0C0FFEE);
		var order = (SimulationOrder)random.Next(3);
		if (mayBeRough && random.Next(2) == 0)
		{
			var latency = Milliseconds(random, 300);
			return new()
			{
				Order = order,
				LossRate = 0.1 + random.NextDouble() * 0.3,
				DuplicateRate = random.NextDouble() * 0.3,
				Latency = latency,
				Jitter = Milliseconds(random, 500),
				Partitions = random.Next(5),
				LongestPartition = TimeSpan.FromSeconds(60),
				IsRough = true,
			};
		}

		var delayed = random.Next(3) != 0;
		return new()
		{
			Order = order,
			LossRate = random.Next(2) == 0 ? 0 : random.NextDouble() * 0.1,
			DuplicateRate = random.Next(2) == 0 ? 0 : random.NextDouble() * 0.1,
			Latency = delayed ? Milliseconds(random, 100) : TimeSpan.Zero,
			Jitter = delayed ? Milliseconds(random, 100) : TimeSpan.Zero,
			Partitions = random.Next(3) == 0 ? random.Next(1, 4) : 0,
		};
	}

	public override string ToString() =>
		$"{Order}, loss {LossRate:0.###}, doubled {DuplicateRate:0.###}, latency {Latency.TotalMilliseconds} ms + {Jitter.TotalMilliseconds} ms, {Partitions} partitions up to {LongestPartition.TotalSeconds} s{(IsRough ? ", rough" : "")}";
}
