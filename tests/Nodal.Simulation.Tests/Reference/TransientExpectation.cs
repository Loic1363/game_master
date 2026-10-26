using System.Collections.Immutable;
using System.Text.Json;
using Nodal.Circuit.Schematics;

namespace Nodal.Simulation.Tests.Reference;

internal sealed record TransientExpectation(
    string Generator,
    TransientSettings Settings,
    double RelativeToRange,
    double VoltageFloor,
    double CurrentFloor,
    ImmutableArray<double> Times,
    ImmutableSortedDictionary<string, ImmutableArray<double>> NodeVoltages,
    ImmutableSortedDictionary<string, ImmutableArray<double>> ElementCurrents)
{
    private const string GeneratorField = "generator";
    private const string TransientField = "transient";
    private const string ToleranceField = "tolerance";
    private const string TimesField = "times";
    private const string NodeVoltagesField = "nodeVoltages";
    private const string ElementCurrentsField = "elementCurrents";
    private const string StopTimeField = "stopTime";
    private const string OutputStepField = "outputStep";
    private const string MaxStepField = "maxStep";
    private const string StartFromZeroField = "startFromZero";
    private const string RelativeToRangeField = "relativeToRange";
    private const string VoltageFloorField = "voltageFloor";
    private const string CurrentFloorField = "currentFloor";

    public static TransientExpectation Parse(string json)
    {
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;
        var transient = root.GetProperty(TransientField);
        var tolerance = root.GetProperty(ToleranceField);

        return new TransientExpectation(
            root.GetProperty(GeneratorField).GetString() ?? string.Empty,
            new TransientSettings(
                transient.GetProperty(StopTimeField).GetDouble(),
                Optional(transient, OutputStepField),
                Optional(transient, MaxStepField),
                transient.GetProperty(StartFromZeroField).GetBoolean()),
            tolerance.GetProperty(RelativeToRangeField).GetDouble(),
            tolerance.GetProperty(VoltageFloorField).GetDouble(),
            tolerance.GetProperty(CurrentFloorField).GetDouble(),
            Values(root.GetProperty(TimesField)),
            Series(root.GetProperty(NodeVoltagesField)),
            Series(root.GetProperty(ElementCurrentsField)));
    }

    public double AllowedFor(ImmutableArray<double> expected, double floor)
    {
        var range = expected.Max() - expected.Min();

        return Math.Max(floor, RelativeToRange * range);
    }

    private static double? Optional(JsonElement element, string name) =>
        element.TryGetProperty(name, out var field) && field.ValueKind == JsonValueKind.Number
            ? field.GetDouble()
            : null;

    private static ImmutableArray<double> Values(JsonElement element) =>
        [.. element.EnumerateArray().Select(value => value.GetDouble())];

    private static ImmutableSortedDictionary<string, ImmutableArray<double>> Series(
        JsonElement element)
    {
        var series = ImmutableSortedDictionary.CreateBuilder<string, ImmutableArray<double>>(
            StringComparer.Ordinal);

        foreach (var property in element.EnumerateObject())
        {
            series[property.Name] = Values(property.Value);
        }

        return series.ToImmutable();
    }
}
