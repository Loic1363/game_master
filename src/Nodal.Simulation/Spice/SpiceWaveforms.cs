using Nodal.Foundation.Waveforms;

namespace Nodal.Simulation.Spice;

internal static class SpiceWaveforms
{
    public const string PulseName = "PULSE";
    public const string SineName = "SIN";
    public const string ExpName = "EXP";
    public const string PwlName = "PWL";

    public static bool IsExportable(Waveform waveform) =>
        waveform is not SineWaveform { Cycles: not null };

    public static string UnsupportedFeatureOf(Waveform waveform) =>
        waveform is SineWaveform { Cycles: not null }
            ? "a cycle count on a sine"
            : string.Empty;

    public static string Text(Waveform waveform) => waveform switch
    {
        PulseWaveform pulse => Composed(PulseName, PulseArgumentsOf(pulse)),
        SineWaveform sine => Composed(SineName, SineArgumentsOf(sine)),
        ExpWaveform exponential => Composed(ExpName, ExpArgumentsOf(exponential)),
        PwlWaveform piecewise => Composed(PwlName, PwlArgumentsOf(piecewise)),
        _ => throw new ArgumentOutOfRangeException(
            nameof(waveform), waveform, "Unknown waveform kind."),
    };

    private static IEnumerable<string> PulseArgumentsOf(PulseWaveform pulse)
    {
        yield return SpiceFormat.Value(pulse.V1);
        yield return SpiceFormat.Value(pulse.V2);
        yield return SpiceFormat.Value(pulse.Delay);
        yield return SpiceFormat.Value(pulse.Rise);
        yield return SpiceFormat.Value(pulse.Fall);
        yield return SpiceFormat.Value(pulse.On);
        yield return SpiceFormat.Value(pulse.Period);

        if (pulse.Cycles is { } cycles)
        {
            yield return SpiceFormat.Count(cycles);
        }
    }

    private static IEnumerable<string> SineArgumentsOf(SineWaveform sine)
    {
        yield return SpiceFormat.Value(sine.Offset);
        yield return SpiceFormat.Value(sine.Amplitude);
        yield return SpiceFormat.Value(sine.Frequency);
        yield return SpiceFormat.Value(sine.Delay);
        yield return SpiceFormat.Value(sine.Theta);
        yield return SpiceFormat.Value(sine.PhaseDegrees);
    }

    private static IEnumerable<string> ExpArgumentsOf(ExpWaveform exponential)
    {
        yield return SpiceFormat.Value(exponential.V1);
        yield return SpiceFormat.Value(exponential.V2);
        yield return SpiceFormat.Value(exponential.RiseDelay);
        yield return SpiceFormat.Value(exponential.RiseTau);
        yield return SpiceFormat.Value(exponential.FallDelay);
        yield return SpiceFormat.Value(exponential.FallTau);
    }

    private static IEnumerable<string> PwlArgumentsOf(PwlWaveform piecewise)
    {
        foreach (var point in piecewise.Points)
        {
            yield return SpiceFormat.Value(point.Time);
            yield return SpiceFormat.Value(point.Value);
        }
    }

    private static string Composed(string name, IEnumerable<string> arguments) =>
        name + "(" + string.Join(' ', arguments) + ")";
}
