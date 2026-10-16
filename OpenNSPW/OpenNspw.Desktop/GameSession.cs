using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.ViewportAdapters;
using DrawingPoint = System.Drawing.Point;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace OpenNspw.Desktop;

// A game of the port: its WinMain (win_main.cpp) on its own thread, as the original runs it, its frames, and the
// keyboard and the mouse turned into DirectInput data and window messages.
internal sealed class GameSession : IDisposable
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

	private readonly DesktopPlatform _platform;
	private readonly Thread _thread;
	private ushort[] _pixels = [];
	private Color[] _colors = [];
	private int _frameNumber = -1;
	private KeyboardState _previousKeyboard;
	private MouseState _previousMouse;
	private volatile bool _hasEnded;

	// The state the game copies as it shows a frame (CaptureState), swapped with Snapshot when the UI takes the frame.
	private BattleSnapshot _captured = new();
	private bool _isDisposed;

	public Nspw Game { get; }

	// Whether WinMain has returned: the game was closed, or failed to connect.
	public bool HasEnded => _hasEnded;

	private void RunGame()
	{
		try
		{
			Game.WinMain(null, null, "", winuser.SW_SHOWNORMAL);
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
			_hasEnded = true;
		}
	}

	// singleplayer: NSPW_122.exe and a scenario of it, to play that scenario against the computer (docs/Singleplayer.md)
	// instead of connecting to a rival.
	public GameSession(string dataDirectory, TextRasterizer text, (byte[] Executable, int Scenario)? singleplayer = null)
	{
		_platform = new DesktopPlatform(dataDirectory, text);
		Game = new Nspw(_platform);
		_platform.CaptureState = () => _captured.CopyFrom(Game);
		if (singleplayer is var (executable, scenario))
		{
			Game.IsSingleplayer = true;
			Game.SingleplayerExecutable = executable;
			Game.ScenarioNumber = (short)scenario;
		}

		_thread = new Thread(RunGame) { IsBackground = true, Name = "NSPW" };
	}

	public void Start()
	{
		_thread.Start();
	}

	// The last message box the game showed, if any.
	public string? LastMessage()
	{
		lock (_platform.Messages)
		{
			return _platform.Messages.LastOrDefault();
		}
	}

	private void UpdateKeyboard()
	{
		var keyboard = Keyboard.GetState();
		foreach (var (key, (dik, vk)) in KeyCodes)
		{
			var down = keyboard.IsKeyDown(key);
			if (down != _previousKeyboard.IsKeyDown(key))
			{
				Game.PostKeyboardInput(dik, down);
				Game.SetKeyState(Game.hwndApp, vk, down);
			}
		}

		_previousKeyboard = keyboard;
	}

	// A press on a form (`isOnForm`, in game pixels) is the form's, not the game's; a release goes to the game anyway.
	private void UpdateMouse(ViewportAdapter viewportAdapter, Func<DrawingPoint, bool> isOnForm)
	{
		var mouse = Mouse.GetState();
		var position = viewportAdapter.PointToScreen(mouse.Position);
		Game.SetCursorPos(position.X, position.Y);
		if (!isOnForm(new DrawingPoint(position.X, position.Y)) || mouse.LeftButton == ButtonState.Released)
		{
			if (mouse.LeftButton != _previousMouse.LeftButton)
			{
				Game.PostMouseInput(0, mouse.LeftButton == ButtonState.Pressed);
			}

			if (mouse.RightButton != _previousMouse.RightButton)
			{
				Game.PostMouseInput(1, mouse.RightButton == ButtonState.Pressed);
			}
		}

		_previousMouse = mouse;
	}

	// Passes the keyboard and the mouse to the game.
	public void UpdateInput(ViewportAdapter viewportAdapter, Func<DrawingPoint, bool> isOnForm)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		UpdateKeyboard();
		UpdateMouse(viewportAdapter, isOnForm);
	}

	private static Color ToColor(ushort pixel)
	{
		var r = (pixel >> 11) & 0x1F;
		var g = (pixel >> 5) & 0x3F;
		var b = pixel & 0x1F;
		return new Color((r << 3) | (r >> 2), (g << 2) | (g >> 4), (b << 3) | (b >> 2));
	}

	// The game's last frame, once it has shown one.
	public Texture2D? Frame { get; private set; }

	// The game's state as it was when it showed the last frame. Read by the UI's thread.
	public BattleSnapshot Snapshot { get; private set; } = new();

	// Whether the last frame is a battle's: the battlefield, the sidebar and the radar.
	public bool IsBattleFrame => Snapshot.IsBattle;

	// Takes the game's last frame into `Frame`, if it has shown a new one.
	public void UpdateFrame(GraphicsDevice graphicsDevice)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if (Frame is null)
		{
			// Black until the game shows its first frame: a new texture holds whatever its memory held, such as the
			// screen before.
			Frame = new Texture2D(graphicsDevice, all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
			Frame.SetData(new Color[Frame.Width * Frame.Height]);
		}

		var frameNumber = _platform.CopyFrame(_frameNumber, ref _pixels, out var width, out var height, () => (Snapshot, _captured) = (_captured, Snapshot));
		if (frameNumber != _frameNumber && width == Frame.Width && height == Frame.Height)
		{
			if (_colors.Length != _pixels.Length)
			{
				_colors = new Color[_pixels.Length];
			}

			for (var i = 0; i < _pixels.Length; i++)
			{
				_colors[i] = ToColor(_pixels[i]);
			}

			Frame.SetData(_colors);
			_frameNumber = frameNumber;
		}
	}

	// Draws the game's last frame, the whole screen.
	public void Draw(SpriteBatch spriteBatch, ViewportAdapter viewportAdapter)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if (Frame is null)
		{
			return;
		}

		spriteBatch.Begin(transformMatrix: viewportAdapter.GetScaleMatrix(), samplerState: SamplerState.PointClamp);
		spriteBatch.Draw(Frame, Vector2.Zero, Color.White);
		spriteBatch.End();
	}

	// Closes the game, as closing its window or cancelling its dialog does in the original, and waits for it to end.
	public void Close()
	{
		if (!_hasEnded && _thread.IsAlive)
		{
			if (Game.g_hDlg is { } dialog)
			{
				Game.PostToGame(() => Game.ClickDlgItem(dialog, windef.IDCANCEL));
			}
			else if (Game.hwndApp is { } window)
			{
				Game.PostMessage(window, winuser.WM_CLOSE, 0, 0);
			}

			_thread.Join(TimeSpan.FromSeconds(3));
		}
	}

	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;
		Close();
		_platform.StopSounds();
		Frame?.Dispose();
	}
}
