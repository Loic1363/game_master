using Nodal.Simulation.Elaboration;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class TransientResultTests
{
    private const double Load = 1000;
    private const double Capacitance = 100e-9;
    private const double Supply = 5;

    private static TransientResult Charge(double stopTime) =>
        TransientCircuits.Rc(Load, Capacitance)
            .Set("V1", "voltage", Supply)
            .Transient(new TransientOptions(stopTime) { StartFromZero = true });

    [Fact]
    public void EverySeriesHasOneValuePerTimePoint()
    {
        var result = Charge(1e-4);

        Assert.True(result.IsSolved);
        Assert.Equal(result.Times.Length, result.PointCount);

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
    public void InterpolationReturnsTheStoredValueAtAStoredTime()
    {
        var result = Charge(1e-4);
        var series = result.Of("C1");

        for (var index = 0; index < result.PointCount; index += 17)
        {
            Assert.Equal(
                series.Currents[index], result.CurrentAt(series.Id, result.Times[index]));
        }
    }

    [Fact]
    public void InterpolationIsLinearBetweenTwoStoredTimes()
    {
        var result = Charge(1e-4);
        var series = result.Of("C1");
        var index = result.PointCount / 2;
        var before = result.Times[index];
        var after = result.Times[index + 1];
        var middle = (before + after) / 2;
        var expected = (series.Voltages[index] + series.Voltages[index + 1]) / 2;

        Approximately.Equal(expected, result.ComponentVoltageAt(series.Id, middle));
    }

    [Fact]
    public void InterpolationIsClampedOutsideTheRun()
    {
        var result = Charge(1e-4);
        var series = result.Of("C1");

        Assert.Equal(series.Voltages[0], result.ComponentVoltageAt(series.Id, -1));
        Assert.Equal(series.Voltages[^1], result.ComponentVoltageAt(series.Id, 1));
        Assert.Equal(
            result.NetVoltages["N1"][^1], result.VoltageAt("N1", result.FinalTime * 2));
    }

    [Fact]
    public void AnUnknownNetOrComponentReadsAsZero()
    {
        var result = Charge(1e-4);

        Assert.Equal(0, result.VoltageAt("NOPE", 1e-5));
        Assert.Equal(0, result.CurrentAt(default, 1e-5));
        Assert.False(result.TryGetNetVoltages("NOPE", out _));
        Assert.False(result.TryGetComponent(default, out _));
    }

    [Fact]
    public void GroundIsPresentAndStaysAtZero()
    {
        var result = Charge(1e-4);

        Assert.True(result.TryGetNetVoltages("0", out var voltages));
        Assert.All(voltages, value => Assert.Equal(0, value));
    }

    [Fact]
    public void InternalParasiticNodesAreNotReported()
    {
        var result = TransientCircuits.Rc(Load, Capacitance)
            .Set("V1", "voltage", Supply)
            .Set("C1", "esr", 2)
            .Set("C1", "esl", 1e-9)
            .Transient(new TransientOptions(1e-4) { StartFromZero = true });

        Assert.True(result.IsSolved);
        Assert.NotEmpty(result.NetVoltages);

        foreach (var net in result.NetVoltages.Keys)
        {
            Assert.DoesNotContain(Elaborator.InternalNodeSuffix, net);
        }
    }

    [Fact]
    public void TheDebugTextNamesTheRunTheNetsAndTheComponents()
    {
        var text = Charge(1e-4).ToDebugText();

        Assert.Contains("solved to 100 µs", text);
        Assert.Contains("net N1 = 5 V", text);
        Assert.Contains("C1 V=", text);
        Assert.Contains("accepted", text);
    }

    [Fact]
    public void TheStatisticsCountTheStepsAndTheIterations()
    {
        var result = Charge(1e-4);
        var statistics = result.Statistics;

        Assert.NotNull(statistics);
        Assert.Equal(result.PointCount - 1, statistics.AcceptedSteps);
        Assert.True(statistics.RejectedSteps >= 0);
        Assert.True(statistics.NewtonIterations >= statistics.AcceptedSteps);
    }
}
