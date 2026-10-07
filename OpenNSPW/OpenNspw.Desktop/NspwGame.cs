using Aigamo.Saruhashi;
using Aigamo.Saruhashi.MonoGame;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.ViewportAdapters;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using SaruhashiKeys = Aigamo.Saruhashi.Keys;
using SaruhashiMouseListener = Aigamo.Saruhashi.MonoGame.MouseListener;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace OpenNspw.Desktop;

internal sealed class DynamicSpriteFontWrapper(DynamicSpriteFont font) : IMonoGameFont
{
	private readonly DynamicSpriteFont _font = font;

	public void Draw(SpriteBatch spriteBatch, string? text, Vector2 position, Color color) => _font.DrawText(spriteBatch, text, position, color);

	public Vector2 MeasureString(string? text) => _font.MeasureString(text);
}

// Runs the game: its WinMain (win_main.cpp) on its own thread, as the original runs it, with this window showing its
// frames and dialogs, and turning the keyboard and the mouse into DirectInput data and window messages.
internal sealed class NspwGame : Game
{
	// MonoGame keys, with their DirectInput scan codes (DIK_*) and virtual-key codes (VK_*).
	private static readonly Dictionary<XnaKeys, (uint Dik, int Vk)> KeyCodes = new()
	{
		[XnaKeys.Escape] = (0x01, 0x1B), [XnaKeys.Enter] = (0x1C, 0x0D), [XnaKeys.Space] = (0x39, 0x20),
		[XnaKeys.Up] = (0xC8, 0x26), [XnaKeys.Left] = (0xCB, 0x25), [XnaKeys.Right] = (0xCD, 0x27), [XnaKeys.Down] = (0xD0, 0x28),
		[XnaKeys.D1] = (0x02, '1'), [XnaKeys.D2] = (0x03, '2'), [XnaKeys.D3] = (0x04, '3'), [XnaKeys.D4] = (0x05, '4'), [XnaKeys.D5] = (0x06, '5'),
		[XnaKeys.D6] = (0x07, '6'), [XnaKeys.D7] = (0x08, '7'), [XnaKeys.D8] = (0x09, '8'), [XnaKeys.D9] = (0x0A, '9'), [XnaKeys.D0] = (0x0B, '0'),
		[XnaKeys.Q] = (0x10, 'Q'), [XnaKeys.W] = (0x11, 'W'), [XnaKeys.E] = (0x12, 'E'), [XnaKeys.R] = (0x13, 'R'), [XnaKeys.T] = (0x14, 'T'),
		[XnaKeys.Y] = (0x15, 'Y'), [XnaKeys.U] = (0x16, 'U'), [XnaKeys.I] = (0x17, 'I'), [XnaKeys.O] = (0x18, 'O'), [XnaKeys.P] = (0x19, 'P'),
		[XnaKeys.A] = (0x1E, 'A'), [XnaKeys.S] = (0x1F, 'S'), [XnaKeys.D] = (0x20, 'D'), [XnaKeys.F] = (0x21, 'F'), [XnaKeys.G] = (0x22, 'G'),
		[XnaKeys.H] = (0x23, 'H'), [XnaKeys.J] = (0x24, 'J'), [XnaKeys.K] = (0x25, 'K'), [XnaKeys.L] = (0x26, 'L'),
		[XnaKeys.Z] = (0x2C, 'Z'), [XnaKeys.X] = (0x2D, 'X'), [XnaKeys.C] = (0x2E, 'C'), [XnaKeys.V] = (0x2F, 'V'), [XnaKeys.B] = (0x30, 'B'),
		[XnaKeys.N] = (0x31, 'N'), [XnaKeys.M] = (0x32, 'M'),
		[XnaKeys.F1] = (0x3B, 0x70), [XnaKeys.F2] = (0x3C, 0x71), [XnaKeys.F3] = (0x3D, 0x72), [XnaKeys.F4] = (0x3E, 0x73), [XnaKeys.F5] = (0x3F, 0x74),
		[XnaKeys.F6] = (0x40, 0x75), [XnaKeys.F7] = (0x41, 0x76), [XnaKeys.F8] = (0x42, 0x77), [XnaKeys.F9] = (0x43, 0x78), [XnaKeys.F10] = (0x44, 0x79),
		[XnaKeys.F11] = (0x57, 0x7A), [XnaKeys.F12] = (0x58, 0x7B),
		[XnaKeys.NumPad0] = (0x52, 0x60), [XnaKeys.NumPad1] = (0x4F, 0x61), [XnaKeys.NumPad2] = (0x50, 0x62), [XnaKeys.NumPad3] = (0x51, 0x63),
		[XnaKeys.NumPad4] = (0x4B, 0x64), [XnaKeys.NumPad5] = (0x4C, 0x65), [XnaKeys.NumPad6] = (0x4D, 0x66), [XnaKeys.NumPad7] = (0x47, 0x67),
		[XnaKeys.NumPad8] = (0x48, 0x68), [XnaKeys.NumPad9] = (0x49, 0x69),
	};

