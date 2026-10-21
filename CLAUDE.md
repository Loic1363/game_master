# Nodal — rules for the implementer

## Roles
- Project lead (human): final decisions, all Git and GitHub operations.
- Architect (AI): writes task specs in docs/specs/, reviews reports.
- Implementer (you): implements one spec at a time, verifies, reports.

## Permanent rules
1. Never run any git command. Never create Git hooks.
2. Never write comments in code: no //, /* */, /// in C#; no <!-- --> in .csproj, .props or .tscn; no comments in hand-written config files. Logging and prints are allowed. Code explains itself through naming and structure.
3. Implement only what the current spec asks. Propose extras in the report.
4. Do not add dependencies without the spec allowing them.
5. If a spec is ambiguous or wrong, take the most conservative option and report it; stop and ask when architecture is affected.
6. English for code, docs and reports. The game is English-only until a spec says otherwise.
7. Build, test and run before reporting. Report real results.
8. Never install system-wide software.
9. Keep reports concise, about one screen. Give details only for failures, deviations and open questions.
10. Never create or edit ADRs on your own initiative. ADRs are dictated by the architect in specs, and you record them verbatim. If you had to make a significant choice the spec did not cover (architecture, dependencies, file formats, frameworks, interfaces between layers, simulation algorithms), report it under "Deviations" so the architect can record it.

## Architecture
Layers, bottom to top. A layer may only depend on layers below it.
- Platform: Godot 4, .NET, Windows.
- Foundation (src/Nodal.Foundation): logging, units, math, IDs, events, profiling.
- Content (src/Nodal.Content): loading and validating versioned data files.
- Circuit (src/Nodal.Circuit): schematic model, undoable commands, schematic-to-netlist compilation.
- Simulation, Characterization, Assembly (later).
- Gameplay (later): game flow, objectives, progression, scoring.
- Presentation (game/): Godot scenes and scripts only; adapters call into the core.

Everything under src/ is pure .NET with no Godot reference. tests/Nodal.Architecture.Tests enforces this.

## Conventions
- C# latest, nullable enabled, warnings are errors.
- Small types, small methods, intention-revealing names.
- xUnit for tests. Every public behavior in src/ gets tests.
- Data files are versioned JSON.
- Every editor action is an undoable command.

## Workflow
1. Read the spec in docs/specs/.
2. Implement, then build, test and run.
3. Write the report in docs/reports/ using the same number as the spec, and print it in full at the end of your answer.
