using Nodal.Circuit.Commands;
using Nodal.Circuit.Schematics;
using Nodal.Editor.Simulation;
using Xunit;

namespace Nodal.Editor.Tests;

public class TransientFormTests
{
    private static TransientForm Empty() => TransientForm.For(AnalysisSettings.None);

    private static TransientForm Of(TransientSettings settings) =>
        TransientForm.For(new AnalysisSettings(settings));

    [Fact]
    public void AnEmptyFormNeedsAStopTime()
    {
        var form = Empty();

        Assert.False(form.IsValid);
        Assert.Equal("Required.", form.StopTime.Error);
        Assert.Null(form.OutputStep.Error);
        Assert.Null(form.MaxStep.Error);
        Assert.Null(form.Settings);
        Assert.Null(form.BuildCommand());
    }

    [Fact]
    public void AStopTimeInQuantityNotationIsEnough()
    {
        var form = Empty();

        form.Write(form.StopTime, "10m");

        Assert.True(form.IsValid);
        Assert.Equal(new TransientSettings(0.01), form.Settings);
    }

    [Theory]
    [InlineData("500u", 500e-6)]
    [InlineData("1", 1)]
    [InlineData("2.5m", 2.5e-3)]
    [InlineData("1,5m", 1.5e-3)]
    [InlineData("100n", 100e-9)]
    [InlineData("3 s", 3)]
    [InlineData("40 ms", 0.04)]
    public void TheFieldsReadTheGameQuantityNotation(string text, double expected)
    {
        var form = Empty();

        form.Write(form.StopTime, text);

        Assert.True(form.StopTime.IsValid);
        Assert.Equal(expected, form.StopTime.Seconds);
    }

    [Theory]
    [InlineData("0", "Must be greater than zero.")]
    [InlineData("-1m", "Must be greater than zero.")]
    [InlineData("fast", "Not a number.")]
    [InlineData("10X", "Unknown metric prefix.")]
    [InlineData("10 V", "Expected a time in seconds.")]
    public void AnUnreadableStopTimeIsReported(string text, string expected)
    {
        var form = Empty();

        form.Write(form.StopTime, text);

        Assert.Equal(expected, form.StopTime.Error);
        Assert.False(form.IsValid);
    }

    [Fact]
    public void TheOptionalStepsMayStayEmpty()
    {
        var form = Of(new TransientSettings(0.01, 1e-5, 1e-7));

        form.Write(form.OutputStep, string.Empty);
        form.Write(form.MaxStep, "   ");

        Assert.True(form.IsValid);
        Assert.True(form.OutputStep.IsUnset);
        Assert.True(form.MaxStep.IsUnset);
        Assert.Equal(new TransientSettings(0.01), form.Settings);
    }

    [Fact]
    public void AnInvalidOptionalStepInvalidatesTheForm()
    {
        var form = Of(new TransientSettings(0.01));

        form.Write(form.MaxStep, "-1u");

        Assert.False(form.IsValid);
        Assert.Null(form.Settings);
    }

    [Fact]
    public void TheFieldsStartFromTheSavedSettings()
    {
        var form = Of(new TransientSettings(0.01, 1e-5, 2e-7, true));

        Assert.Equal("10 ms", form.StopTime.Text);
        Assert.Equal("10 µs", form.OutputStep.Text);
        Assert.Equal("200 ns", form.MaxStep.Text);
        Assert.True(form.StartFromZero);
        Assert.True(form.IsValid);
        Assert.False(form.HasChanges);
        Assert.Null(form.BuildCommand());
    }

    [Fact]
    public void TheFormBuildsAnUndoableCommand()
    {
        var form = Empty();

        form.Write(form.StopTime, "10m");
        form.StartFromZero = true;

        var command = Assert.IsType<SetAnalysisSettings>(form.BuildCommand());

        Assert.Equal(
            new AnalysisSettings(new TransientSettings(0.01, null, null, true)), command.Settings);

        var result = command.Apply(Schematic.Empty, BaseContent.Catalog);

        Assert.True(result.Succeeded);
        Assert.Equal("Change analysis settings", result.Description);
    }

    [Fact]
    public void ChangingOnlyTheStartFromZeroFlagIsAChange()
    {
        var form = Of(new TransientSettings(0.01));

        Assert.False(form.HasChanges);

        form.StartFromZero = true;

        Assert.True(form.HasChanges);
        Assert.NotNull(form.BuildCommand());
    }

    [Fact]
    public void TheFieldsCarryTheirLabels()
    {
        var form = Empty();

        Assert.Equal(TransientForm.StopTimeLabel, form.StopTime.Label);
        Assert.Equal(TransientForm.OutputStepLabel, form.OutputStep.Label);
        Assert.Equal(TransientForm.MaxStepLabel, form.MaxStep.Label);
        Assert.True(form.StopTime.IsRequired);
        Assert.False(form.OutputStep.IsRequired);
        Assert.False(form.MaxStep.IsRequired);
        Assert.Equal(3, form.Fields.Length);
    }

    [Fact]
    public void AFieldFromAnotherFormIsRefused()
    {
        var form = Empty();
        var other = Empty();

        Assert.Throws<ArgumentException>(() => form.Write(other.StopTime, "1m"));
    }
}
