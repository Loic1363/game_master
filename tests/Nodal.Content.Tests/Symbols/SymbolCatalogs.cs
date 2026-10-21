using Nodal.Content.Components;

namespace Nodal.Content.Tests.Symbols;

internal static class SymbolCatalogs
{
    public static ComponentCatalog TwoPinParts { get; } = new([
        TwoPinPart("widget", "W"),
        TwoPinPart("gadget", "G"),
    ]);

    private static ComponentDefinition TwoPinPart(string id, string designatorPrefix) =>
        new(id,
            id,
            "A part used by the symbol tests.",
            ComponentCategory.Passive,
            designatorPrefix,
            [new PinDefinition("1", -2, 0), new PinDefinition("2", 2, 0)],
            []);
}
