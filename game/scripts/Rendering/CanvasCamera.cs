using Godot;
using Nodal.Circuit.Geometry;
using Nodal.Foundation.Geometry;

namespace Nodal.Game.Rendering;

public sealed class CanvasCamera
{
    public float Zoom { get; private set; } = 1f;

    public Vector2 Origin { get; private set; } = Vector2.Zero;

    public float PixelsPerGridUnit => CanvasMetrics.GridPixelsAtUnitZoom * Zoom;

    public Vector2 ToScreen(GridVector point) =>
        Origin + (new Vector2((float)point.X, (float)point.Y) * PixelsPerGridUnit);

    public Vector2 ToScreen(GridPoint point) => ToScreen(new GridVector(point.X, point.Y));

    public GridVector ToGrid(Vector2 screen)
    {
        var local = (screen - Origin) / PixelsPerGridUnit;

        return new GridVector(local.X, local.Y);
    }

    public void PanBy(Vector2 delta) => Origin += delta;

    public void ZoomBy(float factor, Vector2 anchor)
    {
        var anchored = ToGrid(anchor);

        Zoom = Math.Clamp(Zoom * factor, CanvasMetrics.MinimumZoom, CanvasMetrics.MaximumZoom);
        Origin = anchor - (new Vector2((float)anchored.X, (float)anchored.Y) * PixelsPerGridUnit);
    }

    public void Fit(GridBounds? bounds, Vector2 viewportSize)
    {
        if (viewportSize.X <= 0 || viewportSize.Y <= 0)
        {
            return;
        }

        if (bounds is not { } box)
        {
            Zoom = 1f;
            Origin = viewportSize / 2f;

            return;
        }

        var grown = box.Grown(CanvasMetrics.FitMarginInGridUnits);
        var width = Math.Max(grown.Width, 1) * CanvasMetrics.GridPixelsAtUnitZoom;
        var height = Math.Max(grown.Height, 1) * CanvasMetrics.GridPixelsAtUnitZoom;

        Zoom = Math.Clamp(
            Math.Min(viewportSize.X / width, viewportSize.Y / height),
            CanvasMetrics.MinimumZoom,
            CanvasMetrics.MaximumZoom);

        var center = grown.Center;

        Origin = (viewportSize / 2f)
            - (new Vector2((float)center.X, (float)center.Y) * PixelsPerGridUnit);
    }
}
