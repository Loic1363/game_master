# ADR-0016: Parameter tiers, optional parameters and automatic labels

Status: Accepted

## Context
Players should discover component parameters progressively, as they learn, rather than face a full datasheet at once. Real components have parasitic and rating parameters that ideal simulation ignores. Label anchors in symbol files did not survive rotation.

## Decision
Each parameter has an English label, a tier (basic or advanced) and optional inclusive bounds. A parameter with a default is required and always set; a parameter without a default is optional, unset by default, and an unset parameter means ideal or absent in simulation. The properties dialog shows basic parameters and an advanced section collapsed by default; future progression may hide tiers the player has not unlocked. Labels are placed automatically from the transformed symbol bounding box, above and below for horizontal orientations and to the right for vertical ones, and always stay upright; symbol files no longer carry label anchors. This amends ADR-0013 on labels.

## Consequences
Adding a parasitic parameter is a data change only. The simulator must define the ideal behavior for every unset optional parameter. Symbol files are simpler, and label layout is consistent across components.
