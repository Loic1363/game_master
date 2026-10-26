using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;
using Nodal.Foundation.Waveforms;

namespace Nodal.Circuit.Commands;

public sealed record SetParameter(ComponentId Id, string ParameterName, ParameterValue Value)
    : IEditCommand
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

        if (Rejection(component, parameter) is { } rejection)
        {
            return rejection;
        }

        var description = $"Set {component.Designator} {parameter.Name} to {Value}";

        if (component.Parameters.TryGetValue(parameter.Name, out var current) && current == Value)
        {
            return CommandResult.Success(schematic, description);
        }

        var updated = component with { Parameters = component.Parameters.SetItem(parameter.Name, Value) };

        return CommandResult.Success(schematic.WithUpdatedComponent(updated), description);
    }

    private CommandResult? Rejection(ComponentInstance component, ParameterDefinition parameter)
    {
        if (Value.Kind != parameter.Kind)
        {
            return CommandResult.Failure(
                EditErrorCode.KindMismatch,
                $"The parameter {parameter.Name} of {component.Designator} expects a " +
                $"{Named(parameter.Kind)} value, not a {Named(Value.Kind)} one.");
        }

        if (Value.Unit != parameter.Unit)
        {
            return CommandResult.Failure(
                EditErrorCode.UnitMismatch,
                $"The parameter {parameter.Name} of {component.Designator} expects a value in " +
                $"{parameter.Unit}, not in {Value.Unit}.");
        }

        return Value.IsWaveform ? WaveformRejection(component, parameter) : QuantityRejection(component, parameter);
    }

    private CommandResult? WaveformRejection(
        ComponentInstance component, ParameterDefinition parameter)
    {
        if (Value.Waveform.Validate() == WaveformError.None)
        {
            return null;
        }

        return CommandResult.Failure(
            EditErrorCode.InvalidWaveform,
            $"The {parameter.Name} of {component.Designator} is not a valid "
            + $"{Value.Waveform.Kind} function.");
    }

    private CommandResult? QuantityRejection(
        ComponentInstance component, ParameterDefinition parameter)
    {
        var value = Value.Quantity;

        if (parameter.Minimum is { } minimum && value.Value < minimum.Value)
        {
            return CommandResult.Failure(
                EditErrorCode.ValueOutOfBounds,
                $"{value} is below the minimum {minimum} of {parameter.Name} on {component.Designator}.");
        }

        if (parameter.Maximum is { } maximum && value.Value > maximum.Value)
        {
            return CommandResult.Failure(
                EditErrorCode.ValueOutOfBounds,
                $"{value} is above the maximum {maximum} of {parameter.Name} on {component.Designator}.");
        }

        if (parameter.Series is { } series && !BelongsToSeries(series, value.Value))
        {
            return CommandResult.Failure(
                EditErrorCode.ValueNotInSeries,
                $"{value} does not belong to series {series}.");
        }

        return null;
    }

    private static string Named(ParameterKind kind) =>
        kind == ParameterKind.Waveform ? "waveform" : "quantity";

    private static bool BelongsToSeries(StandardSeries series, double value) =>
        value > 0 && ESeries.Contains(series, value);
}
