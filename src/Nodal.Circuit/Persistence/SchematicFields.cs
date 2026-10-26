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
    public const string Analysis = "analysis";
    public const string Transient = "transient";
    public const string StopTime = "stopTime";
    public const string OutputStep = "outputStep";
    public const string MaxStep = "maxStep";
    public const string StartFromZero = "startFromZero";
    public const string Probes = "probes";
    public const string Voltage = "voltage";
    public const string Current = "current";
    public const string ProbeComponent = "component";
    public const string Pin = "pin";
    public const string ProbeWire = "wire";

    public static readonly string[] Root =
    [
        SchemaVersion,
        Kind,
        CreatedWith,
        NextComponentId,
        NextWireId,
        Components,
        Wires,
        Analysis,
    ];

    public static readonly string[] Component =
        [Id, Definition, Designator, X, Y, Rotation, Mirrored, Parameters];

    public static readonly string[] Wire = [Id, From, To];

    public static readonly string[] Analyses = [Transient, Probes];

    public static readonly string[] ProbeEntry = [Voltage, Current];

    public static readonly string[] VoltageTarget = [ProbeComponent, Pin, ProbeWire];

    public static readonly string[] CurrentTarget = [ProbeComponent];

    public static readonly string[] TransientAnalysis =
        [StopTime, OutputStep, MaxStep, StartFromZero];
}
