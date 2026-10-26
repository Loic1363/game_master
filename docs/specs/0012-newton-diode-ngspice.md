# Task 0012 — Newton-Raphson, the diode, and the ngspice reference

## Mode

Stay in the current mode. **First present your implementation plan** (the Newton-Raphson loop, convergence criteria, limiting, fallbacks, the diode companion model, the SPICE exporter, the ngspice harness and reference file layout, the style-layer migration, tests), **then stop and wait** for approval.

## Context

Report 0011 is accepted. This task makes the DC analysis nonlinear with Newton-Raphson, adds the diode, introduces SPICE netlist export, and builds a reference harness comparing Nodal with ngspice. MOSFETs and BJTs follow in task 0013. All CLAUDE.md rules apply.

The project lead has installed ngspice. The `NGSPICE` environment variable points to its console executable. Verify it in Step 0 and report its version.

## Part A — Housekeeping and the style layer

1. **New permanent rule.** Add to CLAUDE.md's "Permanent rules", verbatim:

   `11. All visual styling lives in the style layer: the Godot theme and the style code in game/scripts/Style/ (color tokens, metrics, fonts, and helper functions that create or style controls). Presentation and rendering code never sets colors, fonts, sizes or theme overrides inline; it calls the style layer. Changing the look of the game must only require editing the style layer.`

2. **Apply it now.**
   - Move `VisualTokens` and `CanvasMetrics` into `game/scripts/Style/`.
   - Every visual element added in this task goes through the style layer.
   - Do not migrate older menus and dialogs yet, but **list in the report every remaining inline style** (file and line), for a later migration task.

3. **NoDcPath message:** `N3 has no DC path to ground; its voltage is undefined.` Undefined nets are absent from `NetVoltages`; `IsUndefined` stays.

4. **Status bar:** when hovering a net after a successful analysis, also show its voltage (`N1 = 6.88 V`).

## Part B — Newton-Raphson DC analysis

**Options.** Add `DcOptions`, with SPICE defaults:

- `RelTol = 1e-3`
- `VnTol = 1e-6 V`
- `AbsTol = 1e-12 A`
- `MaxIterations = 100`
- `Gmin = 1e-12 S`

`DcAnalysis.Solve` takes optional options. The game uses the defaults.

**Loop:**

- Each iteration linearizes the nonlinear devices into companion models (conductance plus current source), assembles the MNA system with the linear stamps of task 0011, solves it, and applies limiting.
- Convergence is reached when, for every node, |Δv| ≤ RelTol × max(|v_new|, |v_old|) + VnTol, and every nonlinear device's current converges with the same rule using AbsTol.
- A circuit without nonlinear devices must still solve in a single linear solve. All task 0011 tests stay green unchanged.

**Junction voltage limiting:** use SPICE's `pnjlim` algorithm, with the critical voltage Vcrit = n·Vt·ln(n·Vt / (√2 · Is)).

**Fallbacks** when plain Newton-Raphson fails:

1. **Gmin stepping:** an extra conductance from every node to ground, starting at 1e-2 S and divided by 10 at each successful step down to zero, each step reusing the previous solution.
2. **Source stepping:** all independent sources scaled from 0 to 100 % in steps, halving the step on failure.

If both fail: **error `NonConvergence`**, `The DC analysis did not converge after N iterations. The circuit may have no stable operating point, or a value may be unrealistic.`

**Diagnostics:** record which strategy succeeded and the total iteration count on `OperatingPoint` (for example `DC: solved (12 iterations)`), and show that text in the status bar.

**Junction gmin, as SPICE:** a 1e-12 S conductance in parallel with every pn junction. It only affects nonlinear devices; linear results stay exact.

## Part C — The diode

**Model:** Shockley, with parameters fixed in code and not exposed to the player (ADR-0019):

- Is = 1e-14 A
- n = 1
- T = 300.15 K, so Vt = k·T/q with exact CODATA constants
- no series resistance, no breakdown

**Results:** voltage across (anode minus cathode), current entering the anode, power, using the existing `ComponentOperatingPoint`.

The diode is now supported. Remove it from the unsupported refusals; the transistors stay refused until task 0013.

## Part D — SPICE export (`Nodal.Simulation.Spice`)

`SpiceExporter.Export(Netlist, ComponentCatalog) -> string` produces a deterministic, SPICE3-compatible deck that ngspice and LTspice can read:

- **Header:** a title line `* Nodal <version> export`.
- **Elements:**
  - one element per component, prefixed by type (R, C, L, D, V, I) and named from the designator;
  - nodes are our net names, with `0` as ground;
  - values are plain numbers in SI units.
- **Current sources:** the sign is flipped, so the SPICE element drives current the same way ours does (ADR-0018).
- **Parasitics:** expanded as extra elements, using internal nodes named `<designator>_int`:
  - a voltage-source series resistance becomes a resistor;
  - an inductor series resistance becomes a resistor;
  - a capacitor parallel resistance becomes a resistor;
  - a current-source parallel resistance becomes a resistor.
  - Ratings are not exported.
- **Model line:** `.model NODAL_D D(IS=1e-14 N=1)`. Use `.options TNOM=27` and make sure the simulation runs at 27 °C.
- **No analysis commands;** a final `.end`.
- Components the simulator does not support yet are refused with diagnostics.

**Godot:** add File › Export SPICE netlist… (no shortcut), writing a `.cir` file through the existing file adapters. This is an educational bridge to real tools, allowed by ADR-0015.

## Part E — ngspice reference harness

- **`tests/reference-circuits/`:** one folder per circuit, each containing:
  - `circuit.nodal`, the source of truth;
  - `circuit.cir`, generated by the exporter;
  - `expected.json`, generated by ngspice, holding node voltages and the current of every element.
