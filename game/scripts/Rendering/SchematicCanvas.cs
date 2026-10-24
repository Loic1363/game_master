using System.Collections.Immutable;
using Godot;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Hit;
using Nodal.Circuit.Schematics;
using Nodal.Circuit.Symbols;
using Nodal.Content.Symbols;
using Nodal.Editor;
using Nodal.Editor.Simulation;
using Nodal.Foundation.Geometry;
using Nodal.Simulation.Dc;

namespace Nodal.Game.Rendering;

public partial class SchematicCanvas : Control
{
    private readonly CanvasCamera _camera = new();

    private EditorSession? _session;
    private SymbolLibrary _symbols = SymbolLibrary.Empty;
    private ConnectivityResult? _connectivity;
    private OperatingPoint? _operatingPoint;
    private ImmutableArray<NetVoltageLabel> _voltageLabels = [];
    private Net? _hoveredNet;
    private ComponentId? _hoveredComponent;
    private Font? _font;
    private Vector2? _pointer;
    private bool _panning;
    private bool _fitted;

    public event Action<Net?>? HoveredNetChanged;

    public event Action<ComponentId?>? HoveredComponentChanged;

    public event Action? ViewChanged;

    public bool ShowGrid { get; set; } = true;

    public bool PanModifierHeld { get; set; }

    private Font Font => _font ??= GetThemeDefaultFont();

    public override void _Ready()
    {
        _font = GetThemeDefaultFont();
        Resized += OnResized;
        MouseExited += OnMouseExited;
    }

    public void Display(EditorSession session, SymbolLibrary symbols, ConnectivityResult connectivity)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(symbols);
        ArgumentNullException.ThrowIfNull(connectivity);

        _session = session;
        _symbols = symbols;
        _connectivity = connectivity;
        _hoveredNet = null;
        _hoveredComponent = null;
        _fitted = false;

        ClearOperatingPoint();

