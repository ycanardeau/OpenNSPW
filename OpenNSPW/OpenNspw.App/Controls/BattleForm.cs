using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.App.Controls;

// The battle screen, laid out as the original's: the battlefield on the left, and the sidebar with the deck, the unit's
// information, the combat menu and the minimap; with the buttons of what only the keyboard does (CommandBar). Each part draws from the snapshot what the game draws there, and takes its
// clicks with the commands of the host's UI (docs/UI.md).
internal sealed class BattleForm : Form
{
	private readonly ScrollAhead _scrollAhead;
	private readonly Battlefield _battlefield;
	private readonly Control _sidebar;
	private readonly Deck _deck;
	private readonly InfoPanel _infoPanel;
	private readonly CombatMenu _combatMenu;
	private readonly Minimap _minimap;
	private readonly CommandBar _commandBar;

	// The game pixels of the screen right of the original's layout, on a wide screen (WideViewportAdapter).
	private int _extraWidth;

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

	// Fits the battle screen to `extraWidth` right of the original's layout: the command bar there, in a column of its own,
	// where it fits, and over the battlefield's top right corner otherwise.
	public void Fit(int extraWidth)
	{
		var isBeside = extraWidth >= _commandBar.Width + 16;
		_extraWidth = isBeside ? extraWidth : 0;
		Size = new DrawingSize(all_head.SCRN_WIDTH + _extraWidth, all_head.SCRN_HEIGHT);
		_commandBar.Location = isBeside
			? new(all_head.SCRN_WIDTH + (extraWidth - _commandBar.Width) / 2, 16)
			: new(all_head.CMBT_WIDTH - _commandBar.Width - 8, 8);
	}

	// isTouchScreen: whether the player touches the screen (IAppHost.IsTouchScreen), which the battlefield takes as taps and
	// pans.
	public BattleForm(GameSession session, UIFonts fonts, BattleArt art, bool isTouchScreen)
	{
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = DrawingColor.Transparent;

		_scrollAhead = new ScrollAhead(session);
		_battlefield = new Battlefield(session, fonts, art, _scrollAhead, isTouchScreen)
		{
			Bounds = Battlefield.Area,
		};
		Controls.Add(_battlefield);

		_sidebar = new Control
		{
			Bounds = new DrawingRectangle(
				all_head.CMBT_WIDTH,
				0,
				all_head.SCRN_WIDTH - all_head.CMBT_WIDTH,
				all_head.SCRN_HEIGHT
			),
			BackColor = DrawingColor.Transparent,
		};
		Controls.Add(_sidebar);

		_deck = new Deck(session, art);
		AddToSidebar(_deck, Deck.Area);
		_infoPanel = new InfoPanel(session, fonts, art);
		AddToSidebar(_infoPanel, InfoPanel.Area);
		_combatMenu = new CombatMenu(session, fonts, art);
		AddToSidebar(_combatMenu, CombatMenu.Area);
		_minimap = new Minimap(session, art);
		AddToSidebar(_minimap, Minimap.Area);

		_commandBar = new CommandBar(session, fonts);
		Controls.Add(_commandBar);
		_commandBar.BringToFront();
		Fit(0);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		if (_extraWidth > 0)
		{
			Painter.Fill(
				e.Graphics,
				new DrawingRectangle(all_head.SCRN_WIDTH, 0, _extraWidth, all_head.SCRN_HEIGHT),
				Theme.Navy
			);
			Painter.Fill(
				e.Graphics,
				new DrawingRectangle(all_head.SCRN_WIDTH, 0, 1, all_head.SCRN_HEIGHT),
				Theme.Alpha(Theme.Line, 0.9f)
			);
		}
	}

	// Shows the game as it was in the last frame: on the UI's thread, each frame.
	public void UpdateFrom(GameSnapshot snapshot)
	{
		_scrollAhead.Apply(snapshot);
		_battlefield.UpdateFrom(snapshot);
		_deck.UpdateFrom(snapshot);
		_infoPanel.UpdateFrom(snapshot);
		_combatMenu.UpdateFrom(snapshot);
		_minimap.UpdateFrom(snapshot);
	}

	// Whether a point of the screen is on the battle screen, whose parts take their own clicks (with the host's UI's
	// commands): the mouse's buttons there are not the game's.
	public bool Owns(System.Drawing.Point point)
	{
		return Visible && ClientRectangle.Contains(point);
	}
}
