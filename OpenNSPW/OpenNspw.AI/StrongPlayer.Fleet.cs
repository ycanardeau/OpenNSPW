namespace OpenNspw.AI;

// The strong player's ships: its carriers kept out of the computer's strike range, and its warships fighting in groups
// and repairing.
public sealed partial class StrongPlayer
{
	// A carrier's formation as it forms: its members, taking their places around it, since a frame, last ordered to them
	// in another, and its warships in the order of their places (WarshipOrder).
	private sealed record Ring(List<int> Members, int Since, int Ordered, List<int> Warships);

	// Where the warships of a carrier's formation keep, by their order: Formation.Radius around it, evenly, the first ahead
	// (Ring), or across 160° of the way the enemy most likely is (Screen). The game keeps each from the carrier's heading
	// as it rounds it, to 45° (SetShipFormation), so the formation turns with it.
	private WorldPosition WarshipPlace(Nspw game, int carrier, int index, int count)
	{
		ref var leader = ref game.Units[carrier];
		var heading = Math.Round(leader.Direction.Degrees / 45) * 45;
		var bearing =
			Formation.Kind == FormationKind.Screen
				? LikelyBearing(game, leader.Position)
					+ (count == 1 ? 0 : -80 + (index * 160.0 / (count - 1)))
				: heading + (index * 360.0 / count);
		return PointAt(leader.Position, bearing, Formation.Radius);
	}

	// The order of a formation's warships, which gives each its place (WarshipPlace): battleships first, then cruisers and
	// destroyers, by number; or, with RingAssignment, the order that sails them the least in all to their places (the
	// assignment problem, by dynamic programming over the places taken), so that none crosses the formation, nor runs
	// into the others on its way: the ways of an assignment with the least sum of distances never cross.
	private List<int> WarshipOrder(Nspw game, int carrier, List<int> members)
	{
		var warships = members
			.Where(m => !UnitFacts.IsDeck(game.Units[m].Kind))
			.OrderBy(m => game.Units[m].Kind)
			.ThenBy(m => m)
			.ToList();
		var count = warships.Count;
		if (!Parameters.RingAssignment || count < 2 || count > 16)
			return warships;

		var places = Enumerable
			.Range(0, count)
			.Select(i => WarshipPlace(game, carrier, i, count))
			.ToList();
		// least[taken]: the least sailing of the first popcount(taken) warships to the places in taken; from[taken], the
		// place the last of them takes.
		var least = new double[1 << count];
		var from = new int[1 << count];
		Array.Fill(least, double.PositiveInfinity);
		least[0] = 0;
		for (var taken = 0; taken < 1 << count; taken++)
		{
			if (double.IsPositiveInfinity(least[taken]))
				continue;
			var ship = System.Numerics.BitOperations.PopCount((uint)taken);
			if (ship == count)
				continue;
			for (var place = 0; place < count; place++)
			{
				if ((taken & (1 << place)) != 0)
					continue;
				var next = taken | (1 << place);
				var cost =
					least[taken]
					+ UnitFacts.Distance(game.Units[warships[ship]].Position, places[place]);
				if (cost < least[next])
				{
					least[next] = cost;
					from[next] = place;
				}
			}
		}

		var order = new int[count];
		for (int all = (1 << count) - 1, ship = count - 1; ship >= 0; ship--)
		{
			order[from[all]] = warships[ship];
			all &= ~(1 << from[all]);
		}

		return [.. order];
	}

	// Where a member of a carrier's formation keeps: the other carriers 250 behind it, then to each side; the warships at
	// their places (WarshipPlace), in the formation's order.
	private WorldPosition RingPlace(Nspw game, int carrier, Ring ring, int member)
	{
		ref var leader = ref game.Units[carrier];
		var heading = Math.Round(leader.Direction.Degrees / 45) * 45;
		var place = ring
			.Members.Where(m => UnitFacts.IsDeck(game.Units[m].Kind))
			.ToList()
			.IndexOf(member);
		if (place >= 0)
			return PointAt(
				leader.Position,
				heading
					+ (
						place switch
						{
							0 => 180,
							1 => 90,
							_ => 270,
						}
					),
				RingCarrierDistance
			);

		return WarshipPlace(game, carrier, ring.Warships.IndexOf(member), ring.Warships.Count);
	}

