using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Waveforms;
using Xunit;

namespace Nodal.Editor.Tests;

public class ProbeToolTests
{
    private static EditorSession Probing(Schematic schematic)
    {
        var session = Sessions.Of(schematic);

        session.ChooseProbe();

        return session;
    }

    private static Schematic Rc() => TransientRuns.Rc();

    private static ConnectivityResult Connectivity(Schematic schematic) =>
        ConnectivityAnalyzer.Analyze(schematic, BaseContent.Catalog);

    private static string? NetOf(Schematic schematic, Probe probe) =>
        ProbeTraces.NetOf(Connectivity(schematic), probe);

    private static Schematic ManyWires(int count)
    {
        var schematic = Rc();

        for (var index = 0; index < count; index++)
        {
            schematic = schematic.Wire(20, 10 + (index * 2), 24, 10 + (index * 2));
        }

        return schematic;
    }

    [Fact]
    public void ChoosingTheProbeToolLabelsTheTool()
    {
        var session = Probing(Rc());

        Assert.Equal(EditorToolKind.Probe, session.Tool);
        Assert.Equal("Probe", session.View.ToolLabel);
    }

    [Fact]
    public void EscapeReturnsToSelect()
    {
        var session = Probing(Rc());

        session.Cancel();

        Assert.Equal(EditorToolKind.Select, session.Tool);
    }

    [Fact]
    public void ClickingAPinProbesItsVoltage()
    {
        var session = Probing(Rc());

        session.ClickAt(6, -2);

        var probe = Assert.Single(session.Schematic.Analysis.Probes);

        Assert.True(probe.IsVoltage);
        Assert.NotNull(NetOf(session.Schematic, probe));
    }

    [Fact]
    public void ClickingTheSamePinAgainRemovesTheProbe()
    {
        var session = Probing(Rc());

        session.ClickAt(6, -2);
        session.ClickAt(6, -2);

        Assert.Empty(session.Schematic.Analysis.Probes);
    }

    [Fact]
    public void ClickingAnotherPinOfTheSameNetRemovesTheProbe()
    {
        var session = Probing(Rc());

        session.ClickAt(6, -2);

        var probe = Assert.Single(session.Schematic.Analysis.Probes);
        var net = NetOf(session.Schematic, probe);

        session.ClickAt(0, -2);

        Assert.Empty(session.Schematic.Analysis.Probes);
        Assert.Equal(net, NetOf(session.Schematic, Probe.At(new PinRef(new ComponentId(1), "plus"))));
    }

    [Fact]
    public void ClickingAWireProbesTheFirstPinOfItsNet()
    {
        var session = Probing(Rc());

        session.ClickAt(3, -2);

        var probe = Assert.Single(session.Schematic.Analysis.Probes);
        var pin = Assert.IsType<PinVoltageProbe>(probe);

        Assert.Equal(new ComponentId(1), pin.Pin.ComponentId);
    }

    [Fact]
    public void ClickingAWireWithoutAPinOnItsNetProbesTheWire()
    {
        var session = Probing(Rc().Wire(20, 20, 26, 20));

        session.ClickAt(23, 20);

        var probe = Assert.Single(session.Schematic.Analysis.Probes);

        Assert.IsType<WireVoltageProbe>(probe);
    }

    [Fact]
    public void ClickingAComponentBodyProbesItsCurrent()
    {
        var session = Probing(Rc());

        session.ClickAt(6, 0);

        var probe = Assert.Single(session.Schematic.Analysis.Probes);
        var current = Assert.IsType<ComponentCurrentProbe>(probe);

        Assert.Equal(session.Schematic.IdOf("R1"), current.Component);
    }

    [Fact]
    public void ClickingTheSameBodyAgainRemovesTheProbe()
    {
        var session = Probing(Rc());

        session.ClickAt(6, 0);
        session.ClickAt(6, 0);

        Assert.Empty(session.Schematic.Analysis.Probes);
    }

    [Fact]
    public void AVoltageAndACurrentProbeOnTheSameComponentLiveTogether()
    {
        var session = Probing(Rc());

        session.ClickAt(6, 0);
        session.ClickAt(6, -2);

        Assert.Equal(2, session.Schematic.Analysis.Probes.Length);
    }

    [Fact]
    public void ClickingNothingProbesNothing()
    {
        var session = Probing(Rc());

        session.ClickAt(40, 40);

        Assert.Empty(session.Schematic.Analysis.Probes);
    }

    [Fact]
    public void EveryProbeIsUndoableAndRedoable()
    {
        var session = Probing(Rc());

        session.ClickAt(6, 0);

        Assert.True(session.History.IsDirty);
        Assert.Equal("Add probe", session.History.UndoDescription);

        session.Undo();

        Assert.Empty(session.Schematic.Analysis.Probes);

        session.Redo();

        Assert.Single(session.Schematic.Analysis.Probes);

        session.ClickAt(6, 0);

        Assert.Equal("Remove probe", session.History.UndoDescription);

        session.Undo();

        Assert.Single(session.Schematic.Analysis.Probes);
    }

    [Fact]
    public void TheNinthProbeIsRefusedInTheStatusBar()
    {
        var count = AnalysisSettings.MaxProbes;
        var session = Probing(ManyWires(count));
        var refusals = new List<string?>();

        session.RefusalChanged += refusals.Add;

        for (var index = 0; index < count; index++)
        {
            session.ClickAt(22, 10 + (index * 2));
        }

        Assert.Equal(count, session.Schematic.Analysis.Probes.Length);
        Assert.All(refusals, message => Assert.Null(message));

        session.ClickAt(6, 0);

        Assert.Equal(count, session.Schematic.Analysis.Probes.Length);
        Assert.Equal("At most 8 probes.", refusals[^1]);
    }

    [Fact]
    public void ASuccessfulProbeClearsTheRefusal()
    {
        var count = AnalysisSettings.MaxProbes;
        var session = Probing(ManyWires(count));

        for (var index = 0; index < count; index++)
        {
            session.ClickAt(22, 10 + (index * 2));
        }

        session.ClickAt(6, 0);

        string? last = "unset";
        session.RefusalChanged += message => last = message;

        session.ClickAt(22, 10);

        Assert.Equal(count - 1, session.Schematic.Analysis.Probes.Length);
        Assert.Null(last);
    }

    [Fact]
    public void TheProbeToolSelectsNothing()
    {
        var session = Probing(Rc());

        session.ClickAt(6, 0);

        Assert.True(session.Selection.IsEmpty);
    }

    [Fact]
    public void TheProbeToolHasNoGhostOrPreview()
    {
        var view = Probing(Rc()).View;

        Assert.Null(view.Ghost);
        Assert.Empty(view.WirePreview);
    }

    [Fact]
    public void AProbeSurvivesMovingItsComponent()
    {
        var session = Probing(Rc());

        session.ClickAt(6, -2);

        var probe = Assert.Single(session.Schematic.Analysis.Probes);

        session.ChooseSelect();
        session.DragFrom(6, 0, 10, 0);

        Assert.Equal(probe, Assert.Single(session.Schematic.Analysis.Probes));
        Assert.NotNull(NetOf(session.Schematic, probe));
    }
}
