namespace Nodal.Circuit.Schematics;

public abstract record Probe
{
    internal Probe()
    {
    }

    public abstract ProbeKind Kind { get; }

    public bool IsVoltage => Kind is ProbeKind.PinVoltage or ProbeKind.WireVoltage;

    public bool IsCurrent => Kind == ProbeKind.ComponentCurrent;

    public static Probe At(PinRef pin) => new PinVoltageProbe(pin);

    public static Probe At(WireId wire) => new WireVoltageProbe(wire);

    public static Probe Through(ComponentId component) => new ComponentCurrentProbe(component);
}
