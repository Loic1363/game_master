namespace Nodal.Foundation.Waveforms;

public abstract record Waveform
{
    internal Waveform()
    {
    }

    public abstract WaveformKind Kind { get; }

    public abstract double ValueAt(double time);

    public abstract WaveformError Validate();

    public abstract IEnumerable<double> BreakpointsUntil(double stopTime);

    public abstract Waveform Negated();

    public bool IsValid => Validate() == WaveformError.None;

    public sealed override string ToString() => WaveformText.Format(this);

    protected static WaveformError CheckTimes(params double[] times)
    {
        foreach (var time in times)
        {
            if (!double.IsFinite(time))
            {
                return WaveformError.NumberOutOfRange;
            }

            if (time < 0)
            {
                return WaveformError.NegativeTime;
            }
        }

        return WaveformError.None;
    }

    protected static WaveformError CheckAmplitudes(params double[] amplitudes) =>
        amplitudes.All(double.IsFinite)
            ? WaveformError.None
            : WaveformError.NumberOutOfRange;

    protected static WaveformError CheckCycles(int? cycles) =>
        cycles is null or > 0 ? WaveformError.None : WaveformError.InvalidCycleCount;

    protected static IEnumerable<double> Within(
        IEnumerable<double> candidates, double stopTime)
    {
        foreach (var candidate in candidates)
        {
            if (candidate > stopTime)
            {
                yield break;
            }

            if (candidate > 0)
            {
                yield return candidate;
            }
        }
    }
}
