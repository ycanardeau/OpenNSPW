namespace OpenNspw.Desktop;

// What the host's UI shows of the game, as it was when the game showed a frame (docs/Ui.md): copied on the game's
// thread as it shows the frame, so that the UI reads it while the game goes on. Its fields keep the globals' types and
// meanings.
internal sealed class GameSnapshot
{
	public Array256<Unit> Units;
	public Array512<Fire> Fires;
	public Array1024<Effect> Effects;

	// The map's tiles, by row and column (MapTiles): 0 for the sea.
	public Array256<Array256<ushort>> MapTiles;

	// The sprites' places in the sheet (set_sprt_data), and where the game last put them.
	public SpriteArray Sprites;

	public Array4096<Cloud> Clouds;

	// The first plane's number, after both sides' ships (JPN_PLANE_START).
	public int FirstPlane;

	// What the player sees (ReadSight): the 11 by 11 tiles of the battlefield from (FirstColumn, FirstRow) that are drawn,
	// by row, and the effects drawn.
	public readonly bool[] SeenTiles = new bool[11 * 11];
	public int FirstColumn;
	public int FirstRow;
	public readonly bool[] SeenEffects = new bool[all_head.EFFECT_MAX];

	// Whether a left click on each unit counts, unit 0 being the land where the selected transport lands (ReadClickable),
	// and whether one on the battlefield gives the selected unit a destination (ReadCanSetDestination).
	public readonly bool[] Clickable = new bool[256];
	public bool CanSetDestination;

	// Whether each unit is a parked plane on the deck shown in the sidebar (ReadShownOnDeck).
	public readonly bool[] ShownOnDeck = new bool[256];

	// Whether the next destination starts the selected unit's path again (MoveOrders[1].ClearsPath), or adds to it.
	public bool ClearsPath;
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

	// What the game writes over its screens: the result of the battle once it is over, the notices of the top left
	// corner, and the players' chat (ReadResult, ReadNotices, ReadChat).
	public string[] ResultText = [];
	public string[] Notices = [];
	public string MyChat = "";
	public string RivalChat = "";

	// The setting screens (UpdateGameSetting): the scenario chosen, and on the page of its list, the scenarios and what
	// the screen says about it; the host's side (0 for Japan); the rules, as the host sets them and the guest is sent them;
	// and whether the host can start from a battle saved.
	public short ScenarioNumber;
	public string[] ScenarioNames = [];
	public string[] ScenarioDescription = [];
	public int HostSide;
	public bool IsDecisionEnabled;
	public Array2<short> SupplyRates;
	public short SwapTime;
	public short SwapRule;
	public Array2<short> InitialSupplyPoints;
	public string ArrivalControl = "";
	public bool WasHost;
	public bool HasAutoSave;

	// Whether the frame is a battle's, with the battlefield, the sidebar and the radar: not the map editor's.
	public bool IsBattle => Mode == GameMode.Battle && !IsEditingMap;

	// Whether the frame is a setting screen's: the scenarios' or the rules'.
	public bool IsSetting => Mode is GameMode.GameSetting or GameMode.ConfigSetting;

	// The setting screens' fields, which the game writes as text only on them.
	private void CopySettingFrom(Nspw game)
	{
		ScenarioNumber = game.ScenarioNumber;
		ScenarioNames = game.ReadScenarioNames();
		ScenarioDescription = game.ReadScenarioDescription();
		HostSide = game.HostSide;
		IsDecisionEnabled = game.IsDecisionEnabled;
		SupplyRates = game.SupplyRates;
		SwapTime = game.SwapTime;
		SwapRule = game.SwapRule;
		InitialSupplyPoints = game.InitialSupplyPoints;
		ArrivalControl = game.ReadArrivalControl();
		WasHost = game.WasHost;
		HasAutoSave = game.HasAutoSave != 0;
	}

	// On the game's thread.
	public void CopyFrom(Nspw game)
	{
		Units = game.Units;
		Fires = game.Fires;
		Effects = game.Effects;
		MapTiles = game.MapTiles;
		Sprites = game.Sprites;
		Clouds = game.Clouds;
		FirstPlane = game.JPN_PLANE_START;
		game.ReadSight(SeenTiles, out FirstColumn, out FirstRow, SeenEffects);
		game.ReadClickable(Clickable);
		game.ReadShownOnDeck(ShownOnDeck);
		CanSetDestination = game.ReadCanSetDestination();
		ClearsPath = game.MoveOrders[1].ClearsPath != 0;
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
		ResultText = game.ReadResult();
		Notices = game.ReadNotices();
		(MyChat, RivalChat) = game.ReadChat();
		if (IsSetting)
			CopySettingFrom(game);
	}
}
