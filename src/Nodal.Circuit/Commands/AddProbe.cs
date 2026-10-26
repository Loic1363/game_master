using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

public sealed record AddProbe(Probe Probe) : IEditCommand
{
    public const string TooManyProbesMessage = "At most 8 probes.";

    private const string Description = "Add probe";

    public CommandResult Apply(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        if (Probe is null)
        {
            return CommandResult.Failure(EditErrorCode.InvalidProbe, "The probe is missing.");
        }

        if (!ProbeTargets.IsValid(Probe, schematic, catalog))
        {
            return CommandResult.Failure(
                EditErrorCode.InvalidProbe,
                $"A probe cannot point at {ProbeTargets.Describe(Probe)}, which the schematic "
                + "does not hold.");
        }

        var probes = schematic.Analysis.Probes;

        if (probes.Contains(Probe))
        {
            return CommandResult.Success(schematic, Description);
        }

        if (probes.Length >= AnalysisSettings.MaxProbes)
        {
            return CommandResult.Failure(EditErrorCode.TooManyProbes, TooManyProbesMessage);
        }

        return CommandResult.Success(
            schematic.WithAnalysis(schematic.Analysis with { Probes = probes.Add(Probe) }),
            Description);
    }
}
