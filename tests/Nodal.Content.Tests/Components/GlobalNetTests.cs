using Nodal.Content.Components;
using Nodal.Content.Diagnostics;
using Nodal.Content.Sources;
using Xunit;
using static Nodal.Content.Tests.Components.ComponentJson;

namespace Nodal.Content.Tests.Components;

public class GlobalNetTests
{
    private const string FileName = "widget.json";

    [Fact]
    public void APinWithoutAGlobalNetDeclaresNone()
    {
        var pin = Assert.Single(Load(Pins("""{ "name": "1", "x": 0, "y": 0 }""")).Pins);

        Assert.Null(pin.GlobalNet);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("VCC")]
    [InlineData("VDD_5V")]
    [InlineData("A")]
    [InlineData("GND_2")]
    public void AValidGlobalNetNameIsKept(string name)
    {
        var json = Pins($$"""{ "name": "1", "x": 0, "y": 0, "globalNet": "{{name}}" }""");

        Assert.Equal(name, Assert.Single(Load(json).Pins).GlobalNet);
    }

    [Theory]
    [InlineData("vcc")]
    [InlineData("Vcc")]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("_VCC")]
    [InlineData("VCC-2")]
    [InlineData("VCC 2")]
    [InlineData("")]
    public void AnInvalidGlobalNetNameIsRejected(string name)
    {
        var json = Pins($$"""{ "name": "1", "x": 0, "y": 0, "globalNet": "{{name}}" }""");

        var diagnostic = Assert.Single(Reject(json));

        Assert.Equal("$.pins[0].globalNet", diagnostic.Path);
        Assert.Contains("is not a valid global net name", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AGlobalNetThatIsNotAStringIsRejected()
    {
        var json = Pins("""{ "name": "1", "x": 0, "y": 0, "globalNet": 0 }""");

        var diagnostic = Assert.Single(Reject(json));

        Assert.Equal("$.pins[0].globalNet", diagnostic.Path);
        Assert.Contains("must be a string", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyTheGroundPinDeclaresAGlobalNetInTheBaseCatalog()
    {
        var catalog = ComponentCatalogLoader
            .Load(new DirectoryContentSource(RepositoryPaths.ComponentsDirectory))
            .Catalog;

        var declared = catalog.Definitions
            .SelectMany(definition => definition.Pins.Select(pin => (definition.Id, pin)))
            .Where(entry => entry.pin.GlobalNet is not null)
            .ToArray();

        var only = Assert.Single(declared);

        Assert.Equal("ground", only.Id);
        Assert.Equal("gnd", only.pin.Name);
        Assert.Equal("0", only.pin.GlobalNet);
    }

    private static string Pins(string pinJson) =>
        ComponentJson.Valid().With("pins", "[" + pinJson + "]").ToString();

    private static ComponentDefinition Load(string json)
    {
        var result = ComponentCatalogLoader.Load(
            new InMemoryContentSource(new ContentFile(FileName, json)));

        Assert.Empty(result.Diagnostics);

        return Assert.Single(result.Catalog.Definitions);
    }

    private static IReadOnlyList<ContentDiagnostic> Reject(string json)
    {
        var result = ComponentCatalogLoader.Load(
            new InMemoryContentSource(new ContentFile(FileName, json)));

        Assert.Empty(result.Catalog.Definitions);

        return result.Diagnostics;
    }
}
