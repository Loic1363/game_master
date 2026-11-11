# Report 0017 — Controlled sources, switches, push buttons, and ADR-0023 compliance

Status: Done, with one circuit the spec asked for that cannot be an ngspice reference. Build, tests
and the headless run all pass. Parts A to H are in. **Twelve** of the thirteen reference circuits
are committed; `transient-switch-relaxation` is verified against its closed form instead, and the
measurement the spec asked for in that case is under **Open questions**.

## Acceptance

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**.
- `dotnet test Nodal.sln`: **2960 passed, 0 failed** (baseline before the task: 2482, re-measured
  before touching any code; +478). Per phase: A 2507, B 2578, C 2600, D 2643, E 2655, F 2667,
  G 2681, H 2960.
- Headless Godot (`--headless --path game --quit`), exit 0, output unchanged from 0016 except the
  catalogue counts: `Loaded 17 component definitions, 0 diagnostics`,
  `Loaded 17 symbols, 0 diagnostics`, then the same netlist, DC solution,
  `solved to 500 µs in 1004 points`, `1003 accepted, 0 rejected, 1003 iterations`,
  `V(N2) at 100 µs = 3.16 V` and `V(N2) at 500 µs = 4.97 V`.
- ADR-0024 recorded verbatim at `docs/decisions/ADR-0024-controlled-sources-and-switches.md`; the
  `## Live interaction` section appended verbatim to `docs/ideas.md`.
- No comments in any hand-written file. **No git command of any kind was run**, read-only ones
  included. No new package. Rule 11 respected: nothing outside the style layer sets a colour, font
  or size.

## What was built

- **Part A.** `SchematicLimits` (16 MiB file, 20,000 components, 100,000 wires, 200 diagnostics,
  1,000,000 output points, 50,000,000 samples). The size check is a pure function used by the game
  and the CLI; counts are refused by array length before any entry is read; the diagnostics list
  caps and appends `… and {n} more problems.`; `TransientSettings.IsValid` covers the output-point
  and max-step bounds and the reader repairs them per ADR-0017; `TransientRecorder` stops the run
  at the sample limit and returns the partial result; `DirectoryContentSource` reports IO failures
  as content diagnostics.
- **Part B.** `Unit.Siemens` (`S`, Ordinal matching so `s` still means second) and the four
  controlled sources. A current-controlled source emits a 0 V `CurrentSense` voltage-source
  primitive across its own control pins; that branch unknown **is** Ictrl, it joins the existing
  voltage-branch cycle detection for free, and the source never refers to another component.
  `ControlledFeedback` names the sources in a singular feedback loop.
- **Part C/D.** `SwitchDevice` is a nonlinear device whose state is (across, control, flag);
  `Limit` applies the hysteresis against the last accepted flag and returns true on a change, which
  forces another Newton iteration, and `NewtonSolver` reports
  `No stable operating point: switch S1 keeps changing state.` after repeated toggling. The push
  button carries a `PressSchedule`, stored as exact numbers, strict and bounded at 1,000 presses.
- **Part E.** Per-pin voltages, currents and power for all six types; `DeviceRegion.On/Off`; the
  eight hover strings and the `I(E1)`, `I(S1)`, `I(SW1)` probe labels are pinned by tests.
- **Part F.** E, G, F, H and S elements, one `.model NODAL_SW_<des> SW(...)` per switch instance,
  `V<des>_sense` and `V<des>_ctrl` hidden elements, and a control PWL with 1 ns edges centred on
  each instant. Golden deck regenerated, 37 lines to 46.

**Iout sign convention, as the spec asked.** Two conventions are in play, both inherited from the
independent sources. `Of(x).Current` and the current probes are **passive**: positive current
enters the plus terminal, so a VCVS sourcing 10 mA probes as **-10 mA**, exactly like `I(V1)`. The
hover text shows the **delivered** current instead, so the same source reads `Iout 10 mA`, exactly
as an independent source's hover text reads its current as delivered.

## Reference agreement

Nine DC circuits, worst deviation over every probed node and element, against allowances of 1 µV
and 1 pA:

| circuit | worst dV | worst dI | spec expectation, met |
|---|---|---|---|
| `vcvs-amplifier` | 0 | 0 | Vout 5 V, Iout 5 mA |
| `vcvs-inverting-amplifier` | 3.3e-12 V | 2.1e-16 A | Vout **-9.99890012099 V**, inverting input +99.989 µV |
| `vccs-load` | 0 | 0 | V(plus) **+2 V** |
| `cccs-load` | 0 | 0 | Ictrl 1 mA, Iout 10 mA, Vout 1 V |
| `ccvs-load` | 0 | 0 | Vout 2 V, Iout 2 mA |
| `switch-on` | 4.4e-15 V | 4.4e-15 A | I 4.995005 mA, V 4.995 mV |
| `switch-off` | 0 | 5.0e-21 A | I 5.000 nA |
| `push-button-pressed` | 5.3e-15 V | 3.9e-15 A | I 4.99995 mA |
| `push-button-released` | 0 | 5.0e-21 A | I 5.000 nA |

