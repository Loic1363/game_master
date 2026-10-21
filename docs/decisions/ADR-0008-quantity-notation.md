# ADR-0008: Quantity notation

Status: Accepted

## Context
Component values are typed by players with an electronics background, many of whom know LTspice. LTspice parses prefixes case-insensitively, so "1M" means milli, a well-known trap. Players also use RKM notation from part markings (4k7, 2R2) and, in much of Europe, a comma as decimal separator.

## Decision
Prefixes follow SI and are case-sensitive: m is milli, M is mega. "meg" in any letter case is also accepted as mega for SPICE users. RKM notation is accepted, with R as the decimal marker for resistance. Both "." and "," are accepted as decimal separators; thousands separators are not. Parsing is culture-invariant. Display uses engineering notation with up to 3 significant digits and µ for micro. Values are stored as double in SI base units.

## Consequences
"1M" means mega, unlike LTspice; the in-game documentation must state this. Because a comma is always a decimal separator, list input must use another separator. Changing any of these rules later breaks saved user input expectations and requires a new ADR.
