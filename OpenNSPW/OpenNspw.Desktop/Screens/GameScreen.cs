using Microsoft.Xna.Framework;

namespace OpenNspw.Desktop.Screens;

// A game of the port as it draws itself: its frames, and its dialogs as forms (Dialogs.cs). The main menu comes back
// when the game ends.
//
// startSession: starts the game when the screen is shown, or hands over one already started. The screen owns the game
// from then on, and disposes it when it is unloaded, after its last frame is drawn.
internal sealed class GameScreen(NspwGame game, Func<GameSession> startSession) : Screen(game)
{
	private readonly Func<GameSession> _startSession = startSession;
	private GameSession _session = default!;
	private DialogForms _dialogs = default!;

	public override void LoadContent()
	{
		base.LoadContent();
		_session = _startSession();
		_dialogs = new DialogForms(_session.Game, WindowManager);
	}

	public override void UnloadContent()
	{
		base.UnloadContent();
		_session.Dispose();
	}

	public override void Update(GameTime gameTime)
	{
		if (_session.HasEnded)
		{
			Game.ShowScreen(new MainMenuScreen(Game));
			return;
		}

		if (Game.IsActive)
		{
			_session.UpdateInput(Game.ViewportAdapter, _dialogs.Contains);
		}

		_dialogs.Update();
	}

	public override void Draw(GameTime gameTime)
	{
		_session.Draw(Game.SpriteBatch, Game.ViewportAdapter);
	}
}
