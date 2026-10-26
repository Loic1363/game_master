using System.Collections.Immutable;

namespace Nodal.Simulation.Spice;

public sealed class SpiceProbePlan
{
    internal SpiceProbePlan(ImmutableArray<string> nodes, ImmutableArray<SpiceProbe> components)
    {
        Nodes = nodes;
        Components = components;
    }

    public ImmutableArray<string> Nodes { get; }

    public ImmutableArray<SpiceProbe> Components { get; }
}
