# Task 0009 — Properties dialog and parameter tiers

## Mode

Stay in the current mode. **First present your implementation plan** (files, types, the schema changes, the ParameterForm logic, the Godot dialog structure, label placement, tests), **then stop and wait** for approval.

## Context

Report 0008 is accepted, with one interaction change in Part A. This task gives components editable parameters through a properties dialog, extends the parameter schema with labels, tiers, optional parameters and bounds, and fixes label placement. All CLAUDE.md rules apply.

## Part A — Editing comfort

1. **Click-and-drag in one gesture.**
   - In Select, pressing on an unselected component selects it, and dragging beyond a 4-screen-pixel threshold starts the move immediately.
   - A press and release without passing the threshold is a plain click.
   - Update the session tests.
2. **Dangling wire ends.** Draw every `DanglingWireEnd` position from the connectivity diagnostics as a small open square, 6 px wide, in the stroke color.
3. **Automatic label placement.** Labels no longer come from symbol files.
   - **R0 or R180:** the designator is centered above the transformed symbol bounding box and the value is centered below it.
   - **R90 or R270:** both labels sit to the right of the box, left-aligned and vertically centered, designator first, then value.
   - The gap is 0.4 grid units. Labels are always upright.
   - Remove the `labels` field from the symbol schema and from the eleven symbol files. The schema version stays 1: this is pre-release and no user data exists. The loader now rejects `labels` as an unknown field.
   - Compute placement in pure .NET (`Nodal.Editor` or `Nodal.Circuit.Symbols`), with tests for all eight orientations.
   - Update `SymbolExtent.CornersWithLabels` and view fitting accordingly.

## Part B — Parameter schema

Changes to the component parameter definition (schema version stays 1, same reason as above):

- **`name`:** lowercase words separated by single hyphens, matching `[a-z][a-z0-9]*(-[a-z0-9]+)*`, for example `voltage-rating`.
- **`label`:** required. An English display name, for example `"Voltage rating"`.
- **`tier`:** required, `"basic"` or `"advanced"`.
- **`default`:** now optional.
  - With a default, the parameter is **required**: it always has a value.
  - Without one, it is **optional**: unset by default, and the simulator will later treat an unset parameter as ideal or absent.
- **`min` and `max`:** optional, inclusive bounds written in quantity notation, in the parameter's unit.
  - A default must respect them.
  - `min` must be less than or equal to `max`.

**Model and commands:**

- A `ComponentInstance` holds values only for set parameters. Required parameters are always set.
- `AddComponent` initializes required parameters only.
- `SetParameter` also validates `min` and `max`, with a new error code.
- New command, `ClearParameter(id, name)`: it unsets an optional parameter. It fails with a new error code on a required parameter. Clearing an already unset parameter is a no-op and returns the same schematic instance.

**Catalog update.** Every existing parameter gets a label and the tier `basic`. Add these new optional `advanced` parameters, all with `min` `0`:

| Component | New parameters (name, label, unit) |
|---|---|
| resistor | power-rating, Power rating, watt |
| capacitor | voltage-rating, Voltage rating, volt; rms-current-rating, RMS current rating, ampere; esr, Equivalent series resistance, ohm; esl, Equivalent series inductance, henry; parallel-resistance, Equivalent parallel resistance, ohm; parallel-capacitance, Equivalent parallel capacitance, farad |
| inductor | series-resistance, Series resistance, ohm; current-rating, Current rating, ampere; parallel-capacitance, Parallel capacitance, farad |
| voltage-source | series-resistance, Series resistance, ohm |
| current-source | parallel-resistance, Parallel resistance, ohm |

Diode and transistor parameters will come with their simulation models; they stay parameterless.

## Part C — Properties dialog

**Logic in pure .NET.** Add a `ParameterForm` in `Nodal.Editor`. It is built from a component instance and its definition, and holds:

- per field: the raw text, the parsed `Quantity` or a validation error, and an optional series suggestion;
- the overall validity;
- the command to apply, built from the changed fields only.

Validation rules:

- An empty required field gives the error `Required`. An empty optional field means unset.
- A parse failure gives a readable message per `QuantityParseError`.
- A value outside `min` or `max` gives an error naming the bound.
- A value not in its series gives an error such as `Not in E12.`, plus a suggestion holding the nearest series value (`ESeries.Nearest`), which the dialog offers as a `Use 4.7 nF` button.
- `Step(field, direction)` moves a series-constrained field to `ESeries.Next` or `ESeries.Previous` of its current valid value. This is what the mouse wheel will drive.

