using Nodal.Circuit.Schematics;
using Nodal.Simulation.Dc;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class PowerBalanceTests
{
    public static TheoryData<string> BalancedCircuits =>
        new("divider", "bridge", "thevenin", "current source", "inductor and capacitor");

    [Theory]
    [MemberData(nameof(BalancedCircuits))]
    public void TotalAbsorbedPowerEqualsTotalDeliveredPower(string name)
    {
        var solution = Circuit(name).Solve();

        Assert.True(solution.IsSolved);

        var absorbed = Sum(solution, power => power > 0);
        var delivered = -Sum(solution, power => power < 0);

        Assert.True(absorbed > 0);
        Approximately.Equal(absorbed, delivered);
    }

    private static double Sum(OperatingPoint solution, Func<double, bool> keep) =>
        solution.Components.Values
            .Select(component => component.Power.Value)
            .Where(keep)
            .Sum();

    private static Schematic Circuit(string name) => name switch
    {
        "divider" => Ladder(1000, 2200),
        "bridge" => Bridge(),
        "thevenin" => Ladder(100, 220).Set("V1", "series-resistance", 330),
        "current source" => CurrentDriven(),
        _ => Reactive(),
    };

    private static Schematic Ladder(double upper, double lower) => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Resistor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("V1", "voltage", 10)
        .Set("R1", "resistance", upper)
        .Set("R2", "resistance", lower);

    private static Schematic Bridge() => Ladder(1000, 2200)
        .Add(Circuits.Resistor, 12, 0, Circuits.FirstPinUp)
        .Add(Circuits.Resistor, 12, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 12, 8)
        .Add(Circuits.Resistor, 9, 4)
        .Wire(6, -2, 12, -2)
        .Wire(12, 2, 12, 4)
        .Wire(6, 4, 7, 4)
        .Wire(11, 4, 12, 4)
        .Set("R3", "resistance", 470)
        .Set("R4", "resistance", 680)
        .Set("R5", "resistance", 1500);

    private static Schematic CurrentDriven() => Schematic.Empty
        .Add(Circuits.CurrentSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 2)
        .Wire(0, -2, 6, -2)
        .Set("I1", "current", 0.002)
        .Set("I1", "parallel-resistance", 4700)
        .Set("R1", "resistance", 330);

    private static Schematic Reactive() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Inductor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Resistor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Capacitor, 12, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Add(Circuits.Ground, 12, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Wire(6, 4, 12, 4)
        .Set("V1", "voltage", 10)
        .Set("L1", "series-resistance", 47)
        .Set("R1", "resistance", 220)
        .Set("C1", "parallel-resistance", 100000);
}
