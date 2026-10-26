namespace Nodal.Content.Components;

internal static class ComponentFields
{
    public const string SchemaVersion = "schemaVersion";
    public const string Kind = "kind";
    public const string Id = "id";
    public const string Name = "name";
    public const string Description = "description";
    public const string Category = "category";
    public const string DesignatorPrefix = "designatorPrefix";
    public const string Pins = "pins";
    public const string Parameters = "parameters";
    public const string X = "x";
    public const string Y = "y";
    public const string Label = "label";
    public const string Tier = "tier";
    public const string Unit = "unit";
    public const string Default = "default";
    public const string Minimum = "min";
    public const string Maximum = "max";
    public const string Series = "series";

    public const string GlobalNet = "globalNet";

    public static readonly string[] Root =
        [SchemaVersion, Kind, Id, Name, Description, Category, DesignatorPrefix, Pins, Parameters];

    public static readonly string[] Pin = [Name, X, Y, GlobalNet];

    public static readonly string[] Parameter =
        [Name, Label, Tier, Kind, Unit, Default, Minimum, Maximum, Series];
}
