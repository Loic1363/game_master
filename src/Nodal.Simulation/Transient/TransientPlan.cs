namespace Nodal.Simulation.Transient;

internal sealed record TransientPlan(
    double StopTime, double OutputStep, double MaxStep, double MinStep, double MinBreak)
{
    private const double StepsPerOutputStep = 50;
    private const double OutputStepsPerRun = 1000;
    private const double FirstStepDivisor = 10;
    private const double MinStepFraction = 1e-9;
    private const double MinBreakFraction = 5e-5;

    public double FirstStep => Math.Min(MaxStep, OutputStep) / FirstStepDivisor;

    public double StepAfterBreakpoint(double distanceToNextBreakpoint) =>
        Math.Max(
            MinStep, Math.Min(MaxStep, distanceToNextBreakpoint / FirstStepDivisor));

    public static TransientPlan Resolve(TransientOptions options)
    {
        var stopTime = options.StopTime;
        var outputStep = Positive(options.OutputStep, nameof(options.OutputStep))
            ?? stopTime / OutputStepsPerRun;

        var maxStep = Positive(options.MaxStep, nameof(options.MaxStep))
            ?? Math.Min(outputStep, stopTime / StepsPerOutputStep);

        var minStep = Positive(options.MinStep, nameof(options.MinStep))
            ?? stopTime * MinStepFraction;

        return new TransientPlan(
            stopTime, outputStep, maxStep, minStep, maxStep * MinBreakFraction);
    }

    private static double? Positive(double? value, string name)
    {
        if (value is null)
        {
            return null;
        }

        if (!double.IsFinite(value.Value) || value.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                name, value, $"{name} must be a finite positive number of seconds.");
        }

        return value;
    }
}
