# Ideas backlog

Ideas from the project lead, kept for future game design. Nothing here is a decision; decisions live in docs/decisions/.

## Game vision
- A game for curious people, not an educational game: challenging but approachable. Low floor, high ceiling.
- Players learn by doing and by seeing what things are for, never by reading a datasheet or following a course.
- Progressive levels, very simple at first.

## Troubleshooting mode
- Faulty boards to diagnose: a chip does not work, the player measures, forms hypotheses and finds the culprit (for example a blown diode shorting a 24 V supply).
- Components get fault states as data: short, open, drifted.
- Scoring can reward finding the fault with few measurements.

## Community
- Share circuits, including broken ones: "here is my circuit, why doesn't it work?"

## Wider simulation
- Sensors and actuators, analog and digital, motors, frequency drives.
- Electrical cabinets and racks built from the player's own boards, with a front view or a 3D view; alarm faults to troubleshoot in one's own or community cabinets.
- Industrial automation and PLCs, much later.

## Component parameters
- Parameters unlock progressively: a capacitor starts with capacitance only; voltage rating, RMS current rating, ESR, ESL and parallel parasitics appear as the player progresses. Inspired by LTspice's component dialogs, but game-like.

## Visual design (for the planned full design overhaul)
- The DC voltage source should be a plain circle with + and - markings, without a line through it, as in LTspice; a line through it can be confusing.
- Menus with icons.
- Full visual identity to be designed later; current colors are provisional.

## Editor
- Connection-preserving drag (wires follow the component), as LTspice's Drag.

## Troubleshooting, further thoughts
- Part of the core game, not only an endgame: alternate building missions (synthesis) with troubleshooting missions (analysis).
- Troubleshooting introduces components before they are formally taught (for example an optocoupler), with a short in-game datasheet, so players discover them by necessity. Red herrings can point back to components learned earlier.
- Community-made faulty boards provide endless endgame content and avoid the "nothing left to do" problem of building games.
- The game should verify that a published fault is detectable: at least one allowed measurement distinguishes the faulty board from the healthy one.
- Scoring by measurements taken, time, and cost of wrongly replaced parts, shown as histograms against other players.
- A troubleshooting challenge is a data file: schematic, faults, allowed instruments, scoring.

## Boards and installations
- PCB routing, layers and ground planes interest a minority; the schematic already plays the role of the board. A PCB layer could come much later, for players building full installations and racks (see ADR-0015 for export limits).

## Polish backlog (from the project lead's tests)
- Labels overlap: voltage pills cover designators and values (V1, 1 kΩ), and a source's function text collides with nearby ground labels. Labels need collision avoidance.
- Leaving the probe tool (Escape) and hiding the waveform panel (View menu) are not discoverable; the panel needs a visible close button and the probe tool a clearer entry and exit.
- The P shortcut for probes is not ideal; revisit shortcuts during the design overhaul.
- Menus with icons.

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

## Live interaction
- Pressing a push button or turning a potentiometer while the circuit runs needs a real-time simulation mode. Until then the push button follows a press schedule set before the run (ADR-0024).
