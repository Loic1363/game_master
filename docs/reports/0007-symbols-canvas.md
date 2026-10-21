# Report 0007 — Symbols and schematic canvas (display only)

Status: Done. Plan approved with corrections; all six parts implemented, built, tested and run.

## Results

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**, 8 projects, `net10.0`.
- `dotnet test Nodal.sln`: **771 tests, 771 passed, 0 failed, 0 skipped** (was 630).
  Nodal.Foundation.Tests 241 (was 227), Nodal.Content.Tests 160 (was 87),
  Nodal.Circuit.Tests 342 (was 288), Nodal.Architecture.Tests 28.
- Headless Godot run, exit 0, prints `Loaded 11 component definitions, 0 diagnostics`,
  `Loaded 11 symbols, 0 diagnostics`, then the demo netlist:
  `V1 voltage-source plus=N1 minus=0 voltage=5 V`, `R1 resistor 1=N1 2=N2 resistance=1 kΩ`,
  `C1 capacitor 1=N2 2=0 capacitance=100 nF`, `GND1 ground gnd=0`, `GND2 ground gnd=0`.
- The demo analyses to 3 nets, 2 junctions, 0 diagnostics, so the status bar reads
  `Nodal 0.0.1    3 nets    0 warnings`.
- No comments in any hand-written file; no Git command was run; no new package; no new project.

## Commands

```
dotnet build Nodal.sln
dotnet test Nodal.sln
& $env:GODOT --headless --path game --quit-after 10
```

## Changes

Part A, `Nodal.Foundation.Collections.StructuralEquality` (public, per the approved correction):
`ItemsEqual` / `ItemsHash` for `ImmutableArray`, `EntriesEqual` / `EntriesHash` for
`ImmutableSortedDictionary`. Records given structural equality: **`Net`**, **`NetlistElement`**,
**`ConnectivityDiagnostic`**, **`ComponentInstance`** — the complete set of public records in `src/`
holding an `ImmutableArray` or an immutable dictionary before this task — plus the new **`Symbol`**,
**`SymbolPolyline`** and **`SymbolPolygon`**.

Part B, `src/Nodal.Content/Symbols/`: `Symbol`, `SymbolStyle`, the seven `SymbolPrimitive` records,
`SymbolLibrary`, `SymbolLibraryLoadResult`, `SymbolLibraryLoader`, the internal `SymbolReader`,
`SymbolFields` and `SymbolVocabulary`. `ContentDiagnostic` gained a `Warning` factory.
`Nodal.Foundation.Geometry.GridVector` is the shared fractional grid point.
The orientation transform is `Nodal.Circuit.Symbols.SymbolTransform` (it needs `Orientation`, which
lives in `Nodal.Circuit.Geometry`).

Part C: eleven `game/content/symbols/<id>.iec.json` files, with the corrected source symbols
(voltage source bisected along the lead axis, current source across it with an arrow beside the
circle at x = 1.3).

Part D, `game/scripts/`: `Rendering/VisualTokens`, `CanvasMetrics`, `CanvasCamera`, `CanvasText`,
`SymbolRenderer`, `SchematicCanvas`; `Presentation/MainScreen`, `DemoSchematic`; a rewritten
`scenes/Main.tscn` (canvas plus status bar, both coloured from the tokens in code).
Core additions used by the canvas: `Nodal.Circuit.Hit.SchematicHitTester` and `SchematicHit`,
`Nodal.Circuit.Geometry.GridBounds` and `SchematicBounds`, `Nodal.Circuit.Symbols.SymbolExtent`.

Part E: `tests/Nodal.Content.Tests/Symbols/` (base library, table-driven invalid files, library),
`tests/Nodal.Circuit.Tests/Symbols/SymbolTransformTests`, `Hit/SchematicHitTesterTests`,
`Equality/StructuralRecordEqualityTests`, `Geometry/SchematicBoundsTests`,
`tests/Nodal.Foundation.Tests/Geometry/GridVectorTests` and `Collections/StructuralEqualityTests`.

Part F: `ADR-0013-symbols-and-canvas.md` recorded verbatim.

## Deviations

Significant choices the spec did not cover, per rule 10.

1. **Fit uses symbol extents, not only pins.** The first rendered frame clipped the `GND1` and
   `GND2` designators, so `SchematicBounds.Of(schematic, catalog, symbols)` now covers pin
   positions, wire ends, every transformed primitive and both label anchors, rounded outward, plus
   a 2 grid unit margin. The view is fitted once at startup and on Home.
