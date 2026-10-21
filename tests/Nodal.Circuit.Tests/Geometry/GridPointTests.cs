using Nodal.Circuit.Geometry;
using Xunit;

namespace Nodal.Circuit.Tests.Geometry;

public class GridPointTests
{
    [Fact]
    public void OriginIsTheZeroPoint()
    {
        Assert.Equal(new GridPoint(0, 0), GridPoint.Origin);
        Assert.Equal(GridPoint.Origin, default);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(3, 4, 1, 2, 4, 6)]
    [InlineData(-2, 5, 2, -5, 0, 0)]
    [InlineData(-7, -1, -3, -4, -10, -5)]
    public void AdditionAddsBothCoordinates(int ax, int ay, int bx, int by, int x, int y) =>
        Assert.Equal(new GridPoint(x, y), new GridPoint(ax, ay) + new GridPoint(bx, by));

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(3, 4, 1, 2, 2, 2)]
    [InlineData(-2, 5, 2, -5, -4, 10)]
    [InlineData(-7, -1, -3, -4, -4, 3)]
    public void SubtractionSubtractsBothCoordinates(int ax, int ay, int bx, int by, int x, int y) =>
        Assert.Equal(new GridPoint(x, y), new GridPoint(ax, ay) - new GridPoint(bx, by));

    [Fact]
    public void EqualPointsAreEqualAndShareAHashCode()
    {
        var left = new GridPoint(4, -9);
        var right = new GridPoint(4, -9);

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.False(left != right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void PointsThatDifferInOneCoordinateAreNotEqual()
    {
        Assert.NotEqual(new GridPoint(1, 2), new GridPoint(2, 1));
        Assert.NotEqual(new GridPoint(1, 2), new GridPoint(1, 3));
    }

    [Fact]
    public void TextIsTheCoordinatePair() => Assert.Equal("(-3, 7)", new GridPoint(-3, 7).ToString());
}
