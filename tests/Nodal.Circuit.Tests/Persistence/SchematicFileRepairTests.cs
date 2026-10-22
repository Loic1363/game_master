using Nodal.Circuit.Persistence;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Circuit.Tests.Persistence;

public class SchematicFileRepairTests
{
    private const string Resistance = "resistance";
    private const string PowerRating = "power-rating";

    public static TheoryData<string, string, string> RepairedFiles
    {
        get
        {
            var data = new TheoryData<string, string, string>();

            Add(
                data,
                Resistor("""{ "resistance": 1000, "resistence": 5 }"""),
                "$.components[0].parameters.resistence",
                "Unknown parameter 'resistence' was dropped.");

            Add(
                data,
                Resistor("{}"),
                "$.components[0].parameters.resistance",
                "Required parameter 'resistance' was missing and was set to the default 1 k");

            Add(
                data,
                Resistor("""{ "resistance": 1234 }"""),
                "$.components[0].parameters.resistance",
                "does not belong to series E24. The value was reset to the default 1 k");

            Add(
                data,
                Resistor("""{ "resistance": "1k" }"""),
                "$.components[0].parameters.resistance",
                "must be a number. The value was reset to the default 1 k");

            Add(
                data,
                Resistor("""{ "resistance": 1e999 }"""),
                "$.components[0].parameters.resistance",
                "must be a finite number. The value was reset to the default 1 k");

            Add(
                data,
                Resistor("""{ "resistance": 1000, "power-rating": -1 }"""),
                "$.components[0].parameters.power-rating",
                "is below the minimum 0 W of 'power-rating'. The value was unset.");

            Add(
                data,
                Resistor("""{ "resistance": 1000, "power-rating": 1e999 }"""),
                "$.components[0].parameters.power-rating",
                "must be a finite number. The value was unset.");

            Add(
                data,
                Resistor("""{ "resistance": 1000, "power-rating": true }"""),
                "$.components[0].parameters.power-rating",
                "must be a number. The value was unset.");

            Add(
                data,
                SchematicJson.Single("flux-capacitor", "X1", """{ "resistance": 1000 }"""),
                "$.components[0].parameters.resistance",
                "The component definition 'flux-capacitor' is unknown, so the stored value 1000 "
                + "of 'resistance' was dropped.");

            Add(
                data,
                SchematicJson.Valid().With("extra", "true"),
                "$.extra",
                "Unknown field 'extra' was ignored.");

            Add(
                data,
                SchematicJson.Valid().With("createdWith", "7"),
                "$.createdWith",
                "should be a string. It was ignored.");

            Add(
                data,
                SchematicJson.Valid().With(
                    "components",
                    """
                    [
                      { "id": 1, "definition": "resistor", "designator": "R1", "x": 0, "y": 0,
                        "rotation": 0, "mirrored": false, "colour": "red",
                        "parameters": { "resistance": 1000 } }
                    ]
                    """),
                "$.components[0].colour",
                "Unknown field 'colour' was ignored.");

            Add(
                data,
                SchematicJson.Valid().With(
                    "wires",
                    """[{ "id": 1, "from": [0, 2], "to": [0, 4], "style": "thick" }]"""),
                "$.wires[0].style",
                "Unknown field 'style' was ignored.");

            return data;
        }
    }

    public static TheoryData<string, string, string> RepairedWidgetFiles
    {
        get
        {
            var data = new TheoryData<string, string, string>();

            Add(
                data,
                Widget("""{ "level": 99 }"""),
                "$.components[0].parameters.level",
                "is above the maximum 10 V of 'level'. The value was reset to the default 5 V.");

            Add(
                data,
                Widget("""{ "level": -1 }"""),
                "$.components[0].parameters.level",
                "is below the minimum 0 V of 'level'. The value was reset to the default 5 V.");

            Add(
                data,
                Widget("""{ "level": 5, "trim": 99 }"""),
                "$.components[0].parameters.trim",
                "is above the maximum 10 V of 'trim'. The value was unset.");

            Add(
                data,
                Widget("""{ "level": 5, "tuned": 1.23 }"""),
                "$.components[0].parameters.tuned",
                "does not belong to series E12. The value was unset.");

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(RepairedFiles))]
    public void ARepairableFileLoadsWithAWarning(string text, string path, string messagePart)
    {
        AssertRepaired(SchematicFile.Read(text, BaseCatalog.Catalog), path, messagePart);
    }

