using Nodal.Foundation.Geometry;

namespace Nodal.Content.Symbols;

public sealed record SymbolText(GridVector At, string Value) : SymbolPrimitive;
