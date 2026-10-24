using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class TheveninTests
{
    private const double Supply = 10;
    private const double Series = 100;
    private const double Load = 220;
    private const double Current = Supply / (Series + Load);
    private const double Terminal = Supply * Load / (Series + Load);

    private static Schematic Circuit() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 2)
        .Wire(0, -2, 6, -2)
        .Set("V1", "voltage", Supply)
        .Set("V1", "series-resistance", Series)
        .Set("R1", "resistance", Load);

    [Fact]
    public void TheTerminalVoltageDropsToSixPointEightSevenFiveVolts()
    {
        var schematic = Circuit();
        var solution = schematic.Solve();

        Approximately.Equal(Terminal, solution.Volts(schematic.NetAt("R1", "1")));
    }

    [Fact]
    public void TheLoadCarriesThirtyOnePointTwoFiveMilliampere()
    {
        var solution = Circuit().Solve();

        Approximately.Equal(Current, solution.Amperes("R1"));
        Approximately.Equal(-Current, solution.Amperes("V1"));
    }

    [Fact]
    public void TheSourceTerminalsDeliverWhatTheLoadAbsorbs()
    {
        var solution = Circuit().Solve();

        Approximately.Equal(Terminal * Current, solution.Watts("R1"));
        Approximately.Equal(-Terminal * Current, solution.Watts("V1"));
    }

    [Fact]
    public void AnIdealSourceKeepsItsFullVoltageAcrossTheLoad()
    {
        var schematic = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 2)
            .Wire(0, -2, 6, -2)
            .Set("V1", "voltage", Supply)
            .Set("R1", "resistance", Load);

        var solution = schematic.Solve();

        Approximately.Equal(Supply, solution.Volts(schematic.NetAt("R1", "1")));
        Approximately.Equal(Supply / Load, solution.Amperes("R1"));
    }
}
