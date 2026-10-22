using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Persistence;

internal static class SchematicAssert
{
    public static void Equal(Schematic expected, Schematic actual)
    {
        Assert.Equal(expected.NextComponentId, actual.NextComponentId);
        Assert.Equal(expected.NextWireId, actual.NextWireId);
        Assert.Equal(expected.Components.Keys, actual.Components.Keys);
        Assert.Equal(expected.Wires.Keys, actual.Wires.Keys);

        foreach (var entry in expected.Components)
        {
            Assert.Equal(entry.Value, actual.Components[entry.Key]);
        }

        foreach (var entry in expected.Wires)
        {
            Assert.Equal(entry.Value.Start, actual.Wires[entry.Key].Start);
            Assert.Equal(entry.Value.End, actual.Wires[entry.Key].End);
        }
    }
}
