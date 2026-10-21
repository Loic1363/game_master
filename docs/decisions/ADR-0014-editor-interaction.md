# ADR-0014: Editor interaction model

Status: Accepted

## Context
The editor must feel familiar to LTspice users while staying simple to implement and test, and every action must go through undoable commands (ADR-0011).

## Decision
Editing uses a tool state machine with Select, Place and Wire tools; Escape always returns to Select. Placement repeats until cancelled. Wires are drawn as L-shaped routes, horizontal then vertical, committed one click at a time as a single command. Moving a component does not move attached wires; a connection-preserving drag is deferred. Shortcuts are defined in a single table that drives input, menus and help, so remapping can be added later. Hit-testing and wire routing live in the pure .NET core. Menus contain only working items; each feature adds its own menu entries when it lands. The tool state machine and editor session live in Nodal.Editor, a pure .NET project between Nodal.Circuit and the game, so they can be unit-tested without Godot.

## Consequences
The menu bar grows with the features. Moving a component can leave dangling wires, which the connectivity warnings report. Adding a connection-preserving drag later will need a new command and possibly new routing logic.
