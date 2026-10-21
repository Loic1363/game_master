# ADR-0004 — Components, symbols, chips and challenges are versioned JSON

Status: Accepted

## Context

The game needs many component families, schematic symbols, player-saved chips and authored
challenges. Expressing each of them as a C# class would make every content change a code
change and a rebuild, and would make player-created content impossible.

## Decision

Define content as data files in JSON, each carrying an explicit schema version. Code provides
the behaviors that data refers to; data provides the catalogue. `src/Nodal.Content` loads and
validates these files and rejects anything malformed with a readable error.

## Consequences

- Adding or tuning a component is a data edit, reviewable as a text diff.
- Loading must validate, because data is now an untrusted input, including player-made content.
- Schema versions must be honored, with migrations when a format changes, so old saves and old
  chips keep working.
