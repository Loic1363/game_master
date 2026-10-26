# Report 0012 — Newton-Raphson, the diode, and the ngspice reference

Status: Done. Build, tests, headless run and the ngspice reference harness all pass.

## Verification

- **Step 0.** `NGSPICE` = `C:\Spice64\bin\ngspice_con.exe`. Version **ngspice-47**, "Compiled with KLU
  Direct Linear Solver", creation date Aug 11 2026.
- `dotnet build Nodal.sln --no-incremental`: **0 warnings, 0 errors**.
- `dotnet test Nodal.sln`: **1303 passed, 0 failed** (Foundation 250, Content 189, Circuit 488,
  Simulation 203, Editor 107, Architecture 66). The 203 simulation tests include 65 reference
  assertions, 5 per circuit over the 13 committed reference circuits.
- Headless Godot (`--import` then `--quit-after 10`, exit 0) prints the demo netlist and solution
  exactly as in 0011.
- No Git command was run. No new package. No comments in any hand-written file.

## Reference agreement

Nodal versus the committed ngspice results, tight `DcOptions` (`RelTol = 1e-9`, `VnTol = 1e-12`,
`AbsTol = 1e-15`). Tolerances are 1e-6 relative or 1 µV / 1 pA absolute; nothing was loosened.

| circuit | strategy | iters | max abs ΔV | max rel V | max abs ΔI | max rel I |
|---|---|---|---|---|---|---|
| divider | Direct | 1 | 8.9e-16 | 1.3e-16 | 8.7e-19 | 2.8e-16 |
| wheatstone-bridge | Direct | 1 | 1.5e-13 | 2.1e-14 | 4.9e-14 | 1.3e-12 |
| current-source-resistor | Direct | 1 | 0 | 0 | 0 | 0 |
| thevenin | Direct | 1 | 0 | 0 | 0 | 0 |
| inductor-series-resistance | Direct | 1 | 0 | 0 | 6.9e-18 | 1.7e-16 |
| capacitor-leakage | Direct | 1 | 1.8e-15 | 1.8e-16 | 1.6e-17 | 1.6e-12 |
| diode-forward | NewtonRaphson | 12 | 1.7e-14 | 2.5e-14 | 6.8e-16 | 1.6e-13 |
| diode-reverse | NewtonRaphson | 2 | 8.9e-16 | 1.8e-16 | 1.9e-21 | 3.7e-10 |
| diodes-in-series | NewtonRaphson | 12 | 4.3e-13 | 3.1e-13 | 3.6e-15 | 9.9e-13 |
| antiparallel-diodes | NewtonRaphson | 12 | 2.2e-14 | 3.1e-14 | 4.9e-15 | 9.9e-07 |
| bridge-rectifier | NewtonRaphson | 10 | 3.8e-13 | 4.4e-13 | 5.7e-15 | 3.1e-11 |
| current-source-diode | NewtonRaphson | 8 | 1.2e-14 | 1.9e-14 | 0 | 0 |
| diode-clamp | NewtonRaphson | 14 | 3.0e-13 | 7.5e-14 | 3.3e-15 | 3.2e-12 |

Ruling 1 was decisive: with ngspice's CODATA 2014 constants the diode circuits agree to ~1e-13,
where the exact post-2019 SI values measured a systematic 3.4e-7 relative (0.22 µV per junction
drop). The two large relative current figures are reverse-biased junctions whose absolute
differences are 4.9e-15 A and 1.9e-21 A, far inside the 1 pA absolute tolerance.

As an extra cross-check, the golden exporter deck (every component kind and every parasitic) was run
through ngspice-47 by hand: `n1 = 0.8247806 V`, `l1#branch = 0.4123903 A`, `v1#branch = -1.11752 A`,
against Nodal's `825 mV`, `412 mA`, `-1.12 A`.

## Deviations and choices the spec did not cover

1. **Exponent guard in the diode model.** `DiodeModel` clamps the Shockley exponent argument at 700
   (≈ 18.1 V forward). Without it a diverging Newton iterate can reach `Infinity` and a second one
   makes the convergence comparison `NaN`, which would silently read as converged. The clamp is far
   above any physical forward bias, so no operating point is affected.
2. **A diode directly across an ideal voltage source is refused** with `NonConvergence`. The junction
   conductance reaches ~1e71 S, which exceeds `LuDecomposition`'s relative pivot tolerance
   (`MaxMagnitude × 1e-14`), so the factorisation reports singular at every strategy. Previously that
   circuit was refused as an unsupported component, so no behaviour regressed, but the message is now
   about convergence rather than about an impossible circuit.
3. **A structurally singular nonlinear circuit reports `NonConvergence`, not `Singular`.** Part B
   dictates `NonConvergence` when both fallbacks fail, so a diode circuit that also has two ideal
   sources in parallel loses the structural explanation that the linear path gives. See proposals.
4. **Gmin stepping did not recover the fallback test case; source stepping did.** With
   `MaxIterations = 3` on the 5 V / 1 kΩ / diode circuit, gmin stepping fails (the first level needs
   more than three iterations) and source stepping converges, reporting 630 total iterations. The
   test accepts either strategy, as Part F words it.
5. **Three task-0011 tests changed**, because Part A.3 changed the `NoDcPath` message and removed
   undefined nets from `NetVoltages`: the message assertions in `FloatingNodeTests` and
   `SimulationDiagnosticTests`, and `TheFloatingNodeStillSolvesNearZeroVolts`, which became
   `TheFloatingNodeHasNoVoltageInTheSolution`. `RefusalTests.ADiodeIsNotSupportedYet` became
   `ADiodeIsSupportedAndNoLongerRefused`, and the two-unsupported-components test now uses
   transistors. Every other 0011 test is untouched and green.
