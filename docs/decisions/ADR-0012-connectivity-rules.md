# ADR-0012: Connectivity rules and net naming

Status: Accepted

## Context
The netlist is the only input to the simulator (ADR-0003), so connectivity rules must be unambiguous, match the habits of electronics engineers, and be deterministic. Ground must be expressible as data, not as a special case in code.

## Decision
Connection points are wire endpoints and pin positions. A connection point connects to every wire containing it and to every other connection point at the same position. Crossing wire interiors never connect. A pin may declare an optional globalNet in its definition; all pins with the same globalNet belong to the same net. The ground pin declares globalNet "0", following SPICE. Nets carrying a global net take its name; others are named N1, N2 and so on, by smallest pin reference, then by smallest wire id for wire-only nets. A point of degree three or more is a junction and gets a dot. Floating pins, dangling wire ends and a missing ground are warnings; a short between two global nets and an unknown definition are errors.

## Consequences
The component schema gains an optional, backward-compatible globalNet field on pins, which also enables future power symbols such as VCC. Net names can change when the schematic changes, so nothing persistent may refer to an N-name; persistent references use pin references. A spatial index may be needed later for very large schematics.
