# Report 0003 — Housekeeping and physical quantities

Status: Done. All four housekeeping items applied, `Quantity` and `ESeries` implemented and tested.

## Results

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**, 6 projects, `net10.0`.
- `dotnet test Nodal.sln`: **223 tests, 223 passed, 0 failed, 0 skipped** (was 17).
  Nodal.Architecture.Tests 16 (was 15), Nodal.Foundation.Tests 207 (was 2).
- Headless Godot run printed exactly `Nodal — development build` and `Version 0.0.1`, exit 0.
  Import left `game/Nodal.Game.csproj` untouched, no `.csproj.old`.
- No comments in any hand-written file; no Git command was run; no new dependency.

## Commands

```
dotnet build Nodal.sln
dotnet test Nodal.sln
& $env:GODOT --headless --path game --import
& $env:GODOT --headless --path game --quit-after 10
```

## Changes

Part A: CLAUDE.md rule 10 added verbatim; `*.import` removed from `.gitignore` (`.godot/` kept);
`global.json` gained `"sdk": { "version": "10.0.400", "rollForward": "latestFeature" }`;
`GameProjectFrameworkTests` added, with `ProjectFile.PropertyValue` and two new `RepositoryPaths` members.

Part B, all in `src/Nodal.Foundation/Quantities/`: `Unit`, `UnitSymbols`, `Quantity`,
`QuantityParseError`, `QuantityParser`, `QuantityFormatter`, `MetricPrefixes`, `UnitSymbolSuffix`,
`StandardSeries`, `ESeries`.

Part C: six test files in `tests/Nodal.Foundation.Tests/Quantities/`, all table-driven except the
E-series invariants. Round-trip covers 336 values per unit (28 decades × 12 mantissas × 9 units).

Part D: `docs/decisions/ADR-0008-quantity-notation.md` recorded verbatim.

## Deviations

Significant choices the spec did not cover, per rule 10:

1. **New namespace `Nodal.Foundation.Quantities`** in a `Quantities/` folder, rather than the flat
   `Nodal.Foundation` that `NodalInfo` uses. Foundation is meant to hold logging, math, IDs, events
   and profiling as well, so a flat namespace would not survive. Affects every later `using`.
2. **The series selector is a separate enum, `StandardSeries`.** `ESeries.Contains(series, value)`
   requires `ESeries` to be the static class, so the enum could not also be called `ESeries`.
   Call sites read `ESeries.Nearest(StandardSeries.E24, value)`.
3. **E-series invalid input throws `ArgumentOutOfRangeException`** (non-finite or non-positive), applied
   to all five operations. Rationale: these arguments come from code, not from players, unlike
   `Quantity.TryParse`. `ValuesInRange(min, max)` with `min > max` returns an empty list rather than
   throwing, since an empty range is a meaningful answer.
4. **Equality operators `==` and `!=` on `Quantity`.** Not arithmetic, and `IEquatable` without them is
   a trap on a value type. No other operator was added.
5. **Parse errors for rejections the spec listed without naming an error:** `4k7k`, `1.2.3`, `k`,
   `--1`, two inner spaces and a bare unit symbol give `MalformedNumber`; a thousands separator
   (`1 234`, `1,234.5`) gives `MalformedNumber`; NaN, infinity and overflow such as `1e400` give
   `OutOfRange`. `UnknownPrefix` is returned when a stray letter sits where a prefix would
   (`4.7q`, and `2R2` when ohms are not expected, since `R` is resistance-only).
6. **Values outside the f…T prefix range clamp the prefix and let the mantissa leave [1, 1000):**
   1e15 V formats as `1000 TV`, 1e-18 F as `0.001 fF`. Both still round-trip. The alternative,
   scientific notation, would not parse back.
7. **`new Quantity(double.NaN, …)` is not rejected.** The spec only constrains parsing, so the
   constructor stores what it is given; `ToString` then yields `NaN`/`Infinity` with no prefix.
8. **Only `u` and `µ` (U+00B5) are accepted for micro**, as written in the spec. Greek mu (U+03BC),
   which Word and Greek keyboards produce, is rejected as an unknown prefix.
9. **`R` as the resistance decimal marker is uppercase-only.** The spec makes only the ohm *symbol*
   case-insensitive, so `2r2` is rejected.

## Proposals for later specs

- Accept U+03BC and lowercase `r` (items 8 and 9) if player testing shows they are typed.
- `Quantity` arithmetic, tolerance bands, and E48/E96, when characterization needs them.
- `global.json` pins the band floor `10.0.400`, not the exact `10.0.402`; a machine with only an
  older 10.0 band installed will now fail to build, which is the intent of pinning.

## Open questions

None.
