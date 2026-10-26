using System.Collections.Immutable;
using System.Globalization;
using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Persistence;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Persistence;

public class ProbeFileTests
{
    private static readonly PinRef ResistorPin = new(new ComponentId(1), "1");

    private static Schematic Base() => Loaded(SchematicJson.Valid().ToString());

    private static Schematic Loaded(string text)
    {
        var result = SchematicFile.Read(text, BaseCatalog.Catalog);

        Assert.True(result.Loaded);

        return result.Schematic;
    }

    private static Schematic Probed(params Probe[] probes)
    {
        var schematic = Base();

        foreach (var probe in probes)
        {
            schematic = new AddProbe(probe).Succeed(schematic);
        }

        return schematic;
    }

    private static SchematicReadResult ReadWith(string probes) =>
        ReadWith(SchematicJson.Valid(), probes);

    private static SchematicReadResult ReadWith(SchematicJson json, string probes) =>
        SchematicFile.Read(
            json.With("analysis", $$"""{ "probes": {{probes}} }""").ToString(),
            BaseCatalog.Catalog);

    private static SchematicJson WithWires(int count)
    {
        var wires = Enumerable.Range(1, count).Select(
            id => $$"""{ "id": {{id}}, "from": [0, {{id * 2}}], "to": [4, {{id * 2}}] }""");

        return SchematicJson.Valid()
            .With("wires", "[" + string.Join(", ", wires) + "]")
            .With("nextWireId", (count + 1).ToString(CultureInfo.InvariantCulture));
    }

    private static string WireProbes(int count) =>
        "["
        + string.Join(
            ", ",
            Enumerable.Range(1, count).Select(
                id => $$"""{ "voltage": { "wire": {{id}} } }"""))
        + "]";

