#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <string>

#include "function_tests.h"
#include "layout.h"
#include "state.h"

#include "all_head.h"
#include "all_extern.h"
#include "all_forward.h"

// Generates the inputs of the tests. Not rand(), so that generating inputs does not change the CRT's sequence.
// SplitMix64.
class rng
{
	unsigned long long state_;

public:
	explicit rng(unsigned long long seed) : state_(seed) {}

	unsigned long long next()
	{
		unsigned long long z = (state_ += 0x9E3779B97F4A7C15ull);
		z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9ull;
		z = (z ^ (z >> 27)) * 0x94D049BB133111EBull;
		return z ^ (z >> 31);
	}

	unsigned u32() { return (unsigned)(next() >> 32); }

	// A number in [lo, hi].
	int range(int lo, int hi) { return (int)(lo + (long long)(next() % (unsigned long long)((long long)hi - lo + 1))); }

	// A number in [lo, hi).
	double real(double lo, double hi) { return lo + (hi - lo) * ((next() >> 11) * (1.0 / 9007199254740992.0)); }

	bool chance(int percent) { return range(0, 99) < percent; }
};

static unsigned long long hash(const char *s)
{
	unsigned long long h = 0xCBF29CE484222325ull;
	for (; *s; s++)
		h = (h ^ (unsigned char)*s) * 0x100000001B3ull;
	return h;
}

static std::string json_double(double v)
{
	unsigned long long bits;
	memcpy(&bits, &v, sizeof bits);
	char s[32];
	sprintf_s(s, "\"%016llX\"", bits);
	return s;
}

static std::string json_int(long long v)
{
	char s[32];
	sprintf_s(s, "%lld", v);
	return s;
}

// One call of a function. A test sets the globals and the arguments, calls call() right before calling the function,
// and passes the results to ret() and out().
//
// Written as one line of JSON:
//   seed    The CRT's rand() seed, set with srand() right before the call.
//   args    The arguments. Doubles are written as the hex of their bits.
//   before  The globals that differ from their initial values before the call, as [global, offset, base64] runs.
//   return  The return value, if any.
//   outs    Values the function wrote through pointer arguments, if any.
//   after   The globals that the call changed, as [global, offset, base64] runs.
//   rand    The next two values of rand() after the call, which show the CRT's state.
class test_case
{
	rng &rng_;
	const nspw_state &initial_;
	nspw_state before_;
	unsigned seed_ = 0;
	std::string args_;
	std::string ret_;
	std::string outs_;

	static void append(std::string &list, const std::string &value)
	{
		if (!list.empty())
			list += ',';
		list += value;
	}

public:
	test_case(rng &g, const nspw_state &initial) : rng_(g), initial_(initial) {}

	void arg(int v) { append(args_, json_int(v)); }
	void arg(double v) { append(args_, json_double(v)); }

	void call()
	{
		call(rng_.u32());
	}

	// call() with a chosen seed, for a function that only does something for some values of rand().
	void call(unsigned seed)
	{
		before_ = nspw_state::take();
		seed_ = seed;
		srand(seed_);
	}

	void ret(int v) { ret_ = json_int(v); }
	void ret(double v) { ret_ = json_double(v); }

	void out(int v) { append(outs_, json_int(v)); }
	void out(double v) { append(outs_, json_double(v)); }

	std::string finish()
	{
		const nspw_state after = nspw_state::take();
		const int rand1 = rand();
		const int rand2 = rand();

		std::string line = "{\"seed\":" + json_int(seed_) + ",\"args\":[" + args_ + "],\"before\":";
		before_.append_diff(initial_, line);
		if (!ret_.empty())
			line += ",\"return\":" + ret_;
		if (!outs_.empty())
			line += ",\"outs\":[" + outs_ + "]";
		line += ",\"after\":";
		after.append_diff(before_, line);
		line += ",\"rand\":[" + json_int(rand1) + "," + json_int(rand2) + "]}\n";
		return line;
	}
};

class recorder
{
	std::string directory_;
	nspw_state initial_;
	bool failed_ = false;

public:
	explicit recorder(const std::string &directory) : directory_(directory), initial_(nspw_state::take()) {}

	bool failed() const { return failed_; }

