using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;

namespace Nodal.Circuit.Commands;

public sealed record SetParameter(ComponentId Id, string ParameterName, Quantity Value) : IEditCommand
{
    public CommandResult Apply(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!schematic.TryGetComponent(Id, out var component))
        {
            return EditFailures.UnknownComponent(Id);
        }

        if (!catalog.TryGetById(component.DefinitionId, out var definition))
        {
            return EditFailures.UnknownDefinition(component.DefinitionId);
        }

        var parameter = definition.Parameters.FirstOrDefault(
            candidate => string.Equals(candidate.Name, ParameterName, StringComparison.Ordinal));

        if (parameter is null)
        {
            return CommandResult.Failure(
                EditErrorCode.UnknownParameter,
                $"{component.Designator} has no parameter named '{ParameterName}'.");
        }

        if (Value.Unit != parameter.Unit)
        {
            return CommandResult.Failure(
                EditErrorCode.UnitMismatch,
                $"The parameter {parameter.Name} of {component.Designator} expects a value in " +
                $"{parameter.Unit}, not in {Value.Unit}.");
        }

        if (parameter.Series is { } series && !BelongsToSeries(series, Value.Value))
        {
            return CommandResult.Failure(
                EditErrorCode.ValueNotInSeries,
                $"{Value} does not belong to series {series}.");
        }

        var description = $"Set {component.Designator} {parameter.Name} to {Value}";

        if (component.Parameters.TryGetValue(parameter.Name, out var current) && current == Value)
        {
            return CommandResult.Success(schematic, description);
        }

        var updated = component with { Parameters = component.Parameters.SetItem(parameter.Name, Value) };

        return CommandResult.Success(schematic.WithUpdatedComponent(updated), description);
    }

    private static bool BelongsToSeries(StandardSeries series, double value) =>
        value > 0 && ESeries.Contains(series, value);
}
