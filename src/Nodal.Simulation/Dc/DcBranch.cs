namespace Nodal.Simulation.Dc;

internal sealed record DcBranch(double Voltage, double Resistance)
{
    public static DcBranch Short { get; } = new(0, 0);

    public bool IsIdeal => Resistance == 0;
}
