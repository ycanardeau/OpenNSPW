namespace OpenNspw.AI;

// Where an enemy unit was when it was last found, whether it flew, heading where, at what speed, and in which frame.
public sealed record Contact(
	int Unit,
	UnitKind Kind,
	UnitCategory Category,
	bool IsFlying,
	WorldPosition Position,
	double Direction,
	double Speed,
	int Frame
)
{
	// Where the unit would be after `ticks` more ticks on its heading.
	public WorldPosition Predict(double ticks) =>
		new(
			Position.X + (Math.Cos(Direction * Math.PI / 180) * Speed * ticks),
			Position.Y + (Math.Sin(Direction * Math.PI / 180) * Speed * ticks)
		);
}

// The enemy units the player has found, kept after they are lost from sight, as a player remembers the marks on the
// map. A unit is forgotten once it is gone from the battle.
public sealed class Contacts
{
	private readonly Dictionary<int, Contact> _contacts = [];

	// Whether every enemy unit counts as found, as the computer sees the player's units: an experiment, against the
	// rules of docs/StrongAI.md.
	public bool SeesEverything { get; set; }

	public IEnumerable<Contact> All => _contacts.Values;

	public void Update(Nspw game)
	{
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var unit = ref game.Units[n];
			if (!unit.IsUsed || unit.Side == game.LocalSide)
			{
				_contacts.Remove(n);
			}
			else if (
				(unit.IsFound || SeesEverything)
				&& !(unit.Kind == UnitKind.Submarine && unit.IsSubmerged)
			)
			{
				_contacts[n] = new Contact(
					n,
					unit.Kind,
					unit.Category,
					unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Flying,
					unit.Position,
					unit.Direction.Degrees,
					unit.Speed,
					game.Renderer.FrameCount
				);
			}
		}
	}

	public Contact? Find(int unit) => _contacts.GetValueOrDefault(unit);
}
