# ADR-0018: Simulation foundations and the DC operating point

Status: Accepted

## Context
The simulator is the core of the game and must be correct, explainable and comparable with SPICE. It starts with the DC operating point of linear circuits before nonlinear devices and time-domain analysis.

## Decision
Simulation lives in Nodal.Simulation, a pure .NET project consuming the netlist. Analyses use Modified Nodal Analysis with node voltages and voltage-source branch currents as unknowns, solved by a hand-written dense LU decomposition with partial pivoting. Nodes with no DC path to ground are detected structurally, reported as warnings, and receive a gmin of 1e-12 S to ground so they remain solvable; all other nodes are solved without gmin, so linear results are exact. Unset optional parameters are ideal. Currents follow the passive sign convention of SPICE: the current enters the first pin, and power is positive when absorbed. Our DC current source drives current out of its plus terminal, the opposite of SPICE's I element, so exports must flip it. Unsupported components refuse the analysis with a clear message. Ratings produce non-blocking warnings. The DC analysis runs on the UI thread; long analyses will move to a worker thread later.

## Consequences
Linear DC results are exact up to floating-point error and are tested against hand calculations. Nonlinear devices will add Newton-Raphson iteration on top of the same matrix assembly. A sparse solver may replace the dense one when circuits grow.
