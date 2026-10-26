namespace Nodal.Simulation.Transient;

internal sealed record TransientSolve(
    bool Converged, double[] Solution, double[] JunctionVoltages, int Iterations)
{
    public static TransientSolve Failed(int iterations) => new(false, [], [], iterations);
}
