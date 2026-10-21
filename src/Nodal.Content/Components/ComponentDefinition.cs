using System.Collections.Immutable;
using Nodal.Foundation.Collections;

namespace Nodal.Content.Components;

public sealed record ComponentDefinition(
    string Id,
    string Name,
    string Description,
    ComponentCategory Category,
    string DesignatorPrefix,
    ImmutableArray<PinDefinition> Pins,
    ImmutableArray<ParameterDefinition> Parameters)
{
    public bool Equals(ComponentDefinition? other) =>
        other is not null
        && string.Equals(Id, other.Id, StringComparison.Ordinal)
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && string.Equals(Description, other.Description, StringComparison.Ordinal)
        && Category == other.Category
        && string.Equals(DesignatorPrefix, other.DesignatorPrefix, StringComparison.Ordinal)
        && StructuralEquality.ItemsEqual(Pins, other.Pins)
        && StructuralEquality.ItemsEqual(Parameters, other.Parameters);

    public override int GetHashCode() => HashCode.Combine(
        Id,
        Name,
        Description,
        Category,
        DesignatorPrefix,
        StructuralEquality.ItemsHash(Pins),
        StructuralEquality.ItemsHash(Parameters));
}
