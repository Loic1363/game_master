# ADR-0002 — Godot 4 .NET with C#

Status: Accepted

## Context

Nodal is a tool-like 2D application: a schematic canvas, panels, dialogs and instrument
displays, with a numerical simulation behind them. It targets Windows PC and must stay free of
licensing risk.

## Decision

Build on Godot 4 .NET and write the game in C#. Godot's `Control` node system suits tool-like
2D interfaces, it is MIT licensed with no royalties or license review, and C# is a language the
project lead already knows.

## Consequences

- The core stays plain .NET (ADR-0001), so the engine choice is reversible.
- C# garbage collection must be respected in the simulation hot path: pre-allocated buffers,
  no per-step allocation.
- If profiling later shows the solver is the bottleneck, it can move to native code behind the
  same interface without touching the rest of the project.
