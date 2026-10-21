# Report 0005 — Schematic model, edit commands and undo

Status: Done. Plan approved before implementation; all six parts implemented, built, tested and run.

## Results

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**, 8 projects, `net10.0`.
- `dotnet test Nodal.sln`: **525 tests, 525 passed, 0 failed, 0 skipped** (was 323).
  Nodal.Foundation.Tests 227, Nodal.Content.Tests 71, **Nodal.Circuit.Tests 199 (new)**,
  Nodal.Architecture.Tests 28 (was 25).
- The architecture tests still prove `Nodal.Circuit` declares no Godot reference, in the compiled
  assembly and in the project file; `Nodal.Circuit.Tests` is now covered by the same three theories.
- Headless Godot run unchanged: `Loaded 11 component definitions, 0 diagnostics`, exit 0.
- No comments in any hand-written file; no Git command was run; no new package
  (`System.Collections.Immutable` ships with the base library).

## Commands

```
dotnet build Nodal.sln
dotnet test Nodal.sln
& $env:GODOT --headless --path game --quit-after 10
```

## Changes

Part A, `src/Nodal.Circuit/Geometry/`: `GridPoint` (integer point, y down, `+` and `-`), `Rotation`,
`Rotations` (the quarter-turn arithmetic as extensions, so `Orientation.Rotation` never shadows the
enum type) and `Orientation`, whose `Transform` mirrors horizontally first and then applies
`(x, y) => (-y, x)` once per quarter turn.

Part B, `src/Nodal.Circuit/Schematics/`: `ComponentId` and `WireId` (comparable int wrappers, first
id 1), `ComponentInstance`, `Wire`, `PinPosition` and `Schematic`. The schematic holds two
`ImmutableSortedDictionary` instances, so enumeration is ascending by id, plus the next id of each
kind. `TryGetPinPositions` returns absolute pin positions in definition pin order.

Part C, `src/Nodal.Circuit/Commands/`: `IEditCommand`, `CommandResult`, `EditError`,
`EditErrorCode`, the internal `Designators` allocator and `EditFailures`, and the nine commands.
Part D, `src/Nodal.Circuit/Editing/EditHistory.cs`: two snapshot stacks, a `Changed` event, and
descriptions for the future Edit menu.

Part E, `tests/Nodal.Circuit.Tests/` (199 tests): orientation (all 8 orientations against the real
resistor and NPN pin offsets, exact absolute positions off a non-zero placement), every command's
success and failure paths, designator gaps and shared prefixes, id allocation after removal,
composite atomicity, the history, and immutability as a theory over all nine commands.
`Nodal.sln` gained the project; `TestProjects.cs` and `TestProjectLayeringTests.cs` gained its
allow-list entry and a fourth `[InlineData]` on each theory.

Part F: `ADR-0011-immutable-schematic.md` recorded verbatim.

## Deviations

Significant choices the spec did not cover, per rule 10. Items 1 to 3 were decided by the architect
during plan review; the rest are mine.

1. **The description moved off the command onto the successful result.** `IEditCommand` has only
   `Apply`; `CommandResult.Success` carries the English description, produced during apply from the
   designator actually resolved or allocated. A parameterless `Description` property could not read
   `Add R1`, since that designator only exists once the command has run. `CompositeCommand` returns
   its own given description and exposes it as `CompositeCommand.Description`.
2. **Namespaces:** `Nodal.Circuit.Commands` for the commands, result and error codes;
   `Nodal.Circuit.Editing` for `EditHistory`.
3. **The architecture tests were extended**, not just the solution, so the new test project's
   layering is enforced like the other three.
4. **`Nodal.Circuit` defines its own failure type.** Foundation has no `Result<T>` and Content's
   `ContentDiagnostic` is not reachable from a sibling layer, so `CommandResult` plus a ten-value
   `EditErrorCode` is a third failure idiom in the repo. It follows ADR-0009: no throwing for player
   input, `ArgumentException` for programming errors.
5. **`Changed` is an `event Action?`**, not `EventHandler`. There is no sender or payload to pass,
   and a Godot adapter can subscribe a parameterless method directly.
6. **`Designator` stays a `string`**, matching `ComponentDefinition.Id`. The allocator recognizes an
   existing designator as its prefix followed by digits, which is unambiguous because designators are
   only ever generated that way and prefixes are uppercase letters.
7. **`Schematic`'s mutation helpers are `internal`** and are exercised through the commands, the way
   Content's internal reader is tested through its public loader. The public surface is queries only.
   They also require an incoming id to equal the schematic's next id, which is what makes reuse
   impossible by construction.
8. **`Undo()` and `Redo()` return `bool`**, false when the stack is empty, and raise nothing then.
9. **A non-positive value for a series-constrained parameter fails as `ValueNotInSeries`.**
   `ESeries.Contains` throws for zero and negatives, so the sign is checked first — the same guard
   the content reader already uses.
10. **An empty `CompositeCommand` succeeds** and returns the same schematic instance; a zero-step
    edit is not an error.

## Proposals for later specs

- A history cap, once a session can run long enough for snapshots to matter. Structural sharing
  makes this cheap today, so no limit was added.
- A public way to build a non-empty `Schematic` directly. Only commands can populate one now, which
  is right for the editor but will not serve schematic file loading.
- A pin reference type (`ComponentId` plus pin name) for connectivity in task 0006; today a caller
  pairs a `ComponentId` with a `PinPosition.PinName` by hand.
- Copy and paste will need a designator rule of its own: the smallest-free-number allocator renumbers
  pasted components, which may not be what a player expects.
- `EditErrorCode` messages are built at the failure site. If the UI needs to restyle or group them,
  a message catalog keyed by code would be the place.

## Open questions

None.
