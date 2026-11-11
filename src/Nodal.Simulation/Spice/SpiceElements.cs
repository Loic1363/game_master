using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Spice;

internal static class SpiceElements
{
    public static IEnumerable<string> LinesOf(Primitive primitive)
    {
        if (primitive.IsTransistor)
        {
            yield return Transistor(primitive);

            yield break;
        }

        if (primitive.IsControlledSource)
        {
            yield return ControlledSource(primitive);

            yield break;
        }

        if (primitive.Kind == PrimitiveKind.Switch)
        {
            yield return Switch(primitive);

            yield break;
        }

        if (primitive.Kind == PrimitiveKind.PushButton)
        {
            yield return Button(primitive);
            yield return ButtonControl(primitive);

            yield break;
        }

        yield return Line(
            NameOf(primitive), primitive.PlusNode, primitive.MinusNode, ValueOf(primitive));
    }

    public static string NameOf(Primitive primitive) => SpiceFormat.Name(
        primitive.Designator, TypeOf(primitive.Kind), SuffixOf(primitive.Role));

    public static string SenseNameOf(Primitive primitive) => SpiceFormat.Name(
        primitive.Designator, SpiceElementType.VoltageSource, SpiceFormat.SenseSuffix);

    private static string ControlNameOf(Primitive primitive) => SpiceFormat.Name(
        primitive.Designator, SpiceElementType.VoltageSource, SpiceFormat.ControlSuffix);

    private static string ControlNodeOf(Primitive primitive) =>
        primitive.Designator + SpiceFormat.ControlSuffix;

    private static string Transistor(Primitive primitive)
    {
        var name = NameOf(primitive);
        var drain = primitive.PlusNode;
        var source = primitive.MinusNode;
        var control = primitive.ControlNode;
        var model = SpiceModels.NameOf(primitive.Kind);

        if (primitive.IsBipolar)
        {
            return $"{name} {drain} {control} {source} {model}";
        }

        return $"{name} {drain} {control} {source} {source} {model} "
            + $"W={SpiceFormat.ChannelWidth} L={SpiceFormat.ChannelLength}";
    }

    private static string ControlledSource(Primitive primitive)
    {
        var name = NameOf(primitive);
        var plus = primitive.PlusNode;
        var minus = primitive.MinusNode;
        var value = SpiceFormat.Value(primitive.Value);

        return primitive.Kind switch
        {
            PrimitiveKind.Vcvs =>
                $"{name} {plus} {minus} {Controls(primitive)} {value}",

            PrimitiveKind.Vccs =>
                $"{name} {minus} {plus} {Controls(primitive)} {value}",

            PrimitiveKind.Cccs =>
                $"{name} {minus} {plus} {SenseNameOf(primitive)} {value}",

            _ => $"{name} {plus} {minus} {SenseNameOf(primitive)} {value}",
        };
    }

    private static string Switch(Primitive primitive) =>
        $"{NameOf(primitive)} {primitive.PlusNode} {primitive.MinusNode} "
        + $"{Controls(primitive)} {SpiceModels.SwitchNameOf(primitive)}";

    private static string Button(Primitive primitive) =>
        $"{NameOf(primitive)} {primitive.PlusNode} {primitive.MinusNode} "
        + $"{ControlNodeOf(primitive)} {SpiceFormat.GroundNode} "
        + SpiceModels.SwitchNameOf(primitive);

    private static string ButtonControl(Primitive primitive) =>
        $"{ControlNameOf(primitive)} {ControlNodeOf(primitive)} {SpiceFormat.GroundNode} "
        + SpiceWaveforms.Text(SpiceSchedules.PwlOf(primitive.Switch?.Presses));

    private static string Controls(Primitive primitive) =>
        $"{primitive.ControlPlusNode} {primitive.ControlMinusNode}";

    private static char TypeOf(PrimitiveKind kind) => kind switch
    {
        PrimitiveKind.Resistor => SpiceElementType.Resistor,
        PrimitiveKind.Capacitor => SpiceElementType.Capacitor,
        PrimitiveKind.Inductor => SpiceElementType.Inductor,
        PrimitiveKind.VoltageSource => SpiceElementType.VoltageSource,
        PrimitiveKind.CurrentSource => SpiceElementType.CurrentSource,
        PrimitiveKind.Nmos or PrimitiveKind.Pmos => SpiceElementType.Mosfet,
        PrimitiveKind.Npn or PrimitiveKind.Pnp => SpiceElementType.Bipolar,
        PrimitiveKind.Vcvs => SpiceElementType.Vcvs,
        PrimitiveKind.Vccs => SpiceElementType.Vccs,
        PrimitiveKind.Cccs => SpiceElementType.Cccs,
        PrimitiveKind.Ccvs => SpiceElementType.Ccvs,
        PrimitiveKind.Switch or PrimitiveKind.PushButton => SpiceElementType.Switch,
        _ => SpiceElementType.Diode,
    };

    private static string SuffixOf(PrimitiveRole role) => role switch
    {
        PrimitiveRole.Esr => SpiceFormat.EsrSuffix,
        PrimitiveRole.Esl => SpiceFormat.EslSuffix,
        PrimitiveRole.ParallelCapacitance => SpiceFormat.ParallelSuffix,
        PrimitiveRole.CurrentSense => SpiceFormat.SenseSuffix,
        _ => string.Empty,
    };

    private static string ValueOf(Primitive primitive)
    {
        if (primitive.Kind == PrimitiveKind.Diode)
        {
            return SpiceFormat.DiodeModelName;
        }

        var flipped = primitive.Kind == PrimitiveKind.CurrentSource;

        if (primitive.Function is { } function)
        {
            return SpiceWaveforms.Text(flipped ? function.Negated() : function);
        }

        return SpiceFormat.Value(flipped ? -primitive.Value : primitive.Value);
    }

    private static string Line(string name, string first, string second, string value) =>
        $"{name} {first} {second} {value}";
}
