# Task 0005 — Schematic model, edit commands and undo

## Mode

This task starts in **plan mode**. Before writing any code, present your implementation plan: files, types, public API signatures, and how you will test. Then stop and wait. The project lead will relay the architect's approval or corrections. Implement only after approval.

## Context

Report 0004 is accepted as is. This task builds the schematic model in `Nodal.Circuit`: placed components, wires, orientation, parameter values, edit commands and undo/redo. Connectivity and the netlist are **not** part of this task (task 0006). No Godot changes.

All CLAUDE.md rules apply.

## Part A — Geometry (`Nodal.Circuit.Geometry`)

- `GridPoint`: an immutable integer point (`X`, `Y`), y pointing down, with addition and subtraction.
- `Rotation`: `R0`, `R90`, `R180`, `R270`, clockwise as seen on screen.
- `Orientation`: a `Rotation` plus a `Mirrored` flag. It transforms a pin offset by first mirroring horizontally (x becomes -x) if mirrored, then rotating clockwise. One clockwise quarter turn with y down maps (x, y) to (-y, x).
- The absolute pin position is the component position plus the transformed pin offset.

## Part B — Model (`Nodal.Circuit.Schematics`)

Every type in this part is immutable. Use `System.Collections.Immutable`, which is part of the base library and is not a new package.

- **`ComponentId` and `WireId`:** strongly typed identifiers wrapping an int. They are allocated sequentially per schematic and never reused, even after removal.
- **`ComponentInstance`:** `Id`, `DefinitionId` (the catalog id string), `Designator`, `Position`, `Orientation`, and the parameter values (parameter name to `Quantity`).
- **`Wire`:** `Id`, `Start`, `End`.
  - A wire is horizontal or vertical, never diagonal.
  - Its length is never zero.
  - Its constructor enforces both rules (ADR-0009).
- **`Schematic`:** components and wires by id, with deterministic enumeration in ascending id order, plus the next ids to allocate.
  - `Schematic.Empty` is the empty schematic.
  - A query returns the absolute pin positions of a component, given the `ComponentCatalog`.

## Part C — Commands

**Interface.** Every command implements an interface with:

- a human-readable English `Description`, such as `Add R1`, `Move R1`, `Rotate Q2`, `Set R1 resistance to 4.7 kΩ`, `Add wire`;
- an apply method taking a `Schematic` and a `ComponentCatalog`, and returning a result: either the new `Schematic`, or a failure with an error code and an English message.

Commands are pure. They never throw for player input (ADR-0009), and they never modify their input.

**Commands to implement:**

1. **`AddComponent(definitionId, position, orientation)`**
   - Fails if the definition id is unknown.
   - Assigns the designator automatically: the definition's prefix followed by the smallest positive number not already used with that prefix. With R1 and R3 present, the next is R2.
   - Initializes every parameter to its definition default.
2. **`RemoveComponent(id)`**
3. **`MoveComponent(id, newPosition)`**
4. **`RotateComponent(id)`:** one clockwise quarter turn.
5. **`MirrorComponent(id)`:** toggles `Mirrored`.
6. **`SetParameter(id, parameterName, Quantity)`**
   - Fails if the parameter does not exist for that component, if the unit does not match, or if the definition constrains the parameter to a series and the value is not in it.
7. **`AddWire(start, end)`**
   - Fails if the wire would be diagonal or of zero length.
   - Fails if an identical wire already exists, in either direction.
8. **`RemoveWire(id)`**
9. **`CompositeCommand(description, commands)`:** applies its commands in sequence. It is all or nothing: if any one fails, the whole command fails and the schematic is unchanged.

Every command that references an id fails cleanly when that id does not exist.

## Part D — Edit history

`EditHistory` holds the current schematic and the catalog.

- **`Execute(command)`:** on success, pushes the previous snapshot onto the undo stack and clears redo. On failure, nothing changes and the failure is returned.
- **`Undo()` and `Redo()`:** restore snapshots; they never invert commands.
- **`CanUndo`, `CanRedo`, `UndoDescription`, `RedoDescription`:** for the future Edit menu, which will show "Undo Move R1".
- **`Changed` event** (Observer pattern): raised after every successful execute, undo or redo.
- No history limit yet.

## Part E — Tests

- **Orientation:** all 8 orientations applied to the resistor and NPN pin offsets, with exact expected absolute positions.
- **Commands:** each command's success path and every one of its failure paths.
- **Designators:**
  - allocation with gaps (R1, R3 present gives R2);
  - independent counters per prefix;
  - reuse after removal.
- **Ids:** never reused after removal.
- **Composite:** atomicity; a failing third step leaves the schematic unchanged.
- **History:**
  - undo and redo sequences;
  - redo is cleared after a new execute;
  - descriptions;
  - `Changed` is raised exactly once per successful operation and never on failure.
- **Immutability:** a snapshot taken before a command is unchanged after it.
- Tests use the real base catalog from `game/content/components`, or an in-memory catalog where clearer.

## Part F — ADR to record verbatim

Create `docs/decisions/ADR-0011-immutable-schematic.md` with exactly:

--- BEGIN ADR-0011 ---
# ADR-0011: Immutable schematic model with snapshot undo

Status: Accepted

## Context
Every editor action must be undoable (architecture principle). The simulation will later run on its own thread and needs consistent views of the schematic. Tests need deterministic behavior.

## Decision
The schematic and all its parts are immutable values built on System.Collections.Immutable. Every edit is a command object (Command pattern) mapping a schematic to a new schematic, or returning an error without throwing for player input. The edit history stores previous snapshots, so undo restores a snapshot instead of inverting a command. Identifiers are sequential integers per schematic and are never reused. Geometry uses an integer grid with y pointing down; an orientation is an optional horizontal mirror followed by a clockwise rotation in quarter turns. Wires are horizontal or vertical segments; a bend is two wires. Designators take the smallest free number for their prefix.

## Consequences
Undo is correct by construction. Memory grows with history; structural sharing keeps this small, and a cap can be added later. The simulation can read a snapshot without locking. Commands must stay pure. Diagonal wires are not supported.
--- END ADR-0011 ---

## Acceptance criteria

- [ ] Plan presented and approved before implementation.
- [ ] `dotnet build Nodal.sln`: 0 warnings, 0 errors.
- [ ] `dotnet test`: all tests pass, with the count reported.
- [ ] The architecture tests still pass: `Nodal.Circuit` has no Godot reference.
- [ ] No comments in any hand-written file. No Git command was run. No new package.
- [ ] Spec saved at `docs/specs/0005-schematic-model.md`; report at `docs/reports/0005-schematic-model.md`.

## Out of scope

Connectivity, nets, junctions, the netlist, schematic file save and load, any Godot or UI change, multi-selection, copy and paste.
