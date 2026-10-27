using Nodal.Simulation.Dc;
using Nodal.Simulation.Diagnostics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class TransistorTests
{
    private const double RailMargin = 1e-3;

    [Theory]
    [InlineData(Circuits.Nmos, "M1")]
    [InlineData(Circuits.Pmos, "M1")]
    [InlineData(Circuits.Npn, "Q1")]
    [InlineData(Circuits.Pnp, "Q1")]
    public void EveryTransistorIsSupported(string definitionId, string designator)
    {
        var solution = TransistorCircuits.BiasBox(definitionId).Solve();

        Assert.True(solution.IsSolved, Listed(solution));
        Assert.DoesNotContain(
            SimulationDiagnosticCode.UnsupportedComponent,
            solution.Diagnostics.Select(diagnostic => diagnostic.Code));

        Assert.NotNull(solution.Of(designator));
    }

    [Fact]
    public void ACrossCoupledPairOfInvertersConvergesToOneOfItsStates()
    {
        var schematic = TransistorCircuits.CrossCoupledInverters();
        var first = schematic.NetAt("M1", "drain");
        var second = schematic.NetAt("M3", "drain");
        var solution = schematic.Solve();

        Assert.True(solution.IsSolved, Listed(solution));
        Assert.Empty(solution.Errors);

        var left = solution.Volts(first);
        var right = solution.Volts(second);
        var state = $"the outputs settled at {left:R} V and {right:R} V";

        Assert.True(IsLatchedState(left, right) || IsMetastableState(left, right), state);
    }

    [Fact]
    public void AnNmosInSaturationCarriesTheClosedFormDrainCurrent()
    {
        var solution = TransistorCircuits.BiasBox(Circuits.Nmos, 5, 3).Solve();
        var expected = MosfetModel.DrainCurrent(false, 3, 5, -5);

        Assert.True(solution.IsSolved, Listed(solution));
        Approximately.Equal(expected, solution.Amperes("M1"), 1e-9);
        Approximately.Equal(2.2e-3, solution.Amperes("M1"), 1e-3);
    }

    [Fact]
    public void AnNmosBelowItsThresholdCarriesOnlyTheBodyDiodeLeakage()
    {
        var solution = TransistorCircuits.BiasBox(Circuits.Nmos, 5, 0.5).Solve();

        Assert.True(solution.IsSolved, Listed(solution));
        Approximately.Equal(
            MosfetModel.BodyCurrentAtDrain(false, -5), solution.Amperes("M1"), 1e-6);
    }

    [Fact]
    public void APmosMirrorsAnNmosOfTheSameOverdrive()
    {
        var nChannel = TransistorCircuits.BiasBox(Circuits.Nmos, 5, 3).Solve();
        var pChannel = TransistorCircuits.BiasBox(Circuits.Pmos, 5, 2).Solve();

        Assert.True(pChannel.IsSolved, Listed(pChannel));
        Approximately.Equal(
            -nChannel.Amperes("M1"), pChannel.Amperes("M1"), 1e-9);
    }

    [Fact]
    public void AnNpnInForwardActiveCarriesTheClosedFormCollectorCurrent()
    {
        var solution = TransistorCircuits.BiasBox(Circuits.Npn, 5, 0.65).Solve();
        var expected = BipolarModel.Evaluate(false, 0.65, 0.65 - 5);

        Assert.True(solution.IsSolved, Listed(solution));
        Assert.Equal(DeviceRegion.ForwardActive, expected.Region);
        Approximately.Equal(expected.CollectorCurrent, solution.Amperes("Q1"), 1e-9);
        Approximately.Equal(8.2047636e-4, solution.Amperes("Q1"), 1e-6);
    }

    [Fact]
    public void APnpMirrorsAnNpnOfTheSameJunctionBias()
    {
        var npn = TransistorCircuits.BiasBox(Circuits.Npn, 5, 0.65).Solve();
        var pnp = TransistorCircuits.BiasBox(Circuits.Pnp, 5, 5 - 0.65).Solve();

        Assert.True(pnp.IsSolved, Listed(pnp));
        Approximately.Equal(-npn.Amperes("Q1"), pnp.Amperes("Q1"), 1e-9);
    }

    [Fact]
    public void AGroundedBaseLeavesTheNpnInCutoff()
    {
        var solution = TransistorCircuits.BiasBox(Circuits.Npn, 5, 0).Solve();

        Assert.True(solution.IsSolved, Listed(solution));
        Assert.InRange(Math.Abs(solution.Amperes("Q1")), 0, 1e-11);
    }

    [Theory]
    [InlineData(Circuits.Nmos, "M1", "drain", "source", "gate")]
    [InlineData(Circuits.Npn, "Q1", "collector", "emitter", "base")]
    public void TheGateAndBaseNetsKeepTheirOwnVoltages(
        string definitionId, string designator, string first, string second, string control)
    {
        var schematic = TransistorCircuits.BiasBox(definitionId);
        var solution = schematic.Solve();

        Assert.True(solution.IsSolved, Listed(solution));
        Approximately.Equal(
            TransistorCircuits.Supply,
            solution.Volts(schematic.NetAt(designator, first)),
            1e-9);

        Assert.Equal("0", schematic.NetAt(designator, second));
        Approximately.Equal(
            TransistorCircuits.ControlFor(definitionId),
            solution.Volts(schematic.NetAt(designator, control)),
            1e-9);
    }

    private static bool IsLatchedState(double left, double right) =>
        (IsLow(left) && IsHigh(right)) || (IsHigh(left) && IsLow(right));

    private static bool IsMetastableState(double left, double right) =>
        IsMidRail(left) && IsMidRail(right);

    private static bool IsLow(double voltage) => Math.Abs(voltage) <= RailMargin;

    private static bool IsHigh(double voltage) =>
        Math.Abs(voltage - TransistorCircuits.Supply) <= RailMargin;

    private static bool IsMidRail(double voltage) =>
        Math.Abs(voltage - (TransistorCircuits.Supply / 2)) <= RailMargin;

    private static string Listed(OperatingPoint solution) =>
        string.Join("; ", solution.Diagnostics.Select(diagnostic => diagnostic.Message));
}
