namespace OpenNspw.Arena;

// What a side lost in a skirmish: its ships sunk and its planes lost, by kind, the HP its ships lost, sunk or not, and
// what all of it was worth (SkirmishLog.Worth).
internal sealed record SideLosses(
	IReadOnlyDictionary<UnitKind, int> Sunk,
	IReadOnlyDictionary<UnitKind, int> PlanesLost,
	int ShipHP,
	double Worth
)
{
	public int ShipsSunk => Sunk.Values.Sum();

	public int Planes => PlanesLost.Values.Sum();

	private static string Short(UnitKind kind) =>
		kind switch
		{
			UnitKind.Battleship => "BB",
			UnitKind.Cruiser => "CA",
			UnitKind.Destroyer => "DD",
			UnitKind.Submarine => "SS",
			UnitKind.Carrier => "CV",
			UnitKind.LightCarrier => "CVL",
			UnitKind.Fighter => "F",
			UnitKind.Attacker => "A",
			UnitKind.Bomber => "B",
			_ => kind.ToString(),
		};

	// The kinds and their counts, as "CA2 DD1".
	public static string Kinds(IReadOnlyDictionary<UnitKind, int> counts) =>
		string.Join(" ", counts.OrderBy(p => p.Key).Select(p => $"{Short(p.Key)}{p.Value}"));
}

// The losses of both sides of a skirmish.
internal sealed record SkirmishLosses(SideLosses Own, SideLosses Enemy);

// The units of a skirmish as it starts, to count at its end what each side lost. It reads the whole state: it is for
// the developer, not the player.
internal sealed class SkirmishLog
{
	private readonly Dictionary<
		int,
		(Side Side, UnitKind Kind, UnitCategory Category, int HP)
	> _start = [];

	// What a unit is worth, whole, by its kind: what the strong player reckons a ship is worth (StrongPlayer.Value), a
	// plane 1.
	public static double Worth(UnitKind kind) =>
		kind switch
		{
			UnitKind.Carrier => 10,
			UnitKind.LightCarrier => 8,
			UnitKind.Battleship => 6,
			UnitKind.Cruiser => 4,
			UnitKind.Destroyer => 2,
			_ => 1,
		};

	// Whether a unit counted at the start is still there: in use, of the same side and kind.
	private bool IsThere(Nspw game, int m) =>
		game.Units[m].IsUsed
		&& game.Units[m].Side == _start[m].Side
		&& game.Units[m].Kind == _start[m].Kind;

	private SideLosses LossesOf(Nspw game, Side side)
	{
		var sunk = new Dictionary<UnitKind, int>();
		var planes = new Dictionary<UnitKind, int>();
		var hp = 0;
		var worth = 0.0;
		foreach (var (m, unit) in _start.Where(p => p.Value.Side == side))
		{
			var there = IsThere(game, m);
			if (unit.Category == UnitCategory.Plane)
			{
				if (!there)
				{
					planes[unit.Kind] = planes.GetValueOrDefault(unit.Kind) + 1;
					worth += Worth(unit.Kind);
				}

				continue;
			}

			var lost = there ? Math.Max(0, unit.HP - game.Units[m].HP) : unit.HP;
			if (!there)
				sunk[unit.Kind] = sunk.GetValueOrDefault(unit.Kind) + 1;
			hp += lost;
			worth += Worth(unit.Kind) * lost / Math.Max(1, unit.HP);
		}

		return new SideLosses(sunk, planes, hp, worth);
	}

	// Counts the units as they start, once.
	public void Start(Nspw game)
	{
		if (_start.Count > 0)
			return;

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var unit = ref game.Units[m];
			if (unit.IsUsed)
				_start[m] = (unit.Side, unit.Kind, unit.Category, unit.HP);
		}
	}

	// Whether a side has none of its units left.
	public bool IsWipedOut(Nspw game, Side side) =>
		_start.Count > 0 && _start.Where(p => p.Value.Side == side).All(p => !IsThere(game, p.Key));

	public SkirmishLosses Losses(Nspw game) =>
		new(
			LossesOf(game, game.LocalSide),
			LossesOf(game, game.LocalSide == Side.Japan ? Side.UnitedStates : Side.Japan)
		);
}
