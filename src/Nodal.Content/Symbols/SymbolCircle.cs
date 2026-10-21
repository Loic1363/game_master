using Nodal.Foundation.Geometry;

namespace Nodal.Content.Symbols;

public sealed record SymbolCircle(GridVector Center, double Radius, bool Filled) : SymbolPrimitive;
