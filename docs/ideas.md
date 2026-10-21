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
