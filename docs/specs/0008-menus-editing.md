# Task 0008 — Menus and schematic editing

## Mode

Stay in the current mode. **First present your implementation plan** (files, types, the tool state machine, the shortcut table, how menus are built, the hover bug investigation approach, tests), **then stop and wait** for approval.

## Context

Report 0007 is accepted. The project lead tested the canvas and reported that hovering does not reliably recognize some wires. This task fixes that, adds a menu bar, and makes the schematic editable: place, wire, select, move, delete, rotate, mirror, undo and redo. Parameter editing is task 0009. All CLAUDE.md rules apply.

## Part A — Housekeeping

1. `ComponentDefinition`, `ParameterDefinition` and `ComponentCatalogLoadResult`: replace `IReadOnlyList` members with `ImmutableArray`, with structural equality through `StructuralEquality`.
2. The symbol loader reports an error when a file name does not match `<component>.<style>.json`.
3. Create `docs/ideas.md` with exactly the content at the end of this spec.

## Part B — Hover reliability bug

The project lead reports that hovering does not recognize some wires. Reproduce it, find the root cause, fix it, and **explain the cause in the report**.

Check at least:

- the screen-to-grid conversion (canvas offset versus global coordinates, status bar height, camera transform);
- whether the hover is recomputed after pan and zoom without mouse motion;
- whether wires under or next to symbols are shadowed;
- whether the 6 px tolerance is applied in the right unit.

Raise the hover and selection tolerance to 8 screen pixels. Add a core test reproducing the cause if the bug is in the core; if it is in the Godot code, say so.

## Part C — Menu bar

A menu bar at the top of the window: **File, Edit, View, Place, Simulate, Tools, Help.** Every item shows its shortcut when it has one. Items that do not work yet are disabled, with the tooltip `Not available yet`.

| Menu | Items |
|---|---|
| File | New (confirm if the history is not empty); Open…, Save, Save As… (disabled); Quit (confirm if the history is not empty) |
| Edit | Undo and Redo (labels show the history descriptions, for example `Undo Move R1`, disabled when unavailable); Delete; Rotate; Mirror; Wire tool |
| View | Zoom in; Zoom out; Fit to schematic; Show grid (checkable) |
| Place | One submenu per category (Passive, Semiconductor, Source, Reference), listing every catalog component by display name. Built from the catalog, not hard-coded. |
| Simulate | Run, Stop (disabled) |
| Tools | Preferences… (disabled) |
| Help | Keyboard shortcuts (a dialog listing every shortcut, generated from the shortcut table); About (`Nodal 0.0.1 — development build`) |

## Part D — Editor tools

Tools form a small state machine (State pattern): **Select**, **Place(component)** and **Wire**. Escape, or a right-click with no action in progress, always returns to Select.

**Select:**

- Hit order on click: a pin selects its component; otherwise a wire; otherwise a component body (its transformed symbol extent).
- Only a single selection for now.
- Left-drag on a selected component moves it, snapped to the grid, with one `MoveComponent` on release, so one undo entry. Wires do not follow: this is a "move", not a "drag".
- Delete removes the selection.
- Ctrl+R rotates and Ctrl+E mirrors the selected component.

**Place:**

- A ghost of the component follows the cursor, snapped, at half opacity.
- Ctrl+R and Ctrl+E rotate and mirror the ghost.
- A left click places it with `AddComponent`, using the ghost's orientation, and the tool stays in Place for repeated placement, as in LTspice.

**Wire:**

1. The first click sets the start point.
2. The preview is an L shape: horizontal first, then vertical.
3. A click commits the non-zero segments as one `CompositeCommand` (`Add wire`), skipping any segment identical to an existing wire, then continues from the clicked point.
4. Escape, a right-click or a double-click ends the chain.

**Visual:**

- Add one visual token, selection `#2F6FDE`, in the existing token file only.
- The selected item is drawn in the selection color.
- The status bar adds the current tool (`Select`, `Place Resistor`, `Wire`) and the cursor's grid coordinates.

## Part E — Shortcuts

Shortcuts come from **one table** that drives the input handling, the menu shortcut display and the Help dialog. Remapping will come later; keep it possible. Use Godot InputMap actions registered from that table.

| Action | Keys |
|---|---|
| Place resistor, capacitor, inductor, diode, ground | R, C, L, D, G |
| Wire tool | W, F3 |
| Delete | Delete, F5 |
| Rotate | Ctrl+R |
| Mirror | Ctrl+E |
| Undo | Ctrl+Z |
| Redo | Ctrl+Y, Ctrl+Shift+Z |
| Cancel | Escape |
| Fit | Home |
| Zoom in, out | Ctrl+=, Ctrl+- |

Single-letter shortcuts must not fire while Space is held for panning.

## Part F — Core support and tests

- **Selection hit-testing in the core:** add component-body hit-testing to `SchematicHitTester`, using transformed symbol extents, with the hit order of Part D.
- **Wire routing as pure .NET:** given a start, an end and the existing schematic, return the segments to add. Test it, including skipped zero-length and duplicate segments.
- **Tool state transitions:** keep the transition logic in plain C#, independent of Godot nodes, so it can be unit-tested. If that is not reasonable in the game project, say why in the plan.
- **Tests** for all of the above.

