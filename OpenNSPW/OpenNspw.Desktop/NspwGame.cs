using Aigamo.Saruhashi;
using Aigamo.Saruhashi.MonoGame;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
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

	public void Draw(SpriteBatch spriteBatch, string? text, Vector2 position, Color color) => _font.DrawText(spriteBatch, text, position, color);

	public Vector2 MeasureString(string? text) => _font.MeasureString(text);
}

// The app: a window showing the screens of the UI (Screens), which start the port's games (GameSession).
internal sealed class NspwGame : Game
{
	private readonly GraphicsDeviceManager _graphics;
	private readonly ScreenManager _screenManager = new();
	private readonly string? _singleplayerExecutablePath;
	private readonly int? _scenario;
	private byte[]? _singleplayerExecutable;
	private Screen? _nextScreen;
	private GameSession? _lastSession;

	// When the screen shown last was shown, to fade it in from black.
	private float _shownAt;
	private const float FadeSeconds = 0.35f;

	// The game's data: t3.bmp, wav, Map, Scenario, Saved.
	public string DataDirectory { get; }

	public TextRasterizer Text { get; } = new();

	public SpriteBatch SpriteBatch { get; private set; } = default!;

	public ViewportAdapter ViewportAdapter { get; private set; } = default!;

	public WindowManager WindowManager { get; private set; } = default!;

	public UiFonts Fonts { get; private set; } = default!;

	// The single-player scenario chosen last, to select it again in the browser.
	public int? LastScenario { get; set; }

	// Whether NSPW_122.exe was found, to play single-player.
	public bool HasSingleplayerExecutable => _singleplayerExecutablePath is not null;

	// singleplayerExecutablePath: NSPW_122.exe, to play single-player (docs/Singleplayer.md). scenario: a scenario of it
	// to play at once, without the menus.
	public NspwGame(string dataDirectory, string? singleplayerExecutablePath, int? scenario = null)
	{
		_graphics = new GraphicsDeviceManager(this);
		_singleplayerExecutablePath = singleplayerExecutablePath;
		_scenario = scenario;
		DataDirectory = dataDirectory;
		Texts.Language = UiSettings.LoadLanguage();
		IsMouseVisible = true;
		Window.Title = all_head.CAPTION;
		Window.AllowUserResizing = true;
		Components.Add(_screenManager);
	}

	public byte[] ReadSingleplayerExecutable()
	{
		return _singleplayerExecutable ??= File.ReadAllBytes(_singleplayerExecutablePath ?? throw new InvalidOperationException("NSPW_122.exe was not found."));
	}

	// Shows a screen, from the next update.
	public void ShowScreen(Screen screen)
	{
		_nextScreen = screen;
	}

	// Starts a game of the port. The screen that starts it owns it, and disposes it when it is unloaded, so that nothing
	// draws a game after it is disposed. singleplayer: a scenario of NSPW 1.22 to play against the computer, instead of
	// connecting to a rival.
	public GameSession StartSession(int? singleplayer = null)
	{
		_lastSession = new GameSession(DataDirectory, Text, singleplayer is { } scenario ? (ReadSingleplayerExecutable(), scenario) : null);
		_lastSession.Start();
		return _lastSession;
	}

	protected override void Initialize()
	{
		_graphics.PreferredBackBufferWidth = all_head.SCRN_WIDTH;
		_graphics.PreferredBackBufferHeight = all_head.SCRN_HEIGHT;
		_graphics.ApplyChanges();
		ViewportAdapter = new BoxingViewportAdapter(Window, GraphicsDevice, all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		base.Initialize();
	}

	protected override void LoadContent()
	{
		SpriteBatch = new SpriteBatch(GraphicsDevice);

		var mouseListener = new SaruhashiMouseListener(ViewportAdapter);
		var keyboardListener = new KeyboardListener(new KeyboardListenerSettings { InitialDelayMilliseconds = 500, RepeatDelayMilliseconds = 30 });
		Components.Add(new InputListenerComponent(this, mouseListener, keyboardListener));

		Fonts = new UiFonts(Text.FontData);
		WindowManager = new WindowManager(
			new DrawingRectangle(0, 0, all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT),
			new MonoGameGraphicsFactory(SpriteBatch, ViewportAdapter),
			Fonts.Regular(14));
		mouseListener.MouseDown += (_, e) => WindowManager.OnMouseDown(e);
		mouseListener.MouseMove += (_, e) => WindowManager.OnMouseMove(e);
		mouseListener.MouseUp += (_, e) => WindowManager.OnMouseUp(e);
		mouseListener.MouseWheel += (_, e) => WindowManager.OnMouseWheel(e);
		keyboardListener.KeyPressed += (_, e) => WindowManager.OnKeyDown(new KeyEventArgs((SaruhashiKeys)e.Key));
		keyboardListener.KeyReleased += (_, e) => WindowManager.OnKeyUp(new KeyEventArgs((SaruhashiKeys)e.Key));
		Window.TextInput += (_, e) => WindowManager.OnKeyPress(new KeyPressEventArgs(e.Character));

		ShowScreen(_scenario is { } scenario ? new GameScreen(this, () => StartSession(scenario)) : new MainMenuScreen(this));
	}

	protected override void Update(GameTime gameTime)
	{
		if (_nextScreen is { } screen)
		{
			_nextScreen = null;
			_screenManager.ReplaceScreen(screen);
			_shownAt = Painter.Now;
		}

		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(Color.Black);
		base.Draw(gameTime);
		WindowManager.Draw();

		var fade = 1 - (Painter.Now - _shownAt) / FadeSeconds;
		if (fade > 0)
		{
			SpriteBatch.Begin(transformMatrix: ViewportAdapter.GetScaleMatrix());
			SpriteBatch.FillRectangle(new RectangleF(0, 0, all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT), Color.Black * fade);
			SpriteBatch.End();
		}
	}

	// Closing the window closes the game being played: its screen disposes it, or, if it was started for a screen not shown
	// yet, it is disposed here.
	protected override void OnExiting(object sender, ExitingEventArgs args)
	{
		_screenManager.ClearScreens();
		_lastSession?.Dispose();
		base.OnExiting(sender, args);
	}
}
