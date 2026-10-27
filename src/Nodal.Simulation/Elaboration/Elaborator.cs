using System.Collections.Immutable;
using System.Globalization;
using Nodal.Circuit.Netlists;
using Nodal.Content.Components;
using Nodal.Foundation.Waveforms;
using static Nodal.Simulation.Elaboration.ParameterValues;

namespace Nodal.Simulation.Elaboration;

public static class Elaborator
{
    public const string InternalNodeSuffix = "_int";

    public static ElaboratedCircuit Elaborate(Netlist netlist, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(netlist);
        ArgumentNullException.ThrowIfNull(catalog);

        var primitives = ImmutableArray.CreateBuilder<Primitive>();
        var components = ImmutableArray.CreateBuilder<ElaboratedComponent>();
        var nodes = netlist.NetNames.ToList();

        foreach (var element in netlist.Elements)
        {
            if (!ComponentModels.TryGetKind(element.DefinitionId, out var kind)
                || kind == ComponentKind.Ground
                || !catalog.TryGetById(element.DefinitionId, out var definition))
            {
                continue;
            }

            components.Add(Expand(element, definition, kind, primitives, nodes));
        }

        return new ElaboratedCircuit(
            primitives.ToImmutable(), components.ToImmutable(), [.. nodes], netlist.NetNames);
    }

    private static ElaboratedComponent Expand(
        NetlistElement element,
        ComponentDefinition definition,
        ComponentKind kind,
        ImmutableArray<Primitive>.Builder primitives,
        List<string> nodes)
    {
        var pins = PinsOf(element, kind);

        return ComponentModels.IsTransistor(kind)
            ? Transistor(element, definition, kind, pins, primitives)
            : TwoTerminal(element, definition, kind, pins, primitives, nodes);
    }

    private static ImmutableArray<ElaboratedPin> PinsOf(
        NetlistElement element, ComponentKind kind) =>
    [
        .. ComponentModels.PinsOf(kind).Select(
            name => new ElaboratedPin(name, ComponentModels.NetOf(element, name))),
    ];

    private static ElaboratedComponent Transistor(
        NetlistElement element,
        ComponentDefinition definition,
        ComponentKind kind,
        ImmutableArray<ElaboratedPin> pins,
        ImmutableArray<Primitive>.Builder primitives)
    {
        var index = primitives.Count;

        primitives.Add(new Primitive(
            element.Id,
            element.Designator,
            ComponentModels.PrimitiveOf(kind),
            PrimitiveRole.Principal,
            [.. pins.Select(pin => pin.Net)],
            0));

        return new ElaboratedComponent(
            element.Id,
            element.Designator,
            definition.Name,
            kind,
            pins[0].Net,
            pins[1].Net,
            pins,
            [index],
            [index],
            RatingsOf(element));
    }

    private static ElaboratedComponent TwoTerminal(
        NetlistElement element,
        ComponentDefinition definition,
        ComponentKind kind,
        ImmutableArray<ElaboratedPin> pins,
        ImmutableArray<Primitive>.Builder primitives,
        List<string> nodes)
    {
        var plus = pins[0].Net;
        var minus = pins[1].Net;
        var chain = Chain(element, definition, kind);
        var shunts = Shunts(element, kind);
        var internals = InternalNodes(element.Designator, chain.Count - 1);

        nodes.AddRange(internals);

        var owned = ImmutableArray.CreateBuilder<int>(chain.Count + shunts.Count);
        var terminals = ImmutableArray.CreateBuilder<int>(1 + shunts.Count);
        var from = plus;

        for (var index = 0; index < chain.Count; index++)
        {
            var to = index == chain.Count - 1 ? minus : internals[index];

            owned.Add(primitives.Count);

            if (index == 0)
            {
                terminals.Add(primitives.Count);
            }

            primitives.Add(chain[index].Between(element, from, to));
            from = to;
        }

        foreach (var shunt in shunts)
        {
            owned.Add(primitives.Count);
            terminals.Add(primitives.Count);
            primitives.Add(shunt.Between(element, plus, minus));
        }

        return new ElaboratedComponent(
            element.Id,
            element.Designator,
            definition.Name,
            kind,
            plus,
            minus,
            pins,
            owned.ToImmutable(),
            terminals.ToImmutable(),
            RatingsOf(element));
    }

