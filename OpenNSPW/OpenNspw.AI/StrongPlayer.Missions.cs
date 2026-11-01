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
	private int _missionOrdered = -1000;

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

	// Scenario 300: the carrier sails toward Tokyo until its bombers can reach the corner after it, or its fuel is spent;
	// then, two cities destroyed, each bomber, in the air or on the deck, flies to the corner.
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

	// The scenario's mission, if it has one: true if it gave an order.
	private bool TryMission(Nspw game) =>
		game.ScenarioNumber switch
		{
			300 => TryDoolittle(game),
			_ => false,
		};

	// Whether the mission holds the general strikes back.
	private bool HoldsStrikes(Nspw game) =>
		game.ScenarioNumber switch
		{
			300 => !IsDoolittleLaunching(game) || CitiesDestroyed(game) >= 2,
			_ => false,
		};

	// Whether the mission holds the scouts back.
	private bool HoldsScouts(Nspw game) =>
		game.ScenarioNumber switch
		{
			300 => true,
			_ => false,
		};

	// Whether the mission holds the carriers' general moves back.
	private bool HoldsFleet(Nspw game) =>
		game.ScenarioNumber switch
		{
			300 => true,
			_ => false,
		};
}