	// Records `count` calls of `body`, each starting from the initial state.
	template <class F>
	void function(const char *file, const char *name, int count, F body)
	{
		const std::string dir = directory_ + "\\Functions\\" + file;
		CreateDirectoryA((directory_ + "\\Functions").c_str(), NULL);
		CreateDirectoryA(dir.c_str(), NULL);

		const std::string path = dir + "\\" + name + ".jsonl";
		FILE *f;
		if (fopen_s(&f, path.c_str(), "wb") != 0)
		{
			failed_ = true;
			return;
		}

		rng g(hash(file) ^ hash(name));
		for (int i = 0; i < count; i++)
		{
			initial_.restore();
			test_case c(g, initial_);
			body(g, c);
			const std::string line = c.finish();
			fwrite(line.data(), 1, line.size(), f);
		}

		fclose(f);
		initial_.restore();
	}
};

//============================================================================
// Inputs
//----------------------------------------------------------------------------

// An angle in radians, as the game computes them: mostly degrees times a_PI.
static double random_angle(rng &g)
{
	switch (g.range(0, 9))
	{
	case 0: return g.range(-720, 720) * a_PI;
	case 1: return g.real(-1.0, 1.0);
	case 2: return g.real(-1e-6, 1e-6);
	case 3: return g.range(-64, 64) * (PI / 4) + g.real(-1e-12, 1e-12);
	case 4: return g.real(-1e6, 1e6);
	case 5: return g.real(-1e300, 1e300) * g.real(0.0, 1.0) * g.real(0.0, 1.0);
	case 6: return g.chance(50) ? 0.0 : -0.0;
	default: return g.real(-3600.0, 3600.0) * a_PI;
	}
}

// A distance on the map, as the game passes to atan2.
static double random_distance(rng &g)
{
	switch (g.range(0, 9))
	{
	case 0: return 0.0;
	case 1: return -0.0;
	case 2: return (double)g.range(-20000, 20000);
	case 3: return g.real(-1e-3, 1e-3);
	case 4: return g.real(-1e30, 1e30) * g.real(0.0, 1.0) * g.real(0.0, 1.0);
	case 5: return g.chance(50) ? HUGE_VAL : -HUGE_VAL;
	default: return g.real(-20000.0, 20000.0);
	}
}

// kumo[] in one of the states the game can be in: all free, nearly all used, or a few used.
static void random_kumo(rng &g, int kind)
{
	int m;

	switch (kind)
	{
	case 0:
		break;

	case 1:
		for (m = 0; m < KUMO_MAX; m++)
			kumo[m].used = 1;
		for (m = g.range(0, 24); m > 0; m--)
			kumo[g.range(0, KUMO_MAX - 1)].used = 0;
		for (m = g.range(0, 40); m > 0; m--)
			kumo[g.range(0, KUMO_MAX - 1)].x = MAP_LEFT + g.range(-4, 4) * 0.5;
		break;

	default:
		for (m = g.range(0, 64); m > 0; m--)
		{
			const int n = g.range(0, 63);
			kumo[n].used = 1;
			kumo[n].x = g.chance(30) ? MAP_LEFT + g.range(-4, 4) * 0.5 : g.real(MAP_LEFT, MAP_RIGHT + 400.0);
			kumo[n].y = g.real(MAP_BOTTOM, MAP_TOP + 160.0);
			kumo[n].kind = 1;
		}
		break;
	}
}

// A position on the map: mostly whole numbers, which compress well, and sometimes 0, so that two units share a
// coordinate.
static double random_coordinate(rng &g, double lo, double hi)
{
	switch (g.range(0, 9))
	{
	case 0: return 0.0;
	case 1: case 2: case 3: return g.real(lo, hi);
	default: return (double)g.range((int)lo, (int)hi);
	}
}

