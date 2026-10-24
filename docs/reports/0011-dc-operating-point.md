# Report 0011 — DC operating point (linear components)

Status: Done. Plan approved with six rulings; all parts implemented, built, tested and run.

## Results

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**, 12 projects, `net10.0`.
- `dotnet test Nodal.sln`: **1180 tests, 1180 passed, 0 failed, 0 skipped** (was 1047).
  Foundation **250** (was 241), Content 189 (unchanged), Circuit **488** (was 486),
  **Simulation 93** (new), Editor **99** (was 89), Architecture **61** (was 42).
- Headless Godot run, exit 0, prints the catalog and symbol lines, the five netlist lines and the
  DC solution: `net 0 = 0 V`, `net N1 = 5 V`, `net N2 = 5 V`, `V1 V=5 V I=0 A P=0 W`,
  `R1 V=0 V I=0 A P=0 W`, `C1 V=5 V I=0 A P=0 W`. The currents are **exactly** zero, as ruling 2
  intends.
- No comments in any hand-written file; no Git command was run; no new package.

## Commands

```
dotnet build Nodal.sln
dotnet test Nodal.sln
& $env:GODOT --headless --path game --quit-after 20
```

## Changes

**Part A.** `MainScreen._Ready` sets `GetTree().AutoAcceptQuit = false` and a new `_Notification`
routes `NotificationWMCloseRequest` into `OnCommand(EditorCommand.FileQuit)`, so the close button,
File › Quit and the Save/Discard/Cancel dialog share one path. `SchematicDiagnostic` gained
`Subject` and `Location => Subject ?? Path`; `SchematicReader` reads the designator first and passes
it to every warning raised inside a component, and the parameter messages take the approved
wording (`R1: resistance 1.23 kΩ is not in E24; reset to 1 kΩ.`, `…; unset.`,
`R1: resistance was missing; set to the default 1 kΩ.`). Errors keep the JSON path.

**Part B.** `src/Nodal.Simulation` (Foundation, Content, Circuit; no Godot) and
`tests/Nodal.Simulation.Tests`, both added to `Nodal.sln`. `DisjointSet` moved to
`Nodal.Foundation.Collections` as a public type with guards, `Count` and `AreJoined`, used by both
the connectivity graph and the simulator (ruling 4). Architecture tests: Simulation joined
`CoreLayers` and the two layering theories, `TestProjects` gained the new project and Simulation in
the Editor and Architecture allow-lists, and new `SimulationLayerTests` checks that Foundation,
Content and Circuit reference neither its assembly nor its project, that Simulation references
Circuit but not Editor, and that Editor and the game may reference it. CLAUDE.md and
`docs/architecture.md` updated.

**Part C.** `LinearAlgebra/`: `DenseMatrix` (square, `Add`, `MaxMagnitude`, `IsFinite`, `Copy`) and
`LuDecomposition` (in-place LU, partial pivoting, `TryFactor`/`Solve`). `Dc/`: `DcAnalysis.Solve`
with `public const double Gmin = 1e-12`, `OperatingPoint`, `ComponentOperatingPoint`
(voltage, current, power per ruling 5), the internal `DcModels`, `DcElements`, `DcElement`,
`DcBranch`, `DcRatings`, `DcUnknowns`, `DcSystem`, `DcPaths`, `VoltageBranchCycles`, `RatingChecks`.
`Diagnostics/`: `SimulationDiagnostic`, its code and severity. The flow is refusals → resolve
elements → structural DC-path analysis → assemble (gmin only on the nodes that need it) → factor →
solve → warnings.

**Part D.** `src/Nodal.Editor/Simulation/OperatingPointOverlay` computes the net labels (first
junction, else first-wire midpoint, else first pin; ground skipped; `undefined` for a `NoDcPath`
net) and the hover line `R1: 5 V, 5 mA, 25 mW absorbed`. The game adds the Simulate menu with
**DC operating point** on **F9** in the shortcut table, runs the analysis on the UI thread, draws
the voltage pills and rating-flagged symbols in the error token, reports `DC: solved` or
`DC: not run`, lists refusals and rating warnings in the capped dialog, and clears the overlay
whenever the solved `Schematic` instance is no longer current, including on New and Open.

