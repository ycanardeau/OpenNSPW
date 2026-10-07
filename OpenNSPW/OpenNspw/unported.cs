namespace OpenNspw;

// The functions that the ported files call but that are not ported yet. Each throws when called, so that a game that
// reaches one stops at a clear message. Delete a stub when its function is ported.
public partial class Nspw
{
	private static NotImplementedException NotPorted(string function, string file)
	{
		return new NotImplementedException($"{function} ({file}) is not ported yet.");
	}

	public void chara_cont() => throw NotPorted(nameof(chara_cont), "chara_cont.cpp");

	public void cnct_decision() => throw NotPorted(nameof(cnct_decision), "cnct_game_cont.cpp");

	public void cnct_game_init() => throw NotPorted(nameof(cnct_game_init), "cnct_game_cont.cpp");

	public void draw_cmbt_area() => throw NotPorted(nameof(draw_cmbt_area), "draw_cmbt_area.cpp");

	public void draw_map() => throw NotPorted(nameof(draw_map), "draw_cmbt_area.cpp");

	public void edit_now() => throw NotPorted(nameof(edit_now), "etc3.cpp");

	public void get_sinario_data() => throw NotPorted(nameof(get_sinario_data), "cnct_game_cont.cpp");

	public void load_on_resume(int type) => throw NotPorted(nameof(load_on_resume), "cnct_game_cont.cpp");

	public void make_map_cg() => throw NotPorted(nameof(make_map_cg), "cnct_game_cont.cpp");

	public void set_pos_of_dynmc(int n) => throw NotPorted(nameof(set_pos_of_dynmc), "etc1.cpp");

	public int set_new_unit_2(int side, int kind, int type, double rx, double ry, double drctn) => throw NotPorted(nameof(set_new_unit_2), "cnct_game_cont.cpp");

	public int set_new_unit_plane(int side, int kind, int type, int no, int planes, int arm) => throw NotPorted(nameof(set_new_unit_plane), "cnct_game_cont.cpp");

	public void unit_info_cont() => throw NotPorted(nameof(unit_info_cont), "unit_info_cont.cpp");
}
