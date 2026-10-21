using System.Collections.Immutable;
using Nodal.Circuit.Geometry;
using Nodal.Content.Symbols;
using Nodal.Foundation.Geometry;

namespace Nodal.Circuit.Symbols;

public static class SymbolTransform
{
    private const double DegreesPerQuarterTurn = 90;
    private const double DegreesPerTurn = 360;
    private const double StraightAngleDegrees = 180;

    public static GridVector Apply(GridVector point, Orientation orientation)
    {
        var current = orientation.Mirrored ? new GridVector(-point.X, point.Y) : point;

        for (var turn = 0; turn < orientation.Rotation.QuarterTurns(); turn++)
        {
            current = new GridVector(-current.Y, current.X);
        }

        return current;
    }

    public static Symbol Apply(Symbol symbol, Orientation orientation)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        return symbol with
        {
            Primitives = [.. symbol.Primitives.Select(primitive => Apply(primitive, orientation))],
            DesignatorAnchor = Apply(symbol.DesignatorAnchor, orientation),
            ValueAnchor = symbol.ValueAnchor is { } anchor ? Apply(anchor, orientation) : null,
        };
    }

    public static SymbolPrimitive Apply(SymbolPrimitive primitive, Orientation orientation)
    {
        ArgumentNullException.ThrowIfNull(primitive);

        return primitive switch
        {
            SymbolLine line => new SymbolLine(Apply(line.From, orientation), Apply(line.To, orientation)),
            SymbolPolyline polyline => polyline with { Points = ApplyAll(polyline.Points, orientation) },
            SymbolRect rect => ApplyToRect(rect, orientation),
            SymbolCircle circle => circle with { Center = Apply(circle.Center, orientation) },
            SymbolArc arc => ApplyToArc(arc, orientation),
            SymbolPolygon polygon => polygon with { Points = ApplyAll(polygon.Points, orientation) },
            SymbolText text => text with { At = Apply(text.At, orientation) },
            _ => throw new ArgumentOutOfRangeException(
                nameof(primitive), primitive, "Unknown symbol primitive."),
        };
    }

    private static ImmutableArray<GridVector> ApplyAll(
        ImmutableArray<GridVector> points, Orientation orientation) =>
        [.. points.Select(point => Apply(point, orientation))];

    private static SymbolRect ApplyToRect(SymbolRect rect, Orientation orientation)
    {
        var first = Apply(new GridVector(rect.X, rect.Y), orientation);
        var second = Apply(new GridVector(rect.X + rect.Width, rect.Y + rect.Height), orientation);

        return new SymbolRect(
            Math.Min(first.X, second.X),
            Math.Min(first.Y, second.Y),
            Math.Abs(second.X - first.X),
            Math.Abs(second.Y - first.Y));
    }

    private static SymbolArc ApplyToArc(SymbolArc arc, Orientation orientation)
    {
        var start = orientation.Mirrored ? StraightAngleDegrees - arc.StartDegrees : arc.StartDegrees;
        var sweep = orientation.Mirrored ? -arc.SweepDegrees : arc.SweepDegrees;

        start += DegreesPerQuarterTurn * orientation.Rotation.QuarterTurns();

        return new SymbolArc(Apply(arc.Center, orientation), arc.Radius, Normalized(start), sweep);
    }

    private static double Normalized(double degrees) =>
        ((degrees % DegreesPerTurn) + DegreesPerTurn) % DegreesPerTurn;
}