**Part E.** 93 simulation tests across `Dc/` (divider, Wheatstone bridge, current source,
Thevenin, superposition, inductor, capacitor, floating node, singular, refusals, ratings, power
balance, result shape), `LinearAlgebra/` and `Diagnostics/`, plus 10 overlay tests in
`Nodal.Editor.Tests` and 9 `DisjointSet` tests in `Nodal.Foundation.Tests`.

**Part F.** `docs/decisions/ADR-0018-dc-operating-point.md` recorded verbatim with the gmin
sentence replaced as ruling 2 dictates.

## Verification of the Godot layer

A temporary headless probe (added, run, then removed before this report) drove the real
`RunDcOperatingPoint` on the demo circuit and printed
`probe status=Nodal 0.0.1    3 nets    0 warnings    DC: solved    Select    (0, 0)`,
`probe label N1 at (0, -3) reads 5 V`, `probe label N2 at (8, -3) reads 5 V`,
`probe hover V1=V1: 5 V, 0 A, 0 W absorbed`, `probe hover GND1=none`. Drawing, the dialogs and the
close button need a window and are in the checklist below.

## Deviations

Choices the spec did not cover, per rule 10.

1. **`WireGeometry` is now public** in `Nodal.Circuit.Connectivity`. Deciding which junction belongs
   to a net needs a point-on-wire test, and duplicating that geometry in the Editor was worse.
2. **Part D's logic lives in `Nodal.Editor.Simulation`** (`OperatingPointOverlay`,
   `NetVoltageLabel`), so label placement and the hover wording are unit-tested and the canvas only
   renders. This is what makes the allowed Editor → Simulation reference real.
3. **A fourth diagnostic idiom**: `SimulationDiagnostic`, `SimulationDiagnosticCode`,
   `SimulationSeverity`, beside `ContentDiagnostic`, `ConnectivityDiagnostic`, `EditError` and
   `SchematicDiagnostic`. A connectivity error is wrapped with its message reused verbatim.
4. **One code for ratings**, `RatingExceeded`, with the rating named in the message, so the canvas
   can ask `HasRatingWarning(id)` once instead of matching three codes.
5. **Support is a definition-id to model map** (`DcModels`). A definition outside it, or one whose
   expected pin names are missing, is refused as unsupported, so a new content file is unsupported
   until it gets a model rather than crashing the solver.
6. **A missing required parameter falls back to the catalog default**; today's netlists always carry
   them.
7. **`OperatingPoint` carries more than the spec's three items**: `IsSolved`, `Errors`, `Warnings`,
   `RatingWarnings`, `UndefinedNets`, `HasRatingWarning`, `IsUndefined`, the `TryGet` pair and
   `ToDebugText()`, the last one because the acceptance criteria want the headless run to print the
   solution.
8. **The `NoDcPath` message is kept verbatim** from the spec, including "(shown as 0 V)", although
   ruling 2 makes the overlay read `undefined`. The dictated text and the overlay now disagree;
   say the word and I will reword the message.
9. **Singular causes** come from a BFS over zero-resistance branches: the message distinguishes
   parallel sources, a loop, and a component short-circuited onto one net, names the members ordered
   by component id, and adapts the noun ("ideal voltage sources", "ideal inductors",
   "zero-resistance branches").
10. **Singularity threshold**: a pivot at or below `1e-14 × max|A|`, non-finite factors, or a
    non-finite solution all report `Singular` rather than throwing, which keeps a bad circuit from
    crashing the UI.
11. **A `0 Ω` resistance stamps as a `0 V` branch** wherever it comes from, including a resistor's
    own `resistance`, not only the parasitics of ruling 6.
12. **`DC: solved` / `DC: not run` sits after the warning count** in the status bar, and the hover
    line is appended last.

## Manual checks for the project lead

Run `& $env:GODOT --path game`.