	private readonly GraphicsDeviceManager _graphics;
	private readonly DesktopPlatform _platform;
	private readonly Nspw _game;
	private readonly Thread _thread;
	private SpriteBatch _spriteBatch = default!;
	private ViewportAdapter _viewportAdapter = default!;
	private WindowManager _windowManager = default!;
	private DialogForms _dialogs = default!;
	private Texture2D _screen = default!;
	private ushort[] _pixels = [];
	private Color[] _colors = [];
	private int _frameNumber = -1;
	private KeyboardState _previousKeyboard;
	private MouseState _previousMouse;
	private volatile bool _gameEnded;

	private void RunGame()
	{
		try
		{
			_game.WinMain(null, null, "", winuser.SW_SHOWNORMAL);
		}
		catch (Exception e)
		{
			lock (_platform.Messages)
			{
				_platform.Messages.Add(e.ToString());
			}

			Console.Error.WriteLine(e);
		}
		finally
		{
			_gameEnded = true;
		}
	}

	public NspwGame(string dataDirectory)
	{
		_graphics = new GraphicsDeviceManager(this);
		IsMouseVisible = true;
		Window.Title = all_head.CAPTION;
		Window.AllowUserResizing = true;
		_platform = new DesktopPlatform(dataDirectory);
		_game = new Nspw(_platform);
		_thread = new Thread(RunGame) { IsBackground = true, Name = "NSPW" };
	}

