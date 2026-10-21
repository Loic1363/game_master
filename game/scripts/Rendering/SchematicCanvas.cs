using Godot;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Editing;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Hit;
using Nodal.Circuit.Schematics;
using Nodal.Circuit.Symbols;
using Nodal.Content.Symbols;
using Nodal.Foundation.Geometry;

namespace Nodal.Game.Rendering;

public partial class SchematicCanvas : Control
{
    private readonly CanvasCamera _camera = new();

    private EditHistory? _history;
    private SymbolLibrary _symbols = SymbolLibrary.Empty;
    private ConnectivityResult? _connectivity;
    private Net? _hoveredNet;
    private Font? _font;
    private bool _panning;
    private bool _spaceHeld;
    private bool _fitted;

    public event Action<Net?>? HoveredNetChanged;

    private Font Font => _font ??= GetThemeDefaultFont();

    public override void _Ready()
    {
        _font = GetThemeDefaultFont();
        Resized += OnResized;
        MouseExited += OnMouseExited;
    }

    public void Display(EditHistory history, SymbolLibrary symbols, ConnectivityResult connectivity)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(symbols);
        ArgumentNullException.ThrowIfNull(connectivity);

        _history = history;
        _symbols = symbols;
        _connectivity = connectivity;
        _hoveredNet = null;
        _fitted = false;

