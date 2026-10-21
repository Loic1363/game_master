using Nodal.Circuit.Geometry;

namespace Nodal.Editor;

public readonly record struct WireSegment(GridPoint Start, GridPoint End)
{
    public override string ToString() => $"{Start} to {End}";
}
