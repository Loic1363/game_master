namespace Nodal.Foundation.Waveforms;

public sealed record ExpWaveform(
    double V1,
    double V2,
    double RiseDelay,
    double RiseTau,
    double FallDelay,
    double FallTau) : Waveform
{
    public override WaveformKind Kind => WaveformKind.Exp;

    public override double ValueAt(double time)
    {
        if (time < RiseDelay)
        {
            return V1;
        }

        var rising = 1 - Math.Exp(-(time - RiseDelay) / RiseTau);

        if (time < FallDelay)
        {
            return V1 + ((V2 - V1) * rising);
        }

        var falling = 1 - Math.Exp(-(time - FallDelay) / FallTau);

        return V1 + ((V2 - V1) * rising) + ((V1 - V2) * falling);
    }

    public override WaveformError Validate()
    {
        var times = CheckTimes(RiseDelay, FallDelay);

        if (times != WaveformError.None)
        {
            return times;
        }

        var amplitudes = CheckAmplitudes(V1, V2, RiseTau, FallTau);

        if (amplitudes != WaveformError.None)
        {
            return amplitudes;
        }

        return RiseTau > 0 && FallTau > 0
            ? WaveformError.None
            : WaveformError.NonPositiveTau;
    }

    public override Waveform Negated() => this with { V1 = -V1, V2 = -V2 };

    public override IEnumerable<double> BreakpointsUntil(double stopTime) =>
        Within(Corners(), stopTime);

    private IEnumerable<double> Corners()
    {
        yield return Math.Min(RiseDelay, FallDelay);
        yield return Math.Max(RiseDelay, FallDelay);
    }
}
