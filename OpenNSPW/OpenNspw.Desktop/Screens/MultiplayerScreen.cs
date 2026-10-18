using Microsoft.Xna.Framework;
using OpenNspw.Desktop.Controls;

namespace OpenNspw.Desktop.Screens;

// Hosting or joining a game. The form stands in for the game's connection dialog (IDD_ADDRESS_OVERRIDE): it is filled
// from the dialog, and fills and clicks the dialog in turn, so that the game connects as the original does. Once the
// dialog is gone, connected or into the scenario editor, the game's own screens follow.
internal sealed class MultiplayerScreen(NspwGame game) : Screen(game)
{
	private GameSession _session = default!;
	private ConnectionForm _form = default!;
	private bool _hasDialog;

	// Whether the game screen has the game, once connected; until then, this screen owns it.
	private bool _isHandedOver;

	// Set on the game's thread once the dialog has changed its fields.
	private volatile bool _needsFill = true;

	// The game's connection dialog, while it exists.
	private HWND? FindDialog()
	{
		lock (_session.Game.WindowsLock)
		{
			return _session.Game.windows.FirstOrDefault(w => w.Kind == "DIALOG" && !w.Destroyed && w.Id == resource.IDD_ADDRESS_OVERRIDE);
		}
	}

	private string ItemText(HWND dialog, int item)
	{
		lock (_session.Game.WindowsLock)
		{
			return _session.Game.GetDlgItem(dialog, item)?.Text ?? "";
		}
	}

	// Switches hosting in the dialog, which sets the address and the port again, as the original's does.
	private void ClickHost()
	{
		if (FindDialog() is not { } dialog)
		{
			return;
		}

		var playerName = _form.PlayerName;
		var game = _session.Game;
		game.PostToGame(() =>
		{
			game.TypeDlgItemText(dialog, resource.IDC_PLAYER_NAME, playerName);
			game.ClickDlgItem(dialog, resource.IDC_HOST_SESSION);
			_needsFill = true;
		});
	}

	private void Connect()
	{
		if (FindDialog() is not { } dialog)
		{
			return;
		}

		var (playerName, isHost, address, port) = (_form.PlayerName, _form.IsHost, _form.Address, _form.Port);
		var game = _session.Game;
		game.PostToGame(() =>
		{
			game.TypeDlgItemText(dialog, resource.IDC_PLAYER_NAME, playerName);
			if ((game.IsDlgButtonChecked(dialog, resource.IDC_HOST_SESSION) == winuser.BST_CHECKED) != isHost)
			{
				game.ClickDlgItem(dialog, resource.IDC_HOST_SESSION);
			}

			game.TypeDlgItemText(dialog, resource.IDC_ADDRESS_LINE1, address);
			game.TypeDlgItemText(dialog, resource.IDC_ADDRESS_LINE2, port);
			game.ClickDlgItem(dialog, windef.IDOK);
		});
	}

	private void EditScenario()
	{
		if (FindDialog() is { } dialog)
		{
			_session.Game.PostToGame(() => _session.Game.ClickDlgItem(dialog, resource.IDSNROEDIT));
		}
	}

	public override void LoadContent()
	{
		base.LoadContent();
		_session = Game.StartSession();

		_form = new ConnectionForm { CanConnect = false };
		_form.HostClicked += (_, _) => ClickHost();
		_form.Connecting += (_, _) => Connect();
		_form.EditingScenario += (_, _) => EditScenario();
		_form.Canceled += (_, _) => Game.ShowScreen(new MainMenuScreen(Game));
		WindowManager.Root.Controls.Add(_form);
		_form.Show();
		_form.Focus();
	}

	public override void UnloadContent()
	{
		base.UnloadContent();
		if (!_isHandedOver)
		{
			_session.Dispose();
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (_session.HasEnded)
		{
			Game.ShowScreen(new MainMenuScreen(Game));
			return;
		}

		var dialog = FindDialog();
		if (dialog is null)
		{
			// Before the game has created its dialog, nothing to do; after, the game goes on.
			if (_hasDialog)
			{
				_isHandedOver = true;
				Game.ShowScreen(new GameScreen(Game, () => _session));
			}

			return;
		}

		_hasDialog = true;
		if (_needsFill)
		{
			_needsFill = false;
			_form.Fill(
				ItemText(dialog, resource.IDC_PLAYER_NAME),
				_session.Game.IsDlgButtonChecked(dialog, resource.IDC_HOST_SESSION) == winuser.BST_CHECKED,
				ItemText(dialog, resource.IDC_ADDRESS_LINE1),
				ItemText(dialog, resource.IDC_ADDRESS_LINE2));
		}

		_form.CanConnect = _session.Game.GetDlgItem(dialog, windef.IDOK)?.Enabled == true;
		_form.Status = _session.LastMessage() ?? "";
	}
}
