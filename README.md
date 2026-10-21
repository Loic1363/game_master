# Nodal

Nodal is a Windows PC sandbox game about building electronics from the ground up. Players place
real components — resistors, capacitors, diodes, MOSFETs, BJTs, sources — wire them on a
schematic, and watch real simulated waveforms on in-game instruments. Validated circuits are
saved as reusable chips, and the long arc of the game is building and programming your own CPU
in assembly.

## Prerequisites

- Windows 11.
- .NET SDK 10.0.402 or newer, able to target `net10.0`. Verified with 10.0.402.
- Godot 4.7.2 **.NET** editor on `PATH` or reachable through a `GODOT` environment variable. The
  `Godot.NET.Sdk` version in `game/Nodal.Game.csproj` and `config/features` in
  `game/project.godot` must match the installed editor.

## Build, test, run

```
dotnet build Nodal.sln
dotnet test Nodal.sln
godot --path game
```

`dotnet test` runs on Microsoft.Testing.Platform, selected by `global.json`.

The game can also be run by opening `game/` in the Godot editor and pressing F5.

## Documentation

`CLAUDE.md` holds the working rules, `docs/architecture.md` the layers, `docs/decisions/` the
architecture decision records, `docs/specs/` the task specs and `docs/reports/` the reports.
