#pragma once

// Records function tests: calls single functions of the original source with generated inputs, and writes the
// state before and after each call to <directory>/Functions/<file>/<function>.jsonl. Also writes the layout of the
// structs and globals to <directory>/Layout.json.
//
// Returns the process exit code.
int record_function_tests(const char *directory);
