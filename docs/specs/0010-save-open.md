# Task 0010 — Save and open schematics

## Mode

Stay in the current mode. **First present your implementation plan** (file format, types, the read and repair rules, the dirty-state mechanism, the Godot file dialogs, tests), **then stop and wait** for approval.

## Context

Report 0009 is accepted. This task lets the player save and reopen schematics, plus three small comfort items. All CLAUDE.md rules apply.

## Part A — Comfort

1. When the properties dialog opens, focus the first editable field and select its text.
2. Add a Properties shortcut, **F4**, to the shortcut table. The menu item shows it.
3. In `ParameterForm`, `Step` on a field **without** a series and holding a valid, strictly positive value multiplies or divides it by 10, then re-validates through the normal path. An unset, zero, negative or invalid field does nothing. Update the tests.

## Part B — Schematic file format (`Nodal.Circuit.Persistence`)

A schematic file is UTF-8 JSON with the extension `.nodal`:

```json
{
  "schemaVersion": 1,
  "kind": "schematic",
  "createdWith": "Nodal 0.0.1",
  "nextComponentId": 6,
  "nextWireId": 7,
  "components": [
    {
      "id": 1,
      "definition": "resistor",
      "designator": "R1",
      "x": 6, "y": -4,
      "rotation": 0,
      "mirrored": false,
      "parameters": { "resistance": 1000 }
    }
  ],
  "wires": [
    { "id": 1, "from": [0, -2], "to": [0, -4] }
  ]
}
```

**Rules:**

- **Parameter values** are plain JSON numbers in SI base units, the unit coming from the definition. Values must round-trip exactly: never write formatted, rounded text.
- **`rotation`** is one of 0, 90, 180, 270, in degrees clockwise.
- **Output is deterministic and Git-friendly:**
  - components and wires ascending by id;
  - parameters in name order;
  - 2-space indentation, LF line endings, and a trailing newline.

**API:**

- `SchematicFile.Write(Schematic) -> string`.
- `SchematicFile.Read(string text, ComponentCatalog catalog) -> SchematicReadResult`, holding the schematic (or nothing) and diagnostics. It never throws for bad input (ADR-0009).
- Add a public, validated way to build a non-empty `Schematic` (proposal from report 0005), for example `Schematic.Create(components, wires, nextComponentId, nextWireId)`. It throws `ArgumentException` on broken invariants: duplicate ids, next ids not greater than every existing id, duplicate designators. The reader validates first, so the player gets diagnostics rather than exceptions.

**Read rules.** Errors refuse the file; warnings load it with repairs.

| Errors (file refused) | Warnings (file loaded, repaired) |
|---|---|
| Malformed JSON | Unknown component definition: keep the component; connectivity already reports it |
| Wrong `kind` | Unknown parameter name: dropped |
| `schemaVersion` other than 1 | Required parameter missing: set to its definition default |
| Missing or mistyped required field | Parameter value out of bounds or not in its series: required ones reset to their default, optional ones unset |
| Duplicate component or wire ids | Non-finite parameter value: required ones reset to their default, optional ones unset |
| Duplicate designators | |
| Invalid rotation | |
| Diagonal or zero-length wire | |
| Next ids not greater than every used id | |

- Unknown top-level or per-item fields are warnings and are ignored. This keeps future files partly readable by older builds.
- Every diagnostic carries a severity, a JSON path and an English message.

## Part C — Dirty state

- `EditHistory` gains a saved marker: `MarkSaved()` remembers the current schematic instance.
- `IsDirty` is true when the current schematic is not that instance.
- Undoing back to the saved snapshot makes it clean again.
- A new history starts clean.
- Raise `Changed` when dirtiness changes.

## Part D — Godot

**File menu:** New, Open… (Ctrl+O), Save (Ctrl+S), Save As… (Ctrl+Shift+S), Quit. Add the three new shortcuts to the shortcut table.

**Files:**

- The default folder is `Documents/Nodal`, from `OS.GetSystemDir`. Create it if it does not exist.
- Use Godot's `FileDialog` with the native dialog when available, filtered to `*.nodal`.
- Save on an untitled schematic acts as Save As.
- Write through a temporary file then rename it, so a crash never leaves a half-written file.

**Window title:** `Nodal — <file name>` or `Nodal — Untitled`, with a trailing `*` when dirty.

**Confirmations:** New, Open and Quit ask for confirmation only when the history is dirty (it was `CanUndo` before). The dialog offers Save, Discard and Cancel.

**Load results:**

- On errors, show a dialog listing them and keep the current schematic.
- On warnings, load the file and show a dialog listing what was repaired.
- In both cases the dialog lists at most 20 lines, plus `and N more`.

**Startup:** the demo schematic, untitled and clean.

File I/O lives in the game adapters. The format, the reading and the repairs live in the core.

## Part E — Tests

- Exact round-trip:
  - write then read gives a structurally equal schematic;
  - this holds for values such as 3333.3333 and 1e-12, for every rotation and mirror combination, and with non-contiguous ids after removals.
- Deterministic output: the same schematic always gives byte-identical text, for example via a golden file of the demo schematic.
- Every error and every warning in Part B, table-driven.
- `Schematic.Create` invariants.
- Dirty-state transitions: execute, undo to the saved point, redo, no-op commands, `MarkSaved`, a new history.
- `ParameterForm` decade stepping.
- Architecture tests still pass.

## Part F — ADR to record verbatim

Create `docs/decisions/ADR-0017-schematic-file-format.md` with exactly:

--- BEGIN ADR-0017 ---
# ADR-0017: Schematic file format

Status: Accepted

## Context
Players must save and reopen their work without loss, files should be readable and Git-friendly like the rest of the project, and a slightly damaged or outdated file should not destroy hours of work.

## Decision
A schematic is saved as a .nodal file: versioned, deterministic, indented JSON. Parameter values are stored as exact numbers in SI base units, never as formatted text. Component and wire identifiers are saved so references stay stable. Reading never throws: structural problems refuse the file with diagnostics, while value-level problems are repaired with warnings (unknown parameters dropped, invalid values reset to the default or unset), and unknown fields are ignored with a warning. Saving writes to a temporary file and renames it. The editor tracks a saved marker in the edit history to show unsaved changes.

## Consequences
Changing the format requires a new schemaVersion and a migration in the reader. Older builds can partly read newer files that only add fields. Repairs mean a loaded schematic can differ from the file; the player is always told what was changed.
--- END ADR-0017 ---

## Acceptance criteria

- [ ] Plan presented and approved before implementation.
- [ ] `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all pass, with the count reported.
- [ ] The headless Godot run still prints the catalog, symbol and demo netlist lines.
- [ ] No comments in any hand-written file. No Git command was run. No new package.
- [ ] Spec saved at `docs/specs/0010-save-open.md`; report at `docs/reports/0010-save-open.md`, with a manual test checklist.

## Out of scope

Recent files list, autosave, file migrations beyond version 1, saving content definitions or symbols, simulation.

## Rulings on the plan

1. File layout: use the indented `Utf8JsonWriter` as proposed, one array element and one member per line. The inline layout of the spec sample is not required; the in-box writer is preferred over a hand-written emitter.
2. Unknown definition: keep the component, but drop its stored parameters with one warning per dropped parameter (path and value in the message). Do not introduce placeholder units in the model. Preserving values of unknown definitions will be designed later if mods appear.
3. `SchematicDiagnostic` as a new type in `Nodal.Circuit.Persistence`: approved. Record it under Deviations as planned.
