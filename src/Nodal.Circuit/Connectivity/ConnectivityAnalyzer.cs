using System.Collections.Immutable;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Collections;

namespace Nodal.Circuit.Connectivity;

public static class ConnectivityAnalyzer
{
    public static ConnectivityResult Analyze(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        return new ConnectivityGraph(schematic, catalog).Analyze();
    }
}

internal sealed class ConnectivityGraph
{
    private const string NetNamePrefix = "N";

    private readonly Schematic _schematic;
    private readonly ComponentCatalog _catalog;
    private readonly PinRefOrder _pinOrder;
    private readonly List<ConnectivityDiagnostic> _unknownDefinitions = [];
    private readonly List<ConnectivityDiagnostic> _globalNetShorts = [];
    private readonly List<PlacedPin> _pins = [];
    private readonly List<Wire> _wires;
    private readonly Dictionary<GridPoint, int> _pointIndices = [];
    private readonly List<GridPoint> _points = [];
    private readonly List<string> _globalNetNames = [];

    public ConnectivityGraph(Schematic schematic, ComponentCatalog catalog)
    {
        _schematic = schematic;
        _catalog = catalog;
        _pinOrder = PinRefOrder.For(schematic, catalog);
        _wires = [.. schematic.Wires.Values];
    }

    public ConnectivityResult Analyze()
    {
        CollectPins();
        CollectPoints();

        var classes = BuildClasses();
        var nets = NameNets(classes);
        var degrees = MeasureDegrees();

        return new ConnectivityResult(nets, Junctions(degrees), Diagnose(nets, degrees));
    }

    private void CollectPins()
    {
        foreach (var component in _schematic.Components.Values)
        {
            if (!_catalog.TryGetById(component.DefinitionId, out var definition))
            {
                _unknownDefinitions.Add(
                    ConnectivityDiagnostic.UnknownDefinition(component.Id, component.DefinitionId));
                continue;
            }

            foreach (var pin in definition.Pins)
            {
                var offset = component.Orientation.Transform(new GridPoint(pin.X, pin.Y));

                _pins.Add(new PlacedPin(
                    new PinRef(component.Id, pin.Name), component.Position + offset, pin.GlobalNet));
            }
        }
    }

    private void CollectPoints()
    {
        foreach (var wire in _wires)
        {
            AddPoint(wire.Start);
            AddPoint(wire.End);
        }

        foreach (var pin in _pins)
        {
            AddPoint(pin.Position);

            if (pin.GlobalNet is { } name && !_globalNetNames.Contains(name))
            {
                _globalNetNames.Add(name);
            }
        }
    }

    private void AddPoint(GridPoint point)
    {
        if (_pointIndices.TryAdd(point, _points.Count))
        {
            _points.Add(point);
        }
    }

    private List<EquivalenceClass> BuildClasses()
    {
        var set = new DisjointSet(_wires.Count + _pins.Count + _points.Count + _globalNetNames.Count);

        for (var index = 0; index < _pins.Count; index++)
        {
            set.Union(PinNode(index), PointNode(_pins[index].Position));

            if (_pins[index].GlobalNet is { } name)
            {
                set.Union(PinNode(index), GlobalNode(name));
            }
        }

        for (var index = 0; index < _wires.Count; index++)
        {
            foreach (var point in _points)
            {
                if (WireGeometry.Contains(_wires[index], point))
                {
                    set.Union(index, PointNode(point));
                }
            }
        }

        return GroupByRoot(set);
    }

    private List<EquivalenceClass> GroupByRoot(DisjointSet set)
    {
        var classesByRoot = new Dictionary<int, EquivalenceClass>();
        var ordered = new List<EquivalenceClass>();

        EquivalenceClass ClassOf(int node)
        {
            var root = set.Find(node);

            if (!classesByRoot.TryGetValue(root, out var found))
            {
                found = new EquivalenceClass();
                classesByRoot.Add(root, found);
                ordered.Add(found);
            }

            return found;
        }

        for (var index = 0; index < _pins.Count; index++)
        {
            var member = ClassOf(PinNode(index));
            member.Pins.Add(_pins[index].Pin);

            if (_pins[index].GlobalNet is { } name && !member.GlobalNetNames.Contains(name))
            {
                member.GlobalNetNames.Add(name);
            }
        }

        for (var index = 0; index < _wires.Count; index++)
        {
            ClassOf(index).Wires.Add(_wires[index].Id);
        }

        return ordered;
    }

