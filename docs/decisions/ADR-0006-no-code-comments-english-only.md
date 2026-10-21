# ADR-0006 — No comments in code, and English only for now

Status: Accepted

## Context

Comments drift away from the code they describe and are often used to excuse unclear naming.
Separately, the project must decide early whether player-facing text goes through a localization
layer, because retrofitting one is costly but building one too early is wasted work on a game
with no players yet.

## Decision

Write no comments in code: no `//`, `/* */` or `///` in C#, no XML markup comments in `.csproj`,
`.props` or `.tscn`, and no comments in hand-written configuration files. Intent is carried by
names, small methods and types. Logging and print statements remain allowed, and Markdown
documentation is prose, so it is unaffected. The game is English-only for now; all code, docs,
reports and player-facing text are in English, and localization is deferred until a spec
introduces it.

## Consequences

- Explanations that do not fit in a name belong in `docs/`, in an ADR or in a report.
- Comment headers that a tool generates on its own are tolerated and listed in the task report.
- Player-facing strings are written inline today, so introducing localization later will mean a
  deliberate extraction pass.