- [ ] **Close button.** Place a resistor, then close the window with the X: the Save / Discard /
      Cancel dialog appears. Cancel keeps the window open, Discard quits, Save writes first. With no
      unsaved changes the X quits straight away.
- [ ] **Repair dialog.** Save a schematic, edit the file by hand to `"resistance": 1234`, add
      `"colour": "red"` to that component and an unknown parameter, then reopen: the dialog reads
      `R1: resistance 1.23 kΩ is not in E24; reset to 1 kΩ.` and `R1: unknown field 'colour' was
      ignored.`. Break a brace instead and the errors still show `$.`-paths.
- [ ] **Demo circuit, F9.** Simulate › DC operating point (F9): `5 V` pills on both nets, nothing on
      the ground net, status `DC: solved`. Hover the resistor: `R1: 0 V, 0 A, 0 W absorbed`. Hover
      the source: `V1: 5 V, 0 A, 0 W absorbed` (no current flows, so nothing is delivered).
- [ ] **Hand-checkable divider.** 10 V source, 1 kΩ then 2.2 kΩ to ground. F9 shows `10 V` and
      `6.88 V`; the resistors read `3.13 mA` with `9.77 mW` and `21.5 mW` absorbed; the source reads
      `3.13 mA`, `31.3 mW` **delivered**. Absorbed powers add up to the delivered one.
- [ ] **Sign convention.** Replace the source with a 1 mA current source into a single 1 kΩ: the
      node reads `1 V`, the resistor `1 mA, 1 mW absorbed`, the source `1 mA, 1 mW delivered`.
- [ ] **Capacitor and inductor.** A capacitor in series with a resistor: no current, the full supply
      across the capacitor. Swap it for an inductor: the inductor reads `0 V` and the full `V/R`.
- [ ] **Floating node.** Leave the far pin of a capacitor unconnected: F9 still solves and that
      net's pill reads `undefined`. The status warning count tracks connectivity warnings only, so
      the `NoDcPath` warning shows up in the solution, not there.
- [ ] **Rating warning.** Put a 1 kΩ resistor across a 10 V source and give it a 250 mW power
      rating: it dissipates 100 mW, so F9 stays silent. Raise the supply to 20 V (400 mW): the
      symbol turns red and the dialog reads `R1 dissipates 400 mW, above its 250 mW power rating.`
- [ ] **Refusals.** Delete the ground: F9 opens a dialog with
      `The circuit has no net named 0, so the DC analysis has no voltage reference.` and no overlay.
      Place a diode: `D1 (Diode) is not supported by the simulator yet.`. Put two 5 V sources in
      parallel: the dialog names both as ideal voltage sources in parallel.
- [ ] **Stale results.** After a successful F9, move a component: the pills disappear and the status
      reads `DC: not run`. Ctrl+S alone does **not** clear them. File › New and File › Open clear
      them.
- [ ] **Shortcuts.** Help › Keyboard shortcuts lists `DC operating point   F9`, and the Simulate
      menu shows the same key.

## Proposals for later specs

- Show the hovered net's voltage in the status bar as well; it was out of this spec's scope.
- A display threshold or a `0 A` shortcut for currents below, say, 1 pA, once nonlinear devices make
  gmin currents visible again.
- `ComponentOperatingPoint` could carry the pin currents once multi-terminal devices arrive.
- An ideal current source driving a node with no DC path solves at `I / gmin` (1 mA gives 1 GV). The
  net is reported as `NoDcPath` and the overlay says `undefined`, but `NetVoltages` still holds the
  huge number; clamping such nets to 0 V may read better.
- A sparse solver, matrix reuse between runs, and running the analysis on a worker thread, all
  explicitly out of scope here.
- `ConnectivityResult` could expose the junctions per net, which would let the overlay drop its
  point-on-wire scan.

## Open questions

Only deviation 8: the `NoDcPath` message still says "(shown as 0 V)" while the overlay now says
`undefined`. I kept the dictated text rather than editing a spec message on my own initiative.
