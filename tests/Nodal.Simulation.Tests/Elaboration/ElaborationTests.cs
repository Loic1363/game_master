using Nodal.Circuit.Schematics;
using Nodal.Foundation.Waveforms;
using Nodal.Simulation.Elaboration;
using Xunit;

namespace Nodal.Simulation.Tests.Elaboration;

public class ElaborationTests
{
    private static Schematic Passive(string definitionId) => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(definitionId, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 2)
        .Wire(0, -2, 6, -2);

    private static Schematic Source(string definitionId) => Schematic.Empty
        .Add(definitionId, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 2)
        .Wire(0, -2, 6, -2);

    private static Primitive Only(ElaboratedCircuit circuit, string designator, PrimitiveRole role)
    {
        var component = circuit.Components.Single(
            candidate => string.Equals(candidate.Designator, designator, StringComparison.Ordinal));

        return circuit.Primitives
            .Where((_, index) => component.Primitives.Contains(index))
            .Single(primitive => primitive.Role == role);
    }

    [Fact]
    public void AResistorBecomesOnePrimitiveAcrossItsOwnNets()
    {
        var circuit = Passive(Circuits.Resistor).Elaborate();
        var resistor = Only(circuit, "R1", PrimitiveRole.Principal);

        Assert.Equal(PrimitiveKind.Resistor, resistor.Kind);
        Assert.Equal(1000, resistor.Value);
        Assert.Equal("N1", resistor.PlusNode);
        Assert.Equal("0", resistor.MinusNode);
        Assert.Empty(circuit.InternalNodes);
    }

    [Fact]
    public void GroundContributesNoPrimitive()
    {
        var circuit = Passive(Circuits.Resistor).Elaborate();

        Assert.DoesNotContain(
            circuit.Components,
            component => component.Kind == ComponentKind.Ground);
    }

    [Fact]
    public void ADiodeBecomesOnePrimitive()
    {
        var circuit = Passive(Circuits.Diode).Elaborate();

        Assert.Equal(PrimitiveKind.Diode, Only(circuit, "D1", PrimitiveRole.Principal).Kind);
    }

    [Fact]
    public void ACapacitorEsrFollowsTheCapacitorThroughOneInternalNode()
    {
        var circuit = Passive(Circuits.Capacitor).Set("C1", "esr", 0.05).Elaborate();
        var capacitor = Only(circuit, "C1", PrimitiveRole.Principal);
        var esr = Only(circuit, "C1", PrimitiveRole.Esr);

        Assert.Equal(["C1_int"], circuit.InternalNodes);
        Assert.Equal("N1", capacitor.PlusNode);
        Assert.Equal("C1_int", capacitor.MinusNode);
        Assert.Equal(PrimitiveKind.Resistor, esr.Kind);
        Assert.Equal(0.05, esr.Value);
        Assert.Equal("C1_int", esr.PlusNode);
        Assert.Equal("0", esr.MinusNode);
    }

    [Fact]
    public void ACapacitorEslIsAnInductorInSeries()
    {
        var circuit = Passive(Circuits.Capacitor).Set("C1", "esl", 1e-9).Elaborate();
        var esl = Only(circuit, "C1", PrimitiveRole.Esl);

        Assert.Equal(["C1_int"], circuit.InternalNodes);
        Assert.Equal(PrimitiveKind.Inductor, esl.Kind);
        Assert.Equal(1e-9, esl.Value);
        Assert.Equal("C1_int", esl.PlusNode);
        Assert.Equal("0", esl.MinusNode);
    }

