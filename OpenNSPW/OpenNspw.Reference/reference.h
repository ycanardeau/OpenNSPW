#pragma once

// Forced-include header (/FI) of the reference build. It is included before every source file, including the
// unmodified files in Original/NSPW_NET, and redirects what the reference replaces.

// The math functions: both the reference and the port use the portable implementation in nspw_math.cpp.
// <math.h> is included first, so that its own declarations keep their names.
#include <math.h>
#include "nspw_math.h"

#define sin nspw_sin
#define cos nspw_cos
#define atan2 nspw_atan2

// The entry point: reference_main.cpp defines the real WinMain, which either runs the game's WinMain (win_main.cpp)
// or one of the reference's own tools.
#define WinMain nspw_WinMain
