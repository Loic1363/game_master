using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Transient;

namespace Nodal.Editor.Simulation;

public sealed class SolutionSnapshot
{
    private readonly ImmutableSortedDictionary<string, Quantity> _netVoltages;
    private readonly ImmutableSortedDictionary<ComponentId, ComponentOperatingPoint> _components;
    private readonly ImmutableArray<string> _undefinedNets;
    private readonly ImmutableArray<ComponentId> _ratingWarnings;

    private SolutionSnapshot(
        bool isSolved,
        double time,
        ImmutableSortedDictionary<string, Quantity> netVoltages,
        ImmutableSortedDictionary<ComponentId, ComponentOperatingPoint> components,
        ImmutableArray<string> undefinedNets,
        ImmutableArray<ComponentId> ratingWarnings)
    {
        IsSolved = isSolved;
        Time = time;
        _netVoltages = netVoltages;
        _components = components;
        _undefinedNets = undefinedNets;
        _ratingWarnings = ratingWarnings;
    }

    public bool IsSolved { get; }

    public double Time { get; }

    public static SolutionSnapshot FromDc(OperatingPoint operatingPoint)
    {
        ArgumentNullException.ThrowIfNull(operatingPoint);

        return new SolutionSnapshot(
            operatingPoint.IsSolved,
            0,
            operatingPoint.NetVoltages,
            operatingPoint.Components,
            operatingPoint.UndefinedNets,
            [
                .. operatingPoint.RatingWarnings.SelectMany(
                    diagnostic => diagnostic.Components),
            ]);
    }

    public static SolutionSnapshot FromTransient(TransientResult result, double time)
    {
        ArgumentNullException.ThrowIfNull(result);

        var netVoltages = ImmutableSortedDictionary.CreateBuilder<string, Quantity>(
            StringComparer.Ordinal);

        foreach (var net in result.NetVoltages.Keys)
        {
            netVoltages[net] = new Quantity(result.VoltageAt(net, time), Unit.Volt);
        }

        var components =
            ImmutableSortedDictionary.CreateBuilder<ComponentId, ComponentOperatingPoint>();

        foreach (var series in result.Components.Values)
        {
            var voltage = result.ComponentVoltageAt(series.Id, time);
            var current = result.CurrentAt(series.Id, time);

            var pins = PinsOf(result, series, time);

            components[series.Id] = new ComponentOperatingPoint(
                series.Id,
                series.Designator,
                series.Name,
                new Quantity(voltage, Unit.Volt),
                new Quantity(current, Unit.Ampere),
                new Quantity(pins.Sum(pin => pin.Power.Value), Unit.Watt),
                pins,
                result.DeviceAt(series.Id, time));
        }

        return new SolutionSnapshot(
            result.HasValues,
            time,
            netVoltages.ToImmutable(),
            components.ToImmutable(),
            result.UndefinedNets,
            []);
    }

    public bool HasRatingWarning(ComponentId id) => _ratingWarnings.Contains(id);

    public bool IsUndefined(string netName) =>
        _undefinedNets.Contains(netName, StringComparer.Ordinal);

    public bool TryGetNetVoltage(string netName, out Quantity voltage) =>
        _netVoltages.TryGetValue(netName, out voltage);

    public bool TryGetComponent(
        ComponentId id, [NotNullWhen(true)] out ComponentOperatingPoint? component) =>
        _components.TryGetValue(id, out component);

    private static ImmutableArray<PinOperatingPoint> PinsOf(
        TransientResult result, TransientSeries series, double time) =>
    [
        .. series.Pins.Select(pin => new PinOperatingPoint(
            pin.Name,
            new Quantity(result.VoltageAt(pin.Net, time), Unit.Volt),
            new Quantity(result.CurrentAt(series.Id, pin.Name, time), Unit.Ampere))),
    ];
}
