namespace Nodal.Simulation.LinearAlgebra;

public sealed class DenseMatrix
{
    private readonly double[] _values;

    public DenseMatrix(int size)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size), size, "A matrix cannot have a negative size.");
        }

        Size = size;
        _values = new double[size * size];
    }

    public int Size { get; }

    public double this[int row, int column]
    {
        get
        {
            RequireCell(row, column);

            return _values[(row * Size) + column];
        }

        set
        {
            RequireCell(row, column);

            _values[(row * Size) + column] = value;
        }
    }

    public void Add(int row, int column, double value) => this[row, column] += value;

    public double MaxMagnitude()
    {
        var largest = 0.0;

        foreach (var value in _values)
        {
            largest = Math.Max(largest, Math.Abs(value));
        }

        return largest;
    }

    public bool IsFinite()
    {
        foreach (var value in _values)
        {
            if (!double.IsFinite(value))
            {
                return false;
            }
        }

        return true;
    }

    public DenseMatrix Copy()
    {
        var copy = new DenseMatrix(Size);

        Array.Copy(_values, copy._values, _values.Length);

        return copy;
    }

    private void RequireCell(int row, int column)
    {
        if (row < 0 || row >= Size)
        {
            throw new ArgumentOutOfRangeException(
                nameof(row), row, $"A matrix of size {Size} has no such row.");
        }

        if (column < 0 || column >= Size)
        {
            throw new ArgumentOutOfRangeException(
                nameof(column), column, $"A matrix of size {Size} has no such column.");
        }
    }
}
