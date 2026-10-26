using System.Collections.Immutable;
using System.Text.Json;

namespace Nodal.Simulation.Tests.Reference;

internal sealed record ReferenceExpectation(
    string Generator,
    ImmutableSortedDictionary<string, double> NodeVoltages,
    ImmutableSortedDictionary<string, double> ElementCurrents)
{
    private const string GeneratorField = "generator";
    private const string NodeVoltagesField = "nodeVoltages";
    private const string ElementCurrentsField = "elementCurrents";

    public static ReferenceExpectation Parse(string json)
    {
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        return new ReferenceExpectation(
            root.GetProperty(GeneratorField).GetString() ?? string.Empty,
            Numbers(root.GetProperty(NodeVoltagesField)),
            Numbers(root.GetProperty(ElementCurrentsField)));
    }

    private static ImmutableSortedDictionary<string, double> Numbers(JsonElement element)
    {
        var numbers = ImmutableSortedDictionary.CreateBuilder<string, double>(
            StringComparer.Ordinal);

        foreach (var property in element.EnumerateObject())
        {
            numbers[property.Name] = property.Value.GetDouble();
        }

        return numbers.ToImmutable();
    }
}
