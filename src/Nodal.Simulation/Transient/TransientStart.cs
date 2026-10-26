using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Transient;

internal static class TransientStart
{
    public static bool TrySolve(
        ElaboratedCircuit circuit,
        TransientOptions options,
        [NotNullWhen(true)] out DcSolution? solution,
        [NotNullWhen(false)] out SimulationDiagnostic? failure) =>
        DcSolver.TrySolve(
            options.StartFromZero ? WithoutStoredEnergy(circuit) : circuit,
            DcOptionsFor(options),
            out solution,
            out failure);

    private static DcOptions DcOptionsFor(TransientOptions options) => new()
    {
        RelTol = options.RelTol,
        VnTol = options.VnTol,
        AbsTol = options.AbsTol,
        Gmin = options.Gmin,
    };

    private static ElaboratedCircuit WithoutStoredEnergy(ElaboratedCircuit circuit) => new(
        [.. circuit.Primitives.Select(AtRest)],
        circuit.Components,
        circuit.Nodes,
        circuit.ExternalNets);

    private static Primitive AtRest(Primitive primitive) => primitive.Kind switch
    {
        PrimitiveKind.Capacitor => primitive with
        {
            Kind = PrimitiveKind.VoltageSource,
            Value = 0,
            Function = null,
        },

        PrimitiveKind.Inductor => primitive with
        {
            Kind = PrimitiveKind.CurrentSource,
            Value = 0,
            Function = null,
        },

        _ => primitive,
    };
}
