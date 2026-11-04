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
			_engaged[target] = UnitFacts.Now(g);
			Log?.Invoke(
				$"{UnitFacts.Now(g)}: {g.Units[leader].Kind} {leader} and {g.SelectionCount} more launched toward {g.Units[target].Kind} {target}"
			);
			return true;
		});
	}

	// How far a plane flies out to strike a place: 45% of its range on a full tank, to come back; a strategic bomber,
	// 62%, as its mission is to get there.
	private static double Reach(ref Unit plane) =>
		plane.MaxSpeed * plane.FuelInterval * 100 * (plane.Kind == UnitKind.Bomber ? 0.62 : 0.45);

	// A strike's waypoint to one side of its way to a point, or, with Parameters.StraightWhenFar, the point itself if the
	// way round is longer than the planes' reach, which leaves them the fuel to find the target, attack it and come home:
	// at the edge of their range (201, where the American carrier is 8000 away), a strike that flies round turns home
	// short of it.
	private WorldPosition Waypoint(
		WorldPosition from,
		WorldPosition point,
		double side,
		double offset,
		double reach
	)
	{
		var bearing = BearingTo(from, point);
		var waypoint = PointAt(
			PointAt(from, bearing, UnitFacts.Distance(from, point) * 0.6),
			bearing + side,
			offset
		);
		return
			Parameters.StraightWhenFar
			&& UnitFacts.Distance(from, waypoint) + UnitFacts.Distance(waypoint, point) > reach
			? point
			: waypoint;
	}

	// Launches the selected parked planes, led by `leader`, at an enemy unit, by a waypoint 60% of the way there and
	// to one side, so that the strike does not fly into the enemy's own, whose fighters fire at what flies before them:
	// while hunting carriers, 2000 to the other side from the enemy's attackers and bombers found in the air lately; or
	// 1000 to one side, the other side each strike, unless the way round is too long (Waypoint). TryDirect flies it on
	// from the waypoint to where the target is then.
	private void LaunchStrike(Nspw game, int leader, Contact target, WorldPosition from)
	{
		var raid = _contacts
			.All.Where(c =>
				HuntsCarriers
				&& c.Category == UnitCategory.Plane
				&& UnitFacts.IsStrikePlane(c.Kind)
				&& c.IsFlying
				&& UnitFacts.Now(game) - c.Frame < 100
			)
			.MinBy(c => UnitFacts.Distance(c.Position, from));
		var (side, offset) = raid is null
			? (_strikeTargets.Count % 2 == 0 ? 90 : -90, 1000.0)
			: (
				((target.Position.X - from.X) * (raid.Position.Y - from.Y))
					- ((target.Position.Y - from.Y) * (raid.Position.X - from.X))
				> 0
					? -90
					: 90,
				Parameters.SideOffset
			);
		var waypoint = Waypoint(from, target.Position, side, offset, Reach(ref game.Units[leader]));
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != leader)
				return GiveUp(g, $"launch of {leader}: selected {g.SelectedUnit}");
			g.Commands.SetDestination(waypoint);
			_engaged[target.Unit] = UnitFacts.Now(g);
			_strikeTargets[leader] = target.Unit;
			_toWaypoint.Add(leader);
			Log?.Invoke(
				$"{UnitFacts.Now(g)}: {g.Units[leader].Kind} {leader} and {g.SelectionCount} more launched at {target.Kind} {target.Unit} by ({waypoint.X:F0}, {waypoint.Y:F0})"
			);
			return true;
		});
	}

	// Launches the selected parked planes, led by `leader`, toward a point where an enemy is thought to be; while
	// hunting carriers, by a waypoint 60% of the way there and 2000 to one side, the other side each strike, as their
	// own strike most likely comes straight the other way (LaunchStrike), unless the way round is longer than the planes'
	// `reach` (Waypoint), or `straight`: TryFlyOnFromWaypoint flies it on from there.
	private void LaunchToward(
		int leader,
		WorldPosition point,
		WorldPosition from,
		double reach,
		bool straight = false
	)
	{
		var waypoint =
			HuntsCarriers && !straight
				? Waypoint(
					from,
					point,
					_lastBlindStrike % 2 == 0 ? 90 : -90,
					Parameters.SideOffset,
					reach
				)
				: point;
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != leader)
				return GiveUp(g, $"launch of {leader}: selected {g.SelectedUnit}");
			g.Commands.SetDestination(waypoint);
			if (HuntsCarriers)
			{
				_blindPoints[leader] = point;
				_toWaypoint.Add(leader);
			}

			Log?.Invoke(
				$"{UnitFacts.Now(g)}: {g.Units[leader].Kind} {leader} and {g.SelectionCount} more launched toward ({point.X:F0}, {point.Y:F0}) by ({waypoint.X:F0}, {waypoint.Y:F0})"
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
				&& UnitFacts.Now(game) - c.Frame < ContactFrames
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

	// Whether enemy planes found in the air are near a deck; while hunting carriers, fighters, which shoot down the
	// planes as they climb, as the attackers in the air when scenario 201 starts do not.
	private bool IsUnderRaid(Nspw game, int deck) =>
		_contacts.All.Any(c =>
			c.IsFlying
			&& (!HuntsCarriers || c.Kind == UnitKind.Fighter)
			&& UnitFacts.Now(game) - c.Frame < 50
			&& UnitFacts.Distance(c.Position, game.Units[deck].Position) <= RaidRange
		);

	// Whether every ready fighter goes up against an enemy plane of a kind, not four: when the player holds on, or the
	// victory conditions count the kind.
	private bool DefendsAll(UnitKind kind) =>
		_knowledge.HoldsOn || _knowledge.Counted.Contains(kind);

	// The player's flying fighters that attack a plane, but `fighter`.
	private static int FightersOn(Nspw game, int target, int fighter)
	{
		var count = 0;
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var plane = ref game.Units[n];
			if (
				IsOwn(game, n)
				&& plane.Kind == UnitKind.Fighter
				&& plane.PlaneState == UnitState.Flying
				&& plane.Target == target
				&& n != fighter
			)
				count++;
		}

		return count;
	}

	// Fighters against an enemy attacker or bomber found near a deck, or near a place held from a deck not too far, or a
	// ship of the player's.
	private bool TryIntercept(Nspw game, List<int> decks)
	{
		if (HoldsFighters(game))
			return false;

		foreach (
			var contact in Parameters.FocusFire
				? _contacts.All.OrderByDescending(c => FightersOn(game, c.Unit, 0))
				: _contacts.All
		)
		{
			if (
				contact.Category != UnitCategory.Plane
				|| !UnitFacts.IsStrikePlane(contact.Kind)
				|| !game.Units[contact.Unit].IsFound
				|| game.Units[contact.Unit].PlaneState != UnitState.Flying
				|| (WasEngaged(game, contact.Unit) && !Parameters.FocusFire)
			)
				continue;

			var nearHeld = IsNearHeldPlace(game, contact.Position);
			foreach (
				var deck in decks.OrderBy(d =>
					UnitFacts.Distance(game.Units[d].Position, contact.Position)
				)
			)
			{
				var distance = UnitFacts.Distance(game.Units[deck].Position, contact.Position);
				if (
					(
						distance > Parameters.InterceptRange
						&& !(nearHeld && distance <= HeldPlaceRange * 2)
					) || !UnitFacts.CanLaunch(ref game.Units[deck])
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

				var group = fighters
					.Take(
						DefendsAll(contact.Kind) || Parameters.InterceptWithAll ? fighters.Count : 4
					)
					.ToList();
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

	// Counts the weapons the player's planes drop on each enemy unit, as the player sees an attacker let go of its
	// torpedo or bombs at its target and turn home whole: a plane that turns home damaged drops its weapon in the sea.
	private void CountDrops(Nspw game)
	{
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var plane = ref game.Units[n];
			if (
				!IsOwn(game, n)
				|| !UnitFacts.IsStrikePlane(plane.Kind)
				|| plane.PlaneState != UnitState.Flying
			)
			{
				_aimedAt.Remove(n);
				continue;
			}

			if (
				_aimedAt.TryGetValue(n, out var aimed)
				&& aimed.Ammo > plane.Ammo
				&& plane.HP > plane.MaxHP * 0.7
				&& aimed.Target != 0
			)
				_dropsOn[aimed.Target] = _dropsOn.GetValueOrDefault(aimed.Target) + 1;
			_aimedAt[n] = (plane.Target, plane.Ammo);
		}
	}

	// Whether a carrier found is thought to be out of the fight, at a fifth of its HP or less, where it can neither
	// launch nor take its planes back (UnitFacts.CanLaunch, MoveUnit): the player sees no enemy's damage, so it reckons
	// it from the weapons it saw dropped on it (CountDrops), six in ten hitting, for 7 HP each.
	private bool IsThoughtCrippled(Nspw game, Contact contact) =>
		_dropsOn.GetValueOrDefault(contact.Unit) * 0.6 * 7 >= game.Units[contact.Unit].MaxHP * 0.8;

	// Whether a strike plane may take a target the victory conditions do not count: when they count no carriers, or when
	// it has fuel left for little more than coming home.
	private bool TakesAnyTarget(Nspw game, int plane) =>
		!HuntsCarriers || game.Units[plane].Fuel <= FuelToHome(game, plane) + 15;

	// Whether a plane is of the share of the strike planes that attack the escorts (Targeting.Split, EscortShare): by its
	// number, spread evenly over the planes by the golden ratio.
	private bool IsEscortStriker(int plane) => plane * 0.6180339887 % 1 < Parameters.EscortShare;

	// Whether a ship found is an escort a strike plane that hunts carriers attacks first (Parameters.Targeting): a
	// battleship or a cruiser, or a cruiser only, or, for the share of the planes that do (IsEscortStriker), a battleship
	// or a cruiser, within 1500 of a carrier the victory conditions count, found lately.
	private bool IsEscortToStrike(Nspw game, Contact contact, int plane) =>
		HuntsCarriers
		&& (
			Parameters.Targeting switch
			{
				Targeting.Escorts => contact.Kind is UnitKind.Battleship or UnitKind.Cruiser,
				Targeting.Cruisers => contact.Kind == UnitKind.Cruiser,
				Targeting.Split => IsEscortStriker(plane)
					&& contact.Kind is UnitKind.Battleship or UnitKind.Cruiser,
				_ => false,
			}
		)
		&& _contacts.All.Any(c =>
			IsCounted(c.Kind)
			&& c.Category == UnitCategory.Ship
			&& game.Units[c.Unit].IsUsed
			&& UnitFacts.Now(game) - c.Frame < ContactFrames
			&& UnitFacts.Distance(c.Position, contact.Position) <= 1500
		);

	// Whether a point is within `distance` of a ship of the player's.
	private static bool IsNearOwnShip(Nspw game, WorldPosition point, double distance)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (
				IsOwn(game, m)
				&& game.Units[m].Category == UnitCategory.Ship
				&& UnitFacts.Distance(game.Units[m].Position, point) <= distance
			)
				return true;
		}

		return false;
	}

	// The best target found for a flying plane of the player's: for an attacker or a bomber, an enemy ship its weapon
	// can attack, within 2500, by its worth, the one it was sent at favored, the ones other strikes attack already
	// less; while it hunts carriers, only the ones the victory conditions count (TakesAnyTarget), the nearest, as every
	// carrier must be sunk and the nearest is the one the strike reaches with the most fuel left, the ones hit most
	// counted nearer (Parameters.FocusDistance), or, as
	// Parameters.Targeting says, the escorts around them first (IsEscortToStrike); for a fighter, an enemy plane in the
	// air, attackers and bombers first, or, with Parameters.FightersFirst, the ones within 1500 of a ship of the
	// player's, then fighters, within 1500, the nearest, or, with Parameters.SpreadFighters, the ones fewer of the
	// player's fighters attack (FightersOn); or, with Parameters.FocusFire, the attacker or bomber within 2500 that the
	// most of them attack.
	private Contact? ChooseTargetInFlight(Nspw game, int plane)
	{
		ref var unit = ref game.Units[plane];
		var position = unit.Position;
		var weapon = unit.Weapon;
		var takesAny = TakesAnyTarget(game, plane);
		var focus = Parameters.FocusFire
			? _contacts
				.All.Where(c =>
					c.Category == UnitCategory.Plane
					&& UnitFacts.IsStrikePlane(c.Kind)
					&& game.Units[c.Unit].IsFound
					&& game.Units[c.Unit].PlaneState == UnitState.Flying
					&& UnitFacts.Distance(position, c.Position) <= 2500
				)
				.OrderByDescending(c => FightersOn(game, c.Unit, plane))
				.ThenBy(c => UnitFacts.Distance(position, c.Position))
				.FirstOrDefault()
			: null;
		return unit.Kind == UnitKind.Fighter && focus is not null ? focus
			: unit.Kind == UnitKind.Fighter
				? _contacts
					.All.Where(c =>
						c.Category == UnitCategory.Plane
						&& game.Units[c.Unit].IsFound
						&& game.Units[c.Unit].PlaneState == UnitState.Flying
						&& UnitFacts.Distance(position, c.Position) <= 1500
					)
					.OrderByDescending(c =>
						!Parameters.FightersFirst ? (UnitFacts.IsStrikePlane(c.Kind) ? 1 : 0)
						: UnitFacts.IsStrikePlane(c.Kind) && IsNearOwnShip(game, c.Position, 1500)
							? 2
						: c.Kind == UnitKind.Fighter ? 1
						: 0
					)
					.ThenBy(c =>
						UnitFacts.Distance(position, c.Position)
						+ (Parameters.SpreadFighters ? 400 * FightersOn(game, c.Unit, plane) : 0)
					)
					.FirstOrDefault()
			: _contacts
				.All.Where(c =>
					game.Units[c.Unit].IsFound
					&& CanAttack(weapon, c)
					&& UnitFacts.Distance(position, c.Position) <= 2500
					&& (takesAny || IsCounted(c.Kind) || IsEscortToStrike(game, c, plane))
					&& (!_knowledge.StrikesCountedOnly || IsCounted(c.Kind))
				)
				.OrderByDescending(c =>
					HuntsCarriers
						? (
							IsEscortToStrike(game, c, plane) ? 200
							: IsCounted(c.Kind)
								? (
									Parameters.SpareCrippled && IsThoughtCrippled(game, c)
										? 50
										: 100
								)
							: 0
						)
							- (
								(
									UnitFacts.Distance(position, c.Position)
									- (
										Parameters.FocusDistance
										* _dropsOn.GetValueOrDefault(c.Unit)
									)
								) / 100
							)
						: Value(c.Kind)
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
			|| unit.PathX[1] != World.Right + 1
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
				$"{UnitFacts.Now(g)}: {g.Units[plane].Kind} {plane} sweeps toward ({point.X:F0}, {point.Y:F0})"
			);
			return true;
		});
		Deselect();
		return true;
	}

	// The area the briefing expects an enemy fleet in that a carrier found was heading for, within 45°, from beyond it,
	// while hunting carriers, or null: its home, where the computer's carrier sails back to its naval base, once it has
	// no more than 2 attackers or half its HP, to be repaired and to get its planes back, and comes out again
	// (ComputerPlayer.md, Returning to base).
	private Area? HomeOf(Contact contact) =>
		!HuntsCarriers || contact.Kind is not (UnitKind.Carrier or UnitKind.LightCarrier)
			? null
			: _knowledge
				.Enemy.Where(a =>
					!a.IsPlace
					&& UnitFacts.Distance(contact.Position, a.Center) > a.Radius
					&& Math.Abs(
						((BearingTo(contact.Position, a.Center) - contact.Direction + 540) % 360)
							- 180
					) <= 45
				)
				.MinBy(a => UnitFacts.Distance(contact.Position, a.Center));

	// Where an enemy unit found most likely is `ticks` from now, on the heading it was found on, for at most 1500 ticks
	// from when it was found: a ship zigzags, and one lost from sight long ago is better looked for where it was; a
	// carrier heading home (HomeOf) and lost for CarrierMemoryFrames, on its way there after those 1500 ticks, at
	// HomewardSpeed or faster, until it is there. A carrier sails away from the enemy within its strike range, toward home
	// as often as not, which is why it is thought to be going home only once it has been lost that long.
	private WorldPosition WhereNow(Nspw game, Contact contact, double ticks)
	{
		var elapsed = ((UnitFacts.Now(game) - contact.Frame) * 2) + ticks;
		var near = contact.Predict(Math.Min(elapsed, 1500));
		if (
			UnitFacts.Now(game) - contact.Frame < Parameters.CarrierMemoryFrames
			|| HomeOf(contact) is not { } home
		)
			return near;

		return PointAt(
			near,
			BearingTo(near, home.Center),
			Math.Min(
				(elapsed - 1500) * Math.Max(contact.Speed, HomewardSpeed),
				UnitFacts.Distance(near, home.Center)
			)
		);
	}

	// Whether a carrier found is still thought to be where WhereNow has it: for CarrierMemoryFrames, or, heading home,
	// until it would be there, and HomeFrames more.
	private bool IsRemembered(Nspw game, Contact contact) =>
		UnitFacts.Now(game) - contact.Frame < Parameters.CarrierMemoryFrames
		|| (
			HomeOf(contact) is { } home
			&& UnitFacts.Now(game) - contact.Frame
				< (
					UnitFacts.Distance(contact.Position, home.Center)
					/ Math.Max(contact.Speed, HomewardSpeed)
					/ 2
				) + HomeFrames
		);

	// The middle of a contact and the enemy ships found lately within 1500 of it, where they would be after `ticks`
	// more ticks: where the carriers or the battleships of a formation sail, its escorts around them.
	private WorldPosition FormationMiddle(Nspw game, Contact contact, double ticks)
	{
		var ships = _contacts
			.All.Where(c =>
				c.Unit == contact.Unit
				|| (
					c.Category == UnitCategory.Ship
					&& UnitFacts.Now(game) - c.Frame < ContactFrames
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
	// found before the carrier it screens, to the middle of its formation; or, launched toward a point, to that point.
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
		var target = _contacts.Find(_strikeTargets.GetValueOrDefault(leader));
		WorldPosition point;
		if (target is not null)
		{
			var ticks =
				((UnitFacts.Now(game) - target.Frame) * 2)
				+ (
					UnitFacts.Distance(unit.Position, target.Position)
					/ Math.Max(unit.MaxSpeed, 0.5)
					/ 2
				);
			point = IsCounted(target.Kind)
				? target.Predict(Math.Min(ticks, 1500))
				: FormationMiddle(game, target, ticks);
		}
		else if (_blindPoints.Remove(leader, out var blind))
		{
			point = blind;
		}
		else
		{
			return false;
		}

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
				$"{UnitFacts.Now(g)}: {g.Units[leader].Kind} {leader} flies on toward {(target is null ? "" : $"{target.Kind} {target.Unit} ")}at ({point.X:F0}, {point.Y:F0})"
			);
			return true;
		});
		Deselect();
		return true;
	}

	// A strike whose target is alive but not found flies on to where it would be now (WhereNow), then, not finding it
	// there, to where it was last found, and takes no other target until it has been there, if the victory conditions
	// count the target: the escorts around a carrier are found first. Once there, with the target still not found, the
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
		{
			_lookedAhead.Remove(leader);
			return false;
		}

		var point = _lookedAhead.Contains(leader) ? target.Position : WhereNow(game, target, 0);
		if (UnitFacts.Distance(unit.Position, point) < 400)
		{
			if (_lookedAhead.Add(leader))
				return true;

			_lookedAhead.Remove(leader);
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
	// to its own point 250 from it, the points fanned out across the side it comes from, 75° each way, or 45° while
	// hunting carriers, as the strike comes in from the side the escorts cover least (TryApproach), so that the guns
	// and the target's turns meet the torpedoes from many sides at once. Within 400 of the target, each plane's own
	// attack takes over (SetAttackerAttackDestination). A plane splits, sweeps, disperses and approaches again on each
	// flight: what it did on its last is forgotten once it is back on its deck.
	private bool TrySplitAttack(Nspw game)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var leader = ref game.Units[m];
			var target = leader.Target;
			if (
				IsOwn(game, m)
				&& leader.Category == UnitCategory.Plane
				&& leader.PlaneState == UnitState.Parked
			)
			{
				_split.Remove(m);
				_sweeps.Remove(m);
				_approaching.Remove(m);
				_holding.Remove(m);
				_lookedAhead.Remove(m);
				_dispersed.Remove(m);
				continue;
			}

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
			var fan = HuntsCarriers ? Parameters.FanAngle : 75.0;
			for (var i = 0; i < planes.Count; i++)
			{
				var plane = planes[i];
				var turn = planes.Count == 1 ? 0 : -fan + (2 * fan * i / (planes.Count - 1));
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
				$"{UnitFacts.Now(game)}: {planes.Count} planes of {leader.Kind} {m} split up against {game.Units[target].Kind} {target}"
			);
			return true;
		}

		return false;
	}

	// While hunting carriers, a strike in the air disperses: a formation flies at about 60% of its planes' speed, as its
	// leader slows down for any plane out of its place (SteerUnit), and burns its fuel all the same, so once none of its
	// planes is left on the deck, each is selected alone and sent on to where the leader flies, which takes it out of
	// the formation (ApplyOrders), with the leader's waypoint, target or point; each then flies on, approaches and
	// attacks alone, at full speed (TryDirect, TryApproach). Not with Parameters.Disperse false.
	private bool TryDisperse(Nspw game)
	{
		if (!HuntsCarriers || !Parameters.Disperse)
			return false;

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var leader = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| !UnitFacts.IsStrikePlane(leader.Kind)
				|| leader.PlaneState != UnitState.Flying
				|| leader.GroupLeader != 0
				|| leader.IsGroupLeader < 2
				|| leader.Mode == UnitMode.Return
				|| leader.Ammo <= 0
				|| _scoutLegs.ContainsKey(m)
				|| _dispersed.Contains(m)
			)
				continue;

			var followers = new List<int>();
			var waiting = false;
			for (var n = 1; n <= game.MaxUnitId; n++)
			{
				if (IsOwn(game, n) && game.Units[n].GroupLeader == m)
				{
					followers.Add(n);
					waiting |= game.Units[n].PlaneState != UnitState.Flying;
				}
			}

			if (waiting)
				continue;

			_dispersed.Add(m);
			_groups[m] = [m, .. followers];
			var leaderUnit = m;
			var point = new WorldPosition(leader.PathX[0], leader.PathY[0]);
			foreach (var follower in followers)
			{
				SelectFlying(follower, "dispersing");
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != follower)
						return GiveUp(g, $"dispersing {follower}: selected {g.SelectedUnit}");
					g.Commands.SetDestination(point);
					_dispersed.Add(follower);
					if (_toWaypoint.Contains(leaderUnit))
						_toWaypoint.Add(follower);
					if (_strikeTargets.TryGetValue(leaderUnit, out var target))
						_strikeTargets[follower] = target;
					if (_blindPoints.TryGetValue(leaderUnit, out var blind))
						_blindPoints[follower] = blind;
					return true;
				});
			}

			Deselect();
			Log?.Invoke(
				$"{UnitFacts.Now(game)}: {followers.Count + 1} planes of {leader.Kind} {m} disperse toward ({point.X:F0}, {point.Y:F0})"
			);
			return true;
		}

		return false;
	}

	// How much an enemy warship's anti-aircraft fire covers a point: a battleship's shells reach 700, a cruiser's 600
	// (ShipFireAntiAircraftShellWithoutTarget), a destroyer's machine guns 300, and a battleship fires most.
	private static double GunCover(UnitKind kind, double distance) =>
		kind switch
		{
			UnitKind.Battleship when distance <= 750 => 1.5,
			UnitKind.Cruiser when distance <= 650 => 1,
			UnitKind.Destroyer when distance <= 300 => 0.3,
			_ => 0,
		};

	// The distance from a point to a segment.
	private static double DistanceToSegment(WorldPosition point, WorldPosition a, WorldPosition b)
	{
		var (dx, dy) = (b.X - a.X, b.Y - a.Y);
		var t = Math.Clamp(
			(((point.X - a.X) * dx) + ((point.Y - a.Y) * dy)) / Math.Max((dx * dx) + (dy * dy), 1),
			0,
			1
		);
		return UnitFacts.Distance(point, new WorldPosition(a.X + (t * dx), a.Y + (t * dy)));
	}

	// Whether a torpedo plane can run in on a target at a point from a bearing, as for the land: the sea all the way in
	// from 240, and a drop allowed (UnitFacts.CanDropTorpedoAt) at 100 to 220, where it drops (PlaneDropTorpedo), so
	// that it does not circle over the land with its torpedo until it is shot down or turns home.
	private static bool IsSeaRun(Nspw game, WorldPosition at, double bearing)
	{
		for (var d = 20; d <= 240; d += 20)
		{
			var point = PointAt(at, bearing, d);
			if (
				!UnitFacts.IsSea(game, point)
				|| (d is >= 100 and <= 220 && !UnitFacts.CanDropTorpedoAt(game, point))
			)
				return false;
		}

		return true;
	}

	// The side from which a strike runs in on a target at a point, coming from a bearing: the bearing from the target
	// whose last 1500 the warships found around it cover least, then the nearest to where the strike comes from; with
	// Parameters.BeamApproach, only within 30° of square to the target's heading, on either side; with
	// Parameters.SeaApproach, only those with the sea to run in on (IsSeaRun), or any bearing that has it, if none.
	private double ApproachBearing(Nspw game, int target, WorldPosition at, double from)
	{
		var heading = _contacts.Find(target)?.Direction;
		var bearings =
			Parameters.BeamApproach && heading is { } h
				?
				[
					.. new[] { h + 90, h - 90 }.SelectMany(beam =>
						Enumerable.Range(-2, 5).Select(i => beam + (i * 15))
					),
				]
				: Enumerable.Range(0, 24).Select(i => from + (i * 15)).ToList();
		if (Parameters.SeaApproach)
		{
			var sea = bearings.Where(b => IsSeaRun(game, at, b)).ToList();
			if (sea.Count == 0)
				sea =
				[
					.. Enumerable
						.Range(0, 24)
						.Select(i => from + (i * 15))
						.Where(b => IsSeaRun(game, at, b)),
				];
			if (sea.Count > 0)
				bearings = sea;
		}

		var guns = _contacts
			.All.Where(c =>
				c.Unit != target
				&& c.Category == UnitCategory.Ship
				&& game.Units[c.Unit].IsUsed
				&& UnitFacts.Now(game) - c.Frame < ContactFrames
				&& UnitFacts.Distance(c.Position, at) <= 3000
			)
			.Select(c => (c.Kind, Position: c.Predict((UnitFacts.Now(game) - c.Frame) * 2)))
			.ToList();
		return bearings.MinBy(b =>
		{
			var (outer, inner) = (PointAt(at, b, 1500), PointAt(at, b, 200));
			var turn = Math.Abs(((b - from + 540) % 360) - 180);
			return guns.Sum(g => GunCover(g.Kind, DistanceToSegment(g.Position, outer, inner)))
				+ (turn / 180 * 0.5);
		});
	}

	// The strikes in the air that have not approached their target yet, each as the planes that fly together: a
	// dispersed strike (TryDisperse) as its planes, a plane alone as itself; a formation waits until it has dispersed.
	private IEnumerable<List<int>> StrikesToApproach(Nspw game)
	{
		bool IsReady(int n) =>
			IsOwn(game, n)
			&& UnitFacts.IsStrikePlane(game.Units[n].Kind)
			&& game.Units[n].PlaneState == UnitState.Flying
			&& game.Units[n].GroupLeader == 0
			&& game.Units[n].Mode != UnitMode.Return
			&& game.Units[n].Ammo > 0
			&& !_scoutLegs.ContainsKey(n)
			&& !_split.Contains(n)
			&& !_approaching.ContainsKey(n);

		foreach (var (leader, members) in _groups.ToList())
		{
			var planes = members.Where(IsReady).ToList();
			if (planes.Count == 0)
				_groups.Remove(leader);
			else
				yield return planes;
		}

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (IsReady(m) && !_dispersed.Contains(m) && game.Units[m].IsGroupLeader <= 1)
				yield return [m];
		}
	}

	// Sends a strike in to attack a target, each plane along its path (TryApproach), running in from a bearing.
	private void Approach(
		List<int> planes,
		int targetUnit,
		double bearing,
		List<WorldPosition>[] paths
	)
	{
		var steps = paths[0].Count - 1;
		for (var i = 0; i < planes.Count; i++)
		{
			var plane = planes[i];
			var path = paths[i];
			_approaching[plane] = path[^1];
			_toWaypoint.Remove(plane);
			_blindPoints.Remove(plane);
			_holding.Remove(plane);
			SelectFlying(plane, "approaching");
			foreach (var point in path)
			{
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != plane)
						return GiveUp(g, $"approach of {plane}: selected {g.SelectedUnit}");
					g.Commands.SetDestination(point);
					return true;
				});
			}
		}

		var first = planes[0];
		_clicks.Enqueue(g =>
			Log?.Invoke(
				$"{UnitFacts.Now(g)}: {planes.Count} strikes of {g.Units[first].Kind} {first} approach {g.Units[targetUnit].Kind} {targetUnit} from {bearing % 360:F0}° by {steps + 1} waypoints"
			)
		);
		Deselect();
	}

	// The bearing a strike runs in from, chosen by playing it forward (Lookahead): the heuristic's (ApproachBearing) and
	// eight others around the target, each flown in copies of the battle on Parameters.LookaheadSeeds seeds of their own
	// for Parameters.LookaheadFrames frames; scored by the HP the enemy's ships the victory counts lose within 3000 of the
	// target, less Parameters.LookaheadPlaneCost for each plane of the strike lost. In a copy, the strike is sent in as
	// TryApproach sends it, and then, with Parameters.LookaheadPlays, the player plays on in it, a copy of it (CopyFor);
	// or else the planes fly their paths, and the game's own attack takes over at the end of them.
	private double LookAheadApproach(
		Nspw game,
		List<int> planes,
		int targetUnit,
		WorldPosition at,
		double from,
		double heuristic,
		Func<double, (double Bearing, List<WorldPosition>[] Paths)> pathsOf
	)
	{
		double[] candidates = [heuristic, .. Enumerable.Range(0, 8).Select(k => from + (k * 45.0))];
		var plans = candidates.Select(pathsOf).ToArray();
		var routes = plans.Select(p => p.Paths).ToArray();
		var counted = _contacts
			.All.Where(c =>
				c.Category == UnitCategory.Ship
				&& IsCounted(c.Kind)
				&& game.Units[c.Unit].IsUsed
				&& UnitFacts.Distance(c.Position, at) <= 3000
			)
			.Select(c => c.Unit)
			.ToList();
		var hp = counted.ToDictionary(n => n, n => game.Units[n].HP);

		// A candidate's orders in a copy, one click a frame, as the game takes them: each plane its path, as TryApproach
		// gives it; then, as each comes to the end of its path, the target to attack, as the player gives it once the
		// planes are there.
		Action<GameFork> Orders(int candidate)
		{
			var clicks = new Queue<Action<GameFork>>();
			void Deselect() =>
				clicks.Enqueue(f =>
				{
					if (f.Game.SelectedUnit != 0)
						f.RightClick();
				});

			for (var i = 0; i < planes.Count; i++)
			{
				var plane = planes[i];
				Deselect();
				clicks.Enqueue(f => f.ClickUnit(plane));
				foreach (var point in routes[candidate][i])
					clicks.Enqueue(f =>
					{
						if (f.Game.SelectedUnit == plane)
							f.SetDestination(point);
					});
			}

			Deselect();
			var attacking = new HashSet<int>();
			return fork =>
			{
				var g = fork.Game;
				if (clicks.Count == 0)
				{
					for (var i = 0; i < planes.Count; i++)
					{
						var plane = planes[i];
						if (
							attacking.Contains(plane)
							|| !g.Units[plane].IsUsed
							|| g.Units[plane].PlaneState != UnitState.Flying
							|| !g.Units[targetUnit].IsUsed
							|| UnitFacts.Distance(g.Units[plane].Position, routes[candidate][i][^1])
								> 250
						)
							continue;

						attacking.Add(plane);
						Deselect();
						clicks.Enqueue(f => f.ClickUnit(plane));
						clicks.Enqueue(f =>
						{
							if (f.Game.SelectedUnit == plane)
								f.ClickUnit(targetUnit);
						});
						Deselect();
					}
				}

				if (clicks.TryDequeue(out var click))
					click(fork);
			};
		}

		// A candidate played by a copy of the player in a copy of the battle: the strike sent in, its first clicks given in
		// the frame of the choice, whose rest the player has done already (Act), then the player playing on.
		Action<GameFork> Played(int candidate, GameFork fork)
		{
			var player = CopyFor(game, fork.Game);
			player.Approach(planes, targetUnit, plans[candidate].Bearing, plans[candidate].Paths);
			var isFirst = true;
			return f =>
			{
				if (isFirst)
					player.GiveClicks(f.Game);
				else
					player.Act(f.Game);
				isFirst = false;
				f.Step();
			};
		}

		double Score(Nspw g) =>
			counted.Sum(n => hp[n] - Math.Max(g.Units[n].IsUsed ? g.Units[n].HP : 0, 0))
			- (Parameters.LookaheadPlaneCost * planes.Count(n => !g.Units[n].IsUsed));

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		var scores = Lookahead.Evaluate(
			game,
			candidates.Length,
			Parameters.LookaheadPlays
				? Played
				: (candidate, _) =>
				{
					var orders = Orders(candidate);
					return f => f.Step(orders);
				},
			Parameters.LookaheadFrames,
			Parameters.LookaheadSeeds,
			Score,
			(uint)UnitFacts.Now(game)
		);
		var best = Array.IndexOf(scores, scores.Max());
		Log?.Invoke(
			$"{UnitFacts.Now(game)}: lookahead runs in from {candidates[best] % 360:F0}°, the heuristic's {heuristic % 360:F0}°, in {stopwatch.ElapsedMilliseconds} ms: {string.Join(" ", scores.Select(v => v.ToString("F1")))}"
		);
		return scores[best] > scores[0] ? candidates[best] : heuristic;
	}

	// A strike that hunts carriers, 1300 to 4000 from where the nearest one remembered (IsRemembered) most
	// likely is (WhereNow), or, knowing of none, 700 or more from the middle of the nearest ships found within 3000,
	// where the carrier they screen most likely is, as escorts are found from 1000, outside their guns' reach, before
	// the carrier, holds off 3300 to one side of it while enemy planes are found in the air within 2500 of it, as long
	// as its fuel lets: a strike forming up over its carrier, or coming home to it, whose gunners and fighters break up
	// an attack, and which sails off to the player's carriers. Then it flies around it at 2000, out of the reach of its
	// escorts' guns, to the side they cover least (ApproachBearing), as far as its fuel lets, and
	// runs in from there, a dispersed strike's planes (TryDisperse) fanned out across that side, so that they come in
	// together from many sides: TrySplitAttack splits a formation up as it comes within 2200. The game's own attack
	// takes over only within 400 of the target, or at the end of the path, so the strike follows its path around.
	private bool TryApproach(Nspw game)
	{
		if (!HuntsCarriers)
			return false;

		foreach (var planes in StrikesToApproach(game))
		{
			if (_oneWay.Contains(planes[0]))
				continue;

			ref var leader = ref game.Units[planes[0]];
			var position = leader.Position;
			var speed = Math.Max(leader.MaxSpeed, 0.5);
			var target = _contacts
				.All.Where(c =>
					c.Category == UnitCategory.Ship
					&& IsCounted(c.Kind)
					&& game.Units[c.Unit].IsUsed
					&& IsRemembered(game, c)
					&& UnitFacts.Distance(position, WhereNow(game, c, 0)) <= 4000
				)
				.MinBy(c => UnitFacts.Distance(position, WhereNow(game, c, 0)));
			var escort = _contacts
				.All.Where(c =>
					c.Category == UnitCategory.Ship
					&& !UnitFacts.IsBase(c.Kind)
					&& game.Units[c.Unit].IsFound
					&& UnitFacts.Distance(position, c.Position) <= 3000
				)
				.MinBy(c => UnitFacts.Distance(position, c.Position));
			var aim = target ?? escort;
			var at =
				target is not null
					? WhereNow(
						game,
						target,
						UnitFacts.Distance(position, WhereNow(game, target, 0)) / speed
					)
				: escort is not null ? FormationMiddle(game, escort, 0)
				: position;
			if (aim is null || UnitFacts.Distance(position, at) < (target is null ? 700 : 1300))
				continue;

			// A unit of fuel lasts FuelInterval ticks: the arc is as long as the fuel lets, after the run in and the
			// flight home, with 10 units to spare.
			var home = game.Units[leader.Carrier].Position;
			var guarded = _contacts
				.All.Where(c =>
					c.Category == UnitCategory.Plane
					&& c.IsFlying
					&& UnitFacts.Now(game) - c.Frame < 60
					&& UnitFacts.Distance(c.Position, at) <= 2500
				)
				.ToList();
			if (guarded.Count > 0 && leader.Fuel > FuelToHome(game, planes[0]) + 25)
			{
				var middle = new WorldPosition(
					guarded.Average(c => c.Position.X),
					guarded.Average(c => c.Position.Y)
				);
				var hold = new[] { 90, -90 }
					.Select(turn => PointAt(at, BearingTo(at, home) + turn, Parameters.HoldOff))
					.MaxBy(p => UnitFacts.Distance(p, middle));
				var holding = planes
					.Where(n =>
						!_holding.TryGetValue(n, out var held)
						|| UnitFacts.Distance(held, hold) > 500
					)
					.ToList();
				if (holding.Count == 0)
					continue;

				foreach (var plane in holding)
				{
					_holding[plane] = hold;
					SelectFlying(plane, "holding");
					_clicks.Enqueue(g =>
					{
						if (g.SelectedUnit != plane)
							return GiveUp(g, $"holding {plane}: selected {g.SelectedUnit}");
						g.Commands.SetDestination(hold);
						return true;
					});
				}

				var guardedTarget = aim.Unit;
				_clicks.Enqueue(g =>
					Log?.Invoke(
						$"{UnitFacts.Now(g)}: {holding.Count} strikes hold off {g.Units[guardedTarget].Kind} {guardedTarget} at ({hold.X:F0}, {hold.Y:F0}), {guarded.Count} planes over it"
					)
				);
				Deselect();
				return true;
			}

			var from = BearingTo(at, position);
			var heuristic = ApproachBearing(game, target?.Unit ?? 0, at, from);
			var spare =
				(
					((leader.Fuel - 10) * leader.FuelInterval)
					- ((1300 + UnitFacts.Distance(at, home)) / speed)
				) * speed;
			var arc = Math.Max(
				spare
					- Math.Max(UnitFacts.Distance(position, at) - Parameters.ApproachRadius, 0)
					- 700,
				0
			);

			// The bearing a strike can come round to, as far as its fuel lets, and each plane's path: the arc's points,
			// then its point of the fan across the side it runs in from; with Parameters.PincerAngle, each half of the
			// strike comes round to its own side, that many degrees apart.
			(double Bearing, List<WorldPosition>[] Paths) PathsOf(double toward)
			{
				double TurnTo(double bearing) =>
					Math.Clamp(
						((bearing - from + 540) % 360) - 180,
						-arc / Parameters.ApproachRadius * 180 / Math.PI,
						arc / Parameters.ApproachRadius * 180 / Math.PI
					);

				List<WorldPosition> ArcTo(double turn)
				{
					var steps = (int)Math.Ceiling(Math.Abs(turn) / 40);
					return
					[
						.. Enumerable
							.Range(1, steps)
							.Select(i =>
								PointAt(at, from + (turn * i / steps), Parameters.ApproachRadius)
							),
					];
				}

				var turn = TurnTo(toward);
				var halves = Parameters.PincerAngle > 0 && planes.Count > 1;
				var sides = halves
					? new[] { -Parameters.PincerAngle / 2, Parameters.PincerAngle / 2 }
						.Select(o => TurnTo(toward + o))
						.ToArray()
					: [turn, turn];
				var arcs = sides.Select(ArcTo).ToArray();
				return (
					from + turn,
					[
						.. planes.Select(
							(_, i) =>
							{
								var half = halves ? i % 2 : 0;
								var count = halves ? (planes.Count + 1 - half) / 2 : planes.Count;
								var place = halves ? i / 2 : i;
								return arcs[half]
									.Append(
										PointAt(
											at,
											from
												+ sides[half]
												+ (
													count == 1
														? 0
														: -30 + (60.0 * place / (count - 1))
												),
											1300
										)
									)
									.ToList();
							}
						),
					]
				);
			}

			var bearing =
				Parameters.LookaheadApproach && !_isFork
					? LookAheadApproach(game, planes, aim.Unit, at, from, heuristic, PathsOf)
					: heuristic;
			var (runIn, paths) = PathsOf(bearing);
			Approach(planes, aim.Unit, runIn, paths);
			return true;
		}

		return false;
	}

	// A strike that finds only ships the victory conditions do not count, while it hunts carriers, flies on from its
	// waypoint to the nearest carrier it knows of within 5000, where it would be when the strike gets there; or, knowing
	// of none, to the middle of the nearest ships found within 2500, where the carrier they screen most likely is.
	private bool TryCloseOnFormation(Nspw game, int leader)
	{
		ref var unit = ref game.Units[leader];
		var position = unit.Position;
		var speed = Math.Max(unit.MaxSpeed, 0.5);
		var counted = _contacts
			.All.Where(c =>
				c.Category == UnitCategory.Ship
				&& IsCounted(c.Kind)
				&& game.Units[c.Unit].IsUsed
				&& IsRemembered(game, c)
				&& UnitFacts.Distance(position, c.Position) <= 5000
			)
			.MinBy(c => UnitFacts.Distance(position, c.Position));
		var nearest = _contacts
			.All.Where(c =>
				c.Category == UnitCategory.Ship
				&& !UnitFacts.IsBase(c.Kind)
				&& game.Units[c.Unit].IsFound
				&& UnitFacts.Distance(position, c.Position) <= 2500
			)
			.MinBy(c => UnitFacts.Distance(position, c.Position));
		if (
			!UnitFacts.IsStrikePlane(unit.Kind)
			|| _toWaypoint.Contains(leader)
			|| (counted ?? nearest) is not { } around
		)
			return false;

		var point = counted is not null
			? WhereNow(game, counted, UnitFacts.Distance(position, counted.Position) / speed)
			: FormationMiddle(game, around, 0);
		if (UnitFacts.Distance(new WorldPosition(unit.PathX[0], unit.PathY[0]), point) < 200)
			return false;

		SelectFlying(leader, "closing in");
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != leader)
				return GiveUp(g, $"closing in {leader}: selected {g.SelectedUnit}");
			g.Commands.SetDestination(point);
			Log?.Invoke(
				$"{UnitFacts.Now(g)}: {g.Units[leader].Kind} {leader} closes in on {around.Kind} {around.Unit} at ({point.X:F0}, {point.Y:F0})"
			);
			return true;
		});
		Deselect();
		return true;
	}

	// While hunting carriers, a strike plane in the air, but its scouts, comes home once it has fuel for little more
	// than that (FuelToHome), the formation it leads with it: the game turns a plane home only at 20, which is too
	// little when it is far, and the player's carriers may have sailed off while it was away. A plane attacking a
	// target it sees within 2500 finishes its run first, on most of FuelToHome's margin. With Parameters.FighterBingo, a
	// fighter too, hunting carriers or not: one that chases a raid far from its deck runs dry otherwise.
	private bool TryBingo(Nspw game)
	{
		var fighterBingo = Parameters.FighterBingo || RaidedBase(game) != 0;
		if (!HuntsCarriers && !fighterBingo)
			return false;

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var unit = ref game.Units[m];
			var isFighter = unit.Kind == UnitKind.Fighter;
			var attacking =
				unit.Target != 0
				&& game.Units[unit.Target].IsUsed
				&& game.Units[unit.Target].IsFound
				&& UnitFacts.Distance(unit.Position, game.Units[unit.Target].Position) <= 2500;
			if (
				!IsOwn(game, m)
				|| !(isFighter ? fighterBingo : HuntsCarriers && UnitFacts.IsStrikePlane(unit.Kind))
				|| unit.PlaneState != UnitState.Flying
				|| unit.GroupLeader != 0
				|| unit.Mode == UnitMode.Return
				|| _scoutLegs.ContainsKey(m)
				|| (_oneWay.Contains(m) && unit.Ammo > 0)
				|| unit.Fuel
					> FuelToHome(game, m)
						+ (
							attacking && !isFighter
								? Parameters.AttackingBingoMargin
								: Parameters.BingoMargin
						)
			)
				continue;

			SelectFlying(m, "sending home");
			ReturnSelected(m, "sending home");
			Deselect();
			return true;
		}

		return false;
	}

	// Each flying plane of the player's whose deck is at a fifth of its HP or less, where no plane lands (MoveUnit), or
	// gone, is given the nearest deck of the player's that can take it, as a click on a deck with the plane selected
	// does (ApplyOrders): a bomber only an air base; while Parameters.DivertFromCrippledDecks says so, once every
	// RetargetFramesForDivert. It would circle its own until its fuel ran out.
	private bool TryDivert(Nspw game)
	{
		if (!Parameters.DivertFromCrippledDecks)
			return false;

		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var plane = ref game.Units[n];
			if (
				!IsOwn(game, n)
				|| plane.Category != UnitCategory.Plane
				|| plane.PlaneState != UnitState.Flying
				|| (
					IsOwn(game, plane.Carrier)
					&& game.Units[plane.Carrier].HP > game.Units[plane.Carrier].MaxHP * 0.2
				)
				|| UnitFacts.Now(game) - _diverted.GetValueOrDefault(n, int.MinValue / 2)
					< RetargetFramesForDivert
			)
				continue;

			var position = plane.Position;
			var isBomber = plane.Kind == UnitKind.Bomber;
			var deck = Decks(game)
				.Where(d =>
					game.Units[d].HP > game.Units[d].MaxHP * 0.2
					&& game.Units[d].BuildTime == 0
					&& (!isBomber || game.Units[d].Kind == UnitKind.AirBase)
				)
				.OrderBy(d => UnitFacts.Distance(game.Units[d].Position, position))
				.FirstOrDefault();
			if (deck == 0)
				continue;

			var flying = n;
			_diverted[flying] = UnitFacts.Now(game);
			SelectFlying(flying, "diverting");
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != flying)
					return GiveUp(g, $"diverting {flying}: selected {g.SelectedUnit}");
				g.Commands.ClickUnit(deck);
				Log?.Invoke(
					$"{UnitFacts.Now(g)}: {g.Units[flying].Kind} {flying} lands on {g.Units[deck].Kind} {deck} instead"
				);
				return true;
			});
			Deselect();
			return true;
		}

		return false;
	}

	// Whether a fighter of the player's on its way home goes back to the fight (Parameters.Reengage): it has
	// ammunition, fuel for more than the way home, and 60% of its HP or more, or, damaged, an enemy attacker or bomber
	// within 1200 of it is within 1500 of a ship or a base of the player's, about to drop.
	private bool CanReengage(Nspw game, int m)
	{
		ref var unit = ref game.Units[m];
		if (
			!Parameters.Reengage
			|| unit.Kind != UnitKind.Fighter
			|| unit.Ammo <= 0
			|| unit.Fuel <= FuelToHome(game, m) + Parameters.BingoMargin + 10
		)
			return false;

		if (unit.HP >= unit.MaxHP * 0.6)
			return true;

		var position = unit.Position;
		return _contacts.All.Any(c =>
			c.Category == UnitCategory.Plane
			&& UnitFacts.IsStrikePlane(c.Kind)
			&& game.Units[c.Unit].IsUsed
			&& game.Units[c.Unit].IsFound
			&& game.Units[c.Unit].PlaneState == UnitState.Flying
			&& UnitFacts.Distance(c.Position, position) <= 1200
			&& IsNearOwnShip(game, c.Position, 1500)
		);
	}

	// A flying plane of the player's that leads its formation, armed and with no target in sight, given the best target
	// near it: the formation is selected with it, and takes the target too (ApplyOrders); a fighter on its way home too,
	// if it can go back to the fight (CanReengage).
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
				|| _scoutLegs.ContainsKey(m)
				|| (unit.Mode == UnitMode.Return && !CanReengage(game, m))
				|| unit.Ammo <= 0
				|| (
					unit.Target != 0
					&& game.Units[unit.Target].IsUsed
					&& game.Units[unit.Target].IsFound
				)
				|| (_directed.TryGetValue(m, out var frame) && UnitFacts.Now(game) - frame < 100)
			)
				continue;

			if (
				(
					_approaching.TryGetValue(m, out var approach)
					&& UnitFacts.Distance(UnitFacts.FinalDestination(ref unit), approach) < 50
					&& UnitFacts.Distance(unit.Position, approach) > 300
				)
				|| (
					_holding.TryGetValue(m, out var hold)
					&& UnitFacts.Distance(UnitFacts.FinalDestination(ref unit), hold) < 50
					&& unit.Fuel > FuelToHome(game, m) + 25
				)
			)
				continue;

			_directed[m] = UnitFacts.Now(game);
			if (TryFlyOnFromWaypoint(game, m) || TryKeepOnTarget(game, m))
				return true;

			if (ChooseTargetInFlight(game, m) is not { } target)
			{
				if ((!TakesAnyTarget(game, m) && TryCloseOnFormation(game, m)) || TrySweep(game, m))
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
							UnitFacts.Now(g)
						).Predict(ticks)
					);
					return true;
				});
			}

			_clicks.Enqueue(g =>
			{
				Log?.Invoke(
					$"{UnitFacts.Now(g)}: {g.Units[plane].Kind} {plane} {(g.Units[plane].Target == target.Unit ? "attacks" : "did not take")} {target.Kind} {target.Unit}"
				);
				if (g.SelectedUnit != 0)
					g.Commands.RightClick();
			});
			return true;
		}

		return false;
	}

	// The best enemy found lately within reach of a deck's planes armed with a weapon, or null; while the strikes hunt
	// carriers, a carrier remembered (IsRemembered), where it most likely is now, within 85% of the planes'
	// `reach`, which leaves fuel to look for it and to come round to its side, the nearest first, the ones hit most
	// counted nearer (Parameters.FocusDistance), or, with Parameters.SpareCrippled, the nearest of those not thought out
	// of the fight (IsThoughtCrippled) first.
	private Contact? ChooseTarget(Nspw game, int deck, FireKind weapon, double reach)
	{
		var position = game.Units[deck].Position;
		return _contacts
			.All.Where(c =>
				(
					HuntsCarriers
						? IsCounted(c.Kind)
							&& game.Units[c.Unit].IsUsed
							&& IsRemembered(game, c)
							&& UnitFacts.Distance(position, WhereNow(game, c, 0))
								<= reach * Parameters.TargetReach
						: UnitFacts.Now(game) - c.Frame < ContactFrames
							&& UnitFacts.Distance(position, c.Position) <= StrikeRange
				)
				&& CanAttack(weapon, c)
				&& (!_knowledge.StrikesCountedOnly || IsCounted(c.Kind))
				&& !WasEngaged(game, c.Unit)
			)
			.OrderByDescending(c =>
				HuntsCarriers
					? -UnitFacts.Distance(position, WhereNow(game, c, 0))
						+ (Parameters.FocusDistance * _dropsOn.GetValueOrDefault(c.Unit))
						- (Parameters.SpareCrippled && IsThoughtCrippled(game, c) ? 100_000 : 0)
					: Value(c.Kind) - (UnitFacts.Distance(position, c.Position) / 2000)
			)
			.FirstOrDefault();
	}

	// A place the briefing names, within reach of a deck, where nothing has been found lately and no strike went
	// lately: a strike goes there to find what to attack, as a player strikes Pearl Harbor without seeing it first.
	private WorldPosition? PlaceToStrike(Nspw game, int deck, double reach)
	{
		var position = game.Units[deck].Position;
		var place = _knowledge.Enemy.FirstOrDefault(a =>
			a.IsPlace
			&& UnitFacts.Distance(position, a.Center) <= reach
			&& UnitFacts.Now(game) - _placeStrikes.GetValueOrDefault(a.Name, -PlaceStrikeFrames)
				>= PlaceStrikeFrames
			&& !_contacts.All.Any(c =>
				UnitFacts.Distance(c.Position, a.Center) <= a.Radius + 500
				&& UnitFacts.Now(game) - c.Frame < ContactFrames
			)
		);
		if (place is null)
			return null;

		_placeStrikes[place.Name] = UnitFacts.Now(game);
		return place.Center;
	}

	// Whether an area where the briefing expects the enemy's carriers is one that a strike from a deck at a point goes to
	// before any is found (ExpectedCarriersToStrike): the only one, or one so near that the carriers there would strike
	// the deck (ComputerStrikeRange).
	private bool IsEarlyStrikeArea(Nspw game, WorldPosition deck, Area area) =>
		_knowledge.Enemy.Count(a => !a.IsPlace) == 1
		|| UnitFacts.Distance(deck, area.Center) <= ComputerStrikeRange(game) + area.Radius + 1000;

	// While hunting carriers, before any is found, early in the battle, while they most likely are still there, an area
	// where the briefing expects them, within reach of a deck's planes (IsEarlyStrikeArea): a strike goes there once, to
	// find them and attack before their own strikes are ready, as in scenarios 4 and 201, whose carriers may start
	// within reach.
	private WorldPosition? ExpectedCarriersToStrike(Nspw game, int deck, double reach)
	{
		if (
			!HuntsCarriers
			|| !Parameters.EarlyStrike
			|| UnitFacts.Now(game) > 2500
			|| _contacts.All.Any(c => c.Category == UnitCategory.Ship && IsCounted(c.Kind))
		)
			return null;

		var position = game.Units[deck].Position;
		var area = _knowledge
			.Enemy.Where(a =>
				!a.IsPlace
				&& UnitFacts.Distance(position, a.Center) <= reach
				&& !_placeStrikes.ContainsKey(a.Name)
				&& IsEarlyStrikeArea(game, position, a)
			)
			.MinBy(a => UnitFacts.Distance(position, a.Center));
		if (area is null)
			return null;

		_placeStrikes[area.Name] = UnitFacts.Now(game);
		return area.Center;
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

	// Until which frame an enemy strike most likely stays in the air: each enemy attacker or bomber found flying in the
	// last 1200 frames flies home to the nearest carrier found, at 2.2 a tick, or for 600 frames when none is.
	private int EnemyStrikeAirborneUntil(Nspw game)
	{
		var carriers = _contacts
			.All.Where(c =>
				c.Category == UnitCategory.Ship
				&& UnitFacts.IsDeck(c.Kind)
				&& game.Units[c.Unit].IsUsed
			)
			.ToList();
		return _contacts
			.All.Where(c =>
				c.Category == UnitCategory.Plane
				&& UnitFacts.IsStrikePlane(c.Kind)
				&& c.IsFlying
				&& UnitFacts.Now(game) - c.Frame < 1200
				&& game.Units[c.Unit].IsUsed
			)
			.Select(c =>
				c.Frame
				+ (
					carriers.Count == 0
						? 600
						: (int)(carriers.Min(k => UnitFacts.Distance(k.Position, c.Position)) / 4.4)
							+ 100
				)
			)
			.DefaultIfEmpty(0)
			.Max();
	}

	// What a deck's strike about to take off at `target` does, chosen by looking ahead (LookAhead): goes at it, or at one
	// of the two nearest other targets the victory conditions count that it can reach (ChooseTarget), or waits
	// Parameters.StrikeWaitFrames, as a copy of the player does, given the choice (_forcedStrikes); the first that
	// scores best, going at `target` if none does better. The unit to go at, or 0 to wait.
	private int LookAheadStrike(Nspw game, int deck, FireKind weapon, double reach, Contact target)
	{
		var position = game.Units[deck].Position;
		var others = _contacts
			.All.Where(c =>
				c.Unit != target.Unit
				&& c.Category == UnitCategory.Ship
				&& IsCounted(c.Kind)
				&& game.Units[c.Unit].IsUsed
				&& IsRemembered(game, c)
				&& CanAttack(weapon, c)
				&& UnitFacts.Distance(position, WhereNow(game, c, 0))
					<= reach * Parameters.TargetReach
			)
			.OrderBy(c => UnitFacts.Distance(position, WhereNow(game, c, 0)))
			.Take(2)
			.Select(c => c.Unit);
		int[] candidates = [target.Unit, .. others, 0];
		var until = UnitFacts.Now(game) + Parameters.StrikeWaitFrames;
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		var scores = LookAhead(
			game,
			candidates.Length,
			(player, candidate) => player._forcedStrikes[deck] = (candidates[candidate], until),
			Parameters.LookaheadStrikeFrames
		);
		var best = Array.IndexOf(scores, scores.Max());
		Log?.Invoke(
			$"{UnitFacts.Now(game)}: lookahead has the strike of {game.Units[deck].Kind} {deck} {(candidates[best] == 0 ? "wait" : $"go at {game.Units[candidates[best]].Kind} {candidates[best]}")}, in {stopwatch.ElapsedMilliseconds} ms: {string.Join(" ", candidates.Zip(scores, (c, v) => $"{c}:{v:F1}"))}"
		);
		return scores[best] > scores[0] ? candidates[best] : target.Unit;
	}

	// A deck's ready attackers and bombers, with escorting fighters, against the best enemy found within reach, once
	// three of its armed planes are ready, or all it has, and none will be ready soon, and no enemy strike is over it;
	// or, while another deck's strike has just gone, with whatever is ready, at the same target or place, so that they
	// arrive together: a strike that takes off into the enemy's is broken up as it climbs, so it waits for the raid to
	// pass, and goes while the enemy's planes are coming home. While hunting carriers, it goes with no fighters, as the
	// computer's fly only with its strikes (ComputerPlayer.md, Air strikes), so that ours are better kept to meet those;
	// and it waits until the enemy's planes would be home as it gets there: a strike that meets them over their carrier
	// is broken up by their gunners and fighters, while the planes that have landed are serviced, and fight no more for
	// a while.
	private bool TryStrike(Nspw game, List<int> decks)
	{
		if (HoldsStrikes(game))
			return false;

		foreach (var deck in decks)
		{
			if (!MayLaunch(game, deck) || IsUnderRaid(game, deck))
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
					&& UnitFacts.Now(game) - w.Frame < WaveFrames
					&& CanAttack(weapon, w.Target)
					&& game.Units[w.Target.Unit].IsUsed
					&& UnitFacts.Distance(game.Units[deck].Position, w.Target.Position)
						<= StrikeRange
						? w.Target
						: null;
				var reach = ready.Count == 0 ? 0 : ready.Min(n => Reach(ref game.Units[n]));
				var pointWave =
					_pointWave is { } p
					&& UnitFacts.Now(game) - p.Frame < PointWaveFrames
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
							|| (
								ready.Count < Parameters.MinStrike
								&& ready.Count < StrikePlanesOf(game, deck)
							)
							|| ready.Count
								< Math.Ceiling(StrikePlanesOf(game, deck) * Parameters.StrikeShare)
						)
					)
				)
					continue;

				var target = wave ?? ChooseTarget(game, deck, weapon, reach);
				if (
					StaysHome
					&& target is not null
					&& UnitFacts.Distance(target.Position, game.Units[deck].Position) > DefenseRange
					&& !IsNearHeldPlace(game, target.Position)
				)
					target = null;

				// A formation flies at about 60% of its planes' speed, two ticks a frame.
				var arrival = target is null
					? 0
					: UnitFacts.Now(game)
						+ (
							UnitFacts.Distance(game.Units[deck].Position, target.Position)
							/ (game.Units[ready[0]].MaxSpeed * 2 * 0.6)
						);
				if (HuntsCarriers && EnemyStrikeAirborneUntil(game) > arrival && target is not null)
					continue;

				// A choice looked ahead on: a wait, until its time is up, or a target to go at; or, with
				// Parameters.LookaheadStrike, a strike about to go at a target the victory conditions count looks ahead,
				// again once a wait is up.
				var forced = _forcedStrikes.GetValueOrDefault(deck, (Target: -1, Until: 0));
				if (forced.Target == 0 && UnitFacts.Now(game) < forced.Until)
					continue;

				_forcedStrikes.Remove(deck);
				if (
					forced.Target > 0
					&& game.Units[forced.Target].IsUsed
					&& _contacts.Find(forced.Target) is { } chosen
				)
				{
					target = chosen;
				}
				else if (
					forced.Target <= 0
					&& Parameters.LookaheadStrike
					&& !_isFork
					&& HuntsCarriers
					&& wave is null
					&& target is not null
					&& IsCounted(target.Kind)
				)
				{
					var choice = LookAheadStrike(game, deck, weapon, reach, target);
					if (choice == 0)
					{
						_forcedStrikes[deck] = (
							0,
							UnitFacts.Now(game) + Parameters.StrikeWaitFrames
						);
						continue;
					}

					target = _contacts.Find(choice) ?? target;
				}

				var origin =
					target is null && !StaysHome
						? pointWave
							?? (
								HuntsCarriers
									? ExpectedCarriersToStrike(game, deck, reach)
									: EnemyStrikeOrigin(game)
							)
							?? PlaceToStrike(game, deck, reach)
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
				var escorts =
					HuntsCarriers && !Parameters.EscortHunts
						? []
						: fighters.Take(
							fighters.Count
								- (int)Math.Ceiling(fighters.Count * Parameters.FightersKeptBack)
						);
				ShowDeck(deck);
				SelectPlanes(ready[0], ready.Skip(1).Concat(escorts));
				if (target is not null)
				{
					if (wave is null)
						_wave = (target, UnitFacts.Now(game));
					LaunchStrike(game, ready[0], target, game.Units[deck].Position);
				}
				else
				{
					_lastBlindStrike = UnitFacts.Now(game);
					if (pointWave is null)
						_pointWave = (origin!.Value, UnitFacts.Now(game));
					LaunchToward(ready[0], origin!.Value, game.Units[deck].Position, reach);
				}

				return true;
			}
		}

		return false;
	}

	// A deck's unarmed attackers and bombers armed: torpedoes for attackers (bombs with Parameters.BombGround, when the
	// victory counts ground units), bombs for bombers.
	private bool TryArm(Nspw game, List<int> decks)
	{
		foreach (var deck in decks)
		{
			if (game.Units[deck].IsSupplying)
				continue;

			foreach (
				var (kind, item) in new[]
				{
					(
						UnitKind.Attacker,
						(Parameters.BombGround && _knowledge.Counted.Any(UnitFacts.IsGround))
						|| RaidedBase(game) != 0
							? CombatMenuItem.ArmWithBomb
							: CombatMenuItem.ArmWithTorpedo
					),
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
						$"{UnitFacts.Now(g)}: {unarmed.Count} {kind} on {deck} armed with {item}"
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

	// When the victory conditions count enemy planes, and the player does not win by holding on, which its fighters
	// defend at home, a deck's ready fighters but one fly to the nearest area where the briefing expects them, within
	// their reach, every so often, and wait there, as they see only 500 around: TryDirect gives them the planes they
	// find.
	private bool TryHunt(Nspw game, List<int> decks)
	{
		if (
			StaysHome
			|| HoldsFighters(game)
			|| !_knowledge.Counted.Any(k =>
				k is UnitKind.Bomber or UnitKind.Attacker or UnitKind.Fighter
			)
		)
			return false;

		foreach (var deck in decks)
		{
			if (
				!MayLaunch(game, deck)
				|| UnitFacts.Now(game) - _huntedFrom.GetValueOrDefault(deck, -HuntFrames)
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

			_huntedFrom[deck] = UnitFacts.Now(game);
			var group = fighters.Take(fighters.Count - 1).ToList();
			ShowDeck(deck);
			SelectPlanes(group[0], group.Skip(1));
			LaunchToward(group[0], area.Center, position, reach);
			return true;
		}

		return false;
	}

	// A carrier, or an air base where the player holds on, keeps a fighter in the air above it, toward where the enemy
	// most likely is, while it has two ready: it sees the enemy's strikes coming, and fights them, until its fuel brings
	// it home (at 30) and another goes up.
	private bool TryPatrol(Nspw game, List<int> decks)
	{
		if (HoldsFighters(game))
			return false;

		foreach (var deck in decks)
		{
			ref var unit = ref game.Units[deck];
			if (
				(unit.Category != UnitCategory.Ship && !_knowledge.HoldsOn)
				|| !UnitFacts.CanLaunch(ref unit)
			)
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
					$"{UnitFacts.Now(g)}: Fighter {fighter} patrols at ({point.X:F0}, {point.Y:F0})"
				);
				return true;
			});
			return true;
		}

		return false;
	}

	// Whether an attacker could drop its torpedo on a ship now, as the game lets it (PlaneDropTorpedo): the point where
	// the ship would be after the torpedo's run, at its speed and heading, is 100 to 220 away and within 45° of the
	// plane's heading, and the sea lets a torpedo be dropped there and run (UnitFacts.CanDropTorpedoAt, IsSeaRun).
	private static bool CanDropTorpedoNow(Nspw game, int plane, int ship)
	{
		ref var unit = ref game.Units[plane];
		ref var target = ref game.Units[ship];
		var ahead = target.Speed * (all_head.AIR_TPD_LOS_DSTC / all_head.AIR_TPD_SPD);
		var aim = new WorldPosition(
			target.Position.X + (target.Direction.Cos() * ahead),
			target.Position.Y + (target.Direction.Sin() * ahead)
		);
		var distance = UnitFacts.Distance(unit.Position, aim);
		var off = Math.Abs(
			((BearingTo(unit.Position, aim) - unit.Direction.Degrees + 540) % 360) - 180
		);
		return distance is >= 100 and <= all_head.AIR_TPD_LOS_DSTC
			&& off <= 45
			&& UnitFacts.CanDropTorpedoAt(game, unit.Position)
			&& IsSeaRun(game, aim, BearingTo(aim, unit.Position));
	}

	// With Parameters.RetargetWhenClose, an attacker with its torpedo that has been within 400 of the ship it attacks for
	// 60 frames without being able to drop on it (CanDropTorpedoNow), circling to come round, takes as its target another
	// enemy ship found that it can drop on now, one the victory counts first, then the one whose aim is nearest the
	// middle of the drop's reach; at most once every 50 frames a plane: coming round costs time over the escorts' guns.
	private bool TryRetargetWhenClose(Nspw game)
	{
		if (!Parameters.RetargetWhenClose)
			return false;

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var plane = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| plane.Kind != UnitKind.Attacker
				|| plane.Weapon != FireKind.Torpedo
				|| plane.Ammo == 0
				|| plane.PlaneState != UnitState.Flying
				|| plane.Mode == UnitMode.Return
				|| plane.Target == 0
				|| !game.Units[plane.Target].IsUsed
				|| game.Units[plane.Target].Category != UnitCategory.Ship
				|| UnitFacts.Distance(plane.Position, game.Units[plane.Target].Position) > 400
				|| CanDropTorpedoNow(game, m, plane.Target)
			)
			{
				_closeSince.Remove(m);
				continue;
			}

			_closeSince.TryAdd(m, UnitFacts.Now(game));
			if (
				UnitFacts.Now(game) - _closeSince[m] < 60
				|| UnitFacts.Now(game) - _retargeted.GetValueOrDefault(m, int.MinValue / 2) < 50
			)
				continue;

			var current = plane.Target;
			var position = plane.Position;
			var other = Enumerable
				.Range(1, game.MaxUnitId)
				.Where(n =>
					n != current
					&& game.Units[n].IsUsed
					&& game.Units[n].Side != game.LocalSide
					&& game.Units[n].Category == UnitCategory.Ship
					&& !UnitFacts.IsBase(game.Units[n].Kind)
					&& game.Units[n].IsFound
					&& !(game.Units[n].Kind == UnitKind.Submarine && game.Units[n].IsSubmerged)
					&& CanDropTorpedoNow(game, m, n)
				)
				.OrderBy(n => IsCounted(game.Units[n].Kind) ? 0 : 1)
				.ThenBy(n => Math.Abs(UnitFacts.Distance(position, game.Units[n].Position) - 160))
				.FirstOrDefault();
			if (other == 0)
				continue;

			_retargeted[m] = UnitFacts.Now(game);
			var attacker = m;
			SelectFlying(m, "retargeting");
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != attacker)
					return GiveUp(g, $"retargeting {attacker}: selected {g.SelectedUnit}");
				if (g.Units[other].IsUsed && g.Units[other].IsFound)
				{
					g.Commands.ClickUnit(other);
					Log?.Invoke(
						$"{UnitFacts.Now(g)}: {g.Units[attacker].Kind} {attacker}, too close to {g.Units[current].Kind} {current}, aims at {g.Units[other].Kind} {other}"
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
