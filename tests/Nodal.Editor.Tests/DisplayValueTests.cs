using Nodal.Editor.Simulation;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Editor.Tests;

public class DisplayValueTests
{
    [Theory]
    [InlineData(0, "0 V")]
    [InlineData(1e-10, "0 V")]
    [InlineData(-1e-10, "0 V")]
    [InlineData(1e-12, "0 V")]
    [InlineData(5e-10, "0 V")]
    public void AVoltageBelowANanovoltReadsAsZero(double value, string expected) =>
        Assert.Equal(expected, DisplayValue.Text(value, Unit.Volt));

    [Theory]
    [InlineData(1e-9, "1 nV")]
    [InlineData(-1e-9, "-1 nV")]
    [InlineData(2e-9, "2 nV")]
    [InlineData(1e-6, "1 µV")]
    [InlineData(5, "5 V")]
    [InlineData(-3.3, "-3.3 V")]
    public void AVoltageAtOrAboveANanovoltKeepsItsValue(double value, string expected) =>
        Assert.Equal(expected, DisplayValue.Text(value, Unit.Volt));

    [Theory]
    [InlineData(0, "0 A")]
    [InlineData(1e-13, "0 A")]
    [InlineData(-1e-13, "0 A")]
    [InlineData(9.9e-13, "0 A")]
    public void ACurrentBelowAPicoampereReadsAsZero(double value, string expected) =>
        Assert.Equal(expected, DisplayValue.Text(value, Unit.Ampere));

    [Theory]
    [InlineData(1e-12, "1 pA")]
    [InlineData(-1e-12, "-1 pA")]
    [InlineData(1e-9, "1 nA")]
    [InlineData(5e-3, "5 mA")]
    public void ACurrentAtOrAboveAPicoampereKeepsItsValue(double value, string expected) =>
        Assert.Equal(expected, DisplayValue.Text(value, Unit.Ampere));

    [Fact]
    public void ACurrentBetweenTheTwoFloorsIsKept()
    {
        Assert.Equal("1 pA", DisplayValue.Text(1e-12, Unit.Ampere));
        Assert.Equal("0 V", DisplayValue.Text(1e-12, Unit.Volt));
    }

    [Theory]
    [InlineData(Unit.Watt, "1 pW")]
    [InlineData(Unit.Ohm, "1 pΩ")]
    [InlineData(Unit.Second, "1 ps")]
    [InlineData(Unit.Farad, "1 pF")]
    public void OtherUnitsAreNotFloored(Unit unit, string expected) =>
        Assert.Equal(expected, DisplayValue.Text(1e-12, unit));

    [Fact]
    public void AQuantityIsFlooredJustLikeAValue()
    {
        Assert.Equal("0 V", DisplayValue.Text(new Quantity(1e-15, Unit.Volt)));
        Assert.Equal("1 V", DisplayValue.Text(new Quantity(1, Unit.Volt)));
    }

    [Fact]
    public void TheMagnitudeDropsTheSignAndStillFloors()
    {
        Assert.Equal("3.3 V", DisplayValue.Magnitude(new Quantity(-3.3, Unit.Volt)));
        Assert.Equal("0 V", DisplayValue.Magnitude(new Quantity(-1e-12, Unit.Volt)));
        Assert.Equal("0 A", DisplayValue.Magnitude(new Quantity(-1e-15, Unit.Ampere)));
    }

    [Fact]
    public void TheFloorsAreTheOnesTheSpecNames()
    {
        Assert.Equal(1e-9, DisplayValue.VoltageFloor);
        Assert.Equal(1e-12, DisplayValue.CurrentFloor);
    }
}
