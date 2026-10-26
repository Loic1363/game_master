# ADR-0021: Waveform viewer and probes

Status: Accepted

## Context
Players must see how their circuits behave over time, as with LTspice's plot pane, and a transient result alone is invisible without a viewer.

## Decision
A waveform panel below the canvas plots transient results with a time axis, a volt axis and an ampere axis, 1-2-5 ticks with SI prefixes, auto-scaling, a legend and a cursor readout. A Probe tool adds voltage traces by clicking nets and current traces by clicking components, up to eight. Probes are stored persistently in the schematic file by pin reference, wire id or component id, never by net name, and adding or removing one is undoable. While the cursor hovers the plot, the schematic overlay shows values at the cursor time. Stale results stay visible but faded until the next run, and failed runs show their accepted prefix up to the failure time. Plot logic, ticks, decimation and readout live in pure .NET; Godot only draws.

## Consequences
The viewer can grow into a full oscilloscope with multiple panes, cursors and measurements without changing the probe model. Probes survive edits that rename nets.
