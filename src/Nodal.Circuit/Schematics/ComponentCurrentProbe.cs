namespace Nodal.Circuit.Schematics;

public sealed record ComponentCurrentProbe(ComponentId Component) : Probe
{
    public override ProbeKind Kind => ProbeKind.ComponentCurrent;

    public override string ToString() => $"I({Component})";
}
