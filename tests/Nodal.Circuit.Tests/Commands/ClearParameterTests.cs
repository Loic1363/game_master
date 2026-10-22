using Nodal.Circuit.Commands;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class ClearParameterTests
{
    [Fact]
    public void ClearingASetOptionalParameterUnsetsIt()
    {
        var schematic = Capacitor(out var id);
        var withEsr = new SetParameter(id, "esr", new Quantity(0.05, Unit.Ohm)).Succeed(schematic);

        var cleared = new ClearParameter(id, "esr").Succeed(withEsr);

        Assert.True(cleared.TryGetComponent(id, out var component));
        Assert.Equal(["capacitance"], component.Parameters.Keys);
    }

    [Fact]
    public void ClearingAnUnsetParameterReturnsTheSameSchematic()
    {
        var schematic = Capacitor(out var id);

        Assert.Same(schematic, new ClearParameter(id, "esr").Succeed(schematic));
    }

    [Fact]
    public void ClearingARequiredParameterFails()
    {
        var schematic = Capacitor(out var id);

        var error = new ClearParameter(id, "capacitance").Reject(schematic);

        Assert.Equal(EditErrorCode.ParameterIsRequired, error.Code);
        Assert.Contains("is required", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ClearingAnUnknownParameterFails()
    {
        var schematic = Capacitor(out var id);

        Assert.Equal(
            EditErrorCode.UnknownParameter, new ClearParameter(id, "tolerance").Reject(schematic).Code);
    }

    [Fact]
    public void ClearingOnAnUnknownComponentFails()
    {
        Assert.Equal(
            EditErrorCode.UnknownComponent,
            new ClearParameter(new ComponentId(9), "esr").Reject(Schematic.Empty).Code);
    }

    [Fact]
    public void TheDescriptionNamesTheDesignatorAndTheParameter()
    {
        var schematic = Capacitor(out var id);

        Assert.Equal("Clear C1 esr", new ClearParameter(id, "esr").DescriptionOf(schematic));
    }

    [Fact]
    public void ClearingLeavesTheRestOfTheComponentAlone()
    {
        var schematic = Capacitor(out var id);
        var withEsr = new SetParameter(id, "esr", new Quantity(0.05, Unit.Ohm)).Succeed(schematic);

        var cleared = new ClearParameter(id, "esr").Succeed(withEsr);

        Assert.True(cleared.TryGetComponent(id, out var component));
        Assert.Equal("C1", component.Designator);
        Assert.Equal(new GridPoint(1, 2), component.Position);
        Assert.Equal(new Quantity(100e-9, Unit.Farad), component.Parameters["capacitance"]);
    }

    private static Schematic Capacitor(out ComponentId id)
    {
        id = Schematic.Empty.NextComponentId;

        return new AddComponent("capacitor", new GridPoint(1, 2), Orientation.Default)
            .Succeed(Schematic.Empty);
    }
}
