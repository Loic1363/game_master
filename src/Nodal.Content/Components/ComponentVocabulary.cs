using Nodal.Foundation.Quantities;

namespace Nodal.Content.Components;

internal static class ComponentVocabulary
{
    private static readonly Dictionary<string, ComponentCategory> CategoryByName = new(StringComparer.Ordinal)
    {
        ["passive"] = ComponentCategory.Passive,
        ["semiconductor"] = ComponentCategory.Semiconductor,
        ["source"] = ComponentCategory.Source,
        ["reference"] = ComponentCategory.Reference,
    };

    private static readonly Dictionary<string, Unit> UnitByName = new(StringComparer.Ordinal)
    {
        ["dimensionless"] = Unit.Dimensionless,
        ["ohm"] = Unit.Ohm,
        ["farad"] = Unit.Farad,
        ["henry"] = Unit.Henry,
        ["volt"] = Unit.Volt,
        ["ampere"] = Unit.Ampere,
        ["second"] = Unit.Second,
        ["hertz"] = Unit.Hertz,
        ["watt"] = Unit.Watt,
    };

    private static readonly Dictionary<string, StandardSeries> SeriesByName = new(StringComparer.Ordinal)
    {
        ["E6"] = StandardSeries.E6,
        ["E12"] = StandardSeries.E12,
        ["E24"] = StandardSeries.E24,
    };

    public static string CategoryNames => Join(CategoryByName.Keys);

    public static string UnitNames => Join(UnitByName.Keys);

    public static string SeriesNames => Join(SeriesByName.Keys);

    public static bool TryCategory(string name, out ComponentCategory category) =>
        CategoryByName.TryGetValue(name, out category);

    public static bool TryUnit(string name, out Unit unit) => UnitByName.TryGetValue(name, out unit);

    public static bool TrySeries(string name, out StandardSeries series) =>
        SeriesByName.TryGetValue(name, out series);

    private static string Join(IEnumerable<string> names) => string.Join(", ", names);
}
