namespace Nodal.Simulation.Transient;

internal sealed class TransientState
{
    public TransientState(double[] across, double[] through, double[] deviceState)
    {
        Across = across;
        Through = through;
        DeviceState = deviceState;
    }

    public double[] Across { get; }

    public double[] Through { get; }

    public double[] DeviceState { get; }
}
