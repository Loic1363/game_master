using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Nodal.Foundation.Schedules;

namespace Nodal.Circuit.Persistence;

internal static class ScheduleJson
{
    private const int OpenEndedLength = 1;
    private const int ClosedLength = 2;

    public static bool TryRead(
        JsonElement element, [NotNullWhen(true)] out PressSchedule? schedule, out string problem)
    {
        schedule = null;

        if (!TryReadBody(element, out var body, out problem)
            || !TryReadPresses(body, out var presses, out problem))
        {
            return false;
        }

        var candidate = new PressSchedule(presses);
        var fault = candidate.Validate();

        if (!fault.IsFault)
        {
            schedule = candidate;

            return true;
        }

        problem = Lowered(PressScheduleErrorText.Of(fault));

        return false;
    }

    public static void Write(Utf8JsonWriter writer, PressSchedule schedule)
    {
        writer.WriteStartObject();
        writer.WriteStartObject(ScheduleFields.Schedule);
        writer.WriteStartArray(ScheduleFields.Presses);

        foreach (var press in schedule.Presses)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(press.Start);

            if (press.End is { } end)
            {
                writer.WriteNumberValue(end);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static bool TryReadBody(JsonElement element, out JsonElement body, out string problem)
    {
        body = element;
        problem = string.Empty;

        if (element.ValueKind != JsonValueKind.Object)
        {
            problem = $"must be {JsonValueKindNames.Of(JsonValueKind.Object)}";

            return false;
        }

        var names = new List<string>();

        foreach (var property in element.EnumerateObject())
        {
            names.Add(property.Name);
        }

        if (names.Count != 1
            || !string.Equals(names[0], ScheduleFields.Schedule, StringComparison.Ordinal))
        {
            problem = $"must hold exactly one field named '{ScheduleFields.Schedule}'";

            return false;
        }

        body = element.GetProperty(ScheduleFields.Schedule);

        if (body.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        problem = $"'{ScheduleFields.Schedule}' must be "
            + JsonValueKindNames.Of(JsonValueKind.Object);

        return false;
    }

    private static bool TryReadPresses(
        JsonElement body, out ImmutableArray<PressInterval> presses, out string problem)
    {
        presses = [];
        problem = string.Empty;

        if (!body.TryGetProperty(ScheduleFields.Presses, out var array)
            || array.ValueKind != JsonValueKind.Array)
        {
            problem = $"needs '{ScheduleFields.Presses}' as "
                + JsonValueKindNames.Of(JsonValueKind.Array);

            return false;
        }

        var builder = ImmutableArray.CreateBuilder<PressInterval>();

        foreach (var entry in array.EnumerateArray())
        {
            if (!TryReadPress(entry, out var press))
            {
                problem = $"'{ScheduleFields.Presses}' must hold one or two finite numbers "
                    + "per press";

                return false;
            }

            builder.Add(press);

            if (builder.Count > PressSchedule.MaxPresses)
            {
                problem = Lowered(PressScheduleErrorText.Of(
                    PressScheduleFault.Of(PressScheduleError.TooManyPresses)));

                return false;
            }
        }

        presses = builder.ToImmutable();

        return true;
    }

    private static bool TryReadPress(JsonElement entry, out PressInterval press)
    {
        press = PressInterval.FromStart;

        if (entry.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var length = entry.GetArrayLength();

        if (length is not (OpenEndedLength or ClosedLength))
        {
            return false;
        }

        var numbers = new double[length];
        var index = 0;

        foreach (var number in entry.EnumerateArray())
        {
            if (!TryNumber(number, out numbers[index]))
            {
                return false;
            }

            index++;
        }

        press = length == OpenEndedLength
            ? new PressInterval(numbers[0], null)
            : new PressInterval(numbers[0], numbers[1]);

        return true;
    }

    private static bool TryNumber(JsonElement field, out double number)
    {
        number = 0;

        return field.ValueKind == JsonValueKind.Number
            && field.TryGetDouble(out number)
            && double.IsFinite(number);
    }

    private static string Lowered(string sentence) =>
        sentence.Length == 0
            ? sentence
            : char.ToLowerInvariant(sentence[0]) + sentence[1..].TrimEnd('.');
}
