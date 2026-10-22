# Report 0010 — Save and open schematics

Status: Done. Plan approved with three rulings; all parts implemented, built, tested and run.

## Results

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**, 10 projects, `net10.0`.
- `dotnet test Nodal.sln`: **1047 tests, 1047 passed, 0 failed, 0 skipped** (was 943).
  Foundation 241 (unchanged), Content 189 (unchanged), **Circuit 486 (was 385)**,
  **Editor 89 (was 86)**, Architecture 42 (unchanged).
- Headless Godot run, exit 0, prints `Loaded 11 component definitions, 0 diagnostics`,
  `Loaded 11 symbols, 0 diagnostics` and the five demo netlist lines.
- No comments in any hand-written file; no Git command was run; no new package
  (`System.Text.Json` ships with the base library).

## Commands

```
dotnet build Nodal.sln
dotnet test Nodal.sln
& $env:GODOT --headless --path game --quit-after 20
```

## Changes

**Part A.** `PropertiesDialog.Open` ends with a deferred `FocusFirstField`, which grabs focus on the
first row whose `LineEdit` is visible in the tree and selects its text. `EditorAction.Properties`
(F4) joined the shortcut table and the `Properties…` item now shows it. `ParameterField.Step` keeps
the series walk and, with no series, multiplies or divides by 10; both paths write through `Fill` →
`Write` → `Validate`, so a stepped value outside the bounds shows the usual bound error.

**Part B,** `src/Nodal.Circuit/Persistence/`: `SchematicFile` (public `Write`, `Read` and
`Extension`), `SchematicWriter`, `SchematicReader`, `SchematicReadResult`, `SchematicDiagnostic`,
`SchematicSeverity`, plus the internal `SchematicFields`, `SchematicFormat`, `RotationDegrees`,
`JsonPath` and `JsonValueKindNames`. The writer uses `Utf8JsonWriter` with `Indented = true` and
`NewLine = "\n"`, writes parameter values through `WriteNumberValue` (shortest round-trippable form),
orders parameters by name and relies on the schematic's sorted dictionaries for ids, then appends the
trailing newline. The reader mirrors `ComponentDefinitionReader`: one pass with `Reject`/`Warn`, all
structural checks first, then the parameter repairs, and `Schematic.Create` only once nothing was
rejected, so `Read` cannot throw. `Schematic.Create(components, wires, nextComponentId, nextWireId)`
is public and throws `ArgumentException` on duplicate ids, duplicate designators and next ids that
are not greater than every used id.

**Part C.** `EditHistory` holds a `_saved` schematic, set from the constructor, with `MarkSaved()`
and `IsDirty => !ReferenceEquals(Schematic, _saved)`. `Execute`, `Undo` and `Redo` already raise
`Changed` exactly when the schematic instance changes, which is when dirtiness can change; a no-op
command returns the same instance and raises nothing.

**Part D.** `EditorCommand` gained `FileOpen`, `FileSave`, `FileSaveAs`; `EditorAction` gained
`Open` (Ctrl+O), `Save` (Ctrl+S), `SaveAs` (Ctrl+Shift+S) and `Properties` (F4), all in the shortcut
table and in the File and Edit menus. New `game/scripts/Adapters/SchematicFiles.cs` owns the file
I/O: the `Documents/Nodal` folder, `WithExtension`, `TryRead`, and `TryWrite` writing `<path>.tmp`
then replacing the target. New `game/scripts/Presentation/SchematicDocument.cs` holds the current
path and builds the window title, `Nodal — Untitled` or `Nodal — filter.nodal`, with `*` when dirty;
`MainScreen` refreshes it from the history's `Changed`. `MainScreen` also builds the two
`FileDialog` nodes (filesystem access, native dialog, `*.nodal`), the diagnostics `AcceptDialog`
(at most 20 lines plus `and N more`), and the confirmation dialog now offering Save, Discard and
Cancel on `IsDirty` instead of `CanUndo`. Startup is the demo schematic, untitled and clean.

**Part E,** `tests/Nodal.Circuit.Tests/Persistence/` (round-trip, determinism and golden file, 39
refused files and 17 repaired files, both table-driven), `Schematics/SchematicCreateTests.cs`,
`Editing/DirtyStateTests.cs`, and the rewritten stepping tests in `ParameterFormTests`. The golden
file is `tests/Nodal.Circuit.Tests/Persistence/golden-schematic.nodal`.

**Part F.** `docs/decisions/ADR-0017-schematic-file-format.md` recorded verbatim.

## Verification of the Godot layer

The dialogs cannot be driven by the unit tests, so a temporary headless probe (added, run, then
removed before this report) exercised the adapter and the title: it printed
`probe default dir=C:/Users/FlowUP/Documents/Nodal`, `probe clean title=Nodal — Untitled
dirty=False`, a successful temporary-file write and read back, `probe loaded=True diagnostics=0
components=5 wires=6`, and `probe tracked title=Nodal — nodal-probe.nodal*`. Everything else is
signal wiring and needs the manual checks below.

## Deviations

Significant choices the spec did not cover, per rule 10. Items 1 to 3 were ruled by the architect
during plan review; the rest are mine.

1. **Indented `Utf8JsonWriter` layout**: one array element and one member per line, so
   `"from": [\n 0,\n -2\n ]` rather than the sample's inline form.
