using Aigamo.Saruhashi;
using Microsoft.Xna.Framework;
using GameScreenBase = MonoGame.Extended.Screens.GameScreen;

namespace OpenNspw.Desktop.Screens;

// A screen of the UI, whose forms are on the window manager while it is shown.
internal abstract class Screen(NspwGame game) : GameScreenBase(game)
{
	protected new NspwGame Game { get; } = game;

	protected WindowManager WindowManager => Game.WindowManager;

	public override void UnloadContent()
	{
		WindowManager.Root.Controls.Clear();
	}

	public override void Update(GameTime gameTime) { }

	public override void Draw(GameTime gameTime) { }
}
