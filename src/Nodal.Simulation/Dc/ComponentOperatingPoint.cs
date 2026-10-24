using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;

namespace Nodal.Simulation.Dc;

public sealed record ComponentOperatingPoint(
    ComponentId Id, string Designator, Quantity Voltage, Quantity Current, Quantity Power)
{
    public bool Absorbs => Power.Value >= 0;

    public override string ToString() => $"{Designator} V={Voltage} I={Current} P={Power}";
}
