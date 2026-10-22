using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Persistence;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Circuit.Tests.Persistence;

public class SchematicFileWriteTests
{
    [Fact]
    public void TheSameSchematicAlwaysGivesTheSameText()
    {
        var first = SchematicFile.Write(GoldenSchematic.Build());
        var second = SchematicFile.Write(GoldenSchematic.Build());

        Assert.Equal(first, second);
    }

    [Fact]
    public void TheDemoSchematicMatchesTheGoldenFile()
    {
        Assert.Equal(GoldenSchematic.GoldenText(), SchematicFile.Write(GoldenSchematic.Build()));
    }

    [Fact]
    public void TheGoldenFileReadsBackAsTheDemoSchematic()
    {
        var result = SchematicFile.Read(GoldenSchematic.GoldenText(), BaseCatalog.Catalog);

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
        SchematicAssert.Equal(GoldenSchematic.Build(), result.Schematic);
    }

    [Fact]
    public void TheTextUsesLineFeedsAndEndsWithOne()
    {
        var text = SchematicFile.Write(GoldenSchematic.Build());

        Assert.DoesNotContain("\r", text);
        Assert.EndsWith("}\n", text);
        Assert.Contains("\n  \"kind\": \"schematic\",\n", text);
    }

    [Fact]
    public void ComponentsAndWiresAreWrittenInAscendingIdentifierOrder()
    {
        var text = SchematicFile.Write(GoldenSchematic.Build());

        Assert.Equal(
            [1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 6],
            IdentifiersIn(text));
    }

    [Fact]
    public void ParametersAreWrittenInNameOrder()
    {
        var schematic = new AddComponent("capacitor", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);

        schematic = new SetParameter(ComponentId.First, "esr", new Quantity(0.01, Unit.Ohm))
            .Succeed(schematic);

        schematic = new SetParameter(ComponentId.First, "voltage-rating", new Quantity(16, Unit.Volt))
            .Succeed(schematic);

        var text = SchematicFile.Write(schematic);

        Assert.True(text.IndexOf("\"capacitance\"", StringComparison.Ordinal)
            < text.IndexOf("\"esr\"", StringComparison.Ordinal));

        Assert.True(text.IndexOf("\"esr\"", StringComparison.Ordinal)
            < text.IndexOf("\"voltage-rating\"", StringComparison.Ordinal));
    }

    [Fact]
    public void ValuesAreWrittenAsPlainNumbersInBaseUnits()
    {
        var schematic = new AddComponent("capacitor", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);

        Assert.Contains("\"capacitance\": 1E-07", SchematicFile.Write(schematic));
    }

    private static IEnumerable<int> IdentifiersIn(string text) =>
        text.Split('\n')
            .Where(line => line.Contains("\"id\":", StringComparison.Ordinal))
            .Select(line => int.Parse(
                line.Split(':')[1].Trim().TrimEnd(','), System.Globalization.CultureInfo.InvariantCulture));
}
