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

        if (!ParameterLookup.TryFind(schematic, catalog, Id, ParameterName, out var target, out var failure))
        {
            return failure;
        }

        var component = target.Component;
        var parameter = target.Parameter;

        if (Value.Unit != parameter.Unit)
        {
            return CommandResult.Failure(
                EditErrorCode.UnitMismatch,
                $"The parameter {parameter.Name} of {component.Designator} expects a value in " +
                $"{parameter.Unit}, not in {Value.Unit}.");
        }

        if (parameter.Minimum is { } minimum && Value.Value < minimum.Value)
        {
            return CommandResult.Failure(
                EditErrorCode.ValueOutOfBounds,
                $"{Value} is below the minimum {minimum} of {parameter.Name} on {component.Designator}.");
        }

        if (parameter.Maximum is { } maximum && Value.Value > maximum.Value)
        {
            return CommandResult.Failure(
                EditErrorCode.ValueOutOfBounds,
                $"{Value} is above the maximum {maximum} of {parameter.Name} on {component.Designator}.");
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