Three transient circuits, worst deviation as a fraction of the per-series allowance
`max(floor, 1e-4 × range)`:

| circuit | worst series | worst / allowed | spec expectation, met |
|---|---|---|---|
| `transient-vcvs-rc` | I(C1), I(E1), I(R1) | **0.055** | amplitude 4.224 V sampled (4.2338 V analytic, the 25 µs grid cannot sample the peak), phase **-32.14°** against -32.14° analytic |
| `transient-switch-rc` | V(N3) | **0.112** | V(C) at 1 ms **3.15784 V**, held at 3.1606 V while off |
| `transient-push-button` | I(C1) | **0.000** | V(Y) at 3.008 ms 4.0386 V, at 4.992 ms 3.3118 V |

Re-running `Update-References.ps1` over all 49 folders reproduces every previously committed file
**byte for byte**, so the script changes are regression-free.

## ngspice-47 SW behaviour, measured

- **DC**: the switch starts **open** and closes only when Vctrl is *strictly* above VT + VH.
  Measured with VT 2.5, VH 1 at Vctrl 1.4, 2.0, 2.5, 3.0, 3.4, **3.5 (still open)** and 3.6
  (closed). Nodal's zero state is open, so the rule matches.
- **Transient**: closes above VT + VH, opens below VT - VH, keeps its state in between. Measured
  with a 0-5-0 V ramp: closes at Vctrl 3.5014, opens at 1.4975.
- **Timestep control: none.** Through both transitions the step stayed at the uniform 1 µs maxstep.
  ngspice inserts no breakpoint and does not limit the step at an SW threshold; Nodal does not
  either for a voltage-controlled switch, so the two models match. This is the root of the open
  question below.
- **`@s[i]` is wrong in `op` when the switch is closed**: it reports V/ROFF (4.995e-12 A where the
  true current is 4.995e-3 A). It is correct in `tran`, hence the workaround in the script.
- **Push-button export**: the 1 ns control edge centred on each instant switches ngspice at
  t = 1.000000000196 ms and t = 3.000000000 ms, so the export's switching time differs from
  Nodal's exact instant by **under 0.2 ps**.

## Defect found and fixed in Nodal

`transient-push-button` refused with `the timestep became too small` at the press instant (1064
accepted, 76 rejected, dead at t = 1 ms). The button was stamped with its state at the step's
**target**, so the resistance discontinuity sat *inside* the step that lands on the breakpoint; the
truncation-error limit rejected that step, the stepper crept toward the breakpoint with doubling
steps, landed, was rejected again, and ran out of step. `TransientSystem` now stamps
`primitive.ResistanceAt(integration.Midpoint(time))`: a step carries the state it starts with, no
state change ever falls strictly inside a step because every press instant is a breakpoint, and the
discontinuity lands exactly on a step boundary where backward Euler restarts. The run is now 5027
accepted, 2 rejected, and agrees with ngspice to a ratio of 0.000 on every series. The recorded
*state* series still reads the instant itself, so `IsClosedAt(1 ms)` is pressed while the current
recorded at that same point is the left limit; the two differ only at a switching instant.

## Deviations and choices the spec did not spell out

1. **`transient-switch-rc` needed `startFromZero`.** Without it the capacitor sits at 5 V from the
   operating point, because nothing discharges it through the open switch, and the spec's 3.16 V at
   1 ms is unreachable. With it the circuit matches.
2. **`transient-push-button` uses a 16 µs output step**, not the 25 µs the other 5 ms circuits use,
   so that 1 ms and 3 ms fall exactly half-way between grid points. On the 25 µs grid both instants
   are grid points, where `linearize` interpolates across the switching edge and ngspice's own
   reference reads 4.55 V for a node that is either 0.00005 V or 4.99995 V; no tool has a defined
   value at a discontinuity.
3. **The harness now allows a transient-only reference circuit.** `ReferenceCircuits.Names()` is
   driven by the presence of `expected.json`, as `TransientNames` already was, and
   `ReferenceFolderTests` pins that every folder carries a schematic, a deck, a probe plan and at
   least one expectation, and that a folder may skip `expected.json` only when Nodal refuses its
   DC. Nothing uses the skip today; it exists so the relaxation oscillator can come back as a
   transient-only reference if you rule that way.
4. **`Update-OperatingPoint` refuses to write a DC expectation when ngspice fell back to a
   pseudo-transient op** (it prints `Transient op started`), and throws if an `expected.json`
   already exists for such a circuit. ngspice's pseudo-transient result is wherever its fake
   transient stopped, not a DC solution, so it must never become a reference.
5. **`Invoke-Ngspice` no longer dies on an ngspice note written to stderr.** Under Windows
   PowerShell 5.1 with `$ErrorActionPreference = 'Stop'`, `& ngspice … 2>&1` turns every stderr
   line into a terminating `NativeCommandError`. The gmin and source-stepping notes of a circuit
   with no operating point tripped it. The function now reads stderr as text, and the existing
   `error|fatal` check still throws on a real failure.
6. The spec's illustrative hover digits are not what the formatter produces: with three significant
   digits the switch reads `V 5 mV, I 5 mA` where the spec wrote `4.99`. The formats match exactly.
