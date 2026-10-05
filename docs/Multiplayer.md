# Multiplayer
A proposal for how OpenNSPW should support multiplayer, including games with three or more players. It builds on how the original NSPW NET does it, described in [Original/docs/NSPW_NET/Synchronization.md](../Original/docs/NSPW_NET/Synchronization.md).

This is a proposal, not a final design. [Open questions](#open-questions) lists what still needs to be decided.

## Summary
- Keep the original's **deterministic lockstep**: every peer runs the whole simulation and only player orders are sent.
- Make the simulation **deterministic and testable before writing any networking**.
- Use a **host relay**: players send their orders to the host, and the host sends every peer one bundle per turn with all orders, in a fixed order.
- Design for **N players** from the start, even if the first version only allows two.
- Keep the **transport** behind an interface. Compatibility with the original's DirectPlay 8 protocol is optional and comes last.

## Why lockstep
The battle state is large: the original keeps 256 units with up to 64 waypoints each, 512 projectiles and 1024 effects. The simulation was written to run identically on every peer. Orders are small and infrequent, so sending only orders keeps bandwidth tiny regardless of the battle's size.

The alternative, a server that sends the state to clients, would mean redesigning the simulation and sending far more data. It would mainly help against cheating (see [Trade-offs](#trade-offs)).

## Playing against the original
Whether OpenNSPW must play against the original `NSPW_NET.exe` 1.10 is the biggest decision, because interop fixes almost everything else:

| | OpenNSPW only | Also against 1.10 |
| --- | --- | --- |
| Players | Any number | 2 only, because the original allows no more |
| Transport | Anything reliable and ordered | DirectPlay 8 over the network ([Otsuki](../Otsuki) for the messages, plus the reliable-session and connect/name-table logic) |
| Game protocol | Free to design | Exactly the original's: packed C structs, 20-tick turns with sync points at ticks 6 and 13, one order per turn, 1-byte checksums |
| Simulation | Deterministic across OpenNSPW builds | Bit-identical to the original: the MSVC `rand()` sequence, x87 floating point and the MSVC CRT's `sin`, `cos` and `atan2`, and the original's bugs, including the order in which it applies orders |

This proposal targets OpenNSPW-only play. It keeps interop possible as an optional extra transport and protocol, to be attempted only if the [parity test](#3-check-parity-with-the-original) shows that the simulation can match the original exactly.

## Determinism
The simulation is a pure step function: the state plus all players' orders for a tick produce the next state. Everything else follows from that rule:

- **Isolation.** Drawing, UI, audio and input never write simulation state. They read it, and input produces orders.
- **Random numbers.** The simulation uses one random generator, a copy of MSVC's `rand()`: `seed = seed * 214013 + 2531011; return (seed >> 16) & 0x7FFF`. Copying it costs nothing and keeps the parity test possible. Cosmetic effects use a separate generator that the simulation never reads, like the original's `my_rnd`.
- **Order of operations.** Units, projectiles and effects stay in fixed-size arrays processed in index order. The simulation never iterates over a `Dictionary` or `HashSet`, and never depends on wall-clock time or frame rate.
- **Floating point.** Basic `double` arithmetic is the same on x64 and ARM64, and since .NET 9, converting a floating-point number to an integer saturates the same way on every platform. `Math.Sin`, `Math.Cos` and `Math.Atan2` may give slightly different results on different platforms or runtimes, so the simulation uses its own implementations of them.
- **Hashing.** The simulation can compute a hash of its whole state. It is used for desync detection and in tests.

## Protocol
### Turns
The simulation advances in fixed ticks, grouped into turns of a few ticks each. Orders given during turn *k* are scheduled to run at the start of a later turn, *k* + *d*. The input delay *d* hides the network delay. The host can raise or lower it during the game, announced in a bundle, so every peer switches at the same turn.

Unlike the original, a player can give several orders per turn, and turns can be much shorter than the original's one second.

### Host relay
1. Each player sends its orders to the host, tagged with the turn number they are for. A player with no orders for a turn sends an empty message, so the host knows it is not waiting for more.
2. When the host has every player's orders for a turn, or gives up waiting for a player (see [Disconnects](#disconnects)), it sends every peer a **turn bundle**: the turn number and all orders, sorted by player ID.
3. Every peer, the host included, runs a turn only when it has that turn's bundle. Bundles are the only thing that changes the simulation.

Compared with every peer sending to every other peer:

- **The same order everywhere.** Peers apply orders exactly as the bundle lists them. This avoids the original's bug, where each peer applies its own order after the rival's, so the two peers apply them in opposite orders.
- **Simpler networking.** Only the host needs a reachable address. Spectators just receive bundles.
- **One place for decisions** about disconnects, input delay and resyncs.
- **Cost.** Orders from non-hosts take about half a round trip longer to arrive.

Messages are tagged with turn numbers and queued per turn. They are never stored in one global slot per message type, as the original does.

### Orders
Each order names units by their absolute unit number, never by a selection relative to the sender's side. Every peer checks orders with the same rules, using only simulation state, and drops orders for units the sender does not own. A sender applies its own order only when it comes back in a bundle, so all peers act on the same data.

## Players and sides
Two ways to have more than two players:

1. **Team play.** Japan and the US are still the only sides, but several players can share a side, each commanding part of its navy. The game rules hardly change. This should come first.
2. **More sides.** For example, a British navy. This is game design as much as networking (see below).

Both need these changes from the original's model:

- **Ownership.** Each unit has a `side` and an `owner` (a player). They replace the original's fixed unit-number ranges per side. Scenarios assign starting units to players.
- **Per-player state.** Supply points and reinforcement requests belong to a player and are part of the simulation, so every peer can check them. In the original, only the sender tracks its own supply points.
- **Visibility.** The original has one `found` flag per unit, meaning "seen by the other side". With teams it can stay as it is. With more sides it needs one flag per side (`found[side]`), and the simulation must ask whether the *attacking unit's* side can see the target.
- **Win conditions.** Each scenario's victory check must handle the sides it uses.

## Disconnects
When a player leaves or times out, the host puts "player *P* left before turn *k*" into a bundle. From that turn on, every peer handles *P*'s units the same way: either they stay idle, or a deterministic AI takes over. The single-player code (`set_cpu_root2` in the original) is a starting point for that AI. The game goes on for the remaining players.

If the host leaves, the game ends. Choosing a new host is possible later, because every peer has the full state.

## Desync detection and recovery
Every peer sends the host the state hash for each turn (or every few turns). If a peer's hash does not match the host's, the host sends that peer its saved state and the turn to continue from, and the peer replaces its state. This reuses the code for saving and loading games.

The original only detects a desync and asks players to restart from an autosave.

## Pacing
The slowest peer stalls everyone, as in the original. To keep that rare:

- Game speed and pause are orders, so all peers change them at the same turn. In the original each player sets their own speed.
- The host chooses the input delay from the slowest player's round-trip time.
- A peer that keeps falling behind can run extra ticks per frame to catch up.

## Transport
The game talks to the network through a small interface: connect, disconnect, send a reliable ordered message to a peer, and receive messages. The first implementations:

1. **In process**, for tests and for running several peers in one process.
2. **Over the network**, using TCP or a reliable UDP library.

A DirectPlay 8 transport using [Otsuki](../Otsuki) is only needed for interop with the original. It would also need the original's game protocol on top, as a separate protocol implementation.

## Trade-offs
- **Cheating.** Every peer has the full state, so a modified client could reveal hidden units. Lockstep cannot prevent this. It can only prevent illegal orders, because every peer checks them.
- **Input delay.** Orders take effect after a short delay, as in the original, but shorter.
- **Determinism must be maintained.** Any change that breaks determinism breaks multiplayer. The replay tests below guard against that.

## Implementation steps
### 1. Deterministic simulation
Separate the simulation from drawing, UI and audio, with the random generator and math functions described in [Determinism](#determinism).

### 2. Replay harness
A headless tool that runs the simulation from a seed, a scenario and a list of orders per turn, and outputs a state hash per turn. Running the same replay twice, and on different platforms, must give the same hashes. Recorded games become regression tests.

### 3. Check parity with the original
Add a per-turn state dump to the original NSPW NET source, which now builds (see [CHANGES.md](../Original/docs/NSPW_NET/CHANGES.md)). Run the same seed, scenario and orders through both, and compare. This shows how far OpenNSPW is from the original, which helps porting, and whether interop is realistic.

### 4. Two-player lockstep
Host relay, turn bundles, the in-process transport and then a network transport. Lobby: the host chooses the scenario and settings, assigns players, collects ready states, and sends the seed and settings.

### 5. Team play
Unit ownership, per-player state, and more than two players per side.

### 6. Optional
More sides, spectators, choosing a new host, and the DirectPlay 8 transport for interop with 1.10.

## Open questions
- Must OpenNSPW play against the original 1.10? This proposal assumes not.
- Does "three or more players" mean team play, more sides, or both?
- What should happen to a disconnected player's units: stay idle or be taken over by an AI?
- Which network library to use for the network transport.
