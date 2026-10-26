using Nodal.Circuit.Netlists;
using Nodal.Circuit.Persistence;
using Nodal.Content.Components;
using Nodal.Content.Sources;
using Nodal.Simulation.Spice;

namespace Nodal.Cli;

public static class Program
{
    private const string ExportCommand = "export";
    private const string ProbesFileName = "probes.json";
    private const int ExportArgumentCount = 4;
    private const int Success = 0;
    private const int Failure = 1;
    private const int BadUsage = 2;

    private const string Usage =
        "usage: Nodal.Cli export <schematic.nodal> <deck.cir> <components-directory>";

    public static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length != ExportArgumentCount
            || !string.Equals(args[0], ExportCommand, StringComparison.Ordinal))
        {
            Console.Error.WriteLine(Usage);

            return BadUsage;
        }

        try
        {
            return Export(args[1], args[2], args[3]);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or InvalidOperationException)
        {
            Console.Error.WriteLine(exception.Message);

            return Failure;
        }
    }

    private static int Export(string schematicPath, string deckPath, string componentsDirectory)
    {
        if (!TryLoadCatalog(componentsDirectory, out var catalog))
        {
            return Failure;
        }

        var result = SchematicFile.Read(File.ReadAllText(schematicPath), catalog);

        if (!result.Loaded)
        {
            Report($"{schematicPath} could not be read:", result.Errors.Select(Described));

            return Failure;
        }

        var netlist = NetlistCompiler.Compile(result.Schematic, catalog);

        if (!SpiceExporter.TryExport(netlist, catalog, out var deck, out var refusals))
        {
            Report(
                $"{schematicPath} cannot be exported:",
                refusals.Select(diagnostic => diagnostic.Message));

            return Failure;
        }

        if (!SpiceProbes.TryPlan(netlist, catalog, out var plan, out refusals))
        {
            Report(
                $"{schematicPath} has no probe plan:",
                refusals.Select(diagnostic => diagnostic.Message));

            return Failure;
        }

        var probesPath = Path.Combine(
            Path.GetDirectoryName(deckPath) ?? string.Empty, ProbesFileName);

        File.WriteAllText(deckPath, deck);
        File.WriteAllText(probesPath, ProbeJson.Write(plan, result.Schematic.Analysis));

        Console.WriteLine($"Exported {schematicPath} to {deckPath} and {probesPath}");

        return Success;
    }

    private static bool TryLoadCatalog(string directory, out ComponentCatalog catalog)
    {
        var result = ComponentCatalogLoader.Load(new DirectoryContentSource(directory));

        catalog = result.Catalog;

        if (result.Diagnostics.Length == 0)
        {
            return true;
        }

        Report(
            $"{directory} is not a valid component catalog:",
            result.Diagnostics.Select(diagnostic => diagnostic.ToString()));

        return false;
    }

    private static void Report(string headline, IEnumerable<string> lines)
    {
        Console.Error.WriteLine(headline);

        foreach (var line in lines)
        {
            Console.Error.WriteLine("  " + line);
        }
    }

    private static string Described(SchematicDiagnostic diagnostic) =>
        $"{diagnostic.Location}: {diagnostic.Message}";
}
