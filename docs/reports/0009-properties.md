# Report 0009 — Properties dialog and parameter tiers

Status: Done. Plan approved with four rulings; all parts implemented, built, tested and run.

## Results

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**, 10 projects, `net10.0`.
- `dotnet test Nodal.sln`: **943 tests, 943 passed, 0 failed, 0 skipped** (was 865).
  Foundation 241 (unchanged), Content 189 (was 174), Circuit 385 (was 361), Editor 86 (was 47),
  Architecture 42 (unchanged).
- Headless Godot run, exit 0, prints `Loaded 11 component definitions, 0 diagnostics`,
  `Loaded 11 symbols, 0 diagnostics` and the five demo netlist lines.
- No comments in any hand-written file; no Git command was run; no new package.
- `FoldableContainer` exists and behaves as expected in Godot 4.7.2, so the fallback of ruling 4 was
  not needed.

## Commands

```
dotnet build Nodal.sln
dotnet test Nodal.sln
& $env:GODOT --headless --path game --quit-after 20
```

## Changes

**Part A1, click-and-drag.** `EditorSession.DragThreshold` (grid units, default 0.25) alongside
`HitTolerance`; `SchematicCanvas.UpdateTolerance` sets it from `CanvasMetrics.DragThresholdPixels =
4f` at the current zoom. `SelectTool` now records the press (`_pressed`, raw `_pressedAt`,
`_grabbedAt`) without moving, and `PointerMoved` promotes it to a move once the pointer passes the
threshold; release without promotion is a plain click. This replaces deviation 4 of report 0008.

**Part A2, dangling ends.** `SchematicCanvas.DrawDanglingEnds` draws every `DanglingWireEnd`
position as an unfilled 6 px square in `VisualTokens.Stroke`, between the wires and the junctions.

**Part A3, label placement.** New `Nodal.Circuit.Symbols.LabelPlacement` with `Gap = 0.4`,
`LineHeight = 0.6`, `Of(symbol, orientation)` and `Anchors(...)`, plus `LabelLayout`, `LabelAnchor`
and `LabelAlignment`. It transforms the drawn corners, takes the `GridBox` and branches on the
rotation only: centred above and below for R0 and R180, left-aligned and stacked to the right for
R90 and R270. `Symbol` lost `DesignatorAnchor` and `ValueAnchor`, `SymbolFields` lost `labels`,
`SymbolReader` lost `TryReadLabels` (so `labels` is now reported as `Unknown field 'labels'`), and
the field was removed from the eleven symbol files. `SymbolExtent.CornersWithLabels(symbol,
orientation)` yields the transformed corners plus both anchors, and `SchematicBounds` passes the
untransformed symbol with its orientation. `CanvasMetrics.LabelHeightInGridUnits` was deleted:
`CanvasText.FontSize` now reads `LabelPlacement.LineHeight`, and `CanvasText.DrawAligned` honours
the anchor alignment.

**Part B, schema.** `ParameterDefinition(Name, Label, Tier, Unit, Quantity? Default,
Quantity? Minimum, Quantity? Maximum, StandardSeries? Series)` with `IsRequired => Default is not
null`; new `ParameterTier`; `ComponentFields.Parameter` is now `[name, label, tier, unit, default,
min, max, series]`; `ComponentVocabulary` gained the tier table; `ComponentNameRules.IsParameterName`
enforces `[a-z][a-z0-9]*(-[a-z0-9]+)*`. The reader validates the label, the tier, an optional
default, optional bounds in the parameter's unit, `min <= max`, the default inside the bounds and
the default in the series.

**Part B, commands.** `AddComponent` seeds required parameters only. `SetParameter` checks the
bounds with the new `EditErrorCode.ValueOutOfBounds`. New `ClearParameter` unsets an optional
parameter, fails with `EditErrorCode.ParameterIsRequired` on a required one and returns the same
schematic instance when the parameter is already unset, so `EditHistory` records nothing. The
component and parameter lookup shared by both commands moved to an internal `ParameterLookup`
with a `ParameterTarget`.

