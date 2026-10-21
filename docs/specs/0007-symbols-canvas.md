# Task 0007 — Symbols and schematic canvas (display only)

## Mode

Stay in the current mode. **First present your implementation plan** (files, types, public API, the symbol geometry you intend for each component, the rendering approach in Godot, the tests), **then stop and wait** for approval. Do not write code before approval.

## Context

Report 0006 is accepted. This is the first visible task. It adds symbol definitions as data, a canvas that draws a schematic with pan and zoom, and net highlighting on hover. Editing interactions (placing, wiring, moving, undo shortcuts) are the next task, 0008. All CLAUDE.md rules apply.

## Part A — Structural equality

`Net`, `NetlistElement` and `ConnectivityDiagnostic` compare their array and dictionary members by reference today. Give them structural equality (equal contents, in order) with matching hash codes, and add tests. Apply the same treatment to any other public record in `src/` holding an `ImmutableArray` or an immutable dictionary, and list them in the report.

## Part B — Symbol definition format (`Nodal.Content.Symbols`)

One JSON file per symbol, in `game/content/symbols/`, named `<component-id>.<style>.json`:

```json
{
  "schemaVersion": 1,
  "kind": "symbol",
  "component": "resistor",
  "style": "iec",
  "primitives": [
    { "type": "line", "from": [-2, 0], "to": [-1, 0] },
    { "type": "rect", "x": -1, "y": -0.4, "width": 2, "height": 0.8 },
    { "type": "line", "from": [1, 0], "to": [2, 0] }
  ],
  "labels": { "designator": [0, -1.2], "value": [0, 1.2] }
}
```

**Fields:**

- `schemaVersion`: `1`.
- `kind`: `"symbol"`.
- `component`: a component id from the catalog.
- `style`: `"iec"` only for now. ANSI comes later.
- `labels`: anchor points for the designator and value texts.
- Unknown fields are errors.

**Primitive types:**

| type | fields |
|---|---|
| `line` | `from`, `to` |
| `polyline` | `points`, `closed` |
| `rect` | `x`, `y`, `width`, `height` |
| `circle` | `center`, `radius`, optional `filled` |
| `arc` | `center`, `radius`, `startDegrees`, `sweepDegrees` |
| `polygon` | `points`, optional `filled` |
| `text` | `at`, `value`, a short string such as "+" |

**Conventions:**

- Coordinates are finite numbers in grid units, y down, with absolute values of 10 or less.
- Angles are in degrees, measured clockwise on screen from +x. A positive sweep is clockwise.

**Loader:** `SymbolLibraryLoader.Load(IContentSource, ComponentCatalog)` returns a symbol library and diagnostics. It never throws for invalid content (ADR-0009). It reports, in addition to field-level errors:

- **Error:** the symbol references an unknown component id.
- **Error:** a duplicate (component, style) pair.
- **Error:** a component pin is not reached by any `line` endpoint or `polyline` vertex of its symbol. Every pin must have a visible lead ending exactly on it.
- **Warning:** a catalog component has no `iec` symbol.

Rotated and mirrored components use the same transform as pins: mirror first (x becomes -x), then rotate clockwise. Mirroring an arc reverses its sweep direction, and rotation adds to its angles. Text and labels stay upright; only their anchor positions are transformed. Provide this transform in pure .NET, with tests.

## Part C — The eleven IEC symbols

Draw one `iec` symbol per base component, following IEC 60617 conventions in simplified form:

- **Resistor:** rectangle.
- **Capacitor:** two parallel plates.
- **Inductor:** a row of semicircular arcs.
- **Diode:** a triangle pointing to a bar on the cathode side.
- **NMOS and PMOS:** gate line separated from the channel, with drain and source leads. An arrow on the source or body shows the polarity. Simplified, but unambiguous between N and P.
- **NPN and PNP:** base bar, collector and emitter, with the emitter arrow pointing out for NPN and in for PNP.
- **DC voltage source:** circle with a line through it, plus a "+" text near the `plus` pin.
- **DC current source:** circle with a perpendicular line and an arrow in the direction of conventional current.
- **Ground:** IEC ground symbol.

**Constraints:**

- Every symbol fits within its pins' extent plus 1 grid unit of margin.
- Every pin has a lead ending exactly on it.
- The base library loads with 11 symbols and 0 diagnostics.

The project lead will review the symbols visually. Describe each one briefly in your plan.

## Part D — Canvas in Godot (`game/scripts/Presentation/`, `game/scripts/Rendering/`)

The main scene becomes the schematic view.

**Layout:**

