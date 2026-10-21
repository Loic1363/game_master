using Godot;

namespace Nodal.Game.Input;

public sealed record ShortcutStroke(Key Key, bool Control = false, bool Shift = false)
{
    public bool IsUnmodifiedLetter => !Control && !Shift && Key >= Key.A && Key <= Key.Z;

    public InputEventKey ToEvent() => new()
    {
        Keycode = Key,
        PhysicalKeycode = Godot.Key.None,
        CtrlPressed = Control,
        ShiftPressed = Shift,
        Pressed = true,
    };
}
