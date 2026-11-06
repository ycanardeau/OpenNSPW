using System.Collections.Immutable;
using Aigamo.MatchGenerator;

namespace OpenNspw;

// The extension API (docs/Extensions.md): what the game offers to what is built around it, such as replays, in projects
// of their own. Not part of the original.

// Where in a frame the game takes an action (IGameExtension.InputSite): where the battle takes the mouse's clicks on the
// battlefield (DrawBattleArea), on the minimap (DrawMinimap), on the switch of a carrier's view (UpdateDeckView), on the
// combat menu (UpdateCombatMenu), on the supply panel (UpdateSupplyRequest), or on the host's system menu
// (UpdateSystemMenu); where the setting screens take them on the list of scenarios (UpdateScenarioList), on the buttons
// under it (UpdateScenarioPageButton, UpdateResumeButton, UpdateAutoSaveButton, UpdateSideSetting), or on the rules
// (UpdateConfigSetting); or where the battle takes the frame's orders, at the start of HandleInput (Orders).
public enum ActionSite
{
	Battlefield,
	Minimap,
	DeckView,
	CombatMenu,
	SupplyPanel,
	SystemMenu,
	ScenarioList,
	ScenarioMenu,
	Rules,
	Orders,
}

// The player's orders of a frame, as the input leaves them for HandleInput: the move order (new_pp[1]) and the select
// order (new_slct[1]) that the clicks on the battle area made, the item of the combat menu clicked, for its unit, and
// the selected unit and the selection (the_slct_unit, slct_unit[1] and its count) that they go to.
public sealed record PlayerOrders(
	MoveOrder Move,
	SelectOrder Select,
	MenuOrder Menu,
	short SelectedUnit,
	short SelectionCount,
	short[] Selections
)
{
	public const int SelectionLength = 256;
}

// What the player did, as the game does it: what a click, a command of the host's UI, a computer player or a key did,
// in the function that they all reach (docs/UI.md). A closed set of cases, each with what its function takes, handled
// with the generated Match, which a case added makes fail to compile wherever it is not handled.
[GenerateMatch]
public abstract record PlayerAction
{
	private PlayerAction() { }

	// ClickUnit: unit `Unit`, or, for 0, the ground where the selected transport lands.
	public sealed record ClickUnit(int Unit, WorldPosition Ground = default) : PlayerAction;

	public sealed record SetDestination(WorldPosition Destination) : PlayerAction;

	public sealed record CancelSelection(int Unit) : PlayerAction;

	public sealed record RestoreSelection : PlayerAction;

	public sealed record ChooseCombatMenuItem(CombatMenuItem Item) : PlayerAction;

	public sealed record ToggleDeckView : PlayerAction;

	// SelectNextSupplyTarget, or SelectPreviousSupplyTarget.
	public sealed record SelectSupplyTarget(bool Next) : PlayerAction;

	public sealed record RequestSupply : PlayerAction;

	// The system menu's question answered OK (ConfirmSystemMenu).
	public sealed record ConfirmSystemMenu(MessageType Answer) : PlayerAction;

	// The debug keys that change the battle (MainWndProc, DBG_MODE): F5 and F9.
	public sealed record SwapLocalSide : PlayerAction;

	public sealed record SinkSelectedUnit : PlayerAction;

	// A frame's orders given whole, as the input leaves them (GivePlayerOrders), such as a trace of 1.22 gives its
	// player's.
	public sealed record GiveOrders(PlayerOrders Orders) : PlayerAction;

	// Where in a frame the game takes the action (IGameExtension.InputSite); none for the dialog's answer and the keys,
	// which the game takes between frames.
	public ActionSite? Site =>
		this.Match<ActionSite?>(
			ClickUnit: static _ => ActionSite.Battlefield,
			SetDestination: static _ => ActionSite.Battlefield,
			CancelSelection: static _ => ActionSite.Battlefield,
			RestoreSelection: static _ => ActionSite.Battlefield,
			ChooseCombatMenuItem: static _ => ActionSite.CombatMenu,
			ToggleDeckView: static _ => ActionSite.DeckView,
			SelectSupplyTarget: static _ => ActionSite.SupplyPanel,
			RequestSupply: static _ => ActionSite.SupplyPanel,
			ConfirmSystemMenu: static _ => null,
			SwapLocalSide: static _ => null,
			SinkSelectedUnit: static _ => null,
			GiveOrders: static _ => ActionSite.Orders
		);
}

