using System.Text.Json;

namespace OpenNspw.AI;

// What the player saw of the enemy in past games of a scenario, as a player who plays it again remembers: where each
// enemy unit the victory conditions count was found, and when (Sighting), the units numbered as the scenario numbers
// them, the same in every game. A game adds what it sees (Add); the next ones ask where a unit most likely is at a
// frame (Expected), over the games it was seen in near then. Safe to share between games played at once.
public sealed class ScenarioMemory
{
	// How far from a frame a sighting tells of it; and, for a unit seen only farther from it, how far its sightings
	// nearest in time still do: a carrier found first at frame 1000 was near there from the start.
	private const int FrameWindow = 600;
	private const int NearestWindow = 1500;

	// A unit of the enemy's found at a frame (UnitFacts.Now), where it was.
	public sealed record Sighting(int Unit, UnitKind Kind, int Frame, double X, double Y);

	private readonly Dictionary<int, List<Sighting>> _sightings = [];
	private readonly Lock _lock = new();

	private static string PathOf(string directory, int scenario) =>
		Path.Combine(directory, $"{scenario}.json");

	public void Add(int scenario, Sighting sighting)
	{
		lock (_lock)
		{
			if (!_sightings.TryGetValue(scenario, out var list))
				_sightings[scenario] = list = [];
			list.Add(sighting);
		}
	}

	// Where each unit seen within FrameWindow of `frame` in the games remembered, or else within NearestWindow, the
	// sightings nearest in time then, most likely is: the middle of those sightings, and how far they spread (the root
	// mean square of their distances from it).
	public IReadOnlyList<(int Unit, UnitKind Kind, WorldPosition Center, double Spread)> Expected(
		int scenario,
		int frame
	)
	{
		lock (_lock)
		{
			if (!_sightings.TryGetValue(scenario, out var list))
				return [];

			return
			[
				.. list.GroupBy(s => (s.Unit, s.Kind))
					.Select(g =>
					{
						var near = g.Where(s => Math.Abs(s.Frame - frame) <= FrameWindow).ToList();
						if (near.Count > 0)
							return near;

						var nearest = g.Min(s => Math.Abs(s.Frame - frame));
						return nearest > NearestWindow
							? near
							: [.. g.Where(s => Math.Abs(s.Frame - frame) <= nearest + 250)];
					})
					.Where(g => g.Count > 0)
					.Select(g =>
					{
						var center = new WorldPosition(g.Average(s => s.X), g.Average(s => s.Y));
						var spread = Math.Sqrt(
							g.Average(s =>
								((s.X - center.X) * (s.X - center.X))
								+ ((s.Y - center.Y) * (s.Y - center.Y))
							)
						);
						return (g[0].Unit, g[0].Kind, center, spread);
					}),
			];
		}
	}

	// The sightings of the scenarios remembered in a directory, a file a scenario, if there is one.
	public static ScenarioMemory Load(string directory)
	{
		var memory = new ScenarioMemory();
		if (!Directory.Exists(directory))
			return memory;

		foreach (var path in Directory.GetFiles(directory, "*.json"))
		{
			if (!int.TryParse(Path.GetFileNameWithoutExtension(path), out var scenario))
				continue;

			memory._sightings[scenario] =
				JsonSerializer.Deserialize<List<Sighting>>(File.ReadAllText(path)) ?? [];
		}

		return memory;
	}

	public void Save(string directory)
	{
		lock (_lock)
		{
			Directory.CreateDirectory(directory);
			foreach (var (scenario, list) in _sightings)
				File.WriteAllText(PathOf(directory, scenario), JsonSerializer.Serialize(list));
		}
	}
}
