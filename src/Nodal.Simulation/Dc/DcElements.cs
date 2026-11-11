using System.Collections.Immutable;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Dc;

internal static class DcElements
{
    public static ImmutableArray<DcElement> Resolve(ElaboratedCircuit circuit)
    {
        var elements = ImmutableArray.CreateBuilder<DcElement>(circuit.Primitives.Length);
        var senseByPrimitive = SenseBranches.Of(circuit.Primitives);

        for (var index = 0; index < circuit.Primitives.Length; index++)
        {
            elements.Add(Resolve(circuit.Primitives[index], senseByPrimitive[index]));
        }

        return elements.ToImmutable();
    }

    private static DcElement Resolve(Primitive primitive, int senseElement) => new(
        primitive.Owner,
        primitive.Designator,
        primitive.Kind,
        primitive.Nodes,
        primitive.ConductionPaths,
        ConductanceOf(primitive),
        BranchOf(primitive),
        SourceCurrentOf(primitive),
        primitive.Value,
        ControlOf(primitive, senseElement),
        primitive.Switch);

    private static double? ConductanceOf(Primitive primitive) => primitive.Kind switch
    {
        PrimitiveKind.Resistor when primitive.Value > 0 => 1 / primitive.Value,
        PrimitiveKind.PushButton when primitive.ResistanceAt(0) > 0 =>
            1 / primitive.ResistanceAt(0),
        _ => null,
    };

    private static DcBranch? BranchOf(Primitive primitive) => primitive.Kind switch
    {
        PrimitiveKind.Resistor when primitive.Value <= 0 => DcBranch.Short,
        PrimitiveKind.PushButton when primitive.ResistanceAt(0) <= 0 => DcBranch.Short,
        PrimitiveKind.Inductor => DcBranch.Short,
        PrimitiveKind.VoltageSource => new DcBranch(primitive.ValueAt(0), 0),
        PrimitiveKind.Vcvs or PrimitiveKind.Ccvs => DcBranch.Short,
        _ => null,
    };

    private static double SourceCurrentOf(Primitive primitive) =>
        primitive.Kind == PrimitiveKind.CurrentSource ? primitive.ValueAt(0) : 0;

    private static DcControl? ControlOf(Primitive primitive, int senseElement) =>
        primitive.IsControlledSource
            ? new DcControl(
                primitive.ControlPlusNode, primitive.ControlMinusNode, senseElement)
            : null;
}
