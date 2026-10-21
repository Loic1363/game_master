# ADR-0011: Immutable schematic model with snapshot undo

Status: Accepted

## Context
Every editor action must be undoable (architecture principle). The simulation will later run on its own thread and needs consistent views of the schematic. Tests need deterministic behavior.

## Decision
The schematic and all its parts are immutable values built on System.Collections.Immutable. Every edit is a command object (Command pattern) mapping a schematic to a new schematic, or returning an error without throwing for player input. The edit history stores previous snapshots, so undo restores a snapshot instead of inverting a command. Identifiers are sequential integers per schematic and are never reused. Geometry uses an integer grid with y pointing down; an orientation is an optional horizontal mirror followed by a clockwise rotation in quarter turns. Wires are horizontal or vertical segments; a bend is two wires. Designators take the smallest free number for their prefix.

## Consequences
Undo is correct by construction. Memory grows with history; structural sharing keeps this small, and a cap can be added later. The simulation can read a snapshot without locking. Commands must stay pure. Diagonal wires are not supported.
