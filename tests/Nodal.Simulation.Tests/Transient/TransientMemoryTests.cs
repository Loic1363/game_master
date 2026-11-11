using Nodal.Foundation.Quantities;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class TransientMemoryTests
{
    private const double StopTime = 1e-3;

    [Fact]
    public void TheDefaultSampleLimitLetsAnOrdinaryRunFinish()
    {
        var result = Run(TransientOptions.DefaultMaxSamples);

        Assert.True(result.IsSolved);
        Assert.Equal(StopTime, result.FinalTime);
    }

    [Fact]
    public void TheRunStopsWhenTheRecordedSamplesPassTheLimit()
    {
        var result = Run(40);

        Assert.False(result.IsSolved);
        Assert.True(result.IsPartial);
        Assert.True(result.PointCount >= 2, $"only {result.PointCount} points were kept");
        Assert.True(result.FinalTime > 0);
        Assert.True(result.FinalTime < StopTime);
    }

    [Fact]
    public void TheStoppedRunKeepsWhatItRecordedAndSaysWhy()
    {
        var result = Run(40);
        var error = Assert.Single(result.Errors);

        Assert.Equal(SimulationDiagnosticCode.SampleLimit, error.Code);
        Assert.Equal(
            $"The transient analysis stopped at {Seconds(result.FinalTime)}: the result reached "
            + "the memory limit of 50 million samples. Shorten the stop time or increase the "
            + "output step.",
            error.Message);

        Assert.Equal(result.PointCount, result.Of("C1").Voltages.Length);
    }

    private static string Seconds(double time) => new Quantity(time, Unit.Second).ToString();

    private static TransientResult Run(long maxSamples) =>
        TransientCircuits.Rc(1000, 100e-9)
            .Set("V1", "voltage", 5)
            .Transient(new TransientOptions(StopTime) { MaxSamples = maxSamples });
}
