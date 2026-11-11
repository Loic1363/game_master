using System.Collections.Immutable;
using Nodal.Circuit.Schematics;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Transient;

internal sealed class TransientRecorder
{
    private const int SeriesPerComponent = 2;

    private readonly ElaboratedCircuit _circuit;
    private readonly long _maxSamples;
    private readonly int _seriesCount;
    private readonly ImmutableArray<string> _nets;
    private readonly List<double> _times = [];
    private readonly Dictionary<string, List<double>> _netVoltages = new(StringComparer.Ordinal);
    private readonly Dictionary<ComponentId, List<double>> _voltages = [];
    private readonly Dictionary<ComponentId, List<double>> _currents = [];
    private readonly Dictionary<ComponentId, List<double>[]> _pinCurrents = [];
    private readonly Dictionary<ComponentId, List<bool>> _states = [];

    public TransientRecorder(
        ElaboratedCircuit circuit, ImmutableArray<string> excludedNets, long maxSamples)
    {
        _circuit = circuit;
        _maxSamples = maxSamples;
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
            _pinCurrents[component.Id] =
                [.. component.Pins.Select(_ => new List<double>())];

            if (HasState(component))
            {
                _states[component.Id] = [];
            }
        }

        _seriesCount = _nets.Length
            + circuit.Components.Sum(
                component => SeriesPerComponent
                    + component.Pins.Length
                    + (HasState(component) ? 1 : 0));
    }

    public bool IsFull => (long)_times.Count * _seriesCount > _maxSamples;

    public void Add(
        double time,
        Func<string, double> nodeVoltage,
        Func<int, double> primitiveCurrent,
        Func<int, double[]> pinCurrents,
        Func<int, bool> isClosed)
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

            var current = component.TerminalPrimitives.Sum(primitiveCurrent);

            _currents[component.Id].Add(current);
            AddPinCurrents(component, current, pinCurrents);

            if (HasState(component))
            {
                _states[component.Id].Add(isClosed(component.TerminalPrimitives[0]));
            }
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

    private static bool HasState(ElaboratedComponent component) =>
        component.IsVoltageControlledSwitch || component.IsPushButton;

    private void AddPinCurrents(
        ElaboratedComponent component, double current, Func<int, double[]> pinCurrents)
    {
        var lists = _pinCurrents[component.Id];
        var values = component.ReportsPinCurrents
            ? pinCurrents(component.TerminalPrimitives[0])
            : [current, -current];

        for (var pin = 0; pin < lists.Length; pin++)
        {
            lists[pin].Add(values[pin]);
        }
    }

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
                component.Name,
                component.Kind,
                [.. _voltages[component.Id]],
                [.. _currents[component.Id]],
                PinSeriesOf(component),
                StatesOf(component));
        }

        return components.ToImmutable();
    }

    private ImmutableArray<bool> StatesOf(ElaboratedComponent component) =>
        _states.TryGetValue(component.Id, out var states) ? [.. states] : [];

    private ImmutableArray<PinSeries> PinSeriesOf(ElaboratedComponent component) =>
    [
        .. component.Pins.Select((pin, index) => new PinSeries(
            pin.Name, pin.Net, [.. _pinCurrents[component.Id][index]])),
    ];
}
