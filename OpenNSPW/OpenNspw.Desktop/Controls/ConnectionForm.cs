using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.Desktop.Controls;

// Hosting or joining a game: the player's name, whether to host, and the address and the port, as the original's
// connection dialog (IDD_ADDRESS_OVERRIDE) asks for them. The form shows what the dialog has (Fill), and reports what
// the user does.
internal sealed class ConnectionForm : Form
{
	private readonly EditBox _name;
	private readonly CheckBoxControl _host;
	private readonly EditBox _address;
	private readonly EditBox _port;
	private readonly Button _connect;
	private readonly Label _status;

	public event EventHandler? HostClicked;

	public event EventHandler? Connecting;

	public event EventHandler? EditingScenario;

	public event EventHandler? Canceled;

	public string PlayerName => _name.Text;

	public bool IsHost => _host.Checked;

	public string Address => _address.Text;

	public string Port => _port.Text;

	// Whether the game can connect now: not while it is connecting.
	public bool CanConnect
	{
		get => _connect.Enabled;
		set => _connect.Enabled = value;
	}

	// The last message the game showed, such as a failure to connect.
	public string Status
	{
		get => _status.Text;
		set => _status.Text = value;
	}

	private static Label CreateLabel(DrawingRectangle bounds, string text)
	{
		return new Label { Bounds = bounds, Text = text, BackColor = DrawingColor.Transparent };
	}

	private static Button CreateButton(DrawingRectangle bounds, string text, EventHandler onClick)
	{
		var button = new Button { Bounds = bounds, Text = text };
		button.Click += onClick;
		return button;
	}

	// A host listens on its port, at no address of its own.
	private void UpdateHost()
	{
		_address.Enabled = !_host.Checked;
		_connect.Text = _host.Checked ? "Host" : "Join";
	}

	public ConnectionForm()
	{
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = Palette.SettingScreen;

		Controls.Add(new Label { Bounds = new DrawingRectangle(312, 200, 400, 20), Text = "Multiplayer", ForeColor = DrawingColor.Yellow, BackColor = DrawingColor.Transparent });

		Controls.Add(CreateLabel(new DrawingRectangle(312, 260, 110, 20), "Your Name:"));
		_name = new EditBox { Bounds = new DrawingRectangle(432, 258, 280, 22) };
		Controls.Add(_name);

		_host = new CheckBoxControl { Bounds = new DrawingRectangle(432, 300, 200, 20), Text = "Host Player", ForeColor = DrawingColor.White, BackColor = DrawingColor.Transparent };
		_host.Clicked += (_, _) => HostClicked?.Invoke(this, EventArgs.Empty);
		Controls.Add(_host);

		Controls.Add(CreateLabel(new DrawingRectangle(312, 340, 110, 20), "Address:"));
		_address = new EditBox { Bounds = new DrawingRectangle(432, 338, 280, 22) };
		Controls.Add(_address);

		Controls.Add(CreateLabel(new DrawingRectangle(312, 380, 110, 20), "Port:"));
		_port = new EditBox { Bounds = new DrawingRectangle(432, 378, 100, 22) };
		Controls.Add(_port);

		_connect = CreateButton(new DrawingRectangle(432, 430, 120, 35), "", (_, _) => Connecting?.Invoke(this, EventArgs.Empty));
		Controls.Add(_connect);

		_status = new Label { Bounds = new DrawingRectangle(312, 490, 400, 60), ForeColor = DrawingColor.Yellow, BackColor = DrawingColor.Transparent };
		Controls.Add(_status);

		Controls.Add(CreateButton(new DrawingRectangle(67, 690, 140, 35), "Back", (_, _) => Canceled?.Invoke(this, EventArgs.Empty)));
		Controls.Add(CreateButton(new DrawingRectangle(817, 690, 140, 35), "Scenario Editor", (_, _) => EditingScenario?.Invoke(this, EventArgs.Empty)));

		UpdateHost();
	}

	// Fills the fields with what the game's dialog has: the saved name and address, and its default port, which it sets
	// again whenever hosting is switched.
	public void Fill(string playerName, bool isHost, string address, string port)
	{
		_name.Text = playerName;
		_host.Checked = isHost;
		_address.Text = address;
		_port.Text = port;
		UpdateHost();
	}
}
