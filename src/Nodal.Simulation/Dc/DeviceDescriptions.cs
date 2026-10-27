using System.Collections.Immutable;
using Nodal.Foundation.Quantities;

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

        if (Math.Abs(baseCurrent) > MeasurableBaseCurrent)
        {
            quantities.Add(new DeviceQuantity(
                DeviceQuantityNames.CurrentGain,
                new Quantity(collectorCurrent / baseCurrent, Unit.Dimensionless)));
        }

        return new DeviceOperatingPoint(
            BipolarModel.Evaluate(pType, baseEmitter, baseCollector).Region,
            quantities.ToImmutable());
    }
}
