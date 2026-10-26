using System.Collections.Immutable;
using Nodal.Foundation.Collections;

namespace Nodal.Simulation.Spice;

public sealed record SpiceProbe(string Designator, ImmutableArray<string> Elements)
{
    public bool Equals(SpiceProbe? other) =>
        other is not null
        && string.Equals(Designator, other.Designator, StringComparison.Ordinal)
        && StructuralEquality.ItemsEqual(Elements, other.Elements);

    public override int GetHashCode() =>
        HashCode.Combine(Designator, StructuralEquality.ItemsHash(Elements));
}
