using System.Collections.Immutable;
using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Foundation.Geometry;

namespace Nodal.Editor.Tools;

internal sealed class WireTool : IEditorTool
{
    private const string ToolLabel = "Wire";
    private const string WireDescription = "Add wire";

    private readonly EditorSession _session;

    private GridPoint? _start;
    private GridPoint _cursor;

    public WireTool(EditorSession session, GridPoint cursor)
    {
        _session = session;
        _cursor = cursor;
    }

    public EditorToolKind Kind => EditorToolKind.Wire;

    public string Label => ToolLabel;

    public ComponentGhost? Ghost => null;

    public ImmutableArray<WireSegment> Preview =>
        _start is { } start ? WireRouter.Preview(start, _cursor) : [];

    public void PointerMoved(GridVector point) => _cursor = GridSnap.Nearest(point);

    public void PrimaryPressed(GridVector point)
    {
        _cursor = GridSnap.Nearest(point);

        if (_start is not { } start)
        {
            _start = _cursor;

            return;
        }

        var segments = WireRouter.Route(_session.Schematic, start, _cursor);

        if (segments.Length > 0)
        {
            _session.Run(new CompositeCommand(
                WireDescription, segments.Select(segment => new AddWire(segment.Start, segment.End))));
        }

        _start = _cursor;
    }

    public void PrimaryReleased(GridVector point)
    {
    }

    public bool SecondaryPressed(GridVector point) => false;

    public void DoubleClicked() => _start = null;

    public bool Cancel()
    {
        if (_start is null)
        {
            return false;
        }

        _start = null;

        return true;
    }

    public void Rotate()
    {
    }

    public void Mirror()
    {
    }
}
