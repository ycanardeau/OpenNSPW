using Aigamo.Otsuki.Bluetooth;
using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.Desktop.Controls;

// Hosting or joining a game: the player's name, whether to host, and the address and the port, as the original's
// connection dialog (IDD_ADDRESS_OVERRIDE) asks for them. Where the app may use Bluetooth, the form also chooses between
// the Internet and Bluetooth, the dialog's service providers; over Bluetooth, a guest gives the host's device instead of
// the address and the port, typed or chosen from the paired devices. The form shows what the dialog has (Fill), and
// reports what the user does. Tab moves between the fields, Enter connects and Escape goes back.
internal sealed class ConnectionForm : Form
{
	// How many paired devices the form offers.
	private const int MaxDeviceButtons = 4;

	private readonly UIFonts _fonts;

	// Where the panel starts its fields, below the choice between the Internet and Bluetooth when there is one.
	private readonly int _top;

	private readonly DrawingRectangle _panelBounds;
	private readonly ActionButton _host;
	private readonly ActionButton _join;
	private readonly ActionButton? _internet;
	private readonly ActionButton? _bluetooth;
	private readonly TextField _name;
	private readonly TextField _address;
	private readonly TextField _port;
	private readonly TextField _device;

	// The computer's own Bluetooth address, which a host shows its guest.
	private readonly TextField _localDevice;
	private readonly ActionButton[] _deviceButtons;
	private readonly ActionButton _connect;
	private bool _isHost;
	private bool _overBluetooth;
	private bool _canConnect;
	private IReadOnlyList<BluetoothDevice> _pairedDevices = [];

	// Until the dialog's fields come (Fill), first and after switching hosting or the service provider, which set them
	// again: what is typed before would be lost, so the fields take nothing.
	private bool _awaitsFill = true;

	public event EventHandler? HostClicked;

	public event EventHandler? BluetoothClicked;

	public event EventHandler? Connecting;

	public event EventHandler? EditingScenario;

	public event EventHandler? Canceled;

	public string PlayerName => _name.Text;

	public bool IsHost => _isHost;

	public bool OverBluetooth => _overBluetooth;

	public string Address => _address.Text;

	public string Port => _port.Text;

	// The host's Bluetooth address, as typed or chosen.
	public string Device
	{
		get => _device.Text;
		set => _device.Text = value;
	}

	// The last message the game showed, such as a failure to connect.
	public string Status { get; set; } = "";

	private bool ShowsPairedDevices => _overBluetooth && !_isHost && _pairedDevices.Count > 0;

	private void UpdateControls()
	{
		_host.IsPrimary = _isHost;
		_join.IsPrimary = !_isHost;
		_host.Enabled = _join.Enabled = _canConnect && !_awaitsFill;
		if (_internet is not null && _bluetooth is not null)
		{
			_internet.IsPrimary = !_overBluetooth;
			_bluetooth.IsPrimary = _overBluetooth;
			_internet.Enabled = _bluetooth.Enabled = _canConnect && !_awaitsFill;
		}

		_name.Enabled = _port.Enabled = !_awaitsFill;
		_address.Enabled = !_isHost && !_awaitsFill;
		_address.Placeholder = _isHost
			? Texts.HostAddressPlaceholder
			: Texts.JoinAddressPlaceholder;
		_address.Visible = _port.Visible = !_overBluetooth;

		_device.Visible = _overBluetooth && !_isHost;
		_device.Enabled = !_awaitsFill;
		_localDevice.Visible = _overBluetooth && _isHost;
		for (var i = 0; i < _deviceButtons.Length; i++)
		{
			var button = _deviceButtons[i];
			button.Visible = ShowsPairedDevices && i < _pairedDevices.Count;
			button.Enabled = !_awaitsFill;
			if (i < _pairedDevices.Count)
				button.Text = _pairedDevices[i].Name;
		}

		_connect.Enabled = _canConnect && !_awaitsFill;
		_connect.Text =
			_awaitsFill ? "…"
			: !_canConnect ? Texts.Connecting
			: _isHost ? Texts.HostGame
			: Texts.JoinGame;
	}

