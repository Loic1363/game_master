using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class InductorTests
{
    private const double Supply = 10;
    private const double Load = 100;

    private static Schematic Circuit() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Inductor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Resistor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("V1", "voltage", Supply)
        .Set("R1", "resistance", Load);

    [Fact]
    public void AnIdealInductorIsAShortAndCarriesTheSupplyOverTheLoad()
    {
        var schematic = Circuit();
        var solution = schematic.Solve();

        Approximately.Equal(Supply / Load, solution.Amperes("L1"));
        Approximately.Equal(Supply, solution.Volts(schematic.NetAt("R1", "1")));
        Approximately.Zero(solution.Of("L1").Voltage.Value, 1e-12);
        Approximately.Zero(solution.Watts("L1"), 1e-12);
    }

    [Fact]
    public void ASeriesResistanceSharesTheSupplyWithTheLoad()
    {
        var series = 150.0;
        var schematic = Circuit().Set("L1", "series-resistance", series);
        var solution = schematic.Solve();
        var current = Supply / (series + Load);

        Approximately.Equal(current, solution.Amperes("L1"));
        Approximately.Equal(series * current, solution.Of("L1").Voltage.Value);
        Approximately.Equal(current * current * series, solution.Watts("L1"));
        Approximately.Equal(Load * current, solution.Volts(schematic.NetAt("R1", "1")));
    }

    [Fact]
    public void AZeroSeriesResistanceStaysAShort()
    {
        var solution = Circuit().Set("L1", "series-resistance", 0).Solve();

        Approximately.Equal(Supply / Load, solution.Amperes("L1"));
        Approximately.Zero(solution.Of("L1").Voltage.Value, 1e-12);
    }

    [Fact]
    public void TheInductorKeepsTheNodeBehindItConnectedToGround()
    {
        Assert.Empty(Circuit().Solve().Diagnostics);
    }
}
