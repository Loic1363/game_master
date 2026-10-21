using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Foundation.Tests.Quantities;

public class QuantityParseRejectionTests
{
    [Theory]
    [InlineData("", Unit.Ohm, QuantityParseError.Empty)]
    [InlineData("   ", Unit.Ohm, QuantityParseError.Empty)]
    [InlineData("\t", Unit.Ohm, QuantityParseError.Empty)]
    [InlineData("4k7k", Unit.Ohm, QuantityParseError.MalformedNumber)]
    [InlineData("1.2.3", Unit.Ohm, QuantityParseError.MalformedNumber)]
    [InlineData("k", Unit.Ohm, QuantityParseError.MalformedNumber)]
    [InlineData("--1", Unit.Ohm, QuantityParseError.MalformedNumber)]
    [InlineData("1 234", Unit.Ohm, QuantityParseError.MalformedNumber)]
    [InlineData("1,234.5", Unit.Ohm, QuantityParseError.MalformedNumber)]
    [InlineData("4.7  k", Unit.Ohm, QuantityParseError.MalformedNumber)]
    [InlineData("Ω", Unit.Ohm, QuantityParseError.MalformedNumber)]
    [InlineData("4.7q", Unit.Ohm, QuantityParseError.UnknownPrefix)]
    [InlineData("4.7 x", Unit.Ohm, QuantityParseError.UnknownPrefix)]
    [InlineData("4.7 kV", Unit.Ohm, QuantityParseError.WrongUnit)]
    [InlineData("4.7kV", Unit.Ohm, QuantityParseError.WrongUnit)]
    [InlineData("5V", Unit.Ohm, QuantityParseError.WrongUnit)]
    [InlineData("1Hz", Unit.Henry, QuantityParseError.WrongUnit)]
    [InlineData("1kΩ", Unit.Dimensionless, QuantityParseError.WrongUnit)]
    [InlineData("NaN", Unit.Ohm, QuantityParseError.OutOfRange)]
    [InlineData("nan", Unit.Ohm, QuantityParseError.OutOfRange)]
    [InlineData("Infinity", Unit.Ohm, QuantityParseError.OutOfRange)]
    [InlineData("-Infinity", Unit.Ohm, QuantityParseError.OutOfRange)]
    [InlineData("1e400", Unit.Ohm, QuantityParseError.OutOfRange)]
    [InlineData("1e400k", Unit.Ohm, QuantityParseError.OutOfRange)]
    [InlineData("2R2", Unit.Volt, QuantityParseError.UnknownPrefix)]
    [InlineData("2r2", Unit.Volt, QuantityParseError.UnknownPrefix)]
    [InlineData("r47", Unit.Farad, QuantityParseError.UnknownPrefix)]
    public void RejectsInvalidInput(string text, Unit unit, QuantityParseError expectedError)
    {
        Assert.False(Quantity.TryParse(text, unit, out var quantity, out var error));

        Assert.Equal(expectedError, error);
        Assert.Equal(default(Quantity), quantity);
    }

    [Fact]
    public void RejectsNullWithoutThrowing()
    {
        Assert.False(Quantity.TryParse(null!, Unit.Ohm, out var quantity, out var error));

        Assert.Equal(QuantityParseError.Empty, error);
        Assert.Equal(default(Quantity), quantity);
    }
}
