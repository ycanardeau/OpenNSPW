namespace OpenNspw.Desktop.Controls;

// The selected unit's orders (UpdateCombatMenu), on the side's flag (ButtonBase, from 20 below the deck), down to the
// radar.
internal sealed class CombatMenu(GameSession session) : FrameView(session, new(all_head.CMBT_WIDTH, 438, all_head.SCRN_WIDTH - all_head.CMBT_WIDTH, Radar.Top - 438));
