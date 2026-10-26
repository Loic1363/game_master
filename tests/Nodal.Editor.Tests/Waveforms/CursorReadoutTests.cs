using System.Collections.Immutable;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Waveforms;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Editor.Tests.Waveforms;

public class CursorReadoutTests
{
    private static readonly Schematic Circuit = TransientRuns.Rc();

    private static TransientResult Result { get; } =
        TransientRuns.Run(Circuit, new TransientOptions(1e-6) { StartFromZero = true });

    private static ImmutableArray<WaveformTrace> Traces() => ProbeTraces.Of(
        ConnectivityAnalyzer.Analyze(Circuit, BaseContent.Catalog),
        Result,
        [
            Probe.At(new PinRef(Circuit.IdOf("C1"), "1")),
            Probe.Through(Circuit.IdOf("R1")),
        ]);

    [Fact]
    public void TheFirstLineIsTheCursorTime()
    {
        var lines = CursorReadout.Of(Result, Traces(), 5e-7);

        Assert.Equal("t", lines[0].Label);
        Assert.Equal("500 ns", lines[0].Text);
    }

    [Fact]
    public void EveryTraceGetsALineInOrder()
    {
        var traces = Traces();
        var lines = CursorReadout.Of(Result, traces, 5e-7);

        Assert.Equal(traces.Length + 1, lines.Length);
        Assert.Equal(traces[0].Label, lines[1].Label);
        Assert.Equal(traces[1].Label, lines[2].Label);
        Assert.EndsWith("V", lines[1].Text, StringComparison.Ordinal);
        Assert.EndsWith("A", lines[2].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void AValueAtAStoredTimeIsTheStoredValue()
    {
        var trace = Traces()[0];
        var index = Result.PointCount / 2;

        Assert.Equal(
            trace.Values[index],
            CursorReadout.ValueAt(Result, trace, Result.Times[index]));
    }

    [Fact]
    public void AValueBetweenTwoStoredTimesIsInterpolated()
    {
        var trace = Traces()[0];
        var index = Result.PointCount / 2;
        var before = Result.Times[index];
        var after = Result.Times[index + 1];
        var expected = (trace.Values[index] + trace.Values[index + 1]) / 2;

        Assert.Equal(
            expected, CursorReadout.ValueAt(Result, trace, (before + after) / 2), 12);
    }

    [Fact]
    public void ATimeBeforeTheRunIsClampedToItsStart()
    {
        var trace = Traces()[0];

        Assert.Equal(Result.Times[0], CursorReadout.ClampedTime(Result, -1));
        Assert.Equal(trace.Values[0], CursorReadout.ValueAt(Result, trace, -1));
    }

    [Fact]
    public void ATimeAfterTheRunIsClampedToItsEnd()
    {
        var trace = Traces()[0];

        Assert.Equal(Result.FinalTime, CursorReadout.ClampedTime(Result, 1));
        Assert.Equal(trace.Values[^1], CursorReadout.ValueAt(Result, trace, 1));
    }

    [Fact]
    public void ANonFiniteTimeReadsAsZero()
    {
        Assert.Equal(0, CursorReadout.ClampedTime(Result, double.NaN));
        Assert.Equal(0, CursorReadout.ClampedTime(Result, double.PositiveInfinity));
    }

    [Fact]
    public void TheReadoutUsesTheDisplayThreshold()
    {
        var zero = CursorReadout.Of(Result, Traces(), 0);

        Assert.Equal("0 s", zero[0].Text);
        Assert.Equal("0 V", zero[1].Text);
    }

    [Fact]
    public void ARunWithoutValuesHasNoReadout()
    {
        var refused = TransientRuns.Run(
            Schematic.Empty.Add("resistor", 0, 0), new TransientOptions(1e-6));

        Assert.Empty(CursorReadout.Of(refused, Traces(), 0));
        Assert.Equal(0, CursorReadout.ClampedTime(refused, 1e-6));
    }

    [Fact]
    public void AReadoutWithoutTracesStillNamesTheTime()
    {
        var line = Assert.Single(CursorReadout.Of(Result, [], 1e-7));

        Assert.Equal("t", line.Label);
        Assert.Equal("100 ns", line.Text);
    }

    [Fact]
    public void AStoppedRunIsReadUpToItsFailureTime()
    {
        var stopped = TransientRuns.Stopped();
        var schematic = TransientRuns.PulsedRc();

        var traces = ProbeTraces.Of(
            ConnectivityAnalyzer.Analyze(schematic, BaseContent.Catalog),
            stopped,
            [Probe.Through(schematic.IdOf("R1"))]);

        Assert.Equal(stopped.FinalTime, CursorReadout.ClampedTime(stopped, 1));
        Assert.Equal(
            traces[0].Values[^1], CursorReadout.ValueAt(stopped, traces[0], 1));
    }
}
