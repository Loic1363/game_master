using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class WheatstoneBridgeTests
{
    private const double Supply = 10;
    private const double LeftTop = 100;
    private const double RightTop = 220;
    private const double LeftBottom = 330;
    private const double RightBottom = 470;
    private const double Bridge = 1000;

    private static double Left => Solved().Left;

    private static double Right => Solved().Right;

    private static Schematic Circuit() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Resistor, 12, 0, Circuits.FirstPinUp)
        .Add(Circuits.Resistor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Resistor, 12, 6, Circuits.FirstPinUp)
        .Add(Circuits.Resistor, 9, 4)
        .Add(Circuits.Ground, 6, 8)
        .Add(Circuits.Ground, 12, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, -2, 12, -2)
        .Wire(6, 2, 6, 4)
        .Wire(12, 2, 12, 4)
        .Wire(6, 4, 7, 4)
        .Wire(11, 4, 12, 4)
        .Set("V1", "voltage", Supply)
        .Set("R1", "resistance", LeftTop)
        .Set("R2", "resistance", RightTop)
        .Set("R3", "resistance", LeftBottom)
        .Set("R4", "resistance", RightBottom)
        .Set("R5", "resistance", Bridge);

    private static (double Left, double Right) Solved()
    {
        var leftDiagonal = (1 / LeftTop) + (1 / LeftBottom) + (1 / Bridge);
        var rightDiagonal = (1 / RightTop) + (1 / RightBottom) + (1 / Bridge);
        var coupling = -1 / Bridge;
        var leftInjection = Supply / LeftTop;
        var rightInjection = Supply / RightTop;
        var determinant = (leftDiagonal * rightDiagonal) - (coupling * coupling);

        return (((leftInjection * rightDiagonal) - (coupling * rightInjection)) / determinant,
            ((leftDiagonal * rightInjection) - (coupling * leftInjection)) / determinant);
    }

    [Fact]
    public void TheTwoBridgeNodesTakeTheirHandCalculatedVoltages()
    {
        var schematic = Circuit();
        var solution = schematic.Solve();

        Assert.True(solution.IsSolved);
        Approximately.Equal(Left, solution.Volts(schematic.NetAt("R1", "2")));
        Approximately.Equal(Right, solution.Volts(schematic.NetAt("R2", "2")));
    }

    [Fact]
    public void TheUnbalancedBridgeCarriesANonZeroCurrent()
    {
        var current = Circuit().Solve().Amperes("R5");

        Approximately.Equal((Left - Right) / Bridge, current);
        Assert.True(Math.Abs(current) > 1e-6);
    }

    [Fact]
    public void TheCurrentsEnteringEachBridgeNodeSumToZero()
    {
        var solution = Circuit().Solve();

        Approximately.Equal(
            solution.Amperes("R1"), solution.Amperes("R3") + solution.Amperes("R5"));
        Approximately.Equal(
            solution.Amperes("R2") + solution.Amperes("R5"), solution.Amperes("R4"));
    }
}
