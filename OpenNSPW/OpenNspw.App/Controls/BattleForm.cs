using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.App.Controls;

// The battle screen, laid out as the original's: the battlefield on the left, and the sidebar with the deck, the unit's
// information, the combat menu and the minimap; with the buttons of what only the keyboard does (CommandBar). Each part
// draws from the snapshot what the game draws there, and takes its clicks with the commands of the host's UI
// (docs/UI.md). On a wide screen, the command bar and the minimap are in a column of their own at the right, and the rest
// of the sidebar is a panel beside it, which a button shows and hides (Fit): the battlefield, magnified, takes the room
// up to the column, under the panel while it is shown, which is drawn larger on a small screen.
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

	// The original's layout, on a screen with too little room right of it for the column: the command bar over the
	// battlefield's top right corner.
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
		_commandBar.Location = new(all_head.CMBT_WIDTH - _commandBar.Width - 8, 8);
		_panelButton.Visible = false;
	}

	// The layout of a wide screen, `extraWidth` wider than the original's: the column at the right, with the command bar
	// at its top, the panel's button under it, and the minimap at its bottom, as large as it fits on a touch screen, twice
	// as large at most; the panel left of the column; and the battlefield up to the column, as far as its zoom lets it.
	private void FitWide(int extraWidth)
	{
		var width = all_head.SCRN_WIDTH + extraWidth;
		Size = new DrawingSize(width, all_head.SCRN_HEIGHT);
		_minimap.SetScale(
			_isTouchScreen ? Math.Clamp(extraWidth / (float)Minimap.Area.Width, 1, 2) : 1
		);
		var columnWidth = Math.Max(_commandBar.Width + Margin * 2, _minimap.Width);
		var columnLeft = width - columnWidth;
		_columnLeft = columnLeft;
		_commandBar.Location = new(columnLeft + (columnWidth - _commandBar.Width) / 2, Margin);
		_panelButton.Bounds = new DrawingRectangle(
			_commandBar.Bounds.Left + CommandBarLayout.Padding,
			_commandBar.Bounds.Bottom + Margin / 2,
			_commandBar.Width - CommandBarLayout.Padding * 2,
			_commandBar.Layout.ButtonHeight
		);
		_panelButton.Visible = true;
		_panelButton.IsPrimary = _isPanelOpen;
		_minimap.Location = new(
			columnLeft + (columnWidth - _minimap.Width) / 2,
			all_head.SCRN_HEIGHT - _minimap.Height
		);
		_sidebar.Scale = _panelScale;
		_sidebar.Bounds = new DrawingRectangle(
			columnLeft - (int)MathF.Ceiling(SidebarWidth * _panelScale),
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

		_minimap = new Minimap(
			session,
			art,
			isTouchScreen,
			_battlefield.Pan,
			_battlefield.ShowWindow
		);
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
		};
		_panelButton.Click += (_, _) => _isPanelOpen = !_isPanelOpen;
		Controls.Add(_panelButton);
		_panelButton.BringToFront();
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

	// Shows the game as it was in the last frame: on the UI's thread, each frame.
	public void UpdateFrom(GameSnapshot snapshot)
	{
		_scrollAhead.Apply(snapshot);
		_battlefield.UpdateFrom(snapshot);
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
