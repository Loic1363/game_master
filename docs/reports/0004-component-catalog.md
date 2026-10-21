# Report 0004 — Quantity fixes and the component catalog

Status: Done. All seven parts implemented, built, tested and run.

## Results

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**, 7 projects, `net10.0`.
- `dotnet test Nodal.sln`: **323 tests, 323 passed, 0 failed, 0 skipped** (was 223).
  Nodal.Foundation.Tests 227 (was 207), Nodal.Content.Tests 71 (new), Nodal.Architecture.Tests 25 (was 16).
- Headless Godot run printed exactly, after the two title lines,
  `Loaded 11 component definitions, 0 diagnostics`, exit 0. No `.csproj.old`, no stray `.import`.
- No comments in any hand-written file; no Git command was run; no new dependency
  (`System.Text.Json` ships with the base library).

## Commands

```
dotnet build Nodal.sln
dotnet test Nodal.sln
& $env:GODOT --headless --path game --import
& $env:GODOT --headless --path game --quit-after 10
```

## Changes

Part A, in `src/Nodal.Foundation/Quantities/`: `MetricPrefixes` maps U+03BC to micro alongside `u`
and U+00B5; `QuantityParser` accepts `r` as well as `R` as the resistance decimal marker;
`Quantity`'s constructor throws `ArgumentOutOfRangeException` for NaN and infinity. Tests added in
`QuantityParseTests` (`1μ`, `470μ`, `4μ7`, `4μ7F`, `2r2`, `r47`, `1r`, `2r2ohm`),
`QuantityParseRejectionTests` (`2r2` and `r47` outside ohms), `QuantityFormatTests` (micro formats as
U+00B5 only) and a new `QuantityInvariantTests`.

Parts B and C, in `src/Nodal.Content/`: `Sources/` holds `ContentFile`, `IContentSource` and
`DirectoryContentSource`; `Diagnostics/` holds `ContentDiagnosticSeverity` and `ContentDiagnostic`;
`Components/` holds the model (`ComponentCategory`, `PinDefinition`, `ParameterDefinition`,
`ComponentDefinition`, `ComponentCatalog`), the entry points (`ComponentCatalogLoader`,
`ComponentCatalogLoadResult`) and the internal validator (`ComponentDefinitionReader`,
`ComponentFields`, `ComponentVocabulary`, `ComponentNameRules`, `JsonPath`, `JsonValueKindNames`).
The reader walks a `JsonDocument` field by field, so every diagnostic carries a real JSON path.

Part D: the eleven files in `game/content/components/`, exactly as tabulated in the spec.

Part E: `game/scripts/Adapters/GodotContentSource.cs` reads `res://content/components` through
`DirAccess` and `Godot.FileAccess`; `MainScreen._Ready` loads the catalog and prints the summary
line plus one line per diagnostic. The label text is unchanged.

Part F: `tests/Nodal.Content.Tests` with `BaseCatalogTests` (the real files),
`InvalidComponentFileTests` (28 table-driven invalid cases plus duplicate id),
`ComponentCatalogTests`, `DirectoryContentSourceTests`, and the `InMemoryContentSource` and
`ComponentJson` helpers. `tests/Nodal.Architecture.Tests` gained `TestProjects` and
`TestProjectLayeringTests`, which assert that every test project declares no Godot reference, only
allowed Nodal project references, and the shared target-framework property.

Part G: `ADR-0009-code-conventions.md` and `ADR-0010-component-definitions.md` recorded verbatim.

## Deviations

Significant choices the spec did not cover, per rule 10:

1. **`parameters` is a required field that may hold an empty array**, not an optional field. The spec
   marks only `series` as optional, so the six components without parameters carry `"parameters": []`.
2. **Parameter names must match `[a-z][a-z0-9]*`.** "Lowercase identifiers" left the separator open;
   hyphens and underscores are rejected for now. All five names in the base catalog are plain letters.
3. **A missing content directory throws, it does not load empty.** `DirectoryContentSource.ReadAll`
   lets `DirectoryNotFoundException` escape. ADR-0009's no-throw rule covers invalid *content*; a
   missing directory is a deployment or call-site error and should be loud. The Godot adapter instead
   logs with `GD.PrintErr` and yields nothing, because `DirAccess.Open` returns null rather than
   throwing.
4. **`ContentDiagnosticSeverity` declares `Warning` as well as `Error`.** Nothing emits `Warning`
   yet; a one-value severity enum would be pointless. Any diagnostic excludes its file today.
5. **Malformed JSON reports a fixed English message**, `The file is not valid JSON.`, without the
   `JsonException` text, which .NET localizes to the OS language and would break rule 6.
6. **A rejected file can report several diagnostics**, one per broken rule, since the reader keeps
   reading after the first failure; it stops early only for an unsupported `schemaVersion`, a wrong
   `kind`, a non-object root and unparseable JSON, where nothing else can be trusted.
7. **A default value constrained to a series must be strictly positive.** `ESeries.Contains` throws
   for zero and negatives, so the reader checks the sign first and reports "does not belong to series".
8. **`ComponentCatalog` orders by id, ordinal**, and its constructor throws `ArgumentException` on a
   duplicate id, per ADR-0009. The loader never reaches that, since it reports duplicates itself.
9. **`game/Nodal.Game.csproj` now references `Nodal.Content` directly.** It compiled through the
   transitive reference from `Nodal.Circuit`, but the game uses Content types itself.
10. **`QuantityFormatter` keeps its non-finite branch.** It is unreachable through `Quantity` now that
    the constructor rejects NaN and infinity, but it is the only guard on the internal formatter.

## Proposals for later specs

- A `symbol` field or a parallel symbol file per component, for `game/content/symbols/`, which is
  still empty while pin geometry already lives in the component files.
- Pin electrical roles (input, output, power) once the netlist compiler needs them.
- A `GodotContentSource` test, which needs a Godot-hosted test runner; the adapter is currently only
  covered by the headless run.
- Export configuration will need `*.json` kept in the pack and non-imported resource remapping
  checked, since `DirAccess.GetFiles` behaves differently inside a packed archive.

## Open questions

None.
