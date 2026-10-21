using System.Diagnostics.CodeAnalysis;
using Nodal.Circuit.Schematics;

namespace Nodal.Circuit.Commands;

public sealed class CommandResult
{
    private CommandResult(Schematic? schematic, string? description, EditError? error)
    {
        Schematic = schematic;
        Description = description;
        Error = error;
    }

    [MemberNotNullWhen(true, nameof(Schematic))]
    [MemberNotNullWhen(true, nameof(Description))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool Succeeded => Error is null;

    public Schematic? Schematic { get; }

    public string? Description { get; }

    public EditError? Error { get; }

    public static CommandResult Success(Schematic schematic, string description)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentException.ThrowIfNullOrEmpty(description);

        return new CommandResult(schematic, description, null);
    }

    public static CommandResult Failure(EditErrorCode code, string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);

        if (code == EditErrorCode.None)
        {
            throw new ArgumentException("A failure needs a real error code.", nameof(code));
        }

        return new CommandResult(null, null, new EditError(code, message));
    }
}
