using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.App.Controls;

// A part of the settings, a tab of their form.
internal enum SettingsSection
{
	// The language, and the sounds' volume.
	General,

	// How a touch screen is played (TouchSettings): on a touch screen only.
	Touch,

	// OpenNSPW's options for single-player battles (docs/Options.md), which play them otherwise than 1.22: in a Debug build
	// only, as a player's battles are 1.22's.
	Debug,
}

// The settings, a tab for each of their sections shown, and a row of choices for each setting of the tab chosen, the one
// in use in brass; each takes effect at once, also over a battle, but the language, which the owner shows the menus in
// again (LanguageChosen). Over the whole screen, dimmed, so that nothing under it takes a click or a touch. Close closes
// it.
internal sealed class SettingsForm : Form
{
	// A choice of a row: its text, whether it is the one in use, and what choosing it does.
	private sealed record Choice(string Text, Func<bool> IsChosen, Action Choose);

	private const int LabelWidth = 300;
	private const int ChoiceWidth = 140;
	private const int Gap = 8;

	// The width of a row's choices together: four of the widest; more are narrower.
	private const int ChoicesWidth = ChoiceWidth * 4 + Gap * 3;
	private const int ChoiceHeight = 44;
	private const int RowHeight = ChoiceHeight + 12;
	private const int Padding = 28;
	private const int HeaderHeight = 120;
	private const int FooterHeight = 72;

	// How many rows a section has.
	private static int RowsOf(SettingsSection section) =>
		section switch
		{
			SettingsSection.Touch => 7,
			_ => 2,
		};

	// The panel, centered, as high as the most rows of the sections shown need.
	private static DrawingRectangle PanelFor(IReadOnlyList<SettingsSection> sections)
	{
		var width = Padding * 2 + LabelWidth + ChoicesWidth;
		var height = HeaderHeight + RowHeight * sections.Max(RowsOf) + FooterHeight;
		return new(
			(all_head.SCRN_WIDTH - width) / 2,
			(all_head.SCRN_HEIGHT - height) / 2,
			width,
			height
		);
	}

	private readonly DrawingRectangle _panel;

	private readonly UIFonts _fonts;
	private readonly TouchSettings _touch;
	private readonly SoundVolume _volume;
	private readonly Func<BattleOptions> _options;
	private readonly Action<BattleOptions> _setOptions;
	private readonly Dictionary<SettingsSection, TabButton> _tabs = [];
	private readonly Dictionary<SettingsSection, List<(string Label, int Top)>> _labels = [];
	private readonly List<(SettingsSection Section, ActionButton Button, Choice Choice)> _choices =
	[];
	private SettingsSection _shown;

	// A language was chosen (SettingsSection.General).
	public event EventHandler<Language>? LanguageChosen;

	public event EventHandler? Closed;

	// The section shown.
	public SettingsSection Section => _shown;

	// Shows a section's rows, and which choice of each is the one in use.
	private void ShowSection(SettingsSection section)
	{
		_shown = section;
		foreach (var (tabSection, tab) in _tabs)
			tab.IsSelected = tabSection == section;
		foreach (var (rowSection, button, choice) in _choices)
		{
			button.Visible = rowSection == section;
			button.IsPrimary = choice.IsChosen();
		}
	}

	private void AddRow(SettingsSection section, string label, params Choice[] choices)
	{
		if (!_labels.TryGetValue(section, out var labels))
			_labels[section] = labels = [];
		var top = _panel.Y + HeaderHeight + labels.Count * RowHeight;
		labels.Add((label, top));
		var width = Math.Min(
			ChoiceWidth,
			(ChoicesWidth - Gap * (choices.Length - 1)) / choices.Length
		);
		for (var i = 0; i < choices.Length; i++)
		{
			var choice = choices[i];
			var button = new ActionButton(_fonts)
			{
				Bounds = new DrawingRectangle(
					_panel.X + Padding + LabelWidth + i * (width + Gap),
					top,
					width,
					ChoiceHeight
				),
				Text = choice.Text,
				FontSize = 15,
			};
			button.Click += (_, _) =>
			{
				choice.Choose();
				ShowSection(_shown);
			};
			_choices.Add((section, button, choice));
			Controls.Add(button);
		}
	}

	// A choice of a touch option's value.
	private Choice Touch<T>(
		string text,
		T value,
		Func<TouchOptions, T> get,
		Func<TouchOptions, T, TouchOptions> set
	) =>
		new(
			text,
			() => EqualityComparer<T>.Default.Equals(get(_touch.Options), value),
			() => _touch.Change(set(_touch.Options, value))
		);