// A unit in a state the game can be in, with the fields that the functions in etc2.cpp read. Units refer to each
// other (info[1], ltl_ldr) only within [0, count). pp_x and pp_y always end with MAP_RIGHT+1, and have random points
// before it if `paths` is set.
static void random_unit(rng &g, int m, int count, bool paths)
{
	int i;

	unit[m].used = (short)(g.chance(75) ? g.range(JPN, USA) : 0);
	unit[m].ctgry = g.range(SHIP, BASE);
	// Mostly the kinds that the functions treat specially.
	switch (g.range(0, 9))
	{
	case 0: case 1: unit[m].kind = SS1; break;
	case 2: unit[m].kind = DD1; break;
	case 3: unit[m].kind = SP; break;
	case 4: unit[m].kind = FT1; break;
	default: unit[m].kind = g.range(BB1, GF3); break;
	}
	unit[m].type = (short)g.range(0, 2);
	unit[m].x = random_coordinate(g, -3000, 3000);
	unit[m].y = random_coordinate(g, -3000, 3000);
	unit[m].drctn = g.chance(40) ? g.real(0.0, 360.0) : (double)g.range(0, 359);
	unit[m].spd = g.chance(30) ? 0.0 : g.chance(50) ? g.range(1, 4) * 0.5 : g.real(0.0, 3.0);
	unit[m].max_spd = g.chance(50) ? g.range(2, 6) * 0.5 : g.real(1.0, 3.0);
	unit[m].spry = g.chance(80) ? 0 : 1;
	unit[m].found = g.range(0, 1);
	unit[m].ltl_ldr = (short)g.range(0, count - 1);
	unit[m].no = (short)g.range(0, 5);

	unit[m].info[0] = g.chance(60) ? 0 : g.range(FLYING, PARKING);
	unit[m].info[1] = g.range(0, count - 1);
	unit[m].info[2] = g.range(0, 31);
	unit[m].info[5] = g.range(0, 3);
	unit[m].info[6] = g.range(0, 1);
	unit[m].info[10] = g.chance(70) ? 0 : g.range(1, 3);

	const int pp = paths ? g.range(0, 5) : 0;
	for (i = 0; i < pp; i++)
	{
		unit[m].pp_x[i] = random_coordinate(g, -3000, 3000);
		unit[m].pp_y[i] = random_coordinate(g, -3000, 3000);
	}
	unit[m].pp_x[pp] = MAP_RIGHT + 1;
}

// Units 0 to max_unit.
static void random_units(rng &g, bool paths = false)
{
	max_unit = (short)(g.chance(90) ? g.range(1, 12) : g.range(13, 40));
	for (int m = 0; m <= max_unit; m++)
		random_unit(g, m, max_unit + 1, paths);
}

static RECT random_rect(rng &g)
{
	RECT r;
	r.left = g.range(-100, 900);
	r.top = g.range(-100, 900);
	r.right = g.chance(80) ? r.left + g.range(0, 300) : g.range(-100, 900);
	r.bottom = g.chance(80) ? r.top + g.range(0, 300) : g.range(-100, 900);
	return r;
}

static void arg(test_case &c, const RECT &r)
{
	c.arg((int)r.left);
	c.arg((int)r.top);
	c.arg((int)r.right);
	c.arg((int)r.bottom);
}

static void out(test_case &c, const RECT &r)
{
	c.out((int)r.left);
	c.out((int)r.top);
	c.out((int)r.right);
	c.out((int)r.bottom);
}

//============================================================================
// Tests
//----------------------------------------------------------------------------

static void record_math(recorder &r)
{
	r.function("math", "sin", 5000, [](rng &g, test_case &c) {
		const double x = random_angle(g);
		c.arg(x);
		c.call();
		c.ret(sin(x));
	});

	r.function("math", "cos", 5000, [](rng &g, test_case &c) {
		const double x = random_angle(g);
		c.arg(x);
		c.call();
		c.ret(cos(x));
	});

	r.function("math", "atan2", 5000, [](rng &g, test_case &c) {
		const double y = random_distance(g);
		const double x = g.chance(5) ? y : random_distance(g);
		c.arg(y);
		c.arg(x);
		c.call();
		c.ret(atan2(y, x));
	});
}

