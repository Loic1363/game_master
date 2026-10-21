using System.Collections.Immutable;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;

namespace Nodal.Editor;

public static class WireRouter
{
    public static ImmutableArray<WireSegment> Route(
        Schematic schematic, GridPoint start, GridPoint end)
    {
        ArgumentNullException.ThrowIfNull(schematic);

        var corner = new GridPoint(end.X, start.Y);
        var segments = ImmutableArray.CreateBuilder<WireSegment>(2);

        AddUnless(segments, schematic, new WireSegment(start, corner));
        AddUnless(segments, schematic, new WireSegment(corner, end));

        return segments.ToImmutable();
    }

    public static ImmutableArray<WireSegment> Preview(GridPoint start, GridPoint end)
    {
        var corner = new GridPoint(end.X, start.Y);
        var segments = ImmutableArray.CreateBuilder<WireSegment>(2);

        if (start != corner)
        {
            segments.Add(new WireSegment(start, corner));
        }

        if (corner != end)
        {
            segments.Add(new WireSegment(corner, end));
        }

        return segments.ToImmutable();
    }

    private static void AddUnless(
        ImmutableArray<WireSegment>.Builder segments, Schematic schematic, WireSegment segment)
    {
        if (segment.Start == segment.End || Exists(schematic, segment))
        {
            return;
        }

        segments.Add(segment);
    }

    private static bool Exists(Schematic schematic, WireSegment segment) =>
        schematic.Wires.Values.Any(wire => wire.HasEndpoints(segment.Start, segment.End));
}
