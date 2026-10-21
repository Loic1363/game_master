using System.Collections.Immutable;
using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Foundation.Geometry;

namespace Nodal.Editor.Tools;

internal sealed class PlaceTool : IEditorTool
{
    private const string LabelPrefix = "Place ";

    private readonly EditorSession _session;
    private readonly string _definitionId;
    private readonly string _displayName;

    private Orientation _orientation = Orientation.Default;
    private GridPoint _position;

    public PlaceTool(EditorSession session, string definitionId, string displayName, GridPoint position)
    {
        _session = session;
        _definitionId = definitionId;
        _displayName = displayName;
        _position = position;
    }

    public EditorToolKind Kind => EditorToolKind.Place;

    public string Label => LabelPrefix + _displayName;

    public ComponentGhost? Ghost => new(_definitionId, _position, _orientation);

    public ImmutableArray<WireSegment> Preview => [];

    public void PointerMoved(GridVector point) => _position = GridSnap.Nearest(point);

    public void PrimaryPressed(GridVector point)
    {
        _position = GridSnap.Nearest(point);

        _session.Run(new AddComponent(_definitionId, _position, _orientation));
    }

    public void PrimaryReleased(GridVector point)
    {
    }

    public void DoubleClicked()
    {
    }

    public bool Cancel() => false;

    public void Rotate() => _orientation = _orientation.RotatedClockwise();

    public void Mirror() => _orientation = _orientation.Flipped();
}
