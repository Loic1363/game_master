using System.Buffers;
using System.Text;
using System.Text.Json;
using Nodal.Circuit.Schematics;
using Nodal.Simulation.Spice;

namespace Nodal.Cli;

internal static class ProbeJson
{
    private const int SchemaVersion = 1;
    private const string LineFeed = "\n";

    private static readonly JsonWriterOptions Options = new()
    {
        Indented = true,
        NewLine = LineFeed,
    };

    public static string Write(SpiceProbePlan plan, AnalysisSettings analysis)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(analysis);

        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer, Options))
        {
            WritePlan(writer, plan, analysis);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan) + LineFeed;
    }

    private static void WritePlan(
        Utf8JsonWriter writer, SpiceProbePlan plan, AnalysisSettings analysis)
    {
        writer.WriteStartObject();
        writer.WriteNumber("schemaVersion", SchemaVersion);

        writer.WriteStartArray("nodes");

        foreach (var node in plan.Nodes)
        {
            writer.WriteStringValue(node);
        }

        writer.WriteEndArray();

        writer.WriteStartArray("components");

        foreach (var component in plan.Components)
        {
            writer.WriteStartObject();
            writer.WriteString("designator", component.Designator);
            writer.WriteStartArray("elements");

            foreach (var element in component.Elements)
            {
                writer.WriteStringValue(element);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        if (analysis.Transient is { } transient)
        {
            WriteTransient(writer, transient);
        }

        writer.WriteEndObject();
    }

    private static void WriteTransient(Utf8JsonWriter writer, TransientSettings transient)
    {
        writer.WriteStartObject("transient");
        writer.WriteNumber("stopTime", transient.StopTime);
        WriteOptional(writer, "outputStep", transient.OutputStep);
        WriteOptional(writer, "maxStep", transient.MaxStep);
        writer.WriteBoolean("startFromZero", transient.StartFromZero);
        writer.WriteEndObject();
    }

    private static void WriteOptional(Utf8JsonWriter writer, string name, double? seconds)
    {
        if (seconds is { } value)
        {
            writer.WriteNumber(name, value);

            return;
        }

        writer.WriteNull(name);
    }
}
