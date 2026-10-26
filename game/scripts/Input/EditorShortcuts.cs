using System.Collections.Immutable;
using Godot;

namespace Nodal.Game.Input;

public static class EditorShortcuts
{
    public static ImmutableArray<ShortcutBinding> All { get; } =
    [
        Place(EditorAction.PlaceResistor, "Place Resistor", "resistor", Key.R),
        Place(EditorAction.PlaceCapacitor, "Place Capacitor", "capacitor", Key.C),
        Place(EditorAction.PlaceInductor, "Place Inductor", "inductor", Key.L),
        Place(EditorAction.PlaceDiode, "Place Diode", "diode", Key.D),
        Place(EditorAction.PlaceGround, "Place Ground", "ground", Key.G),
        Command(EditorAction.Open, "Open", new ShortcutStroke(Key.O, Control: true)),
        Command(EditorAction.Save, "Save", new ShortcutStroke(Key.S, Control: true)),
        Command(EditorAction.SaveAs, "Save as", new ShortcutStroke(Key.S, Control: true, Shift: true)),
        Command(EditorAction.Properties, "Properties", new ShortcutStroke(Key.F4)),
        Command(EditorAction.WireTool, "Wire tool", new ShortcutStroke(Key.W), new ShortcutStroke(Key.F3)),
        Command(EditorAction.ProbeTool, "Probe tool", new ShortcutStroke(Key.P)),
        Command(EditorAction.Delete, "Delete", new ShortcutStroke(Key.Delete), new ShortcutStroke(Key.F5)),
        Command(EditorAction.Rotate, "Rotate", new ShortcutStroke(Key.R, Control: true)),
        Command(EditorAction.Mirror, "Mirror", new ShortcutStroke(Key.E, Control: true)),
        Command(EditorAction.Undo, "Undo", new ShortcutStroke(Key.Z, Control: true)),
        Command(
            EditorAction.Redo,
            "Redo",
            new ShortcutStroke(Key.Y, Control: true),
            new ShortcutStroke(Key.Z, Control: true, Shift: true)),
        Command(EditorAction.Cancel, "Cancel", new ShortcutStroke(Key.Escape)),
        Command(
            EditorAction.DcOperatingPoint,
            "DC operating point",
            new ShortcutStroke(Key.F9)),
        Command(
            EditorAction.Transient,
            "Transient analysis",
            new ShortcutStroke(Key.F10)),
        Command(EditorAction.Fit, "Fit to schematic", new ShortcutStroke(Key.Home)),
        Command(EditorAction.ZoomIn, "Zoom in", new ShortcutStroke(Key.Equal, Control: true)),
        Command(EditorAction.ZoomOut, "Zoom out", new ShortcutStroke(Key.Minus, Control: true)),
    ];

    public static void Register()
    {
        foreach (var binding in All)
        {
            if (InputMap.HasAction(binding.Action))
            {
                InputMap.EraseAction(binding.Action);
            }

            InputMap.AddAction(binding.Action);

            foreach (var stroke in binding.Strokes)
            {
                InputMap.ActionAddEvent(binding.Action, stroke.ToEvent());
            }
        }
    }

    public static ShortcutBinding? ForAction(string action) =>
        All.FirstOrDefault(binding => string.Equals(binding.Action, action, StringComparison.Ordinal));

    public static ShortcutBinding? ForPlacement(string definitionId) =>
        All.FirstOrDefault(binding =>
            string.Equals(binding.PlacesDefinitionId, definitionId, StringComparison.Ordinal));

    public static string Describe(ShortcutBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);

        return string.Join(", ", binding.Strokes.Select(Describe));
    }

    public static string Describe(ShortcutStroke stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);

        var text = stroke.Key switch
        {
            Key.Equal => "=",
            Key.Minus => "-",
            _ => OS.GetKeycodeString(stroke.Key),
        };

        if (stroke.Shift)
        {
            text = "Shift+" + text;
        }

        return stroke.Control ? "Ctrl+" + text : text;
    }

    private static ShortcutBinding Place(string action, string label, string definitionId, Key key) =>
        new(action, label, definitionId, [new ShortcutStroke(key)]);

    private static ShortcutBinding Command(string action, string label, params ShortcutStroke[] strokes) =>
        new(action, label, null, [.. strokes]);
}
