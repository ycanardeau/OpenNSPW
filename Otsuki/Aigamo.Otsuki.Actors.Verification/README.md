# Aigamo.Otsuki.Actors.Verification

A model of `Mailbox<T>` ([Mailbox.cs](../Aigamo.Otsuki.Actors/Mailbox.cs)), the only code of Otsuki and OpenNSPW whose threads share memory, which [GenMC](https://github.com/MPI-SWS/genmc) checks in every order its threads' steps can take, and with every write that each read may see under a memory model: not a few million runs of a test, but all of them, for the threads and the messages of each scenario.

## Why a model checker

A test runs the mailbox on the processor it runs on, in the orders that the threads happen to take. A bug that one order in millions shows, or only another processor, passes it: the mailbox once left a message in its queue until the next post, about once in a hundred thousand posts on x64 (commit 2e6a32f), and none of its tests saw it. Its owner set a word with `Volatile.Write` and then read the queue, and the processor let the read go ahead of the write, still in its buffer.

A model checker that takes one thread's step at a time (sequential consistency, as Coyote does) finds no such bug: in every such order the old mailbox was right. It takes a memory model, which says what a processor may reorder.

## The models

`verify.sh` checks each scenario under each of:

| Model | What it is |
| --- | --- |
| `sc` | One thread's step at a time, in some order. |
| `tso` | x86 and x64: a store waits in a buffer while later loads go ahead. |
| `arm8` | Arm64. |
| `rc11` | C and C++'s atomics. |
| `imm` | The model whose programs compile correctly to x86, Arm and POWER: what holds under it holds on each of them. |

## What the model takes of .NET

Each step of the C# is a step of the model, with what .NET's memory model promises of it, or less, so that what is right in the model is right in .NET:

- An `Interlocked` method is a full fence on every architecture: a sequentially consistent read-modify-write between two sequentially consistent fences. (A C11 read-modify-write alone is not a fence for the accesses around it, and is too weak: with it, even the fixed mailbox fails under `rc11`.)
- `Volatile.Read` and `Volatile.Write`, as `ConcurrentQueue<T>` uses them, are an acquire load and a release store.
- `ConcurrentQueue<T>` is one segment of it (`ConcurrentQueueSegment<T>`), with its sequence numbers, which the scenarios never fill.
- `SemaphoreSlim` is an acquire on `Wait` and a release on `Release`.
- What one thread only touches (the owner's `_passed`, the handler's state) is plain: GenMC reports a race on it if two threads could touch it at once, or one not see what the other did before.

The thread pool runs each scheduled handler on a thread of its own. `ManualScheduler`, which runs on one thread, is not modelled.

## The scenarios

| Scenario | What must hold |
| --- | --- |
| `pooled_goes_idle` | A post as the handler goes idle is handled, without another post. |
| `pooled_posters_and_complete` | Posts race each other and `Complete`: each message taken is handled, and the completion is set once they are, and only then. |
| `pooled_start` | Posts race `Start`: what was held is handled. |
| `pooled_capacity` | A bounded mailbox handles all that it took. |
| `owner_sleeps` | An owner that sleeps on an empty mailbox wakes for each message, each poster's in its order. |
| `owner_complete` | An owner receives each message posted before `Complete`, then stops. |
| `owner_waits_for_a_kind` | An owner that waits for one kind of message, woken by another, takes its kind, and finds the other after. |

In each, the handler never runs twice at once, and each run sees what the one before did (a race on its plain state otherwise). A message left in the queue shows as a count that falls short, or, for an owner asleep for good, as a liveness violation (`--check-liveness`).

`verify.sh` then checks that the scenarios find the bug that was fixed: with `VOLATILE_UNMARK`, the owner sets its wake word back with a release store, as `Volatile.Write` did, and `pooled_goes_idle`, `pooled_start`, `owner_sleeps` and `owner_complete` must fail under `tso`, `rc11` and `imm`. They hold under `sc`, which is why a checker of thread orders alone does not find it, and under `arm8`, whose acquire load (`ldar`) waits for an earlier release store (`stlr`); but the JIT reads with `ldapr` or `ldapur` on the processors that have them, such as Apple's M4, which do not wait, and `imm` covers them.

## Keeping the model with the code

[mailbox.h](mailbox.h) records the hash of the Mailbox.cs that it follows, with its lines ended by LF, and `MailboxModelTests` fails when Mailbox.cs changes and the hash does not. A change to Mailbox.cs is then a change to mailbox.h, step for step, and `verify.sh`, before the new hash is recorded.

## Running it

GenMC 0.20 builds with LLVM 19 to 22 (its README):

```bash
git clone --depth 1 --branch v0.20.0 https://github.com/MPI-SWS/genmc
cmake -DCMAKE_BUILD_TYPE=Release -DCMAKE_PREFIX_PATH=/usr/lib/llvm-20/lib/cmake -B genmc/build -S genmc
cmake --build genmc/build
Otsuki/Aigamo.Otsuki.Actors.Verification/verify.sh genmc/build/bin/genmc
```

The CI runs it on each push to main and each pull request ([verification.yml](../../.github/workflows/verification.yml)). The model is of the program, not of a processor, so it runs on one: `imm` stands for all of them.

## What it does not cover

- Scenarios larger than these: a few threads and a few messages each. Concurrency bugs are known to show in small ones, as this one did with one poster and two messages, but a larger one is not checked.
- .NET itself: that the JIT emits for each processor the fences that its memory model promises, and that `ConcurrentQueue<T>` and `SemaphoreSlim` are right beyond what the model takes of them.
- The model's fidelity to the C#, which is kept by hand, step for step, and by the hash.
