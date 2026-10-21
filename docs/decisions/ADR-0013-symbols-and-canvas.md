# ADR-0013: Symbol definitions and canvas conventions

Status: Accepted

## Context
The editor must draw components without hard-coding their appearance, support both IEC and ANSI styles eventually, and stay consistent with pin geometry defined in component files (ADR-0010).

## Decision
Each symbol is a versioned JSON file per component and style, made of simple primitives in grid units: line, polyline, rect, circle, arc, polygon and text. The loader verifies that every pin of the component is reached by a lead. Symbols follow the same orientation transform as pins; text stays upright. IEC is the only style for now and the default. On screen, one grid unit is 16 px at zoom 1, strokes have a constant 2 px screen width, and visual tokens are defined in a single place. Hit-testing and geometry live in the pure .NET core; Godot code only maps input, converts coordinates and draws.

## Consequences
New components need a symbol file but no code. ANSI symbols are additional files with style "ansi". A full visual identity will later replace the provisional tokens without touching rendering logic.
