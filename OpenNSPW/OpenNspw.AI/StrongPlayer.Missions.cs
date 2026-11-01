namespace OpenNspw.AI;

// The strong player's missions: the plans of the scenarios whose goals the general tasks do not reach, played before
// them (TryMission), which may hold the general strikes back (HoldsStrikes) and the carriers' moves (HoldsFleet).
public sealed partial class StrongPlayer
{
	// Scenario 300: a corner of the map beyond x < -8400 and y < -6000, where the bombers gather (ComputerPlayer.md,
	// decision).
	private static readonly WorldPosition DoolittleCorner = new(-9000, -6700);

	// Scenario 300: the share of the bombers' range they may fly, over Tokyo and on to the corner.
	private const double DoolittleRangeShare = 0.85;

	private readonly HashSet<int> _seenCities = [];
	private readonly HashSet<int> _placeTargets = [];
	private int _missionOrdered = -1000;

	// Scenario 205: the point the rescue flies by each way, north-east of Guadalcanal, clear of the enemy fighters that
	// guard the waters south of it.
	private static readonly WorldPosition RescueWaypoint = new(2500, 1500);

	private readonly List<int> _rescueStops = [];
	private int _rescueBomber;
	private int _rescueLeg;
	private bool _hasDefendedBougainville;
	private int _defenders;

	private bool IsMissionOrderDue(Nspw game, int frames) =>
		UnitFacts.Now(game) - _missionOrdered >= frames;

