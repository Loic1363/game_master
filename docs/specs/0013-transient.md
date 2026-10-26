# Task 0013 — Transient analysis and source functions

## Mode

Stay in the current mode. **First present your implementation plan** (elaboration layer, integration method, timestep control and breakpoints, the waveform data model and text syntax, persistence, the inline label editor, the ngspice transient harness and tolerances, tests), **then stop and wait** for approval.

## Context

Report 0012 is accepted. This task adds time-domain (transient) analysis and time-varying source functions in the spirit of LTspice, with inline text editing of values on the schematic. The waveform viewer is task 0014, transistors are task 0015. All CLAUDE.md rules apply.

## Part A — Follow-ups from report 0012

1. **Structural cause before non-convergence.** Before reporting `NonConvergence`, run the structural checks (voltage-branch cycles, and a diode connected directly across an ideal voltage source) and report the structural cause instead when one is found. For the diode: `D1 is connected directly across the ideal source V1; its current would be unbounded. Add a series resistance.`
2. **Ground check on export.** The SPICE export refuses a circuit without a net `0`, using the existing `NoGround` message.

## Part B — Elaboration layer

- Introduce one elaboration step in `Nodal.Simulation`. It turns a netlist into **primitive elements** (R, C, L, D, V, I, each with optional time function), with parasitics expanded:
  - series parasitics get internal nodes named `<designator>_int` (for example `<designator>_int1`, `<designator>_int2` when several are needed);
  - parallel parasitics are plain elements across the same nets.
- The DC analysis, the new transient analysis and the SPICE exporter all consume this one elaborated form, so they can never disagree. All existing DC and reference tests stay green.
- **Every parasitic is now elaborated:**
  - capacitor `esr` (series R), `esl` (series L), `parallel-resistance`, `parallel-capacitance`;
  - inductor `series-resistance`, `parallel-capacitance`;
  - voltage-source `series-resistance`;
  - current-source `parallel-resistance`.
- **Internal nodes stay internal:** they never appear in `NetVoltages`, the overlay or the UI. They may appear in debug text.

## Part C — Source functions

**Definitions.** The voltage source and the current source gain an optional basic parameter named `function`, label `Function`, of a new parameter kind `waveform`, with the source's amplitude unit (volt or ampere).

- Parameter `kind` becomes an optional schema field: `"quantity"`, the default, or `"waveform"`.
- A waveform parameter has no default, no series and no bounds.
- The schema version stays 1. This is pre-release.

**Model.** Parameter values become a small closed union of a quantity value or a waveform value. Commands, `ParameterForm`, persistence, connectivity, the netlist and the exporter adapt.

- `SetParameter` accepts the waveform kind.
- A unit or kind mismatch is an error.

