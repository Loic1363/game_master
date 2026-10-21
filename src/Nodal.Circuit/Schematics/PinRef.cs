namespace Nodal.Circuit.Schematics;

public readonly record struct PinRef(ComponentId ComponentId, string PinName) : IComparable<PinRef>
{
    public int CompareTo(PinRef other)
    {
        var byComponent = ComponentId.CompareTo(other.ComponentId);

        return byComponent != 0
            ? byComponent
            : string.CompareOrdinal(PinName, other.PinName);
    }

    public override string ToString() => $"{ComponentId}.{PinName}";
}
