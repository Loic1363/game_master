using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Nodal.Foundation.Quantities;

namespace Nodal.Foundation.Schedules;

public static class PressScheduleText
{
    public const string ReleasedName = "released";
    public const string PressedName = "pressed";
    public const string RangeSeparator = "to";
    public const string Template = "1m to 3m, 5m to 6m";

    private const char PressDelimiter = ',';
    private const string PressSeparator = ", ";
    private const int RangeTokens = 3;

    public static bool TryParse(
        string? text,
        [NotNullWhen(true)] out PressSchedule? schedule,
        out PressScheduleFault fault)
    {
        schedule = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            fault = PressScheduleFault.Of(PressScheduleError.Unrecognized);

            return false;
        }

        var trimmed = text.Trim();

        if (Named(trimmed, ReleasedName))
        {
            schedule = PressSchedule.Released;
            fault = PressScheduleFault.None;

            return true;
        }

        if (Named(trimmed, PressedName))
        {
            schedule = PressSchedule.Pressed;
            fault = PressScheduleFault.None;

            return true;
        }

        if (!TryReadPresses(trimmed, out var presses, out fault))
        {
            return false;
        }

        var candidate = new PressSchedule(presses);

        fault = candidate.Validate();

        if (fault.IsFault)
        {
            return false;
        }

        schedule = candidate;

        return true;
    }

    public static string Format(PressSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        if (schedule.IsReleased)
        {
            return ReleasedName;
        }

        return schedule.IsPressedThroughout
            ? PressedName
            : string.Join(PressSeparator, schedule.Presses.Select(TextOf));
    }

    private static string TextOf(PressInterval press) =>
        PrefixedNumber.Format(press.Start)
        + " " + RangeSeparator + " "
        + PrefixedNumber.Format(press.End ?? press.Start);

    private static bool TryReadPresses(
        string text, out ImmutableArray<PressInterval> presses, out PressScheduleFault fault)
    {
        var builder = ImmutableArray.CreateBuilder<PressInterval>();

        foreach (var part in text.Split(PressDelimiter))
        {
            if (!TryReadPress(part, out var press))
            {
                presses = [];
                fault = PressScheduleFault.Of(PressScheduleError.Unrecognized);

                return false;
            }

            builder.Add(press);

            if (builder.Count > PressSchedule.MaxPresses)
            {
                presses = [];
                fault = PressScheduleFault.Of(PressScheduleError.TooManyPresses);

                return false;
            }
        }

        presses = builder.ToImmutable();
        fault = PressScheduleFault.None;

        return true;
    }

    private static bool TryReadPress(string text, out PressInterval press)
    {
        press = PressInterval.FromStart;

        var tokens = text.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length != RangeTokens
            || !Named(tokens[1], RangeSeparator)
            || !TrySeconds(tokens[0], out var start)
            || !TrySeconds(tokens[2], out var end))
        {
            return false;
        }

        press = new PressInterval(start, end);

        return true;
    }

    private static bool TrySeconds(string text, out double seconds)
    {
        if (Quantity.TryParse(text, Unit.Second, out var quantity, out _))
        {
            seconds = quantity.Value;

            return true;
        }

        seconds = 0;

        return false;
    }

    private static bool Named(string text, string name) =>
        string.Equals(text, name, StringComparison.OrdinalIgnoreCase);
}