    [Fact]
    public void TwoSeriesParasiticsGetNumberedInternalNodes()
    {
        var circuit = Passive(Circuits.Capacitor)
            .Set("C1", "esr", 0.05)
            .Set("C1", "esl", 1e-9)
            .Elaborate();

        Assert.Equal(["C1_int1", "C1_int2"], circuit.InternalNodes);
        Assert.Equal("C1_int1", Only(circuit, "C1", PrimitiveRole.Principal).MinusNode);
        Assert.Equal("C1_int1", Only(circuit, "C1", PrimitiveRole.Esr).PlusNode);
        Assert.Equal("C1_int2", Only(circuit, "C1", PrimitiveRole.Esr).MinusNode);
        Assert.Equal("C1_int2", Only(circuit, "C1", PrimitiveRole.Esl).PlusNode);
        Assert.Equal("0", Only(circuit, "C1", PrimitiveRole.Esl).MinusNode);
    }

    [Fact]
    public void ACapacitorParallelResistanceSitsAcrossTheSameNets()
    {
        var circuit = Passive(Circuits.Capacitor)
            .Set("C1", "parallel-resistance", 1e6)
            .Elaborate();

        var parallel = Only(circuit, "C1", PrimitiveRole.ParallelResistance);

        Assert.Empty(circuit.InternalNodes);
        Assert.Equal(PrimitiveKind.Resistor, parallel.Kind);
        Assert.Equal(1e6, parallel.Value);
        Assert.Equal("N1", parallel.PlusNode);
        Assert.Equal("0", parallel.MinusNode);
    }

    [Fact]
    public void ACapacitorParallelCapacitanceSitsAcrossTheSameNets()
    {
        var circuit = Passive(Circuits.Capacitor)
            .Set("C1", "parallel-capacitance", 1e-12)
            .Elaborate();

        var parallel = Only(circuit, "C1", PrimitiveRole.ParallelCapacitance);

        Assert.Equal(PrimitiveKind.Capacitor, parallel.Kind);
        Assert.Equal(1e-12, parallel.Value);
        Assert.Equal("N1", parallel.PlusNode);
        Assert.Equal("0", parallel.MinusNode);
    }

    [Fact]
    public void AnInductorSeriesResistanceFollowsTheInductor()
    {
        var circuit = Passive(Circuits.Inductor)
            .Set("L1", "series-resistance", 2)
            .Set("L1", "parallel-capacitance", 2e-12)
            .Elaborate();

        Assert.Equal(["L1_int"], circuit.InternalNodes);
        Assert.Equal("L1_int", Only(circuit, "L1", PrimitiveRole.Principal).MinusNode);
        Assert.Equal("L1_int", Only(circuit, "L1", PrimitiveRole.SeriesResistance).PlusNode);
        Assert.Equal("0", Only(circuit, "L1", PrimitiveRole.SeriesResistance).MinusNode);
        Assert.Equal("N1", Only(circuit, "L1", PrimitiveRole.ParallelCapacitance).PlusNode);
    }

    [Fact]
    public void AVoltageSourceSeriesResistanceUsesAnInternalNode()
    {
        var circuit = Source(Circuits.VoltageSource)
            .Set("V1", "series-resistance", 10)
            .Elaborate();

        Assert.Equal(["V1_int"], circuit.InternalNodes);
        Assert.Equal("V1_int", Only(circuit, "V1", PrimitiveRole.Principal).MinusNode);
        Assert.Equal("V1_int", Only(circuit, "V1", PrimitiveRole.SeriesResistance).PlusNode);
    }

    [Fact]
    public void ACurrentSourceParallelResistanceSitsAcrossTheSameNets()
    {
        var circuit = Source(Circuits.CurrentSource)
            .Set("I1", "parallel-resistance", 2200)
            .Elaborate();

        Assert.Empty(circuit.InternalNodes);
        Assert.Equal(2200, Only(circuit, "I1", PrimitiveRole.ParallelResistance).Value);
    }

