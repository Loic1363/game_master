using Nodal.Content.Components;
using Nodal.Content.Diagnostics;
using Nodal.Content.Sources;
using Nodal.Foundation.Geometry;

namespace Nodal.Content.Symbols;

public static class SymbolLibraryLoader
{
    private const double LeadTolerance = 1e-9;
    private const string JsonExtension = ".json";

    public static SymbolLibraryLoadResult Load(IContentSource source, ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(catalog);

        var diagnostics = new List<ContentDiagnostic>();
        var symbols = new List<Symbol>();
        var sourceFileByKey = new Dictionary<(string ComponentId, SymbolStyle Style), string>();

        foreach (var file in source.ReadAll())
        {
            var symbol = SymbolReader.Read(file, diagnostics);

            if (symbol is null)
            {
                continue;
            }

            if (!catalog.TryGetById(symbol.ComponentId, out var definition))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    file.Name,
                    JsonPath.Field(JsonPath.Root, SymbolFields.Component),
                    $"No component carries the id '{symbol.ComponentId}'."));
                continue;
            }

            var expectedFileName = ExpectedFileName(symbol.ComponentId, symbol.Style);

            if (!string.Equals(file.Name, expectedFileName, StringComparison.Ordinal))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    file.Name,
                    JsonPath.Root,
                    "A symbol file must be named after its component and style, "
                    + $"so this file must be named {expectedFileName}."));
                continue;
            }

            var key = (symbol.ComponentId, symbol.Style);

            if (sourceFileByKey.TryGetValue(key, out var owner))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    file.Name,
                    JsonPath.Root,
                    $"The symbol for component '{symbol.ComponentId}' in style " +
                    $"'{SymbolVocabulary.NameOf(symbol.Style)}' is already defined by {owner}."));
                continue;
            }

            sourceFileByKey.Add(key, file.Name);

            var unreached = UnreachedPins(definition, symbol);

            if (unreached.Count > 0)
            {
                diagnostics.AddRange(unreached.Select(pin => ContentDiagnostic.Error(
                    file.Name,
                    JsonPath.Field(JsonPath.Root, SymbolFields.Primitives),
                    $"No lead ends on the pin '{pin.Name}' at ({pin.X}, {pin.Y}).")));
                continue;
            }

            symbols.Add(symbol);
        }

        diagnostics.AddRange(MissingSymbolWarnings(catalog, symbols));

        return new SymbolLibraryLoadResult(new SymbolLibrary(symbols), [.. diagnostics]);
    }

    private static IEnumerable<ContentDiagnostic> MissingSymbolWarnings(
        ComponentCatalog catalog, List<Symbol> symbols) =>
        catalog.Definitions
            .Where(definition => !symbols.Any(symbol =>
                symbol.Style == SymbolStyle.Iec
                && string.Equals(symbol.ComponentId, definition.Id, StringComparison.Ordinal)))
            .Select(definition => ContentDiagnostic.Warning(
                ExpectedFileName(definition.Id, SymbolStyle.Iec),
                JsonPath.Root,
                $"No {SymbolVocabulary.NameOf(SymbolStyle.Iec)} symbol draws the component " +
                $"'{definition.Id}'."));

    private static string ExpectedFileName(string componentId, SymbolStyle style) =>
        componentId + "." + SymbolVocabulary.NameOf(style) + JsonExtension;

    private static List<PinDefinition> UnreachedPins(ComponentDefinition definition, Symbol symbol)
    {
        var leadEnds = LeadEnds(symbol).ToArray();

        return definition.Pins
            .Where(pin => !leadEnds.Any(end => Reaches(end, pin)))
            .ToList();
    }

    private static IEnumerable<GridVector> LeadEnds(Symbol symbol)
    {
        foreach (var primitive in symbol.Primitives)
        {
            switch (primitive)
            {
                case SymbolLine line:
                    yield return line.From;
                    yield return line.To;
                    break;

                case SymbolPolyline polyline:
                    foreach (var point in polyline.Points)
                    {
                        yield return point;
                    }

                    break;
            }
        }
    }

    private static bool Reaches(GridVector end, PinDefinition pin) =>
        Math.Abs(end.X - pin.X) <= LeadTolerance && Math.Abs(end.Y - pin.Y) <= LeadTolerance;
}