// What the host did on the setting screens, as the game does it (demo.cs): the clicks on the list of scenarios, on the
// buttons under it and on the rules, in the functions that the mouse and the host UI's commands both reach. A closed set
// of cases, as PlayerAction's; a battle has none of them.
[GenerateMatch]
public abstract record SettingAction
{
	private SettingAction() { }

	// The cursor on the n-th scenario of the list shown, in every frame it is there.
	public sealed record ChooseScenario(int N) : SettingAction;

	// A click on the scenario chosen (LeaveGameSetting): on to the rules with it.
	public sealed record ConfirmScenario : SettingAction;

	public sealed record TurnScenarioPage : SettingAction;

	public sealed record StartFromResume : SettingAction;

	public sealed record StartFromAutoSave : SettingAction;

	public sealed record SwitchHostSide : SettingAction;

	public sealed record ToggleDecision : SettingAction;

	// A player's increase of supply points up or down (IncreaseSupplyRate, DecreaseSupplyRate): 0 for the host's, 1 for
	// the guest's.
	public sealed record ChangeSupplyRate(int Player, bool Increase) : SettingAction;

	public sealed record ChangeSwapTime(bool Increase) : SettingAction;

	public sealed record ToggleSwapRule : SettingAction;

	public sealed record ChangeInitialSupplyPoints(int Player, bool Increase) : SettingAction;

	public sealed record CycleArrivalControl : SettingAction;

	// Back to the scenarios (BackToScenarioSetting).
	public sealed record BackToScenarios : SettingAction;

	public sealed record StartBattle : SettingAction;

	// Where in a frame the setting screens take the action (IGameExtension.InputSite).
	public ActionSite Site =>
		this.Match(
			ChooseScenario: static _ => ActionSite.ScenarioList,
			ConfirmScenario: static _ => ActionSite.ScenarioList,
			TurnScenarioPage: static _ => ActionSite.ScenarioMenu,
			StartFromResume: static _ => ActionSite.ScenarioMenu,
			StartFromAutoSave: static _ => ActionSite.ScenarioMenu,
			SwitchHostSide: static _ => ActionSite.ScenarioMenu,
			ToggleDecision: static _ => ActionSite.Rules,
			ChangeSupplyRate: static _ => ActionSite.Rules,
			ChangeSwapTime: static _ => ActionSite.Rules,
			ToggleSwapRule: static _ => ActionSite.Rules,
			ChangeInitialSupplyPoints: static _ => ActionSite.Rules,
			CycleArrivalControl: static _ => ActionSite.Rules,
			BackToScenarios: static _ => ActionSite.Rules,
			StartBattle: static _ => ActionSite.Rules
		);
}

// What a network battle starts with, that the setting screens and the rival's messages set (demo.cs, dplay.cs):
// whether the game hosts it, the scenario (below 0 for a saved battle, StartFromSave), the shared seed, the host's side,
// the rules, the map editor's scenario, the players' names and the rival's mode; and what the rival's messages that came
// before the battle left of the turns, which InitializeGame keeps: whether the turn may go on at each sync point
// (CanAdvance1, CanAdvance2), and the rival's checksums and wait (ReceiveSyncFlag).
public sealed record NetworkBattleSettings(
	bool IsHost,
	short Scenario,
	short SharedRandomSeed,
	int HostSide,
	short HostSupplyRate,
	short GuestSupplyRate,
	byte IsDecisionEnabled,
	short HostInitialSupplyPoints,
	short GuestInitialSupplyPoints,
	byte ArrivalControl,
	short SwapTime,
	short SwapRule,
	string UserScenarioFileName,
	string LocalPlayerName,
	string RivalPlayerName,
	short RivalMode,
	bool CanAdvance1,
	bool CanAdvance2,
	byte RivalTickChecksum,
	byte RivalUnitChecksum,
	byte RivalRandomChecksum,
	byte RivalTickWait
);

// An extension of the game, which the game calls at the points of its frames, on its own thread, and waits for. Every
// method does nothing by default. An extension changes the battle only through the player's actions (Nspw.Do) and what
// the points let it set: the seeds, a frame's start and ticks, and the camera.
public interface IGameExtension
{
	// Whether it gives the player's input, such as a replay that plays: the host's keys, clicks and commands are then held
	// back. At most one extension does at a time.
	bool HoldsInput => false;

	// Whether it starts a battle itself, as the game starts (GameStarted): the game then does not connect to a rival
	// first.
	bool StartsBattle => false;