    [Fact]
    public void ASourceFunctionTravelsOnThePrincipalPrimitive()
    {
        var waveform = new PulseWaveform(0, 12, 0, 1e-9, 1e-9, 0.02, 0.04);
        var circuit = Source(Circuits.VoltageSource).Function("V1", waveform).Elaborate();
        var source = Only(circuit, "V1", PrimitiveRole.Principal);

        Assert.Equal(waveform, source.Function);
        Assert.Equal(0, source.ValueAt(0));
        Assert.Equal(12, source.ValueAt(0.01));
    }

    [Fact]
    public void APrimitiveWithoutAFunctionKeepsItsConstantValue()
    {
        var circuit = Source(Circuits.VoltageSource).Set("V1", "voltage", 9).Elaborate();
        var source = Only(circuit, "V1", PrimitiveRole.Principal);

        Assert.Null(source.Function);
        Assert.Equal(9, source.ValueAt(0));
        Assert.Equal(9, source.ValueAt(1));
    }

    [Fact]
    public void InternalNodesAreNodesButNotExternalNets()
    {
        var circuit = Passive(Circuits.Capacitor).Set("C1", "esr", 0.05).Elaborate();

        Assert.Contains("C1_int", circuit.Nodes);
        Assert.DoesNotContain("C1_int", circuit.ExternalNets);
        Assert.True(circuit.IsInternal("C1_int"));
        Assert.False(circuit.IsInternal("N1"));
        Assert.All(circuit.ExternalNets, net => Assert.False(circuit.IsInternal(net)));
    }

    [Fact]
    public void InternalNodesNeverReachTheOperatingPoint()
    {
        var solution = Passive(Circuits.Capacitor)
            .Set("C1", "esr", 0.05)
            .Set("C1", "esl", 1e-9)
            .Solve();

        Assert.True(solution.IsSolved);
        Assert.DoesNotContain("C1_int1", solution.NetVoltages.Keys);
        Assert.DoesNotContain("C1_int2", solution.NetVoltages.Keys);
        Assert.Equal(["0", "N1"], solution.NetVoltages.Keys.Order(StringComparer.Ordinal));
        Assert.Empty(solution.Warnings);
    }

    [Fact]
    public void EveryParasiticOfAComponentBelongsToItsOwnComponent()
    {
        var circuit = Passive(Circuits.Capacitor)
            .Set("C1", "esr", 0.05)
            .Set("C1", "parallel-resistance", 1e6)
            .Elaborate();

        var capacitor = circuit.Components.Single(
            component => component.Kind == ComponentKind.Capacitor);

        Assert.Equal(3, capacitor.Primitives.Length);
        Assert.All(
            capacitor.Primitives,
            index => Assert.Equal("C1", circuit.Primitives[index].Designator));
    }

    [Fact]
    public void TheTerminalPrimitivesAreTheFirstOfTheChainAndEveryShunt()
    {
        var circuit = Passive(Circuits.Capacitor)
            .Set("C1", "esr", 0.05)
            .Set("C1", "parallel-resistance", 1e6)
            .Elaborate();

        var capacitor = circuit.Components.Single(
            component => component.Kind == ComponentKind.Capacitor);

        var roles = capacitor.TerminalPrimitives
            .Select(index => circuit.Primitives[index].Role)
            .Order()
            .ToArray();

        Assert.Equal([PrimitiveRole.Principal, PrimitiveRole.ParallelResistance], roles);
    }

    [Fact]
    public void ACapacitorWithALeakageResistanceCarriesItsLeakageCurrent()
    {
        var solution = Passive(Circuits.Capacitor)
            .Set("C1", "parallel-resistance", 1e6)
            .Set("V1", "voltage", 10)
            .Solve();

        Approximately.Equal(10e-6, solution.Amperes("C1"), 1e-9);
    }

    [Fact]
    public void ACapacitorWithOnlySeriesParasiticsCarriesNoDcCurrent()
    {
        var solution = Passive(Circuits.Capacitor)
            .Set("C1", "esr", 0.05)
            .Set("V1", "voltage", 10)
            .Solve();

        Assert.Equal(0, solution.Amperes("C1"));
    }
}
