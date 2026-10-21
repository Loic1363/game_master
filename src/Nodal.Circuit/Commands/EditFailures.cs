using Nodal.Circuit.Schematics;

namespace Nodal.Circuit.Commands;

internal static class EditFailures
{
    public static CommandResult UnknownComponent(ComponentId id) => CommandResult.Failure(
        EditErrorCode.UnknownComponent, $"No component carries the id {id}.");

    public static CommandResult UnknownWire(WireId id) => CommandResult.Failure(
        EditErrorCode.UnknownWire, $"No wire carries the id {id}.");

    public static CommandResult UnknownDefinition(string definitionId) => CommandResult.Failure(
        EditErrorCode.UnknownDefinition, $"The catalog has no component definition '{definitionId}'.");
}
