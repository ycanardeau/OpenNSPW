namespace OpenNspw.Desktop.Controls;

// The sea, the land and the units, with the paths, the selection and the targets (DrawBattleArea).
internal sealed class Battlefield(GameSession session) : FrameView(session, new(0, 0, all_head.CMBT_WIDTH, all_head.CMBT_HEIGHT));