	private bool IsRinging(int m) => _rings.Values.Any(r => r.Members.Contains(m));

	// Selects a carrier and the ships of its formation, and orders it on, 80 to the east, so that they keep where they
	// are from it (SetShipFormation).
	private void Group(int carrier, List<int> members, WorldPosition position)
	{
		Deselect();
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != 0 || !IsOwn(g, carrier))
				return GiveUp(g, $"forming {carrier}: selected {g.SelectedUnit}");
			g.Commands.ClickUnit(carrier);
			return true;
		});
		foreach (var member in members)
		{
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != carrier)
					return GiveUp(g, $"forming {carrier}: selected {g.SelectedUnit}");
				if (IsOwn(g, member) && g.Selections[1][member] == 0)
					g.Commands.ClickUnit(member);
				return true;
			});
		}

		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != carrier)
				return GiveUp(g, $"forming {carrier}: selected {g.SelectedUnit}");
			g.Commands.SetDestination(new WorldPosition(position.X + 80, position.Y));
			Log?.Invoke(
				$"{UnitFacts.Now(g)}: {g.Units[carrier].Kind} {carrier} forms with {g.SelectionCount} ships"
			);
			return true;
		});
		Deselect();
	}

	// Each carrier of the player's takes the warships and the other carriers near it into its formation, once, so that
	// their guns cover it and they move as one: a formation keeps the shape its ships are in when they are grouped
	// (SetShipFormation), so they first sail to their places around it (RingPlace), each ordered again only once its
	// place has moved from where it sails, as the carrier moves or turns, at most every RingOrderFrames, and are grouped
	// once all are there, or after RingFrames: orders to all of them every time would keep the clicks busy, and the
	// player from scouting and striking, until they are grouped. A ring covers the carrier from every side
	// with its escorts' guns, which a line does from only one.
	private bool TryForm(Nspw game, List<int> decks)
	{
		foreach (var (carrier, ring) in _rings.ToList())
		{
			ring.Members.RemoveAll(m => !IsOwn(game, m));
			ring.Warships.RemoveAll(m => !IsOwn(game, m));
			if (!IsOwn(game, carrier) || ring.Members.Count == 0)
			{
				_rings.Remove(carrier);
				continue;
			}

			var away = ring
				.Members.Where(m =>
					UnitFacts.Distance(game.Units[m].Position, RingPlace(game, carrier, ring, m))
					> RingTolerance
				)
				.ToList();
			if (away.Count == 0 || UnitFacts.Now(game) - ring.Since >= RingFrames)
			{
				_rings.Remove(carrier);
				Group(carrier, ring.Members, game.Units[carrier].Position);
				return true;
			}

			var strays = away.Where(m =>
					UnitFacts.Distance(
						UnitFacts.FinalDestination(ref game.Units[m]),
						RingPlace(game, carrier, ring, m)
					) > RingTolerance
				)
				.ToList();
			if (strays.Count == 0 || UnitFacts.Now(game) - ring.Ordered < RingOrderFrames)
				continue;

			_rings[carrier] = ring with { Ordered = UnitFacts.Now(game) };
			foreach (var member in strays)
			{
				SelectAlone(member, "ringing");
				SendSelected(
					member,
					RingPlace(game, carrier, ring, member),
					$"takes its place around {game.Units[carrier].Kind} {carrier} at"
				);
			}

			return true;
		}

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
			if (
				Formation.Kind == FormationKind.Line
				|| (
					Formation.Kind == FormationKind.Auto
					&& escorts.Any(m => game.Units[m].Kind == UnitKind.Battleship)
				)
			)
				Group(carrier, escorts, unit.Position);
			else
				_rings[carrier] = new Ring(
					escorts,
					UnitFacts.Now(game),
					-RingOrderFrames,
					WarshipOrder(game, carrier, escorts)
				);
			return true;
		}

		return false;
	}

	// How far the computer's carriers strike from (3550, or 4350 as Japan: ComputerPlayer.md, Air strikes).
	private static double ComputerStrikeRange(Nspw game) =>
		game.CpuSide == Side.Japan ? 4350 : 3550;

	// What a carrier keeps its distance from, each with the distance: the enemy's carriers found, where they were last
	// found, or, heading home (HomeOf) and lost from sight for CarrierMemoryFrames, where they would be by now, which the
	// carrier follows to keep them within its planes' reach, the computer's strike range (3550, or 4350 as Japan) and, but
	// those, more for each frame since, as they may have come on by half their speed, up to 3000, or, while hunting
	// carriers, at their full speed (0.7 a tick), up to 1000: farther, the carrier would sail out of its own planes' reach
	// before the scouts find them again; or, none found yet, the areas where the briefing expects them, if the victory
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
				HomeOf(c) is not null
				&& UnitFacts.Now(game) - c.Frame >= Parameters.CarrierMemoryFrames
					? (WhereNow(game, c, 0), range, true)
					: (
						c.Position,
						range
							+ (
								HuntsCarriers
									? Math.Min((UnitFacts.Now(game) - c.Frame) * 2 * 0.7, 1000)
									: Math.Min(
										(UnitFacts.Now(game) - c.Frame) * 2 * 0.35 * 0.5,
										3000
									)
							),
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

	// Whether planes of a deck's are in its landing pattern, within 1500 (DeckPhase 1, SetLandingDestination): they come
	// in from astern along its heading, to 45°, and land only so (TryLandOnCarrier), so a deck that turns sends them
	// round again, on what fuel they have left.
	private static bool IsLandingOn(Nspw game, int deck)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var plane = ref game.Units[m];
			if (
				IsOwn(game, m)
				&& plane.Category == UnitCategory.Plane
				&& plane.PlaneState == UnitState.Flying
				&& plane.Carrier == deck
				&& plane.Mode == UnitMode.Return
				&& plane.DeckPhase == 1
				&& UnitFacts.Distance(plane.Position, game.Units[deck].Position) <= 1500
			)
				return true;
		}

		return false;
	}

	// A carrier keeps just beyond the computer's strike range from what threatens it, with a margin: the computer
	// launches only within it, so a carrier out of it is struck by none, while its own planes reach farther. It closes
	// in while it is farther than its planes should fly from what is known, found or a place, not what is only
	// expected, which may be coming on; and moves away while it is within the range, across the sea, its formation with
	// it. With Parameters.HoldCourse, a carrier whose planes are landing (IsLandingOn) holds its course, unless it is
	// within the range.
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
				|| UnitFacts.Now(game) - _positioned.GetValueOrDefault(carrier, -PositionFrames)
					< PositionFrames
			)
				continue;

			var position = unit.Position;
			var (threat, range, isKnown) = threats.MinBy(t =>
				UnitFacts.Distance(t.Position, position) - t.Range
			);
			var low = range + Parameters.KeepAwayMargin;
			var distance = UnitFacts.Distance(position, threat);
			var aim = PointAt(
				threat,
				BearingTo(threat, position),
				low + (Parameters.ApproachBand / 2)
			);
			var destination = UnitFacts.FinalDestination(ref unit);
			var isMoving = UnitFacts.Distance(destination, position) > 400;
			if (
				(distance >= low && (distance <= low + Parameters.ApproachBand || !isKnown))
				|| (isMoving && UnitFacts.Distance(destination, aim) < 800)
			)
				continue;

			if (Parameters.HoldCourse && distance >= range && IsLandingOn(game, carrier))
				continue;

			_positioned[carrier] = UnitFacts.Now(game);
			SelectAlone(carrier, "moving");
			return SailSelected(
					game,
					carrier,
					aim,
					$"moves {low + (Parameters.ApproachBand / 2):F0} from ({threat.X:F0}, {threat.Y:F0}) to"
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

	// A warship that has lost most of its HP sails to the nearest naval base of the player's, and supplies there once
	// it has stopped, which the combat menu offers only within 280 of the base (ListCombatMenu); it fights again once
	// repaired.
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
				|| UnitFacts.Now(game) - _repairOrdered.GetValueOrDefault(m, -RepairFrames)
					< RepairFrames
			)
				continue;

			_repairing.Add(m);
			_repairOrdered[m] = UnitFacts.Now(game);
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
					$"{UnitFacts.Now(g)}: {g.Units[ship1].Kind} {ship1} supplies, {g.Units[ship1].HP}/{g.Units[ship1].MaxHP}"
				);
				return true;
			});
			_clicks.Wait(8);
			Deselect();
			return true;
		}

		return false;
	}

	// Whether a ship of the player's has fought at close quarters lately (TryCloseQuarters), which the other orders leave
	// it to.
	private bool IsAtCloseQuarters(Nspw game, int m) =>
		UnitFacts.Now(game) - _closeAt.GetValueOrDefault(m, int.MinValue / 2) < 300;

	// Each destroyer of the player's within 1200 of an enemy cruiser or battleship found, while
	// Parameters.CloseQuarters says so, closes on it: to 135 from where it will be (165 from a battleship), a step of
	// 30° further round it each time, every CloseQuartersFrames, so that it keeps moving, as a ship that stands turns
	// away from the enemy's within 900 (EvadeShip). A ship's guns fire at nothing nearer than 30% of their range
	// (ShipFireGunWithoutTarget), 150 for a cruiser's (500) and 180 for a battleship's (600), or 150 for any at its
	// target (ShipFireGunAtTarget), but 120 for a destroyer's own (400): there it hits them and they cannot hit it back,
	// while its torpedoes, fired by themselves from 100 to 600 at a ship on their beam, go in on the way.
	private bool TryCloseQuarters(Nspw game)
	{
		if (!Parameters.CloseQuarters)
			return false;

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var destroyer = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| destroyer.Kind != UnitKind.Destroyer
				|| destroyer.IsSupplying
				|| UnitFacts.IsAnchored(ref destroyer)
				|| UnitFacts.Now(game) - _closeAt.GetValueOrDefault(m, int.MinValue / 2)
					< CloseQuartersFrames
			)
				continue;

			var position = destroyer.Position;
			var target = _contacts
				.All.Where(c =>
					c.Kind is UnitKind.Cruiser or UnitKind.Battleship
					&& game.Units[c.Unit].IsUsed
					&& game.Units[c.Unit].IsFound
					&& UnitFacts.Distance(position, c.Position) <= 1200
				)
				.MinBy(c => UnitFacts.Distance(position, c.Position));
			if (target is null)
				continue;

			_closeAt[m] = UnitFacts.Now(game);
			var at = target.Predict(CloseQuartersFrames * 2);
			var point = PointAt(
				at,
				BearingTo(at, position) + 30,
				target.Kind == UnitKind.Battleship ? 165 : 135
			);
			SelectAlone(m, "closing");
			SendSelected(m, point, $"closes on {target.Kind} {target.Unit} at");
			return true;
		}

		return false;
	}

	// While hunting carriers, once the strikes have dropped two weapons or more on a carrier found lately (CountDrops),
	// the cruisers and destroyers screening the player's carriers leave them, one at a time, and go at it, as TryEngage
	// then sends each, its worth first: a crippled carrier is slow to get away from them, while the player's carriers
	// keep out of the computer's strike range, which the warships need not. They go only as many as the enemy's
	// warships found lately within 4000 of the carrier, or more, and while no enemy attacker or bomber has been found
	// in the air for 3000 frames; the battleships, whose anti-aircraft guns reach farthest, stay: a computer that still
	// strikes would find the carriers without their guns.
	private bool TryDetach(Nspw game)
	{
		if (
			!HuntsCarriers
			|| _contacts.All.Any(c =>
				c.Category == UnitCategory.Plane
				&& UnitFacts.IsStrikePlane(c.Kind)
				&& c.IsFlying
				&& UnitFacts.Now(game) - c.Frame < 3000
			)
		)
			return false;

		var crippled = _contacts
			.All.Where(c =>
				c.Category == UnitCategory.Ship
				&& IsCounted(c.Kind)
				&& game.Units[c.Unit].IsUsed
				&& _dropsOn.GetValueOrDefault(c.Unit) >= 2
				&& IsRemembered(game, c)
			)
			.MaxBy(c => _dropsOn.GetValueOrDefault(c.Unit));
		if (crippled is null)
			return false;

		var screening = new List<int>();
		var detached = 0;
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var ship = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| ship.Kind is not (UnitKind.Cruiser or UnitKind.Destroyer)
				|| ship.IsSupplying
				|| UnitFacts.IsAnchored(ref ship)
				|| IsAtCloseQuarters(game, m)
			)
				continue;

			if (ship.GroupLeader != 0 && UnitFacts.IsDeck(game.Units[ship.GroupLeader].Kind))
				screening.Add(m);
			else
				detached++;
		}

		var guns = _contacts.All.Count(c =>
			IsWarship(c.Kind)
			&& game.Units[c.Unit].IsUsed
			&& UnitFacts.Now(game) - c.Frame < Parameters.CarrierMemoryFrames
			&& UnitFacts.Distance(c.Position, crippled.Position) <= 4000
		);
		if (screening.Count == 0 || screening.Count + detached < guns)
			return false;

		var warship = screening[0];
		SelectAlone(warship, "detaching");
		return SailSelected(
				game,
				warship,
				WhereNow(game, crippled, 0),
				$"leaves its carrier for {crippled.Kind} {crippled.Unit} at"
			) || true;
	}

	// The best enemy ship for a surface group to fight: found lately, by its worth and nearness, the ones the victory
	// conditions count first; or null.
	private Contact? ChooseSurfaceTarget(Nspw game, WorldPosition from) =>
		_contacts
			.All.Where(c =>
				c.Category == UnitCategory.Ship
				&& UnitFacts.Now(game) - c.Frame < Parameters.CarrierMemoryFrames
			)
			.OrderByDescending(c =>
				Value(c.Kind)
				- (UnitFacts.Distance(from, c.Position) / 1500)
				- ((UnitFacts.Now(game) - c.Frame) / 300.0)
			)
			.FirstOrDefault();

	// Each submarine of the player's, while it hunts carriers and Parameters.UseSubmarines says so, sails every
	// SubmarineFrames at the nearest carrier it remembers (IsRemembered), where it would be as the submarine gets there,
	// 300 to the side of its way that the submarine is on; or, knowing of none, at the nearest area where the briefing
	// expects the enemy's fleet. A submarine dives as it finds any unit, and fires its torpedoes by itself at a ship found
	// within 100 to 600 (500 as the United States) that it has on its beam (ShipFireTorpedoWithoutTarget), so it needs
	// only to be across the carrier's way: as in 201, where I-168 finished Yorktown.
	private bool TrySubmarines(Nspw game)
	{
		if (!HuntsCarriers || !Parameters.UseSubmarines)
			return false;

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var submarine = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| submarine.Kind != UnitKind.Submarine
				|| submarine.IsSupplying
				|| submarine.GroupLeader != 0
				|| UnitFacts.Now(game) - _submarineOrders.GetValueOrDefault(m, int.MinValue / 2)
					< SubmarineFrames
			)
				continue;

			var position = submarine.Position;
			var speed = Math.Max(submarine.MaxSpeed, 0.2);
			var carrier = _contacts
				.All.Where(c =>
					c.Category == UnitCategory.Ship
					&& IsCounted(c.Kind)
					&& game.Units[c.Unit].IsUsed
					&& IsRemembered(game, c)
				)
				.MinBy(c => UnitFacts.Distance(position, WhereNow(game, c, 0)));
			WorldPosition aim;
			if (carrier is not null)
			{
				var at = WhereNow(
					game,
					carrier,
					UnitFacts.Distance(position, WhereNow(game, carrier, 0)) / speed
				);
				var side = new[] { 90.0, -90.0 }
					.Select(turn => PointAt(at, carrier.Direction + turn, 300))
					.MinBy(p => UnitFacts.Distance(p, position));
				aim = side;
			}
			else if (
				_knowledge
					.Enemy.Where(a => !a.IsPlace)
					.MinBy(a => UnitFacts.Distance(position, a.Center)) is
				{ } area
			)
			{
				aim = area.Center;
			}
			else
			{
				continue;
			}

			_submarineOrders[m] = UnitFacts.Now(game);
			if (UnitFacts.Distance(UnitFacts.FinalDestination(ref submarine), aim) < 200)
				continue;

			SelectAlone(m, "hunting");
			return SailSelected(
					game,
					m,
					aim,
					carrier is null
						? "sails toward the enemy's fleet at"
						: $"sails across {carrier.Kind} {carrier.Unit}'s way at"
				) || true;
		}

		return false;
	}

	// Each surface group of the player's, led by a warship, goes at the best enemy ship it knows of, all its ships
	// firing at it once it is found (ApplyOrders gives the formation the leader's target), and keeps at it while it is
	// found, unless one the victory conditions count is found; or, knowing of none, toward where the briefing expects
	// the enemy.
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
				|| IsRinging(m)
				|| IsAtCloseQuarters(game, m)
				|| UnitFacts.Now(game) - _engagedAt.GetValueOrDefault(m, -EngageFrames)
					< EngageFrames
			)
				continue;

			_engagedAt[m] = UnitFacts.Now(game);
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
					) + ((UnitFacts.Now(game) - target.Frame) * 2);
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

	// How far a warship's anti-aircraft shells reach (ShipFireAntiAircraftShellAtTarget): a battleship's 700, a
	// cruiser's 600, from a quarter of that. Others fire none.
	private static double AntiAircraftRange(UnitKind kind) =>
		kind switch
		{
			UnitKind.Battleship => 700,
			UnitKind.Cruiser => 600,
			_ => 0,
		};

	// The enemy attacker or bomber, its weapon still aboard, found and flying, within a warship's anti-aircraft reach, that
	// is the nearest one of the player's carriers: the one about to drop on it. Null if none is.
	private static int? AttackerToTarget(Nspw game, int ship, List<int> decks)
	{
		ref var unit = ref game.Units[ship];
		var range = AntiAircraftRange(unit.Kind);
		int? best = null;
		var bestDistance = double.PositiveInfinity;
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var plane = ref game.Units[n];
			if (
				!plane.IsUsed
				|| plane.Side == game.LocalSide
				|| !UnitFacts.IsStrikePlane(plane.Kind)
				|| plane.Ammo == 0
				|| plane.PlaneState != UnitState.Flying
				|| !plane.IsFound
			)
				continue;

			var distance = UnitFacts.Distance(plane.Position, unit.Position);
			if (distance < range * 0.25 || distance > range)
				continue;

			var at = plane.Position;
			var fromDeck =
				decks.Count == 0
					? distance
					: decks.Min(d => UnitFacts.Distance(at, game.Units[d].Position));
			if (fromDeck < bestDistance)
				(best, bestDistance) = (n, fromDeck);
		}

		return best;
	}

	// With Parameters.TargetAttackers, each battleship and cruiser of the player's takes as its target the enemy attacker
	// within its anti-aircraft reach that is nearest the player's carriers (AttackerToTarget), unless its target already
	// is such a plane, and at most every 60 frames: a battleship with a plane as its target fires an aimed salvo at it as
	// well as its others (ControlBattleshipFiring), half as many shells again; a cruiser aims at it, and with a ship as its
	// target, or a plane out of its reach, fires none (ControlCruiserFiring).
	private bool TryTargetAttackers(Nspw game)
	{
		if (!Parameters.TargetAttackers)
			return false;

		var decks = Decks(game);
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var ship = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| ship.Category != UnitCategory.Ship
				|| AntiAircraftRange(ship.Kind) == 0
				|| UnitFacts.Now(game) - _antiAircraftOrders.GetValueOrDefault(m, int.MinValue / 2)
					< 60
			)
				continue;

			if (AttackerToTarget(game, m, decks) is not { } attacker)
				continue;

			ref var current = ref game.Units[ship.Target];
			var range = AntiAircraftRange(ship.Kind);
			if (
				ship.Target != 0
				&& current.IsUsed
				&& current.Category == UnitCategory.Plane
				&& current.Side != game.LocalSide
				&& current.Ammo != 0
				&& UnitFacts.Distance(current.Position, ship.Position) is var d
				&& d >= range * 0.25
				&& d <= range
			)
				continue;

			_antiAircraftOrders[m] = UnitFacts.Now(game);
			var warship = m;
			SelectAlone(m, "aiming at attackers");
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != warship)
					return GiveUp(g, $"aiming {warship}: selected {g.SelectedUnit}");
				if (g.Units[attacker].IsUsed && g.Units[attacker].IsFound)
				{
					g.Commands.ClickUnit(attacker);
					Log?.Invoke(
						$"{UnitFacts.Now(g)}: {g.Units[warship].Kind} {warship} aims at {g.Units[attacker].Kind} {attacker}: target {g.Units[warship].Target}"
					);
				}

				return true;
			});
			Deselect();
			return true;
		}

		return false;
	}
}