	// A choice of a battle option's value.
	private Choice Option(
		string text,
		bool value,
		Func<BattleOptions, bool> get,
		Func<BattleOptions, bool, BattleOptions> set
	) => new(text, () => get(_options()) == value, () => _setOptions(set(_options(), value)));

	// A choice of the sounds' volume.
	private Choice Volume(string text, float value) =>
		new(text, () => _volume.Value == value, () => _volume.Change(value));

	private void AddGeneral()
	{
		AddRow(
			SettingsSection.General,
			Texts.LanguageLabel,
			new(
				Texts.NameOf(Language.English),
				() => Texts.Language == Language.English,
				() => LanguageChosen?.Invoke(this, Language.English)
			),
			new(
				Texts.NameOf(Language.Japanese),
				() => Texts.Language == Language.Japanese,
				() => LanguageChosen?.Invoke(this, Language.Japanese)
			)
		);
		AddRow(
			SettingsSection.General,
			Texts.VolumeLabel,
			Volume(Texts.Off, 0),
			Volume("25%", 0.25f),
			Volume("50%", 0.5f),
			Volume("75%", 0.75f),
			Volume("100%", 1)
		);
	}

	private void AddTouch()
	{
		Func<TouchOptions, WaypointGesture> waypoint = o => o.Waypoint;
		Func<TouchOptions, WaypointGesture, TouchOptions> setWaypoint = (o, v) =>
			o with
			{
				Waypoint = v,
			};
		AddRow(
			SettingsSection.Touch,
			Texts.WaypointGestureLabel,
			Touch(Texts.WaypointTap, WaypointGesture.Tap, waypoint, setWaypoint),
			Touch(Texts.WaypointHold, WaypointGesture.Hold, waypoint, setWaypoint),
			Touch(Texts.WaypointTwoFingers, WaypointGesture.TwoFingers, waypoint, setWaypoint),
			Touch(Texts.WaypointMoveButton, WaypointGesture.MoveButton, waypoint, setWaypoint)
		);

		Func<TouchOptions, MinimapTouch> minimap = o => o.Minimap;
		Func<TouchOptions, MinimapTouch, TouchOptions> setMinimap = (o, v) =>
			o with
			{
				Minimap = v,
			};
		AddRow(
			SettingsSection.Touch,
			Texts.MinimapTouchLabel,
			Touch(Texts.MinimapAim, MinimapTouch.Aim, minimap, setMinimap),
			Touch(Texts.MinimapJump, MinimapTouch.Jump, minimap, setMinimap),
			Touch(Texts.MinimapFollow, MinimapTouch.Follow, minimap, setMinimap)
		);

		Func<TouchOptions, bool> glides = o => o.Glides;
		Func<TouchOptions, bool, TouchOptions> setGlides = (o, v) => o with { Glides = v };
		AddRow(
			SettingsSection.Touch,
			Texts.GlideLabel,
			Touch(Texts.On, true, glides, setGlides),
			Touch(Texts.Off, false, glides, setGlides)
		);

		Func<TouchOptions, int> slop = o => o.SlopDp;
		Func<TouchOptions, int, TouchOptions> setSlop = (o, v) => o with { SlopDp = v };
		AddRow(
			SettingsSection.Touch,
			Texts.SlopLabel,
			Touch(Texts.Small, 8, slop, setSlop),
			Touch(Texts.Medium, 16, slop, setSlop),
			Touch(Texts.Large, 24, slop, setSlop),
			Touch(Texts.Larger, 32, slop, setSlop)
		);

		Func<TouchOptions, int> reach = o => o.ReachDp;
		Func<TouchOptions, int, TouchOptions> setReach = (o, v) => o with { ReachDp = v };
		AddRow(
			SettingsSection.Touch,
			Texts.ReachLabel,
			Touch(Texts.Small, 16, reach, setReach),
			Touch(Texts.Medium, 24, reach, setReach),
			Touch(Texts.Large, 32, reach, setReach),
			Touch(Texts.Larger, 40, reach, setReach)
		);

		Func<TouchOptions, bool> isPanelLeft = o => o.IsPanelLeft;
		Func<TouchOptions, bool, TouchOptions> setPanelLeft = (o, v) => o with { IsPanelLeft = v };
		AddRow(
			SettingsSection.Touch,
			Texts.PanelSideLabel,
			Touch(Texts.Left, true, isPanelLeft, setPanelLeft),
			Touch(Texts.Right, false, isPanelLeft, setPanelLeft)
		);

		Func<TouchOptions, bool> follows = o => o.PanelFollowsSelection;
		Func<TouchOptions, bool, TouchOptions> setFollows = (o, v) =>
			o with
			{
				PanelFollowsSelection = v,
			};
		AddRow(
			SettingsSection.Touch,
			Texts.PanelFollowsLabel,
			Touch(Texts.On, true, follows, setFollows),
			Touch(Texts.Off, false, follows, setFollows)
		);
	}

