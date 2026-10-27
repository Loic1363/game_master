using Nodal.Simulation.Dc;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class FetLimitingTests
{
    private const double Threshold = 1;

    [Fact]
    public void AnOffDeviceCannotJumpFarPastTheThreshold()
    {
        Assert.Equal(Threshold + 0.5, FetLimiting.Gate(20, -5, Threshold));
    }

    [Fact]
    public void AnOffDeviceReachingJustBelowTheThresholdIsLeftAlone()
    {
        Assert.Equal(Threshold + 0.4, FetLimiting.Gate(Threshold + 0.4, -5, Threshold));
    }

    [Fact]
    public void AnOffDeviceTakesASmallStepTowardsTheThresholdUnchanged()
    {
        Assert.Equal(-4.5, FetLimiting.Gate(-4.5, -5, Threshold));
    }

    [Fact]
    public void AnOffDeviceFallingFastIsHeldBack()
    {
        var fast = Math.Abs(2 * (-5 - Threshold)) + 2;

        Assert.Equal(-5 - fast, FetLimiting.Gate(-100, -5, Threshold));
    }

    [Fact]
    public void AnOffDeviceFallingSlowlyIsLeftAlone()
    {
        Assert.Equal(-6, FetLimiting.Gate(-6, -5, Threshold));
    }

    [Fact]
    public void NearTheThresholdARiseIsCappedFourVoltsAbove()
    {
        Assert.Equal(Threshold + 4, FetLimiting.Gate(50, Threshold + 1, Threshold));
    }

    [Fact]
    public void NearTheThresholdAFallIsFlooredHalfAVoltBelow()
    {
        Assert.Equal(Threshold - 0.5, FetLimiting.Gate(-50, Threshold + 1, Threshold));
    }

    [Fact]
    public void WellAboveTheThresholdARiseIsBoundedByTheFastStep()
    {
        var old = Threshold + 5;
        var fast = Math.Abs(2 * (old - Threshold)) + 2;

        Assert.Equal(old + fast, FetLimiting.Gate(100, old, Threshold));
    }

    [Fact]
    public void WellAboveTheThresholdASmallRiseIsLeftAlone()
    {
        var old = Threshold + 5;

        Assert.Equal(old + 1, FetLimiting.Gate(old + 1, old, Threshold));
    }

    [Fact]
    public void WellAboveTheThresholdAFallStaysAboveTheApproachVoltage()
    {
        Assert.Equal(
            Threshold + 2, FetLimiting.Gate(-20, Threshold + 5, Threshold));
    }

    [Fact]
    public void WellAboveTheThresholdAFallInsideStrongInversionIsLeftAlone()
    {
        Assert.Equal(
            Threshold + 4, FetLimiting.Gate(Threshold + 4, Threshold + 20, Threshold));
    }

    [Fact]
    public void TheGateLimiterConvergesOnItsTarget()
    {
        var voltage = 0.0;

        for (var step = 0; step < 100; step++)
        {
            voltage = FetLimiting.Gate(8, voltage, Threshold);
            Assert.InRange(voltage, 0, 8);
        }

        Assert.Equal(8, voltage);
    }

    [Theory]
    [InlineData(2, 1, 2)]
    [InlineData(-0.2, 1, -0.2)]
    [InlineData(-5, 1, -0.5)]
    [InlineData(10, 1, 4)]
    [InlineData(50, 0, 4)]
    public void BelowStrongConductionTheDrainVoltageIsClampedToABand(
        double newVoltage, double oldVoltage, double expected)
    {
        Assert.Equal(expected, FetLimiting.DrainSource(newVoltage, oldVoltage));
    }

    [Theory]
    [InlineData(100, 5, 17)]
    [InlineData(8, 5, 8)]
    [InlineData(0, 5, 2)]
    [InlineData(3, 4, 3)]
    public void AboveStrongConductionTheDrainVoltageGrowsAndFallsInBounds(
        double newVoltage, double oldVoltage, double expected)
    {
        Assert.Equal(expected, FetLimiting.DrainSource(newVoltage, oldVoltage));
    }

    [Fact]
    public void TheDrainLimiterConvergesOnItsTarget()
    {
        var voltage = 0.0;

        for (var step = 0; step < 100; step++)
        {
            voltage = FetLimiting.DrainSource(30, voltage);
            Assert.InRange(voltage, 0, 30);
        }

        Assert.Equal(30, voltage);
    }

    [Fact]
    public void TheDrainLimiterNeverPullsAVoltageBelowHalfAVoltNegative()
    {
        Assert.Equal(-0.5, FetLimiting.DrainSource(-100, 0));
        Assert.Equal(-0.5, FetLimiting.DrainSource(-100, -0.5));
    }
}
