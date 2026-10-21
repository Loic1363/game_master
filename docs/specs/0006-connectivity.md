# Task 0006 — Connectivity and netlist

## Mode

Stay in the current mode. **First present your implementation plan** (files, types, public API, connectivity algorithm, test list), **then stop and wait** for the architect's approval, relayed by the project lead. Do not write code before approval.

## Context

Report 0005 is accepted as is. This task computes electrical connectivity from a schematic and compiles a netlist, the only input the future simulator will see (ADR-0003). All CLAUDE.md rules apply. No Godot changes.

## Part A — No-op edits

A command that changes nothing must not pollute the history.

- `MoveComponent` to the current position, and `SetParameter` to the current value, return success with the **same schematic instance**. An empty `CompositeCommand` already does this.
- `EditHistory.Execute`: when the returned schematic is the same instance as the current one, it records nothing, keeps the redo stack, and does not raise `Changed`. It still returns the success result.
- Add tests for both.

## Part B — Pin references and global nets

1. Add `PinRef` (a `ComponentId` plus a pin name), comparable, with ordering by component id and then by the pin's index in its definition. Expose the pin order through a helper that takes the catalog.
2. **Content schema addition** (schemaVersion stays 1; this is a backward-compatible optional field):
   - a pin may declare an optional `"globalNet"` string: uppercase letters, digits and underscore, or exactly `"0"`;
   - every pin with the same `globalNet`, anywhere in the schematic, belongs to that net;
   - update `game/content/components/ground.json` so its `gnd` pin has `"globalNet": "0"`;
   - extend the validator and add tests for valid and invalid values.

## Part C — Connectivity (`Nodal.Circuit.Connectivity`)

`ConnectivityAnalyzer.Analyze(Schematic, ComponentCatalog)` is pure and returns a `ConnectivityResult`.

**Connection rules:**

1. Connection points are wire endpoints and absolute pin positions.
2. A connection point connects to every wire whose segment contains it, at an endpoint or in the interior, and to every other connection point at the same position.
3. Two wires whose interiors cross, without an endpoint of either lying on the other, are **not** connected.
4. Pins sharing a `globalNet` value are connected regardless of position.

**Net naming:**

- A net containing a global net takes that name, for example `0`.
- All other nets are named `N1`, `N2`, and so on, in this order:
  - first the nets that contain pins, ordered by their smallest `PinRef`;
  - then the nets made only of wires, ordered by their smallest `WireId`.
- Naming is deterministic: the same schematic always gives the same names.

**`ConnectivityResult` exposes:**

- **Nets:** each net with its name, its `PinRef`s (sorted) and its `WireId`s (sorted).
- **Lookups:** the net of a `PinRef`, and the net of a `WireId`.
- **Junctions:** grid positions where the editor will draw a junction dot.
  - The degree of a point is the number of wire endpoints at it, plus 2 for each wire passing through it in its interior, plus the number of pins at it.
  - A junction is a point of degree 3 or more.
- **Diagnostics,** each with a severity, a code, an English message, and the related ids and positions:
  - **Warning `FloatingPin`:** a pin whose net contains no other pin.
  - **Warning `DanglingWireEnd`:** a wire endpoint of degree 1 with no pin on it.
  - **Warning `NoGround`:** the schematic has components but no net named `0`.
  - **Error `GlobalNetShort`:** one net contains two different global nets. The net takes the alphabetically first name.
  - **Error `UnknownDefinition`:** a component whose definition is missing from the catalog. Its pins are skipped.

Use a union-find structure. Correctness first: a straightforward approach with no spatial index is acceptable at this stage.

## Part D — Netlist (`Nodal.Circuit.Netlists`)

`NetlistCompiler.Compile(Schematic, ComponentCatalog)` returns a `Netlist`:

- **Elements:** one per component with a known definition, in ascending `ComponentId` order. Each holds the component id, designator, definition id, parameter values, and the net name of each pin, in definition pin order.
- **Net names:** the ordered list of net names.
- **Diagnostics:** those from connectivity.
- **`ToDebugText()`:** a stable, human-readable text, one line per element, for example `R1 resistor 1=N1 2=0 resistance=1 kΩ`. It is not a SPICE format; that comes with the simulator.

## Part E — Tests

Use small schematics built with commands from the real base catalog. Cover at least:

- two resistors in series sharing a wire;
- a T-junction, which connects, and is reported as a junction;
- an X crossing, which does not connect and is not a junction;
- a pin lying on a wire interior, which connects;
- two pins touching directly;
- several grounds merged into net `0`;
- a rotated and a mirrored component connecting correctly;
- every diagnostic: floating pin, dangling end, no ground, global-net short, unknown definition;
- deterministic naming: the same schematic built in a different command order yields the same net contents, and names that follow the ordering rule;
- netlist element order, pin-to-net mapping, and the exact `ToDebugText()` output for a small RC circuit: a voltage source, a resistor, a capacitor and a ground.

## Part F — ADR to record verbatim

Create `docs/decisions/ADR-0012-connectivity-rules.md` with exactly:

--- BEGIN ADR-0012 ---
# ADR-0012: Connectivity rules and net naming

Status: Accepted

## Context
The netlist is the only input to the simulator (ADR-0003), so connectivity rules must be unambiguous, match the habits of electronics engineers, and be deterministic. Ground must be expressible as data, not as a special case in code.

## Decision
Connection points are wire endpoints and pin positions. A connection point connects to every wire containing it and to every other connection point at the same position. Crossing wire interiors never connect. A pin may declare an optional globalNet in its definition; all pins with the same globalNet belong to the same net. The ground pin declares globalNet "0", following SPICE. Nets carrying a global net take its name; others are named N1, N2 and so on, by smallest pin reference, then by smallest wire id for wire-only nets. A point of degree three or more is a junction and gets a dot. Floating pins, dangling wire ends and a missing ground are warnings; a short between two global nets and an unknown definition are errors.

## Consequences
The component schema gains an optional, backward-compatible globalNet field on pins, which also enables future power symbols such as VCC. Net names can change when the schematic changes, so nothing persistent may refer to an N-name; persistent references use pin references. A spatial index may be needed later for very large schematics.
--- END ADR-0012 ---

## Acceptance criteria

- [ ] Plan presented and approved before implementation.
- [ ] `dotnet build Nodal.sln`: 0 warnings, 0 errors.
- [ ] `dotnet test`: all tests pass, with the count reported.
- [ ] The headless Godot run still prints `Loaded 11 component definitions, 0 diagnostics`.
- [ ] No comments in any hand-written file. No Git command was run. No new package.
- [ ] Spec saved at `docs/specs/0006-connectivity.md`; report at `docs/reports/0006-connectivity.md`.

## Out of scope

The simulator, SPICE export, schematic file save and load, any Godot or UI change, a spatial index.

## Decisions relayed at approval

1. **globalNet rule:** the stricter reading. A global net name starts with an uppercase letter, followed by uppercase letters, digits or underscores; `"0"` is the only exception.
2. **PinRef orderings:** `IComparable<PinRef>` stays context-free (component id, then pin name ordinal) for use as a sorted key. `PinRefOrder` (component id, then definition pin index) must be used for every net-facing ordering: net pin lists, smallest-`PinRef` selection for naming, `FloatingPin` diagnostic order, and netlist output. One test must prove the two orders differ on a component whose definition pin order is not alphabetical, for example the NPN.
3. **`ToDebugText()`:** `"\n"` between lines, no trailing newline.
