namespace OpenNspw.AI;

// The strong player's air operations: its carriers' and air bases' planes armed, launched at the enemy, kept at their
// targets in the air, and sent up to intercept and to patrol.
public sealed partial class StrongPlayer
{
	// Launches the selected parked planes, led by `leader`, toward an enemy unit found, to where they meet it, as
	// computed when the click comes. A parked plane cannot be given a target (CanClickUnit): TryDirect gives the planes
	// theirs once they fly.
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

	// Launches the selected parked planes, led by `leader`, at an enemy unit, by a waypoint 60% of the way there and
	// 1000 to one side, the other side each strike, so that the strike does not fly into the enemy's own coming the
	// other way; TryDirect flies it on from the waypoint to where the target is then.
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

	// The player's flying attackers and bombers that attack a unit, but those of the formation led by `leader`.
	private int AttackersOn(Nspw game, int target, int leader)
	{
		var count = 0;
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var plane = ref game.Units[n];
			if (
				IsOwn(game, n)
				&& UnitFacts.IsStrikePlane(plane.Kind)
				&& plane.PlaneState == UnitState.Flying
				&& plane.Target == target
				&& plane.Ammo > 0
				&& n != leader
				&& plane.GroupLeader != leader
			)
				count++;
		}

		return count;
	}

	// The best target found for a flying plane of the player's: for an attacker or a bomber, an enemy ship its weapon
	// can attack, within 2500, by its worth, the one it was sent at favored, the ones other strikes attack already
	// less; for a fighter, an enemy plane in the air, attackers and bombers first, within 1500.
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
					- (AttackersOn(game, c.Unit, plane) * 1.5)
				)
				.FirstOrDefault();
	}

	// A strike that has reached where it was sent and found nothing there sweeps on, 1500 at a time, turning 60° one
	// way, then the other, around the way the enemy most likely is, twice; then it waits there until its fuel sends it
	// home.
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

	// The middle of a contact and the enemy ships found lately within 1500 of it, where they would be after `ticks`
	// more ticks: where the carriers or the battleships of a formation sail, its escorts around them.
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

	// A strike whose target is alive but not found flies on to where it would be now, and takes no other target until
	// it has been there, if the victory conditions count the target: the escorts around a carrier are found first. Once
	// there, with the target still not found, the strike gives it up and takes what it finds.
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

	// A strike that has its target found within 2200, one the victory conditions count, splits up, as the computer's
	// attackers do (ComputerPlayer.md, The planes' targets): each plane is selected alone, given the target, and sent
	// to its own point 250 from it, the points fanned out across the side it comes from, so that the guns and the
	// target's turns meet the torpedoes from many sides at once. Within 400 of the target, each plane's own attack
	// takes over (SetAttackerAttackDestination).
	private bool TrySplitAttack(Nspw game)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var leader = ref game.Units[m];
			var target = leader.Target;
			if (
				!IsOwn(game, m)
				|| !UnitFacts.IsStrikePlane(leader.Kind)
				|| leader.PlaneState != UnitState.Flying
				|| leader.GroupLeader != 0
				|| leader.IsGroupLeader == 0
				|| target == 0
				|| !game.Units[target].IsUsed
				|| !game.Units[target].IsFound
				|| !IsCounted(game.Units[target].Kind)
				|| _split.Contains(m)
				|| UnitFacts.Distance(leader.Position, game.Units[target].Position) > 2200
			)
				continue;

			_split.Add(m);
			var planes = new List<int> { m };
			for (var n = 1; n <= game.MaxUnitId; n++)
			{
				if (
					IsOwn(game, n)
					&& game.Units[n].GroupLeader == m
					&& UnitFacts.IsStrikePlane(game.Units[n].Kind)
					&& game.Units[n].Ammo > 0
				)
					planes.Add(n);
			}

			if (planes.Count < 2)
				continue;

			var from = BearingTo(game.Units[target].Position, leader.Position);
			for (var i = 0; i < planes.Count; i++)
			{
				var plane = planes[i];
				var turn = planes.Count == 1 ? 0 : -75 + (150.0 * i / (planes.Count - 1));
				Deselect();
				_clicks.Enqueue(g =>
				{
					if (
						g.SelectedUnit != 0
						|| !IsOwn(g, plane)
						|| g.Units[plane].PlaneState != UnitState.Flying
					)
						return true;
					g.Commands.ClickUnit(plane);
					return true;
				});
				_clicks.Enqueue(g =>
				{
					if (
						g.SelectedUnit != plane
						|| !g.Units[target].IsUsed
						|| !g.Units[target].IsFound
					)
						return true;
					if (g.Units[plane].Target != target)
						g.Commands.ClickUnit(target);
					return true;
				});
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != plane || !g.Units[target].IsUsed)
						return true;
					g.Commands.SetDestination(PointAt(g.Units[target].Position, from + turn, 250));
					return true;
				});
			}

			Deselect();
			Log?.Invoke(
				$"{game.Renderer.FrameCount}: {planes.Count} planes of {leader.Kind} {m} split up against {game.Units[target].Kind} {target}"
			);
			return true;
		}

		return false;
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
				// The attack takes over within 400 of the target, or at the end of the path
				// (SetAttackerAttackDestination), which a plane circles wider than it checks: the path goes right at
				// the target.
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

	// How far a plane flies out to strike a place: 45% of its range on a full tank, to come back; a strategic bomber,
	// 62%, as its mission is to get there.
	private static double Reach(ref Unit plane) =>
		plane.MaxSpeed * plane.FuelInterval * 100 * (plane.Kind == UnitKind.Bomber ? 0.62 : 0.45);

	// A place the briefing names, within reach of a deck, where nothing has been found lately and no strike went
	// lately: a strike goes there to find what to attack, as a player strikes Pearl Harbor without seeing it first.
	private WorldPosition? PlaceToStrike(Nspw game, int deck, double reach)
	{
		var position = game.Units[deck].Position;
		var place = _knowledge.Enemy.FirstOrDefault(a =>
			a.IsPlace
			&& UnitFacts.Distance(position, a.Center) <= reach
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

	// A deck's ready attackers and bombers, with escorting fighters, against the best enemy found within reach, once
	// three of its armed planes are ready, or all it has, and none will be ready soon, and no enemy strike is over it;
	// or, while another deck's strike has just gone, with whatever is ready, at the same target or place, so that they
	// arrive together: a strike that takes off into the enemy's is broken up as it climbs, so it waits for the raid to
	// pass, and goes while the enemy's planes are coming home.
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
				var reach = ready.Count == 0 ? 0 : ready.Min(n => Reach(ref game.Units[n]));
				var pointWave =
					_pointWave is { } p
					&& game.Renderer.FrameCount - p.Frame < PointWaveFrames
					&& UnitFacts.Distance(game.Units[deck].Position, p.Point) <= reach
						? p.Point
						: (WorldPosition?)null;
				if (
					ready.Count == 0
					|| (
						wave is null
						&& pointWave is null
						&& (
							soon > 0
							|| (ready.Count < MinStrike && ready.Count < StrikePlanesOf(game, deck))
						)
					)
				)
					continue;

				var target = wave ?? ChooseTarget(game, deck, weapon);
				var origin = target is null
					? pointWave ?? EnemyStrikeOrigin(game) ?? PlaceToStrike(game, deck, reach)
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
					if (pointWave is null)
						_pointWave = (origin!.Value, game.Renderer.FrameCount);
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

	// When the victory conditions count enemy planes, a deck's ready fighters but one fly to the nearest area where the
	// briefing expects them, within their reach, every so often, and wait there, as they see only 500 around: TryDirect
	// gives them the planes they find.
	private bool TryHunt(Nspw game, List<int> decks)
	{
		if (
			!_knowledge.Counted.Any(k =>
				k is UnitKind.Bomber or UnitKind.Attacker or UnitKind.Fighter
			)
		)
			return false;

		foreach (var deck in decks)
		{
			if (
				!UnitFacts.CanLaunch(ref game.Units[deck])
				|| game.Renderer.FrameCount - _huntedFrom.GetValueOrDefault(deck, -HuntFrames)
					< HuntFrames
			)
				continue;

			var fighters = ParkedOn(
				game,
				deck,
				n => game.Units[n].Kind == UnitKind.Fighter && UnitFacts.IsReady(ref game.Units[n])
			);
			if (fighters.Count < 2)
				continue;

			var position = game.Units[deck].Position;
			var reach = Reach(ref game.Units[fighters[0]]);
			var area = _knowledge
				.Enemy.Where(a => !a.IsPlace && UnitFacts.Distance(position, a.Center) <= reach)
				.MinBy(a => UnitFacts.Distance(position, a.Center));
			if (area is null)
				continue;

			_huntedFrom[deck] = game.Renderer.FrameCount;
			var group = fighters.Take(fighters.Count - 1).ToList();
			ShowDeck(deck);
			SelectPlanes(group[0], group.Skip(1));
			LaunchToward(group[0], area.Center);
			return true;
		}

		return false;
	}

	// A carrier keeps a fighter in the air above it, toward where the enemy most likely is, while it has two ready: it
	// sees the enemy's strikes coming, and fights them, until its fuel brings it home (at 30) and another goes up.
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
}
