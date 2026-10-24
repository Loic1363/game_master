# Task 0011 — DC operating point (linear components)

## Mode

Stay in the current mode. **First present your implementation plan** (project structure, MNA formulation and stamps, solver, gmin strategy, diagnostics, result types, sign conventions, UI integration, tests with their hand-calculated expected values), **then stop and wait** for approval.

## Context

Report 0010 is accepted. This task starts the simulator: a DC operating point analysis for linear components, shown on the schematic. Nonlinear devices come in task 0012, transient in 0013, probes and oscilloscope in 0014. All CLAUDE.md rules apply.

## Part A — Data-loss fix and readability

1. **Window close button.** Closing the window (`NOTIFICATION_WM_CLOSE_REQUEST`) goes through the same dirty-state confirmation as File › Quit: Save, Discard, Cancel. Disable Godot's automatic quit-on-close so the confirmation can run.
2. **Repair dialog.** It names components by designator (`R1: resistance 1.23 kΩ is not in E24; reset to 1 kΩ.`) instead of JSON paths, wherever a component is known. Keep the path for structural errors.

## Part B — New project `Nodal.Simulation`

- `src/Nodal.Simulation`, pure .NET, referencing `Nodal.Foundation`, `Nodal.Content` and `Nodal.Circuit`, never Godot.
- `Nodal.Editor` and the game may reference it.
- `tests/Nodal.Simulation.Tests`, set up like the other test projects.
- Extend the architecture tests:
  - Simulation's allowed references;
  - no Godot;
  - Circuit, Content and Foundation must not reference Simulation;
  - the new test project in the allow-list.
- Update CLAUDE.md's Architecture section, replacing the "Simulation, Characterization, Assembly (later)" line with:
  - `Simulation (src/Nodal.Simulation): analyses on the netlist, pure .NET.`
  - `Characterization, Assembly (later).`
- Update `docs/architecture.md`.

No new package. Write the linear algebra yourself: a dense LU decomposition with partial pivoting is enough at this stage.

## Part C — DC operating point analysis

**Input:** a `Netlist` (from `NetlistCompiler`) and the `ComponentCatalog`.

**Refusals,** returned as error diagnostics with no solution:

- any connectivity error (`GlobalNetShort`, `UnknownDefinition`);
- no net named `0` (`NoGround` becomes blocking for simulation);
- a component the simulator does not support yet (diode, NMOS, PMOS, NPN, PNP), with the message `D1 (Diode) is not supported by the simulator yet.`, one diagnostic per component.

**Formulation:** Modified Nodal Analysis.

- Unknowns: the voltage of every net except `0`, plus one branch current per voltage-source-like branch.
- **Stamps:**

| Component | DC stamp |
|---|---|
| Resistor | Conductance 1/R. |
| Capacitor | Open circuit. If `parallel-resistance` is set, a resistor of that value. All other capacitor parameters are ignored in DC. |
| Inductor | If `series-resistance` is set and positive, a resistor of that value. Otherwise a 0 V voltage branch, so its current is known. `parallel-capacitance` is ignored in DC. |
| DC voltage source | V(plus) − V(minus) = voltage. If `series-resistance` is set and positive, include it in the branch equation. |
| DC current source | Drives `current` out of its plus terminal into the external circuit. If `parallel-resistance` is set, add that resistor between its terminals. |
| Ground | Defines net `0`. |

- **Unset optional parameters are ideal** (ADR-0016).
- **gmin:** add a conductance of 1e-12 S from every non-ground node to ground, as SPICE does, so nodes without a DC path still solve.

**Diagnostics after solving:**

- **Warning `NoDcPath`:** a node with no DC-conducting path to ground, detected structurally, not numerically. Message: `N3 has no DC path to ground; its voltage is undefined (shown as 0 V).`
- **Error `Singular`:** a singular matrix even with gmin, for example a loop of ideal voltage sources or of inductors without series resistance. The message names the likely cause when a structural check can find it: voltage-source loops, parallel ideal sources.

**Sign conventions** (passive sign convention, as SPICE):

- Node voltages are relative to net `0`.
- The current of a two-terminal component is the current **entering its first pin** (`1` for passives, `plus` for sources) and leaving the second.
- Its power is V(first pin − second pin) × current. Positive means absorbed, negative means delivered.
- A source delivering energy therefore has a negative current and power. The UI words it as "delivers".
- Record that our current source convention is the opposite of SPICE's `I` element, so a future SPICE export must flip the sign.

**Result `OperatingPoint`:**

- the voltage per net name;
- per component: current and power, as `Quantity` values in ampere and watt;
- the diagnostics.

**Rating warnings,** non-blocking, checked only when the rating parameter is set:

- resistor: |power| > `power-rating`;
- capacitor: |voltage across| > `voltage-rating`;
- inductor: |current| > `current-rating`.

Messages read like `R1 dissipates 1.23 W, above its 250 mW power rating.`

## Part D — Editor and UI

- Add a **Simulate** menu (features now exist) with **DC operating point**, shortcut **F9**, in the shortcut table.
- Running it compiles the netlist from the current schematic and runs the analysis on the UI thread, which is fine for DC.

**On success:**

