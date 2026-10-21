using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Foundation.Tests.Quantities;

public class QuantityParseTests
{
    [Theory]
    [InlineData("4700", Unit.Ohm, 4700.0)]
    [InlineData("4.7e3", Unit.Ohm, 4700.0)]
    [InlineData("4.7E3", Unit.Ohm, 4700.0)]
    [InlineData("0.001", Unit.Ohm, 0.001)]
    [InlineData("-4700", Unit.Volt, -4700.0)]
    [InlineData("+4700", Unit.Volt, 4700.0)]
    [InlineData("0", Unit.Volt, 0.0)]
    public void ParsesPlainNumbers(string text, Unit unit, double expected)
    {
        AssertParses(text, unit, expected);
    }

    [Theory]
    [InlineData("1f", Unit.Farad, 1e-15)]
    [InlineData("1p", Unit.Farad, 1e-12)]
    [InlineData("1n", Unit.Farad, 1e-9)]
    [InlineData("1u", Unit.Farad, 1e-6)]
    [InlineData("1µ", Unit.Farad, 1e-6)]
    [InlineData("1μ", Unit.Farad, 1e-6)]
    [InlineData("1m", Unit.Ohm, 1e-3)]
    [InlineData("1k", Unit.Ohm, 1e3)]
    [InlineData("1M", Unit.Ohm, 1e6)]
    [InlineData("1G", Unit.Ohm, 1e9)]
    [InlineData("1T", Unit.Ohm, 1e12)]
    [InlineData("4.7k", Unit.Ohm, 4700.0)]
    [InlineData("470u", Unit.Farad, 470e-6)]
    [InlineData("470μ", Unit.Farad, 470e-6)]
    [InlineData("4μ7", Unit.Farad, 4.7e-6)]
    public void ParsesCaseSensitiveSiPrefixes(string text, Unit unit, double expected)
    {
        AssertParses(text, unit, expected);
    }

    [Theory]
    [InlineData("1meg", Unit.Ohm, 1e6)]
    [InlineData("1MEG", Unit.Ohm, 1e6)]
    [InlineData("1Meg", Unit.Ohm, 1e6)]
    [InlineData("1mEg", Unit.Ohm, 1e6)]
    [InlineData("4.7meg", Unit.Ohm, 4.7e6)]
    public void ParsesSpiceMega(string text, Unit unit, double expected)
    {
        AssertParses(text, unit, expected);
    }

    [Theory]
    [InlineData("4k7", Unit.Ohm, 4700.0)]
    [InlineData("2M2", Unit.Ohm, 2.2e6)]
    [InlineData("4n7", Unit.Farad, 4.7e-9)]
    [InlineData("1k05", Unit.Ohm, 1050.0)]
    [InlineData("2R2", Unit.Ohm, 2.2)]
    [InlineData("R47", Unit.Ohm, 0.47)]
    [InlineData("2r2", Unit.Ohm, 2.2)]
    [InlineData("r47", Unit.Ohm, 0.47)]
    [InlineData("1r", Unit.Ohm, 1.0)]
    public void ParsesRkmNotation(string text, Unit unit, double expected)
    {
        AssertParses(text, unit, expected);
    }

    [Theory]
    [InlineData("4.7kΩ", Unit.Ohm, 4700.0)]
    [InlineData("4.7kohm", Unit.Ohm, 4700.0)]
    [InlineData("4.7kohms", Unit.Ohm, 4700.0)]
    [InlineData("4.7kOhm", Unit.Ohm, 4700.0)]
    [InlineData("4.7kOHMS", Unit.Ohm, 4700.0)]
    [InlineData("100nF", Unit.Farad, 100e-9)]
    [InlineData("5V", Unit.Volt, 5.0)]
    [InlineData("1kHz", Unit.Hertz, 1000.0)]
    [InlineData("10mA", Unit.Ampere, 0.01)]
    [InlineData("1mH", Unit.Henry, 1e-3)]
    [InlineData("1ms", Unit.Second, 1e-3)]
    [InlineData("1W", Unit.Watt, 1.0)]
    [InlineData("2R2Ω", Unit.Ohm, 2.2)]
    [InlineData("2r2ohm", Unit.Ohm, 2.2)]
    [InlineData("4μ7F", Unit.Farad, 4.7e-6)]
    public void ParsesOptionalUnitSymbol(string text, Unit unit, double expected)
    {
        AssertParses(text, unit, expected);
    }

    [Theory]
    [InlineData("4,7k", Unit.Ohm, 4700.0)]
    [InlineData("0,001", Unit.Ohm, 0.001)]
    [InlineData("4,7", Unit.Ohm, 4.7)]
    public void AcceptsCommaAsDecimalSeparator(string text, Unit unit, double expected)
    {
        AssertParses(text, unit, expected);
    }

    [Theory]
    [InlineData("  4.7k  ", Unit.Ohm, 4700.0)]
    [InlineData("4.7 kΩ", Unit.Ohm, 4700.0)]
    [InlineData("4.7 k", Unit.Ohm, 4700.0)]
    [InlineData("100 nF", Unit.Farad, 100e-9)]
    [InlineData("4.7 Ω", Unit.Ohm, 4.7)]
    [InlineData("\t4.7k\n", Unit.Ohm, 4700.0)]
    public void IgnoresSurroundingWhitespaceAndAcceptsOneInnerSpace(string text, Unit unit, double expected)
    {
        AssertParses(text, unit, expected);
    }

    [Fact]
    public void ParsedQuantityCarriesTheExpectedUnit()
    {
        Assert.True(Quantity.TryParse("4k7", Unit.Ohm, out var quantity, out var error));

        Assert.Equal(QuantityParseError.None, error);
        Assert.Equal(Unit.Ohm, quantity.Unit);
    }

    private static void AssertParses(string text, Unit unit, double expected)
    {
        Assert.True(Quantity.TryParse(text, unit, out var quantity, out var error), $"{text} was rejected: {error}.");
        Assert.Equal(QuantityParseError.None, error);
        Assert.Equal(unit, quantity.Unit);
        Approximately.Equal(expected, quantity.Value);
    }
}
