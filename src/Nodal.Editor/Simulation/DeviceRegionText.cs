using Nodal.Simulation.Dc;
using Nodal.Simulation.Elaboration;

namespace Nodal.Editor.Simulation;

public static class DeviceRegionText
{
    public const string Cutoff = "cutoff";
    public const string Linear = "linear";
    public const string Saturation = "saturation";
    public const string ForwardActive = "forward active";
    public const string ReverseActive = "reverse active";
    public const string On = "on";
    public const string Off = "off";
    public const string Pressed = "pressed";
    public const string Released = "released";

    public static string Of(DeviceRegion region) => region switch
    {
        DeviceRegion.Linear => Linear,
        DeviceRegion.Saturation => Saturation,
        DeviceRegion.ForwardActive => ForwardActive,
        DeviceRegion.ReverseActive => ReverseActive,
        DeviceRegion.On => On,
        DeviceRegion.Off => Off,
        _ => Cutoff,
    };

    public static string Of(DeviceRegion region, ComponentKind kind)
    {
        if (kind != ComponentKind.PushButton)
        {
            return Of(region);
        }

        return region == DeviceRegion.On ? Pressed : Released;
    }
}