- **A script in `tools/ngspice-reference/`** (PowerShell, no new dependency):
  - regenerates `circuit.cir` and `expected.json` for every folder;
  - exports through a small CLI entry point (`src/Nodal.Cli`, pure .NET, which is allowed in the architecture tests) or through a test-time export, your choice in the plan;
  - runs ngspice in batch mode with tight options: `reltol=1e-9 vntol=1e-12 abstol=1e-15`;
  - parses its operating point output.
- **Tests:**
  - load each `circuit.nodal`;
  - solve with tight `DcOptions` (`RelTol = 1e-9`, `VnTol = 1e-12`, `AbsTol = 1e-15`);
  - compare node voltages within 1e-6 relative or 1 µV absolute, and currents within 1e-6 relative or 1 pA absolute;
  - check that `circuit.cir` matches a fresh export (catches exporter drift).
  - The tests never call ngspice: they use the committed files.
- **If a reference disagrees beyond tolerance,** do not loosen the tolerance silently. Report the circuit, the values and your analysis of the cause.

**Reference circuits:**

- **Linear** (cross-checking task 0011): the 10 V divider with 1 kΩ and 2.2 kΩ; the Wheatstone bridge; a current source into a resistor; Thevenin; an inductor with series resistance; capacitor leakage.
- **Diode:**
  - forward biased: 5 V, 1 kΩ, diode;
  - reverse biased;
  - two diodes in series;
  - antiparallel diodes;
  - a full bridge rectifier from a DC source to a resistive load;
  - a 1 mA current source into a diode;
  - a diode clamp: 5 V through 1 kΩ, clamped by a diode to a 3.3 V source.

## Part F — Tests (in addition to the references)

- **Analytical:** 1 mA into a diode gives V = n·Vt·ln(1 + I/Is), within 1e-7 relative.
- **Companion model:** the diode Jacobian matches finite differences at several biases.
- **`pnjlim`:** unit tests at several biases.
- **Fallbacks:** on a circuit where plain Newton-Raphson fails with `MaxIterations = 3`, gmin or source stepping succeeds and is the reported strategy; and `NonConvergence` is reported when every strategy fails, for example with `MaxIterations = 1` and stepping disabled through an options flag.
- **Linear circuits** solve in one iteration and report it.
- **Exporter:** golden output for a circuit with every component kind and every parasitic, including the flipped current source and the internal nodes.

## Part G — ADR to record verbatim

Create `docs/decisions/ADR-0019-nonlinear-dc-and-diode.md` with exactly:

--- BEGIN ADR-0019 ---
# ADR-0019: Nonlinear DC analysis, the diode model and the ngspice reference

Status: Accepted

## Context
Semiconductor devices make the DC analysis nonlinear. The simulator must converge robustly, stay faithful to SPICE, and be verifiable independently of the AI agents that write it.

## Decision
The DC analysis uses Newton-Raphson on the existing MNA assembly, with SPICE default tolerances, SPICE pnjlim junction limiting, and gmin stepping then source stepping as fallbacks. Every pn junction carries a 1e-12 S gmin in parallel, as in SPICE; linear circuits are still solved exactly in one step. The diode follows the Shockley equation with fixed, hidden parameters (Is = 1e-14 A, n = 1, 300.15 K, no series resistance, no breakdown), consistent with the project's simplified physics. A SPICE3 netlist exporter, with parasitics expanded and the current source sign flipped, provides both an educational bridge to real tools and the basis of a reference harness: reference circuits are simulated once by ngspice with tight tolerances, the results are committed, and tests compare Nodal against them on every build.

## Consequences
Simulator correctness is checked against an independent, established simulator rather than only against hand calculations. Changing a device model requires regenerating the references with ngspice. Exposing device parameters to players later is possible but would be a new decision.
--- END ADR-0019 ---

## Acceptance criteria

- [ ] Plan presented and approved before implementation.
- [ ] `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all pass, with the count reported, including every reference circuit.
- [ ] The headless Godot run prints the demo solution as before.
- [ ] No comments in any hand-written file. No Git command was run. No new package.
- [ ] The remaining inline styles are listed in the report.
- [ ] Spec saved at `docs/specs/0012-newton-diode-ngspice.md`; report at `docs/reports/0012-newton-diode-ngspice.md`, with a manual checklist that includes exporting a circuit and opening it in ngspice or LTspice.

## Out of scope

MOSFETs and BJTs (task 0013), transient analysis, diode breakdown, temperature as a variable, exposing device parameters, migrating older dialogs to the style layer.

## Rulings on the implementer's open items

Given after the plan was approved, and binding for this task:

1. **Thermal voltage constants.** Use ngspice's constants (CODATA 2014: k = 1.38064852e-23, q = 1.6021766208e-19) so that Nodal agrees with the reference exactly. The difference is physically meaningless for the game, and the reference must match. ADR-0019 is recorded with one change: in its Decision section, `(Is = 1e-14 A, n = 1, 300.15 K, no series resistance, no breakdown)` becomes `(Is = 1e-14 A, n = 1, 300.15 K with the Boltzmann constant and elementary charge from CODATA 2014 as used by ngspice, no series resistance, no breakdown)`. The rest of ADR-0019 is unchanged. Report the measured agreement per reference circuit.
2. **`SpiceExporter.TryExport` alongside `Export`:** approved as proposed.
3. **Parallel parasitics.** Capacitor and current-source parallel resistances are exported as a plain resistor across the same two nets, with no internal node. Part D was wrong on that point; internal nodes are used only for the series resistances.

`src/Nodal.Cli` and the throwaway generator for the reference `.nodal` files are approved.
