namespace Nodal.Simulation.Dc;

internal sealed record DcBias(double[] JunctionVoltages, double NodeGmin, double SourceScale)
{
    public static DcBias Rest(int elementCount) => new(new double[elementCount], 0, 1);

    public DcBias At(double nodeGmin, double sourceScale) =>
        this with { NodeGmin = nodeGmin, SourceScale = sourceScale };

    public DcBias With(double[] junctionVoltages) =>
        this with { JunctionVoltages = junctionVoltages };
}
