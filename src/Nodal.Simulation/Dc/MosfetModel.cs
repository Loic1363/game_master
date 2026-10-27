namespace Nodal.Simulation.Dc;

public static class MosfetModel
{
    public const double Threshold = 1;

    public const double TransconductanceParameter = 1e-3;

    public const double ChannelLengthModulation = 0.02;

    public const double WidthOverLength = 1;

    public static double ThresholdOf(bool pChannel) => Polarity(pChannel) * Threshold;

    public static MosfetBias Evaluate(bool pChannel, double gateSource, double drainSource)
    {
        var polarity = Polarity(pChannel);
        var typed = Typed(polarity * gateSource, polarity * drainSource);

        return typed with { ChannelCurrent = polarity * typed.ChannelCurrent };
    }

    public static double BodyDiodeVoltage(bool pChannel, double drainSource) =>
        BodyDiodeOrientation(pChannel) * drainSource;

    public static double BodyCurrentAtDrain(bool pChannel, double bodyDiodeVoltage) =>
        BodyDiodeOrientation(pChannel) * DiodeModel.Current(bodyDiodeVoltage);

    public static double DrainCurrent(
        bool pChannel, double gateSource, double drainSource, double bodyDiodeVoltage) =>
        Evaluate(pChannel, gateSource, drainSource).ChannelCurrent
        + BodyCurrentAtDrain(pChannel, bodyDiodeVoltage);

    public static double LimitGate(bool pChannel, double newVoltage, double oldVoltage)
    {
        var polarity = Polarity(pChannel);

        return polarity * FetLimiting.Gate(
            polarity * newVoltage, polarity * oldVoltage, Threshold);
    }

    public static double LimitDrainSource(bool pChannel, double newVoltage, double oldVoltage)
    {
        var polarity = Polarity(pChannel);
        var typedNew = polarity * newVoltage;
        var typedOld = polarity * oldVoltage;
        var orientation = typedNew < 0 ? -1 : 1;

        return polarity * orientation
            * FetLimiting.DrainSource(orientation * typedNew, orientation * typedOld);
    }

    private static double Polarity(bool pChannel) => pChannel ? -1 : 1;

    private static double BodyDiodeOrientation(bool pChannel) => pChannel ? 1 : -1;

    private static MosfetBias Typed(double gateSource, double drainSource) =>
        drainSource < 0
            ? Reversed(Forward(gateSource - drainSource, -drainSource))
            : Forward(gateSource, drainSource);

    private static MosfetBias Forward(double gateSource, double drainSource)
    {
        var overdrive = gateSource - Threshold;

        if (overdrive <= 0)
        {
            return new MosfetBias(DeviceRegion.Cutoff, 0, 0, 0);
        }

        var beta = TransconductanceParameter * WidthOverLength;
        var modulation = 1 + (ChannelLengthModulation * drainSource);
        var modulated = beta * modulation;

        if (drainSource >= overdrive)
        {
            var square = overdrive * overdrive / 2;

            return new MosfetBias(
                DeviceRegion.Saturation,
                modulated * square,
                modulated * overdrive,
                beta * ChannelLengthModulation * square);
        }

        var shape = drainSource * (overdrive - (drainSource / 2));

        return new MosfetBias(
            DeviceRegion.Linear,
            modulated * shape,
            modulated * drainSource,
            (modulated * (overdrive - drainSource))
            + (beta * ChannelLengthModulation * shape));
    }

    private static MosfetBias Reversed(MosfetBias swapped) => swapped with
    {
        ChannelCurrent = -swapped.ChannelCurrent,
        Transconductance = -swapped.Transconductance,
        OutputConductance = swapped.Transconductance + swapped.OutputConductance,
    };
}
