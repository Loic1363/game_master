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
        WireId nextWireId,
        AnalysisSettings analysis)
    {
        Components = components;
        Wires = wires;
        NextComponentId = nextComponentId;
        NextWireId = nextWireId;
        Analysis = analysis;
    }

    public static Schematic Empty { get; } = new(
        ImmutableSortedDictionary<ComponentId, ComponentInstance>.Empty,
        ImmutableSortedDictionary<WireId, Wire>.Empty,
        ComponentId.First,
        WireId.First,
        AnalysisSettings.None);

    public ImmutableSortedDictionary<ComponentId, ComponentInstance> Components { get; }

    public ImmutableSortedDictionary<WireId, Wire> Wires { get; }

    public ComponentId NextComponentId { get; }

    public WireId NextWireId { get; }

    public AnalysisSettings Analysis { get; }

    public static Schematic Create(
        IEnumerable<ComponentInstance> components,
        IEnumerable<Wire> wires,
        ComponentId nextComponentId,
        WireId nextWireId,
        AnalysisSettings? analysis = null)
    {
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(wires);

        var componentsById = CollectComponents(components);
        var wiresById = CollectWires(wires);

        RequireNextComponentId(componentsById, nextComponentId);
        RequireNextWireId(wiresById, nextWireId);

        return new Schematic(
            componentsById,
            wiresById,
            nextComponentId,
            nextWireId,
            analysis ?? AnalysisSettings.None);
    }

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

        var builder = ImmutableArray.CreateBuilder<PinPosition>(definition.Pins.Length);

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
            Components.Add(component.Id, component),
            Wires,
            NextComponentId.Next(),
            NextWireId,
            Analysis);
    }

    internal Schematic WithUpdatedComponent(ComponentInstance component)
    {
        if (!Components.ContainsKey(component.Id))
        {
            throw new ArgumentException(
                $"No component carries the id {component.Id}.", nameof(component));
        }

        return new Schematic(
            Components.SetItem(component.Id, component),
            Wires,
            NextComponentId,
            NextWireId,
            Analysis);
    }

    internal Schematic WithoutComponent(ComponentId id)
    {
        if (!Components.ContainsKey(id))
        {
            throw new ArgumentException($"No component carries the id {id}.", nameof(id));
        }

        return new Schematic(
            Components.Remove(id), Wires, NextComponentId, NextWireId, Analysis);
    }

    internal Schematic WithWire(Wire wire)
    {
        ArgumentNullException.ThrowIfNull(wire);

        if (wire.Id != NextWireId)
        {
            throw new ArgumentException(
                $"A new wire must carry the next id {NextWireId}, not {wire.Id}.", nameof(wire));
        }

        return new Schematic(
            Components, Wires.Add(wire.Id, wire), NextComponentId, NextWireId.Next(), Analysis);
    }

    internal Schematic WithoutWire(WireId id)
    {
        if (!Wires.ContainsKey(id))
        {
            throw new ArgumentException($"No wire carries the id {id}.", nameof(id));
        }

        return new Schematic(
            Components, Wires.Remove(id), NextComponentId, NextWireId, Analysis);
    }

    internal Schematic WithAnalysis(AnalysisSettings analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        return new Schematic(Components, Wires, NextComponentId, NextWireId, analysis);
    }

    private static ImmutableSortedDictionary<ComponentId, ComponentInstance> CollectComponents(
        IEnumerable<ComponentInstance> components)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<ComponentId, ComponentInstance>();
        var designators = new HashSet<string>(StringComparer.Ordinal);

        foreach (var component in components)
        {
            ArgumentNullException.ThrowIfNull(component, nameof(components));

            if (builder.ContainsKey(component.Id))
            {
                throw new ArgumentException(
                    $"The component id {component.Id} is used more than once.", nameof(components));
            }

            if (!designators.Add(component.Designator))
            {
                throw new ArgumentException(
                    $"The designator '{component.Designator}' is used more than once.",
                    nameof(components));
            }

            builder.Add(component.Id, component);
        }

        return builder.ToImmutable();
    }

    private static ImmutableSortedDictionary<WireId, Wire> CollectWires(IEnumerable<Wire> wires)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<WireId, Wire>();

        foreach (var wire in wires)
        {
            ArgumentNullException.ThrowIfNull(wire, nameof(wires));

            if (builder.ContainsKey(wire.Id))
            {
                throw new ArgumentException(
                    $"The wire id {wire.Id} is used more than once.", nameof(wires));
            }

            builder.Add(wire.Id, wire);
        }

        return builder.ToImmutable();
    }

    private static void RequireNextComponentId(
        ImmutableSortedDictionary<ComponentId, ComponentInstance> components, ComponentId nextComponentId)
    {
        if (nextComponentId.Value < ComponentId.First.Value)
        {
            throw new ArgumentException(
                $"The next component id {nextComponentId} must be at least {ComponentId.First}.",
                nameof(nextComponentId));
        }

        foreach (var id in components.Keys)
        {
            if (id.Value >= nextComponentId.Value)
            {
                throw new ArgumentException(
                    $"The next component id {nextComponentId} must be greater than the used id {id}.",
                    nameof(nextComponentId));
            }
        }
    }

    private static void RequireNextWireId(
        ImmutableSortedDictionary<WireId, Wire> wires, WireId nextWireId)
    {
        if (nextWireId.Value < WireId.First.Value)
        {
            throw new ArgumentException(
                $"The next wire id {nextWireId} must be at least {WireId.First}.", nameof(nextWireId));
        }

        foreach (var id in wires.Keys)
        {
            if (id.Value >= nextWireId.Value)
            {
                throw new ArgumentException(
                    $"The next wire id {nextWireId} must be greater than the used id {id}.",
                    nameof(nextWireId));
            }
        }
    }
}
