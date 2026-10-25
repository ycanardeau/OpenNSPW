using OpenNspw.Tests.Lockstep;
using Xunit;
using static OpenNspw.all_head;
using static OpenNspw.resource;
using static OpenNspw.Tests.Lockstep.BattleScripts;
using static OpenNspw.windef;

namespace OpenNspw.Tests;

// Characterization traces (docs/Refactoring.md): scripts that play two whole games in lockstep, whose traces are
// committed. A refactoring must leave every trace unchanged.
public class CharacterizationTests
{
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

	// Runs a script on two new games, and compares its trace with the committed one. Neither game may detect a desync
	// (first_r_error), which the games check every turn from the checksums they exchange.
	private static void Check(string name, Action<LockstepRun> script)
	{
		using var run = new LockstepRun();
		Exception? failure = null;
		try
		{
			script(run);
			Assert.Empty(run.Host.MessageBoxes);
			Assert.Empty(run.Guest.MessageBoxes);
			Assert.False(run.Host.Game.HasSavedDesync, "The host detected a desync.");
			Assert.False(run.Guest.Game.HasSavedDesync, "The guest detected a desync.");
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
		Check(
			$"scenario-{index + 1}",
			run =>
			{
				StartBattle(run, index);
				Battle(run, (uint)(1 + index), 2000);
				Assert.Equal(GameMode.Battle, run.Host.Game.Mode);
			}
		);
	}

	// The original's user scenarios tst3.dat and てすと.dat (Scenario/), whose fleets start close, so these cover fighting.
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	public void User_scenario_with_random_input(int index)
	{
		Check(
			$"user-scenario-{index}",
			run =>
			{
				StartUserBattle(run, index);
				Battle(run, (uint)(100 + index), 3000);
			}
		);
	}

	// ミッドウェイ攻略１, whose host starts with transports: those with a clear course land their cargo, which builds bases.
	[Fact]
	public void Transports_landing()
	{
		Check(
			"transports-landing",
			run =>
			{
				StartBattle(run, 5);
				var nspw = run.Host.Game;
				var input = new MouseInput(run, run.Host);
				var transports = Enumerable
					.Range(1, nspw.MaxUnitId)
					.Where(m =>
						nspw.Units[m].IsUsed
						&& nspw.Units[m].Kind == UnitKind.Transport
						&& nspw.Units[m].Side == nspw.LocalSide
					)
					.ToList();
				transports = [.. transports.Where(m => LandTransport(run, run.Host, input, m))];
				Assert.True(
					transports.Count >= 2,
					$"Only {transports.Count} transports could be ordered to land."
				);

				run.Until(
					() =>
						transports.All(m =>
							!nspw.Units[m].IsUsed || nspw.Units[m].Kind != UnitKind.Transport
						),
					10000
				);
				run.Frames(run.Host, 1000);
			}
		);
	}

	[Fact]
	public void Runs_are_deterministic()
	{
		var first = Play(Title);
		var second = Play(Title);
		Assert.Null(Traces.Difference(first, second));
	}
}
