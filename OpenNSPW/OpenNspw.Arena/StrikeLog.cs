namespace OpenNspw.Arena;

// What became of the player's strikes in a game, in all: the weapons its attackers and bombers dropped, the planes that
// turned home damaged or for fuel, their weapon dumped, and the ones lost; the HP the enemy's ships lost, to anything;
// the carriers each side lost; and the HP the player's carriers lost, to anything, which tells how well they are
// defended sooner than the carriers sunk; and the weapons dropped at enemy carriers, and the hits on them, whose ratio
// is the strikes' aim; and the player's planes of every kind lost with no fuel left, and the fighters among them.
internal sealed record StrikeCounts(
	int Drops,
	int TurnedBack,
	int OutOfFuel,
	int PlanesLost,
	int EnemyDamage,
	int EnemyCarriersSunk,
	int OwnCarriersSunk,
	int CarrierDamage,
	int CarrierDrops = 0,
	int CarrierHits = 0,
	int PlanesDry = 0,
	int FightersDry = 0
);

// What becomes of the player's attackers and bombers, each time one drops its weapon, turns home damaged or is lost,
// and each hit on an enemy ship, to see how the strikes fare: counted in every game, and written as it goes with the
// arena's --strikes. It reads the whole state: it is for the developer, not the player.
internal sealed class StrikeLog(bool writes)
{
	private readonly bool _writes = writes;
	private readonly Dictionary<
		int,
		(int Ammo, int HP, double Fuel, int Target, WorldPosition Position)
	> _planes = [];
	private readonly Dictionary<
		int,
		(UnitKind Kind, double Fuel, WorldPosition Position, int Carrier, string State)
	> _flying = [];
	private readonly Dictionary<int, int> _ships = [];
	private readonly HashSet<int> _carriers = [];
	private readonly Dictionary<int, Side> _sides = [];
	private int _drops;
	private int _turnedBack;
	private int _outOfFuel;
	private int _planesLost;
	private int _enemyDamage;
	private int _carrierDamage;
	private int _carrierDrops;
	private int _carrierHits;
	private int _planesDry;
	private int _fightersDry;
	private bool _started;

	private static double Distance(WorldPosition a, WorldPosition b) =>
		Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

	// The enemy units of a category nearest a point, within 800, each with its distance.
	private static string Near(Nspw game, WorldPosition position, Side side, UnitCategory category)
	{
		var near = new List<(int Unit, double Distance)>();
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var other = ref game.Units[n];
			if (
				other.IsUsed
				&& other.Side != side
				&& other.Category == category
				&& !(category == UnitCategory.Plane && other.PlaneState != UnitState.Flying)
				&& Distance(other.Position, position) <= 800
			)
				near.Add((n, Distance(other.Position, position)));
		}

