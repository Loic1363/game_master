namespace Nodal.Foundation.Schedules;

public sealed record PressInterval(double Start, double? End)
{
    public static PressInterval FromStart { get; } = new(0, null);

    public bool IsOpenEnded => End is null;

    public bool Covers(double time) =>
        time >= Start && (End is not { } end || time < end);

    public IEnumerable<double> Instants => End is { } end ? [Start, end] : [Start];
}
