using System.Buffers;
using System.Collections.Immutable;
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

        var analysis = Pruned(schematic);

        if (!analysis.IsEmpty)
        {
            WriteAnalysis(writer, analysis);
        }

        writer.WriteEndObject();
    }

    private static AnalysisSettings Pruned(Schematic schematic)
    {
        var analysis = schematic.Analysis;
        var kept = analysis.Probes.Where(probe => ProbeTargets.Exists(probe, schematic));

        return analysis with { Probes = [.. kept] };
    }

    private static void WriteAnalysis(Utf8JsonWriter writer, AnalysisSettings analysis)
    {
        writer.WriteStartObject(SchematicFields.Analysis);

        if (analysis.Transient is { } transient)
        {
            writer.WriteStartObject(SchematicFields.Transient);
            writer.WriteNumber(SchematicFields.StopTime, transient.StopTime);
            WriteOptionalTime(writer, SchematicFields.OutputStep, transient.OutputStep);
            WriteOptionalTime(writer, SchematicFields.MaxStep, transient.MaxStep);
            writer.WriteBoolean(SchematicFields.StartFromZero, transient.StartFromZero);
            writer.WriteEndObject();
        }

        if (!analysis.Probes.IsEmpty)
        {
            WriteProbes(writer, analysis.Probes);
        }

        writer.WriteEndObject();
    }

    private static void WriteProbes(Utf8JsonWriter writer, ImmutableArray<Probe> probes)
    {
        writer.WriteStartArray(SchematicFields.Probes);

        foreach (var probe in probes)
        {
            writer.WriteStartObject();

            switch (probe)
            {
                case PinVoltageProbe pin:
                    writer.WriteStartObject(SchematicFields.Voltage);
                    writer.WriteNumber(
                        SchematicFields.ProbeComponent, pin.Pin.ComponentId.Value);
                    writer.WriteString(SchematicFields.Pin, pin.Pin.PinName);
                    writer.WriteEndObject();
                    break;

                case WireVoltageProbe wire:
                    writer.WriteStartObject(SchematicFields.Voltage);
                    writer.WriteNumber(SchematicFields.ProbeWire, wire.Wire.Value);
                    writer.WriteEndObject();
                    break;

                case ComponentCurrentProbe current:
                    writer.WriteStartObject(SchematicFields.Current);
                    writer.WriteNumber(
                        SchematicFields.ProbeComponent, current.Component.Value);
                    writer.WriteEndObject();
                    break;
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteOptionalTime(
        Utf8JsonWriter writer, string fieldName, double? seconds)
    {
        if (seconds is { } value)
        {
            writer.WriteNumber(fieldName, value);

            return;
        }

        writer.WriteNull(fieldName);
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
            WriteParameter(writer, parameter.Key, parameter.Value);
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteParameter(Utf8JsonWriter writer, string name, ParameterValue value)
    {
        if (value.TryGetWaveform(out var waveform))
        {
            writer.WritePropertyName(name);
            WaveformJson.Write(writer, waveform);

            return;
        }

        writer.WriteNumber(name, value.Quantity.Value);
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

    private static IEnumerable<KeyValuePair<string, ParameterValue>> OrderedByName(
        ComponentInstance component) =>
        component.Parameters.OrderBy(parameter => parameter.Key, StringComparer.Ordinal);
}
