using Aigamo.Saruhashi;
using Aigamo.Saruhashi.MonoGame;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.ViewportAdapters;
using OpenNspw.App.Controls;
using OpenNspw.App.Screens;
using OpenNspw.Replay;
using OpenNspw.Verification;
using DrawingRectangle = System.Drawing.Rectangle;
using SaruhashiKeys = Aigamo.Saruhashi.Keys;
using SaruhashiMouseListener = Aigamo.Saruhashi.MonoGame.MouseListener;
using Screen = OpenNspw.App.Screens.Screen;
using ScreenManager = MonoGame.Extended.Screens.ScreenManager;

namespace OpenNspw.App;

internal sealed class DynamicSpriteFontWrapper(DynamicSpriteFont font) : IMonoGameFont
{
	private readonly DynamicSpriteFont _font = font;

	public void Draw(SpriteBatch spriteBatch, string? text, Vector2 position, Color color) =>
		_font.DrawText(spriteBatch, text, position, color);

	public Vector2 MeasureString(string? text) => _font.MeasureString(text);
}

// The app: a window showing the screens of the UI (Screens), which start the port's games (GameSession).
internal sealed class NspwGame : Game
{
	// The window's title: the app's name, not the original's (all_head.CAPTION), which the game still gives its own window
	// and message boxes.
	public const string Title = "OpenNSPW";

	private readonly GraphicsDeviceManager _graphics;
	private readonly ScreenManager _screenManager = new();
	private readonly int? _scenario;
	private readonly Trace122Replay? _trace;
	private readonly ReplayWatch? _watch;
	private Screen? _nextScreen;
	private GameSession? _lastSession;

	// What the system the app runs on gives it (IAppHost).
	public IAppHost Host { get; }

	// How a touch screen is played, as the player chose last (TouchSettingsForm).
	public TouchSettings Touch { get; } = new(UISettings.LoadTouch());

	// What the player points with, which the UI and the game read in each update: the mouse, or the screen's touches, while
	// the window is active (ActivePointer).
	public IPointer Pointer { get; }

	// Where the games' files come from: the embedded ones, or those given on the command line.
	public GameData Data { get; }

	public TextRasterizer Text { get; } = new();

	public SpriteBatch SpriteBatch { get; private set; } = default!;

	public WideViewportAdapter ViewportAdapter { get; private set; } = default!;

	public WindowManager WindowManager { get; private set; } = default!;

	public UIFonts Fonts { get; private set; } = default!;

	// The single-player scenario chosen last, to select it again in the browser.
	public int? LastScenario { get; set; }

	// The options of single-player battles (docs/Options.md), as the player chose last.
	public BattleOptions Options { get; set; } = UISettings.LoadOptions();

	// host: what the system the app runs on gives it. data: where the games' files come from, 1.22's too, to play
	// single-player (docs/Singleplayer.md). scenario: a scenario of 1.22 to play at once, without the menus. trace: a trace
	// of 1.22 in that scenario, which that game follows, to watch it (docs/Verification.md). watch: a replay to watch at
	// once (docs/Replays.md).
	public NspwGame(
		IAppHost host,
		GameData data,
		int? scenario = null,
		Trace122Replay? trace = null,
		ReplayWatch? watch = null
	)
	{
		_graphics = new GraphicsDeviceManager(this);
		_scenario = scenario;
		_trace = trace;
		_watch = watch;
		Host = host;
		// A finger moves TouchOptions.SlopDp before it drags: 16 dp at first, as Android's paging does
		// (ViewConfiguration.ScaledPagingTouchSlop).
		Pointer = new ActivePointer(
			host.CreatePointer(Window, () => Touch.Options.SlopDp),
			() => IsActive
		);
		Data = data;
		Texts.Language = UISettings.LoadLanguage();
		IsMouseVisible = !host.IsTouchScreen;
		Window.Title = Title;
		Window.AllowUserResizing = true;
		Components.Add(_screenManager);
	}

	// Shows a screen, from the next update.
	public void ShowScreen(Screen screen)
	{
		_nextScreen = screen;
	}

	// Starts a game of the port. The screen that starts it owns it, and disposes it when it is unloaded, so that nothing
	// draws a game after it is disposed. singleplayer: a scenario of NSPW 1.22 to play against the computer, instead of
	// connecting to a rival; trace: a trace of 1.22 in that scenario, which the game follows.
	public GameSession StartSession(int? singleplayer = null, Trace122Replay? trace = null)
	{
		_lastSession = new GameSession(
			Data.Files(singleplayer is not null),
			Text,
			singleplayer,
			trace,
			options: Options
		);
		_lastSession.Start();
		return _lastSession;
	}

