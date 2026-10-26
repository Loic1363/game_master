# Report 0014 — Waveform viewer and probes

Status: Done. Build, tests and the headless run all pass. The panel was verified on screen with
the demo RC circuit.

## Results

- `dotnet build Nodal.sln`: **0 warnings, 0 errors**.
- `dotnet test Nodal.sln`: **2035 passed, 0 failed** (baseline before the task: 1795).
- Headless Godot run (`--headless --quit`), exit 0, printing exactly the existing lines:
  the netlist, the DC solution, `solved to 500 µs in 1004 points`, `V(N2) at 100 µs = 3.16 V`
  and `V(N2) at 500 µs = 4.97 V`.
- ADR-0021 recorded verbatim at `docs/decisions/ADR-0021-waveform-viewer.md`.
- CLAUDE.md rule 12 added verbatim after rule 11.
- No comments in any hand-written file, no Git command was run, no new package. All new visuals
  go through `game/scripts/Style/`.

## What was built

**Part A — partial results.** `TransientResult` carries `FailureTime`, `HasValues` and
`IsPartial` (false when not solved and no values); `Partial(...)` joins `Solution(...)` and
`Refused(...)`. `TransientStepper.Failed(time)` returns the accepted prefix with its statistics,
the `TimestepTooSmall` error and the `NoDcPath` warnings. `ToDebugText` prints
`stopped at 1 µs in 12 points` for a partial only. `SolutionSnapshot.FromTransient` keys on
`HasValues`, so a partial drives the overlay and a refusal still does not.

**Part B — probe tool.** `EditorToolKind.Probe` and `Tools/ProbeTool.cs` (label `Probe`, no ghost,
no preview, no selection). `EditorSession` gained `ChooseProbe()`, the internal `Analyze()`
(connectivity per click, ruling 6), `Toggle(probe, connectivity)` and `RefusalChanged`.
`Waveforms/ProbeToggle` decides between `AddProbe` and `RemoveProbe`: a current probe matches by
component, a voltage probe by resolved net, so clicking any pin or wire of an already probed net
removes it. A pin hit probes its net's first pin by `PinRefOrder`, a wire hit the same, or the
wire id when its net has no pin, and a body hit the component's current. In `game/`:
`EditorAction.ProbeTool` bound to **P**, `Edit › Probe tool`, and `MainScreen` showing
`RefusalChanged` in the status bar.

**Part C — persistence.** `Probe` is a closed union (`PinVoltageProbe`, `WireVoltageProbe`,
`ComponentCurrentProbe`). `ProbeTargets` answers `Exists` (save-time pruning) and `IsValid` (pin
names too). `AnalysisSettings` gained `Probes` and `MaxProbes = 8`; the 8-probe limit lives in
`AddProbe` (`At most 8 probes.`) and the reader. The writer prunes probes whose component or wire
is gone and writes `"probes"` inside `analysis` in stored order; the reader validates component
ids, pin names, wire ids, the one-of shape and the limit, warning and dropping each bad entry and
never rejecting the file. Schema version stays 1.

**Part E — pure logic.** `Nodal.Editor.Waveforms`: `AxisRange` (5 % margin, `Around` for the flat
case, zoom, union, mapping), `AxisTicks` (1-2-5 steps, SI labels through `Quantity`), `PlotZoom`
(time zoom about a cursor fraction and pan, clamped inside the run; vertical zoom about each
centre), `WaveformTrace` and `ProbeTraces` (resolution to the current net or component, `V(N1)` /
`I(R1)` labels, a stable colour index), `TraceDecimation` (first, minimum, maximum and last per
pixel column above two points per pixel), `CursorReadout`, `TransientFailureText` (code-to-phrase
map, ruling 8), `WaveformPlot` (immutable data, fitted limits, stale and partial state) and
`WaveformView` (the mutable window the panel drives). `DisplayValue` floors volts below 1 nV and
amperes below 1 pA (the amendment), and `OperatingPointOverlay` formats through it.

**Part D — the Godot panel.** `Main.tscn` now holds `Layout/Split` (a `VSplitContainer`) with the
canvas and a new `WaveformPanel`. `game/scripts/Style/WaveformStyle.cs` and `WaveformMetrics.cs`
plus the new `VisualTokens` entries (8-colour trace palette, plot background, grid, axis, cursor,
readout, marker, splitter, stale opacity) hold every colour and size, and the style layer also
builds the split container, the panel and the readout box.
`game/scripts/Rendering/WaveformPanel.cs` draws from `WaveformView` only: grid, time axis, left
volt axis when `HasVoltage`, right ampere axis when `HasCurrent`, traces, legend, cursor line and
readout box, the partial marker at `FailureTime`, and the faded stale state with its status text.
Wheel zooms time at the cursor, Ctrl+wheel zooms the vertical axes, left drag pans, double-click
fits, hover drives `SolutionSnapshot.FromTransient(result, t)` on the schematic and reverts on
exit, and a legend click runs `RemoveProbe`. `View › Waveform panel` is a check item, the panel
opens itself after a solved or partial run, `OnSchematicChanged` marks it stale instead of
dropping the result, and `game/scripts/Adapters/UserPreferences.cs` remembers its height.

