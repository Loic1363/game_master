# ADR-0003 — The schematic is the source of truth, compiled into a netlist

Status: Accepted

## Context

The simulator needs nodes, branches and device models. The player edits symbols, pins and wires
on a canvas. Letting the solver read the editor model would couple numerical code to drawing
coordinates, selection state and undo history.

## Decision

The schematic is the single source of truth and the only thing saved. A compilation step turns
it into a netlist: nodes, elements and parameters, with no geometry. The netlist is the only
input the simulator ever sees. This follows KiCad, where the netlist is the artifact that
carries a schematic between tools.

## Consequences

- The solver can be tested from hand-written netlists, with no editor involved.
- Validation and error reporting (floating pins, shorted sources, no ground) belong to the
  compilation step and have one clear home.
- Recompilation cost is managed with a dirty flag so that editing one value does not rebuild
  the whole netlist.
