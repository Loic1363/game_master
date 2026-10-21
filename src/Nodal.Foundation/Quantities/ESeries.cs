using System.Runtime.CompilerServices;

namespace Nodal.Foundation.Quantities;

public static class ESeries
{
    public const double RelativeTolerance = 1e-9;

    private const double LogarithmicTieTolerance = 1e-12;
    private const int NeighbouringDecades = 1;

    private static readonly double[] E6BaseValues = [1.0, 1.5, 2.2, 3.3, 4.7, 6.8];

    private static readonly double[] E12BaseValues =
        [1.0, 1.2, 1.5, 1.8, 2.2, 2.7, 3.3, 3.9, 4.7, 5.6, 6.8, 8.2];

    private static readonly double[] E24BaseValues =
    [
        1.0, 1.1, 1.2, 1.3, 1.5, 1.6, 1.8, 2.0, 2.2, 2.4, 2.7, 3.0,
        3.3, 3.6, 3.9, 4.3, 4.7, 5.1, 5.6, 6.2, 6.8, 7.5, 8.2, 9.1,
    ];

    private static readonly Dictionary<StandardSeries, double[]> BaseValuesBySeries = new()
    {
        [StandardSeries.E6] = E6BaseValues,
        [StandardSeries.E12] = E12BaseValues,
        [StandardSeries.E24] = E24BaseValues,
    };

    public static bool Contains(StandardSeries series, double value)
    {
        RequirePositive(value);

        return AreClose(Nearest(series, value), value);
    }

    public static double Nearest(StandardSeries series, double value)
    {
        RequirePositive(value);

        var nearest = 0.0;
        var shortestDistance = double.PositiveInfinity;

        foreach (var candidate in AscendingCandidatesAround(series, value))
        {
            var distance = Math.Abs(Math.Log(candidate) - Math.Log(value));

            if (distance <= shortestDistance + LogarithmicTieTolerance)
            {
                nearest = candidate;
                shortestDistance = Math.Min(shortestDistance, distance);
            }
        }

        return nearest;
    }

    public static double Next(StandardSeries series, double value)
    {
        RequirePositive(value);

        return AscendingCandidatesAround(series, value)
            .First(candidate => candidate > value * (1 + RelativeTolerance));
    }

    public static double Previous(StandardSeries series, double value)
    {
        RequirePositive(value);

        return AscendingCandidatesAround(series, value)
            .Last(candidate => candidate < value * (1 - RelativeTolerance));
    }

    public static IReadOnlyList<double> ValuesInRange(StandardSeries series, double min, double max)
    {
        RequirePositive(min);
        RequirePositive(max);

        if (min > max)
        {
            return [];
        }

        var lowerBound = min * (1 - RelativeTolerance);
        var upperBound = max * (1 + RelativeTolerance);
        var values = new List<double>();

        for (var decade = DecadeOf(min) - 1; decade <= DecadeOf(max) + 1; decade++)
        {
            foreach (var baseValue in BaseValuesOf(series))
            {
                var candidate = Rescale(baseValue, decade);

                if (candidate >= lowerBound && candidate <= upperBound)
                {
                    values.Add(candidate);
                }
            }
        }

        return values;
    }

    private static IEnumerable<double> AscendingCandidatesAround(StandardSeries series, double value)
    {
        var decade = DecadeOf(value);

        for (var step = -NeighbouringDecades; step <= NeighbouringDecades; step++)
        {
            foreach (var baseValue in BaseValuesOf(series))
            {
                yield return Rescale(baseValue, decade + step);
            }
        }
    }

    private static double[] BaseValuesOf(StandardSeries series) =>
        BaseValuesBySeries.TryGetValue(series, out var baseValues)
            ? baseValues
            : throw new ArgumentOutOfRangeException(nameof(series), series, "Unknown E-series.");

    private static int DecadeOf(double value) => (int)Math.Floor(Math.Log10(value));

    private static double Rescale(double value, int decade) =>
        decade >= 0 ? value * Math.Pow(10, decade) : value / Math.Pow(10, -decade);

    private static bool AreClose(double left, double right) =>
        Math.Abs(left - right) <= RelativeTolerance * Math.Max(Math.Abs(left), Math.Abs(right));

    private static void RequirePositive(
        double value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName, value, "E-series values must be finite and strictly positive.");
        }
    }
}
