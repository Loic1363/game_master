namespace Nodal.Simulation.Transient;

internal sealed class TransientState
{
    public TransientState(double[] across, double[] through, double[] junctionVoltages)
    {
        Across = across;
        Through = through;
        JunctionVoltages = junctionVoltages;
    }

    public double[] Across { get; }

    public double[] Through { get; }

    public double[] JunctionVoltages { get; }
}
