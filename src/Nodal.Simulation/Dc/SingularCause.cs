using System.Collections.Immutable;
using Nodal.Circuit.Schematics;

namespace Nodal.Simulation.Dc;

internal sealed record SingularCause(
    string Description, ImmutableArray<ComponentId> Components, ImmutableArray<string> Nets);
