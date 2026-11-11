using System.Text;
using Nodal.Circuit.Persistence;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Persistence;

public class SchematicLimitTests
{
    private const long OneMegabyte = 1024 * 1024;

    [Fact]
    public void AFileOfExactlyTheMaximumSizeIsAccepted() =>
        Assert.Null(SchematicFile.FileSizeProblem(SchematicLimits.MaxFileBytes));

    [Fact]
    public void AFileOneByteAboveTheMaximumSizeIsRefused() =>
        Assert.Equal(
            "The file is too large to open: 16.0 MB, while the limit is 16 MB.",
            SchematicFile.FileSizeProblem(SchematicLimits.MaxFileBytes + 1));

    [Fact]
    public void TheRefusalStatesTheSizeOfTheFileWithOneDecimal() =>
        Assert.Equal(
            "The file is too large to open: 128.5 MB, while the limit is 16 MB.",
            SchematicFile.FileSizeProblem((long)(128.5 * OneMegabyte)));

    [Fact]
    public void AnEmptyFileIsSmallEnough() => Assert.Null(SchematicFile.FileSizeProblem(0));

    [Fact]
    public void AFileAtTheComponentLimitIsRead()
    {
        var result = Read(Components(SchematicLimits.MaxComponents));

        Assert.True(result.Loaded, Listed(result));
        Assert.Equal(SchematicLimits.MaxComponents, result.Schematic!.Components.Count);
    }

    [Fact]
    public void AFileAboveTheComponentLimitIsRefused()
    {
        var result = Read(Components(SchematicLimits.MaxComponents + 1));

        Assert.False(result.Loaded);
        Assert.Contains(
            "The file has too many components: 20,001, while the limit is 20,000.",
            result.Errors.Select(error => error.Message));
    }

    [Fact]
    public void AFileAtTheWireLimitIsRead()
    {
        var result = Read(Wires(SchematicLimits.MaxWires));

        Assert.True(result.Loaded, Listed(result));
        Assert.Equal(SchematicLimits.MaxWires, result.Schematic!.Wires.Count);
    }

    [Fact]
    public void AFileAboveTheWireLimitIsRefused()
    {
        var result = Read(Wires(SchematicLimits.MaxWires + 1));

        Assert.False(result.Loaded);
        Assert.Contains(
            "The file has too many wires: 100,001, while the limit is 100,000.",
            result.Errors.Select(error => error.Message));
    }

    [Fact]
    public void TheDiagnosticsOfAReadFileAreCappedWithACountingSummary()
    {
        var result = Read(SchematicJson.Single("resistor", "R1", UnknownParameters(250)).ToString());

        Assert.True(result.Loaded, Listed(result));
        Assert.Equal(SchematicLimits.MaxDiagnostics + 1, result.Diagnostics.Length);
        Assert.Equal("… and 50 more problems.", result.Diagnostics[^1].Message);
        Assert.Equal(SchematicSeverity.Warning, result.Diagnostics[^1].Severity);
    }

    [Fact]
    public void TheSummaryOfARefusedFileIsAnErrorSoAReasonIsAlwaysVisible()
    {
        var result = Read(SchematicJson.Valid().With("components", Diagonals(250)).ToString());

        Assert.False(result.Loaded);
        Assert.Equal(SchematicLimits.MaxDiagnostics + 1, result.Diagnostics.Length);
        Assert.Equal("… and 50 more problems.", result.Diagnostics[^1].Message);
        Assert.Equal(SchematicSeverity.Error, result.Diagnostics[^1].Severity);
    }

    private static SchematicReadResult Read(string text) =>
        SchematicFile.Read(text, BaseCatalog.Catalog);

    private static string Listed(SchematicReadResult result) =>
        string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.ToString()));

    private static string Components(int count)
    {
        var entries = new StringBuilder();

        for (var index = 1; index <= count; index++)
        {
            entries.Append(entries.Length == 0 ? string.Empty : ",");
            entries.Append(
                $$"""
                { "id": {{index}}, "definition": "resistor", "designator": "R{{index}}",
                  "x": {{index * 6}}, "y": 0, "rotation": 0, "mirrored": false,
                  "parameters": { "resistance": 1000 } }
                """);
        }

        return SchematicJson.Valid()
            .With("components", "[" + entries + "]")
            .With("nextComponentId", (count + 1).ToString())
            .With("wires", "[]")
            .With("nextWireId", "1")
            .ToString();
    }

    private static string Wires(int count)
    {
        var entries = new StringBuilder();

        for (var index = 1; index <= count; index++)
        {
            entries.Append(entries.Length == 0 ? string.Empty : ",");
            entries.Append(
                $$"""{ "id": {{index}}, "from": [0, {{index * 4}}], "to": [0, {{(index * 4) + 2}}] }""");
        }

        return SchematicJson.Valid()
            .With("components", "[]")
            .With("nextComponentId", "1")
            .With("wires", "[" + entries + "]")
            .With("nextWireId", (count + 1).ToString())
            .ToString();
    }

    private static string UnknownParameters(int count)
    {
        var entries = new StringBuilder("{ \"resistance\": 1000");

        for (var index = 1; index <= count; index++)
        {
            entries.Append($", \"unknown{index}\": {index}");
        }

        return entries.Append(" }").ToString();
    }

    private static string Diagonals(int count)
    {
        var entries = new StringBuilder();

        for (var index = 1; index <= count; index++)
        {
            entries.Append(entries.Length == 0 ? string.Empty : ",");
            entries.Append(
                $$"""
                { "id": {{index}}, "definition": "resistor", "designator": "R{{index}}",
                  "x": {{index * 6}}, "y": 0, "rotation": 7, "mirrored": false,
                  "parameters": { "resistance": 1000 } }
                """);
        }

        return "[" + entries + "]";
    }
}
