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

		// Over the battlefield's top right corner, in front of it.
		_commandBar = new CommandBar(session, fonts);
		_commandBar.Location = new(all_head.CMBT_WIDTH - _commandBar.Width - 8, 8);
		Controls.Add(_commandBar);
		_commandBar.BringToFront();
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
