namespace Nodal.Foundation.Schedules;

public enum PressScheduleError
{
    None,
    Unrecognized,
    EndNotAfterStart,
    OutOfOrder,
    NegativeStart,
    TooManyPresses,
}