	// WinMain, before its message loop: an extension may start a battle (Nspw.StartNetworkBattle).
	void GameStarted(Nspw game) { }

	// InitializeGame, at its start, and at its end: an extension may set rand's state (Nspw.srand).
	void BattleStarting(Nspw game) { }

	void BattleStarted(Nspw game) { }

	// Whether the next frame of the battle starts now (true), waits (false), or starts when the game's timer says (null).
	bool? StartsFrame(Nspw game) => null;

	// Whether the battle of a single-player game stands still in the frame that starts (Nspw.IsBattleHeld): no tick runs,
	// the computer does not play, the scenario's events and a reinforcement requested wait, the effects draw no random
	// numbers, and the frame is not counted (FrameCount); the player's input and orders and the drawing go on. Null if it
	// does not say: the battle then runs.
	bool? HoldsBattle(Nspw game) => null;

	// Whether the frame of the battle that starts is shown (true, or null if it does not say) or not (false): a frame not
	// shown runs as any other, its drawing too, which draws random numbers and takes the clicks, but draws no pixel and is
	// not presented (Renderer.DrawsNothing), so that the battle is the same whether it is shown or not.
	bool? ShowsFrame(Nspw game) => null;

	// A frame of the battle starts, with `ticks` ticks: returns the ticks it runs. An extension may set the speed
	// (GameSpeed), which the game shows.
	short FrameStarting(Nspw game, short ticks) => ticks;

	// A tick of the battle starts, before UpdateBattle.
	void TickStarting(Nspw game) { }

	// The battle's result is checked (CheckResult), at its start.
	void ResultChecking(Nspw game) { }

	// Before the battle is drawn, after its ticks: an extension may set the camera.
	void Drawing(Nspw game) { }

	// Where the game takes the actions of a part of the screen (Extensions.InputSite), or the frame's orders: an extension may do
	// the player's actions there (Nspw.Do). `counts`: whether a click counts there in this frame, as the part is shown
	// and the player may give it.
	void InputSite(Nspw game, ActionSite site, bool counts) { }

	// A turn's orders start to be applied: the player's, and, in a single-player game, the computer's.
	void OrdersStarting(Nspw game) { }

	// The player did something, as the game does it.
	void PlayerActed(Nspw game, PlayerAction action) { }

	// The host did something on the setting screens, as the game does it.
	void SettingActed(Nspw game, SettingAction action) { }

	// What Windows' stack held where 1.22 reads a local it has not set, at the instruction of 1.22 that reads it
	// (docs/Verification.md), or null: the game then reads what the port's own local holds, or what Windows' stack holds
	// there in every trace recorded so far.
	byte[]? UninitializedLocal(Nspw game, uint instruction) => null;

	// The frame of the battle has ended, after HandleInput.
	void FrameEnded(Nspw game) { }

	// A message of the rival has come (DirectPlayMessageHandler), or the rival has left.
	void MessageReceived(Nspw game, ReadOnlySpan<byte> message) { }

	void RivalLeft(Nspw game) { }

	// A frame has been shown: not one that is not (ShowsFrame).
	void Presented(Nspw game) { }
}

// The computer player of a single-player game (docs/ComputerPlayersAsExtensions.md), which plays the computer's side
// (Nspw.CpuSide), once a frame, where the turn's orders are applied. Unlike an extension, it writes the battle itself, as
// 1.22's computer does: only in Plan, drawing only the game's rand(), and keeping what it needs between frames in the game
// (the computer's fleets, the units), so that a replay that names it plays the same.
public interface IComputerPlayer
{
	// The computer's order of the frame, in MoveOrders[0] and Selections[0], and what it changes of the units and the
	// fleets.
	void Plan(Nspw game);
}

// The extensions of a game, called in the order they were added. Added and removed on any thread; called on the game's.
public sealed class GameExtensions(Nspw game)
{
	private readonly Nspw _game = game;
	private ImmutableArray<IGameExtension> _all = [];

	public ImmutableArray<IGameExtension> All => _all;

	// Whether an extension starts a battle itself (IGameExtension.StartsBattle).
	public bool StartsBattle
	{
		get
		{
			foreach (var extension in _all)
			{
				if (extension.StartsBattle)
					return true;
			}

			return false;
		}
	}

	// Whether an extension gives the player's input (IGameExtension.HoldsInput).
	public bool HoldsInput
	{
		get
		{
			foreach (var extension in _all)
			{
				if (extension.HoldsInput)
					return true;
			}

			return false;
		}
	}

