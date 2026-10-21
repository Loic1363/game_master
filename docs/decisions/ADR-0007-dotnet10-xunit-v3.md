# ADR-0007 — Target .NET 10 and test with xunit v3

Status: Accepted

## Context

Task 0001 left the target framework and the test framework open because no SDK was installed and
nothing could be verified. .NET 8 reaches end of support on 2026-11-10, while .NET 10 is LTS until
November 2028. The installed toolchain is .NET SDK 10.0.402 and the Godot 4.7.2 .NET editor, whose
bundled `Godot.NET.Sdk` still defaults to `net8.0`.

## Decision

All projects target `net10.0`, driven by the `NodalTargetFramework` property in
`Directory.Build.props`. The Godot game project is the one exception to that indirection: it
declares `net10.0` literally, because the Godot editor rewrites any `TargetFramework` it cannot
parse as a framework moniker back to its own `net8.0` default. Both test projects use `xunit.v3`
instead of `xunit`. xunit v3 test projects are executables and run on Microsoft.Testing.Platform,
which the .NET 10 SDK only drives when `global.json` selects that runner.

## Consequences

- Godot 4.7.2 builds and runs the game on `net10.0`, so the `net8.0` fallback was not needed.
- The game project's framework must be changed in two places, so it can drift from the libraries;
  a build break is the symptom, since a game on an older framework cannot reference them.
- `global.json` is now part of the build contract: without its `test.runner` entry, `dotnet test`
  fails before running anything.
- Test assemblies are executables and can be launched directly, which also makes them runnable
  without a VSTest host.