**Part B, catalog.** Every existing parameter gained its label and `"tier": "basic"`; the twelve new
optional `advanced` parameters were added with `"min": "0"` (resistor 1, capacitor 6, inductor 3,
voltage-source 1, current-source 1). Diode, the four transistors and ground stay parameterless.

**Part C, `ParameterForm`.** `Nodal.Editor` gained `ParameterForm`, `ParameterField` and
`StepDirection`. The form exposes `Fields`, `FieldsOf(tier)`, `IsValid`, `HasChanges`,
`Write(field, text)`, `ApplySuggestion(field)`, `Step(field, direction)` and `BuildCommand()`, which
returns a `CompositeCommand` titled `Edit <designator>` over the changed fields only, or `null`.
Each field holds its raw text, its parsed value or an error, and a series suggestion.

**Part C, dialog.** New `game/scripts/Presentation/PropertiesDialog.cs`, a `ConfirmationDialog`
built in code: window title `<name> — <designator>`, the description below it, a `Basic` header with
its rows and an `Advanced` `FoldableContainer` folded by default, a `No parameters` line for a
parameterless component. Each row is a label `Capacitance (F)`, a `LineEdit` with the `not set`
placeholder, a `Use 4.7 nF` suggestion button and an error label in the new `VisualTokens.Error`
(`#C0392B`). The wheel over a series field steps the series, OK is disabled while the form is
invalid, Enter confirms from inside a field, Escape cancels, and the composite command runs through
`EditHistory`, giving one `Undo Edit C1` entry. Opening: `IEditorTool.SecondaryPressed(point)` and
`EditorSession.SecondaryPressed(GridVector)` route a right-click over a component to the new
`EditorSession.PropertiesRequested` event (a right-click elsewhere still cancels), a double-click in
Select raises the same event, and `Edit › Properties…` calls
`EditorSession.RequestPropertiesForSelection()`, the item being disabled without a selected
component.

**Docs.** `docs/specs/0009-properties.md` (spec plus the four rulings),
`docs/decisions/ADR-0016-parameter-tiers-and-labels.md` recorded verbatim. ADR-0013 is untouched, as
ruled.

## Verification of the dialog

The dialog cannot be driven by the unit tests, so a temporary headless probe (added, run, then
removed before this report) opened it on the demo capacitor and on a ground. It reported
`Capacitor — C1 fields 7 folders 1 ok disabled False` and `Ground — GND1 fields 0 folders 0`, with
no engine error, which confirms the row building, the advanced folder and the parameterless case.
Everything else in the dialog is signal wiring and needs the manual checks below.

## Deviations

Significant choices the spec did not cover, per rule 10.

1. **Label placement returns alignment, not just a point.** `LabelAnchor` carries a `LabelAlignment`
   (`Centered` or `Left`) because the vertical case is left-aligned, and `CanvasText.DrawAligned`
   consumes it. The anchor is the text's vertical centre in both cases.
2. **`LineHeight` lives in the core.** The 0.6 grid-unit label height moved from `CanvasMetrics` into
   `LabelPlacement`, so the gap, the stacking and the font size share one constant.
3. **The label slot exists even with no value.** Placement is pure geometry, so a parameterless
   component such as ground reserves the value slot; view fitting therefore grows slightly below it.
   The ground designator also moved from below the symbol (its old anchor) to above it.
4. **`SymbolExtent.CornersWithLabels` takes the untransformed symbol plus the orientation** and does
   the transform itself, since the placement rule needs the rotation and not only the box.
5. **`Step` is inert on an invalid or unset field**, and on a field with no series. It writes through
   the same validation path, so a stepped value that leaves the bounds shows a bound error.
6. **A non-positive value outside the series carries no suggestion**, since `ESeries.Nearest`
   requires a strictly positive value. The error is still `Not in E12.`.
