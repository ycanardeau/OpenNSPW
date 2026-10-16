using Microsoft.Xna.Framework;
using OpenNspw.Desktop.Controls;

namespace OpenNspw.Desktop.Screens;

// A game of the port: in a battle, the battle screen (MainForm), whose parts show what the game draws in them; on its
// other screens, its frames as it draws them. Its dialogs are forms (Dialogs.cs). The main menu comes back when the game
// ends.
//
// startSession: starts the game when the screen is shown, or hands over one already started. The screen owns the game
// from then on, and disposes it when it is unloaded, after its last frame is drawn.
internal sealed class GameScreen(NspwGame game, Func<GameSession> startSession) : Screen(game)
{
	private readonly Func<GameSession> _startSession = startSession;
	private GameSession _session = default!;
	private MainForm _mainForm = default!;
	private DialogForms _dialogs = default!;

	public override void LoadContent()
	{
		base.LoadContent();
		_session = _startSession();

		// Under the dialogs, which are added as they open.
		_mainForm = new MainForm(_session) { Visible = false };
		WindowManager.Root.Controls.Add(_mainForm);
		_dialogs = new DialogForms(_session.Game, WindowManager, Game.Fonts);
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

		_session.UpdateFrame(GraphicsDevice);
		_mainForm.Visible = _session.IsBattleFrame;
		if (Game.IsActive)
		{
			_session.UpdateInput(Game.ViewportAdapter, _dialogs.Contains);
		}

		_dialogs.Update();
	}

	public override void Draw(GameTime gameTime)
	{
		if (!_session.IsBattleFrame)
		{
			_session.Draw(Game.SpriteBatch, Game.ViewportAdapter);
		}
	}
}
