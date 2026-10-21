using Nodal.Circuit.Commands;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Tests;

internal static class Edits
{
    public static Schematic Succeed(this IEditCommand command, Schematic schematic) =>
        command.Succeed(schematic, BaseCatalog.Catalog);

    public static Schematic Succeed(
        this IEditCommand command, Schematic schematic, ComponentCatalog catalog)
    {
        var result = command.Apply(schematic, catalog);

        return result.Succeeded
            ? result.Schematic
            : throw new InvalidOperationException($"The command failed with {result.Error}.");
    }

    public static EditError Reject(this IEditCommand command, Schematic schematic) =>
        command.Reject(schematic, BaseCatalog.Catalog);

    public static EditError Reject(
        this IEditCommand command, Schematic schematic, ComponentCatalog catalog)
    {
        var result = command.Apply(schematic, catalog);

        return result.Succeeded
            ? throw new InvalidOperationException("The command unexpectedly succeeded.")
            : result.Error;
    }

    public static string DescriptionOf(this IEditCommand command, Schematic schematic) =>
        command.DescriptionOf(schematic, BaseCatalog.Catalog);

    public static string DescriptionOf(
        this IEditCommand command, Schematic schematic, ComponentCatalog catalog)
    {
        var result = command.Apply(schematic, catalog);

        return result.Succeeded
            ? result.Description
            : throw new InvalidOperationException($"The command failed with {result.Error}.");
    }
}