	// The player's first unit of a kind, or 0.
	private static int FirstOwn(Nspw game, UnitKind kind)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (IsOwn(game, m) && game.Units[m].Kind == kind)
				return m;
		}

		return 0;
	}

	// The enemy cities destroyed: found once, gone since.
	private int CitiesDestroyed(Nspw game)
	{
		foreach (var contact in _contacts.All.Where(c => c.Kind == UnitKind.City))
		{
			_seenCities.Add(contact.Unit);
		}

		return _seenCities.Count(m => !game.Units[m].IsUsed || game.Units[m].Kind != UnitKind.City);
	}

	// Scenario 300: whether the carrier is where its bombers launch: their fuel carries them over Tokyo and on to the
	// corner, or the carrier's own fuel, 9 at the start, is all but spent.
	private bool IsDoolittleLaunching(Nspw game)
	{
		var carrier = FirstOwn(game, UnitKind.Carrier);
		var bomber = FirstOwn(game, UnitKind.Bomber);
		if (carrier == 0 || bomber == 0 || game.Units[carrier].Fuel <= 1)
			return true;

		ref var plane = ref game.Units[bomber];
		var range = plane.MaxSpeed * plane.FuelInterval * plane.Fuel;
		var tokyo = ScenarioKnowledge.Tokyo.Center;
		return UnitFacts.Distance(game.Units[carrier].Position, tokyo)
				+ UnitFacts.Distance(tokyo, DoolittleCorner)
			<= range * DoolittleRangeShare;
	}

	// Scenario 300: the carrier sails toward Tokyo until its bombers can reach the corner after it, or its fuel is
	// spent; then, two cities destroyed, each bomber, in the air or on the deck, flies to the corner.
	private bool TryDoolittle(Nspw game)
	{
		if (CitiesDestroyed(game) >= 2)
		{
			if (!IsMissionOrderDue(game, 60))
				return false;

			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				ref var bomber = ref game.Units[m];
				if (
					!IsOwn(game, m)
					|| bomber.Kind != UnitKind.Bomber
					|| bomber.GroupLeader != 0
					|| UnitFacts.Distance(UnitFacts.FinalDestination(ref bomber), DoolittleCorner)
						< 300
				)
					continue;

				_missionOrdered = UnitFacts.Now(game);
				var plane = m;
				if (bomber.PlaneState == UnitState.Parked)
				{
					if (
						!UnitFacts.IsParked(ref bomber)
						|| !UnitFacts.CanLaunch(ref game.Units[bomber.Carrier])
						|| bomber.ReloadTime > 0
					)
						continue;

					ShowDeck(bomber.Carrier);
					SelectPlanes(m, []);
				}
				else
				{
					SelectAlone(m, "gathering");
					_clicks.Enqueue(g =>
					{
						if (g.SelectedUnit == plane && g.Units[plane].Mode == UnitMode.Return)
							g.Commands.ChooseCombatMenuItem(CombatMenuItem.Move);
					});
					_clicks.Wait(8);
				}

				SendSelected(m, DoolittleCorner, "gathers at");
				return true;
			}

			return false;
		}

		if (IsDoolittleLaunching(game) || !IsMissionOrderDue(game, 300))
			return false;

		var carrier = FirstOwn(game, UnitKind.Carrier);
		var tokyo = ScenarioKnowledge.Tokyo.Center;
		if (UnitFacts.Distance(UnitFacts.FinalDestination(ref game.Units[carrier]), tokyo) < 3000)
			return false;

		_missionOrdered = UnitFacts.Now(game);
		SelectAlone(carrier, "sailing to launch");
		return SailSelected(
				game,
				carrier,
				PointAt(tokyo, BearingTo(tokyo, game.Units[carrier].Position), 2500),
				"sails toward Tokyo, to"
			) || true;
	}

	// The player's air base nearest a point, or 0.
	private static int NearestOwnAirBase(Nspw game, WorldPosition position)
	{
		var best = 0;
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (
				IsOwn(game, m)
				&& game.Units[m].Kind == UnitKind.AirBase
				&& (
					best == 0
					|| UnitFacts.Distance(game.Units[m].Position, position)
						< UnitFacts.Distance(game.Units[best].Position, position)
				)
			)
				best = m;
		}

		return best;
	}

	// Scenario 205: the strategic bomber that flies the rescue, the first of Nouméa's.
	private int RescueBomber(Nspw game)
	{
		if (_rescueBomber == 0 || !IsOwn(game, _rescueBomber))
			_rescueBomber = FirstOwn(game, UnitKind.Bomber);
		return _rescueBomber;
	}

	// Gives the selected flying plane a base, and brings it home there: a click on the base (SelectFriendlyTarget),
	// then Return.
	private void LandSelectedAt(int plane, int airBase)
	{
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != plane)
				return GiveUp(g, $"landing {plane} at {airBase}: selected {g.SelectedUnit}");
			g.Commands.ClickUnit(airBase);
			return true;
		});
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != plane || g.Units[plane].Carrier != airBase)
				return GiveUp(
					g,
					$"landing {plane} at {airBase}: selected {g.SelectedUnit}, based at {g.Units[plane].Carrier}"
				);
			g.Commands.ChooseCombatMenuItem(CombatMenuItem.Return);
			Log?.Invoke(
				$"{UnitFacts.Now(g)}: {g.Units[plane].Kind} {plane} lands at {g.Units[airBase].Kind} {airBase}"
			);
			return true;
		});
		_clicks.Wait(8);
		Deselect();
	}

	// Scenario 205: the airfields the rescue lands at in turn: Bougainville's, then home, Nouméa's.
	private List<int> RescueStops(Nspw game)
	{
		if (_rescueStops.Count == 0)
			_rescueStops.AddRange([
				NearestOwnAirBase(game, ScenarioKnowledge.Bougainville.Center),
				game.Units[RescueBomber(game)].Carrier,
			]);
		return _rescueStops;
	}

	// Scenario 205: Guadalcanal's ready fighters fly to Bougainville's airfield, once, and are based there, to defend
	// it from the raids that would bring it under a fifth of its HP before the bomber lands, which loses the scenario.
	private bool TryDefendBougainville(Nspw game)
	{
		if (_hasDefendedBougainville)
			return false;

		var guadalcanal = NearestOwnAirBase(game, ScenarioKnowledge.Guadalcanal.Center);
		var bougainville = NearestOwnAirBase(game, ScenarioKnowledge.Bougainville.Center);
		if (
			guadalcanal == 0
			|| bougainville == 0
			|| guadalcanal == bougainville
			|| !UnitFacts.CanLaunch(ref game.Units[guadalcanal])
		)
			return false;

		var fighters = ParkedOn(
			game,
			guadalcanal,
			n => game.Units[n].Kind == UnitKind.Fighter && UnitFacts.IsReady(ref game.Units[n])
		);
		if (fighters.Count == 0)
			return false;

		_hasDefendedBougainville = true;
		_defenders = fighters[0];
		ShowDeck(guadalcanal);
		SelectPlanes(fighters[0], fighters.Skip(1));
		SendSelected(fighters[0], game.Units[bougainville].Position, "defends");
		return true;
	}

	// Scenario 205: the defenders, once in the air, are based at Bougainville's airfield.
	private bool TryBaseDefenders(Nspw game)
	{
		var bougainville = NearestOwnAirBase(game, ScenarioKnowledge.Bougainville.Center);
		if (
			_defenders == 0
			|| !IsOwn(game, _defenders)
			|| game.Units[_defenders].PlaneState != UnitState.Flying
			|| game.Units[_defenders].Carrier == bougainville
		)
			return false;

		var leader = _defenders;
		_defenders = 0;
		SelectAlone(leader, "basing");
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit == leader)
				g.Commands.ClickUnit(bougainville);
		});
		Deselect();
		return true;
	}

	// Scenario 205: Nouméa's bomber lands at Bougainville's airfield, and back at Nouméa (ComputerPlayer.md, decision):
	// parked, once serviced, it takes off for the next airfield, by a point clear of the enemy's fighters, with the
	// ready fighters there as escort; in the air, it and its escort are given that airfield as their base, so that the
	// fighters, short of fuel, come down there too; and once within 1000 of it, the bomber comes down (Return), not
	// before, as returning planes leave their formation.
	private bool TryRescue(Nspw game)
	{
		var bomber = RescueBomber(game);
		if (TryDefendBougainville(game) || TryBaseDefenders(game))
			return true;

		if (bomber == 0 || !IsMissionOrderDue(game, 20))
			return false;

		ref var plane = ref game.Units[bomber];
		var stops = RescueStops(game);
		if (
			plane.PlaneState == UnitState.Parked
			&& _rescueLeg < stops.Count
			&& plane.Carrier == stops[_rescueLeg]
		)
			_rescueLeg++;

		if (_rescueLeg >= stops.Count || !game.Units[stops[_rescueLeg]].IsUsed)
			return false;

		var next = stops[_rescueLeg];
		if (plane.PlaneState == UnitState.Parked)
		{
			var deck = plane.Carrier;
			if (
				!UnitFacts.IsParked(ref plane)
				|| plane.ReloadTime > 0
				|| !UnitFacts.CanLaunch(ref game.Units[deck])
			)
				return false;

			_missionOrdered = UnitFacts.Now(game);
			var escorts = ParkedOn(
					game,
					deck,
					n =>
						game.Units[n].Kind == UnitKind.Fighter
						&& UnitFacts.IsReady(ref game.Units[n])
				)
				.Take(6);
			ShowDeck(deck);
			SelectPlanes(bomber, escorts);
			SendSelected(bomber, RescueWaypoint, "flies the rescue by");
			return true;
		}

		if (
			UnitFacts.Distance(plane.Position, RescueWaypoint) <= 300
			&& UnitFacts.Distance(UnitFacts.FinalDestination(ref plane), game.Units[next].Position)
				> 300
		)
		{
			_missionOrdered = UnitFacts.Now(game);
			SelectAlone(bomber, "rescuing");
			SendSelected(bomber, game.Units[next].Position, "flies the rescue on to");
			return true;
		}

		var isNear = UnitFacts.Distance(plane.Position, game.Units[next].Position) <= 1000;
		if (plane.Carrier == next && (!isNear || plane.Mode == UnitMode.Return))
			return false;

		_missionOrdered = UnitFacts.Now(game);
		SelectAlone(bomber, "rescuing");
		if (plane.Carrier != next)
		{
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit == bomber)
					g.Commands.ClickUnit(next);
			});
			Deselect();
		}
		else
		{
			LandSelectedAt(bomber, next);
		}

		return true;
	}

	// Whether a unit is kept for the mission, out of the general tasks.
	private bool IsReserved(Nspw game, int m) =>
		game.ScenarioNumber switch
		{
			205 => m == RescueBomber(game),
			_ => false,
		};

	// Whether the places the briefing names are destroyed: what the victory conditions count there has been found, and is
	// gone, all of it.
	private bool ArePlacesDestroyed(Nspw game)
	{
		foreach (
			var contact in _contacts.All.Where(c =>
				IsCounted(c.Kind)
				&& _knowledge.Enemy.Any(a =>
					a.IsPlace && UnitFacts.Distance(c.Position, a.Center) <= a.Radius + 800
				)
			)
		)
		{
			_placeTargets.Add(contact.Unit);
		}

		return _placeTargets.Count > 0 && _placeTargets.All(m => !game.Units[m].IsUsed);
	}

	// Once the places the briefing names are destroyed, every ship of the player's that leads or sails alone sails back to
	// where the briefing wants it at the end, its formation with it.
	private bool TryReturnHome(Nspw game)
	{
		if (
			_knowledge.Home is not { } home
			|| !ArePlacesDestroyed(game)
			|| !IsMissionOrderDue(game, 40)
		)
			return false;

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var ship = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| ship.Category != UnitCategory.Ship
				|| UnitFacts.IsBase(ship.Kind)
				|| ship.GroupLeader != 0
				|| UnitFacts.IsAnchored(ref ship)
				|| UnitFacts.Distance(UnitFacts.FinalDestination(ref ship), home.Center)
					< home.Radius * 0.6
			)
				continue;

			_missionOrdered = UnitFacts.Now(game);
			SelectAlone(m, "coming home");
			var point = PointAt(home.Center, m * 47.0, Math.Min(home.Radius * 0.5, 80.0 * (m % 6)));
			return SailSelected(game, m, point, "comes home to") || true;
		}

		return false;
	}

	// The scenario's mission, if it has one: true if it gave an order.
	private bool TryMission(Nspw game) =>
		game.ScenarioNumber switch
		{
			300 => TryDoolittle(game),
			205 => TryRescue(game),
			_ => TryReturnHome(game),
		};

	// Whether the mission holds the general strikes back.
	private bool HoldsStrikes(Nspw game) =>
		game.ScenarioNumber switch
		{
			300 => !IsDoolittleLaunching(game) || CitiesDestroyed(game) >= 2,
			_ => _knowledge.Home is not null && ArePlacesDestroyed(game),
		};

	// Whether the mission holds the scouts back.
	private bool HoldsScouts(Nspw game) =>
		game.ScenarioNumber switch
		{
			300 or 205 => true,
			_ => false,
		};

	// Whether the mission holds the carriers' general moves back.
	private bool HoldsFleet(Nspw game) =>
		game.ScenarioNumber switch
		{
			300 => true,
			_ => _knowledge.Home is not null && ArePlacesDestroyed(game),
		};
}