    private static List<Part> Chain(
        NetlistElement element, ComponentDefinition definition, ComponentKind kind)
    {
        var parts = new List<Part> { Principal(element, definition, kind) };

        switch (kind)
        {
            case ComponentKind.Capacitor:
                AddSeries(
                    parts,
                    element,
                    CircuitParameters.Esr,
                    PrimitiveKind.Resistor,
                    PrimitiveRole.Esr);

                AddSeries(
                    parts,
                    element,
                    CircuitParameters.Esl,
                    PrimitiveKind.Inductor,
                    PrimitiveRole.Esl);

                break;

            case ComponentKind.Inductor:
            case ComponentKind.VoltageSource:
                AddSeries(
                    parts,
                    element,
                    CircuitParameters.SeriesResistance,
                    PrimitiveKind.Resistor,
                    PrimitiveRole.SeriesResistance);

                break;
        }

        return parts;
    }

    private static List<Part> Shunts(NetlistElement element, ComponentKind kind)
    {
        var parts = new List<Part>();

        switch (kind)
        {
            case ComponentKind.Capacitor:
                AddShuntResistance(parts, element);
                AddShuntCapacitance(parts, element);

                break;

            case ComponentKind.Inductor:
                AddShuntCapacitance(parts, element);

                break;

            case ComponentKind.CurrentSource:
                AddShuntResistance(parts, element);

                break;
        }

        return parts;
    }

    private static Part Principal(
        NetlistElement element, ComponentDefinition definition, ComponentKind kind) => kind switch
        {
            ComponentKind.Resistor => new Part(
                PrimitiveKind.Resistor,
                PrimitiveRole.Principal,
                Required(element, definition, CircuitParameters.Resistance)),

            ComponentKind.Capacitor => new Part(
                PrimitiveKind.Capacitor,
                PrimitiveRole.Principal,
                Required(element, definition, CircuitParameters.Capacitance)),

            ComponentKind.Inductor => new Part(
                PrimitiveKind.Inductor,
                PrimitiveRole.Principal,
                Required(element, definition, CircuitParameters.Inductance)),

            ComponentKind.VoltageSource => new Part(
                PrimitiveKind.VoltageSource,
                PrimitiveRole.Principal,
                Required(element, definition, CircuitParameters.Voltage),
                Function(element)),

            ComponentKind.CurrentSource => new Part(
                PrimitiveKind.CurrentSource,
                PrimitiveRole.Principal,
                Required(element, definition, CircuitParameters.Current),
                Function(element)),

            _ => new Part(PrimitiveKind.Diode, PrimitiveRole.Principal, 0),
        };

    private static void AddSeries(
        List<Part> parts,
        NetlistElement element,
        string parameterName,
        PrimitiveKind kind,
        PrimitiveRole role)
    {
        if (Positive(element, parameterName) is { } value)
        {
            parts.Add(new Part(kind, role, value));
        }
    }

    private static void AddShuntResistance(List<Part> parts, NetlistElement element)
    {
        if (Optional(element, CircuitParameters.ParallelResistance) is { } resistance
            && resistance >= 0)
        {
            parts.Add(new Part(
                PrimitiveKind.Resistor, PrimitiveRole.ParallelResistance, resistance));
        }
    }

    private static void AddShuntCapacitance(List<Part> parts, NetlistElement element)
    {
        if (Positive(element, CircuitParameters.ParallelCapacitance) is { } capacitance)
        {
            parts.Add(new Part(
                PrimitiveKind.Capacitor, PrimitiveRole.ParallelCapacitance, capacitance));
        }
    }

    private static ImmutableArray<string> InternalNodes(string designator, int count)
    {
        if (count <= 0)
        {
            return [];
        }

        if (count == 1)
        {
            return [designator + InternalNodeSuffix];
        }

        return
        [
            .. Enumerable.Range(1, count).Select(
                index => designator
                    + InternalNodeSuffix
                    + index.ToString(CultureInfo.InvariantCulture)),
        ];
    }

    private static ElementRatings RatingsOf(NetlistElement element) => new(
        Optional(element, CircuitParameters.PowerRating),
        Optional(element, CircuitParameters.VoltageRating),
        Optional(element, CircuitParameters.CurrentRating));

    private readonly record struct Part(
        PrimitiveKind Kind, PrimitiveRole Role, double Value, Waveform? Function = null)
    {
        public Primitive Between(NetlistElement element, string plus, string minus) =>
            new(element.Id, element.Designator, Kind, Role, plus, minus, Value, Function);
    }
}
