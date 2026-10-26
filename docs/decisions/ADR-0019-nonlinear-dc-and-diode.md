# ADR-0019: Nonlinear DC analysis, the diode model and the ngspice reference

Status: Accepted

## Context
Semiconductor devices make the DC analysis nonlinear. The simulator must converge robustly, stay faithful to SPICE, and be verifiable independently of the AI agents that write it.

## Decision
The DC analysis uses Newton-Raphson on the existing MNA assembly, with SPICE default tolerances, SPICE pnjlim junction limiting, and gmin stepping then source stepping as fallbacks. Every pn junction carries a 1e-12 S gmin in parallel, as in SPICE; linear circuits are still solved exactly in one step. The diode follows the Shockley equation with fixed, hidden parameters (Is = 1e-14 A, n = 1, 300.15 K with the Boltzmann constant and elementary charge from CODATA 2014 as used by ngspice, no series resistance, no breakdown), consistent with the project's simplified physics. A SPICE3 netlist exporter, with parasitics expanded and the current source sign flipped, provides both an educational bridge to real tools and the basis of a reference harness: reference circuits are simulated once by ngspice with tight tolerances, the results are committed, and tests compare Nodal against them on every build.

## Consequences
Simulator correctness is checked against an independent, established simulator rather than only against hand calculations. Changing a device model requires regenerating the references with ngspice. Exposing device parameters to players later is possible but would be a new decision.
