using Nodal.Circuit.Persistence;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Simulation.Tests.Reference;

internal static class ReferenceCircuits
{
    public const string SchematicFileName = "circuit.nodal";
    public const string DeckFileName = "circuit.cir";
    public const string ExpectationFileName = "expected.json";
    public const string ProbesFileName = "probes.json";

    public static TheoryData<string> Names()
    {
        var names = new TheoryData<string>();

        var folders = Directory
            .GetDirectories(RepositoryPaths.ReferenceCircuitsDirectory)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal);

        foreach (var folder in folders)
        {
            names.Add(folder);
        }

        return names;
    }

    public const string TransientExpectationFileName = "expected-transient.json";

    public static TheoryData<string> TransientNames()
    {
        var names = new TheoryData<string>();

        var folders = Directory
            .GetDirectories(RepositoryPaths.ReferenceCircuitsDirectory)
            .Where(folder => File.Exists(Path.Combine(folder, TransientExpectationFileName)))
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal);

        foreach (var folder in folders)
        {
            names.Add(folder);
        }

        return names;
    }

    public static ReferenceProbes Probes(string name) =>
        ReferenceProbes.Parse(Text(name, ProbesFileName));

    public static TransientExpectation TransientExpectation(string name) =>
        Reference.TransientExpectation.Parse(Text(name, TransientExpectationFileName));

    public static Schematic Schematic(string name)
    {
        var result = SchematicFile.Read(Text(name, SchematicFileName), BaseCatalog.Catalog);

        Assert.True(
            result.Loaded,
            $"{name}/{SchematicFileName} could not be read: "
            + string.Join("; ", result.Errors.Select(error => error.Message)));

        return result.Schematic;
    }

    public static string Deck(string name) => Text(name, DeckFileName);

    public static ReferenceExpectation Expectation(string name) =>
        ReferenceExpectation.Parse(Text(name, ExpectationFileName));

    private static string Text(string name, string fileName) => File.ReadAllText(
        Path.Combine(RepositoryPaths.ReferenceCircuitsDirectory, name, fileName));
}
