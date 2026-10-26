namespace Nodal.Simulation.Elaboration;

public sealed record ElementRatings(double? Power, double? Voltage, double? Current)
{
    public static ElementRatings None { get; } = new(null, null, null);
}
