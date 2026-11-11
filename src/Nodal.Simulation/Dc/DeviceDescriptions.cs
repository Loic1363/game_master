using System.Collections.Immutable;
using Nodal.Foundation.Quantities;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Dc;

internal static class DeviceDescriptions
{
    private const double MeasurableBaseCurrent = 1e-12;

    public static DeviceOperatingPoint Mosfet(
        bool pChannel, double gateSource, double drainSource, double drainCurrent) =>
        new(
            MosfetModel.Evaluate(pChannel, gateSource, drainSource).Region,
            [
                new DeviceQuantity(
                    DeviceQuantityNames.GateSourceVoltage,
                    new Quantity(gateSource, Unit.Volt)),

                new DeviceQuantity(
                    DeviceQuantityNames.DrainSourceVoltage,
                    new Quantity(drainSource, Unit.Volt)),

                new DeviceQuantity(
                    DeviceQuantityNames.DrainCurrent,
                    new Quantity(drainCurrent, Unit.Ampere)),
            ]);

    public static DeviceOperatingPoint Bipolar(
        bool pType,
        double baseEmitter,
        double baseCollector,
        double collectorCurrent,
        double baseCurrent)
    {
        var region = BipolarModel.Evaluate(pType, baseEmitter, baseCollector).Region;
        var quantities = ImmutableArray.CreateBuilder<DeviceQuantity>(5);

        quantities.Add(new DeviceQuantity(
            DeviceQuantityNames.BaseEmitterVoltage, new Quantity(baseEmitter, Unit.Volt)));

        quantities.Add(new DeviceQuantity(
            DeviceQuantityNames.CollectorEmitterVoltage,
            new Quantity(baseEmitter - baseCollector, Unit.Volt)));

        quantities.Add(new DeviceQuantity(
            DeviceQuantityNames.CollectorCurrent,
            new Quantity(collectorCurrent, Unit.Ampere)));

        quantities.Add(new DeviceQuantity(
            DeviceQuantityNames.BaseCurrent, new Quantity(baseCurrent, Unit.Ampere)));

        if (ReportsCurrentGain(region, baseCurrent))
        {
            quantities.Add(new DeviceQuantity(
                DeviceQuantityNames.CurrentGain,
                new Quantity(collectorCurrent / baseCurrent, Unit.Dimensionless)));
        }

        return new DeviceOperatingPoint(region, quantities.ToImmutable());
    }

    public static DeviceOperatingPoint Switch(
        bool closed, double control, double voltage, double current) =>
        new(
            closed ? DeviceRegion.On : DeviceRegion.Off,
            [
                Quantity(DeviceQuantityNames.ControlVoltage, control, Unit.Volt),
                Quantity(DeviceQuantityNames.Voltage, voltage, Unit.Volt),
                Quantity(DeviceQuantityNames.Current, current, Unit.Ampere),
            ]);

    public static DeviceOperatingPoint PushButton(
        bool pressed, double voltage, double current) =>
        new(
            pressed ? DeviceRegion.On : DeviceRegion.Off,
            [
                Quantity(DeviceQuantityNames.Voltage, voltage, Unit.Volt),
                Quantity(DeviceQuantityNames.Current, current, Unit.Ampere),
            ]);

    public static DeviceOperatingPoint ControlledSource(
        ComponentKind kind,
        double control,
        double outputVoltage,
        double outputCurrent)
    {
        var quantities = ImmutableArray.CreateBuilder<DeviceQuantity>(3);

        quantities.Add(ComponentModels.IsCurrentControlled(kind)
            ? Quantity(DeviceQuantityNames.ControlCurrent, control, Unit.Ampere)
            : Quantity(DeviceQuantityNames.ControlVoltage, control, Unit.Volt));

        var voltage = Quantity(DeviceQuantityNames.OutputVoltage, outputVoltage, Unit.Volt);
        var current = Quantity(DeviceQuantityNames.OutputCurrent, outputCurrent, Unit.Ampere);

        if (kind is ComponentKind.Vcvs or ComponentKind.Ccvs)
        {
            quantities.Add(voltage);
            quantities.Add(current);
        }
        else
        {
            quantities.Add(current);
            quantities.Add(voltage);
        }

        return new DeviceOperatingPoint(null, quantities.ToImmutable());
    }

    private static DeviceQuantity Quantity(string name, double value, Unit unit) =>
        new(name, new Quantity(value, unit));

    private static bool ReportsCurrentGain(DeviceRegion region, double baseCurrent) =>
        region is DeviceRegion.ForwardActive or DeviceRegion.Saturation
        && Math.Abs(baseCurrent) > MeasurableBaseCurrent;
}