	// Starts a game that plays a replay from its start, as fast as it can up to frame `seekTo`, in a folder of its own to
	// write in, with the viewer's choices.
	public GameSession StartReplaySession(ReplayWatch watch, int seekTo)
	{
		var player = new ReplayPlayer(File.OpenRead(watch.Path))
		{
			InitialSpeed = watch.Speed,
			IsInitiallyPaused = watch.IsPaused,
		};
		player.SeekTo(seekTo);
		var scratch = Directory.CreateTempSubdirectory("OpenNSPW-replay-").FullName;
		_lastSession = new GameSession(
			Data.ReplayFiles(player.Start, scratch),
			Text,
			player.Start.Mode == ReplayMode.Singleplayer ? player.Start.Scenario : null,
			replay: player,
			view: watch.View,
			scratchDirectory: scratch
		);
		_lastSession.Start();
		return _lastSession;
	}

	// Shows a replay being watched, from frame `seekTo`.
	public void Watch(ReplayWatch watch, int seekTo = 0)
	{
		ShowScreen(new GameScreen(this, () => StartReplaySession(watch, seekTo), watch));
	}

	protected override void Initialize()
	{
		// A window of the game's screen on a desktop; on a touch screen, the whole screen, in landscape, whose shape the
		// screen takes (WideViewportAdapter), as MonoGame's Android otherwise keeps the client area to the back buffer's.
		var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
		(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight) =
			Host.IsTouchScreen
				? (Math.Max(display.Width, display.Height), Math.Min(display.Width, display.Height))
				: (all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		_graphics.ApplyChanges();
		ViewportAdapter = new WideViewportAdapter(GraphicsDevice);
		base.Initialize();
	}

	protected override void LoadContent()
	{
		SpriteBatch = new SpriteBatch(GraphicsDevice);

		var mouseListener = new SaruhashiMouseListener(ViewportAdapter, () => Pointer.State);
		var keyboardListener = new KeyboardListener(
			new KeyboardListenerSettings
			{
				InitialDelayMilliseconds = 500,
				RepeatDelayMilliseconds = 30,
			}
		);
		Components.Add(new InputListenerComponent(this, mouseListener, keyboardListener));

		Fonts = new UIFonts();
		// Wider than the layout, for the battle screen's controls beside it on a wide screen (WideViewportAdapter).
		WindowManager = new WindowManager(
			new DrawingRectangle(0, 0, all_head.SCRN_WIDTH * 3, all_head.SCRN_HEIGHT),
			new MonoGameGraphicsFactory(SpriteBatch, ViewportAdapter),
			Fonts.Regular(14)
		);
		mouseListener.MouseDown += (_, e) => WindowManager.OnMouseDown(e);
		mouseListener.MouseMove += (_, e) => WindowManager.OnMouseMove(e);
		mouseListener.MouseUp += (_, e) => WindowManager.OnMouseUp(e);
		mouseListener.MouseWheel += (_, e) => WindowManager.OnMouseWheel(e);
		keyboardListener.KeyPressed += (_, e) =>
			WindowManager.OnKeyDown(new KeyEventArgs((SaruhashiKeys)e.Key));
		keyboardListener.KeyReleased += (_, e) =>
			WindowManager.OnKeyUp(new KeyEventArgs((SaruhashiKeys)e.Key));
		Host.ListenToText(Window, c => WindowManager.OnKeyPress(new KeyPressEventArgs(c)));

		if (_watch is not null)
			Watch(_watch);
		else
			ShowScreen(
				_scenario is { } scenario
					? new GameScreen(this, () => StartSession(scenario, _trace))
					: new MainMenuScreen(this)
			);
	}

	protected override void Update(GameTime gameTime)
	{
		if (_nextScreen is { } screen)
		{
			_nextScreen = null;
			_screenManager.ReplaceScreen(screen);
		}

		ViewportAdapter.Fit();
		Pointer.Update();
		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(Color.Black);
		base.Draw(gameTime);
		WindowManager.Draw();
	}

	// Closing the window closes the game being played: its screen disposes it, or, if it was started for a screen not shown
	// yet, it is disposed here.
	protected override void OnExiting(object sender, ExitingEventArgs args)
	{
		_screenManager.ClearScreens();
		_lastSession?.Dispose();
		_lastSession?.WaitForEnd(TimeSpan.FromSeconds(3));
		base.OnExiting(sender, args);
	}
}
