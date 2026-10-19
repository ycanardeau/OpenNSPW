using Microsoft.Xna.Framework;
using OpenNspw.Desktop.Controls;

namespace OpenNspw.Desktop.Screens;

// A game of the port: in a battle, the battle screen (BattleForm); on the setting screens, the setting form (SettingForm);
// on its other screens, its frames as it draws them. Its dialogs are forms (Dialogs.cs). The main menu comes back when
// the game ends.
//
// startSession: starts the game when the screen is shown, or hands over one already started. The screen owns the game
// from then on, and disposes it when it is unloaded, after its last frame is drawn.
internal sealed class GameScreen(NspwGame game, Func<GameSession> startSession) : Screen(game)
{
	private readonly Func<GameSession> _startSession = startSession;
	private GameSession _session = default!;
	private BattleArt _art = default!;
	private BattleForm _battleForm = default!;
	private SettingForm _settingForm = default!;
	private Backdrop _backdrop = default!;
	private DialogForms _dialogs = default!;
	private ConfirmForm _confirmQuit = default!;

	private void HideConfirmQuit()
	{
		_confirmQuit.Visible = false;
		if (_settingForm.Visible)
			_settingForm.Focus();
	}

	// Asks before Escape or F12 closes the game, over the screen and its dialogs; the key again cancels.
	private void ToggleConfirmQuit()
	{
		if (_confirmQuit.Visible)
		{
			HideConfirmQuit();
			return;
		}

		_confirmQuit.BringToFront();
		_confirmQuit.Visible = true;
		_confirmQuit.Focus();
	}

	public override void LoadContent()
	{
		base.LoadContent();
		_session = _startSession();

		// Under the dialogs, which are brought to the front as they open.
		_art = new BattleArt(GraphicsDevice, _session.SpriteSheetPath);
		_battleForm = new BattleForm(_session, Game.Fonts, _art) { Visible = false };
		WindowManager.Root.Controls.Add(_battleForm);
		_settingForm = new SettingForm(_session, Game.Fonts) { Visible = false };
		WindowManager.Root.Controls.Add(_settingForm);
		_backdrop = new Backdrop(GraphicsDevice);
		_dialogs = new DialogForms(_session.Game, WindowManager, Game.Fonts);

		_confirmQuit = new ConfirmForm(Game.Fonts, Texts.QuitGameTitle, Texts.QuitGameMessage, Texts.Quit) { Visible = false };
		_confirmQuit.Confirmed += (_, _) =>
		{
			HideConfirmQuit();
			_session.Quit();
		};
		_confirmQuit.Canceled += (_, _) => HideConfirmQuit();
		WindowManager.Root.Controls.Add(_confirmQuit);
	}

	public override void UnloadContent()
	{
		base.UnloadContent();
		_session.Dispose();
		_art.Dispose();
		_backdrop.Dispose();
	}

	public override void Update(GameTime gameTime)
	{
		if (_session.HasEnded)
		{
			Game.ShowScreen(new MainMenuScreen(Game));
			return;
		}

		_session.UpdateFrame(GraphicsDevice);
		var snapshot = _session.Snapshot;
		_battleForm.Visible = snapshot.IsBattle;
		_battleForm.UpdateFrom(snapshot);
		if (_settingForm.Visible != snapshot.IsSetting)
		{
			_settingForm.Visible = snapshot.IsSetting;
			if (snapshot.IsSetting)
				_settingForm.Focus();
		}

		_settingForm.UpdateFrom(snapshot);
		if (Game.IsActive)
		{
			var cursor = _settingForm.Visible ? SettingForm.GameCursor : (System.Drawing.Point?)null;
			_session.UpdateInput(Game.ViewportAdapter, point => _confirmQuit.Visible || _dialogs.Contains(point) || _battleForm.Owns(point) || _settingForm.Owns(point), cursor);
			if (_session.IsQuitRequested)
				ToggleConfirmQuit();
		}

		_dialogs.Update();
	}

	public override void Draw(GameTime gameTime)
	{
		if (_settingForm.Visible)
		{
			_backdrop.Draw(Game.SpriteBatch, Game.ViewportAdapter);
		}
		else if (!_session.IsBattleFrame)
		{
			_session.Draw(Game.SpriteBatch, Game.ViewportAdapter);
		}
	}
}
