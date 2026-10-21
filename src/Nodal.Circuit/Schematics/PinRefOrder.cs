using System.Collections.Immutable;
using Nodal.Content.Components;

namespace Nodal.Circuit.Schematics;

public sealed class PinRefOrder : IComparer<PinRef>
{
    private const int Unknown = int.MaxValue;

    private readonly ImmutableDictionary<PinRef, int> _indexByPin;

    private PinRefOrder(ImmutableDictionary<PinRef, int> indexByPin) => _indexByPin = indexByPin;

    public static PinRefOrder For(Schematic schematic, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);

        var builder = ImmutableDictionary.CreateBuilder<PinRef, int>();

        foreach (var component in schematic.Components.Values)
        {
            if (!catalog.TryGetById(component.DefinitionId, out var definition))
            {
                continue;
            }

            for (var index = 0; index < definition.Pins.Length; index++)
            {
                builder[new PinRef(component.Id, definition.Pins[index].Name)] = index;
            }
        }

        return new PinRefOrder(builder.ToImmutable());
    }

    public int Compare(PinRef left, PinRef right)
    {
        var byComponent = left.ComponentId.CompareTo(right.ComponentId);

        if (byComponent != 0)
        {
            return byComponent;
        }

        var byIndex = IndexOf(left).CompareTo(IndexOf(right));

        return byIndex != 0 ? byIndex : string.CompareOrdinal(left.PinName, right.PinName);
    }

    private int IndexOf(PinRef pin) => _indexByPin.TryGetValue(pin, out var index) ? index : Unknown;
}
