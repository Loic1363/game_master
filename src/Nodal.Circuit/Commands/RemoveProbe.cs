using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

public sealed record RemoveProbe(Probe Probe) : IEditCommand
{
    private const string Description = "Remove probe";

    public CommandResult Apply(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        if (Probe is null)
        {
            return CommandResult.Failure(EditErrorCode.InvalidProbe, "The probe is missing.");
        }

        var probes = schematic.Analysis.Probes;
        var index = probes.IndexOf(Probe);

        if (index < 0)
        {
            return CommandResult.Success(schematic, Description);
        }

        return CommandResult.Success(
            schematic.WithAnalysis(schematic.Analysis with { Probes = probes.RemoveAt(index) }),
            Description);
    }
}
