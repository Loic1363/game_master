# ADR-0009: Code conventions for namespaces, errors and invariants

Status: Accepted

## Context
Report 0003 raised recurring questions: how to organize namespaces as Foundation grows, how to report invalid input, and whether value types may hold invalid states.

## Decision
Namespaces mirror folders: each feature area lives in its own folder and namespace (for example Nodal.Foundation.Quantities). Input that comes from players or data files is validated without exceptions, through Try-patterns or result objects carrying readable diagnostics. Invalid arguments passed by code are programming errors and throw ArgumentException or a subclass. Value types enforce their invariants in their constructors, so an invalid instance can never exist.

## Consequences
Every player-facing or file-facing entry point needs a non-throwing API. Exceptions in logs always indicate a bug, never bad user input. Tests must cover both paths.
