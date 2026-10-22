using System.Collections.Immutable;
using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Hit;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Geometry;

namespace Nodal.Editor.Tools;

internal sealed class SelectTool : IEditorTool
{
    private const string ToolLabel = "Select";

    private readonly EditorSession _session;

    private ComponentId? _pressed;
    private bool _moving;
    private GridVector _pressedAt;
    private GridPoint _grabbedAt;
    private GridPoint _movedTo;

    public SelectTool(EditorSession session) => _session = session;

    public EditorToolKind Kind => EditorToolKind.Select;

    public string Label => ToolLabel;

    public ComponentGhost? Ghost =>
        _moving && _pressed is { } id && _session.Schematic.TryGetComponent(id, out var component)
            ? new ComponentGhost(component.DefinitionId, _movedTo, component.Orientation)
            : null;

    public ImmutableArray<WireSegment> Preview => [];

    public void PointerMoved(GridVector point)
    {
        if (_pressed is not { } component)
        {
            return;
        }

        if (!_moving)
        {
            if ((point - _pressedAt).Length <= _session.DragThreshold)
            {
                return;
            }

            _moving = true;
        }

        _movedTo = Displaced(component, GridSnap.Nearest(point));
    }

    public void PrimaryPressed(GridVector point)
    {
        var hit = _session.Pick(point);

        _session.Select(SelectionOf(hit));

        _pressed = null;
        _moving = false;

        if (hit?.Component is { } component && _session.Schematic.TryGetComponent(component, out _))
        {
            _pressed = component;
            _pressedAt = point;
            _grabbedAt = GridSnap.Nearest(point);
            _movedTo = Displaced(component, _grabbedAt);
        }
    }

    public void PrimaryReleased(GridVector point)
    {
        var dragged = _moving ? _pressed : null;

        _pressed = null;
        _moving = false;

        if (dragged is not { } component)
        {
            return;
        }

        var destination = Displaced(component, GridSnap.Nearest(point));

        if (_session.Schematic.TryGetComponent(component, out var instance)
            && instance.Position != destination)
        {
            _session.Run(new MoveComponent(component, destination));
        }
    }

    public bool SecondaryPressed(GridVector point)
    {
        if (_session.Pick(point)?.Component is not { } component)
        {
            return false;
        }

        Abandon();

        _session.Select(Selection.Of(component));
        _session.RequestProperties(component);

        return true;
    }

    public void DoubleClicked()
    {
        Abandon();

        if (_session.Selection.Component is { } component)
        {
            _session.RequestProperties(component);
        }
    }

    public bool Cancel()
    {
        var wasMoving = _moving;

        Abandon();

        if (wasMoving)
        {
            return true;
        }

        if (_session.Selection.IsEmpty)
        {
            return false;
        }

        _session.Select(Selection.None);

        return true;
    }

    public void Rotate() => RunOnSelection(id => new RotateComponent(id));

    public void Mirror() => RunOnSelection(id => new MirrorComponent(id));

    private void Abandon()
    {
        _pressed = null;
        _moving = false;
    }

    private void RunOnSelection(Func<ComponentId, IEditCommand> commandOf)
    {
        if (_session.Selection.Component is { } component)
        {
            _session.Run(commandOf(component));
        }
    }

    private GridPoint Displaced(ComponentId component, GridPoint cursor)
    {
        if (!_session.Schematic.TryGetComponent(component, out var instance))
        {
            return cursor;
        }

        return instance.Position + (cursor - _grabbedAt);
    }

    private static Selection SelectionOf(SchematicHit? hit)
    {
        if (hit?.Component is { } component)
        {
            return Selection.Of(component);
        }

        return hit?.Wire is { } wire ? Selection.Of(wire) : Selection.None;
    }
}
