namespace Nodal.Editor.Waveforms;

public readonly record struct AxisRange(double Min, double Max)
{
    public const double DefaultMargin = 0.05;

    private const double FlatSpanAtZero = 1;
    private const double Half = 0.5;

    public double Span => Max - Min;

    public bool IsFlat => Span <= 0;

    public double Centre => Min + (Span * Half);

    public static AxisRange Around(double value)
    {
        var half = double.IsFinite(value) && value != 0 ? Math.Abs(value) : FlatSpanAtZero;

        return new AxisRange(value - half, value + half);
    }

    public static AxisRange Fitting(IEnumerable<double> values, double margin = DefaultMargin)
    {
        ArgumentNullException.ThrowIfNull(values);

        var minimum = double.PositiveInfinity;
        var maximum = double.NegativeInfinity;

        foreach (var value in values.Where(double.IsFinite))
        {
            minimum = Math.Min(minimum, value);
            maximum = Math.Max(maximum, value);
        }

        if (!double.IsFinite(minimum) || !double.IsFinite(maximum))
        {
            return Around(0);
        }

        return minimum == maximum
            ? Around(minimum)
            : new AxisRange(minimum, maximum).Grown(margin);
    }

    public AxisRange Grown(double margin)
    {
        var grown = Span * margin;

        return new AxisRange(Min - grown, Max + grown);
    }

    public AxisRange ZoomedBy(double factor)
    {
        if (!double.IsFinite(factor) || factor <= 0 || IsFlat)
        {
            return this;
        }

        var half = Span / factor * Half;
        var centre = Centre;

        return new AxisRange(centre - half, centre + half);
    }

    public AxisRange MovedBy(double offset) => new(Min + offset, Max + offset);

    public AxisRange Union(AxisRange other) =>
        new(Math.Min(Min, other.Min), Math.Max(Max, other.Max));

    public double FractionOf(double value) => IsFlat ? Half : (value - Min) / Span;

    public double ValueAt(double fraction) => Min + (fraction * Span);

    public double Clamp(double value) => Math.Clamp(value, Min, Max);

    public bool Contains(double value) => value >= Min && value <= Max;
}
