using System.Collections.Immutable;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Waveforms;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Editor.Tests.Waveforms;

public class WaveformPlotTests
{
    private static readonly Schematic Circuit = TransientRuns.Rc();

    private static TransientResult Solved { get; } =
        TransientRuns.Run(Circuit, new TransientOptions(1e-6) { StartFromZero = true });

    private static ConnectivityResult Connectivity(Schematic schematic) =>
        ConnectivityAnalyzer.Analyze(schematic, BaseContent.Catalog);

    private static ImmutableArray<Probe> Both() =>
    [
        Probe.At(new PinRef(Circuit.IdOf("C1"), "1")),
        Probe.Through(Circuit.IdOf("R1")),
    ];

    private static WaveformPlot PlotOf(ImmutableArray<Probe> probes, bool isStale = false) =>
        WaveformPlot.Of(Solved, Connectivity(Circuit), probes, isStale);

    [Fact]
    public void AnEmptyPlotHasNothingToDraw()
    {
        Assert.False(WaveformPlot.None.HasResult);
        Assert.False(WaveformPlot.None.HasTraces);
        Assert.False(WaveformPlot.None.IsStale);
        Assert.False(WaveformPlot.None.IsPartial);
        Assert.Null(WaveformPlot.None.StatusText);
        Assert.Empty(WaveformPlot.None.Traces);
    }

    [Fact]
    public void TheTimeLimitsSpanTheRun()
    {
        var plot = PlotOf(Both());

        Assert.Equal(Solved.Times[0], plot.TimeLimits.Min);
        Assert.Equal(Solved.FinalTime, plot.TimeLimits.Max);
    }

    [Fact]
    public void BothAxesAreNeededWhenBothKindsOfProbeArePresent()
    {
        var plot = PlotOf(Both());

        Assert.True(plot.HasVoltage);
        Assert.True(plot.HasCurrent);
        Assert.Equal(2, plot.Traces.Length);
    }

    [Fact]
    public void OnlyTheVoltAxisIsNeededForAVoltageProbe()
    {
        var plot = PlotOf([Probe.At(new PinRef(Circuit.IdOf("C1"), "1"))]);

        Assert.True(plot.HasVoltage);
        Assert.False(plot.HasCurrent);
    }

    [Fact]
    public void OnlyTheAmpereAxisIsNeededForACurrentProbe()
    {
        var plot = PlotOf([Probe.Through(Circuit.IdOf("R1"))]);

        Assert.False(plot.HasVoltage);
        Assert.True(plot.HasCurrent);
    }

    [Fact]
    public void EachAxisFitsItsOwnTraces()
    {
        var plot = PlotOf(Both());
        var voltages = plot.Traces.First(trace => trace.Axis == TraceAxis.Volts).Values;
        var currents = plot.Traces.First(trace => trace.Axis == TraceAxis.Amperes).Values;

        Assert.True(plot.VoltageLimits.Min <= voltages.Min());
        Assert.True(plot.VoltageLimits.Max >= voltages.Max());
        Assert.True(plot.CurrentLimits.Min <= currents.Min());
        Assert.True(plot.CurrentLimits.Max >= currents.Max());
        Assert.Equal(plot.VoltageLimits, plot.LimitsOf(TraceAxis.Volts));
        Assert.Equal(plot.CurrentLimits, plot.LimitsOf(TraceAxis.Amperes));
    }

    [Fact]
    public void AFreshPlotHasNoStatusText()
    {
        Assert.Null(PlotOf(Both()).StatusText);
    }

    [Fact]
    public void AStalePlotKeepsItsTracesAndSaysItIsOutOfDate()
    {
        var plot = PlotOf(Both()).Staled();

        Assert.True(plot.IsStale);
        Assert.Equal(2, plot.Traces.Length);
        Assert.Equal("Out of date — press F10 to run again", plot.StatusText);
    }

    [Fact]
    public void StalingAnEmptyPlotChangesNothing()
    {
        Assert.Same(WaveformPlot.None, WaveformPlot.None.Staled());
    }

    [Fact]
    public void AStoppedRunSaysWhereAndWhyItStopped()
    {
        var schematic = TransientRuns.PulsedRc();
        var stopped = TransientRuns.Stopped();

        var plot = WaveformPlot.Of(
            stopped,
            Connectivity(schematic),
            [Probe.Through(schematic.IdOf("R1"))]);

        Assert.True(plot.IsPartial);
        Assert.Equal(stopped.FailureTime, plot.FailureTime);
        Assert.Equal("Stopped at 1 µs: the timestep became too small", plot.StatusText);
    }

    [Fact]
    public void AStaleStoppedRunSaysItIsOutOfDateFirst()
    {
        var schematic = TransientRuns.PulsedRc();

        var plot = WaveformPlot.Of(
            TransientRuns.Stopped(),
            Connectivity(schematic),
            [Probe.Through(schematic.IdOf("R1"))],
            isStale: true);

        Assert.Equal("Out of date — press F10 to run again", plot.StatusText);
    }

    [Fact]
    public void ARefusedRunHasNoTraces()
    {
        var refused = TransientRuns.Run(
            Schematic.Empty.Add("resistor", 0, 0), new TransientOptions(1e-6));

        var plot = WaveformPlot.Of(refused, Connectivity(Circuit), Both());

        Assert.False(plot.HasResult);
        Assert.False(plot.HasTraces);
        Assert.False(plot.IsPartial);
    }

    [Theory]
    [InlineData(SimulationDiagnosticCode.TimestepTooSmall, "the timestep became too small")]
    [InlineData(SimulationDiagnosticCode.NonConvergence, "the analysis stopped converging")]
    [InlineData(SimulationDiagnosticCode.Singular, "the circuit has no unique solution")]
    [InlineData(SimulationDiagnosticCode.UnboundedCurrent, "a current would be unbounded")]
    [InlineData(SimulationDiagnosticCode.NoGround, "the circuit has no ground")]
    [InlineData(SimulationDiagnosticCode.Connectivity, "the schematic is not connected")]
    [InlineData(SimulationDiagnosticCode.UnsupportedComponent, "a component is not supported yet")]
    [InlineData(SimulationDiagnosticCode.RatingExceeded, "the analysis was refused")]
    public void EveryFailureCodeHasAShortReason(SimulationDiagnosticCode code, string expected) =>
        Assert.Equal(expected, TransientFailureText.ReasonOf(code));

    [Fact]
    public void ASolvedRunHasNoFailureText()
    {
        Assert.Null(TransientFailureText.Of(Solved));
    }
}
