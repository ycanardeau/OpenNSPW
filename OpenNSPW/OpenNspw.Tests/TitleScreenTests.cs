using System.IO.Compression;
using OpenNspw.DirectPlay;
using Xunit;
using static OpenNspw.all_head;
using static OpenNspw.resource;
using static OpenNspw.windef;
using static OpenNspw.winuser;

namespace OpenNspw.Tests;

// The platform of a whole game in the tests: the game's files in a copy of Original/NSPW_NET, DirectPlay 8 on Otsuki,
// the frames it shows, and text with the desktop app's rasterizer, where its font exists. No sound.
internal sealed class GameTestPlatform(string dataDirectory) : INspwPlatform
{
	private readonly string _dataDirectory = dataDirectory;
	private readonly Lock _frameLock = new();
	private readonly OpenNspw.Desktop.TextRasterizer _text = new();
	private ushort[] _frame = [];

	public DirectPlay8Factory DirectPlay { get; } = new();

	public List<string> MessageBoxes { get; } = [];

	public int FrameCount { get; private set; }

	public ushort[] LastFrame()
	{
		lock (_frameLock)
		{
			return [.. _frame];
		}
	}

	public Stream? OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
	{
		try
		{
			return new FileStream(GameFiles.Resolve(_dataDirectory, path), mode, access, share);
		}
		catch (IOException)
		{
			return null;
		}
	}

	public IReadOnlyList<string> FindFiles(string directory, string pattern) => GameFiles.Find(_dataDirectory, directory, pattern);

	public int CoCreateInstance(Guid rclsid, Guid riid, out object? ppv) => DirectPlay.CoCreateInstance(rclsid, riid, out ppv);

	public int MessageBox(HWND? hWnd, string lpText, string lpCaption, uint uType)
	{
		lock (MessageBoxes)
		{
			MessageBoxes.Add(lpText);
		}

		return IDOK;
	}

	public uint timeGetTime() => (uint)Environment.TickCount64;

	public int time() => 0;

	public void Present(DirectDrawSurface7 primary)
	{
		lock (_frameLock)
		{
			_frame = primary.Pixels.ToArray();
			FrameCount++;
		}
	}

	public TextBitmap? RasterizeText(string text, int height, string faceName) => _text.Rasterize(text, height);

	public ISound? CreateSound(WAVEFORMATEX format, ReadOnlySpan<byte> data) => null;

	public string? ReadSetting(string key) => null;

	public void WriteSetting(string key, string value)
	{
	}
}

// A whole game, running its WinMain (win_main.cpp) on its own thread, as the desktop app does.
internal sealed class RunningGame : IDisposable
{
	private readonly Thread _thread;

	public string PlayerName { get; }

	public GameTestPlatform Platform { get; }

	public Nspw Game { get; }

	public Exception? Error { get; private set; }

	public bool Ended => !_thread.IsAlive;

	public RunningGame(string dataDirectory, string playerName)
	{
		Platform = new GameTestPlatform(dataDirectory);
		Game = new Nspw(Platform);
		_thread = new Thread(() =>
		{
			try
			{
				Game.WinMain(null, null, "", SW_SHOWNORMAL);
			}
			catch (Exception e)
			{
				Error = e;
			}
		})
		{ IsBackground = true, Name = playerName };
		PlayerName = playerName;
	}

	public void Start()
	{
		_thread.Start();
	}

	// Runs a user's action on the game's thread.
	public void Do(Action<Nspw> action)
	{
		Game.PostToGame(() => action(Game));
	}

	public void Dispose()
	{
		if (_thread.IsAlive)
		{
			if (Game.hwndApp is { } window)
			{
				Game.PostMessage(window, WM_CLOSE, 0, 0);
			}
			else if (Game.g_hDlg is { } dialog)
			{
				Do(g => g.ClickDlgItem(dialog, IDCANCEL));
			}

			_thread.Join(TimeSpan.FromSeconds(5));
		}
	}
}

