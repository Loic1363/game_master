# Report 0006 — Connectivity and netlist

Status: Done. Plan approved before implementation; all six parts implemented, built, tested and run.

## Results

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**, 8 projects, `net10.0`.
- `dotnet test Nodal.sln`: **630 tests, 630 passed, 0 failed, 0 skipped** (was 525).
  Nodal.Foundation.Tests 227, Nodal.Content.Tests 87 (was 71),
  Nodal.Circuit.Tests 288 (was 199), Nodal.Architecture.Tests 28.
- Headless Godot run unchanged: `Loaded 11 component definitions, 0 diagnostics`, exit 0, which is
  also the live check that the validator accepts the new `globalNet` field on `ground.json`.
- No comments in any hand-written file; no Git command was run; no new package.

## Commands

```
dotnet build Nodal.sln
dotnet test Nodal.sln
& $env:GODOT --headless --path game --quit-after 10
```

## Changes

Part A: `MoveComponent` and `SetParameter` return the input schematic instance when the edit changes
nothing, and `EditHistory.Execute` records nothing, keeps redo and stays silent when the result is
that same instance. `SetParameter` validates before comparing, so a wrong unit still fails rather
than passing as a no-op. A `CompositeCommand` of no-ops inherits the behavior for free.

Part B: `PinRef` and `PinRefOrder` in `src/Nodal.Circuit/Schematics/`. In `Nodal.Content`,
`PinDefinition` gained `string? GlobalNet = null`, `ComponentFields` the `globalNet` name and its
entry in the pin field list, `ComponentNameRules` the `IsGlobalNetName` rule, and
`ComponentDefinitionReader` a `TryReadGlobalNet` modelled on the existing `TryReadSeries`.
`game/content/components/ground.json` declares `"globalNet": "0"` on its `gnd` pin.

Part C, `src/Nodal.Circuit/Connectivity/`: `ConnectivityAnalyzer` plus the internal
`ConnectivityGraph`, `DisjointSet` and `WireGeometry`, and the public `ConnectivityResult`, `Net`,
`ConnectivityDiagnostic`, `ConnectivityDiagnosticCode` and `ConnectivitySeverity`. The union-find
runs over one node per wire, per pin, per distinct connection point and per global net name. Rule 3
needs no special case: two wires are joined only through a shared connection point, and a bare
interior crossing is not one, so a T connects and an X does not.

Part D, `src/Nodal.Circuit/Netlists/`: `NetlistCompiler`, `Netlist`, `NetlistElement` and
`PinConnection`.

Part E: `tests/Nodal.Circuit.Tests/Connectivity/` (`ConnectivityTests`, `JunctionTests`,
`NetNamingTests`, `DiagnosticTests`, the `Circuits` builder), `Netlists/NetlistTests.cs`,
`Schematics/PinRefTests.cs`, and `Components/GlobalNetTests.cs` in the Content tests.

Part F: `ADR-0012-connectivity-rules.md` recorded verbatim.

## Deviations

Significant choices the spec did not cover, per rule 10. The three points raised in the plan were
settled by the architect at approval and are recorded at the end of `docs/specs/0006-connectivity.md`.

1. **Global-named nets come first in `Nets` and `NetNames`**, ordered ordinally by name, then the
   `N`-numbered nets in assignment order. The spec fixes the N-numbering order but not where a
   global net sits in the list; putting `0` first is deterministic and reads well in a netlist.
2. **`Net` record equality is reference-based on its `ImmutableArray` members**, so two structurally
   identical nets from separate analyses are not `Equals`. Tests compare net contents instead. See
   the proposal below.
3. **`PinRefOrder` sorts an unknown pin or an unknown definition last**, falling back to the ordinal
   pin name, so a stale `PinRef` can still be ordered without throwing.
4. **`ConnectivityDiagnostic` carries four related-id arrays** (`Components`, `Wires`, `Pins`,
   `Positions`) with per-code factories filling only what applies. The spec asked for "the related
   ids and positions" without fixing a shape.
5. **Diagnostics are emitted in a fixed sequence**, asserted by a test: `UnknownDefinition` by
   component id, `GlobalNetShort` by net name, `FloatingPin` in `PinRefOrder`, `DanglingWireEnd` by
   wire id then endpoint, `NoGround` last.
6. **`ConnectivityResult` also exposes `HasErrors`**, a one-line convenience the future simulator
   gate will want.
7. **An unconnected pin in the netlist would print `?`** as its net name. Unreachable today, since
   every pin of a known component creates a connection point and therefore a net, but
   `NetlistCompiler` needs a value for the case rather than throwing.
8. **`NoGround` fires on a wires-only schematic only when components exist**, per the spec's wording;
   an empty schematic and a wires-only schematic both report nothing.
9. **`TryReadGlobalNet` rejects an empty string** with the same message as any other invalid name.

## Proposals for later specs

- Structural equality on `Net`, `ConnectivityResult` and `NetlistElement`, or a documented note that
  their array members compare by reference. This caught me in my own tests and will catch the UI.
- A spatial index. `BuildClasses` is points × wires and `MeasureDegrees` is points × wires again,
  which is fine for the schematics a challenge will hold but not for thousands of wires.
- `DanglingWireEnd` currently fires for both ends of a wire the player is still drawing. The editor
  will want to suppress diagnostics for an in-progress wire, or analyze a schematic that excludes it.
- A junction-dot suppression rule for the four-way case, where some editors draw a crossover instead,
  if the visual style calls for it.
- Power symbols using `globalNet`, such as `VCC`, now cost only a JSON file and a symbol; the
  analyzer already handles them and the global-net-short error is already covered by tests.

## Open questions

None.
