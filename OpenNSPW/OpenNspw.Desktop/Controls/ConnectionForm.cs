using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.Desktop.Controls;

// Hosting or joining a game: the player's name, whether to host, and the address and the port, as the original's
// connection dialog (IDD_ADDRESS_OVERRIDE) asks for them. The form shows what the dialog has (Fill), and reports what
// the user does. Tab moves between the fields, Enter connects and Escape goes back.
internal sealed class ConnectionForm : Form
{
	private static readonly DrawingRectangle PanelBounds = new(262, 176, 500, 440);

	private readonly UiFonts _fonts;
	private readonly ActionButton _host;
	private readonly ActionButton _join;
	private readonly TextField _name;
	private readonly TextField _address;
	private readonly TextField _port;
	private readonly ActionButton _connect;
	private bool _isHost;
	private bool _canConnect;

	// Until the dialog's fields come (Fill), first and after switching hosting, which sets them again: what is typed
	// before would be lost, so the fields take nothing.
	private bool _awaitsFill = true;

	public event EventHandler? HostClicked;

	public event EventHandler? Connecting;

	public event EventHandler? EditingScenario;

	public event EventHandler? Canceled;

	public string PlayerName => _name.Text;

	public bool IsHost => _isHost;

	public string Address => _address.Text;

	public string Port => _port.Text;

	// The last message the game showed, such as a failure to connect.
	public string Status { get; set; } = "";

	private void UpdateControls()
	{
		_host.IsPrimary = _isHost;
		_join.IsPrimary = !_isHost;
		_host.Enabled = _join.Enabled = _canConnect && !_awaitsFill;
		_name.Enabled = _port.Enabled = !_awaitsFill;
		_address.Enabled = !_isHost && !_awaitsFill;
		_address.Placeholder = _isHost ? "A host listens on its port" : "The host's address";
		_connect.Enabled = _canConnect && !_awaitsFill;
		_connect.Text = _awaitsFill ? "…" : !_canConnect ? "Connecting…" : _isHost ? "Host Game" : "Join Game";
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

	private static Label CreateLabel(DrawingRectangle bounds, string text)
	{
		return new Label { Bounds = bounds, Text = text, BackColor = DrawingColor.Transparent };
	}

	private void Connect()
	{
		if (_canConnect && !_awaitsFill)
			Connecting?.Invoke(this, EventArgs.Empty);
	}

	// Switches hosting through the dialog, which sets the address and the port again: the form shows that once Filled.
	private ActionButton CreateSegment(DrawingRectangle bounds, string text, bool isHost)
	{
		var segment = new ActionButton(_fonts) { Bounds = bounds, Text = text };
		segment.Click += (_, _) =>
		{
			if (_isHost != isHost && !_awaitsFill)
			{
				_awaitsFill = true;
				UpdateControls();
				HostClicked?.Invoke(this, EventArgs.Empty);
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

	public ConnectionForm(UiFonts fonts)
	{
		_fonts = fonts;
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = DrawingColor.Transparent;
		KeyPreview = true;

		var (x, width) = (PanelBounds.X + 32, PanelBounds.Width - 64);
		_host = CreateSegment(new DrawingRectangle(x, PanelBounds.Y + 32, width / 2, 40), "Host", true);
		_join = CreateSegment(new DrawingRectangle(x + width / 2, PanelBounds.Y + 32, width / 2, 40), "Join", false);
		_name = CreateField(new DrawingRectangle(x, PanelBounds.Y + 124, width, 40));
		_address = CreateField(new DrawingRectangle(x, PanelBounds.Y + 206, width, 40));
		_port = CreateField(new DrawingRectangle(x, PanelBounds.Y + 288, 160, 40));

		_connect = new ActionButton(fonts) { Bounds = new DrawingRectangle(x, PanelBounds.Y + 360, width, 48), IsPrimary = true };
		_connect.Click += (_, _) => Connect();
		Controls.Add(_connect);

		var back = new ActionButton(fonts) { Bounds = new DrawingRectangle(48, 688, 150, 44), Text = "Back" };
		back.Click += (_, _) => Canceled?.Invoke(this, EventArgs.Empty);
		Controls.Add(back);

		var editor = new ActionButton(fonts) { Bounds = new DrawingRectangle(796, 688, 180, 44), Text = "Scenario Editor" };
		editor.Click += (_, _) => EditingScenario?.Invoke(this, EventArgs.Empty);
		Controls.Add(editor);

		UpdateControls();
	}

	// Fills the fields with what the game's dialog has: the saved name and address, and its default port, which it sets
	// again whenever hosting is switched.
	public void Fill(string playerName, bool isHost, string address, string port)
	{
		_name.Text = playerName;
		_isHost = isHost;
		_address.Text = address;
		_port.Text = port;
		_awaitsFill = false;
		UpdateControls();
	}

	// The fields that take the focus in turn with Tab.
	private void FocusNextField()
	{
		TextField[] fields = _isHost ? [_name, _port] : [_name, _address, _port];
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
		Painter.SpacedText(graphics, text, _fonts.Bold(12), Theme.Brass, PanelBounds.X + 32, y, 2);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		var g = e.Graphics;
		Painter.SpacedText(g, "MULTIPLAYER  ·  NSPW ON THE NET 1.10", _fonts.Bold(12), Theme.Brass, 48, 34, 3);
		Painter.Text(g, "Connect", _fonts.Bold(34), Theme.Text, 48, 52);
		Painter.Text(g, "Two players: one hosts a game on a port, the other joins it at the host's address.", _fonts.Regular(14), Theme.Muted, 48, 104);

		Painter.Fill(g, PanelBounds, Theme.Alpha(Theme.Panel, 0.9f));
		Painter.Frame(g, PanelBounds, Theme.Alpha(Theme.Line, 0.9f));
		Painter.Brackets(g, PanelBounds, 16, Theme.Brass);
		PaintLabel(g, "PLAYER NAME", PanelBounds.Y + 100);
		PaintLabel(g, "ADDRESS", PanelBounds.Y + 182);
		PaintLabel(g, "PORT", PanelBounds.Y + 264);

		if (Status.Length > 0)
		{
			var font = _fonts.Regular(15);
			foreach (var (line, i) in Painter.Wrap(g, Status, font, PanelBounds.Width).Take(3).Select((l, i) => (l, i)))
				Painter.Text(g, line, font, Theme.Alert, PanelBounds.X, PanelBounds.Bottom + 18 + i * 24);
		}
	}
}
