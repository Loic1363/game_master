namespace Nodal.Circuit.Schematics;

public sealed record PinVoltageProbe(PinRef Pin) : Probe
{
    public override ProbeKind Kind => ProbeKind.PinVoltage;

    public override string ToString() => $"V({Pin})";
}