6. **`OperatingPointOverlay` was reordered** so an undefined net still shows `undefined`. It used to
   require a voltage lookup to succeed, which now fails for undefined nets, and the pill would have
   disappeared.
7. **`DcAnalysis.Gmin` (public const) is gone**, replaced by `DcOptions.Gmin`.
8. **Capacitor `esr`, `esl` and `parallel-capacitance`, and inductor `parallel-capacitance`, are not
   exported.** The DC model ignores them too, so the deck matches what we simulate, and Part D lists
   only the four resistances.
9. **`OperatingPoint.ToDebugText` is unchanged** — no convergence line — so the headless demo output
   stays as before. The convergence text lives in `Nodal.Editor.Simulation.DcStatusText`.
10. **SPICE export does not check for a ground net.** Part D only requires refusing unsupported
    components. A circuit with no net `0` exports a deck that ngspice will reject.
11. **The harness prints node voltages one by one** (`print v(N1)`) rather than `print all`: with a
    single-node circuit ngspice labels the only vector `all` instead of the node name.
12. **Internal nodes only for series parasitics** and **`SpiceExporter.TryExport` beside `Export`**,
    both per rulings 2 and 3. `src/Nodal.Cli` is a new src project, declared in the architecture
    tests as a top-of-src tool allowed to reference Foundation, Content, Circuit and Simulation and
    no Godot.

## Remaining inline styles, for a later migration task

The style layer is now `game/scripts/Style/` (`VisualTokens`, `CanvasMetrics`, and the new
`StatusBarStyle`, which took over the two inline calls that used to be in `MainScreen._Ready`).
Still inline:

- `game/scripts/Presentation/PropertiesDialog.cs:20-23` — `LabelMinimumWidth`, `FieldMinimumWidth`,
  `DialogMinimumWidth`, `DialogMinimumHeight` metric constants, used at `:32`, `:110`, `:134`,
  `:165`.
- `game/scripts/Presentation/PropertiesDialog.cs:141` — `AddThemeColorOverride` applied inline (the
  colour itself is a style token).
- `game/scripts/Rendering/SchematicCanvas.cs:546` — ghost colour composed inline from a style token.
- `game/scripts/Rendering/CanvasText.cs:9-12` — font-size computation in the rendering layer; its
  callers are `SchematicCanvas.cs:582`, `SchematicCanvas.cs:633` and `SymbolRenderer.cs:95`.
- `game/scenes/Main.tscn:23` — `theme_override_constants/separation = 0`.
- `game/scenes/Main.tscn:35` — status-bar `custom_minimum_size = Vector2(0, 26)`.

No Godot theme resource exists yet; creating one looked like a decision for the migration task
rather than something to invent here.

## Manual checklist

- [ ] **Diode, forward.** 5 V source, 1 kΩ, diode to ground. F9 solves; the diode pill reads about
      `693 mV`, the status bar reads `DC: solved (10 iterations)`, and hovering the middle net reads
      `net N2 = 693 mV`.
- [ ] **Diode, reverse.** Mirror the diode: the middle net reads `5 V`, the diode about `-5 V`, and
      its current is about `-5 pA`.
- [ ] **Clamp.** 5 V through 1 kΩ into a diode whose cathode sits on a 3.3 V source: the middle net
      settles near `3.96 V` and the diode carries about `1.04 mA`.
- [ ] **Linear circuits still report one iteration.** The 10 V / 1 kΩ / 2.2 kΩ divider reads
      `DC: solved (1 iteration)` and `6.88 V`, exactly as in 0011.
- [ ] **Hovered net voltage.** With no solution the status bar shows `net N1`; after F9 the same
      hover shows `net N1 = 6.88 V`, and an undefined net shows `net N2 = undefined`.
- [ ] **Transistors still refused.** Place an NPN: F9 opens `Q1 (NPN transistor) is not supported by
      the simulator yet.`
- [ ] **Export and open in a real simulator.** File › Export SPICE netlist… on the diode circuit,
      save as `diode.cir`, then run `& $env:NGSPICE -b diode.cir` after appending a
      `.control op print all .endc` block, or open the file in LTspice and run an operating point.
      ngspice-47 accepts the deck as exported; the node voltages should match the pills to the digits
      shown.
- [ ] **Export refusal.** Place an NPN and export: the dialog reads
      `The SPICE export was refused` and names `Q1`.
- [ ] **Regenerating the references.** `powershell -File tools\ngspice-reference\Update-References.ps1`
      rewrites all 13 `circuit.cir` and `expected.json` files and leaves the tests green.

## Proposals for later specs

- Report the structural cause (deviation 3) when a nonlinear circuit is singular rather than
  non-convergent, by running `VoltageBranchCycles.Find` before emitting `NonConvergence`.
- Refuse the SPICE export when the circuit has no ground net, with the existing `NoGround` message.
- Make `DcOptions` reachable from the UI (a Simulate › Analysis settings… dialog) once more analyses
  exist; today the game always uses the defaults.
- Export the capacitor and inductor parasitics the DC model ignores (`esr`, `esl`,
  `parallel-capacitance`) once a transient analysis uses them.
- A display threshold for currents below about 1 pA: reverse-biased junctions now show gmin currents
  such as `-9.3 pA` in the overlay, which reads as noise to a player.
- Reuse the LU factorisation between Newton iterations, and use an absolute rather than relative
  pivot floor so very large junction conductances do not read as singular (deviation 2).
- Add reference circuits with a deliberately hard operating point once MOSFETs land, so that gmin
  stepping is exercised by a circuit that genuinely needs it rather than by a reduced iteration
  limit.

## Open questions

None. The three open items from the plan were ruled on and are implemented as ruled; ADR-0019 is
recorded with the one dictated change to its Decision section.
