# ADR-0015: Product positioning, a game rather than an engineering tool

Status: Accepted

## Context
Nodal is inspired by professional engineering tools and aims to feel close to reality. Players must not mistake it for a free design or validation tool, both for their own sake and to limit liability.

## Decision
Nodal is a game that makes people want to learn and prepares them to approach real tools; it is not an engineering tool. Physics models are deliberately simplified and documented as such. Immersion is embraced: datasheets look like real datasheets, but components and manufacturers are fictional. A discreet notice states that the simulation is simplified, is meant for play, and must not be used to design or validate real circuits; it appears in the terms of use, the About screen and store pages, never as an in-game banner. Marketing speaks of building, understanding and experimenting, never of designing or validating real circuits. No manufacturing export (Gerber files, ordering bills of materials) exists for now.

## Consequences
Adding any manufacturing-oriented export later requires a new ADR and prior legal advice. Component data must never describe real commercial parts. Turning Nodal into a professional tool would be a separate product decision.
