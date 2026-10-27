using System.Collections.Immutable;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Dc;

internal static class DcElements
{
    public static ImmutableArray<DcElement> Resolve(ElaboratedCircuit circuit)
    {
        var elements = ImmutableArray.CreateBuilder<DcElement>(circuit.Primitives.Length);

        foreach (var primitive in circuit.Primitives)
        {
            elements.Add(Resolve(primitive));
        }

        return elements.ToImmutable();
    }

    private static DcElement Resolve(Primitive primitive) => new(
        primitive.Owner,
        primitive.Designator,
        primitive.Kind,
        primitive.Nodes,
        primitive.ConductionPaths,
        ConductanceOf(primitive),
        BranchOf(primitive),
        SourceCurrentOf(primitive));

    private static double? ConductanceOf(Primitive primitive) =>
        primitive.Kind == PrimitiveKind.Resistor && primitive.Value > 0
            ? 1 / primitive.Value
            : null;

    private static DcBranch? BranchOf(Primitive primitive) => primitive.Kind switch
    {
        PrimitiveKind.Resistor when primitive.Value <= 0 => DcBranch.Short,
        PrimitiveKind.Inductor => DcBranch.Short,
        PrimitiveKind.VoltageSource => new DcBranch(primitive.ValueAt(0), 0),
        _ => null,
    };

    private static double SourceCurrentOf(Primitive primitive) =>
        primitive.Kind == PrimitiveKind.CurrentSource ? primitive.ValueAt(0) : 0;
}
