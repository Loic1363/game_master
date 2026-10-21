using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Nodal.Circuit.Geometry;
using Nodal.Content.Components;

namespace Nodal.Circuit.Schematics;

public sealed class Schematic
{
    private Schematic(
        ImmutableSortedDictionary<ComponentId, ComponentInstance> components,
        ImmutableSortedDictionary<WireId, Wire> wires,
        ComponentId nextComponentId,
        WireId nextWireId)
    {
        Components = components;
        Wires = wires;
        NextComponentId = nextComponentId;
        NextWireId = nextWireId;
    }

    public static Schematic Empty { get; } = new(
        ImmutableSortedDictionary<ComponentId, ComponentInstance>.Empty,
        ImmutableSortedDictionary<WireId, Wire>.Empty,
        ComponentId.First,
        WireId.First);

    public ImmutableSortedDictionary<ComponentId, ComponentInstance> Components { get; }

    public ImmutableSortedDictionary<WireId, Wire> Wires { get; }

    public ComponentId NextComponentId { get; }

    public WireId NextWireId { get; }

    public bool TryGetComponent(ComponentId id, [NotNullWhen(true)] out ComponentInstance? component) =>
        Components.TryGetValue(id, out component);

    public bool TryGetWire(WireId id, [NotNullWhen(true)] out Wire? wire) =>
        Wires.TryGetValue(id, out wire);

    public bool TryGetPinPositions(
        ComponentId id, ComponentCatalog catalog, out ImmutableArray<PinPosition> positions)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        if (!TryGetComponent(id, out var component) ||
            !catalog.TryGetById(component.DefinitionId, out var definition))
        {
            positions = default;
            return false;
        }

        var builder = ImmutableArray.CreateBuilder<PinPosition>(definition.Pins.Count);

        foreach (var pin in definition.Pins)
        {
            var offset = component.Orientation.Transform(new GridPoint(pin.X, pin.Y));
            builder.Add(new PinPosition(pin.Name, component.Position + offset));
        }

        positions = builder.MoveToImmutable();
        return true;
    }

    internal Schematic WithComponent(ComponentInstance component)
    {
        if (component.Id != NextComponentId)
        {
            throw new ArgumentException(
                $"A new component must carry the next id {NextComponentId}, not {component.Id}.",
                nameof(component));
        }

        return new Schematic(
            Components.Add(component.Id, component), Wires, NextComponentId.Next(), NextWireId);
    }

    internal Schematic WithUpdatedComponent(ComponentInstance component)
    {
        if (!Components.ContainsKey(component.Id))
        {
            throw new ArgumentException(
                $"No component carries the id {component.Id}.", nameof(component));
        }

        return new Schematic(
            Components.SetItem(component.Id, component), Wires, NextComponentId, NextWireId);
    }

    internal Schematic WithoutComponent(ComponentId id)
    {
        if (!Components.ContainsKey(id))
        {
            throw new ArgumentException($"No component carries the id {id}.", nameof(id));
        }

        return new Schematic(Components.Remove(id), Wires, NextComponentId, NextWireId);
    }

    internal Schematic WithWire(Wire wire)
    {
        ArgumentNullException.ThrowIfNull(wire);

        if (wire.Id != NextWireId)
        {
            throw new ArgumentException(
                $"A new wire must carry the next id {NextWireId}, not {wire.Id}.", nameof(wire));
        }

        return new Schematic(Components, Wires.Add(wire.Id, wire), NextComponentId, NextWireId.Next());
    }

    internal Schematic WithoutWire(WireId id)
    {
        if (!Wires.ContainsKey(id))
        {
            throw new ArgumentException($"No wire carries the id {id}.", nameof(id));
        }

        return new Schematic(Components, Wires.Remove(id), NextComponentId, NextWireId);
    }
}
