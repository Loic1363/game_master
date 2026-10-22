using Nodal.Circuit.Commands;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Editing;

public sealed class EditHistory
{
    private readonly Stack<HistoryEntry> _undo = new();
    private readonly Stack<HistoryEntry> _redo = new();

    private Schematic _saved;

    public EditHistory(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        Schematic = schematic;
        Catalog = catalog;
        _saved = schematic;
    }

    public event Action? Changed;

    public Schematic Schematic { get; private set; }

    public ComponentCatalog Catalog { get; }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public bool IsDirty => !ReferenceEquals(Schematic, _saved);

    public string? UndoDescription => CanUndo ? _undo.Peek().Description : null;

    public string? RedoDescription => CanRedo ? _redo.Peek().Description : null;

    public void MarkSaved()
    {
        if (!IsDirty)
        {
            return;
        }

        _saved = Schematic;
        Changed?.Invoke();
    }

    public CommandResult Execute(IEditCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var result = command.Apply(Schematic, Catalog);

        if (!result.Succeeded || ReferenceEquals(result.Schematic, Schematic))
        {
            return result;
        }

        _undo.Push(new HistoryEntry(Schematic, result.Description));
        _redo.Clear();
        Schematic = result.Schematic;
        Changed?.Invoke();

        return result;
    }

    public bool Undo()
    {
        if (!CanUndo)
        {
            return false;
        }

        var entry = _undo.Pop();
        _redo.Push(new HistoryEntry(Schematic, entry.Description));
        Schematic = entry.Schematic;
        Changed?.Invoke();

        return true;
    }

    public bool Redo()
    {
        if (!CanRedo)
        {
            return false;
        }

        var entry = _redo.Pop();
        _undo.Push(new HistoryEntry(Schematic, entry.Description));
        Schematic = entry.Schematic;
        Changed?.Invoke();

        return true;
    }

    private sealed record HistoryEntry(Schematic Schematic, string Description);
}
