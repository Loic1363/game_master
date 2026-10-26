using System.Globalization;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Waveforms;

namespace Nodal.Simulation.Elaboration;

public sealed record Primitive(
    ComponentId Owner,
    string Designator,
    PrimitiveKind Kind,
    PrimitiveRole Role,
    string PlusNode,
    string MinusNode,
    double Value,
    Waveform? Function = null)
{
    public bool IsSource =>
        Kind is PrimitiveKind.VoltageSource or PrimitiveKind.CurrentSource;

    public bool StoresEnergy => Kind is PrimitiveKind.Capacitor or PrimitiveKind.Inductor;

    public double ValueAt(double time) => Function?.ValueAt(time) ?? Value;

    public override string ToString() =>
        $"{Kind} {Designator} {Role} {PlusNode} {MinusNode} "
        + (Function?.ToString() ?? Value.ToString("R", CultureInfo.InvariantCulture));
}
