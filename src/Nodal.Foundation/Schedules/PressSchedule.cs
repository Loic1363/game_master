using System.Collections.Immutable;
using Nodal.Foundation.Collections;

namespace Nodal.Foundation.Schedules;

public sealed record PressSchedule
{
    public const int MaxPresses = 1000;

    public PressSchedule(ImmutableArray<PressInterval> presses) =>
        Presses = presses.IsDefault ? [] : presses;

    public static PressSchedule Released { get; } = new([]);

    public static PressSchedule Pressed { get; } = new([PressInterval.FromStart]);

    public ImmutableArray<PressInterval> Presses { get; }

    public bool IsReleased => Presses.IsEmpty;

    public bool IsPressedThroughout => Presses.Length == 1 && Presses[0].IsOpenEnded;

    public bool IsValid => !Validate().IsFault;

    public ImmutableArray<double> Instants => [.. Presses.SelectMany(press => press.Instants)];

    public bool IsClosedAt(double time)
    {
        foreach (var press in Presses)
        {
            if (press.Covers(time))
            {
                return true;
            }
        }

        return false;
    }

    public PressScheduleFault Validate()
    {
        if (Presses.Length > MaxPresses)
        {
            return PressScheduleFault.Of(PressScheduleError.TooManyPresses);
        }

        for (var index = 0; index < Presses.Length; index++)
        {
            var fault = FaultOf(index);

            if (fault.IsFault)
            {
                return fault;
            }
        }

        return PressScheduleFault.None;
    }

    public bool Equals(PressSchedule? other) =>
        other is not null && StructuralEquality.ItemsEqual(Presses, other.Presses);

    public override int GetHashCode() => StructuralEquality.ItemsHash(Presses);

    public override string ToString() => PressScheduleText.Format(this);

    private PressScheduleFault FaultOf(int index)
    {
        var press = Presses[index];

        if (!double.IsFinite(press.Start)
            || (press.End is { } bound && !double.IsFinite(bound)))
        {
            return PressScheduleFault.Of(PressScheduleError.Unrecognized);
        }

        if (press.Start < 0)
        {
            return PressScheduleFault.Of(PressScheduleError.NegativeStart);
        }

        if (press.End is { } end && end <= press.Start)
        {
            return PressScheduleFault.At(PressScheduleError.EndNotAfterStart, press);
        }

        if (press.IsOpenEnded && press.Start != 0)
        {
            return PressScheduleFault.Of(PressScheduleError.Unrecognized);
        }

        if (press.IsOpenEnded && index < Presses.Length - 1)
        {
            return PressScheduleFault.Of(PressScheduleError.OutOfOrder);
        }

        return index > 0 && Presses[index - 1].End is { } previous && press.Start < previous
            ? PressScheduleFault.Of(PressScheduleError.OutOfOrder)
            : PressScheduleFault.None;
    }
}
