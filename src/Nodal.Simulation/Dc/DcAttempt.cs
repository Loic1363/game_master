namespace Nodal.Simulation.Dc;

internal sealed record DcAttempt(
    bool Converged, double[] Solution, double[] DeviceState, int Iterations)
{
    public static DcAttempt Failed(double[] deviceState, int iterations) =>
        new(false, [], deviceState, iterations);
}