		return string.Join(
			" ",
			near.OrderBy(t => t.Distance)
				.Take(4)
				.Select(t =>
					FormattableString.Invariant(
						$"{game.Units[t.Unit].Kind.ToString()[..2]}{t.Unit}@{t.Distance:F0}"
					)
				)
		);
	}

	private static bool IsCarrier(UnitKind kind) =>
		kind is UnitKind.Carrier or UnitKind.LightCarrier;

	private void WriteLine(string line)
	{
		if (_writes)
			Console.WriteLine(line);
	}

	// The counts so far, with the carriers seen at the start that are gone or sinking, at no HP: the battle may end
	// while one sinks.
	public StrikeCounts Counts(Nspw game)
	{
		var sunk = _carriers
			.Where(n =>
				!game.Units[n].IsUsed || !IsCarrier(game.Units[n].Kind) || game.Units[n].HP <= 0
			)
			.ToList();
		return new StrikeCounts(
			_drops,
			_turnedBack,
			_outOfFuel,
			_planesLost,
			_enemyDamage,
			sunk.Count(n => _sides[n] != game.LocalSide),
			sunk.Count(n => _sides[n] == game.LocalSide),
			_carrierDamage,
			_carrierDrops,
			_carrierHits,
			_planesDry,
			_fightersDry
		);
	}

	public void Write(Nspw game)
	{
		if (!_started)
		{
			_started = true;
			for (var n = 1; n <= game.MaxUnitId; n++)
			{
				if (game.Units[n].IsUsed && IsCarrier(game.Units[n].Kind))
				{
					_carriers.Add(n);
					_sides[n] = game.Units[n].Side;
				}
			}
		}

		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var unit = ref game.Units[n];

			// A plane whose fuel runs out slows to a stop and is lost (MoveUnit): one gone with no fuel left ran dry.
			if (!unit.IsUsed && _flying.Remove(n, out var flown) && flown.Fuel <= 1)
			{
				_planesDry++;
				if (flown.Kind == UnitKind.Fighter)
					_fightersDry++;
				var deck = game.Units[flown.Carrier];
				WriteLine(
					FormattableString.Invariant(
						$"STRIKE {game.Renderer.FrameCount} {flown.Kind} #{n} lost with no fuel, {(deck.IsUsed ? Distance(deck.Position, flown.Position) : -1):F0} from its deck {flown.Carrier} ({(deck.IsUsed ? $"{deck.HP}/{deck.MaxHP}, {deck.PlaneCount}/{deck.Capacity} aboard, {deck.PlanesToLaunch} to launch" : "gone")}), {flown.State}"
					)
				);
			}
			else if (
				unit.IsUsed
				&& unit.Side == game.LocalSide
				&& unit.Category == UnitCategory.Plane
				&& unit.PlaneState == UnitState.Flying
			)
				_flying[n] = (
					unit.Kind,
					unit.Fuel,
					unit.Position,
					unit.Carrier,
					$"{unit.Mode}, phase {unit.DeckPhase}, heading {unit.Direction.Degrees:F0}, deck heading {game.Units[unit.Carrier].Direction.Degrees:F0}, lock {game.Units[unit.Carrier].LandingLock}"
				);
			else
				_flying.Remove(n);

			if (!unit.IsUsed && _planes.Remove(n, out var gone))
			{
				_planesLost++;
				WriteLine(
					FormattableString.Invariant(
						$"STRIKE {game.Renderer.FrameCount} #{n} lost, ammo {gone.Ammo}, hp {gone.HP}, fuel {gone.Fuel:F0}, near [{Near(game, gone.Position, game.LocalSide, UnitCategory.Ship)}] [{Near(game, gone.Position, game.LocalSide, UnitCategory.Plane)}]"
					)
				);
				continue;
			}

			if (
				unit.IsUsed
				&& unit.Side == game.LocalSide
				&& (unit.Kind is UnitKind.Attacker or UnitKind.Bomber)
			)
			{
				var was = _planes.GetValueOrDefault(n);
				if (
					_planes.ContainsKey(n)
					&& was.Ammo > 0
					&& unit.Ammo < was.Ammo
					&& unit.PlaneState == UnitState.Flying
				)
				{
					var why =
						unit.HP <= unit.MaxHP * 0.7 ? $"turned home damaged, hp {unit.HP}"
						: unit.Fuel <= 20 ? "turned home for fuel"
						: "dropped";
					if (unit.HP <= unit.MaxHP * 0.7)
						_turnedBack++;
					else if (unit.Fuel <= 20)
						_outOfFuel++;
					else
					{
						_drops++;
						if (
							was.Target != 0
							&& game.Units[was.Target].IsUsed
							&& IsCarrier(game.Units[was.Target].Kind)
						)
							_carrierDrops++;
					}

					var target =
						was.Target != 0 && game.Units[was.Target].IsUsed
							? FormattableString.Invariant(
								$" at {game.Units[was.Target].Kind} {was.Target} {Distance(game.Units[was.Target].Position, unit.Position):F0} away"
							)
							: "";
					WriteLine(
						FormattableString.Invariant(
							$"STRIKE {game.Renderer.FrameCount} #{n} {unit.Weapon} {why}{target}, near [{Near(game, unit.Position, game.LocalSide, UnitCategory.Ship)}] [{Near(game, unit.Position, game.LocalSide, UnitCategory.Plane)}]"
						)
					);
				}

				_planes[n] = (unit.Ammo, unit.HP, unit.Fuel, unit.Target, unit.Position);
			}
			else if (
				unit.IsUsed
				&& unit.Side != game.LocalSide
				&& unit.Category == UnitCategory.Ship
			)
			{
				if (_ships.TryGetValue(n, out var hp) && unit.HP < hp)
				{
					_enemyDamage += hp - unit.HP;
					if (IsCarrier(unit.Kind))
						_carrierHits++;
					WriteLine(
						$"STRIKE {game.Renderer.FrameCount} {unit.Kind} {n} hit, {hp} -> {unit.HP}/{unit.MaxHP}"
					);
				}

				_ships[n] = unit.HP;
			}
			else if (unit.IsUsed && unit.Side == game.LocalSide && IsCarrier(unit.Kind))
			{
				if (_ships.TryGetValue(n, out var hp) && unit.HP < hp)
					_carrierDamage += hp - unit.HP;
				_ships[n] = unit.HP;
			}
		}
	}
}
