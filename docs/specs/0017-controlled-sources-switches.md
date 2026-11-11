# Task 0017 — Controlled sources, switches, push buttons, and ADR-0023 compliance

## Mode
Stay in the current mode. **First present your implementation plan** (the ADR-0023 fixes with their limits, the new unit, the four controlled-source primitives and their stamps, the current-sense branch, the switch model and its state handling in DC and transient, the press schedule value and its file representation, symbols with pin coordinates, results and hover text, export with hidden elements, the reference circuits, tests, phases), **then stop and wait** for approval. Follow rule 12 (progress file) throughout. Rule 1 covers every git command, read-only ones included.

## Context
Report 0016 is accepted. This task adds the remaining linear building blocks and the first switching elements, and closes the gaps the 0016 audit found.

- **Why Part A:** the 0016 audit showed that a hostile `.nodal` file can crash the game (F1) or exhaust its memory (F3), which already violates ADR-0023. Bounding the readers is cheap now and mandatory before any file sharing.
- **Why controlled sources:** they are the basis of behavioral op-amps, optocouplers and the future chip characterization bench; every later analog abstraction is built on them.
- **Why switches and push buttons:** players need circuits that change state; a voltage-controlled switch is the SPICE primitive, and the push button is the player-facing version.
- **Why the push button follows a schedule:** the transient analysis is computed in one batch, so a player cannot press a button during a run. A button is therefore either in a fixed state chosen before the run, or pressed at scheduled instants. A live button needs a real-time simulation, which is a later decision.

All CLAUDE.md rules apply.

## Part A — ADR-0023 compliance (audit findings F1 to F4)
- **F1, file size.** Before reading a `.nodal` file into memory, check its length. Refuse files above **16 MB** as an ordinary read error, with exactly: `The file is too large to open: {size} MB, while the limit is 16 MB.` (size with one decimal). Apply it in the game and in the CLI.
- **F2, counts.** Add `MaxComponents = 20000` and `MaxWires = 100000` next to `MaxProbes`. A file above either limit is refused: `The file has too many components: {n}, while the limit is 20,000.` / `The file has too many wires: {n}, while the limit is 100,000.` Cap the reader's diagnostics list at **200** entries, followed by one final entry: `… and {n} more problems.`
- **F3, transient bounds.** `TransientSettings.IsValid` also requires stopTime / outputStep ≤ **1,000,000**, and, when set, maxStep ≥ stopTime × 1e-9. Invalid settings read from a file are repaired as ADR-0017 prescribes (reset to the defaults with a warning): `The transient settings ask for {n} output points, more than the limit of 1,000,000; they were reset to the defaults.` and `The maximum step is too small for the stop time; it was unset.` In addition, the recorder stops the run when the stored samples (time points × recorded series) exceed **50,000,000**, keeping what was recorded as a cancelled run does, with the error: `The transient analysis stopped at {time}: the result reached the memory limit of 50 million samples. Shorten the stop time or increase the output step.`
- **F4, content loading.** `DirectoryContentSource` catches file failures (`IOException`, `UnauthorizedAccessException`, a missing directory) and reports them as content diagnostics, as `GodotContentSource` does: `Could not read content file {path}: {reason}.`
- F5 and F6 need no change. Keep the loaders' deduplication, which keeps the throwing constructors unreachable.
- Tests: each limit at its value (accepted) and one above it (refused or repaired with the exact message), the 16 MB check without allocating 16 MB in memory, the diagnostics cap, the recorder cap with a deliberately small limit injected in the test, and F4 with a missing directory.

## Part B — Unit and controlled sources
**New unit:** `Unit.Siemens`, symbol `S`, parsed in the quantity notation (`1mS`, `1m`). Check that it does not collide with any existing suffix and that `s` still means second.

**Four new components** (category `source`, all ideal and linear, gains are basic player parameters):

| id | name | prefix | parameter | unit | default |
|---|---|---|---|---|---|
| `vcvs` | Voltage-controlled voltage source | `E` | `gain` | dimensionless | `10` |
| `vccs` | Voltage-controlled current source | `G` | `transconductance` | siemens | `1m` |
| `cccs` | Current-controlled current source | `F` | `gain` | dimensionless | `10` |
| `ccvs` | Current-controlled voltage source | `H` | `transresistance` | ohm | `1k` |

Pins: `plus`, `minus` (output), `control-plus`, `control-minus`. Gains may be negative or zero; no bounds.

