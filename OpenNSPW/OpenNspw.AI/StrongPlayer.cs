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

	// How close to a deck or a ship an enemy attacker or bomber is intercepted.
	private const double InterceptRange = 3000;

	// How far a scout flies out from its deck.
	private const double ScoutRange = 3800;

	// The frames after a strike or an interception before the same target is given more planes.
	private const int RetargetFrames = 400;

	// The frames for which a strike is still launched at an enemy that is no longer found, where it would be by now.
	private const int ContactFrames = 600;

	// The ready fighters of a deck kept back from escorting strikes, to intercept.
	private const double FightersKeptBack = 0.5;

	private readonly ClickQueue _clicks = new();
	private readonly Contacts _contacts = new();
	private readonly Dictionary<int, int> _engaged = [];
	private readonly Dictionary<int, int> _directed = [];
	private readonly Dictionary<int, int> _sweeps = [];
	private readonly Dictionary<int, int> _strikeTargets = [];
	private readonly HashSet<int> _toWaypoint = [];
	private readonly Dictionary<int, List<int>> _scoutsOf = [];
	private readonly HashSet<int> _formed = [];
	private readonly Dictionary<int, Queue<WorldPosition>> _scoutLegs = [];
	private int _searches;
	private int _lastBlindStrike = -1;

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

	// How much sinking or destroying a unit of a kind is worth.
	private static double Value(UnitKind kind) =>
		kind switch
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
	// attack, within 2500, the one it was sent at first; for a fighter, an enemy plane in the air, attackers and bombers
	// first, within 1500.
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
				.OrderByDescending(c => _strikeTargets.GetValueOrDefault(plane) == c.Unit)
				.ThenByDescending(c =>
					Value(c.Kind) - (UnitFacts.Distance(position, c.Position) / 1000)
				)
				.FirstOrDefault();
	}

	// The bearing from a deck along which the enemy most likely is: back along the way of the last enemy attacker or
	// bomber found, which came from its carrier or base; else toward the middle of the map.
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

	// A strike that has reached its waypoint flies on to where its target is now, as last found, leading it by half the
	// time the strike takes to get there.
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

		var point = target.Predict(
			(game.Renderer.FrameCount - target.Frame) * 2
				+ (
					UnitFacts.Distance(unit.Position, target.Position)
					/ Math.Max(unit.MaxSpeed, 0.5)
					/ 2
				)
		);
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
	// has been there: the escorts around a carrier are found first. Once there, with the target still not found, the
	// strike gives it up and takes what it finds.
	private bool TryKeepOnTarget(Nspw game, int leader)
	{
		ref var unit = ref game.Units[leader];
		if (!_strikeTargets.TryGetValue(leader, out var targetUnit) || _toWaypoint.Contains(leader))
			return false;

		if (!game.Units[targetUnit].IsUsed || _contacts.Find(targetUnit) is not { } target)
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

	// A deck's ready attackers and bombers, with escorting fighters, against the best enemy found within reach, once its
	// armed planes are all ready, or six of them.
	private bool TryStrike(Nspw game, List<int> decks)
	{
		foreach (var deck in decks)
		{
			if (!UnitFacts.CanLaunch(ref game.Units[deck]))
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
				if (ready.Count == 0 || (ready.Count < armed.Count && ready.Count < 6))
					continue;

				var target = ChooseTarget(game, deck, weapon);
				var origin = target is null ? EnemyStrikeOrigin(game) : null;
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

	// The planes a deck keeps unarmed to scout, attackers before bombers: two of a deck with six attackers and bombers or
	// more, one of a deck with two or more, none of the others.
	private List<int> ScoutsOf(Nspw game, int deck)
	{
		if (!_scoutsOf.TryGetValue(deck, out var scouts))
		{
			var strikers = ParkedOn(game, deck, n => UnitFacts.IsStrikePlane(game.Units[n].Kind));
			var count =
				strikers.Count >= 6 ? 2
				: strikers.Count >= 2 ? 1
				: 0;
			scouts =
			[
				.. strikers
					.Where(n => game.Units[n].Weapon == FireKind.Unarmed)
					.OrderBy(n => game.Units[n].Kind == UnitKind.Bomber)
					.Take(count),
			];
			_scoutsOf[deck] = scouts;
		}

		scouts.RemoveAll(n => !IsOwn(game, n) || game.Units[n].Carrier != deck);
		return scouts;
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
			var i = _searches++;
			var offset = i == 0 ? 0 : ((i + 1) / 2) * 60.0 * (i % 2 == 0 ? -1 : 1);
			var bearing = LikelyBearing(game, position) + offset;
			var first = PointAt(position, bearing - 20, ScoutRange);
			_scoutLegs[scout] = new Queue<WorldPosition>([
				PointAt(position, bearing + 20, ScoutRange),
			]);
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
					unit.PlaneState == UnitState.Parked
					&& unit.DeckPhase == 0
					&& game.Renderer.FrameCount % 200 == 0
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

	// Each carrier of the player's takes the warships near it into its formation, once, so that their guns cover it.
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
					IsOwn(game, m)
					&& ship.Kind is UnitKind.Battleship or UnitKind.Cruiser or UnitKind.Destroyer
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

	private void Plan(Nspw game)
	{
		var decks = Decks(game);
		if (
			!TryIntercept(game, decks)
			&& !TryDirect(game)
			&& !TryForm(game, decks)
			&& !TryGuideScouts(game)
			&& !TryStrike(game, decks)
			&& !TryArm(game, decks)
			&& !TryScout(game, decks)
		)
			_clicks.Wait(10);
	}

	// What the battle is like, for the log: the HP of each side's carriers and air bases, and how many planes each has.
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
				if (unit.IsUsed && unit.Side == side && UnitFacts.IsDeck(unit.Kind))
					decks.Add($"{unit.Kind} {m} {unit.HP}/{unit.MaxHP}");
				if (unit.IsUsed && unit.Side == side && unit.Category == UnitCategory.Plane)
					planes++;
			}

			return $"{side}: {string.Join(", ", decks)}; {planes} planes";
		});
		Log?.Invoke($"{game.Renderer.FrameCount}: STATUS {string.Join(" | ", sides)}");
	}

	public void Act(Nspw game)
	{
		if (game.Renderer.FrameCount % 250 == 0)
			LogStatus(game);

		_contacts.Update(game);
		if (_clicks.IsEmpty)
			Plan(game);

		_clicks.Act(game);
	}
}
