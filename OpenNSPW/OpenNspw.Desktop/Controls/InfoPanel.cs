namespace OpenNspw.Desktop.Controls;

// The text right of the deck: the game speed and the time, the selected unit's details (DrawUnitDetails), the supply
// (UpdateSupplyPanel) and the system menu (UpdateSystemMenu).
internal sealed class InfoPanel(GameSession session) : FrameView(session, new(all_head.CMBT_WIDTH + 120, 0, all_head.SCRN_WIDTH - all_head.CMBT_WIDTH - 120, 438));
