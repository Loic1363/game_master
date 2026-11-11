using Nodal.Simulation.Dc;
using Nodal.Simulation.Diagnostics;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class ControlledSourceTests
{
    private const double VoltageTolerance = 1e-9;
    private const double CurrentTolerance = 1e-12;

    [Theory]
    [InlineData(10.0, 5.0)]
    [InlineData(-10.0, -5.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(1.0, 0.5)]
    public void AVoltageControlledVoltageSourceFollowsItsGain(double gain, double expected)
    {
        var solution = ControlCircuits.VcvsAmplifier(0.5, gain, 1000).Solve();
        var source = solution.Of("E1");

        Assert.True(solution.IsSolved);
        Approximately.Equal(expected, source.Voltage.Value, VoltageTolerance);
        Approximately.Equal(-expected / 1000, source.Current.Value, CurrentTolerance);
    }

    [Fact]
    public void TheControlPinsOfAVoltageControlDrawNoCurrent()
    {
        var source = ControlCircuits.VcvsAmplifier(0.5, 10, 1000).Solve().Of("E1");

        Assert.True(source.TryGetPin("control-plus", out var plus));
        Assert.True(source.TryGetPin("control-minus", out var minus));
        Approximately.Zero(plus.Current.Value, CurrentTolerance);
        Approximately.Zero(minus.Current.Value, CurrentTolerance);
    }

    [Fact]
    public void AVoltageControlledCurrentSourceDrivesItsCurrentOutOfPlus()
    {
        var solution = ControlCircuits.VccsLoad(1, 1e-3, 2000).Solve();
        var source = solution.Of("G1");

        Approximately.Equal(2, source.Voltage.Value, VoltageTolerance);
        Approximately.Equal(-1e-3, source.Current.Value, CurrentTolerance);
        Approximately.Equal(1e-3, solution.Of("R1").Current.Value, CurrentTolerance);
    }

    [Theory]
    [InlineData(1e-3, 1e-3)]
    [InlineData(-1e-3, -1e-3)]
    [InlineData(0.0, 0.0)]
    public void ATransconductanceOfEitherSignHolds(double transconductance, double expected)
    {
        var source = ControlCircuits.VccsLoad(1, transconductance, 2000).Solve().Of("G1");

        Approximately.Equal(expected * 2000, source.Voltage.Value, VoltageTolerance);
        Approximately.Equal(-expected, source.Current.Value, CurrentTolerance);
    }

    [Fact]
    public void ACurrentControlledCurrentSourceSensesItsOwnControlPins()
    {
        var solution = ControlCircuits.CccsLoad(1, 1000, 10, 100).Solve();
        var source = solution.Of("F1");

        Assert.True(source.TryGetPin("control-plus", out var control));
        Approximately.Equal(1e-3, control.Current.Value, CurrentTolerance);
        Approximately.Equal(1e-3, solution.Of("R1").Current.Value, CurrentTolerance);
        Approximately.Equal(-10e-3, source.Current.Value, CurrentTolerance);
        Approximately.Equal(1, source.Voltage.Value, VoltageTolerance);
    }

    [Fact]
    public void TheCurrentControlPortIsAnIdealAmmeter()
    {
        var source = ControlCircuits.CccsLoad(1, 1000, 10, 100).Solve().Of("F1");

        Assert.True(source.TryGetPin("control-plus", out var plus));
        Assert.True(source.TryGetPin("control-minus", out var minus));
        Approximately.Equal(plus.Voltage.Value, minus.Voltage.Value, VoltageTolerance);
        Approximately.Equal(-plus.Current.Value, minus.Current.Value, CurrentTolerance);
    }

    [Fact]
    public void ACurrentControlledVoltageSourceFollowsItsTransresistance()
    {
        var solution = ControlCircuits.CcvsLoad(1, 1000, 2000, 1000).Solve();
        var source = solution.Of("H1");

        Approximately.Equal(2, source.Voltage.Value, VoltageTolerance);
        Approximately.Equal(-2e-3, source.Current.Value, CurrentTolerance);
        Approximately.Equal(2e-3, solution.Of("R2").Current.Value, CurrentTolerance);
    }

    [Theory]
    [InlineData(2000.0, 2.0)]
    [InlineData(-2000.0, -2.0)]
    [InlineData(0.0, 0.0)]
    public void ATransresistanceOfEitherSignHolds(double transresistance, double expected)
    {
        var source = ControlCircuits.CcvsLoad(1, 1000, transresistance, 1000).Solve().Of("H1");

        Approximately.Equal(expected, source.Voltage.Value, VoltageTolerance);
    }

    [Theory]
    [InlineData(ControlCircuits.Vcvs, "E1")]
    [InlineData(ControlCircuits.Vccs, "G1")]
    [InlineData(ControlCircuits.Cccs, "F1")]
    [InlineData(ControlCircuits.Ccvs, "H1")]
    public void EveryPinCurrentOfAControlledSourceSumsToZero(string definitionId, string designator)
    {
        var source = Driven(definitionId).Of(designator);

        Assert.Equal(4, source.Pins.Length);
        Approximately.Zero(source.Pins.Sum(pin => pin.Current.Value), CurrentTolerance);
    }

    [Theory]
    [InlineData(ControlCircuits.Vcvs, "E1")]
    [InlineData(ControlCircuits.Vccs, "G1")]
    [InlineData(ControlCircuits.Cccs, "F1")]
    [InlineData(ControlCircuits.Ccvs, "H1")]
    public void AControlledSourceDeliversThePowerItsPinsCarry(
        string definitionId, string designator)
    {
        var source = Driven(definitionId).Of(designator);

        Approximately.Equal(
            source.Pins.Sum(pin => pin.Power.Value), source.Power.Value, CurrentTolerance);

        Assert.False(source.Absorbs);
    }

    [Fact]
    public void AnInvertingAmplifierBuiltFromAVcvsFollowsTheResistorRatio()
    {
        var solution = ControlCircuits.InvertingAmplifier(1e5, 1, 1000, 10000).Solve();

        Assert.True(solution.IsSolved, Listed(solution));
        Approximately.Equal(-10.0 / (1 + 11e-5), solution.Of("E1").Voltage.Value, 1e-9);
    }

    [Fact]
    public void AnInvertingAmplifierKeepsItsInvertingInputNearGround()
    {
        var solution = ControlCircuits.InvertingAmplifier(1e5, 1, 1000, 10000).Solve();

        Assert.True(solution.Of("E1").TryGetPin("control-minus", out var inverting));
        Approximately.Equal(1.0 / (1 + 11e-5) * 1e-4, inverting.Voltage.Value, 1e-12);
    }

    [Fact]
    public void AGainOfOneFeedingItsOwnControlHasNoUniqueSolution()
    {
        var solution = ControlCircuits.VcvsFeedback(1).Solve();

        Assert.False(solution.IsSolved);

        var diagnostic = Assert.Single(solution.Diagnostics);

        Assert.Equal(SimulationDiagnosticCode.Singular, diagnostic.Code);
        Assert.Equal(
            "The circuit has no unique DC solution: E1 forms a feedback loop whose gain leaves "
            + "the circuit undetermined.",
            diagnostic.Message);
    }

    [Fact]
    public void AStructuralCauseIsPreferredOverNamingAControlledSource()
    {
        var solution = ControlCircuits.ParallelControlledSources(1).Solve();

        Assert.False(solution.IsSolved);
        Assert.Contains(
            "are controlled voltage sources in parallel",
            Assert.Single(solution.Diagnostics).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ASolvableFeedbackCircuitNamesNoSource()
    {
        var solution = ControlCircuits.InvertingAmplifier(1e5, 1, 1000, 10000).Solve();

        Assert.True(solution.IsSolved);
        Assert.Empty(solution.Errors);
    }

    private static OperatingPoint Driven(string definitionId) => definitionId switch
    {
        ControlCircuits.Vcvs => ControlCircuits.VcvsAmplifier(0.5, 10, 1000).Solve(),
        ControlCircuits.Vccs => ControlCircuits.VccsLoad(1, 1e-3, 2000).Solve(),
        ControlCircuits.Cccs => ControlCircuits.CccsLoad(1, 1000, 10, 100).Solve(),
        _ => ControlCircuits.CcvsLoad(1, 1000, 2000, 1000).Solve(),
    };

    private static string Listed(OperatingPoint solution) =>
        string.Join("; ", solution.Diagnostics.Select(diagnostic => diagnostic.Message));
}
