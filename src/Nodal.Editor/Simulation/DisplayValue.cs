using Nodal.Foundation.Quantities;

namespace Nodal.Editor.Simulation;

public static class DisplayValue
{
    public const double VoltageFloor = 1e-9;
    public const double CurrentFloor = 1e-12;

    public static string Text(Quantity quantity) =>
        new Quantity(Floored(quantity.Value, quantity.Unit), quantity.Unit).ToString();

    public static string Text(double value, Unit unit) => Text(new Quantity(value, unit));

    public static string Magnitude(Quantity quantity) =>
        Text(Math.Abs(quantity.Value), quantity.Unit);

    private static double Floored(double value, Unit unit) =>
        FloorOf(unit) is { } floor && Math.Abs(value) < floor ? 0 : value;

    private static double? FloorOf(Unit unit) => unit switch
    {
        Unit.Volt => VoltageFloor,
        Unit.Ampere => CurrentFloor,
        _ => null,
    };
}
