namespace OpenNspw.AI;

// The strong player's defense, where it wins by holding on: the places it holds are where its bases, cities and
// anchored ships stand, as it sees them; its fighters patrol over them and meet the raids near them, its strikes hit
// the enemy near them, and its warships keep station there rather than chase the enemy away from them.
public sealed partial class StrongPlayer
{
	// How far around a place held enemy planes are intercepted, and enemy ships struck and fought.
	private const double HeldPlaceRange = 3500;

	// How far from the front air base another must be for its planes to move up: nearer, they defend the front from
	// where they are.
	private const double RebaseFrom = 4000;

	// The frames for which the places held are kept before they are found again.
	private const int HeldPlaceFrames = 500;

	private readonly Dictionary<WorldPosition, int> _guardOrdered = [];
	private readonly Dictionary<int, int> _rebasing = [];
	private int _rebased = -1000;
	private List<WorldPosition> _heldPlaces = [];
	private int _heldPlacesFrame = -HeldPlaceFrames;

	private static bool IsHeld(ref Unit unit) =>
		unit.Kind
			is UnitKind.AirBase
				or UnitKind.NavalBase
				or UnitKind.City
				or UnitKind.InfantryBase
				or UnitKind.Pillboxes
				or UnitKind.Fortress
		|| UnitFacts.IsAnchored(ref unit);

	// The places the player holds: its bases, cities and anchored ships, gathered within 1000 of each other, each place
	// where the first of them stands; none where it does not win by holding on.
	private List<WorldPosition> HeldPlaces(Nspw game)
	{
		if (!_knowledge.HoldsOn)
			return [];

		if (UnitFacts.Now(game) - _heldPlacesFrame < HeldPlaceFrames)
			return _heldPlaces;

		_heldPlacesFrame = UnitFacts.Now(game);
		_heldPlaces = [];
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			var position = game.Units[m].Position;
			if (
				IsOwn(game, m)
				&& IsHeld(ref game.Units[m])
				&& _heldPlaces.All(p => UnitFacts.Distance(p, position) > 1000)
			)
				_heldPlaces.Add(position);
		}

