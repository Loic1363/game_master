using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Geometry;
using Xunit;

namespace Nodal.Editor.Tests;

public class SelectionTests
{
    [Fact]
    public void APinSelectsItsComponent()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.ClickAt(-2, 0.1);

        Assert.Equal(ComponentId.First, session.Selection.Component);
        Assert.Null(session.Selection.Wire);
    }

    [Fact]
    public void AWireIsSelectedWhenNoPinIsCloser()
    {
        var session = Sessions.Of(Schematic.Empty.Wire(0, 0, 8, 0));

        session.ClickAt(4, 0.2);

        Assert.Equal(WireId.First, session.Selection.Wire);
        Assert.Null(session.Selection.Component);
    }

    [Fact]
    public void TheBodySelectsTheComponentWhenNoPinOrWireIsHit()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.ClickAt(0, 0.2);

        Assert.Equal(ComponentId.First, session.Selection.Component);
    }

    [Fact]
    public void ClickingEmptySpaceClearsTheSelection()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.ClickAt(0, 0);
        session.ClickAt(20, 20);

        Assert.True(session.Selection.IsEmpty);
    }

    [Fact]
    public void OnlyOneItemIsSelectedAtATime()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0).Wire(0, 6, 8, 6));

        session.ClickAt(0, 0);
        session.ClickAt(4, 6);

        Assert.Null(session.Selection.Component);
        Assert.Equal(WireId.First, session.Selection.Wire);
    }

    [Fact]
    public void DeleteRemovesTheSelectedComponent()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.ClickAt(0, 0);
        session.DeleteSelection();

        Assert.Empty(session.Schematic.Components);
        Assert.True(session.Selection.IsEmpty);
        Assert.Equal("Remove R1", session.History.UndoDescription);
    }

    [Fact]
    public void DeleteRemovesTheSelectedWire()
    {
        var session = Sessions.Of(Schematic.Empty.Wire(0, 0, 8, 0));

        session.ClickAt(4, 0);
        session.DeleteSelection();

        Assert.Empty(session.Schematic.Wires);
        Assert.True(session.Selection.IsEmpty);
    }

    [Fact]
    public void DeleteWithoutASelectionChangesNothing()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.DeleteSelection();

        Assert.Single(session.Schematic.Components);
        Assert.False(session.History.CanUndo);
    }

    [Fact]
    public void RotateAndMirrorActOnTheSelectedComponent()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.ClickAt(0, 0);
        session.Rotate();
        session.Mirror();

        Assert.Equal(new Orientation(Rotation.R90, true), session.Component(1).Orientation);
        Assert.Equal("Mirror R1", session.History.UndoDescription);
    }

    [Fact]
    public void RotateWithoutASelectionChangesNothing()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.Rotate();

        Assert.Equal(Orientation.Default, session.Component(1).Orientation);
        Assert.False(session.History.CanUndo);
    }

    [Fact]
    public void DraggingASelectedComponentMovesItOnceOnRelease()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.ClickAt(0, 0);
        session.PrimaryPressed(new GridVector(0, 0));
        session.PointerMoved(new GridVector(3.4, 2));
        session.PrimaryReleased(new GridVector(3.4, 2));

        Assert.Equal(new GridPoint(3, 2), session.Component(1).Position);
        Assert.Equal("Move R1", session.History.UndoDescription);

        session.Undo();

        Assert.Equal(GridPoint.Origin, session.Component(1).Position);
        Assert.False(session.History.CanUndo);
    }

    [Fact]
    public void TheDragGhostShowsTheDestinationBeforeRelease()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.ClickAt(0, 0);
        session.PrimaryPressed(new GridVector(0, 0));
        session.PointerMoved(new GridVector(3, 2));

        Assert.Equal(new GridPoint(3, 2), session.View.Ghost?.Position);
        Assert.Equal(GridPoint.Origin, session.Component(1).Position);
    }

    [Fact]
    public void AMoveBackToTheSamePlaceAddsNoHistoryEntry()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.ClickAt(0, 0);
        session.PrimaryPressed(new GridVector(0, 0));
        session.PointerMoved(new GridVector(3, 2));
        session.PrimaryReleased(new GridVector(0.1, -0.1));

        Assert.False(session.History.CanUndo);
    }

    [Fact]
    public void AFirstClickSelectsWithoutMoving()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.PrimaryPressed(new GridVector(0, 0));
        session.PointerMoved(new GridVector(5, 5));
        session.PrimaryReleased(new GridVector(5, 5));

        Assert.Equal(GridPoint.Origin, session.Component(1).Position);
        Assert.Equal(ComponentId.First, session.Selection.Component);
    }

    [Fact]
    public void WiresDoNotFollowAMovedComponent()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0).Wire(-2, 0, -6, 0));

        session.ClickAt(0, 0);
        session.PrimaryPressed(new GridVector(0, 0));
        session.PrimaryReleased(new GridVector(0, 4));

        Assert.True(session.Schematic.TryGetWire(WireId.First, out var wire));
        Assert.Equal(new GridPoint(-2, 0), wire.Start);
    }

    [Fact]
    public void EscapeDuringADragLeavesTheComponentWhereItWas()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.ClickAt(0, 0);
        session.PrimaryPressed(new GridVector(0, 0));
        session.PointerMoved(new GridVector(4, 4));
        session.Cancel();
        session.PrimaryReleased(new GridVector(4, 4));

        Assert.Equal(GridPoint.Origin, session.Component(1).Position);
        Assert.False(session.History.CanUndo);
    }

    [Fact]
    public void UndoAndRedoClearASelectionThatDisappears()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.ClickAt(0, 0);
        session.DeleteSelection();
        session.Undo();

        Assert.Single(session.Schematic.Components);
        Assert.True(session.Selection.IsEmpty);

        session.ClickAt(0, 0);
        session.Redo();

        Assert.True(session.Selection.IsEmpty);
    }

    [Fact]
    public void TheCursorFollowsThePointerInTheView()
    {
        var session = Sessions.Empty();

        session.PointerMoved(new GridVector(-2.4, 6.6));

        Assert.Equal(new GridPoint(-2, 7), session.View.Cursor);
    }

    [Fact]
    public void ANegativeToleranceIsRejected()
    {
        var session = Sessions.Empty();

        Assert.Throws<ArgumentOutOfRangeException>(() => session.HitTolerance = -1);
    }
}
