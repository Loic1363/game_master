using Nodal.Circuit.Geometry;
using Nodal.Content.Symbols;
using Nodal.Foundation.Geometry;

namespace Nodal.Circuit.Symbols;

public static class LabelPlacement
{
    public const double Gap = 0.4;

    public const double LineHeight = 0.6;

    private const double HalfLineHeight = LineHeight / 2;

    public static LabelLayout Of(Symbol symbol, Orientation orientation)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var box = TransformedBox(symbol, orientation);

        return IsUpright(orientation.Rotation) ? AboveAndBelow(box) : ToTheRight(box);
    }

    public static IEnumerable<GridVector> Anchors(Symbol symbol, Orientation orientation)
    {
        var layout = Of(symbol, orientation);

        yield return layout.Designator.Position;
        yield return layout.Value.Position;
    }

    private static bool IsUpright(Rotation rotation) =>
        rotation is Rotation.R0 or Rotation.R180;

    private static LabelLayout AboveAndBelow(GridBox box)
    {
        var center = (box.Minimum.X + box.Maximum.X) / 2;

        return new LabelLayout(
            new LabelAnchor(
                new GridVector(center, box.Minimum.Y - Gap - HalfLineHeight), LabelAlignment.Centered),
            new LabelAnchor(
                new GridVector(center, box.Maximum.Y + Gap + HalfLineHeight), LabelAlignment.Centered));
    }

    private static LabelLayout ToTheRight(GridBox box)
    {
        var left = box.Maximum.X + Gap;
        var center = (box.Minimum.Y + box.Maximum.Y) / 2;

        return new LabelLayout(
            new LabelAnchor(new GridVector(left, center - HalfLineHeight), LabelAlignment.Left),
            new LabelAnchor(new GridVector(left, center + HalfLineHeight), LabelAlignment.Left));
    }

    private static GridBox TransformedBox(Symbol symbol, Orientation orientation) =>
        GridBox.Around(
            SymbolExtent.Corners(symbol).Select(corner => SymbolTransform.Apply(corner, orientation)));
}