    [Fact]
    public void ASchematicWithoutProbesWritesNoAnalysisField()
    {
        var text = SchematicFile.Write(Base());

        Assert.DoesNotContain("\"analysis\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void EachProbeShapeIsWrittenAsSpecified()
    {
        var text = SchematicFile.Write(Probed(
            Probe.At(ResistorPin), Probe.At(new WireId(1)), Probe.Through(new ComponentId(2))));

        Assert.Contains("\"probes\": [", text, StringComparison.Ordinal);
        Assert.Contains("\"voltage\": {", text, StringComparison.Ordinal);
        Assert.Contains("\"component\": 1", text, StringComparison.Ordinal);
        Assert.Contains("\"pin\": \"1\"", text, StringComparison.Ordinal);
        Assert.Contains("\"wire\": 1", text, StringComparison.Ordinal);
        Assert.Contains("\"current\": {", text, StringComparison.Ordinal);
        Assert.Contains("\"component\": 2", text, StringComparison.Ordinal);
        Assert.Contains("\"schemaVersion\": 1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TheProbesSurviveWritingAndReadingInOrder()
    {
        ImmutableArray<Probe> probes =
        [
            Probe.At(new WireId(1)),
            Probe.Through(new ComponentId(2)),
            Probe.At(ResistorPin),
        ];

        var result = SchematicFile.Read(
            SchematicFile.Write(Probed([.. probes])), BaseCatalog.Catalog);

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(probes, result.Schematic.Analysis.Probes);
    }

    [Fact]
    public void ProbesAndTransientSettingsLiveSideBySide()
    {
        var probed = new SetAnalysisSettings(
                new AnalysisSettings(new TransientSettings(1e-3), [Probe.At(ResistorPin)]))
            .Succeed(Base());

        var result = SchematicFile.Read(SchematicFile.Write(probed), BaseCatalog.Catalog);

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new TransientSettings(1e-3), result.Schematic.Analysis.Transient);
        Assert.Equal(Probe.At(ResistorPin), Assert.Single(result.Schematic.Analysis.Probes));
    }

    [Fact]
    public void WritingTheSameProbesTwiceGivesTheSameText()
    {
        var probed = Probed(Probe.At(ResistorPin), Probe.Through(new ComponentId(2)));

        Assert.Equal(SchematicFile.Write(probed), SchematicFile.Write(probed));
    }

    [Fact]
    public void AProbeWhoseComponentIsGoneIsPrunedOnSave()
    {
        var probed = Probed(Probe.Through(new ComponentId(1)), Probe.At(new WireId(1)));
        var removed = Edits.Succeed(new RemoveComponent(new ComponentId(1)), probed);

        Assert.Equal(2, removed.Analysis.Probes.Length);

        var result = SchematicFile.Read(SchematicFile.Write(removed), BaseCatalog.Catalog);

        Assert.True(result.Loaded);
        Assert.Equal(Probe.At(new WireId(1)), Assert.Single(result.Schematic.Analysis.Probes));
    }

    [Fact]
    public void AProbeWhoseWireIsGoneIsPrunedOnSave()
    {
        var probed = Probed(Probe.At(new WireId(1)), Probe.At(ResistorPin));
        var removed = Edits.Succeed(new RemoveWire(new WireId(1)), probed);

        var result = SchematicFile.Read(SchematicFile.Write(removed), BaseCatalog.Catalog);

        Assert.True(result.Loaded);
        Assert.Equal(Probe.At(ResistorPin), Assert.Single(result.Schematic.Analysis.Probes));
    }

    [Fact]
    public void AnAnalysisObjectWithAnEmptyProbeListReadsAsNone()
    {
        var result = ReadWith("[]");

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
        Assert.True(result.Schematic.Analysis.IsEmpty);
    }

    [Theory]
    [InlineData("""[{ "voltage": { "component": 99, "pin": "1" } }]""", "no component carries")]
    [InlineData("""[{ "current": { "component": 99 } }]""", "no component carries")]
    [InlineData("""[{ "voltage": { "component": 1, "pin": "9" } }]""", "has no pin '9'")]
    [InlineData("""[{ "voltage": { "wire": 0 } }]""", "positive whole number")]
    [InlineData("""[{ "voltage": { "wire": 99 } }]""", "no wire carries")]
    [InlineData("""[{ "voltage": { "wire": "one" } }]""", "positive whole number")]
    [InlineData("""[{ "voltage": { "component": 1 } }]""", "must be a pin name")]
    [InlineData("""[{ "voltage": {} }]""", "either 'component'")]
    [InlineData("""[{ "voltage": { "component": 1, "wire": 1 } }]""", "either 'component'")]
    [InlineData("""[{ "voltage": 3 }]""", "probe target must be an object")]
    [InlineData("""[{ "current": { } }]""", "positive whole number")]
    [InlineData("""[{}]""", "exactly one of 'voltage'")]
    [InlineData(
        """[{ "voltage": { "wire": 1 }, "current": { "component": 1 } }]""",
        "exactly one of 'voltage'")]
    [InlineData("""[3]""", "a probe must be an object")]
    public void AnInvalidOrDanglingProbeIsDroppedWithAWarning(string probes, string expected)
    {
        var result = ReadWith(probes);

        Assert.True(result.Loaded);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Schematic.Analysis.Probes);

        Assert.Contains(
            result.Warnings,
            warning => warning.Message.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void AMistypedProbeListIsDroppedWithAWarning()
    {
        var result = ReadWith("""{ "voltage": { "wire": 1 } }""");

        Assert.True(result.Loaded);
        Assert.Empty(result.Schematic.Analysis.Probes);

        var warning = Assert.Single(result.Warnings);

        Assert.Contains("'probes' must be an array", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AValidProbeBesideAnInvalidOneIsKept()
    {
        var result = ReadWith(
            """[{ "current": { "component": 99 } }, { "voltage": { "wire": 1 } }]""");

        Assert.True(result.Loaded);
        Assert.Equal(Probe.At(new WireId(1)), Assert.Single(result.Schematic.Analysis.Probes));
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void ARepeatedProbeIsDroppedWithAWarning()
    {
        var result = ReadWith("""[{ "voltage": { "wire": 1 } }, { "voltage": { "wire": 1 } }]""");

        Assert.True(result.Loaded);
        Assert.Single(result.Schematic.Analysis.Probes);

        var warning = Assert.Single(result.Warnings);

        Assert.Contains("listed more than once", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheEighthProbeIsStillRead()
    {
        var count = AnalysisSettings.MaxProbes;
        var result = ReadWith(WithWires(count), WireProbes(count));

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(count, result.Schematic.Analysis.Probes.Length);
    }

    [Fact]
    public void AProbePastTheLimitIsDroppedWithAWarning()
    {
        var count = AnalysisSettings.MaxProbes + 1;
        var result = ReadWith(WithWires(count), WireProbes(count));

        Assert.True(result.Loaded);
        Assert.Empty(result.Errors);
        Assert.Equal(AnalysisSettings.MaxProbes, result.Schematic.Analysis.Probes.Length);

        var warning = Assert.Single(result.Warnings);

        Assert.Contains("at most 8 probes", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownFieldInsideAProbeIsIgnoredWithAWarning()
    {
        var result = ReadWith("""[{ "voltage": { "wire": 1 }, "colour": "red" }]""");

        Assert.True(result.Loaded);
        Assert.Single(result.Schematic.Analysis.Probes);

        var warning = Assert.Single(result.Warnings);

        Assert.Contains(
            "unknown field 'colour' was ignored", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownFieldInsideAProbeTargetIsIgnoredWithAWarning()
    {
        var result = ReadWith("""[{ "current": { "component": 1, "sign": -1 } }]""");

        Assert.True(result.Loaded);
        Assert.Single(result.Schematic.Analysis.Probes);

        var warning = Assert.Single(result.Warnings);

        Assert.Contains(
            "unknown field 'sign' was ignored", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ADanglingProbeStaysInMemoryUntilItIsSaved()
    {
        var probed = Probed(Probe.At(new WireId(1)));
        var removed = Edits.Succeed(new RemoveWire(new WireId(1)), probed);

        Assert.Single(removed.Analysis.Probes);
        Assert.False(ProbeTargets.Exists(Probe.At(new WireId(1)), removed));
    }

    [Fact]
    public void AProbePointingAtAMovedComponentStillResolves()
    {
        var probed = Probed(Probe.At(ResistorPin));
        var moved = Edits.Succeed(
            new MoveComponent(new ComponentId(1), new GridPoint(20, 20)), probed);

        Assert.True(ProbeTargets.IsValid(Probe.At(ResistorPin), moved, BaseCatalog.Catalog));

        var result = SchematicFile.Read(SchematicFile.Write(moved), BaseCatalog.Catalog);

        Assert.True(result.Loaded);
        Assert.Equal(Probe.At(ResistorPin), Assert.Single(result.Schematic.Analysis.Probes));
    }
}
