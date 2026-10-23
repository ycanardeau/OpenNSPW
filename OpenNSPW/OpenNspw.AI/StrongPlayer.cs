namespace OpenNspw.AI;

// The strong player (docs/StrongAI.md): it remembers the enemies it has found, and runs its carriers and air bases:
// arms their planes, strikes the enemies found within reach, intercepts the enemy's strikes, and scouts while it has
// found no enemy ship. Each task is a series of clicks, given by the click queue; the next task is chosen once the last
// one is done.
public sealed class StrongPlayer : IPlayer
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

	// Launches the selected parked planes, led by `leader`, toward an enemy unit found, to where they meet it, as computed
	// when the click comes. A parked plane cannot be given a target (CanClickUnit): TryDirect gives the planes theirs
	// once they fly.
	private void Launch(int leader, int target, Func<Nspw, WorldPosition> aim)
	{
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != leader)
				return GiveUp(g, $"launch of {leader}: selected {g.SelectedUnit}");
			g.Commands.SetDestination(aim(g));
			_engaged[target] = g.Renderer.FrameCount;
			Log?.Invoke(
				$"{g.Renderer.FrameCount}: {g.Units[leader].Kind} {leader} and {g.SelectionCount} more launched toward {g.Units[target].Kind} {target}"
			);
			return true;
		});
	}

	// Launches the selected parked planes, led by `leader`, at an enemy unit, by a waypoint 60% of the way there and 1000
	// to one side, the other side each strike, so that the strike does not fly into the enemy's own coming the other way;
	// TryDirect flies it on from the waypoint to where the target is then.
	private void LaunchStrike(int leader, Contact target, WorldPosition from)
	{
		var side = _strikeTargets.Count % 2 == 0 ? 90 : -90;
		var bearing = BearingTo(from, target.Position);
		var waypoint = PointAt(
			PointAt(from, bearing, UnitFacts.Distance(from, target.Position) * 0.6),
			bearing + side,
			1000
		);
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != leader)
				return GiveUp(g, $"launch of {leader}: selected {g.SelectedUnit}");
			g.Commands.SetDestination(waypoint);
			_engaged[target.Unit] = g.Renderer.FrameCount;
			_strikeTargets[leader] = target.Unit;
			_toWaypoint.Add(leader);
			Log?.Invoke(
				$"{g.Renderer.FrameCount}: {g.Units[leader].Kind} {leader} and {g.SelectionCount} more launched at {target.Kind} {target.Unit} by ({waypoint.X:F0}, {waypoint.Y:F0})"
			);
			return true;
		});
	}

	// Launches the selected parked planes, led by `leader`, toward a point where an enemy is thought to be.
	private void LaunchToward(int leader, WorldPosition point)
	{
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != leader)
				return GiveUp(g, $"launch of {leader}: selected {g.SelectedUnit}");
			g.Commands.SetDestination(point);
			Log?.Invoke(
				$"{g.Renderer.FrameCount}: {g.Units[leader].Kind} {leader} and {g.SelectionCount} more launched toward ({point.X:F0}, {point.Y:F0})"
			);
			return true;
		});
	}

	// Where an enemy strike seen lately most likely came from, back along its way, as far as the computer launches its
	// strikes from (3550, or 4350 as Japan); or null, if none was seen, or a strike was already launched there since.
	private WorldPosition? EnemyStrikeOrigin(Nspw game)
	{
		var plane = _contacts
			.All.Where(c =>
				c.Category == UnitCategory.Plane
				&& UnitFacts.IsStrikePlane(c.Kind)
				&& c.IsFlying
				&& game.Renderer.FrameCount - c.Frame < ContactFrames
				&& c.Frame > _lastBlindStrike
			)
			.OrderByDescending(c => c.Frame)
			.FirstOrDefault();
		return plane is null ? null : PointAt(plane.Position, plane.Direction + 180, 3000);
	}

	// Where a plane from a deck meets a contact, leading it by the time the plane takes to fly there.
	private static WorldPosition Intercept(Nspw game, int plane, Contact target)
	{
		var position = game.Units[game.Units[plane].Carrier].Position;
		var ticks =
			UnitFacts.Distance(position, target.Position)
			/ Math.Max(game.Units[plane].MaxSpeed, 0.5);
		var current = game.Units[target.Unit].IsFound
			? target with
			{
				Position = game.Units[target.Unit].Position,
				Direction = game.Units[target.Unit].Direction.Degrees,
				Speed = game.Units[target.Unit].Speed,
			}
			: target;
		return current.Predict(ticks);
	}

	// Whether enemy planes found in the air are near a deck.
	private bool IsUnderRaid(Nspw game, int deck) =>
		_contacts.All.Any(c =>
			c.IsFlying
			&& game.Renderer.FrameCount - c.Frame < 50
			&& UnitFacts.Distance(c.Position, game.Units[deck].Position) <= RaidRange
		);

	// Fighters against an enemy attacker or bomber found near a deck or a ship of the player's.
	private bool TryIntercept(Nspw game, List<int> decks)
	{
		foreach (var contact in _contacts.All)
		{
			if (
				contact.Category != UnitCategory.Plane
				|| !UnitFacts.IsStrikePlane(contact.Kind)
				|| !game.Units[contact.Unit].IsFound
				|| game.Units[contact.Unit].PlaneState != UnitState.Flying
				|| WasEngaged(game, contact.Unit)
			)
				continue;

			foreach (
				var deck in decks.OrderBy(d =>
					UnitFacts.Distance(game.Units[d].Position, contact.Position)
				)
			)
			{
				if (
					UnitFacts.Distance(game.Units[deck].Position, contact.Position) > InterceptRange
					|| !UnitFacts.CanLaunch(ref game.Units[deck])
				)
					continue;

				var fighters = ParkedOn(
					game,
					deck,
					n =>
						game.Units[n].Kind == UnitKind.Fighter
						&& UnitFacts.IsReady(ref game.Units[n])
				);
				if (fighters.Count == 0)
					continue;

				var group = fighters.Take(4).ToList();
				ShowDeck(deck);
				SelectPlanes(group[0], group.Skip(1));
				Launch(group[0], contact.Unit, g => Intercept(g, group[0], contact));
				return true;
			}
		}

		return false;
	}

	// The best target found for a flying plane of the player's: for an attacker or a bomber, an enemy ship its weapon can
	// attack, within 2500, by its worth, the one it was sent at favored; for a fighter, an enemy plane in the air,
	// attackers and bombers first, within 1500.
	private Contact? ChooseTargetInFlight(Nspw game, int plane)
	{
		ref var unit = ref game.Units[plane];
		var position = unit.Position;
		var weapon = unit.Weapon;
		return unit.Kind == UnitKind.Fighter
			? _contacts
				.All.Where(c =>
					c.Category == UnitCategory.Plane
					&& game.Units[c.Unit].IsFound
					&& game.Units[c.Unit].PlaneState == UnitState.Flying
					&& UnitFacts.Distance(position, c.Position) <= 1500
				)
				.OrderByDescending(c => UnitFacts.IsStrikePlane(c.Kind))
				.ThenBy(c => UnitFacts.Distance(position, c.Position))
				.FirstOrDefault()
			: _contacts
				.All.Where(c =>
					game.Units[c.Unit].IsFound
					&& CanAttack(weapon, c)
					&& UnitFacts.Distance(position, c.Position) <= 2500
				)
				.OrderByDescending(c =>
					Value(c.Kind)
					+ (_strikeTargets.GetValueOrDefault(plane) == c.Unit ? 4 : 0)
					- (UnitFacts.Distance(position, c.Position) / 1000)
				)
				.FirstOrDefault();
	}

	// The bearing from a deck along which the enemy most likely is: back along the way of the last enemy attacker or
	// bomber found, which came from its carrier or base; else toward the nearest area where the enemy is expected; else
	// toward the middle of the map.
	private double LikelyBearing(Nspw game, WorldPosition from)
	{
		var plane = _contacts
			.All.Where(c =>
				c.Category == UnitCategory.Plane
				&& UnitFacts.IsStrikePlane(c.Kind)
				&& c.IsFlying
				&& game.Renderer.FrameCount - c.Frame < 2000
			)
			.OrderByDescending(c => c.Frame)
			.FirstOrDefault();
		if (plane is not null)
			return BearingTo(from, plane.Predict(-1500));

		if (_knowledge.Enemy.Count > 0)
			return BearingTo(
				from,
				_knowledge.Enemy.MinBy(a => UnitFacts.Distance(from, a.Center))!.Center
			);

		return Math.Abs(from.X) + Math.Abs(from.Y) < 1000
			? 0
			: BearingTo(from, new WorldPosition(0, 0));
	}

	// A strike that has reached where it was sent and found nothing there sweeps on, 1500 at a time, turning 60° one way,
	// then the other, around the way the enemy most likely is, twice; then it waits there until its fuel sends it home.
	private bool TrySweep(Nspw game, int plane)
	{
		ref var unit = ref game.Units[plane];
		if (
			!UnitFacts.IsStrikePlane(unit.Kind)
			|| _scoutLegs.ContainsKey(plane)
			|| _toWaypoint.Contains(plane)
			|| UnitFacts.Distance(unit.Position, new WorldPosition(unit.PathX[0], unit.PathY[0]))
				> 250
		)
			return false;

		var sweeps = _sweeps.GetValueOrDefault(plane);
		if (sweeps >= 2)
			return false;

		_sweeps[plane] = sweeps + 1;
		var point = PointAt(
			unit.Position,
			LikelyBearing(game, unit.Position) + (sweeps == 0 ? 60 : -60),
			1500
		);
		Deselect();
		_clicks.Enqueue(g =>
		{
			if (
				g.SelectedUnit != 0
				|| !IsOwn(g, plane)
				|| g.Units[plane].PlaneState != UnitState.Flying
			)
				return GiveUp(g, $"sweeping {plane}: selected {g.SelectedUnit}");
			g.Commands.ClickUnit(plane);
			return true;
		});
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != plane)
				return GiveUp(g, $"sweep of {plane}: selected {g.SelectedUnit}");
			g.Commands.SetDestination(point);
			Log?.Invoke(
				$"{g.Renderer.FrameCount}: {g.Units[plane].Kind} {plane} sweeps toward ({point.X:F0}, {point.Y:F0})"
			);
			return true;
		});
		Deselect();
		return true;
	}

	// Whether the victory conditions count a kind, or count nothing in particular.
	private bool IsCounted(UnitKind kind) =>
		_knowledge.Counted.Count == 0 || _knowledge.Counted.Contains(kind);

	// The middle of a contact and the enemy ships found lately within 1500 of it, where they would be after `ticks` more
	// ticks: where the carriers or the battleships of a formation sail, its escorts around them.
	private WorldPosition FormationMiddle(Nspw game, Contact contact, double ticks)
	{
		var ships = _contacts
			.All.Where(c =>
				c.Unit == contact.Unit
				|| (
					c.Category == UnitCategory.Ship
					&& game.Renderer.FrameCount - c.Frame < ContactFrames
					&& UnitFacts.Distance(c.Position, contact.Position) <= 1500
				)
			)
			.Select(c => c.Predict(ticks))
			.Append(contact.Predict(ticks))
			.ToList();
		return new WorldPosition(ships.Average(p => p.X), ships.Average(p => p.Y));
	}

	// A strike that has reached its waypoint flies on to where its target is now, as last found, leading it by half the
	// time the strike takes to get there; or, if the target is not what the victory conditions count, such as an escort
	// found before the carrier it screens, to the middle of its formation.
	private bool TryFlyOnFromWaypoint(Nspw game, int leader)
	{
		ref var unit = ref game.Units[leader];
		if (
			!_toWaypoint.Contains(leader)
			|| UnitFacts.Distance(unit.Position, new WorldPosition(unit.PathX[0], unit.PathY[0]))
				> 250
		)
			return false;

		_toWaypoint.Remove(leader);
		if (_contacts.Find(_strikeTargets.GetValueOrDefault(leader)) is not { } target)
			return false;

		var ticks =
			(game.Renderer.FrameCount - target.Frame) * 2
			+ (
				UnitFacts.Distance(unit.Position, target.Position)
				/ Math.Max(unit.MaxSpeed, 0.5)
				/ 2
			);
		var point = IsCounted(target.Kind)
			? target.Predict(ticks)
			: FormationMiddle(game, target, ticks);
		Deselect();
		_clicks.Enqueue(g =>
		{
			if (
				g.SelectedUnit != 0
				|| !IsOwn(g, leader)
				|| g.Units[leader].PlaneState != UnitState.Flying
			)
				return GiveUp(g, $"flying on {leader}: selected {g.SelectedUnit}");
			g.Commands.ClickUnit(leader);
			return true;
		});
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != leader)
				return GiveUp(g, $"flying on {leader}: selected {g.SelectedUnit}");
			g.Commands.SetDestination(point);
			Log?.Invoke(
				$"{g.Renderer.FrameCount}: {g.Units[leader].Kind} {leader} flies on toward {target.Kind} {target.Unit} at ({point.X:F0}, {point.Y:F0})"
			);
			return true;
		});
		Deselect();
		return true;
	}

	// A strike whose target is alive but not found flies on to where it would be now, and takes no other target until it
	// has been there, if the victory conditions count the target: the escorts around a carrier are found first. Once there, with the target still not found, the
	// strike gives it up and takes what it finds.
	private bool TryKeepOnTarget(Nspw game, int leader)
	{
		ref var unit = ref game.Units[leader];
		if (!_strikeTargets.TryGetValue(leader, out var targetUnit) || _toWaypoint.Contains(leader))
			return false;

		if (
			!game.Units[targetUnit].IsUsed
			|| _contacts.Find(targetUnit) is not { } target
			|| !IsCounted(target.Kind)
		)
		{
			_strikeTargets.Remove(leader);
			return false;
		}

		if (game.Units[targetUnit].IsFound)
			return false;

		var point = target.Predict((game.Renderer.FrameCount - target.Frame) * 2);
		if (UnitFacts.Distance(unit.Position, point) < 400)
		{
			_strikeTargets.Remove(leader);
			return false;
		}

		if (UnitFacts.Distance(new WorldPosition(unit.PathX[0], unit.PathY[0]), point) < 300)
			return true;

		Deselect();
		_clicks.Enqueue(g =>
		{
			if (
				g.SelectedUnit != 0
				|| !IsOwn(g, leader)
				|| g.Units[leader].PlaneState != UnitState.Flying
			)
				return GiveUp(g, $"keeping {leader} on target: selected {g.SelectedUnit}");
			g.Commands.ClickUnit(leader);
			return true;
		});
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != leader)
				return GiveUp(g, $"keeping {leader} on target: selected {g.SelectedUnit}");
			g.Commands.SetDestination(point);
			return true;
		});
		Deselect();
		return true;
	}

	// A flying plane of the player's that leads its formation, armed and with no target in sight, given the best target
	// near it: the formation is selected with it, and takes the target too (ApplyOrders).
	private bool TryDirect(Nspw game)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var unit = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| unit.Category != UnitCategory.Plane
				|| unit.PlaneState != UnitState.Flying
				|| unit.GroupLeader != 0
				|| unit.Mode == UnitMode.Return
				|| unit.Ammo <= 0
				|| (
					unit.Target != 0
					&& game.Units[unit.Target].IsUsed
					&& game.Units[unit.Target].IsFound
				)
				|| (
					_directed.TryGetValue(m, out var frame)
					&& game.Renderer.FrameCount - frame < 100
				)
			)
				continue;

			_directed[m] = game.Renderer.FrameCount;
			if (TryFlyOnFromWaypoint(game, m) || TryKeepOnTarget(game, m))
				return true;

			if (ChooseTargetInFlight(game, m) is not { } target)
			{
				if (TrySweep(game, m))
					return true;
				continue;
			}

			var plane = m;
			Deselect();
			_clicks.Enqueue(g =>
			{
				if (
					g.SelectedUnit != 0
					|| !IsOwn(g, plane)
					|| g.Units[plane].PlaneState != UnitState.Flying
				)
					return GiveUp(g, $"directing {plane}: selected {g.SelectedUnit}");
				g.Commands.ClickUnit(plane);
				return true;
			});
			_clicks.Enqueue(g =>
			{
				if (
					g.SelectedUnit != plane
					|| !g.Units[target.Unit].IsUsed
					|| !g.Units[target.Unit].IsFound
				)
					return GiveUp(g, $"target {target.Unit} of {plane}: selected {g.SelectedUnit}");
				g.Commands.ClickUnit(target.Unit);
				return true;
			});
			if (UnitFacts.IsStrikePlane(unit.Kind))
			{
				// The attack takes over within 400 of the target, or at the end of the path (SetAttackerAttackDestination),
				// which a plane circles wider than it checks: the path goes right at the target.
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != plane || !g.Units[target.Unit].IsUsed)
						return GiveUp(g, $"run of {plane}: selected {g.SelectedUnit}");
					ref var aim = ref g.Units[target.Unit];
					var ticks =
						UnitFacts.Distance(g.Units[plane].Position, aim.Position)
						/ Math.Max(g.Units[plane].MaxSpeed, 0.5);
					g.Commands.SetDestination(
						new Contact(
							target.Unit,
							aim.Kind,
							aim.Category,
							false,
							aim.Position,
							aim.Direction.Degrees,
							aim.Speed,
							g.Renderer.FrameCount
						).Predict(ticks)
					);
					return true;
				});
			}

			_clicks.Enqueue(g =>
			{
				Log?.Invoke(
					$"{g.Renderer.FrameCount}: {g.Units[plane].Kind} {plane} {(g.Units[plane].Target == target.Unit ? "attacks" : "did not take")} {target.Kind} {target.Unit}"
				);
				if (g.SelectedUnit != 0)
					g.Commands.RightClick();
			});
			return true;
		}

		return false;
	}

	// The best enemy found lately within reach of a deck's planes armed with a weapon, or null.
	private Contact? ChooseTarget(Nspw game, int deck, FireKind weapon)
	{
		var position = game.Units[deck].Position;
		return _contacts
			.All.Where(c =>
				game.Renderer.FrameCount - c.Frame < ContactFrames
				&& CanAttack(weapon, c)
				&& !WasEngaged(game, c.Unit)
				&& UnitFacts.Distance(position, c.Position) <= StrikeRange
			)
			.OrderByDescending(c =>
				Value(c.Kind) - (UnitFacts.Distance(position, c.Position) / 2000)
			)
			.FirstOrDefault();
	}

	// A place the briefing names, within reach of a deck, where nothing has been found lately and no strike went lately:
	// a strike goes there to find what to attack, as a player strikes Pearl Harbor without seeing it first.
	private WorldPosition? PlaceToStrike(Nspw game, int deck)
	{
		var position = game.Units[deck].Position;
		var place = _knowledge.Enemy.FirstOrDefault(a =>
			a.IsPlace
			&& UnitFacts.Distance(position, a.Center) <= PlaceStrikeRange
			&& game.Renderer.FrameCount
				- _placeStrikes.GetValueOrDefault(a.Name, -PlaceStrikeFrames)
				>= PlaceStrikeFrames
			&& !_contacts.All.Any(c =>
				UnitFacts.Distance(c.Position, a.Center) <= a.Radius + 500
				&& game.Renderer.FrameCount - c.Frame < ContactFrames
			)
		);
		if (place is null)
			return null;

		_placeStrikes[place.Name] = game.Renderer.FrameCount;
		return place.Center;
	}

	// The planes a deck keeps to scout, unarmed ones first, then attackers before bombers: two of a deck with six attackers
	// and bombers or more, one of a deck with two or more, none of the others, and three in all at most.
	private List<int> ScoutsOf(Nspw game, int deck)
	{
		if (!_scoutsOf.TryGetValue(deck, out var scouts))
		{
			var strikers = ParkedOn(game, deck, n => UnitFacts.IsStrikePlane(game.Units[n].Kind));
			var count = Math.Min(
				strikers.Count >= 6 ? 2
					: strikers.Count >= 2 ? 1
					: 0,
				MaxScouts - _scoutsOf.Values.Sum(l => l.Count)
			);
			scouts =
			[
				.. strikers
					.OrderBy(n => game.Units[n].Weapon != FireKind.Unarmed)
					.ThenBy(n => game.Units[n].Kind == UnitKind.Bomber)
					.Take(count),
			];
			_scoutsOf[deck] = scouts;
		}

		scouts.RemoveAll(n => !IsOwn(game, n) || game.Units[n].Carrier != deck);
		return scouts;
	}

	// The attackers and bombers based on a deck, in the air or not, but its scouts.
	private int StrikePlanesOf(Nspw game, int deck)
	{
		var scouts = ScoutsOf(game, deck);
		var count = 0;
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			if (
				IsOwn(game, n)
				&& UnitFacts.IsStrikePlane(game.Units[n].Kind)
				&& game.Units[n].Carrier == deck
				&& !scouts.Contains(n)
			)
				count++;
		}

		return count;
	}

	// A deck's ready attackers and bombers, with escorting fighters, against the best enemy found within reach, once three
	// of its armed planes are ready, or all it has, and none will be ready soon, and no enemy strike is over it; or, while
	// another deck's strike has just gone, with whatever is ready, at the same target, so that they arrive together: a strike that takes off into the enemy's
	// is broken up as it climbs, so it waits for the raid to pass, and goes while the enemy's planes are coming home.
	private bool TryStrike(Nspw game, List<int> decks)
	{
		foreach (var deck in decks)
		{
			if (!UnitFacts.CanLaunch(ref game.Units[deck]) || IsUnderRaid(game, deck))
				continue;

			foreach (var weapon in new[] { FireKind.Torpedo, FireKind.Bomb })
			{
				var armed = ParkedOn(
					game,
					deck,
					n =>
						UnitFacts.IsStrikePlane(game.Units[n].Kind)
						&& game.Units[n].Weapon == weapon
						&& game.Units[n].Ammo > 0
				);
				var ready = armed.Where(n => UnitFacts.IsReady(ref game.Units[n])).ToList();
				var soon = armed.Count(n => game.Units[n].ReloadTime is > 0 and <= SoonReadyTicks);
				var wave =
					_wave is { } w
					&& game.Renderer.FrameCount - w.Frame < WaveFrames
					&& CanAttack(weapon, w.Target)
					&& game.Units[w.Target.Unit].IsUsed
					&& UnitFacts.Distance(game.Units[deck].Position, w.Target.Position)
						<= StrikeRange
						? w.Target
						: null;
				if (
					ready.Count == 0
					|| (
						wave is null
						&& (
							soon > 0
							|| (ready.Count < MinStrike && ready.Count < StrikePlanesOf(game, deck))
						)
					)
				)
					continue;

				var target = wave ?? ChooseTarget(game, deck, weapon);
				var origin = target is null
					? EnemyStrikeOrigin(game) ?? PlaceToStrike(game, deck)
					: null;
				if (target is null && origin is null)
					continue;

				var fighters = ParkedOn(
					game,
					deck,
					n =>
						game.Units[n].Kind == UnitKind.Fighter
						&& UnitFacts.IsReady(ref game.Units[n])
				);
				var escorts = fighters.Take(
					fighters.Count - (int)Math.Ceiling(fighters.Count * FightersKeptBack)
				);
				ShowDeck(deck);
				SelectPlanes(ready[0], ready.Skip(1).Concat(escorts));
				if (target is not null)
				{
					if (wave is null)
						_wave = (target, game.Renderer.FrameCount);
					LaunchStrike(ready[0], target, game.Units[deck].Position);
				}
				else
				{
					_lastBlindStrike = game.Renderer.FrameCount;
					LaunchToward(ready[0], origin!.Value);
				}

				return true;
			}
		}

		return false;
	}

	// A deck's unarmed attackers and bombers armed: torpedoes for attackers, bombs for bombers.
	private bool TryArm(Nspw game, List<int> decks)
	{
		foreach (var deck in decks)
		{
			if (game.Units[deck].IsSupplying)
				continue;

			foreach (
				var (kind, item) in new[]
				{
					(UnitKind.Attacker, CombatMenuItem.ArmWithTorpedo),
					(UnitKind.Bomber, CombatMenuItem.ArmWithBomb),
				}
			)
			{
				var scouts = ScoutsOf(game, deck);
				var unarmed = ParkedOn(
					game,
					deck,
					n =>
						!scouts.Contains(n)
						&& game.Units[n].Kind == kind
						&& game.Units[n].Weapon == FireKind.Unarmed
						&& game.Units[n].ReloadTime == 0
				);
				if (unarmed.Count == 0)
					continue;

				ShowDeck(deck);
				SelectPlanes(unarmed[0], unarmed.Skip(1));
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != unarmed[0])
						return GiveUp(g, $"arming {unarmed[0]}: selected {g.SelectedUnit}");
					g.Commands.ChooseCombatMenuItem(item);
					Log?.Invoke(
						$"{g.Renderer.FrameCount}: {unarmed.Count} {kind} on {deck} armed with {item}"
					);
					return true;
				});
				_clicks.Wait(8);
				Deselect();
				return true;
			}
		}

		return false;
	}

	// The two legs of the n-th search from a deck: across an area where the enemy is expected and within reach, the
	// areas in turn, from one side of it to the other, wider each time around; or, with no such area, an arc around the
	// way the enemy most likely is, a new one each time, nearest that way first.
	private (WorldPosition First, WorldPosition Second) SearchLegs(
		Nspw game,
		WorldPosition from,
		int n
	)
	{
		var areas = _knowledge
			.Enemy.Where(a => UnitFacts.Distance(from, a.Center) <= ScoutReach)
			.ToList();
		if (
			areas.Count > 0
			&& _contacts.All.All(c =>
				c.Category != UnitCategory.Plane
				|| !c.IsFlying
				|| game.Renderer.FrameCount - c.Frame > 2000
			)
		)
		{
			var area = areas[n % areas.Count];
			var across = BearingTo(from, area.Center) + 90;
			var half = Math.Max(area.Radius * (0.5 + (0.5 * (n / areas.Count % 2))), 500);
			return (PointAt(area.Center, across, half), PointAt(area.Center, across + 180, half));
		}

		var offset = n == 0 ? 0 : ((n + 1) / 2) * 60.0 * (n % 2 == 0 ? -1 : 1);
		var bearing = LikelyBearing(game, from) + offset;
		return (PointAt(from, bearing - 20, ScoutRange), PointAt(from, bearing + 20, ScoutRange));
	}

	// While no enemy ship has been found lately, the decks' scouts fly out, each sweeping an arc where the enemy most
	// likely is, a new arc each flight, nearest the likely bearing first; TryGuideScouts flies them on.
	private bool TryScout(Nspw game, List<int> decks)
	{
		if (
			_contacts.All.Any(c =>
				c.Category == UnitCategory.Ship && game.Renderer.FrameCount - c.Frame < 300
			)
		)
			return false;

		foreach (var deck in decks)
		{
			if (!UnitFacts.CanLaunch(ref game.Units[deck]))
				continue;

			var scout = ScoutsOf(game, deck)
				.FirstOrDefault(n =>
					!_scoutLegs.ContainsKey(n)
					&& UnitFacts.IsParked(ref game.Units[n])
					&& game.Units[n].ReloadTime == 0
				);
			if (scout == 0)
				continue;

			var position = game.Units[deck].Position;
			var (first, second) = SearchLegs(game, position, _searches++);
			_scoutLegs[scout] = new Queue<WorldPosition>([second]);
			ShowDeck(deck);
			SelectPlanes(scout, []);
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != scout)
				{
					_scoutLegs.Remove(scout);
					return GiveUp(g, $"scout {scout}: selected {g.SelectedUnit}");
				}

				g.Commands.SetDestination(first);
				_scoutLaunches[scout] = g.Renderer.FrameCount;
				Log?.Invoke(
					$"{g.Renderer.FrameCount}: {g.Units[scout].Kind} {scout} scouts toward ({first.X:F0}, {first.Y:F0})"
				);
				return true;
			});
			return true;
		}

		return false;
	}

	// A scout that has reached the end of its leg flies the next, or comes home (Return) after the last.
	private bool TryGuideScouts(Nspw game)
	{
		foreach (var (scout, legs) in _scoutLegs)
		{
			ref var unit = ref game.Units[scout];
			if (
				!IsOwn(game, scout)
				|| (
					UnitFacts.IsParked(ref unit)
					&& unit.DeckPhase == 0
					&& game.Renderer.FrameCount - _scoutLaunches.GetValueOrDefault(scout) > 300
				)
			)
			{
				_scoutLegs.Remove(scout);
				return false;
			}

			if (
				unit.PlaneState != UnitState.Flying
				|| unit.Mode == UnitMode.Return
				|| UnitFacts.Distance(
					unit.Position,
					new WorldPosition(unit.PathX[0], unit.PathY[0])
				) > 200
			)
				continue;

			Deselect();
			_clicks.Enqueue(g =>
			{
				if (
					g.SelectedUnit != 0
					|| !IsOwn(g, scout)
					|| g.Units[scout].PlaneState != UnitState.Flying
				)
					return GiveUp(g, $"guiding scout {scout}: selected {g.SelectedUnit}");
				g.Commands.ClickUnit(scout);
				return true;
			});
			if (legs.TryDequeue(out var next))
			{
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != scout)
						return GiveUp(g, $"scout {scout} not selected: {g.SelectedUnit}");
					g.Commands.SetDestination(next);
					return true;
				});
			}
			else
			{
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != scout)
						return GiveUp(g, $"scout {scout} not selected: {g.SelectedUnit}");
					g.Commands.ChooseCombatMenuItem(CombatMenuItem.Return);
					return true;
				});
				_clicks.Wait(8);
			}

			Deselect();
			return true;
		}

		return false;
	}

	// Each carrier of the player's takes the warships and the other carriers near it into its formation, once, so that
	// their guns cover it and they move as one.
	private bool TryForm(Nspw game, List<int> decks)
	{
		foreach (var carrier in decks)
		{
			ref var unit = ref game.Units[carrier];
			if (
				unit.Category != UnitCategory.Ship
				|| UnitFacts.IsBase(unit.Kind)
				|| !_formed.Add(carrier)
			)
				continue;

			var escorts = new List<int>();
			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				ref var ship = ref game.Units[m];
				if (
					m != carrier
					&& IsOwn(game, m)
					&& ship.Kind
						is UnitKind.Battleship
							or UnitKind.Cruiser
							or UnitKind.Destroyer
							or UnitKind.Carrier
							or UnitKind.LightCarrier
					&& ship.GroupLeader == 0
					&& ship.IsGroupLeader == 0
					&& UnitFacts.Distance(ship.Position, unit.Position) < 2000
					&& !_formed.Contains(m)
				)
					escorts.Add(m);
			}

			if (escorts.Count == 0)
				continue;

			_formed.UnionWith(escorts);
			var leader = carrier;
			var position = unit.Position;
			Deselect();
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != 0 || !IsOwn(g, leader))
					return GiveUp(g, $"forming {leader}: selected {g.SelectedUnit}");
				g.Commands.ClickUnit(leader);
				return true;
			});
			foreach (var escort in escorts)
			{
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != leader)
						return GiveUp(g, $"forming {leader}: selected {g.SelectedUnit}");
					if (IsOwn(g, escort) && g.Selections[1][escort] == 0)
						g.Commands.ClickUnit(escort);
					return true;
				});
			}

			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != leader)
					return GiveUp(g, $"forming {leader}: selected {g.SelectedUnit}");
				g.Commands.SetDestination(new WorldPosition(position.X + 80, position.Y));
				Log?.Invoke(
					$"{g.Renderer.FrameCount}: {g.Units[leader].Kind} {leader} forms with {g.SelectionCount} ships"
				);
				return true;
			});
			Deselect();
			return true;
		}

		return false;
	}

	// A carrier keeps a fighter in the air above it, toward where the enemy most likely is, while it has two ready: it sees
	// the enemy's strikes coming, and fights them, until its fuel brings it home (at 30) and another goes up.
	private bool TryPatrol(Nspw game, List<int> decks)
	{
		foreach (var deck in decks)
		{
			ref var unit = ref game.Units[deck];
			if (unit.Category != UnitCategory.Ship || !UnitFacts.CanLaunch(ref unit))
				continue;

			var flying = 0;
			for (var n = 1; n <= game.MaxUnitId; n++)
			{
				ref var plane = ref game.Units[n];
				if (
					IsOwn(game, n)
					&& plane.Kind == UnitKind.Fighter
					&& plane.PlaneState == UnitState.Flying
					&& plane.Carrier == deck
					&& plane.Mode != UnitMode.Return
				)
					flying++;
			}

			var ready = ParkedOn(
				game,
				deck,
				n => game.Units[n].Kind == UnitKind.Fighter && UnitFacts.IsReady(ref game.Units[n])
			);
			if (flying > 0 || ready.Count < 2)
				continue;

			var fighter = ready[0];
			var point = PointAt(unit.Position, LikelyBearing(game, unit.Position), PatrolDistance);
			ShowDeck(deck);
			SelectPlanes(fighter, []);
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != fighter)
					return GiveUp(g, $"patrol of {fighter}: selected {g.SelectedUnit}");
				g.Commands.SetDestination(point);
				Log?.Invoke(
					$"{g.Renderer.FrameCount}: Fighter {fighter} patrols at ({point.X:F0}, {point.Y:F0})"
				);
				return true;
			});
			return true;
		}

		return false;
	}

	// How far the computer's carriers strike from (3550, or 4350 as Japan: ComputerPlayer.md, Air strikes).
	private static double ComputerStrikeRange(Nspw game) =>
		game.CpuSide == Side.Japan ? 4350 : 3550;

	// What a carrier keeps its distance from: the enemy's carriers, as found lately, where they would be by now; or, none
	// found yet, the areas where the briefing expects them, if the victory conditions count them; and the places the
	// briefing names, whose air bases strike 1.5 times as far (ComputerPlayer.md, Air strikes). Each with the distance the
	// computer strikes from.
	private List<(WorldPosition Position, double Range)> Threats(Nspw game)
	{
		var range = ComputerStrikeRange(game);
		var threats = _contacts
			.All.Where(c =>
				c.Kind is UnitKind.Carrier or UnitKind.LightCarrier
				&& game.Renderer.FrameCount - c.Frame < CarrierMemoryFrames
			)
			.Select(c => (c.Predict(Math.Min(game.Renderer.FrameCount - c.Frame, 600) * 2), range))
			.ToList();
		if (threats.Count == 0 && _knowledge.Counted.Contains(UnitKind.Carrier))
			threats.AddRange(
				_knowledge.Enemy.Where(a => !a.IsPlace).Select(a => (a.Center, range))
			);

		threats.AddRange(
			_knowledge.Enemy.Where(a => a.IsPlace).Select(a => (a.Center, range * 1.5))
		);
		return threats;
	}

	// The point up to 4000 from a ship toward a distance from a threat, across the sea: straight at, or away from, the
	// threat, or turned up to 90° either way, whichever first brings it nearer that distance; or null.
	private static WorldPosition? Toward(
		Nspw game,
		WorldPosition from,
		WorldPosition threat,
		double distance
	)
	{
		var current = UnitFacts.Distance(from, threat);
		var bearing = BearingTo(from, threat) + (current < distance ? 180 : 0);
		var step = Math.Min(Math.Abs(current - distance), 4000);
		foreach (var turn in new[] { 0, 30, -30, 60, -60, 90, -90 })
		{
			var point = PointAt(from, bearing + turn, step);
			if (
				Math.Abs(UnitFacts.Distance(point, threat) - distance)
				>= Math.Abs(current - distance) - 150
			)
				continue;

			var samples = (int)(step / 150) + 1;
			var clear = true;
			for (var i = 1; i <= samples && clear; i++)
			{
				clear = UnitFacts.IsSea(
					game,
					new WorldPosition(
						from.X + ((point.X - from.X) * i / samples),
						from.Y + ((point.Y - from.Y) * i / samples)
					)
				);
			}

			if (clear)
				return point;
		}

		return null;
	}

	// A carrier keeps just beyond the computer's strike range from what threatens it, with a margin: the computer launches
	// only within it, so a carrier out of it is struck by none, while its own planes reach farther. It closes in while it
	// is farther than its planes should fly, and moves away while it is within the range, along the sea, its formation with
	// it.
	private bool TryPosition(Nspw game, List<int> decks)
	{
		var threats = Threats(game);
		foreach (var carrier in decks)
		{
			ref var unit = ref game.Units[carrier];
			if (
				unit.Category != UnitCategory.Ship
				|| UnitFacts.IsBase(unit.Kind)
				|| unit.GroupLeader != 0
				|| unit.IsSupplying
				|| threats.Count == 0
				|| game.Renderer.FrameCount
					- _positioned.GetValueOrDefault(carrier, -PositionFrames)
					< PositionFrames
			)
				continue;

			var position = unit.Position;
			var (threat, range) = threats.MinBy(t =>
				UnitFacts.Distance(t.Position, position) - t.Range
			);
			var low = range + KeepAwayMargin;
			var distance = UnitFacts.Distance(position, threat);
			var destination = new WorldPosition(unit.PathX[0], unit.PathY[0]);
			var isMoving =
				unit.PathX[0] != World.Right + 1 && UnitFacts.Distance(destination, position) > 400;
			var aim = low + (ApproachBand / 2);
			if (
				(distance >= low && distance <= low + ApproachBand)
				|| (
					isMoving
					&& Math.Abs(UnitFacts.Distance(destination, threat) - aim)
						< Math.Abs(distance - aim)
					&& UnitFacts.Distance(destination, threat) >= low
				)
			)
				continue;

			_positioned[carrier] = game.Renderer.FrameCount;
			if (Toward(game, position, threat, aim) is not { } point)
				continue;

			var leader = carrier;
			Deselect();
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != 0 || !IsOwn(g, leader))
					return GiveUp(g, $"moving {leader}: selected {g.SelectedUnit}");
				g.Commands.ClickUnit(leader);
				return true;
			});
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != leader)
					return GiveUp(g, $"moving {leader}: selected {g.SelectedUnit}");
				g.Commands.SetDestination(point);
				Log?.Invoke(
					$"{g.Renderer.FrameCount}: {g.Units[leader].Kind} {leader} moves to ({point.X:F0}, {point.Y:F0}), {UnitFacts.Distance(point, threat):F0} from ({threat.X:F0}, {threat.Y:F0})"
				);
				return true;
			});
			Deselect();
			return true;
		}

		return false;
	}

	private static bool IsWarship(UnitKind kind) =>
		kind is UnitKind.Battleship or UnitKind.Cruiser or UnitKind.Destroyer;

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

	// The warships that sail alone, near each other, form a line behind the strongest of them, battleships first, once.
	private bool TryFormBattleGroups(Nspw game)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var seed = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| !IsWarship(seed.Kind)
				|| _formed.Contains(m)
				|| seed.GroupLeader != 0
				|| seed.IsGroupLeader != 0
				|| seed.IsSupplying
			)
				continue;

			var position = seed.Position;
			var group = new List<int>();
			for (var n = 1; n <= game.MaxUnitId; n++)
			{
				ref var ship = ref game.Units[n];
				if (
					IsOwn(game, n)
					&& IsWarship(ship.Kind)
					&& !_formed.Contains(n)
					&& ship.GroupLeader == 0
					&& ship.IsGroupLeader == 0
					&& !ship.IsSupplying
					&& UnitFacts.Distance(ship.Position, position) <= 2500
				)
					group.Add(n);
			}

			_formed.UnionWith(group);
			if (group.Count < 2)
				continue;

			var leader = group
				.OrderBy(n => game.Units[n].Kind)
				.ThenByDescending(n => game.Units[n].HP)
				.First();
			var leaderPosition = game.Units[leader].Position;
			SelectAlone(leader, "forming");
			foreach (var member in group.Where(n => n != leader))
			{
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != leader)
						return GiveUp(g, $"forming {leader}: selected {g.SelectedUnit}");
					if (IsOwn(g, member) && g.Selections[1][member] == 0)
						g.Commands.ClickUnit(member);
					return true;
				});
			}

			SendSelected(
				leader,
				new WorldPosition(leaderPosition.X + 80, leaderPosition.Y),
				$"leads {group.Count - 1} ships to"
			);
			return true;
		}

		return false;
	}

	// The player's naval base nearest a point, built, or 0.
	private static int NearestNavalBase(Nspw game, WorldPosition position)
	{
		var best = 0;
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var unit = ref game.Units[m];
			if (
				IsOwn(game, m)
				&& unit.Kind == UnitKind.NavalBase
				&& unit.BuildTime == 0
				&& (
					best == 0
					|| UnitFacts.Distance(unit.Position, position)
						< UnitFacts.Distance(game.Units[best].Position, position)
				)
			)
				best = m;
		}

		return best;
	}

	// A warship that has lost most of its HP sails to the nearest naval base of the player's, and supplies there once it
	// has stopped, which the combat menu offers only within 280 of the base (ListCombatMenu); it fights again once repaired.
	private bool TryRepair(Nspw game)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var ship = ref game.Units[m];
			if (!IsOwn(game, m) || !IsWarship(ship.Kind))
				continue;

			if (ship.IsSupplying || ship.HP >= ship.MaxHP)
			{
				if (_repairing.Remove(m) && !ship.IsSupplying)
					_formed.Remove(m);
				continue;
			}

			if (!_repairing.Contains(m) && ship.HP >= ship.MaxHP * RepairBelow)
				continue;

			var navalBase = NearestNavalBase(game, ship.Position);
			if (
				navalBase == 0
				|| game.Renderer.FrameCount - _repairOrdered.GetValueOrDefault(m, -RepairFrames)
					< RepairFrames
			)
				continue;

			_repairing.Add(m);
			_repairOrdered[m] = game.Renderer.FrameCount;
			var basePosition = game.Units[navalBase].Position;
			var ship1 = m;
			if (UnitFacts.Distance(ship.Position, basePosition) > 240)
			{
				var berth = PointAt(basePosition, BearingTo(basePosition, ship.Position), 160);
				SelectAlone(m, "sailing to repair");
				SendSelected(m, berth, "sails to repair at");
				return true;
			}

			if (!ship.IsStopping || ship.Speed != 0)
				continue;

			SelectAlone(m, "supplying");
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != ship1)
					return GiveUp(g, $"supplying {ship1}: selected {g.SelectedUnit}");
				g.Commands.ChooseCombatMenuItem(CombatMenuItem.Supply);
				Log?.Invoke(
					$"{g.Renderer.FrameCount}: {g.Units[ship1].Kind} {ship1} supplies, {g.Units[ship1].HP}/{g.Units[ship1].MaxHP}"
				);
				return true;
			});
			_clicks.Wait(8);
			Deselect();
			return true;
		}

		return false;
	}

	// The best enemy ship for a surface group to fight: found lately, by its worth and nearness, the ones the victory
	// conditions count first; or null.
	private Contact? ChooseSurfaceTarget(Nspw game, WorldPosition from) =>
		_contacts
			.All.Where(c =>
				c.Category == UnitCategory.Ship
				&& game.Renderer.FrameCount - c.Frame < CarrierMemoryFrames
			)
			.OrderByDescending(c =>
				Value(c.Kind)
				- (UnitFacts.Distance(from, c.Position) / 1500)
				- ((game.Renderer.FrameCount - c.Frame) / 300.0)
			)
			.FirstOrDefault();

	// Each surface group of the player's, led by a warship, goes at the best enemy ship it knows of, all its ships firing at
	// it once it is found (ApplyOrders gives the formation the leader's target), and keeps at it while it is found, unless
	// one the victory conditions count is found; or, knowing of none, toward where the briefing expects the enemy.
	private bool TryEngage(Nspw game)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var leader = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| !IsWarship(leader.Kind)
				|| leader.GroupLeader != 0
				|| leader.IsSupplying
				|| _repairing.Contains(m)
				|| game.Renderer.FrameCount - _engagedAt.GetValueOrDefault(m, -EngageFrames)
					< EngageFrames
			)
				continue;

			_engagedAt[m] = game.Renderer.FrameCount;
			var position = leader.Position;
			var current =
				leader.Target != 0
				&& game.Units[leader.Target].IsUsed
				&& game.Units[leader.Target].IsFound
					? _contacts.Find(leader.Target)
					: null;
			var best = ChooseSurfaceTarget(game, position);
			var target =
				current is not null
				&& (best is null || IsCounted(current.Kind) || !IsCounted(best.Kind))
					? current
					: best;
			WorldPosition point;
			if (target is not null)
			{
				var ticks =
					Math.Min(
						UnitFacts.Distance(position, target.Position)
							/ Math.Max(leader.MaxSpeed, 0.3)
							/ 2,
						600
					) + ((game.Renderer.FrameCount - target.Frame) * 2);
				point = target.Predict(ticks);
			}
			else if (_knowledge.Enemy.Count > 0)
			{
				point = _knowledge
					.Enemy.OrderBy(a => a.IsPlace)
					.ThenBy(a => UnitFacts.Distance(position, a.Center))
					.First()
					.Center;
			}
			else
			{
				continue;
			}

			var destination = new WorldPosition(leader.PathX[0], leader.PathY[0]);
			var hasTarget =
				target is null || leader.Target == target.Unit || !game.Units[target.Unit].IsFound;
			if (
				hasTarget
				&& leader.PathX[0] != World.Right + 1
				&& UnitFacts.Distance(destination, point) < 600
			)
				continue;

			var ship = m;
			SelectAlone(m, "engaging");
			if (target is not null)
			{
				var targetUnit = target.Unit;
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != ship)
						return GiveUp(g, $"engaging {ship}: selected {g.SelectedUnit}");
					if (
						g.Units[targetUnit].IsUsed
						&& g.Units[targetUnit].IsFound
						&& g.Units[ship].Target != targetUnit
					)
						g.Commands.ClickUnit(targetUnit);
					return true;
				});
			}

			SendSelected(
				m,
				point,
				target is null ? "sails toward" : $"engages {target.Kind} {target.Unit} at"
			);
			return true;
		}

		return false;
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
