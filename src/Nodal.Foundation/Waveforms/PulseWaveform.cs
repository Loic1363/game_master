namespace Nodal.Foundation.Waveforms;

public sealed record PulseWaveform(
    double V1,
    double V2,
    double Delay,
    double Rise,
    double Fall,
    double On,
    double Period,
    int? Cycles = null) : Waveform
{
    public override WaveformKind Kind => WaveformKind.Pulse;

    public override double ValueAt(double time)
    {
        if (time < Delay)
        {
            return V1;
        }

        var local = time - Delay;

        if (Period <= 0)
        {
            return Shape(local);
        }

        var cycle = Math.Floor(local / Period);

        return Cycles is { } count && cycle >= count ? V1 : Shape(local - (cycle * Period));
    }

    public override WaveformError Validate()
    {
        var times = CheckTimes(Delay, Rise, Fall, On, Period);

        if (times != WaveformError.None)
        {
            return times;
        }

        var amplitudes = CheckAmplitudes(V1, V2);

        if (amplitudes != WaveformError.None)
        {
            return amplitudes;
        }

        if (Period < Rise + On + Fall)
        {
            return WaveformError.PeriodTooShort;
        }

        return CheckCycles(Cycles);
    }

    public override Waveform Negated() => this with { V1 = -V1, V2 = -V2 };

    public override IEnumerable<double> BreakpointsUntil(double stopTime) =>
        Within(Corners(stopTime), stopTime);

    private IEnumerable<double> Corners(double stopTime)
    {
        if (Period <= 0)
        {
            yield break;
        }

        for (var cycle = 0; ; cycle++)
        {
            var start = Delay + (cycle * Period);

            if (start > stopTime)
            {
                yield break;
            }

            if (Cycles is { } count && cycle >= count)
            {
                yield return start;

                yield break;
            }

            yield return start;
            yield return start + Rise;
            yield return start + Rise + On;
            yield return start + Rise + On + Fall;
        }
    }

    private double Shape(double phase)
    {
        if (phase < Rise)
        {
            return V1 + ((V2 - V1) * (phase / Rise));
        }

        var afterRise = phase - Rise;

        if (afterRise < On)
        {
            return V2;
        }

        var afterOn = afterRise - On;

        return afterOn < Fall ? V2 + ((V1 - V2) * (afterOn / Fall)) : V1;
    }
}
