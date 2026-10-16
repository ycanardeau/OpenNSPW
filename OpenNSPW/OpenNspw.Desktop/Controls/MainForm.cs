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

	// Puts a part in the sidebar, where it is on the game's screen.
	private void AddToSidebar(FrameView view)
	{
		view.Bounds = new DrawingRectangle(view.Area.X - _sidebar.X, view.Area.Y - _sidebar.Y, view.Area.Width, view.Area.Height);
		_sidebar.Controls.Add(view);
	}

	public MainForm(GameSession session)
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

		AddToSidebar(new Deck(session));
		AddToSidebar(new InfoPanel(session));
		AddToSidebar(new CombatMenu(session));
		AddToSidebar(new Radar(session));
	}
}
