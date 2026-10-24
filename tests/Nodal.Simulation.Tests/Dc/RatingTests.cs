using Nodal.Circuit.Schematics;
using Nodal.Simulation.Diagnostics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class RatingTests
{
    private static Schematic AcrossTheSupply(string definitionId, double supply) =>
        Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(definitionId, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 2)
            .Wire(0, -2, 6, -2)
            .Set("V1", "voltage", supply);

    private static Schematic InductorToGround() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Inductor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("V1", "voltage", 10)
        .Set("R1", "resistance", 100);

    [Fact]
    public void AResistorAboveItsPowerRatingWarns()
    {
        var schematic = AcrossTheSupply(Circuits.Resistor, 10)
            .Set("R1", "resistance", 100)
            .Set("R1", "power-rating", 0.25);

        var solution = schematic.Solve();

        Assert.True(solution.IsSolved);
        Assert.Equal(
            "R1 dissipates 1 W, above its 250 mW power rating.",
            solution.Diagnostics.Single().Message);
        Assert.True(solution.HasRatingWarning(schematic.IdOf("R1")));
    }

    [Fact]
    public void ACapacitorAboveItsVoltageRatingWarns()
    {
        var schematic = AcrossTheSupply(Circuits.Capacitor, 12)
            .Set("C1", "voltage-rating", 10);

        var solution = schematic.Solve();

        Assert.Equal(
            "C1 holds 12 V, above its 10 V voltage rating.",
            solution.Diagnostics.Single().Message);
        Assert.Equal(
            SimulationDiagnosticCode.RatingExceeded, solution.Diagnostics.Single().Code);
    }

    [Fact]
    public void AnInductorAboveItsCurrentRatingWarns()
    {
        var solution = InductorToGround().Set("L1", "current-rating", 0.05).Solve();

        Assert.Equal(
            "L1 carries 100 mA, above its 50 mA current rating.",
            solution.Diagnostics.Single().Message);
    }

    [Fact]
    public void ARatingThatIsNotExceededStaysSilent()
    {
        var solution = AcrossTheSupply(Circuits.Resistor, 10)
            .Set("R1", "resistance", 100)
            .Set("R1", "power-rating", 2)
            .Solve();

        Assert.Empty(solution.Diagnostics);
    }

    [Fact]
    public void AnUnsetRatingIsNeverChecked()
    {
        var solution = AcrossTheSupply(Circuits.Resistor, 10)
            .Set("R1", "resistance", 100)
            .Solve();

        Assert.Empty(solution.Diagnostics);
        Assert.False(solution.HasRatingWarning(solution.Of("R1").Id));
    }

    [Fact]
    public void ARatingWarningLeavesTheSolutionUsable()
    {
        var schematic = AcrossTheSupply(Circuits.Resistor, 10)
            .Set("R1", "resistance", 100)
            .Set("R1", "power-rating", 0.25);

        var solution = schematic.Solve();

        Assert.True(solution.IsSolved);
        Assert.Empty(solution.Errors);
        Approximately.Equal(0.1, solution.Amperes("R1"));
    }
}
