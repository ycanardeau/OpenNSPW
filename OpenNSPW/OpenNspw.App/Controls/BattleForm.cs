using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.App.Controls;

// The battle screen, laid out as the original's: the battlefield on the left, and the sidebar with the deck, the unit's
// information, the combat menu and the minimap; with the buttons of what only the keyboard does (CommandBar), which a
// button shows and hides, shown at first on a touch screen only. Each part draws from the snapshot what the game draws
// there, and takes its clicks with the commands of the host's UI (docs/UI.md). On a wide screen, the command bar and the
// minimap are in a column of their own at the right, and the rest of the sidebar is a panel beside it, which a button
// shows and hides (Fit): the battlefield, magnified, takes the room up to the column, under the panel while it is shown,
// which is drawn larger on a small screen.
internal sealed class BattleForm : Form
{
	// The room around the column's parts.
	private const int Margin = 16;

	private static readonly int SidebarWidth = all_head.SCRN_WIDTH - all_head.CMBT_WIDTH;

	private readonly ScrollAhead _scrollAhead;
	private readonly Battlefield _battlefield;
	private readonly Control _sidebar;
	private readonly Deck _deck;
	private readonly InfoPanel _infoPanel;
	private readonly CombatMenu _combatMenu;
	private readonly Minimap _minimap;
	private readonly CommandBar _commandBar;
	private readonly ActionButton _panelButton;
	private readonly ActionButton _commandBarButton;

	// Whether the player touches the screen (IAppHost.IsTouchScreen): the minimap is drawn larger, and the panel is hidden
	// at first, for the battlefield.
	private readonly bool _isTouchScreen;

	// How many times as large the panel is drawn on a wide screen: on a small screen, as high as the screen, for its text
	// to be read and its buttons touched; as the original's otherwise.
	private readonly float _panelScale;

	// Where the column is, on a wide screen; none on another.
	private int? _columnLeft;

	// Whether the panel of the sidebar is shown, on a wide screen.
	private bool _isPanelOpen;

	// Whether the panel was opened by selecting a carrier or an air base (FollowSelection), rather than by its button.
	private bool _isPanelOpenedBySelection;

	// The unit selected in the last frame.
	private int _lastSelected;

	// Whether the command bar is shown, as the player chose last (UISettings.LoadCommandBar).
	private bool _isCommandBarShown;

	// Puts a part in the sidebar, where it is on the game's screen.
	private void AddToSidebar(Control part, DrawingRectangle area)
	{
		part.Bounds = new DrawingRectangle(
			area.X - _sidebar.X,
			area.Y - _sidebar.Y,
			area.Width,
			area.Height
		);
		_sidebar.Controls.Add(part);
	}

	// How much the battlefield is magnified (Battlefield.Zoom), around its middle when set.
	public float Zoom
	{
		get => _battlefield.Zoom;
		set => _battlefield.SetZoom(value, new(_battlefield.Width / 2, _battlefield.Height / 2));
	}

	// The command bar's button, and the command bar under it while it is shown, from `top` down, centered on `center`;
	// neither if `isShown` is false.
	private void FitCommandBar(int center, int top, bool isShown = true)
	{
		var layout = _commandBar.Layout;
		_commandBarButton.Bounds = new DrawingRectangle(
			center - layout.ButtonWidth / 2,
			top,
			layout.ButtonWidth,
			layout.ButtonHeight
		);
		_commandBarButton.IsPrimary = _isCommandBarShown;
		_commandBarButton.Visible = isShown;
		_commandBar.Visible = isShown && _isCommandBarShown;
		_commandBar.Location = new(
			center - _commandBar.Width / 2,
			top + layout.ButtonHeight + CommandBarLayout.Gap
		);
	}

	// The original's layout, on a screen with too little room right of it for the column: on a touch screen, the command
	// bar's button over the battlefield's top right corner, and the command bar under it; elsewhere, neither, for the
	// original's screen to be whole, as the keyboard and the mouse do what the command bar does.
	private void FitLayout()
	{
		_columnLeft = null;
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		_battlefield.FitWidth(all_head.CMBT_WIDTH);
		_sidebar.Bounds = new DrawingRectangle(
			all_head.CMBT_WIDTH,
			0,
			SidebarWidth,
			all_head.SCRN_HEIGHT
		);
		_sidebar.Scale = 1;
		_sidebar.Visible = true;
		_minimap.SetScale(1);
		_minimap.Location = Minimap.Area.Location;
		FitCommandBar(all_head.CMBT_WIDTH - _commandBar.Width / 2 - 8, 8, _isTouchScreen);
		_panelButton.Visible = false;
	}

