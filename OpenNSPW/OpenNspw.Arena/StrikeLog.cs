namespace OpenNspw.Arena;

// What became of the player's strikes in a game, in all: the weapons its attackers and bombers dropped, the planes that
// turned home damaged or for fuel, their weapon dumped, and the ones lost; the HP the enemy's ships lost, to anything;
// the carriers each side lost; and the HP the player's carriers lost, to anything, which tells how well they are
// defended sooner than the carriers sunk; and the weapons dropped at enemy carriers, and the hits on them, whose ratio
// is the strikes' aim; and the player's planes of every kind lost with no fuel left, and the fighters among them; and
// the attack runs stuck: a torpedo plane of the player's within 400 of the ship it attacks for 150 frames without
// dropping, and those stuck over or near land, where no torpedo may be dropped (PlaneDropTorpedo); and the enemy's
// attackers and bombers lost within 1000 of a ship of the player's with their weapon still aboard, shot down before
// they dropped, and those turned back, their weapon dumped at 70% of their HP or less, within 1500 of one, which tell
// how well the player's ships defend themselves: a plane hit that hard turns home before it is shot down; and the
// battle's losses, of each side, of every unit but mines, which are spent as they go off: the ships sunk, transports
// among them, the planes lost, of every kind, and the bases lost, cities and naval bases among them, destroyed or taken,
// to anything, which tell how the battle went where the outcome does not.
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
	int FightersDry = 0,
	int StuckRuns = 0,
	int StuckOverLand = 0,
	int EnemyStrikersDowned = 0,
	int EnemyStrikersTurnedBack = 0,
	int OwnShipsSunk = 0,
	int EnemyShipsSunk = 0,
	int OwnPlanesLost = 0,
	int EnemyPlanesLost = 0,
	int OwnBasesLost = 0,
	int EnemyBasesLost = 0
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
	private readonly Dictionary<int, (int Since, bool OverLand, bool Counted)> _runs = [];
	private readonly Dictionary<int, WorldPosition> _enemyStrikers = [];
	private readonly Dictionary<int, int> _ships = [];
	private readonly HashSet<int> _carriers = [];
	private readonly Dictionary<int, Side> _sides = [];
	private readonly Dictionary<
		int,
		(Side Side, UnitKind Kind, UnitCategory Category, WorldPosition Position)
	> _alive = [];
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
	private int _stuckRuns;
	private int _stuckOverLand;
	private int _enemyStrikersDowned;
	private int _enemyStrikersTurnedBack;
	private int _ownShipsSunk;
	private int _enemyShipsSunk;
	private int _ownPlanesLost;
	private int _enemyPlanesLost;
	private int _ownBasesLost;
	private int _enemyBasesLost;
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

	// Whether a torpedo may be dropped at a point, as for the land: its tile and the eight around it are sea
	// (PlaneDropTorpedo), the map's edge counted as land.
	private static bool CanDropTorpedoAt(Nspw game, WorldPosition position)
	{
		ref var units = ref game.Renderer.Sprites[SpriteId.JapanUnits];
		ref var tiles = ref game.Renderer.Sprites[SpriteId.MapTiles];
		var column = (int)((position.X + (units.Size.Width / 2) - World.Left) / tiles.Size.Width);
		var row = (int)((World.Top - position.Y + (units.Size.Height / 2)) / tiles.Size.Height);
		for (var dy = -1; dy <= 1; dy++)
		{
			for (var dx = -1; dx <= 1; dx++)
			{
				var (c, r) = (column + dx, row + dy);
				if (c is < 0 or >= 256 || r is < 0 or >= 256 || game.MapTiles[r][c] != 0)
					return false;
			}
		}

		return true;
	}

	// Whether a point is within `distance` of a ship of the player's.
	private static bool IsNearOwnShip(Nspw game, WorldPosition point, double distance)
	{
		for (var s = 1; s <= game.MaxUnitId; s++)
		{
			ref var ship = ref game.Units[s];
			if (
				ship.IsUsed
				&& ship.Side == game.LocalSide
				&& ship.Category == UnitCategory.Ship
				&& Distance(ship.Position, point) <= distance
			)
				return true;
		}

		return false;
	}

	private static bool IsCarrier(UnitKind kind) =>
		kind is UnitKind.Carrier or UnitKind.LightCarrier;

	// Whether a unit is a base: an air base, a naval base, a city or a ground unit, mines aside.
	private static bool IsBase(UnitKind kind) =>
		kind is >= UnitKind.AirBase and <= UnitKind.Fortress and not UnitKind.Mine;

	// Counts a unit alive that is gone, or became another or another side's, as lost to its side, with the enemy's
	// ships and planes near where it was last.
	private void CountLoss(Nspw game, int n)
	{
		if (!_alive.TryGetValue(n, out var was))
			return;

		ref var unit = ref game.Units[n];
		if (unit.IsUsed && unit.Side == was.Side && unit.Kind == was.Kind)
			return;

		_alive.Remove(n);
		if (was.Kind == UnitKind.Mine)
			return;

		var own = was.Side == game.LocalSide;
		WriteLine(
			$"LOSS {game.Renderer.FrameCount} {(own ? "own" : "enemy")} {was.Kind} #{n}, near [{Near(game, was.Position, was.Side, UnitCategory.Ship)}] [{Near(game, was.Position, was.Side, UnitCategory.Plane)}]"
		);
		if (was.Category == UnitCategory.Plane)
		{
			if (own)
				_ownPlanesLost++;
			else
				_enemyPlanesLost++;
		}
		else if (IsBase(was.Kind))
		{
			if (own)
				_ownBasesLost++;
			else
				_enemyBasesLost++;
		}
		else if (own)
			_ownShipsSunk++;
		else
			_enemyShipsSunk++;
	}

	private void WriteLine(string line)
	{
		if (_writes)
			Console.WriteLine(line);
	}

	// The counts so far, with the carriers seen at the start that are gone or sinking, at no HP, and the ships alive
	// that are sinking: the battle may end while one sinks.
	public StrikeCounts Counts(Nspw game)
	{
		var sinking = _alive
			.Where(a =>
				a.Value.Category == UnitCategory.Ship
				&& !IsBase(a.Value.Kind)
				&& game.Units[a.Key].IsUsed
				&& game.Units[a.Key].HP <= 0
			)
			.ToList();
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
			_fightersDry,
			_stuckRuns,
			_stuckOverLand,
			_enemyStrikersDowned,
			_enemyStrikersTurnedBack,
			_ownShipsSunk + sinking.Count(a => a.Value.Side == game.LocalSide),
			_enemyShipsSunk + sinking.Count(a => a.Value.Side != game.LocalSide),
			_ownPlanesLost,
			_enemyPlanesLost,
			_ownBasesLost,
			_enemyBasesLost
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

		foreach (var n in _alive.Keys.Where(n => n > game.MaxUnitId).ToList())
			CountLoss(game, n);

		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var unit = ref game.Units[n];
			CountLoss(game, n);
			if (unit.IsUsed)
				_alive[n] = (unit.Side, unit.Kind, unit.Category, unit.Position);

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

			if (
				unit.IsUsed
				&& unit.Side == game.LocalSide
				&& unit.Kind == UnitKind.Attacker
				&& unit.Weapon == FireKind.Torpedo
				&& unit.Ammo > 0
				&& unit.PlaneState == UnitState.Flying
				&& unit.Target != 0
				&& game.Units[unit.Target].IsUsed
				&& game.Units[unit.Target].Category == UnitCategory.Ship
				&& Distance(unit.Position, game.Units[unit.Target].Position) <= 400
			)
			{
				var run = _runs.GetValueOrDefault(
					n,
					(Since: game.Renderer.FrameCount, OverLand: false, Counted: false)
				);
				run.OverLand |= !CanDropTorpedoAt(game, unit.Position);
				if (!run.Counted && game.Renderer.FrameCount - run.Since >= 150)
				{
					run.Counted = true;
					_stuckRuns++;
					if (run.OverLand)
						_stuckOverLand++;
					WriteLine(
						$"STRIKE {game.Renderer.FrameCount} #{n} stuck on {game.Units[unit.Target].Kind} {unit.Target}{(run.OverLand ? ", over land" : "")}"
					);
				}

				_runs[n] = run;
			}
			else
				_runs.Remove(n);

			if (!unit.IsUsed && _enemyStrikers.Remove(n, out var downedAt))
			{
				if (IsNearOwnShip(game, downedAt, 1000))
					_enemyStrikersDowned++;
			}
			else if (
				unit.IsUsed
				&& unit.Side != game.LocalSide
				&& (unit.Kind is UnitKind.Attacker or UnitKind.Bomber)
				&& unit.PlaneState == UnitState.Flying
				&& unit.Ammo > 0
			)
				_enemyStrikers[n] = unit.Position;
			else if (
				_enemyStrikers.Remove(n)
				&& unit.IsUsed
				&& unit.Ammo == 0
				&& unit.HP <= unit.MaxHP * 0.7
				&& IsNearOwnShip(game, unit.Position, 1500)
			)
				_enemyStrikersTurnedBack++;

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
