namespace Nodal.Simulation.Dc;

public readonly record struct MosfetBias(
    DeviceRegion Region,
    double ChannelCurrent,
    double Transconductance,
    double OutputConductance);
