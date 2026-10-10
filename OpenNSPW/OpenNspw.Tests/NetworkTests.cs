using System.Text;
using OpenNspw.DirectPlay;
using Xunit;
using static OpenNspw.all_head;
using static OpenNspw.dplay8;
using static OpenNspw.resource;
using static OpenNspw.windef;
using static OpenNspw.winuser;

namespace OpenNspw.Tests;

// The platform of a game in the tests: DirectPlay 8 on Otsuki, no files, and message boxes that are recorded and
// answered with OK.
internal sealed class NetworkTestPlatform : INspwPlatform
{
	public DirectPlay8Factory DirectPlay { get; } = new();

	public List<string> MessageBoxes { get; } = [];

	public Stream? OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
	{
		return null;
	}

	public int CoCreateInstance(Guid rclsid, Guid riid, out object? ppv)
	{
		return DirectPlay.CoCreateInstance(rclsid, riid, out ppv);
	}

	public int MessageBox(HWND? hWnd, string lpText, string lpCaption, uint uType)
	{
		MessageBoxes.Add(lpText);
		return IDOK;
	}

	public IReadOnlyList<string> FindFiles(string directory, string pattern) => NullPlatform.Instance.FindFiles(directory, pattern);

	public uint timeGetTime() => NullPlatform.Instance.timeGetTime();

	public int time() => NullPlatform.Instance.time();

	public void Present(DirectDrawSurface7 primary) => NullPlatform.Instance.Present(primary);

	public TextBitmap? RasterizeText(string text, int height, string faceName) => NullPlatform.Instance.RasterizeText(text, height, faceName);

	public ISound? CreateSound(WAVEFORMATEX format, ReadOnlySpan<byte> data) => NullPlatform.Instance.CreateSound(format, data);

	public string? ReadSetting(string key) => NullPlatform.Instance.ReadSetting(key);

	public void WriteSetting(string key, string value) => NullPlatform.Instance.WriteSetting(key, value);
}

// A sound buffer that does nothing.
internal sealed unsafe class SilentSoundBuffer : IDirectSoundBuffer
{
	public int Lock(uint dwOffset, uint dwBytes, void** ppvAudioPtr1, uint* pdwAudioBytes1, void** ppvAudioPtr2, uint* pdwAudioBytes2, uint dwFlags) => dsound.DSERR_INVALIDPARAM;

	public int Unlock(void* pvAudioPtr1, uint dwAudioBytes1, void* pvAudioPtr2, uint dwAudioBytes2) => 0;

	public int Play(uint dwReserved1, uint dwPriority, uint dwFlags) => 0;

	public int Stop() => 0;

	public int SetVolume(int lVolume) => 0;

	public int SetCurrentPosition(uint dwNewPosition) => 0;

	public uint Release() => 0;
}

// A game, until the end of its connection dialogs: what WinMain (win_main.cpp) does before creating the main window.
internal sealed class ConnectingGame : IDisposable
{
	private MSG _msg;

	public NetworkTestPlatform Platform { get; } = new();

	public Nspw Game { get; }

	// Whether the loop that waits for the connection still runs (wait_for_connect in WinMain).
	public bool WaitingForConnect { get; private set; } = true;

	public ConnectingGame(string playerName)
	{
		Game = new Nspw(Platform);

		// The defaults that WinMain reads from the registry.
		tchar.ShiftJis.GetBytes(playerName).CopyTo(Game.LocalPlayerName);
		tchar.ShiftJis.GetBytes("DirectPlay8 TCP/IP Service Provider").CopyTo(Game.PreferredProvider);
		tchar.ShiftJis.GetBytes("localhost").CopyTo(Game.RemoteHostName);

		// Normally created by InitDSound.
		for (var m = 0; m < NUM_SOUND_EFFECTS; m++)
		{
			for (var n = 0; n < SND_DUP; n++)
			{
				Game.lpDSB_[m][n] = new SilentSoundBuffer();
			}
		}

		Assert.Equal(0, Game.CoCreateInstance(CLSID_DirectPlay8ThreadPool, null, Nspw.CLSCTX_INPROC_SERVER, IID_IDirectPlay8ThreadPool, out Game.g_pThreadPool));
		Game.g_pThreadPool!.Initialize(null, Game.DirectPlayMessageHandler, DPNINITIALIZE_DISABLEPARAMVAL);
		Game.g_pThreadPool.SetThreadCount(unchecked((uint)-1), 0, 0);
		Assert.Equal(0, Game.CoCreateInstance(CLSID_DirectPlay8Peer, null, Nspw.CLSCTX_INPROC_SERVER, IID_IDirectPlay8Peer, out Game.g_pDP));
		Game.g_pDP!.Initialize(null, Game.DirectPlayMessageHandler, DPNINITIALIZE_DISABLEPARAMVAL);

		Game.g_hDlg = Game.CreateDialog(Game.hInstApp, MAKEINTRESOURCE(IDD_ADDRESS_OVERRIDE), null, Game.OverrideDlgProc);
	}

