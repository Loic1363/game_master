using Nodal.Circuit.Schematics;

namespace Nodal.Editor;

public sealed record Selection(ComponentId? Component, WireId? Wire)
{
    public static Selection None { get; } = new(null, null);

    public bool IsEmpty => Component is null && Wire is null;

    public static Selection Of(ComponentId component) => new(component, null);

    public static Selection Of(WireId wire) => new(null, wire);
}
