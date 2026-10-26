using Nodal.Foundation.Waveforms;
using Xunit;

namespace Nodal.Foundation.Tests.Waveforms;

public class WaveformTextTests
{
    [Fact]
    public void APulseIsWrittenWithShortSiPrefixes()
    {
        var pulse = new PulseWaveform(0, 12, 0, 1e-9, 1e-9, 20e-3, 40e-3);

        Assert.Equal("PULSE(0 12 0 1n 1n 20m 40m)", WaveformText.Format(pulse));
    }

    [Fact]
    public void AWaveformPrintsItsOwnText()
    {
        var pulse = new PulseWaveform(0, 12, 0, 1e-9, 1e-9, 20e-3, 40e-3);

        Assert.Equal("PULSE(0 12 0 1n 1n 20m 40m)", pulse.ToString());
    }

    [Fact]
    public void APulseWithACycleCountWritesIt()
    {
        var pulse = new PulseWaveform(0, 5, 0, 1e-6, 1e-6, 1e-3, 2e-3, 3);

        Assert.Equal("PULSE(0 5 0 1µ 1µ 1m 2m 3)", WaveformText.Format(pulse));
    }

    [Fact]
    public void ASineWritesOnlyTheArgumentsItNeeds()
    {
        Assert.Equal("SINE(0 10 50)", WaveformText.Format(new SineWaveform(0, 10, 50)));

        Assert.Equal(
            "SINE(0 10 50 1m)",
            WaveformText.Format(new SineWaveform(0, 10, 50, Delay: 1e-3)));

        Assert.Equal(
            "SINE(0 10 50 0 0 90)",
            WaveformText.Format(new SineWaveform(0, 10, 50, PhaseDegrees: 90)));

        Assert.Equal(
            "SINE(0 10 50 0 0 0 4)",
            WaveformText.Format(new SineWaveform(0, 10, 50, Cycles: 4)));
    }

    [Fact]
    public void AnExponentialAndAPiecewiseFunctionAreWrittenInFull()
    {
        Assert.Equal(
            "EXP(0 5 0 100µ 1m 100µ)",
            WaveformText.Format(new ExpWaveform(0, 5, 0, 100e-6, 1e-3, 100e-6)));

        Assert.Equal(
            "PWL(0 0 1m 5 2m 0)",
            WaveformText.Format(new PwlWaveform(
            [
                new WaveformPoint(0, 0),
                new WaveformPoint(1e-3, 5),
                new WaveformPoint(2e-3, 0),
            ])));
    }

    [Theory]
    [InlineData("PULSE(0 12 0 1n 1n 20m 40m)")]
    [InlineData("PULSE(0 5 0 1µ 1µ 1m 2m 3)")]
    [InlineData("SINE(0 10 50)")]
    [InlineData("SINE(-1 10 50 1m 100 90 4)")]
    [InlineData("EXP(0 5 0 100µ 1m 100µ)")]
    [InlineData("PWL(0 0 1m 5 2m 0)")]
    [InlineData("PWL(1m 2.5)")]
    public void EveryFunctionSurvivesAParseAndFormatRoundTrip(string text)
    {
        Assert.True(WaveformText.TryParse(text, out var waveform, out var error));
        Assert.Equal(WaveformError.None, error);
        Assert.Equal(text, WaveformText.Format(waveform));
    }

    [Theory]
    [InlineData("pulse(0 12 0 1n 1n 20m 40m)")]
    [InlineData("Pulse(0 12 0 1n 1n 20m 40m)")]
    [InlineData("  PULSE( 0  12 0 1n 1n 20m 40m )  ")]
    public void FunctionNamesAndSpacingAreForgiving(string text)
    {
        Assert.True(WaveformText.TryParse(text, out var waveform, out _));
        Assert.Equal("PULSE(0 12 0 1n 1n 20m 40m)", WaveformText.Format(waveform));
    }

    [Fact]
    public void SinIsAcceptedAsAnAliasForSine()
    {
        Assert.True(WaveformText.TryParse("SIN(0 10 50)", out var waveform, out _));
        Assert.Equal(WaveformKind.Sine, waveform.Kind);
        Assert.Equal("SINE(0 10 50)", WaveformText.Format(waveform));
    }

    [Theory]
    [InlineData("PWL(0 0 4k7 1)", 4700)]
    [InlineData("PWL(0 0 1meg 1)", 1e6)]
    [InlineData("PWL(0 0 1MEG 1)", 1e6)]
    [InlineData("PWL(0 0 1,5 1)", 1.5)]
    [InlineData("PWL(0 0 1.5 1)", 1.5)]
    [InlineData("PWL(0 0 1m 1)", 1e-3)]
    [InlineData("PWL(0 0 1M 1)", 1e6)]
    public void ArgumentsUseTheGameQuantityNotation(string text, double expectedTime)
    {
        Assert.True(WaveformText.TryParse(text, out var waveform, out _));

        var piecewise = Assert.IsType<PwlWaveform>(waveform);

        Assert.Equal(expectedTime, piecewise.Points[1].Time);
    }

