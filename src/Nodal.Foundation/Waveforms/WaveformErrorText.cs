namespace Nodal.Foundation.Waveforms;

public static class WaveformErrorText
{
    public static string Of(WaveformError error) => error switch
    {
        WaveformError.None => string.Empty,
        WaveformError.Empty => "No function.",
        WaveformError.UnknownFunction =>
            $"Unknown function. Use {WaveformText.PulseName}, {WaveformText.SineName}, "
            + $"{WaveformText.ExpName} or {WaveformText.PwlName}.",
        WaveformError.MalformedSyntax => "Expected a function such as PULSE(0 5 0 1u 1u 0.5m 1m).",
        WaveformError.WrongArgumentCount => "Wrong number of arguments.",
        WaveformError.MalformedNumber => "An argument is not a number. Separate them with spaces.",
        WaveformError.UnknownPrefix => "An argument carries an unknown metric prefix.",
        WaveformError.UnitNotAllowed => "Arguments carry no unit symbol.",
        WaveformError.NumberOutOfRange => "An argument is out of range.",
        WaveformError.NegativeTime => "Times and durations cannot be negative.",
        WaveformError.PeriodTooShort => "The period is shorter than rise + on + fall.",
        WaveformError.NonPositiveTau => "A time constant must be greater than zero.",
        WaveformError.InvalidCycleCount => "The cycle count must be a positive whole number.",
        WaveformError.NoPoints => "A piecewise function needs at least one time and value.",
        WaveformError.UnorderedPoints => "Times must increase from one point to the next.",
        _ => "Not a valid function.",
    };
}
