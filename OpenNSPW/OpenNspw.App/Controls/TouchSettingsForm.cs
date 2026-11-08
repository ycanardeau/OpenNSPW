using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.App.Controls;

// The touch options, to try them (TouchSettings): a row of choices for each, the one chosen in brass, taking effect at
// once, also over a battle; over the whole screen, dimmed, so that nothing under it takes a touch. Close closes it.
internal sealed class TouchSettingsForm : Form
{
	// A choice of a row: its text, whether it is the one chosen, and the options with it chosen.
	private sealed record Choice(
		string Text,
		Func<TouchOptions, bool> IsChosen,
		Func<TouchOptions, TouchOptions> Choose
	);

	private const int LabelWidth = 300;
	private const int ChoiceWidth = 140;
	private const int ChoiceHeight = 44;
	private const int Gap = 8;
	private const int RowHeight = ChoiceHeight + 12;
	private const int Rows = 7;
	private const int Padding = 28;
	private const int HeaderHeight = 72;
	private const int FooterHeight = 72;

	private static readonly DrawingRectangle PanelBounds = new(
		(all_head.SCRN_WIDTH - (Padding * 2 + LabelWidth + ChoiceWidth * 4 + Gap * 3)) / 2,
		(all_head.SCRN_HEIGHT - (HeaderHeight + RowHeight * Rows + FooterHeight)) / 2,
		Padding * 2 + LabelWidth + ChoiceWidth * 4 + Gap * 3,
		HeaderHeight + RowHeight * Rows + FooterHeight
	);

	private readonly UIFonts _fonts;
	private readonly TouchSettings _touch;
	private readonly List<(string Label, int Top)> _labels = [];
	private readonly List<(ActionButton Button, Choice Choice)> _choices = [];

	public event EventHandler? Closed;

	// Shows which choice of each row is the one in use.
	private void ShowChosen()
	{
		foreach (var (button, choice) in _choices)
			button.IsPrimary = choice.IsChosen(_touch.Options);
	}

	private void AddRow(string label, params Choice[] choices)
	{
		var top = PanelBounds.Y + HeaderHeight + _labels.Count * RowHeight;
		_labels.Add((label, top));
		for (var i = 0; i < choices.Length; i++)
		{
			var choice = choices[i];
			var button = new ActionButton(_fonts)
			{
				Bounds = new DrawingRectangle(
					PanelBounds.X + Padding + LabelWidth + i * (ChoiceWidth + Gap),
					top,
					ChoiceWidth,
					ChoiceHeight
				),
				Text = choice.Text,
				FontSize = 15,
			};
			button.Click += (_, _) =>
			{
				_touch.Change(choice.Choose(_touch.Options));
				ShowChosen();
			};
			_choices.Add((button, choice));
			Controls.Add(button);
		}
	}

	// A row's choice of an option's value.
	private static Choice Of<T>(
		string text,
		T value,
		Func<TouchOptions, T> get,
		Func<TouchOptions, T, TouchOptions> set
	) => new(text, o => EqualityComparer<T>.Default.Equals(get(o), value), o => set(o, value));