    [Theory]
    [InlineData("", WaveformError.Empty)]
    [InlineData("   ", WaveformError.Empty)]
    [InlineData("PULSE", WaveformError.MalformedSyntax)]
    [InlineData("PULSE(0 1", WaveformError.MalformedSyntax)]
    [InlineData("(0 1)", WaveformError.MalformedSyntax)]
    [InlineData("PULSE(0 1) extra", WaveformError.MalformedSyntax)]
    [InlineData("RAMP(0 1)", WaveformError.UnknownFunction)]
    [InlineData("PULSE(0 12 0 1n 1n 20m)", WaveformError.WrongArgumentCount)]
    [InlineData("PULSE(0 12 0 1n 1n 20m 40m 2 9)", WaveformError.WrongArgumentCount)]
    [InlineData("SINE(0 10)", WaveformError.WrongArgumentCount)]
    [InlineData("EXP(0 1 0 1m 2m)", WaveformError.WrongArgumentCount)]
    [InlineData("PWL(1m)", WaveformError.WrongArgumentCount)]
    [InlineData("PWL()", WaveformError.WrongArgumentCount)]
    [InlineData("PWL(0 0 1m five)", WaveformError.MalformedNumber)]
    [InlineData("PWL(0 0 1q 5)", WaveformError.UnknownPrefix)]
    [InlineData("PWL(0 0 1ms 5)", WaveformError.UnitNotAllowed)]
    [InlineData("PWL(0 0 1V 5)", WaveformError.UnitNotAllowed)]
    [InlineData("PULSE(0, 12, 0, 1n, 1n, 20m, 40m)", WaveformError.MalformedNumber)]
    [InlineData("PWL(0 0 1m. 5)", WaveformError.MalformedNumber)]
    [InlineData("PULSE(0 12 0 1n 1n 20m 40m 2.5)", WaveformError.InvalidCycleCount)]
    [InlineData("PULSE(0 12 0 1n 1n 20m 40m 0)", WaveformError.InvalidCycleCount)]
    [InlineData("PULSE(0 12 0 1n 1n 20m 10m)", WaveformError.PeriodTooShort)]
    [InlineData("PULSE(0 12 -1m 1n 1n 20m 40m)", WaveformError.NegativeTime)]
    [InlineData("EXP(0 1 0 0 2m 1m)", WaveformError.NonPositiveTau)]
    [InlineData("PWL(1m 1 1m 2)", WaveformError.UnorderedPoints)]
    public void MalformedTextIsRejectedWithItsOwnError(string text, WaveformError expected)
    {
        Assert.False(WaveformText.TryParse(text, out var waveform, out var error));
        Assert.Null(waveform);
        Assert.Equal(expected, error);
    }

    [Theory]
    [InlineData(WaveformKind.Pulse)]
    [InlineData(WaveformKind.Sine)]
    [InlineData(WaveformKind.Exp)]
    [InlineData(WaveformKind.Pwl)]
    public void EveryTemplateParsesIntoItsOwnKind(WaveformKind kind)
    {
        Assert.True(
            WaveformText.TryParse(WaveformText.TemplateOf(kind), out var waveform, out _));

        Assert.Equal(kind, waveform.Kind);
    }

    [Theory]
    [InlineData(0.02, "20m")]
    [InlineData(1e-9, "1n")]
    [InlineData(12, "12")]
    [InlineData(-5, "-5")]
    [InlineData(1234, "1.234k")]
    [InlineData(1e6, "1M")]
    [InlineData(1.5e-6, "1.5µ")]
    [InlineData(1.0 / 3, "333.333m")]
    public void NumbersAreWrittenInTheShortestPrefixedForm(double value, string expected)
    {
        var piecewise = new PwlWaveform([new WaveformPoint(0, value)]);

        Assert.Equal($"PWL(0 {expected})", WaveformText.Format(piecewise));
    }

    [Fact]
    public void ANumberKeepsSixSignificantDigits()
    {
        Assert.True(WaveformText.TryParse("PWL(0 1.234567)", out var waveform, out _));

        var piecewise = Assert.IsType<PwlWaveform>(waveform);

        Assert.Equal(1.234567, piecewise.Points[0].Value);
        Assert.Equal("PWL(0 1.23457)", WaveformText.Format(waveform));
    }
}
