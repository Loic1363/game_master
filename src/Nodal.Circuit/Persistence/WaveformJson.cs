using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Nodal.Foundation.Waveforms;

namespace Nodal.Circuit.Persistence;

internal static class WaveformJson
{
    private const int PointLength = 2;

    public static bool TryRead(
        JsonElement element, [NotNullWhen(true)] out Waveform? waveform, out string problem)
    {
        waveform = null;
        problem = string.Empty;

        if (element.ValueKind != JsonValueKind.Object)
        {
            problem = $"must be {JsonValueKindNames.Of(JsonValueKind.Object)}";

            return false;
        }

        if (!TryReadKind(element, out var kind, out var body, out problem))
        {
            return false;
        }

        if (!TryReadShape(kind, body, out waveform, out problem))
        {
            return false;
        }

        var error = waveform.Validate();

        if (error == WaveformError.None)
        {
            return true;
        }

        waveform = null;
        problem = Lowered(WaveformErrorText.Of(error));

        return false;
    }

    public static void Write(Utf8JsonWriter writer, Waveform waveform)
    {
        writer.WriteStartObject();

        switch (waveform)
        {
            case PulseWaveform pulse:
                WritePulse(writer, pulse);
                break;

            case SineWaveform sine:
                WriteSine(writer, sine);
                break;

            case ExpWaveform exponential:
                WriteExp(writer, exponential);
                break;

            case PwlWaveform piecewise:
                WritePwl(writer, piecewise);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(waveform), waveform, "Unknown waveform kind.");
        }