// Two whole games start, connect through the connection dialogs and reach the title screen, with the original's data.
public class TitleScreenTests
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

	private static void WaitUntil(Func<bool> condition, params RunningGame[] games)
	{
		string Status() => string.Join("; ", games.Select(g =>
			$"{g.PlayerName}: dialog {g.Game.g_hDlg?.Id}, players {g.Game.ActivePlayerCount}, mode {g.Game.Mode}, rival mode {g.Game.RivalMode}, frames {g.Platform.FrameCount}, ended {g.Ended}, boxes [{string.Join(", ", g.Platform.MessageBoxes)}]"));

		var deadline = DateTime.UtcNow + Timeout;
		while (!condition())
		{
			foreach (var game in games)
			{
				if (game.Error is { } error)
				{
					throw new InvalidOperationException($"{game.PlayerName}'s game failed.", error);
				}

				Assert.False(game.Ended, $"{game.PlayerName}'s game ended: {Status()}");
			}

			Assert.True(DateTime.UtcNow < deadline, "Timed out: " + Status());
			Thread.Sleep(10);
		}
	}

	private static uint Crc32(ReadOnlySpan<byte> data)
	{
		var crc = 0xFFFFFFFFu;
		foreach (var b in data)
		{
			crc ^= b;
			for (var k = 0; k < 8; k++)
			{
				crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
			}
		}

		return ~crc;
	}

	// Writes RGB565 pixels as a PNG file.
	private static void WritePng(string path, ushort[] pixels, int width, int height)
	{
		static byte[] Chunk(string type, byte[] data)
		{
			var chunk = new byte[data.Length + 12];
			System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(chunk, data.Length);
			System.Text.Encoding.ASCII.GetBytes(type).CopyTo(chunk, 4);
			data.CopyTo(chunk, 8);
			System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(data.Length + 8), Crc32(chunk.AsSpan(4, data.Length + 4)));
			return chunk;
		}

		var header = new byte[13];
		System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header, width);
		System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
		header[8] = 8;
		header[9] = 2;
		var raw = new MemoryStream();
		for (var y = 0; y < height; y++)
		{
			raw.WriteByte(0);
			for (var x = 0; x < width; x++)
			{
				var p = pixels[y * width + x];
				raw.WriteByte((byte)(((p >> 11) & 0x1F) << 3));
				raw.WriteByte((byte)(((p >> 5) & 0x3F) << 2));
				raw.WriteByte((byte)((p & 0x1F) << 3));
			}
		}

		var compressed = new MemoryStream();
		using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
		{
			raw.WriteTo(zlib);
		}

		using var file = File.Create(path);
		file.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);
		file.Write(Chunk("IHDR", header));
		file.Write(Chunk("IDAT", compressed.ToArray()));
		file.Write(Chunk("IEND", []));
	}

	// Clicks the left mouse button at a point of the screen, as the user does.
	private static void Click(RunningGame game, int x, int y)
	{
		game.Game.SetCursorPos(x, y);
		game.Game.PostMouseInput(0, true);
		Thread.Sleep(100);
		game.Game.PostMouseInput(0, false);
	}

	private static void SaveFrame(RunningGame game, string name)
	{
		WritePng(Path.Combine(AppContext.BaseDirectory, name), game.Platform.LastFrame(), SCRN_WIDTH, SCRN_HEIGHT);
	}

	// Starts two games, the host and the guest, on a copy of the original's data, connects them through the connection
	// dialogs over loopback, starts them from the host and waits for both to show the title screen. Then plays them.
	private static void PlayTwoGames(Action<RunningGame, RunningGame> play)
	{
		using var data = GameData.CreateCopy();
		using var host = new RunningGame(data.Path, "Alice");
		using var guest = new RunningGame(data.Path, "Bob");
		host.Start();
		guest.Start();
		WaitUntil(() => host.Game.g_hDlg?.Id == IDD_ADDRESS_OVERRIDE && guest.Game.g_hDlg?.Id == IDD_ADDRESS_OVERRIDE, host, guest);

		host.Do(g =>
		{
			g.TypeDlgItemText(g.g_hDlg!, IDC_ADDRESS_LINE2, "0");
			g.ClickDlgItem(g.g_hDlg!, IDOK);
		});
		WaitUntil(() => host.Game.g_hDlg?.Id == IDD_MAIN_GAME, host, guest);
		var port = host.Platform.DirectPlay.Peers.Single().LocalEndPoint!.Port;

		guest.Do(g =>
		{
			g.ClickDlgItem(g.g_hDlg!, IDC_HOST_SESSION);
			g.TypeDlgItemText(g.g_hDlg!, IDC_ADDRESS_LINE1, "127.0.0.1");
			g.TypeDlgItemText(g.g_hDlg!, IDC_ADDRESS_LINE2, $"{port}");
			g.ClickDlgItem(g.g_hDlg!, IDOK);
		});
		WaitUntil(() => host.Game.ActivePlayerCount == 2 && guest.Game.ActivePlayerCount == 2 && guest.Game.g_hDlg?.Id == IDD_MAIN_GAME, host, guest);

		host.Do(g => g.ClickDlgItem(g.g_hDlg!, IDC_START_GAME));
		WaitUntil(() => host.Platform.FrameCount > 20 && guest.Platform.FrameCount > 20, host, guest);

		play(host, guest);
		Assert.Empty(host.Platform.MessageBoxes);
		Assert.Empty(guest.Platform.MessageBoxes);
	}

	[Fact]
	public void Two_games_connect_and_reach_the_title_screen()
	{
		PlayTwoGames((host, guest) =>
		{
			Assert.Equal(GameMode.Title, host.Game.Mode);
			Assert.Equal(GameMode.Title, guest.Game.Mode);
			Assert.Equal(1, host.Game.IsHost);
			Assert.Equal(0, guest.Game.IsHost);

			SaveFrame(host, "title_host.png");
			Assert.True(host.Platform.LastFrame().Distinct().Count() > 16, "The title screen is drawn.");
		});
	}

	// The host clicks the title screen, the first scenario and Game Start (demo.cpp), and both games play the battle.
	[Fact]
	public void Two_games_start_a_battle()
	{
		PlayTwoGames((host, guest) =>
		{
			Click(host, 512, 384);
			WaitUntil(() => host.Game.Mode == GameMode.GameSetting && host.Game.RivalMode == GameMode.GameSetting, host, guest);
			SaveFrame(host, "game_setting_host.png");

			Click(host, 130, 150 + 12);
			WaitUntil(() => host.Game.Mode == GameMode.ConfigSetting && host.Game.RivalMode == GameMode.ConfigSetting, host, guest);
			SaveFrame(host, "config_setting_host.png");

			Click(host, 630 - 120 + 10, 700 + 12);
			WaitUntil(() => host.Game.Mode == GameMode.Battle && guest.Game.Mode == GameMode.Battle, host, guest);

			var frames = (host.Platform.FrameCount, guest.Platform.FrameCount);
			WaitUntil(() => host.Platform.FrameCount > frames.Item1 + 200 && guest.Platform.FrameCount > frames.Item2 + 200, host, guest);
			SaveFrame(host, "battle_host.png");
			SaveFrame(guest, "battle_guest.png");
			Assert.Equal(GameMode.Battle, host.Game.Mode);
			Assert.Equal(GameMode.Battle, guest.Game.Mode);
		});
	}
}