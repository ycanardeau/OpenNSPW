namespace OpenNspw.AI;

// The strong player's scouting: where the enemy most likely is, and the scouts that fly out to find it.
public sealed partial class StrongPlayer
{
	// The bearing from a deck along which the enemy most likely is: back along the way of the last enemy attacker or
	// bomber found, which came from its carrier or base; else toward the nearest area where the enemy is expected; else
	// toward the middle of the map. While it hunts carriers, toward the nearest area where the briefing expects them
	// first: a plane found may be on its way home, as the ones in the air when scenario 201 starts.
	private double LikelyBearing(Nspw game, WorldPosition from)
	{
		var fleets = _knowledge.Enemy.Where(a => !a.IsPlace).ToList();
		if (HuntsCarriers && fleets.Count > 0)
			return BearingTo(from, fleets.MinBy(a => UnitFacts.Distance(from, a.Center))!.Center);

		var plane = _contacts
			.All.Where(c =>
				c.Category == UnitCategory.Plane
				&& UnitFacts.IsStrikePlane(c.Kind)
				&& c.IsFlying
				&& UnitFacts.Now(game) - c.Frame < 2000
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

	// How far a scout flies out: 36% of its range on a full tank, as it flies out, across and back (2.5 times as far),
	// coming home before its tank is dry (FuelToHome).
	private double ScoutDistance(ref Unit plane) =>
		plane.MaxSpeed * plane.FuelInterval * 100 * Parameters.ScoutFraction;

	// The legs of the n-th search from a deck. While it hunts carriers, by a scout that flies out as far as `reach`: to
	// the likeliest place the map has (LikeliestPlace, with Parameters.ProbabilitySearch), and 700 each side across it;
	// or across where the carrier found last most likely is, half the way it was heading, wider the longer it has been lost,
	// or, lost for CarrierMemoryFrames and heading home, on its way there (WhereNow), 1500 each side; or, early in the
	// battle, across an area where the briefing expects them, each once, the nearest first, but the ones a strike goes to
	// (IsEarlyStrikeArea), as scenario 4's carrier starts in one of three, as far as a flight there, across and back lets
	// (20% farther than `reach`, which allows for a longer way across); or back and forth across the way to an area where
	// the briefing expects them, the areas in turn, nearest first, 25° each side, 2500 beyond the computer's strike range
	// (3550, or 4350 as Japan) or as far as the area: a picket that a fleet coming within strike range of the deck crosses
	// while the scout is out. Else, across an area where the enemy is expected and within ScoutReach, the areas in turn,
	// from one side of it to the other, wider each time around; or, with no such area, an arc around the way the enemy
	// most likely is, a new one each time, nearest that way first. The scout comes home sooner if its fuel runs low
	// (TryGuideScouts).
	private List<WorldPosition> SearchLegs(Nspw game, WorldPosition from, int n, double reach)
	{
		if (HuntsCarriers && LikeliestPlace(game, from, reach) is { } likeliest)
		{
			var side = BearingTo(from, likeliest) + 90;
			return [likeliest, PointAt(likeliest, side, 700), PointAt(likeliest, side + 180, 700)];
		}

		var lost = _contacts
			.All.Where(c =>
				c.Category == UnitCategory.Ship
				&& !UnitFacts.IsBase(c.Kind)
				&& IsCounted(c.Kind)
				&& game.Units[c.Unit].IsUsed
				&& IsRemembered(game, c)
			)
			.MaxBy(c => c.Frame);
		var homing =
			lost is not null && UnitFacts.Now(game) - lost.Frame >= Parameters.CarrierMemoryFrames;
		if (
			HuntsCarriers
			&& lost is not null
			&& UnitFacts.Distance(from, homing ? WhereNow(game, lost, 0) : lost.Position) <= reach
		)
		{
			var ticks = (UnitFacts.Now(game) - lost.Frame) * 2;
			var at = homing ? WhereNow(game, lost, 0) : lost.Predict(ticks / 2.0);
			var across = BearingTo(from, at) + 90;
			var half = homing ? 1500 : 700 + (ticks * 0.35);
			return [PointAt(at, across, half), PointAt(at, across + 180, half)];
		}

		if (HuntsCarriers)
		{
			var unswept = _knowledge
				.Enemy.Where(a =>
					!a.IsPlace
					&& UnitFacts.Distance(from, a.Center) + a.Radius <= reach * 1.2
					&& !_sweptAreas.Contains(a.Name)
					&& !IsEarlyStrikeArea(game, from, a)
				)
				.MinBy(a => UnitFacts.Distance(from, a.Center));
			if (unswept is not null && UnitFacts.Now(game) < 2500)
			{
				_sweptAreas.Add(unswept.Name);
				var across = BearingTo(from, unswept.Center) + 90;
				return
				[
					PointAt(unswept.Center, across, unswept.Radius),
					PointAt(unswept.Center, across + 180, unswept.Radius),
				];
			}

			var fleets = _knowledge
				.Enemy.Where(a => !a.IsPlace)
				.OrderBy(a => UnitFacts.Distance(from, a.Center))
				.ToList();
			var toward = fleets.Count == 0 ? (Area?)null : fleets[n % fleets.Count];
			var distance = toward is null ? reach : UnitFacts.Distance(from, toward.Center);
			var picket = Math.Clamp(
				distance,
				Math.Min(3000, reach),
				Math.Min(ComputerStrikeRange(game) + 2500, reach)
			);
			var likely = toward is null
				? LikelyBearing(game, from)
				: BearingTo(from, toward.Center);
			return
			[
				.. Enumerable
					.Range(0, 6)
					.Select(i => PointAt(from, likely + (i % 2 == 0 ? -25 : 25), picket)),
			];
		}

		var areas = _knowledge
			.Enemy.Where(a => UnitFacts.Distance(from, a.Center) <= ScoutReach)
			.ToList();
		if (
			areas.Count > 0
			&& _contacts.All.All(c =>
				c.Category != UnitCategory.Plane
				|| !c.IsFlying
				|| UnitFacts.Now(game) - c.Frame > 2000
			)
		)
		{
			var area = areas[n % areas.Count];
			var across = BearingTo(from, area.Center) + 90;
			var half = Math.Max(area.Radius * (0.5 + (0.5 * (n / areas.Count % 2))), 500);
			return [PointAt(area.Center, across, half), PointAt(area.Center, across + 180, half)];
		}

		var offset = n == 0 ? 0 : ((n + 1) / 2) * 60.0 * (n % 2 == 0 ? -1 : 1);
		var bearing = LikelyBearing(game, from) + offset;
		return [PointAt(from, bearing - 20, ScoutRange), PointAt(from, bearing + 20, ScoutRange)];
	}

	// Whether the player, hunting carriers, knows where an enemy carrier is: one found it remembers (IsRemembered).
	private bool KnowsCarrier(Nspw game) =>
		HuntsCarriers
		&& _contacts.All.Any(c =>
			c.Category == UnitCategory.Ship
			&& IsCounted(c.Kind)
			&& game.Units[c.Unit].IsUsed
			&& IsRemembered(game, c)
		);

	// The planes a deck keeps to scout, unarmed ones first, then attackers before bombers: two of a deck with six
	// attackers and bombers or more, one of a deck with two or more, none of the others, and three in all at most;
	// while hunting carriers, which takes every torpedo a deck can spare, two from eight, one from seven, and a scout
	// lost is replaced by another plane of its deck, once one is back on it; none while a carrier is known
	// (KnowsCarrier) and the player does not keep scouting (Parameters.KeepScouting), so that they strike.
	private List<int> ScoutsOf(Nspw game, int deck)
	{
		if (!Parameters.KeepScouting && KnowsCarrier(game))
			return [];

		if (!_scoutsOf.TryGetValue(deck, out var scouts))
		{
			var strikers = ParkedOn(game, deck, n => UnitFacts.IsStrikePlane(game.Units[n].Kind));
			var (two, one) = HuntsCarriers ? (8, 7) : (6, 2);
			var count = Math.Min(
				strikers.Count >= two ? 2
					: strikers.Count >= one ? 1
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
			_scoutCounts[deck] = scouts.Count;
		}

		scouts.RemoveAll(n => !IsOwn(game, n) || game.Units[n].Carrier != deck);
		if (HuntsCarriers && scouts.Count < _scoutCounts.GetValueOrDefault(deck))
		{
			var replacement = ParkedOn(
					game,
					deck,
					n => UnitFacts.IsStrikePlane(game.Units[n].Kind) && !scouts.Contains(n)
				)
				.OrderBy(n => game.Units[n].Weapon != FireKind.Unarmed)
				.FirstOrDefault();
			if (replacement != 0)
				scouts.Add(replacement);
		}

		return scouts;
	}

	// The fuel a flying plane needs to come home to its deck, and a margin: a unit of fuel lasts FuelInterval ticks, and
	// the deck may be sailing away from the plane, which it then closes on slower.
	private double FuelToHome(Nspw game, int plane)
	{
		ref var unit = ref game.Units[plane];
		ref var deck = ref game.Units[unit.Carrier];
		var away =
			deck.Speed
			* Math.Cos(
				(deck.Direction.Degrees - BearingTo(unit.Position, deck.Position)) * Math.PI / 180
			);
		var closing = Math.Max(unit.MaxSpeed - Math.Max(away, 0), 0.5);
		return (UnitFacts.Distance(unit.Position, deck.Position) / (closing * unit.FuelInterval))
			+ Parameters.HomeFuelMargin;
	}

	// Whether a plane of a deck's is coming home to it, within 2000, with fuel for little more than the way
	// (FuelToHome and Parameters.RecoverMargin): a deck takes no plane while a launch from it is under way
	// (TryLandOnCarrier), and one launch of a strike keeps the deck for a while, so the plane would circle it until its
	// fuel ran out.
	private bool IsRecovering(Nspw game, int deck)
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
				&& UnitFacts.Distance(plane.Position, game.Units[deck].Position) <= 2000
				&& plane.Fuel <= FuelToHome(game, m) + Parameters.RecoverMargin
			)
				return true;
		}

		return false;
	}

