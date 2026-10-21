using Nodal.Circuit.Schematics;

namespace Nodal.Circuit.Hit;

public sealed record SchematicHit(PinRef? Pin, WireId? Wire, ComponentId? Body, double Distance)
{
    public ComponentId? Component => Pin?.ComponentId ?? Body;
}