On confirm, the form returns a `CompositeCommand` titled `Edit <designator>` containing a `SetParameter` or `ClearParameter` per changed field, or no command when nothing changed. Test everything.

**Godot dialog.**

- **Opening:** double-click a component in Select, or right-click a component in Select. A right-click on empty space keeps its current behavior. Add an Edit › Properties… menu item, enabled when a component is selected.
- **Title:** `<definition name> — <designator>`, for example `Capacitor — C1`. Below it, the definition description.
- **Sections:** "Basic", always expanded, and "Advanced", collapsed by default and expandable. Omit a section that has no parameters.
- **Each row:**
  - label with unit symbol, for example `Capacitance (F)`;
  - a text field holding the formatted value, or empty with the placeholder `not set`;
  - the inline error below the field, in a new visual token, error `#C0392B`;
  - the suggestion button when there is one.
- **Mouse wheel** over a series-constrained field steps through the series.
- **Buttons and keys:** OK is disabled while the form is invalid. Enter confirms, Escape cancels.
- **Applying:** the composite command goes through `EditHistory`, so confirming a change gives one undo entry, `Undo Edit C1`.
- A component without parameters opens the dialog with its title and description and a `No parameters` line.

## Part D — Tests

- Content:
  - the new schema fields and their validation;
  - every invalid case (bad name, missing label or tier, bad tier, default outside the bounds, `min` greater than `max`, bound in the wrong unit, `labels` now rejected in symbols);
  - the updated base catalog loads with 0 diagnostics.
- Circuit:
  - `AddComponent` sets only required parameters;
  - `SetParameter` bound errors;
  - `ClearParameter` success, required failure and no-op.
- Editor:
  - `ParameterForm`, every rule above;
  - click-and-drag in one gesture;
  - label placement in all eight orientations.
- Architecture tests still pass.

## Part E — ADR to record verbatim

Create `docs/decisions/ADR-0016-parameter-tiers-and-labels.md` with exactly:

--- BEGIN ADR-0016 ---
# ADR-0016: Parameter tiers, optional parameters and automatic labels

Status: Accepted

## Context
Players should discover component parameters progressively, as they learn, rather than face a full datasheet at once. Real components have parasitic and rating parameters that ideal simulation ignores. Label anchors in symbol files did not survive rotation.

## Decision
Each parameter has an English label, a tier (basic or advanced) and optional inclusive bounds. A parameter with a default is required and always set; a parameter without a default is optional, unset by default, and an unset parameter means ideal or absent in simulation. The properties dialog shows basic parameters and an advanced section collapsed by default; future progression may hide tiers the player has not unlocked. Labels are placed automatically from the transformed symbol bounding box, above and below for horizontal orientations and to the right for vertical ones, and always stay upright; symbol files no longer carry label anchors. This amends ADR-0013 on labels.

## Consequences
Adding a parasitic parameter is a data change only. The simulator must define the ideal behavior for every unset optional parameter. Symbol files are simpler, and label layout is consistent across components.
--- END ADR-0016 ---

## Acceptance criteria

- [ ] Plan presented and approved before implementation.
- [ ] `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all pass, with the count reported.
- [ ] The headless Godot run prints catalog and symbol lines with 0 diagnostics, plus the demo netlist.
- [ ] No comments in any hand-written file. No Git command was run. No new package.
- [ ] Spec saved at `docs/specs/0009-properties.md`; report at `docs/reports/0009-properties.md`, with a manual test checklist.

## Out of scope

Renaming designators, file save and load, simulation, progression gating of tiers, diode and transistor parameters, placement overlap checks.

## Rulings on the plan

1. DragThreshold: approved as proposed, a session property in grid units, set by the canvas from 4 screen pixels at the current zoom, like HitTolerance.
2. Error messages: normalize. Every validation message is a full sentence ending with a period, so "Required." instead of "Required". "Not in E12." stays as is. The spec wording was imprecise.
3. Also approved: SchematicCanvas showing the first required parameter as the value label, and leaving ADR-0013 untouched since ADR-0016 records the amendment.
4. If FoldableContainer turns out not to be available or not to behave as expected in Godot 4.7.2, fall back to a header Button that toggles the visibility of the advanced rows, and mention it in the report.
