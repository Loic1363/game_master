using Nodal.Circuit.Commands;
using Nodal.Circuit.Editing;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class ProbeCommandTests
{
    private static Schematic Circuit()
    {
        var withResistor = Edits.Succeed(
            new AddComponent("resistor", new GridPoint(0, 0), Orientation.Default),
            Schematic.Empty);

        return Edits.Succeed(
            new AddWire(new GridPoint(2, 0), new GridPoint(6, 0)), withResistor);
    }

    private static ComponentId ResistorOf(Schematic schematic) =>
        schematic.Components.Values.First().Id;

    private static WireId WireOf(Schematic schematic) => schematic.Wires.Values.First().Id;

    private static Probe PinProbe(Schematic schematic) =>
        Probe.At(new PinRef(ResistorOf(schematic), "1"));

    private static Schematic WithProbes(Schematic schematic, int count)
    {
        var result = schematic;

        for (var index = 0; index < count; index++)
        {
            var wire = Edits.Succeed(
                new AddWire(new GridPoint(0, 10 + (index * 2)), new GridPoint(4, 10 + (index * 2))),
                result);

            result = new AddProbe(Probe.At(WireIdsOf(wire).Last())).Succeed(wire);
        }

        return result;
    }

    private static IEnumerable<WireId> WireIdsOf(Schematic schematic) => schematic.Wires.Keys;

    [Fact]
    public void AnEmptySchematicHasNoProbes()
    {
        Assert.Empty(Schematic.Empty.Analysis.Probes);
        Assert.True(Schematic.Empty.Analysis.IsEmpty);
    }

    [Fact]
    public void AVoltageProbeOnAPinIsStored()
    {
        var schematic = Circuit();
        var probe = PinProbe(schematic);
        var probed = new AddProbe(probe).Succeed(schematic);

        Assert.Equal(probe, Assert.Single(probed.Analysis.Probes));
        Assert.False(probed.Analysis.IsEmpty);
    }

    [Fact]
    public void AVoltageProbeOnAWireIsStored()
    {
        var schematic = Circuit();
        var probe = Probe.At(WireOf(schematic));

        Assert.Equal(
            probe, Assert.Single(new AddProbe(probe).Succeed(schematic).Analysis.Probes));
    }

    [Fact]
    public void ACurrentProbeOnAComponentIsStored()
    {
        var schematic = Circuit();
        var probe = Probe.Through(ResistorOf(schematic));

        Assert.Equal(
            probe, Assert.Single(new AddProbe(probe).Succeed(schematic).Analysis.Probes));
    }

    [Fact]
    public void ProbesKeepTheOrderTheyWereAddedIn()
    {
        var schematic = Circuit();
        var first = PinProbe(schematic);
        var second = Probe.Through(ResistorOf(schematic));
        var third = Probe.At(WireOf(schematic));

        var probed = new AddProbe(third).Succeed(
            new AddProbe(second).Succeed(new AddProbe(first).Succeed(schematic)));

        Assert.Equal([first, second, third], probed.Analysis.Probes);
    }

    [Fact]
    public void RemovingAProbeLeavesTheOthersInOrder()
    {
        var schematic = Circuit();
        var first = PinProbe(schematic);
        var second = Probe.Through(ResistorOf(schematic));
        var third = Probe.At(WireOf(schematic));

        var probed = new AddProbe(third).Succeed(
            new AddProbe(second).Succeed(new AddProbe(first).Succeed(schematic)));

        var removed = new RemoveProbe(second).Succeed(probed);

        Assert.Equal([first, third], removed.Analysis.Probes);
    }

    [Fact]
    public void AddingTheSameProbeTwiceChangesNothing()
    {
        var schematic = Circuit();
        var probe = PinProbe(schematic);
        var probed = new AddProbe(probe).Succeed(schematic);
        var again = new AddProbe(probe).Succeed(probed);

        Assert.Same(probed, again);
    }

    [Fact]
    public void RemovingAProbeThatIsNotThereChangesNothing()
    {
        var schematic = Circuit();
        var removed = new RemoveProbe(PinProbe(schematic)).Succeed(schematic);

        Assert.Same(schematic, removed);
    }

    [Fact]
    public void TheProbesAreDescribedForTheUndoHistory()
    {
        var schematic = Circuit();
        var probe = PinProbe(schematic);

        Assert.Equal("Add probe", new AddProbe(probe).DescriptionOf(schematic));
        Assert.Equal(
            "Remove probe",
            new RemoveProbe(probe).DescriptionOf(new AddProbe(probe).Succeed(schematic)));
    }

    [Fact]
    public void AProbeOnAComponentThatIsNotThereIsRefused()
    {
        var error = new AddProbe(Probe.Through(new ComponentId(99))).Reject(Circuit());

        Assert.Equal(EditErrorCode.InvalidProbe, error.Code);
    }

    [Fact]
    public void AProbeOnAWireThatIsNotThereIsRefused()
    {
        var error = new AddProbe(Probe.At(new WireId(99))).Reject(Circuit());

        Assert.Equal(EditErrorCode.InvalidProbe, error.Code);
    }

    [Fact]
    public void AProbeOnAPinTheDefinitionDoesNotHaveIsRefused()
    {
        var schematic = Circuit();
        var error = new AddProbe(Probe.At(new PinRef(ResistorOf(schematic), "3")))
            .Reject(schematic);

        Assert.Equal(EditErrorCode.InvalidProbe, error.Code);
    }

    [Fact]
    public void TheNinthProbeIsRefusedWithTheLimitMessage()
    {
        var schematic = WithProbes(Circuit(), AnalysisSettings.MaxProbes);

        Assert.Equal(AnalysisSettings.MaxProbes, schematic.Analysis.Probes.Length);

        var error = new AddProbe(PinProbe(schematic)).Reject(schematic);

        Assert.Equal(EditErrorCode.TooManyProbes, error.Code);
        Assert.Equal("At most 8 probes.", error.Message);
    }

    [Fact]
    public void RemovingAProbeMakesRoomForAnother()
    {
        var full = WithProbes(Circuit(), AnalysisSettings.MaxProbes);
        var freed = new RemoveProbe(full.Analysis.Probes[0]).Succeed(full);
        var probed = new AddProbe(PinProbe(freed)).Succeed(freed);

        Assert.Equal(AnalysisSettings.MaxProbes, probed.Analysis.Probes.Length);
    }

    [Fact]
    public void AddingAndRemovingAProbeIsUndoableAndRedoable()
    {
        var schematic = Circuit();
        var probe = PinProbe(schematic);
        var history = new EditHistory(schematic, BaseCatalog.Catalog);

        Assert.True(history.Execute(new AddProbe(probe)).Succeeded);
        Assert.Equal(probe, Assert.Single(history.Schematic.Analysis.Probes));
        Assert.True(history.IsDirty);

        Assert.True(history.Undo());
        Assert.Empty(history.Schematic.Analysis.Probes);

        Assert.True(history.Redo());
        Assert.Equal(probe, Assert.Single(history.Schematic.Analysis.Probes));

        Assert.True(history.Execute(new RemoveProbe(probe)).Succeeded);
        Assert.Empty(history.Schematic.Analysis.Probes);

        Assert.True(history.Undo());
        Assert.Equal(probe, Assert.Single(history.Schematic.Analysis.Probes));
    }

    [Fact]
    public void ProbingLeavesTheComponentsWiresAndTransientSettingsAlone()
    {
        var before = new SetAnalysisSettings(new AnalysisSettings(new TransientSettings(1e-3)))
            .Succeed(Circuit());

        var after = new AddProbe(PinProbe(before)).Succeed(before);

        Assert.Equal(before.Components, after.Components);
        Assert.Equal(before.Wires, after.Wires);
        Assert.Equal(before.NextComponentId, after.NextComponentId);
        Assert.Equal(before.NextWireId, after.NextWireId);
        Assert.Equal(before.Analysis.Transient, after.Analysis.Transient);
    }

    [Fact]
    public void ChangingTheTransientSettingsKeepsTheProbes()
    {
        var probed = new AddProbe(PinProbe(Circuit())).Succeed(Circuit());
        var settings = new AnalysisSettings(
            new TransientSettings(1e-3), probed.Analysis.Probes);

        var changed = new SetAnalysisSettings(settings).Succeed(probed);

        Assert.Equal(new TransientSettings(1e-3), changed.Analysis.Transient);
        Assert.Equal(probed.Analysis.Probes, changed.Analysis.Probes);
    }

    [Fact]
    public void ProbesCompareByValueNotByReference()
    {
        var schematic = Circuit();
        var left = new AnalysisSettings(null, [PinProbe(schematic)]);
        var right = new AnalysisSettings(null, [PinProbe(schematic)]);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.NotEqual(left, new AnalysisSettings(null, []));
    }

    [Fact]
    public void ADefaultProbeArrayReadsAsEmpty()
    {
        var settings = new AnalysisSettings(new TransientSettings(1e-3));

        Assert.Empty(settings.Probes);
        Assert.Empty((settings with { Probes = default }).Probes);
    }

    [Fact]
    public void AProbeNamesItsTargetAndKind()
    {
        var schematic = Circuit();

        Assert.Equal(ProbeKind.PinVoltage, PinProbe(schematic).Kind);
        Assert.True(PinProbe(schematic).IsVoltage);
        Assert.False(PinProbe(schematic).IsCurrent);
        Assert.Equal(ProbeKind.WireVoltage, Probe.At(WireOf(schematic)).Kind);
        Assert.True(Probe.At(WireOf(schematic)).IsVoltage);
        Assert.Equal(ProbeKind.ComponentCurrent, Probe.Through(ResistorOf(schematic)).Kind);
        Assert.True(Probe.Through(ResistorOf(schematic)).IsCurrent);
        Assert.False(Probe.Through(ResistorOf(schematic)).IsVoltage);
    }
}
