#!/usr/bin/env bash
# Checks the model of Mailbox<T> (mailbox.h) with GenMC: each scenario in every order its threads' steps can take,
# under each memory model, must hold; and with the volatile write that Mailbox.cs once had (VOLATILE_UNMARK), the
# scenarios that would have found it must fail, so that the checks are known to find such a bug. See README.md.
#
# Usage: verify.sh [path to genmc], or GENMC=... verify.sh.
set -uo pipefail

cd "$(dirname "$0")"
genmc="${1:-${GENMC:-genmc}}"

# sc: one thread's step at a time; tso: x86 and x64; arm8: Arm64; rc11: C and C++'s; imm: the model whose programs
# compile correctly to x86, Arm and POWER, and so is right on each of them.
models=(sc tso arm8 rc11 imm)
scenarios=(
	pooled_goes_idle
	pooled_posters_and_complete
	pooled_start
	pooled_capacity
	owner_sleeps
	owner_complete
	owner_waits_for_a_kind
)
# The scenarios that the volatile write fails, and the models under which it does: those that let a write wait while a
# later read goes ahead.
bug_scenarios=(pooled_goes_idle pooled_start owner_sleeps owner_complete)
bug_models=(tso rc11 imm)

failed=0

check() {
	local expect="$1" scenario="$2" model="$3"
	shift 3
	local output
	output=$("$genmc" "--$model" --check-liveness "$@" "$scenario.c" 2>&1)
	local result=fails
	grep -q "No errors were detected" <<<"$output" && result=holds
	if [[ "$result" == "$expect" ]]; then
		printf 'ok    %-28s %-5s %s %s\n' "$scenario" "$model" "$result" "$*"
	else
		printf 'FAIL  %-28s %-5s %s, expected to %s %s\n' "$scenario" "$model" "$result" "${expect%s}" "$*"
		echo "$output" | tail -20
		failed=1
	fi
}

for scenario in "${scenarios[@]}"; do
	for model in "${models[@]}"; do
		check holds "$scenario" "$model"
	done
done

for scenario in "${bug_scenarios[@]}"; do
	for model in "${bug_models[@]}"; do
		check fails "$scenario" "$model" -- -DVOLATILE_UNMARK
	done
done

exit $failed
