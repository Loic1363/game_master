using Nodal.Foundation.Geometry;

namespace Nodal.Content.Symbols;

public sealed record SymbolLine(GridVector From, GridVector To) : SymbolPrimitive;
