using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class SuperpositionTests
{
    private const double Left = 10;
    private const double Right = 5;

    private static Schematic Circuit(double left, double right) => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.VoltageSource, 24, 0)
        .Add(Circuits.Ground, 24, 2)
        .Add(Circuits.Resistor, 6, -2)
        .Add(Circuits.Resistor, 18, -2)
        .Add(Circuits.Resistor, 12, 2, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 12, 4)
        .Wire(0, -2, 4, -2)
        .Wire(8, -2, 16, -2)
        .Wire(20, -2, 24, -2)
        .Wire(12, -2, 12, 0)
        .Set("V1", "voltage", left)
        .Set("V2", "voltage", right)
        .Set("R1", "resistance", 100)
        .Set("R2", "resistance", 220)
        .Set("R3", "resistance", 330);

    [Fact]
    public void TheMiddleNodeIsTheSumOfTheTwoSingleSourceSolutions()
    {
        var schematic = Circuit(Left, Right);
        var middle = schematic.NetAt("R3", "1");

        var both = schematic.Solve().Volts(middle);
        var leftAlone = Circuit(Left, 0).Solve().Volts(middle);
        var rightAlone = Circuit(0, Right).Solve().Volts(middle);

        Approximately.Equal(leftAlone + rightAlone, both);
    }

    [Fact]
    public void EveryBranchCurrentIsTheSumOfTheTwoSingleSourceCurrents()
    {
        var both = Circuit(Left, Right).Solve();
        var leftAlone = Circuit(Left, 0).Solve();
        var rightAlone = Circuit(0, Right).Solve();

        foreach (var designator in new[] { "R1", "R2", "R3", "V1", "V2" })
        {
            Approximately.Equal(
                leftAlone.Amperes(designator) + rightAlone.Amperes(designator),
                both.Amperes(designator));
        }
    }

    [Fact]
    public void BothSourcesTogetherStillBalanceTheirPower()
    {
        var solution = Circuit(Left, Right).Solve();
        var total = solution.Components.Values.Sum(component => component.Power.Value);

        Approximately.Zero(total, 1e-9 * solution.Watts("R1"));
    }
}