static void record_etc3(recorder &r)
{
	r.function("etc3", "make_my_rnd", 10, [](rng &g, test_case &c) {
		my_rnd_pt = (short)g.range(0, 4095);
		rnd_count = (int)g.u32();
		c.call();
		make_my_rnd();
	});

	r.function("etc3", "my_rnd", 1000, [](rng &g, test_case &c) {
		my_rnd_pt = (short)g.range(0, 4095);
		my_rnd_sheet[(my_rnd_pt + 1) % 4096] = g.chance(90) ? g.range(0, 32767) : (int)g.u32();
		const int r = g.chance(90) ? g.range(1, 4096) : g.range(1, 0x7fffffff) * (g.chance(50) ? 1 : -1);
		c.arg(r);
		c.call();
		c.ret(my_rnd(r));
	});

	r.function("etc3", "rnd", 1000, [](rng &g, test_case &c) {
		rnd_count = (int)g.u32();
		const int x = g.chance(90) ? g.range(1, 65536) : g.range(1, 0x7fffffff) * (g.chance(50) ? 1 : -1);
		c.arg(x);
		c.call();
		c.ret(rnd(x));
	});

	r.function("etc3", "set_sprt_data", 4, [](rng &g, test_case &c) {
		if (g.chance(75))
		{
			for (int m = 0; m < MAX_SPRT; m++)
			{
				sprt[m].no = g.range(-1000, 1000);
				sprt[m].x = g.range(-1000, 1000);
				sprt[m].y = g.range(-1000, 1000);
				sprt[m].cx = g.range(-1000, 1000);
				sprt[m].cy = g.range(-1000, 1000);
				sprt[m].wd = g.range(-1000, 1000);
				sprt[m].ht = g.range(-1000, 1000);
				sprt[m].base_x = g.range(-1000, 1000);
				sprt[m].base_y = g.range(-1000, 1000);
				sprt[m].os_of_x = g.range(-1000, 1000);
			}
		}
		c.call();
		set_sprt_data();
	});

	r.function("etc3", "cloud_in_start", 12, [](rng &g, test_case &c) {
		set_sprt_data();
		rnd_count = (int)g.u32();
		random_kumo(g, g.range(0, 2));
		c.call();
		cloud_in_start();
	});

	r.function("etc3", "cloud_cont", 300, [](rng &g, test_case &c) {
		set_sprt_data();
		rnd_count = (int)g.u32();
		game_end = g.chance(80) ? 0 : g.range(1, 4);
		random_kumo(g, g.chance(97) ? 2 : g.range(0, 1));
		c.call();
		cloud_cont();
	});
}

// Not declared in all_forward.h.
int find_out_ss(int m, int n);