7. **`ParameterForm` methods take the field object**, not its name, matching the spec's
   `Step(field, direction)`; a field from another form is rejected with `ArgumentException`.
8. **Enter inside a text field** emits the OK button's `pressed` signal when the form is valid,
   because a `LineEdit` consumes the key itself.
9. **`Basic` is a plain header, not a `FoldableContainer`**, because the spec says it is always
   expanded and a foldable section could be collapsed by the player.
10. **The dialog is rebuilt on each open**, so no field state survives a cancel.
11. **`EditorMenus` learns the selection through a `Func<bool>`**, like the existing
    `Func<EditHistory>`, because the menus are built before the session exists.
12. **The value label shows the first required parameter** (ruling 3), not the first declared one.
13. **A right-click over a component selects it as well as opening the dialog**, so the canvas shows
    which component is being edited.

## Manual checks for the project lead

Run `& $env:GODOT --path game`.

- [ ] **Click-and-drag.** Click empty space to clear the selection, then press on the resistor and
      drag in one gesture: it selects and a ghost follows immediately, release commits one
      `Move R1`. A press and release with a tiny wiggle (under 4 px) selects without moving and adds
      no undo entry.
- [ ] **Dangling ends.** Drag a component away from its wires: each orphaned wire end shows a small
      open square, and the warning count in the status bar rises.
- [ ] **Labels.** The designator sits above each horizontal symbol and the value below it; rotate
      with Ctrl+R to R90 and both labels move to the right of the symbol, upright, designator on
      top. Mirroring keeps them readable. Home refits with the labels inside the view.
- [ ] **Open the dialog** three ways: double-click C1, right-click C1, and Edit › Properties… with C1
      selected. The item is greyed with nothing selected. A right-click on empty space still clears
      the selection.
- [ ] **Dialog content.** Title `Capacitor — C1`, the description below it, `Basic` showing
      `Capacitance (F)` at `100 nF`, and a folded `Advanced` holding the six parasitic and rating
      rows, all empty with the `not set` placeholder. Unfold it and fold it again.
- [ ] **Validation.** Clear the capacitance: `Required.` appears in red and OK greys out. Type
      `5 nF`: `Not in E12.` with a `Use 4.7 nF` button; press the button and OK comes back. Type
      `5 V` into it: `Expected a value in F.`. Type `-1` into `Equivalent series resistance (Ω)`:
      `Must be at least 0 Ω.`.
- [ ] **Mouse wheel** over the capacitance field walks 100 nF → 120 nF → 150 nF and back down.
      The wheel over the advanced resistance fields does nothing, since they carry no series.
- [ ] **Confirm and undo.** Set the capacitance to `220 nF` and a voltage rating to `16 V`, press OK:
      the canvas value label reads `220 nF` and Edit shows `Undo Edit C1`; one undo restores both.
      Escape, or Cancel, applies nothing.
- [ ] **Clearing.** Reopen C1, empty the voltage rating field, press OK: the parameter is unset again
      and the undo entry is still a single `Edit C1`.
- [ ] **Parameterless component.** Open a ground or a transistor: the dialog shows its title,
      description and `No parameters`, and OK simply closes it.
- [ ] Status bar, menus, place, wire, zoom and the hover highlight behave as in report 0008.

## Proposals for later specs

- The dialog has no keyboard focus on the first field when it opens, so Enter confirms before
  anything is typed. A first-field focus would help.
- A parameter could carry a short help sentence for a tooltip; the schema has room for it.
- The mouse wheel over a non-series field could step by a decade, which would make the advanced
  parasitic fields as fast to fill as the basic ones.
- `Edit › Properties…` has no shortcut; Alt+Enter or F4 would be conventional.
- The canvas shows only one value label; a second line for a chosen advanced parameter (an ESR, say)
  would help when tuning a filter.
- Placing a component on top of an existing one is still allowed, and `MoveComponent` still leaves
  wires behind; the new dangling-end squares make the second visible but do not prevent it.