        FitToSchematic();
        QueueRedraw();
    }

    public void SetConnectivity(ConnectivityResult connectivity)
    {
        ArgumentNullException.ThrowIfNull(connectivity);

        _connectivity = connectivity;
        _hoveredNet = null;

        RefreshPointer();
        QueueRedraw();
    }

    public void ShowOperatingPoint(OperatingPoint operatingPoint)
    {
        ArgumentNullException.ThrowIfNull(operatingPoint);

        _operatingPoint = operatingPoint;
        _voltageLabels = _session is null || _connectivity is null
            ? []
            : OperatingPointOverlay.Labels(
                _session.Schematic, _session.Catalog, _connectivity, operatingPoint);

        QueueRedraw();
    }

    public void ClearOperatingPoint()
    {
        _operatingPoint = null;
        _voltageLabels = [];

        QueueRedraw();
    }

    public void FitToSchematic()
    {
        if (_session is null || Size.X <= 0 || Size.Y <= 0)
        {
            return;
        }

        _camera.Fit(SchematicBounds.Of(_session.Schematic, _session.Catalog, _symbols), Size);
        _fitted = true;

        AfterCameraChanged();
    }

    public void ZoomAtCentre(float factor)
    {
        _camera.ZoomBy(factor, _pointer ?? (Size / 2f));

        AfterCameraChanged();
    }

    public void PanModifierReleased()
    {
        if (_panning)
        {
            _panning = false;
        }
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), VisualTokens.Background);

        if (ShowGrid)
        {
            DrawGridDots();
        }

        if (_session is null)
        {
            return;
        }

        var view = _session.View;

        DrawComponents(_session);
        DrawWires(_session);
        DrawDanglingEnds();
        DrawJunctions();
        DrawSelection(_session, view);
        DrawHoveredNet(_session);
        DrawGhost(view);
        DrawWirePreview(view);
        DrawLabels(_session);
        DrawVoltageLabels();
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

    private void OnResized()
    {
        if (_fitted)
        {
            AfterCameraChanged();
            return;
        }

        FitToSchematic();
    }

    private void OnMouseExited()
    {
        _pointer = null;

        SetHoveredNet(null);
        SetHoveredComponent(null);
    }

    private void HandleButton(InputEventMouseButton button)
    {
        _pointer = button.Position;

        switch (button.ButtonIndex)
        {
            case MouseButton.WheelUp when button.Pressed:
                _camera.ZoomBy(CanvasMetrics.ZoomStep, button.Position);
                AfterCameraChanged();
                break;

            case MouseButton.WheelDown when button.Pressed:
                _camera.ZoomBy(1f / CanvasMetrics.ZoomStep, button.Position);
                AfterCameraChanged();
                break;

            case MouseButton.Middle:
                _panning = button.Pressed;
                break;

            case MouseButton.Left:
                HandleLeftButton(button);
                break;

            case MouseButton.Right when button.Pressed:
                UpdateTolerance();
                _session?.SecondaryPressed(_camera.ToGrid(button.Position));
                Report();
                break;
        }
    }

    private void HandleLeftButton(InputEventMouseButton button)
    {
        if (_session is null)
        {
            return;
        }

        if (button.Pressed && PanModifierHeld)
        {
            _panning = true;
            return;
        }

        if (!button.Pressed && _panning)
        {
            _panning = false;
            return;
        }

        UpdateTolerance();

        if (button.Pressed)
        {
            _session.PrimaryPressed(_camera.ToGrid(button.Position));

            if (button.DoubleClick)
            {
                _session.DoubleClicked();
            }
        }
        else
        {
            _session.PrimaryReleased(_camera.ToGrid(button.Position));
        }

        Report();
    }

    private void HandleMotion(InputEventMouseMotion motion)
    {
        _pointer = motion.Position;

        if (_panning)
        {
            _camera.PanBy(motion.Relative);
            AfterCameraChanged();

            return;
        }

        RefreshPointer();
    }

    private void AfterCameraChanged()
    {
        RefreshPointer();
        QueueRedraw();
    }

    private void RefreshPointer()
    {
        if (_session is null || _pointer is not { } position)
        {
            return;
        }

        UpdateTolerance();

        _session.PointerMoved(_camera.ToGrid(position));

        SetHoveredNet(NetUnder(position));
        SetHoveredComponent(ComponentUnder(position));
        Report();
    }

    private void UpdateTolerance()
    {
        if (_session is not null)
        {
            _session.HitTolerance = CanvasMetrics.HoverTolerancePixels / _camera.PixelsPerGridUnit;
            _session.DragThreshold = CanvasMetrics.DragThresholdPixels / _camera.PixelsPerGridUnit;
        }
    }

    private Net? NetUnder(Vector2 position)
    {
        if (_session is null)
        {
            return null;
        }

        var hit = SchematicHitTester.Nearest(
            _session.Schematic,
            _session.Catalog,
            _camera.ToGrid(position),
            CanvasMetrics.HoverTolerancePixels / _camera.PixelsPerGridUnit);

        return NetOf(hit);
    }

    private ComponentId? ComponentUnder(Vector2 position)
    {
        if (_session is null)
        {
            return null;
        }

        return SchematicHitTester.Nearest(
            _session.Schematic,
            _session.Catalog,
            _symbols,
            _camera.ToGrid(position),
            CanvasMetrics.HoverTolerancePixels / _camera.PixelsPerGridUnit)?.Component;
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

    private void SetHoveredComponent(ComponentId? component)
    {
        if (component == _hoveredComponent)
        {
            return;
        }

        _hoveredComponent = component;
        HoveredComponentChanged?.Invoke(component);
    }

    private void Report()
    {
        ViewChanged?.Invoke();
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

    private void DrawComponents(EditorSession session)
    {
        foreach (var component in session.Schematic.Components.Values)
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
                    StrokeOf(component.Id));
            }
        }
    }

    private void DrawWires(EditorSession session)
    {
        foreach (var wire in session.Schematic.Wires.Values)
        {
            DrawWire(wire, VisualTokens.Stroke);
        }
    }

    private Color StrokeOf(ComponentId id) =>
        _operatingPoint?.HasRatingWarning(id) == true ? VisualTokens.Error : VisualTokens.Stroke;

    private void DrawWire(Wire wire, Color color) => DrawLine(
        _camera.ToScreen(wire.Start), _camera.ToScreen(wire.End), color, CanvasMetrics.StrokeWidth);

    private void DrawDanglingEnds()
    {
        if (_connectivity is null)
        {
            return;
        }

        var side = CanvasMetrics.DanglingEndSizePixels;

        foreach (var diagnostic in _connectivity.Diagnostics)
        {
            if (diagnostic.Code != ConnectivityDiagnosticCode.DanglingWireEnd)
            {
                continue;
            }

            foreach (var position in diagnostic.Positions)
            {
                var center = _camera.ToScreen(position);

                DrawRect(
                    new Rect2(center - (Vector2.One * (side / 2f)), new Vector2(side, side)),
                    VisualTokens.Stroke,
                    filled: false,
                    CanvasMetrics.StrokeWidth);
            }
        }
    }

    private void DrawJunctions()
    {
        if (_connectivity is null)
        {
            return;
        }

        foreach (var junction in _connectivity.Junctions)
        {
            DrawCircle(
                _camera.ToScreen(junction), CanvasMetrics.JunctionRadius, VisualTokens.Stroke);
        }
    }

    private void DrawSelection(EditorSession session, EditorView view)
    {
        if (view.Selection.Component is { } id
            && session.Schematic.TryGetComponent(id, out var component)
            && _symbols.TryGet(component.DefinitionId, SymbolStyle.Iec, out var symbol))
        {
            SymbolRenderer.Draw(
                this,
                Font,
                _camera,
                symbol,
                component.Position,
                component.Orientation,
                VisualTokens.Selection);
        }

        if (view.Selection.Wire is { } wireId && session.Schematic.TryGetWire(wireId, out var wire))
        {
            DrawWire(wire, VisualTokens.Selection);
        }
    }

    private void DrawHoveredNet(EditorSession session)
    {
        if (_hoveredNet is not { } net)
        {
            return;
        }

        foreach (var id in net.Wires)
        {
            if (session.Schematic.TryGetWire(id, out var wire))
            {
                DrawWire(wire, VisualTokens.NetHighlight);
            }
        }

        foreach (var pin in net.Pins)
        {
            DrawPin(session, pin);
        }
    }

    private void DrawGhost(EditorView view)
    {
        if (view.Ghost is not { } ghost
            || !_symbols.TryGet(ghost.DefinitionId, SymbolStyle.Iec, out var symbol))
        {
            return;
        }

        var colour = VisualTokens.Selection;

        SymbolRenderer.Draw(
            this,
            Font,
            _camera,
            symbol,
            ghost.Position,
            ghost.Orientation,
            new Color(colour, CanvasMetrics.GhostOpacity));
    }

    private void DrawWirePreview(EditorView view)
    {
        foreach (var segment in view.WirePreview)
        {
            DrawLine(
                _camera.ToScreen(segment.Start),
                _camera.ToScreen(segment.End),
                VisualTokens.Selection,
                CanvasMetrics.StrokeWidth);
        }
    }

    private void DrawPin(EditorSession session, PinRef pin)
    {
        if (!session.Schematic.TryGetPinPositions(pin.ComponentId, session.Catalog, out var positions))
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

    private void DrawLabels(EditorSession session)
    {
        var fontSize = CanvasText.FontSize(_camera.Zoom);

        foreach (var component in session.Schematic.Components.Values)
        {
            if (!_symbols.TryGet(component.DefinitionId, SymbolStyle.Iec, out var symbol))
            {
                continue;
            }

            var layout = LabelPlacement.Of(symbol, component.Orientation);

            DrawLabel(component.Position, layout.Designator, component.Designator, fontSize);

            if (FirstValue(session, component) is { } value)
            {
                DrawLabel(component.Position, layout.Value, value, fontSize);
            }
        }
    }

    private void DrawLabel(GridPoint position, LabelAnchor anchor, string text, int fontSize) =>
        CanvasText.DrawAligned(
            this,
            Font,
            Screen(position, anchor.Position),
            anchor.Alignment,
            text,
            fontSize,
            VisualTokens.LabelText);

    private static string? FirstValue(EditorSession session, ComponentInstance component)
    {
        if (!session.Catalog.TryGetById(component.DefinitionId, out var definition))
        {
            return null;
        }

        var required = definition.Parameters.FirstOrDefault(parameter => parameter.IsRequired);

        if (required is null)
        {
            return null;
        }

        return component.Parameters.TryGetValue(required.Name, out var quantity)
            ? quantity.ToString()
            : null;
    }

    private void DrawVoltageLabels()
    {
        var fontSize = CanvasText.FontSize(_camera.Zoom);

        foreach (var label in _voltageLabels)
        {
            CanvasText.DrawPill(
                this,
                Font,
                _camera.ToScreen(label.Position),
                label.Text,
                fontSize,
                VisualTokens.LabelPill,
                VisualTokens.LabelText);
        }
    }

    private Vector2 Screen(GridPoint position, GridVector local) =>
        _camera.ToScreen(new GridVector(position.X + local.X, position.Y + local.Y));
}
