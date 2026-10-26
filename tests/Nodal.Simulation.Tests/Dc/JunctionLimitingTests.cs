using Nodal.Simulation.Dc;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class JunctionLimitingTests
{
    private const double ThermalVoltage = 0.025;
    private const double CriticalVoltage = 0.7;

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(0.3, 0.3)]
    [InlineData(0.7, 0.7)]
    public void AVoltageBelowTheCriticalOneIsLeftAlone(double newVoltage, double oldVoltage)
    {
        var limited = JunctionLimiting.Limit(
            newVoltage, oldVoltage, ThermalVoltage, CriticalVoltage, out var wasLimited);

        Assert.False(wasLimited);
        Assert.Equal(newVoltage, limited);
    }

    [Fact]
    public void ASmallStepAboveTheCriticalVoltageIsLeftAlone()
    {
        var limited = JunctionLimiting.Limit(
            0.72, 0.71, ThermalVoltage, CriticalVoltage, out var wasLimited);

        Assert.False(wasLimited);
        Assert.Equal(0.72, limited);
    }

    [Fact]
    public void ALargeStepFromAPositiveBiasGrowsLogarithmically()
    {
        var limited = JunctionLimiting.Limit(
            10, 0.6, ThermalVoltage, CriticalVoltage, out var wasLimited);

        Assert.True(wasLimited);
        Assert.Equal(
            0.6 + (ThermalVoltage * Math.Log(1 + ((10 - 0.6) / ThermalVoltage))),
            limited,
            1e-12);

        Assert.InRange(limited, 0.6, 0.6 + (7 * ThermalVoltage));
    }

    [Fact]
    public void ALargeStepFromANonPositiveBiasStartsFromTheThermalVoltage()
    {
        var limited = JunctionLimiting.Limit(
            10, 0, ThermalVoltage, CriticalVoltage, out var wasLimited);

        Assert.True(wasLimited);
        Assert.Equal(ThermalVoltage * Math.Log(10 / ThermalVoltage), limited, 1e-12);
    }

    [Fact]
    public void ANegativeLogarithmArgumentFallsBackToTheCriticalVoltage()
    {
        var limited = JunctionLimiting.Limit(
            0.8, 60, ThermalVoltage, CriticalVoltage, out var wasLimited);

        Assert.True(wasLimited);
        Assert.Equal(CriticalVoltage, limited);
    }

    [Fact]
    public void RepeatedLimitingWalksTowardsTheTargetWithoutOvershooting()
    {
        var voltage = 0.0;

        for (var step = 0; step < 100; step++)
        {
            voltage = JunctionLimiting.Limit(
                5, voltage, ThermalVoltage, CriticalVoltage, out _);

            Assert.InRange(voltage, 0, 5);
        }

        Assert.Equal(5, voltage, 1e-9);
    }

    [Fact]
    public void TheDiodeModelLimitsWithItsOwnThermalAndCriticalVoltages()
    {
        var expected = JunctionLimiting.Limit(
            10,
            0.6,
            DiodeModel.EmissionVoltage,
            DiodeModel.CriticalVoltage,
            out var expectedLimited);

        var actual = DiodeModel.Limit(10, 0.6, out var actualLimited);

        Assert.Equal(expected, actual);
        Assert.Equal(expectedLimited, actualLimited);
    }
}