**Functions,** with exact ngspice semantics (cite the formulas in the code's tests, not in comments):

| Function | Arguments (times in s, amplitudes in the source unit) |
|---|---|
| PULSE | V1 V2 Tdelay Trise Tfall Ton Tperiod [Ncycles] |
| SINE (also accept SIN) | Voffset Vamp Freq [Tdelay [Theta [Phi [Ncycles]]]], with Theta in 1/s and Phi in degrees |
| EXP | V1 V2 Td1 Tau1 Td2 Tau2 |
| PWL | t1 v1 t2 v2 …, at least one pair, strictly increasing times. It holds the last value after the last point, and the first value before the first point. |

Validation, as errors: negative times or durations, Tperiod smaller than Trise + Ton + Tfall, a non-positive Tau, and Ncycles that is not a positive integer.

**DC behavior:** when a source has a function, the DC analysis uses the function's value at t = 0, as ngspice does when no DC value is given. Without a function, the existing DC value is used.

**Text syntax:** `PULSE(0 12 0 1n 1n 20m 40m)`.

- Numbers use the game's quantity notation (ADR-0008: case-sensitive prefixes, `meg`, RKM, comma or dot), without unit symbols.
- Separators are spaces; commas outside numbers are not allowed.
- Function names are case-insensitive.
- Formatting writes the shortest form with SI prefixes and up to 6 significant digits, for example `PULSE(0 12 0 1n 1n 20m 40m)`.

**Persistence:** waveforms are saved as structured JSON with exact numbers, never as text (ADR-0017). For example:

```json
"function": { "pulse": { "v1": 0, "v2": 12, "delay": 0, "rise": 1e-9, "fall": 1e-9, "on": 0.02, "period": 0.04 } }
```

**SPICE export:**

- Write the function in ngspice syntax with plain numbers.
- If a function feature has no ngspice equivalent in ngspice-47 (check `Ncycles` on PULSE and SINE), refuse the export with a clear diagnostic rather than writing something different.
- Now that the transient uses them, export every parasitic.

## Part D — Transient analysis

**Options (`TransientOptions`):**

- `StopTime`, required.
- `OutputStep`: defaults to StopTime / 1000.
- `MaxStep`: defaults to min(OutputStep, StopTime / 50), as SPICE.
- `RelTol`, `VnTol`, `AbsTol`, `ChgTol = 1e-14`, `TrTol = 7`: SPICE defaults.
- `StartFromZero = false`:
  - false: start from the DC operating point at t = 0;
  - true: all capacitor voltages and inductor currents start at zero, with sources at their t = 0 value, like SPICE's UIC.

**Method:**

- **Integration:** trapezoidal, with backward Euler on the first step and right after each breakpoint.
- **Timestep control:** local truncation error estimated from the charge and flux history, as SPICE does.
- **On Newton failure:** halve the step and retry. Below a minimum step, fail with `TimestepTooSmall`: `The transient analysis failed at t = 1.23 ms: the timestep became too small.`
- **Breakpoints** at every waveform corner: PULSE edges, PWL points, EXP and SINE delays. The integration lands exactly on them.
- Diodes use the DC model, with no junction capacitance, per the simplified physics.

**Result (`TransientResult`):**

- the accepted time points;
- the voltage of every net (internal nodes excluded);
- the current of every two-terminal component, with the passive sign convention;
- interpolation helpers: `VoltageAt(net, t)`, `CurrentAt(component, t)`;
- diagnostics and statistics (accepted and rejected steps, Newton iterations).

**Threading:**

- The analysis is a pure function, cancellable through a `CancellationToken`, with progress reporting (simulated time / StopTime).
- The game runs it on a background task:
  - the status bar shows `Transient: 37 %`;
  - Escape or Simulate › Stop cancels it;
  - results are marshalled back to the main thread.
- The schematic snapshot used is immutable, so editing during a run is safe. A result whose snapshot is no longer current is discarded.

## Part E — Analysis settings in the file

- A schematic file may hold an optional `analysis` object, for example `{ "transient": { "stopTime": 0.01, "outputStep": 1e-5, "maxStep": null, "startFromZero": false } }`.
- Reading validates it. Invalid values are warnings: the settings are dropped.
- Older builds already ignore unknown fields with a warning, so the schema version stays 1.
- `EditHistory` is unaffected. Changing analysis settings marks the document dirty, through a small command or an equivalent mechanism that you propose in the plan.

## Part F — UI

**Simulate › Transient…** (shortcut F10, in the shortcut table) opens a dialog built through the style layer:

- fields: stop time (required), output step, max step (both optional), a "Start from zero" checkbox;
- the fields use quantity notation in seconds, with live validation as in the properties dialog;
- Run starts the analysis; the settings are saved with the document.

**After a successful run:**

- the voltage pills show the values at the stop time;
- hovering a component shows its current at the stop time;
- the status bar reads `Transient: solved to 10 ms (412 points)`.
- The waveform viewer is task 0014. The result object must already hold everything it will need.

**Inline value editing, as LTspice:**

- The value label of a component shows its function text when set (`PULSE(0 12 0 1n 1n 20m 40m)`); otherwise it shows the first required parameter's value, as today.
- Double-click on a value label opens an inline text field over it, styled by the style layer. It edits the function, or the first required parameter.
- Enter applies a `SetParameter` command (one undo entry). Escape cancels.
- Invalid text keeps the field open with the error shown in the status bar, and applies nothing.
- A double-click on the symbol body still opens the properties dialog.

**Properties dialog:**

- The `function` parameter appears as a text field with the same syntax and live validation.
- A small dropdown next to it (None, PULSE, SINE, EXP, PWL) inserts a valid template, for example `PULSE(0 5 0 1u 1u 0.5m 1m)`.
- Per-function input fields like LTspice's are a later polish.

## Part G — References and tests

**ngspice transient references.** Extend the harness:

- run `.tran` with tight options;
- resample with ngspice `linearize` onto the output step;
- commit `expected.json` time series for the nets and element currents.

Your plan must propose and justify the comparison tolerances, absolute and relative to the signal range. Never loosen them silently; report any disagreement with analysis.

**Reference circuits:**

- an RC charge from a PULSE step: 5 V, 1 kΩ, 100 nF, starting from zero;
- an RC discharge from a DC operating point, using a PULSE falling edge;
- an RL step;
- an underdamped series RLC: 10 Ω, 1 mH, 1 µF;
- a half-wave rectifier: SINE 10 V at 50 Hz, diode, 1 kΩ load, with a 100 µF smoothing capacitor;
- a PWM into an RC low-pass;
- an EXP source into a resistor;
- a PWL source into an RC;
- a capacitor with ESR and ESL driven by a PULSE.

**Additional tests:**

- Analytical RC and RL step responses within 1e-4 relative, using a small MaxStep.
- Every function's value at chosen times, against the ngspice formulas, including delays, Theta, Phi and Ncycles.
- The text parser and formatter: round-trip, every validation error, the quantity notation inside.
- Waveform persistence round-trip: exact numbers, and invalid structures repaired or refused per ADR-0017.
- Breakpoints hit exactly.
- Cancellation and progress reporting.
- `TimestepTooSmall` on a pathological case through options.
- The elaboration layer: every parasitic expansion, internal node naming, and internal nodes hidden from results.
- The existing DC references unchanged.

**Headless run:** after the existing prints, run a transient on the demo circuit with `StartFromZero = true` and `StopTime = 500 µs`, and print V(N2) at 100 µs (expected 5 × (1 − e⁻¹) ≈ 3.16 V) and at 500 µs.

## Part H — ADR to record verbatim

Create `docs/decisions/ADR-0020-transient-and-source-functions.md` with exactly:

--- BEGIN ADR-0020 ---
# ADR-0020: Transient analysis, source functions and elaboration

Status: Accepted

## Context
Players must see circuits evolve in time, as in LTspice: capacitors charging, PWM signals, rectifiers. Parasitic parameters become meaningful in the time domain, and several analyses must interpret the circuit identically.

## Decision
A single elaboration step turns the netlist into primitive elements with parasitics expanded, using hidden internal nodes for series parasitics; the DC analysis, the transient analysis and the SPICE exporter all consume it. The transient analysis uses trapezoidal integration with backward Euler after breakpoints, SPICE-style truncation-error timestep control, step halving on Newton failure, breakpoints at waveform corners, and starts from the DC operating point unless asked to start from zero. Voltage and current sources accept PULSE, SINE, EXP and PWL functions with ngspice semantics, written in a text syntax that uses the game's quantity notation and saved as structured exact numbers. Values can be edited inline on the schematic. Transient analyses run on a background task with progress and cancellation. Analysis settings are saved in the schematic file.

## Consequences
Every new parasitic or device must be added to the elaboration step only once. Transient results are verified against ngspice time series with justified tolerances. Waveform features without an ngspice equivalent refuse the SPICE export rather than exporting something different.
--- END ADR-0020 ---

## Acceptance criteria

- [ ] Plan presented and approved before implementation.
- [ ] `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all pass, with the count reported, including every DC and transient reference.
- [ ] The headless run prints the demo transient values.
- [ ] No comments in any hand-written file. No Git command was run. No new package.
- [ ] Every new visual element goes through the style layer (rule 11).
- [ ] Spec saved at `docs/specs/0013-transient.md`; report at `docs/reports/0013-transient.md`, with a manual checklist and a table of reference agreement.

## Out of scope

The waveform viewer and probes (task 0014), transistors, AC (frequency-domain) analysis, SFFM, PWL files, per-function input fields in the dialog, migrating old inline styles.

## Rulings after the plan

1. New `UnboundedCurrent` code for the diode across an ideal source.
2. Deck naming: keep existing names, suffixes only for the new extras (`_esr`, `_esl`, `_par`).
3. Analysis settings on `Schematic` through an undoable `SetAnalysisSettings` command.
4. PWL JSON shape `{ "pwl": { "points": [[t, v], ...] } }`.
5. `ParameterKind` in Foundation beside `ParameterValue`.
6. Tolerance `max(absoluteFloor, 1e-4 x range)` with 1 µV / 1 nA floors; per-circuit exceptions only recorded in `expected-transient.json` with their justification in the report.
7. Transient `MaxIterations = 10` and `MinStep = StopTime x 1e-9`.

Two additional instructions:

A. Timestep control: implement the local truncation error estimate exactly as ngspice does (its `CKTterr` logic: the divided differences used, the trapezoidal and backward Euler error constants, and the root taken for each integration order), rather than the simplified cube-root formula of the plan. Verify it against ngspice's behavior on at least one reference circuit by comparing accepted step counts, and report them. Only the results are asserted in tests.

B. Phase checkpoints: implement in the phase order. At the end of each phase, run the full build and test suite, and update a short progress file `docs/reports/0013-progress.md` stating which phases are complete, the current test count, and what comes next. If the session is interrupted, it must be possible to resume from that file alone. Fold its content into the final report and delete it at the end.
