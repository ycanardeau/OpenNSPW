using OpenNspw.HostUI;

namespace OpenNspw.Desktop;

// What the host's UI shows of the game, as it was when the game showed a frame (docs/UI.md): copied on the game's
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

	// What the player sees (ReadSight): the UIState.SightSize by UIState.SightSize tiles on and around the battlefield from
	// (FirstColumn, FirstRow) that are drawn, by row, and the effects drawn.
	public readonly bool[] SeenTiles = new bool[UIState.SightSize * UIState.SightSize];
	public int FirstColumn;
	public int FirstRow;
	public readonly bool[] SeenEffects = new bool[all_head.EFFECT_MAX];

	// Whether every unit is shown, as a replay's view that sees both sides shows them (ReplayView.SeesAll).
	public bool SeesAll;

	// Whether a left click on each unit counts, unit 0 being the land where the selected transport lands (ReadClickable),
	// and whether one on the battlefield gives the selected unit a destination (ReadCanSetDestination).
	public readonly bool[] Clickable = new bool[256];
	public bool CanSetDestination;

	// Whether each unit is a parked plane on the deck shown in the sidebar (ReadShownOnDeck).
	public readonly bool[] ShownOnDeck = new bool[256];

	// The sheet each unit is drawn from on the battlefield and on the deck (ReadUnitSprites).
	public readonly SpriteId[] UnitSprites = new SpriteId[256];

	// Whether the next destination starts the selected unit's path again (MoveOrders[1].ClearsPath), or adds to it.
	public bool ClearsPath;
	public short MaxUnitId;

	// Where the game has the battlefield, and where the UI shows it: ahead of the game by the moves it has given and the
	// game has not taken yet (ScrollAhead); and the last of them that the game has taken (LastCameraMove).
	public WorldPosition GameCameraPosition;
	public WorldPosition CameraPosition;
	public int LastCameraMove;
	public short SelectedUnit;

	// The local player's selection (Selections[1]): each unit's number in it, or 0.
	public Array256<short> Selections;

	public short CombatMenuKind;

	// The item of the combat menu in effect for the selected unit (ReadActiveCombatMenuItem), which the menu shows chosen.
	public CombatMenuItem ActiveCombatMenuItem;

	// The combat menu's items, as the game showed them in the frame (UICommands.ShownCombatMenu).
	public CombatMenuItem[] CombatMenu = [];

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

	// The supply panel: the points to spend, the reinforcement chosen, its picture and its cost, whether it can be
	// requested, and the time until a requested one comes (0 when none is coming). The picture is shown or not as the
	// last frame showed it, blinking while one is coming.
	public short SupplyPoints;
	public short SupplyTarget;
	public string SupplyTargetName = "";
	public (int Row, int Column) SupplyTargetFrame;
	public bool IsSupplyTargetShown;
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

	// Whether a frame of `mode` is a battle's, with the battlefield, the sidebar and the minimap: not the map editor's.
	private static bool IsBattleOf(GameMode mode, bool isEditingMap) =>
		mode == GameMode.Battle && !isEditingMap;

	// Whether a frame of `mode` is a setting screen's: the scenarios' or the rules'.
	private static bool IsSettingOf(GameMode mode) =>
		mode is GameMode.GameSetting or GameMode.ConfigSetting;

	// Whether the UI draws the screen of the frame that the game draws now in the game's place: a battle, but the map
	// editor, or a setting screen. On the game's thread.
	public static bool IsHostScreenOf(Nspw game) =>
		IsBattleOf(game.Mode, game.IsEditingMap) || IsSettingOf(game.Mode);

	// Whether the frame is a battle's.
	public bool IsBattle => IsBattleOf(Mode, IsEditingMap);

	// Whether the frame is a setting screen's.
	public bool IsSetting => IsSettingOf(Mode);

	// Whether the UI draws the frame's screen in the game's place.
	public bool IsHostScreen => IsBattle || IsSetting;

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

	// On the game's thread. view: what the viewer of a replay sees, or null for the player's view.
	public void CopyFrom(Nspw game, UICommands commands, ReplayView? view = null)
	{
		var camera = view?.CameraFor(game.CameraPosition) ?? game.CameraPosition;
		SeesAll = view?.SeesAll ?? false;
		Units = game.Units;
		Fires = game.Fires;
		Effects = game.Effects;
		MapTiles = game.MapTiles;
		Sprites = game.Renderer.Sprites;
		Clouds = game.Clouds;
		FirstPlane = game.JPN_PLANE_START;
		game.ReadSight(camera, SeesAll, SeenTiles, out FirstColumn, out FirstRow, SeenEffects);
		game.ReadClickable(Clickable);
		game.ReadShownOnDeck(ShownOnDeck);
		game.ReadUnitSprites(UnitSprites);
		CanSetDestination = game.ReadCanSetDestination();
		ClearsPath = game.MoveOrders[1].ClearsPath != 0;
		MaxUnitId = game.MaxUnitId;
		GameCameraPosition = CameraPosition = camera;
		LastCameraMove = commands.LastCameraMove;
		SelectedUnit = game.SelectedUnit;
		Selections = game.Selections[1];
		CombatMenuKind = game.CombatMenuKind;
		ActiveCombatMenuItem = game.ReadActiveCombatMenuItem();
		CombatMenu = commands.ShownCombatMenu;
		CanOrder = game.CanOrder;
		UnitInfoPanel = game.UnitInfoPanel;
		UnitDetails = game.ReadUnitDetails();
		GameSpeed = game.GameSpeed;
		BattleTime = game.BattleTime;
		SupplyPoints = game.SupplyPoints;
		SupplyTarget = game.SupplyTarget;
		SupplyTargetName = game.ReadSupplyTargetName();
		SupplyTargetFrame = game.ReadSupplyTargetFrame();
		IsSupplyTargetShown = game.IsSupplyTargetShown;
		SupplyCost = game.GetSupplyPointsPerUnit();
		SupplyRequestState = game.ReadSupplyRequestState();
		SupplyCount = game.SupplyCount;
		IsHost = game.IsHost;
		RivalMode = game.RivalMode;
		LocalSide = game.LocalSide;
		FrameCount = game.Renderer.FrameCount;
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
