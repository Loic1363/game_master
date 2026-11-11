# Task 0016 — Documentation, untrusted content ADR, and 0015 follow-ups

## Mode
Stay in the current mode. First present your implementation plan (files touched, tests changed or added, phases), then stop and wait for approval. Follow rule 12 (progress file).

## Context
Report 0015 is accepted. Its deviations are accepted: the P-channel `@m[id]` negation and `@name[current]` in the harness, no collector-substrate gmin path, the re-laid-out differential pair, the high-start PULSE on the NPN switch. One reminder: rule 1 forbids every git command, including read-only ones such as `git status`, `git diff` or `git log`. When you need to prove that files did not change, use hashes as you did in 0015. This task is mostly documentation, plus two small code follow-ups from 0015.

## Part A — Ideas notebook
Append these sections to the end of `docs/ideas.md`, verbatim:

--- BEGIN ideas.md addition ---
## Chips: contract and quality
- A chip is validated as a black box against its expected behavior (truth table or function), whatever its construction.
- Once valid, continuous metrics are optimized: cost, propagation delay, power, later noise margins and fan-out, shown as Zachtronics-style histograms.
- Metrics roll up the hierarchy: a NAND used 200 times in an ALU counts 200 times in the ALU's metrics.
- A chip can have a version 2 that replaces version 1 everywhere when its contract is identical.
- Start small, grow, and come back to optimize the small chips when the large circuits demand it.

## Lab notebook (endgame, unlockable)
- Per-project notes with formatted equations.
- Live calculation with units inside the notes: `5 V / 4k7` gives 1.06 mA; `4k7 * 100n` gives 470 µs.
- Plot an expression next to a simulation result.
- An engineering calculator.
- Keep it simple: these are advanced options.

## In-game code editor
- Pillar 6: assembly is written inside the game, with a debugger and a register view. Never an external tool to install.

## AI and challenges
- AI is not forbidden, but challenges favor information that must be earned: in troubleshooting, the fault is invisible until measured, so a screenshot is not enough for an AI.
- Scores are measured inside the simulation.

## Score validation
- Shared scores are validated by re-simulation, never by trusting the file.

## File size
- Transparent compression of .nodal files for large circuits. JSON stays the format (ADR-0017); LTspice and KiCad also use text formats.

## Playtesting
- First, the project lead's engineer friends with varied profiles (digital, telecom, power): watch them play without explaining, and test early.
- Later, curious non-engineers, to validate the low floor.

## Player security
- See ADR-0023. Dedicated ADRs and a security review are mandatory before file sharing, mods, external PWL files and any online service.
- Prefer Steam Workshop; never direct connections between players.
- The game is single-player. The community shares circuits and faults; a leaderboard is possible later, but it is not a competitive game.

## Engine backlog (from task reports)
- Transistor capacitances, needed for realistic propagation delays before chips.
- A genuinely hard DC operating point to exercise gmin and source stepping; the CMOS latch converges by plain Newton–Raphson to its symmetric state.
- Choosing which pin a current probe measures; the probe model is ready and the file format needs no change.
- A four-terminal MOSFET, which would make the body diode and the body effect meaningful.
--- END ideas.md addition ---

## Part B — Effective β only where it means something
Change the rule of task 0015. The effective β = Ic / Ib is reported only when the region is ForwardActive or Saturation **and** |Ib| > 1 pA. In Cutoff and ReverseActive it is absent from the device quantities and from the hover text. In Saturation it is the forced β, with no extra wording.

Tests: an NPN and a PNP in each of the four regions assert the presence or absence of β in the quantities and in the hover text. Update any existing test that asserted β in cutoff.

## Part C — No negative zero in exported decks
The SPICE exporter prints `-0` when it negates a zero value (for example `PULSE(-0 -0.001 …)` on a current source). Every exported number equal to zero must print as `0`. Add a unit test. Regenerate all committed reference decks. Confirm with SHA-256, without git, that every `expected.json` and `expected-transient.json` is byte-identical, and list the decks whose text changed.

## Part D — ADR to record verbatim
Create `docs/decisions/ADR-0023-untrusted-content.md` with exactly:

--- BEGIN ADR-0023 ---
# ADR-0023: Untrusted content

Status: Accepted

## Context
Players will share schematics and possibly other content. A file from a stranger must never endanger the player's computer, as happened with remote code execution flaws in other games.

## Decision
Every file the game opens is untrusted data. Files never contain executable code; mods, if any, are data only. Readers are strict, bounded and never throw for bad input, and they never follow arbitrary file paths taken from a file's content. Player programs only ever run inside the simulated CPU, never as native code. Analyses that can run long are cancellable and bounded.

## Consequences
Each feature that imports external content (file sharing, mods, external PWL files, online services) requires its own security ADR and a security review before implementation.
--- END ADR-0023 ---

## Part E — Audit against ADR-0023 (report only, no code change)
Check the current `.nodal` reader and the content loaders against ADR-0023: can any input make them throw, allocate without bound, loop without bound, or open a path taken from the file? List each finding with its location and a proposed fix. Do not fix anything in this task.

## Acceptance criteria
- [ ] Plan presented and approved before implementation.
- [ ] dotnet build: 0 warnings, 0 errors. dotnet test: all pass, with the count reported.
- [ ] Headless run output unchanged from 0015.
- [ ] No comments in any hand-written file. No git command of any kind was run. No new package. Rule 11 respected.
- [ ] Spec saved at docs/specs/0016-docs-untrusted-content.md; report at docs/reports/0016-docs-untrusted-content.md, with a short manual checklist (the β visibility in the hover text of an NPN switch at base 5 V and 0 V).

## Out of scope
Any fix found by the Part E audit, transistor capacitances, any new analysis, any UI change beyond the hover text.
