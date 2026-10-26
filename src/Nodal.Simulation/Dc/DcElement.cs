using Nodal.Circuit.Schematics;
using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Dc;

internal sealed record DcElement(
    ComponentId Id,
    string Designator,
    PrimitiveKind Kind,
    string FirstNode,
    string SecondNode,
    double? Conductance,
    DcBranch? Branch,
    double SourceCurrent)
{
    public bool IsNonlinear => Kind == PrimitiveKind.Diode;

    public bool Conducts => Branch is not null || Conductance is not null || IsNonlinear;

    public bool HasIdealBranch => Branch is { IsIdeal: true };
}