	// The layout of a wide screen, `extraWidth` wider than the original's: the column at the right, with the panel's
	// button and the command bar's at its top, the command bar under them, and the minimap at its bottom, as large as it
	// fits on a touch screen, twice as large at most; the panel left of the column, or at the screen's left edge on a touch
	// screen, for the left thumb as the column is for the right; and the battlefield up to the column, as far as its zoom
	// lets it.
	private void FitWide(int extraWidth)
	{
		var width = all_head.SCRN_WIDTH + extraWidth;
		Size = new DrawingSize(width, all_head.SCRN_HEIGHT);
		_minimap.SetScale(
			_isTouchScreen ? Math.Clamp(extraWidth / (float)Minimap.Area.Width, 1, 2) : 1
		);
		var layout = _commandBar.Layout;
		var buttonsWidth = layout.ButtonWidth * 2 + CommandBarLayout.Gap;
		var columnWidth = Math.Max(
			Math.Max(_commandBar.Width, buttonsWidth) + Margin * 2,
			_minimap.Width
		);
		var columnLeft = width - columnWidth;
		_columnLeft = columnLeft;
		var buttonsLeft = columnLeft + (columnWidth - buttonsWidth) / 2;
		_panelButton.Bounds = new DrawingRectangle(
			buttonsLeft,
			Margin,
			layout.ButtonWidth,
			layout.ButtonHeight
		);
		FitCommandBar(buttonsLeft + buttonsWidth - layout.ButtonWidth / 2, Margin);
		_commandBar.Location = new(
			columnLeft + (columnWidth - _commandBar.Width) / 2,
			_commandBar.Location.Y
		);
		_panelButton.Visible = true;
		_panelButton.IsPrimary = _isPanelOpen;
		_minimap.Location = new(
			columnLeft + (columnWidth - _minimap.Width) / 2,
			all_head.SCRN_HEIGHT - _minimap.Height
		);
		_sidebar.Scale = _panelScale;
		_sidebar.Bounds = new DrawingRectangle(
			_isTouchScreen ? 0 : columnLeft - (int)MathF.Ceiling(SidebarWidth * _panelScale),
			0,
			SidebarWidth,
			Minimap.Top
		);
		_sidebar.Visible = _isPanelOpen;
		_battlefield.FitWidth(Math.Min(columnLeft, (int)(all_head.CMBT_WIDTH * _battlefield.Zoom)));
	}

	// Fits the battle screen to `extraWidth` right of the original's layout (WideViewportAdapter): the column there, where
	// the command bar fits in it, and the original's layout otherwise.
	public void Fit(int extraWidth)
	{
		if (extraWidth >= _commandBar.Width + Margin)
			FitWide(extraWidth);
		else
			FitLayout();
	}

