using System.Collections.Immutable;
using Nodal.Foundation.Schedules;
using Nodal.Foundation.Waveforms;

namespace Nodal.Simulation.Spice;

internal static class SpiceSchedules
{
    public const double EdgeDuration = 1e-9;
    public const double OnLevel = 1;
    public const double OffLevel = 0;
    public const double Threshold = 0.5;

    private const double Half = 0.5;

    public static PwlWaveform PwlOf(PressSchedule? schedule)
    {
        var presses = schedule ?? PressSchedule.Released;
        var transitions = Transitions(presses);
        var level = presses.IsClosedAt(0) ? OnLevel : OffLevel;
        var points = new List<WaveformPoint>();

        for (var index = 0; index < transitions.Count; index++)
        {
            var transition = transitions[index];
            var edge = EdgeOf(transitions, index);
            var rising = transition.Time - (edge * Half);

            if (rising <= 0)
            {
                level = transition.Level;

                continue;
            }

            points.Add(new WaveformPoint(rising, Previous(points, level)));
            points.Add(new WaveformPoint(transition.Time + (edge * Half), transition.Level));
        }

        return new PwlWaveform([new WaveformPoint(0, level), .. points]);
    }

    private static double Previous(List<WaveformPoint> points, double level) =>
        points.Count == 0 ? level : points[^1].Value;

    private static List<Transition> Transitions(PressSchedule schedule)
    {
        var transitions = new List<Transition>();

        foreach (var press in schedule.Presses)
        {
            if (press.Start > 0)
            {
                transitions.Add(new Transition(press.Start, OnLevel));
            }

            if (press.End is { } end)
            {
                transitions.Add(new Transition(end, OffLevel));
            }
        }

        return transitions;
    }

    private static double EdgeOf(List<Transition> transitions, int index)
    {
        var time = transitions[index].Time;
        var before = index > 0 ? time - transitions[index - 1].Time : double.PositiveInfinity;

        var after = index < transitions.Count - 1
            ? transitions[index + 1].Time - time
            : double.PositiveInfinity;

        var gap = Math.Min(before, after);

        return gap < EdgeDuration ? gap * Half : EdgeDuration;
    }

    private readonly record struct Transition(double Time, double Level);
}
