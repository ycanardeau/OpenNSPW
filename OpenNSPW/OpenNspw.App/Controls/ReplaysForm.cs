using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.App.Controls;

// The replays of the battles played (docs/Replays.md), newest first, a page at a time: watched, deleted after asking, or
// shown in their folder. Chosen with the mouse, or with the arrow keys, Page Up and Down, Enter, Delete and Escape.
internal sealed class ReplaysForm : Form
{
	private const int RowHeight = 40;
	private const int RowsPerPage = 11;

	private readonly UIFonts _fonts;
	private readonly IReadOnlyList<ReplayRow> _rows;
	private readonly ActionButton _watch;
	private readonly ActionButton _delete;
	private int _selected;

	public event EventHandler<string>? WatchChosen;

	public event EventHandler<string>? DeleteChosen;

	public event EventHandler? OpenFolderChosen;

	public event EventHandler? Canceled;

	private int Page => _selected / RowsPerPage;

	private int Pages => Math.Max(1, (_rows.Count + RowsPerPage - 1) / RowsPerPage);

	private ReplayRow? Selected => _rows.Count == 0 ? null : _rows[_selected];

	private void Select(int index)
	{
		_selected = Math.Clamp(index, 0, Math.Max(0, _rows.Count - 1));
		for (var i = 0; i < _rows.Count; i++)
		{
			_rows[i].Visible = i / RowsPerPage == Page;
			_rows[i].IsSelected = i == _selected;
		}

		_watch.Enabled = Selected?.Summary is not null;
		_delete.Enabled = Selected is not null;
	}

	private void Watch()
	{
		if (Selected is { Summary: not null } row)
			WatchChosen?.Invoke(this, row.Path);
	}

	private void Delete()
	{
		if (Selected is { } row)
			DeleteChosen?.Invoke(this, row.Path);
	}

	private ActionButton AddButton(int x, int width, string text, bool isPrimary, Action onClick)
	{
		var button = new ActionButton(_fonts)
		{
			Bounds = new DrawingRectangle(x, 688, width, 44),
			Text = text,
			IsPrimary = isPrimary,
		};
		button.Click += (_, _) => onClick();
		Controls.Add(button);
		return button;
	}

	// rows: the replays, newest first. selected: the one to select first, such as the one after a replay deleted.
	public ReplaysForm(UIFonts fonts, IReadOnlyList<ReplayRow> rows, int selected)
	{
		_fonts = fonts;
		_rows = rows;
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = DrawingColor.Transparent;
		KeyPreview = true;

		for (var i = 0; i < rows.Count; i++)
		{
			var index = i;
			var row = rows[i];
			row.Bounds = new DrawingRectangle(
				48,
				160 + i % RowsPerPage * RowHeight,
				928,
				RowHeight
			);
			row.Click += (_, _) =>
			{
				if (index == _selected)
					Watch();
				else
					Select(index);
			};
			Controls.Add(row);
		}

		AddButton(48, 150, Texts.Back, false, () => Canceled?.Invoke(this, EventArgs.Empty));
		_delete = AddButton(214, 150, Texts.Delete, false, Delete);
		AddButton(
			380,
			190,
			Texts.OpenFolder,
			false,
			() => OpenFolderChosen?.Invoke(this, EventArgs.Empty)
		);
		_watch = AddButton(826, 150, Texts.Watch, true, Watch);
		Select(selected);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		switch (e.KeyCode)
		{
			case Keys.Up when _rows.Count > 0:
				Select((_selected - 1 + _rows.Count) % _rows.Count);
				break;
			case Keys.Down when _rows.Count > 0:
				Select((_selected + 1) % _rows.Count);
				break;
			case Keys.PageUp:
				Select(_selected - RowsPerPage);
				break;
			case Keys.PageDown:
				Select(_selected + RowsPerPage);
				break;
			case Keys.Enter:
				Watch();
				break;
			case Keys.Delete:
				Delete();
				break;
			case Keys.Escape:
				Canceled?.Invoke(this, EventArgs.Empty);
				break;
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		var g = e.Graphics;
		Painter.SpacedText(g, Texts.ReplaysKicker, _fonts.Bold(12), Theme.Brass, 48, 34, 3);
		Painter.Text(g, Texts.RecordedBattles, _fonts.Bold(34), Theme.Text, 48, 52);
		Painter.Text(g, Texts.RecordedBattlesDescription, _fonts.Regular(14), Theme.Muted, 48, 112);
		Painter.Fill(g, new(48, 146, 928, 1), Theme.Alpha(Theme.Line, 0.9f));

		var small = _fonts.Regular(13);
		if (_rows.Count == 0)
			Painter.Text(g, Texts.NoReplays, _fonts.Regular(16), Theme.Muted, 48, 176);
		else
		{
			var page = Texts.ReplayPage(Page + 1, Pages);
			Painter.Text(
				g,
				page,
				small,
				Theme.Muted,
				976 - Painter.Measure(g, page, small).Width,
				120
			);
		}

		var hint = Texts.ReplaysHint;
		Painter.Text(
			g,
			hint,
			small,
			Theme.Muted,
			(Width - Painter.Measure(g, hint, small).Width) / 2,
			650
		);
	}
}
