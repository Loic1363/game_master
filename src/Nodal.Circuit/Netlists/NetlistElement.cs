using System.Collections.Immutable;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;

namespace Nodal.Circuit.Netlists;

public sealed record NetlistElement(
    ComponentId Id,
    string Designator,
    string DefinitionId,
    ImmutableSortedDictionary<string, Quantity> Parameters,
    ImmutableArray<PinConnection> Pins);
