using System.Buffers;
using System.Text;
using System.Text.Json;
using Nodal.Circuit.Schematics;
using Nodal.Foundation;
using Nodal.Foundation.Quantities;

namespace Nodal.Circuit.Persistence;

internal static class SchematicWriter
{
    private static readonly JsonWriterOptions Options = new()
    {
        Indented = true,
        NewLine = SchematicFormat.LineFeed,
    };

    public static string Write(Schematic schematic)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer, Options))
        {
            WriteSchematic(writer, schematic);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan) + SchematicFormat.LineFeed;
    }

    private static void WriteSchematic(Utf8JsonWriter writer, Schematic schematic)
    {
        writer.WriteStartObject();
        writer.WriteNumber(SchematicFields.SchemaVersion, SchematicFormat.SupportedSchemaVersion);
        writer.WriteString(SchematicFields.Kind, SchematicFormat.Kind);
        writer.WriteString(SchematicFields.CreatedWith, $"{NodalInfo.Name} {NodalInfo.Version}");
        writer.WriteNumber(SchematicFields.NextComponentId, schematic.NextComponentId.Value);
        writer.WriteNumber(SchematicFields.NextWireId, schematic.NextWireId.Value);

        writer.WriteStartArray(SchematicFields.Components);

        foreach (var component in schematic.Components.Values)
        {
            WriteComponent(writer, component);
        }

        writer.WriteEndArray();

        writer.WriteStartArray(SchematicFields.Wires);

        foreach (var wire in schematic.Wires.Values)
        {
            WriteWire(writer, wire);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteComponent(Utf8JsonWriter writer, ComponentInstance component)
    {
        writer.WriteStartObject();
        writer.WriteNumber(SchematicFields.Id, component.Id.Value);
        writer.WriteString(SchematicFields.Definition, component.DefinitionId);
        writer.WriteString(SchematicFields.Designator, component.Designator);
        writer.WriteNumber(SchematicFields.X, component.Position.X);
        writer.WriteNumber(SchematicFields.Y, component.Position.Y);
        writer.WriteNumber(SchematicFields.Rotation, RotationDegrees.Of(component.Orientation.Rotation));
        writer.WriteBoolean(SchematicFields.Mirrored, component.Orientation.Mirrored);

        writer.WriteStartObject(SchematicFields.Parameters);

        foreach (var parameter in OrderedByName(component))
        {
            writer.WriteNumber(parameter.Key, parameter.Value.Value);
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteWire(Utf8JsonWriter writer, Wire wire)
    {
        writer.WriteStartObject();
        writer.WriteNumber(SchematicFields.Id, wire.Id.Value);
        WritePoint(writer, SchematicFields.From, wire.Start);
        WritePoint(writer, SchematicFields.To, wire.End);
        writer.WriteEndObject();
    }

    private static void WritePoint(Utf8JsonWriter writer, string fieldName, Geometry.GridPoint point)
    {
        writer.WriteStartArray(fieldName);
        writer.WriteNumberValue(point.X);
        writer.WriteNumberValue(point.Y);
        writer.WriteEndArray();
    }

    private static IEnumerable<KeyValuePair<string, Quantity>> OrderedByName(ComponentInstance component) =>
        component.Parameters.OrderBy(parameter => parameter.Key, StringComparer.Ordinal);
}
