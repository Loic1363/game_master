using System.Collections.Immutable;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Spice;

internal static class SpiceModels
{
    public static string? NameOf(PrimitiveKind kind) => kind switch
    {
        PrimitiveKind.Diode => SpiceFormat.DiodeModelName,
        PrimitiveKind.Nmos => SpiceFormat.NmosModelName,
        PrimitiveKind.Pmos => SpiceFormat.PmosModelName,
        PrimitiveKind.Npn => SpiceFormat.NpnModelName,
        PrimitiveKind.Pnp => SpiceFormat.PnpModelName,
        _ => null,
    };

    public static ImmutableArray<string> LinesFor(ElaboratedCircuit circuit)
    {
        var kinds = circuit.Primitives.Select(primitive => primitive.Kind).ToHashSet();

        return
        [
            .. Declarations().Where(declaration => kinds.Contains(declaration.Kind))
                .Select(declaration => declaration.Line),
        ];
    }

    private static IEnumerable<(PrimitiveKind Kind, string Line)> Declarations()
    {
        yield return (PrimitiveKind.Diode, SpiceFormat.DiodeModelLine);
        yield return (PrimitiveKind.Nmos, SpiceFormat.NmosModelLine);
        yield return (PrimitiveKind.Pmos, SpiceFormat.PmosModelLine);
        yield return (PrimitiveKind.Npn, SpiceFormat.NpnModelLine);
        yield return (PrimitiveKind.Pnp, SpiceFormat.PnpModelLine);
    }
}
