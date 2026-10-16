using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.Desktop.Controls;

// The battle screen, laid out as the original's: the battlefield on the left, and the sidebar with the deck, the unit's
// information, the combat menu and the radar. Each part shows what the game draws there, until it draws itself
// (docs/Ui.md).
internal sealed class MainForm : Form
{
	private readonly Control _sidebar;
	private readonly InfoPanel _infoPanel;
	private readonly CombatMenu _combatMenu;

	// Puts a part in the sidebar, where it is on the game's screen.
	private void AddToSidebar(Control part, DrawingRectangle area)
	{
		part.Bounds = new DrawingRectangle(area.X - _sidebar.X, area.Y - _sidebar.Y, area.Width, area.Height);
		_sidebar.Controls.Add(part);
	}

	public MainForm(GameSession session, UiFonts fonts, BattleArt art)
	{
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = DrawingColor.Transparent;

		var battlefield = new Battlefield(session);
		battlefield.Bounds = battlefield.Area;
		Controls.Add(battlefield);

		_sidebar = new Control
		{
			Bounds = new DrawingRectangle(all_head.CMBT_WIDTH, 0, all_head.SCRN_WIDTH - all_head.CMBT_WIDTH, all_head.SCRN_HEIGHT),
			BackColor = DrawingColor.Transparent,
		};
		Controls.Add(_sidebar);

		var deck = new Deck(session);
		AddToSidebar(deck, deck.Area);
		_infoPanel = new InfoPanel(session, fonts);
		AddToSidebar(_infoPanel, InfoPanel.Area);
		_combatMenu = new CombatMenu(session, fonts, art);
		AddToSidebar(_combatMenu, CombatMenu.Area);
		var radar = new Radar(session);
		AddToSidebar(radar, radar.Area);
	}

	// Shows the game as it was in the last frame: on the UI's thread, each frame.
	public void UpdateFrom(BattleSnapshot snapshot)
	{
		_infoPanel.UpdateFrom(snapshot);
		_combatMenu.UpdateFrom(snapshot);
	}

	// Whether a point of the screen is on a part that takes its own clicks (with the host's UI's commands), and not the
	// game's: the mouse's buttons there are not the game's.
	public bool Owns(System.Drawing.Point point)
	{
		return Visible && (InfoPanel.Area.Contains(point) || CombatMenu.Area.Contains(point));
	}
}
