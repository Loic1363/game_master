# Report 0013 — Transient analysis and source functions

Status: Done. Build, tests, headless run and the ngspice reference harness all pass.

## Results

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**.
- `dotnet test Nodal.sln`: **1795 passed, 0 failed** (Foundation 372, Content 192, Circuit 562,
  Simulation 444, Editor 159, Architecture 66). Baseline before the task: 1303.
- Headless Godot run (`--headless --quit`), exit 0, printing after the existing netlist and DC
  lines: `solved to 500 µs in 1004 points`, then `V(N2) at 100 µs = 3.16 V` and
  `V(N2) at 500 µs = 4.97 V`. The expected value is 5 × (1 − e⁻¹) = 3.1606 V.
- ADR-0020 recorded verbatim at `docs/decisions/ADR-0020-transient-and-source-functions.md`.
- No comments in any hand-written file, no Git command was run, no new package.

## What was built

**Part A.** `UnboundedCurrent` diagnostic and the `DiodeAcrossIdealSource` structural check, run
before `NonConvergence` (order: unbounded current, voltage-branch cycle, non-convergence); the
SPICE export refuses a circuit without net `0`.

**Part B.** `Nodal.Simulation.Elaboration` turns a netlist into primitives with every parasitic
expanded (capacitor `esr`, `esl`, `parallel-resistance`, `parallel-capacitance`; inductor
`series-resistance`, `parallel-capacitance`; voltage-source `series-resistance`; current-source
`parallel-resistance`). Series parasitics use hidden internal nodes `<designator>_int`, or
`_int1`, `_int2` when several are needed. The DC analysis, the transient analysis and the SPICE
exporter all consume it. `DcModels`, `DcParameters` and `DcRatings` are gone.

**Part C.** `Nodal.Foundation.Waveforms` holds the closed `Waveform` union (`PULSE`, `SINE`/`SIN`,
`EXP`, `PWL`) with ngspice semantics, the text syntax using the game's quantity notation, and the
shortest-form formatter. `ParameterValue` is the quantity-or-waveform union; `SetParameter`
reports `KindMismatch` and `InvalidWaveform`. Waveforms persist as structured exact numbers.

**Part D.** `Nodal.Simulation.Transient`: trapezoidal integration with backward Euler on the first
step and after each breakpoint, ngspice `CKTterr` truncation-error control, step halving on Newton
failure, breakpoints at every waveform corner landed on exactly, and a `TransientResult` carrying
the accepted time points, every external net's voltage, every component's voltage and current,
interpolation helpers, diagnostics and statistics. The analysis is a pure function with progress
reporting and cancellation.

**Part E.** `Schematic.Analysis` (`AnalysisSettings` with `TransientSettings`), the undoable
`SetAnalysisSettings` command, and reader and writer support. Invalid settings in a file are
warnings and the settings are dropped; the schema version stays 1.

**Part F.** `Simulate › Transient…` on **F10** (in the shortcut table) and `Simulate › Stop`
(disabled unless a run is in flight). The dialog is built through the new `DialogStyle`; the
fields use quantity notation in seconds with live validation. Runs happen on a background task:
the status bar shows `Transient: 37 %`, then `Transient: solved to 10 ms (412 points)`; Escape or
`Simulate › Stop` cancels; a result whose schematic is no longer current is discarded. The voltage
pills and the hover text read the stop-time values through `SolutionSnapshot`. Value labels show
the function text when set, and a double-click opens an inline field styled by `InlineEditStyle`
(Enter applies one undoable command, Escape cancels, invalid text keeps the field open with the
error in the status bar). The properties dialog's `function` row gained a template dropdown.

**Part G.** The harness writes `probes.json` beside each deck, runs the operating point as before
and, for folders carrying transient settings, runs `.tran` with the tight options, `linearize` and
`wrdata`, committing `expected-transient.json`. Nine transient reference circuits were added.

## Reference agreement

Both engines run the settings stored in each `circuit.nodal`; ngspice adds
`reltol=1e-6 vntol=1e-12 abstol=1e-15 chgtol=1e-16 trtol=1 method=trap` and the same `maxStep`,
Nodal the same tolerances. Tolerance, as ruled: `max(floor, 1e-4 × range)` over each series, with
floors of 1 µV and 1 nA. `range` is the reference series' own max − min. **No per-circuit
exception was needed**; the worst sample of each circuit is listed as a fraction of what the rule
allows.

