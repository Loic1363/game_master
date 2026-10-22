using Nodal.Circuit.Schematics;
using Nodal.Foundation.Geometry;
using Xunit;

namespace Nodal.Editor.Tests;

public class PropertiesRequestTests
{
    [Fact]
    public void ADoubleClickOnAComponentRequestsItsProperties()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));
        var requested = Watch(session);

        session.PrimaryPressed(new GridVector(0, 0));
        session.DoubleClicked();

        Assert.Equal([ComponentId.First], requested);
    }

    [Fact]
    public void ARightClickOnAComponentSelectsItAndRequestsItsProperties()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));
        var requested = Watch(session);

        session.SecondaryPressed(new GridVector(0, 0));

        Assert.Equal(ComponentId.First, session.Selection.Component);
        Assert.Equal([ComponentId.First], requested);
    }

    [Fact]
    public void ARightClickOnEmptySpaceClearsTheSelectionInstead()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));
        var requested = Watch(session);

        session.ClickAt(0, 0);
        session.SecondaryPressed(new GridVector(7, 7));

        Assert.True(session.Selection.IsEmpty);
        Assert.Empty(requested);
    }

    [Fact]
    public void ARightClickInAnotherToolStillReturnsToSelect()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));
        var requested = Watch(session);

        session.ChooseWire();
        session.SecondaryPressed(new GridVector(0, 0));

        Assert.Equal(EditorToolKind.Select, session.Tool);
        Assert.Empty(requested);
    }

    [Fact]
    public void TheMenuRequestNeedsASelectedComponent()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));
        var requested = Watch(session);

        session.RequestPropertiesForSelection();

        Assert.Empty(requested);

        session.ClickAt(0, 0);
        session.RequestPropertiesForSelection();

        Assert.Equal([ComponentId.First], requested);
    }

    [Fact]
    public void ADoubleClickWithoutASelectionRequestsNothing()
    {
        var session = Sessions.Of(Schematic.Empty.Add("resistor", 0, 0));
        var requested = Watch(session);

        session.PrimaryPressed(new GridVector(7, 7));
        session.DoubleClicked();

        Assert.Empty(requested);
    }

    private static List<ComponentId> Watch(EditorSession session)
    {
        var requested = new List<ComponentId>();

        session.PropertiesRequested += requested.Add;

        return requested;
    }
}
