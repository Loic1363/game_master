using Nodal.Simulation.Dc;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class BipolarModelTests
{
    private const double JacobianStep = 1e-7;
    private const double JacobianTolerance = 1e-4;
    private const double ConductanceFloor = 1e-9;

    public static TheoryData<double, double> Biases =>
        new()
        {
            { 0, 0 },
            { 0.6, -4.4 },
            { 0.65, -4.35 },
            { 0.7, 0.5 },
            { 0.75, 0.6 },
            { -1, -1 },
            { -5, 0.6 },
            { 0.5, -0.5 },
            { 0.2, 0.2 },
        };

    [Theory]
    [InlineData(0, 0, DeviceRegion.Cutoff)]
    [InlineData(-1, -1, DeviceRegion.Cutoff)]
    [InlineData(0.65, -4.35, DeviceRegion.ForwardActive)]
    [InlineData(0.75, 0.6, DeviceRegion.Saturation)]
    [InlineData(-5, 0.6, DeviceRegion.ReverseActive)]
    public void TheRegionFollowsTheJunctionBiases(
        double baseEmitter, double baseCollector, DeviceRegion expected)
    {
        Assert.Equal(
            expected, BipolarModel.Evaluate(false, baseEmitter, baseCollector).Region);

        Assert.Equal(
            expected, BipolarModel.Evaluate(true, -baseEmitter, -baseCollector).Region);
    }

    [Fact]
    public void TheForwardActiveCurrentsMatchTheClosedForm()
    {
        var bias = BipolarModel.Evaluate(false, 0.65, -4.35);
        var thermal = DiodeModel.EmissionVoltage;
        var emitter = BipolarModel.SaturationCurrent * (Math.Exp(0.65 / thermal) - 1);
        var collector = BipolarModel.SaturationCurrent * (Math.Exp(-4.35 / thermal) - 1);

        var collectorBranch = (collector / BipolarModel.ReverseBeta)
            + (DiodeModel.JunctionGmin * -4.35);

        Approximately.Equal(emitter - collector - collectorBranch, bias.CollectorCurrent);

        Approximately.Equal(
            (emitter / BipolarModel.ForwardBeta)
            + (DiodeModel.JunctionGmin * 0.65)
            + collectorBranch,
            bias.BaseCurrent);
    }

    [Fact]
    public void TheNgspiceForwardActiveBiasIsReproduced()
    {
        var bias = BipolarModel.Evaluate(false, 0.65, -4.35);

        Approximately.Equal(8.204763689363e-04, bias.CollectorCurrent, 1e-11);
        Approximately.Equal(8.204759935663e-06, bias.BaseCurrent, 1e-11);
        Approximately.Equal(3.172159277949e-02, bias.ForwardTransconductance, 1e-11);
        Approximately.Equal(3.172159287949e-04, bias.EmitterJunctionConductance, 1e-11);
        Approximately.Equal(1.000000001949e-12, bias.CollectorJunctionConductance, 1e-8);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-1.35)]
    [InlineData(-4.35)]
    public void AReverseBiasedJunctionConductsOnlyTheJunctionGmin(double baseCollector)
    {
        var bias = BipolarModel.Evaluate(false, 0.65, baseCollector);

        Approximately.Equal(
            DiodeModel.JunctionGmin, bias.CollectorJunctionConductance, 1e-2);
    }

    [Fact]
    public void TheEffectiveBetaIsTheForwardBetaInForwardActive()
    {
        var bias = BipolarModel.Evaluate(false, 0.65, -4.35);

        Approximately.Equal(
            BipolarModel.ForwardBeta, bias.CollectorCurrent / bias.BaseCurrent, 1e-5);
    }

    [Fact]
    public void SaturationCollapsesTheEffectiveBeta()
    {
        var bias = BipolarModel.Evaluate(false, 0.75, 0.6);

        Assert.Equal(DeviceRegion.Saturation, bias.Region);
        Assert.True(
            bias.CollectorCurrent / bias.BaseCurrent < BipolarModel.ForwardBeta,
            "A saturated transistor must have less current gain than its forward beta.");
    }

    [Fact]
    public void ReverseActiveUsesTheReverseBeta()
    {
        var bias = BipolarModel.Evaluate(false, -5, 0.6);

        Assert.Equal(DeviceRegion.ReverseActive, bias.Region);
        Approximately.Equal(
            BipolarModel.ReverseBeta, bias.EmitterCurrent / bias.BaseCurrent, 1e-6);
    }

    [Fact]
    public void CutoffLeavesOnlyTheLeakageAndTheJunctionGmin()
    {
        var bias = BipolarModel.Evaluate(false, -1, -1);

        Assert.Equal(DeviceRegion.Cutoff, bias.Region);
        Approximately.Equal(DiodeModel.JunctionGmin, bias.EmitterJunctionConductance, 1e-6);
        Approximately.Equal(DiodeModel.JunctionGmin, bias.CollectorJunctionConductance, 1e-6);
        Assert.InRange(Math.Abs(bias.CollectorCurrent), 0, 1e-11);
        Assert.InRange(Math.Abs(bias.BaseCurrent), 0, 1e-11);
    }

    [Theory]
    [MemberData(nameof(Biases))]
    public void TheEmitterCarriesWhatTheCollectorAndTheBaseDoNot(
        double baseEmitter, double baseCollector)
    {
        var bias = BipolarModel.Evaluate(false, baseEmitter, baseCollector);
        var emitterJunction =
            BipolarModel.SaturationCurrent * (DiodeModel.Exponential(baseEmitter) - 1);

        var collectorJunction =
            BipolarModel.SaturationCurrent * (DiodeModel.Exponential(baseCollector) - 1);

        var emitterBranch = (emitterJunction / BipolarModel.ForwardBeta)
            + (DiodeModel.JunctionGmin * baseEmitter);

        Approximately.Equal(
            collectorJunction - emitterJunction - emitterBranch, bias.EmitterCurrent, 1e-8);

        Approximately.Zero(
            bias.CollectorCurrent + bias.BaseCurrent + bias.EmitterCurrent, 1e-18);
    }

    [Theory]
    [MemberData(nameof(Biases))]
    public void APnpMirrorsAnNpn(double baseEmitter, double baseCollector)
    {
        var npn = BipolarModel.Evaluate(false, baseEmitter, baseCollector);
        var pnp = BipolarModel.Evaluate(true, -baseEmitter, -baseCollector);

        Approximately.Equal(-npn.CollectorCurrent, pnp.CollectorCurrent);
        Approximately.Equal(-npn.BaseCurrent, pnp.BaseCurrent);
        Approximately.Equal(
            npn.EmitterJunctionConductance, pnp.EmitterJunctionConductance);

        Approximately.Equal(
            npn.CollectorJunctionConductance, pnp.CollectorJunctionConductance);

        Approximately.Equal(npn.ForwardTransconductance, pnp.ForwardTransconductance);
        Approximately.Equal(npn.ReverseTransconductance, pnp.ReverseTransconductance);
    }

    [Theory]
    [MemberData(nameof(Biases))]
    public void TheNpnJacobianMatchesFiniteDifferences(
        double baseEmitter, double baseCollector) =>
        JacobianAgrees(false, baseEmitter, baseCollector);

    [Theory]
    [MemberData(nameof(Biases))]
    public void ThePnpJacobianMatchesFiniteDifferences(
        double baseEmitter, double baseCollector) =>
        JacobianAgrees(true, -baseEmitter, -baseCollector);

    [Fact]
    public void LimitingOfAPnpMirrorsAnNpn()
    {
        Approximately.Equal(
            -BipolarModel.Limit(false, 10, 0.6), BipolarModel.Limit(true, -10, -0.6));

        Assert.Equal(
            DiodeModel.Limit(10, 0.6, out _), BipolarModel.Limit(false, 10, 0.6));
    }

    private static void JacobianAgrees(bool pType, double baseEmitter, double baseCollector)
    {
        var bias = BipolarModel.Evaluate(pType, baseEmitter, baseCollector);

        var baseOverEmitter = Derivative(
            step => BipolarModel.Evaluate(
                pType, baseEmitter + step, baseCollector).BaseCurrent);

        var baseOverCollector = Derivative(
            step => BipolarModel.Evaluate(
                pType, baseEmitter, baseCollector + step).BaseCurrent);

        var collectorOverEmitter = Derivative(
            step => BipolarModel.Evaluate(
                pType, baseEmitter + step, baseCollector).CollectorCurrent);

        var collectorOverCollector = Derivative(
            step => BipolarModel.Evaluate(
                pType, baseEmitter, baseCollector + step).CollectorCurrent);

        Difference(bias.EmitterJunctionConductance, baseOverEmitter, "base over vbe");
        Difference(bias.CollectorJunctionConductance, baseOverCollector, "base over vbc");
        Difference(bias.ForwardTransconductance, collectorOverEmitter, "collector over vbe");

        Difference(
            -bias.ReverseTransconductance - bias.CollectorJunctionConductance,
            collectorOverCollector,
            "collector over vbc");
    }

    private static double Derivative(Func<double, double> current) =>
        (current(JacobianStep) - current(-JacobianStep)) / (2 * JacobianStep);

    private static void Difference(double analytic, double numeric, string what)
    {
        var allowed = Math.Max(
            ConductanceFloor,
            JacobianTolerance * Math.Max(Math.Abs(analytic), Math.Abs(numeric)));

        Assert.True(
            Math.Abs(analytic - numeric) <= allowed,
            $"The {what} derivative is {analytic:R} but finite differences give {numeric:R}, "
            + $"allowed difference {allowed:R}.");
    }
}
