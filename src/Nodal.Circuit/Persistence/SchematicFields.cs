namespace Nodal.Circuit.Persistence;

internal static class SchematicFields
{
    public const string SchemaVersion = "schemaVersion";
    public const string Kind = "kind";
    public const string CreatedWith = "createdWith";
    public const string NextComponentId = "nextComponentId";
    public const string NextWireId = "nextWireId";
    public const string Components = "components";
    public const string Wires = "wires";
    public const string Id = "id";
    public const string Definition = "definition";
    public const string Designator = "designator";
    public const string X = "x";
    public const string Y = "y";
    public const string Rotation = "rotation";
    public const string Mirrored = "mirrored";
    public const string Parameters = "parameters";
    public const string From = "from";
    public const string To = "to";

    public static readonly string[] Root =
        [SchemaVersion, Kind, CreatedWith, NextComponentId, NextWireId, Components, Wires];

    public static readonly string[] Component =
        [Id, Definition, Designator, X, Y, Rotation, Mirrored, Parameters];

    public static readonly string[] Wire = [Id, From, To];
}
