namespace Nodal.Simulation.Dc;

public sealed record DcOptions
{
    public static DcOptions Default { get; } = new();

    public double RelTol { get; init; } = 1e-3;

    public double VnTol { get; init; } = 1e-6;

    public double AbsTol { get; init; } = 1e-12;

    public int MaxIterations { get; init; } = 100;

    public double Gmin { get; init; } = 1e-12;

    public bool StepOnFailure { get; init; } = true;
}
