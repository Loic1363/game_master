# Task 0014 — Waveform viewer and probes

## Mode

Stay in the current mode. **First present your implementation plan** (panel layout, probe tool, plot model and its pure .NET logic, axis ticks, decimation, cursor readout, probe persistence, stale-result handling, tests), **then stop and wait** for approval.

## Context

Report 0013 is accepted. This task adds the first oscilloscope-like waveform viewer, in the spirit of LTspice's plot pane, reading the existing `TransientResult`. No simulation changes. The full oscilloscope (multiple panes, cursors, measurements, logic analyzer) is a later task. All CLAUDE.md rules apply, including rule 11 (style layer).

## Part A — Permanent rule and follow-up

1. Add to CLAUDE.md's "Permanent rules", verbatim:

   `12. For any task with several phases, maintain docs/reports/NNNN-progress.md (NNNN is the task number), updated at the end of each phase, stating the completed phases, the current test count and the next step, precise enough to resume from it and the spec alone after an interruption. Fold it into the final report and delete it at the end.`

   Apply it to this task.

2. **Partial transient results:** when a transient run fails (`TimestepTooSmall` or another refusal after it started), `TransientResult` keeps the accepted prefix of every series, flagged as partial, with the failure time. Full successes are unchanged.

## Part B — Probe tool

- A new editor tool, **Probe**, with key **P** in the shortcut table. It is available at any time.
- **Clicking a net** (a wire or a pin, using the existing hit-test order) toggles its voltage trace, `V(N1)`.
- **Clicking a component body** toggles its current trace, `I(R1)`.
- Escape returns to Select. The status bar shows `Probe`.
- Probes reference the schematic persistently (ADR-0012: never by N-name):
  - a voltage probe is stored as a `PinRef`, or a wire id when no pin exists on the net;
  - a current probe is stored as a `ComponentId`.
  - Each probe is resolved to its current net or component at display time.
  - A probe whose target no longer exists is dropped silently on display, and pruned on save.
- **Adding or removing a probe** is an undoable command and marks the document dirty, like the analysis settings.
- Up to 8 probes. Clicking a ninth target shows `At most 8 probes.` in the status bar.

## Part C — Probe persistence

- Probes are stored in the schematic file inside the existing optional `analysis` object, for example `"probes": [ { "voltage": { "component": 3, "pin": "1" } }, { "voltage": { "wire": 7 } }, { "current": { "component": 2 } } ]`.
- Reading validates them. Invalid or dangling entries are warnings and are dropped.
- The schema version stays 1. Writing stays deterministic.

## Part D — Waveform panel (Godot)

**Layout:**

- A bottom panel under the canvas, with a draggable splitter, built through the style layer.
- It opens automatically after a successful or partial transient run. View › Waveform panel toggles it, with a checkable item.
- Its last height is remembered in the user preferences, not in the schematic file.

**Plot:**

- **Time axis** at the bottom.
- **Left axis** in volts, shown when at least one voltage trace exists.
- **Right axis** in amperes, shown when at least one current trace exists.
- **Ticks** follow a 1-2-5 sequence, labelled with SI prefixes (`0 s`, `200 µs`, `400 µs`; `1 V`, `2 V`; `5 mA`).
- **Grid lines** at the major ticks.
- **Auto-scale:** each vertical axis fits its traces with a 5 % margin. A flat trace gets a sensible symmetric range around its value.
- **Trace colors** come from a palette of 8 distinct colors defined in the style layer, assigned by probe order.
- **Legend** along the top: one entry per trace (`V(N1)`, `I(R1)`) in its color. Clicking an entry removes that probe (undoable).

**Interaction:**

- **Mouse wheel** zooms the time axis around the cursor.
- **Ctrl + mouse wheel** zooms the vertical axes.
- **Left drag** pans in time.
- **Double-click** fits everything.

**Cursor readout:**

- Hovering the plot draws a vertical cursor line.
- A small box shows the time and the interpolated value of every trace at that time.
- **While hovering,** the schematic's voltage pills and the component hover text show the values at the cursor time, through `SolutionSnapshot.FromTransient(result, t)`. Otherwise they show the stop-time values.

**Partial result:**

- The traces stop at the failure time.
- A marker is drawn at that time.
- The legend shows `Stopped at 1.23 ms: <reason>`.

