using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Diagnostics;

namespace Nodal.Simulation.Transient;

public sealed class TransientResult
{
    private const string RefusedText = "refused";
    private const string StoppedText = "stopped at ";

    private TransientResult(
        bool isSolved,
        double? failureTime,
        ImmutableArray<double> times,
        ImmutableSortedDictionary<string, ImmutableArray<double>> netVoltages,
        ImmutableSortedDictionary<ComponentId, TransientSeries> components,
        ImmutableArray<SimulationDiagnostic> diagnostics,
        TransientStatistics? statistics)
    {
        IsSolved = isSolved;
        FailureTime = failureTime;
        Times = times;
        NetVoltages = netVoltages;
        Components = components;
        Diagnostics = diagnostics;
        Statistics = statistics;
    }

    public bool IsSolved { get; }

    public double? FailureTime { get; }

    public ImmutableArray<double> Times { get; }

    public ImmutableSortedDictionary<string, ImmutableArray<double>> NetVoltages { get; }

    public ImmutableSortedDictionary<ComponentId, TransientSeries> Components { get; }

    public ImmutableArray<SimulationDiagnostic> Diagnostics { get; }

    public TransientStatistics? Statistics { get; }

    public int PointCount => Times.Length;

    public bool HasValues => !Times.IsDefaultOrEmpty;

    public bool IsPartial => !IsSolved && HasValues;

    public double FinalTime => Times.IsDefaultOrEmpty ? 0 : Times[^1];

    public ImmutableArray<SimulationDiagnostic> Errors =>
        [.. Diagnostics.Where(diagnostic => diagnostic.Severity == SimulationSeverity.Error)];

    public ImmutableArray<SimulationDiagnostic> Warnings =>
        [.. Diagnostics.Where(diagnostic => diagnostic.Severity == SimulationSeverity.Warning)];

    public ImmutableArray<string> UndefinedNets =>
    [
        .. Diagnostics
            .Where(diagnostic => diagnostic.Code == SimulationDiagnosticCode.NoDcPath)
            .SelectMany(diagnostic => diagnostic.Nets),
    ];

    public bool TryGetNetVoltages(string netName, out ImmutableArray<double> voltages) =>
        NetVoltages.TryGetValue(netName, out voltages);

    public bool TryGetComponent(
        ComponentId id, [NotNullWhen(true)] out TransientSeries? series) =>
        Components.TryGetValue(id, out series);

    public double VoltageAt(string netName, double time) =>
        NetVoltages.TryGetValue(netName, out var voltages) ? At(voltages, time) : 0;

    public double CurrentAt(ComponentId id, double time) =>
        Components.TryGetValue(id, out var series) ? At(series.Currents, time) : 0;

    public double CurrentAt(ComponentId id, string pinName, double time) =>
        Components.TryGetValue(id, out var series) && series.TryGetPin(pinName, out var pin)
            ? At(pin.Currents, time)
            : 0;

    public DeviceOperatingPoint? DeviceAt(ComponentId id, double time) =>
        Components.TryGetValue(id, out var series) && series.IsTransistor
            ? TransientDevices.Describe(
                series, net => VoltageAt(net, time), pin => CurrentAt(id, pin, time))
            : null;

    public double ComponentVoltageAt(ComponentId id, double time) =>
        Components.TryGetValue(id, out var series) ? At(series.Voltages, time) : 0;

    public double SampleAt(ImmutableArray<double> values, double time) => At(values, time);

    public string ToDebugText()
    {
        var text = new StringBuilder();

        if (IsPartial)
        {
            Line(text)
                .Append(StoppedText)
                .Append(new Quantity(FailureTime ?? FinalTime, Unit.Second))
                .Append(" in ")
                .Append(PointCount)
                .Append(" points");
        }
        else if (!IsSolved)
        {
            text.Append(RefusedText);
        }
        else
        {
            Line(text)
                .Append("solved to ")
                .Append(new Quantity(FinalTime, Unit.Second))
                .Append(" in ")
                .Append(PointCount)
                .Append(" points");
        }

        foreach (var entry in NetVoltages)
        {
            Line(text)
                .Append("net ")
                .Append(entry.Key)
                .Append(" = ")
                .Append(new Quantity(entry.Value[^1], Unit.Volt));
        }

        foreach (var entry in Components)
        {
            Line(text)
                .Append(entry.Value.Designator)
                .Append(" V=")
                .Append(new Quantity(entry.Value.Voltages[^1], Unit.Volt))
                .Append(" I=")
                .Append(new Quantity(entry.Value.Currents[^1], Unit.Ampere));
        }

        foreach (var diagnostic in Diagnostics)
        {
            Line(text).Append(diagnostic);
        }

        if (Statistics is { } statistics)
        {
            Line(text).Append(statistics);
        }

        return text.ToString();
    }

    internal static TransientResult Refused(ImmutableArray<SimulationDiagnostic> diagnostics) =>
        new(false,
            null,
            [],
            ImmutableSortedDictionary.Create<string, ImmutableArray<double>>(
                StringComparer.Ordinal),
            ImmutableSortedDictionary<ComponentId, TransientSeries>.Empty,
            diagnostics,
            null);

    internal static TransientResult Solution(
        ImmutableArray<double> times,
        ImmutableSortedDictionary<string, ImmutableArray<double>> netVoltages,
        ImmutableSortedDictionary<ComponentId, TransientSeries> components,
        ImmutableArray<SimulationDiagnostic> diagnostics,
        TransientStatistics statistics) =>
        new(true, null, times, netVoltages, components, diagnostics, statistics);

    internal static TransientResult Partial(
        double failureTime,
        ImmutableArray<double> times,
        ImmutableSortedDictionary<string, ImmutableArray<double>> netVoltages,
        ImmutableSortedDictionary<ComponentId, TransientSeries> components,
        ImmutableArray<SimulationDiagnostic> diagnostics,
        TransientStatistics statistics) =>
        new(false, failureTime, times, netVoltages, components, diagnostics, statistics);

    private static StringBuilder Line(StringBuilder text)
    {
        if (text.Length > 0)
        {
            text.Append('\n');
        }

        return text;
    }

    private double At(ImmutableArray<double> values, double time)
    {
        if (values.IsDefaultOrEmpty)
        {
            return 0;
        }

        if (time <= Times[0])
        {
            return values[0];
        }

        if (time >= Times[^1])
        {
            return values[^1];
        }

        var found = ImmutableArray.BinarySearch(Times, time);

        if (found >= 0)
        {
            return values[found];
        }

        var upper = ~found;
        var lower = upper - 1;
        var span = Times[upper] - Times[lower];
        var weight = span > 0 ? (time - Times[lower]) / span : 0;

        return values[lower] + (weight * (values[upper] - values[lower]));
    }
}
