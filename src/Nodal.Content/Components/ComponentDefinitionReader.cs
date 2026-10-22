using System.Collections.Immutable;
using System.Text.Json;
using Nodal.Content.Diagnostics;
using Nodal.Content.Sources;
using Nodal.Foundation.Quantities;

namespace Nodal.Content.Components;

internal sealed class ComponentDefinitionReader
{
    private const int SupportedSchemaVersion = 1;
    private const string ComponentKind = "component";

    private static readonly JsonDocumentOptions StrictJson = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
    };

    private readonly string _sourceFile;
    private readonly List<ContentDiagnostic> _diagnostics;
    private bool _rejected;

    private ComponentDefinitionReader(string sourceFile, List<ContentDiagnostic> diagnostics)
    {
        _sourceFile = sourceFile;
        _diagnostics = diagnostics;
    }

    public static ComponentDefinition? Read(ContentFile file, List<ContentDiagnostic> diagnostics) =>
        new ComponentDefinitionReader(file.Name, diagnostics).ReadDefinition(file.Text);

    private ComponentDefinition? ReadDefinition(string text)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(text, StrictJson);
        }
        catch (JsonException)
        {
            Reject(JsonPath.Root, "The file is not valid JSON.");
            return null;
        }

        using (document)
        {
            return ReadRoot(document.RootElement);
        }
    }

    private ComponentDefinition? ReadRoot(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            Reject(JsonPath.Root, "A component file must contain a JSON object.");
            return null;
        }

        RejectUnknownFields(root, JsonPath.Root, ComponentFields.Root);

        if (!HasSupportedSchemaVersion(root) || !HasComponentKind(root))
        {
            return null;
        }

        var definition = new ComponentDefinition(
            ReadId(root),
            ReadRequiredText(root, ComponentFields.Name),
            ReadRequiredText(root, ComponentFields.Description),
            ReadCategory(root),
            ReadDesignatorPrefix(root),
            ReadPins(root),
            ReadParameters(root));

        return _rejected ? null : definition;
    }

    private bool HasSupportedSchemaVersion(JsonElement root)
    {
        if (!TryGetField(root, JsonPath.Root, ComponentFields.SchemaVersion, JsonValueKind.Number, out var element))
        {
            return false;
        }

        if (element.TryGetInt32(out var version) && version == SupportedSchemaVersion)
        {
            return true;
        }

        Reject(
            JsonPath.Field(JsonPath.Root, ComponentFields.SchemaVersion),
            $"Unsupported schema version. This build reads version {SupportedSchemaVersion}.");

        return false;
    }

    private bool HasComponentKind(JsonElement root)
    {
        if (!TryGetField(root, JsonPath.Root, ComponentFields.Kind, JsonValueKind.String, out var element))
        {
            return false;
        }

        if (element.ValueEquals(ComponentKind))
        {
            return true;
        }

        Reject(
            JsonPath.Field(JsonPath.Root, ComponentFields.Kind),
            $"A component file must declare kind '{ComponentKind}'.");

        return false;
    }

    private string ReadId(JsonElement root)
    {
        var id = ReadRequiredText(root, ComponentFields.Id);

        if (id.Length == 0 || ComponentNameRules.IsComponentId(id))
        {
            return id;
        }

        Reject(
            JsonPath.Field(JsonPath.Root, ComponentFields.Id),
            "A component id may contain only lowercase letters, digits and hyphens.");

        return string.Empty;
    }

    private ComponentCategory ReadCategory(JsonElement root)
    {
        var name = ReadRequiredText(root, ComponentFields.Category);

        if (ComponentVocabulary.TryCategory(name, out var category))
        {
            return category;
        }

        if (name.Length > 0)
        {
            Reject(
                JsonPath.Field(JsonPath.Root, ComponentFields.Category),
                $"Unknown category '{name}'. Known categories are {ComponentVocabulary.CategoryNames}.");
        }

        return ComponentCategory.Passive;
    }

    private string ReadDesignatorPrefix(JsonElement root)
    {
        var prefix = ReadRequiredText(root, ComponentFields.DesignatorPrefix);

        if (prefix.Length == 0 || ComponentNameRules.IsDesignatorPrefix(prefix))
        {
            return prefix;
        }

        Reject(
            JsonPath.Field(JsonPath.Root, ComponentFields.DesignatorPrefix),
            "A designator prefix must be one or more uppercase letters.");

        return string.Empty;
    }

    private ImmutableArray<PinDefinition> ReadPins(JsonElement root)
    {
        if (!TryGetField(root, JsonPath.Root, ComponentFields.Pins, JsonValueKind.Array, out var array))
        {
            return [];
        }

        var path = JsonPath.Field(JsonPath.Root, ComponentFields.Pins);

        if (array.GetArrayLength() == 0)
        {
            Reject(path, "A component must declare at least one pin.");
            return [];
        }

        var pins = ReadElements(array, path, ReadPin);

        RejectDuplicates(pins, path, pin => pin.Name, name => $"The pin name '{name}' is used more than once.");

        RejectDuplicates(
            pins,
            path,
            pin => (pin.X, pin.Y),
            position => $"Two pins share the position ({position.X}, {position.Y}).");

        return pins;
    }

    private PinDefinition? ReadPin(JsonElement element, string path)
    {
        if (!RequireObject(element, path))
        {
            return null;
        }

        RejectUnknownFields(element, path, ComponentFields.Pin);

        var name = ReadRequiredText(element, path, ComponentFields.Name);
        var hasX = TryReadInteger(element, path, ComponentFields.X, out var x);
        var hasY = TryReadInteger(element, path, ComponentFields.Y, out var y);
        var hasGlobalNet = TryReadGlobalNet(element, path, out var globalNet);

        return name.Length > 0 && hasX && hasY && hasGlobalNet
            ? new PinDefinition(name, x, y, globalNet)
            : null;
    }

    private ImmutableArray<ParameterDefinition> ReadParameters(JsonElement root)
    {
        if (!TryGetField(root, JsonPath.Root, ComponentFields.Parameters, JsonValueKind.Array, out var array))
        {
            return [];
        }

        var path = JsonPath.Field(JsonPath.Root, ComponentFields.Parameters);
        var parameters = ReadElements(array, path, ReadParameter);

        RejectDuplicates(
            parameters,
            path,
            parameter => parameter.Name,
            name => $"The parameter name '{name}' is used more than once.");

        return parameters;
    }

    private ParameterDefinition? ReadParameter(JsonElement element, string path)
    {
        if (!RequireObject(element, path))
        {
            return null;
        }

        RejectUnknownFields(element, path, ComponentFields.Parameter);

        var name = ReadParameterName(element, path);
        var label = ReadRequiredText(element, path, ComponentFields.Label);

        if (!TryReadUnit(element, path, out var unit)
            || !TryReadTier(element, path, out var tier)
            || !TryReadQuantity(element, path, ComponentFields.Default, unit, out var defaultValue)
            || !TryReadQuantity(element, path, ComponentFields.Minimum, unit, out var minimum)
            || !TryReadQuantity(element, path, ComponentFields.Maximum, unit, out var maximum)
            || !TryReadSeries(element, path, out var series))
        {
            return null;
        }

        if (!BoundsAreOrdered(minimum, maximum, path)
            || !RespectsBounds(defaultValue, minimum, maximum, path)
            || !BelongsToSeries(series, defaultValue, path))
        {
            return null;
        }

        return name.Length > 0 && label.Length > 0
            ? new ParameterDefinition(name, label, tier, unit, defaultValue, minimum, maximum, series)
            : null;
    }

    private string ReadParameterName(JsonElement element, string path)
    {
        var name = ReadRequiredText(element, path, ComponentFields.Name);

        if (name.Length == 0 || ComponentNameRules.IsParameterName(name))
        {
            return name;
        }

        Reject(
            JsonPath.Field(path, ComponentFields.Name),
            "A parameter name must start with a lowercase letter and hold lowercase words "
            + "separated by single hyphens.");

        return string.Empty;
    }

    private bool TryReadUnit(JsonElement element, string path, out Unit unit)
    {
        unit = Unit.Dimensionless;

        var name = ReadRequiredText(element, path, ComponentFields.Unit);

        if (name.Length == 0)
        {
            return false;
        }

        if (ComponentVocabulary.TryUnit(name, out unit))
        {
            return true;
        }

        Reject(
            JsonPath.Field(path, ComponentFields.Unit),
            $"Unknown unit '{name}'. Known units are {ComponentVocabulary.UnitNames}.");

        return false;
    }

    private bool TryReadTier(JsonElement element, string path, out ParameterTier tier)
    {
        tier = ParameterTier.Basic;

        var name = ReadRequiredText(element, path, ComponentFields.Tier);

        if (name.Length == 0)
        {
            return false;
        }

        if (ComponentVocabulary.TryTier(name, out tier))
        {
            return true;
        }

        Reject(
            JsonPath.Field(path, ComponentFields.Tier),
            $"Unknown tier '{name}'. Known tiers are {ComponentVocabulary.TierNames}.");

        return false;
    }

    private bool TryReadQuantity(
        JsonElement element, string path, string fieldName, Unit unit, out Quantity? value)
    {
        value = null;

        if (!element.TryGetProperty(fieldName, out var field))
        {
            return true;
        }

        if (field.ValueKind != JsonValueKind.String)
        {
            Reject(
                JsonPath.Field(path, fieldName),
                $"Field '{fieldName}' must be {JsonValueKindNames.Of(JsonValueKind.String)}.");

            return false;
        }

        var text = field.GetString() ?? string.Empty;

        if (text.Trim().Length == 0)
        {
            Reject(JsonPath.Field(path, fieldName), $"Field '{fieldName}' must not be empty.");

            return false;
        }

        if (Quantity.TryParse(text, unit, out var parsed, out var error))
        {
            value = parsed;

            return true;
        }

        Reject(
            JsonPath.Field(path, fieldName),
            $"'{text}' is not a valid value for this unit ({error}).");

        return false;
    }

    private bool BoundsAreOrdered(Quantity? minimum, Quantity? maximum, string path)
    {
        if (minimum is not { } low || maximum is not { } high || low.Value <= high.Value)
        {
            return true;
        }

        Reject(
            JsonPath.Field(path, ComponentFields.Minimum),
            $"The minimum {low} must not be greater than the maximum {high}.");

        return false;
    }

    private bool RespectsBounds(Quantity? value, Quantity? minimum, Quantity? maximum, string path)
    {
        if (value is not { } quantity)
        {
            return true;
        }

        if (minimum is { } low && quantity.Value < low.Value)
        {
            Reject(
                JsonPath.Field(path, ComponentFields.Default),
                $"The default value {quantity} is below the minimum {low}.");

            return false;
        }

        if (maximum is { } high && quantity.Value > high.Value)
        {
            Reject(
                JsonPath.Field(path, ComponentFields.Default),
                $"The default value {quantity} is above the maximum {high}.");

            return false;
        }

        return true;
    }

    private bool TryReadGlobalNet(JsonElement element, string path, out string? globalNet)
    {
        globalNet = null;

        if (!element.TryGetProperty(ComponentFields.GlobalNet, out var field))
        {
            return true;
        }

        if (field.ValueKind != JsonValueKind.String)
        {
            Reject(
                JsonPath.Field(path, ComponentFields.GlobalNet),
                $"Field '{ComponentFields.GlobalNet}' must be {JsonValueKindNames.Of(JsonValueKind.String)}.");

            return false;
        }

        var name = field.GetString() ?? string.Empty;

        if (ComponentNameRules.IsGlobalNetName(name))
        {
            globalNet = name;
            return true;
        }

        Reject(
            JsonPath.Field(path, ComponentFields.GlobalNet),
            $"'{name}' is not a valid global net name. A global net name starts with an uppercase " +
            $"letter followed by uppercase letters, digits or underscores, or is exactly " +
            $"'{ComponentNameRules.GroundNetName}'.");

        return false;
    }

    private bool TryReadSeries(JsonElement element, string path, out StandardSeries? series)
    {
        series = null;

        if (!element.TryGetProperty(ComponentFields.Series, out var field))
        {
            return true;
        }

        if (field.ValueKind != JsonValueKind.String)
        {
            Reject(
                JsonPath.Field(path, ComponentFields.Series),
                $"Field '{ComponentFields.Series}' must be {JsonValueKindNames.Of(JsonValueKind.String)}.");

            return false;
        }

        var name = field.GetString() ?? string.Empty;

        if (ComponentVocabulary.TrySeries(name, out var parsed))
        {
            series = parsed;
            return true;
        }

        Reject(
            JsonPath.Field(path, ComponentFields.Series),
            $"Unknown series '{name}'. Known series are {ComponentVocabulary.SeriesNames}.");

        return false;
    }

    private bool BelongsToSeries(StandardSeries? series, Quantity? value, string path)
    {
        if (series is not { } standard || value is not { } quantity)
        {
            return true;
        }

        if (quantity.Value > 0 && ESeries.Contains(standard, quantity.Value))
        {
            return true;
        }

        Reject(
            JsonPath.Field(path, ComponentFields.Default),
            $"The default value {quantity} does not belong to series {standard}.");

        return false;
    }

    private string ReadRequiredText(JsonElement owner, string fieldName) =>
        ReadRequiredText(owner, JsonPath.Root, fieldName);

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

    private bool TryGetField(
        JsonElement owner, string ownerPath, string fieldName, JsonValueKind expectedKind, out JsonElement field)
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

    private void RejectUnknownFields(JsonElement element, string path, string[] knownFields)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!knownFields.Contains(property.Name, StringComparer.Ordinal))
            {
                Reject(JsonPath.Field(path, property.Name), $"Unknown field '{property.Name}'.");
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

    private void Reject(string path, string message)
    {
        _rejected = true;
        _diagnostics.Add(ContentDiagnostic.Error(_sourceFile, path, message));
    }
}