	// Whether the game can connect now: not while it is connecting.
	public bool CanConnect
	{
		get => _canConnect;
		set
		{
			_canConnect = value;
			UpdateControls();
		}
	}

	private void Connect()
	{
		if (_canConnect && !_awaitsFill)
			Connecting?.Invoke(this, EventArgs.Empty);
	}

	// A choice of two segments, each switching something through the dialog, which sets the fields again: the form shows
	// that once Filled.
	private ActionButton CreateSegment(
		DrawingRectangle bounds,
		string text,
		Func<bool> isChosen,
		Action click
	)
	{
		var segment = new ActionButton(_fonts) { Bounds = bounds, Text = text };
		segment.Click += (_, _) =>
		{
			if (!isChosen() && !_awaitsFill)
			{
				_awaitsFill = true;
				UpdateControls();
				click();
			}
		};
		Controls.Add(segment);
		return segment;
	}

	private TextField CreateField(DrawingRectangle bounds)
	{
		var field = new TextField(_fonts) { Bounds = bounds };
		Controls.Add(field);
		return field;
	}

	// A paired device, which fills the host's address when clicked.
	private ActionButton CreateDeviceButton(DrawingRectangle bounds, int index)
	{
		var button = new ActionButton(_fonts)
		{
			Bounds = bounds,
			FontSize = 14,
			Visible = false,
		};
		button.Click += (_, _) =>
		{
			if (index < _pairedDevices.Count)
				_device.Text = _pairedDevices[index].EndPoint.ToString();
		};
		Controls.Add(button);
		return button;
	}

	// offersBluetooth: whether the app may use Bluetooth, so that the form offers it beside the Internet.
	public ConnectionForm(UIFonts fonts, bool offersBluetooth)
	{
		_fonts = fonts;
		_top = offersBluetooth ? 56 : 0;
		_panelBounds = new DrawingRectangle(262, 176 - _top / 2, 500, 440 + _top);
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = DrawingColor.Transparent;
		KeyPreview = true;

		var (x, width, y) = (_panelBounds.X + 32, _panelBounds.Width - 64, _panelBounds.Y);
		_host = CreateSegment(
			new DrawingRectangle(x, y + 32, width / 2, 40),
			Texts.Host,
			() => _isHost,
			() => HostClicked?.Invoke(this, EventArgs.Empty)
		);
		_join = CreateSegment(
			new DrawingRectangle(x + width / 2, y + 32, width / 2, 40),
			Texts.Join,
			() => !_isHost,
			() => HostClicked?.Invoke(this, EventArgs.Empty)
		);
		if (offersBluetooth)
		{
			_internet = CreateSegment(
				new DrawingRectangle(x, y + 84, width / 2, 36),
				Texts.Internet,
				() => !_overBluetooth,
				() => BluetoothClicked?.Invoke(this, EventArgs.Empty)
			);
			_bluetooth = CreateSegment(
				new DrawingRectangle(x + width / 2, y + 84, width / 2, 36),
				Texts.Bluetooth,
				() => _overBluetooth,
				() => BluetoothClicked?.Invoke(this, EventArgs.Empty)
			);
		}

		y += _top;
		_name = CreateField(new DrawingRectangle(x, y + 124, width, 40));
		_address = CreateField(new DrawingRectangle(x, y + 206, width, 40));
		_device = CreateField(new DrawingRectangle(x, y + 206, width, 40));
		_device.Placeholder = Texts.HostDevicePlaceholder;
		_localDevice = CreateField(new DrawingRectangle(x, y + 206, width, 40));
		_localDevice.Enabled = false;
		_localDevice.Placeholder = Texts.BluetoothOff;
		_port = CreateField(new DrawingRectangle(x, y + 288, 160, 40));
		_deviceButtons =
		[
			.. Enumerable
				.Range(0, MaxDeviceButtons)
				.Select(i =>
					CreateDeviceButton(
						new DrawingRectangle(
							x + i % 2 * (width / 2 + 4),
							y + 282 + i / 2 * 38,
							width / 2 - 4,
							32
						),
						i
					)
				),
		];

		_connect = new ActionButton(fonts)
		{
			Bounds = new DrawingRectangle(x, y + 360, width, 48),
			IsPrimary = true,
		};
		_connect.Click += (_, _) => Connect();
		Controls.Add(_connect);

		var back = new ActionButton(fonts)
		{
			Bounds = new DrawingRectangle(48, 688, 150, 44),
			Text = Texts.Back,
		};
		back.Click += (_, _) => Canceled?.Invoke(this, EventArgs.Empty);
		Controls.Add(back);

		var editor = new ActionButton(fonts)
		{
			Bounds = new DrawingRectangle(756, 688, 220, 44),
			Text = Texts.ScenarioEditor,
		};
		editor.Click += (_, _) => EditingScenario?.Invoke(this, EventArgs.Empty);
		Controls.Add(editor);

		UpdateControls();
	}