	// Adds or removes an extension: on the game's thread, or before it runs.
	public void Add(IGameExtension extension)
	{
		_all = _all.Add(extension);
	}

	public void Remove(IGameExtension extension)
	{
		_all = _all.Remove(extension);
	}

	internal void GameStarted()
	{
		foreach (var extension in _all)
			extension.GameStarted(_game);
	}

	internal void BattleStarting()
	{
		foreach (var extension in _all)
			extension.BattleStarting(_game);
	}

	internal void BattleStarted()
	{
		foreach (var extension in _all)
			extension.BattleStarted(_game);
	}

	// The first extension that answers.
	internal bool? StartsFrame()
	{
		foreach (var extension in _all)
		{
			if (extension.StartsFrame(_game) is { } starts)
				return starts;
		}

		return null;
	}

	// The first extension that answers.
	internal bool? HoldsBattle()
	{
		foreach (var extension in _all)
		{
			if (extension.HoldsBattle(_game) is { } holds)
				return holds;
		}

		return null;
	}

	// The first extension that answers.
	internal bool? ShowsFrame()
	{
		foreach (var extension in _all)
		{
			if (extension.ShowsFrame(_game) is { } shows)
				return shows;
		}

		return null;
	}

	// Each extension sees the ticks that the one before it returned.
	internal short FrameStarting(short ticks)
	{
		foreach (var extension in _all)
			ticks = extension.FrameStarting(_game, ticks);
		return ticks;
	}

	internal void TickStarting()
	{
		foreach (var extension in _all)
			extension.TickStarting(_game);
	}

	internal void ResultChecking()
	{
		foreach (var extension in _all)
			extension.ResultChecking(_game);
	}

	internal void Drawing()
	{
		foreach (var extension in _all)
			extension.Drawing(_game);
	}

	internal void InputSite(ActionSite site, bool counts)
	{
		foreach (var extension in _all)
			extension.InputSite(_game, site, counts);
	}

	internal void OrdersStarting()
	{
		foreach (var extension in _all)
			extension.OrdersStarting(_game);
	}

	internal void PlayerActed(PlayerAction action)
	{
		foreach (var extension in _all)
			extension.PlayerActed(_game, action);
	}

	internal void SettingActed(SettingAction action)
	{
		foreach (var extension in _all)
			extension.SettingActed(_game, action);
	}

	// The first extension that answers.
	internal byte[]? UninitializedLocal(uint instruction)
	{
		foreach (var extension in _all)
		{
			if (extension.UninitializedLocal(_game, instruction) is { } value)
				return value;
		}

		return null;
	}

	internal void FrameEnded()
	{
		foreach (var extension in _all)
			extension.FrameEnded(_game);
	}

	internal void MessageReceived(ReadOnlySpan<byte> message)
	{
		foreach (var extension in _all)
			extension.MessageReceived(_game, message);
	}

	internal void RivalLeft()
	{
		foreach (var extension in _all)
			extension.RivalLeft(_game);
	}

	internal void Presented()
	{
		foreach (var extension in _all)
			extension.Presented(_game);
	}
}

public unsafe partial class Nspw
{
	private GameExtensions? _extensions;

	// The extensions of this game (docs/Extensions.md).
	public GameExtensions Extensions => _extensions ??= new GameExtensions(this);

	// The platform the game runs on, which an extension may read the game's files through.
	public INspwPlatform Platform => _platform;

	// Does what the player did, as the click or the key it stands for did, without the checks that a click passes before
	// it: an extension does an action that the game did (IGameExtension.PlayerActed), in the same state.
	public void Do(PlayerAction action)
	{
		action.Match<Action>(
			ClickUnit: click => () => ClickUnit(click.Unit, click.Ground),
			SetDestination: destination => () => SetDestination(destination.Destination),
			CancelSelection: cancel => () => CancelSelection(cancel.Unit),
			RestoreSelection: _ => RestoreSelection,
			ChooseCombatMenuItem: choose => () => ChooseCombatMenuItem(choose.Item),
			ToggleDeckView: _ => ToggleDeckView,
			SelectSupplyTarget: select =>
				select.Next ? SelectNextSupplyTarget : SelectPreviousSupplyTarget,
			RequestSupply: _ => RequestSupplyUnit,
			ConfirmSystemMenu: confirm => () => ConfirmSystemMenu(confirm.Answer),
			SwapLocalSide: _ => SwapLocalSide,
			SinkSelectedUnit: _ => SinkSelectedUnit,
			GiveOrders: give => () => GivePlayerOrders(give.Orders)
		)();
	}

