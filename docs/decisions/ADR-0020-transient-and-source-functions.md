# ADR-0020: Transient analysis, source functions and elaboration

Status: Accepted

## Context
Players must see circuits evolve in time, as in LTspice: capacitors charging, PWM signals, rectifiers. Parasitic parameters become meaningful in the time domain, and several analyses must interpret the circuit identically.

## Decision
A single elaboration step turns the netlist into primitive elements with parasitics expanded, using hidden internal nodes for series parasitics; the DC analysis, the transient analysis and the SPICE exporter all consume it. The transient analysis uses trapezoidal integration with backward Euler after breakpoints, SPICE-style truncation-error timestep control, step halving on Newton failure, breakpoints at waveform corners, and starts from the DC operating point unless asked to start from zero. Voltage and current sources accept PULSE, SINE, EXP and PWL functions with ngspice semantics, written in a text syntax that uses the game's quantity notation and saved as structured exact numbers. Values can be edited inline on the schematic. Transient analyses run on a background task with progress and cancellation. Analysis settings are saved in the schematic file.

## Consequences
Every new parasitic or device must be added to the elaboration step only once. Transient results are verified against ngspice time series with justified tolerances. Waveform features without an ngspice equivalent refuse the SPICE export rather than exporting something different.
