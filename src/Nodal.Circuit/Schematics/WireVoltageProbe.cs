namespace Nodal.Circuit.Schematics;

public sealed record WireVoltageProbe(WireId Wire) : Probe
{
    public override ProbeKind Kind => ProbeKind.WireVoltage;

    public override string ToString() => $"V(wire {Wire})";
}
