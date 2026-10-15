using OpenNspw.Singleplayer;
using Xunit;

namespace OpenNspw.Tests;

// The scenario sets and titles read from the single-player executable.
public class ScenarioSelectionTests
{
	private static readonly byte[] Executable = File.ReadAllBytes(Path.Combine(GameData.FindSingleplayer(), "NSPW_122.exe"));

	[Fact]
	public void SetsListEveryScenarioOnceInOrder()
	{
		var sets = ScenarioSelection.Read(Executable);

		Assert.Equal(Functions.Scenarios, sets.SelectMany(s => s.Scenarios).Select(s => s.Number));
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
}
