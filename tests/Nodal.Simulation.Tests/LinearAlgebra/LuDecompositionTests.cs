using Nodal.Simulation.LinearAlgebra;
using Xunit;

namespace Nodal.Simulation.Tests.LinearAlgebra;

public class LuDecompositionTests
{
    [Fact]
    public void ATwoByTwoSystemIsSolvedExactly()
    {
        var matrix = Matrix(new double[,] { { 2, 1 }, { 1, 3 } });

        Assert.True(LuDecomposition.TryFactor(matrix, out var decomposition));

        var solution = decomposition.Solve([5, 10]);

        Approximately.Equal(1, solution[0]);
        Approximately.Equal(3, solution[1]);
    }

    [Fact]
    public void AZeroLeadingPivotIsSwappedAway()
    {
        var matrix = Matrix(new double[,] { { 0, 1 }, { 1, 0 } });

        Assert.True(LuDecomposition.TryFactor(matrix, out var decomposition));

        var solution = decomposition.Solve([1, 2]);

        Approximately.Equal(2, solution[0]);
        Approximately.Equal(1, solution[1]);
    }

    [Fact]
    public void PartialPivotingKeepsAnIllOrderedSystemAccurate()
    {
        var matrix = Matrix(new double[,]
        {
            { 1e-10, 1, 0 },
            { 1, 1, 1 },
            { 0, 2, 1 },
        });

        Assert.True(LuDecomposition.TryFactor(matrix, out var decomposition));

        var solution = decomposition.Solve([1, 2, 3]);
        var residual = Residual(matrix, solution, [1, 2, 3]);

        Approximately.Zero(residual, 1e-12);
    }

    [Fact]
    public void ARepeatedRowIsDetectedAsSingular()
    {
        var matrix = Matrix(new double[,] { { 1, 2 }, { 2, 4 } });

        Assert.False(LuDecomposition.TryFactor(matrix, out var decomposition));
        Assert.Null(decomposition);
    }

    [Fact]
    public void AZeroRowIsDetectedAsSingular()
    {
        var matrix = Matrix(new double[,] { { 1, 2 }, { 0, 0 } });

        Assert.False(LuDecomposition.TryFactor(matrix, out _));
    }

    [Fact]
    public void AVerySmallButRealPivotStillFactors()
    {
        var matrix = Matrix(new double[,] { { 1e-12, 0 }, { 0, 1 } });

        Assert.True(LuDecomposition.TryFactor(matrix, out var decomposition));

        var solution = decomposition.Solve([1e-12, 2]);

        Approximately.Equal(1, solution[0]);
        Approximately.Equal(2, solution[1]);
    }

    [Fact]
    public void AnEmptySystemSolvesToNothing()
    {
        Assert.True(LuDecomposition.TryFactor(new DenseMatrix(0), out var decomposition));
        Assert.Empty(decomposition.Solve([]));
        Assert.Equal(0, decomposition.Size);
    }

    [Fact]
    public void AMismatchedRightHandSideIsRejected()
    {
        Assert.True(LuDecomposition.TryFactor(Matrix(new double[,] { { 1 } }), out var decomposition));
        Assert.Throws<ArgumentException>(() => decomposition.Solve([1, 2]));
    }

    [Fact]
    public void FactoringNothingIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => LuDecomposition.TryFactor(null!, out _));
    }

    private static double Residual(
        DenseMatrix matrix, double[] solution, double[] rightHandSide)
    {
        var worst = 0.0;

        for (var row = 0; row < matrix.Size; row++)
        {
            var value = 0.0;

            for (var column = 0; column < matrix.Size; column++)
            {
                value += matrix[row, column] * solution[column];
            }

            worst = Math.Max(worst, Math.Abs(value - rightHandSide[row]));
        }

        return worst;
    }

    private static DenseMatrix Matrix(double[,] values)
    {
        var matrix = new DenseMatrix(values.GetLength(0));

        for (var row = 0; row < matrix.Size; row++)
        {
            for (var column = 0; column < matrix.Size; column++)
            {
                matrix[row, column] = values[row, column];
            }
        }

        return matrix;
    }
}
