# Task 0003 — Housekeeping and physical quantities

## Context

Task 0002 is accepted. This task applies the follow-ups from report 0002, adds a permanent rule, and builds the first real Foundation feature: physical quantities with engineering notation and E-series values. Component values in later tasks depend on it.

All CLAUDE.md rules apply: no Git, no comments in code, no unlisted dependencies, concise report.

## Part A — Housekeeping

1. **CLAUDE.md:** add this as rule 10 under "Permanent rules", verbatim:
   `10. Never create or edit ADRs on your own initiative. ADRs are dictated by the architect in specs, and you record them verbatim. If you had to make a significant choice the spec did not cover (architecture, dependencies, file formats, frameworks, interfaces between layers, simulation algorithms), report it under "Deviations" so the architect can record it.`
2. **Architecture test:** add a test asserting that the `TargetFramework` declared in `game/Nodal.Game.csproj` equals `NodalTargetFramework` from `Directory.Build.props`.
3. **.gitignore:** stop ignoring `*.import`. Godot 4 expects `.import` files to be versioned. Keep `.godot/` ignored.
4. **global.json:** add an `sdk` section pinning the 10.0 SDK band currently installed, with `rollForward` set to `latestFeature`. Keep the existing test runner setting.

## Part B — Physical quantities in Nodal.Foundation

Everything in this part is pure .NET in `Nodal.Foundation`, with no Godot reference and no new dependency.

### B1. Units

A `Unit` type covering: dimensionless, ohm (Ω), farad (F), henry (H), volt (V), ampere (A), second (s), hertz (Hz), watt (W). Each unit knows its symbol.

### B2. Quantity

An immutable `Quantity` value type holding:

- a `double` value in SI base units (for example 4.7 kΩ is stored as 4700);
- its `Unit`.

Equality is exact on both value and unit. Do not add arithmetic operators in this task.

### B3. Parsing

`Quantity.TryParse(string text, Unit expectedUnit, out Quantity result, out QuantityParseError error)`:

- no exceptions for invalid user input;
- `QuantityParseError` says why parsing failed: empty, malformed number, unknown prefix, wrong unit, out of range.

Accepted forms:

- **Plain numbers:** `4700`, `4.7e3`, `0.001`.
- **SI prefixes, case-sensitive:** `f` (1e-15), `p`, `n`, `u` and `µ` (micro), `m` (milli), `k`, `M` (mega), `G`, `T`. So `1m` is 1e-3 and `1M` is 1e6.
- **SPICE habit:** `meg`, in any letter case, is mega.
- **Notation where the prefix replaces the decimal point (RKM, IEC 60062):** `4k7` means 4.7e3, `2M2` means 2.2e6, `4n7` means 4.7e-9. For resistance only, `R` marks the decimal point: `2R2` means 2.2 Ω and `R47` means 0.47 Ω.
- **Unit symbol:**
  - optional, and must match `expectedUnit` if present: `4.7kΩ`, `4.7kohm`, `100nF`, `5V`, `1kHz`, `10mA`;
  - for ohms, accept `Ω`, `ohm` and `ohms` in any letter case;
  - a unit symbol that does not match `expectedUnit` returns the wrong-unit error.
- **Decimal separator:** both `.` and `,`. Thousands separators are not accepted.
- **Whitespace:** leading and trailing whitespace is ignored, and a single space is allowed between the number and the prefix or unit (`4.7 kΩ`).
- **Culture:** parsing never depends on the OS culture.

Rejected forms include: `4k7k`, `1.2.3`, `k`, `--1`, `4.7 kV` when ohms are expected, and NaN or infinity.

### B4. Formatting

`Quantity.ToString()` produces engineering notation:

- the exponent is a multiple of 3, and the prefix is chosen so the mantissa lies in [1, 1000);
- at most 3 significant digits, trailing zeros removed;
- a space between the number and the prefixed unit;
- `.` as the decimal separator, `µ` for micro.

Examples: 4700 Ω gives `4.7 kΩ`, 1e-7 F gives `100 nF`, 0.0022 A gives `2.2 mA`, and 0 V gives `0 V`.

Round-trip: formatting then parsing returns an equal quantity, up to 3-significant-digit rounding.

### B5. E-series

A static `ESeries` facility for E6, E12 and E24. Store the standard base values as data:

| Series | Base values |
|---|---|
| E6 | 1.0 1.5 2.2 3.3 4.7 6.8 |
| E12 | 1.0 1.2 1.5 1.8 2.2 2.7 3.3 3.9 4.7 5.6 6.8 8.2 |
| E24 | 1.0 1.1 1.2 1.3 1.5 1.6 1.8 2.0 2.2 2.4 2.7 3.0 3.3 3.6 3.9 4.3 4.7 5.1 5.6 6.2 6.8 7.5 8.2 9.1 |

Operations:

- `Contains(series, value)`: true when the value equals a series value times a power of ten, with a relative tolerance of 1e-9.
- `Nearest(series, value)`: the closest series value on a logarithmic scale. When two values are equally close, take the higher one.
- `Next(series, value)` and `Previous(series, value)`: step up or down through the series across decades. These will drive scroll-wheel value editing later.
- `ValuesInRange(series, min, max)`: all series values within an inclusive range, ascending.

Positive values only. Zero or a negative value is invalid input: decide whether to use a Try-pattern or an exception, apply it consistently, and report the choice.

## Part C — Tests

Use table-driven xUnit tests (`[Theory]`). They must cover:

- every accepted form listed in B3;
- every rejection listed in B3, with the expected error;
- formatting for each prefix from f to T, and for zero;
- round-trip on a few hundred values spread across all decades;
- E-series membership, nearest value, next and previous across decade boundaries (`9.1 → 10` in E24, `1.0 → 0.91` in E24), and range queries.

## Part D — ADR to record verbatim

Create `docs/decisions/ADR-0008-quantity-notation.md` with exactly this content:

--- BEGIN ADR-0008 ---
# ADR-0008: Quantity notation

Status: Accepted

## Context
Component values are typed by players with an electronics background, many of whom know LTspice. LTspice parses prefixes case-insensitively, so "1M" means milli, a well-known trap. Players also use RKM notation from part markings (4k7, 2R2) and, in much of Europe, a comma as decimal separator.

## Decision
Prefixes follow SI and are case-sensitive: m is milli, M is mega. "meg" in any letter case is also accepted as mega for SPICE users. RKM notation is accepted, with R as the decimal marker for resistance. Both "." and "," are accepted as decimal separators; thousands separators are not. Parsing is culture-invariant. Display uses engineering notation with up to 3 significant digits and µ for micro. Values are stored as double in SI base units.

## Consequences
"1M" means mega, unlike LTspice; the in-game documentation must state this. Because a comma is always a decimal separator, list input must use another separator. Changing any of these rules later breaks saved user input expectations and requires a new ADR.
--- END ADR-0008 ---

## Acceptance criteria

- [ ] `dotnet build Nodal.sln`: 0 warnings, 0 errors.
- [ ] `dotnet test`: all tests pass, with the count reported (the old 17 plus the new ones).
- [ ] The headless Godot run still prints the two expected lines.
- [ ] No comments in any hand-written file.
- [ ] No Git command was run.
- [ ] Spec saved at `docs/specs/0003-quantities.md`; report at `docs/reports/0003-quantities.md`.

## Out of scope

Arithmetic on quantities, logging, E48/E96, any game or UI change, any use of quantities in other projects.
