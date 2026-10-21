using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Foundation.Tests.Quantities;

public class QuantityRoundTripTests
{
    private const double ThreeSignificantDigitTolerance = 6e-3;

    private static readonly double[] Mantissas =
        [1.0, 1.05, 1.23, 1.5, 2.0, 2.2, 3.33, 4.7, 5.6, 6.8, 8.25, 9.99];

    public static TheoryData<Unit> Units
    {
        get
        {
            var data = new TheoryData<Unit>();

            foreach (var unit in Enum.GetValues<Unit>())
            {
                data.Add(unit);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Units))]
    public void FormattingThenParsingReturnsAnEqualQuantity(Unit unit)
    {
        var checkedValues = 0;

        for (var exponent = -15; exponent <= 12; exponent++)
        {
            foreach (var mantissa in Mantissas)
            {
                var original = new Quantity(mantissa * Math.Pow(10, exponent), unit);
                var text = original.ToString();

                Assert.True(
                    Quantity.TryParse(text, unit, out var reparsed, out var error),
                    $"{text} was rejected: {error}.");

                Assert.Equal(unit, reparsed.Unit);
                Approximately.Equal(original.Value, reparsed.Value, ThreeSignificantDigitTolerance);
                Assert.Equal(text, reparsed.ToString());

                checkedValues++;
            }
        }

        Assert.Equal(28 * Mantissas.Length, checkedValues);
    }

    [Theory]
    [MemberData(nameof(Units))]
    public void FormattingThenParsingKeepsNegativeValues(Unit unit)
    {
        var original = new Quantity(-4.7e-9, unit);
        var text = original.ToString();

        Assert.True(Quantity.TryParse(text, unit, out var reparsed, out var error), $"{text} was rejected: {error}.");
        Assert.Equal(original, reparsed);
    }

    [Fact]
    public void EqualityIsExactOnValueAndUnit()
    {
        Assert.Equal(new Quantity(4700, Unit.Ohm), new Quantity(4700, Unit.Ohm));
        Assert.NotEqual(new Quantity(4700, Unit.Ohm), new Quantity(4700, Unit.Volt));
        Assert.NotEqual(new Quantity(4700, Unit.Ohm), new Quantity(4700.0000000001, Unit.Ohm));
        Assert.True(new Quantity(4700, Unit.Ohm) == new Quantity(4700, Unit.Ohm));
        Assert.True(new Quantity(4700, Unit.Ohm) != new Quantity(4701, Unit.Ohm));
        Assert.Equal(
            new Quantity(4700, Unit.Ohm).GetHashCode(),
            new Quantity(4700, Unit.Ohm).GetHashCode());
    }
}