**Stale result:**

- When the schematic changes after a run, the panel keeps the last traces drawn faded through a style token.
- It shows `Out of date — press F10 to run again`.
- Adding a probe on a stale result shows the trace only after the next run.
- A new run replaces the data and keeps the probes.

## Part E — Pure .NET logic (`Nodal.Editor.Waveforms`)

Keep the following in pure .NET, unit-tested, so the Godot side only draws:

- probe resolution, to the net or component currently referenced;
- axis ranges with margins, and the flat-trace case;
- 1-2-5 tick generation with SI-prefixed labels;
- time and vertical zoom and pan arithmetic, with clamping to the result's time span;
- the cursor readout (time to interpolated values);
- **decimation for drawing:** when a trace has more points than twice the plot width in pixels, reduce it per pixel column to the first, minimum, maximum and last values, so spikes are never lost;
- stale and partial states.

## Part F — Tests

- Probe tool: toggling, the 8-probe limit, undo and redo, persistence round-trip, dangling probes dropped, resolution after the schematic changes (a renamed net still resolves through its `PinRef`).
- Axis ticks: several ranges, from nanoseconds to seconds and from microvolts to kilovolts; negative ranges; ranges crossing zero; the flat-trace case.
- Decimation: spikes preserved; the number of output points bounded.
- Cursor readout: interpolation, clamping, and the snapshot at cursor time driving the overlay.
- Partial results: the failing run keeps its prefix and failure time.
- Architecture tests still pass.

## Part G — ADR to record verbatim

Create `docs/decisions/ADR-0021-waveform-viewer.md` with exactly:

--- BEGIN ADR-0021 ---
# ADR-0021: Waveform viewer and probes

Status: Accepted

## Context
Players must see how their circuits behave over time, as with LTspice's plot pane, and a transient result alone is invisible without a viewer.

## Decision
A waveform panel below the canvas plots transient results with a time axis, a volt axis and an ampere axis, 1-2-5 ticks with SI prefixes, auto-scaling, a legend and a cursor readout. A Probe tool adds voltage traces by clicking nets and current traces by clicking components, up to eight. Probes are stored persistently in the schematic file by pin reference, wire id or component id, never by net name, and adding or removing one is undoable. While the cursor hovers the plot, the schematic overlay shows values at the cursor time. Stale results stay visible but faded until the next run, and failed runs show their accepted prefix up to the failure time. Plot logic, ticks, decimation and readout live in pure .NET; Godot only draws.

## Consequences
The viewer can grow into a full oscilloscope with multiple panes, cursors and measurements without changing the probe model. Probes survive edits that rename nets.
--- END ADR-0021 ---

## Acceptance criteria

- [ ] Plan presented and approved before implementation.
- [ ] `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all pass, with the count reported.
- [ ] The headless run still prints the existing lines.
- [ ] No comments in any hand-written file. No Git command was run. No new package. All new visuals go through the style layer.
- [ ] Spec saved at `docs/specs/0014-waveform-viewer.md`; report at `docs/reports/0014-waveform-viewer.md`, with a manual checklist using the demo circuit and a PULSE-driven RC.

## Out of scope

Multiple plot panes, measurement cursors and math on traces, differential voltage probes, power traces, the logic analyzer, data export, transistors, simulation changes.

## Amendment from the project lead

Display threshold: in the voltage pills, the hover text and the cursor readout, show values whose magnitude is below 1 nV as "0 V" and below 1 pA as "0 A". This is display only; results and saved data keep the exact numbers. Implement it in the pure .NET formatting used by the overlay and the readout, with tests.

## Rulings after the plan

1. `IsSolved` stays false for a partial run; the snapshot and pills key on having values.
2. A partial run opens the panel; the modal stays for runs with no data.
3. Statistics are kept on a partial result.
4. Flat-trace range: `v ± |v|`, or `±1` when `v == 0`.
5. Save-time pruning checks component and wire existence only; pin names are validated on read.
6. The Probe tool analyses connectivity on each click.
7. `UserPreferences` over Godot `ConfigFile` at `user://preferences.cfg` for the panel height; record it under Deviations.
8. Short failure phrases from a code-to-phrase map in the editor layer.
