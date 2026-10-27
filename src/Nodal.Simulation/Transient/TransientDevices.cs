using Nodal.Simulation.Dc;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Transient;

internal static class TransientDevices
{
    public static DeviceOperatingPoint? Describe(
        TransientSeries series,
        Func<string, double> nodeVoltage,
        Func<string, double> pinCurrent) => series.Kind switch
        {
            ComponentKind.Nmos or ComponentKind.Pmos => Mosfet(
                series, nodeVoltage, pinCurrent, series.Kind == ComponentKind.Pmos),

            ComponentKind.Npn or ComponentKind.Pnp => Bipolar(
                series, nodeVoltage, pinCurrent, series.Kind == ComponentKind.Pnp),

            _ => null,
        };

    private static DeviceOperatingPoint? Mosfet(
        TransientSeries series,
        Func<string, double> nodeVoltage,
        Func<string, double> pinCurrent,
        bool pChannel)
    {
        if (NetOf(series, ComponentModels.DrainPin) is not { } drain
            || NetOf(series, ComponentModels.SourcePin) is not { } source
            || NetOf(series, ComponentModels.GatePin) is not { } gate)
        {
            return null;
        }

        var reference = nodeVoltage(source);

        return DeviceDescriptions.Mosfet(
            pChannel,
            nodeVoltage(gate) - reference,
            nodeVoltage(drain) - reference,
            pinCurrent(ComponentModels.DrainPin));
    }

    private static DeviceOperatingPoint? Bipolar(
        TransientSeries series,
        Func<string, double> nodeVoltage,
        Func<string, double> pinCurrent,
        bool pType)
    {
        if (NetOf(series, ComponentModels.CollectorPin) is not { } collector
            || NetOf(series, ComponentModels.EmitterPin) is not { } emitter
            || NetOf(series, ComponentModels.BasePin) is not { } baseNet)
        {
            return null;
        }

        var reference = nodeVoltage(baseNet);

        return DeviceDescriptions.Bipolar(
            pType,
            reference - nodeVoltage(emitter),
            reference - nodeVoltage(collector),
            pinCurrent(ComponentModels.CollectorPin),
            pinCurrent(ComponentModels.BasePin));
    }

    private static string? NetOf(TransientSeries series, string pinName) =>
        series.TryGetPin(pinName, out var pin) ? pin.Net : null;
}
