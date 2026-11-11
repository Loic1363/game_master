using Nodal.Circuit.Netlists;
using Nodal.Circuit.Persistence;
using Nodal.Circuit.Schematics;
using Nodal.Simulation.Dc;
using Xunit;

namespace Nodal.Simulation.Tests.Reference;

internal static class ReferenceCircuits
{
    private static readonly DcOptions Tight = new()
    {
        RelTol = 1e-9,
        VnTol = 1e-12,
        AbsTol = 1e-15,
    };

    public const string SchematicFileName = "circuit.nodal";
    public const string DeckFileName = "circuit.cir";
    public const string ExpectationFileName = "expected.json";
    public const string ProbesFileName = "probes.json";

    public static TheoryData<string> Names() => Named(ExpectationFileName);

    public const string TransientExpectationFileName = "expected-transient.json";

    public static TheoryData<string> TransientNames() => Named(TransientExpectationFileName);

    public static IEnumerable<string> Folders() => Directory
        .GetDirectories(RepositoryPaths.ReferenceCircuitsDirectory)
        .Select(Path.GetFileName)
        .OfType<string>()
        .Order(StringComparer.Ordinal);

    public static bool Carries(string name, string fileName) => File.Exists(
        Path.Combine(RepositoryPaths.ReferenceCircuitsDirectory, name, fileName));

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

    public static OperatingPoint Solve(string name) => DcAnalysis.Solve(
        NetlistCompiler.Compile(Schematic(name), BaseCatalog.Catalog), BaseCatalog.Catalog, Tight);

    private static TheoryData<string> Named(string expectation)
    {
        var names = new TheoryData<string>();

        foreach (var folder in Folders().Where(name => Carries(name, expectation)))
        {
            names.Add(folder);
        }

        return names;
    }

    private static string Text(string name, string fileName) => File.ReadAllText(
        Path.Combine(RepositoryPaths.ReferenceCircuitsDirectory, name, fileName));
}
