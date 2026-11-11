# ADR-0024: Controlled sources, switches and push buttons

Status: Accepted

## Context
Behavioral op-amps, optocouplers, the future characterization bench and circuits that change state need ideal controlled sources and switches. The transient analysis is computed in one batch, so a player cannot press a button during a run.

## Decision
The four SPICE controlled sources are base components: voltage-controlled voltage and current sources, current-controlled current and voltage sources. They are ideal and linear, with infinite input impedance on a voltage control and zero on a current control. A current-controlled source senses the current through its own control pins, so it never refers to another component. Controlled current sources drive their current out of their plus terminal, as in ADR-0018. Gains are player parameters, like resistances. The voltage-controlled switch follows ngspice's SW model: a resistance that changes between on and off values at a threshold, with optional hysteresis and the same state semantics. The push button is a two-terminal switch whose state follows a press schedule set before the run: released, pressed, or a list of time intervals, stored as exact numbers and bounded. Its state changes exactly at the scheduled instants, which are breakpoints. Exports use SPICE E, F, G, H and S elements, with hidden sense and control sources where SPICE needs them. The schedule value does not change the schemaVersion, since only the new push-button type uses it. Pressing a button during a run requires a real-time simulation and is a later decision.

## Consequences
Behavioral models can be built from controlled sources. Switches are discontinuous, so transient accuracy around switching instants depends on timestep control, and references verify it against ngspice.
