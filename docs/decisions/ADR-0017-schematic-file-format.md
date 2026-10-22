# ADR-0017: Schematic file format

Status: Accepted

## Context
Players must save and reopen their work without loss, files should be readable and Git-friendly like the rest of the project, and a slightly damaged or outdated file should not destroy hours of work.

## Decision
A schematic is saved as a .nodal file: versioned, deterministic, indented JSON. Parameter values are stored as exact numbers in SI base units, never as formatted text. Component and wire identifiers are saved so references stay stable. Reading never throws: structural problems refuse the file with diagnostics, while value-level problems are repaired with warnings (unknown parameters dropped, invalid values reset to the default or unset), and unknown fields are ignored with a warning. Saving writes to a temporary file and renames it. The editor tracks a saved marker in the edit history to show unsaved changes.

## Consequences
Changing the format requires a new schemaVersion and a migration in the reader. Older builds can partly read newer files that only add fields. Repairs mean a loaded schematic can differ from the file; the player is always told what was changed.
