using Aigamo.Otsuki.Actors;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// A small case of the simulation whose orders an exploration takes, by name: one run of it on the scheduler that the
/// conditions give.
/// </summary>
internal sealed record SimulationExploration(string Name, Action<SimulationConditions> Run);

/// <summary>
/// Explores the orders of small cases (<see cref="ManualScheduler.ExploreAll"/>): the session forms in the order of
/// scheduling, then every order of what follows the point the case is about, within a bound of delays or all of them.
/// </summary>
internal static class SimulationExplorations
{
	public static IReadOnlyList<SimulationExploration> All { get; } =
	[
		new(
			"MigrationOf3",
			conditions =>
				SimulationScenarios.Departures(conditions, 3, true, 0, crashes: false, joins: false)
		),
		new(
			"MigrationOf3AfterACrash",
			conditions =>
				SimulationScenarios.Departures(conditions, 3, true, 0, crashes: true, joins: false)
		),
		new(
			"MigrationOf4",
			conditions =>
				SimulationScenarios.Departures(conditions, 4, true, 0, crashes: false, joins: false)
		),
		new(
			"MigrationOf4AfterACrash",
			conditions =>
				SimulationScenarios.Departures(conditions, 4, true, 0, crashes: true, joins: false)
		),
		new(
			"SecondLeavesInAMigrationOf4",
			conditions =>
				SimulationScenarios.Departures(conditions, 4, true, 1, crashes: false, joins: false)
		),
		new(
			"SecondCrashesInAMigrationOf4",
			conditions =>
				SimulationScenarios.Departures(conditions, 4, true, 1, crashes: true, joins: false)
		),
		new(
			"JoinRacesALeave",
			conditions =>
				SimulationScenarios.Departures(conditions, 3, false, 1, crashes: false, joins: true)
		),
		new(
			"JoinRacesTheHostLeaving",
			conditions =>
				SimulationScenarios.Departures(conditions, 3, true, 0, crashes: false, joins: true)
		),
	];

	/// <summary>
	/// The choices of a run that are not the first that waits, as step:choice, which <see cref="Replay"/> takes again.
	/// </summary>
	public static string Choices(IReadOnlyList<(int Choice, int Of)> taken) =>
		string.Join(
			",",
			taken
				.Select((step, index) => (step.Choice, index))
				.Where(step => step.Choice != 0)
				.Select(step => $"{step.index}:{step.Choice}")
		);

	private static List<int> ParseChoices(string choices)
	{
		List<int> parsed = [];
		foreach (var pair in choices.Split(',', StringSplitOptions.RemoveEmptyEntries))
		{
			var parts = pair.Split(':');
			var step = int.Parse(parts[0]);
			while (parsed.Count < step)
				parsed.Add(0);
			parsed.Add(int.Parse(parts[1]));
		}

		return parsed;
	}

	/// <summary>
	/// Runs <paramref name="exploration"/> once in the order that <paramref name="choices"/> (<see cref="Choices"/>)
	/// say.
	/// </summary>
	public static void Replay(SimulationExploration exploration, string choices) =>
		exploration.Run(
			SimulationConditions.Plain with
			{
				Scheduler = ManualScheduler.Following(ParseChoices(choices)),
			}
		);

	/// <summary>
	/// Runs <paramref name="exploration"/> in every order from its marked point that passes over at most
	/// <paramref name="maxDelays"/> mailboxes, <paramref name="maxRuns"/> at most; throws, naming the order, at the
	/// first that fails. Returns the runs, whether they were every order within the bound, and the step explored from.
	/// </summary>
	public static (int Runs, bool IsExhausted, int FromStep) Explore(
		SimulationExploration exploration,
		int maxRuns,
		int maxDelays = int.MaxValue
	)
	{
		Simulation? first = null;
		exploration.Run(
			SimulationConditions.Plain with
			{
				Scheduler = ManualScheduler.Following([]),
				OnStart = simulation => first = simulation,
			}
		);
		var fromStep = first!.MarkedStep;
		var (runs, isExhausted) = ManualScheduler.ExploreAll(
			scheduler =>
			{
				try
				{
					exploration.Run(SimulationConditions.Plain with { Scheduler = scheduler });
				}
				catch (Exception exception)
				{
					throw new InvalidOperationException(
						$"{exploration.Name} fails in the order {Choices(scheduler.Taken)}: {exception.Message}",
						exception
					);
				}
			},
			maxRuns,
			fromStep,
			maxDelays
		);
		return (runs, isExhausted, fromStep);
	}
}