- Show each net's voltage as a small label on the canvas, using the label text token with a light background pill for readability. Place it at a stable point of the net: its first junction, otherwise the midpoint of its first wire, otherwise its first pin.
- Ground net labels are hidden (always 0 V).
- Hovering a component shows in the status bar its current and power, for example `R1: 4.55 mA, 20.7 mW absorbed`, or `V1: 4.55 mA, 22.7 mW delivered`.
- Components with a rating warning are drawn in the error token, and a dialog lists the warnings, using the same capped list as the load dialog.

**On refusal:** a dialog lists the errors, and nothing is overlaid.

**Stale results:** any schematic change clears the overlay. The status bar shows `DC: solved` or `DC: not run`.

## Part E — Tests

All expected values are computed by hand in the test or its name, with a relative tolerance of 1e-9. Cover at least:

- a voltage divider: 10 V, 1 kΩ and 2.2 kΩ;
- a Wheatstone bridge with a non-zero bridge current;
- a current source into a resistor, checking the sign convention;
- a source with series resistance feeding a load (Thevenin);
- superposition: two sources, compared with the sum of the single-source solutions;
- an inductor as a short (its current equals V/R), and with series resistance;
- a capacitor as an open circuit, and with parallel resistance acting as leakage;
- a floating node behind a capacitor: `NoDcPath` warning, voltage near 0;
- two ideal voltage sources in parallel: `Singular` error with a named cause;
- refusals: no ground, an unsupported diode, a connectivity error;
- rating warnings for each of the three checks;
- power balance: total absorbed power equals total delivered within 1e-9 relative, on several circuits;
- the LU solver alone: pivoting and singular detection.

Architecture tests are updated per Part B.

## Part F — ADR to record verbatim

Create `docs/decisions/ADR-0018-dc-operating-point.md` with exactly:

--- BEGIN ADR-0018 ---
# ADR-0018: Simulation foundations and the DC operating point

Status: Accepted

## Context
The simulator is the core of the game and must be correct, explainable and comparable with SPICE. It starts with the DC operating point of linear circuits before nonlinear devices and time-domain analysis.

## Decision
Simulation lives in Nodal.Simulation, a pure .NET project consuming the netlist. Analyses use Modified Nodal Analysis with node voltages and voltage-source branch currents as unknowns, solved by a hand-written dense LU decomposition with partial pivoting. A gmin of 1e-12 S to ground on every node keeps undriven nodes solvable; such nodes are reported structurally. Unset optional parameters are ideal. Currents follow the passive sign convention of SPICE: the current enters the first pin, and power is positive when absorbed. Our DC current source drives current out of its plus terminal, the opposite of SPICE's I element, so exports must flip it. Unsupported components refuse the analysis with a clear message. Ratings produce non-blocking warnings. The DC analysis runs on the UI thread; long analyses will move to a worker thread later.

## Consequences
Linear DC results are exact up to floating-point error and are tested against hand calculations. Nonlinear devices will add Newton-Raphson iteration on top of the same matrix assembly. A sparse solver may replace the dense one when circuits grow.
--- END ADR-0018 ---

## Acceptance criteria

- [ ] Plan presented and approved before implementation.
- [ ] `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all pass, with the count reported.
- [ ] The headless Godot run also prints the demo circuit's DC solution. With the defaults (5 V, 1 kΩ, 100 nF), N1 and N2 are both at 5 V and no current flows, because the capacitor is open in DC.
- [ ] No comments in any hand-written file. No Git command was run. No new package.
- [ ] Spec saved at `docs/specs/0011-dc-operating-point.md`; report at `docs/reports/0011-dc-operating-point.md`, with a manual test checklist that includes hand-checkable circuits for the project lead.

## Out of scope

Diodes and transistors, Newton-Raphson, transient analysis, probes, the oscilloscope, sparse matrices, worker threads, SPICE export.

## Rulings given at plan approval

1. Repair wording: adopt the new wording from the implementer's table and update the test expectations.
2. gmin: apply it only to nodes detected structurally as having no DC path to ground, not to every node. Circuits with full DC paths are then solved exactly, so hand-value tests run at 1e-9 relative on any resistor values and the power balance test runs on all circuits, including the 1 kΩ / 2.2 kΩ divider. Nonlinear devices may later add junction gmin in task 0012. In the UI overlay, a `NoDcPath` net shows "undefined" instead of a number. The gmin sentence of ADR-0018 is replaced, before recording, by: "Nodes with no DC path to ground are detected structurally, reported as warnings, and receive a gmin of 1e-12 S to ground so they remain solvable; all other nodes are solved without gmin, so linear results are exact."
3. Demo printout: with ruling 2 the demo's currents are exactly zero; print the real numbers. No display threshold.
4. `DisjointSet`: do not duplicate it. Promote the existing one from `Nodal.Circuit` to a public type in `Nodal.Foundation` and use it from both projects.
5. `ComponentOperatingPoint`: add the voltage across the component (first pin minus second), as a `Quantity` in volt. Use it for the capacitor voltage-rating check and in the hover text: `R1: 4.55 V, 4.55 mA, 20.7 mW absorbed`.
6. Approved smaller choices: a 0 Ω parasitic resistance stamps as a short branch; ground components have no result entry.
