using Nodal.Foundation.Quantities;

namespace Nodal.Simulation.Dc;

public sealed record PinOperatingPoint(string Name, Quantity Voltage, Quantity Current)
{
    public Quantity Power => new(Voltage.Value * Current.Value, Unit.Watt);
}