## Part G — ADR to record verbatim

Create `docs/decisions/ADR-0014-editor-interaction.md` with exactly:

--- BEGIN ADR-0014 ---
# ADR-0014: Editor interaction model

Status: Accepted

## Context
The editor must feel familiar to LTspice users while staying simple to implement and test, and every action must go through undoable commands (ADR-0011).

## Decision
Editing uses a tool state machine with Select, Place and Wire tools; Escape always returns to Select. Placement repeats until cancelled. Wires are drawn as L-shaped routes, horizontal then vertical, committed one click at a time as a single command. Moving a component does not move attached wires; a connection-preserving drag is deferred. Shortcuts are defined in a single table that drives input, menus and help, so remapping can be added later. Hit-testing and wire routing live in the pure .NET core. Menu items not yet functional are shown disabled.

## Consequences
Players see the full menu structure early. Moving a component can leave dangling wires, which the connectivity warnings report. Adding a connection-preserving drag later will need a new command and possibly new routing logic.
--- END ADR-0014 ---

## Content of docs/ideas.md (record verbatim)

--- BEGIN ideas.md ---
# Ideas backlog

Ideas from the project lead, kept for future game design. Nothing here is a decision; decisions live in docs/decisions/.

## Game vision
- A game for curious people, not an educational game: challenging but approachable. Low floor, high ceiling.
- Players learn by doing and by seeing what things are for, never by reading a datasheet or following a course.
- Progressive levels, very simple at first.

## Troubleshooting mode
- Faulty boards to diagnose: a chip does not work, the player measures, forms hypotheses and finds the culprit (for example a blown diode shorting a 24 V supply).
- Components get fault states as data: short, open, drifted.
- Scoring can reward finding the fault with few measurements.

## Community
- Share circuits, including broken ones: "here is my circuit, why doesn't it work?"

## Wider simulation
- Sensors and actuators, analog and digital, motors, frequency drives.
- Electrical cabinets and racks built from the player's own boards, with a front view or a 3D view; alarm faults to troubleshoot in one's own or community cabinets.
- Industrial automation and PLCs, much later.

## Component parameters
- Parameters unlock progressively: a capacitor starts with capacitance only; voltage rating, RMS current rating, ESR, ESL and parallel parasitics appear as the player progresses. Inspired by LTspice's component dialogs, but game-like.

## Visual design (for the planned full design overhaul)
- The DC voltage source should be a plain circle with + and - markings, without a line through it, as in LTspice; a line through it can be confusing.
- Menus with icons.
- Full visual identity to be designed later; current colors are provisional.

## Editor
- Connection-preserving drag (wires follow the component), as LTspice's Drag.
--- END ideas.md ---

## Acceptance criteria

- [ ] Plan presented and approved before implementation.
- [ ] `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all pass, with the count reported.
- [ ] The headless Godot run still prints the catalog, symbols and demo netlist lines.
- [ ] Hover bug root cause explained and fixed.
- [ ] No comments in any hand-written file. No Git command was run. No new package.
- [ ] Spec saved at `docs/specs/0008-menus-editing.md`; report at `docs/reports/0008-menus-editing.md`, with a checklist of manual tests for the project lead.

## Out of scope

The parameter dialog (task 0009), file save and load, multi-selection, copy and paste, connection-preserving drag, simulation, icons.

## Amendment from the project lead (overrides the spec above)

1. Menus contain only working items. Omit Open…, Save and Save As… from File, and omit the Simulate and Tools menus entirely. New items will be added by the task that implements their feature. The "Not available yet" tooltip rule is removed.
2. In ADR-0014, replace the sentence "Menu items not yet functional are shown disabled." with "Menus contain only working items; each feature adds its own menu entries when it lands." In its Consequences, replace "Players see the full menu structure early." with "The menu bar grows with the features."

## Decisions taken with the approved plan

1. The tool state machine lives in a new pure .NET project `src/Nodal.Editor` (namespace `Nodal.Editor`), between `Nodal.Circuit` and the game. It may reference `Nodal.Foundation`, `Nodal.Content` and `Nodal.Circuit`, never Godot. It holds the tools, `EditorSession`, `EditorView`, `Selection`, `EditorToolKind`, `WireRouter` and `WireSegment`. `EditHistory` stays in `Nodal.Circuit.Editing`. `tests/Nodal.Editor.Tests` is created with the same setup as the other test projects, `Nodal.Game` references `Nodal.Editor`, and the architecture tests cover the new layer. `CLAUDE.md` and `docs/architecture.md` gain the Editor layer.
2. Hit-testing compares a pin and a wire by distance, the pin wins ties, and the body is always last.
3. `ComponentCatalog.Definitions`, `SymbolLibrary.Symbols` and `SymbolLibraryLoadResult.Diagnostics` also become `ImmutableArray` with structural equality. `ParameterDefinition` stays unchanged.
4. The shortcut table lives in the game project, `GridBox` is public, `SymbolExtent` is public.
5. ADR-0014 carries the two amended sentences plus a final sentence in its Decision section naming `Nodal.Editor`.
6. `docs/decisions/ADR-0015-product-positioning.md` is recorded verbatim, and the troubleshooting and boards sections are appended to `docs/ideas.md`.
