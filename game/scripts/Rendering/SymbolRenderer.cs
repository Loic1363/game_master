using System.Collections.Immutable;
using Godot;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Symbols;
using Nodal.Content.Symbols;
using Nodal.Foundation.Geometry;
using Orientation = Nodal.Circuit.Geometry.Orientation;

namespace Nodal.Game.Rendering;

public static class SymbolRenderer
{
    private const int MinimumArcPoints = 12;
    private const float ArcPixelsPerPoint = 2f;
    private const float FilledWidth = -1f;
    private const double RadiansPerDegree = Math.PI / 180.0;

    public static void Draw(
        CanvasItem canvas,
        Font font,
        CanvasCamera camera,
        Symbol symbol,
        GridPoint position,
        Orientation orientation,
        Color color)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(camera);

        foreach (var primitive in SymbolTransform.Apply(symbol, orientation).Primitives)
        {
            Draw(canvas, font, camera, primitive, position, color);
        }
    }

    private static void Draw(
        CanvasItem canvas,
        Font font,
        CanvasCamera camera,
        SymbolPrimitive primitive,
        GridPoint position,
        Color color)
    {
        switch (primitive)
        {
            case SymbolLine line:
                canvas.DrawLine(
                    Screen(camera, position, line.From),
                    Screen(camera, position, line.To),
                    color,
                    CanvasMetrics.StrokeWidth);
                break;

            case SymbolPolyline polyline:
                canvas.DrawPolyline(
                    Path(camera, position, polyline.Points, polyline.Closed),
                    color,
                    CanvasMetrics.StrokeWidth);
                break;

            case SymbolRect rect:
                canvas.DrawRect(
                    ScreenRect(camera, position, rect), color, false, CanvasMetrics.StrokeWidth);
                break;

            case SymbolCircle circle:
                canvas.DrawCircle(
                    Screen(camera, position, circle.Center),
                    (float)circle.Radius * camera.PixelsPerGridUnit,
                    color,
                    circle.Filled,
                    circle.Filled ? FilledWidth : CanvasMetrics.StrokeWidth);
                break;

            case SymbolArc arc:
                DrawArc(canvas, camera, arc, position, color);
                break;

            case SymbolPolygon polygon when polygon.Filled:
                canvas.DrawColoredPolygon(Path(camera, position, polygon.Points, false), color);
                break;

            case SymbolPolygon polygon:
                canvas.DrawPolyline(
                    Path(camera, position, polygon.Points, true), color, CanvasMetrics.StrokeWidth);
                break;

            case SymbolText text:
                CanvasText.DrawCentered(
                    canvas,
                    font,
                    Screen(camera, position, text.At),
                    text.Value,
                    CanvasText.FontSize(camera.Zoom),
                    color);
                break;
        }
    }

    private static void DrawArc(
        CanvasItem canvas, CanvasCamera camera, SymbolArc arc, GridPoint position, Color color)
    {
        var radius = (float)arc.Radius * camera.PixelsPerGridUnit;
        var start = (float)(arc.StartDegrees * RadiansPerDegree);
        var end = (float)((arc.StartDegrees + arc.SweepDegrees) * RadiansPerDegree);
        var points = Math.Max(MinimumArcPoints, (int)(radius / ArcPixelsPerPoint));

        canvas.DrawArc(
            Screen(camera, position, arc.Center),
            radius,
            start,
            end,
            points,
            color,
            CanvasMetrics.StrokeWidth);
    }

    private static Vector2 Screen(CanvasCamera camera, GridPoint position, GridVector local) =>
        camera.ToScreen(new GridVector(position.X + local.X, position.Y + local.Y));

    private static Vector2[] Path(
        CanvasCamera camera, GridPoint position, ImmutableArray<GridVector> points, bool closed)
    {
        var screen = new List<Vector2>(points.Length + 1);

        foreach (var point in points)
        {
            screen.Add(Screen(camera, position, point));
        }

        if (closed && screen.Count > 0)
        {
            screen.Add(screen[0]);
        }

        return [.. screen];
    }

    private static Rect2 ScreenRect(CanvasCamera camera, GridPoint position, SymbolRect rect)
    {
        var topLeft = Screen(camera, position, new GridVector(rect.X, rect.Y));
        var size = new Vector2((float)rect.Width, (float)rect.Height) * camera.PixelsPerGridUnit;

        return new Rect2(topLeft, size);
    }
}
