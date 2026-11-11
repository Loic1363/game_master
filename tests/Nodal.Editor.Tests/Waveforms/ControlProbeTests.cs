using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Waveforms;
using Nodal.Foundation.Schedules;
using Nodal.Foundation.Waveforms;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Editor.Tests.Waveforms;

public class ControlProbeTests
{
    private const double Supply = 5;
    private const double StopTime = 2e-3;

    [Theory]
    [InlineData("vcvs", "E1")]
    [InlineData("vccs", "G1")]
    [InlineData("switch", "S1")]
    public void ACurrentProbeOnAFourPinSourceCarriesItsDesignator(
        string definitionId, string designator)
    {
        var schematic = Driven(definitionId);
        var trace = TraceOf(schematic, designator);

        Assert.Equal($"I({designator})", trace.Label);
        Assert.Equal(TraceAxis.Amperes, trace.Axis);
    }

    [Fact]
    public void ACurrentProbeOnAPushButtonCarriesItsDesignator()
    {
        var schematic = Button();
        var trace = TraceOf(schematic, "SW1");

        Assert.Equal("I(SW1)", trace.Label);
        Assert.Equal(TraceAxis.Amperes, trace.Axis);
    }

    [Fact]
    public void ACurrentProbeKeepsThePassiveConventionOfTheIndependentSources()
    {
        var schematic = Loaded("vcvs").Set("V1", "voltage", 1);
        var delivered = TraceOf(schematic, "E1").Values;

        Assert.NotEmpty(delivered);
        Assert.All(delivered, value => Approximately(-10e-3, value));
    }

    [Fact]
    public void ACurrentProbeOnASwitchShowsTheCurrentEnteringPlus()
    {
        var schematic = Driven("switch");
        var result = Run(schematic);
        var series = result.Components[schematic.IdOf("S1")];

        Assert.True(series.TryGetPin("plus", out var plus));
        Assert.Equal(plus.Currents, series.Currents);
    }

    private static void Approximately(double expected, double actual) =>
        Assert.True(
            Math.Abs(expected - actual) <= 1e-9, $"expected {expected:R} but got {actual:R}");

    private static WaveformTrace TraceOf(Schematic schematic, string designator) =>
        Assert.Single(
            ProbeTraces.Of(
                ConnectivityAnalyzer.Analyze(schematic, BaseContent.Catalog),
                Run(schematic),
                [Probe.Through(schematic.IdOf(designator))]));

    private static Schematic Driven(string definitionId) => Loaded(definitionId)
        .Function("V1", new PulseWaveform(0, Supply, 0, 1e-6, 1e-6, StopTime / 2, StopTime));

    private static Schematic Loaded(string definitionId) => Schematic.Empty
        .Add("voltage-source", -8, 0)
        .Add("ground", -8, 2)
        .Add(definitionId, 0, 0)
        .Add("ground", 0, 2)
        .Add("resistor", 6, 0, Sessions.FirstPinUp)
        .Add("ground", 6, 2)
        .Wire(-8, -2, -2, -2)
        .Wire(-2, -2, -2, -1)
        .Wire(-2, 1, -2, 2)
        .Wire(-2, 2, -8, 2)
        .Wire(0, -2, 6, -2)
        .Set("R1", "resistance", 1000);

    private static Schematic Button() => Schematic.Empty
        .Add("voltage-source", -8, 0)
        .Add("ground", -8, 2)
        .Add("push-button", 0, 0)
        .Add("resistor", 6, 0)
        .Add("ground", 8, 0)
        .Wire(-8, -2, -2, -2)
        .Wire(-2, -2, -2, 0)
        .Wire(2, 0, 4, 0)
        .Set("V1", "voltage", Supply)
        .Set("R1", "resistance", 1000)
        .Presses("SW1", PressSchedule.Pressed);

    private static TransientResult Run(Schematic schematic) =>
        TransientRuns.Run(schematic, new TransientOptions(StopTime));
}
