#pragma once

#include <stddef.h>

#include <string>
#include <vector>

// A global variable of the game that is part of its state. Platform objects (handles, COM interfaces, pointers) are
// not part of the state.
struct nspw_global
{
	const char *name;
	void *address;
	size_t size;
};

extern const nspw_global nspw_globals[];
extern const int nspw_global_count;

std::string base64(const unsigned char *data, size_t size);

// A copy of every global in nspw_globals, in that order.
class nspw_state
{
	std::vector<unsigned char> bytes_;

public:
	static nspw_state take();

	// The state with every global set to zero.
	static nspw_state zero();

	void restore() const;

	// Appends the bytes that differ from `from` to `json`, as a JSON array of [global, offset, base64] runs.
	void append_diff(const nspw_state &from, std::string &json) const;
};