**Sign conventions (exact):**
- Vctrl = V(control-plus) − V(control-minus). Ictrl = **the current entering control-plus**, flowing through the source and leaving by control-minus.
- **Voltage control:** the control pins draw no current (infinite input impedance).
- **Current control:** the control pins are joined by an internal ideal ammeter (0 V), so the control port is placed **in series** with the branch to sense. The sense branch is an MNA branch-current unknown, **never a visible element**. The source needs no reference to another component.
- VCVS: V(plus) − V(minus) = gain × Vctrl. CCVS: V(plus) − V(minus) = transresistance × Ictrl.
- VCCS and CCCS follow **ADR-0018**: they drive their current **out of their plus terminal** into the external circuit, like our independent current source (I = transconductance × Vctrl, or gain × Ictrl).

**Diagnostics:**
- A VCVS or CCVS output, or a current-control port, takes part in the existing voltage-branch cycle detection exactly as a voltage source does.
- A singular system caused by controlled-source feedback (for example a VCVS of gain exactly 1 whose output drives its own control) must produce a diagnostic that **names the controlled sources involved**. Reuse the existing singular-system machinery; propose the exact wording in your plan.

**Symbols (IEC):** a diamond for the output, with + and − for a voltage output and an arrow for a current output, and the control port as two pins on the left, marked + and − for a voltage control and with an arrow for a current control. Propose the grid coordinates in your plan.

## Part C — Voltage-controlled switch
Component `switch`, name `Voltage-controlled switch`, prefix `S`, category `source`. Pins: `plus`, `minus` (contacts), `control-plus`, `control-minus` (no current drawn).

| parameter | tier | unit | default | bounds |
|---|---|---|---|---|
| `threshold` | basic | volt | `2.5` | none |
| `hysteresis` | advanced | volt | `0` | min `0` |
| `on-resistance` | advanced | ohm | `1` | min `1m` |
| `off-resistance` | advanced | ohm | `1G` | min `1` |

- **Semantics: ngspice-47's SW model**, with VT = threshold, VH = hysteresis, RON, ROFF. The switch is on above VT + VH, off below VT − VH, and keeps its previous state in between. **Measure ngspice-47's behavior and match it**: the initial state, the DC iteration rule, and the transient timestep control near the thresholds if ngspice has one. Report what you measured, as you did for the defaults audit in 0015.
- **DC:** a state change during Newton iterations forces another iteration. If the state keeps toggling, the analysis fails with exactly: `No stable operating point: switch {designator} keeps changing state.`
- **Transient:** the hysteresis memory is the state at the last accepted time point.

## Part D — Push button and the press schedule
Component `push-button`, name `Push button`, prefix `SW`, category `source`. Pins `a`, `b`, no polarity.

| parameter | tier | kind | default |
|---|---|---|---|
| `presses` | basic | **new `ParameterKind.Schedule`** | `released` |
| `on-resistance` | advanced | ohm, min `1m` | `10m` |
| `off-resistance` | advanced | ohm, min `1` | `1G` |

**Schedule text syntax**, parsed with the game's quantity notation, units optional:
- `released`: open for the whole run;
- `pressed`: closed for the whole run;
- a list of intervals such as `1m to 3m, 5m to 6m`: closed during each [start, end), open elsewhere.

Exact error messages:
- `Unrecognized press schedule. Use released, pressed, or intervals such as 1m to 3m, 5m to 6m.`
- `Each press must end after it starts: {start} to {end}.`
- `Presses must be in increasing time order and must not overlap.`
- `A press cannot start before 0 s.`
- `A schedule has at most 1,000 presses.` (ADR-0023 bound)

**Semantics:**
- **DC operating point:** the state at t = 0, so the button is pressed if the schedule is `pressed` or an interval starts at 0.
- **Transient:** the resistance switches **exactly** at each start and end. These instants are breakpoints, followed by backward Euler, like waveform corners.

**Storage:** the schedule is saved as structured exact numbers (ADR-0017), never as text, and its reader is strict and bounded (ADR-0023). Propose the JSON shape in the plan. **schemaVersion stays unchanged**, because only the new component type uses this value; ADR-0024 records this.

Append to `docs/ideas.md`, verbatim:

--- BEGIN ideas.md addition ---
## Live interaction
- Pressing a push button or turning a potentiometer while the circuit runs needs a real-time simulation mode. Until then the push button follows a press schedule set before the run (ADR-0024).
--- END ideas.md addition ---

