using Nodal.Circuit.Netlists;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Reference;

public class TransientReferenceTests
{
    public static TheoryData<string> Circuits => ReferenceCircuits.TransientNames();

    [Theory]
    [MemberData(nameof(Circuits))]
    public void EveryTransientReferenceSolves(string name)
    {
        var result = Run(name);

        Assert.True(result.IsSolved, $"{name} was refused: {Listed(result)}");
        Assert.Empty(result.Errors);
    }

    [Theory]
    [MemberData(nameof(Circuits))]
    public void TheStoredSettingsDriveTheRun(string name)
    {
        var expected = ReferenceCircuits.TransientExpectation(name);
        var settings = ReferenceCircuits.Schematic(name).Analysis.Transient;

        Assert.Equal(expected.Settings, settings);
        Assert.Equal(expected.Settings.StopTime, Run(name).FinalTime);
    }

    [Theory]
    [MemberData(nameof(Circuits))]
    public void TheReferenceGridIsTheOutputStep(string name)
    {
        var expected = ReferenceCircuits.TransientExpectation(name);
        var step = expected.Settings.OutputStep!.Value;
        var first = expected.Settings.StartFromZero ? 1 : 0;

        Assert.NotEmpty(expected.Times);
        Approximately.Zero(expected.Times[0] - (first * step), 1e-18);

        for (var index = 1; index < expected.Times.Length; index++)
        {
            Approximately.Equal((index + first) * step, expected.Times[index], 1e-9);
        }
    }

    [Theory]
    [MemberData(nameof(Circuits))]
    public void NetVoltagesMatchNgspiceOverTime(string name)
    {
        var result = Run(name);
        var expected = ReferenceCircuits.TransientExpectation(name);

        Assert.NotEmpty(expected.NodeVoltages);

        foreach (var entry in expected.NodeVoltages)
        {
            Agrees(
                name,
                $"net {entry.Key}",
                expected,
                entry.Value,
                expected.AllowedFor(entry.Value, expected.VoltageFloor),
                time => result.VoltageAt(entry.Key, time));
        }
    }

    [Theory]
    [MemberData(nameof(Circuits))]
    public void ElementCurrentsMatchNgspiceOverTime(string name)
    {
        var result = Run(name);
        var expected = ReferenceCircuits.TransientExpectation(name);

        Assert.NotEmpty(expected.ElementCurrents);

        foreach (var entry in expected.ElementCurrents)
        {
            var series = result.Of(entry.Key);

            Agrees(
                name,
                $"element {entry.Key}",
                expected,
                entry.Value,
                expected.AllowedFor(entry.Value, expected.CurrentFloor),
                time => result.CurrentAt(series.Id, time));
        }
    }

    [Theory]
    [MemberData(nameof(Circuits))]
    public void EveryComponentOfTheCircuitHasAReferenceCurrent(string name)
    {
        var expected = ReferenceCircuits.TransientExpectation(name);
        var solved = Run(name).Components.Values
            .Select(series => series.Designator)
            .Order(StringComparer.Ordinal);

        Assert.Equal(expected.ElementCurrents.Keys.Order(StringComparer.Ordinal), solved);
    }

    [Theory]
    [MemberData(nameof(Circuits))]
    public void InternalNodesStayOutOfTheReference(string name)
    {
        var expected = ReferenceCircuits.TransientExpectation(name);

        Assert.DoesNotContain(
            expected.NodeVoltages.Keys,
            net => net.Contains("_int", StringComparison.Ordinal));
    }

    private static TransientResult Run(string name)
    {
        var schematic = ReferenceCircuits.Schematic(name);
        var settings = schematic.Analysis.Transient;

        Assert.NotNull(settings);

        var options = TransientOptions.From(settings) with
        {
            RelTol = 1e-6,
            VnTol = 1e-12,
            AbsTol = 1e-15,
            ChgTol = 1e-16,
            TrTol = 1,
        };

        return TransientAnalysis.Run(
            NetlistCompiler.Compile(schematic, BaseCatalog.Catalog), BaseCatalog.Catalog, options);
    }

    private static void Agrees(
        string name,
        string what,
        TransientExpectation expected,
        System.Collections.Immutable.ImmutableArray<double> reference,
        double allowed,
        Func<double, double> actual)
    {
        var worst = 0.0;
        var worstIndex = 0;
        var worstValue = reference[0];

        for (var index = 0; index < expected.Times.Length; index++)
        {
            var value = actual(expected.Times[index]);
            var difference = Math.Abs(reference[index] - value);

            if (difference > worst)
            {
                worst = difference;
                worstIndex = index;
                worstValue = value;
            }
        }

        if (worst <= allowed)
        {
            return;
        }

        Assert.Fail(
            $"{name} {what}: at t = {expected.Times[worstIndex]:R} "
            + $"ngspice {reference[worstIndex]:R}, Nodal {worstValue:R}, "
            + $"difference {worst:R}, allowed {allowed:R}.");
    }

    private static string Listed(TransientResult result) =>
        string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message));
}
