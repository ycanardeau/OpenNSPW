using OpenNspw.Desktop.Controls;
using OpenNspw.Singleplayer;

namespace OpenNspw.Desktop.Screens;

// Choosing a scenario of NSPW 1.22 to play against the computer (docs/Singleplayer.md).
internal sealed class SingleplayerScreen(NspwGame game) : Screen(game)
{
	public override void LoadContent()
	{
		base.LoadContent();

		var form = new ScenarioBrowserForm(ScenarioSelection.Read(Game.ReadSingleplayerExecutable()));
		form.ScenarioSelected += (_, scenario) => Game.ShowScreen(new GameScreen(Game, () => Game.StartSession(scenario)));
		form.Canceled += (_, _) => Game.ShowScreen(new MainMenuScreen(Game));
		WindowManager.Root.Controls.Add(form);
		form.Show();
		form.Focus();
	}
}
