using Nodal.Circuit.Schematics;

namespace Nodal.Circuit.Hit;

public sealed record SchematicHit(PinRef? Pin, WireId? Wire, double Distance);
