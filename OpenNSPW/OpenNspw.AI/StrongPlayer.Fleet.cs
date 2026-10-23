namespace OpenNspw.AI;

// The strong player's ships: its carriers kept out of the computer's strike range, and its warships fighting in groups
// and repairing.
public sealed partial class StrongPlayer
{
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
}
