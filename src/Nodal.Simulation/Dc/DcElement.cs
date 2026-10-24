using Nodal.Circuit.Schematics;

namespace Nodal.Simulation.Dc;

internal sealed record DcElement(
    ComponentId Id,
    string Designator,
    DcModelKind Kind,
    string FirstNet,
    string SecondNet,
    double? Conductance,
    DcBranch? Branch,
    double SourceCurrent,
    DcRatings Ratings)
{
    public bool Conducts => Branch is not null || Conductance is not null;

    public bool HasIdealBranch => Branch is { IsIdeal: true };
}
