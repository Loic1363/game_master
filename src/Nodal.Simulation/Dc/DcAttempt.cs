namespace Nodal.Simulation.Dc;

internal sealed record DcAttempt(
    bool Converged, double[] Solution, double[] JunctionVoltages, int Iterations)
{
    public static DcAttempt Failed(double[] junctionVoltages, int iterations) =>
        new(false, [], junctionVoltages, iterations);
}
