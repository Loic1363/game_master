using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Geometry;
using Xunit;

namespace Nodal.Editor.Tests;

public class DragGestureTests
{
    [Fact]
    public void APressOnAnUnselectedComponentSelectsItAndMovesItInOneGesture()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.DragFrom(0, 0, 3.4, 2);

        Assert.Equal(ComponentId.First, session.Selection.Component);
        Assert.Equal(new GridPoint(3, 2), session.Component(1).Position);
        Assert.Equal("Move R1", session.History.UndoDescription);
    }

    [Fact]
    public void APressAndReleaseInsideTheThresholdIsAPlainClick()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.DragFrom(0, 0, 0.2, 0.1);

        Assert.Equal(ComponentId.First, session.Selection.Component);
        Assert.Equal(GridPoint.Origin, session.Component(1).Position);
        Assert.False(session.History.CanUndo);
    }

    [Fact]
    public void NoGhostShowsBeforeTheThresholdIsPassed()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.PrimaryPressed(new GridVector(0, 0));
        session.PointerMoved(new GridVector(0.2, 0));

        Assert.Null(session.View.Ghost);

        session.PointerMoved(new GridVector(3, 2));

        Assert.Equal(new GridPoint(3, 2), session.View.Ghost?.Position);
    }

    [Fact]
    public void ADragStaysUnderwayWhenThePointerComesBackInsideTheThreshold()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.PrimaryPressed(new GridVector(0, 0));
        session.PointerMoved(new GridVector(3, 2));
        session.PointerMoved(new GridVector(0.1, 0));

        Assert.NotNull(session.View.Ghost);

        session.PrimaryReleased(new GridVector(4, 0));

        Assert.Equal(new GridPoint(4, 0), session.Component(1).Position);
    }

    [Fact]
    public void ADragOnEmptySpaceMovesNothing()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.DragFrom(6, 6, 9, 9);

        Assert.True(session.Selection.IsEmpty);
        Assert.Equal(GridPoint.Origin, session.Component(1).Position);
        Assert.False(session.History.CanUndo);
    }

    [Fact]
    public void AMoveCommitsOnlyOnceAndUndoesInOneStep()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));

        session.DragFrom(0, 0, 3, 2);
        session.Undo();

        Assert.Equal(GridPoint.Origin, session.Component(1).Position);
        Assert.False(session.History.CanUndo);
    }
}
