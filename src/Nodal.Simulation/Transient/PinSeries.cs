using System.Collections.Immutable;
using Nodal.Foundation.Collections;

namespace Nodal.Simulation.Transient;

public sealed record PinSeries(string Name, string Net, ImmutableArray<double> Currents)
{
    public bool Equals(PinSeries? other) =>
        other is not null
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && string.Equals(Net, other.Net, StringComparison.Ordinal)
        && StructuralEquality.ItemsEqual(Currents, other.Currents);

    public override int GetHashCode() =>
        HashCode.Combine(Name, Net, StructuralEquality.ItemsHash(Currents));
}
