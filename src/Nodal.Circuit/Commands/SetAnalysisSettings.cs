using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

public sealed record SetAnalysisSettings(AnalysisSettings Settings) : IEditCommand
{
    private const string ChangeDescription = "Change analysis settings";
    private const string ClearDescription = "Clear analysis settings";

    public CommandResult Apply(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        if (Settings is null)
        {
            return CommandResult.Failure(
                EditErrorCode.InvalidAnalysisSettings, "The analysis settings are missing.");
        }

        if (!Settings.IsValid)
        {
            return CommandResult.Failure(
                EditErrorCode.InvalidAnalysisSettings,
                "The transient settings need a positive stop time, and a positive output step "
                + "and maximum step when they are set.");
        }

        var description = Settings.IsEmpty ? ClearDescription : ChangeDescription;

        return Settings == schematic.Analysis
            ? CommandResult.Success(schematic, description)
            : CommandResult.Success(schematic.WithAnalysis(Settings), description);
    }
}
