namespace OpenNspw.AI;

// The strong player (docs/StrongAI.md): it remembers the enemies it has found, knows what the briefing tells, and runs
// its air operations (StrongPlayer.Air.cs), its scouting (StrongPlayer.Scouting.cs) and its ships (StrongPlayer.Fleet.cs).
// Each task is a series of clicks, given by the click queue; the next task is chosen once the last one is done, by its
// priority (Plan).
public sealed partial class StrongPlayer : IPlayer
{
	// How far from a deck its planes strike: well within their fuel, and beyond the computer's own strikes (3550, or 4350
	// as Japan).
	private const double StrikeRange = 6000;

	// How close to a deck enemy planes hold its strikes back.
	private const double RaidRange = 2000;

	// The share of its HP under which a warship goes to repair.
	private const double RepairBelow = 0.4;

	// The frames between a damaged ship's orders.
	private const int RepairFrames = 200;

	// The frames between a surface group's orders.
	private const int EngageFrames = 150;

	// How far beyond the computer's strike range a carrier keeps from the enemy's carriers.
	private const double KeepAwayMargin = 1000;

	// The frames between a carrier's moves.
	private const int PositionFrames = 150;

	// How much farther than its margin a carrier may be from what threatens it, before it closes in.
	private const double ApproachBand = 1000;

	// The frames for which a carrier found is thought to be where it would be by now.
	private const int CarrierMemoryFrames = 3000;

	// How far from its deck a combat air patrol flies, toward where the enemy most likely is.
	private const double PatrolDistance = 700;

	// How close to a deck or a ship an enemy attacker or bomber is intercepted.
	private const double InterceptRange = 3000;

	// How far a scout flies out from its deck when no area is known.
	private const double ScoutRange = 3800;

	// The frames after a strike or an interception before the same target is given more planes.
	private const int RetargetFrames = 400;

	// How far from a deck its planes strike a place the briefing names, before anything there is found.
	private const double PlaceStrikeRange = 8000;

	// The scouts of all the decks, at most.
	private const int MaxScouts = 3;

	// How far out a scout searches, at most.
	private const double ScoutReach = 7500;

	// The frames between strikes at the same place, before anything there is found.
	private const int PlaceStrikeFrames = 1500;

	// The frames for which a strike is still launched at an enemy that is no longer found, where it would be by now.
	private const int ContactFrames = 600;

	// The frames after a strike in which the other decks' ready planes join it.
	private const int WaveFrames = 200;

	// The planes a strike waits for, when the deck has them armed.
	private const int MinStrike = 3;

	// How soon, in ticks, an armed plane being serviced is worth waiting for.
	private const int SoonReadyTicks = 200;

	// The ready fighters of a deck kept back from escorting strikes, to intercept.
	private const double FightersKeptBack = 0.5;

	private readonly ClickQueue _clicks = new();
	private readonly Contacts _contacts = new();
	private readonly Dictionary<int, int> _engaged = [];
	private readonly Dictionary<string, int> _placeStrikes = [];
	private readonly Dictionary<int, int> _directed = [];
	private readonly Dictionary<int, int> _sweeps = [];
	private readonly Dictionary<int, int> _strikeTargets = [];
	private readonly HashSet<int> _toWaypoint = [];
	private readonly Dictionary<int, List<int>> _scoutsOf = [];
	private readonly HashSet<int> _formed = [];
	private readonly Dictionary<int, int> _positioned = [];
	private readonly HashSet<int> _repairing = [];
	private readonly Dictionary<int, int> _repairOrdered = [];
	private readonly Dictionary<int, int> _engagedAt = [];
	private readonly Dictionary<int, Queue<WorldPosition>> _scoutLegs = [];
	private readonly Dictionary<int, int> _scoutLaunches = [];
	private ScenarioKnowledge _knowledge = ScenarioKnowledge.For(0);
	private int _searches;
	private int _lastBlindStrike = -1;
	private (Contact Target, int Frame)? _wave;

	public string Name => "strong";

