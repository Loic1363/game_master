using Nodal.Simulation.Dc;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class MosfetModelTests
{
    private const double JacobianStep = 1e-7;
    private const double JacobianTolerance = 1e-5;
    private const double ConductanceFloor = 1e-9;

    public static TheoryData<double, double> Biases =>
        new()
        {
            { 0, 0 },
            { 0.5, 2 },
            { 1, 5 },
            { 2, 5 },
            { 3, 5 },
            { 5, 1 },
            { 5, 0.5 },
            { 5, 10 },
            { 3, -5 },
            { 5, -0.4 },
            { 0.5, -3 },
            { 2, 0 },
        };

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    public void BelowTheThresholdTheChannelIsCutOff(double gateSource)
    {
        var bias = MosfetModel.Evaluate(false, gateSource, 5);

        Assert.Equal(DeviceRegion.Cutoff, bias.Region);
        Assert.Equal(0, bias.ChannelCurrent);
        Assert.Equal(0, bias.Transconductance);
        Assert.Equal(0, bias.OutputConductance);
    }

    [Fact]
    public void AboveTheThresholdWithALargeDrainVoltageTheChannelSaturates()
    {
        var bias = MosfetModel.Evaluate(false, 3, 5);
        var overdrive = 3 - MosfetModel.Threshold;
        var beta = MosfetModel.TransconductanceParameter * MosfetModel.WidthOverLength;
        var modulated = beta * (1 + (MosfetModel.ChannelLengthModulation * 5));

        Assert.Equal(DeviceRegion.Saturation, bias.Region);
        Approximately.Equal(modulated * overdrive * overdrive / 2, bias.ChannelCurrent);
        Approximately.Equal(modulated * overdrive, bias.Transconductance);

        Approximately.Equal(
            beta * MosfetModel.ChannelLengthModulation * overdrive * overdrive / 2,
            bias.OutputConductance);
    }

    [Fact]
    public void BelowTheOverdriveTheChannelIsLinear()
    {
        var bias = MosfetModel.Evaluate(false, 5, 1);
        var overdrive = 5 - MosfetModel.Threshold;
        var beta = MosfetModel.TransconductanceParameter * MosfetModel.WidthOverLength;
        var modulated = beta * (1 + (MosfetModel.ChannelLengthModulation * 1));

        Assert.Equal(DeviceRegion.Linear, bias.Region);
        Approximately.Equal(modulated * 1 * (overdrive - 0.5), bias.ChannelCurrent);
        Approximately.Equal(modulated * 1, bias.Transconductance);
    }

    [Fact]
    public void TheSaturationBoundaryIsContinuous()
    {
        var overdrive = 2.0;
        var saturated = MosfetModel.Evaluate(false, 3, overdrive);
        var linear = MosfetModel.Evaluate(false, 3, overdrive - 1e-9);

        Assert.Equal(DeviceRegion.Saturation, saturated.Region);
        Assert.Equal(DeviceRegion.Linear, linear.Region);
        Approximately.Equal(saturated.ChannelCurrent, linear.ChannelCurrent, 1e-8);
        Approximately.Equal(saturated.Transconductance, linear.Transconductance, 1e-8);
    }

    [Fact]
    public void ANegativeDrainVoltageSwapsDrainAndSource()
    {
        var reversed = MosfetModel.Evaluate(false, 5, -0.4);
        var swapped = MosfetModel.Evaluate(false, 5 + 0.4, 0.4);

        Assert.Equal(DeviceRegion.Linear, reversed.Region);
        Approximately.Equal(-swapped.ChannelCurrent, reversed.ChannelCurrent);
    }

    [Fact]
    public void TheNgspiceSaturationCurrentIsReproduced()
    {
        var drainSource = 7.692307684902;
        var bias = MosfetModel.Evaluate(false, 3, drainSource);
        var body = MosfetModel.BodyDiodeVoltage(false, drainSource);

        Approximately.Equal(2.307692307396e-3, bias.ChannelCurrent, 1e-11);
        Approximately.Equal(
            2.307692315098e-3,
            bias.ChannelCurrent + MosfetModel.BodyCurrentAtDrain(false, body),
            1e-11);
    }

    [Fact]
    public void TheNgspiceLinearCurrentIsReproduced()
    {
        var bias = MosfetModel.Evaluate(false, 5, 1);
        var body = MosfetModel.BodyDiodeVoltage(false, 1);

        Approximately.Equal(
            3.570000001010e-3,
            bias.ChannelCurrent + MosfetModel.BodyCurrentAtDrain(false, body),
            1e-9);
    }

    [Fact]
    public void TheNgspiceReversedCurrentIsReproduced()
    {
        var bias = MosfetModel.Evaluate(false, 5, -0.4);
        var body = MosfetModel.BodyDiodeVoltage(false, -0.4);

        Approximately.Equal(
            -1.69349204172e-3,
            bias.ChannelCurrent + MosfetModel.BodyCurrentAtDrain(false, body),
            1e-9);
    }

    [Theory]
    [MemberData(nameof(Biases))]
    public void TheChannelOfAPChannelMirrorsTheNChannel(double gateSource, double drainSource)
    {
        var nChannel = MosfetModel.Evaluate(false, gateSource, drainSource);
        var pChannel = MosfetModel.Evaluate(true, -gateSource, -drainSource);

        Assert.Equal(nChannel.Region, pChannel.Region);
        Approximately.Equal(-nChannel.ChannelCurrent, pChannel.ChannelCurrent);
        Approximately.Equal(nChannel.Transconductance, pChannel.Transconductance);
        Approximately.Equal(nChannel.OutputConductance, pChannel.OutputConductance);
    }

    [Theory]
    [MemberData(nameof(Biases))]
    public void TheNChannelJacobianMatchesFiniteDifferences(
        double gateSource, double drainSource) =>
        JacobianAgrees(false, gateSource, drainSource);

    [Theory]
    [MemberData(nameof(Biases))]
    public void ThePChannelJacobianMatchesFiniteDifferences(
        double gateSource, double drainSource) =>
        JacobianAgrees(true, -gateSource, -drainSource);

    [Fact]
    public void TheBodyDiodeIsReverseBiasedWhenTheDrainIsAboveTheSource()
    {
        Assert.Equal(-5, MosfetModel.BodyDiodeVoltage(false, 5));
        Assert.Equal(5, MosfetModel.BodyDiodeVoltage(false, -5));
        Assert.Equal(5, MosfetModel.BodyDiodeVoltage(true, 5));
        Assert.Equal(-5, MosfetModel.BodyDiodeVoltage(true, -5));
    }

    [Fact]
    public void TheBodyDiodeCarriesTheSameCurrentAsADiode()
    {
        var voltage = MosfetModel.BodyDiodeVoltage(false, -0.4);

        Approximately.Equal(
            -DiodeModel.Current(voltage), MosfetModel.BodyCurrentAtDrain(false, voltage));

        Approximately.Equal(
            DiodeModel.Current(voltage), MosfetModel.BodyCurrentAtDrain(true, voltage));
    }

    [Fact]
    public void TheThresholdOfAPChannelIsNegative()
    {
        Assert.Equal(MosfetModel.Threshold, MosfetModel.ThresholdOf(false));
        Assert.Equal(-MosfetModel.Threshold, MosfetModel.ThresholdOf(true));
    }

    [Fact]
    public void GateLimitingOfAPChannelMirrorsTheNChannel()
    {
        Approximately.Equal(
            -MosfetModel.LimitGate(false, 20, 1), MosfetModel.LimitGate(true, -20, -1));

        Approximately.Equal(
            -MosfetModel.LimitDrainSource(false, 40, 2),
            MosfetModel.LimitDrainSource(true, -40, -2));
    }

    [Theory]
    [InlineData(-0.629, 0)]
    [InlineData(-0.629, -0.5)]
    [InlineData(-3, -2)]
    public void AReversedDrainVoltageIsNotClampedByTheForwardFloor(
        double newVoltage, double oldVoltage)
    {
        Assert.Equal(newVoltage, MosfetModel.LimitDrainSource(false, newVoltage, oldVoltage));
        Assert.Equal(-newVoltage, MosfetModel.LimitDrainSource(true, -newVoltage, -oldVoltage));
    }

    [Fact]
    public void TheReversedDrainLimiterMirrorsTheForwardOne()
    {
        Approximately.Equal(
            -MosfetModel.LimitDrainSource(false, 100, 1),
            MosfetModel.LimitDrainSource(false, -100, -1));
    }

    [Fact]
    public void TheReversedDrainLimiterConvergesOnABodyDiodeVoltage()
    {
        var voltage = 0.0;

        for (var step = 0; step < 100; step++)
        {
            voltage = MosfetModel.LimitDrainSource(false, -0.629440710772, voltage);
        }

        Assert.Equal(-0.629440710772, voltage);
    }

    private static void JacobianAgrees(bool pChannel, double gateSource, double drainSource)
    {
        var bias = MosfetModel.Evaluate(pChannel, gateSource, drainSource);

        var gate = Derivative(
            step => MosfetModel.Evaluate(
                pChannel, gateSource + step, drainSource).ChannelCurrent);

        var drain = Derivative(
            step => MosfetModel.Evaluate(
                pChannel, gateSource, drainSource + step).ChannelCurrent);

        Difference(bias.Transconductance, gate, "transconductance");
        Difference(bias.OutputConductance, drain, "output conductance");
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
            $"The {what} is {analytic:R} but finite differences give {numeric:R}, "
            + $"allowed difference {allowed:R}.");
    }
}
