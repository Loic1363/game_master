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

    private static ImmutableSortedDictionary<string, Quantity> NoParameters { get; } =
        ImmutableSortedDictionary.Create<string, Quantity>(StringComparer.Ordinal);

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
                components, wires, new ComponentId(nextComponentId), new WireId(nextWireId)));
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

        WarnUnknownFields(element, path, SchematicFields.Component);

        var hasId = TryReadIdentifier(element, path, SchematicFields.Id, out var id);
        var definitionId = ReadRequiredText(element, path, SchematicFields.Definition);
        var designator = ReadRequiredText(element, path, SchematicFields.Designator);
        var hasX = TryReadInteger(element, path, SchematicFields.X, out var x);
        var hasY = TryReadInteger(element, path, SchematicFields.Y, out var y);
        var hasRotation = TryReadRotation(element, path, out var rotation);
        var hasMirrored = TryReadMirrored(element, path, out var mirrored);
        var parameters = ReadParameters(element, path, definitionId);

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

    private ImmutableSortedDictionary<string, Quantity> ReadParameters(
        JsonElement element, string path, string definitionId)
    {
        var parametersPath = JsonPath.Field(path, SchematicFields.Parameters);
        var known = _catalog.TryGetById(definitionId, out var definition);

        if (!element.TryGetProperty(SchematicFields.Parameters, out var field))
        {
            return known ? WithMissingDefaults(NoParameters, definition!, parametersPath) : NoParameters;
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
            return DroppedParameters(field, parametersPath, definitionId);
        }

        return WithMissingDefaults(
            ReadKnownParameters(field, parametersPath, definition!), definition!, parametersPath);
    }

    private ImmutableSortedDictionary<string, Quantity> ReadKnownParameters(
        JsonElement parameters, string parametersPath, ComponentDefinition definition)
    {
        var values = NoParameters;

        foreach (var property in parameters.EnumerateObject())
        {
            var valuePath = JsonPath.Field(parametersPath, property.Name);
            var parameter = ParameterOf(definition, property.Name);

            if (parameter is null)
            {
                Warn(valuePath, $"Unknown parameter '{property.Name}' was dropped.");

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
                Warn(valuePath, $"{problem} The value was reset to the default {fallback}.");
                values = values.SetItem(parameter.Name, fallback);
            }
            else
            {
                Warn(valuePath, $"{problem} The value was unset.");
            }
        }

        return values;
    }

    private ImmutableSortedDictionary<string, Quantity> DroppedParameters(
        JsonElement parameters, string parametersPath, string definitionId)
    {
        foreach (var property in parameters.EnumerateObject())
        {
            Warn(
                JsonPath.Field(parametersPath, property.Name),
                $"The component definition '{definitionId}' is unknown, so the stored value "
                + $"{property.Value.GetRawText()} of '{property.Name}' was dropped.");
        }

        return NoParameters;
    }

    private ImmutableSortedDictionary<string, Quantity> WithMissingDefaults(
        ImmutableSortedDictionary<string, Quantity> values,
        ComponentDefinition definition,
        string parametersPath)
    {
        foreach (var parameter in definition.Parameters)
        {
            if (parameter.Default is not { } fallback || values.ContainsKey(parameter.Name))
            {
                continue;
            }

            Warn(
                JsonPath.Field(parametersPath, parameter.Name),
                $"Required parameter '{parameter.Name}' was missing and was set to the "
                + $"default {fallback}.");

            values = values.SetItem(parameter.Name, fallback);
        }

        return values;
    }

    private static string? ParameterProblem(
        JsonElement element, ParameterDefinition parameter, out Quantity value)
    {
        value = default;

        if (element.ValueKind != JsonValueKind.Number)
        {
            return $"Parameter '{parameter.Name}' must be "
                + $"{JsonValueKindNames.Of(JsonValueKind.Number)}.";
        }

        if (!element.TryGetDouble(out var number) || !double.IsFinite(number))
        {
            return $"Parameter '{parameter.Name}' must be a finite number.";
        }

        value = new Quantity(number, parameter.Unit);

        if (parameter.Minimum is { } minimum && number < minimum.Value)
        {
            return $"{value} is below the minimum {minimum} of '{parameter.Name}'.";
        }

        if (parameter.Maximum is { } maximum && number > maximum.Value)
        {
            return $"{value} is above the maximum {maximum} of '{parameter.Name}'.";
        }

        if (parameter.Series is { } series && !BelongsToSeries(series, number))
        {
            return $"{value} does not belong to series {series}.";
        }

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

    private void WarnUnknownFields(JsonElement element, string path, string[] knownFields)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!knownFields.Contains(property.Name, StringComparer.Ordinal))
            {
                Warn(JsonPath.Field(path, property.Name), $"Unknown field '{property.Name}' was ignored.");
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

    private void Warn(string path, string message) =>
        _diagnostics.Add(SchematicDiagnostic.Warning(path, message));
}
