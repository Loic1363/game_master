namespace Nodal.Simulation.Dc;

public static class JunctionLimiting
{
    private const double StepsWithoutLimiting = 2;

    public static double Limit(
        double newVoltage,
        double oldVoltage,
        double thermalVoltage,
        double criticalVoltage,
        out bool limited)
    {
        limited = false;

        if (newVoltage <= criticalVoltage
            || Math.Abs(newVoltage - oldVoltage) <= StepsWithoutLimiting * thermalVoltage)
        {
            return newVoltage;
        }

        limited = true;

        if (oldVoltage <= 0)
        {
            return thermalVoltage * Math.Log(newVoltage / thermalVoltage);
        }

        var argument = 1 + ((newVoltage - oldVoltage) / thermalVoltage);

        return argument > 0
            ? oldVoltage + (thermalVoltage * Math.Log(argument))
            : criticalVoltage;
    }
}
