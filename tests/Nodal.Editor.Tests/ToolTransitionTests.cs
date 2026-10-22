using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Geometry;
using Xunit;

namespace Nodal.Editor.Tests;

public class ToolTransitionTests
{
    [Fact]
    public void ANewSessionStartsInSelect()
    {
        var session = Sessions.Empty();

        Assert.Equal(EditorToolKind.Select, session.Tool);
        Assert.Equal("Select", session.View.ToolLabel);
        Assert.True(session.Selection.IsEmpty);
    }

    [Fact]
    public void ChoosingAComponentEntersPlaceAndLabelsTheTool()
    {
        var session = Sessions.Empty();

        session.ChoosePlace("resistor");

        Assert.Equal(EditorToolKind.Place, session.Tool);
        Assert.Equal("Place Resistor", session.View.ToolLabel);
    }

    [Fact]
    public void ChoosingAnUnknownComponentKeepsTheCurrentTool()
    {
        var session = Sessions.Empty();

        session.ChoosePlace("widget");

        Assert.Equal(EditorToolKind.Select, session.Tool);
    }

    [Fact]
    public void EscapeReturnsToSelectFromPlace()
    {
        var session = Sessions.Empty();

        session.ChoosePlace("resistor");
        session.Cancel();

        Assert.Equal(EditorToolKind.Select, session.Tool);
    }

    [Fact]
    public void EscapeReturnsToSelectFromAnIdleWireTool()
    {
        var session = Sessions.Empty();

        session.ChooseWire();
        session.Cancel();

        Assert.Equal(EditorToolKind.Select, session.Tool);
    }

    [Fact]
    public void EscapeEndsTheWireChainBeforeLeavingTheTool()
    {
        var session = Sessions.Empty();

        session.ChooseWire();
        session.ClickAt(0, 0);

        session.Cancel();

        Assert.Equal(EditorToolKind.Wire, session.Tool);
        Assert.Empty(session.View.WirePreview);

        session.Cancel();

        Assert.Equal(EditorToolKind.Select, session.Tool);
    }

    [Fact]
    public void ARightClickBehavesLikeEscape()
    {
        var session = Sessions.Empty();

        session.ChooseWire();
        session.ClickAt(0, 0);
        session.SecondaryPressed(new GridVector(0, 0));

        Assert.Equal(EditorToolKind.Wire, session.Tool);

        session.SecondaryPressed(new GridVector(0, 0));

        Assert.Equal(EditorToolKind.Select, session.Tool);
    }

    [Fact]
    public void EscapeInSelectClearsTheSelection()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.ClickAt(-2, 0);

        Assert.Equal(ComponentId.First, session.Selection.Component);

        session.Cancel();

        Assert.True(session.Selection.IsEmpty);
        Assert.Equal(EditorToolKind.Select, session.Tool);
    }

    [Fact]
    public void PlacingAddsAComponentAndStaysInPlace()
    {
        var session = Sessions.Empty();

        session.ChoosePlace("resistor");
        session.ClickAt(4.2, -1.9);
        session.ClickAt(10, 10);

        Assert.Equal(EditorToolKind.Place, session.Tool);
        Assert.Equal(2, session.Schematic.Components.Count);
        Assert.Equal(new GridPoint(4, -2), session.Component(1).Position);
        Assert.Equal(new GridPoint(10, 10), session.Component(2).Position);
    }

    [Fact]
    public void TheGhostFollowsTheCursorSnappedToTheGrid()
    {
        var session = Sessions.Empty();

        session.ChoosePlace("capacitor");
        session.PointerMoved(new GridVector(3.4, -0.6));

        var ghost = session.View.Ghost;

        Assert.NotNull(ghost);
        Assert.Equal("capacitor", ghost.DefinitionId);
        Assert.Equal(new GridPoint(3, -1), ghost.Position);
        Assert.Equal(Orientation.Default, ghost.Orientation);
    }

    [Fact]
    public void RotatingAndMirroringTurnTheGhostAndThePlacedComponent()
    {
        var session = Sessions.Empty();

        session.ChoosePlace("resistor");
        session.Rotate();
        session.Mirror();

        Assert.Equal(new Orientation(Rotation.R90, true), session.View.Ghost?.Orientation);

        session.ClickAt(0, 0);

        Assert.Equal(new Orientation(Rotation.R90, true), session.Component(1).Orientation);
    }

    [Fact]
    public void TheWireToolShowsAnLShapedPreviewAfterTheFirstClick()
    {
        var session = Sessions.Empty();

        session.ChooseWire();

        Assert.Empty(session.View.WirePreview);

        session.ClickAt(0, 0);
        session.PointerMoved(new GridVector(4, 3));

        Assert.Equal(
            [
                new WireSegment(new GridPoint(0, 0), new GridPoint(4, 0)),
                new WireSegment(new GridPoint(4, 0), new GridPoint(4, 3)),
            ],
            session.View.WirePreview);
    }

    [Fact]
    public void AWireChainCommitsOneHistoryEntryPerClick()
    {
        var session = Sessions.Empty();

        session.ChooseWire();
        session.ClickAt(0, 0);
        session.ClickAt(4, 3);
        session.ClickAt(8, 3);

        Assert.Equal(3, session.Schematic.Wires.Count);
        Assert.Equal("Add wire", session.History.UndoDescription);

        session.Undo();

        Assert.Equal(2, session.Schematic.Wires.Count);
    }

    [Fact]
    public void TheFirstClickOfAChainAddsNoWire()
    {
        var session = Sessions.Empty();

        session.ChooseWire();
        session.ClickAt(0, 0);

        Assert.Empty(session.Schematic.Wires);
        Assert.False(session.History.CanUndo);
    }

    [Fact]
    public void AClickOnTheSamePointAddsNoWire()
    {
        var session = Sessions.Empty();

        session.ChooseWire();
        session.ClickAt(0, 0);
        session.ClickAt(0.2, -0.2);

        Assert.Empty(session.Schematic.Wires);
    }

    [Fact]
    public void ADoubleClickEndsTheChainWithoutLeavingTheTool()
    {
        var session = Sessions.Empty();

        session.ChooseWire();
        session.ClickAt(0, 0);
        session.DoubleClicked();

        Assert.Empty(session.View.WirePreview);
        Assert.Equal(EditorToolKind.Wire, session.Tool);
    }

    [Fact]
    public void LeavingSelectDropsTheSelection()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.ClickAt(0, 0);
        session.ChooseWire();

        Assert.True(session.Selection.IsEmpty);

        session.Cancel();

        Assert.True(session.Selection.IsEmpty);
    }
}
