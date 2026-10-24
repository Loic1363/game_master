using System.Collections.Immutable;
using Nodal.Circuit.Netlists;
using Nodal.Content.Components;

namespace Nodal.Simulation.Dc;

internal static class DcElements
{
    public static ImmutableArray<DcElement> Resolve(Netlist netlist, ComponentCatalog catalog)
    {
        var elements = ImmutableArray.CreateBuilder<DcElement>(netlist.Elements.Length);

        foreach (var element in netlist.Elements)
        {
            if (!DcModels.TryGetKind(element.DefinitionId, out var kind)
                || kind == DcModelKind.Ground
                || !catalog.TryGetById(element.DefinitionId, out var definition))
            {
                continue;
            }

            elements.Add(Resolve(element, definition, kind));
        }

        return elements.ToImmutable();
    }

    public static bool IsSupported(NetlistElement element, out DcModelKind kind) =>
        DcModels.TryGetKind(element.DefinitionId, out kind)
        && DcModels.PinsOf(kind).All(name => HasPin(element, name));

    private static DcElement Resolve(
        NetlistElement element, ComponentDefinition definition, DcModelKind kind)
    {
        var pins = DcModels.PinsOf(kind);

        return new DcElement(
            element.Id,
            element.Designator,
            kind,
            NetOf(element, pins[0]),
            NetOf(element, pins[1]),
            ConductanceOf(element, definition, kind),
            BranchOf(element, definition, kind),
            kind == DcModelKind.CurrentSource
                ? Required(element, definition, DcParameters.Current)
                : 0,
            RatingsOf(element));
    }

    private static double? ConductanceOf(
        NetlistElement element, ComponentDefinition definition, DcModelKind kind)
    {
        var resistance = kind switch
        {
            DcModelKind.Resistor => Required(element, definition, DcParameters.Resistance),
            DcModelKind.Capacitor or DcModelKind.CurrentSource =>
                Optional(element, DcParameters.ParallelResistance),
            DcModelKind.Inductor => Optional(element, DcParameters.SeriesResistance),
            _ => null,
        };

        return resistance is > 0 ? 1 / resistance.Value : null;
    }

    private static DcBranch? BranchOf(
        NetlistElement element, ComponentDefinition definition, DcModelKind kind)
    {
        if (kind == DcModelKind.VoltageSource)
        {
            var series = Optional(element, DcParameters.SeriesResistance);

            return new DcBranch(
                Required(element, definition, DcParameters.Voltage),
                series is > 0 ? series.Value : 0);
        }

        if (kind == DcModelKind.Inductor)
        {
            return Optional(element, DcParameters.SeriesResistance) is > 0 ? null : DcBranch.Short;
        }

        if (kind == DcModelKind.Resistor)
        {
            return Required(element, definition, DcParameters.Resistance) > 0 ? null : DcBranch.Short;
        }

        return Optional(element, DcParameters.ParallelResistance) is { } parallel and <= 0
            ? DcBranch.Short
            : null;
    }

    private static DcRatings RatingsOf(NetlistElement element) => new(
        Optional(element, DcParameters.PowerRating),
        Optional(element, DcParameters.VoltageRating),
        Optional(element, DcParameters.CurrentRating));

    private static double Required(
        NetlistElement element, ComponentDefinition definition, string parameterName)
    {
        if (Optional(element, parameterName) is { } value)
        {
            return value;
        }

        var declared = definition.Parameters.FirstOrDefault(
            parameter => string.Equals(parameter.Name, parameterName, StringComparison.Ordinal));

        return declared?.Default?.Value ?? 0;
    }

    private static double? Optional(NetlistElement element, string parameterName) =>
        element.Parameters.TryGetValue(parameterName, out var value) ? value.Value : null;

    private static string NetOf(NetlistElement element, string pinName) =>
        element.Pins.First(
            pin => string.Equals(pin.PinName, pinName, StringComparison.Ordinal)).NetName;

    private static bool HasPin(NetlistElement element, string pinName) =>
        element.Pins.Any(pin => string.Equals(pin.PinName, pinName, StringComparison.Ordinal));
}
