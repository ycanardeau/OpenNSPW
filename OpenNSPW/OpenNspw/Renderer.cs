using static OpenNspw.Nspw;

namespace OpenNspw;

// The drawing: the DirectDraw surfaces, the sprites and the fonts, and the functions that draw the battle area and the
// battle's end (draw.cpp, and parts of draw_cmbt_area.cpp, etc2.cpp, etc3.cpp, cnct_game_cont.cpp and win_proc.cpp),
// with the globals of win_main.cpp that they use.
[Globals]
public sealed unsafe partial class Renderer(Nspw game)
{
	private readonly Nspw _game = game;

	[Original("my_cursor")]
	public object? GameCursor;

	[Original("fullscreen")]
	public Bool32 IsFullscreen;

	//	DirectXオブジェクト

	public IDirectDraw7? lpDD = null;
	public IDirectDrawSurface7? lpDDSPrimary = null;
	public IDirectDrawSurface7? lpDDSBack = null;

	public IDirectDrawClipper? lpDDclip = null;

	[Original("lpDDS_OS")]
	public IDirectDrawSurface7? OffscreenSurface = null;

	// フォント
	[Original("gameFont_1")]
	public HFONT GameFont1;

	[Original("gameFont_2")]
	public HFONT GameFont2;

	/////////////////////

	// iNSPWからもってきたやつ

	[Original("sprt")]
	public SpriteArray Sprites;

	[Original("missed_pending")]
	public uint MissedPending;

	[Original("a_paint_speed")]
	public uint PaintSpeed;

	public int FrameCount;

	//unsigned char		*dst_vram;							//書き込むＶＲＡＭのアドレス

	//unsigned char		*src_vram;							//読み込むＶＲＡＭのアドレス

	[Original("scrn_mode")]
	public int ScreenMode;

	[Original("video_memory")]
	public int VideoMemory;

	// 0x48A1EC of 1.22: whether this tick is the last of its frame, the one that is drawn.
	public bool IsDrawnTick;
}