	// Does what the host did on the setting screens, as the click it stands for did, without the checks that a click
	// passes before it.
	public void Do(SettingAction action)
	{
		NameMessage message = default;
		SyncFlagMessage flagMessage = default;
		action.Match<Action>(
			ChooseScenario: choose => () => ChooseScenario(choose.N),
			ConfirmScenario: _ => () => LeaveGameSetting(ref message),
			TurnScenarioPage: _ => TurnScenarioPage,
			StartFromResume: _ => () => StartFromResume(ref message),
			StartFromAutoSave: _ => () => StartFromAutoSave(ref message),
			SwitchHostSide: _ => SwitchHostSide,
			ToggleDecision: _ => ToggleDecision,
			ChangeSupplyRate: change =>
				change.Increase
					? () => IncreaseSupplyRate(change.Player)
					: () => DecreaseSupplyRate(change.Player),
			ChangeSwapTime: change => change.Increase ? IncreaseSwapTime : DecreaseSwapTime,
			ToggleSwapRule: _ => ToggleSwapRule,
			ChangeInitialSupplyPoints: change =>
				change.Increase
					? () => IncreaseInitialSupplyPoints(change.Player)
					: () => DecreaseInitialSupplyPoints(change.Player),
			CycleArrivalControl: _ => CycleArrivalControl,
			BackToScenarios: _ => () => BackToScenarioSetting(ref flagMessage),
			StartBattle: _ => () => StartBattle(ref message)
		)();
	}

	// What the setting screens and the rival's messages have set for a network battle, as it starts.
	public NetworkBattleSettings ReadNetworkBattleSettings()
	{
		return new NetworkBattleSettings(
			IsHost,
			ScenarioNumber,
			SharedRandomSeed,
			HostSide,
			SupplyRates[0],
			SupplyRates[1],
			IsDecisionEnabled.Value,
			InitialSupplyPoints[0],
			InitialSupplyPoints[1],
			ArrivalControl,
			SwapTime,
			SwapRule,
			CString(UserScenarioFileName),
			CString(LocalPlayerName),
			CString(RivalPlayerName),
			(short)RivalMode,
			CanAdvance1,
			CanAdvance2,
			TickChecksums[0],
			UnitChecksums[0],
			RandomChecksums[0],
			TickWaits[0]
		);
	}

	// Starts a network battle from its settings, over a peer and its thread pool, without the setting screens: as the
	// setting screens and the rival's messages would (LeaveConfigSetting, ReceiveLeaveConfigSetting), then InitializeGame,
	// as within the frame of the setting screen that starts it, which then counts itself.
	public void StartNetworkBattle(
		NetworkBattleSettings settings,
		IDirectPlay8Peer peer,
		IDirectPlay8ThreadPool threadPool
	)
	{
		DirectPlayPeer?.Close(0);
		DirectPlayPeer = peer;
		DirectPlayThreadPool = threadPool;
		ActivePlayerCount = 2;
		IsHost = settings.IsHost;
		ScenarioNumber = settings.Scenario;
		SharedRandomSeed = settings.SharedRandomSeed;
		HostSide = settings.HostSide;
		SupplyRates[0] = settings.HostSupplyRate;
		SupplyRates[1] = settings.GuestSupplyRate;
		IsDecisionEnabled = new Bool8(settings.IsDecisionEnabled);
		InitialSupplyPoints[0] = settings.HostInitialSupplyPoints;
		InitialSupplyPoints[1] = settings.GuestInitialSupplyPoints;
		ArrivalControl = settings.ArrivalControl;
		SwapTime = settings.SwapTime;
		SwapRule = settings.SwapRule;
		UserScenarioFileName = TEXT<Array260<byte>>(settings.UserScenarioFileName);
		LocalPlayerName = TEXT<Array260<byte>>(settings.LocalPlayerName);
		RivalPlayerName = TEXT<Array260<byte>>(settings.RivalPlayerName);
		RivalMode = (GameMode)settings.RivalMode;
		CanAdvance1 = settings.CanAdvance1;
		CanAdvance2 = settings.CanAdvance2;
		TickChecksums[0] = settings.RivalTickChecksum;
		UnitChecksums[0] = settings.RivalUnitChecksum;
		RandomChecksums[0] = settings.RivalRandomChecksum;
		TickWaits[0] = settings.RivalTickWait;

		Mode = GameMode.Battle;
		InitializeGame();
		Renderer.FrameCount++;
	}
}
