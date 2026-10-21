using Nodal.Circuit.Schematics;
using Nodal.Circuit.Symbols;
using Nodal.Content.Components;
using Nodal.Content.Symbols;
using Nodal.Foundation.Geometry;

namespace Nodal.Circuit.Geometry;

public static class SchematicBounds
{
    public static GridBounds? Of(Schematic schematic, ComponentCatalog catalog, SymbolLibrary symbols)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(symbols);

        var points = Points(schematic, catalog, symbols).ToArray();

        if (points.Length == 0)
        {
            return null;
        }

        return new GridBounds(
            new GridPoint(
                (int)Math.Floor(points.Min(point => point.X)),
                (int)Math.Floor(points.Min(point => point.Y))),
            new GridPoint(
                (int)Math.Ceiling(points.Max(point => point.X)),
                (int)Math.Ceiling(points.Max(point => point.Y))));
    }

    private static IEnumerable<GridVector> Points(
        Schematic schematic, ComponentCatalog catalog, SymbolLibrary symbols)
    {
        foreach (var component in schematic.Components.Values)
        {
            foreach (var point in PointsOf(schematic, catalog, symbols, component))
            {
                yield return point;
            }
        }

        foreach (var wire in schematic.Wires.Values)
        {
            yield return At(wire.Start);
            yield return At(wire.End);
        }
    }

    private static IEnumerable<GridVector> PointsOf(
        Schematic schematic,
        ComponentCatalog catalog,
        SymbolLibrary symbols,
        ComponentInstance component)
    {
        if (symbols.TryGet(component.DefinitionId, SymbolStyle.Iec, out var symbol))
        {
            var placed = SymbolTransform.Apply(symbol, component.Orientation);

            foreach (var corner in SymbolExtent.Corners(placed))
            {
                yield return At(component.Position) + corner;
            }
        }

        if (!schematic.TryGetPinPositions(component.Id, catalog, out var positions))
        {
            yield return At(component.Position);

            yield break;
        }

        foreach (var pin in positions)
        {
            yield return At(pin.Position);
        }
    }

    private static GridVector At(GridPoint position) => new(position.X, position.Y);
}