    private ImmutableArray<Net> NameNets(List<EquivalenceClass> classes)
    {
        var globalNets = new List<(Net Net, EquivalenceClass Member)>();
        var pinNets = new List<EquivalenceClass>();
        var wireNets = new List<EquivalenceClass>();

        foreach (var member in classes)
        {
            member.Pins.Sort(_pinOrder);
            member.Wires.Sort();
            member.GlobalNetNames.Sort(StringComparer.Ordinal);

            if (member.GlobalNetNames.Count > 0)
            {
                globalNets.Add((ToNet(member, member.GlobalNetNames[0]), member));
            }
            else if (member.Pins.Count > 0)
            {
                pinNets.Add(member);
            }
            else
            {
                wireNets.Add(member);
            }
        }

        globalNets.Sort((left, right) => string.CompareOrdinal(left.Net.Name, right.Net.Name));
        pinNets.Sort((left, right) => _pinOrder.Compare(left.Pins[0], right.Pins[0]));
        wireNets.Sort((left, right) => left.Wires[0].CompareTo(right.Wires[0]));

        var nets = ImmutableArray.CreateBuilder<Net>(classes.Count);

        foreach (var (net, member) in globalNets)
        {
            nets.Add(net);

            if (member.GlobalNetNames.Count > 1)
            {
                _globalNetShorts.Add(ConnectivityDiagnostic.GlobalNetShort(
                    member.GlobalNetNames, net.Pins, net.Wires));
            }
        }

        var number = 1;

        foreach (var member in pinNets.Concat(wireNets))
        {
            nets.Add(ToNet(member, NetNamePrefix + number));
            number++;
        }

        return nets.ToImmutable();
    }

    private Dictionary<GridPoint, int> MeasureDegrees()
    {
        var degrees = new Dictionary<GridPoint, int>();

        foreach (var point in _points)
        {
            var degree = _pins.Count(pin => pin.Position == point);

            foreach (var wire in _wires)
            {
                if (wire.Start == point || wire.End == point)
                {
                    degree++;
                }
                else if (WireGeometry.ContainsInInterior(wire, point))
                {
                    degree += 2;
                }
            }

            degrees.Add(point, degree);
        }

        return degrees;
    }

    private static ImmutableArray<GridPoint> Junctions(Dictionary<GridPoint, int> degrees) =>
    [
        .. degrees
            .Where(entry => entry.Value >= 3)
            .Select(entry => entry.Key)
            .OrderBy(point => point.Y)
            .ThenBy(point => point.X),
    ];

    private ImmutableArray<ConnectivityDiagnostic> Diagnose(
        ImmutableArray<Net> nets, Dictionary<GridPoint, int> degrees)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ConnectivityDiagnostic>();

        diagnostics.AddRange(_unknownDefinitions);
        diagnostics.AddRange(_globalNetShorts);
        diagnostics.AddRange(FloatingPins(nets));
        diagnostics.AddRange(DanglingWireEnds(degrees));

        if (_schematic.Components.Count > 0 &&
            !nets.Any(net => net.Name == ConnectivityDiagnostic.GroundNetName))
        {
            diagnostics.Add(ConnectivityDiagnostic.NoGround());
        }

        return diagnostics.ToImmutable();
    }

    private IEnumerable<ConnectivityDiagnostic> FloatingPins(ImmutableArray<Net> nets) =>
        nets
            .Where(net => net.Pins.Length == 1)
            .Select(net => (Net: net, Pin: net.Pins[0]))
            .OrderBy(entry => entry.Pin, _pinOrder)
            .Select(entry => ConnectivityDiagnostic.FloatingPin(
                entry.Pin, entry.Net.Name, PositionOf(entry.Pin)));

    private IEnumerable<ConnectivityDiagnostic> DanglingWireEnds(Dictionary<GridPoint, int> degrees)
    {
        foreach (var wire in _wires)
        {
            foreach (var endpoint in new[] { wire.Start, wire.End })
            {
                if (degrees[endpoint] == 1 && !_pins.Any(pin => pin.Position == endpoint))
                {
                    yield return ConnectivityDiagnostic.DanglingWireEnd(wire.Id, endpoint);
                }
            }
        }
    }

    private GridPoint PositionOf(PinRef pin) =>
        _pins.First(placed => placed.Pin == pin).Position;

    private static Net ToNet(EquivalenceClass member, string name) =>
        new(name, [.. member.Pins], [.. member.Wires]);

    private int PinNode(int index) => _wires.Count + index;

    private int PointNode(GridPoint point) => _wires.Count + _pins.Count + _pointIndices[point];

    private int GlobalNode(string name) =>
        _wires.Count + _pins.Count + _points.Count + _globalNetNames.IndexOf(name);

    private sealed record PlacedPin(PinRef Pin, GridPoint Position, string? GlobalNet);

    private sealed class EquivalenceClass
    {
        public List<PinRef> Pins { get; } = [];

        public List<WireId> Wires { get; } = [];

        public List<string> GlobalNetNames { get; } = [];
    }
}
