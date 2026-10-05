using Aigamo.Saruhashi.MonoGame.Demo.Screens;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.Screens;
using MonoGame.Extended.ViewportAdapters;
using DrawingRectangle = System.Drawing.Rectangle;
using SaruhashiKeys = Aigamo.Saruhashi.Keys;
using SaruhashiMouseListener = Aigamo.Saruhashi.MonoGame.MouseListener;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace Aigamo.Saruhashi.MonoGame.Demo;

public class MainGame : Game
{
	private readonly GraphicsDeviceManager _graphics;
	private SpriteBatch _spriteBatch = default!;

	private readonly ScreenManager _screenManager;
	private ViewportAdapter _viewportAdapter = default!;
	private WindowManager _windowManager = default!;

	public MainGame()
	{
		_graphics = new GraphicsDeviceManager(this);
		Content.RootDirectory = "Content";
		IsMouseVisible = true;

		_screenManager = Components.Add<ScreenManager>();
	}

	protected override void Initialize()
	{
		_graphics.PreferredBackBufferWidth = 1024;
		_graphics.PreferredBackBufferHeight = 768;
		_graphics.ApplyChanges();

		_viewportAdapter = new BoxingViewportAdapter(Window, GraphicsDevice, 1024, 768);

		base.Initialize();
	}

	protected override void LoadContent()
	{
		_spriteBatch = new SpriteBatch(GraphicsDevice);

		var mouseListener = new SaruhashiMouseListener(_viewportAdapter);
		var keyboardListener = new KeyboardListener(
			new KeyboardListenerSettings
			{
				InitialDelayMilliseconds = 500,
				RepeatDelayMilliseconds = 30,
			}
		);
		Components.Add(new InputListenerComponent(this, mouseListener, keyboardListener));

		using var stream = TitleContainer.OpenStream("Content/Fonts/FreeSans.ttf");
		var fontSystem = new FontSystem();
		fontSystem.AddFont(stream);
		var defaultFont = new DynamicSpriteFontWrapper(
			(DynamicSpriteFont)fontSystem.GetFont(fontSize: 16)
		);
		_windowManager = new WindowManager(
			new DrawingRectangle(0, 0, 1024, 768),
			new MonoGameGraphicsFactory(_spriteBatch, _viewportAdapter),
			defaultFont
		);
		mouseListener.MouseDown += (sender, e) => _windowManager.OnMouseDown(e);
		mouseListener.MouseMove += (sender, e) => _windowManager.OnMouseMove(e);
		mouseListener.MouseUp += (sender, e) => _windowManager.OnMouseUp(e);
		keyboardListener.KeyPressed += (sender, e) =>
			_windowManager.OnKeyDown(new KeyEventArgs((SaruhashiKeys)e.Key));
		keyboardListener.KeyReleased += (sender, e) =>
			_windowManager.OnKeyUp(new KeyEventArgs((SaruhashiKeys)e.Key));
		Window.TextInput += (sender, e) =>
			_windowManager.OnKeyPress(new KeyPressEventArgs(e.Character));

		_screenManager.ShowScreen(new Screen1(_windowManager));
	}

	protected override void Update(GameTime gameTime)
	{
		if (
			GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
			|| Keyboard.GetState().IsKeyDown(XnaKeys.Escape)
		)
			Exit();

		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(Color.CornflowerBlue);

		base.Draw(gameTime);

		_windowManager.Draw();
	}
}
