namespace Nodal.Simulation.Dc;

internal sealed record DcBias(double[] DeviceState, double NodeGmin, double SourceScale)
{
    public static DcBias Rest(int stateCount) => new(new double[stateCount], 0, 1);

    public DcBias At(double nodeGmin, double sourceScale) =>
        this with { NodeGmin = nodeGmin, SourceScale = sourceScale };

    public DcBias With(double[] deviceState) => this with { DeviceState = deviceState };
}
