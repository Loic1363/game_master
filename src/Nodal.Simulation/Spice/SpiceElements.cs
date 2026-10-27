using Nodal.Simulation.Elaboration;

namespace Nodal.Simulation.Spice;

internal static class SpiceElements
{
    public static string Of(Primitive primitive) => primitive.IsTransistor
        ? Transistor(primitive)
        : Line(
            NameOf(primitive), primitive.PlusNode, primitive.MinusNode, ValueOf(primitive));

    public static string NameOf(Primitive primitive) => SpiceFormat.Name(
        primitive.Designator, TypeOf(primitive.Kind), SuffixOf(primitive.Role));

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

    private static char TypeOf(PrimitiveKind kind) => kind switch
    {
        PrimitiveKind.Resistor => SpiceElementType.Resistor,
        PrimitiveKind.Capacitor => SpiceElementType.Capacitor,
        PrimitiveKind.Inductor => SpiceElementType.Inductor,
        PrimitiveKind.VoltageSource => SpiceElementType.VoltageSource,
        PrimitiveKind.CurrentSource => SpiceElementType.CurrentSource,
        PrimitiveKind.Nmos or PrimitiveKind.Pmos => SpiceElementType.Mosfet,
        PrimitiveKind.Npn or PrimitiveKind.Pnp => SpiceElementType.Bipolar,
        _ => SpiceElementType.Diode,
    };

    private static string SuffixOf(PrimitiveRole role) => role switch
    {
        PrimitiveRole.Esr => SpiceFormat.EsrSuffix,
        PrimitiveRole.Esl => SpiceFormat.EslSuffix,
        PrimitiveRole.ParallelCapacitance => SpiceFormat.ParallelSuffix,
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
