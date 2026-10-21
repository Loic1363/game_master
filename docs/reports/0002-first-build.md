# Report 0002 — First real build: verify, align, fix

Status: Done. Build, tests and headless run all pass on the real toolchain.

## Environment

- .NET SDK: `10.0.402` (the only SDK installed). Runtimes present: 6.0.36, 8.0.29, 10.0.10, 10.0.13.
- Godot: `4.7.2.stable.mono.official.ed1daf0bf`, via `GODOT` =
  `C:\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`.
- `Godot.NET.Sdk/4.7.2` and `config/features=PackedStringArray("4.7", ...)` already matched the
  installed editor. No change was needed there.

## Results

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**, all 6 projects, on `net10.0`.
- `dotnet test Nodal.sln`: **17 tests, 17 passed, 0 failed, 0 skipped**.
- Godot headless import: exit 0. Headless run printed exactly `Nodal — development build` and
  `Version 0.0.1`.
- The `net8.0` fallback was **not** needed: Godot 4.7.2 builds and runs the game on `net10.0`.

## Commands

```
dotnet build Nodal.sln
dotnet test Nodal.sln
& $env:GODOT --headless --path game --import
& $env:GODOT --headless --path game --quit-after 10
```

## Changes

- `Directory.Build.props`: `NodalTargetFramework` `net8.0` → `net10.0`.
- Both test projects: `xunit` 2.9.3 → `xunit.v3` 4.0.2, `xunit.runner.visualstudio` 2.8.2 → 4.0.1,
  `Microsoft.NET.Test.Sdk` 17.12.0 → 18.11.0 (latest stable, checked with `dotnet package search`),
  plus `<OutputType>Exe</OutputType>`, which xunit v3 requires.
- `global.json`: new, selecting the Microsoft.Testing.Platform runner for `dotnet test`.
- `tests/Nodal.Architecture.Tests/ProjectFile.cs:22`: `'\'` → `'\\'`.
- `game/Nodal.Game.csproj`: `$(NodalTargetFramework)` → literal `net10.0`.
- `game/scripts/Presentation/MainScreen.cs`: `GD.Print` now prints the label text itself.
- `.gitignore`: added `*.csproj.old`.
- Docs: ADR-0007 added, README prerequisites updated, CLAUDE.md rule 9 added, spec saved.

## Failures found and fixed

1. **`ProjectFile.cs` did not compile.** `include.Replace('\', …)` was an unterminated character
   literal — `CS1010`, `CS1012`, `CS1026`, `CS1002`. This was the only genuine source defect from
   task 0001; it was invisible without an SDK. Fixed by escaping the backslash.
2. **`dotnet test` refused to run.** `error : Testing with VSTest target is no longer supported by
   Microsoft.Testing.Platform on .NET 10 SDK and later.` xunit v3 runs on
   Microsoft.Testing.Platform, which the .NET 10 SDK only drives when opted in. The per-project
   `TestingPlatformDotnetTestSupport` property had no effect; `global.json` with
   `"test": { "runner": "Microsoft.Testing.Platform" }` fixed it, so the ineffective property was
   removed again.
3. **Godot silently downgraded the game project to `net8.0`.** On every `--import`, the editor
   backed up `game/Nodal.Game.csproj` as `.csproj.old` and rewrote
   `<TargetFramework>$(NodalTargetFramework)</TargetFramework>` to `<TargetFramework>net8.0
   </TargetFramework>`. It does not evaluate MSBuild properties, reads the raw string as an
   invalid moniker, and "repairs" it to its own default. Reproduced twice. With a literal
   `net10.0` the editor leaves the file untouched (verified: no rewrite, no `.old` backup).

## Deviation

Acceptance asked for `NodalTargetFramework` to drive every project **including the game**. The
game project now declares `net10.0` literally instead, because the property reference does not
survive a Godot import (failure 3). This is the conservative option: the alternative is a project
that reverts to `net8.0` behind the project lead's back and then fails to reference the `net10.0`
libraries. The cost is that the framework lives in two places. Recorded in ADR-0007.

## Tool-generated files

- `game/.godot/` — editor cache, already excluded by `.gitignore`.
- `game/Nodal.Game.csproj.old` — Godot's backup of the rewritten csproj. Deleted, and
  `*.csproj.old` added to `.gitignore`.
- `game/scripts/Presentation/MainScreen.cs.uid` — Godot script identity file containing
  `uid://bjexfuqh7k6ln`. Not a comment, and Godot expects it to be versioned, so it is left in
  place and not ignored.
- `game/project.godot` was **not** rewritten; Godot added no header to it.

No comments exist in any hand-written code or config file. A search for `//`, `/*`, `*/` and
`<!--` across all `.cs`, `.csproj`, `.props`, `.tscn`, `.godot`, `.json` and `.sln` files matches
only `res://` and `uid://` URIs and the `.vscode/*` gitignore glob.

No Git command was run; the working directory still contains no `.git` directory.

## Proposals for later specs

1. Add an architecture test asserting that `game/Nodal.Game.csproj` targets the same framework as
   `NodalTargetFramework`, so the two-place duplication cannot drift unnoticed.
2. `.gitignore` excludes `*.import`. That is Godot 3 guidance; Godot 4 expects `.import` sidecars
   to be versioned. Harmless today (no imported assets yet), but it will bite once `game/assets/`
   holds real files.
3. Consider pinning the SDK with an `sdk` section in the new `global.json` once the toolchain is
   stable.

## Open questions

None.
