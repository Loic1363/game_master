using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Foundation.Tests.Quantities;

public class QuantityInvariantTests
{
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ConstructorRejectsNonFiniteValues(double value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(value, Unit.Ohm));

        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(4700.0)]
    [InlineData(-4700.0)]
    [InlineData(double.MaxValue)]
    [InlineData(double.Epsilon)]
    public void ConstructorAcceptsEveryFiniteValue(double value)
    {
        Assert.Equal(value, new Quantity(value, Unit.Ohm).Value);
    }

    [Fact]
    public void DefaultQuantityIsZeroAndDimensionless()
    {
        var quantity = default(Quantity);

        Assert.Equal(0.0, quantity.Value);
        Assert.Equal(Unit.Dimensionless, quantity.Unit);
    }
}
