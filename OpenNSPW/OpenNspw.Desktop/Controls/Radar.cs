namespace OpenNspw.Desktop.Controls;

// The map of the whole battle, with the units and the camera (DrawMinimap), at the bottom of the sidebar (Minimap,
// set_sprt_data).
internal sealed class Radar(GameSession session) : FrameView(session, new(all_head.CMBT_WIDTH, Top, all_head.SCRN_WIDTH - all_head.CMBT_WIDTH, all_head.SCRN_HEIGHT - Top))
{
	// The minimap's top: 199 high, at the bottom of the screen.
	public const int Top = all_head.CMBT_HEIGHT - 199;
}