## Part E — Results, hover text, probes
- **DC and transient results:** per-pin voltages and currents and power as the sum over pins, as for transistors in 0015. Per-pin `CurrentAt` in transient, including the four-pin devices.
- **Hover text formats** (values through the existing display threshold):
  - `E1 (Voltage-controlled voltage source): Vctrl 500 mV, Vout 5 V, Iout 5 mA`
  - `G1 (Voltage-controlled current source): Vctrl 1 V, Iout 1 mA, Vout 2 V`
  - `F1 (Current-controlled current source): Ictrl 1 mA, Iout 10 mA, Vout 1 V`
  - `H1 (Current-controlled voltage source): Ictrl 1 mA, Vout 2 V, Iout 2 mA`
  - `S1 (Voltage-controlled switch): on, Vctrl 5 V, V 4.99 mV, I 4.99 mA`
  - `SW1 (Push button): pressed, V 50 µV, I 5 mA`
- Definitions: Vout = V(plus) − V(minus); for switches V = V(plus or a) − V(minus or b) and I = the current entering plus or a. **Iout uses the same sign convention as the existing hover text of the independent sources**; state which one it is in the report.
- **Switch states:** extend `DeviceRegion` with `On` and `Off`. The hover text reads `on`/`off` for the switch and `pressed`/`released` for the push button.
- **Current probes:** label `I(E1)`, `I(S1)`, `I(SW1)`. They show the output current of a controlled source with the same convention as the independent sources, and the current entering plus or a for switches.

## Part F — Export
- `E<des> plus minus control-plus control-minus <gain>`
- `G<des> minus plus control-plus control-minus <gm>` (nodes flipped, as for our current source)
- `F<des> minus plus <sense> <gain>` and `H<des> plus minus <sense> <r>`, with a hidden 0 V sense source `V<des>_sense control-plus control-minus 0`. Its positive current enters control-plus, which matches Ictrl.
- `S<des> plus minus control-plus control-minus <model>`, with one `.model <name> SW(VT= VH= RON= ROFF=)` per switch, since parameters vary per instance.
- Push button: an S element driven by a hidden control source `V<des>_ctrl` carrying a 0 V / 1 V PWL, with VT = 0.5 and VH = 0. **Center each 0 → 1 V edge on the scheduled instant** (propose the edge duration). Measure and report the resulting switching-time difference with Nodal.
- Hidden names must never collide with player designators; follow the existing `_int`/`_esr` naming style. Regenerate the golden deck with all six new types.

## Part G — References and tests
**Same comparison rules and tolerances as tasks 0012 and 0013. DC: 1e-6 relative, or 1 µV / 1 pA absolute. Transient: max(floor, 1e-4 × range) per series, floors 1 µV and 1 nA. Never loosen a tolerance, silently or not; if a circuit cannot agree, stop and report it with analysis.**

**DC reference circuits** (expected values by hand):

| circuit | setup | expected |
|---|---|---|
| `vcvs-amplifier` | 0.5 V on the control port, gain 10, 1 kΩ load | Vout 5 V, Iout 5 mA |
| `vcvs-inverting-amplifier` | VCVS gain 1e5 used as an op-amp: Rin 1 kΩ, Rf 10 kΩ, Vin 1 V, + input grounded | Vout = −10 / (1 + 11e-5) = **−9.998900 V**; inverting input ≈ +100 µV |
| `vccs-load` | 1 V control, 1 mS, 2 kΩ from plus to ground | V(plus) **+2 V** (current out of plus, ADR-0018) |
| `cccs-load` | 1 V source through 1 kΩ into the control port, gain 10, 100 Ω load | Ictrl 1 mA, Iout 10 mA, Vout 1 V |
| `ccvs-load` | same control, 2 kΩ, 1 kΩ load | Vout 2 V, Iout 2 mA |
| `switch-on` | 5 V control, 5 V supply → switch → 1 kΩ | I = 5 / 1001 = **4.995 mA**, V(switch) 4.995 mV |
| `switch-off` | 0 V control, same | I ≈ **5 nA**, V(switch) ≈ 5 V |
| `push-button-pressed` | `pressed`, 5 V → button → 1 kΩ | I = 5 / 1000.01 = **4.99995 mA** |
| `push-button-released` | `released`, same | I ≈ 5 nA |

**Transient reference circuits:**

