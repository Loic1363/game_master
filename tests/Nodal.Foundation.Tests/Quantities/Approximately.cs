using Xunit;

namespace Nodal.Foundation.Tests.Quantities;

internal static class Approximately
{
    public const double DefaultRelativeTolerance = 1e-12;

    public static void Equal(double expected, double actual, double relativeTolerance = DefaultRelativeTolerance)
    {
        var allowed = relativeTolerance * Math.Max(Math.Abs(expected), Math.Abs(actual));

        Assert.True(
            Math.Abs(expected - actual) <= allowed,
            $"Expected {expected:R} but got {actual:R}, allowed difference {allowed:R}.");
    }

    public static void EqualSequence(IReadOnlyList<double> expected, IReadOnlyList<double> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        for (var index = 0; index < expected.Count; index++)
        {
            Equal(expected[index], actual[index]);
        }
    }
}
