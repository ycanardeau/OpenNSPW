using OpenNspw.Singleplayer;
using Xunit;

namespace OpenNspw.Tests;

// The scenario sets, titles and briefings read from the single-player executable.
public class ScenarioSelectionTests
{
	private static readonly byte[] Executable = File.ReadAllBytes(
		Path.Combine(GameData.FindSingleplayer(), "NSPW_122.exe")
	);

	// The side of each scenario, as docs/ComputerPlayer.md lists them from the setting screen.
	private static readonly int[] JapanScenarios =
	[
		2,
		4,
		8,
		9,
		10,
		100,
		101,
		104,
		105,
		108,
		200,
		201,
		209,
		301,
		302,
		304,
		305,
		306,
	];

	private static IEnumerable<ScenarioEntry> Entries() =>
		ScenarioSelection.Read(Executable).SelectMany(s => s.Scenarios);

	[Fact]
	public void SetsListEveryScenarioOnceInOrder()
	{
		Assert.Equal(Functions.Scenarios, Entries().Select(s => s.Number));
	}

	[Fact]
	public void TitlesAndHeadingsAreTheExecutables()
	{
		var sets = ScenarioSelection.Read(Executable);

		Assert.Equal("シナリオ選択画面（第一集、本ゲームの標準シナリオです。）", sets[0].Heading);
		Assert.Equal("シナリオ選択画面（第四集、難易度順には列記されていません）", sets[3].Heading);
		Assert.Equal("ハワイ沖大演習", sets[0].Scenarios[0].Title);
		Assert.Equal("日本上空迎撃戦", sets[0].Scenarios[^1].Title);
		Assert.Equal("沈黙の艦隊", sets[1].Scenarios[0].Title);
		Assert.Equal("ミッドウェイ島攻略", sets[2].Scenarios[^1].Title);
		Assert.Equal("沈黙の艦隊、再出撃", sets[3].Scenarios[^1].Title);
	}

	[Fact]
	public void SidesAreTheSettingScreens()
	{
		foreach (var entry in Entries())
		{
			var side = JapanScenarios.Contains(entry.Number)
				? ScenarioSide.Japan
				: ScenarioSide.UnitedStates;
			Assert.True(
				side == entry.Side,
				$"Scenario {entry.Number} gives {entry.Side}, not {side}."
			);
			Assert.Equal(side == ScenarioSide.Japan ? "日本海軍" : "合衆国海軍", entry.SideName);
		}
	}

	[Fact]
	public void BriefingsStateTheVictoryConditionsExceptScenario303()
	{
		foreach (var entry in Entries())
		{
			var states = entry.Briefing.Any(l =>
				l.StartsWith("勝敗条件：", StringComparison.Ordinal)
			);
			Assert.True(
				states == (entry.Number != 303),
				$"Scenario {entry.Number}: {string.Join("", entry.Briefing)}"
			);
		}
	}

	// Each briefing ends a sentence: none is cut where the setting screen's code or its strings were thought to end.
	// Scenario 303's trails off, as the message that destroys itself does.
	[Fact]
	public void BriefingsEndTheirLastSentence()
	{
		foreach (var entry in Entries())
		{
			var end = entry.Number == 303 ? "、、、" : "。";
			Assert.True(
				entry.Briefing[^1].EndsWith(end, StringComparison.Ordinal),
				$"Scenario {entry.Number} ends with {entry.Briefing[^1]}"
			);
		}
	}

	[Fact]
	public void BriefingsAreTheirScenarios()
	{
		var entries = Entries().ToDictionary(e => e.Number);

		Assert.Equal(
			"練習シナリオです。ハワイ南方に標的艦船が浮かんでいますので、",
			entries[1].Briefing[0]
		);
		Assert.Contains(entries[2].Briefing, l => l.Contains("真珠湾"));
		Assert.Contains(entries[107].Briefing, l => l.Contains("ヤマモト"));
		Assert.Contains(entries[206].Briefing, l => l.Contains("大和"));
		Assert.Contains(entries[305].Briefing, l => l.Contains("沖縄"));
		Assert.Equal("最低２つ作ってください。", entries[306].Briefing[^1]);
	}
}
