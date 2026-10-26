using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class TransientOptionTests
{
    private const double Load = 1000;
    private const double Supply = 5;
    private const double BreakpointSlack = 1 + 1e-4;

    private static double LargestGap(TransientResult result)
    {
        var largest = 0.0;

        for (var index = 1; index < result.Times.Length; index++)
        {
            largest = Math.Max(largest, result.Times[index] - result.Times[index - 1]);
        }

        return largest;
    }

    private static TransientResult Run(TransientOptions options) =>
        TransientCircuits.SourceAndResistor(Load)
            .Set("V1", "voltage", Supply)
            .Transient(options);

    [Fact]
    public void TheOutputStepDefaultsToAThousandthOfTheStopTime()
    {
        var stopTime = 1e-3;
        var result = Run(new TransientOptions(stopTime));

        Assert.True(result.IsSolved);
        Assert.True(LargestGap(result) <= stopTime / 1000 * BreakpointSlack);
        Assert.True(result.PointCount > 1000);
    }

    [Fact]
    public void TheMaxStepDefaultsToAFiftiethOfTheStopTimeWhenTheOutputStepIsLarger()
    {
        var stopTime = 1e-3;
        var result = Run(new TransientOptions(stopTime) { OutputStep = stopTime });

        Assert.True(result.IsSolved);
        Assert.True(LargestGap(result) <= stopTime / 50 * BreakpointSlack);
        Assert.True(result.PointCount > 50);
    }

    [Fact]
    public void TheMaxStepIsHonouredWhenGiven()
    {
        var maxStep = 1e-6;
        var result = Run(new TransientOptions(1e-3) { MaxStep = maxStep });

        Assert.True(result.IsSolved);
        Assert.True(LargestGap(result) <= maxStep * BreakpointSlack);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AStopTimeThatIsNotAFinitePositiveNumberIsRefused(double stopTime) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new TransientOptions(stopTime));

    [Theory]
    [InlineData(0)]
    [InlineData(-1e-6)]
    [InlineData(double.NaN)]
    public void AnOutputStepThatIsNotAFinitePositiveNumberIsRefused(double outputStep) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Run(new TransientOptions(1e-3) { OutputStep = outputStep }));

    [Theory]
    [InlineData(0)]
    [InlineData(-1e-6)]
    [InlineData(double.NaN)]
    public void AMaxStepThatIsNotAFinitePositiveNumberIsRefused(double maxStep) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Run(new TransientOptions(1e-3) { MaxStep = maxStep }));

    [Theory]
    [InlineData(0)]
    [InlineData(-1e-6)]
    [InlineData(double.NaN)]
    public void AMinStepThatIsNotAFinitePositiveNumberIsRefused(double minStep) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Run(new TransientOptions(1e-3) { MinStep = minStep }));

    [Fact]
    public void TheDefaultOptionsCarryTheSpiceTolerances()
    {
        var options = new TransientOptions(1);

        Assert.Equal(1e-3, options.RelTol);
        Assert.Equal(1e-6, options.VnTol);
        Assert.Equal(1e-12, options.AbsTol);
        Assert.Equal(1e-14, options.ChgTol);
        Assert.Equal(7, options.TrTol);
        Assert.False(options.StartFromZero);
    }

    [Fact]
    public void AResistiveCircuitFollowsTheSourceWithoutTruncationLimits()
    {
        var result = Run(new TransientOptions(1e-3));

        Assert.True(result.IsSolved);
        Approximately.Equal(Supply, result.VoltsAt("R1", 0));
        Approximately.Equal(Supply, result.VoltsAt("R1", 5e-4));
        Approximately.Equal(Supply / Load, result.AmperesAt("R1", 5e-4));
        Assert.Equal(0, result.Statistics?.RejectedSteps);
    }
}
