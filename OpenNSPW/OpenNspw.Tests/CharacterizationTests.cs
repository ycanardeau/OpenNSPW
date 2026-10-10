using OpenNspw.Tests.Lockstep;
using Xunit;
using static OpenNspw.all_head;
using static OpenNspw.resource;
using static OpenNspw.windef;

namespace OpenNspw.Tests;

// Characterization traces (docs/Refactoring.md): scripts that play two whole games in lockstep, whose traces are
// committed. A refactoring must leave every trace unchanged.
public class CharacterizationTests
{
	// Connects the games through the connection dialogs, as the user does, starts them from the host and waits for the
	// title screen.
	private static void Connect(LockstepRun run)
	{
		run.Until(() => run.Host.Game.g_hDlg?.Id == IDD_ADDRESS_OVERRIDE && run.Guest.Game.g_hDlg?.Id == IDD_ADDRESS_OVERRIDE);
		run.Host.Post(g =>
		{
			g.TypeDlgItemText(g.g_hDlg!, IDC_ADDRESS_LINE2, "0");
			g.ClickDlgItem(g.g_hDlg!, IDOK);
		});
		run.Until(() => run.Host.Game.g_hDlg?.Id == IDD_MAIN_GAME);

		run.Guest.Post(g =>
		{
			g.ClickDlgItem(g.g_hDlg!, IDC_HOST_SESSION);
			g.TypeDlgItemText(g.g_hDlg!, IDC_ADDRESS_LINE1, "127.0.0.1");
			g.TypeDlgItemText(g.g_hDlg!, IDC_ADDRESS_LINE2, $"{ADDRESSOVERRIDE_PORT}");
			g.ClickDlgItem(g.g_hDlg!, IDOK);
		});
		run.Until(() => run.Host.Game.g_dwNumberOfActivePlayers == 2 && run.Guest.Game.g_dwNumberOfActivePlayers == 2 && run.Guest.Game.g_hDlg?.Id == IDD_MAIN_GAME);

		run.Host.Post(g => g.ClickDlgItem(g.g_hDlg!, IDC_START_GAME));
		run.Until(() => run.Host.Game.mode == DEMO && run.Guest.Game.mode == DEMO);
	}

	// Runs a script on two new games, and checks its trace.
	private static IReadOnlyList<string> Play(Action<LockstepRun> script)
	{
		using var run = new LockstepRun();
		script(run);
		Assert.Empty(run.Host.MessageBoxes);
		Assert.Empty(run.Guest.MessageBoxes);
		return run.FinishTrace();
	}

	private static void Title(LockstepRun run)
	{
		Connect(run);
		run.Frames(run.Host, 100);
	}

	// Clicks the left mouse button at a point of the screen, as the user does.
	private static void Click(LockstepRun run, LockstepGame game, int x, int y)
	{
		game.Game.SetCursorPos(x, y);
		game.Game.PostMouseInput(0, true);
		run.Frames(game, 2);
		game.Game.PostMouseInput(0, false);
		run.Frames(game, 2);
	}

	// From the title screen, the host chooses the scenario at `index` in the list of the setting screen (demo.cpp) and
	// starts the battle.
	private static void StartBattle(LockstepRun run, int index)
	{
		Connect(run);
		run.Frames(run.Host, 20);
		Click(run, run.Host, 512, 384);
		run.Until(() => run.Host.Game.mode == CNCT_GAME_SETTING && run.Host.Game.rival_mode == CNCT_GAME_SETTING);
		Click(run, run.Host, 130, 150 + (index * 25) + 12);
		run.Until(() => run.Host.Game.mode == CNCT_CNFG_SETTING && run.Host.Game.rival_mode == CNCT_CNFG_SETTING);
		Click(run, run.Host, 630 - 120 + 10, 700 + 12);
		run.Until(() => run.Host.Game.mode == CMBT && run.Guest.Game.mode == CMBT);
	}

	// From the title screen, the host chooses the user scenario at `index` in the list of the load dialog (win_proc.cpp,
	// the files of Scenario/) and starts the battle.
	private static void StartUserBattle(LockstepRun run, int index)
	{
		Connect(run);
		run.Frames(run.Host, 20);
		Click(run, run.Host, 512, 384);
		run.Until(() => run.Host.Game.mode == CNCT_GAME_SETTING && run.Host.Game.rival_mode == CNCT_GAME_SETTING);
		Click(run, run.Host, 130, 150 + (8 * 25) + 12);
		run.Until(() => FileDialog(run.Host) is not null);
		var dialog = FileDialog(run.Host)!;
		run.Host.Post(g =>
		{
			g.SelectDlgItem(dialog, IDC_LIST, index);
			g.ClickDlgItem(dialog, IDOK);
		});
		run.Until(() => run.Host.Game.mode == CNCT_CNFG_SETTING && run.Host.Game.rival_mode == CNCT_CNFG_SETTING);
		Click(run, run.Host, 630 - 120 + 10, 700 + 12);
		run.Until(() => run.Host.Game.mode == CMBT && run.Guest.Game.mode == CMBT);
	}

	private static HWND? FileDialog(LockstepGame game)
	{
		return game.Game.windows.FirstOrDefault(w => w is { Destroyed: false, Id: IDD_FILE_CONT });
	}

	// Plays a battle with a monkey on each side, for `frames` frames of the host, or until the battle ends.
	private static void Battle(LockstepRun run, uint seed, int frames)
	{
		Monkey[] monkeys = [new(run.Host, seed), new(run.Guest, seed + 1000)];
		var end = run.Host.FrameCount + frames;
		run.Until(() => run.Host.FrameCount >= end || run.Host.Game.mode != CMBT, frames * 2, () =>
		{
			foreach (var monkey in monkeys)
			{
				monkey.Act();
			}
		});
	}

	// Runs a script on two new games, and compares its trace with the committed one.
	private static void Check(string name, Action<LockstepRun> script)
	{
		using var run = new LockstepRun();
		Exception? failure = null;
		try
		{
			script(run);
			Assert.Empty(run.Host.MessageBoxes);
			Assert.Empty(run.Guest.MessageBoxes);
		}
		catch (Exception e)
		{
			failure = e;
		}

		Traces.Check(name, run.FinishTrace(), failure);
	}

	[Fact]
	public void Title_screen()
	{
		Check("title", Title);
	}

	// The first scenarios of the setting screen. Their fleets start far apart, so these cover moving, the battle's
	// protocol and drawing more than fighting.
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void Scenario_with_random_input(int index)
	{
		Check($"scenario-{index + 1}", run =>
		{
			StartBattle(run, index);
			Battle(run, (uint)(1 + index), 2000);
			Assert.Equal(CMBT, run.Host.Game.mode);
		});
	}

	// The original's user scenarios tst3.dat and てすと.dat (Scenario/), whose fleets start close, so these cover fighting.
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	public void User_scenario_with_random_input(int index)
	{
		Check($"user-scenario-{index}", run =>
		{
			StartUserBattle(run, index);
			Battle(run, (uint)(100 + index), 3000);
		});
	}

	[Fact]
	public void Runs_are_deterministic()
	{
		var first = Play(Title);
		var second = Play(Title);
		Assert.Null(Traces.Difference(first, second));
	}
}
