# Report 0008 — Menus and schematic editing

Status: Done. Plan approved with six decisions; all parts implemented, built, tested and run.

## Results

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**, 10 projects, `net10.0`.
- `dotnet test Nodal.sln`: **865 tests, 865 passed, 0 failed, 0 skipped** (was 771).
  Foundation 241, Content 174 (was 160), Circuit 361 (was 342), **Editor 47 (new)**, Architecture 42 (was 28).
- Headless Godot run, exit 0, still prints `Loaded 11 component definitions, 0 diagnostics`,
  `Loaded 11 symbols, 0 diagnostics` and the five demo netlist lines.
- No comments in any hand-written file; no Git command was run; no new package.

## Commands

```
dotnet build Nodal.sln
dotnet test Nodal.sln
& $env:GODOT --headless --path game --quit-after 20
```

## Part B — the hover bug, root cause

**Cause: the hover was never recomputed after a camera change. It is a Godot-layer bug, not a core bug.**

`SchematicCanvas` only called `UpdateHoveredNet` from `HandleMotion`. Wheel zoom, middle-drag pan,
Home and window resize all changed `CanvasCamera` and called `QueueRedraw()`, but never re-ran the
hit test, and `HandleMotion` returned early while panning. The screen-to-grid mapping therefore
changed under a pointer that had not moved, and the highlight kept answering the **old** mapping
until the next mouse motion. The visible symptom is exactly the one reported: you zoom or pan to
look at a wire, point at it, and nothing lights up.

I reproduced it with a temporary headless probe (deleted before this report) that drove the real
`CanvasCamera` and `SchematicHitTester` over the demo schematic:

| Situation, pointer never moves | Highlight shown | Really under the pointer |
|---|---|---|
| three wheel notches in, anchored at the pointer | `N1` | none |
| middle-drag pan by (40, 25) | `N1` | none |
| a wire panned under the pointer | none | `N1` |

The same probe then drove the real `SchematicCanvas` node with synthetic input events and the fixed
canvas answered correctly in all four situations.

The other three suspects named in the spec were checked and were **not** the cause:

1. **Screen-to-grid conversion** is exact. `_GuiInput` positions are canvas-local, `ToScreen` and
   `ToGrid` are inverses, and in the old scene the canvas was anchored at (0, 0) so local and global
   agreed. It matters more now that the canvas sits below the menu bar, and the new layout keeps the
   canvas in its own rect, so the two can no longer drift apart.
2. **Shadowing of wires by pins** does not occur in this schematic: 0 of 54 wire-interior probes at
   zoom 0.25, 1.0, 3.31 and 8.0 were taken by a pin. It is still a real defect in principle (the old
   `NearestPin(...) ?? NearestWire(...)` let a pin 7 px away beat a wire 0.1 px away), and decision 2
   fixed it, but it is not what the project lead hit.
3. **The tolerance unit** was already correct: `pixels / PixelsPerGridUnit` converts screen pixels to
   grid units, which is the unit the hit test works in.

**Fix.** The canvas caches the last canvas-local pointer position and re-runs `RefreshPointer()`
after every camera change (wheel, pan, `FitToSchematic`, resize), which refreshes the hover, the
session cursor and the status bar together. The old full-rect canvas under an opaque status bar was
also replaced by a `VBoxContainer` layout, so no part of the canvas is hidden behind another node.
Hover and selection tolerance is now **8 screen pixels**.

Because the cause is in the Godot layer there is no core regression test for it. The related core
defect (pin shadowing) does have one: `ACloserWireIsNotShadowedByAPinInsideTheTolerance`.

## Changes

