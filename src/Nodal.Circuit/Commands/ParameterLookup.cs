using System.Diagnostics.CodeAnalysis;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

internal static class ParameterLookup
{
    public static bool TryFind(
        Schematic schematic,
        ComponentCatalog catalog,
        ComponentId id,
        string parameterName,
        [NotNullWhen(true)] out ParameterTarget? target,
        [NotNullWhen(false)] out CommandResult? failure)
    {
        target = null;

        if (!schematic.TryGetComponent(id, out var component))
        {
            failure = EditFailures.UnknownComponent(id);

            return false;
        }

        if (!catalog.TryGetById(component.DefinitionId, out var definition))
        {
            failure = EditFailures.UnknownDefinition(component.DefinitionId);

            return false;
        }

        var parameter = definition.Parameters.FirstOrDefault(
            candidate => string.Equals(candidate.Name, parameterName, StringComparison.Ordinal));

        if (parameter is null)
        {
            failure = CommandResult.Failure(
                EditErrorCode.UnknownParameter,
                $"{component.Designator} has no parameter named '{parameterName}'.");

            return false;
        }

        target = new ParameterTarget(component, parameter);
        failure = null;

        return true;
    }
}
