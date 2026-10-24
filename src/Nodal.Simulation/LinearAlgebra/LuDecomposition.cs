using System.Diagnostics.CodeAnalysis;

namespace Nodal.Simulation.LinearAlgebra;

public sealed class LuDecomposition
{
    public const double RelativePivotTolerance = 1e-14;

    private readonly DenseMatrix _factors;
    private readonly int[] _rowOrder;

    private LuDecomposition(DenseMatrix factors, int[] rowOrder)
    {
        _factors = factors;
        _rowOrder = rowOrder;
    }

    public int Size => _factors.Size;

    public static bool TryFactor(
        DenseMatrix matrix, [NotNullWhen(true)] out LuDecomposition? decomposition)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        decomposition = null;

        var factors = matrix.Copy();
        var tolerance = matrix.MaxMagnitude() * RelativePivotTolerance;
        var rowOrder = new int[matrix.Size];

        for (var index = 0; index < rowOrder.Length; index++)
        {
            rowOrder[index] = index;
        }

        for (var column = 0; column < factors.Size; column++)
        {
            var pivot = PivotRow(factors, column);

            if (Math.Abs(factors[pivot, column]) <= tolerance)
            {
                return false;
            }

            if (pivot != column)
            {
                SwapRows(factors, rowOrder, pivot, column);
            }

            Eliminate(factors, column);
        }

        if (!factors.IsFinite())
        {
            return false;
        }

        decomposition = new LuDecomposition(factors, rowOrder);

        return true;
    }

    public double[] Solve(IReadOnlyList<double> rightHandSide)
    {
        ArgumentNullException.ThrowIfNull(rightHandSide);

        if (rightHandSide.Count != Size)
        {
            throw new ArgumentException(
                $"A system of size {Size} needs {Size} right-hand-side values, not "
                + $"{rightHandSide.Count}.",
                nameof(rightHandSide));
        }

        var solution = new double[Size];

        for (var row = 0; row < Size; row++)
        {
            var value = rightHandSide[_rowOrder[row]];

            for (var column = 0; column < row; column++)
            {
                value -= _factors[row, column] * solution[column];
            }

            solution[row] = value;
        }

        for (var row = Size - 1; row >= 0; row--)
        {
            var value = solution[row];

            for (var column = row + 1; column < Size; column++)
            {
                value -= _factors[row, column] * solution[column];
            }

            solution[row] = value / _factors[row, row];
        }

        return solution;
    }

    private static int PivotRow(DenseMatrix factors, int column)
    {
        var pivot = column;

        for (var row = column + 1; row < factors.Size; row++)
        {
            if (Math.Abs(factors[row, column]) > Math.Abs(factors[pivot, column]))
            {
                pivot = row;
            }
        }

        return pivot;
    }

    private static void SwapRows(DenseMatrix factors, int[] rowOrder, int left, int right)
    {
        for (var column = 0; column < factors.Size; column++)
        {
            (factors[left, column], factors[right, column]) =
                (factors[right, column], factors[left, column]);
        }

        (rowOrder[left], rowOrder[right]) = (rowOrder[right], rowOrder[left]);
    }

    private static void Eliminate(DenseMatrix factors, int column)
    {
        for (var row = column + 1; row < factors.Size; row++)
        {
            var multiplier = factors[row, column] / factors[column, column];

            factors[row, column] = multiplier;

            for (var next = column + 1; next < factors.Size; next++)
            {
                factors[row, next] -= multiplier * factors[column, next];
            }
        }
    }
}
