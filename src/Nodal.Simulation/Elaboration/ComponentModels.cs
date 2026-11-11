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
    public const string DrainPin = "drain";
    public const string GatePin = "gate";
    public const string SourcePin = "source";
    public const string CollectorPin = "collector";
    public const string BasePin = "base";
    public const string EmitterPin = "emitter";
    public const string ControlPlusPin = "control-plus";
    public const string ControlMinusPin = "control-minus";
    public const string FirstContactPin = "a";
    public const string SecondContactPin = "b";

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
            ["nmos"] = ComponentKind.Nmos,
            ["pmos"] = ComponentKind.Pmos,
            ["npn"] = ComponentKind.Npn,
            ["pnp"] = ComponentKind.Pnp,
            ["vcvs"] = ComponentKind.Vcvs,
            ["vccs"] = ComponentKind.Vccs,
            ["cccs"] = ComponentKind.Cccs,
            ["ccvs"] = ComponentKind.Ccvs,
            ["switch"] = ComponentKind.Switch,
            ["push-button"] = ComponentKind.PushButton,
        };

    public static bool TryGetKind(string definitionId, out ComponentKind kind) =>
        KindByDefinitionId.TryGetValue(definitionId, out kind);

    public static ImmutableArray<string> PinsOf(ComponentKind kind) => kind switch
    {
        ComponentKind.VoltageSource or ComponentKind.CurrentSource => [PlusPin, MinusPin],
        ComponentKind.Diode => [AnodePin, CathodePin],
        ComponentKind.Ground => [],
        ComponentKind.Nmos or ComponentKind.Pmos => [DrainPin, SourcePin, GatePin],
        ComponentKind.Npn or ComponentKind.Pnp => [CollectorPin, EmitterPin, BasePin],
        ComponentKind.PushButton => [FirstContactPin, SecondContactPin],
        _ when IsControlledSource(kind) || kind == ComponentKind.Switch =>
            [PlusPin, MinusPin, ControlPlusPin, ControlMinusPin],
        _ => [FirstPassivePin, SecondPassivePin],
    };

    public static bool IsTransistor(ComponentKind kind) =>
        kind is ComponentKind.Nmos or ComponentKind.Pmos
            or ComponentKind.Npn or ComponentKind.Pnp;

    public static bool IsControlledSource(ComponentKind kind) =>
        kind is ComponentKind.Vcvs or ComponentKind.Vccs
            or ComponentKind.Cccs or ComponentKind.Ccvs;

    public static bool IsCurrentControlled(ComponentKind kind) =>
        kind is ComponentKind.Cccs or ComponentKind.Ccvs;

    public static bool IsSwitch(ComponentKind kind) =>
        kind is ComponentKind.Switch or ComponentKind.PushButton;

    public static bool ReportsItsPinCurrents(ComponentKind kind) =>
        IsTransistor(kind) || IsControlledSource(kind) || kind == ComponentKind.Switch;

    public static PrimitiveKind PrimitiveOf(ComponentKind kind) => kind switch
    {
        ComponentKind.Nmos => PrimitiveKind.Nmos,
        ComponentKind.Pmos => PrimitiveKind.Pmos,
        ComponentKind.Npn => PrimitiveKind.Npn,
        ComponentKind.Vcvs => PrimitiveKind.Vcvs,
        ComponentKind.Vccs => PrimitiveKind.Vccs,
        ComponentKind.Cccs => PrimitiveKind.Cccs,
        ComponentKind.Ccvs => PrimitiveKind.Ccvs,
        ComponentKind.Switch => PrimitiveKind.Switch,
        ComponentKind.PushButton => PrimitiveKind.PushButton,
        _ => PrimitiveKind.Pnp,
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
