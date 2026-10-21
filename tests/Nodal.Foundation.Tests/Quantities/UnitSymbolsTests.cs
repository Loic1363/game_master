using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Foundation.Tests.Quantities;

public class UnitSymbolsTests
{
    [Theory]
    [InlineData(Unit.Dimensionless, "")]
    [InlineData(Unit.Ohm, "Ω")]
    [InlineData(Unit.Farad, "F")]
    [InlineData(Unit.Henry, "H")]
    [InlineData(Unit.Volt, "V")]
    [InlineData(Unit.Ampere, "A")]
    [InlineData(Unit.Second, "s")]
    [InlineData(Unit.Hertz, "Hz")]
    [InlineData(Unit.Watt, "W")]
    public void UnitKnowsItsSymbol(Unit unit, string expectedSymbol)
    {
        Assert.Equal(expectedSymbol, unit.Symbol());
    }

    [Fact]
    public void EveryUnitHasASymbol()
    {
        foreach (var unit in Enum.GetValues<Unit>())
        {
            Assert.NotNull(unit.Symbol());
        }
    }
}
