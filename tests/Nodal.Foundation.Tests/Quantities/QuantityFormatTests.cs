using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Foundation.Tests.Quantities;

public class QuantityFormatTests
{
    [Theory]
    [InlineData(1e-15, Unit.Farad, "1 fF")]
    [InlineData(1e-12, Unit.Farad, "1 pF")]
    [InlineData(1e-9, Unit.Farad, "1 nF")]
    [InlineData(1e-6, Unit.Farad, "1 µF")]
    [InlineData(1e-3, Unit.Volt, "1 mV")]
    [InlineData(1.0, Unit.Volt, "1 V")]
    [InlineData(1e3, Unit.Ohm, "1 kΩ")]
    [InlineData(1e6, Unit.Ohm, "1 MΩ")]
    [InlineData(1e9, Unit.Ohm, "1 GΩ")]
    [InlineData(1e12, Unit.Ohm, "1 TΩ")]
    public void FormatsEveryPrefixFromFemtoToTera(double value, Unit unit, string expected)
    {
        Assert.Equal(expected, new Quantity(value, unit).ToString());
    }

    [Theory]
    [InlineData(0.0, Unit.Volt, "0 V")]
    [InlineData(0.0, Unit.Ohm, "0 Ω")]
    [InlineData(0.0, Unit.Dimensionless, "0")]
    public void FormatsZero(double value, Unit unit, string expected)
    {
        Assert.Equal(expected, new Quantity(value, unit).ToString());
    }

    [Theory]
    [InlineData(4700.0, Unit.Ohm, "4.7 kΩ")]
    [InlineData(1e-7, Unit.Farad, "100 nF")]
    [InlineData(0.0022, Unit.Ampere, "2.2 mA")]
    [InlineData(4700.0, Unit.Dimensionless, "4.7 k")]
    [InlineData(4.7, Unit.Dimensionless, "4.7")]
    [InlineData(-4700.0, Unit.Ohm, "-4.7 kΩ")]
    [InlineData(2.2e6, Unit.Ohm, "2.2 MΩ")]
    [InlineData(1000.0, Unit.Hertz, "1 kHz")]
    [InlineData(0.1, Unit.Volt, "100 mV")]
    [InlineData(1e-3, Unit.Second, "1 ms")]
    public void FormatsEngineeringNotation(double value, Unit unit, string expected)
    {
        Assert.Equal(expected, new Quantity(value, unit).ToString());
    }

    [Theory]
    [InlineData(123456.0, Unit.Ohm, "123 kΩ")]
    [InlineData(1.23456e-6, Unit.Farad, "1.23 µF")]
    [InlineData(12.3456, Unit.Volt, "12.3 V")]
    [InlineData(999.4, Unit.Ohm, "999 Ω")]
    [InlineData(999.6, Unit.Ohm, "1 kΩ")]
    [InlineData(1.0005, Unit.Volt, "1 V")]
    [InlineData(1.05, Unit.Volt, "1.05 V")]
    public void KeepsAtMostThreeSignificantDigitsWithoutTrailingZeros(double value, Unit unit, string expected)
    {
        Assert.Equal(expected, new Quantity(value, unit).ToString());
    }

    [Fact]
    public void FormatsMicroWithTheMicroSignAndNotGreekMu()
    {
        var text = new Quantity(1e-6, Unit.Farad).ToString();

        Assert.Equal("1 µF", text);
        Assert.DoesNotContain("μ", text);
    }

    [Theory]
    [InlineData(1e15, Unit.Volt, "1000 TV")]
    [InlineData(1e-18, Unit.Farad, "0.001 fF")]
    public void ClampsMantissaWhenTheExponentLeavesThePrefixRange(double value, Unit unit, string expected)
    {
        Assert.Equal(expected, new Quantity(value, unit).ToString());
    }
}
