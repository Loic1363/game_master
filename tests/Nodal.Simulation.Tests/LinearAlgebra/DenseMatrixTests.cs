using Nodal.Simulation.LinearAlgebra;
using Xunit;

namespace Nodal.Simulation.Tests.LinearAlgebra;

public class DenseMatrixTests
{
    [Fact]
    public void ANewMatrixIsAllZeros()
    {
        var matrix = new DenseMatrix(3);

        Assert.Equal(3, matrix.Size);
        Assert.Equal(0, matrix[2, 1]);
        Assert.Equal(0, matrix.MaxMagnitude());
    }

    [Fact]
    public void EntriesAreStoredPerRowAndColumn()
    {
        var matrix = new DenseMatrix(2);

        matrix[0, 1] = 4;
        matrix.Add(0, 1, 3);

        Assert.Equal(7, matrix[0, 1]);
        Assert.Equal(0, matrix[1, 0]);
    }

    [Fact]
    public void TheLargestMagnitudeIgnoresTheSign()
    {
        var matrix = new DenseMatrix(2);

        matrix[0, 0] = -9;
        matrix[1, 1] = 4;

        Assert.Equal(9, matrix.MaxMagnitude());
    }

    [Fact]
    public void ACopyIsIndependent()
    {
        var matrix = new DenseMatrix(2);
        matrix[0, 0] = 1;

        var copy = matrix.Copy();
        copy[0, 0] = 2;

        Assert.Equal(1, matrix[0, 0]);
        Assert.Equal(2, copy[0, 0]);
    }

    [Fact]
    public void ANonFiniteEntryIsReported()
    {
        var matrix = new DenseMatrix(1);

        Assert.True(matrix.IsFinite());

        matrix[0, 0] = double.PositiveInfinity;

        Assert.False(matrix.IsFinite());
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(2, 0)]
    [InlineData(0, 2)]
    public void ACellOutsideTheMatrixIsRejected(int row, int column)
    {
        var matrix = new DenseMatrix(2);

        Assert.Throws<ArgumentOutOfRangeException>(() => matrix[row, column]);
    }

    [Fact]
    public void ANegativeSizeIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DenseMatrix(-1));
    }
}
