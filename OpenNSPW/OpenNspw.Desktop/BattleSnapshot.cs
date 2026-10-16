namespace OpenNspw.Desktop;

// What the battle screen shows of the game, as it was when the game showed a frame (docs/Ui.md): copied on the game's
// thread as it shows the frame, so that the UI reads it while the game goes on. Its fields keep the globals' types and
// meanings.
internal sealed class BattleSnapshot
{
	public Array256<Unit> Units;
	public Array512<Fire> Fires;
	public Array1024<Effect> Effects;
	public short MaxUnitId;
	public WorldPosition CameraPosition;
	public short SelectedUnit;

	// The local player's selection (Selections[1]): each unit's number in it, or 0.
	public Array256<short> Selections;

	public short CombatMenuKind;
	public CombatMenuItem CombatMenuSelection;

	// The combat menu's items, as the game showed them in the frame (ShownCombatMenu).
	public Array6<CombatMenuItem> CombatMenu;
	public int CombatMenuCount;

	// Whether the player can give the turn's order (CanOrder).
	public bool CanOrder;

	// The unit whose information the sidebar shows (UnitInfoPanel): [0] its kind, [1] 1 for a carrier's hangar and 0 for
	// its flight deck, [3] its number, [4] its side.
	public Array10<int> UnitInfoPanel;

	public Side LocalSide;
	public int FrameCount;
	public GameMode Mode;
	public bool IsEditingMap;
	public GameResult Result;

	// Whether the frame is a battle's, with the battlefield, the sidebar and the radar: not the map editor's.
	public bool IsBattle => Mode == GameMode.Battle && !IsEditingMap;

	// On the game's thread.
	public void CopyFrom(Nspw game)
	{
		Units = game.Units;
		Fires = game.Fires;
		Effects = game.Effects;
		MaxUnitId = game.MaxUnitId;
		CameraPosition = game.CameraPosition;
		SelectedUnit = game.SelectedUnit;
		Selections = game.Selections[1];
		CombatMenuKind = game.CombatMenuKind;
		CombatMenuSelection = game.CombatMenuSelection;
		CombatMenu = game.ShownCombatMenu;
		CombatMenuCount = game.ShownCombatMenuCount;
		CanOrder = game.CanOrder;
		UnitInfoPanel = game.UnitInfoPanel;
		LocalSide = game.LocalSide;
		FrameCount = game.FrameCount;
		Mode = game.Mode;
		IsEditingMap = game.IsEditingMap;
		Result = game.Result;
	}
}
