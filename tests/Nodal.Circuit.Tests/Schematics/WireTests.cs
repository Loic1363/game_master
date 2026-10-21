using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Schematics;

public class WireTests
{
    [Theory]
    [InlineData(0, 0, 4, 0)]
    [InlineData(4, 0, 0, 0)]
    [InlineData(-3, 7, 10, 7)]
    public void AHorizontalWireIsAccepted(int startX, int startY, int endX, int endY)
    {
        var wire = new Wire(WireId.First, new GridPoint(startX, startY), new GridPoint(endX, endY));

        Assert.True(wire.IsHorizontal);
    }

    [Theory]
    [InlineData(0, 0, 0, 4)]
    [InlineData(0, 4, 0, 0)]
    [InlineData(5, -3, 5, 10)]
    public void AVerticalWireIsAccepted(int startX, int startY, int endX, int endY)
    {
        var wire = new Wire(WireId.First, new GridPoint(startX, startY), new GridPoint(endX, endY));

        Assert.False(wire.IsHorizontal);
    }

    [Fact]
    public void AWireKeepsItsIdentifierAndEndpoints()
    {
        var wire = new Wire(new WireId(4), new GridPoint(1, 2), new GridPoint(1, 9));

        Assert.Equal(new WireId(4), wire.Id);
        Assert.Equal(new GridPoint(1, 2), wire.Start);
        Assert.Equal(new GridPoint(1, 9), wire.End);
    }

    [Fact]
    public void AZeroLengthWireIsARejectedArgument()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new Wire(WireId.First, new GridPoint(3, 3), new GridPoint(3, 3)));

        Assert.Equal("end", exception.ParamName);
    }

    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(0, 0, -4, 2)]
    [InlineData(5, 5, 2, 8)]
    public void ADiagonalWireIsARejectedArgument(int startX, int startY, int endX, int endY)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new Wire(WireId.First, new GridPoint(startX, startY), new GridPoint(endX, endY)));

        Assert.Equal("end", exception.ParamName);
    }

    [Fact]
    public void EndpointsMatchInEitherDirection()
    {
        var wire = new Wire(WireId.First, new GridPoint(0, 0), new GridPoint(0, 5));

        Assert.True(wire.HasEndpoints(new GridPoint(0, 0), new GridPoint(0, 5)));
        Assert.True(wire.HasEndpoints(new GridPoint(0, 5), new GridPoint(0, 0)));
    }

    [Fact]
    public void DifferentEndpointsDoNotMatch()
    {
        var wire = new Wire(WireId.First, new GridPoint(0, 0), new GridPoint(0, 5));

        Assert.False(wire.HasEndpoints(new GridPoint(0, 0), new GridPoint(0, 6)));
        Assert.False(wire.HasEndpoints(new GridPoint(1, 0), new GridPoint(1, 5)));
    }
}
