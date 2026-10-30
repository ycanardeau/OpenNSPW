namespace OpenNspw.Desktop;

internal enum Language
{
	English,
	Japanese,
}

// The words of the UI, in the language chosen (docs/UI.md). Where the UI shows what the game's screens say, the
// Japanese are the game's words and the English those of its English edition; what the game itself writes goes through
// GameTexts.
internal static class Texts
{
	public static Language Language { get; set; } = Language.English;

	private static string Pick(string english, string japanese) =>
		Language == Language.Japanese ? japanese : english;

	// The language's own name, for the switch.
	public static string NameOf(Language language) =>
		language == Language.Japanese ? "日本語" : "English";

	public static string Back => Pick("Back", "戻る");
	public static string Start => Pick("Start", "開始");
	public static string Next => Pick("Next", "次へ");

	public static string SinglePlayer => Pick("Single Player", "シングルプレイ");
	public static string SinglePlayerDescription =>
		Pick(
			"A scenario of NSPW 1.22, against the computer",
			"NSPW 1.22 のシナリオでコンピューターと対戦"
		);
	public static string Multiplayer => Pick("Multiplayer", "マルチプレイ");
	public static string MultiplayerDescription =>
		Pick(
			"Host or join a game of NSPW on the Net",
			"NSPW on the Net の対戦をホスト、または参加"
		);
	public static string Quit => Pick("Quit", "終了");
	public static string Cancel => Pick("Cancel", "キャンセル");
	public static string QuitDescription => Pick("Close OpenNSPW", "OpenNSPW を終了します");

	public static string SinglePlayerKicker =>
		Pick("SINGLE PLAYER  ·  NSPW 1.22", "シングルプレイ  ·  NSPW 1.22");
	public static string Scenarios => Pick("Scenarios", "シナリオ");
	public static string BrowserHint =>
		Pick(
			"↑↓ Scenario    ←→ Set    PgUp PgDn Briefing    Enter Start    Esc Back",
			"↑↓ 選択    ←→ 集の切り替え    PgUp PgDn スクロール    Enter 開始    Esc 戻る"
		);

	public static string ScenarioNumber(int number) =>
		Pick($"SCENARIO {number}", $"シナリオ {number}");

	public static string PlayerSide => Pick("Side", "操作対象");
	public static string VictoryConditions => Pick("Victory conditions", "勝敗条件");

	public static string MultiplayerKicker =>
		Pick("MULTIPLAYER  ·  NSPW ON THE NET 1.10", "マルチプレイ  ·  NSPW ON THE NET 1.10");
	public static string Connect => Pick("Connect", "接続");
	public static string ConnectDescription =>
		Pick(
			"Two players: one hosts a game on a port, the other joins it at the host's address.",
			"二人で対戦します。一人がポートを指定してホストになり、もう一人がホストのアドレスに接続して参加します。"
		);
	public static string Host => Pick("Host", "ホスト");
	public static string Join => Pick("Join", "参加");
	public static string PlayerName => Pick("PLAYER NAME", "プレイヤー名");
	public static string Address => Pick("ADDRESS", "アドレス");
	public static string Port => Pick("PORT", "ポート");
	public static string HostAddressPlaceholder =>
		Pick("A host listens on its port", "ホストは指定したポートで接続を待ちます");
	public static string JoinAddressPlaceholder => Pick("The host's address", "ホストのアドレス");
	public static string Internet => Pick("Internet", "インターネット");
	public static string Bluetooth => "Bluetooth";
	public static string HostDevice =>
		Pick("HOST'S BLUETOOTH ADDRESS", "ホストの Bluetooth アドレス");
	public static string ThisDevice =>
		Pick("THIS COMPUTER'S BLUETOOTH ADDRESS", "このコンピューターの Bluetooth アドレス");
	public static string HostDevicePlaceholder =>
		Pick("Such as 00:1A:7D:DA:71:13", "例: 00:1A:7D:DA:71:13");
	public static string BluetoothOff => Pick("Bluetooth is off", "Bluetooth がオフです");
	public static string PairedDevices => Pick("PAIRED DEVICES", "ペアリング済みのデバイス");
	public static string InvalidBluetoothAddress =>
		Pick(
			"Enter the host's Bluetooth address, such as 00:1A:7D:DA:71:13, or choose a paired device.",
			"ホストの Bluetooth アドレス（例: 00:1A:7D:DA:71:13）を入力するか、ペアリング済みのデバイスを選んでください。"
		);
	public static string Connecting => Pick("Connecting…", "接続中…");
	public static string HostGame => Pick("Host Game", "ホストとして開始");
	public static string JoinGame => Pick("Join Game", "参加");
	public static string ScenarioEditor => Pick("Scenario Editor", "シナリオエディター");

	public static string MapEditorKicker => Pick("MAP EDITOR", "マップエディター");
	public static string HostKicker => Pick("MULTIPLAYER  ·  HOST", "マルチプレイ  ·  ホスト");
	public static string GuestKicker => Pick("MULTIPLAYER  ·  GUEST", "マルチプレイ  ·  ゲスト");
	public static string Rules => Pick("Rules", "ルール");
	public static string HostChoosesScenario =>
		Pick(
			"The host is choosing a scenario and the sides.",
			"ホストがシナリオと操作対象を選んでいます。"
		);
	public static string HostSetsRules =>
		Pick("The host is setting the rules.", "ホストがルールを決めています。");
	public static string WaitingForGuest =>
		Pick("Waiting for the guest…", "ゲストの参加を待っています…");
	public static string ChooseScenarioToEdit =>
		Pick(
			"Choose a scenario to edit, then Next.",
			"編集するシナリオを選び、「次へ」を押してください。"
		);
	public static string ChooseScenario =>
		Pick(
			"Choose a scenario for both players, then Next.",
			"対戦するシナリオを選び、「次へ」を押してください。"
		);
	public static string SetRules =>
		Pick("Set the rules, then Start.", "ルールを決め、「開始」を押してください。");
	public static string Switch => Pick("Switch", "切り替え");
	public static string Change => Pick("Change", "変更");
	public static string Resume => Pick("Resume", "リジューム");
	public static string AutoSave => Pick("Auto-save", "オートセーブ");

