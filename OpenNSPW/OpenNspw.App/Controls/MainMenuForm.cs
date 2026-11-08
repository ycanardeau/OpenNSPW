using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.App.Controls;

// The main menu's items, chosen with the mouse or with the arrow keys and Enter, over the title's picture that the
// screen draws: the settings, the language among them, are one of them (SettingsForm).
internal sealed class MainMenuForm : Form
{
	private readonly UIFonts _fonts;
	private readonly List<MenuItemButton> _items = [];

	private int _highlighted = -1;

	public event EventHandler? SingleplayerChosen;

	public event EventHandler? MultiplayerChosen;

	public event EventHandler? ReplaysChosen;

	public event EventHandler? QuitChosen;

	public event EventHandler? SettingsChosen;

	private void Highlight(int index)
	{
		_highlighted = index;
		for (var i = 0; i < _items.Count; i++)
			_items[i].IsHighlighted = i == index;
	}

	private void AddItem(string caption, string description, Func<EventHandler?> chosen)
	{
		var index = _items.Count;
		var item = new MenuItemButton(_fonts)
		{
			Bounds = new DrawingRectangle(64, 364 + index * 68, 520, 64),
			Text = caption,
			Description = description,
		};
		item.Hovered += (_, _) => Highlight(index);
		item.Click += (_, _) => chosen()?.Invoke(this, EventArgs.Empty);
		_items.Add(item);
		Controls.Add(item);
	}

	public MainMenuForm(UIFonts fonts)
	{
		_fonts = fonts;
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = DrawingColor.Transparent;
		KeyPreview = true;

		AddItem(Texts.SinglePlayer, Texts.SinglePlayerDescription, () => SingleplayerChosen);
		AddItem(Texts.Multiplayer, Texts.MultiplayerDescription, () => MultiplayerChosen);
		AddItem(Texts.Replays, Texts.ReplaysDescription, () => ReplaysChosen);
		AddItem(Texts.SettingsTitle, Texts.SettingsDescription, () => SettingsChosen);
		AddItem(Texts.Quit, Texts.QuitDescription, () => QuitChosen);
	}

	// The next item that can be chosen, from the highlighted one, in a direction.
	private int NextEnabled(int direction)
	{
		var index = _highlighted;
		for (var i = 0; i < _items.Count; i++)
		{
			index =
				((index < 0 && direction < 0 ? 0 : index) + direction + _items.Count)
				% _items.Count;
			if (_items[index].Enabled)
				return index;
		}

		return _highlighted;
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		switch (e.KeyCode)
		{
			case Keys.Up:
				Highlight(NextEnabled(-1));
				break;
			case Keys.Down:
				Highlight(NextEnabled(1));
				break;
			case Keys.Enter when _highlighted >= 0:
				_items[_highlighted].Press();
				break;
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		var g = e.Graphics;
		Painter.Text(
			g,
			"Naval South Pacific War on the Net 1.10  ·  © 2002 Ken-ichi Tokumitsu",
			_fonts.Regular(13),
			Theme.Muted,
			64,
			728
		);
		var brand = _fonts.Bold(13);
		Painter.SpacedText(g, "OPENNSPW", brand, Theme.Brass, Width - 64 - 8 * 14, 728, 5);
	}
}
