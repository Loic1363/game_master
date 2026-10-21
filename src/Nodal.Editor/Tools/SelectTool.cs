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

    private ComponentId? _moving;
    private GridPoint _grabbedAt;
    private GridPoint _movedTo;

    public SelectTool(EditorSession session) => _session = session;

    public EditorToolKind Kind => EditorToolKind.Select;

    public string Label => ToolLabel;

    public ComponentGhost? Ghost => _moving is { } id && _session.Schematic.TryGetComponent(id, out var component)
        ? new ComponentGhost(component.DefinitionId, _movedTo, component.Orientation)
        : null;

    public ImmutableArray<WireSegment> Preview => [];

    public void PointerMoved(GridVector point)
    {
        if (_moving is null)
        {
            return;
        }

        _movedTo = Displaced(GridSnap.Nearest(point));
    }

    public void PrimaryPressed(GridVector point)
    {
        var previous = _session.Selection;
        var hit = _session.Pick(point);

        _session.Select(SelectionOf(hit));

        if (hit?.Component is { } component
            && previous.Component == component
            && _session.Schematic.TryGetComponent(component, out _))
        {
            _moving = component;
            _grabbedAt = GridSnap.Nearest(point);
            _movedTo = Displaced(_grabbedAt);
        }
    }

    public void PrimaryReleased(GridVector point)
    {
        if (_moving is not { } component)
        {
            return;
        }

        var destination = Displaced(GridSnap.Nearest(point));

        _moving = null;

        if (_session.Schematic.TryGetComponent(component, out var instance)
            && instance.Position != destination)
        {
            _session.Run(new MoveComponent(component, destination));
        }
    }

    public void DoubleClicked()
    {
    }

    public bool Cancel()
    {
        if (_moving is not null)
        {
            _moving = null;

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

    private void RunOnSelection(Func<ComponentId, IEditCommand> commandOf)
    {
        if (_session.Selection.Component is { } component)
        {
            _session.Run(commandOf(component));
        }
    }

    private GridPoint Displaced(GridPoint cursor)
    {
        if (_moving is not { } component
            || !_session.Schematic.TryGetComponent(component, out var instance))
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
