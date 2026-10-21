using System.Collections.Immutable;
using Godot;

namespace Nodal.Game.Input;

public sealed record ShortcutBinding(
    string Action,
    string Label,
    string? PlacesDefinitionId,
    ImmutableArray<ShortcutStroke> Strokes)
{
    public ShortcutStroke First => Strokes[0];

    public Shortcut ToShortcut()
    {
        var shortcut = new Shortcut();
        var events = new Godot.Collections.Array();

        foreach (var stroke in Strokes)
        {
            events.Add(stroke.ToEvent());
        }

        shortcut.Events = events;

        return shortcut;
    }
}
