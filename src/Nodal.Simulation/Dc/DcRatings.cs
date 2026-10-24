namespace Nodal.Simulation.Dc;

internal sealed record DcRatings(double? Power, double? Voltage, double? Current)
{
    public static DcRatings None { get; } = new(null, null, null);
}
