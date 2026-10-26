using Nodal.Foundation.Waveforms;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class TransientPartialTests
{
    private const double FailureTime = 1e-6;

    private static TransientResult Stopped()
    {
        var pulse = new PulseWaveform(0, 5, 1e-6, 1e-9, 1e-9, 2e-6, 5e-6);

        return TransientCircuits.Rc(1000, 100e-12)
            .Function("V1", pulse)
            .Transient(new TransientOptions(20e-6)
            {
                OutputStep = 1e-6,
                MaxStep = 1e-6,
                MinStep = 50e-9,
            });
    }

    [Fact]
    public void AFailedRunIsPartialAndNotSolved()
    {
        var result = Stopped();

        Assert.False(result.IsSolved);
        Assert.True(result.IsPartial);
        Assert.True(result.HasValues);
    }

    [Fact]
    public void AFailedRunCarriesTheFailureTime()
    {
        var result = Stopped();

        Assert.NotNull(result.FailureTime);
        Approximately.Equal(FailureTime, result.FailureTime!.Value);
    }

    [Fact]
    public void TheAcceptedPrefixStopsAtTheFailureTime()
    {
        var result = Stopped();

        Assert.NotEmpty(result.Times);
        Assert.Equal(0, result.Times[0]);
        Assert.True(result.FinalTime <= result.FailureTime);
    }

    [Fact]
    public void TheAcceptedTimesRise()
    {
        var times = Stopped().Times;

        for (var index = 1; index < times.Length; index++)
        {
            Assert.True(times[index] > times[index - 1]);
        }
    }

    [Fact]
    public void EverySeriesKeepsOneValuePerAcceptedTime()
    {
        var result = Stopped();

        Assert.True(result.PointCount > 0);

        foreach (var voltages in result.NetVoltages.Values)
        {
            Assert.Equal(result.PointCount, voltages.Length);
        }

        foreach (var series in result.Components.Values)
        {
            Assert.Equal(result.PointCount, series.Voltages.Length);
            Assert.Equal(result.PointCount, series.Currents.Length);
        }
    }

    [Fact]
    public void TheStatisticsOfTheAcceptedStepsAreKept()
    {
        var statistics = Stopped().Statistics;

        Assert.NotNull(statistics);
        Assert.True(statistics.AcceptedSteps > 0);
        Assert.True(statistics.NewtonIterations >= statistics.AcceptedSteps);
    }

    [Fact]
    public void TheFailureIsStillReportedAsAnError()
    {
        var diagnostic = Assert.Single(Stopped().Errors);

        Assert.Equal(SimulationDiagnosticCode.TimestepTooSmall, diagnostic.Code);
    }

    [Fact]
    public void TheDebugTextNamesTheStopTimeAndThePointCount()
    {
        var result = Stopped();
        var text = result.ToDebugText();

        Assert.Contains($"stopped at 1 µs in {result.PointCount} points", text);
        Assert.DoesNotContain("solved to", text);
        Assert.DoesNotContain("refused", text);
    }

    [Fact]
    public void TheValuesOfTheStoppedRunAreStillReadable()
    {
        var result = Stopped();
        var series = result.Of("C1");

        Assert.Equal(
            series.Voltages[^1], result.ComponentVoltageAt(series.Id, result.FinalTime));

        Assert.Equal(
            series.Voltages[^1], result.ComponentVoltageAt(series.Id, result.FinalTime * 2));
    }
}
