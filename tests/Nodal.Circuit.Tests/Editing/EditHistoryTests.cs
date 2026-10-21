using Nodal.Circuit.Commands;
using Nodal.Circuit.Editing;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Circuit.Tests.Editing;

public class EditHistoryTests
{
    [Fact]
    public void AFreshHistoryHoldsTheGivenSchematicAndNothingToUndo()
    {
        var history = NewHistory();

        Assert.Same(Schematic.Empty, history.Schematic);
        Assert.Same(BaseCatalog.Catalog, history.Catalog);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Null(history.UndoDescription);
        Assert.Null(history.RedoDescription);
    }

    [Fact]
    public void ExecutingAdvancesTheSchematicAndOffersAnUndo()
    {
        var history = NewHistory();

        var result = history.Execute(AddResistor());

        Assert.True(result.Succeeded);
        Assert.Same(result.Schematic, history.Schematic);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal("Add R1", history.UndoDescription);
    }

    [Fact]
    public void UndoRestoresThePreviousSnapshot()
    {
        var history = NewHistory();
        history.Execute(AddResistor());

        Assert.True(history.Undo());

        Assert.Same(Schematic.Empty, history.Schematic);
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
        Assert.Equal("Add R1", history.RedoDescription);
    }

    [Fact]
    public void RedoRestoresTheUndoneSnapshot()
    {
        var history = NewHistory();
        var added = history.Execute(AddResistor()).Schematic;
        history.Undo();

        Assert.True(history.Redo());

        Assert.Same(added, history.Schematic);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal("Add R1", history.UndoDescription);
    }

