using System.Collections.Immutable;
using Nodal.Foundation.Quantities;

namespace Nodal.Editor.Waveforms;

public static class AxisTicks
{
    public const int MinimumTicks = 2;

    private static readonly double[] Multiples = [1, 2, 5];

    public static ImmutableArray<AxisTick> Of(AxisRange range, int maxTicks, Unit unit)
    {
        var limit = Math.Max(maxTicks, MinimumTicks);

        if (range.IsFlat || !double.IsFinite(range.Span))
        {
            return [TickAt(range.Min, unit)];
        }

        var step = StepFor(range.Span, limit);

        if (!double.IsFinite(step) || step <= 0)
        {
            return [TickAt(range.Min, unit)];
        }

        var first = (long)Math.Ceiling(range.Min / step);
        var last = (long)Math.Floor(range.Max / step);
        var ticks = ImmutableArray.CreateBuilder<AxisTick>();

        for (var index = first; index <= last; index++)
        {
            ticks.Add(TickAt(index * step, unit));
        }

        return ticks.ToImmutable();
    }

    public static double StepFor(double span, int maxTicks)
    {
        var intervals = Math.Max(maxTicks, MinimumTicks) - 1;
        var rough = span / intervals;

        if (!double.IsFinite(rough) || rough <= 0)
        {
            return 0;
        }

        var exponent = Math.Floor(Math.Log10(rough));
        var magnitude = Math.Pow(10, exponent);
        var normalized = rough / magnitude;

        foreach (var multiple in Multiples)
        {
            if (normalized <= multiple)
            {
                return multiple * magnitude;
            }
        }

        return 10 * magnitude;
    }

    private static AxisTick TickAt(double value, Unit unit) =>
        new(value, new Quantity(value, unit).ToString());
}
