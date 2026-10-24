using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class CapacitorTests
{
    private const double Supply = 10;
    private const double Series = 100;
    private const double Leakage = 1e6;

    private static Schematic Circuit() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Capacitor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("V1", "voltage", Supply)
        .Set("R1", "resistance", Series);

    [Fact]
    public void AnIdealCapacitorIsAnOpenCircuitSoNoCurrentFlows()
    {
        var schematic = Circuit();
        var solution = schematic.Solve();

        Approximately.Zero(solution.Amperes("C1"), 1e-15);
        Approximately.Zero(solution.Amperes("R1"), 1e-15);
        Approximately.Zero(solution.Watts("C1"), 1e-15);
        Approximately.Equal(Supply, solution.Volts(schematic.NetAt("C1", "1")));
        Approximately.Equal(Supply, solution.Of("C1").Voltage.Value);
    }

    [Fact]
    public void AnOpenCapacitorLeavesNoVoltageAcrossTheSeriesResistor()
    {
        var solution = Circuit().Solve();

        Approximately.Zero(solution.Of("R1").Voltage.Value, 1e-12);
        Assert.Empty(solution.Diagnostics);
    }

    [Fact]
    public void AParallelResistanceLeaksCurrentThroughTheDivider()
    {
        var schematic = Circuit().Set("C1", "parallel-resistance", Leakage);
        var solution = schematic.Solve();
        var current = Supply / (Series + Leakage);

        Approximately.Equal(current, solution.Amperes("C1"));
        Approximately.Equal(current, solution.Amperes("R1"));
        Approximately.Equal(Leakage * current, solution.Of("C1").Voltage.Value);
        Approximately.Equal(
            Leakage * current, solution.Volts(schematic.NetAt("C1", "1")));
    }

    [Fact]
    public void AZeroParallelResistanceShortsTheCapacitor()
    {
        var solution = Circuit().Set("C1", "parallel-resistance", 0).Solve();

        Approximately.Zero(solution.Of("C1").Voltage.Value, 1e-12);
        Approximately.Equal(Supply / Series, solution.Amperes("C1"));
    }
}
