# Report 0016 — Documentation, untrusted content ADR, and 0015 follow-ups

Status: Done. Build, tests and the headless run all pass. Parts A to D are in; Part E is the audit
below, with nothing fixed, as asked. Four small choices the spec did not spell out are listed under
Deviations.

## Acceptance

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**.
- `dotnet test Nodal.sln`: **2482 passed, 0 failed** (baseline before the task: 2465, re-measured
  before touching any code; +17 = 8 region cases in the quantities, 8 in the hover text, 1 exporter
  test).
- Headless Godot (`--headless --path game --quit`), exit 0, output unchanged from 0015: the 11
  definitions and 11 symbols, the netlist, the DC solution, `solved to 500 µs in 1004 points`,
  `1003 accepted, 0 rejected, 1003 iterations`, `V(N2) at 100 µs = 3.16 V` and
  `V(N2) at 500 µs = 4.97 V`.
- The eight Part A sections appended verbatim to `docs/ideas.md`; ADR-0023 recorded verbatim at
  `docs/decisions/ADR-0023-untrusted-content.md`.
- No comments in any hand-written file (the six touched `.cs` files were checked for `//` and
  `/* */`). **No git command of any kind was run**, read-only ones included. No new package.
  Rule 11 respected: no colour, font, size or theme override was touched; the β visibility is
  decided in the simulation layer, and the hover text follows because it concatenates device
  quantities.
- Spec at `docs/specs/0016-docs-untrusted-content.md`; the progress file
  `docs/reports/0016-progress.md` is folded into this report and deleted.

## Part B — effective β

`DeviceDescriptions.Bipolar` now computes the region once and gates the β quantity on
`ReportsCurrentGain(region, baseCurrent)`: ForwardActive or Saturation **and** |Ib| > 1 pA. In
Saturation it is the plain Ic / Ib with no extra wording. Both reporting paths, the device
quantities and the hover text, change from this one place.

The eight biases, each asserting the region as well as the β, with the PNP mirrored as
`control = supply − drive`, the mirroring the existing PNP tests already used:

| Bias (supply, NPN base drive) | Region | β |
|---|---|---|
| 5 V, 0.65 V | forward active | shown |
| 0.1 V, 0.7 V | saturation | shown |
| −0.65 V, 0 V | reverse active | absent |
| 5 V, 0 V | cutoff | absent |

`ACutoffBipolarWithABiasedCollectorStillLeaksAboveThePicoampThreshold` is the test that asserted β
in cutoff; it is now `ACutoffBipolarReportsNoBetaEvenWhenItLeaksAboveThePicoampThreshold`, keeping
the leak assertion and asserting β is absent.

## Part C — no negative zero

`SpiceFormat.Value` prints `"0"` for any value equal to zero, which covers −0.0 and therefore every
negated zero, in plain element values and in every waveform argument alike.
`SpiceExporterTests.ANegatedZeroIsPrintedWithoutItsSign` drives a current source with
PULSE(0 1e-3 …) and asserts both the exact line
`I1 N1 0 PULSE(0 -0.001 0 1E-09 1E-09 1E-06 2E-06)` and that no token of the deck equals `-0`.

All 37 reference decks were regenerated with `dotnet Nodal.Cli export`. SHA-256 before and after:

- The **49** `expected.json` / `expected-transient.json` files are **byte-identical**, zero
  differing hashes. ngspice was never invoked.
- Exactly **one** deck changed text:
  `tests/reference-circuits/transient-ring-oscillator/circuit.cir`, line 15,
  `PULSE(-0 -0.001 …)` to `PULSE(0 -0.001 …)` (`5026a36b…e012387` to `4d53348a…cc7040b7`). The
  other 36 decks and `tests/Nodal.Simulation.Tests/Spice/golden-deck.cir` are byte-identical. No
  `-0` token remains in any committed deck.

## Part E — Audit against ADR-0023 (report only, nothing fixed)

**What already holds.** No reader in `src/Nodal.Circuit/Persistence/` or `src/Nodal.Content/`
touches `Path`, `File` or `Directory`, and neither `SchematicFields` nor `WaveformFields` has a
field that names a file, so **no file path is ever taken from a file's content**; symbol files are
matched by expected name, not by a path they carry. JSON is parsed strictly, no comments and no
trailing commas, with `JsonDocument`'s default depth limit of 64, so nesting cannot blow the stack.
Every value kind is checked before reading, every number must be finite, ids must be positive
int32, duplicate ids and reused next-ids are rejected, PWL points are validated for strict time
order, probes are capped at 8, and bad content produces diagnostics instead of exceptions
(`JsonException` is caught in `SchematicReader.ReadFile`).

**F1 — The text handed to the reader is unbounded.**
`game/scripts/Adapters/SchematicFiles.cs:47` and `src/Nodal.Cli/Program.cs:54` read the whole file
into a string before `SchematicFile.Read` sees it. Above roughly a gigabyte `File.ReadAllText`
throws `OutOfMemoryException`, which `IsFileFailure` (`SchematicFiles.cs:94`) does not catch, so
the game dies on a hostile file without the reader ever running.
*Fix:* check the file length first and refuse anything above a stated maximum, a few MB being
already far beyond any real schematic, reported as an ordinary read error.