2. **Unknown definition**: the component is kept and its stored parameters are dropped, one warning
   per dropped parameter carrying the path and the raw value. No placeholder units enter the model.
3. **`SchematicDiagnostic` and `SchematicSeverity` are new types** in `Nodal.Circuit.Persistence`.
   `ContentDiagnostic` needs a source file name and `Read` only takes text; this is a third
   diagnostic idiom in the repo, alongside `ContentDiagnostic` and `EditError`.
4. **Which fields are required.** Per component: `id`, `definition`, `designator`, `x`, `y`,
   `rotation`; `mirrored` defaults to false and `parameters` to empty. `createdWith` is known,
   optional and ignored on read. Per wire: `id`, `from`, `to`.
5. **A mistyped optional structural field refuses the file** (`mirrored` not a boolean, `parameters`
   not an object). The conservative option is to refuse rather than silently reinterpret geometry.
6. **A parameter value that is not a number at all** (string, boolean, object) takes the same repair
   path as a non-finite one: required resets to the default, optional is unset. ADR-0017 says
   value-level problems are repaired.
7. **Ids must be positive whole numbers**, read as a mistyped required field when they are not;
   `Schematic.Create` also refuses next ids below 1.
8. **`IsSteppable` changed meaning**, from "the parameter has a series" to "a step would change the
   value" (set, valid and strictly positive), because the properties dialog gates the mouse wheel on
   it. The old assertion in `SteppingDoesNothingWithoutASeries` was replaced.
9. **The adapter uses `System.IO`**, not Godot's `FileAccess`, for these absolute user paths: it
   gives an atomic `File.Move(overwrite: true)` and readable error messages. `GodotContentSource`
   keeps `FileAccess` for `res://`. The default folder is joined with Godot's `PathJoin` so the path
   stays Godot-style, and it is created lazily when a dialog opens, so a headless run touches
   nothing.
10. **Save inside a confirmation** is asynchronous when the schematic is untitled: the pending New,
    Open or Quit runs after a successful write, and is abandoned if the save dialog is cancelled or
    the write fails.
11. **The golden file embeds `"createdWith": "Nodal 0.0.1"`**, so bumping `Version` in
    `Directory.Build.props` will require regenerating it.
12. **`SchematicReadResult` exposes `Errors` and `Warnings`** so the dialogs can list one severity,
    and `MarkSaved` raises `Changed` only when it actually clears a dirty state.

## Manual checks for the project lead

Run `& $env:GODOT --path game`.

- [ ] **Title.** The window reads `Nodal — Untitled`. Place a resistor: it becomes
      `Nodal — Untitled*`. Undo: the star disappears.
- [ ] **Save As.** File › Save As… (Ctrl+Shift+S) opens in `Documents/Nodal`, which exists. Save as
      `filter.nodal`: the title becomes `Nodal — filter.nodal`, with no star. Open the file in a text
      editor: indented JSON, LF endings, components then wires, ids ascending, values as plain
      numbers.
- [ ] **Save.** Move a component, then Ctrl+S: the star disappears with no dialog. Check the file
      changed, and that no `filter.nodal.tmp` is left behind.
- [ ] **Open.** File › New, then Ctrl+O and reopen `filter.nodal`: the schematic comes back
      identically, clean, with the file name in the title. Home fits it.
- [ ] **Confirmations.** With unsaved changes, File › New offers Save, Discard and Cancel. Cancel
      keeps everything; Discard clears to an empty untitled schematic; Save writes first, then
      clears. With no unsaved changes, New, Open and Quit do not ask.
- [ ] **Save inside a confirmation on an untitled schematic**: pressing Save opens the save dialog,
      and cancelling it abandons the New or Quit.
- [ ] **A repaired file.** Edit a saved file by hand: change `"resistance": 1000` to `1234`, add
      `"colour": "red"` to a component and an unknown parameter. Reopen it: the schematic loads and a
      dialog lists the repairs; the resistor reads `1 kΩ` again.
- [ ] **A refused file.** Break a brace, or set `"rotation": 45`, and reopen: a dialog lists the
      errors and the current schematic is untouched.
- [ ] **Properties comfort.** Select a capacitor and press F4: the dialog opens with the capacitance
      field focused and its text selected, so typing replaces it. The mouse wheel over
      `Series resistance (Ω)` of a voltage source now walks decades, and over the capacitance still
      walks E12.
- [ ] **Keyboard shortcuts** (Help › Keyboard shortcuts) lists Open, Save, Save as and Properties.
- [ ] Status bar, menus, place, wire, zoom, drag and the hover highlight behave as in report 0009.

## Proposals for later specs

- A recent files list and an autosave, both explicitly out of scope here.
- `Quit` through the window close button does not pass through the confirmation yet; wiring
  `WindowCloseRequest` to the same path would close the gap.
- The repaired-file dialog lists paths such as `$.components[0].parameters.resistance`; naming the
  designator instead would read better for a player.
- Saving the view (zoom and pan) and the selection in the file, as an optional `view` object that
  older builds would simply ignore.
- A file written by a future build with a higher `schemaVersion` is refused; a "read anyway" option
  could be offered once a version 2 exists.
- The golden-file test ties the output to the product version; a `createdWith` that older and newer
  builds both ignore on read is already the case, so the golden could mask it if that becomes a
  nuisance.

## Open questions

None.
