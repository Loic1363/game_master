using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Waveforms;
using Nodal.Foundation.Waveforms;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Editor.Tests.Waveforms;

public class TransistorProbeTests
{
    private const double Supply = 5;
    private const double Period = 2e-6;

    [Theory]
    [InlineData("nmos", "M1")]
    [InlineData("npn", "Q1")]
    public void ACurrentProbeOnATransistorIsLabelledWithItsDesignator(
        string definitionId, string designator)
    {
        var schematic = Driven(definitionId);
        var result = Run(schematic);
        var probe = Probe.Through(schematic.IdOf(designator));

        var trace = Assert.Single(
            ProbeTraces.Of(
                ConnectivityAnalyzer.Analyze(schematic, BaseContent.Catalog),
                result,
                [probe]));

        Assert.Equal($"I({designator})", trace.Label);
        Assert.Equal(TraceAxis.Amperes, trace.Axis);
    }

    [Theory]
    [InlineData("nmos", "M1", "drain")]
    [InlineData("npn", "Q1", "collector")]
    public void ACurrentProbeOnATransistorShowsItsDrainOrCollectorCurrent(
        string definitionId, string designator, string pinName)
    {
        var schematic = Driven(definitionId);
        var result = Run(schematic);
        var series = result.Components[schematic.IdOf(designator)];

        var trace = Assert.Single(
            ProbeTraces.Of(
                ConnectivityAnalyzer.Analyze(schematic, BaseContent.Catalog),
                result,
                [Probe.Through(series.Id)]));

        Assert.True(series.TryGetPin(pinName, out var pin));
        Assert.Equal(pin.Currents, trace.Values);
    }

    private static Schematic Driven(string definitionId) => Schematic.Empty
        .Add("voltage-source", 0, 0)
        .Add("ground", 0, 2)
        .Add("resistor", 6, 0, Sessions.FirstPinUp)
        .Add(definitionId, 6, 6)
        .Add("ground", 6, 8)
        .Add("resistor", 0, 6)
        .Add("voltage-source", -6, 8)
        .Add("ground", -6, 10)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Wire(2, 6, 4, 6)
        .Wire(-6, 6, -2, 6)
        .Set("V1", "voltage", Supply)
        .Set("R1", "resistance", 1000)
        .Set("R2", "resistance", 10000)
        .Function("V2", new PulseWaveform(0, Supply, 0, 1e-9, 1e-9, Period / 2, Period));

    private static TransientResult Run(Schematic schematic) => TransientRuns.Run(
        schematic, new TransientOptions(Period) { MaxStep = Period / 200 });
}