		return _heldPlaces;
	}

	// Whether a point is near a place the player holds.
	private bool IsNearHeldPlace(Nspw game, WorldPosition position) =>
		HeldPlaces(game).Any(p => UnitFacts.Distance(p, position) <= HeldPlaceRange);

	// The place held nearest a point, or null.
	private WorldPosition? NearestHeldPlace(Nspw game, WorldPosition position) =>
		HeldPlaces(game) is { Count: > 0 } places
			? places.MinBy(p => UnitFacts.Distance(p, position))
			: null;

	// Each place held keeps a fighter in the air above it, from the nearest deck whose fighters reach it, while that
	// deck has two ready.
	private bool TryGuardHeldPlaces(Nspw game, List<int> decks)
	{
		foreach (var place in HeldPlaces(game))
		{
			var isGuarded = false;
			for (var n = 1; n <= game.MaxUnitId && !isGuarded; n++)
			{
				ref var plane = ref game.Units[n];
				isGuarded =
					IsOwn(game, n)
					&& plane.Kind == UnitKind.Fighter
					&& plane.PlaneState == UnitState.Flying
					&& plane.Mode != UnitMode.Return
					&& UnitFacts.Distance(UnitFacts.FinalDestination(ref plane), place) <= 1500;
			}

			if (
				isGuarded
				|| UnitFacts.Now(game) - _guardOrdered.GetValueOrDefault(place, -300) < 300
			)
				continue;

			foreach (
				var deck in decks.OrderBy(d => UnitFacts.Distance(game.Units[d].Position, place))
			)
			{
				var fighters = ParkedOn(
					game,
					deck,
					n =>
						game.Units[n].Kind == UnitKind.Fighter
						&& UnitFacts.IsReady(ref game.Units[n])
				);
				if (
					fighters.Count < 2
					|| !UnitFacts.CanLaunch(ref game.Units[deck])
					|| UnitFacts.Distance(game.Units[deck].Position, place)
						> Reach(ref game.Units[fighters[0]])
				)
					continue;

				_guardOrdered[place] = UnitFacts.Now(game);
				ShowDeck(deck);
				SelectPlanes(fighters[0], []);
				SendSelected(fighters[0], place, "guards");
				return true;
			}
		}

		return false;
	}

	// The player's air base nearest where the briefing expects the enemy, which the raids reach first, or 0.
	private int FrontAirBase(Nspw game)
	{
		if (_knowledge.Enemy.Count == 0)
			return 0;

		var front = 0;
		var nearest = double.MaxValue;
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (!IsOwn(game, m) || game.Units[m].Kind != UnitKind.AirBase)
				continue;

			var position = game.Units[m].Position;
			var distance = _knowledge.Enemy.Min(a => UnitFacts.Distance(a.Center, position));
			if (distance < nearest)
			{
				front = m;
				nearest = distance;
			}
		}

		return front;
	}

	// Where the player holds on, the ready fighters and attackers of its air bases 4000 or more away fly to the one
	// nearest the enemy, as many as it has room for (Capacity), within 70% of their range, to defend it from there: in
	// the air, they are given it as their base, and come down there (Return).
	private bool TryRebase(Nspw game, List<int> decks)
	{
		if (!_knowledge.HoldsOn || FrontAirBase(game) is not (> 0 and var front))
			return false;

		foreach (var (leader, airBase) in _rebasing)
		{
			ref var plane = ref game.Units[leader];
			if (!IsOwn(game, leader) || !game.Units[airBase].IsUsed || plane.Carrier == airBase)
			{
				_rebasing.Remove(leader);
				return false;
			}

			if (plane.PlaneState != UnitState.Flying)
				continue;

			_rebasing.Remove(leader);
			SelectAlone(leader, "rebasing");
			LandSelectedAt(leader, airBase);
			return true;
		}

		ref var frontBase = ref game.Units[front];
		var room = frontBase.Capacity - frontBase.PlaneCount;
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var plane = ref game.Units[n];
			if (
				IsOwn(game, n)
				&& plane.Category == UnitCategory.Plane
				&& plane.PlaneState == UnitState.Flying
				&& (
					plane.Carrier == front
					|| UnitFacts.Distance(UnitFacts.FinalDestination(ref plane), frontBase.Position)
						< 300
				)
			)
				room--;
		}

		if (room <= 0 || UnitFacts.Now(game) - _rebased < 300)
			return false;

		foreach (var deck in decks)
		{
			if (
				deck == front
				|| game.Units[deck].Kind != UnitKind.AirBase
				|| !UnitFacts.CanLaunch(ref game.Units[deck])
			)
				continue;

			var distance = UnitFacts.Distance(game.Units[deck].Position, frontBase.Position);
			if (distance < RebaseFrom)
				continue;

			var planes = ParkedOn(
					game,
					deck,
					n =>
						game.Units[n].Kind is UnitKind.Fighter or UnitKind.Attacker
						&& game.Units[n].ReloadTime == 0
						&& game.Units[n].MaxSpeed
							* game.Units[n].FuelInterval
							* game.Units[n].Fuel
							* 0.7
							>= distance
				)
				.OrderBy(n => game.Units[n].Kind != UnitKind.Fighter)
				.Take(Math.Min(room, 8))
				.ToList();
			if (planes.Count == 0)
				continue;

			_rebased = UnitFacts.Now(game);
			var leader = planes[0];
			ShowDeck(deck);
			SelectPlanes(leader, planes.Skip(1));
			SendSelected(leader, frontBase.Position, $"and {planes.Count - 1} more rebase to");
			_clicks.Enqueue(g =>
			{
				if (IsOwn(g, leader))
					_rebasing[leader] = front;
			});
			return true;
		}

		return false;
	}
}
