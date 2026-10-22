using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

public sealed record ClearParameter(ComponentId Id, string ParameterName) : IEditCommand
{
    public CommandResult Apply(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!ParameterLookup.TryFind(schematic, catalog, Id, ParameterName, out var target, out var failure))
        {
            return failure;
        }

        var component = target.Component;
        var parameter = target.Parameter;

        if (parameter.IsRequired)
        {
            return CommandResult.Failure(
                EditErrorCode.ParameterIsRequired,
                $"The parameter {parameter.Name} of {component.Designator} is required, so it cannot be unset.");
        }

        var description = $"Clear {component.Designator} {parameter.Name}";

        if (!component.Parameters.ContainsKey(parameter.Name))
        {
            return CommandResult.Success(schematic, description);
        }

        var updated = component with { Parameters = component.Parameters.Remove(parameter.Name) };

        return CommandResult.Success(schematic.WithUpdatedComponent(updated), description);
    }
}