**Part A.** `ComponentDefinition` (`Pins`, `Parameters`), `ComponentCatalogLoadResult`,
`SymbolLibraryLoadResult` (`Diagnostics`), `ComponentCatalog.Definitions` and
`SymbolLibrary.Symbols` now hold `ImmutableArray`, with hand-written structural equality through
`StructuralEquality` on the three records. `ParameterDefinition` is unchanged: it holds no list and
already compares structurally. `SymbolLibraryLoader` reports an error when a file is not named
`<component>.<style>.json`, after the catalog check so an unknown component is still reported
against `$.component`. `docs/ideas.md` created with the base content plus the two appended sections.

**New layer.** `src/Nodal.Editor` (10 files) and `tests/Nodal.Editor.Tests`, both added to the
solution; `Nodal.Game` references it. `EditorSession` is the state machine host; `SelectTool`,
`PlaceTool` and `WireTool` are internal `IEditorTool` implementations; `EditorView`,
`ComponentGhost`, `Selection`, `EditorToolKind`, `WireSegment` and `WireRouter` are public.
`EditHistory` stayed in `Nodal.Circuit.Editing`.

**Core.** `GridBox` and `GridSnap` in `Nodal.Circuit.Geometry`; `SymbolExtent` made public with
`BoundingBox`; `SchematicHit` gained `ComponentId? Body` and a `Component` accessor;
`SchematicHitTester` gained a `SymbolLibrary` overload with component-body hit-testing.

**Godot.** `game/scripts/Input/` (`EditorAction`, `ShortcutStroke`, `ShortcutBinding`,
`EditorShortcuts`), `Presentation/EditorMenus` and `EditorCommand`, rewritten `MainScreen` and
`SchematicCanvas`, `VisualTokens.Selection = #2F6FDE`, `CanvasMetrics.HoverTolerancePixels = 8` and
`GhostOpacity = 0.5`, and a `Main.tscn` rebuilt as MenuBar / canvas / status bar in a `VBoxContainer`.

**Architecture tests.** `CoreLayers.Editor` and `TestProjects.Editor` added to the tables and to
every layering theory; new `EditorLayerTests` asserts that Circuit, Content and Foundation do not
reference Editor, that Editor references Circuit, and that the game references Editor.
`CLAUDE.md` and `docs/architecture.md` gained the Editor layer.

**ADRs.** `ADR-0014-editor-interaction.md` recorded with the two amended sentences and the added
`Nodal.Editor` sentence; `ADR-0015-product-positioning.md` recorded verbatim.

## Deviations

Significant choices the spec did not cover, per rule 10.

1. **The body extent excludes label anchors.** `SymbolExtent.Corners` now yields only the drawn
   primitives and the new `CornersWithLabels` adds the designator and value anchors; view fitting
   uses the latter, body hit-testing the former. With the anchors included, a rotated resistor's
   clickable box reached 1.2 grid units past its leads, so clicking clearly empty space selected it.
   Clicking the designator text therefore does **not** select the component.
2. **Changing tool clears the selection.** Entering Place or Wire with a component still highlighted
   in the selection colour read as a bug; the selection is dropped on every tool change.
3. **Undo and redo prune a vanished selection.** After undoing a placement the selected component
   may no longer exist, so the session clears a selection whose component or wire is gone.