        FitToSchematic();
        QueueRedraw();
    }

    public void FitToSchematic()
    {
        if (_history is null || Size.X <= 0 || Size.Y <= 0)
        {
            return;
        }

        _camera.Fit(SchematicBounds.Of(_history.Schematic, _history.Catalog, _symbols), Size);
        _fitted = true;

        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), VisualTokens.Background);
        DrawGridDots();

        if (_history is null || _connectivity is null)
        {
            return;
        }

        DrawComponents(_history);
        DrawWires(_history);
        DrawJunctions(_connectivity);
        DrawHoveredNet(_history);
        DrawLabels(_history);
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton button:
                HandleButton(button);
                break;

            case InputEventMouseMotion motion:
                HandleMotion(motion);
                break;
        }
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key)
        {
            return;
        }

        if (key.Keycode == Key.Space)
        {
            _spaceHeld = key.Pressed;

            if (!key.Pressed)
            {
                _panning = false;
            }

            return;
        }

        if (key.Keycode == Key.Home && key.Pressed)
        {
            FitToSchematic();
        }
    }

    private void OnResized()
    {
        if (_fitted)
        {
            QueueRedraw();
            return;
        }

        FitToSchematic();
    }

    private void OnMouseExited() => SetHoveredNet(null);

    private void HandleButton(InputEventMouseButton button)
    {
        switch (button.ButtonIndex)
        {
            case MouseButton.WheelUp when button.Pressed:
                _camera.ZoomBy(CanvasMetrics.ZoomStep, button.Position);
                QueueRedraw();
                break;

            case MouseButton.WheelDown when button.Pressed:
                _camera.ZoomBy(1f / CanvasMetrics.ZoomStep, button.Position);
                QueueRedraw();
                break;

            case MouseButton.Middle:
                _panning = button.Pressed;
                break;

            case MouseButton.Left:
                _panning = button.Pressed && _spaceHeld;
                break;
        }
    }

    private void HandleMotion(InputEventMouseMotion motion)
    {
        if (_panning)
        {
            _camera.PanBy(motion.Relative);
            QueueRedraw();

            return;
        }

        UpdateHoveredNet(motion.Position);
    }

    private void UpdateHoveredNet(Vector2 screenPosition)
    {
        if (_history is null)
        {
            return;
        }

        var tolerance = CanvasMetrics.HoverTolerancePixels / _camera.PixelsPerGridUnit;

        var hit = SchematicHitTester.Nearest(
            _history.Schematic, _history.Catalog, _camera.ToGrid(screenPosition), tolerance);

        SetHoveredNet(NetOf(hit));
    }

    private Net? NetOf(SchematicHit? hit)
    {
        if (_connectivity is null || hit is null)
        {
            return null;
        }

        if (hit.Pin is { } pin)
        {
            return _connectivity.TryGetNetOfPin(pin, out var pinNet) ? pinNet : null;
        }

        if (hit.Wire is { } wire)
        {
            return _connectivity.TryGetNetOfWire(wire, out var wireNet) ? wireNet : null;
        }

        return null;
    }

    private void SetHoveredNet(Net? net)
    {
        if (ReferenceEquals(net, _hoveredNet))
        {
            return;
        }

        _hoveredNet = net;
        HoveredNetChanged?.Invoke(net);

        QueueRedraw();
    }

    private void DrawGridDots()
    {
        if (_camera.PixelsPerGridUnit < CanvasMetrics.MinimumDotSpacing)
        {
            return;
        }

        var topLeft = _camera.ToGrid(Vector2.Zero);
        var bottomRight = _camera.ToGrid(Size);

        for (var y = (int)Math.Floor(topLeft.Y); y <= (int)Math.Ceiling(bottomRight.Y); y++)
        {
            for (var x = (int)Math.Floor(topLeft.X); x <= (int)Math.Ceiling(bottomRight.X); x++)
            {
                DrawCircle(
                    _camera.ToScreen(new GridVector(x, y)),
                    CanvasMetrics.DotRadius,
                    VisualTokens.GridDots);
            }
        }
    }

    private void DrawComponents(EditHistory history)
    {
        foreach (var component in history.Schematic.Components.Values)
        {
            if (_symbols.TryGet(component.DefinitionId, SymbolStyle.Iec, out var symbol))
            {
                SymbolRenderer.Draw(
                    this,
                    Font,
                    _camera,
                    symbol,
                    component.Position,
                    component.Orientation,
                    VisualTokens.Stroke);
            }
        }
    }

    private void DrawWires(EditHistory history)
    {
        foreach (var wire in history.Schematic.Wires.Values)
        {
            DrawWire(wire, VisualTokens.Stroke);
        }
    }

    private void DrawWire(Wire wire, Color color) => DrawLine(
        _camera.ToScreen(wire.Start), _camera.ToScreen(wire.End), color, CanvasMetrics.StrokeWidth);

    private void DrawJunctions(ConnectivityResult connectivity)
    {
        foreach (var junction in connectivity.Junctions)
        {
            DrawCircle(
                _camera.ToScreen(junction), CanvasMetrics.JunctionRadius, VisualTokens.Stroke);
        }
    }

    private void DrawHoveredNet(EditHistory history)
    {
        if (_hoveredNet is not { } net)
        {
            return;
        }

        foreach (var id in net.Wires)
        {
            if (history.Schematic.TryGetWire(id, out var wire))
            {
                DrawWire(wire, VisualTokens.NetHighlight);
            }
        }

        foreach (var pin in net.Pins)
        {
            DrawPin(history, pin);
        }
    }

    private void DrawPin(EditHistory history, PinRef pin)
    {
        if (!history.Schematic.TryGetPinPositions(pin.ComponentId, history.Catalog, out var positions))
        {
            return;
        }

        foreach (var placed in positions)
        {
            if (string.Equals(placed.PinName, pin.PinName, StringComparison.Ordinal))
            {
                DrawCircle(
                    _camera.ToScreen(placed.Position),
                    CanvasMetrics.JunctionRadius,
                    VisualTokens.NetHighlight);
            }
        }
    }

    private void DrawLabels(EditHistory history)
    {
        var fontSize = CanvasText.FontSize(_camera.Zoom);

        foreach (var component in history.Schematic.Components.Values)
        {
            if (!_symbols.TryGet(component.DefinitionId, SymbolStyle.Iec, out var symbol))
            {
                continue;
            }

            var placed = SymbolTransform.Apply(symbol, component.Orientation);

            CanvasText.DrawCentered(
                this,
                Font,
                Screen(component.Position, placed.DesignatorAnchor),
                component.Designator,
                fontSize,
                VisualTokens.LabelText);

            if (placed.ValueAnchor is { } anchor && FirstValue(history, component) is { } value)
            {
                CanvasText.DrawCentered(
                    this, Font, Screen(component.Position, anchor), value, fontSize, VisualTokens.LabelText);
            }
        }
    }

    private static string? FirstValue(EditHistory history, ComponentInstance component)
    {
        if (!history.Catalog.TryGetById(component.DefinitionId, out var definition)
            || definition.Parameters.Count == 0)
        {
            return null;
        }

        var first = definition.Parameters[0];

        return component.Parameters.TryGetValue(first.Name, out var quantity)
            ? quantity.ToString()
            : null;
    }

    private Vector2 Screen(GridPoint position, GridVector local) =>
        _camera.ToScreen(new GridVector(position.X + local.X, position.Y + local.Y));
}
