#include <stdio.h>

#include <string>

#include "layout.h"
#include "state.h"

#include "all_head.h"
#include "all_extern.h"

// Writes, as JSON: the size of each struct in all_typedef.h and the offset and size of each of its fields, the size of
// each global in nspw_globals, as compiled, and the initial values of the globals.

class layout_writer
{
	FILE *f_;
	bool first_struct_ = true;
	bool first_field_ = true;

public:
	explicit layout_writer(FILE *f) : f_(f) {}

	void begin_struct(const char *name, size_t size)
	{
		fprintf(f_, "%s\n\t\t\"%s\": { \"size\": %u, \"fields\": {", first_struct_ ? "" : ",", name, (unsigned)size);
		first_struct_ = false;
		first_field_ = true;
	}

	void field(const char *name, size_t offset, size_t size)
	{
		fprintf(f_, "%s \"%s\": [%u, %u]", first_field_ ? "" : ",", name, (unsigned)offset, (unsigned)size);
		first_field_ = false;
	}

	void end_struct()
	{
		fprintf(f_, " } }");
	}
};

#define BEGIN(type) w.begin_struct(#type, sizeof(type)); { typedef type T;
#define F(name) w.field(#name, offsetof(T, name), sizeof(((T *)0)->name));
#define END w.end_struct(); }

static void write_structs(layout_writer &w)
{
	BEGIN(UNIT)
		F(used) F(x) F(y) F(ctgry) F(kind) F(type) F(info) F(os_indx_y) F(os_indx_x)
		F(drctn) F(drctn_add) F(a_drctn_add) F(spd) F(spd_add) F(a_spd_add) F(min_spd) F(max_spd)
		F(stop) F(spry) F(mark) F(pp_x) F(pp_y) F(em_flg) F(em_x) F(em_y) F(to_ldr_drctn) F(to_ldr_dstc)
		F(is_ltl_ldr) F(ltl_ldr) F(no) F(for_ltl_ldr) F(for_form_spd) F(hp) F(arm) F(arm2) F(gas) F(found) F(tech)
		F(rnd_250) F(rnd_225) F(rnd_200) F(rnd_175) F(rnd_150) F(rnd_125)
		F(rnd_100) F(rnd_80) F(rnd_65) F(rnd_50) F(rnd_40) F(rnd_30) F(rnd_20) F(rnd_10)
	END
	BEGIN(EFFECT)
		F(used) F(layer) F(kind) F(no) F(info) F(x) F(y) F(x2) F(y2) F(found)
	END
	BEGIN(FIRE)
		F(used) F(kind) F(info) F(no) F(x) F(y) F(drctn) F(spd) F(spd_add) F(last_spd) F(last_x) F(last_y)
	END
	BEGIN(NEW_PP)
		F(used) F(x) F(y) F(cls)
	END
	BEGIN(NEW_SLCT)
		F(sw) F(the_slct_unit) F(m) F(gr_x) F(gr_y)
	END
	BEGIN(NEW_MENU)
		F(menu) F(the_slct_unit)
	END
	BEGIN(KUMO)
		F(used) F(x) F(y) F(kind)
	END
	BEGIN(SPRT)
		F(no) F(x) F(y) F(cx) F(cy) F(wd) F(ht) F(base_x) F(base_y) F(os_of_x)
	END
	BEGIN(POINT)
		F(x) F(y)
	END
	BEGIN(RECT)
		F(left) F(top) F(right) F(bottom)
	END
	BEGIN(GENERICMSG)
		F(dwType)
	END
	BEGIN(UNIT_MSG)
		F(byType) F(used) F(x) F(y) F(cls)
	END
	BEGIN(_DP_DATA_1)
		F(dwType) F(my_name) F(data)
	END
	BEGIN(_DP_NEW_PP)
		F(dwType) F(used) F(x) F(y) F(cls) F(slct_unit)
	END
	BEGIN(_DP_NEW_PP_SHIP)
		F(dwType) F(used) F(x) F(y) F(cls) F(slct_unit)
	END
	BEGIN(_DP_NEW_PP_PLANE)
		F(dwType) F(used) F(x) F(y) F(cls) F(slct_unit)
	END
	BEGIN(_DP_NEW_SLCT)
		F(dwType) F(sw) F(the_slct_unit) F(m) F(gr_x) F(gr_y) F(slct_unit)
	END
	BEGIN(_DP_NEW_SLCT_SHIP)
		F(dwType) F(sw) F(the_slct_unit) F(m) F(gr_x) F(gr_y) F(slct_unit)
	END
	BEGIN(_DP_NEW_SLCT_PLANE)
		F(dwType) F(sw) F(the_slct_unit) F(m) F(gr_x) F(gr_y) F(slct_unit)
	END
	BEGIN(_DP_NEW_SLCT_LAND)
		F(dwType) F(sw) F(the_slct_unit) F(m) F(gr_x) F(gr_y)
	END
	BEGIN(_DP_NEW_MENU)
		F(dwType) F(menu) F(the_slct_unit) F(slct_unit)
	END
	BEGIN(_DP_FLAG)
		F(dwType) F(cc_chk) F(unit_chk) F(rnd_chk) F(ccc_wait_chk) F(rival_mode)
	END
	BEGIN(_DP_DATA_20)
		F(dwType) F(friend_chat)
	END
}

bool write_layout(const char *path)
{
	FILE *f;
	if (fopen_s(&f, path, "wb") != 0)
		return false;

	fprintf(f, "{\n\t\"structs\": {");
	layout_writer w(f);
	write_structs(w);
	fprintf(f, "\n\t},\n\t\"globals\": {");
	for (int i = 0; i < nspw_global_count; i++)
		fprintf(f, "%s\n\t\t\"%s\": %u", i == 0 ? "" : ",", nspw_globals[i].name, (unsigned)nspw_globals[i].size);
	fprintf(f, "\n\t},\n\t\"initial\": ");
	std::string initial;
	nspw_state::take().append_diff(nspw_state::zero(), initial);
	fwrite(initial.data(), 1, initial.size(), f);
	fprintf(f, "\n}\n");

	fclose(f);
	return true;
}