	// The port the host listens on.
	public int HostPort => Platform.DirectPlay.Peers.Single().LocalEndPoint!.Port;

	public HWND Dialog => Game.g_hDlg ?? throw new InvalidOperationException("No dialog.");

	public string DialogText(int id)
	{
		return Game.GetDlgItem(Dialog, id)!.Text;
	}

	// One iteration of WinMain's loop: the queued window messages, then DirectPlay's events.
	public void Pump()
	{
		while (Game.PeekMessage(ref _msg, null, 0, 0, PM_REMOVE) != 0)
		{
			if (_msg.message == WM_QUIT)
			{
				WaitingForConnect = false;
				break;
			}

			if (Game.IsDialogMessage(Game.g_hDlg, ref _msg) == 0)
			{
				Game.TranslateMessage(ref _msg);
				Game.DispatchMessage(ref _msg);
			}
		}

		Game.g_pThreadPool!.DoWork(DOWORK_TIMESLICE, 0);
	}

	public void Dispose()
	{
		Game.g_pDP?.Close(0);
		Game.g_pDP?.Release();
	}
}

// Two games in one process, connected over loopback UDP through the original's connection dialogs (dplay.cpp), with
// DirectPlay 8 on Otsuki.
public class NetworkTests
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

	private static void PumpUntil(Func<bool> condition, params ConnectingGame[] games)
	{
		var deadline = DateTime.UtcNow + Timeout;
		while (!condition())
		{
			Assert.True(DateTime.UtcNow < deadline, "Timed out.");
			foreach (var game in games)
			{
				game.Pump();
			}

			Thread.Sleep(1);
		}
	}

	// Connects a host and a guest, until both show the greeting dialog with each other's names.
	private static (ConnectingGame Host, ConnectingGame Guest) Connect()
	{
		var host = new ConnectingGame("Alice");
		var guest = new ConnectingGame("Bob");

		// The host keeps "Host Player" checked, and listens on any free port.
		Assert.Equal(BST_CHECKED, host.Game.IsDlgButtonChecked(host.Dialog, IDC_HOST_SESSION));
		host.Game.TypeDlgItemText(host.Dialog, IDC_ADDRESS_LINE2, "0");
		host.Game.ClickDlgItem(host.Dialog, IDOK);
		Assert.Equal(IDD_MAIN_GAME, host.Dialog.Id);

		// The guest unchecks it and enters the host's address.
		guest.Game.ClickDlgItem(guest.Dialog, IDC_HOST_SESSION);
		Assert.Equal("localhost", guest.DialogText(IDC_ADDRESS_LINE1));
		guest.Game.TypeDlgItemText(guest.Dialog, IDC_ADDRESS_LINE1, "127.0.0.1");
		guest.Game.TypeDlgItemText(guest.Dialog, IDC_ADDRESS_LINE2, $"{host.HostPort}");
		guest.Game.ClickDlgItem(guest.Dialog, IDOK);

		PumpUntil(
			() => guest.Dialog.Id == IDD_MAIN_GAME && guest.Game.ActivePlayerCount == 2 && host.Game.ActivePlayerCount == 2,
			host,
			guest);
		return (host, guest);
	}

	[Fact]
	public void Host_and_guest_connect_through_the_connection_dialogs()
	{
		var (host, guest) = Connect();
		using var _ = host;
		using var __ = guest;

		Assert.Equal("Host Player", host.Dialog.Text);
		Assert.Equal("Guest Player", guest.Dialog.Text);
		PumpUntil(() => host.DialogText(IDC_PLAYER_NAME2) == "Bob", host, guest);
		Assert.Equal("Alice", host.DialogText(IDC_PLAYER_NAME));
		Assert.Equal("Alice", guest.DialogText(IDC_PLAYER_NAME2));
		Assert.Equal("Bob", Encoding.ASCII.GetString(host.Game.RivalPlayerName[..3]));
		Assert.True(host.Game.GetDlgItem(host.Dialog, IDC_START_GAME)!.Enabled);
		Assert.NotEqual(0u, host.Game.g_dpnidRivalPlayer);
		Assert.Equal(1, host.Game.IsHostPlayer);
		Assert.Equal(0, guest.Game.IsHostPlayer);

		// The host starts the game; the guest follows when MSG_EXIT_WAITING arrives.
		host.Game.ClickDlgItem(host.Dialog, IDC_START_GAME);
		PumpUntil(() => !host.WaitingForConnect && !guest.WaitingForConnect, host, guest);
		Assert.Null(host.Game.g_hDlg);
		Assert.Null(guest.Game.g_hDlg);
		Assert.Empty(host.Platform.MessageBoxes);
		Assert.Empty(guest.Platform.MessageBoxes);
	}

	private static unsafe void Send<T>(ConnectingGame game, T message) where T : unmanaged
	{
		game.Game.bufferDesc.dwBufferSize = (uint)sizeof(T);
		game.Game.bufferDesc.pBufferData = (byte*)&message;
		game.Game.g_pDP!.SendTo(game.Game.g_dpnidRivalPlayer, ref game.Game.bufferDesc, 1, 0, null, ref game.Game.hAsync, MUST_SEND);
	}

	[Fact]
	public void The_rival_receives_the_game_messages()
	{
		var (host, guest) = Connect();
		using var _ = host;
		using var __ = guest;

		// The checksums of a turn (DP_FLAG_1), as chara_cont sends them.
		Send(host, new _DP_FLAG { dwType = MessageType.SyncFlag, cc_chk = 12, unit_chk = 34, rnd_chk = 56, ccc_wait_chk = 1, rival_mode = (short)GameMode.Battle });
		PumpUntil(() => guest.Game.CanAdvance1.Value == 1, host, guest);
		Assert.Equal(12, guest.Game.TickChecksums[0]);
		Assert.Equal(34, guest.Game.UnitChecksums[0]);
		Assert.Equal(56, guest.Game.RandomChecksums[0]);
		Assert.Equal(1, guest.Game.TickWaits[0]);
		Assert.Equal(GameMode.Battle, guest.Game.RivalMode);

		// An order (DP_NEW_PP_SHIP).
		var order = new _DP_NEW_PP_SHIP { dwType = MessageType.MoveShipsOrder, used = 1, x = -1234, y = 567, cls = 1 };
		order.slct_unit[0] = 3;
		order.slct_unit[JPN_SHIP_END - 1] = 7;
		Send(guest, order);
		PumpUntil(() => host.Game.CanAdvance2.Value == 1, host, guest);
		Assert.Equal(1, host.Game.BufferedMoveOrders[0].Unit);
		Assert.Equal(-1234.0, host.Game.BufferedMoveOrders[0].Destination.X);
		Assert.Equal(567.0, host.Game.BufferedMoveOrders[0].Destination.Y);
		Assert.Equal(3, host.Game.BufferedSelections[0][0]);
		Assert.Equal(7, host.Game.BufferedSelections[0][JPN_SHIP_END - 1]);

		// A chat message (DP_CHAT_1). CHAT_DSP_TIME (400) does not fit in friend_chat_dsp_time, a BYTE.
		var chat = new _DP_DATA_20 { dwType = MessageType.Chat };
		tchar.ShiftJis.GetBytes("こんにちは").CopyTo(chat.friend_chat);
		Send(host, chat);
		PumpUntil(() => guest.Game.RivalChatDisplayTime != 0, host, guest);
		Assert.Equal(144, guest.Game.RivalChatDisplayTime);
		Assert.Equal("こんにちは", tchar.ShiftJis.GetString(guest.Game.RivalChat[..10]));

		// The game settings, which the guest only takes in the setting modes.
		guest.Game.Mode = GameMode.GameSetting;
		var settings = new _DP_DATA_1 { dwType = MessageType.SideAndScenario };
		for (short i = 0; i < 10; i++)
		{
			settings.data[i] = (short)(i + 1);
		}

		Send(host, settings);
		PumpUntil(() => guest.Game.ScenarioNumber == 2, host, guest);
		Assert.Equal(1, guest.Game.HostSide);
		Assert.Equal(3, guest.Game.SupplyRates[0]);
		Assert.Equal(4, guest.Game.SupplyRates[1]);
		Assert.Equal(5, guest.Game.IsDecisionEnabled.Value);
		Assert.Equal(8, guest.Game.ArrivalControl);
		Assert.Equal(10, guest.Game.SwapRule);

		// GO_GAME_SETTING sends a guest on the title screen to the game setting screen (go_cnct_game_setting).
		guest.Game.Mode = GameMode.Title;
		Send(host, new GENERICMSG { dwType = MessageType.GoToGameSetting });
		PumpUntil(() => guest.Game.Mode == GameMode.GameSetting, host, guest);
		Assert.Equal(0, guest.Game.ScenarioNumber);
		Assert.Equal(1, guest.Game.IsDecisionEnabled.Value);
		Assert.Equal(0, guest.Game.HasAutoSave);
	}

	[Fact]
	public void The_host_sees_the_guest_leave()
	{
		var (host, guest) = Connect();
		using var _ = host;

		guest.Dispose();
		PumpUntil(() => host.Game.ActivePlayerCount == 1, host);
		PumpUntil(() => host.DialogText(IDC_PLAYER_NAME2) == " - - - ", host);
		Assert.False(host.Game.GetDlgItem(host.Dialog, IDC_START_GAME)!.Enabled);
	}
}
