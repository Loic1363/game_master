# ADR-0010: Component definitions as versioned JSON

Status: Accepted

## Context
Components must be data, not code (ADR-0004), so new component types can be added without touching the editor or the simulator. The game also needs pin geometry to compute connectivity, and parameter values that respect standard series.

## Decision
Each component type is one JSON file with a schemaVersion, loaded and validated by Nodal.Content through a content-source abstraction, so the same loader works on disk and inside an exported Godot archive. Unknown fields are errors. Pin positions are integer grid coordinates relative to the component origin, y pointing down. Parameter defaults are written in the game's quantity notation (ADR-0008) and may be constrained to an E-series. MOSFETs have three terminals with the body tied to the source, in line with the simplified physics of the project; a four-terminal variant may be added later as a separate type.