	// The pages of the multiplayer list (シナリオ切り替え), named as the single-player sets are.
	public static string Page(int page) =>
		page == 0 ? Pick("Set 1", "第一集") : Pick("Set 2", "第二集");

	// The sides, and whose they are (UpdateSideSetting).
	public static string SideName(int side) =>
		side == 0 ? Pick("Japan Navy", "日本海軍") : Pick("U.S. Navy", "合衆国海軍");

	public static string HostPlayer => Pick("Host", "ホスト");
	public static string GuestPlayer => Pick("Guest", "ゲスト");

	public static string SideOf(int side, string player) =>
		Pick($"{SideName(side)} ({player})", $"{SideName(side)}（{player}）");

	// The rules (UpdateConfigSetting).
	public static string Decision => Pick("Decision", "勝敗判定");

	public static string DecisionValue(bool isEnabled) =>
		isEnabled ? Pick("Valid", "勝敗条件は有効") : Pick("Invalid", "勝敗条件は無効");

	public static string HostSupplyRate =>
		Pick("Increase rate of supply pts (Host)", "補給割当増加率（ホスト側）");
	public static string GuestSupplyRate =>
		Pick("Increase rate of supply pts (Guest)", "補給割当増加率（ゲスト側）");
	public static string SwapTime => Pick("Reverse of supply pts", "補給値反転地点");

	public static string SwapTimeValue(int time) =>
		time != 0 ? Pick($"TIME: {time}", $"経過時間 {time}") : Pick("No reverse", "反転なし");

	public static string SwapRule => Pick("Reverse rule", "補給値反転ルール");

	public static string SwapRuleValue(int rule) =>
		rule == 0 ? Pick("Once a game", "一度だけ") : Pick("Every specified TIME", "指定時間毎");

	public static string HostInitialSupplyPoints =>
		Pick("Supply pts on start (Host)", "初期補給割当（ホスト側）");
	public static string GuestInitialSupplyPoints =>
		Pick("Supply pts on start (Guest)", "初期補給割当（ゲスト側）");
	public static string ArrivalControl => Pick("Supply unit control", "増援ユニット制御");

	public static string Points(int points) => $"{points} pts";

	// The battle's sidebar (UpdateUnitInfo).
	public static string UnitSection => Pick("UNIT", "ユニット");
	public static string StatusSection => Pick("STATUS", "状況");
	public static string SupplySection => Pick("SUPPLY", "補給");
	public static string SystemSection => Pick("SYSTEM", "システム");

	public static string Speed(int speed) => Pick($"SPEED  {speed}", $"スピード  {speed}");

	public static string Time(int time) => Pick($"TIME  {time}", $"経過時間  {time}");

	public static string SupplyPoints(int points) =>
		Pick($"Supply pts  {points}", $"補給割当  {points}");

	public static string Cost(int cost) => Pick($"{cost} pts", $"{cost} 点");

	public static string PreviousKind => "◀";
	public static string NextKind => "▶";
	public static string Request => Pick("Request", "要求する");
	public static string ShortOfPoints => Pick("Shortage of pts", "割当点数不足");
	public static string UnitLimit => Pick("Units Max", "ユニットリミット");
	public static string Restricted => Pick("Restricted Unit", "要求制限");
	public static string Coming => Pick("Wait for coming", "要求中");
	public static string ToMissionMenu => Pick("Mission Menu", "シナリオ選択へ");

	// The question asked before Escape or F12 closes a game.
	public static string QuitGameTitle => Pick("Quit this game?", "このゲームを終了しますか？");
	public static string QuitGameMessage =>
		Pick(
			"You return to the main menu. In a multiplayer game, the connection to the rival is closed.",
			"メインメニューに戻ります。対戦中の場合は、相手との接続が切れます。"
		);

	// The window's title while a game follows a trace of 1.22 (--replay): the frame it is at, then how it ended.
	public static string ReplayFollowing(int frame) =>
		Pick($"Following 1.22's trace: frame {frame}", $"1.22 の記録を再生中: フレーム {frame}");

	public static string ReplayEnded(int frames) =>
		Pick(
			$"1.22's trace ended after {frames} frames, without a difference; the game is yours",
			$"1.22 の記録を最後まで再生しました ({frames} フレーム、相違なし)。ここから操作できます"
		);

	public static string ReplayDiverged(string divergence) =>
		Pick(
			$"Left 1.22's trace: {divergence}; the game is yours",
			$"1.22 の記録と相違: {divergence}。ここから操作できます"
		);

	// The combat menu, whose buttons the original draws as pictures of Japanese words (Buttons1).
	public static string FlightDeck => Pick("Flight Deck", "飛行甲板");
	public static string Hangar => Pick("Hangar", "格納庫");
	public static string Move => Pick("Move / Other", "移動／その他");
	public static string SlowMove => Pick("Slow Move / Other", "低速移動／その他");
	public static string ReturnToBase => Pick("Return", "帰投モード");
	public static string RepairAndSupply => Pick("Repair & Supply", "修理と補給");
	public static string Torpedoes => Pick("Torpedoes", "雷撃装備");
	public static string Bombs => Pick("Bombs", "爆撃装備");
	public static string Unarmed => Pick("Unarmed", "装備無し");
}
