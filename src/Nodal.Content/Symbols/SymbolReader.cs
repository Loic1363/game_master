using System.Collections.Immutable;
using System.Text.Json;
using Nodal.Content.Components;
using Nodal.Content.Diagnostics;
using Nodal.Content.Sources;
using Nodal.Foundation.Geometry;

namespace Nodal.Content.Symbols;

internal sealed class SymbolReader
{
    private const int SupportedSchemaVersion = 1;
    private const int CoordinateLimit = 10;
    private const int PointComponentCount = 2;
    private const int MinimumPointCount = 2;

    private static readonly JsonDocumentOptions StrictJson = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
    };

    private readonly string _sourceFile;
    private readonly List<ContentDiagnostic> _diagnostics;
    private bool _rejected;

    private SymbolReader(string sourceFile, List<ContentDiagnostic> diagnostics)
    {
        _sourceFile = sourceFile;
        _diagnostics = diagnostics;
    }

    public static Symbol? Read(ContentFile file, List<ContentDiagnostic> diagnostics) =>
        new SymbolReader(file.Name, diagnostics).ReadSymbol(file.Text);

    private Symbol? ReadSymbol(string text)
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

    private Symbol? ReadRoot(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            Reject(JsonPath.Root, "A symbol file must contain a JSON object.");
            return null;
        }

        RejectUnknownFields(root, JsonPath.Root, SymbolFields.Root);

        if (!HasSupportedSchemaVersion(root) || !HasSymbolKind(root))
        {
            return null;
        }

        var componentId = ReadComponentId(root);
        var style = ReadStyle(root);
        var primitives = ReadPrimitives(root);
        TryReadLabels(root, out var designatorAnchor, out var valueAnchor);

        var symbol = new Symbol(componentId, style, primitives, designatorAnchor, valueAnchor);

        return _rejected ? null : symbol;
    }

    private bool HasSupportedSchemaVersion(JsonElement root)
    {
        if (!TryGetField(root, JsonPath.Root, SymbolFields.SchemaVersion, JsonValueKind.Number, out var element))
        {
            return false;
        }

        if (element.TryGetInt32(out var version) && version == SupportedSchemaVersion)
        {
            return true;
        }

        Reject(
            JsonPath.Field(JsonPath.Root, SymbolFields.SchemaVersion),
            $"Unsupported schema version. This build reads version {SupportedSchemaVersion}.");

        return false;
    }

    private bool HasSymbolKind(JsonElement root)
    {
        if (!TryGetField(root, JsonPath.Root, SymbolFields.Kind, JsonValueKind.String, out var element))
        {
            return false;
        }

        if (element.ValueEquals(SymbolVocabulary.SymbolKind))
        {
            return true;
        }

        Reject(
            JsonPath.Field(JsonPath.Root, SymbolFields.Kind),
            $"A symbol file must declare kind '{SymbolVocabulary.SymbolKind}'.");

        return false;
    }

    private string ReadComponentId(JsonElement root)
    {
        var id = ReadRequiredText(root, JsonPath.Root, SymbolFields.Component);

        if (id.Length == 0 || ComponentNameRules.IsComponentId(id))
        {
            return id;
        }

        Reject(
            JsonPath.Field(JsonPath.Root, SymbolFields.Component),
            "A component id may contain only lowercase letters, digits and hyphens.");

        return string.Empty;
    }

    private SymbolStyle ReadStyle(JsonElement root)
    {
        var name = ReadRequiredText(root, JsonPath.Root, SymbolFields.Style);

        if (SymbolVocabulary.TryStyle(name, out var style))
        {
            return style;
        }

        if (name.Length > 0)
        {
            Reject(
                JsonPath.Field(JsonPath.Root, SymbolFields.Style),
                $"Unknown style '{name}'. Known styles are {SymbolVocabulary.StyleNames}.");
        }

        return SymbolStyle.Iec;
    }

    private ImmutableArray<SymbolPrimitive> ReadPrimitives(JsonElement root)
    {
        if (!TryGetField(root, JsonPath.Root, SymbolFields.Primitives, JsonValueKind.Array, out var array))
        {
            return [];
        }

        var path = JsonPath.Field(JsonPath.Root, SymbolFields.Primitives);
        var primitives = ImmutableArray.CreateBuilder<SymbolPrimitive>();
        var index = 0;

        foreach (var element in array.EnumerateArray())
        {
            var primitive = ReadPrimitive(element, JsonPath.Element(path, index));
            index++;

            if (primitive is not null)
            {
                primitives.Add(primitive);
            }
        }

        return primitives.ToImmutable();
    }

    private SymbolPrimitive? ReadPrimitive(JsonElement element, string path)
    {
        if (!RequireObject(element, path))
        {
            return null;
        }

        var type = ReadRequiredText(element, path, SymbolFields.Type);

        switch (type)
        {
            case SymbolVocabulary.LineType:
                return ReadLine(element, path);

            case SymbolVocabulary.PolylineType:
                return ReadPolyline(element, path);

            case SymbolVocabulary.RectType:
                return ReadRect(element, path);

            case SymbolVocabulary.CircleType:
                return ReadCircle(element, path);

            case SymbolVocabulary.ArcType:
                return ReadArc(element, path);

            case SymbolVocabulary.PolygonType:
                return ReadPolygon(element, path);

            case SymbolVocabulary.TextType:
                return ReadText(element, path);

            case "":
                return null;

            default:
                Reject(
                    JsonPath.Field(path, SymbolFields.Type),
                    $"Unknown primitive type '{type}'. "
                    + $"Known types are {SymbolVocabulary.PrimitiveTypeNames}.");

                return null;
        }
    }

    private SymbolPrimitive? ReadLine(JsonElement element, string path)
    {
        RejectUnknownFields(element, path, SymbolFields.Line);

        var hasFrom = TryReadPoint(element, path, SymbolFields.From, out var from);
        var hasTo = TryReadPoint(element, path, SymbolFields.To, out var to);

        return hasFrom && hasTo ? new SymbolLine(from, to) : null;
    }

    private SymbolPrimitive? ReadPolyline(JsonElement element, string path)
    {
        RejectUnknownFields(element, path, SymbolFields.Polyline);

        var hasPoints = TryReadPoints(element, path, out var points);
        var hasClosed = TryReadRequiredFlag(element, path, SymbolFields.Closed, out var closed);

        return hasPoints && hasClosed ? new SymbolPolyline(points, closed) : null;
    }

    private SymbolPrimitive? ReadRect(JsonElement element, string path)
    {
        RejectUnknownFields(element, path, SymbolFields.Rect);

        var hasX = TryReadCoordinateField(element, path, SymbolFields.X, out var x);
        var hasY = TryReadCoordinateField(element, path, SymbolFields.Y, out var y);
        var hasWidth = TryReadSize(element, path, SymbolFields.Width, out var width);
        var hasHeight = TryReadSize(element, path, SymbolFields.Height, out var height);

        return hasX && hasY && hasWidth && hasHeight ? new SymbolRect(x, y, width, height) : null;
    }

    private SymbolPrimitive? ReadCircle(JsonElement element, string path)
    {
        RejectUnknownFields(element, path, SymbolFields.Circle);

        var hasCenter = TryReadPoint(element, path, SymbolFields.Center, out var center);
        var hasRadius = TryReadSize(element, path, SymbolFields.Radius, out var radius);
        var hasFilled = TryReadOptionalFlag(element, path, SymbolFields.Filled, out var filled);

        return hasCenter && hasRadius && hasFilled ? new SymbolCircle(center, radius, filled) : null;
    }

    private SymbolPrimitive? ReadArc(JsonElement element, string path)
    {
        RejectUnknownFields(element, path, SymbolFields.Arc);

        var hasCenter = TryReadPoint(element, path, SymbolFields.Center, out var center);
        var hasRadius = TryReadSize(element, path, SymbolFields.Radius, out var radius);
        var hasStart = TryReadAngle(element, path, SymbolFields.StartDegrees, out var start);
        var hasSweep = TryReadAngle(element, path, SymbolFields.SweepDegrees, out var sweep);

        if (hasSweep && sweep == 0)
        {
            Reject(JsonPath.Field(path, SymbolFields.SweepDegrees), "An arc sweep must not be zero.");
            hasSweep = false;
        }

        return hasCenter && hasRadius && hasStart && hasSweep
            ? new SymbolArc(center, radius, start, sweep)
            : null;
    }

    private SymbolPrimitive? ReadPolygon(JsonElement element, string path)
    {
        RejectUnknownFields(element, path, SymbolFields.Polygon);

        var hasPoints = TryReadPoints(element, path, out var points);
        var hasFilled = TryReadOptionalFlag(element, path, SymbolFields.Filled, out var filled);

        return hasPoints && hasFilled ? new SymbolPolygon(points, filled) : null;
    }

    private SymbolPrimitive? ReadText(JsonElement element, string path)
    {
        RejectUnknownFields(element, path, SymbolFields.Text);

        var hasAt = TryReadPoint(element, path, SymbolFields.At, out var at);
        var value = ReadRequiredText(element, path, SymbolFields.Value);

        return hasAt && value.Length > 0 ? new SymbolText(at, value) : null;
    }

    private bool TryReadLabels(JsonElement root, out GridVector designator, out GridVector? value)
    {
        designator = GridVector.Zero;
        value = null;

        if (!TryGetField(root, JsonPath.Root, SymbolFields.Labels, JsonValueKind.Object, out var labels))
        {
            return false;
        }

        var path = JsonPath.Field(JsonPath.Root, SymbolFields.Labels);

        RejectUnknownFields(labels, path, SymbolFields.Label);

        if (!TryReadPoint(labels, path, SymbolFields.Designator, out designator))
        {
            return false;
        }

        if (!labels.TryGetProperty(SymbolFields.Value, out _))
        {
            return true;
        }

        if (!TryReadPoint(labels, path, SymbolFields.Value, out var anchor))
        {
            return false;
        }

        value = anchor;

        return true;
    }

    private bool TryReadPoint(JsonElement owner, string ownerPath, string fieldName, out GridVector point)
    {
        point = GridVector.Zero;

        return TryGetField(owner, ownerPath, fieldName, JsonValueKind.Array, out var array)
            && TryReadPointElement(array, JsonPath.Field(ownerPath, fieldName), out point);
    }

    private bool TryReadPointElement(JsonElement element, string path, out GridVector point)
    {
        point = GridVector.Zero;

        if (element.ValueKind != JsonValueKind.Array || element.GetArrayLength() != PointComponentCount)
        {
            Reject(path, "A point must be an array of two numbers.");
            return false;
        }

        var values = new double[PointComponentCount];
        var index = 0;

        foreach (var component in element.EnumerateArray())
        {
            if (!TryReadCoordinate(component, JsonPath.Element(path, index), out values[index]))
            {
                return false;
            }

            index++;
        }

        point = new GridVector(values[0], values[1]);

        return true;
    }

    private bool TryReadPoints(JsonElement owner, string ownerPath, out ImmutableArray<GridVector> points)
    {
        points = [];

        if (!TryGetField(owner, ownerPath, SymbolFields.Points, JsonValueKind.Array, out var array))
        {
            return false;
        }

        var path = JsonPath.Field(ownerPath, SymbolFields.Points);

        if (array.GetArrayLength() < MinimumPointCount)
        {
            Reject(path, $"A point list needs at least {MinimumPointCount} points.");
            return false;
        }

        var builder = ImmutableArray.CreateBuilder<GridVector>(array.GetArrayLength());
        var index = 0;

        foreach (var element in array.EnumerateArray())
        {
            if (!TryReadPointElement(element, JsonPath.Element(path, index), out var point))
            {
                return false;
            }

            builder.Add(point);
            index++;
        }

        points = builder.MoveToImmutable();

        return true;
    }

    private bool TryReadCoordinateField(
        JsonElement owner, string ownerPath, string fieldName, out double value)
    {
        value = 0;

        return TryGetField(owner, ownerPath, fieldName, JsonValueKind.Number, out var element)
            && TryReadCoordinate(element, JsonPath.Field(ownerPath, fieldName), out value);
    }

    private bool TryReadCoordinate(JsonElement element, string path, out double value)
    {
        value = 0;

        if (element.ValueKind != JsonValueKind.Number)
        {
            Reject(path, "A coordinate must be a number.");
            return false;
        }

        if (!element.TryGetDouble(out value) || !double.IsFinite(value))
        {
            Reject(path, "A coordinate must be a finite number.");
            return false;
        }

        if (Math.Abs(value) > CoordinateLimit)
        {
            Reject(path, $"A coordinate must lie within {CoordinateLimit} grid units of the origin.");
            return false;
        }

        return true;
    }

    private bool TryReadSize(JsonElement owner, string ownerPath, string fieldName, out double value)
    {
        value = 0;

        if (!TryGetField(owner, ownerPath, fieldName, JsonValueKind.Number, out var element))
        {
            return false;
        }

        var path = JsonPath.Field(ownerPath, fieldName);

        if (!element.TryGetDouble(out value) || !double.IsFinite(value))
        {
            Reject(path, $"Field '{fieldName}' must be a finite number.");
            return false;
        }

        if (value <= 0 || value > CoordinateLimit)
        {
            Reject(
                path,
                $"Field '{fieldName}' must be greater than 0 and at most {CoordinateLimit}.");

            return false;
        }

        return true;
    }

    private bool TryReadAngle(JsonElement owner, string ownerPath, string fieldName, out double value)
    {
        value = 0;

        if (!TryGetField(owner, ownerPath, fieldName, JsonValueKind.Number, out var element))
        {
            return false;
        }

        if (element.TryGetDouble(out value) && double.IsFinite(value))
        {
            return true;
        }

        Reject(
            JsonPath.Field(ownerPath, fieldName),
            $"Field '{fieldName}' must be a finite number of degrees.");

        return false;
    }

    private bool TryReadOptionalFlag(
        JsonElement owner, string ownerPath, string fieldName, out bool value)
    {
        value = false;

        return !owner.TryGetProperty(fieldName, out var field)
            || ReadFlag(field, JsonPath.Field(ownerPath, fieldName), fieldName, out value);
    }

    private bool TryReadRequiredFlag(
        JsonElement owner, string ownerPath, string fieldName, out bool value)
    {
        value = false;

        if (!owner.TryGetProperty(fieldName, out var field))
        {
            Reject(
                JsonPath.Field(ownerPath, fieldName),
                $"Required field '{fieldName}' is missing.");

            return false;
        }

        return ReadFlag(field, JsonPath.Field(ownerPath, fieldName), fieldName, out value);
    }

    private bool ReadFlag(JsonElement field, string path, string fieldName, out bool value)
    {
        value = false;

        if (field.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            value = field.GetBoolean();
            return true;
        }

        Reject(path, $"Field '{fieldName}' must be true or false.");

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
            Reject(
                JsonPath.Field(ownerPath, fieldName),
                $"Required field '{fieldName}' is missing.");

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

    private void Reject(string path, string message)
    {
        _rejected = true;
        _diagnostics.Add(ContentDiagnostic.Error(_sourceFile, path, message));
    }
}
