using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Persistence;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Nodal.Foundation.Waveforms;
using Xunit;

namespace Nodal.Circuit.Tests.Persistence;

public class WaveformFileTests
{
    private const string Function = "function";
    private const string Voltage = "voltage";

    public static TheoryData<string, Waveform> Waveforms => new()
    {
        { "pulse", new PulseWaveform(0, 12, 0, 1e-9, 1e-9, 0.02, 0.04) },
        { "pulse with cycles", new PulseWaveform(0, 12, 1e-3, 1e-9, 1e-9, 0.02, 0.04, 3) },
        { "sine", new SineWaveform(0, 10, 50) },
        { "sine with every argument", new SineWaveform(-1, 10, 50, 1e-3, 100, 90, 4) },
        { "exp", new ExpWaveform(0, 5, 0, 1e-4, 1e-3, 1e-4) },
        {
            "pwl",
            new PwlWaveform(
            [
                new WaveformPoint(0, 0),
                new WaveformPoint(1e-3, 5),
                new WaveformPoint(2.5e-3, -1.25),
            ])
        },
    };

    [Theory]
    [MemberData(nameof(Waveforms))]
    public void EveryFunctionSurvivesWritingAndReading(string name, Waveform waveform)
    {
        var result = RoundTrip(WithFunction(waveform));

        Assert.True(result.Loaded, name);
        Assert.Empty(result.Diagnostics);
        Assert.True(result.Schematic.TryGetComponent(ComponentId.First, out var component));
        Assert.True(component.Parameters[Function].TryGetWaveform(out var read));
        Assert.Equal(waveform, read);
    }

    [Fact]
    public void AFunctionIsStoredAsStructuredExactNumbers()
    {
        var text = SchematicFile.Write(
            WithFunction(new PulseWaveform(0, 12, 0, 1e-9, 1e-9, 0.02, 0.04)));

        Assert.Contains("\"function\": {", text, StringComparison.Ordinal);
        Assert.Contains("\"pulse\": {", text, StringComparison.Ordinal);
        Assert.Contains("\"v2\": 12", text, StringComparison.Ordinal);
        Assert.Contains("\"rise\": 1E-09", text, StringComparison.Ordinal);
        Assert.Contains("\"period\": 0.04", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PULSE(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AFunctionIsWrittenWithoutACycleCountWhenItHasNone()
    {
        var text = SchematicFile.Write(
            WithFunction(new PulseWaveform(0, 12, 0, 1e-9, 1e-9, 0.02, 0.04)));

        Assert.DoesNotContain("\"cycles\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AVoltageSourceWithoutAFunctionWritesNoFunctionField()
    {
        var text = SchematicFile.Write(Source());

        Assert.DoesNotContain("\"function\"", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{ "voltage": 5, "function": 3 }""", "function must be an object")]
    [InlineData("""{ "voltage": 5, "function": "PULSE(0 1 0 0 0 1m 2m)" }""",
        "function must be an object")]
    [InlineData("""{ "voltage": 5, "function": {} }""", "function must name exactly one function")]
    [InlineData("""{ "voltage": 5, "function": { "pulse": {}, "sine": {} } }""",
        "function must name exactly one function")]
    [InlineData("""{ "voltage": 5, "function": { "ramp": { "v1": 0 } } }""",
        "function names the unknown function 'ramp'")]
    [InlineData("""{ "voltage": 5, "function": { "pulse": 3 } }""",
        "function 'pulse' must be an object")]
    [InlineData("""{ "voltage": 5, "function": { "pulse": { "v1": 0 } } }""",
        "function needs 'v2' as a finite number")]
    [InlineData(
        """
        { "voltage": 5, "function": { "pulse": { "v1": 0, "v2": 1, "delay": 0, "rise": 0,
          "fall": 0, "on": 1e-3, "period": 1e-4 } } }
        """,
        "function the period is shorter than rise + on + fall")]
    [InlineData(
        """
        { "voltage": 5, "function": { "pulse": { "v1": 0, "v2": 1, "delay": -1, "rise": 0,
          "fall": 0, "on": 1e-3, "period": 2e-3 } } }
        """,
        "function times and durations cannot be negative")]
    [InlineData(
        """
        { "voltage": 5, "function": { "pulse": { "v1": 0, "v2": 1, "delay": 0, "rise": 0,
          "fall": 0, "on": 1e-3, "period": 2e-3, "cycles": 0 } } }
        """,
        "function 'cycles' must be a positive whole number")]
    [InlineData("""{ "voltage": 5, "function": { "pwl": { "points": 3 } } }""",
        "function needs 'points' as an array")]
    [InlineData("""{ "voltage": 5, "function": { "pwl": { "points": [[0]] } } }""",
        "function 'points' must hold pairs of finite numbers")]
    [InlineData("""{ "voltage": 5, "function": { "pwl": { "points": [] } } }""",
        "function a piecewise function needs at least one time and value")]
    public void AnInvalidFunctionIsUnsetWithAWarning(string parameters, string expected)
    {
        var result = SchematicFile.Read(
            SchematicJson.Single("voltage-source", "V1", parameters).ToString(),
            BaseCatalog.Catalog);

        Assert.True(result.Loaded);
        Assert.True(result.Schematic.TryGetComponent(ComponentId.First, out var component));
        Assert.False(component.Parameters.ContainsKey(Function));

        var warning = Assert.Single(result.Warnings);

        Assert.Contains(expected, warning.Message, StringComparison.Ordinal);
        Assert.EndsWith("unset.", warning.Message, StringComparison.Ordinal);
        Assert.Equal("$.components[0].parameters.function", warning.Path);
        Assert.Equal("V1", warning.Subject);
    }

    [Fact]
    public void AnUnknownFieldInsideAFunctionIsIgnored()
    {
        var result = SchematicFile.Read(
            SchematicJson.Single(
                "voltage-source",
                "V1",
                """
                { "voltage": 5, "function": { "pwl": { "points": [[0, 1]], "style": "smooth" } } }
                """).ToString(),
            BaseCatalog.Catalog);

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
        Assert.True(result.Schematic.TryGetComponent(ComponentId.First, out var component));
        Assert.True(component.Parameters[Function].IsWaveform);
    }

    [Fact]
    public void AQuantityGivenAsAFunctionObjectIsRepaired()
    {
        var result = SchematicFile.Read(
            SchematicJson.Single(
                "voltage-source",
                "V1",
                """{ "voltage": { "pulse": {} } }""").ToString(),
            BaseCatalog.Catalog);

        Assert.True(result.Loaded);
        Assert.True(result.Schematic.TryGetComponent(ComponentId.First, out var component));
        Assert.Equal(5, component.Parameters[Voltage].Quantity.Value);
        Assert.Contains(
            "voltage must be a number; reset to 5 V.",
            result.Warnings.Single().Message,
            StringComparison.Ordinal);
    }

    private static Schematic Source() =>
        new AddComponent("voltage-source", GridPoint.Origin, Orientation.Default)
            .Succeed(Schematic.Empty);

    private static Schematic WithFunction(Waveform waveform) =>
        new SetParameter(
            ComponentId.First, Function, ParameterValue.Of(waveform, Unit.Volt))
            .Succeed(Source());

    private static SchematicReadResult RoundTrip(Schematic schematic) =>
        SchematicFile.Read(SchematicFile.Write(schematic), BaseCatalog.Catalog);
}
