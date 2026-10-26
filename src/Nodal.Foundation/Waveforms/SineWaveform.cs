namespace Nodal.Foundation.Waveforms;

public sealed record SineWaveform(
    double Offset,
    double Amplitude,
    double Frequency,
    double Delay = 0,
    double Theta = 0,
    double PhaseDegrees = 0,
    int? Cycles = null) : Waveform
{
    private const double DegreesPerTurn = 360;

    public override WaveformKind Kind => WaveformKind.Sine;

    public override double ValueAt(double time)
    {
        var local = time - Delay;

        if (local < 0 || Stopped(local))
        {
            return Offset + (Amplitude * Math.Sin(PhaseRadians));
        }

        return Offset
            + (Amplitude
                * Math.Exp(-local * Theta)
                * Math.Sin((2 * Math.PI * Frequency * local) + PhaseRadians));
    }

    public override WaveformError Validate()
    {
        var times = CheckTimes(Delay);

        if (times != WaveformError.None)
        {
            return times;
        }

        var amplitudes = CheckAmplitudes(Offset, Amplitude, Frequency, Theta, PhaseDegrees);

        return amplitudes != WaveformError.None ? amplitudes : CheckCycles(Cycles);
    }

    public override Waveform Negated() =>
        this with { Offset = -Offset, Amplitude = -Amplitude };

    public override IEnumerable<double> BreakpointsUntil(double stopTime) =>
        Within(Corners(), stopTime);

    private double PhaseRadians => 2 * Math.PI * PhaseDegrees / DegreesPerTurn;

    private bool Stopped(double local) =>
        Cycles is { } count && Frequency > 0 && local * Frequency >= count;

    private IEnumerable<double> Corners()
    {
        yield return Delay;

        if (Cycles is { } count && Frequency > 0)
        {
            yield return Delay + (count / Frequency);
        }
    }
}