    [Fact]
    public void ALongerSequenceUndoesAndRedoesInOrder()
    {
        var history = NewHistory();
        history.Execute(AddResistor());
        history.Execute(new MoveComponent(ComponentId.First, new GridPoint(4, 4)));
        history.Execute(new RotateComponent(ComponentId.First));

        Assert.Equal("Rotate R1", history.UndoDescription);
        history.Undo();
        Assert.Equal("Move R1", history.UndoDescription);
        Assert.Equal("Rotate R1", history.RedoDescription);
        history.Undo();
        Assert.Equal("Add R1", history.UndoDescription);
        history.Undo();

        Assert.Same(Schematic.Empty, history.Schematic);
        Assert.False(history.CanUndo);
        Assert.Equal("Add R1", history.RedoDescription);

        history.Redo();
        history.Redo();
        history.Redo();

        Assert.True(history.Schematic.TryGetComponent(ComponentId.First, out var component));
        Assert.Equal(new GridPoint(4, 4), component.Position);
        Assert.Equal(new Orientation(Rotation.R90, false), component.Orientation);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void UndoDoesNothingWhenThereIsNothingToUndo()
    {
        var history = NewHistory();

        Assert.False(history.Undo());
        Assert.Same(Schematic.Empty, history.Schematic);
    }

    [Fact]
    public void RedoDoesNothingWhenThereIsNothingToRedo()
    {
        var history = NewHistory();
        var added = history.Execute(AddResistor()).Schematic;

        Assert.False(history.Redo());
        Assert.Same(added, history.Schematic);
    }

    [Fact]
    public void ANewExecuteClearsTheRedoStack()
    {
        var history = NewHistory();
        history.Execute(AddResistor());
        history.Undo();

        history.Execute(new AddComponent("capacitor", GridPoint.Origin, Orientation.Default));

        Assert.False(history.CanRedo);
        Assert.Null(history.RedoDescription);
        Assert.Equal("Add C1", history.UndoDescription);
    }

    [Fact]
    public void AFailedExecuteChangesNothing()
    {
        var history = NewHistory();
        var added = history.Execute(AddResistor()).Schematic;

        var result = history.Execute(new RemoveComponent(new ComponentId(99)));

        Assert.False(result.Succeeded);
        Assert.Equal(EditErrorCode.UnknownComponent, result.Error.Code);
        Assert.Same(added, history.Schematic);
        Assert.True(history.CanUndo);
        Assert.Equal("Add R1", history.UndoDescription);
    }

    [Fact]
    public void AFailedExecuteKeepsTheRedoStack()
    {
        var history = NewHistory();
        history.Execute(AddResistor());
        history.Undo();

        history.Execute(new AddComponent("flux-capacitor", GridPoint.Origin, Orientation.Default));

        Assert.True(history.CanRedo);
        Assert.Equal("Add R1", history.RedoDescription);
    }

    [Fact]
    public void TheDescriptionCarriesTheResolvedDesignator()
    {
        var history = NewHistory();
        history.Execute(new AddComponent("npn", GridPoint.Origin, Orientation.Default));
        history.Execute(new AddComponent("pnp", new GridPoint(8, 0), Orientation.Default));

        Assert.Equal("Add Q2", history.UndoDescription);
    }

    [Fact]
    public void TheDescriptionOfAParameterChangeReadsLikeTheEditMenu()
    {
        var history = NewHistory();
        history.Execute(AddResistor());
        history.Execute(new SetParameter(ComponentId.First, "resistance", new Quantity(4700, Unit.Ohm)));

        Assert.Equal($"Set R1 resistance to 4.7 k{UnitSymbols.Ohm}", history.UndoDescription);
    }

    [Fact]
    public void ChangedIsRaisedOncePerSuccessfulExecute()
    {
        var history = NewHistory();
        var raised = 0;
        history.Changed += () => raised++;

        history.Execute(AddResistor());

        Assert.Equal(1, raised);
    }

    [Fact]
    public void ChangedIsRaisedOncePerUndoAndOncePerRedo()
    {
        var history = NewHistory();
        history.Execute(AddResistor());
        var raised = 0;
        history.Changed += () => raised++;

        history.Undo();
        Assert.Equal(1, raised);

        history.Redo();
        Assert.Equal(2, raised);
    }

    [Fact]
    public void ChangedIsNotRaisedOnAFailedExecute()
    {
        var history = NewHistory();
        var raised = 0;
        history.Changed += () => raised++;

        history.Execute(new RemoveComponent(new ComponentId(99)));

        Assert.Equal(0, raised);
    }

    [Fact]
    public void ChangedIsNotRaisedWhenThereIsNothingToUndoOrRedo()
    {
        var history = NewHistory();
        var raised = 0;
        history.Changed += () => raised++;

        history.Undo();
        history.Redo();

        Assert.Equal(0, raised);
    }

    [Fact]
    public void TheSchematicIsAlreadyUpdatedWhenChangedIsRaised()
    {
        var history = NewHistory();
        var seen = -1;
        history.Changed += () => seen = history.Schematic.Components.Count;

        history.Execute(AddResistor());

        Assert.Equal(1, seen);
    }

    [Fact]
    public void AHistoryStartsFromAnyGivenSchematic()
    {
        var schematic = AddResistor().Succeed(Schematic.Empty);
        var history = new EditHistory(schematic, BaseCatalog.Catalog);

        history.Execute(new RemoveComponent(ComponentId.First));

        Assert.Empty(history.Schematic.Components);
        history.Undo();
        Assert.Same(schematic, history.Schematic);
    }

    [Fact]
    public void AHistoryNeedsASchematicACatalogAndACommand()
    {
        Assert.Equal(
            "schematic",
            Assert.Throws<ArgumentNullException>(
                () => new EditHistory(null!, BaseCatalog.Catalog)).ParamName);
        Assert.Equal(
            "catalog",
            Assert.Throws<ArgumentNullException>(
                () => new EditHistory(Schematic.Empty, null!)).ParamName);
        Assert.Equal(
            "command",
            Assert.Throws<ArgumentNullException>(() => NewHistory().Execute(null!)).ParamName);
    }

    [Fact]
    public void TheCatalogIsTheOneTheHistoryWasBuiltWith()
    {
        var history = new EditHistory(Schematic.Empty, ComponentCatalog.Empty);

        var result = history.Execute(AddResistor());

        Assert.False(result.Succeeded);
        Assert.Equal(EditErrorCode.UnknownDefinition, result.Error.Code);
    }

    private static EditHistory NewHistory() => new(Schematic.Empty, BaseCatalog.Catalog);

    private static AddComponent AddResistor() =>
        new("resistor", GridPoint.Origin, Orientation.Default);
}