**F2 — No cap on components, wires or diagnostics.**
`SchematicReader.ReadComponents` and `ReadWires` accept any number of entries; only probes have a
cap (`AnalysisSettings.MaxProbes` = 8). `_diagnostics` is also uncapped, and each rejected or
unknown field adds a formatted message, so a file made of unknown fields turns its bytes into a
much larger diagnostics list.
*Fix:* `MaxComponents` and `MaxWires` next to `MaxProbes`, refusing the file above them, and a cap
on the diagnostics list with a trailing "and N more" summary.

**F3 — Transient settings from the file are unbounded in work and memory.**
`SchematicReader.ReadTransientSettings` accepts any positive finite `stopTime`, `outputStep` and
`maxStep`; `TransientSettings.IsValid` checks positivity only; `TransientPlan.Resolve` derives
`minStep = stopTime × 1e-9`; and `TransientRecorder` keeps every recorded point of every net,
component and pin in memory. A file asking for `stopTime` 1 s with `outputStep` 1e-15 s requests
1e15 output points and allocates until the process dies. The run **is** cancellable
(`TransientStepper.Run` calls `cancellation.ThrowIfCancellationRequested()`), so half of ADR-0023
holds; it is not bounded.
*Fix:* bound the point count at read time, in `TransientSettings.IsValid`, with `stopTime` over
`outputStep` below a stated maximum and `maxStep` not below `stopTime × 1e-9`, and have the
recorder stop with a diagnostic if the cap is ever reached anyway.

**F4 — `DirectoryContentSource` throws on I/O failure.**
`src/Nodal.Content/Sources/DirectoryContentSource.cs:20,25` wraps neither
`Directory.EnumerateFiles` nor `File.ReadAllText`, so a missing directory or an unreadable file
throws out of `ComponentCatalogLoader.Load` and `SymbolLibraryLoader.Load`. Nothing crashes today:
the game uses `GodotContentSource`, which prints and skips, and the CLI catches `IOException` and
`UnauthorizedAccessException`. It is still a loader that throws where the ADR asks for diagnostics.
*Fix:* catch the file failures inside the source and surface them as content diagnostics, as
`GodotContentSource` does.

**F5 — Latent throws that file content cannot reach today.**
`ComponentCatalog` (`:25`) and `SymbolLibrary` (`:25`) throw `ArgumentException` on a duplicate id
or key, but both loaders deduplicate first and emit a diagnostic, so content files cannot reach
them; `JsonValueKindNames.Of` throws outside its four names, and every call site passes a constant
kind; `WaveformJson` (`:71`) throws only on the write path; `TransientOptions` and
`TransientPlan.Resolve` throw for non-positive times, guarded by `TransientSettings.IsValid`.
*Fix:* none needed. Worth recording that the deduplication in the two loaders is what keeps those
constructors unreachable, so it must not be removed.

**F6 — Quadratic work in symbol loading.**
`SymbolLibraryLoader.UnreachedPins` compares every lead end against every pin of the definition.
Both counts come from bounded input, but the product is quadratic in a hand-made symbol file with
very many polyline points.
*Fix:* nothing urgent; a point count cap per symbol would settle it, together with F2.

## Deviations and choices the spec did not spell out

1. **Part C regeneration** used `dotnet Nodal.Cli export` over the 37 folders rather than
   `tools/ngspice-reference/Update-References.ps1`, as ruled, so the ngspice expectations could be
   proved untouched by hash instead of being rewritten.
2. **The zero test is `value == 0`**, not a sign-bit test, so it catches −0.0 and plain 0 alike and
   prints the shared constant `SpiceFormat.ZeroText`.
3. **The Part C unit test goes through the public exporter**, because `SpiceFormat` is `internal`
   and the project has no `InternalsVisibleTo`; it asserts the exported deck, which is the
   behaviour the spec names.
4. **The PNP biases are mirrored as `control = supply − drive`** in a new helper
   `TransistorCircuits.BipolarRegionBox`, reusing the rule already in
   `TransistorCircuits.ControlFor`. The hover-text test project cannot see that helper, so it
   carries its own copy, as it already does for `BiasBox`.

## Manual checklist for the project lead

1. Build an **NPN switch**: 5 V supply, 1 kΩ collector load, 10 kΩ base resistor from a second
   source at **5 V**. Hover the transistor: it must read
   `Q1 (NPN transistor): saturation, Vbe …, Vce …, Ic …, Ib …, β …`, so **β is present**, with no
   extra wording, and Vce is a couple of hundred millivolts.
2. Drop the base source to **0 V**. The hover text must read
   `Q1 (NPN transistor): cutoff, Vbe …, Vce …, Ic …, Ib …`, so **β is gone**, and the collector
   sits at the supply.
3. Open `tests/reference-circuits/transient-ring-oscillator/circuit.nodal` in the game and export
   it; line 15 of the `.cir` must start `I1 N5 0 PULSE(0 -0.001 …)`, with no `-0`.

## Proposals, not implemented

- The six Part E findings, in that order: F1 and F3 are the two a shared file can actually use
  against the player, so they are the natural content of the first security ADR that ADR-0023 asks
  for.
- Everything already in the Engine backlog section of `docs/ideas.md`, unchanged.
