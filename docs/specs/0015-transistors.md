# Task 0015 — MOSFETs and bipolar transistors

## Mode

Stay in the current mode. **First present your implementation plan** (device models and their companion stamps, limiting, multi-terminal results and regions, elaboration, export, probe semantics, the reference circuits, tests, phases), **then stop and wait** for approval. Follow rule 12 (progress file) throughout.

## Context

Report 0014 is accepted. The project lead tested the waveform viewer with DC and PULSE-driven RC circuits and the results are correct. This task adds the four transistor types to the simulator, in the DC and transient analyses, the SPICE export, the overlay, the hover text and the probes. All CLAUDE.md rules apply.

## Part A — Polish backlog

Append this section to the end of `docs/ideas.md`, verbatim:

--- BEGIN ideas.md addition ---

## Polish backlog (from the project lead's tests)
- Labels overlap: voltage pills cover designators and values (V1, 1 kΩ), and a source's function text collides with nearby ground labels. Labels need collision avoidance.
- Leaving the probe tool (Escape) and hiding the waveform panel (View menu) are not discoverable; the panel needs a visible close button and the probe tool a clearer entry and exit.
- The P shortcut for probes is not ideal; revisit shortcuts during the design overhaul.
- Menus with icons.
--- END ideas.md addition ---

## Part B — Device models

Parameters are fixed in code, hidden from the player, and chosen to match ngspice exactly (simplified physics, ADR-0019 style).

**MOSFETs** (`nmos`, `pmos`), Shichman–Hodges level 1:

- NMOS: VTO = 1 V, KP = 1e-3 A/V², LAMBDA = 0.02 V⁻¹, W/L = 1, no body effect (the body is tied to the source).
- PMOS: VTO = −1 V, KP = 1e-3 A/V², LAMBDA = 0.02 V⁻¹, W/L = 1. Equal strength to NMOS on purpose, for symmetric CMOS, a documented simplification.
- **Regions:** cutoff, linear (triode), saturation.
- **Symmetric device:** when the drain-source voltage reverses, the roles of drain and source swap internally, as SPICE does.
- **Body diode:** with the body tied to the source, include the drain-body junction as a diode with the same Shockley parameters as task 0012 (Is = 1e-14 A, n = 1, CODATA 2014 thermal voltage), matching ngspice level 1's bulk junction with default parameters. This is the real body diode of a discrete MOSFET.
- **No gate or junction capacitances** in this task.
- **Limiting:** use SPICE's `fetlim` and `limvds` algorithms.