    [Theory]
    [MemberData(nameof(RepairedWidgetFiles))]
    public void ABoundedParameterIsRepairedWithAWarning(string text, string path, string messagePart)
    {
        AssertRepaired(SchematicFile.Read(text, WidgetCatalog.Catalog), path, messagePart);
    }

    [Fact]
    public void AMissingRequiredParameterTakesItsDefault()
    {
        var component = Only(Read(Resistor("{}")));

        Assert.Equal(new Quantity(1000, Unit.Ohm), component.Parameters[Resistance]);
    }

    [Fact]
    public void AnAbsentParameterObjectStillSeedsTheRequiredDefaults()
    {
        var text = SchematicJson.Valid().With(
            "components",
            """
            [
              { "id": 1, "definition": "resistor", "designator": "R1", "x": 0, "y": 0,
                "rotation": 0, "mirrored": false }
            ]
            """).ToString();

        var component = Only(SchematicFile.Read(text, BaseCatalog.Catalog));

        Assert.Equal(new Quantity(1000, Unit.Ohm), component.Parameters[Resistance]);
    }

    [Fact]
    public void AnInvalidRequiredValueFallsBackToTheDefault()
    {
        var component = Only(Read(Resistor("""{ "resistance": 1234 }""")));

        Assert.Equal(new Quantity(1000, Unit.Ohm), component.Parameters[Resistance]);
    }

    [Fact]
    public void AnInvalidOptionalValueIsUnset()
    {
        var component = Only(Read(Resistor("""{ "resistance": 1000, "power-rating": -1 }""")));

        Assert.False(component.Parameters.ContainsKey(PowerRating));
        Assert.Single(component.Parameters);
    }

    [Fact]
    public void AnUnknownParameterIsDropped()
    {
        var component = Only(Read(Resistor("""{ "resistance": 1000, "resistence": 5 }""")));

        Assert.Equal([Resistance], component.Parameters.Keys);
    }

    [Fact]
    public void AComponentOfAnUnknownDefinitionIsKeptWithoutItsParameters()
    {
        var text = SchematicJson.Single("flux-capacitor", "X1", """{ "resistance": 1000 }""").ToString();

        var component = Only(SchematicFile.Read(text, BaseCatalog.Catalog));

        Assert.Equal("flux-capacitor", component.DefinitionId);
        Assert.Equal("X1", component.Designator);
        Assert.Empty(component.Parameters);
    }

    [Fact]
    public void AComponentOfAnUnknownDefinitionWithoutParametersWarnsAboutNothing()
    {
        var text = SchematicJson.Single("flux-capacitor", "X1", "{}").ToString();

        var result = SchematicFile.Read(text, BaseCatalog.Catalog);

        Assert.True(result.Loaded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ARepairedFileCarriesNoErrors()
    {
        var result = Read(Resistor("""{ "resistance": 1234 }"""));

        Assert.True(result.Loaded);
        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
    }

    private static void AssertRepaired(SchematicReadResult result, string path, string messagePart)
    {
        Assert.True(result.Loaded);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == SchematicSeverity.Warning
                && diagnostic.Path == path
                && diagnostic.Message.Contains(messagePart, StringComparison.Ordinal));
    }

    private static void Add(
        TheoryData<string, string, string> data, SchematicJson json, string path, string messagePart) =>
        data.Add(json.ToString(), path, messagePart);

    private static SchematicJson Resistor(string parameters) =>
        SchematicJson.Single("resistor", "R1", parameters);

    private static SchematicJson Widget(string parameters) =>
        SchematicJson.Single(WidgetCatalog.DefinitionId, "W1", parameters);

    private static SchematicReadResult Read(SchematicJson json) =>
        SchematicFile.Read(json.ToString(), BaseCatalog.Catalog);

    private static ComponentInstance Only(SchematicReadResult result)
    {
        Assert.True(result.Loaded);

        return result.Schematic.Components.Values.Single();
    }
}
