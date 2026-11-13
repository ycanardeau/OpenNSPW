namespace OpenNspw.App;

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
	public static string SteadySpeed => Pick("Steady speed", "安定スピード");

	public static string SteadySpeedDescription =>
		Pick("The same battle at every speed", "どのスピードでも同じ戦闘になります");

	public static string SharedEffectRandom => Pick("Shared effect table", "エフェクト用乱数表");

	public static string SharedEffectRandomDescription =>
		Pick("Effects never change the battle", "エフェクトが戦闘に影響しません");

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
	public static string Slower => "−";
	public static string NormalSpeed => "1×";
	public static string Faster => "+";
	public static string FastForward => "»";
	public static string Chat => Pick("Chat", "チャット");
	public static string Deselect => Pick("Deselect", "選択解除");
	public static string Panel => Pick("Panel", "パネル");
	public static string CommandBar => Pick("Controls", "操作");
	public static string MoveMode => Pick("Move", "移動");
	public static string TouchSettings => Pick("Settings", "設定");

	// The settings (SettingsForm).
	public static string SettingsTitle => Pick("Settings", "設定");
	public static string SettingsDescription =>
		Pick(
			"The language, the sound, and how the game is played",
			"言語や音量、操作の方法を選びます"
		);
	public static string GeneralTab => Pick("General", "全般");
	public static string TouchTab => Pick("Touch", "タッチ操作");
	public static string DebugTab => Pick("Debug", "デバッグ");
	public static string DebugNote =>
		Pick(
			"For debugging: a single-player battle no longer plays as 1.22's.",
			"デバッグ用：一人用の戦闘が 1.22 とは異なる展開になります。"
		);
	public static string LanguageLabel => Pick("Language", "言語");
	public static string VolumeLabel => Pick("Volume", "音量");
	public static string WaypointGestureLabel => Pick("Waypoint", "航路点の指定");
	public static string WaypointTap => Pick("Tap", "タップ");
	public static string WaypointHold => Pick("Hold", "長押し");
	public static string WaypointTwoFingers => Pick("2-finger scroll", "2本指で移動");
	public static string WaypointMoveButton => Pick("Move button", "移動ボタン");
	public static string MinimapTouchLabel => Pick("Minimap", "ミニマップ");
	public static string MinimapAim => Pick("Aim", "狙って移動");
	public static string MinimapJump => Pick("Tap to jump", "タップで移動");
	public static string MinimapFollow => Pick("Follow finger", "指に追従");
	public static string GlideLabel => Pick("Glide after a flick", "フリックの慣性");
	public static string SlopLabel => Pick("Tap tolerance", "タップの許容範囲");
	public static string ReachLabel => Pick("Unit touch area", "ユニットの当たり判定");
	public static string PanelSideLabel => Pick("Panel side", "パネルの位置");
	public static string PanelFollowsLabel => Pick("Panel for carriers", "空母・基地でパネル");
	public static string On => Pick("On", "オン");
	public static string Off => Pick("Off", "オフ");
	public static string Small => Pick("Small", "小");
	public static string Medium => Pick("Medium", "中");
	public static string Large => Pick("Large", "大");
	public static string Larger => Pick("Larger", "特大");
	public static string Left => Pick("Left", "左");
	public static string Right => Pick("Right", "右");
	public static string Close => Pick("Close", "閉じる");

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

	// The title of what stopped a game (ErrorForm).
	public static string GameErrorTitle =>
		Pick("The game stopped with an error", "エラーでゲームが止まりました");

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

	// The replays (docs/Replays.md): the main menu's item, the list, and the bar that plays one.
	public static string Replays => Pick("Replays", "リプレイ");
	public static string ReplaysDescription =>
		Pick("Watch the battles played, again", "プレイした戦闘をもう一度見る");
	public static string ReplaysKicker => Pick("REPLAYS", "リプレイ");
	public static string RecordedBattles => Pick("Recorded battles", "記録した戦闘");
	public static string RecordedBattlesDescription =>
		Pick(
			"Each battle played is recorded. Watch one again, from the player's view or a view of your own.",
			"プレイした戦闘はすべて記録されます。プレイヤーの視点、または自由な視点でもう一度見られます。"
		);
	public static string ReplaysHint =>
		Pick(
			"↑↓ Replay    PgUp PgDn Page    Enter Watch    Del Delete    Esc Back",
			"↑↓ 選択    PgUp PgDn ページ    Enter 再生    Del 削除    Esc 戻る"
		);
	public static string NoReplays =>
		Pick(
			"No battle has been recorded yet: each battle played is recorded here.",
			"記録された戦闘はまだありません。プレイした戦闘はここに記録されます。"
		);
	public static string Watch => Pick("Watch", "再生");
	public static string Delete => Pick("Delete", "削除");
	public static string OpenFolder => Pick("Open Folder", "フォルダーを開く");
	public static string DeleteReplayTitle =>
		Pick("Delete the replay?", "リプレイを削除しますか？");

	public static string DeleteReplayMessage(string name) =>
		Pick($"{name} is deleted for good.", $"{name} を完全に削除します。");

	public static string UnreadableReplay =>
		Pick(
			"Cannot be read: recorded by another version?",
			"読み込めません（別のバージョンの記録？）"
		);

	public static string ReplayMode(Replay.ReplayMode mode, string rival) =>
		mode switch
		{
			Replay.ReplayMode.NetworkHost => Pick($"Host · vs {rival}", $"ホスト · 対 {rival}"),
			Replay.ReplayMode.NetworkGuest => Pick($"Guest · vs {rival}", $"ゲスト · 対 {rival}"),
			_ => Pick("Single player", "シングルプレイ"),
		};

	public static string NetworkScenario(int number) =>
		number < 0
			? Pick("Resumed battle", "リジュームした戦闘")
			: Pick($"Scenario {number}", $"シナリオ {number}");

	public static string ResultName(GameResult result) =>
		result switch
		{
			GameResult.JapanWon => Pick("Japan won", "日本の勝利"),
			GameResult.UnitedStatesWon => Pick("U.S. won", "米国の勝利"),
			GameResult.JapanLost => Pick("Japan lost", "日本の敗北"),
			GameResult.UnitedStatesLost => Pick("U.S. lost", "米国の敗北"),
			GameResult.Draw => Pick("Draw", "引き分け"),
			_ => Pick("Unfinished", "未決着"),
		};

	public static string Duration(long milliseconds) =>
		TimeSpan.FromMilliseconds(milliseconds) is var time && time.TotalHours >= 1
			? $"{(int)time.TotalHours}:{time:mm\\:ss}"
			: $"{time:mm\\:ss}";

	public static string Pause => Pick("Pause", "一時停止");
	public static string Play => Pick("Play", "再生");
	public static string Step => Pick("Step", "コマ送り");
	public static string PlayersView => Pick("Player's view", "プレイヤー視点");
	public static string FreeView => Pick("Free view", "自由視点");
	public static string BothSides => Pick("Both sides", "両軍を表示");
	public static string TakeOver => Pick("Take over", "操作する");

	public static string ReplayPage(int page, int pages) =>
		Pick($"Page {page} / {pages}", $"{page} / {pages} ページ");

	public static string ReplayFrame(int frame, int frames) =>
		Pick($"Frame {frame} / {frames}", $"フレーム {frame} / {frames}");

	public static string ReplayDiffers(string divergence) =>
		Pick($"Not the battle recorded: {divergence}", $"記録と異なります: {divergence}");

	public static string ReplayOver => Pick("End of the replay", "リプレイ終了");

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
