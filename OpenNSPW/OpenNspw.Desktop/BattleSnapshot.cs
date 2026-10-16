namespace OpenNspw.Desktop;

// What the battle screen shows of the game, as it was when the game showed a frame (docs/Ui.md): copied on the game's
// thread as it shows the frame, so that the UI reads it while the game goes on. Its fields keep the globals' types and
// meanings.
internal sealed class BattleSnapshot
{
	public Array256<Unit> Units;
	public Array512<Fire> Fires;
	public Array1024<Effect> Effects;

	// The map's tiles, by row and column (MapTiles): 0 for the sea.
	public Array256<Array256<ushort>> MapTiles;
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

	// The texts of the unit whose information the sidebar shows, or none: its kind, its damage, its ammunition and its
	// fuel (ReadUnitDetails).
	public string[] UnitDetails = [];

	public short GameSpeed;
	public int BattleTime;

	// The supply panel: the points to spend, the reinforcement chosen and its cost, whether it can be requested, and the
	// time until a requested one comes (0 when none is coming).
	public short SupplyPoints;
	public short SupplyTarget;
	public string SupplyTargetName = "";
	public int SupplyCost;
	public SupplyRequestState SupplyRequestState;
	public short SupplyCount;

	public bool IsHost;
	public GameMode RivalMode;

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
		MapTiles = game.MapTiles;
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
		UnitDetails = game.ReadUnitDetails();
		GameSpeed = game.GameSpeed;
		BattleTime = game.BattleTime;
		SupplyPoints = game.SupplyPoints;
		SupplyTarget = game.SupplyTarget;
		SupplyTargetName = game.ReadSupplyTargetName();
		SupplyCost = game.GetSupplyPointsPerUnit();
		SupplyRequestState = game.ReadSupplyRequestState();
		SupplyCount = game.SupplyCount;
		IsHost = game.IsHost;
		RivalMode = game.RivalMode;
		LocalSide = game.LocalSide;
		FrameCount = game.FrameCount;
		Mode = game.Mode;
		IsEditingMap = game.IsEditingMap;
		Result = game.Result;
	}
}
