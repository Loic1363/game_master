namespace Nodal.Simulation.Dc;

public static class DiodeModel
{
    public const double SaturationCurrent = 1e-14;

    public const double EmissionCoefficient = 1;

    public const double Temperature = 300.15;

    public const double BoltzmannConstant = 1.38064852e-23;

    public const double ElementaryCharge = 1.6021766208e-19;

    public const double JunctionGmin = 1e-12;

    private const double MaximumExponent = 700;

    public static double ThermalVoltage { get; } =
        BoltzmannConstant * Temperature / ElementaryCharge;

    public static double EmissionVoltage { get; } = EmissionCoefficient * ThermalVoltage;

    public static double CriticalVoltage { get; } = EmissionVoltage * Math.Log(
        EmissionVoltage / (Math.Sqrt(2) * SaturationCurrent));

    public static double Current(double voltage) =>
        (SaturationCurrent * (Exponential(voltage) - 1)) + (JunctionGmin * voltage);

    public static double Conductance(double voltage) =>
        (SaturationCurrent / EmissionVoltage * Exponential(voltage)) + JunctionGmin;

    public static double EquivalentCurrent(double voltage) =>
        Current(voltage) - (Conductance(voltage) * voltage);

    public static double Limit(double newVoltage, double oldVoltage, out bool limited) =>
        JunctionLimiting.Limit(
            newVoltage, oldVoltage, EmissionVoltage, CriticalVoltage, out limited);

    private static double Exponential(double voltage) =>
        Math.Exp(Math.Min(voltage / EmissionVoltage, MaximumExponent));
}
