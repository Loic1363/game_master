using System.Collections.Immutable;
using System.Text.Json;

namespace Nodal.Simulation.Tests.Reference;

internal sealed record ReferenceProbes(
    ImmutableArray<string> Nodes,
    ImmutableArray<(string Designator, ImmutableArray<string> Elements)> Components)
{
    private const string NodesField = "nodes";
    private const string ComponentsField = "components";
    private const string DesignatorField = "designator";
    private const string ElementsField = "elements";

    public static ReferenceProbes Parse(string json)
    {
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;
        var components =
            ImmutableArray.CreateBuilder<(string, ImmutableArray<string>)>();

        foreach (var component in root.GetProperty(ComponentsField).EnumerateArray())
        {
            components.Add((
                component.GetProperty(DesignatorField).GetString() ?? string.Empty,
                Strings(component.GetProperty(ElementsField))));
        }

        return new ReferenceProbes(
            Strings(root.GetProperty(NodesField)), components.ToImmutable());
    }

    private static ImmutableArray<string> Strings(JsonElement element) =>
    [
        .. element.EnumerateArray().Select(value => value.GetString() ?? string.Empty),
    ];
}
