namespace Nodal.Circuit.Schematics;

public sealed record TransientSettings(
    double StopTime,
    double? OutputStep = null,
    double? MaxStep = null,
    bool StartFromZero = false)
{
    public bool IsValid =>
        IsPositive(StopTime)
        && IsPositiveOrUnset(OutputStep)
        && IsPositiveOrUnset(MaxStep)
        && OutputPointsAreBounded
        && MaxStepIsLargeEnough;

    public double? OutputPoints =>
        OutputStep is { } step && IsPositive(step) && IsPositive(StopTime)
            ? StopTime / step
            : null;

    public bool OutputPointsAreBounded =>
        OutputPoints is not { } points || points <= SchematicLimits.MaxTransientPoints;

    public bool MaxStepIsLargeEnough =>
        MaxStep is not { } step
        || !IsPositive(StopTime)
        || step >= StopTime * SchematicLimits.SmallestMaxStepFraction;

    private static bool IsPositive(double value) => double.IsFinite(value) && value > 0;

    private static bool IsPositiveOrUnset(double? value) => value is null || IsPositive(value.Value);
}