        writer.WriteEndObject();
    }

    private static bool TryReadKind(
        JsonElement element,
        out WaveformKind kind,
        out JsonElement body,
        out string problem)
    {
        kind = WaveformKind.Pulse;
        body = element;
        problem = string.Empty;

        var names = new List<string>();

        foreach (var property in element.EnumerateObject())
        {
            names.Add(property.Name);
        }

        if (names.Count != 1)
        {
            problem = "must name exactly one function, such as '"
                + WaveformFields.Pulse + "'";

            return false;
        }

        if (!TryKindOf(names[0], out kind))
        {
            problem = $"names the unknown function '{names[0]}'";

            return false;
        }

        body = element.GetProperty(names[0]);

        if (body.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        problem = $"'{names[0]}' must be {JsonValueKindNames.Of(JsonValueKind.Object)}";

        return false;
    }

    private static bool TryKindOf(string name, out WaveformKind kind)
    {
        switch (name)
        {
            case WaveformFields.Pulse:
                kind = WaveformKind.Pulse;

                return true;

            case WaveformFields.Sine:
                kind = WaveformKind.Sine;

                return true;

            case WaveformFields.Exp:
                kind = WaveformKind.Exp;

                return true;

            case WaveformFields.Pwl:
                kind = WaveformKind.Pwl;

                return true;

            default:
                kind = WaveformKind.Pulse;

                return false;
        }
    }

    private static bool TryReadShape(
        WaveformKind kind,
        JsonElement body,
        [NotNullWhen(true)] out Waveform? waveform,
        out string problem)
    {
        waveform = null;
        problem = string.Empty;

        return kind switch
        {
            WaveformKind.Pulse => TryReadPulse(body, out waveform, out problem),
            WaveformKind.Sine => TryReadSine(body, out waveform, out problem),
            WaveformKind.Exp => TryReadExp(body, out waveform, out problem),
            _ => TryReadPwl(body, out waveform, out problem),
        };
    }

    private static bool TryReadPulse(
        JsonElement body, [NotNullWhen(true)] out Waveform? waveform, out string problem)
    {
        waveform = null;

        if (!TryNumbers(
                body,
                out var numbers,
                out problem,
                WaveformFields.FirstValue,
                WaveformFields.SecondValue,
                WaveformFields.Delay,
                WaveformFields.Rise,
                WaveformFields.Fall,
                WaveformFields.On,
                WaveformFields.Period)
            || !TryCycles(body, out var cycles, out problem))
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

    private static bool TryReadSine(
        JsonElement body, [NotNullWhen(true)] out Waveform? waveform, out string problem)
    {
        waveform = null;

        if (!TryNumbers(
                body,
                out var numbers,
                out problem,
                WaveformFields.Offset,
                WaveformFields.Amplitude,
                WaveformFields.Frequency,
                WaveformFields.Delay,
                WaveformFields.Theta,
                WaveformFields.Phase)
            || !TryCycles(body, out var cycles, out problem))
        {
            return false;
        }

        waveform = new SineWaveform(
            numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5], cycles);

        return true;
    }

    private static bool TryReadExp(
        JsonElement body, [NotNullWhen(true)] out Waveform? waveform, out string problem)
    {
        waveform = null;

        if (!TryNumbers(
                body,
                out var numbers,
                out problem,
                WaveformFields.FirstValue,
                WaveformFields.SecondValue,
                WaveformFields.RiseDelay,
                WaveformFields.RiseTau,
                WaveformFields.FallDelay,
                WaveformFields.FallTau))
        {
            return false;
        }

        waveform = new ExpWaveform(
            numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5]);

        return true;
    }

    private static bool TryReadPwl(
        JsonElement body, [NotNullWhen(true)] out Waveform? waveform, out string problem)
    {
        waveform = null;
        problem = string.Empty;

        if (!body.TryGetProperty(WaveformFields.Points, out var array)
            || array.ValueKind != JsonValueKind.Array)
        {
            problem = $"needs '{WaveformFields.Points}' as "
                + JsonValueKindNames.Of(JsonValueKind.Array);

            return false;
        }

        var points = ImmutableArray.CreateBuilder<WaveformPoint>();

        foreach (var entry in array.EnumerateArray())
        {
            if (!TryReadPoint(entry, out var point))
            {
                problem = $"'{WaveformFields.Points}' must hold pairs of finite numbers";

                return false;
            }

            points.Add(point);
        }

        waveform = new PwlWaveform(points.ToImmutable());

        return true;
    }

    private static bool TryReadPoint(JsonElement entry, out WaveformPoint point)
    {
        point = default;

        if (entry.ValueKind != JsonValueKind.Array || entry.GetArrayLength() != PointLength)
        {
            return false;
        }

        var numbers = new double[PointLength];
        var index = 0;

        foreach (var number in entry.EnumerateArray())
        {
            if (!TryNumber(number, out numbers[index]))
            {
                return false;
            }

            index++;
        }

        point = new WaveformPoint(numbers[0], numbers[1]);

        return true;
    }

    private static bool TryNumbers(
        JsonElement body, out double[] numbers, out string problem, params string[] names)
    {
        numbers = new double[names.Length];
        problem = string.Empty;

        for (var index = 0; index < names.Length; index++)
        {
            if (!body.TryGetProperty(names[index], out var field)
                || !TryNumber(field, out numbers[index]))
            {
                problem = $"needs '{names[index]}' as a finite number";
                numbers = [];

                return false;
            }
        }

        return true;
    }

    private static bool TryCycles(JsonElement body, out int? cycles, out string problem)
    {
        cycles = null;
        problem = string.Empty;

        if (!body.TryGetProperty(WaveformFields.Cycles, out var field))
        {
            return true;
        }

        if (field.ValueKind == JsonValueKind.Number
            && field.TryGetInt32(out var count)
            && count > 0)
        {
            cycles = count;

            return true;
        }

        problem = $"'{WaveformFields.Cycles}' must be a positive whole number";

        return false;
    }

    private static bool TryNumber(JsonElement field, out double number)
    {
        number = 0;

        return field.ValueKind == JsonValueKind.Number
            && field.TryGetDouble(out number)
            && double.IsFinite(number);
    }

    private static void WritePulse(Utf8JsonWriter writer, PulseWaveform pulse)
    {
        writer.WriteStartObject(WaveformFields.Pulse);
        writer.WriteNumber(WaveformFields.FirstValue, pulse.V1);
        writer.WriteNumber(WaveformFields.SecondValue, pulse.V2);
        writer.WriteNumber(WaveformFields.Delay, pulse.Delay);
        writer.WriteNumber(WaveformFields.Rise, pulse.Rise);
        writer.WriteNumber(WaveformFields.Fall, pulse.Fall);
        writer.WriteNumber(WaveformFields.On, pulse.On);
        writer.WriteNumber(WaveformFields.Period, pulse.Period);
        WriteCycles(writer, pulse.Cycles);
        writer.WriteEndObject();
    }

    private static void WriteSine(Utf8JsonWriter writer, SineWaveform sine)
    {
        writer.WriteStartObject(WaveformFields.Sine);
        writer.WriteNumber(WaveformFields.Offset, sine.Offset);
        writer.WriteNumber(WaveformFields.Amplitude, sine.Amplitude);
        writer.WriteNumber(WaveformFields.Frequency, sine.Frequency);
        writer.WriteNumber(WaveformFields.Delay, sine.Delay);
        writer.WriteNumber(WaveformFields.Theta, sine.Theta);
        writer.WriteNumber(WaveformFields.Phase, sine.PhaseDegrees);
        WriteCycles(writer, sine.Cycles);
        writer.WriteEndObject();
    }

    private static void WriteExp(Utf8JsonWriter writer, ExpWaveform exponential)
    {
        writer.WriteStartObject(WaveformFields.Exp);
        writer.WriteNumber(WaveformFields.FirstValue, exponential.V1);
        writer.WriteNumber(WaveformFields.SecondValue, exponential.V2);
        writer.WriteNumber(WaveformFields.RiseDelay, exponential.RiseDelay);
        writer.WriteNumber(WaveformFields.RiseTau, exponential.RiseTau);
        writer.WriteNumber(WaveformFields.FallDelay, exponential.FallDelay);
        writer.WriteNumber(WaveformFields.FallTau, exponential.FallTau);
        writer.WriteEndObject();
    }

    private static void WritePwl(Utf8JsonWriter writer, PwlWaveform piecewise)
    {
        writer.WriteStartObject(WaveformFields.Pwl);
        writer.WriteStartArray(WaveformFields.Points);

        foreach (var point in piecewise.Points)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(point.Time);
            writer.WriteNumberValue(point.Value);
            writer.WriteEndArray();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteCycles(Utf8JsonWriter writer, int? cycles)
    {
        if (cycles is { } count)
        {
            writer.WriteNumber(WaveformFields.Cycles, count);
        }
    }

    private static string Lowered(string sentence) =>
        sentence.Length == 0
            ? sentence
            : char.ToLowerInvariant(sentence[0]) + sentence[1..].TrimEnd('.');
}
