using System.Collections.Immutable;
using Nodal.Circuit.Netlists;

namespace Nodal.Simulation.Elaboration;

internal static class ComponentModels
{
    public const string FirstPassivePin = "1";
    public const string SecondPassivePin = "2";
    public const string PlusPin = "plus";
    public const string MinusPin = "minus";
    public const string AnodePin = "anode";
    public const string CathodePin = "cathode";

    private static readonly Dictionary<string, ComponentKind> KindByDefinitionId =
        new(StringComparer.Ordinal)
        {
            ["resistor"] = ComponentKind.Resistor,
            ["capacitor"] = ComponentKind.Capacitor,
            ["inductor"] = ComponentKind.Inductor,
            ["voltage-source"] = ComponentKind.VoltageSource,
            ["current-source"] = ComponentKind.CurrentSource,
            ["diode"] = ComponentKind.Diode,
            ["ground"] = ComponentKind.Ground,
        };

    public static bool TryGetKind(string definitionId, out ComponentKind kind) =>
        KindByDefinitionId.TryGetValue(definitionId, out kind);

    public static ImmutableArray<string> PinsOf(ComponentKind kind) => kind switch
    {
        ComponentKind.VoltageSource or ComponentKind.CurrentSource => [PlusPin, MinusPin],
        ComponentKind.Diode => [AnodePin, CathodePin],
        ComponentKind.Ground => [],
        _ => [FirstPassivePin, SecondPassivePin],
    };

    public static bool IsSupported(NetlistElement element, out ComponentKind kind) =>
        TryGetKind(element.DefinitionId, out kind)
        && PinsOf(kind).All(name => HasPin(element, name));

    public static string NetOf(NetlistElement element, string pinName) =>
        element.Pins.First(
            pin => string.Equals(pin.PinName, pinName, StringComparison.Ordinal)).NetName;

    private static bool HasPin(NetlistElement element, string pinName) =>
        element.Pins.Any(pin => string.Equals(pin.PinName, pinName, StringComparison.Ordinal));
}
