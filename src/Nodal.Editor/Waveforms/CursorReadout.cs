using System.Collections.Immutable;
using Nodal.Editor.Simulation;
using Nodal.Foundation.Quantities;
using Nodal.Simulation.Transient;

namespace Nodal.Editor.Waveforms;

public static class CursorReadout
{
    public const string TimeLabel = "t";

    public static double ClampedTime(TransientResult result, double time)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.HasValues || !double.IsFinite(time))
        {
            return 0;
        }

        return Math.Clamp(time, result.Times[0], result.FinalTime);
    }

    public static ReadoutLine TimeLine(double time) =>
        new(TimeLabel, new Quantity(time, Unit.Second).ToString());

    public static ImmutableArray<ReadoutLine> Of(
        TransientResult result, ImmutableArray<WaveformTrace> traces, double time)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.HasValues)
        {
            return [];
        }

        var at = ClampedTime(result, time);
        var lines = ImmutableArray.CreateBuilder<ReadoutLine>(traces.Length + 1);

        lines.Add(TimeLine(at));

        foreach (var trace in traces)
        {
            lines.Add(new ReadoutLine(trace.Label, ValueText(result, trace, at)));
        }

        return lines.ToImmutable();
    }

    public static double ValueAt(TransientResult result, WaveformTrace trace, double time)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(trace);

        return result.SampleAt(trace.Values, ClampedTime(result, time));
    }

    private static string ValueText(
        TransientResult result, WaveformTrace trace, double time) =>
        DisplayValue.Text(ValueAt(result, trace, time), trace.Unit);
}
