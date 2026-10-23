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
				|| UnitFacts.IsAnchored(ref unit)
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
					&& !UnitFacts.IsAnchored(ref ship)
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

	// What a carrier keeps its distance from, each with the distance: the enemy's carriers found, where they were last
	// found, the computer's strike range (3550, or 4350 as Japan) and more for each frame since, as they may have come on
	// by half their speed, up to 3000; or, none found yet, the areas where the briefing expects them, if the victory
	// conditions count them, the range and the area's radius; and the places the briefing names, whose air bases strike
	// 1.5 times as far (ComputerPlayer.md, Air strikes). Whether each is known, found or a place, or only expected.
	private List<(WorldPosition Position, double Range, bool IsKnown)> Threats(Nspw game)
	{
		var range = ComputerStrikeRange(game);
		var threats = _contacts
			.All.Where(c =>
				c.Kind is UnitKind.Carrier or UnitKind.LightCarrier && game.Units[c.Unit].IsUsed
			)
			.Select(c =>
				(
					c.Position,
					range + Math.Min((game.Renderer.FrameCount - c.Frame) * 2 * 0.35 * 0.5, 3000),
					true
				)
			)
			.ToList();
		if (threats.Count == 0 && _knowledge.Counted.Contains(UnitKind.Carrier))
			threats.AddRange(
				_knowledge
					.Enemy.Where(a => !a.IsPlace)
					.Select(a => (a.Center, range + a.Radius, false))
			);

		threats.AddRange(
			_knowledge.Enemy.Where(a => a.IsPlace).Select(a => (a.Center, range * 1.5, true))
		);
		return threats;
	}

	// A carrier keeps just beyond the computer's strike range from what threatens it, with a margin: the computer launches
	// only within it, so a carrier out of it is struck by none, while its own planes reach farther. It closes in while it
	// is farther than its planes should fly from what is known, found or a place, not what is only expected, which may be
	// coming on; and moves away while it is within the range, across the sea, its formation with it.
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
				|| UnitFacts.IsAnchored(ref unit)
				|| threats.Count == 0
				|| game.Renderer.FrameCount
					- _positioned.GetValueOrDefault(carrier, -PositionFrames)
					< PositionFrames
			)
				continue;

			var position = unit.Position;
			var (threat, range, isKnown) = threats.MinBy(t =>
				UnitFacts.Distance(t.Position, position) - t.Range
			);
			var low = range + KeepAwayMargin;
			var distance = UnitFacts.Distance(position, threat);
			var aim = PointAt(threat, BearingTo(threat, position), low + (ApproachBand / 2));
			var destination = UnitFacts.FinalDestination(ref unit);
			var isMoving = UnitFacts.Distance(destination, position) > 400;
			if (
				(distance >= low && (distance <= low + ApproachBand || !isKnown))
				|| (isMoving && UnitFacts.Distance(destination, aim) < 800)
			)
				continue;

			_positioned[carrier] = game.Renderer.FrameCount;
			SelectAlone(carrier, "moving");
			return SailSelected(
					game,
					carrier,
					aim,
					$"moves {low + (ApproachBand / 2):F0} from ({threat.X:F0}, {threat.Y:F0}) to"
				) || true;
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
				|| UnitFacts.IsAnchored(ref seed)
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
					&& !UnitFacts.IsAnchored(ref ship)
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
			if (!IsOwn(game, m) || !IsWarship(ship.Kind) || UnitFacts.IsAnchored(ref ship))
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
				if (UnitFacts.Distance(UnitFacts.FinalDestination(ref ship), berth) < 120)
					continue;

				SelectAlone(m, "sailing to repair");
				return SailSelected(game, m, berth, "sails to repair at") || true;
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
				|| UnitFacts.IsAnchored(ref leader)
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

			var destination = UnitFacts.FinalDestination(ref leader);
			var hasTarget =
				target is null || leader.Target == target.Unit || !game.Units[target.Unit].IsFound;
			if (
				hasTarget
				&& UnitFacts.Distance(destination, position) > 100
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

			return SailSelected(
					game,
					m,
					point,
					target is null ? "sails toward" : $"engages {target.Kind} {target.Unit} at"
				) || true;
		}

		return false;
	}
}
