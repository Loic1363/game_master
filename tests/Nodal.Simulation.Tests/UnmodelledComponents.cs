using System.Collections.Immutable;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;

namespace Nodal.Simulation.Tests;

internal static class UnmodelledComponents
{
    public const string Transformer = "transformer";
    public const string Relay = "relay";

    public static ComponentCatalog Catalog { get; } = new(
    [
        .. BaseCatalog.Catalog.Definitions,
        Definition(Transformer, "Transformer"),
        Definition(Relay, "Relay"),
    ]);

    public static Netlist Netlist(params string[] definitionIds) =>
        NetlistCompiler.Compile(Schematic(definitionIds), Catalog);

    public static Schematic Schematic(params string[] definitionIds)
    {
        var components = ImmutableArray.CreateBuilder<ComponentInstance>();
        var next = ComponentId.First;

        components.Add(Instance(next, "ground", "GND1", new GridPoint(0, 6)));
        next = new ComponentId(next.Value + 1);

        for (var index = 0; index < definitionIds.Length; index++)
        {
            components.Add(Instance(
                next,
                definitionIds[index],
                "X" + (index + 1),
                new GridPoint(6 * index, 0)));

            next = new ComponentId(next.Value + 1);
        }

        return Circuit.Schematics.Schematic.Create(
            components.ToImmutable(), [], next, WireId.First);
    }

    private static ComponentInstance Instance(
        ComponentId id, string definitionId, string designator, GridPoint position) => new(
        id,
        definitionId,
        designator,
        position,
        Orientation.Default,
        ImmutableSortedDictionary.Create<string, ParameterValue>(StringComparer.Ordinal));

    private static ComponentDefinition Definition(string definitionId, string name) => new(
        definitionId,
        name,
        "A component the simulator does not model.",
        ComponentCategory.Passive,
        "X",
        [new PinDefinition("1", -2, 0), new PinDefinition("2", 2, 0)],
        []);
}
