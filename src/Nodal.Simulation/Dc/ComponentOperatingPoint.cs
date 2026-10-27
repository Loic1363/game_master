using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Collections;
using Nodal.Foundation.Quantities;

namespace Nodal.Simulation.Dc;

public sealed record ComponentOperatingPoint(
    ComponentId Id,
    string Designator,
    string Name,
    Quantity Voltage,
    Quantity Current,
    Quantity Power,
    ImmutableArray<PinOperatingPoint> Pins,
    DeviceOperatingPoint? Device)
{
    public ComponentOperatingPoint(
        ComponentId id,
        string designator,
        string name,
        Quantity voltage,
        Quantity current,
        Quantity power)
        : this(id, designator, name, voltage, current, power, [], null)
    {
    }

    public bool Absorbs => Power.Value >= 0;

    public bool TryGetPin(
        string name, [NotNullWhen(true)] out PinOperatingPoint? pin)
    {
        foreach (var candidate in Pins)
        {
            if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
            {
                pin = candidate;

                return true;
            }
        }

        pin = null;

        return false;
    }

    public bool Equals(ComponentOperatingPoint? other) =>
        other is not null
        && Id == other.Id
        && string.Equals(Designator, other.Designator, StringComparison.Ordinal)
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && Voltage == other.Voltage
        && Current == other.Current
        && Power == other.Power
        && StructuralEquality.ItemsEqual(Pins, other.Pins)
        && Device == other.Device;

    public override int GetHashCode() => HashCode.Combine(
        Id,
        Designator,
        Name,
        Voltage,
        Current,
        Power,
        StructuralEquality.ItemsHash(Pins),
        Device);

    public override string ToString() => $"{Designator} V={Voltage} I={Current} P={Power}";
}
