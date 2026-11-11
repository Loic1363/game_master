namespace Nodal.Simulation.Dc;

internal sealed record DcAttempt(
    bool Converged,
    double[] Solution,
    double[] DeviceState,
    int Iterations,
    SwitchIdentity? UnstableSwitch = null)
{
    public static DcAttempt Failed(
        double[] deviceState, int iterations, SwitchIdentity? unstableSwitch = null) =>
        new(false, [], deviceState, iterations, unstableSwitch);
}
