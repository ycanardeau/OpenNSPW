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
	private Backdrop _backdrop = default!;
	private bool _hasDialog;

	// Whether the game screen has the game, once connected; until then, this screen owns it.
	private bool _isHandedOver;

	// Whether the form reads the dialog's fields again: at first, and once the dialog has changed them (DialogFilled).
	private bool _needsFill = true;

	// The game's connection dialog, while it exists, once it has set its fields (WM_INITDIALOG), as the game last posted
	// it.
	private WindowView? FindDialog()
	{
		return _session.Windows.FirstOrDefault(w =>
			w.Kind == "DIALOG" && w.Initialized && w.Id == resource.IDD_ADDRESS_OVERRIDE
		);
	}

	private static WindowView? Item(WindowView dialog, int item)
	{
		return dialog.Children.FirstOrDefault(c => c.Id == item);
	}

	private static string ItemText(WindowView dialog, int item)
	{
		return Item(dialog, item)?.Text ?? "";
	}

	// Switches hosting in the dialog, which sets the address and the port again, as the original's does.
	private void ClickHost()
	{
		if (FindDialog() is not { } dialog)
		{
			// The form waits for the fields: it gets them again as they are.
			_needsFill = true;
			return;
		}

		var playerName = _form.PlayerName;
		var (session, game, window) = (_session, _session.Game, dialog.Window);
		game.PostToGame(() =>
		{
			game.TypeDlgItemText(window, resource.IDC_PLAYER_NAME, playerName);
			game.ClickDlgItem(window, resource.IDC_HOST_SESSION);
			game.ShowChangedWindows();
			session.PostDialogFilled();
		});
	}

	private void Connect()
	{
		if (FindDialog() is not { } dialog)
		{
			return;
		}

		var (playerName, isHost, address, port) = (
			_form.PlayerName,
			_form.IsHost,
			_form.Address,
			_form.Port
		);
		var (game, window) = (_session.Game, dialog.Window);
		game.PostToGame(() =>
		{
			game.TypeDlgItemText(window, resource.IDC_PLAYER_NAME, playerName);
			if (
				(game.IsDlgButtonChecked(window, resource.IDC_HOST_SESSION) == winuser.BST_CHECKED)
				!= isHost
			)
			{
				game.ClickDlgItem(window, resource.IDC_HOST_SESSION);
			}

			game.TypeDlgItemText(window, resource.IDC_ADDRESS_LINE1, address);
			game.TypeDlgItemText(window, resource.IDC_ADDRESS_LINE2, port);
			game.ClickDlgItem(window, windef.IDOK);
		});
	}

	private void EditScenario()
	{
		if (FindDialog() is { } dialog)
		{
			var (game, window) = (_session.Game, dialog.Window);
			game.PostToGame(() => game.ClickDlgItem(window, resource.IDSNROEDIT));
		}
	}

	public override void LoadContent()
	{
		base.LoadContent();
		_backdrop = new Backdrop(GraphicsDevice);
		_session = Game.StartSession();

		_form = new ConnectionForm(Game.Fonts) { CanConnect = false };
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
		_backdrop.Dispose();
		if (!_isHandedOver)
		{
			_session.Dispose();
		}
	}

	public override void Update(GameTime gameTime)
	{
		_session.TakeMessages();
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
		if (_session.TakeDialogFilled() || _needsFill)
		{
			_needsFill = false;
			_form.Fill(
				ItemText(dialog, resource.IDC_PLAYER_NAME),
				Item(dialog, resource.IDC_HOST_SESSION)?.Checked == winuser.BST_CHECKED,
				ItemText(dialog, resource.IDC_ADDRESS_LINE1),
				ItemText(dialog, resource.IDC_ADDRESS_LINE2)
			);
		}

		_form.CanConnect = Item(dialog, windef.IDOK)?.Enabled == true;
		_form.Status = _session.LastMessage() ?? "";
	}

	public override void Draw(GameTime gameTime)
	{
		_backdrop.Draw(Game.SpriteBatch, Game.ViewportAdapter);
	}
}
