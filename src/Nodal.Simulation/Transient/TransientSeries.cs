using System.Collections.Immutable;
using Nodal.Circuit.Schematics;

namespace Nodal.Simulation.Transient;

public sealed class TransientSeries
{
    internal TransientSeries(
        ComponentId id,
        string designator,
        ImmutableArray<double> voltages,
        ImmutableArray<double> currents)
    {
        Id = id;
        Designator = designator;
        Voltages = voltages;
        Currents = currents;
    }

    public ComponentId Id { get; }

    public string Designator { get; }

    public ImmutableArray<double> Voltages { get; }

    public ImmutableArray<double> Currents { get; }
}
