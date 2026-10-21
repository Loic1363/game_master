# Task 0001 — Repository bootstrap

## Who you are working with

You are the implementer on **Nodal** (working codename), a Windows PC sandbox game about building electronics from the ground up. Players place real components (resistors, capacitors, diodes, MOSFETs, BJTs, sources), wire them on a schematic and observe real simulated waveforms with in-game instruments. They save validated circuits as reusable "chips", and eventually build and program their own CPU in assembly.

There are three roles on this project:

- **Project lead** (human, electronics engineer): final say on everything. Handles **all** Git and GitHub operations.
- **Architect** (another AI assistant with the full project context): writes your task specs, reviews your reports and decides next steps.
- **You**: implement exactly what the spec asks, verify it, and report back.

You receive one task per prompt. Each task ends with a written report in the format at the bottom of this prompt.

## Ground rules (permanent, apply to every task)

1. **No Git, ever.** Do not run any `git` command: no init, status, add, commit, push, branch, checkout or stash. Do not create Git hooks. The project lead handles version control.
2. **No comments in code, ever.**
   - No `//`, `/* */` or `///` XML doc comments in C#.
   - No `<!-- -->` in .csproj, .props or .tscn files.
   - No `#` or `;` comments in config files you write by hand.
   - Code must explain itself through naming and structure.
   - Logging and print statements are allowed.
   - Markdown documentation is prose, not code, so it is fine.
   - Comment headers that a tool generates on its own (for example Godot's `project.godot` header) are tolerated: list them in the report.
3. **Stay in scope.** Implement what the spec asks, nothing more. If you see something worth doing, propose it in the report instead of doing it.
4. **Don't guess on architecture.** If the spec is ambiguous or conflicts with reality (tool versions, APIs), choose the most conservative option and record it under "Deviations". If the choice would affect the architecture, stop and ask in the report.
5. **No new dependencies** (NuGet packages, Godot addons, tools) beyond those named in the spec. Propose them in the report instead.
6. **English everywhere**: code, docs, reports and all player-facing text. The game is English-only for now; localization will come later and is out of scope until a spec says otherwise.
7. **Verify before reporting.** Build, run the tests and run the game where applicable. Report real results, including failures.
8. **Never install system-wide software.** If something is missing, state exactly what the project lead must install.

## Step 1 — Environment check

Detect and record:

- the installed .NET SDK versions;
- whether a Godot 4 **.NET** editor is available (executable on PATH or a `GODOT` environment variable), and its exact version;
- the .NET target framework required by that Godot version.

Use that target framework for **all** projects. If Godot .NET is not installed, do everything else, mark the Godot verification as pending, and give exact install instructions in the report.

## Step 2 — Repository layout

Create exactly this structure. The annotations below describe each entry; they must not appear in the files themselves.

```
nodal/
├── CLAUDE.md
├── README.md
├── .gitignore                    .NET, Godot 4, VS Code, Windows junk; keep .vscode/extensions.json and settings.json tracked
├── .gitattributes                LF normalization for text; binary for images, fonts, audio
├── .editorconfig                 standard C# conventions, 4-space indent, LF line endings
├── Directory.Build.props         Nullable=enable, ImplicitUsings=enable, LangVersion=latest, Version=0.0.1,
│                                 TreatWarningsAsErrors=true for everything under src/ and tests/
├── Nodal.sln                     contains every project below, including the Godot one
├── .vscode/
│   ├── extensions.json           recommends C# Dev Kit and godot-tools
│   └── settings.json             minimal
├── docs/
│   ├── architecture.md
│   ├── decisions/                ADRs, see Step 6
│   ├── specs/
│   │   └── 0001-bootstrap.md     this prompt, verbatim
│   └── reports/
│       └── 0001-bootstrap.md     your final report
├── src/
│   ├── Nodal.Foundation/
│   ├── Nodal.Content/
│   └── Nodal.Circuit/
├── game/
│   ├── project.godot
│   ├── Nodal.Game.csproj
│   ├── scenes/
│   ├── scripts/
│   │   ├── Presentation/
│   │   ├── Rendering/
│   │   ├── Input/
│   │   └── Adapters/
│   ├── content/
│   │   ├── components/
│   │   ├── symbols/
│   │   └── challenges/
│   └── assets/
└── tests/
    ├── Nodal.Foundation.Tests/
    └── Nodal.Architecture.Tests/
```

Use `.gitkeep` files only for folders that would otherwise be empty.

Do **not** create these yet: `Nodal.Simulation`, `Nodal.Characterization`, `Nodal.Assembly`, `Nodal.Gameplay`, `Nodal.Circuit.Tests`, `tests/reference-circuits`, `tools/`, `game/localization`.

## Step 3 — .NET projects and layering

All projects in `src/` are plain class libraries with **no reference to Godot**.

| Project | May reference | Content for this task |
|---|---|---|
| `Nodal.Foundation` | nothing | a public static class `NodalInfo` with `Name` ("Nodal") and `Version` (read from the assembly informational version, so it follows `Directory.Build.props`) |
| `Nodal.Content` | Foundation | no types yet |
| `Nodal.Circuit` | Foundation, Content | no types yet |

Tests use **xUnit**. Allowed packages: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, each at the latest stable version.

- `Nodal.Foundation.Tests`: tests that `NodalInfo.Name` equals "Nodal" and that `NodalInfo.Version` matches the version from `Directory.Build.props`.
- `Nodal.Architecture.Tests`: tests that enforce the layer rule.
  - No assembly built from `src/` references any Godot assembly.
  - `Nodal.Foundation` references no other Nodal assembly.
  - `Nodal.Content` references only `Nodal.Foundation` among Nodal assemblies.
  - `Nodal.Circuit` references only `Nodal.Foundation` and `Nodal.Content` among Nodal assemblies.
  - Check both the compiled assemblies (reflection) and the `.csproj` files (ProjectReference and PackageReference), since unused references disappear from compiled metadata.
  - Avoid adding marker types to production code to make these tests work.

## Step 4 — Godot project

- **Project:** Godot 4 .NET, using the `Godot.NET.Sdk` version that matches the installed editor. `Nodal.Game.csproj` references `Nodal.Circuit`.
- **Window:** title "Nodal", default size 1600×900, resizable. Enable low processor usage mode.
- **Main scene:** `scenes/Main.tscn`, set as the run scene.
  - A full-rect `Control` root.
  - A full-rect `ColorRect` background in pearl off-white `#F7F5F0`.
  - A centered `Label` in dark gray showing: `Nodal — development build` on the first line and `Version X.Y.Z` on the second.
- **Script:** the label text is set by `scripts/Presentation/MainScreen.cs`, which reads `NodalInfo` from `Nodal.Foundation`. This proves the game-to-core reference works.
- **Warnings:** try to keep `TreatWarningsAsErrors` on for the game project too. If Godot's source generators make that impossible, disable it for the game project only and explain why.

## Step 5 — VS Code

`extensions.json` and a minimal `settings.json` only. No launch configurations in this task.

## Step 6 — Documentation

### CLAUDE.md

Write it with exactly this content:

--- BEGIN CLAUDE.md ---
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
--- END CLAUDE.md ---

### docs/architecture.md

Concise, under about 100 lines. Write it from these points:

- **Layers**, bottom to top, each with its responsibility (as in CLAUDE.md), plus the dependency rule.
- **Principles:**
  - the core under `src/` never references Godot;
  - the schematic is the source of truth and is compiled into a netlist, the only input to the simulator (as in KiCad, where the netlist carries the schematic between tools);
  - the simulation will run on its own thread, receiving commands and publishing snapshots and waveforms;
  - every editor action is an undoable command;
  - files are versioned, Git-friendly text (JSON);
  - components are defined as data, not code.
- **Future simulation loop**, following Glenn Fiedler's "Fix Your Timestep!":
  - the UI produces time and the simulation consumes it in steps;
  - a per-frame budget avoids the "spiral of death", so the simulation slows down instead of freezing the UI;
  - simulated time is decoupled from real time (pause, slow motion, single step).
- **Patterns** (Robert Nystrom, *Game Programming Patterns*): Command (undo/redo, replay), Observer and Event Queue (simulation to UI), Type Object (data-defined component types), Dirty Flag (recompile only what changed).
- **References**: Jason Gregory, *Game Engine Architecture*; Robert Nystrom, *Game Programming Patterns*; Glenn Fiedler, "Fix Your Timestep!"; KiCad documentation.

### docs/decisions/

One file per decision, status "Accepted", sections Context / Decision / Consequences, each file under about 25 lines:

- `ADR-0001-layered-architecture.md`: layers and the dependency rule, enforced by architecture tests.
- `ADR-0002-godot-dotnet-csharp.md`: Godot 4 .NET with C#. Its UI system suits tool-like 2D apps, it is free with no license risk, and C# is known to the project lead. Hot paths can move to native code later if profiling demands it.
- `ADR-0003-schematic-to-netlist.md`: the schematic is the source of truth; the simulator only sees a compiled netlist.
- `ADR-0004-data-driven-content.md`: components, symbols, chips and challenges are versioned JSON data.
- `ADR-0005-git-owned-by-project-lead.md`: the implementer never runs Git.
- `ADR-0006-no-code-comments-english-only.md`: no comments in code; English-only game for now, localization deferred.

### README.md

Under 30 lines: what the project is (one paragraph), prerequisites (exact versions from Step 1), and how to build, test and run.

## Acceptance criteria

- [ ] `dotnet build Nodal.sln` succeeds with 0 warnings and 0 errors.
- [ ] `dotnet test` passes, with the number of tests reported.
- [ ] The Godot project imports and runs; the main scene shows both lines with the correct version. If you can verify headlessly, do so and say how. Otherwise mark it as pending for the project lead to check visually.
- [ ] A search of every hand-written file you created for comment syntax (`//`, `/*`, `///`, `<!--`, and `#` or `;` comment lines in config files) finds nothing except tool-generated headers, which are listed.
- [ ] No Git command was run.
- [ ] This prompt is saved verbatim at `docs/specs/0001-bootstrap.md`.
- [ ] The report is saved at `docs/reports/0001-bootstrap.md` and printed in full.

## Out of scope

Schematic editor, components, simulation, content loading, localization, gameplay, CI, launch configurations.

## Report format (use this for every task)

```
# Report — Task NNNN: <title>

## 1. Summary
3–5 sentences.

## 2. Environment
Detected tool versions.

## 3. Work done
By step, briefly.

## 4. Files created or changed
Tree view.

## 5. Verification
Commands run and their results (build output summary, test counts, run results).

## 6. Acceptance criteria
Each criterion: PASS / FAIL / PENDING, with a one-line note.

## 7. Deviations from the spec
What, and why.

## 8. Open questions for the architect

## 9. Manual actions for the project lead
Things to install, check visually, or do in the Godot editor.

## 10. Suggested commit message
Conventional Commits format.

## 11. Proposed next step
```