7. An advanced parameter may carry a default. The catalogue invariant that asserted
   `required count == basic non-waveform count` now asserts only what ADR-0016 states, that every
   basic non-waveform parameter is required, because the switch and the push button have advanced
   parameters with defaults as the spec dictates.
8. An open-ended press must start at 0, so `pressed` is the only open-ended schedule and every
   schedule round-trips through its text. A stored `[[0.001]]` is refused as `Unrecognized`.
9. `ControlledFeedback`'s wording agrees in number: one source reads
   `E1 forms a feedback loop whose gain leaves the circuit undetermined.`
10. A designator read from a `.nodal` file is not character-validated, so a hand-edited file could
    carry an underscore and collide with a hidden name. This pre-dates the task; it applies to
    `_int` and `_esr` too. A test pins that generated designators never contain an underscore.

## Open questions

**The relaxation oscillator cannot be an ngspice reference, and the spec told me to stop and report
rather than loosen. Here is the measurement; the decision is yours.**

Switching instants, same deck, same options, 1 µs maxstep:

| event | ngspice (ms) | Nodal (ms) | difference |
|---|---|---|---|
| close 1 | 1.203996 | 1.203984 | -12 ns |
| open 1 | 1.212546 | 1.212564 | +18 ns |
| close 2 | 2.058562 | 2.059869 | +1.31 µs |
| close 3 | 2.913671 | 2.915772 | +2.10 µs |
| close 4 | 3.763314 | 3.771654 | +8.34 µs |
| close 5 | 4.617550 | 4.627534 | +9.98 µs |

Periods: ngspice 854.57, 855.11, 849.64, 854.24 µs (mean 853.39); Nodal 855.885, 855.903, 855.882,
855.880 µs. The closed form is 1 ms·ln(3.5/1.5) + 9.90099 µs·ln(3.4505/1.4505) = **855.877 µs**, so
**Nodal is within 11 ns (13 ppm) and ngspice within 2.49 µs (2900 ppm)**. The cause is the measured
fact above: with no timestep control at an SW threshold, ngspice overshoots it by up to 3.5 mV, and
during the 146 kV/s discharge that truncates the cycle. The drift is about **+2.5 µs per cycle**,
reaching 10 µs at 5 ms. Any output grid point inside the 8.6 µs discharge then differs by tens of
millivolts against an allowance of 350 µV, and no choice of output step or maxstep closes that gap
between two different integrators. Nodal is the more accurate of the two here.

Nothing was loosened. The circuit was taken out of `tests/reference-circuits` and
`ControlCircuits.RelaxationOscillator` plus `RelaxationOscillatorTests` now verify the first charge
time, every period and both switching voltages against the closed form, at 1e-4 relative on the
instants and 1e-3 on the voltages. Possible rulings: leave it as a closed-form test; bring it back
as a transient-only reference with a tolerance you set explicitly for it; or give Nodal breakpoint
or step limiting at a switch threshold and compare against an ngspice run with a maxstep small
enough to match, which would be a new spec.

## Proposals, not implemented

- Validate designator characters on read, so a hand-edited file cannot collide with a hidden
  export name (deviation 10).
- Give the voltage-controlled switch timestep limiting at its threshold: cut the step when the
  control crosses VT ± VH inside it, as commercial simulators do. It would make the switch far more
  accurate than ngspice's SW, which is already the case here by accident, and would let the
  relaxation oscillator be compared against a finely stepped ngspice run.
- Record two samples at a switching instant, the left and the right limit, so the state and the
  current never disagree at a breakpoint.

## Manual checklist for the project lead

1. **Inverting amplifier from a VCVS.** Open
   `tests/reference-circuits/vcvs-inverting-amplifier/circuit.nodal`. Hover E1: it must read
   `E1 (Voltage-controlled voltage source): Vctrl -100 µV, Vout -10 V, Iout -1 mA` (three
   significant digits, so -9.9989 V shows as -10 V). The node between R1 and R2 must label
   `100 µV`, the virtual ground, and the output node `-10 V`.
2. **Switch-controlled RC.** Open `tests/reference-circuits/transient-switch-rc/circuit.nodal` and
   run the transient. S1 must read `on` while the control pulse is high and `off` after it falls,
   and the waveform viewer must show V on the capacitor node rising to 3.16 V at 1 ms and then
   **holding flat**, not decaying. Probe `I(S1)`: the label must be exactly `I(S1)`, in amperes.
3. **Push button with a schedule.** Open
   `tests/reference-circuits/transient-push-button/circuit.nodal`, open SW1's properties and check
   the Presses field reads `1m to 3m`. Type `1m to 3m, 5m to` and the field must refuse it with
   `Unrecognized press schedule. Use released, pressed, or intervals such as 1m to 3m, 5m to 6m.`
   Restore `1m to 3m`, run, and hover SW1 at 2 ms: `SW1 (Push button): pressed, …`; at 4 ms:
   `released`. Save and reopen the file: the schedule must survive as `1m to 3m`.
4. **Place menu.** The place menu must offer all six new components under the source category:
   the four controlled sources, Voltage-controlled switch and Push button.
