# ADR-0022: Transistor models

Status: Accepted

## Context
Players build logic gates and analog stages from MOSFETs and bipolar transistors. The models must be credible and educational, consistent with the project's simplified physics, and verifiable against ngspice.

## Decision
MOSFETs use the Shichman–Hodges level 1 model with fixed hidden parameters: threshold 1 V for N-channel and −1 V for P-channel, KP of 1 mA/V², lambda of 0.02 V⁻¹, W/L of 1, no body effect since the body is tied to the source, and the drain-body junction modelled as the real body diode. N and P devices have equal strength, a deliberate simplification for symmetric CMOS. Bipolar transistors use the Ebers–Moll transport model with IS of 1e-14 A, forward beta of 100 and reverse beta of 1, without Early effect, resistances, capacitances or high-injection effects. No transistor has internal capacitances yet. Results report pin voltages, pin currents, power, the operating region and key quantities, and a current probe on a transistor shows its drain or collector current. All models are exported to SPICE with matching parameters and verified against ngspice references.

## Consequences
Switching is instantaneous apart from external RC networks until capacitances are added, which will be needed for realistic propagation delays in logic chips. Exposing transistor parameters to players would be a new decision.
