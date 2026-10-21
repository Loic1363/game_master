using Nodal.Foundation.Geometry;

namespace Nodal.Content.Symbols;

public sealed record SymbolArc(
    GridVector Center, double Radius, double StartDegrees, double SweepDegrees) : SymbolPrimitive;
