using Nodal.Circuit.Netlists;
using Nodal.Content.Components;
using Nodal.Foundation.Waveforms;

namespace Nodal.Simulation.Elaboration;

internal static class ParameterValues
{
    public static double Required(
        NetlistElement element, ComponentDefinition definition, string parameterName)
    {
        if (Optional(element, parameterName) is { } value)
        {
            return value;
        }

        var declared = definition.Parameters.FirstOrDefault(
            parameter => string.Equals(parameter.Name, parameterName, StringComparison.Ordinal));

        return declared?.Default?.Value ?? 0;
    }

    public static double? Optional(NetlistElement element, string parameterName) =>
        element.Parameters.TryGetValue(parameterName, out var value)
        && value.TryGetQuantity(out var quantity)
            ? quantity.Value
            : null;

    public static double? Positive(NetlistElement element, string parameterName) =>
        Optional(element, parameterName) is > 0 and var value ? value : null;

    public static Waveform? Function(NetlistElement element) =>
        element.Parameters.TryGetValue(CircuitParameters.Function, out var value)
        && value.TryGetWaveform(out var waveform)
        && waveform.IsValid
            ? waveform
            : null;
}
