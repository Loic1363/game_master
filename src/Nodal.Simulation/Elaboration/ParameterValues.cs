using Nodal.Circuit.Netlists;
using Nodal.Content.Components;
using Nodal.Foundation.Schedules;
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

        return Declared(definition, parameterName)?.DefaultQuantity?.Value ?? 0;
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

    public static PressSchedule Schedule(
        NetlistElement element, ComponentDefinition definition, string parameterName)
    {
        if (element.Parameters.TryGetValue(parameterName, out var value)
            && value.TryGetSchedule(out var schedule)
            && schedule.IsValid)
        {
            return schedule;
        }

        return Declared(definition, parameterName)?.Default is { } fallback
            && fallback.TryGetSchedule(out var declared)
            && declared.IsValid
                ? declared
                : PressSchedule.Released;
    }

    private static ParameterDefinition? Declared(
        ComponentDefinition definition, string parameterName) =>
        definition.Parameters.FirstOrDefault(
            parameter => string.Equals(parameter.Name, parameterName, StringComparison.Ordinal));
}
