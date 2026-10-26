using Nodal.Circuit.Schematics;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class StepResponseTests
{
    private const double Supply = 5;
    private const double Resistance = 1000;
    private const double Capacitance = 100e-9;
    private const double Inductance = 1e-3;
    private const double RelativeTolerance = 1e-4;

    private static Schematic RcCircuit() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Capacitor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("V1", "voltage", Supply)
        .Set("R1", "resistance", Resistance)
        .Set("C1", "capacitance", Capacitance);

    private static Schematic RlCircuit() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Inductor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Resistor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("V1", "voltage", Supply)
        .Set("L1", "inductance", Inductance)
        .Set("R1", "resistance", Resistance);

    private static TransientOptions FineOptions(double stopTime, double timeConstant) =>
        new(stopTime) { StartFromZero = true, MaxStep = timeConstant / 1000 };

    [Theory]
    [InlineData(0.5)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void TheCapacitorFollowsTheAnalyticalRcStepResponse(double constants)
    {
        var timeConstant = Resistance * Capacitance;
        var stopTime = 5 * timeConstant;
        var result = RcCircuit().Transient(FineOptions(stopTime, timeConstant));
        var time = constants * timeConstant;
        var expected = Supply * (1 - Math.Exp(-constants));

        Assert.True(result.IsSolved);
        Approximately.Equal(expected, result.VoltsAt("C1", time), RelativeTolerance);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void TheInductorFollowsTheAnalyticalRlStepResponse(double constants)
    {
        var timeConstant = Inductance / Resistance;
        var stopTime = 5 * timeConstant;
        var result = RlCircuit().Transient(FineOptions(stopTime, timeConstant));
        var time = constants * timeConstant;
        var expected = Supply / Resistance * (1 - Math.Exp(-constants));

        Assert.True(result.IsSolved);
        Approximately.Equal(expected, result.AmperesAt("L1", time), RelativeTolerance);
    }

    [Fact]
    public void TheRcChargeStartsAtZeroAndEndsAtTheSupply()
    {
        var timeConstant = Resistance * Capacitance;
        var result = RcCircuit().Transient(FineOptions(20 * timeConstant, timeConstant));

        Assert.True(result.IsSolved);
        Approximately.Zero(result.VoltsAt("C1", 0), 1e-12);
        Approximately.Equal(Supply, result.VoltsAt("C1", result.FinalTime), 1e-6);
    }

    [Fact]
    public void AnUnchargedCapacitorTakesTheWholeSupplyAcrossTheResistorAtTheStart()
    {
        var timeConstant = Resistance * Capacitance;
        var result = RcCircuit().Transient(FineOptions(5 * timeConstant, timeConstant));

        Assert.True(result.IsSolved);
        Approximately.Equal(Supply / Resistance, result.AmperesAt("R1", 0), 1e-9);
    }

    [Fact]
    public void StartingFromTheOperatingPointLeavesTheCapacitorCharged()
    {
        var timeConstant = Resistance * Capacitance;
        var result = RcCircuit().Transient(new TransientOptions(5 * timeConstant));

        Assert.True(result.IsSolved);
        Approximately.Equal(Supply, result.VoltsAt("C1", 0), 1e-9);
        Approximately.Equal(Supply, result.VoltsAt("C1", result.FinalTime), 1e-9);
        Approximately.Zero(result.AmperesAt("R1", result.FinalTime), 1e-12);
    }
}
