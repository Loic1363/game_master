using Nodal.Content.Symbols;
using Nodal.Foundation.Geometry;

namespace Nodal.Circuit.Symbols;

internal static class SymbolExtent
{
    public static IEnumerable<GridVector> Corners(Symbol symbol)
    {
        foreach (var primitive in symbol.Primitives)
        {
            foreach (var corner in Corners(primitive))
            {
                yield return corner;
            }
        }

        yield return symbol.DesignatorAnchor;

        if (symbol.ValueAnchor is { } anchor)
        {
            yield return anchor;
        }
    }

    private static IEnumerable<GridVector> Corners(SymbolPrimitive primitive)
    {
        switch (primitive)
        {
            case SymbolLine line:
                yield return line.From;
                yield return line.To;
                break;

            case SymbolPolyline polyline:
                foreach (var point in polyline.Points)
                {
                    yield return point;
                }

                break;

            case SymbolPolygon polygon:
                foreach (var point in polygon.Points)
                {
                    yield return point;
                }

                break;

            case SymbolRect rect:
                yield return new GridVector(rect.X, rect.Y);
                yield return new GridVector(rect.X + rect.Width, rect.Y + rect.Height);
                break;

            case SymbolCircle circle:
                yield return circle.Center - new GridVector(circle.Radius, circle.Radius);
                yield return circle.Center + new GridVector(circle.Radius, circle.Radius);
                break;

            case SymbolArc arc:
                yield return arc.Center - new GridVector(arc.Radius, arc.Radius);
                yield return arc.Center + new GridVector(arc.Radius, arc.Radius);
                break;

            case SymbolText text:
                yield return text.At;
                break;
        }
    }
}
