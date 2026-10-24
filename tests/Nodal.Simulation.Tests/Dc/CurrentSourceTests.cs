using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class CurrentSourceTests
{
    private const double Drive = 1e-3;
    private const double Load = 100;

    private static Schematic Circuit() => Schematic.Empty
        .Add(Circuits.CurrentSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 2)
        .Wire(0, -2, 6, -2)
        .Set("I1", "current", Drive)
        .Set("R1", "resistance", Load);

    [Fact]
    public void OneMilliampereIntoOneHundredOhmsGivesOneHundredMillivolts()
    {
        var schematic = Circuit();
        var solution = schematic.Solve();

        Approximately.Equal(Drive * Load, solution.Volts(schematic.NetAt("R1", "1")));
    }

    [Fact]
    public void TheResistorCurrentIsPositiveAndTheSourceCurrentIsNegative()
    {
        var solution = Circuit().Solve();

        Approximately.Equal(Drive, solution.Amperes("R1"));
        Approximately.Equal(-Drive, solution.Amperes("I1"));
    }

    [Fact]
    public void TheSourceDeliversWhatTheResistorAbsorbs()
    {
        var solution = Circuit().Solve();

        Approximately.Equal(Drive * Drive * Load, solution.Watts("R1"));
        Approximately.Equal(-Drive * Drive * Load, solution.Watts("I1"));
        Assert.False(solution.Of("I1").Absorbs);
        Assert.True(solution.Of("R1").Absorbs);
    }

    [Fact]
    public void AParallelResistanceTakesItsShareOfTheDrive()
    {
        var internalResistance = 400.0;
        var schematic = Circuit().Set("I1", "parallel-resistance", internalResistance);
        var solution = schematic.Solve();
        var expected = Drive * internalResistance * Load / (internalResistance + Load);

        Approximately.Equal(expected, solution.Volts(schematic.NetAt("R1", "1")));
        Approximately.Equal(expected / Load, solution.Amperes("R1"));
        Approximately.Equal(-expected / Load, solution.Amperes("I1"));
    }
}