| circuit | points | Nodal steps | worst ΔV / allowed | worst ΔI / allowed |
|---|---|---|---|---|
| transient-rc-step | 200 | 5028 | 0.5 µV / 484 µV = 0.1 % | 0.5 nA / 484 nA = 0.1 % |
| transient-rc-discharge | 201 | 5042 | 1.7 µV / 497 µV = 0.3 % | 1.7 nA / 948 nA = 0.2 % |
| transient-rl-step | 200 | 5027 | 2.9 µV / 484 µV = 0.6 % | 29 nA / 4.8 µA = 0.6 % |
| transient-rlc-ringing | 200 | 5088 | 0.9 µV / 159 µV = 0.6 % | 16 nA / 4.1 µA = 0.4 % |
| transient-half-wave-rectifier | 201 | 21376 | 1.4 µV / 927 µV = 0.2 % | 0.25 µA / 31 µA = 0.8 % |
| transient-pwm-low-pass | 200 | 5174 | 2.4 µV / 116 µV = 2.1 % | 2.4 nA / 616 nA = 0.4 % |
| transient-exp-source | 201 | 60003 | 200 µV / 500 µV = 40 % | 0.2 µA / 0.5 µA = 40 % |
| transient-pwl-source | 201 | 6053 | 1.5 µV / 598 µV = 0.3 % | 1.5 nA / 514 nA = 0.3 % |
| transient-capacitor-esr-esl | 200 | 4296 | 21 µV / 105 µV = 20 % | 21 µA / 105 µA = 20 % |

The two larger residuals are both reference-side artefacts of the mandated `linearize` resampling,
measured and bounded rather than tolerated blindly:

- **transient-exp-source (40 %).** ngspice adds no breakpoint at an `EXP` delay, so `linearize`
  interpolates straight across the slope discontinuity at `Td2`. Driving ngspice's `maxstep` down
  shows the reference converging on Nodal's exact value: at 5e-7 it reads 4.99344 V, at 5e-8
  4.99524 V, at 5e-9 4.99542 V, against the closed-form 5 × (1 − e⁻⁷) = 4.9954406 V, which Nodal
  reproduces to 10 digits. The error is proportional to `maxstep`, so the circuit's stored
  `maxStep` was set to 5e-8, leaving the residual at 40 % of the allowance.
- **transient-capacitor-esr-esl (20 %).** Fast LC ringing behind an ESR and ESL; both engines
  converge to the same answer and the residual is the remaining resampling error at the stored
  `maxStep` of 5e-9.

One reference sample is deliberately not committed: for a `startFromZero` run the harness drops the
`t = 0` row, because ngspice does not compute device currents at its UIC initial point. Its own
output proves it — for `transient-capacitor-esr-esl` ngspice reports I(R1) = −5.0 µA but
I(C1) = −256 µA at `t = 0` for two elements in series, and the two agree exactly from the next
sample on. Nodal's `t = 0` behaviour is pinned by the analytical tests instead (inrush current
through ESR, zero current through ESL).

### Timestep control against ngspice (instruction A)

`CKTterr` is implemented as ngspice specifies: the divided differences of the charge (for a
capacitor) or flux (for an inductor) taken to order + 1 using the stored step history, the
tolerance `max(abstol + reltol·max(|flow_n|, |flow_n−1|), reltol·max(|charge|, chgtol)/h)`, and
`del = trtol·tol/max(abstol, |diff[0]|)` followed by the order-th root — no root at order 1,
a square root at order 2. `CKTtrunc`'s `min(2·h, …)` growth cap is applied, and a step is accepted
when the limit exceeds 0.9 × the step taken.

**No ngspice sources are installed here, only the ngspice-47 binary**, so the formula was
reconstructed from its published algorithm, checked dimensionally (both orders must yield seconds,
which fixes the shape of the expression) and then verified against ngspice's behaviour by
comparing accepted step counts:

| circuit | options | ngspice points | Nodal points |
|---|---|---|---|
| RC, 1 kΩ, 100 nF, 500 µs | SPICE defaults | 1011 | 1004 |
| RLC, 10 Ω, 1 mH, 1 µF, 1 ms | SPICE defaults | 115 | 119 |
| RLC, 10 Ω, 1 mH, 1 µF, 1 ms | tight | 923 | 2027 |

At default tolerances the two agree within 4 %. At tight tolerances Nodal takes about 2.2× more
steps, so it is the more conservative of the two; the results still agree to well under 1e-6 of the
signal range. Only the results are asserted in tests, as instructed.

## Deviations and choices the spec did not cover

1. **Shared MNA layer.** The plan moved the stamping helpers into one internal `MnaStamps`. Two
   neighbours moved with them because the transient needed exactly the same logic: `MnaUnknowns`
   (node and branch indexing, with the branch flags passed in by the caller) and `MnaPaths`
   (nets without a conducting path to ground, now taking the conducting pairs rather than DC
   elements). `DcUnknowns` and `DcPaths` are gone, `DcSystem` is a stateless builder, and all DC
   results are unchanged — the 13 committed DC `expected.json` files regenerate byte for byte.
2. **Probe plan without signs.** The plan said `probes.json` would carry per-element signs. Every
   sign is `+1`, because the exporter already negates a current source's value so that ngspice's
   reported current matches the passive-sign convention, so the field was dropped and the harness
   sums the named elements directly. `SpiceProbes.TryPlan` is public in the simulation layer and
   `Nodal.Cli` writes the JSON.
3. **Truncation error and the integration order.** The error estimate must use the order of the
   step actually taken, and it needs `order + 1` real history generations, so it is skipped on the
   first step (as SPICE does) and on the second (where only the order-1 history exists but the
   step is already trapezoidal). Estimating at order 1 while integrating at order 2 was tried
   first and is wrong: near `t = 0` the order-1 criterion is far stricter and the step collapses.
