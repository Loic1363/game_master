using Nodal.Content.Components;

namespace Nodal.Circuit.Schematics;

public static class ProbeTargets
{
    public static bool Exists(Probe probe, Schematic schematic)
    {
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentNullException.ThrowIfNull(schematic);

        return probe switch
        {
            PinVoltageProbe pin => schematic.Components.ContainsKey(pin.Pin.ComponentId),
            WireVoltageProbe wire => schematic.Wires.ContainsKey(wire.Wire),
            ComponentCurrentProbe current =>
                schematic.Components.ContainsKey(current.Component),
            _ => false,
        };
    }

    public static bool IsValid(Probe probe, Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        return probe is PinVoltageProbe pin
            ? HasPin(schematic, catalog, pin.Pin)
            : Exists(probe, schematic);
    }

    public static string Describe(Probe probe)
    {
        ArgumentNullException.ThrowIfNull(probe);

        return probe switch
        {
            PinVoltageProbe pin =>
                $"the pin {pin.Pin.PinName} of the component {pin.Pin.ComponentId}",
            WireVoltageProbe wire => $"the wire {wire.Wire}",
            ComponentCurrentProbe current => $"the component {current.Component}",
            _ => probe.ToString(),
        };
    }

    private static bool HasPin(Schematic schematic, ComponentCatalog catalog, PinRef pin)
    {
        if (!schematic.TryGetComponent(pin.ComponentId, out var component)
            || !catalog.TryGetById(component.DefinitionId, out var definition))
        {
            return false;
        }

        return definition.Pins.Any(
            candidate => string.Equals(candidate.Name, pin.PinName, StringComparison.Ordinal));
    }
}
