using System.Collections.Immutable;
using Nodal.Circuit.Schematics;

namespace Nodal.Circuit.Connectivity;

public sealed record Net(
    string Name, ImmutableArray<PinRef> Pins, ImmutableArray<WireId> Wires);
