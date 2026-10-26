using System.Globalization;
using Nodal.Foundation.Quantities;
using Nodal.Simulation.Transient;

namespace Nodal.Editor.Simulation;

public static class TransientStatusText
{
    public const string NotRunText = "Transient: not run";
    public const string CancelledText = "Transient: cancelled";
    public const string RefusedText = "Transient: refused";

    private const string Prefix = "Transient: ";
    private const string SolvedPrefix = "solved to ";
    private const string StoppedPrefix = "stopped at ";
    private const string PointNoun = "point";
    private const double Percent = 100;

    public static string Running(double fraction)
    {
        var clamped = Math.Clamp(double.IsFinite(fraction) ? fraction : 0, 0, 1);
        var percent = (int)Math.Floor(clamped * Percent);

        return Prefix + percent.ToString(CultureInfo.InvariantCulture) + " %";
    }

    public static string Of(TransientResult? result)
    {
        if (result is null)
        {
            return NotRunText;
        }

        if (result.IsPartial)
        {
            return Prefix
                + StoppedPrefix
                + new Quantity(result.FailureTime ?? result.FinalTime, Unit.Second);
        }

        if (!result.IsSolved)
        {
            return RefusedText;
        }

        return Prefix
            + SolvedPrefix
            + new Quantity(result.FinalTime, Unit.Second)
            + $" ({Counted(result.PointCount)})";
    }

    private static string Counted(int points) =>
        points == 1 ? $"1 {PointNoun}" : $"{points} {PointNoun}s";
}
