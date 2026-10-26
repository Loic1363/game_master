using Nodal.Circuit.Commands;
using Nodal.Circuit.Schematics;
using Xunit;

namespace Nodal.Circuit.Tests.Commands;

public class SetAnalysisSettingsTests
{
    private static SetAnalysisSettings Transient(TransientSettings settings) =>
        new(new AnalysisSettings(settings));

    [Fact]
    public void AnEmptySchematicHasNoAnalysisSettings()
    {
        Assert.True(Schematic.Empty.Analysis.IsEmpty);
        Assert.Null(Schematic.Empty.Analysis.Transient);
    }

    [Fact]
    public void TheTransientSettingsAreStoredOnTheSchematic()
    {
        var settings = new TransientSettings(0.01, 1e-5, 2e-7, true);
        var schematic = Transient(settings).Succeed(Schematic.Empty);

        Assert.Equal(settings, schematic.Analysis.Transient);
        Assert.False(schematic.Analysis.IsEmpty);
    }

    [Fact]
    public void SettingTheAnalysisLeavesTheComponentsAndWiresAlone()
    {
        var before = Edits.Succeed(new AddWire(new(0, 0), new(0, 2)), Schematic.Empty);
        var after = Transient(new TransientSettings(1e-3)).Succeed(before);

        Assert.Equal(before.Components, after.Components);
        Assert.Equal(before.Wires, after.Wires);
        Assert.Equal(before.NextComponentId, after.NextComponentId);
        Assert.Equal(before.NextWireId, after.NextWireId);
    }

    [Fact]
    public void TheSettingsCanBeCleared()
    {
        var schematic = Transient(new TransientSettings(1e-3)).Succeed(Schematic.Empty);
        var cleared = new SetAnalysisSettings(AnalysisSettings.None).Succeed(schematic);

        Assert.True(cleared.Analysis.IsEmpty);
    }

    [Fact]
    public void ChangingTheSettingsIsDescribedForTheUndoHistory()
    {
        Assert.Equal(
            "Change analysis settings",
            Transient(new TransientSettings(1e-3)).DescriptionOf(Schematic.Empty));

        Assert.Equal(
            "Clear analysis settings",
            new SetAnalysisSettings(AnalysisSettings.None).DescriptionOf(Schematic.Empty));
    }

    [Fact]
    public void SettingTheSameSettingsAgainSucceedsAndChangesNothing()
    {
        var settings = new TransientSettings(1e-3);
        var schematic = Transient(settings).Succeed(Schematic.Empty);
        var again = Transient(settings).Succeed(schematic);

        Assert.Same(schematic, again);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1e-3)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AStopTimeThatIsNotAFinitePositiveNumberIsRefused(double stopTime)
    {
        var error = Transient(new TransientSettings(stopTime)).Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.InvalidAnalysisSettings, error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1e-6)]
    [InlineData(double.NaN)]
    public void AnOutputStepThatIsNotAFinitePositiveNumberIsRefused(double outputStep)
    {
        var error = Transient(new TransientSettings(1e-3, outputStep)).Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.InvalidAnalysisSettings, error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1e-6)]
    [InlineData(double.NaN)]
    public void AMaxStepThatIsNotAFinitePositiveNumberIsRefused(double maxStep)
    {
        var error = Transient(new TransientSettings(1e-3, null, maxStep)).Reject(Schematic.Empty);

        Assert.Equal(EditErrorCode.InvalidAnalysisSettings, error.Code);
    }

    [Fact]
    public void ValidSettingsReportThemselvesAsValid()
    {
        Assert.True(new TransientSettings(1e-3).IsValid);
        Assert.True(new TransientSettings(1e-3, 1e-6, 1e-9, true).IsValid);
        Assert.True(AnalysisSettings.None.IsValid);
        Assert.False(new TransientSettings(0).IsValid);
        Assert.False(new AnalysisSettings(new TransientSettings(0)).IsValid);
    }
}
