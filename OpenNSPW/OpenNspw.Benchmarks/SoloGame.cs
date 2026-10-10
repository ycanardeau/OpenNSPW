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
				throw new TimeoutException($"The game did not reach the state: dialog {Game.g_hDlg?.Id}, mode {Game.Mode}, frames {_platform.FrameCount}.");
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
		WaitUntil(() => Game.g_hDlg?.Id == IDD_MAIN_GAME && Game.ActivePlayerCount == 2);

		Do(g => g.ClickDlgItem(g.g_hDlg!, IDC_START_GAME));
		WaitUntil(() => _platform.FrameCount > 20);

		Click(512, 384);
		WaitUntil(() => Game.Mode == GameMode.GameSetting && Game.RivalMode == GameMode.GameSetting);

		Click(130, 150 + 12);
		WaitUntil(() => Game.Mode == GameMode.ConfigSetting && Game.RivalMode == GameMode.ConfigSetting);

		_platform.ParkAfterFrame = () => Game.Mode == GameMode.Battle;
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

	// The units of a side and category (a ship or a plane) that can be given orders (HandleInput), by number.
	private List<int> Units(Side side, UnitCategory category)
	{
		var units = new List<int>();
		for (var m = 1; m <= Game.MaxUnitId; m++)
		{
			ref var unit = ref Game.Units[m];
			if (unit.Side == side && unit.Category == category && unit.Supply == 0 && unit.Hp > 0 && !(unit.Kind >= UnitKind.AirBase && unit.Kind <= UnitKind.Fortress))
			{
				units.Add(m);
			}
		}

		return units;
	}

	private WorldPosition Center(List<int> units)
	{
		return new WorldPosition(units.Average(m => Game.Units[m].Position.X), units.Average(m => Game.Units[m].Position.Y));
	}

	// Selects units as the player does: cancels the selection (a right click), clicks the first unit, then adds the
	// others.
	private void Select(List<int> units)
	{
		Game.ClearSelection2(1);
		Game.set_the_slct_unit(units[0]);
		foreach (var m in units.Skip(1))
		{
			Game.SelectionCount++;
			Game.Selections[1][m] = Game.SelectionCount;
		}
	}

	// The selection of the rival's order: its ships, then its planes, each numbered from the first of its side, as
	// UpdateBattle reads them into Selections[0].
	private Array90<byte> RivalSelection(List<int> units)
	{
		var japan = Game.LocalSide != Side.Japan;
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

	// One tick of the battle, as UpdateFrame runs it, then the rival's messages.
	public void Tick()
	{
		Game.UpdateBattle();
		Game.CheckResult();
		Game.g_pThreadPool!.DoWork(DOWORK_TIMESLICE, 0);
	}

	// Ticks until the start of the next turn, when the player can give an order (CanOrder).
	private void TickUntilTurn()
	{
		do
		{
			Tick();
		} while (Game.CanOrder == 0);
	}

	// Orders units of both sides to move: the game's as the player does, by selecting them and clicking the point; the
	// rival's by its order (DP_NEW_PP). Both orders take effect at the next turn.
	private void OrderMove(List<int> own, WorldPosition ownPoint, List<int> rival, WorldPosition rivalPoint)
	{
		Select(own);
		Game.MoveOrders[1].Unit = (short)own[0];
		Game.MoveOrders[1].Destination = ownPoint;
		Game.HandleInput();

		_platform.Rival!.Order(new _DP_NEW_PP
		{
			dwType = MessageType.MoveOrder,
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
		var rivalSide = Game.LocalSide == Side.Japan ? Side.UnitedStates : Side.Japan;
		var ownShips = Units(Game.LocalSide, UnitCategory.Ship);
		var rivalShips = Units(rivalSide, UnitCategory.Ship);
		var ownPlanes = Units(Game.LocalSide, UnitCategory.Plane);
		var rivalPlanes = Units(rivalSide, UnitCategory.Plane);

		TickUntilTurn();
		OrderMove(ownShips, Center(rivalShips), rivalShips, Center(ownShips));
		TickUntilTurn();
		OrderMove(ownPlanes, Center(rivalShips), rivalPlanes, Center(ownShips));
	}

	// One frame of the battle at the normal speed: input, a tick, drawing and showing the frame (UpdateFrame), then the
	// rival's messages, as an iteration of WinMain's loop.
	public void Frame()
	{
		Game.UpdateFrame();
		Game.g_pThreadPool!.DoWork(DOWORK_TIMESLICE, 0);
	}

	// The drawing of a frame of the battle, without the tick.
	public void Draw()
	{
		Game.UpdateUnitInfo();
		Game.DrawBattleArea();
		Game.DrawMinimap();
	}

	// A hash of the units, fires, effects and clouds, and of the counts of ticks and random numbers.
	public string StateHash()
	{
		using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		hash.AppendData(MemoryMarshal.AsBytes<Unit>(Game.Units));
		hash.AppendData(MemoryMarshal.AsBytes<Fire>(Game.Fires));
		hash.AppendData(MemoryMarshal.AsBytes<Effect>(Game.Effects));
		hash.AppendData(MemoryMarshal.AsBytes<Cloud>(Game.Clouds));
		hash.AppendData(BitConverter.GetBytes(Game.Tick));
		hash.AppendData(BitConverter.GetBytes(Game.RandomCount));
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
