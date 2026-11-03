using System.Collections.Immutable;
using Aigamo.MatchGenerator;

namespace OpenNspw;

// The extension API (docs/Extensions.md): what the game offers to what is built around it, such as replays, in projects
// of their own. Not part of the original.

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

	// Where in a frame the game takes the click the action stands for (IGameExtension.InputSite); none for the dialog's
	// answer and the keys, which the game takes between frames.
	public UICommandSite? Site =>
		this.Match<UICommandSite?>(
			ClickUnit: static _ => UICommandSite.Battlefield,
			SetDestination: static _ => UICommandSite.Battlefield,
			CancelSelection: static _ => UICommandSite.Battlefield,
			RestoreSelection: static _ => UICommandSite.Battlefield,
			ChooseCombatMenuItem: static _ => UICommandSite.CombatMenu,
			ToggleDeckView: static _ => UICommandSite.DeckView,
			SelectSupplyTarget: static _ => UICommandSite.SupplyPanel,
			RequestSupply: static _ => UICommandSite.SupplyPanel,
			ConfirmSystemMenu: static _ => null,
			SwapLocalSide: static _ => null,
			SinkSelectedUnit: static _ => null
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

	// A frame of the battle starts, with `ticks` ticks: returns the ticks it runs.
	short FrameStarting(Nspw game, short ticks) => ticks;

	// Before the battle is drawn, after its ticks: an extension may set the camera.
	void Drawing(Nspw game) { }

	// Where the game takes the clicks of a part of the screen (RunUICommands): an extension may do the player's actions
	// there (Nspw.Do).
	void InputSite(Nspw game, UICommandSite site) { }

	// The player did something, as the game does it.
	void PlayerActed(Nspw game, PlayerAction action) { }

	// The frame of the battle has ended, after HandleInput.
	void FrameEnded(Nspw game) { }

	// A message of the rival has come (DirectPlayMessageHandler), or the rival has left.
	void MessageReceived(Nspw game, ReadOnlySpan<byte> message) { }

	void RivalLeft(Nspw game) { }

	// A frame has been shown.
	void Presented(Nspw game) { }
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

	public void Add(IGameExtension extension)
	{
		ImmutableInterlocked.Update(ref _all, all => all.Add(extension));
	}

	public void Remove(IGameExtension extension)
	{
		ImmutableInterlocked.Update(ref _all, all => all.Remove(extension));
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

	// Each extension sees the ticks that the one before it returned.
	internal short FrameStarting(short ticks)
	{
		foreach (var extension in _all)
			ticks = extension.FrameStarting(_game, ticks);
		return ticks;
	}

	internal void Drawing()
	{
		foreach (var extension in _all)
			extension.Drawing(_game);
	}

	internal void InputSite(UICommandSite site)
	{
		foreach (var extension in _all)
			extension.InputSite(_game, site);
	}

	internal void PlayerActed(PlayerAction action)
	{
		foreach (var extension in _all)
			extension.PlayerActed(_game, action);
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
			SinkSelectedUnit: _ => SinkSelectedUnit
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
