using System.Collections.Immutable;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

public sealed class CompositeCommand : IEditCommand
{
    private readonly ImmutableArray<IEditCommand> _commands;

    public CompositeCommand(string description, IEnumerable<IEditCommand> commands)
    {
        ArgumentException.ThrowIfNullOrEmpty(description);
        ArgumentNullException.ThrowIfNull(commands);

        Description = description;
        _commands = [.. commands];
    }

    public string Description { get; }

    public CommandResult Apply(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        var current = schematic;

        foreach (var command in _commands)
        {
            var result = command.Apply(current, catalog);

            if (!result.Succeeded)
            {
                return result;
            }

            current = result.Schematic;
        }

        return CommandResult.Success(current, Description);
    }
}
