using Nodal.Circuit.Commands;
using Nodal.Circuit.Persistence;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Persistence;

public class AnalysisSettingsFileTests
{
    private static Schematic WithTransient(TransientSettings settings)
    {
        var result = new SetAnalysisSettings(new AnalysisSettings(settings))
            .Apply(Schematic.Empty, BaseCatalog.Catalog);

        Assert.True(result.Succeeded);

        return result.Schematic;
    }

    private static SchematicReadResult ReadWith(string analysis) => SchematicFile.Read(
        SchematicJson.Valid().With("analysis", analysis).ToString(), BaseCatalog.Catalog);

    [Fact]
    public void ASchematicWithoutAnalysisSettingsWritesNoAnalysisField()
    {
        var text = SchematicFile.Write(Schematic.Empty);

        Assert.DoesNotContain("\"analysis\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TheTransientSettingsAreWrittenWithExactNumbersAndExplicitNulls()
    {
        var text = SchematicFile.Write(
            WithTransient(new TransientSettings(0.01, 1e-5)));

        Assert.Contains("\"analysis\": {", text, StringComparison.Ordinal);
        Assert.Contains("\"transient\": {", text, StringComparison.Ordinal);
        Assert.Contains("\"stopTime\": 0.01", text, StringComparison.Ordinal);
        Assert.Contains("\"outputStep\": 1E-05", text, StringComparison.Ordinal);
        Assert.Contains("\"maxStep\": null", text, StringComparison.Ordinal);
        Assert.Contains("\"startFromZero\": false", text, StringComparison.Ordinal);
    }

    public static TheoryData<string, TransientSettings> Settings => new()
    {
        { "stop time only", new TransientSettings(0.01) },
        { "every field", new TransientSettings(0.01, 1e-5, 2e-7, true) },
        { "output step only", new TransientSettings(1e-3, 1e-6) },
        { "max step only", new TransientSettings(1e-3, null, 1e-9) },
        { "starting from zero", new TransientSettings(0.5, null, null, true) },
    };

    [Theory]
    [MemberData(nameof(Settings))]
    public void TheTransientSettingsSurviveWritingAndReading(
        string name, TransientSettings settings)
    {
        var text = SchematicFile.Write(WithTransient(settings));
        var result = SchematicFile.Read(text, BaseCatalog.Catalog);

        Assert.True(result.Loaded, name);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(settings, result.Schematic.Analysis.Transient);
    }

    [Fact]
    public void AFileWithoutAnalysisSettingsReadsAsNone()
    {
        var result = SchematicFile.Read(SchematicJson.Valid().ToString(), BaseCatalog.Catalog);

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(AnalysisSettings.None, result.Schematic.Analysis);
        Assert.True(result.Schematic.Analysis.IsEmpty);
    }

    [Fact]
    public void AnAnalysisObjectWithoutATransientReadsAsNone()
    {
        var result = ReadWith("{}");

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
        Assert.True(result.Schematic.Analysis.IsEmpty);
    }

    [Theory]
    [InlineData("3", "'analysis' must be an object")]
    [InlineData("\"transient\"", "'analysis' must be an object")]
    [InlineData("""{ "transient": 3 }""", "'transient' must be an object")]
    public void AMistypedAnalysisObjectIsDroppedWithAWarning(string analysis, string expected)
    {
        var result = ReadWith(analysis);

        Assert.True(result.Loaded);
        Assert.True(result.Schematic.Analysis.IsEmpty);

        var warning = Assert.Single(result.Warnings);

        Assert.Contains(expected, warning.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{ "transient": {} }""")]
    [InlineData("""{ "transient": { "outputStep": 1e-5 } }""")]
    [InlineData("""{ "transient": { "stopTime": 0 } }""")]
    [InlineData("""{ "transient": { "stopTime": -1 } }""")]
    [InlineData("""{ "transient": { "stopTime": "fast" } }""")]
    [InlineData("""{ "transient": { "stopTime": 1, "outputStep": 0 } }""")]
    [InlineData("""{ "transient": { "stopTime": 1, "outputStep": -1e-6 } }""")]
    [InlineData("""{ "transient": { "stopTime": 1, "maxStep": 0 } }""")]
    [InlineData("""{ "transient": { "stopTime": 1, "maxStep": "small" } }""")]
    [InlineData("""{ "transient": { "stopTime": 1, "startFromZero": "yes" } }""")]
    public void InvalidTransientSettingsAreDroppedWithAWarning(string analysis)
    {
        var result = ReadWith(analysis);

        Assert.True(result.Loaded);
        Assert.Empty(result.Errors);
        Assert.True(result.Schematic.Analysis.IsEmpty);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(
            result.Warnings,
            warning => warning.Path == "$.analysis.transient"
                || warning.Path.StartsWith("$.analysis.transient.", StringComparison.Ordinal));
    }

    [Fact]
    public void AnUnknownFieldInsideTheAnalysisIsIgnoredWithAWarning()
    {
        var result = ReadWith("""{ "transient": { "stopTime": 1e-3 }, "ac": {} }""");

        Assert.True(result.Loaded);
        Assert.Equal(new TransientSettings(1e-3), result.Schematic.Analysis.Transient);

        var warning = Assert.Single(result.Warnings);

        Assert.Contains("unknown field 'ac' was ignored", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownFieldInsideTheTransientIsIgnoredWithAWarning()
    {
        var result = ReadWith("""{ "transient": { "stopTime": 1e-3, "relTol": 1e-6 } }""");

        Assert.True(result.Loaded);
        Assert.Equal(new TransientSettings(1e-3), result.Schematic.Analysis.Transient);

        var warning = Assert.Single(result.Warnings);

        Assert.Contains(
            "unknown field 'relTol' was ignored", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitNullsReadAsUnsetOptionalSteps()
    {
        var result = ReadWith(
            """
            { "transient": { "stopTime": 1e-3, "outputStep": null, "maxStep": null,
              "startFromZero": null } }
            """);

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new TransientSettings(1e-3), result.Schematic.Analysis.Transient);
    }

    [Fact]
    public void TheSchemaVersionStaysOne()
    {
        var text = SchematicFile.Write(WithTransient(new TransientSettings(0.01)));

        Assert.Contains("\"schemaVersion\": 1", text, StringComparison.Ordinal);
    }
}