- A full-window canvas.
- A one-line status bar at the bottom showing: `Nodal 0.0.1`, the net count, the warning count, and the name of the hovered net, or nothing.

**Visual tokens**, defined in one place in the game project and nowhere else:

| token | value |
|---|---|
| background | `#F7F5F0` |
| grid dots | `#D8D4CC` |
| symbol and wire stroke | `#2B2B2B` |
| label text | `#4A4A4A` |
| net highlight | `#E07A1F` |

**Grid:**

- 1 grid unit is 16 px at zoom 1.
- One dot per grid point. Hide the dots when they would be closer than 8 px on screen.

**Strokes:**

- Strokes are 2 px wide on screen at any zoom.
- Junction dots are filled circles of 3 px radius, at the positions given by `ConnectivityResult.Junctions`.

**Labels:**

- The designator and the first parameter's formatted value (`1 kΩ`) at the symbol label anchors.
- Components without parameters show the designator only.

**Navigation:**

- **Pan:** middle-mouse drag, or Space with left-mouse drag.
- **Zoom:** mouse wheel, centered on the cursor, clamped between 0.25 and 8.
- **Home:** fit the whole schematic in view.

**Hover:**

- When the cursor is within 6 screen pixels of a wire or a pin, highlight every wire and pin of that net in the highlight color, and show the net name in the status bar.
- Do this hit-testing in pure .NET, in `Nodal.Circuit` (for example `SchematicHitTester`), using fractional grid coordinates and a tolerance, with tests. Godot only converts screen coordinates to grid coordinates.

**Demo schematic:**

- At startup, build a schematic through `EditHistory` and commands. No hand-built schematic objects.
- It contains:
  - a DC voltage source V1 at 5 V;
  - a resistor R1 at 1 kΩ, horizontal, from the source's plus terminal;
  - a capacitor C1 at 100 nF, vertical, from the resistor's far end down to ground;
  - one shared ground net for V1 and C1, using two ground symbols;
  - at least one T-junction, so a junction dot is visible.
- Print the demo netlist's `ToDebugText()` with `GD.Print` at startup.

**Godot side:** the Godot code only maps input, converts coordinates and draws. Connectivity, hit-testing and geometry come from the core.

## Part E — Tests

- Symbol loading:
  - the real files give 11 symbols and 0 diagnostics;
  - table-driven invalid cases, one per rule in Part B.
- Transform: mirror and rotation of every primitive type, including arc sweep reversal.
- Hit-testing:
  - inside and outside tolerance;
  - nearest wire wins;
  - a pin takes precedence over a wire passing nearby;
  - nothing hit on empty space.
- Structural equality (Part A).
- Architecture tests still pass.

## Part F — ADR to record verbatim

Create `docs/decisions/ADR-0013-symbols-and-canvas.md` with exactly:

--- BEGIN ADR-0013 ---
# ADR-0013: Symbol definitions and canvas conventions

Status: Accepted

## Context
The editor must draw components without hard-coding their appearance, support both IEC and ANSI styles eventually, and stay consistent with pin geometry defined in component files (ADR-0010).

## Decision
Each symbol is a versioned JSON file per component and style, made of simple primitives in grid units: line, polyline, rect, circle, arc, polygon and text. The loader verifies that every pin of the component is reached by a lead. Symbols follow the same orientation transform as pins; text stays upright. IEC is the only style for now and the default. On screen, one grid unit is 16 px at zoom 1, strokes have a constant 2 px screen width, and visual tokens are defined in a single place. Hit-testing and geometry live in the pure .NET core; Godot code only maps input, converts coordinates and draws.

## Consequences
New components need a symbol file but no code. ANSI symbols are additional files with style "ansi". A full visual identity will later replace the provisional tokens without touching rendering logic.
--- END ADR-0013 ---

## Acceptance criteria

- [ ] Plan presented and approved before implementation.
- [ ] `dotnet build Nodal.sln`: 0 warnings, 0 errors. `dotnet test`: all tests pass, with the count reported.
- [ ] The headless Godot run prints `Loaded 11 component definitions, 0 diagnostics`, a symbols line with 11 symbols and 0 diagnostics, and the demo netlist.
- [ ] No comments in any hand-written file. No Git command was run. No new package.
- [ ] Spec saved at `docs/specs/0007-symbols-canvas.md`; report at `docs/reports/0007-symbols-canvas.md`. List in the report the manual visual checks for the project lead.

## Out of scope

Placing, selecting, moving, deleting or wiring with the mouse; keyboard edit shortcuts; parameter editing; ANSI symbols; file save and load.
