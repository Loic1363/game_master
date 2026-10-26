namespace Nodal.Circuit.Schematics;

public sealed record TransientSettings(
    double StopTime,
    double? OutputStep = null,
    double? MaxStep = null,
    bool StartFromZero = false)
{
    public bool IsValid =>
        IsPositive(StopTime) && IsPositiveOrUnset(OutputStep) && IsPositiveOrUnset(MaxStep);

    private static bool IsPositive(double value) => double.IsFinite(value) && value > 0;

    private static bool IsPositiveOrUnset(double? value) => value is null || IsPositive(value.Value);
}
