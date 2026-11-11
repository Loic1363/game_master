namespace Nodal.Foundation.Schedules;

public readonly record struct PressScheduleFault(PressScheduleError Error, double Start, double End)
{
    public static PressScheduleFault None { get; } = new(PressScheduleError.None, 0, 0);

    public bool IsFault => Error != PressScheduleError.None;

    public static PressScheduleFault Of(PressScheduleError error) => new(error, 0, 0);

    public static PressScheduleFault At(PressScheduleError error, PressInterval press)
    {
        ArgumentNullException.ThrowIfNull(press);

        return new PressScheduleFault(error, press.Start, press.End ?? press.Start);
    }
}