static void record_etc2(recorder &r)
{
	r.function("etc2", "find_out_size", 200, [](rng &g, test_case &c) {
		random_units(g);
		const int m = g.range(0, max_unit);
		const int n = g.range(0, max_unit);
		c.arg(m);
		c.arg(n);
		c.call();
		c.ret(find_out_size(m, n));
	});

	r.function("etc2", "pt_in_rect", 500, [](rng &g, test_case &c) {
		RECT rect = random_rect(g);
		const int x = g.range(-200, 1200);
		const int y = g.range(-200, 1200);
		arg(c, rect);
		c.arg(x);
		c.arg(y);
		c.call();
		c.ret(pt_in_rect(&rect, x, y));
	});

	r.function("etc2", "pt_in_rect2", 500, [](rng &g, test_case &c) {
		RECT rect = random_rect(g);
		const int x = g.range(-200, 1200);
		const int y = g.range(-200, 1200);
		arg(c, rect);
		c.arg(x);
		c.arg(y);
		c.call();
		c.ret(pt_in_rect2(&rect, x, y));
	});

	r.function("etc2", "pt_in_rect3", 500, [](rng &g, test_case &c) {
		RECT rect = random_rect(g);
		const int x = g.range(-200, 1200);
		const int y = g.range(-200, 1200);
		arg(c, rect);
		c.arg(x);
		c.arg(y);
		c.call();
		c.ret(pt_in_rect3(&rect, x, y));
	});

	r.function("etc2", "same_rect", 1000, [](rng &g, test_case &c) {
		RECT dstn_rect = random_rect(g);
		RECT src_rect = random_rect(g);
		RECT field_rect = random_rect(g);
		arg(c, dstn_rect);
		arg(c, src_rect);
		arg(c, field_rect);
		c.call();
		c.ret(same_rect(&dstn_rect, &src_rect, &field_rect));
		out(c, dstn_rect);
		out(c, src_rect);
		out(c, field_rect);
	});

	r.function("etc2", "seek_parking_no", 200, [](rng &g, test_case &c) {
		random_units(g);
		const int m = g.range(0, max_unit);
		c.arg(m);
		c.call();
		c.ret(seek_parking_no(m));
	});

	r.function("etc2", "plane_in_cv", 200, [](rng &g, test_case &c) {
		random_units(g);
		const int m = g.range(0, max_unit);
		c.arg(m);
		c.call();
		c.ret(plane_in_cv(m));
	});

	r.function("etc2", "set_pos_of_parking", 200, [](rng &g, test_case &c) {
		set_sprt_data();
		random_units(g);
		const int m = g.range(0, max_unit);
		c.arg(m);
		c.call();
		set_pos_of_parking(m);
	});

	r.function("etc2", "set_the_slct_unit", 200, [](rng &g, test_case &c) {
		random_units(g);
		your_side = (short)g.range(JPN, USA);
		for (int i = g.range(0, 8); i > 0; i--)
			slct_unit[g.range(0, 1)][g.range(0, 255)] = (short)g.range(1, 5);
		const int m = g.range(0, max_unit);
		c.arg(m);
		c.call();
		set_the_slct_unit(m);
	});

	r.function("etc2", "cls_all_slct_unit", 20, [](rng &g, test_case &c) {
		for (int i = g.range(0, 64); i > 0; i--)
			slct_unit[g.range(0, 1)][g.range(0, 255)] = (short)g.range(1, 5);
		c.call();
		cls_all_slct_unit();
	});

	r.function("etc2", "cls_all_slct_unit_p2", 60, [](rng &g, test_case &c) {
		for (int i = g.range(0, 64); i > 0; i--)
			slct_unit[g.range(0, 1)][g.range(0, 255)] = (short)g.range(1, 5);
		const int side = g.range(0, 2);
		c.arg(side);
		c.call();
		cls_all_slct_unit_p2(side);
	});

	r.function("etc2", "seek_effect_no", 100, [](rng &g, test_case &c) {
		const int used = g.chance(10) ? EFFECT_MAX : g.range(0, 64);
		for (int s = 0; s < used; s++)
			effect[s].layer = (short)g.range(UPPER, LOWER);
		for (int i = g.range(0, 4); i > 0 && used > 0; i--)
			effect[g.range(0, used - 1)].layer = 0;
		c.call();
		c.ret(seek_effect_no());
	});

	r.function("etc2", "seek_fire_no", 100, [](rng &g, test_case &c) {
		const int used = g.chance(10) ? FIRE_MAX : g.range(0, 64);
		for (int s = 0; s < used; s++)
			fire[s].used = g.range(1, 180);
		for (int i = g.range(0, 4); i > 0 && used > 0; i--)
			fire[g.range(0, used - 1)].used = 0;
		c.call();
		c.ret(seek_fire_no());
	});

	r.function("etc2", "rtn_damage_pt", 300, [](rng &g, test_case &c) {
		static const int kinds[] = { BLT, GUN, SHL, TPD, BOM, ASB, RAS };
		rnd_count = (int)g.u32();
		const int m = g.range(0, FIRE_MAX - 1);
		fire[m].kind = kinds[g.range(0, 6)];
		c.arg(m);
		c.call();
		c.ret(rtn_damage_pt(m));
	});

	r.function("etc2", "drctn_for_8", 1000, [](rng &g, test_case &c) {
		const int drctn = g.chance(90) ? g.range(-720, 720) : (int)g.u32();
		c.arg(drctn);
		c.call();
		c.ret(drctn_for_8(drctn));
	});

	r.function("etc2", "set_frmtn_of_ships", 200, [](rng &g, test_case &c) {
		random_units(g);
		const int s = g.range(0, max_unit);
		c.arg(s);
		c.call();
		set_frmtn_of_ships(s);
	});

	r.function("etc2", "set_pos_of_take_down", 200, [](rng &g, test_case &c) {
		rnd_count = (int)g.u32();
		random_units(g);
		const int n = g.range(0, max_unit);
		c.arg(n);
		c.call();
		set_pos_of_take_down(n);
	});

	r.function("etc2", "cont_pos_of_take_down", 200, [](rng &g, test_case &c) {
		rnd_count = (int)g.u32();
		random_units(g, true);
		const int n = g.range(0, max_unit);
		c.arg(n);
		c.call();
		cont_pos_of_take_down(n);
	});

	r.function("etc2", "find_out_ss", 300, [](rng &g, test_case &c) {
		rnd_count = (int)g.u32();
		random_units(g);
		const int m = g.range(0, max_unit);
		const int n = g.range(0, max_unit);
		c.arg(m);
		c.arg(n);
		c.call();
		c.ret(find_out_ss(m, n));
	});

	r.function("etc2", "find_out", 300, [](rng &g, test_case &c) {
		rnd_count = (int)g.u32();
		random_units(g);
		your_side = (short)g.range(JPN, USA);
		reveal = g.chance(5) ? 1 : 0;
		game_end = g.chance(5) ? g.range(1, 4) : 0;
		c.call();
		find_out();
	});
}

