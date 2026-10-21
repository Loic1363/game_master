using System.Collections.Immutable;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Netlists;

public static class NetlistCompiler
{
    private const string UnconnectedNetName = "?";

    public static Netlist Compile(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        var connectivity = ConnectivityAnalyzer.Analyze(schematic, catalog);
        var elements = ImmutableArray.CreateBuilder<NetlistElement>(schematic.Components.Count);

        foreach (var component in schematic.Components.Values)
        {
            if (!catalog.TryGetById(component.DefinitionId, out var definition))
            {
                continue;
            }

            elements.Add(new NetlistElement(
                component.Id,
                component.Designator,
                component.DefinitionId,
                component.Parameters,
                Connections(connectivity, component.Id, definition)));
        }

        return new Netlist(elements.ToImmutable(), connectivity.NetNames, connectivity.Diagnostics);
    }

    private static ImmutableArray<PinConnection> Connections(
        ConnectivityResult connectivity, ComponentId id, ComponentDefinition definition)
    {
        var connections = ImmutableArray.CreateBuilder<PinConnection>(definition.Pins.Length);

        foreach (var pin in definition.Pins)
        {
            var name = connectivity.TryGetNetOfPin(new PinRef(id, pin.Name), out var net)
                ? net.Name
                : UnconnectedNetName;

            connections.Add(new PinConnection(pin.Name, name));
        }

        return connections.MoveToImmutable();
    }
}
