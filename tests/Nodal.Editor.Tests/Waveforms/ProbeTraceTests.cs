using System.Collections.Immutable;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Waveforms;
using Nodal.Foundation.Quantities;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Editor.Tests.Waveforms;

public class ProbeTraceTests
{
    private static ConnectivityResult Connectivity(Schematic schematic) =>
        ConnectivityAnalyzer.Analyze(schematic, BaseContent.Catalog);

    private static ImmutableArray<WaveformTrace> TracesOf(
        Schematic schematic, TransientResult result, params Probe[] probes) =>
        ProbeTraces.Of(Connectivity(schematic), result, [.. probes]);

    private static Schematic SeriesResistors() => Schematic.Empty
        .Add("voltage-source", 0, 0)
        .Add("ground", 0, 2)
        .Add("resistor", 6, 0, Sessions.FirstPinUp)
        .Add("resistor", 6, 6, Sessions.FirstPinUp)
        .Add("ground", 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("R1", "resistance", 1000)
        .Set("R2", "resistance", 2000);

    [Fact]
    public void AVoltageProbeOnAPinIsLabelledWithItsNet()
    {
        var schematic = TransientRuns.Rc();
        var probe = Probe.At(new PinRef(schematic.IdOf("R1"), "1"));
        var netName = ProbeTraces.NetOf(Connectivity(schematic), probe);

        var trace = Assert.Single(
            TracesOf(schematic, TransientRuns.Run(schematic, new TransientOptions(1e-6)), probe));

        Assert.Equal($"V({netName})", trace.Label);
        Assert.Equal(TraceAxis.Volts, trace.Axis);
        Assert.Equal(Unit.Volt, trace.Unit);
        Assert.Equal(0, trace.ColorIndex);
    }

    [Fact]
    public void AVoltageProbeOnAWireIsLabelledWithItsNet()
    {
        var schematic = TransientRuns.Rc();
        var probe = Probe.At(schematic.Wires.Keys.First());
        var netName = ProbeTraces.NetOf(Connectivity(schematic), probe);

        var trace = Assert.Single(
            TracesOf(schematic, TransientRuns.Run(schematic, new TransientOptions(1e-6)), probe));

        Assert.Equal($"V({netName})", trace.Label);
    }

    [Fact]
    public void ACurrentProbeIsLabelledWithTheDesignator()
    {
        var schematic = TransientRuns.Rc();
        var probe = Probe.Through(schematic.IdOf("R1"));

        var trace = Assert.Single(
            TracesOf(schematic, TransientRuns.Run(schematic, new TransientOptions(1e-6)), probe));

        Assert.Equal("I(R1)", trace.Label);
        Assert.Equal(TraceAxis.Amperes, trace.Axis);
        Assert.Equal(Unit.Ampere, trace.Unit);
    }

    [Fact]
    public void EveryTraceCarriesOneValuePerTimePoint()
    {
        var schematic = TransientRuns.Rc();
        var result = TransientRuns.Run(schematic, new TransientOptions(1e-6));

        var traces = TracesOf(
            schematic,
            result,
            Probe.At(new PinRef(schematic.IdOf("R1"), "1")),
            Probe.Through(schematic.IdOf("C1")));

        Assert.Equal(2, traces.Length);
        Assert.All(traces, trace => Assert.Equal(result.PointCount, trace.Values.Length));
    }

    [Fact]
    public void TheColorIndexFollowsTheProbeOrder()
    {
        var schematic = TransientRuns.Rc();
        var result = TransientRuns.Run(schematic, new TransientOptions(1e-6));

        var traces = TracesOf(
            schematic,
            result,
            Probe.Through(schematic.IdOf("R1")),
            Probe.Through(schematic.IdOf("C1")));

        Assert.Equal([0, 1], traces.Select(trace => trace.ColorIndex));
    }

    [Fact]
    public void ADroppedProbeDoesNotShiftTheColorsOfTheOthers()
    {
        var schematic = TransientRuns.Rc();
        var result = TransientRuns.Run(schematic, new TransientOptions(1e-6));

        var traces = TracesOf(
            schematic,
            result,
            Probe.Through(new ComponentId(99)),
            Probe.Through(schematic.IdOf("R1")));

        var trace = Assert.Single(traces);

        Assert.Equal(1, trace.ColorIndex);
        Assert.Equal("I(R1)", trace.Label);
    }

    [Fact]
    public void AProbeOnAComponentThatIsGoneIsDroppedSilently()
    {
        var schematic = TransientRuns.Rc();
        var result = TransientRuns.Run(schematic, new TransientOptions(1e-6));

        Assert.Empty(TracesOf(schematic, result, Probe.Through(new ComponentId(99))));
    }

    [Fact]
    public void AProbeOnAWireThatIsGoneIsDroppedSilently()
    {
        var schematic = TransientRuns.Rc();
        var result = TransientRuns.Run(schematic, new TransientOptions(1e-6));

        Assert.Empty(TracesOf(schematic, result, Probe.At(new WireId(99))));
    }

    [Fact]
    public void AProbeOnAPinThatIsGoneIsDroppedSilently()
    {
        var schematic = TransientRuns.Rc();
        var result = TransientRuns.Run(schematic, new TransientOptions(1e-6));
        var probe = Probe.At(new PinRef(new ComponentId(99), "1"));

        Assert.Null(ProbeTraces.NetOf(Connectivity(schematic), probe));
        Assert.Empty(TracesOf(schematic, result, probe));
    }

    [Fact]
    public void ARenamedNetStillResolvesThroughItsPinReference()
    {
        var before = SeriesResistors();
        var probe = Probe.At(new PinRef(before.IdOf("R2"), "1"));
        var firstName = ProbeTraces.NetOf(Connectivity(before), probe);

        var shorted = before.Wire(6, -2, 6, 2);
        var secondName = ProbeTraces.NetOf(Connectivity(shorted), probe);

        Assert.NotNull(firstName);
        Assert.NotNull(secondName);
        Assert.NotEqual(firstName, secondName);

        var trace = Assert.Single(
            TracesOf(shorted, TransientRuns.Run(shorted, new TransientOptions(1e-6)), probe));

        Assert.Equal($"V({secondName})", trace.Label);
    }

    [Fact]
    public void AProbeOnANetWithoutASeriesIsDropped()
    {
        var schematic = TransientRuns.Rc();
        var result = TransientRuns.Run(schematic, new TransientOptions(1e-6));
        var ground = Probe.At(new PinRef(schematic.IdOf("GND1"), "gnd"));

        Assert.Equal("0", ProbeTraces.NetOf(Connectivity(schematic), ground));
        Assert.Single(TracesOf(schematic, result, ground));
    }

    [Fact]
    public void NoProbesGiveNoTraces()
    {
        var schematic = TransientRuns.Rc();
        var result = TransientRuns.Run(schematic, new TransientOptions(1e-6));

        Assert.Empty(ProbeTraces.Of(Connectivity(schematic), result, []));
        Assert.Empty(ProbeTraces.Of(Connectivity(schematic), result, default));
    }

    [Fact]
    public void ARefusedRunGivesNoTraces()
    {
        var schematic = TransientRuns.Rc();
        var refused = TransientRuns.Run(
            Schematic.Empty.Add("resistor", 0, 0), new TransientOptions(1e-6));

        Assert.False(refused.HasValues);
        Assert.Empty(TracesOf(schematic, refused, Probe.Through(schematic.IdOf("R1"))));
    }

    [Fact]
    public void AStoppedRunStillGivesTheTracesOfItsPrefix()
    {
        var schematic = TransientRuns.PulsedRc();
        var stopped = TransientRuns.Stopped();

        var traces = TracesOf(
            schematic,
            stopped,
            Probe.Through(schematic.IdOf("R1")),
            Probe.At(new PinRef(schematic.IdOf("C1"), "1")));

        Assert.Equal(2, traces.Length);
        Assert.All(traces, trace => Assert.Equal(stopped.PointCount, trace.Values.Length));
    }
}
