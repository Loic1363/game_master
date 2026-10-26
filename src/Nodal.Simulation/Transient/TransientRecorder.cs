using System.Collections.Immutable;
using Nodal.Circuit.Schematics;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Transient;

internal sealed class TransientRecorder
{
    private readonly ElaboratedCircuit _circuit;
    private readonly ImmutableArray<string> _nets;
    private readonly List<double> _times = [];
    private readonly Dictionary<string, List<double>> _netVoltages = new(StringComparer.Ordinal);
    private readonly Dictionary<ComponentId, List<double>> _voltages = [];
    private readonly Dictionary<ComponentId, List<double>> _currents = [];

    public TransientRecorder(ElaboratedCircuit circuit, ImmutableArray<string> excludedNets)
    {
        _circuit = circuit;
        _nets =
        [
            .. circuit.ExternalNets.Where(
                net => !excludedNets.Contains(net, StringComparer.Ordinal)),
        ];

        foreach (var net in _nets)
        {
            _netVoltages[net] = [];
        }

        foreach (var component in circuit.Components)
        {
            _voltages[component.Id] = [];
            _currents[component.Id] = [];
        }
    }

    public void Add(
        double time, Func<string, double> nodeVoltage, Func<int, double> primitiveCurrent)
    {
        _times.Add(time);

        foreach (var net in _nets)
        {
            _netVoltages[net].Add(nodeVoltage(net));
        }

        foreach (var component in _circuit.Components)
        {
            _voltages[component.Id].Add(
                nodeVoltage(component.PlusNet) - nodeVoltage(component.MinusNet));

            _currents[component.Id].Add(component.TerminalPrimitives.Sum(primitiveCurrent));
        }
    }

    public TransientResult ToResult(
        ImmutableArray<SimulationDiagnostic> diagnostics, TransientStatistics statistics) =>
        TransientResult.Solution(
            [.. _times], NetSeries(), ComponentSeries(), diagnostics, statistics);

    public TransientResult ToPartial(
        ImmutableArray<SimulationDiagnostic> diagnostics,
        TransientStatistics statistics,
        double failureTime) =>
        TransientResult.Partial(
            failureTime,
            [.. _times],
            NetSeries(),
            ComponentSeries(),
            diagnostics,
            statistics);

    private ImmutableSortedDictionary<string, ImmutableArray<double>> NetSeries()
    {
        var netVoltages = ImmutableSortedDictionary.CreateBuilder<string, ImmutableArray<double>>(
            StringComparer.Ordinal);

        foreach (var net in _nets)
        {
            netVoltages[net] = [.. _netVoltages[net]];
        }

        return netVoltages.ToImmutable();
    }

    private ImmutableSortedDictionary<ComponentId, TransientSeries> ComponentSeries()
    {
        var components = ImmutableSortedDictionary.CreateBuilder<ComponentId, TransientSeries>();

        foreach (var component in _circuit.Components)
        {
            components[component.Id] = new TransientSeries(
                component.Id,
                component.Designator,
                [.. _voltages[component.Id]],
                [.. _currents[component.Id]]);
        }

        return components.ToImmutable();
    }
}
