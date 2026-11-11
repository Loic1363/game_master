using System.Collections.Immutable;
using Nodal.Circuit.Schematics;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Dc;

internal sealed record DcElement(
    ComponentId Id,
    string Designator,
    PrimitiveKind Kind,
    ImmutableArray<string> Nodes,
    ImmutableArray<(string From, string To)> ConductionPaths,
    double? Conductance,
    DcBranch? Branch,
    double SourceCurrent,
    double Gain = 0,
    DcControl? Control = null,
    SwitchSettings? Switch = null)
{
    public string FirstNode => Nodes[0];

    public string SecondNode => Nodes[1];

    public bool IsDiode => Kind == PrimitiveKind.Diode;

    public bool IsNonlinear =>
        Kind is PrimitiveKind.Diode
            or PrimitiveKind.Nmos
            or PrimitiveKind.Pmos
            or PrimitiveKind.Npn
            or PrimitiveKind.Pnp
            or PrimitiveKind.Switch;

    public bool IsControlledCurrent => Kind is PrimitiveKind.Vccs or PrimitiveKind.Cccs;

    public bool Conducts => Branch is not null || Conductance is not null || IsNonlinear;

    public bool HasIdealBranch => Branch is { IsIdeal: true };
}
