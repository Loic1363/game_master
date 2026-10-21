using Nodal.Circuit.Geometry;

namespace Nodal.Editor;

public sealed record ComponentGhost(string DefinitionId, GridPoint Position, Orientation Orientation);
