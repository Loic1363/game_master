using Nodal.Circuit.Netlists;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Spice;
using Xunit;

namespace Nodal.Simulation.Tests.Reference;

public class ReferenceTests
{
    private const double RelativeTolerance = 1e-6;
    private const double VoltageTolerance = 1e-6;
    private const double CurrentTolerance = 1e-12;

    private static readonly DcOptions Tight = new()
    {
        RelTol = 1e-9,
        VnTol = 1e-12,
        AbsTol = 1e-15,
    };

    public static TheoryData<string> Circuits => ReferenceCircuits.Names();

    [Theory]
    [MemberData(nameof(Circuits))]
    public void EveryReferenceCircuitSolves(string name)
    {
        var solution = Solve(name);

        Assert.True(solution.IsSolved, $"{name} was refused: {Listed(solution)}");
        Assert.Empty(solution.Errors);
    }

    [Theory]
    [MemberData(nameof(Circuits))]
    public void NodeVoltagesMatchNgspice(string name)
    {
        var solution = Solve(name);
        var expected = ReferenceCircuits.Expectation(name);

        Assert.Equal(expected.NodeVoltages.Keys.Order(), solution.NetVoltages.Keys.Order());

        foreach (var entry in expected.NodeVoltages)
        {
            Assert.True(
                solution.TryGetNetVoltage(entry.Key, out var voltage),
                $"{name} has no net {entry.Key}.");

            Agrees(entry.Value, voltage.Value, VoltageTolerance, $"{name} net {entry.Key}");
        }
    }

    [Theory]
    [MemberData(nameof(Circuits))]
    public void ElementCurrentsMatchNgspice(string name)
    {
        var solution = Solve(name);
        var expected = ReferenceCircuits.Expectation(name);

        foreach (var entry in expected.ElementCurrents)
        {
            Agrees(
                entry.Value,
                solution.Of(entry.Key).Current.Value,
                CurrentTolerance,
                $"{name} element {entry.Key}");
        }
    }

    [Theory]
    [MemberData(nameof(Circuits))]
    public void EveryElementOfTheCircuitHasAReferenceCurrent(string name)
    {
        var expected = ReferenceCircuits.Expectation(name);
        var solved = Solve(name).Components.Values
            .Select(component => component.Designator)
            .Order(StringComparer.Ordinal);

        Assert.Equal(expected.ElementCurrents.Keys.Order(StringComparer.Ordinal), solved);
    }

    [Theory]
    [MemberData(nameof(Circuits))]
    public void TheCommittedDeckMatchesAFreshExport(string name)
    {
        var netlist = NetlistCompiler.Compile(
            ReferenceCircuits.Schematic(name), BaseCatalog.Catalog);

        Assert.Equal(
            ReferenceCircuits.Deck(name).ReplaceLineEndings("\n"),
            SpiceExporter.Export(netlist, BaseCatalog.Catalog));
    }

    [Theory]
    [MemberData(nameof(Circuits))]
    public void TheCommittedProbesMatchAFreshPlan(string name)
    {
        var netlist = NetlistCompiler.Compile(
            ReferenceCircuits.Schematic(name), BaseCatalog.Catalog);

        Assert.True(SpiceProbes.TryPlan(netlist, BaseCatalog.Catalog, out var plan, out _));

        var committed = ReferenceCircuits.Probes(name);

        Assert.Equal(plan.Nodes, committed.Nodes);
        Assert.Equal(
            plan.Components.Select(probe => probe.Designator),
            committed.Components.Select(component => component.Designator));

        for (var index = 0; index < plan.Components.Length; index++)
        {
            Assert.Equal(
                plan.Components[index].Elements, committed.Components[index].Elements);
        }
    }

    private static OperatingPoint Solve(string name) => DcAnalysis.Solve(
        NetlistCompiler.Compile(ReferenceCircuits.Schematic(name), BaseCatalog.Catalog),
        BaseCatalog.Catalog,
        Tight);

    private static void Agrees(double expected, double actual, double absolute, string what)
    {
        var allowed = Math.Max(
            absolute, RelativeTolerance * Math.Max(Math.Abs(expected), Math.Abs(actual)));

        Assert.True(
            Math.Abs(expected - actual) <= allowed,
            $"{what}: ngspice {expected:R}, Nodal {actual:R}, "
            + $"difference {Math.Abs(expected - actual):R}, allowed {allowed:R}.");
    }

    private static string Listed(OperatingPoint solution) =>
        string.Join("; ", solution.Diagnostics.Select(diagnostic => diagnostic.Message));
}
