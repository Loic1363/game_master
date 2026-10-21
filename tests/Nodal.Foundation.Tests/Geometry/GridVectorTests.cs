using Nodal.Foundation.Geometry;
using Xunit;

namespace Nodal.Foundation.Tests.Geometry;

public class GridVectorTests
{
    [Fact]
    public void ZeroSitsAtTheOrigin()
    {
        Assert.Equal(new GridVector(0, 0), GridVector.Zero);
    }

    [Fact]
    public void VectorsAddAndSubtractComponentwise()
    {
        var left = new GridVector(1.5, -2);
        var right = new GridVector(0.5, 3);

        Assert.Equal(new GridVector(2, 1), left + right);
        Assert.Equal(new GridVector(1, -5), left - right);
    }

    [Fact]
    public void ScalingMultipliesBothComponents()
    {
        Assert.Equal(new GridVector(1.5, -3), new GridVector(0.5, -1) * 3);
    }

    [Fact]
    public void LengthMeasuresTheDistanceFromTheOrigin()
    {
        Assert.Equal(5, new GridVector(3, 4).Length, 9);
        Assert.Equal(0, GridVector.Zero.Length);
    }

    [Fact]
    public void DotProductCombinesBothComponents()
    {
        Assert.Equal(11, new GridVector(1, 2).Dot(new GridVector(3, 4)), 9);
    }

    [Fact]
    public void TextIsCultureIndependent()
    {
        Assert.Equal("(1.5, -0.25)", new GridVector(1.5, -0.25).ToString());
    }

    [Fact]
    public void VectorsWithEqualComponentsAreEqual()
    {
        Assert.Equal(new GridVector(1.5, 2), new GridVector(1.5, 2));
        Assert.NotEqual(new GridVector(1.5, 2), new GridVector(2, 1.5));
    }
}
