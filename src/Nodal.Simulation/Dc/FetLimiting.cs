namespace Nodal.Simulation.Dc;

public static class FetLimiting
{
    private const double StepFloor = 2;
    private const double OffMargin = 0.5;
    private const double OnMargin = 0.5;
    private const double OnApproach = 2;
    private const double OnCeiling = 4;
    private const double StrongInversionMargin = 3.5;
    private const double StrongDrainSource = 3.5;
    private const double DrainSourceFloor = -0.5;
    private const double DrainSourceGrowth = 3;
    private const double DrainSourceOffset = 2;
    private const double DrainSourceCeiling = 4;

    public static double Gate(double newVoltage, double oldVoltage, double threshold)
    {
        var fast = Math.Abs(2 * (oldVoltage - threshold)) + StepFloor;
        var slow = (fast / 2) + StepFloor;
        var strong = threshold + StrongInversionMargin;

        if (oldVoltage < threshold)
        {
            return BelowThreshold(newVoltage, oldVoltage, threshold, fast, slow);
        }

        return oldVoltage >= strong
            ? StronglyOn(newVoltage, oldVoltage, threshold, strong, fast, slow)
            : NearThreshold(newVoltage, oldVoltage, threshold);
    }

    public static double DrainSource(double newVoltage, double oldVoltage)
    {
        if (oldVoltage < StrongDrainSource)
        {
            return newVoltage > StrongDrainSource
                ? Math.Min(newVoltage, DrainSourceCeiling)
                : Math.Max(newVoltage, DrainSourceFloor);
        }

        if (newVoltage > StrongDrainSource)
        {
            return Math.Min(newVoltage, (DrainSourceGrowth * oldVoltage) + DrainSourceOffset);
        }

        return newVoltage < StrongDrainSource
            ? Math.Max(newVoltage, DrainSourceOffset)
            : newVoltage;
    }

    private static double StronglyOn(
        double newVoltage,
        double oldVoltage,
        double threshold,
        double strong,
        double fast,
        double slow)
    {
        if (newVoltage > oldVoltage)
        {
            return newVoltage - oldVoltage >= fast ? oldVoltage + fast : newVoltage;
        }

        if (newVoltage < strong)
        {
            return Math.Max(newVoltage, threshold + OnApproach);
        }

        return oldVoltage - newVoltage > slow ? oldVoltage - slow : newVoltage;
    }

    private static double NearThreshold(
        double newVoltage, double oldVoltage, double threshold) =>
        newVoltage > oldVoltage
            ? Math.Min(newVoltage, threshold + OnCeiling)
            : Math.Max(newVoltage, threshold - OnMargin);

    private static double BelowThreshold(
        double newVoltage, double oldVoltage, double threshold, double fast, double slow)
    {
        if (newVoltage <= oldVoltage)
        {
            return oldVoltage - newVoltage > fast ? oldVoltage - fast : newVoltage;
        }

        var reachable = threshold + OffMargin;

        if (newVoltage > reachable)
        {
            return reachable;
        }

        return newVoltage - oldVoltage > slow ? oldVoltage + slow : newVoltage;
    }
}
