# ADR-0023: Untrusted content

Status: Accepted

## Context
Players will share schematics and possibly other content. A file from a stranger must never endanger the player's computer, as happened with remote code execution flaws in other games.

## Decision
Every file the game opens is untrusted data. Files never contain executable code; mods, if any, are data only. Readers are strict, bounded and never throw for bad input, and they never follow arbitrary file paths taken from a file's content. Player programs only ever run inside the simulated CPU, never as native code. Analyses that can run long are cancellable and bounded.

## Consequences
Each feature that imports external content (file sharing, mods, external PWL files, online services) requires its own security ADR and a security review before implementation.