4. **Moving requires the component to be selected first**, as the spec says ("left-drag on a
   *selected* component"): the press that selects does not also start a move. A second press begins
   the move. Escape during a move abandons it with no command.
5. **Menu accelerators are display-only.** Items carry a `Shortcut` built from the table with
   `SetItemShortcutDisabled(true)`, so the key text is shown but every keystroke is handled once, by
   the `InputMap` path in `MainScreen`.
6. **Shortcut matching is exact.** `IsActionPressed(..., exactMatch: true)`, otherwise Ctrl+R would
   also fire the plain `R` placement action.
7. **Space blocks only unmodified letters**, tested on the key event itself, so F3 and Delete still
   work while panning but R, C, L, D, G and W do not.
8. **`WireRouter` has two entry points**: `Route` takes the schematic and drops zero-length and
   already-existing segments (needed because `AddWire` fails on a duplicate and `CompositeCommand`
   aborts on the first failure); `Preview` drops only zero-length ones, so the rubber band still
   shows the full L over an existing wire.
9. **File > New builds a fresh `EditHistory`**, so the undo stack is discarded; `EditorMenus` reads
   the history through a `Func<EditHistory>` to survive that.
10. **About text is composed** from `NodalInfo.Name`, `NodalInfo.Version` and `" — development
    build"`, which renders as `Nodal 0.0.1 — development build` rather than being hard-coded.
11. **The duplicate (component, style) symbol check is now unreachable through a directory source**,
    since two files cannot share a name; it is kept as a guard and still tested in memory.
12. **`ComponentCatalog` and `SymbolLibrary` keep reference equality** (they are classes, not
    records), so two load results over different catalog instances are not equal even when their
    definitions are. Tests compare `Definitions` and `Diagnostics`.

## Manual checks for the project lead

Run `& $env:GODOT --path game`.

- [ ] **Hover, the reported bug.** Point at a wire: its whole net turns orange. Now **zoom with the
      wheel without moving the mouse** — the highlight updates immediately. Same with a middle-drag
      pan that brings a wire under a motionless cursor, and with Home.
- [ ] **Menu bar**: File (New, Quit), Edit, View, Place, Help only. No disabled items anywhere.
- [ ] **Edit menu** shows `Undo Add wire` on a fresh start (the demo ends on a wire) and follows every
      action; Redo is greyed until you undo.
- [ ] **Place menu**: Passive (Capacitor, Inductor, Resistor), Semiconductor (Diode, N-channel
      MOSFET, NPN transistor, P-channel MOSFET, PNP transistor), Source (DC current source, DC
      voltage source), Reference (Ground). R, C, L, D and G show their accelerator.
- [ ] **Place tool**: press `R`, a half-opacity blue resistor follows the cursor on the grid; Ctrl+R
      and Ctrl+E turn it; click several times to place several; Escape or right-click returns to
      Select. The status bar reads `Place Resistor` then `Select`.
- [ ] **Wire tool**: press `W`, click a start point, move — the preview goes horizontal then
      vertical; click to commit and continue; double-click, Escape or right-click ends the chain.
      One undo step per committed click.
- [ ] **Select**: click a pin, a wire, or a component body; the selection turns blue. Click an
      already-selected component and drag — a ghost shows the destination, release commits one
      `Move`. Attached wires stay where they were, and the connectivity warning count in the status
      bar rises, which is the expected behaviour from ADR-0014.
- [ ] **Delete / F5** removes the selection; Ctrl+Z and Ctrl+Y walk the history.
- [ ] **View**: Ctrl+= and Ctrl+- zoom on the cursor, Home refits, Show grid toggles the dots and
      keeps its checkmark.
- [ ] **Space + left-drag** pans and, while held, `R` does **not** place a resistor; `F3` still
      switches to the wire tool.
- [ ] **Help > Keyboard shortcuts** lists all fifteen actions; **Help > About** reads
      `Nodal 0.0.1 — development build`.
- [ ] **File > New** and **Quit** ask for confirmation while the history is not empty.
- [ ] Status bar reads, for example, `Nodal 0.0.1    3 nets    0 warnings    Select    (4, -2)`.

## Proposals for later specs

- The status bar is getting crowded; a segmented bar with fixed zones would read better than four
  separated strings.
- Placing a component on top of an existing one is allowed and produces overlapping symbols. A
  placement check, or at least a connectivity warning, would help.
- `MoveComponent` leaves dangling wires. The connectivity warning count makes it visible, but
  colouring dangling wire ends on the canvas would make it obvious.
- The Place menu lists the four transistors and the diode, which declare no parameter yet; they
  place and draw correctly but carry no value label until task 0009 gives them parameters.
- `ComponentCatalog` and `SymbolLibrary` could become records over `ImmutableArray` so that two
  independent loads compare equal, which would simplify content tests.