| circuit | setup | expected |
|---|---|---|
| `transient-vcvs-rc` | SINE(0 1 1k) on the control, gain 5, output → 1 kΩ → 100 nF to ground, 5 ms | steady amplitude 5 / √(1 + (2π·1k·1k·100n)²) = **4.23 V**, phase ≈ −32° |
| `transient-switch-rc` | control PULSE(0 5 0 1u 1u 1m 2m); 5 V → switch → 1 kΩ → 1 µF to ground, 4 ms | V(C) at 1 ms = 5(1 − e^(−1/1.001)) ≈ **3.16 V**, then held while off |
| `transient-switch-relaxation` | 5 V → 10 kΩ → node X, 100 nF from X to ground, switch from X to ground with on-resistance 100 Ω, control = V(X), threshold 2.5 V, hysteresis 1 V, **start from zero**, 5 ms | on above 3.5 V, off below 1.5 V; first charge 1 ms · ln(5/1.5) ≈ **1.20 ms**, then period ≈ 1 ms · ln(3.5/1.5) + 9.9 µs · ln(3.45/1.45) ≈ **0.856 ms** |
| `transient-push-button` | presses `1m to 3m`; 5 V → button → 1 kΩ → node Y, 1 µF and 10 kΩ from Y to ground, 5 ms | V(Y) at 3 ms = 4.545(1 − e^(−2/0.909)) ≈ **4.04 V**; at 5 ms ≈ 4.04 · e^(−0.2) ≈ **3.31 V** |

The relaxation oscillator has no DC operating point (it oscillates), so it must start from zero. If ngspice and Nodal disagree on its switching instants beyond the tolerance, **stop and report** the measured phase drift per cycle; do not loosen.

**Additional tests:**
- every controlled source against its defining equation, including negative and zero gains;
- the sense branch: Ictrl equals the current of the series branch, and KCL holds at all four pins;
- the singular feedback diagnostic;
- switch: on, off, inside the hysteresis band from both sides, and the `keeps changing state` failure on a deliberately unstable circuit;
- schedule parser: every syntax and every error message above, the 1,000-press bound, round-trip through save and open;
- push-button transient: the state changes exactly at the scheduled instants, which are breakpoints;
- hover text and probe labels for all six types; the power balance and pin-KCL theories over all reference circuits; the export golden file.

The headless run output is unchanged.

## Part H — ADR to record verbatim
Create `docs/decisions/ADR-0024-controlled-sources-and-switches.md` with exactly:

--- BEGIN ADR-0024 ---
# ADR-0024: Controlled sources, switches and push buttons

Status: Accepted

## Context
Behavioral op-amps, optocouplers, the future characterization bench and circuits that change state need ideal controlled sources and switches. The transient analysis is computed in one batch, so a player cannot press a button during a run.

## Decision
The four SPICE controlled sources are base components: voltage-controlled voltage and current sources, current-controlled current and voltage sources. They are ideal and linear, with infinite input impedance on a voltage control and zero on a current control. A current-controlled source senses the current through its own control pins, so it never refers to another component. Controlled current sources drive their current out of their plus terminal, as in ADR-0018. Gains are player parameters, like resistances. The voltage-controlled switch follows ngspice's SW model: a resistance that changes between on and off values at a threshold, with optional hysteresis and the same state semantics. The push button is a two-terminal switch whose state follows a press schedule set before the run: released, pressed, or a list of time intervals, stored as exact numbers and bounded. Its state changes exactly at the scheduled instants, which are breakpoints. Exports use SPICE E, F, G, H and S elements, with hidden sense and control sources where SPICE needs them. The schedule value does not change the schemaVersion, since only the new push-button type uses it. Pressing a button during a run requires a real-time simulation and is a later decision.

## Consequences
Behavioral models can be built from controlled sources. Switches are discontinuous, so transient accuracy around switching instants depends on timestep control, and references verify it against ngspice.
--- END ADR-0024 ---

## Acceptance criteria
- [ ] Plan presented and approved before implementation.
- [ ] dotnet build: 0 warnings, 0 errors. dotnet test: all pass, with the count reported, including every reference.
- [ ] A reference agreement table for every new circuit, and the ngspice SW behavior you measured.
- [ ] No comments in any hand-written file. No git command of any kind was run. No new package. Rule 11 respected.
- [ ] Spec saved at docs/specs/0017-controlled-sources-switches.md; report at docs/reports/0017-controlled-sources-switches.md, with a manual checklist including an inverting amplifier built from a VCVS, a switch-controlled RC, and a push button with a schedule.

## Out of scope
A live push button or any real-time simulation, an op-amp component, nonlinear or behavioral (B) sources, output resistance on controlled sources, switch capacitances, transistor capacitances, DC sweep, the F5 and F6 audit items, any UI redesign.
