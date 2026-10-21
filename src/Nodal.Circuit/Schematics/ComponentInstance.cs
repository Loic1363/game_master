using System.Collections.Immutable;
using Nodal.Circuit.Geometry;
using Nodal.Foundation.Quantities;

namespace Nodal.Circuit.Schematics;

public sealed record ComponentInstance(
    ComponentId Id,
    string DefinitionId,
    string Designator,
    GridPoint Position,
    Orientation Orientation,
    ImmutableSortedDictionary<string, Quantity> Parameters);
