using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using OpenNspw.App.Controls;
using OpenNspw.Verification;

namespace OpenNspw.App.Screens;

// A game of the port: in a battle, the battle screen (BattleForm); on the setting screens, the setting form (SettingForm);
// on its other screens, its frames as it draws them. Its dialogs are forms (Dialogs.cs). The main menu comes back when
// the game ends.
//
// startSession: starts the game when the screen is shown, or hands over one already started. The screen owns the game
// from then on, and disposes it when it is unloaded, after its last frame is drawn. watch: the replay that the game
// plays, which the screen shows with its bar (ReplayBar), and whose list comes back when the game ends.
internal sealed class GameScreen(
	NspwGame game,
	Func<GameSession> startSession,
	ReplayWatch? watch = null
) : Screen(game)
{
	private readonly Func<GameSession> _startSession = startSession;
	private readonly ReplayWatch? _watch = watch;
	private ReplayBar? _replayBar;
	private KeyboardState _previousKeyboard;
	private int _viewScrollSpeed;
	private GameSession _session = default!;
	private BattleArt _art = default!;
	private BattleForm _battleForm = default!;

	// How much the battlefield was magnified when the game started: the zoom is saved for the next game if it changed.
	private float _initialZoom;
	private SettingForm _settingForm = default!;
	private Backdrop _backdrop = default!;
	private DialogForms _dialogs = default!;
	private ConfirmForm _confirmQuit = default!;

	// Whether the end of the trace the game follows, or its first difference, was written to the console.
	private bool _hasReportedReplay;

	// Shows in the window's title how far the game follows its trace of 1.22, and writes how that ended to the console,
	// with the difference in full.
	private void ShowReplay(Trace122Progress replay)
	{
		if (replay.Divergence is { } divergence)
		{
			Game.Window.Title = Texts.ReplayDiverged(
				string.Join(
					" ",
					divergence
						.Split(
							'\n',
							StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
						)
						.Take(2)
				)
			);
			if (!_hasReportedReplay)
				Console.WriteLine($"The game left 1.22's trace: {divergence}");
			_hasReportedReplay = true;
		}
		else if (replay.HasEnded)
		{
			Game.Window.Title = Texts.ReplayEnded(replay.Frames);
			if (!_hasReportedReplay)
				Console.WriteLine(
					$"1.22's trace ended after {replay.Frames} frames, without a difference."
				);
			_hasReportedReplay = true;
		}
		else
		{
			Game.Window.Title = Texts.ReplayFollowing(replay.Frames);
		}
	}

	// Goes to a point of the replay, as a fraction of its frames: ahead, as fast as the game can; back, by playing it again
	// from its start.
	private void SeekReplay(ReplayWatch watch, double fraction)
	{
		var frame = (int)Math.Round(fraction * watch.Summary.Frames);
		if (frame > _session.Replay!.Progress.Frame)
			_session.Replay.SeekTo(frame);
		else
			Game.Watch(watch, frame);
	}

	// The player takes the single-player battle over from here, from the viewer's camera: the battlefield scrolls there
	// from the game's camera, which the game reads on its thread.
	private void TakeOverReplay(ReplayWatch watch)
	{
		var viewer = _session.Snapshot.CameraPosition;
		_session.Replay!.TakeOver();
		var (game, commands) = (_session.Game, _session.Commands);
		game.PostToGame(() =>
			commands.ScrollBattlefield(
				(int)(viewer.X - game.CameraPosition.X),
				(int)(game.CameraPosition.Y - viewer.Y),
				0
			)
		);
		watch.View.FollowsPlayer = true;
		watch.View.SeesAll = false;
	}

	// The view of a replay scrolled with W, A, S and D, faster as they are held, as the game scrolls the battlefield with
	// them (ScrollBattleArea).
	private void ScrollReplayView(ReplayView view, KeyboardState keyboard)
	{
		var (dx, dy) = (0, 0);
		if (keyboard.IsKeyDown(Keys.W))
			dy--;
		if (keyboard.IsKeyDown(Keys.S))
			dy++;
		if (keyboard.IsKeyDown(Keys.A))
			dx--;
		if (keyboard.IsKeyDown(Keys.D))
			dx++;
		_viewScrollSpeed = dx == 0 && dy == 0 ? 0 : Math.Min(_viewScrollSpeed + 2, 40);
		if (_viewScrollSpeed > 0)
			view.Move(
				dx * _viewScrollSpeed,
				dy * _viewScrollSpeed,
				_session.Snapshot.CameraPosition
			);
	}

	// The replay's bar, shown in the battle until it is taken over, also once it has ended, to go back in it; and its keys:
	// the period steps a frame, and W, A, S and D scroll the view. The sounds are silent while it goes ahead to a point,
	// skipping what it passes, but not at any speed it plays at, as the game's are at any of its speeds.
	private void UpdateReplay(ReplayWatch watch, ReplayBar bar)
	{
		var progress = _session.Replay!.Progress;
		bar.Visible = _session.Snapshot.IsBattle && !progress.IsTakenOver;
		bar.UpdateFrom();
		SoundEffect.MasterVolume = _session.IsWatching && progress.IsSeeking ? 0 : 1;

		var keyboard = Keyboard.GetState();
		bool Pressed(Keys key) => keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
		if (Game.IsActive && _session.IsWatching && !_confirmQuit.Visible)
		{
			if (Pressed(Keys.OemPeriod))
				bar.Step();
			ScrollReplayView(watch.View, keyboard);
		}

		_previousKeyboard = keyboard;
	}

	private void HideConfirmQuit()
	{
		_confirmQuit.Visible = false;
		if (_settingForm.Visible)
			_settingForm.Focus();
	}

	// Asks before Escape or F12 closes the game, over the screen and its dialogs; the key again cancels.
	private void ToggleConfirmQuit()
	{
		if (_confirmQuit.Visible)
		{
			HideConfirmQuit();
			return;
		}

		_confirmQuit.BringToFront();
		_confirmQuit.Visible = true;
		_confirmQuit.Focus();
	}

	// The touch settings, over the battle, which goes on under them: each takes effect at once (TouchSettings).
	private void ShowTouchSettings()
	{
		var form = new TouchSettingsForm(Game.Fonts, Game.Touch)
		{
			// Dimming the battle screen's column beside the layout too.
			Size = new(Math.Max(all_head.SCRN_WIDTH, _battleForm.Width), all_head.SCRN_HEIGHT),
		};
		form.Closed += (_, _) => WindowManager.Root.Controls.Remove(form);
		WindowManager.Root.Controls.Add(form);
		form.BringToFront();
		form.Show();
		form.Focus();
	}

	public override void LoadContent()
	{
		base.LoadContent();
		_session = _startSession();

		// Under the dialogs, which are brought to the front as they open.
		_art = new BattleArt(GraphicsDevice, _session.Game.IsSingleplayer);
		_battleForm = new BattleForm(
			_session,
			Game.Fonts,
			_art,
			Game.Host.IsTouchScreen,
			isSmallScreen: Game.Host.BattleZoom > 1,
			Game.Touch
		)
		{
			Visible = false,
		};
		_battleForm.TouchSettingsChosen += (_, _) => ShowTouchSettings();
		_initialZoom = UISettings.LoadBattleZoom(Game.Host.BattleZoom);
		_battleForm.Zoom = _initialZoom;
		WindowManager.Root.Controls.Add(_battleForm);
		_settingForm = new SettingForm(_session, Game.Fonts) { Visible = false };
		WindowManager.Root.Controls.Add(_settingForm);
		_backdrop = new Backdrop(GraphicsDevice);
		_dialogs = new DialogForms(_session, WindowManager, Game.Fonts, Game.Host);
		if (_watch is { } watch && _session.Replay is { } player)
		{
			_replayBar = new ReplayBar(Game.Fonts, watch, player) { Visible = false };
			_replayBar.Seek += (_, fraction) => SeekReplay(watch, fraction);
			_replayBar.TakeOverChosen += (_, _) => TakeOverReplay(watch);
			WindowManager.Root.Controls.Add(_replayBar);

			// Over the battle screen, under the dialogs, which come to the front as they open.
			_replayBar.BringToFront();
		}

		_confirmQuit = new ConfirmForm(
			Game.Fonts,
			Texts.QuitGameTitle,
			Texts.QuitGameMessage,
			Texts.Quit
		)
		{
			Visible = false,
		};
		_confirmQuit.Confirmed += (_, _) =>
		{
			HideConfirmQuit();
			_session.Quit();
		};
		_confirmQuit.Canceled += (_, _) => HideConfirmQuit();
		WindowManager.Root.Controls.Add(_confirmQuit);
	}

	public override void UnloadContent()
	{
		base.UnloadContent();
		if (_session.Trace is not null)
			Game.Window.Title = NspwGame.Title;
		SoundEffect.MasterVolume = 1;
		Game.ViewportAdapter.IsLayoutLeft = false;
		if (_battleForm.Zoom != _initialZoom)
			UISettings.SaveBattleZoom(_battleForm.Zoom);
		_session.Dispose();
		_art.Dispose();
		_backdrop.Dispose();
	}

	public override void Update(GameTime gameTime)
	{
		_session.TakeMessages();
		if (_session.HasEnded)
		{
			Game.ShowScreen(
				_watch is not null ? new ReplaysScreen(Game) : new MainMenuScreen(Game)
			);
			return;
		}

		_session.UpdateFrame(GraphicsDevice);
		if (_session.Trace is { } trace)
			ShowReplay(trace.Progress);
		if (_replayBar is { } bar)
			UpdateReplay(_watch!, bar);
		var snapshot = _session.Snapshot;
		_battleForm.Visible = snapshot.IsBattle;

		// The battle screen takes the room right of the layout on a wide screen; the others stay centered in it.
		Game.ViewportAdapter.IsLayoutLeft = snapshot.IsBattle;
		_battleForm.Fit(Game.ViewportAdapter.ExtraWidth);

		// A finger's reach: TouchOptions.ReachDp, 24 dp at first, about half a fingertip's width.
		_battleForm.TouchRadius =
			Game.Touch.Options.ReachDp * Game.Host.Density / Game.ViewportAdapter.Scale;
		// The question to quit dims the battle screen's column beside the layout too.
		_confirmQuit.Size = new(
			snapshot.IsBattle ? _battleForm.Width : all_head.SCRN_WIDTH,
			all_head.SCRN_HEIGHT
		);
		_battleForm.UpdateFrom(snapshot);
		if (_settingForm.Visible != snapshot.IsSetting)
		{
			_settingForm.Visible = snapshot.IsSetting;
			if (snapshot.IsSetting)
				_settingForm.Focus();
		}

		_settingForm.UpdateFrom(snapshot);
		if (Game.IsActive)
		{
			var cursor = _settingForm.Visible
				? SettingForm.GameCursor
				: (System.Drawing.Point?)null;
			_session.UpdateInput(
				Game.Pointer.State,
				Game.ViewportAdapter,
				point =>
					_confirmQuit.Visible
					|| _dialogs.Contains(point)
					|| _battleForm.Owns(point)
					|| _settingForm.Owns(point),
				cursor,
				Game.Host.IsTouchScreen ? SettingForm.GameCursor : null,
				isOnDialog: _dialogs.IsShown
			);
			if (_session.IsQuitRequested)
				ToggleConfirmQuit();
		}

		_dialogs.Update();
	}

	public override void Draw(GameTime gameTime)
	{
		if (_settingForm.Visible)
		{
			_backdrop.Draw(Game.SpriteBatch, Game.ViewportAdapter);
		}
		else if (!_session.IsBattleFrame)
		{
			_session.Draw(Game.SpriteBatch, Game.ViewportAdapter);
		}
	}
}