// A seed whose first rand() is a multiple of `x` in `percent` of the calls, so that a function that acts when
// rnd(x)==0 acts often enough to be tested.
static unsigned seed_for_rnd_zero(rng &g, int x, int percent)
{
	unsigned seed = g.u32();
	if (g.chance(percent))
	{
		for (;;)
		{
			srand(seed);
			if (rand() % x == 0)
				break;
			seed++;
		}
	}
	return seed;
}

// A distance from a unit to a point, around the distances that the landing code compares with: 160 (NSPW_THE_NET),
// 360 and 400.
static double random_landing_distance(rng &g)
{
	switch (g.range(0, 9))
	{
	case 0: return g.real(150.0, 170.0);
	case 1: return g.real(350.0, 370.0);
	case 2: return g.real(390.0, 410.0);
	case 3: return (double)g.range(0, 3);
	case 4: return g.real(0.0, 1000.0);
	default: return g.real(0.0, 450.0);
	}
}

// A transport of side `side` at unit m, inside the world, with a landing point (info[6], info[7]) near it, in a
// direction around its heading (within or beyond the 45 degrees that fire_now accepts).
static void random_transport(rng &g, int m, int side)
{
	unit[m].used = (short)side;
	unit[m].ctgry = SHIP;
	unit[m].kind = TR1;
	unit[m].x = random_coordinate(g, MAP_LEFT + 1000, MAP_RIGHT - 1000);
	unit[m].y = random_coordinate(g, MAP_BOTTOM + 1000, MAP_TOP - 1000);
	unit[m].drctn = g.chance(40) ? g.real(0.0, 360.0) : (double)g.range(0, 359);
	unit[m].spd = g.real(0.0, 1.0);
	unit[m].arm[0] = g.range(TR_SP, TR_GF3);
	unit[m].arm[1] = g.range(0, 1);
	unit[m].arm[2] = g.chance(80) ? max_unit + 1 : g.range(0, max_unit);
	unit[m].arm[4] = 1;

	const double off = g.chance(70) ? g.real(-60.0, 60.0) : g.real(-180.0, 180.0);
	const double dstc = random_landing_distance(g);
	unit[m].info[6] = (int)(unit[m].x + cos((unit[m].drctn + off) * a_PI) * dstc);
	unit[m].info[7] = (int)(unit[m].y + sin((unit[m].drctn + off) * a_PI) * dstc);
	if (g.chance(5))
	{
		unit[m].info[6] = (int)unit[m].x;
		unit[m].info[7] = (int)unit[m].y + (g.chance(50) ? 0 : g.range(-2, 2));
	}
}

// fire[]: a few fires in use, or all of them, so that seek_fire_no finds a free one or none.
static void random_fires(rng &g)
{
	const int used = g.chance(10) ? FIRE_MAX : g.range(0, 64);
	for (int s = 1; s < used; s++)
		fire[s].used = g.range(1, max_unit);
	for (int i = g.range(0, 4); i > 0 && used > 1; i--)
		fire[g.range(1, used - 1)].used = 0;
}

static void record_etc1(recorder &r)
{
	r.function("etc1", "set_pos_of_attack_TR1", 1000, [](rng &g, test_case &c) {
		max_unit = 180;
		rnd_count = (int)g.u32();
		const int m = g.range(1, max_unit);
		random_transport(g, m, g.range(JPN, USA));
		unit[m].em_flg[0] = 0;
		c.arg(m);
		c.call(seed_for_rnd_zero(g, 200, 60));
		set_pos_of_attack_TR1(m);
	});

	// The transports' branch of fire_now: the landing craft. your_side is 0, so that SoundPlayEffect hears nothing and
	// plays no sound.
	r.function("etc1", "fire_now_TR", 1500, [](rng &g, test_case &c) {
		max_unit = 180;
		your_side = 0;
		const int m = g.range(1, max_unit);
		random_transport(g, m, g.range(JPN, USA));
		random_fires(g);
		if (g.chance(40))
		{
			the_slct_unit = (short)m;
			slct_unit[1][m] = (short)g.range(1, 5);
			cmbt_menu_kind = (short)g.range(0, 2);
			cmbt_menu_slctd = (short)g.range(0, 5);
			bf_new_pp[1].cls = g.range(0, 1);
		}
		else if (g.chance(30))
			the_slct_unit = (short)g.range(1, max_unit);
		if (g.chance(40))
		{
			unit_info[0] = g.range(0, 1) ? TR1 : 0;
			unit_info[3] = g.chance(70) ? m : g.range(1, max_unit);
		}
		const int kind = g.chance(90) ? unit[m].arm[0] : g.range(TR_SP, TR_GF3);
		c.arg(m);
		c.arg(0);
		c.arg(kind);
		c.call();
		fire_now(m, 0, kind);
	});
}

