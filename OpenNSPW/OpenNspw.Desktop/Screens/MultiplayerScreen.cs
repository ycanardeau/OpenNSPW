using Aigamo.Otsuki.Bluetooth;
using Microsoft.Xna.Framework;
using OpenNspw.Desktop.Controls;
using OpenNspw.DirectPlay;

namespace OpenNspw.Desktop.Screens;

// Hosting or joining a game. The form stands in for the game's connection dialog (IDD_ADDRESS_OVERRIDE): it is filled
// from the dialog, and fills and clicks the dialog in turn, so that the game connects as the original does. Over
// Bluetooth, the form also stands in for the dialog in which DirectPlay asks for the host's device: it gives the device
// to the app's Bluetooth before the game connects. Once the dialog is gone, connected or into the scenario editor, the
// game's own screens follow.
internal sealed class MultiplayerScreen(NspwGame game) : Screen(game)
{
	private GameSession _session = default!;
	private ConnectionForm _form = default!;
	private Backdrop _backdrop = default!;
	private bool _hasDialog;

	// Whether the game screen has the game, once connected; until then, this screen owns it.
	private bool _isHandedOver;

	// Set on the game's thread once the dialog has changed its fields.
	private volatile bool _needsFill = true;

	// Whether the form shows the computer's Bluetooth devices: only once Bluetooth is chosen, as the first Bluetooth call
	// may ask the user for permission.
	private bool _hasBluetoothDevices;

	// What the form found wrong before connecting, shown instead of the game's last message.
	private string? _error;

	// The game's connection dialog, while it exists, once it has set its fields (WM_INITDIALOG).
	private HWND? FindDialog()
	{
		lock (_session.Game.WindowsLock)
		{
			return _session.Game.windows.FirstOrDefault(w =>
				w.Kind == "DIALOG"
				&& w.Initialized
				&& !w.Destroyed
				&& w.Id == resource.IDD_ADDRESS_OVERRIDE
			);
		}
	}

	private string ItemText(HWND dialog, int item)
	{
		lock (_session.Game.WindowsLock)
		{
			return _session.Game.GetDlgItem(dialog, item)?.Text ?? "";
		}
	}

	// Whether the dialog's service provider is Bluetooth.
	private bool IsOverBluetooth(HWND dialog)
	{
		lock (_session.Game.WindowsLock)
		{
			return _session.Game.GetDlgItem(dialog, resource.IDC_SP_COMBO) is { } list
				&& list.CurSel >= 0
				&& list.CurSel < list.Items.Count
				&& list.Items[list.CurSel].Text == DirectPlay8Peer.BluetoothName;
		}
	}

	// Chooses a service provider in the dialog, as the user does, which sets the address and the port again. On the
	// game's thread.
	private static void SelectServiceProvider(Nspw game, HWND dialog, bool overBluetooth)
	{
		var name = overBluetooth ? DirectPlay8Peer.BluetoothName : DirectPlay8Peer.TcpIPName;
		var list = game.GetDlgItem(dialog, resource.IDC_SP_COMBO);
		var index = list?.Items.FindIndex(i => i.Text == name) ?? -1;
		if (index >= 0 && list!.CurSel != index)
		{
			game.SelectDlgItem(dialog, resource.IDC_SP_COMBO, index);
		}
	}

	// Switches between the Internet and Bluetooth in the dialog.
	private void ClickBluetooth()
	{
		_error = null;
		if (FindDialog() is not { } dialog)
		{
			_needsFill = true;
			return;
		}

		var overBluetooth = !_form.OverBluetooth;
		var game = _session.Game;
		game.PostToGame(() =>
		{
			SelectServiceProvider(game, dialog, overBluetooth);
			_needsFill = true;
		});
	}

	private void ShowBluetoothDevices()
	{
		if (_hasBluetoothDevices || Game.Bluetooth is not { } bluetooth)
		{
			return;
		}

		_hasBluetoothDevices = true;
		_form.ShowBluetoothDevices(
			bluetooth.FindLocalEndPoint()?.ToString(),
			bluetooth.GetPairedDevices()
		);
	}

	// Switches hosting in the dialog, which sets the address and the port again, as the original's does.
	private void ClickHost()
	{
		_error = null;
		if (FindDialog() is not { } dialog)
		{
			// The form waits for the fields: it gets them again as they are.
			_needsFill = true;
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

		var (playerName, isHost, overBluetooth, address, port) = (
			_form.PlayerName,
			_form.IsHost,
			_form.OverBluetooth,
			_form.Address,
			_form.Port
		);

		// A guest over Bluetooth joins the device chosen, which the game asks the app's Bluetooth for.
		if (overBluetooth && !isHost && Game.Bluetooth is { } bluetooth)
		{
			if (!BluetoothEndPoint.TryParse(_form.Device.Trim(), out var host))
			{
				_error = Texts.InvalidBluetoothAddress;
				return;
			}

			bluetooth.Host = host;
			UISettings.SaveBluetoothHost(host!.ToString());
		}

		_error = null;
		var game = _session.Game;
		game.PostToGame(() =>
		{
			game.TypeDlgItemText(dialog, resource.IDC_PLAYER_NAME, playerName);
			SelectServiceProvider(game, dialog, overBluetooth);
			if (
				(game.IsDlgButtonChecked(dialog, resource.IDC_HOST_SESSION) == winuser.BST_CHECKED)
				!= isHost
			)
			{
				game.ClickDlgItem(dialog, resource.IDC_HOST_SESSION);
			}

			// Bluetooth takes no address: the dialog's fields are disabled, and keep nothing typed.
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
		_backdrop = new Backdrop(GraphicsDevice);
		_session = Game.StartSession();

		_form = new ConnectionForm(Game.Fonts, Game.Bluetooth is not null)
		{
			CanConnect = false,
			Device = UISettings.LoadBluetoothHost(),
		};
		_form.HostClicked += (_, _) => ClickHost();
		_form.BluetoothClicked += (_, _) => ClickBluetooth();
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
			var overBluetooth = IsOverBluetooth(dialog);
			if (overBluetooth)
			{
				ShowBluetoothDevices();
			}

			_form.Fill(
				ItemText(dialog, resource.IDC_PLAYER_NAME),
				_session.Game.IsDlgButtonChecked(dialog, resource.IDC_HOST_SESSION)
					== winuser.BST_CHECKED,
				overBluetooth,
				ItemText(dialog, resource.IDC_ADDRESS_LINE1),
				ItemText(dialog, resource.IDC_ADDRESS_LINE2)
			);
		}

		_form.CanConnect = _session.Game.GetDlgItem(dialog, windef.IDOK)?.Enabled == true;
		_form.Status = _error ?? _session.LastMessage() ?? "";
	}

	public override void Draw(GameTime gameTime)
	{
		_backdrop.Draw(Game.SpriteBatch, Game.ViewportAdapter);
	}
}