	public TouchSettingsForm(UIFonts fonts, TouchSettings touch)
	{
		_fonts = fonts;
		_touch = touch;
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = DrawingColor.Transparent;

		Func<TouchOptions, WaypointGesture> waypoint = o => o.Waypoint;
		Func<TouchOptions, WaypointGesture, TouchOptions> setWaypoint = (o, v) =>
			o with
			{
				Waypoint = v,
			};
		AddRow(
			Texts.WaypointGestureLabel,
			Of(Texts.WaypointTap, WaypointGesture.Tap, waypoint, setWaypoint),
			Of(Texts.WaypointHold, WaypointGesture.Hold, waypoint, setWaypoint),
			Of(Texts.WaypointTwoFingers, WaypointGesture.TwoFingers, waypoint, setWaypoint),
			Of(Texts.WaypointMoveButton, WaypointGesture.MoveButton, waypoint, setWaypoint)
		);

		Func<TouchOptions, MinimapTouch> minimap = o => o.Minimap;
		Func<TouchOptions, MinimapTouch, TouchOptions> setMinimap = (o, v) =>
			o with
			{
				Minimap = v,
			};
		AddRow(
			Texts.MinimapTouchLabel,
			Of(Texts.MinimapAim, MinimapTouch.Aim, minimap, setMinimap),
			Of(Texts.MinimapJump, MinimapTouch.Jump, minimap, setMinimap),
			Of(Texts.MinimapFollow, MinimapTouch.Follow, minimap, setMinimap)
		);

		Func<TouchOptions, bool> glides = o => o.Glides;
		Func<TouchOptions, bool, TouchOptions> setGlides = (o, v) => o with { Glides = v };
		AddRow(
			Texts.GlideLabel,
			Of(Texts.On, true, glides, setGlides),
			Of(Texts.Off, false, glides, setGlides)
		);

		Func<TouchOptions, int> slop = o => o.SlopDp;
		Func<TouchOptions, int, TouchOptions> setSlop = (o, v) => o with { SlopDp = v };
		AddRow(
			Texts.SlopLabel,
			Of(Texts.Small, 8, slop, setSlop),
			Of(Texts.Medium, 16, slop, setSlop),
			Of(Texts.Large, 24, slop, setSlop),
			Of(Texts.Larger, 32, slop, setSlop)
		);

		Func<TouchOptions, int> reach = o => o.ReachDp;
		Func<TouchOptions, int, TouchOptions> setReach = (o, v) => o with { ReachDp = v };
		AddRow(
			Texts.ReachLabel,
			Of(Texts.Small, 16, reach, setReach),
			Of(Texts.Medium, 24, reach, setReach),
			Of(Texts.Large, 32, reach, setReach),
			Of(Texts.Larger, 40, reach, setReach)
		);

		Func<TouchOptions, bool> isPanelLeft = o => o.IsPanelLeft;
		Func<TouchOptions, bool, TouchOptions> setPanelLeft = (o, v) => o with { IsPanelLeft = v };
		AddRow(
			Texts.PanelSideLabel,
			Of(Texts.Left, true, isPanelLeft, setPanelLeft),
			Of(Texts.Right, false, isPanelLeft, setPanelLeft)
		);

		Func<TouchOptions, bool> follows = o => o.PanelFollowsSelection;
		Func<TouchOptions, bool, TouchOptions> setFollows = (o, v) =>
			o with
			{
				PanelFollowsSelection = v,
			};
		AddRow(
			Texts.PanelFollowsLabel,
			Of(Texts.On, true, follows, setFollows),
			Of(Texts.Off, false, follows, setFollows)
		);

		var close = new ActionButton(fonts)
		{
			Bounds = new DrawingRectangle(
				PanelBounds.Right - Padding - ChoiceWidth,
				PanelBounds.Bottom - (FooterHeight + ChoiceHeight) / 2,
				ChoiceWidth,
				ChoiceHeight
			),
			Text = Texts.Close,
			IsPrimary = true,
			FontSize = 15,
		};
		close.Click += (_, _) => Closed?.Invoke(this, EventArgs.Empty);
		Controls.Add(close);
		ShowChosen();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		var g = e.Graphics;
		Painter.Fill(g, ClientRectangle, Theme.Alpha(Theme.Ink, 0.55f));
		Painter.Fill(g, PanelBounds, Theme.Panel);
		Painter.Frame(g, PanelBounds, Theme.Line);
		Painter.Brackets(g, PanelBounds, 14, Theme.Brass);
		Painter.Text(
			g,
			Texts.TouchSettingsTitle,
			_fonts.Bold(22),
			Theme.Text,
			PanelBounds.X + Padding,
			PanelBounds.Y + 24
		);
		var label = _fonts.Regular(16);
		foreach (var (text, top) in _labels)
			Painter.Text(g, text, label, Theme.Text, PanelBounds.X + Padding, top + 11);
	}
}
