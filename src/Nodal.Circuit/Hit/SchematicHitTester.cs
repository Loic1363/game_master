using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Geometry;

namespace Nodal.Circuit.Hit;

public static class SchematicHitTester
{
    public static SchematicHit? Nearest(
        Schematic schematic, ComponentCatalog catalog, GridVector point, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!double.IsFinite(tolerance) || tolerance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance), tolerance, "A hit tolerance must be finite and not negative.");
        }

        return NearestPin(schematic, catalog, point, tolerance)
            ?? NearestWire(schematic, point, tolerance);
    }

    private static SchematicHit? NearestPin(
        Schematic schematic, ComponentCatalog catalog, GridVector point, double tolerance)
    {
        SchematicHit? nearest = null;

        foreach (var component in schematic.Components.Values)
        {
            if (!schematic.TryGetPinPositions(component.Id, catalog, out var positions))
            {
                continue;
            }

            foreach (var pin in positions)
            {
                var distance = (point - At(pin.Position)).Length;

                if (distance <= tolerance && (nearest is null || distance < nearest.Distance))
                {
                    nearest = new SchematicHit(new PinRef(component.Id, pin.PinName), null, distance);
                }
            }
        }

        return nearest;
    }

    private static SchematicHit? NearestWire(Schematic schematic, GridVector point, double tolerance)
    {
        SchematicHit? nearest = null;

        foreach (var wire in schematic.Wires.Values)
        {
            var distance = DistanceToSegment(point, At(wire.Start), At(wire.End));

            if (distance <= tolerance && (nearest is null || distance < nearest.Distance))
            {
                nearest = new SchematicHit(null, wire.Id, distance);
            }
        }

        return nearest;
    }

    private static double DistanceToSegment(GridVector point, GridVector start, GridVector end)
    {
        var span = end - start;
        var lengthSquared = span.Dot(span);
        var offset = point - start;

        var position = lengthSquared == 0
            ? 0
            : Math.Clamp(offset.Dot(span) / lengthSquared, 0, 1);

        return (point - (start + (span * position))).Length;
    }

    private static GridVector At(GridPoint position) => new(position.X, position.Y);
}
