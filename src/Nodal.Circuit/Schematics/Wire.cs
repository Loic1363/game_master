using Nodal.Circuit.Geometry;

namespace Nodal.Circuit.Schematics;

public sealed class Wire
{
    public Wire(WireId id, GridPoint start, GridPoint end)
    {
        if (start == end)
        {
            throw new ArgumentException($"A wire cannot have zero length at {start}.", nameof(end));
        }

        if (start.X != end.X && start.Y != end.Y)
        {
            throw new ArgumentException(
                $"A wire cannot be diagonal, from {start} to {end}.", nameof(end));
        }

        Id = id;
        Start = start;
        End = end;
    }

    public WireId Id { get; }

    public GridPoint Start { get; }

    public GridPoint End { get; }

    public bool IsHorizontal => Start.Y == End.Y;

    public bool HasEndpoints(GridPoint first, GridPoint second) =>
        (Start == first && End == second) || (Start == second && End == first);

    public override string ToString() => $"Wire {Id} {Start} to {End}";
}