	// isTouchScreen: whether the player touches the screen (IAppHost.IsTouchScreen), which the battlefield takes as taps and
	// pans, and the minimap too. isSmallScreen: whether the screen is small enough for the battlefield to be magnified at
	// first (IAppHost.BattleZoom), where the buttons are made larger.
	public BattleForm(
		GameSession session,
		UIFonts fonts,
		BattleArt art,
		bool isTouchScreen,
		bool isSmallScreen
	)
	{
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = DrawingColor.Transparent;
		_isTouchScreen = isTouchScreen;
		_panelScale = isSmallScreen ? all_head.SCRN_HEIGHT / (float)Minimap.Top : 1;
		_isPanelOpen = !isTouchScreen;
		_isCommandBarShown = UISettings.LoadCommandBar(isTouchScreen);

		_scrollAhead = new ScrollAhead(session);
		_battlefield = new Battlefield(session, fonts, art, _scrollAhead, isTouchScreen)
		{
			Bounds = Battlefield.Area,
		};
		Controls.Add(_battlefield);

		// Where the original has it, which its parts are put in by (AddToSidebar); moved by the layout (Fit).
		_sidebar = new Control
		{
			Bounds = new DrawingRectangle(
				all_head.CMBT_WIDTH,
				0,
				SidebarWidth,
				all_head.SCRN_HEIGHT
			),
			BackColor = DrawingColor.Transparent,
		};
		Controls.Add(_sidebar);
		_sidebar.BringToFront();

		_deck = new Deck(session, art);
		AddToSidebar(_deck, Deck.Area);
		_infoPanel = new InfoPanel(session, fonts, art);
		AddToSidebar(_infoPanel, InfoPanel.Area);
		_combatMenu = new CombatMenu(session, fonts, art);
		AddToSidebar(_combatMenu, CombatMenu.Area);

		_minimap = new Minimap(session, art, isTouchScreen, _battlefield.ShowWindow);
		Controls.Add(_minimap);
		_minimap.BringToFront();

		_commandBar = new CommandBar(
			session,
			fonts,
			isSmallScreen ? CommandBarLayout.Large : CommandBarLayout.Small
		);
		Controls.Add(_commandBar);
		_commandBar.BringToFront();

		_panelButton = new ActionButton(fonts)
		{
			Text = Texts.Panel,
			FontSize = _commandBar.Layout.TextFontSize,
			IsSolid = true,
		};
		_panelButton.Click += (_, _) =>
		{
			_isPanelOpen = !_isPanelOpen;
			_isPanelOpenedBySelection = false;
		};
		Controls.Add(_panelButton);
		_panelButton.BringToFront();

		_commandBarButton = new ActionButton(fonts)
		{
			Text = Texts.CommandBar,
			FontSize = _commandBar.Layout.TextFontSize,
			IsSolid = true,
		};
		_commandBarButton.Click += (_, _) =>
		{
			_isCommandBarShown = !_isCommandBarShown;
			UISettings.SaveCommandBar(_isCommandBarShown);
		};
		Controls.Add(_commandBarButton);
		_commandBarButton.BringToFront();
		Fit(0);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		if (_columnLeft is not { } columnLeft)
			return;

		// Navy right of the battlefield, as far as the column, and the column's line.
		Painter.Fill(
			e.Graphics,
			new DrawingRectangle(
				_battlefield.Bounds.Right,
				0,
				Width - _battlefield.Bounds.Right,
				all_head.SCRN_HEIGHT
			),
			Theme.Navy
		);
		Painter.Fill(
			e.Graphics,
			new DrawingRectangle(columnLeft, 0, 1, all_head.SCRN_HEIGHT),
			Theme.Alpha(Theme.Line, 0.9f)
		);
	}

	// Opens the panel as one of the player's carriers or air bases is selected, where the player goes on to the planes on
	// its deck; and closes it again once another unit but a plane is selected, if it opened it. Not as nothing is: the game
	// lets the selection go as the deck's view is switched to the hangar, and keeps showing the carrier.
	private void FollowSelection(GameSnapshot snapshot)
	{
		var selected = snapshot.SelectedUnit;
		if (selected == _lastSelected)
			return;

		_lastSelected = selected;
		ref var unit = ref snapshot.Units[selected];
		if (
			selected != 0
			&& unit.Side == snapshot.LocalSide
			&& unit.Kind is UnitKind.Carrier or UnitKind.LightCarrier or UnitKind.AirBase
		)
		{
			_isPanelOpenedBySelection |= !_isPanelOpen;
			_isPanelOpen = true;
		}
		else if (_isPanelOpenedBySelection && selected != 0 && unit.Category != UnitCategory.Plane)
		{
			(_isPanelOpen, _isPanelOpenedBySelection) = (false, false);
		}
	}

	// How far from a unit a touch takes it, in game pixels (Battlefield.TouchRadius).
	public float TouchRadius
	{
		get => _battlefield.TouchRadius;
		set => _battlefield.TouchRadius = value;
	}

	// Shows the game as it was in the last frame: on the UI's thread, each frame.
	public void UpdateFrom(GameSnapshot snapshot)
	{
		FollowSelection(snapshot);
		_scrollAhead.Apply(snapshot);
		_battlefield.UpdateFrom(snapshot);
		_battlefield.Glide();
		_deck.UpdateFrom(snapshot);
		_infoPanel.UpdateFrom(snapshot);
		_combatMenu.UpdateFrom(snapshot);
		_minimap.Shown = _battlefield.Shown;
		_minimap.UpdateFrom(snapshot);
	}

	// Whether a point of the screen is on the battle screen, whose parts take their own clicks (with the host's UI's
	// commands): the mouse's buttons there are not the game's.
	public bool Owns(System.Drawing.Point point)
	{
		return Visible && ClientRectangle.Contains(point);
	}
}
