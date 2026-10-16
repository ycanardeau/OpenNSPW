namespace OpenNspw.Desktop.Controls;

// The selected unit's picture, or a carrier's flight deck or hangar with its planes (UpdateDeckView), where the unit
// information sprite is drawn (JapanUnitInfo, set_sprt_data).
internal sealed class Deck(GameSession session) : FrameView(session, new(all_head.CMBT_WIDTH, 0, 120, 438));
