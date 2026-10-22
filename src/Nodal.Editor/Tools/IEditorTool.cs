using System.Collections.Immutable;
using Nodal.Foundation.Geometry;

namespace Nodal.Editor.Tools;

internal interface IEditorTool
{
    EditorToolKind Kind { get; }

    string Label { get; }

    ComponentGhost? Ghost { get; }

    ImmutableArray<WireSegment> Preview { get; }

    void PointerMoved(GridVector point);

    void PrimaryPressed(GridVector point);

    void PrimaryReleased(GridVector point);

    bool SecondaryPressed(GridVector point);

    void DoubleClicked();

    bool Cancel();

    void Rotate();

    void Mirror();
}
