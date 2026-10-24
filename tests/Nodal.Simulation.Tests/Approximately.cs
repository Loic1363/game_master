using Xunit;

namespace Nodal.Simulation.Tests;

internal static class Approximately
{
    public const double RelativeTolerance = 1e-9;

    public static void Equal(
        double expected, double actual, double relativeTolerance = RelativeTolerance)
    {
        var allowed = relativeTolerance * Math.Max(Math.Abs(expected), Math.Abs(actual));

        Assert.True(
            Math.Abs(expected - actual) <= allowed,
            $"Expected {expected:R} but got {actual:R}, allowed difference {allowed:R}.");
    }

    public static void Zero(double actual, double allowed)
    {
        Assert.True(
            Math.Abs(actual) <= allowed, $"Expected at most {allowed:R} but got {actual:R}.");
    }
}
