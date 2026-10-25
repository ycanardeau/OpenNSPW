namespace OpenNspw.Desktop.Controls;

// Selecting units by brushing over them with the left button held, on the battlefield or on the deck: a press on one of
// the player's units puts it in the selection, or takes it out, and each unit the mouse then passes over is put in, or
// taken out, as that one was (BrushUnit), if it can be in the selected unit's formation.
internal sealed class UnitBrush(GameSession session)
{
	private readonly GameSession _session = session;

	// The units brushed since the press, each brushed once.
	private readonly HashSet<int> _brushed = [];

	// While the button is held after a press that started a brush: whether the units brushed are put in the selection.
	private bool? _adds;

	private static bool IsParkedPlane(ref Unit unit) =>
		unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked;

	// Whether a press on unit m starts a brush, and which: on one of the player's units that can be in a formation with
	// the selected unit, as the game's formations go (IsFormationMate), the press puts it in the selection, or takes it
	// out; the selected unit itself is let go, and starts none.
	private static bool? BrushOf(GameSnapshot snapshot, int m)
	{
		ref var unit = ref snapshot.Units[m];
		ref var selected = ref snapshot.Units[snapshot.SelectedUnit];
		if (
			m == snapshot.SelectedUnit
			|| unit.Side != snapshot.LocalSide
			|| (unit.Kind >= UnitKind.AirBase && unit.Kind <= UnitKind.Fortress)
		)
			return null;

		if (
			snapshot.SelectedUnit != 0
			&& (
				selected.Category != unit.Category
				|| IsParkedPlane(ref selected) != IsParkedPlane(ref unit)
			)
		)
			return null;

		return snapshot.Selections[m] == 0;
	}

	// A press of the left button, on unit m or on none.
	public void Press(GameSnapshot snapshot, int? m)
	{
		_brushed.Clear();
		_adds = m is { } pressed ? BrushOf(snapshot, pressed) : null;
		if (m is { } first)
			_brushed.Add(first);
	}

	// The mouse over unit m, or over none, with the left button held.
	public void Over(int? m)
	{
		if (_adds is not { } adds || m is not { } unit || !_brushed.Add(unit))
			return;

		_session.Game.Commands.BrushUnit(unit, adds);
	}

	public void Release()
	{
		_adds = null;
	}
}
