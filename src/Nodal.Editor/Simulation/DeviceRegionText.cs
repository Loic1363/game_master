using Nodal.Simulation.Dc;

namespace Nodal.Editor.Simulation;

public static class DeviceRegionText
{
    public const string Cutoff = "cutoff";
    public const string Linear = "linear";
    public const string Saturation = "saturation";
    public const string ForwardActive = "forward active";
    public const string ReverseActive = "reverse active";

    public static string Of(DeviceRegion region) => region switch
    {
        DeviceRegion.Linear => Linear,
        DeviceRegion.Saturation => Saturation,
        DeviceRegion.ForwardActive => ForwardActive,
        DeviceRegion.ReverseActive => ReverseActive,
        _ => Cutoff,
    };
}