2. **Field rules the spec left open**, all errors: `width`, `height` and `radius` must be finite,
   greater than 0 and at most 10; an arc sweep must not be zero; a `points` list needs at least two
   points; a `text` value must not be empty; a point must be an array of exactly two numbers; the
   `component` field must match the component-id charset. Pin matching uses a 1e-9 tolerance.
3. **A symbol with an unreached pin is rejected**, like any other invalid file, so its component
   also raises the "no iec symbol" warning. Its (component, style) key is still reserved, so a
   second file for the same pair is reported as a duplicate rather than as a second lead error.
4. **Diagnostic order**: per-file errors in file order, then unknown component and duplicate pair,
   then lead errors, then the missing-symbol warnings in catalog order. The missing-symbol warning
   names the expected file, for example `diode.iec.json`.
5. **Hit-testing**: `SchematicHit(PinRef? Pin, WireId? Wire, double Distance)`. Any pin inside the
   tolerance beats every wire; ties fall to the lower component id, then definition pin order, then
   wire id. A negative or non-finite tolerance throws.
6. **Label text scales with zoom** (0.6 grid units, clamped to 6..64 px) and is centred on its
   anchor, horizontally and vertically.
7. **Navigation details**: one wheel notch zooms by 1.2 at the cursor; the hover is cleared when
   the pointer leaves the canvas; Space released during a drag ends the pan.
8. **Arc transform**: the start angle is normalised to [0, 360) and the sweep keeps its sign, so a
   mirrored arc carries a negative sweep.
9. **The demo issues no `SetParameter`**: the catalog defaults are already 5 V, 1 kΩ and 100 nF.
10. **Two `using Orientation = Nodal.Circuit.Geometry.Orientation;` aliases** in the game project,
    because Godot declares its own `Orientation`.
11. **Label anchors are per symbol**: `(0, ∓1.2)` for the horizontal two-pin parts, `(2.2, ∓0.4)`
    for the voltage source, `(2.6, ∓0.4)` for the current source, `(1.8, 0)` for the transistors,
    `(0, 1.8)` for ground. Parameterless symbols omit `labels.value`.

## Manual visual checks for the project lead

Run `& $env:GODOT --path game`.

1. The demo draws V1 (circle, vertical bar, `+` near the plus pin), R1 (rectangle), C1 (two plates,
   rotated 90°), GND1 and GND2, on a dotted background.
2. Junction dots appear at the two T-junctions on the bottom rail, under V1 and under C1.
3. Labels: `V1` / `5 V`, `R1` / `1 kΩ`, `C1` / `100 nF`, `GND1`, `GND2`.
4. Status bar reads `Nodal 0.0.1    3 nets    0 warnings`, and appends `net 0`, `net N1` or `net N2`
   while hovering a wire or a pin.
5. Hover a wire or a pin: every wire and pin of that net turns orange.
6. Wheel zooms at the cursor between 0.25 and 8; strokes stay 2 px thick; grid dots disappear below
   8 px spacing; middle-drag and Space with left-drag pan; Home refits.
7. **Known cosmetic issue:** C1 is vertical, so its rotated label anchors put `100 nF` and `C1` left
   and right of the plates, where the text touches the symbol. Horizontal parts are clear.
8. **Not reachable in this build:** the inductor, diode, NMOS, PMOS, NPN, PNP and current-source
   symbols are not in the demo schematic, so they cannot be seen until placing components exists in
   0008. Tests prove all eleven load, reach every pin and fit their bounding constraint.

## Proposals for later specs

- `ComponentDefinition`, `ParameterDefinition` and `ComponentCatalogLoadResult` hold `IReadOnlyList`
  and still compare by reference. Moving them to `ImmutableArray` plus `StructuralEquality` would
  finish Part A across the core.
- Orientation-aware label placement, or pushing labels outside the symbol bounding box at draw
  time, would fix the vertical-capacitor overlap without changing the file format.
- A temporary symbol sheet (a scene or a flag that lays out all eleven symbols in each of the eight
  orientations) would make the visual review complete before the editor exists. I can add it on
  request.
- The loader ignores the file name; a check that `<component>.<style>.json` matches the fields
  would catch copy-paste mistakes early.
