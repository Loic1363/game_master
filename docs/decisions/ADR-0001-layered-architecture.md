# ADR-0001 — Layered architecture with a one-way dependency rule

Status: Accepted

## Context

Nodal will grow into a schematic editor, a numerical simulator, a characterization pass, an
assembler and a game layer. Without an explicit rule, UI concerns leak into the solver and the
solver becomes impossible to test or profile on its own.

## Decision

Organize the code as layers: Foundation, Content, Circuit, then Simulation, Characterization,
Assembly, Gameplay, and finally Presentation in the Godot project. A layer may depend only on
layers below it. Every layer below Presentation lives under `src/` as a plain .NET class library.
`tests/Nodal.Architecture.Tests` enforces the rule automatically, checking both compiled
assembly metadata and the `.csproj` files, because an unused reference disappears from metadata.

## Consequences

- The solver and the circuit model can be tested headlessly and fast.
- A violation fails the build instead of being discovered months later.
- Shared helpers must be placed deliberately, usually in Foundation, rather than wherever
  they were first needed.
