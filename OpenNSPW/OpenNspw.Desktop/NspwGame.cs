using Aigamo.Saruhashi;
using Aigamo.Saruhashi.MonoGame;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.ViewportAdapters;
using OpenNspw.Desktop.Controls;
using OpenNspw.Desktop.Screens;
using DrawingRectangle = System.Drawing.Rectangle;
using SaruhashiKeys = Aigamo.Saruhashi.Keys;
using SaruhashiMouseListener = Aigamo.Saruhashi.MonoGame.MouseListener;
using Screen = OpenNspw.Desktop.Screens.Screen;
using ScreenManager = MonoGame.Extended.Screens.ScreenManager;

namespace OpenNspw.Desktop;

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
	private readonly GraphicsDeviceManager _graphics;
	private readonly ScreenManager _screenManager = new();
	private readonly int? _scenario;
	private readonly Trace122Replay? _replay;
	private byte[]? _singleplayerExecutable;
	private Screen? _nextScreen;
	private GameSession? _lastSession;

	// Where the games' files come from: the embedded ones, or those given on the command line.
	public GameData Data { get; }

	public TextRasterizer Text { get; } = new();

	public SpriteBatch SpriteBatch { get; private set; } = default!;

	public ViewportAdapter ViewportAdapter { get; private set; } = default!;

	public WindowManager WindowManager { get; private set; } = default!;

	public UIFonts Fonts { get; private set; } = default!;

	// The single-player scenario chosen last, to select it again in the browser.
	public int? LastScenario { get; set; }

	// data: where the games' files come from, NSPW_122.exe too, to play single-player (docs/Singleplayer.md). scenario: a
	// scenario of 1.22 to play at once, without the menus. replay: a trace of 1.22 in that scenario, which that game
	// follows, to watch it (docs/Verification.md).
	public NspwGame(GameData data, int? scenario = null, Trace122Replay? replay = null)
	{
		_graphics = new GraphicsDeviceManager(this);
		_scenario = scenario;
		_replay = replay;
		Data = data;
		Texts.Language = UISettings.LoadLanguage();
		IsMouseVisible = true;
		Window.Title = all_head.CAPTION;
		Window.AllowUserResizing = true;
		Components.Add(_screenManager);
	}

	public byte[] ReadSingleplayerExecutable()
	{
		return _singleplayerExecutable ??= Data.ReadExecutable();
	}

	// Shows a screen, from the next update.
	public void ShowScreen(Screen screen)
	{
		_nextScreen = screen;
	}

	// Starts a game of the port. The screen that starts it owns it, and disposes it when it is unloaded, so that nothing
	// draws a game after it is disposed. singleplayer: a scenario of NSPW 1.22 to play against the computer, instead of
	// connecting to a rival; replay: a trace of 1.22 in that scenario, which the game follows.
	public GameSession StartSession(int? singleplayer = null, Trace122Replay? replay = null)
	{
		_lastSession = new GameSession(
			Data.Files(singleplayer is not null),
			Text,
			singleplayer is { } scenario ? (ReadSingleplayerExecutable(), scenario) : null,
			replay
		);
		_lastSession.Start();
		return _lastSession;
	}

	protected override void Initialize()
	{
		_graphics.PreferredBackBufferWidth = all_head.SCRN_WIDTH;
		_graphics.PreferredBackBufferHeight = all_head.SCRN_HEIGHT;
		_graphics.ApplyChanges();
		ViewportAdapter = new BoxingViewportAdapter(
			Window,
			GraphicsDevice,
			all_head.SCRN_WIDTH,
			all_head.SCRN_HEIGHT
		);
		base.Initialize();
	}

	protected override void LoadContent()
	{
		SpriteBatch = new SpriteBatch(GraphicsDevice);

		var mouseListener = new SaruhashiMouseListener(ViewportAdapter);
		var keyboardListener = new KeyboardListener(
			new KeyboardListenerSettings
			{
				InitialDelayMilliseconds = 500,
				RepeatDelayMilliseconds = 30,
			}
		);
		Components.Add(new InputListenerComponent(this, mouseListener, keyboardListener));

		Fonts = new UIFonts();
		WindowManager = new WindowManager(
			new DrawingRectangle(0, 0, all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT),
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
		Window.TextInput += (_, e) => WindowManager.OnKeyPress(new KeyPressEventArgs(e.Character));

		ShowScreen(
			_scenario is { } scenario
				? new GameScreen(this, () => StartSession(scenario, _replay))
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
