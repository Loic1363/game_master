# Task 0002 — First real build: verify, align, fix

## Context

Task 0001 produced the repository but nothing could be compiled: no .NET SDK and no Godot were installed. The project lead has now installed the .NET 10 SDK and the Godot 4.7.2 .NET editor. This task makes the repository build, test and run for real. No new features.

All ground rules in CLAUDE.md still apply, including: no Git commands, no comments in code, no new dependencies beyond those named here.

## Architect decisions on the open questions from report 0001

1. **Target framework: `net10.0`.** .NET 8 reaches end of support on 2026-11-10; .NET 10 is LTS until November 2028. Set `NodalTargetFramework` to `net10.0` for all projects, including the Godot game project.
   - If the Godot 4.7.2 editor cannot build or run the game project on `net10.0`, revert the whole solution to `net8.0` (the game project must not target an older framework than the libraries it references).
   - In that case, report exactly what failed, with error messages.
2. **Test framework: xunit v3.** Replace `xunit` with `xunit.v3` in both test projects. Use the latest stable versions compatible with each other of `xunit.v3`, `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk`, verified with `dotnet package search` now that an SDK exists. Apply whatever project settings xunit v3 requires.
3. **Repository root folder name:** keep the current folder as is. No change needed.

## Step 1 — Environment check

Record:

- the installed .NET SDK versions;
- the exact Godot version reported by `godot --version` (or via the `GODOT` environment variable).

`Godot.NET.Sdk` in `game/Nodal.Game.csproj` and `config/features` in `game/project.godot` must match the installed editor version exactly. Fix them if needed.

## Step 2 — Build and test

- `dotnet build Nodal.sln` must succeed with **0 warnings and 0 errors**.
- `dotnet test` must pass all tests. Report the count.
- Fix whatever the first real compilation surfaces. Keep fixes minimal and within the existing design. If a fix would change the architecture, stop and ask.

## Step 3 — Godot verification

1. Run the Godot editor headlessly once on `game/` so it performs its first import and generates its files. Godot may rewrite `project.godot` with its own header; that is the tolerated tool-generated case.
2. Make sure `.gitignore` excludes Godot's generated cache folder (`.godot/`) and any other build output, and that nothing generated is left that should not be versioned.
3. In `MainScreen._Ready`, after setting the label, print the displayed text with `GD.Print`. Prints are allowed; comments are not.
4. Run the main scene headlessly for a few frames and quit. Confirm in the output that the printed text reads `Nodal — development build` and `Version 0.0.1`.
5. Report the exact commands used, so the project lead can repeat them.

The project lead will then do the visual check: open the project in the Godot editor, press F5, and expect a 1600×900 window titled "Nodal" with the two centered lines on a pearl off-white background.

## Step 4 — Documentation updates

- Add `docs/decisions/ADR-0007-dotnet10-xunit-v3.md` (Context / Decision / Consequences, under about 25 lines) recording both decisions above, or the fallback to `net8.0` if it happened.
- Update README prerequisites to the real versions.
- Add this rule to the "Permanent rules" list in CLAUDE.md, as rule 9:
  `9. Keep reports concise, about one screen. Give details only for failures, deviations and open questions.`
- Save this prompt verbatim at `docs/specs/0002-first-build.md`.

## Acceptance criteria

- [ ] `dotnet build Nodal.sln`: 0 warnings, 0 errors.
- [ ] `dotnet test`: all tests pass, with the count reported.
- [ ] Godot headless import succeeds and the headless run prints the two expected lines.
- [ ] Framework and Godot SDK versions are aligned with the installed tools.
- [ ] No comments in any hand-written code or config file. Only tool-generated headers are tolerated, and they must be listed.
- [ ] No Git command was run.
- [ ] Spec saved at `docs/specs/0002-first-build.md`; report saved at `docs/reports/0002-first-build.md` and printed in full.

## Out of scope

Logging, unit types, any new feature, CI, launch configurations.

## Report

Same format as before, now concise per rule 9.
