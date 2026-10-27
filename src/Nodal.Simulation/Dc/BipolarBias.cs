namespace Nodal.Simulation.Dc;

public readonly record struct BipolarBias(
    DeviceRegion Region,
    double EmitterBranchCurrent,
    double CollectorBranchCurrent,
    double TransportCurrent,
    double EmitterJunctionConductance,
    double CollectorJunctionConductance,
    double ForwardTransconductance,
    double ReverseTransconductance)
{
    public double CollectorCurrent => TransportCurrent - CollectorBranchCurrent;

    public double BaseCurrent => EmitterBranchCurrent + CollectorBranchCurrent;

    public double EmitterCurrent => -(CollectorCurrent + BaseCurrent);
}
