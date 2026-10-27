namespace Nodal.Simulation.Dc;

public static class BipolarModel
{
    public const double SaturationCurrent = 1e-14;

    public const double ForwardBeta = 100;

    public const double ReverseBeta = 1;

    private const double OnThermalVoltages = 5;

    public static double JunctionOnVoltage =>
        OnThermalVoltages * DiodeModel.EmissionVoltage;

    public static BipolarBias Evaluate(bool pType, double baseEmitter, double baseCollector)
    {
        var polarity = Polarity(pType);
        var typed = Typed(polarity * baseEmitter, polarity * baseCollector);

        return typed with
        {
            EmitterBranchCurrent = polarity * typed.EmitterBranchCurrent,
            CollectorBranchCurrent = polarity * typed.CollectorBranchCurrent,
            TransportCurrent = polarity * typed.TransportCurrent,
        };
    }

    public static double Limit(bool pType, double newVoltage, double oldVoltage)
    {
        var polarity = Polarity(pType);

        return polarity * DiodeModel.Limit(
            polarity * newVoltage, polarity * oldVoltage, out _);
    }

    private static double Polarity(bool pType) => pType ? -1 : 1;

    private static BipolarBias Typed(double baseEmitter, double baseCollector)
    {
        var thermal = DiodeModel.EmissionVoltage;
        var emitterJunction = SaturationCurrent * (DiodeModel.Exponential(baseEmitter) - 1);
        var collectorJunction = SaturationCurrent * (DiodeModel.Exponential(baseCollector) - 1);
        var forward = SaturationCurrent / thermal * DiodeModel.Exponential(baseEmitter);
        var reverse = SaturationCurrent / thermal * DiodeModel.Exponential(baseCollector);

        var emitterBranch =
            (emitterJunction / ForwardBeta) + (DiodeModel.JunctionGmin * baseEmitter);

        var collectorBranch =
            (collectorJunction / ReverseBeta) + (DiodeModel.JunctionGmin * baseCollector);

        return new BipolarBias(
            Region(baseEmitter, baseCollector),
            emitterBranch,
            collectorBranch,
            emitterJunction - collectorJunction,
            (forward / ForwardBeta) + DiodeModel.JunctionGmin,
            (reverse / ReverseBeta) + DiodeModel.JunctionGmin,
            forward,
            reverse);
    }

    private static DeviceRegion Region(double baseEmitter, double baseCollector) =>
        (IsConducting(baseEmitter), IsConducting(baseCollector)) switch
        {
            (true, false) => DeviceRegion.ForwardActive,
            (true, true) => DeviceRegion.Saturation,
            (false, true) => DeviceRegion.ReverseActive,
            _ => DeviceRegion.Cutoff,
        };

    private static bool IsConducting(double junctionVoltage) =>
        junctionVoltage > JunctionOnVoltage;
}
