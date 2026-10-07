#include <stdio.h>
#include <string.h>

#include "state.h"

#include "all_head.h"
#include "all_extern.h"

// Globals defined outside win_main.cpp and not declared in all_extern.h.
extern int exist_auto_save;		// demo.cpp
extern bool g_bDeviceLost;		// draw.cpp

#define G(name) { #name, (void *)&name, sizeof name },

// In the order of their definitions in win_main.cpp, then the other files.
const nspw_global nspw_globals[] = {
	// ウィンドウアプリケーション変数
	G(appActive)
	G(fullscreen)

	// DirectSoundの変数
	G(snd_)

	// ゲーム用
	G(cc_count)
	G(key_cndtn)

	// デバグ
	G(dbg_menu)
	G(dbg)

	// 通信対戦用
	G(dlg_answer)
	G(g_dwNumberOfActivePlayers)
	G(g_strAppName)
	G(g_strLocalPlayerName)
	G(g_strRivalPlayerName)
	G(g_strPreferredProvider)
	G(g_strRemoteHostname)
	G(g_bHostPlayer)

	// iNSPWからもってきたやつ
	G(sprt)
	G(missed_pending)
	G(a_paint_speed)
	G(FrameCount)
	G(scrn_mode)
	G(video_memory)
	G(anti_air)
	G(reveal)
	G(cmbt_x)
	G(cmbt_y)
	G(scrn_moving_spd)
	G(unit)
	G(max_unit)
	G(the_slct_unit)
	G(old_the_slct_unit)
	G(slct_unit)
	G(slct_unit_no)
	G(crsr_pt)
	G(unit_info)
	G(fire)
	G(max_fire)
	G(effect)
	G(cls_flg)
	G(cmbt_map)
	G(kumo)
	G(lf_btn)
	G(ri_btn)
	G(cmbt_menu_kind)
	G(cmbt_menu_slctd)
	G(wrk_pp_x)
	G(wrk_pp_y)
	G(new_pp)
	G(new_slct)
	G(new_menu)
	G(rest_time)
	G(game_end)
	G(decision_point)
	G(your_side)
	G(game_speed)
	G(mode)
	G(demo_time)
	G(sinario)
	G(map_edit)
	G(put_trgt)
	G(put_kind)
	G(put_kind_sub)
	G(rein)
	G(user_sinario_fn)

	// 通信対戦用
	G(cnct_game)
	G(you_are_host)
	G(you_were_host)
	G(you_can_order)
	G(you_ordered)
	G(bf_new_pp)
	G(bf_new_slct)
	G(bf_new_menu)
	G(bf_game_system_menu)
	G(game_system_menu)
	G(bf_slct_unit)
	G(go_next_1)
	G(go_next_2)
	G(join_game_start)
	G(rival_mode)
	G(my_rnd_sheet)
	G(my_rnd_pt)
	G(cnct_game_rnd_sheed)
	G(host_side)
	G(decision_sw)
	G(arrival_cont)
	G(rnd_count)
	G(bf_cc_count)
	G(bf_rnd_count)
	G(bf_unit_chk)
	G(ccc_out)
	G(rnd_out)
	G(unit_out)
	G(ccc_wait)
	G(cnct_loop_ct)
	G(cnct_loop)
	G(cnct_loop_pt1)
	G(cnct_loop_pt2)
	G(spry_pt)
	G(spry_no_cont)
	G(spry_trgt)
	G(spry_rate)
	G(first_spry_pt)
	G(rvrs_time)
	G(rvrs_rule)
	G(map_now)
	G(bf_arrived_unit)
	G(auto_save_time)
	G(last_tick)
	G(last_tick2)
	G(tick_now)
	G(tick_diff)

	// 通信対戦デバグ用
	G(first_r_error)

	// チャット用
	G(input_chat_now)
	G(my_chat)
	G(friend_chat)
	G(my_chat_dsp_time)
	G(friend_chat_dsp_time)
	G(rival_ver)

	// demo.cpp
	G(exist_auto_save)

	// draw.cpp
	G(g_bDeviceLost)
};

const int nspw_global_count = sizeof nspw_globals / sizeof nspw_globals[0];

std::string base64(const unsigned char *data, size_t size)
{
	static const char table[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

	std::string out;
	out.reserve((size + 2) / 3 * 4);
	size_t i = 0;
	for (; i + 3 <= size; i += 3)
	{
		const unsigned v = (data[i] << 16) | (data[i + 1] << 8) | data[i + 2];
		out += table[(v >> 18) & 63];
		out += table[(v >> 12) & 63];
		out += table[(v >> 6) & 63];
		out += table[v & 63];
	}
	if (size - i == 1)
	{
		const unsigned v = data[i] << 16;
		out += table[(v >> 18) & 63];
		out += table[(v >> 12) & 63];
		out += "==";
	}
	else if (size - i == 2)
	{
		const unsigned v = (data[i] << 16) | (data[i + 1] << 8);
		out += table[(v >> 18) & 63];
		out += table[(v >> 12) & 63];
		out += table[(v >> 6) & 63];
		out += '=';
	}
	return out;
}

nspw_state nspw_state::take()
{
	nspw_state state;
	for (int i = 0; i < nspw_global_count; i++)
	{
		const unsigned char *p = (const unsigned char *)nspw_globals[i].address;
		state.bytes_.insert(state.bytes_.end(), p, p + nspw_globals[i].size);
	}
	return state;
}

nspw_state nspw_state::zero()
{
	nspw_state state;
	for (int i = 0; i < nspw_global_count; i++)
		state.bytes_.resize(state.bytes_.size() + nspw_globals[i].size);
	return state;
}

void nspw_state::restore() const
{
	size_t offset = 0;
	for (int i = 0; i < nspw_global_count; i++)
	{
		memcpy(nspw_globals[i].address, &bytes_[offset], nspw_globals[i].size);
		offset += nspw_globals[i].size;
	}
}

// Runs closer than this are merged, so that a struct array whose fields all changed is written as one run.
static const size_t merge_gap = 32;

void nspw_state::append_diff(const nspw_state &from, std::string &json) const
{
	json += '[';
	bool first = true;
	size_t base = 0;
	for (int g = 0; g < nspw_global_count; g++)
	{
		const size_t size = nspw_globals[g].size;
		const unsigned char *a = &from.bytes_[base];
		const unsigned char *b = &bytes_[base];
		size_t i = 0;
		while (i < size)
		{
			if (a[i] == b[i])
			{
				i++;
				continue;
			}

			const size_t start = i;
			size_t end = i + 1;
			for (;;)
			{
				size_t next = end;
				while (next < size && next < end + merge_gap && a[next] == b[next])
					next++;
				if (next >= size || next >= end + merge_gap)
					break;
				end = next + 1;
			}

			if (!first)
				json += ',';
			first = false;
			char head[128];
			sprintf_s(head, "[\"%s\",%u,\"", nspw_globals[g].name, (unsigned)start);
			json += head;
			json += base64(b + start, end - start);
			json += "\"]";
			i = end;
		}
		base += size;
	}
	json += ']';
}
