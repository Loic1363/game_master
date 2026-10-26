using System.Collections.Immutable;
using Nodal.Foundation.Collections;

namespace Nodal.Foundation.Waveforms;

public sealed record PwlWaveform(ImmutableArray<WaveformPoint> Points) : Waveform
{
    public override WaveformKind Kind => WaveformKind.Pwl;

    public override double ValueAt(double time)
    {
        if (Points.Length == 0)
        {
            return 0;
        }

        if (time <= Points[0].Time)
        {
            return Points[0].Value;
        }

        for (var index = 1; index < Points.Length; index++)
        {
            if (time > Points[index].Time)
            {
                continue;
            }

            var previous = Points[index - 1];
            var next = Points[index];
            var span = next.Time - previous.Time;

            return previous.Value
                + ((next.Value - previous.Value) * ((time - previous.Time) / span));
        }

        return Points[^1].Value;
    }

    public override WaveformError Validate()
    {
        if (Points.Length == 0)
        {
            return WaveformError.NoPoints;
        }

        for (var index = 0; index < Points.Length; index++)
        {
            var point = Points[index];
            var times = CheckTimes(point.Time);

            if (times != WaveformError.None)
            {
                return times;
            }

            var amplitudes = CheckAmplitudes(point.Value);

            if (amplitudes != WaveformError.None)
            {
                return amplitudes;
            }

            if (index > 0 && point.Time <= Points[index - 1].Time)
            {
                return WaveformError.UnorderedPoints;
            }
        }

        return WaveformError.None;
    }

    public override Waveform Negated() =>
        new PwlWaveform(
            [.. Points.Select(point => point with { Value = -point.Value })]);

    public override IEnumerable<double> BreakpointsUntil(double stopTime) =>
        Within(Points.Select(point => point.Time), stopTime);

    public bool Equals(PwlWaveform? other) =>
        other is not null && StructuralEquality.ItemsEqual(Points, other.Points);

    public override int GetHashCode() => StructuralEquality.ItemsHash(Points);
}
