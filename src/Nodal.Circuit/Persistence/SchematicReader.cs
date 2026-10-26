using System.Collections.Immutable;
using System.Text.Json;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;

namespace Nodal.Circuit.Persistence;

internal sealed class SchematicReader
{
    private const int PointLength = 2;

    private static readonly JsonDocumentOptions StrictJson = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
    };

    private readonly ComponentCatalog _catalog;
    private readonly List<SchematicDiagnostic> _diagnostics = [];
    private bool _rejected;

    private SchematicReader(ComponentCatalog catalog) => _catalog = catalog;

    private static ImmutableSortedDictionary<string, ParameterValue> NoParameters { get; } =
        ImmutableSortedDictionary.Create<string, ParameterValue>(StringComparer.Ordinal);

    public static SchematicReadResult Read(string text, ComponentCatalog catalog) =>
        new SchematicReader(catalog).ReadFile(text);

    private SchematicReadResult ReadFile(string text)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(text, StrictJson);
        }
        catch (JsonException)
        {
            return Refused(JsonPath.Root, "The file is not valid JSON.");
        }

        using (document)
        {
            return ReadRoot(document.RootElement);
        }
    }

    private SchematicReadResult ReadRoot(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return Refused(JsonPath.Root, "A schematic file must contain a JSON object.");
        }

        WarnUnknownFields(root, JsonPath.Root, SchematicFields.Root);

        if (!HasSupportedSchemaVersion(root) || !HasSchematicKind(root))
        {
            return Result(null);
        }

        WarnMistypedCreatedWith(root);

        var hasNextComponentId = TryReadIdentifier(
            root, JsonPath.Root, SchematicFields.NextComponentId, out var nextComponentId);

        var hasNextWireId = TryReadIdentifier(
            root, JsonPath.Root, SchematicFields.NextWireId, out var nextWireId);

        var components = ReadComponents(root);
        var wires = ReadWires(root);
        var analysis = ReadAnalysis(root, components, wires);

        RejectDuplicateComponents(components);
        RejectDuplicateWires(wires);

        if (hasNextComponentId)
        {
            RejectReusedNextId(
                nextComponentId,
                components.Select(component => component.Id.Value),
                SchematicFields.NextComponentId);
        }

        if (hasNextWireId)
        {
            RejectReusedNextId(
                nextWireId, wires.Select(wire => wire.Id.Value), SchematicFields.NextWireId);
        }

        return _rejected
            ? Result(null)
            : Result(Schematic.Create(
                components,
                wires,
                new ComponentId(nextComponentId),
                new WireId(nextWireId),
                analysis));
    }

    private AnalysisSettings ReadAnalysis(
        JsonElement root,
        ImmutableArray<ComponentInstance> components,
        ImmutableArray<Wire> wires)
    {
        if (!root.TryGetProperty(SchematicFields.Analysis, out var element))
        {
            return AnalysisSettings.None;
        }

        var path = JsonPath.Field(JsonPath.Root, SchematicFields.Analysis);

        if (element.ValueKind != JsonValueKind.Object)
        {
            Warn(path, $"'{SchematicFields.Analysis}' must be an object; dropped.");

            return AnalysisSettings.None;
        }

        WarnUnknownFields(element, path, SchematicFields.Analyses);

        return new AnalysisSettings(
            ReadTransientSettings(element, path),
            ReadProbes(element, path, components, wires));
    }

    private ImmutableArray<Probe> ReadProbes(
        JsonElement analysis,
        string analysisPath,
        ImmutableArray<ComponentInstance> components,
        ImmutableArray<Wire> wires)
    {
        if (!analysis.TryGetProperty(SchematicFields.Probes, out var element))
        {
            return [];
        }

        var path = JsonPath.Field(analysisPath, SchematicFields.Probes);

        if (element.ValueKind != JsonValueKind.Array)
        {
            Warn(path, $"'{SchematicFields.Probes}' must be an array; dropped.");

            return [];
        }

        var probes = ImmutableArray.CreateBuilder<Probe>();
        var index = 0;

        foreach (var entry in element.EnumerateArray())
        {
            var entryPath = JsonPath.Element(path, index);
            index++;

            if (ReadProbe(entry, entryPath, components, wires) is not { } probe)
            {
                continue;
            }

            if (probes.Contains(probe))
            {
                Warn(
                    entryPath,
                    $"the probe on {ProbeTargets.Describe(probe)} is listed more than once; "
                    + "the repeat was dropped.");

                continue;
            }

            if (probes.Count == AnalysisSettings.MaxProbes)
            {
                Warn(
                    entryPath,
                    $"a schematic holds at most {AnalysisSettings.MaxProbes} probes; this one "
                    + "was dropped.");

                continue;
            }

            probes.Add(probe);
        }

        return probes.ToImmutable();
    }

    private Probe? ReadProbe(
        JsonElement element,
        string path,
        ImmutableArray<ComponentInstance> components,
        ImmutableArray<Wire> wires)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            Warn(path, "a probe must be an object; dropped.");

            return null;
        }

        WarnUnknownFields(element, path, SchematicFields.ProbeEntry);

        var hasVoltage = element.TryGetProperty(SchematicFields.Voltage, out var voltage);
        var hasCurrent = element.TryGetProperty(SchematicFields.Current, out var current);

        if (hasVoltage == hasCurrent)
        {
            Warn(
                path,
                $"a probe must carry exactly one of '{SchematicFields.Voltage}' and "
                + $"'{SchematicFields.Current}'; dropped.");

            return null;
        }

        return hasVoltage
            ? ReadVoltageProbe(
                voltage, JsonPath.Field(path, SchematicFields.Voltage), components, wires)
            : ReadCurrentProbe(
                current, JsonPath.Field(path, SchematicFields.Current), components);
    }

    private Probe? ReadVoltageProbe(
        JsonElement element,
        string path,
        ImmutableArray<ComponentInstance> components,
        ImmutableArray<Wire> wires)
    {
        if (!RequireProbeTarget(element, path, SchematicFields.VoltageTarget))
        {
            return null;
        }

        var hasComponent = element.TryGetProperty(SchematicFields.ProbeComponent, out _);
        var hasWire = element.TryGetProperty(SchematicFields.ProbeWire, out _);

        if (hasComponent == hasWire)
        {
            Warn(
                path,
                $"a voltage probe must carry either '{SchematicFields.ProbeComponent}' with "
                + $"'{SchematicFields.Pin}' or '{SchematicFields.ProbeWire}'; dropped.");

            return null;
        }

        return hasWire
            ? ReadWireVoltageProbe(element, path, wires)
            : ReadPinVoltageProbe(element, path, components);
    }

    private Probe? ReadPinVoltageProbe(
        JsonElement element, string path, ImmutableArray<ComponentInstance> components)
    {
        if (!TryReadProbeIdentifier(element, path, SchematicFields.ProbeComponent, out var id)
            || !TryReadProbePinName(element, path, out var pinName))
        {
            return null;
        }

        var component = ComponentWithId(components, new ComponentId(id));

        if (component is null)
        {
            Warn(path, $"no component carries the id {id}; the probe was dropped.");

            return null;
        }

        if (!HasPin(component, pinName))
        {
            Warn(path, $"{component.Designator} has no pin '{pinName}'; the probe was dropped.");

            return null;
        }

        return Probe.At(new PinRef(component.Id, pinName));
    }

    private Probe? ReadWireVoltageProbe(
        JsonElement element, string path, ImmutableArray<Wire> wires)
    {
        if (!TryReadProbeIdentifier(element, path, SchematicFields.ProbeWire, out var id))
        {
            return null;
        }

        if (!wires.Any(wire => wire.Id.Value == id))
        {
            Warn(path, $"no wire carries the id {id}; the probe was dropped.");

            return null;
        }

        return Probe.At(new WireId(id));
    }

    private Probe? ReadCurrentProbe(
        JsonElement element, string path, ImmutableArray<ComponentInstance> components)
    {
        if (!RequireProbeTarget(element, path, SchematicFields.CurrentTarget)
            || !TryReadProbeIdentifier(element, path, SchematicFields.ProbeComponent, out var id))
        {
            return null;
        }

        var component = ComponentWithId(components, new ComponentId(id));

        if (component is null)
        {
            Warn(path, $"no component carries the id {id}; the probe was dropped.");

            return null;
        }

        return Probe.Through(component.Id);
    }

    private bool RequireProbeTarget(JsonElement element, string path, string[] knownFields)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            Warn(path, "a probe target must be an object; the probe was dropped.");

            return false;
        }

        WarnUnknownFields(element, path, knownFields);

        return true;
    }

    private bool TryReadProbeIdentifier(
        JsonElement owner, string ownerPath, string fieldName, out int value)
    {
        value = 0;

        if (!owner.TryGetProperty(fieldName, out var element)
            || element.ValueKind != JsonValueKind.Number
            || !element.TryGetInt32(out var number)
            || number < ComponentId.First.Value)
        {
            Warn(
                ownerPath,
                $"'{fieldName}' must be a positive whole number; the probe was dropped.");

            return false;
        }

        value = number;

        return true;
    }

    private bool TryReadProbePinName(JsonElement owner, string ownerPath, out string pinName)
    {
        pinName = string.Empty;

        if (!owner.TryGetProperty(SchematicFields.Pin, out var element)
            || element.ValueKind != JsonValueKind.String
            || element.GetString() is not { Length: > 0 } text)
        {
            Warn(
                ownerPath,
                $"'{SchematicFields.Pin}' must be a pin name; the probe was dropped.");

            return false;
        }

        pinName = text;

        return true;
    }

    private static ComponentInstance? ComponentWithId(
        ImmutableArray<ComponentInstance> components, ComponentId id) =>
        components.FirstOrDefault(component => component.Id == id);

    private bool HasPin(ComponentInstance component, string pinName) =>
        _catalog.TryGetById(component.DefinitionId, out var definition)
        && definition.Pins.Any(pin => string.Equals(pin.Name, pinName, StringComparison.Ordinal));

    private TransientSettings? ReadTransientSettings(JsonElement analysis, string analysisPath)
    {
        if (!analysis.TryGetProperty(SchematicFields.Transient, out var element))
        {
            return null;
        }

        var path = JsonPath.Field(analysisPath, SchematicFields.Transient);

        if (element.ValueKind != JsonValueKind.Object)
        {
            Warn(path, $"'{SchematicFields.Transient}' must be an object; dropped.");

            return null;
        }

        WarnUnknownFields(element, path, SchematicFields.TransientAnalysis);

        if (!TryReadSeconds(element, path, SchematicFields.StopTime, out var stopTime)
            || stopTime is null
            || !TryReadSeconds(element, path, SchematicFields.OutputStep, out var outputStep)
            || !TryReadSeconds(element, path, SchematicFields.MaxStep, out var maxStep)
            || !TryReadFlag(element, path, SchematicFields.StartFromZero, out var startFromZero))
        {
            return Dropped(path);
        }

        var settings = new TransientSettings(
            stopTime.Value, outputStep, maxStep, startFromZero);

        return settings.IsValid ? settings : Dropped(path);
    }

    private bool TryReadSeconds(
        JsonElement owner, string ownerPath, string fieldName, out double? seconds)
    {
        seconds = null;

        if (!owner.TryGetProperty(fieldName, out var element)
            || element.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (element.ValueKind != JsonValueKind.Number
            || !element.TryGetDouble(out var value)
            || !double.IsFinite(value))
        {
            Warn(
                JsonPath.Field(ownerPath, fieldName),
                $"'{fieldName}' must be a number of seconds.");

            return false;
        }

        seconds = value;

        return true;
    }

    private bool TryReadFlag(
        JsonElement owner, string ownerPath, string fieldName, out bool flag)
    {
        flag = false;

        if (!owner.TryGetProperty(fieldName, out var element)
            || element.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (element.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            Warn(JsonPath.Field(ownerPath, fieldName), $"'{fieldName}' must be a boolean.");

            return false;
        }

        flag = element.GetBoolean();

        return true;
    }

    private TransientSettings? Dropped(string path)
    {
        Warn(path, "the transient analysis settings were dropped.");

        return null;
    }

    private bool HasSupportedSchemaVersion(JsonElement root)
    {
        if (!TryGetField(
                root,
                JsonPath.Root,
                SchematicFields.SchemaVersion,
                JsonValueKind.Number,
                out var element))
        {
            return false;
        }

        if (element.TryGetInt32(out var version) && version == SchematicFormat.SupportedSchemaVersion)
        {
            return true;
        }

        Reject(
            JsonPath.Field(JsonPath.Root, SchematicFields.SchemaVersion),
            "Unsupported schema version. This build reads version "
            + $"{SchematicFormat.SupportedSchemaVersion}.");

        return false;
    }

    private bool HasSchematicKind(JsonElement root)
    {
        if (!TryGetField(root, JsonPath.Root, SchematicFields.Kind, JsonValueKind.String, out var element))
        {
            return false;
        }

        if (element.ValueEquals(SchematicFormat.Kind))
        {
            return true;
        }

        Reject(
            JsonPath.Field(JsonPath.Root, SchematicFields.Kind),
            $"A schematic file must declare kind '{SchematicFormat.Kind}'.");

        return false;
    }

    private void WarnMistypedCreatedWith(JsonElement root)
    {
        if (root.TryGetProperty(SchematicFields.CreatedWith, out var field)
            && field.ValueKind != JsonValueKind.String)
        {
            Warn(
                JsonPath.Field(JsonPath.Root, SchematicFields.CreatedWith),
                $"Field '{SchematicFields.CreatedWith}' should be a string. It was ignored.");
        }
    }

    private ImmutableArray<ComponentInstance> ReadComponents(JsonElement root)
    {
        if (!TryGetField(
                root, JsonPath.Root, SchematicFields.Components, JsonValueKind.Array, out var array))
        {
            return [];
        }

        return ReadElements(
            array, JsonPath.Field(JsonPath.Root, SchematicFields.Components), ReadComponent);
    }

    private ComponentInstance? ReadComponent(JsonElement element, string path)
    {
        if (!RequireObject(element, path))
        {
            return null;
        }

        var designator = ReadRequiredText(element, path, SchematicFields.Designator);
        var subject = designator.Length > 0 ? designator : null;

        WarnUnknownFields(element, path, SchematicFields.Component, subject);

        var hasId = TryReadIdentifier(element, path, SchematicFields.Id, out var id);
        var definitionId = ReadRequiredText(element, path, SchematicFields.Definition);
        var hasX = TryReadInteger(element, path, SchematicFields.X, out var x);
        var hasY = TryReadInteger(element, path, SchematicFields.Y, out var y);
        var hasRotation = TryReadRotation(element, path, out var rotation);
        var hasMirrored = TryReadMirrored(element, path, out var mirrored);
        var parameters = ReadParameters(element, path, definitionId, subject);

        return hasId && definitionId.Length > 0 && designator.Length > 0 && hasX && hasY
            && hasRotation && hasMirrored
            ? new ComponentInstance(
                new ComponentId(id),
                definitionId,
                designator,
                new GridPoint(x, y),
                new Orientation(rotation, mirrored),
                parameters)
            : null;
    }

    private ImmutableSortedDictionary<string, ParameterValue> ReadParameters(
        JsonElement element, string path, string definitionId, string? subject)
    {
        var parametersPath = JsonPath.Field(path, SchematicFields.Parameters);
        var known = _catalog.TryGetById(definitionId, out var definition);

        if (!element.TryGetProperty(SchematicFields.Parameters, out var field))
        {
            return known
                ? WithMissingDefaults(NoParameters, definition!, parametersPath, subject)
                : NoParameters;
        }

        if (field.ValueKind != JsonValueKind.Object)
        {
            Reject(
                parametersPath,
                $"Field '{SchematicFields.Parameters}' must be "
                + $"{JsonValueKindNames.Of(JsonValueKind.Object)}.");

            return NoParameters;
        }

        if (!known)
        {
            return DroppedParameters(field, parametersPath, definitionId, subject);
        }

        return WithMissingDefaults(
            ReadKnownParameters(field, parametersPath, definition!, subject),
            definition!,
            parametersPath,
            subject);
    }

    private ImmutableSortedDictionary<string, ParameterValue> ReadKnownParameters(
        JsonElement parameters,
        string parametersPath,
        ComponentDefinition definition,
        string? subject)
    {
        var values = NoParameters;

        foreach (var property in parameters.EnumerateObject())
        {
            var valuePath = JsonPath.Field(parametersPath, property.Name);
            var parameter = ParameterOf(definition, property.Name);

            if (parameter is null)
            {
                Warn(valuePath, subject, $"unknown parameter '{property.Name}' was dropped.");

                continue;
            }

            var problem = ParameterProblem(property.Value, parameter, out var value);

            if (problem is null)
            {
                values = values.SetItem(parameter.Name, value);

                continue;
            }

            if (parameter.Default is { } fallback)
            {
                Warn(valuePath, subject, $"{problem}; reset to {fallback}.");
                values = values.SetItem(parameter.Name, fallback);
            }
            else
            {
                Warn(valuePath, subject, $"{problem}; unset.");
            }
        }

        return values;
    }

    private ImmutableSortedDictionary<string, ParameterValue> DroppedParameters(
        JsonElement parameters, string parametersPath, string definitionId, string? subject)
    {
        foreach (var property in parameters.EnumerateObject())
        {
            Warn(
                JsonPath.Field(parametersPath, property.Name),
                subject,
                $"the definition '{definitionId}' is unknown, so the stored value "
                + $"{property.Value.GetRawText()} of '{property.Name}' was dropped.");
        }

        return NoParameters;
    }

    private ImmutableSortedDictionary<string, ParameterValue> WithMissingDefaults(
        ImmutableSortedDictionary<string, ParameterValue> values,
        ComponentDefinition definition,
        string parametersPath,
        string? subject)
    {
        foreach (var parameter in definition.Parameters)
        {
            if (parameter.Default is not { } fallback || values.ContainsKey(parameter.Name))
            {
                continue;
            }

            Warn(
                JsonPath.Field(parametersPath, parameter.Name),
                subject,
                $"{parameter.Name} was missing; set to the default {fallback}.");

            values = values.SetItem(parameter.Name, fallback);
        }

        return values;
    }

    private static string? ParameterProblem(
        JsonElement element, ParameterDefinition parameter, out ParameterValue value)
    {
        value = default;

        if (parameter.IsWaveform)
        {
            return WaveformProblem(element, parameter, out value);
        }

        if (element.ValueKind != JsonValueKind.Number)
        {
            return $"{parameter.Name} must be {JsonValueKindNames.Of(JsonValueKind.Number)}";
        }

        if (!element.TryGetDouble(out var number) || !double.IsFinite(number))
        {
            return $"{parameter.Name} must be a finite number";
        }

        var quantity = new Quantity(number, parameter.Unit);

        value = quantity;

        if (parameter.Minimum is { } minimum && number < minimum.Value)
        {
            return $"{parameter.Name} {quantity} is below the minimum {minimum}";
        }

        if (parameter.Maximum is { } maximum && number > maximum.Value)
        {
            return $"{parameter.Name} {quantity} is above the maximum {maximum}";
        }

        if (parameter.Series is { } series && !BelongsToSeries(series, number))
        {
            return $"{parameter.Name} {quantity} is not in {series}";
        }

        return null;
    }

    private static string? WaveformProblem(
        JsonElement element, ParameterDefinition parameter, out ParameterValue value)
    {
        value = default;

        if (!WaveformJson.TryRead(element, out var waveform, out var problem))
        {
            return $"{parameter.Name} {problem}";
        }

        value = ParameterValue.Of(waveform, parameter.Unit);

        return null;
    }

    private static bool BelongsToSeries(StandardSeries series, double value) =>
        value > 0 && ESeries.Contains(series, value);

    private static ParameterDefinition? ParameterOf(ComponentDefinition definition, string name) =>
        definition.Parameters.FirstOrDefault(
            candidate => string.Equals(candidate.Name, name, StringComparison.Ordinal));

    private ImmutableArray<Wire> ReadWires(JsonElement root)
    {
        if (!TryGetField(root, JsonPath.Root, SchematicFields.Wires, JsonValueKind.Array, out var array))
        {
            return [];
        }

        return ReadElements(array, JsonPath.Field(JsonPath.Root, SchematicFields.Wires), ReadWire);
    }

    private Wire? ReadWire(JsonElement element, string path)
    {
        if (!RequireObject(element, path))
        {
            return null;
        }

        WarnUnknownFields(element, path, SchematicFields.Wire);

        var hasId = TryReadIdentifier(element, path, SchematicFields.Id, out var id);
        var hasStart = TryReadPoint(element, path, SchematicFields.From, out var start);
        var hasEnd = TryReadPoint(element, path, SchematicFields.To, out var end);

        if (!hasId || !hasStart || !hasEnd)
        {
            return null;
        }

        if (start == end)
        {
            Reject(path, $"A wire cannot have zero length at {start}.");

            return null;
        }

        if (start.X != end.X && start.Y != end.Y)
        {
            Reject(path, $"A wire cannot be diagonal, from {start} to {end}.");

            return null;
        }

        return new Wire(new WireId(id), start, end);
    }

    private void RejectDuplicateComponents(ImmutableArray<ComponentInstance> components)
    {
        var path = JsonPath.Field(JsonPath.Root, SchematicFields.Components);

        RejectDuplicates(
            components,
            path,
            component => component.Id,
            id => $"The component id {id} appears more than once.");

        RejectDuplicates(
            components,
            path,
            component => component.Designator,
            designator => $"The designator '{designator}' appears more than once.");
    }

    private void RejectDuplicateWires(ImmutableArray<Wire> wires) =>
        RejectDuplicates(
            wires,
            JsonPath.Field(JsonPath.Root, SchematicFields.Wires),
            wire => wire.Id,
            id => $"The wire id {id} appears more than once.");

    private void RejectReusedNextId(int next, IEnumerable<int> usedIds, string fieldName)
    {
        foreach (var id in usedIds.Where(used => used >= next).Distinct().Order())
        {
            Reject(
                JsonPath.Field(JsonPath.Root, fieldName),
                $"Field '{fieldName}' is {next}, which must be greater than the used id {id}.");
        }
    }

    private bool TryReadRotation(JsonElement owner, string ownerPath, out Rotation rotation)
    {
        rotation = Geometry.Rotation.R0;

        if (!TryReadInteger(owner, ownerPath, SchematicFields.Rotation, out var degrees))
        {
            return false;
        }

        if (RotationDegrees.TryParse(degrees, out rotation))
        {
            return true;
        }

        Reject(
            JsonPath.Field(ownerPath, SchematicFields.Rotation),
            $"A rotation must be 0, 90, 180 or 270 degrees, not {degrees}.");

        return false;
    }

    private bool TryReadMirrored(JsonElement owner, string ownerPath, out bool mirrored)
    {
        mirrored = false;

        if (!owner.TryGetProperty(SchematicFields.Mirrored, out var field))
        {
            return true;
        }

        if (field.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            mirrored = field.GetBoolean();

            return true;
        }

        Reject(
            JsonPath.Field(ownerPath, SchematicFields.Mirrored),
            $"Field '{SchematicFields.Mirrored}' must be true or false.");

        return false;
    }

    private bool TryReadPoint(JsonElement owner, string ownerPath, string fieldName, out GridPoint point)
    {
        point = GridPoint.Origin;

        if (!TryGetField(owner, ownerPath, fieldName, JsonValueKind.Array, out var array))
        {
            return false;
        }

        var path = JsonPath.Field(ownerPath, fieldName);

        if (array.GetArrayLength() != PointLength)
        {
            Reject(path, $"Field '{fieldName}' must hold two whole numbers.");

            return false;
        }

        var coordinates = new int[PointLength];
        var index = 0;

        foreach (var element in array.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out coordinates[index]))
            {
                Reject(JsonPath.Element(path, index), $"Field '{fieldName}' must hold two whole numbers.");

                return false;
            }

            index++;
        }

        point = new GridPoint(coordinates[0], coordinates[1]);

        return true;
    }

    private bool TryReadIdentifier(JsonElement owner, string ownerPath, string fieldName, out int value)
    {
        value = 0;

        if (!TryReadInteger(owner, ownerPath, fieldName, out var number))
        {
            return false;
        }

        if (number >= ComponentId.First.Value)
        {
            value = number;

            return true;
        }

        Reject(
            JsonPath.Field(ownerPath, fieldName), $"Field '{fieldName}' must be a positive whole number.");

        return false;
    }

    private bool TryReadInteger(JsonElement owner, string ownerPath, string fieldName, out int value)
    {
        value = 0;

        if (!TryGetField(owner, ownerPath, fieldName, JsonValueKind.Number, out var element))
        {
            return false;
        }

        if (element.TryGetInt32(out value))
        {
            return true;
        }

        Reject(JsonPath.Field(ownerPath, fieldName), $"Field '{fieldName}' must be a whole number.");

        return false;
    }

    private string ReadRequiredText(JsonElement owner, string ownerPath, string fieldName)
    {
        if (!TryGetField(owner, ownerPath, fieldName, JsonValueKind.String, out var element))
        {
            return string.Empty;
        }

        var text = element.GetString() ?? string.Empty;

        if (text.Trim().Length > 0)
        {
            return text;
        }

        Reject(JsonPath.Field(ownerPath, fieldName), $"Field '{fieldName}' must not be empty.");

        return string.Empty;
    }

    private bool TryGetField(
        JsonElement owner,
        string ownerPath,
        string fieldName,
        JsonValueKind expectedKind,
        out JsonElement field)
    {
        if (!owner.TryGetProperty(fieldName, out field))
        {
            Reject(JsonPath.Field(ownerPath, fieldName), $"Required field '{fieldName}' is missing.");

            return false;
        }

        if (field.ValueKind == expectedKind)
        {
            return true;
        }

        Reject(
            JsonPath.Field(ownerPath, fieldName),
            $"Field '{fieldName}' must be {JsonValueKindNames.Of(expectedKind)}.");

        return false;
    }

    private bool RequireObject(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        Reject(path, "This entry must be a JSON object.");

        return false;
    }

    private void WarnUnknownFields(JsonElement element, string path, string[] knownFields) =>
        WarnUnknownFields(element, path, knownFields, null);

    private void WarnUnknownFields(
        JsonElement element, string path, string[] knownFields, string? subject)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!knownFields.Contains(property.Name, StringComparer.Ordinal))
            {
                Warn(
                    JsonPath.Field(path, property.Name),
                    subject,
                    $"unknown field '{property.Name}' was ignored.");
            }
        }
    }

    private ImmutableArray<TItem> ReadElements<TItem>(
        JsonElement array, string path, Func<JsonElement, string, TItem?> readItem)
        where TItem : class
    {
        var items = new List<TItem>();
        var index = 0;

        foreach (var element in array.EnumerateArray())
        {
            var item = readItem(element, JsonPath.Element(path, index));
            index++;

            if (item is not null)
            {
                items.Add(item);
            }
        }

        return [.. items];
    }

    private void RejectDuplicates<TItem, TKey>(
        ImmutableArray<TItem> items, string path, Func<TItem, TKey> keyOf, Func<TKey, string> describe)
        where TKey : notnull
    {
        foreach (var group in items.GroupBy(keyOf).Where(group => group.Count() > 1))
        {
            Reject(path, describe(group.Key));
        }
    }

    private SchematicReadResult Refused(string path, string message)
    {
        Reject(path, message);

        return Result(null);
    }

    private SchematicReadResult Result(Schematic? schematic) => new(schematic, [.. _diagnostics]);

    private void Reject(string path, string message)
    {
        _rejected = true;
        _diagnostics.Add(SchematicDiagnostic.Error(path, message));
    }

    private void Warn(string path, string message) => Warn(path, null, message);

    private void Warn(string path, string? subject, string message) =>
        _diagnostics.Add(SchematicDiagnostic.Warning(path, subject, message));
}