	// Fills the fields with what the game's dialog has: the saved name and address, its default port, which it sets
	// again whenever hosting is switched, and whether its service provider is Bluetooth.
	public void Fill(
		string playerName,
		bool isHost,
		bool overBluetooth,
		string address,
		string port
	)
	{
		_name.Text = playerName;
		_isHost = isHost;
		_overBluetooth = overBluetooth;
		_address.Text = address;
		_port.Text = port;
		_awaitsFill = false;
		UpdateControls();
	}

	// Shows the computer's own Bluetooth address, which a guest joins, or null if Bluetooth is off, and the paired
	// devices, which a guest can choose from.
	public void ShowBluetoothDevices(
		string? localDevice,
		IReadOnlyList<BluetoothDevice> pairedDevices
	)
	{
		_localDevice.Text = localDevice ?? "";
		_pairedDevices = [.. pairedDevices.Take(MaxDeviceButtons)];
		UpdateControls();
	}

	// The fields that take the focus in turn with Tab.
	private void FocusNextField()
	{
		TextField[] fields =
			_overBluetooth && _isHost ? [_name]
			: _overBluetooth ? [_name, _device]
			: _isHost ? [_name, _port]
			: [_name, _address, _port];
		var current = Array.FindIndex(fields, f => f.Focused);
		WindowManager.SetFocus(fields[(current + 1) % fields.Length]);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		switch (e.KeyCode)
		{
			case Keys.Tab:
				FocusNextField();
				break;
			case Keys.Enter:
				Connect();
				break;
			case Keys.Escape:
				Canceled?.Invoke(this, EventArgs.Empty);
				break;
		}
	}

	private void PaintLabel(Aigamo.Saruhashi.Graphics graphics, string text, int y)
	{
		Painter.SpacedText(
			graphics,
			text,
			_fonts.Bold(12),
			Theme.Brass,
			_panelBounds.X + 32,
			_panelBounds.Y + _top + y,
			2
		);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		var g = e.Graphics;
		Painter.SpacedText(g, Texts.MultiplayerKicker, _fonts.Bold(12), Theme.Brass, 48, 34, 3);
		Painter.Text(g, Texts.Connect, _fonts.Bold(34), Theme.Text, 48, 52);
		Painter.Text(g, Texts.ConnectDescription, _fonts.Regular(14), Theme.Muted, 48, 104);

		Painter.Fill(g, _panelBounds, Theme.Alpha(Theme.Panel, 0.9f));
		Painter.Frame(g, _panelBounds, Theme.Alpha(Theme.Line, 0.9f));
		Painter.Brackets(g, _panelBounds, 16, Theme.Brass);
		PaintLabel(g, Texts.PlayerName, 100);
		if (_overBluetooth)
		{
			PaintLabel(g, _isHost ? Texts.ThisDevice : Texts.HostDevice, 182);
			if (ShowsPairedDevices)
				PaintLabel(g, Texts.PairedDevices, 262);
		}
		else
		{
			PaintLabel(g, Texts.Address, 182);
			PaintLabel(g, Texts.Port, 264);
		}

		if (Status.Length > 0)
		{
			var font = _fonts.Regular(15);
			foreach (
				var (line, i) in Painter
					.Wrap(g, Status, font, _panelBounds.Width)
					.Take(3)
					.Select((l, i) => (l, i))
			)
				Painter.Text(
					g,
					line,
					font,
					Theme.Alert,
					_panelBounds.X,
					_panelBounds.Bottom + 18 + i * 24
				);
		}
	}
}
