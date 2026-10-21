using System.Collections.Immutable;
using Nodal.Circuit.Geometry;

namespace Nodal.Editor;

public sealed record EditorView(
    EditorToolKind Tool,
    string ToolLabel,
    GridPoint Cursor,
    Selection Selection,
    ComponentGhost? Ghost,
    ImmutableArray<WireSegment> WirePreview);