	// Where the player says what it does, to follow it, or null.
	public Action<string>? Log { get; init; }

	// Whether the player sees every enemy unit, as the computer sees the player's (Contacts.SeesEverything).
	public bool SeesEverything
	{
		get => _contacts.SeesEverything;
		init => _contacts.SeesEverything = value;
	}

	// Gives up the clicks of a task, saying why.
	private bool GiveUp(Nspw game, string why)
	{
		Log?.Invoke($"{game.Renderer.FrameCount}: gave up: {why}");
		return false;
	}

	private static bool IsOwn(Nspw game, int m) =>
		game.Units[m].IsUsed && game.Units[m].Side == game.LocalSide;

	// The player's carriers, light carriers and air bases.
	private static List<int> Decks(Nspw game)
	{
		var decks = new List<int>();
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (IsOwn(game, m) && UnitFacts.IsDeck(game.Units[m].Kind))
				decks.Add(m);
		}

		return decks;
	}

	// The player's planes parked on a deck that match.
	private static List<int> ParkedOn(Nspw game, int deck, Func<int, bool> match)
	{
		var planes = new List<int>();
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			if (
				IsOwn(game, n)
				&& UnitFacts.IsParked(ref game.Units[n])
				&& game.Units[n].Carrier == deck
				&& match(n)
			)
				planes.Add(n);
		}

		return planes;
	}

	// How much sinking or destroying a unit of a kind is worth: three times as much if the victory conditions count it.
	private double Value(UnitKind kind) =>
		(_knowledge.Counted.Contains(kind) ? 3 : 1)
		* kind switch
		{
			UnitKind.Carrier => 10,
			UnitKind.LightCarrier => 8,
			UnitKind.Battleship => 6,
			UnitKind.AirBase => 5,
			UnitKind.Cruiser or UnitKind.Transport => 4,
			UnitKind.Destroyer => 2,
			UnitKind.Submarine => 1,
			_ => 3,
		};

	// A point of the map at a bearing and a distance from another, kept 300 inside the map's edges.
	private static WorldPosition PointAt(WorldPosition from, double bearing, double distance)
	{
		var radians = bearing * Math.PI / 180;
		return new WorldPosition(
			Math.Clamp(
				from.X + (Math.Cos(radians) * distance),
				World.Left + 300,
				World.Right - 300
			),
			Math.Clamp(from.Y + (Math.Sin(radians) * distance), World.Bottom + 300, World.Top - 300)
		);
	}

	private static double BearingTo(WorldPosition from, WorldPosition to) =>
		Math.Atan2(to.Y - from.Y, to.X - from.X) * 180 / Math.PI;

	// Whether planes armed with a weapon can attack a unit of a kind: torpedoes only ships, bombs anything but planes.
	private static bool CanAttack(FireKind weapon, Contact target) =>
		target.Category == UnitCategory.Ship
		&& (weapon == FireKind.Bomb || !UnitFacts.IsBase(target.Kind));

	private bool WasEngaged(Nspw game, int target) =>
		_engaged.TryGetValue(target, out var frame)
		&& game.Renderer.FrameCount - frame < RetargetFrames;

	// Deselects the selected unit, if any.
	private void Deselect()
	{
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != 0)
				g.Commands.RightClick();
		});
	}

	// Shows a deck's parked planes in the sidebar, so that they can be clicked: selects the deck and switches its view to
	// the planes, which deselects it, the sidebar still showing it.
	private void ShowDeck(int deck)
	{
		Deselect();
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != 0 || !IsOwn(g, deck))
				return GiveUp(g, $"deck {deck}: selected {g.SelectedUnit}");
			g.Commands.ClickUnit(deck);
			return true;
		});
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != deck)
				return GiveUp(g, $"deck {deck} not selected: {g.SelectedUnit}");
			g.Commands.ToggleDeckView();
			return true;
		});
		_clicks.Enqueue(g =>
		{
			if (g.UnitInfoPanel[3] != deck || g.UnitInfoPanel[1] != 1)
				return GiveUp(
					g,
					$"deck {deck} not shown: selected {g.SelectedUnit}, view {g.UnitInfoPanel[1]}"
				);
			if (g.SelectedUnit != 0)
				g.Commands.RightClick();
			return true;
		});
	}

	// Selects a parked plane, then puts the others in its formation.
	private void SelectPlanes(int leader, IEnumerable<int> members)
	{
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != 0 || !UnitFacts.IsParked(ref g.Units[leader]))
				return GiveUp(g, $"plane {leader}: selected {g.SelectedUnit}");
			g.Commands.ClickUnit(leader);
			return true;
		});
		foreach (var member in members)
		{
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != leader)
					return GiveUp(g, $"plane {leader} not selected: {g.SelectedUnit}");
				if (UnitFacts.IsParked(ref g.Units[member]) && g.Selections[1][member] == 0)
					g.Commands.ClickUnit(member);
				return true;
			});
		}
	}

	// Whether the victory conditions count a kind, or count nothing in particular.
	private bool IsCounted(UnitKind kind) =>
		_knowledge.Counted.Count == 0 || _knowledge.Counted.Contains(kind);

	// Selects a unit of the player's alone, nothing being selected: a ship with the ships that follow it.
	private void SelectAlone(int m, string what)
	{
		Deselect();
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != 0 || !IsOwn(g, m))
				return GiveUp(g, $"{what} {m}: selected {g.SelectedUnit}");
			g.Commands.ClickUnit(m);
			return true;
		});
	}

	// Gives the selected unit m a destination, then deselects it.
	private void SendSelected(int m, WorldPosition point, string what)
	{
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != m)
				return GiveUp(g, $"{what} {m}: selected {g.SelectedUnit}");
			g.Commands.SetDestination(point);
			Log?.Invoke(
				$"{g.Renderer.FrameCount}: {g.Units[m].Kind} {m} {what} ({point.X:F0}, {point.Y:F0})"
			);
			return true;
		});
		Deselect();
	}

	private void Plan(Nspw game)
	{
		var decks = Decks(game);
		if (
			!TryIntercept(game, decks)
			&& !TryDirect(game)
			&& !TryGuideScouts(game)
			&& !TryScout(game, decks)
			&& !TryStrike(game, decks)
			&& !TryForm(game, decks)
			&& !TryFormBattleGroups(game)
			&& !TryRepair(game)
			&& !TryPosition(game, decks)
			&& !TryEngage(game)
			&& !TryPatrol(game, decks)
			&& !TryArm(game, decks)
		)
			_clicks.Wait(10);
	}

	// What the battle is like, for the log: the HP of each side's ships and bases, and how many planes each has.
	private void LogStatus(Nspw game)
	{
		var sides = new[]
		{
			game.LocalSide,
			game.LocalSide == Side.Japan ? Side.UnitedStates : Side.Japan,
		}.Select(side =>
		{
			var decks = new List<string>();
			var planes = 0;
			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				ref var unit = ref game.Units[m];
				if (unit.IsUsed && unit.Side == side && unit.Category == UnitCategory.Ship)
					decks.Add($"{unit.Kind.ToString()[..2]}{m} {unit.HP}/{unit.MaxHP}");
				if (unit.IsUsed && unit.Side == side && unit.Category == UnitCategory.Plane)
					planes++;
			}

			return $"{side}: {string.Join(", ", decks)}; {planes} planes";
		});
		Log?.Invoke($"{game.Renderer.FrameCount}: STATUS {string.Join(" | ", sides)}");
	}

	public void Act(Nspw game)
	{
		if (_knowledge.Enemy.Count == 0 && _knowledge.Counted.Count == 0)
			_knowledge = ScenarioKnowledge.For(game.ScenarioNumber);

		if (game.Renderer.FrameCount % 250 == 0)
			LogStatus(game);

		_contacts.Update(game);
		if (_clicks.IsEmpty)
			Plan(game);

		_clicks.Act(game);
	}
}
