#include <stdlib.h>
#include <string.h>

#include "function_tests.h"

#include "all_head.h"

int WINAPI nspw_WinMain(HINSTANCE hInst, HINSTANCE hPrevInst, LPSTR lpCmdLine, int nCmdShow);

#undef WinMain

// The entry point of the reference.
//
//   OpenNspw.Reference.exe
//       Runs the game.
//   OpenNspw.Reference.exe --record-functions <directory>
//       Records the function tests to <directory>. See function_tests.h.
int WINAPI WinMain(HINSTANCE hInst, HINSTANCE hPrevInst, LPSTR lpCmdLine, int nCmdShow)
{
	if (__argc == 3 && strcmp(__argv[1], "--record-functions") == 0)
		return record_function_tests(__argv[2]);

	return nspw_WinMain(hInst, hPrevInst, lpCmdLine, nCmdShow);
}
