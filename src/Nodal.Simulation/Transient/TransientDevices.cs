using Nodal.Simulation.Dc;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Transient;

internal static class TransientDevices
{
    public static DeviceOperatingPoint? Describe(
        TransientSeries series,
        Func<string, double> nodeVoltage,
        Func<string, double> pinCurrent,
        double across,
        bool closed) => series.Kind switch
        {
            ComponentKind.Nmos or ComponentKind.Pmos => Mosfet(
                series, nodeVoltage, pinCurrent, series.Kind == ComponentKind.Pmos),

            ComponentKind.Npn or ComponentKind.Pnp => Bipolar(
                series, nodeVoltage, pinCurrent, series.Kind == ComponentKind.Pnp),

            ComponentKind.Vcvs or ComponentKind.Vccs
                or ComponentKind.Cccs or ComponentKind.Ccvs =>
                ControlledSource(series, nodeVoltage, pinCurrent, across),

            ComponentKind.Switch => DeviceDescriptions.Switch(
                closed,
                ControlVoltage(series, nodeVoltage),
                across,
                pinCurrent(ComponentModels.PlusPin)),

            ComponentKind.PushButton => DeviceDescriptions.PushButton(
                closed, across, pinCurrent(ComponentModels.FirstContactPin)),

            _ => null,
        };

    private static DeviceOperatingPoint? ControlledSource(
        TransientSeries series,
        Func<string, double> nodeVoltage,
        Func<string, double> pinCurrent,
        double across)
    {
        if (NetOf(series, ComponentModels.ControlPlusPin) is null)
        {
            return null;
        }

        var control = ComponentModels.IsCurrentControlled(series.Kind)
            ? pinCurrent(ComponentModels.ControlPlusPin)
            : ControlVoltage(series, nodeVoltage);

        return DeviceDescriptions.ControlledSource(
            series.Kind, control, across, -pinCurrent(ComponentModels.PlusPin));
    }

    private static double ControlVoltage(
        TransientSeries series, Func<string, double> nodeVoltage)
    {
        if (NetOf(series, ComponentModels.ControlPlusPin) is not { } plus
            || NetOf(series, ComponentModels.ControlMinusPin) is not { } minus)
        {
            return 0;
        }

        return nodeVoltage(plus) - nodeVoltage(minus);
    }

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
