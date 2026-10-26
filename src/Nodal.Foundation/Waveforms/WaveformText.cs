using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Nodal.Foundation.Quantities;

namespace Nodal.Foundation.Waveforms;

public static class WaveformText
{
    public const string PulseName = "PULSE";
    public const string SineName = "SINE";
    public const string SineAlias = "SIN";
    public const string ExpName = "EXP";
    public const string PwlName = "PWL";

    private const char Open = '(';
    private const char Close = ')';
    private const int PulseArguments = 7;
    private const int SineArguments = 3;
    private const int ExpArguments = 6;
    private const int PwlValuesPerPoint = 2;

    public static bool TryParse(
        string? text, [NotNullWhen(true)] out Waveform? waveform, out WaveformError error)
    {
        waveform = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = WaveformError.Empty;

            return false;
        }

        if (!TrySplit(text.Trim(), out var name, out var arguments))
        {
            error = WaveformError.MalformedSyntax;

            return false;
        }

        if (!TryReadNumbers(arguments, out var numbers, out error))
        {
            return false;
        }

        if (!TryBuild(name, numbers, out waveform, out error))
        {
            return false;
        }

        error = waveform.Validate();

        if (error == WaveformError.None)
        {
            return true;
        }

        waveform = null;

        return false;
    }

    public static string Format(Waveform waveform)
    {
        ArgumentNullException.ThrowIfNull(waveform);

        return waveform switch
        {
            PulseWaveform pulse => Composed(PulseName, PulseArgumentsOf(pulse)),
            SineWaveform sine => Composed(SineName, SineArgumentsOf(sine)),
            ExpWaveform exponential => Composed(ExpName, ExpArgumentsOf(exponential)),
            PwlWaveform piecewise => Composed(PwlName, PwlArgumentsOf(piecewise)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(waveform), waveform, "Unknown waveform kind."),
        };
    }

    public static string TemplateOf(WaveformKind kind) => kind switch
    {
        WaveformKind.Pulse => "PULSE(0 5 0 1u 1u 0.5m 1m)",
        WaveformKind.Sine => "SINE(0 5 1k)",
        WaveformKind.Exp => "EXP(0 5 0 100u 1m 100u)",
        WaveformKind.Pwl => "PWL(0 0 1m 5 2m 0)",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown waveform kind."),
    };

    private static bool TrySplit(string text, out string name, out string[] arguments)
    {
        name = string.Empty;
        arguments = [];

        var open = text.IndexOf(Open, StringComparison.Ordinal);

        if (open <= 0 || text[^1] != Close)
        {
            return false;
        }

        name = text[..open].Trim();
        arguments = text[(open + 1)..^1]
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return name.Length > 0;
    }

    private static bool TryReadNumbers(
        string[] arguments, out double[] numbers, out WaveformError error)
    {
        numbers = new double[arguments.Length];
        error = WaveformError.None;

        for (var index = 0; index < arguments.Length; index++)
        {
            if (!TryReadNumber(arguments[index], out numbers[index], out error))
            {
                numbers = [];

                return false;
            }
        }

        return true;
    }

    private static bool TryReadNumber(string argument, out double number, out WaveformError error)
    {
        number = 0;

        if (argument[^1] is ',' or '.')
        {
            error = WaveformError.MalformedNumber;

            return false;
        }

        if (Quantity.TryParse(argument, Unit.Dimensionless, out var parsed, out var parseError))
        {
            number = parsed.Value;
            error = WaveformError.None;

            return true;
        }

        error = parseError switch
        {
            QuantityParseError.UnknownPrefix => WaveformError.UnknownPrefix,
            QuantityParseError.WrongUnit => WaveformError.UnitNotAllowed,
            QuantityParseError.OutOfRange => WaveformError.NumberOutOfRange,
            _ => WaveformError.MalformedNumber,
        };

        return false;
    }

    private static bool TryBuild(
        string name,
        double[] numbers,
        [NotNullWhen(true)] out Waveform? waveform,
        out WaveformError error)
    {
        waveform = null;
        error = WaveformError.None;

        if (Named(name, PulseName))
        {
            return TryPulse(numbers, out waveform, out error);
        }

        if (Named(name, SineName) || Named(name, SineAlias))
        {
            return TrySine(numbers, out waveform, out error);
        }

        if (Named(name, ExpName))
        {
            return TryExp(numbers, out waveform, out error);
        }

        if (Named(name, PwlName))
        {
            return TryPwl(numbers, out waveform, out error);
        }

        error = WaveformError.UnknownFunction;

        return false;
    }

    private static bool TryPulse(
        double[] numbers, [NotNullWhen(true)] out Waveform? waveform, out WaveformError error)
    {
        waveform = null;

        if (numbers.Length is not (PulseArguments or PulseArguments + 1))
        {
            error = WaveformError.WrongArgumentCount;

            return false;
        }

        if (!TryCycles(numbers, PulseArguments, out var cycles, out error))
        {
            return false;
        }

        waveform = new PulseWaveform(
            numbers[0],
            numbers[1],
            numbers[2],
            numbers[3],
            numbers[4],
            numbers[5],
            numbers[6],
            cycles);

        return true;
    }

    private static bool TrySine(
        double[] numbers, [NotNullWhen(true)] out Waveform? waveform, out WaveformError error)
    {
        waveform = null;

        if (numbers.Length < SineArguments || numbers.Length > SineArguments + 4)
        {
            error = WaveformError.WrongArgumentCount;

            return false;
        }

        if (!TryCycles(numbers, SineArguments + 3, out var cycles, out error))
        {
            return false;
        }

        waveform = new SineWaveform(
            numbers[0],
            numbers[1],
            numbers[2],
            At(numbers, SineArguments),
            At(numbers, SineArguments + 1),
            At(numbers, SineArguments + 2),
            cycles);

        return true;
    }

    private static bool TryExp(
        double[] numbers, [NotNullWhen(true)] out Waveform? waveform, out WaveformError error)
    {
        waveform = null;

        if (numbers.Length != ExpArguments)
        {
            error = WaveformError.WrongArgumentCount;

            return false;
        }

        error = WaveformError.None;
        waveform = new ExpWaveform(
            numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5]);

        return true;
    }

    private static bool TryPwl(
        double[] numbers, [NotNullWhen(true)] out Waveform? waveform, out WaveformError error)
    {
        waveform = null;

        if (numbers.Length < PwlValuesPerPoint || numbers.Length % PwlValuesPerPoint != 0)
        {
            error = WaveformError.WrongArgumentCount;

            return false;
        }

        var points = ImmutableArray.CreateBuilder<WaveformPoint>(
            numbers.Length / PwlValuesPerPoint);

        for (var index = 0; index < numbers.Length; index += PwlValuesPerPoint)
        {
            points.Add(new WaveformPoint(numbers[index], numbers[index + 1]));
        }

        error = WaveformError.None;
        waveform = new PwlWaveform(points.ToImmutable());

        return true;
    }

    private static bool TryCycles(
        double[] numbers, int index, out int? cycles, out WaveformError error)
    {
        cycles = null;
        error = WaveformError.None;

        if (numbers.Length <= index)
        {
            return true;
        }

        var value = numbers[index];

        if (value <= 0 || value != Math.Floor(value) || value > int.MaxValue)
        {
            error = WaveformError.InvalidCycleCount;

            return false;
        }

        cycles = (int)value;

        return true;
    }

    private static double At(double[] numbers, int index) =>
        index < numbers.Length ? numbers[index] : 0;

    private static bool Named(string name, string expected) =>
        string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);

    private static string Composed(string name, IEnumerable<string> arguments) =>
        name + Open + string.Join(' ', arguments) + Close;

    private static IEnumerable<string> PulseArgumentsOf(PulseWaveform pulse)
    {
        yield return PrefixedNumber.Format(pulse.V1);
        yield return PrefixedNumber.Format(pulse.V2);
        yield return PrefixedNumber.Format(pulse.Delay);
        yield return PrefixedNumber.Format(pulse.Rise);
        yield return PrefixedNumber.Format(pulse.Fall);
        yield return PrefixedNumber.Format(pulse.On);
        yield return PrefixedNumber.Format(pulse.Period);

        if (pulse.Cycles is { } cycles)
        {
            yield return cycles.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static IEnumerable<string> SineArgumentsOf(SineWaveform sine)
    {
        var tail = new List<string>();

        if (sine.Cycles is { } cycles)
        {
            tail.Add(cycles.ToString(CultureInfo.InvariantCulture));
        }

        Prepend(tail, sine.PhaseDegrees);
        Prepend(tail, sine.Theta);
        Prepend(tail, sine.Delay);

        return
        [
            PrefixedNumber.Format(sine.Offset),
            PrefixedNumber.Format(sine.Amplitude),
            PrefixedNumber.Format(sine.Frequency),
            .. tail,
        ];
    }

    private static void Prepend(List<string> tail, double value)
    {
        if (tail.Count > 0 || value != 0)
        {
            tail.Insert(0, PrefixedNumber.Format(value));
        }
    }

    private static IEnumerable<string> ExpArgumentsOf(ExpWaveform exponential)
    {
        yield return PrefixedNumber.Format(exponential.V1);
        yield return PrefixedNumber.Format(exponential.V2);
        yield return PrefixedNumber.Format(exponential.RiseDelay);
        yield return PrefixedNumber.Format(exponential.RiseTau);
        yield return PrefixedNumber.Format(exponential.FallDelay);
        yield return PrefixedNumber.Format(exponential.FallTau);
    }

    private static IEnumerable<string> PwlArgumentsOf(PwlWaveform piecewise)
    {
        foreach (var point in piecewise.Points)
        {
            yield return PrefixedNumber.Format(point.Time);
            yield return PrefixedNumber.Format(point.Value);
        }
    }
}