**Bipolar transistors** (`npn`, `pnp`), Ebers–Moll (the transport form of SPICE's Gummel–Poon with all extras disabled):

- IS = 1e-14 A, BF = 100, BR = 1, NF = NR = 1.
- No Early effect, no resistances, no capacitances, no high-injection effects.
- **Regions:** cutoff, forward active, saturation, reverse active.
- **Limiting:** `pnjlim` on both junctions.

**Junction gmin:** 1e-12 S across each pn junction, as in task 0012.

Remove the transistors from the unsupported refusals. Every base component is now supported.

## Part C — Multi-terminal results

**DC results.** Extend `ComponentOperatingPoint`, without breaking the existing two-terminal API, with:

- per pin: the pin voltage relative to ground and the current entering that pin;
- the total absorbed power, the sum of the pin voltages times the pin currents;
- for transistors, the region and the key quantities:
  - MOSFET: Vgs, Vds, Id (current entering the drain);
  - BJT: Vbe, Vce, Ic, Ib, and the effective β = Ic / Ib when |Ib| > 1 pA.

**Transient results.** Every pin current of every multi-terminal component, with `CurrentAt` per pin.

**Probes:**

- A current probe on a transistor shows the drain current (MOSFET) or the collector current (BJT), labelled `I(M1)` or `I(Q1)`.
- Choosing another pin is a later feature; keep the probe model ready for it without changing the file format now.

**Hover text:**

- `M1 (N-channel MOSFET): saturation, Vgs 3 V, Vds 5 V, Id 2 mA`
- `Q1 (NPN transistor): forward active, Vbe 652 mV, Vce 4.1 V, Ic 1 mA, Ib 10 µA, β 100`

Values go through the existing display threshold.

## Part D — Elaboration and export

- The transistors enter the elaboration layer as primitives with three external terminals. The MOSFET body diode is part of the MOSFET primitive, not a separate visible element.
- **SPICE export:**
  - `M<designator> drain gate source source NODAL_NMOS` (or `NODAL_PMOS`), with W and L of equal value;
  - `Q<designator> collector base emitter NODAL_NPN` (or `NODAL_PNP`);
  - `.model` lines carrying exactly the parameters of Part B. Verify against ngspice-47 that no default you did not intend remains active (gate capacitances, bulk junction parameters, Gummel–Poon extras), and report what you checked.

## Part E — References and tests

**DC reference circuits:**

- NMOS common source with a drain resistor, one bias in saturation and one in the linear region;
- PMOS common source;
- CMOS inverter (5 V supply) with inputs at 0 V, 2.5 V and 5 V, as three circuits;
- NMOS with its body diode forward-biased (Vds negative);
- NPN common emitter with voltage-divider bias, in forward active;
- NPN saturated switch;
- PNP common emitter;
- NPN current mirror;
- NPN differential pair with resistive loads and a tail resistor.

**Transient reference circuits:**

- CMOS inverter driven by a PULSE, with a 100 pF load capacitor;
- NPN switch driving a resistive load from a PULSE through a base resistor;
- three-stage CMOS ring oscillator, with an RC between stages (1 kΩ and 100 pF). Choose a start-up condition that works identically in both engines: an initial PULSE kick on one node, or start from zero with an asymmetry, as you see fit. Report your choice.

Use the same comparison rules and tolerances as tasks 0012 and 0013. Never loosen silently; report any disagreement with analysis.

**Additional tests:**

- every region of every transistor, at chosen biases, against the closed-form equations;
- Jacobians against finite differences in every region, including the drain-source swap;
- `fetlim` and `limvds` unit tests;
- convergence of a hard operating point that needs a fallback strategy: two cross-coupled CMOS inverters (a latch). Assert only that it converges to one of its valid states, since several exist; do not use it as an ngspice reference;
- multi-terminal results: Kirchhoff's current law at every transistor (pin currents sum to zero within tolerance) and the power balance on every reference circuit;
- hover text and probe labels for transistors;
- the export golden file updated with all four transistor types.

The headless run output is unchanged.

## Part F — ADR to record verbatim

Create `docs/decisions/ADR-0022-transistor-models.md` with exactly:

--- BEGIN ADR-0022 ---
# ADR-0022: Transistor models

Status: Accepted

## Context
Players build logic gates and analog stages from MOSFETs and bipolar transistors. The models must be credible and educational, consistent with the project's simplified physics, and verifiable against ngspice.

## Decision
MOSFETs use the Shichman–Hodges level 1 model with fixed hidden parameters: threshold 1 V for N-channel and −1 V for P-channel, KP of 1 mA/V², lambda of 0.02 V⁻¹, W/L of 1, no body effect since the body is tied to the source, and the drain-body junction modelled as the real body diode. N and P devices have equal strength, a deliberate simplification for symmetric CMOS. Bipolar transistors use the Ebers–Moll transport model with IS of 1e-14 A, forward beta of 100 and reverse beta of 1, without Early effect, resistances, capacitances or high-injection effects. No transistor has internal capacitances yet. Results report pin voltages, pin currents, power, the operating region and key quantities, and a current probe on a transistor shows its drain or collector current. All models are exported to SPICE with matching parameters and verified against ngspice references.

## Consequences
Switching is instantaneous apart from external RC networks until capacitances are added, which will be needed for realistic propagation delays in logic chips. Exposing transistor parameters to players would be a new decision.
--- END ADR-0022 ---

## Acceptance criteria

- [ ] Plan presented and approved before implementation.
- [ ] `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all pass, with the count reported, including every reference.
- [ ] A reference agreement table for every new circuit is in the report.
- [ ] No comments in any hand-written file. No Git command was run. No new package. Rule 11 respected.
- [ ] Spec saved at `docs/specs/0015-transistors.md`; report at `docs/reports/0015-transistors.md`, with a manual checklist including a CMOS inverter and an NPN switch built in the game.

## Out of scope

Transistor capacitances, a four-terminal MOSFET, exposing device parameters, DC sweep analysis, choosing the probed pin, any UI redesign.

## Rulings on the plan

1. DeviceRegion: a single enum (Cutoff, Linear, Saturation, ForwardActive, ReverseActive).
2. `.model` lines: emitted conditionally for all five kinds, including the diode. Regenerate all committed decks and confirm in the report that every expected.json is byte-identical after regeneration.
3. The NonlinearDevice abstraction, the ring-oscillator kick through a pulsed current source, and the phase order are approved.
