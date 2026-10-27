using System.Collections.Immutable;
using Nodal.Foundation.Collections;

namespace Nodal.Simulation.Dc;

public sealed record DeviceOperatingPoint(
    DeviceRegion Region, ImmutableArray<DeviceQuantity> Quantities)
{
    public bool Equals(DeviceOperatingPoint? other) =>
        other is not null
        && Region == other.Region
        && StructuralEquality.ItemsEqual(Quantities, other.Quantities);

    public override int GetHashCode() =>
        HashCode.Combine(Region, StructuralEquality.ItemsHash(Quantities));
}
