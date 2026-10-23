namespace OpenNspw.AI;

// The strong player's scouting: where the enemy most likely is, and the scouts that fly out to find it.
public sealed partial class StrongPlayer
{
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

	// The planes a deck keeps to scout, unarmed ones first, then attackers before bombers: two of a deck with six
	// attackers and bombers or more, one of a deck with two or more, none of the others, and three in all at most.
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

	// While no enemy ship has been found lately, the decks' scouts fly out, each sweeping an arc where the enemy most
	// likely is, a new arc each flight, nearest the likely bearing first; TryGuideScouts flies them on. There is
	// nothing to search for when the briefing names every place the enemy is, within the strikes' reach.
	private bool TryScout(Nspw game, List<int> decks)
	{
		if (
			_contacts.All.Any(c =>
				c.Category == UnitCategory.Ship && game.Renderer.FrameCount - c.Frame < 300
			)
			|| (
				_knowledge.Enemy.Count > 0
				&& _knowledge.Enemy.All(a =>
					a.IsPlace
					&& decks.Any(d =>
						UnitFacts.Distance(game.Units[d].Position, a.Center) <= PlaceStrikeRange
					)
				)
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
}