	protected override void Initialize()
	{
		_graphics.PreferredBackBufferWidth = all_head.SCRN_WIDTH;
		_graphics.PreferredBackBufferHeight = all_head.SCRN_HEIGHT;
		_graphics.ApplyChanges();
		_viewportAdapter = new BoxingViewportAdapter(Window, GraphicsDevice, all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		base.Initialize();
	}

	protected override void LoadContent()
	{
		_spriteBatch = new SpriteBatch(GraphicsDevice);
		_screen = new Texture2D(GraphicsDevice, all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);

		var mouseListener = new SaruhashiMouseListener(_viewportAdapter);
		var keyboardListener = new KeyboardListener(new KeyboardListenerSettings { InitialDelayMilliseconds = 500, RepeatDelayMilliseconds = 30 });
		Components.Add(new InputListenerComponent(this, mouseListener, keyboardListener));

		var fontSystem = new FontSystem();
		if (_platform.FontData is { } fontData)
		{
			fontSystem.AddFont(fontData);
		}

		_windowManager = new WindowManager(
			new DrawingRectangle(0, 0, all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT),
			new MonoGameGraphicsFactory(_spriteBatch, _viewportAdapter),
			new DynamicSpriteFontWrapper((DynamicSpriteFont)fontSystem.GetFont(14)));
		mouseListener.MouseDown += (_, e) => _windowManager.OnMouseDown(e);
		mouseListener.MouseMove += (_, e) => _windowManager.OnMouseMove(e);
		mouseListener.MouseUp += (_, e) => _windowManager.OnMouseUp(e);
		keyboardListener.KeyPressed += (_, e) => _windowManager.OnKeyDown(new KeyEventArgs((SaruhashiKeys)e.Key));
		keyboardListener.KeyReleased += (_, e) => _windowManager.OnKeyUp(new KeyEventArgs((SaruhashiKeys)e.Key));
		Window.TextInput += (_, e) => _windowManager.OnKeyPress(new KeyPressEventArgs(e.Character));
		_dialogs = new DialogForms(_game, _windowManager);

		_thread.Start();
	}

	private void UpdateKeyboard()
	{
		var keyboard = Keyboard.GetState();
		foreach (var (key, (dik, vk)) in KeyCodes)
		{
			var down = keyboard.IsKeyDown(key);
			if (down != _previousKeyboard.IsKeyDown(key))
			{
				_game.PostKeyboardInput(dik, down);
				_game.SetKeyState(_game.hwndApp, vk, down);
			}
		}

		_previousKeyboard = keyboard;
	}

	private void UpdateMouse()
	{
		var mouse = Mouse.GetState();
		var position = _viewportAdapter.PointToScreen(mouse.Position);
		_game.SetCursorPos(position.X, position.Y);
		var onDialog = _dialogs.Contains(new DrawingPoint(position.X, position.Y));
		if (!onDialog || mouse.LeftButton == ButtonState.Released)
		{
			if (mouse.LeftButton != _previousMouse.LeftButton)
			{
				_game.PostMouseInput(0, mouse.LeftButton == ButtonState.Pressed);
			}

			if (mouse.RightButton != _previousMouse.RightButton)
			{
				_game.PostMouseInput(1, mouse.RightButton == ButtonState.Pressed);
			}
		}

		_previousMouse = mouse;
	}

	protected override void Update(GameTime gameTime)
	{
		if (_gameEnded)
		{
			Exit();
		}

		if (IsActive)
		{
			UpdateKeyboard();
			UpdateMouse();
		}

		_dialogs.Update();
		base.Update(gameTime);
	}

	private static Color ToColor(ushort pixel)
	{
		var r = (pixel >> 11) & 0x1F;
		var g = (pixel >> 5) & 0x3F;
		var b = pixel & 0x1F;
		return new Color((r << 3) | (r >> 2), (g << 2) | (g >> 4), (b << 3) | (b >> 2));
	}

	protected override void Draw(GameTime gameTime)
	{
		var frameNumber = _platform.CopyFrame(_frameNumber, ref _pixels, out var width, out var height);
		if (frameNumber != _frameNumber && width == _screen.Width && height == _screen.Height)
		{
			if (_colors.Length != _pixels.Length)
			{
				_colors = new Color[_pixels.Length];
			}

			for (var i = 0; i < _pixels.Length; i++)
			{
				_colors[i] = ToColor(_pixels[i]);
			}

			_screen.SetData(_colors);
			_frameNumber = frameNumber;
		}

		GraphicsDevice.Clear(Color.Black);
		_spriteBatch.Begin(transformMatrix: _viewportAdapter.GetScaleMatrix(), samplerState: SamplerState.PointClamp);
		_spriteBatch.Draw(_screen, Vector2.Zero, Color.White);
		_spriteBatch.End();
		_windowManager.Draw();
		base.Draw(gameTime);
	}

	// Closing the window closes the game, as closing its window or cancelling its dialog does in the original.
	protected override void OnExiting(object sender, ExitingEventArgs args)
	{
		if (!_gameEnded)
		{
			if (_game.g_hDlg is { } dialog)
			{
				_game.PostToGame(() => _game.ClickDlgItem(dialog, windef.IDCANCEL));
			}
			else if (_game.hwndApp is { } window)
			{
				_game.PostMessage(window, winuser.WM_CLOSE, 0, 0);
			}

			_thread.Join(TimeSpan.FromSeconds(3));
		}

		base.OnExiting(sender, args);
	}
}
