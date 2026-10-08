# Improvements
A proposal for improving the port beyond the exact refactoring of [Refactoring.md](Refactoring.md). That refactoring keeps the port's shape and behavior. This document starts where it ends, and goes further in two ways:

- **Changes of shape that keep behavior**, such as typed views per unit category. They are still guarded by the characterization traces, but they need the exact refactoring to be done first.
- **Changes of behavior**, such as fixing the original's quirks. Each one is a decision, because it breaks the parity with the original 1.10 and with the reference's traces.

This is a proposal, not a final design. [Open questions](#open-questions) lists what still needs to be decided.

## Rules
- **The layout invariant still holds** ([Refactoring.md](Refactoring.md#the-layout-invariant)). The state keeps the original's memory layout, so that the single-player mode can map the emulator's memory onto it, the original's files stay readable, and the reference's traces stay applicable.
- **Changes of shape follow Refactoring.md's rules**: one kind of change per commit, the characterization traces unchanged, and the same [hazards](Refactoring.md#hazards) checked.
- **Changes of behavior are separate commits**, each saying what changes and why, with the traces re-recorded in the same commit. They apply to OpenNSPW-only play: anything that must stay compatible with the original 1.10 (see [Multiplayer.md](Multiplayer.md#playing-against-the-original)) keeps the original's behavior.

## Invalid states
The original's types allow many states that make no sense:

- **One struct for every kind.** A `UNIT` has a carrier's capacity, a plane's parking number, a submarine's diving state and a base's build time, whatever its kind, and `info[]` slots mean different things per category.
- **Redundant fields.** `ctgry` follows from `kind`, but is stored separately, so a battleship in the plane category can be represented.
- **Mixed constant groups.** The modes, the supply action and the arming choices share one `#define` group (see [`CombatMenuItem`](RefactoringCatalog.md#combatmenuitem--short)). `FireKind` holds projectiles, a plane's loadout (`TUN`, `NTG`) and a transport's cargo (`TR_SP` ... `TR_GF3`).
- **Sentinels.** Unit 0 is "no unit", side 0 is "unused slot", and a fire's target is also its "used" flag.

The goal is that code cannot read or write a member that does not apply to the unit it holds. The storage cannot change, so the types that the game code sees change instead.

### What the exact refactoring already does
Without changing behavior or shape, [Refactoring.md](Refactoring.md) names what is valid: enums split by meaning (`UnitMode`, `CombatMenuItem`, `FireKind`), slot accessors named per category, and the catalog recording which category each accessor applies to. The accessors are still all on `Unit`.

### Typed views
Views over a unit's storage give each category only its own members:

```csharp
public readonly ref struct PlaneRef(ref Unit unit)
{
	private readonly ref Unit _unit = ref unit;

	public UnitState State { get => (UnitState)_unit.info[0]; set => _unit.info[0] = (int)value; }
	public UnitId Carrier { get => new(_unit.info[1]); set => _unit.info[1] = value.Value; }
	public FireKind Weapon { get => (FireKind)_unit.arm[0]; set => _unit.arm[0] = (int)value; }
}

if (unit.TryAsPlane(out var plane) && plane.State == UnitState.Flying) { ... }
```

- `ShipRef`, `PlaneRef` and `BaseRef`, and narrower ones where kinds differ (`CarrierRef`, `SubmarineRef`, `TransportRef`). `TryAs...` tests the category or kind exactly as the original's condition does, so a view replaces a condition without changing it.
- Views are `ref struct`s holding a `ref Unit`, so they cost nothing and never copy the unit.
- The category accessors move from `Unit` to the views. `Unit` keeps only what applies to every unit (side, kind, position, direction, speed, hit points), and the raw slots, marked as raw.
- Per-category enums replace the shared ones inside the views (`PlaneMode`, `ShipMode`, `PlaneLoadout`, `TransportCargo`), converted from the stored values by casts.
- The same applies to `Fire` and `Effect`, whose `info[]` slots mean different things per kind.

### Finding what the original relies on
Some code reads members that do not apply to the unit: old values left in a reused slot, a carrier's slot read through a plane, a range comparison (`info[5]<=SLOW`) that includes values the code never writes. The views make these visible, because they can only be written with the raw slots.

- A **strict mode**, used only in the tests, makes each view check that the unit has its category or kind, and checks invariants after each tick (`Category` matches `Kind`, a free slot is all zero, `Carrier` refers to a carrier or base of the same side). The characterization traces run with it.
- Each violation found is either rewritten to the view it means, if behavior stays the same, or kept on the raw slots, with a comment and an entry in a list of quirks.

### Fixing quirks
Each entry in the list of quirks can then be fixed, as a change of behavior, under the [rules](#rules) above. A fixed quirk makes its raw access unnecessary, so the raw slots shrink to what the files, the network and the emulator need.

The storage stays one struct with the original's layout even then. Separate storage per category (a table of ships, a table of planes) would need a conversion at the emulator's boundary, every call, and is not planned.

## Implementation steps
### 1. Typed views
`PlaneRef`, `ShipRef`, `BaseRef` and the narrower views, then the same for `Fire` and `Effect`, with the per-category enums. The category accessors move from `Unit` to the views.

### 2. Strict mode and quirks
The checks of the views and the invariants, in the tests, and the list of quirks that the characterization traces find.

### 3. Fixing quirks
One quirk per commit, each a decision.

Steps 1 and 2 keep behavior. Step 3 changes it.

## Open questions
- Which quirks are worth fixing, and whether a fixed quirk should still be selectable, for example for replays of games recorded before the fix.
- How narrow the views should get: per category only, or per kind wherever kinds differ (carriers, submarines, transports).
- Whether the strict mode should also run in debug builds of the desktop app, to find quirks in real games.
