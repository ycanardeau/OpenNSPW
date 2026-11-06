using Microsoft.Xna.Framework;
using OpenNspw.App.Controls;
using OpenNspw.Singleplayer;

namespace OpenNspw.App.Screens;

// Choosing a scenario of NSPW 1.22 to play against the computer (docs/Singleplayer.md).
internal sealed class SingleplayerScreen(NspwGame game) : Screen(game)
{
	private Backdrop _backdrop = default!;

	public override void LoadContent()
	{
		base.LoadContent();
		_backdrop = new Backdrop(GraphicsDevice);

		var form = new ScenarioBrowserForm(
			Game.Fonts,
			SingleplayerTexts.Of(ScenarioSelection.Sets),
			Game.LastScenario,
			Game.Options
		);
		form.ScenarioSelected += (_, scenario) =>
		{
			Game.LastScenario = scenario;
			Game.ShowScreen(new GameScreen(Game, () => Game.StartSession(scenario)));
		};
		form.Canceled += (_, _) => Game.ShowScreen(new MainMenuScreen(Game));
		form.OptionsChosen += (_, options) =>
		{
			Game.Options = options;
			UISettings.SaveOptions(options);
		};
		WindowManager.Root.Controls.Add(form);
		form.Show();
		form.Focus();
	}

	public override void UnloadContent()
	{
		base.UnloadContent();
		_backdrop.Dispose();
	}

	public override void Draw(GameTime gameTime)
	{
		_backdrop.Draw(Game.SpriteBatch, Game.ViewportAdapter);
	}
}
