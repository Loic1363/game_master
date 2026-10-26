using System.Collections.Immutable;

namespace Nodal.Editor.Waveforms;

public static class TraceDecimation
{
    public const int PointsPerPixelBeforeDecimating = 2;

    public static ImmutableArray<PlotPoint> Of(
        ImmutableArray<double> times,
        ImmutableArray<double> values,
        AxisRange window,
        int widthInPixels)
    {
        if (times.IsDefaultOrEmpty || values.IsDefaultOrEmpty)
        {
            return [];
        }

        var count = Math.Min(times.Length, values.Length);
        var first = FirstVisible(times, window, count);
        var last = LastVisible(times, window, count);

        if (last < first)
        {
            return [];
        }

        var visible = last - first + 1;
        var width = Math.Max(widthInPixels, 1);

        return visible > PointsPerPixelBeforeDecimating * width
            ? Reduced(times, values, window, width, first, last)
            : Taken(times, values, first, last);
    }

    private static ImmutableArray<PlotPoint> Taken(
        ImmutableArray<double> times, ImmutableArray<double> values, int first, int last)
    {
        var points = ImmutableArray.CreateBuilder<PlotPoint>(last - first + 1);

        for (var index = first; index <= last; index++)
        {
            points.Add(new PlotPoint(times[index], values[index]));
        }

        return points.ToImmutable();
    }

    private static ImmutableArray<PlotPoint> Reduced(
        ImmutableArray<double> times,
        ImmutableArray<double> values,
        AxisRange window,
        int width,
        int first,
        int last)
    {
        var points = ImmutableArray.CreateBuilder<PlotPoint>();
        var column = ColumnOf(times[first], window, width);
        var start = first;

        for (var index = first; index <= last; index++)
        {
            var at = ColumnOf(times[index], window, width);

            if (at == column)
            {
                continue;
            }

            Flush(points, times, values, start, index - 1);
            column = at;
            start = index;
        }

        Flush(points, times, values, start, last);

        return points.ToImmutable();
    }

    private static void Flush(
        ImmutableArray<PlotPoint>.Builder points,
        ImmutableArray<double> times,
        ImmutableArray<double> values,
        int first,
        int last)
    {
        var lowest = first;
        var highest = first;

        for (var index = first + 1; index <= last; index++)
        {
            if (values[index] < values[lowest])
            {
                lowest = index;
            }

            if (values[index] > values[highest])
            {
                highest = index;
            }
        }

        var kept = new[] { first, Math.Min(lowest, highest), Math.Max(lowest, highest), last };
        var previous = -1;

        foreach (var index in kept)
        {
            if (index == previous)
            {
                continue;
            }

            points.Add(new PlotPoint(times[index], values[index]));
            previous = index;
        }
    }

    private static int ColumnOf(double time, AxisRange window, int width) =>
        Math.Clamp((int)(window.FractionOf(time) * width), 0, width - 1);

    private static int FirstVisible(ImmutableArray<double> times, AxisRange window, int count)
    {
        for (var index = 0; index < count; index++)
        {
            if (times[index] >= window.Min)
            {
                return Math.Max(index - 1, 0);
            }
        }

        return Math.Max(count - 1, 0);
    }

    private static int LastVisible(ImmutableArray<double> times, AxisRange window, int count)
    {
        for (var index = count - 1; index >= 0; index--)
        {
            if (times[index] <= window.Max)
            {
                return Math.Min(index + 1, count - 1);
            }
        }

        return 0;
    }
}
