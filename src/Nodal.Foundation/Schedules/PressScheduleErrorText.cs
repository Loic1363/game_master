using System.Globalization;
using Nodal.Foundation.Quantities;

namespace Nodal.Foundation.Schedules;

public static class PressScheduleErrorText
{
    private const string CountFormat = "N0";

    public static string Of(PressScheduleFault fault) => fault.Error switch
    {
        PressScheduleError.None => string.Empty,

        PressScheduleError.EndNotAfterStart =>
            "Each press must end after it starts: "
            + $"{PrefixedNumber.Format(fault.Start)} to {PrefixedNumber.Format(fault.End)}.",

        PressScheduleError.OutOfOrder =>
            "Presses must be in increasing time order and must not overlap.",

        PressScheduleError.NegativeStart => "A press cannot start before 0 s.",

        PressScheduleError.TooManyPresses =>
            $"A schedule has at most {MaxPressesText} presses.",

        _ => "Unrecognized press schedule. Use "
            + $"{PressScheduleText.ReleasedName}, {PressScheduleText.PressedName}, or intervals "
            + $"such as {PressScheduleText.Template}.",
    };

    private static string MaxPressesText =>
        PressSchedule.MaxPresses.ToString(CountFormat, CultureInfo.InvariantCulture);
}
