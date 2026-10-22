using Nodal.Circuit.Commands;
using Nodal.Circuit.Editing;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Circuit.Tests.Editing;

public class DirtyStateTests
{
    [Fact]
    public void ANewHistoryIsClean()
    {
        Assert.False(NewHistory().IsDirty);
    }

    [Fact]
    public void ExecutingACommandMakesTheHistoryDirty()
    {
        var history = NewHistory();

        history.Execute(AddResistor());

        Assert.True(history.IsDirty);
    }

    [Fact]
    public void UndoingBackToTheSavedSnapshotMakesTheHistoryCleanAgain()
    {
        var history = NewHistory();
        history.Execute(AddResistor());
        history.Execute(AddResistor());

        history.Undo();

        Assert.True(history.IsDirty);

        history.Undo();

        Assert.False(history.IsDirty);
    }

    [Fact]
    public void RedoingAfterTheSavedSnapshotMakesTheHistoryDirtyAgain()
    {
        var history = NewHistory();
        history.Execute(AddResistor());
        history.Undo();

        history.Redo();

        Assert.True(history.IsDirty);
    }

    [Fact]
    public void MarkSavedMakesTheCurrentSchematicTheCleanOne()
    {
        var history = NewHistory();
        history.Execute(AddResistor());

        history.MarkSaved();

        Assert.False(history.IsDirty);

        history.Undo();

        Assert.True(history.IsDirty);

        history.Redo();

        Assert.False(history.IsDirty);
    }

    [Fact]
    public void MarkSavedRaisesChangedOnlyWhenTheHistoryWasDirty()
    {
        var history = NewHistory();
        var raised = 0;
        history.Changed += () => raised++;

        history.MarkSaved();

        Assert.Equal(0, raised);

        history.Execute(AddResistor());
        history.MarkSaved();

        Assert.Equal(2, raised);
    }

    [Fact]
    public void ANoOpCommandLeavesTheHistoryClean()
    {
        var history = NewHistory();
        history.Execute(AddResistor());
        history.MarkSaved();

        var result = history.Execute(
            new SetParameter(ComponentId.First, "resistance", new Quantity(1000, Unit.Ohm)));

        Assert.True(result.Succeeded);
        Assert.False(history.IsDirty);
        Assert.Equal("Add R1", history.UndoDescription);
    }

    [Fact]
    public void AFailedCommandLeavesTheHistoryClean()
    {
        var history = NewHistory();

        var result = history.Execute(new RemoveComponent(ComponentId.First));

        Assert.False(result.Succeeded);
        Assert.False(history.IsDirty);
    }

    [Fact]
    public void AnUndoneAndReExecutedEditIsDirtyAgainOnANewBranch()
    {
        var history = NewHistory();
        history.Execute(AddResistor());
        history.MarkSaved();
        history.Undo();
        history.Execute(AddCapacitor());

        Assert.True(history.IsDirty);
    }

    private static EditHistory NewHistory() => new(Schematic.Empty, BaseCatalog.Catalog);

    private static AddComponent AddResistor() =>
        new("resistor", GridPoint.Origin, Orientation.Default);

    private static AddComponent AddCapacitor() =>
        new("capacitor", GridPoint.Origin, Orientation.Default);
}
