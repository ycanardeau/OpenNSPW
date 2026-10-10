using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static OpenNspw.all_head;
using static OpenNspw.resource;
using static OpenNspw.windef;
using static OpenNspw.winuser;

namespace OpenNspw.Benchmarks;

// A whole game with the original's data, against a ScriptedRival. It runs its WinMain (win_main.cpp) on its own thread
// through the connection dialogs, the title screen and the setting screens, as the player does, until the first frame
// of a battle. There its thread stops, and the battle's frames are run by the caller, with Tick, Frame and Draw.
internal sealed class SoloGame : IDisposable
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

	private readonly SoloPlatform _platform;
	private readonly Thread _thread;
	private Exception? _error;

	public Nspw Game { get; }

	private SoloGame(string dataDirectory)
	{
		_platform = new SoloPlatform(dataDirectory);
		Game = new Nspw(_platform);
		_thread = new Thread(() =>
		{
			try
			{
				Game.WinMain(null, null, "", SW_SHOWNORMAL);
			}
			catch (Exception e)
			{
				_error = e;
			}
		})
		{ IsBackground = true, Name = "Game" };
	}

	private static string FindOriginalData()
	{
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
		{
			var candidate = Path.Combine(directory.FullName, "Original", "NSPW_NET");
			if (File.Exists(Path.Combine(candidate, "t3.bmp")))
			{
				return candidate;
			}
		}

		throw new InvalidOperationException("Original/NSPW_NET is not found.");
	}

	private void ThrowIfFailed()
	{
		if (_error is not null)
		{
			throw new InvalidOperationException("The game failed.", _error);
		}

		if (_platform.MessageBoxes.Count > 0)
		{
			throw new InvalidOperationException($"The game showed a message box: {string.Join(", ", _platform.MessageBoxes)}");
		}
	}

	private void WaitUntil(Func<bool> condition)
	{
		var deadline = DateTime.UtcNow + Timeout;
		while (!condition())
		{
			ThrowIfFailed();
			if (!_thread.IsAlive || DateTime.UtcNow > deadline)
			{
				throw new TimeoutException($"The game did not reach the state: dialog {Game.g_hDlg?.Id}, mode {Game.mode}, frames {_platform.FrameCount}.");
			}

			Thread.Sleep(10);
		}
	}

	// Runs a user's action on the game's thread.
	private void Do(Action<Nspw> action)
	{
		Game.PostToGame(() => action(Game));
	}

	// Clicks the left mouse button at a point of the screen, as the user does.
	private void Click(int x, int y)
	{
		Game.SetCursorPos(x, y);
		Game.PostMouseInput(0, true);
		Thread.Sleep(100);
		Game.PostMouseInput(0, false);
	}

	// Hosts, starts the game with the rival, and clicks the title screen, the first scenario and Game Start (demo.cpp),
	// as TitleScreenTests does.
	private void StartBattle()
	{
		_thread.Start();
		WaitUntil(() => Game.g_hDlg?.Id == IDD_ADDRESS_OVERRIDE);
		Do(g =>
		{
			g.TypeDlgItemText(g.g_hDlg!, IDC_ADDRESS_LINE2, "0");
			g.ClickDlgItem(g.g_hDlg!, IDOK);
		});
		WaitUntil(() => Game.g_hDlg?.Id == IDD_MAIN_GAME && Game.g_dwNumberOfActivePlayers == 2);

		Do(g => g.ClickDlgItem(g.g_hDlg!, IDC_START_GAME));
		WaitUntil(() => _platform.FrameCount > 20);

		Click(512, 384);
		WaitUntil(() => Game.mode == CNCT_GAME_SETTING && Game.rival_mode == CNCT_GAME_SETTING);

		Click(130, 150 + 12);
		WaitUntil(() => Game.mode == CNCT_CNFG_SETTING && Game.rival_mode == CNCT_CNFG_SETTING);

		_platform.ParkAfterFrame = () => Game.mode == CMBT;
		Click(630 - 120 + 10, 700 + 12);
		_platform.WaitUntilParked(Timeout);
		ThrowIfFailed();
	}

	// A game at the first frame of a battle of the first scenario, stopped there.
	public static SoloGame Start()
	{
		var game = new SoloGame(FindOriginalData());
		try
		{
			game.StartBattle();
			return game;
		}
		catch
		{
			game.Dispose();
			throw;
		}
	}

	// The units of a side and category (SHIP or PLANE) that can be given orders (cnct_game_input_cont), by number.
	private List<int> Units(int side, int category)
	{
		var units = new List<int>();
		for (var m = 1; m <= Game.max_unit; m++)
		{
			ref var unit = ref Game.unit[m];
			if (unit.used == side && unit.ctgry == category && unit.spry == 0 && unit.hp[0] > 0 && !(unit.kind >= AP && unit.kind <= GF3))
			{
				units.Add(m);
			}
		}

		return units;
	}

	private (double X, double Y) Center(List<int> units)
	{
		return (units.Average(m => Game.unit[m].x), units.Average(m => Game.unit[m].y));
	}

	// Selects units as the player does: cancels the selection (a right click), clicks the first unit, then adds the
	// others.
	private void Select(List<int> units)
	{
		Game.cls_all_slct_unit_p2(1);
		Game.set_the_slct_unit(units[0]);
		foreach (var m in units.Skip(1))
		{
			Game.slct_unit_no++;
			Game.slct_unit[1][m] = Game.slct_unit_no;
		}
	}

	// The selection of the rival's order: its ships, then its planes, each numbered from the first of its side, as
	// chara_cont reads them into slct_unit[0].
	private Array90<byte> RivalSelection(List<int> units)
	{
		var japan = Game.your_side != JPN;
		var selection = new Array90<byte>();
		for (var i = 0; i < units.Count; i++)
		{
			var m = units[i];
			var index = m < JPN_PLANE_START
				? m - (japan ? 1 : USA_SHIP_START)
				: JPN_SHIP_END + m - (japan ? JPN_PLANE_START : USA_PLANE_START);
			selection[index] = (byte)(i + 1);
		}

		return selection;
	}

	// One tick of the battle, as updateFrame runs it, then the rival's messages.
	public void Tick()
	{
		Game.chara_cont();
		Game.cnct_decision();
		Game.g_pThreadPool!.DoWork(DOWORK_TIMESLICE, 0);
	}

	// Ticks until the start of the next turn, when the player can give an order (you_can_order).
	private void TickUntilTurn()
	{
		do
		{
			Tick();
		} while (Game.you_can_order == 0);
	}

	// Orders units of both sides to move: the game's as the player does, by selecting them and clicking the point; the
	// rival's by its order (DP_NEW_PP). Both orders take effect at the next turn.
	private void OrderMove(List<int> own, (double X, double Y) ownPoint, List<int> rival, (double X, double Y) rivalPoint)
	{
		Select(own);
		Game.new_pp[1].used = (short)own[0];
		Game.new_pp[1].x = ownPoint.X;
		Game.new_pp[1].y = ownPoint.Y;
		Game.cnct_game_input_cont();

		_platform.Rival!.Order(new _DP_NEW_PP
		{
			dwType = DP_NEW_PP,
			used = (byte)rival[0],
			x = (short)rivalPoint.X,
			y = (short)rivalPoint.Y,
			cls = 1,
			slct_unit = RivalSelection(rival),
		});
	}

	// Plays the first turns of the battle for both sides: each sends its ships at the other's ships, then its planes.
	public void SendForcesAtEachOther()
	{
		var rivalSide = Game.your_side == JPN ? USA : JPN;
		var ownShips = Units(Game.your_side, SHIP);
		var rivalShips = Units(rivalSide, SHIP);
		var ownPlanes = Units(Game.your_side, PLANE);
		var rivalPlanes = Units(rivalSide, PLANE);

		TickUntilTurn();
		OrderMove(ownShips, Center(rivalShips), rivalShips, Center(ownShips));
		TickUntilTurn();
		OrderMove(ownPlanes, Center(rivalShips), rivalPlanes, Center(ownShips));
	}

	// One frame of the battle at the normal speed: input, a tick, drawing and showing the frame (updateFrame), then the
	// rival's messages, as an iteration of WinMain's loop.
	public void Frame()
	{
		Game.updateFrame();
		Game.g_pThreadPool!.DoWork(DOWORK_TIMESLICE, 0);
	}

	// The drawing of a frame of the battle, without the tick.
	public void Draw()
	{
		Game.unit_info_cont();
		Game.draw_cmbt_area();
		Game.draw_map();
	}

	// A hash of the units, fires, effects and clouds, and of the counts of ticks and random numbers.
	public string StateHash()
	{
		using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		hash.AppendData(MemoryMarshal.AsBytes<UNIT>(Game.unit));
		hash.AppendData(MemoryMarshal.AsBytes<FIRE>(Game.fire));
		hash.AppendData(MemoryMarshal.AsBytes<EFFECT>(Game.effect));
		hash.AppendData(MemoryMarshal.AsBytes<KUMO>(Game.kumo));
		hash.AppendData(BitConverter.GetBytes(Game.cc_count));
		hash.AppendData(BitConverter.GetBytes(Game.rnd_count));
		return Convert.ToHexString(hash.GetHashAndReset())[..16];
	}

	public void Dispose()
	{
		_platform.Dispose();
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