4. **A rejected step is floored at `MinStep`, not failed there.** The charge term of the tolerance
   is divided by `h`, so the error limit *rises* as the step shrinks; rejecting straight to the
   first estimate and failing when that single estimate falls under `MinStep` refused circuits
   ngspice solves (the RLC and the ESR/ESL references). A rejected step is now reduced towards the
   limit but never below `MinStep`, and a step already at `MinStep` is accepted.
   `TimestepTooSmall` still arises, and is still tested, when a breakpoint lands closer than
   `MinStep` and when Newton halves below it.
5. **The first step is limited by the first breakpoint.** `StepAfterBreakpoint` (a tenth of the
   interval to the next breakpoint) now also applies at `t = 0`. Without it a 10 ns pulse ramp was
   crossed in a single backward-Euler step, which put the PWM reference 2.4e-4 V off the
   closed-form answer; ngspice resolves such a ramp in about ten steps.
6. **Consecutive-rejection cap.** A step that is rejected 64 times in a row without advancing
   fails with `TimestepTooSmall`. This guards the one case where the breakpoint-landing rule could
   otherwise re-expand a just-shrunk step indefinitely.
7. **A failed run returns no partial series.** `TransientResult.Refused` carries the diagnostic
   only. The spec does not say what to keep, so nothing is kept; the waveform viewer may want the
   accepted prefix instead.
8. **No rating checks in the transient.** The spec asks for them only in the DC analysis, so
   `SolutionSnapshot` reports no rating warning for a transient result and the canvas reddens
   over-rated symbols only after a DC run.
9. **Reference schematics carry their own analysis settings**, so Part E is exercised end to end:
   the harness reads the settings from `probes.json` and the tests read them from the same
   `circuit.nodal`.

## Manual checklist

This session has no display and cannot drive the window, so **none of these were observed in the
running game** — they are for the project lead. What backs each item from here is named after it.

- [ ] `Simulate › Transient…` and **F10** open the dialog; `Help › Keyboard shortcuts` lists F10.
      (The binding is in `EditorShortcuts.All`, which is also what the shortcut dialog prints.)
- [ ] Stop time is required, the Run button stays disabled until it parses, `10m`, `500u` and
      `1,5m` are all accepted, and the error text appears under the offending field.
      (`TransientFormTests`, 21 tests, cover the validation and the parsing.)
- [ ] Running the demo circuit fills the voltage pills with the stop-time values and hovering a
      component shows its current at the stop time. (`SolutionSnapshotTests` and
      `OperatingPointOverlayTests` cover the snapshot and the labels; the headless run prints the
      same numbers.)
- [ ] The status bar reads `Transient: solved to 500 µs (1004 points)` after the demo run.
      (`TransientStatusTextTests` pins the wording; the headless run prints 1004 points.)
- [ ] The settings survive save and reload, and changing them marks the document dirty and is
      undoable. (`AnalysisSettingsFileTests` and `SetAnalysisSettingsTests` cover the file and the
      command; the dirty flag follows from running the command through `EditorSession`.)
- [ ] A long run shows `Transient: 37 %`, and Escape and `Simulate › Stop` both cancel it;
      `Stop` is greyed out when nothing is running.
      (`TransientThreadingTests` covers progress and cancellation; the menu item is disabled
      through `EditorMenus.Refresh`.)
- [ ] Editing the schematic during a run discards the result instead of showing a stale overlay.
      (`TransientRun` keeps the schematic it was started with and `CollectTransient` compares it by
      reference; not covered by a test.)
- [ ] Double-clicking a value label opens the inline field; Enter applies one undo entry, Escape
      cancels, and invalid text keeps the field open with the error in the status bar.
      (`InlineValueEditorTests`, 15 tests, cover the text, the validation and the single command;
      the hit-testing of label rectangles and the Godot field itself are not covered.)
- [ ] Double-clicking the symbol body still opens the properties dialog.
      (The canvas only takes the double-click when it lands on a value label.)
- [ ] A voltage source with a function shows the function text on its value label.
      (`InlineValueEditor.LabelTextOf`, covered by tests.)
- [ ] The properties dialog's Function dropdown inserts each template and the field validates it
      live. (`WaveformText.TemplateOf` and `ParameterField` are covered; the dropdown wiring is
      not.)

The untested seams are the Godot ones: label hit-testing, the inline field's lifetime, the
dropdown wiring, and discarding a stale result.

## Open questions and suggestions for later

1. **Keep the accepted prefix of a failed run** so the waveform viewer can show where the analysis
   gave up (see deviation 7).
2. **Rating checks over time** would be more useful than at a single instant: the peak current and
   the mean power over the run are both already in `TransientResult`.
3. **A per-function input form** in the properties dialog, as the spec reserves for later polish.
4. **`SolutionSnapshot` at an arbitrary time** is already supported; a time slider on the canvas
   would be a cheap precursor to task 0014.
5. **The exported deck carries no `.tran` line.** The harness adds it from `probes.json`. If decks
   are ever meant to be run by hand, writing the stored analysis settings into the deck would be a
   small addition.
