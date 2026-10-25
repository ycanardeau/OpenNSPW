using OpenNspw.Tests.Lockstep;
using Xunit;
using static OpenNspw.Tests.Lockstep.BattleScripts;

namespace OpenNspw.Tests;

// The commands of the host's UI (UICommands.cs, docs/UI.md) do what the clicks they stand for do. A script played with
// the commands and with the mouse sends the same orders, so the guest, which knows the host only by them, does the same
// in every round; and the host's battle is the same, but for the mouse and what it draws.
public class UICommandTests
{
	// The globals that only the mouse changes: the cursor, its buttons, and where the units' sprites were last put
	// (Sprites), which follows the cursor while a transport aims its landing.
	private static readonly string[] MouseGlobals = ["crsr_pt", "lf_btn", "ri_btn", "sprt"];

	// ミッドウェイ攻略１, whose host has transports: two of them are ordered to land, which moves the battle area with the
	// minimap, selects and deselects units, and gives landings and destinations. Then the right button deselects, and
	// selects the unit selected before again, and a click on the selected unit deselects it.
	private static void Script(LockstepRun run, BattleInput input)
	{
		StartBattle(run, 5);
		var nspw = run.Host.Game;
		var transports = Enumerable
			.Range(1, nspw.MaxUnitId)
			.Where(m =>
				nspw.Units[m].IsUsed
				&& nspw.Units[m].Kind == UnitKind.Transport
				&& nspw.Units[m].Side == nspw.LocalSide
			)
			.ToList();
		var landing = transports
			.Where(m => LandTransport(run, run.Host, input, m))
			.Take(2)
			.ToList();
		Assert.Equal(2, landing.Count);

		// Clicks are dropped while an order waits for its turn (DrawBattleArea).
		run.Until(() => !nspw.HasOrdered);
		var selected = nspw.SelectedUnit;
		Assert.NotEqual(0, selected);
		input.RightClick();
		Assert.Equal(0, nspw.SelectedUnit);
		input.RightClick();
		Assert.Equal(selected, nspw.SelectedUnit);
		run.Frames(run.Host, 40);
		input.ClickUnit(selected);
		Assert.Equal(0, nspw.SelectedUnit);
		run.Frames(run.Host, 300);
	}

	private static IReadOnlyList<string> Play(Func<LockstepRun, BattleInput> input)
	{
		using var run = new LockstepRun();
		Script(run, input(run));
		Assert.Empty(run.Host.MessageBoxes);
		Assert.Empty(run.Guest.MessageBoxes);
		Assert.False(run.Host.Game.HasSavedDesync, "The host detected a desync.");
		return run.FinishTrace();
	}

	// The trace without what the mouse makes different: the host's state and output in each round, but for its frames
	// and rand(), and the mouse's globals at the checkpoints.
	private static List<string> WithoutMouse(IReadOnlyList<string> trace, string host)
	{
		var lines = new List<string>();
		foreach (var line in trace)
		{
			var parts = line.Split(' ');
			if (parts[2] != host)
			{
				lines.Add(line);
			}
			else if (parts[0] == "r")
			{
				lines.Add($"r {parts[1]} {parts[2]} {parts[3]} {parts[6]}");
			}
			else if (!MouseGlobals.Contains(parts[3]))
			{
				lines.Add(line);
			}
		}

		return lines;
	}

	[Fact]
	public void Commands_send_the_orders_that_clicks_send()
	{
		var mouse = Play(run => new MouseInput(run, run.Host));
		var commands = Play(run => new CommandInput(run, run.Host));

		Assert.Null(
			Traces.Difference(WithoutMouse(mouse, "Alice"), WithoutMouse(commands, "Alice"))
		);
	}
}
