using Nodal.Foundation.Schedules;

namespace Nodal.Simulation.Elaboration;

public sealed record SwitchSettings(
    double OnResistance,
    double OffResistance,
    double Threshold,
    double Hysteresis,
    PressSchedule? Presses)
{
    public double ResistanceOf(bool closed) => closed ? OnResistance : OffResistance;

    public bool IsClosedAt(double time) => Presses is { } presses && presses.IsClosedAt(time);

    public double ResistanceAt(double time) => ResistanceOf(IsClosedAt(time));
}
