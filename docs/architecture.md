# Nodal — architecture

## Layers

Bottom to top. A layer may only depend on layers below it. Nothing reaches upward.

| Layer | Location | Responsibility |
|---|---|---|
| Platform | — | Godot 4 .NET, the .NET runtime, Windows. |
| Foundation | `src/Nodal.Foundation` | Logging, physical units, math, identifiers, events, profiling. |
| Content | `src/Nodal.Content` | Loading and validating versioned data files. |
| Circuit | `src/Nodal.Circuit` | Schematic model, undoable commands, schematic-to-netlist compilation. |
| Simulation | later | Numerical solver driven by a netlist. |
| Characterization | later | Measuring a validated circuit so it can become a reusable chip. |
| Assembly | later | Assembler and machine model for player-built CPUs. |
| Gameplay | later | Game flow, objectives, progression, scoring. |
| Presentation | `game/` | Godot scenes and scripts; adapters call into the core. |

`tests/Nodal.Architecture.Tests` enforces the rule by inspecting both the compiled
assemblies and the project files, so an unused reference cannot slip through.

## Principles

- **The core never references Godot.** Everything under `src/` is plain .NET. The engine is
  a replaceable shell around a library that can be tested, profiled and reused without it.
- **The schematic is the source of truth.** It is compiled into a netlist, and the netlist is
  the only input the simulator ever sees. This mirrors KiCad, where the netlist is the artifact
  that carries a schematic between tools.
- **The simulation runs on its own thread.** It receives commands and publishes snapshots and
  waveforms. The UI never blocks on the solver, and the solver never touches scene nodes.
- **Every editor action is an undoable command.** Placing a component, moving a wire and editing
  a value are all commands with an inverse.
- **Files are versioned, Git-friendly text.** JSON with an explicit schema version, so saves
  diff cleanly and old files can be migrated.
- **Components are data, not code.** Adding a resistor family or a new transistor is a data file,
  not a new class.

## Future simulation loop

Following Glenn Fiedler's "Fix Your Timestep!":

- The UI **produces** time; the simulation **consumes** it in fixed steps. Rendering interpolates
  between the last two published states.
- A **per-frame budget** caps how many steps may run in one frame. This avoids the "spiral of
  death": under load the simulation falls behind in simulated time rather than freezing the UI.
- **Simulated time is decoupled from real time.** Pause, slow motion and single stepping are
  changes to the time source, not to the solver.

## Patterns

From Robert Nystrom, *Game Programming Patterns*:

- **Command** — undo, redo and replay of every editor action.
- **Observer** and **Event Queue** — the simulation thread publishes, the UI consumes at its own pace.
- **Type Object** — component types are data-defined instances, not subclasses.
- **Dirty Flag** — only the part of the schematic that changed is recompiled into a netlist.

## References

- Jason Gregory, *Game Engine Architecture*.
- Robert Nystrom, *Game Programming Patterns*.
- Glenn Fiedler, "Fix Your Timestep!".
- KiCad documentation, on schematics, netlists and symbol libraries.
