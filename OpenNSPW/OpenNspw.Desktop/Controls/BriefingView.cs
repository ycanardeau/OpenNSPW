using Aigamo.Saruhashi;
using OpenNspw.Singleplayer;
using Graphics = Aigamo.Saruhashi.Graphics;

namespace OpenNspw.Desktop.Controls;

// A scenario's briefing and its victory conditions (勝敗条件), as NSPW 1.22's setting screen states them, scrolled with the
// mouse wheel, by dragging the scroll bar, or a page at a time (ScrollPage).
internal sealed class BriefingView(UIFonts fonts) : Control
{
	// How the victory conditions start, in the game's words and in its English edition's.
	private static readonly string[] ConditionsStarts = ["勝敗条件：", "Victory Line : "];

	private const int LineHeight = 26;
	private const int ScrollBarWidth = 4;
	private const int TextRight = 16;

	private readonly UIFonts _fonts = fonts;

	// The lines as wrapped to the view's width, for the scenario they were wrapped for.
	private ScenarioEntry? _wrappedFor;
	private int _wrappedWidth;
	private List<string> _briefing = [];
	private List<string> _conditions = [];

	// How far the text is scrolled, in pixels.
	private float _scroll;

	// Where the scroll bar was grabbed, while it is dragged: the mouse's y, and the scroll then.
	private (int Y, float Scroll)? _drag;

	private ScenarioEntry? _scenario;

	public ScenarioEntry? Scenario
	{
		get => _scenario;
		set
		{
			_scenario = value;
			_scroll = 0;
		}
	}

	// Lines as one text: Japanese lines run on, Latin ones with a space between.
	private static string Join(IEnumerable<string> lines)
	{
		return lines
			.Where(l => l.Length != 0)
			.Aggregate(
				"",
				(text, line) =>
					text.Length != 0 && text[^1] < 0x80 && line[0] < 0x80
						? $"{text} {line}"
						: text + line
			);
	}

	// The briefing, and the victory conditions that end it, each as one text: the screen's lines are broken for its own
	// width.
	private static (string Briefing, string Conditions) Split(ScenarioEntry scenario)
	{
		var lines = scenario.Briefing.ToList();
		var start = lines.FindIndex(l =>
			ConditionsStarts.Any(s => l.StartsWith(s, StringComparison.Ordinal))
		);
		if (start < 0)
			return (Join(lines), "");

		var label = ConditionsStarts.First(s =>
			lines[start].StartsWith(s, StringComparison.Ordinal)
		);
		return (Join(lines.Take(start)), Join(lines.Skip(start))[label.Length..]);
	}

	private void Wrap(Graphics graphics, ScenarioEntry scenario)
	{
		var width = Width - TextRight;
		var (briefing, conditions) = Split(scenario);
		_briefing = Painter.Wrap(graphics, briefing, _fonts.Regular(16), width);
		_conditions = Painter.Wrap(graphics, conditions, _fonts.Regular(16), width - 20);
		_wrappedFor = scenario;
		_wrappedWidth = Width;
	}

	private int ConditionsHeight =>
		_conditions.Count == 0 ? 0 : 12 + 40 + _conditions.Count * LineHeight;

	private int ContentHeight => _briefing.Count * LineHeight + ConditionsHeight;

	private float MaxScroll => Math.Max(0, ContentHeight - Height);

	private void ScrollTo(float scroll)
	{
		_scroll = Math.Clamp(scroll, 0, MaxScroll);
	}

	// Scrolls by pages: up for a negative count.
	public void ScrollPage(int pages)
	{
		ScrollTo(_scroll + pages * (Height - LineHeight));
	}

	public void ScrollToStart()
	{
		ScrollTo(0);
	}

	public void ScrollToEnd()
	{
		ScrollTo(MaxScroll);
	}

	// The scroll bar's thumb, on the right edge: as long as the view is to the text, where the view is in it.
	private System.Drawing.Rectangle Thumb()
	{
		var length = Math.Max(24, Height * Height / Math.Max(1, ContentHeight));
		var top = MaxScroll == 0 ? 0 : (int)((Height - length) * _scroll / MaxScroll);
		return new(Width - ScrollBarWidth, top, ScrollBarWidth, length);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		ScrollTo(_scroll - e.Delta / 120f * LineHeight * 3);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (
			e.Button == MouseButtons.Left
			&& MaxScroll > 0
			&& e.Location.X >= Width - ScrollBarWidth - 8
		)
		{
			var thumb = Thumb();
			if (e.Location.Y < thumb.Top || e.Location.Y >= thumb.Bottom)
				ScrollPage(e.Location.Y < thumb.Top ? -1 : 1);
			else
				_drag = (e.Location.Y, _scroll);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (_drag is var (y, scroll))
		{
			var track = Height - Thumb().Height;
			_scroll = Math.Clamp(
				scroll + (e.Location.Y - y) * MaxScroll / Math.Max(1, track),
				0,
				MaxScroll
			);
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		_drag = null;
	}

	// Shades an edge where more text is beyond it.
	private void PaintFade(Graphics graphics, bool top)
	{
		for (var i = 0; i < 18; i++)
		{
			var y = top ? i : Height - 1 - i;
			Painter.Fill(
				graphics,
				new(0, y, Width - ScrollBarWidth - 4, 1),
				Theme.Alpha(Theme.Panel, 0.9f * (1 - i / 18f))
			);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var g = e.Graphics;
		if (Scenario is not { } scenario)
			return;

		if (_wrappedFor != scenario || _wrappedWidth != Width)
			Wrap(g, scenario);

		_scroll = Math.Clamp(_scroll, 0, MaxScroll);

		var y = -_scroll;
		foreach (var line in _briefing)
		{
			Painter.Text(
				g,
				line,
				_fonts.Regular(16),
				Theme.Lerp(Theme.Text, Theme.Muted, 0.15f),
				0,
				y
			);
			y += LineHeight;
		}

		if (_conditions.Count > 0)
		{
			y += 12;
			var box = new System.Drawing.Rectangle(
				0,
				(int)y,
				Width - TextRight,
				40 + _conditions.Count * LineHeight
			);
			Painter.Fill(g, box, Theme.Alpha(Theme.Brass, 0.09f));
			Painter.Fill(g, new(box.X, box.Y, 3, box.Height), Theme.Brass);
			Painter.Text(
				g,
				Texts.VictoryConditions,
				_fonts.Bold(13),
				Theme.Brass,
				box.X + 16,
				box.Y + 10
			);
			y += 34;
			foreach (var line in _conditions)
			{
				Painter.Text(g, line, _fonts.Regular(16), Theme.Text, box.X + 16, y);
				y += LineHeight;
			}
		}

		if (MaxScroll > 0)
		{
			if (_scroll > 0)
				PaintFade(g, top: true);

			if (_scroll < MaxScroll)
				PaintFade(g, top: false);

			Painter.Fill(
				g,
				new(Width - ScrollBarWidth, 0, ScrollBarWidth, Height),
				Theme.Alpha(Theme.Line, 0.5f)
			);
			Painter.Fill(g, Thumb(), _drag is null ? Theme.Brass : Theme.BrassLight);
		}
	}
}