## Tests

New since the baseline, 240 tests: `TransientPartialTests` (10), `ProbeCommandTests` (20),
`ProbeFileTests` (31), `AxisRangeTests` (17), `AxisTicksTests` (28), `TraceDecimationTests` (12),
`PlotZoomTests` (13), `ProbeTraceTests` (14), `CursorReadoutTests` (11), `WaveformPlotTests` (20),
`WaveformViewTests` (14), `DisplayValueTests` (27), `ProbeToolTests` (17), plus status-text,
snapshot and overlay tests and new editor-test helpers in `Sessions` and `TransientRuns` (a pulsed
RC that solves and one that stops at 1 µs). Architecture tests still pass.

## Deviations

1. **Ruling 7, recorded as asked.** The panel height lives in `UserPreferences`, a Godot
   `ConfigFile` at `user://preferences.cfg`, not in the schematic file. It is saved when the panel
   is closed, when a splitter drag ends and on quit.
2. **`WaveformView.MarkStale()` was added to the pure layer.** `WaveformPlot.Staled()` returns a
   new plot, so the panel could not go stale without rebuilding the view and losing the user's
   zoom and pan. `MarkStale` swaps the plot in place and keeps the window; it is covered by two
   new tests.
3. **`CanvasText.DrawRightAligned`** was added for the left value axis, whose labels sit to the
   left of the axis line. `LabelAlignment` (Circuit layer) only has `Centered` and `Left`, and the
   spec does not allow changing it.
4. **Split offset semantics.** The panel height is applied as `SplitOffsets = [-height]`, deferred
   one frame so the container has laid out. Godot places the dragger at the end of the container
   when the second child does not expand, so the offset is the panel's height negated. The first
   attempt (`minimum - height`, applied immediately) opened the panel at 140 px instead of 260 px;
   it was found and fixed by measuring the real window.

## Open questions

- The new scripts have no `.uid` files yet. The scene references scripts by path, so the game runs
  correctly, and Godot will write them the first time the project is opened in the editor.
- Tracking an unsaved panel height across a crash is not handled: the height is written on close,
  on drag end and on quit, not on every pixel of a drag.

## Verification

Build, tests and the headless run are listed above. The panel itself was checked on screen by
capturing the running game's window: the demo RC with a voltage probe on N2 and a current probe
through R1 opens the panel at 260 px, with the legend `I(R1)` and `V(N2)` in their palette
colours, a left axis `0 V … 5 V`, a right axis `0 A … 5 mA`, a time axis `0 s … 500 µs` in 50 µs
steps, grid lines at the major ticks, and the two RC curves auto-scaled with their margin. The
temporary hook used to open the panel without input was removed afterwards; the final build, test
and headless run above were made after its removal.

## Manual checklist for the project lead

Demo circuit (V1 5 V, R1 1 kΩ, C1 100 nF):

1. `P`, click the wire between R1 and C1 → status bar shows `Probe`; `F10`, Run → the panel opens
   showing `V(N2)` rising to 5 V over 500 µs.
2. Click R1's body → `I(R1)` is added in the second palette colour on the right ampere axis.
3. Hover the plot → the vertical cursor, the readout box with `t`, `V(N2)` and `I(R1)`, and the
   schematic pills follow the cursor time; leave the panel → the pills return to the stop time.
4. Wheel over the plot → the time axis zooms around the pointer; Ctrl+wheel → both value axes
   zoom; left drag → the view pans in time; double-click → everything fits again.
5. Click the `V(N2)` legend entry → the probe is removed; `Ctrl+Z` → it comes back.
6. Move a component → the traces stay, faded, with `Out of date — press F10 to run again`.
   `F10`, Run → the fresh traces replace them and the probes are kept.
7. `View › Waveform panel` → the panel hides and shows, the check mark follows; drag the splitter,
   quit and restart, run again → the panel reopens at the height you left it.
8. Save, reopen the file → the probes are still there. Delete a probed component, save, reopen →
   that probe is gone and the file still loads.
9. Add a ninth probe → the status bar shows `At most 8 probes.`

PULSE-driven RC (set V1's voltage to `PULSE(0 5 0 1u 1u 100u 200u)`, stop time 500 µs):

10. Run → `V(N2)` shows the charge and discharge ramps, `I(R1)` the current spikes at each edge;
    zoom into one edge → the spike keeps its peak (decimation preserves minima and maxima).
11. Set the maximum step to something the solver cannot meet so the run stops early → the traces
    end at the failure time, a red marker is drawn there, and the legend reads
    `Stopped at <t>: the timestep became too small`.