	private void AddDebug()
	{
		AddRow(
			SettingsSection.Debug,
			Texts.SteadySpeed,
			Option(Texts.On, true, o => o.SteadySpeed, (o, v) => o with { SteadySpeed = v }),
			Option(Texts.Off, false, o => o.SteadySpeed, (o, v) => o with { SteadySpeed = v })
		);
		AddRow(
			SettingsSection.Debug,
			Texts.SharedEffectRandom,
			Option(
				Texts.On,
				true,
				o => o.SharedEffectRandom,
				(o, v) => o with { SharedEffectRandom = v }
			),
			Option(
				Texts.Off,
				false,
				o => o.SharedEffectRandom,
				(o, v) => o with { SharedEffectRandom = v }
			)
		);
	}

	private void AddTab(SettingsSection section, string text, int index)
	{
		var tab = new TabButton(_fonts)
		{
			Bounds = new DrawingRectangle(_panel.X + Padding + index * 160, _panel.Y + 64, 144, 32),
			Text = text,
		};
		tab.Click += (_, _) => ShowSection(section);
		_tabs[section] = tab;
		Controls.Add(tab);
	}

	// sections: the sections shown, the first at first, or `initial`. touch: the touch options (SettingsSection.Touch).
	// volume: the sounds' volume (SettingsSection.General). options, setOptions: the options of single-player battles, and
	// how they are changed (SettingsSection.Debug).
	public SettingsForm(
		UIFonts fonts,
		IReadOnlyList<SettingsSection> sections,
		TouchSettings touch,
		SoundVolume volume,
		Func<BattleOptions> options,
		Action<BattleOptions> setOptions,
		SettingsSection? initial = null
	)
	{
		_fonts = fonts;
		_touch = touch;
		_volume = volume;
		_options = options;
		_setOptions = setOptions;
		_panel = PanelFor(sections);
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = DrawingColor.Transparent;

		for (var i = 0; i < sections.Count; i++)
		{
			switch (sections[i])
			{
				case SettingsSection.General:
					AddTab(SettingsSection.General, Texts.GeneralTab, i);
					AddGeneral();
					break;
				case SettingsSection.Touch:
					AddTab(SettingsSection.Touch, Texts.TouchTab, i);
					AddTouch();
					break;
				case SettingsSection.Debug:
					AddTab(SettingsSection.Debug, Texts.DebugTab, i);
					AddDebug();
					break;
			}
		}

		var close = new ActionButton(fonts)
		{
			Bounds = new DrawingRectangle(
				_panel.Right - Padding - ChoiceWidth,
				_panel.Bottom - (FooterHeight + ChoiceHeight) / 2,
				ChoiceWidth,
				ChoiceHeight
			),
			Text = Texts.Close,
			IsPrimary = true,
			FontSize = 15,
		};
		close.Click += (_, _) => Closed?.Invoke(this, EventArgs.Empty);
		Controls.Add(close);
		ShowSection(initial is { } section && sections.Contains(section) ? section : sections[0]);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		var g = e.Graphics;
		Painter.Fill(g, ClientRectangle, Theme.Alpha(Theme.Ink, 0.55f));
		Painter.Fill(g, _panel, Theme.Panel);
		Painter.Frame(g, _panel, Theme.Line);
		Painter.Brackets(g, _panel, 14, Theme.Brass);
		Painter.Text(
			g,
			Texts.SettingsTitle,
			_fonts.Bold(22),
			Theme.Text,
			_panel.X + Padding,
			_panel.Y + 24
		);

		var label = _fonts.Regular(16);
		foreach (var (text, top) in _labels.GetValueOrDefault(_shown) ?? [])
			Painter.Text(g, text, label, Theme.Text, _panel.X + Padding, top + 11);
		if (_shown == SettingsSection.Debug)
			Painter.Text(
				g,
				Texts.DebugNote,
				_fonts.Regular(14),
				Theme.Muted,
				_panel.X + Padding,
				_panel.Bottom - FooterHeight / 2 - 9
			);
	}
}
