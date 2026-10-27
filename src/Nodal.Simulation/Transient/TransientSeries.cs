using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Nodal.Circuit.Schematics;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Transient;

public sealed class TransientSeries
{
    internal TransientSeries(
        ComponentId id,
        string designator,
        string name,
        ComponentKind kind,
        ImmutableArray<double> voltages,
        ImmutableArray<double> currents,
        ImmutableArray<PinSeries> pins)
    {
        Id = id;
        Designator = designator;
        Name = name;
        Kind = kind;
        Voltages = voltages;
        Currents = currents;
        Pins = pins;
    }

    public ComponentId Id { get; }

    public string Designator { get; }

    public string Name { get; }

    public ComponentKind Kind { get; }

    public ImmutableArray<double> Voltages { get; }

    public ImmutableArray<double> Currents { get; }

    public ImmutableArray<PinSeries> Pins { get; }

    public bool IsTransistor => ComponentModels.IsTransistor(Kind);

    public bool TryGetPin(string name, [NotNullWhen(true)] out PinSeries? pin)
    {
        foreach (var candidate in Pins)
        {
            if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
            {
                pin = candidate;

                return true;
            }
        }

        pin = null;

        return false;
    }
}