	// Whether the player launches from a deck now: the deck lets a plane take off (UnitFacts.CanLaunch), and, with
	// Parameters.RecoverFirst, none of its planes short of fuel is coming home to it (IsRecovering), for its strikes and
	// scouts. Interceptions and patrols do not wait (TryIntercept, TryPatrol): they cover the deck the planes land on,
	// and held, they left the carriers to the enemy's strikes, which hit them half as hard again.
	private bool MayLaunch(Nspw game, int deck) =>
		UnitFacts.CanLaunch(ref game.Units[deck])
		&& !(Parameters.RecoverFirst && IsRecovering(game, deck));

	// While no enemy ship has been found lately, the decks' scouts fly out, each on its search (SearchLegs), a new one
	// each flight; TryGuideScouts flies them on. While it hunts carriers: while no carrier has been found lately, one
	// every ScoutFrames while another is on its way out, so that one is always out; and one to relieve the ones keeping
	// a carrier in sight once they have fuel for little more than coming home, or when none is. There is nothing to
	// search for when the briefing names every place the enemy is, within the strikes' reach; nor, unless the player
	// keeps scouting (Parameters.KeepScouting), while it knows where a carrier is (KnowsCarrier).
	private bool TryScout(Nspw game, List<int> decks)
	{
		var relieves =
			HuntsCarriers
			&& _scoutLegs
				.Keys.Where(n =>
					IsOwn(game, n)
					&& game.Units[n].PlaneState == UnitState.Flying
					&& game.Units[n].Mode != UnitMode.Return
				)
				.All(n => game.Units[n].Fuel <= FuelToHome(game, n) + 25);
		if (
			_knowledge.HoldsOn
			|| (
				!relieves
				&& _contacts.All.Any(c =>
					c.Category == UnitCategory.Ship
					&& (!HuntsCarriers || IsCounted(c.Kind))
					&& UnitFacts.Now(game) - c.Frame < 300
				)
			)
			|| (!Parameters.KeepScouting && KnowsCarrier(game))
			|| (
				_knowledge.Enemy.Count > 0
				&& _knowledge.Enemy.All(a =>
					a.IsPlace
					&& decks.Any(d =>
						UnitFacts.Distance(game.Units[d].Position, a.Center) <= PlaceStrikeRange
					)
				)
			)
			|| (
				HuntsCarriers
				&& UnitFacts.Now(game) - _lastScout < Parameters.ScoutFrames
				&& _scoutLegs.Keys.Any(n => IsOwn(game, n) && game.Units[n].Mode != UnitMode.Return)
			)
		)
			return false;

		foreach (var deck in decks)
		{
			if (!MayLaunch(game, deck))
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
			var legs = SearchLegs(
				game,
				position,
				_searches++,
				ScoutDistance(ref game.Units[scout])
			);
			var first = legs[0];
			_scoutLegs[scout] = new Queue<WorldPosition>(legs.Skip(1));
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
				_scoutLaunches[scout] = UnitFacts.Now(g);
				_lastScout = UnitFacts.Now(g);
				Log?.Invoke(
					$"{UnitFacts.Now(g)}: {g.Units[scout].Kind} {scout} scouts toward ({first.X:F0}, {first.Y:F0})"
				);
				return true;
			});
			return true;
		}

		return false;
	}

	// The enemy ship a scout keeps in sight: the nearest one the victory conditions count, but bases, found lately
	// within 3000 of it.
	private Contact? ShadowTarget(Nspw game, WorldPosition from) =>
		_contacts
			.All.Where(c =>
				c.Category == UnitCategory.Ship
				&& !UnitFacts.IsBase(c.Kind)
				&& IsCounted(c.Kind)
				&& game.Units[c.Unit].IsUsed
				&& UnitFacts.Now(game) - c.Frame < 300
				&& UnitFacts.Distance(from, c.Position) <= 3000
			)
			.MinBy(c => UnitFacts.Distance(from, c.Position));

	// Where a scout keeps an enemy ship in sight: ShadowDistance from where it would be by now, as near the way to the
	// scout's deck as the guns of the warships found around it let, 750 from each, which most of their anti-aircraft
	// fire does not reach.
	private WorldPosition ShadowPoint(Nspw game, Contact target, WorldPosition home)
	{
		var at = target.Predict((UnitFacts.Now(game) - target.Frame) * 2);
		var guns = _contacts
			.All.Where(c =>
				c.Kind is UnitKind.Battleship or UnitKind.Cruiser
				&& UnitFacts.Now(game) - c.Frame < ContactFrames
				&& UnitFacts.Distance(c.Position, at) <= 2000
			)
			.Select(c => c.Predict((UnitFacts.Now(game) - c.Frame) * 2))
			.ToList();
		var toHome = BearingTo(at, home);
		return Enumerable
			.Range(0, 19)
			.Select(i =>
				PointAt(
					at,
					toHome + ((i % 2 == 0 ? 1 : -1) * ((i + 1) / 2) * 20),
					Parameters.ShadowDistance
				)
			)
			.MaxBy(p =>
				Math.Min(
					guns.Select(g => UnitFacts.Distance(g, p))
						.DefaultIfEmpty(double.MaxValue)
						.Min(),
					750
				)
			);
	}

	// Selects a flying plane of the player's alone, nothing being selected.
	private void SelectFlying(int plane, string what)
	{
		Deselect();
		_clicks.Enqueue(g =>
		{
			if (
				g.SelectedUnit != 0
				|| !IsOwn(g, plane)
				|| g.Units[plane].PlaneState != UnitState.Flying
			)
				return GiveUp(g, $"{what} {plane}: selected {g.SelectedUnit}");
			g.Commands.ClickUnit(plane);
			return true;
		});
	}

	// How far from an enemy warship found a plane on its way home keeps: beyond its anti-aircraft fire (GunCover), by 150.
	private static double GunClearance(UnitKind kind) =>
		kind switch
		{
			UnitKind.Battleship => 900,
			UnitKind.Cruiser => 800,
			UnitKind.Destroyer => 450,
			_ => 0,
		};

	// Where a plane on its way home turns, to come round the enemy's warships found in the last 600 frames, where they
	// would be by now, rather than over them, where their guns shoot it down: a point to one side of its way, beyond
	// each warship within its clearance (GunClearance) of the way, from which it flies straight home, on the side that
	// is the shorter way round with both legs clear. The warships the plane or its deck is already within the clearance of
	// are left out, as there is no way round them. Null if its way is clear, if neither side is, or if it has not the
	// fuel for the way round (FuelToHome, for its length).
	private WorldPosition? HomeWaypoint(Nspw game, int plane)
	{
		ref var unit = ref game.Units[plane];
		var from = unit.Position;
		var to = game.Units[unit.Carrier].Position;
		var length = UnitFacts.Distance(from, to);
		if (length < 1)
			return null;

		var now = UnitFacts.Now(game);
		var warships = _contacts
			.All.Where(c =>
				c.Category == UnitCategory.Ship && GunClearance(c.Kind) > 0 && now - c.Frame <= 600
			)
			.Select(c =>
				(Position: c.Predict((now - c.Frame) * 2), Clearance: GunClearance(c.Kind))
			)
			.Where(w =>
				UnitFacts.Distance(w.Position, from) >= w.Clearance
				&& UnitFacts.Distance(w.Position, to) >= w.Clearance
			)
			.ToList();
		var threats = warships
			.Where(w => DistanceToSegment(w.Position, from, to) < w.Clearance)
			.ToList();
		if (threats.Count == 0)
			return null;

		var (ux, uy) = ((to.X - from.X) / length, (to.Y - from.Y) / length);
		var along = threats.Average(w =>
			((w.Position.X - from.X) * ux) + ((w.Position.Y - from.Y) * uy)
		);
		var foot = new WorldPosition(from.X + (ux * along), from.Y + (uy * along));
		WorldPosition? best = null;
		var bestLength = double.PositiveInfinity;
		foreach (var side in (ReadOnlySpan<int>)[1, -1])
		{
			var (nx, ny) = (-uy * side, ux * side);
			var offset = threats.Max(w =>
				((w.Position.X - foot.X) * nx) + ((w.Position.Y - foot.Y) * ny) + w.Clearance
			);
			for (var attempt = 0; attempt < 3; attempt++, offset *= 1.3)
			{
				var point = new WorldPosition(foot.X + (nx * offset), foot.Y + (ny * offset));
				if (
					warships.Any(w =>
						DistanceToSegment(w.Position, from, point) < w.Clearance
						|| DistanceToSegment(w.Position, point, to) < w.Clearance
					)
				)
					continue;

				var way = UnitFacts.Distance(from, point) + UnitFacts.Distance(point, to);
				if (way < bestLength)
					(best, bestLength) = (point, way);
				break;
			}
		}

		return best is not null && unit.Fuel > FuelToHome(game, plane) * bestLength / length
			? best
			: null;
	}

	// Sends the selected flying plane home: toward its deck first, as Return turns a plane home only at the end of its
	// path, or, with Parameters.ReturnAround, round the enemy's warships on the way (HomeWaypoint); then Return, which
	// drops its weapon.
	private void ReturnSelected(int plane, string what)
	{
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != plane)
				return GiveUp(g, $"{what} {plane}: selected {g.SelectedUnit}");
			var round = Parameters.ReturnAround ? HomeWaypoint(g, plane) : null;
			g.Commands.SetDestination(round ?? g.Units[g.Units[plane].Carrier].Position);
			if (round is { } point)
				Log?.Invoke(
					$"{UnitFacts.Now(g)}: {g.Units[plane].Kind} {plane} comes home round the enemy by ({point.X:F0}, {point.Y:F0})"
				);
			return true;
		});
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != plane)
				return GiveUp(g, $"{what} {plane}: selected {g.SelectedUnit}");
			g.Commands.ChooseCombatMenuItem(CombatMenuItem.Return);
			Log?.Invoke(
				$"{UnitFacts.Now(g)}: {g.Units[plane].Kind} {plane} comes home, {g.Units[plane].Fuel:F0} fuel left"
			);
			return true;
		});
		_clicks.Wait(8);
	}

	// A scout that has found an enemy carrier, while it hunts carriers, keeps it in sight, from the side of its deck, so
	// that the strikes know where it is, until its fuel brings it home; any other scout that has reached the end of its
	// leg flies the next, or comes home (Return) after the last, or sooner if its fuel runs low.
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
					&& UnitFacts.Now(game) - _scoutLaunches.GetValueOrDefault(scout) > 300
				)
			)
			{
				_scoutLegs.Remove(scout);
				return false;
			}

			if (unit.PlaneState != UnitState.Flying || unit.Mode == UnitMode.Return)
				continue;

			var pathEnd = new WorldPosition(unit.PathX[0], unit.PathY[0]);
			if (
				HuntsCarriers
				&& ShadowTarget(game, unit.Position) is { } target
				&& unit.Fuel > FuelToHome(game, scout)
			)
			{
				var point = ShadowPoint(game, target, game.Units[unit.Carrier].Position);
				if (UnitFacts.Distance(pathEnd, point) <= 250)
					continue;

				legs.Clear();
				SelectFlying(scout, "shadowing");
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != scout)
						return GiveUp(g, $"shadowing {scout}: selected {g.SelectedUnit}");
					g.Commands.SetDestination(point);
					return true;
				});
				Deselect();
				return true;
			}

			if (
				UnitFacts.Distance(unit.Position, pathEnd) > 200
				&& unit.Fuel > FuelToHome(game, scout)
			)
				continue;

			SelectFlying(scout, "guiding scout");
			if (unit.Fuel > FuelToHome(game, scout) && legs.TryDequeue(out var next))
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
				ReturnSelected(scout, "scout");
			}

			Deselect();
			return true;
		}

		return false;
	}
}
