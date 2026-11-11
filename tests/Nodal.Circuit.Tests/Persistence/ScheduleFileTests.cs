using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Persistence;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Nodal.Foundation.Schedules;
using Xunit;

namespace Nodal.Circuit.Tests.Persistence;

public class ScheduleFileTests
{
    [Fact]
    public void AReleasedScheduleIsWrittenAsAnEmptyPressList() =>
        Assert.Contains(
            Lines(
                "        \"presses\": {",
                "          \"schedule\": {",
                "            \"presses\": []",
                "          }",
                "        }"),
            Written(PressSchedule.Released),
            StringComparison.Ordinal);

    [Fact]
    public void APressedScheduleIsWrittenAsOnePressWithoutAnEnd() =>
        Assert.Contains(
            Lines(
                "            \"presses\": [",
                "              [",
                "                0",
                "              ]",
                "            ]"),
            Written(PressSchedule.Pressed),
            StringComparison.Ordinal);

    [Fact]
    public void PressesAreWrittenAsExactNumbersNeverAsText()
    {
        var text = Written(Parsed("1m to 3m, 5m to 6m"));

        Assert.Contains("0.001", text, StringComparison.Ordinal);
        Assert.Contains("0.003", text, StringComparison.Ordinal);
        Assert.Contains("0.005", text, StringComparison.Ordinal);
        Assert.Contains("0.006", text, StringComparison.Ordinal);
        Assert.DoesNotContain("1m to 3m", text, StringComparison.Ordinal);
        Assert.DoesNotContain("released", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("released")]
    [InlineData("pressed")]
    [InlineData("1m to 3m")]
    [InlineData("1m to 3m, 5m to 6m")]
    [InlineData("0 to 1")]
    public void AScheduleSurvivesSavingAndOpening(string text)
    {
        var schedule = Parsed(text);
        var file = SchematicFile.Write(Built(schedule));
        var result = SchematicFile.Read(file, WidgetCatalog.Catalog);

        Assert.True(result.Loaded, Listed(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(
            ParameterValue.Of(schedule, Unit.Second), ValueOf(result.Schematic!));
        Assert.Equal(text, ValueOf(result.Schematic!).ToString());
    }

    [Theory]
    [InlineData("""{ "schedule": { "presses": [[0.003, 0.001]] } }""", "must end after it starts")]
    [InlineData(
        """{ "schedule": { "presses": [[0.005, 0.006], [0.001, 0.003]] } }""",
        "increasing time order")]
    [InlineData("""{ "schedule": { "presses": [[-0.001, 0.003]] } }""", "cannot start before 0 s")]
    [InlineData("""{ "schedule": { "presses": [[0.001]] } }""", "unrecognized press schedule")]
    [InlineData("""{ "schedule": { "presses": [[0, 1, 2]] } }""", "one or two finite numbers")]
    [InlineData("""{ "schedule": { "presses": [[0, "late"]] } }""", "one or two finite numbers")]
    [InlineData("""{ "schedule": { "presses": {} } }""", "needs 'presses' as an array")]
    [InlineData("""{ "schedule": {} }""", "needs 'presses' as an array")]
    [InlineData("""{ "schedule": 3 }""", "'schedule' must be an object")]
    [InlineData("""{ "presses": [] }""", "exactly one field named 'schedule'")]
    [InlineData("""{ "schedule": { "presses": [] }, "extra": 1 }""", "exactly one field")]
    [InlineData("12", "must be an object")]
    public void ABadScheduleIsResetToTheDefaultWithAWarning(string stored, string messagePart)
    {
        var result = Read(stored);

        Assert.True(result.Loaded, Listed(result));
        Assert.Equal(
            ParameterValue.Of(PressSchedule.Released, Unit.Second),
            ValueOf(result.Schematic!));

        var warning = Assert.Single(result.Warnings);

        Assert.Contains(messagePart, warning.Message, StringComparison.Ordinal);
        Assert.Contains("reset to released", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AScheduleAtThePressLimitIsRead()
    {
        var result = Read(StoredPresses(PressSchedule.MaxPresses));

        Assert.True(result.Loaded, Listed(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(
            PressSchedule.MaxPresses, ValueOf(result.Schematic!).Schedule.Presses.Length);
    }

    [Fact]
    public void AScheduleAboveThePressLimitIsResetToTheDefault()
    {
        var result = Read(StoredPresses(PressSchedule.MaxPresses + 1));

        Assert.True(result.Loaded, Listed(result));
        Assert.True(ValueOf(result.Schematic!).Schedule.IsReleased);

        var warning = Assert.Single(result.Warnings);

        Assert.Contains(
            "a schedule has at most 1,000 presses", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingScheduleTakesTheDefault()
    {
        var result = Read(null);

        Assert.True(result.Loaded, Listed(result));
        Assert.True(ValueOf(result.Schematic!).Schedule.IsReleased);
        Assert.Contains(
            "presses was missing; set to the default released",
            Assert.Single(result.Warnings).Message,
            StringComparison.Ordinal);
    }

    private static string Lines(params string[] lines) => string.Join("\n", lines);

    private static string Written(PressSchedule schedule) => SchematicFile.Write(Built(schedule));

    private static Schematic Built(PressSchedule schedule)
    {
        var placed = new AddComponent(
                WidgetCatalog.ButtonId, new GridPoint(0, 0), Orientation.Default)
            .Succeed(Schematic.Empty, WidgetCatalog.Catalog);

        var id = placed.Components.Values.Single().Id;

        return new SetParameter(
                id, WidgetCatalog.ButtonSchedule, ParameterValue.Of(schedule, Unit.Second))
            .Succeed(placed, WidgetCatalog.Catalog);
    }

    private static SchematicReadResult Read(string? storedSchedule)
    {
        var parameters = storedSchedule is null
            ? """{ "on-resistance": 0.01 }"""
            : $$"""{ "on-resistance": 0.01, "presses": {{storedSchedule}} }""";

        return SchematicFile.Read(
            SchematicJson.Single(WidgetCatalog.ButtonId, "SW1", parameters).ToString(),
            WidgetCatalog.Catalog);
    }

    private static ParameterValue ValueOf(Schematic schematic) =>
        schematic.Components.Values.Single().Parameters[WidgetCatalog.ButtonSchedule];

    private static PressSchedule Parsed(string text)
    {
        Assert.True(PressScheduleText.TryParse(text, out var schedule, out _), text);

        return schedule!;
    }

    private static string StoredPresses(int count)
    {
        var presses = string.Join(
            ",",
            Enumerable.Range(1, count).Select(index => $"[{index * 2},{(index * 2) + 1}]"));

        return $$"""{ "schedule": { "presses": [{{presses}}] } }""";
    }

    private static string Listed(SchematicReadResult result) =>
        string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.ToString()));
}