// Land in cmbt_map: a few islands of tiles 1 to 9, as the maps have.
static void random_land(rng &g)
{
	for (int i = g.range(0, 12); i > 0; i--)
	{
		const int cx = g.range(10, 245);
		const int cy = g.range(10, 245);
		for (int y = cy - g.range(0, 3); y <= cy + g.range(0, 3); y++)
			for (int x = cx - g.range(0, 3); x <= cx + g.range(0, 3); x++)
				cmbt_map[y][x] = (unsigned short)g.range(1, 9);
	}
}

// The landing craft of cont_fire (TR_SP to TR_GF3): fires on their way to a landing point (info[6], info[7]),
// some of them close enough to land, near the facilities of a few units, over land and sea.
static void record_cont_fire(recorder &r)
{
	r.function("etc3", "cont_fire_TR", 1000, [](rng &g, test_case &c) {
		set_sprt_data();
		max_unit = 180;
		your_side = 0;
		cc_count = g.range(0, 100000);
		my_rnd_pt = (short)g.range(0, 4095);
		for (int i = 0; i < 4096; i++)
			my_rnd_sheet[i] = g.range(0, 32767);
		random_land(g);

		for (int i = g.range(0, 12); i > 0; i--)
		{
			const int n = g.range(1, max_unit);
			unit[n].used = (short)g.range(JPN, USA);
			unit[n].ctgry = SHIP;
			unit[n].kind = g.range(BB1, GF3);
			unit[n].x = random_coordinate(g, MAP_LEFT + 1000, MAP_RIGHT - 1000);
			unit[n].y = random_coordinate(g, MAP_BOTTOM + 1000, MAP_TOP - 1000);
		}

		for (int i = g.range(1, 6); i > 0; i--)
		{
			const int f = g.range(1, FIRE_MAX - 1);
			fire[f].used = max_unit + 1;
			fire[f].kind = g.range(TR_SP, TR_GF3);
			fire[f].info[6] = g.range(MAP_LEFT + 1000, MAP_RIGHT - 1000);
			fire[f].info[7] = g.range(MAP_BOTTOM + 1000, MAP_TOP - 1000);
			fire[f].info[8] = g.range(JPN, USA);
			const double dstc = g.chance(50) ? g.real(0.0, 30.0) : random_landing_distance(g);
			const double drctn = g.real(0.0, 360.0);
			fire[f].x = fire[f].info[6] + cos(drctn * a_PI) * dstc;
			fire[f].y = fire[f].info[7] + sin(drctn * a_PI) * dstc;
			fire[f].drctn = g.chance(70) ? drctn + 180.0 : g.real(0.0, 360.0);
			fire[f].spd = g.chance(80) ? 1.0 : g.real(0.0, 3.0);
			fire[f].spd_add = 0.0;
			fire[f].info[0] = g.chance(80) ? g.range(0, 360) : g.range(355, 365);
			fire[f].info[1] = 360;
			fire[f].info[2] = g.range(0, 400);
			if (g.chance(30))
			{
				// A facility at the landing point, where the craft builds nothing.
				const int n = g.range(1, max_unit);
				unit[n].used = (short)g.range(JPN, USA);
				unit[n].kind = g.range(AP, GF3);
				unit[n].x = fire[f].info[6] + g.range(-30, 30);
				unit[n].y = fire[f].info[7] + g.range(-30, 30);
			}
		}

		const int effects = g.chance(5) ? EFFECT_MAX : g.range(0, 64);
		for (int s = 0; s < effects; s++)
			effect[s].layer = (short)g.range(UPPER, LOWER);

		c.call();
		cont_fire();
	});
}

int record_function_tests(const char *directory)
{
	CreateDirectoryA(directory, NULL);
	if (!write_layout((std::string(directory) + "\\Layout.json").c_str()))
		return 1;

	recorder r(directory);
	record_math(r);
	record_etc1(r);
	record_etc2(r);
	record_etc3(r);
	record_cont_fire(r);
	return r.failed() ? 1 : 0;
}
