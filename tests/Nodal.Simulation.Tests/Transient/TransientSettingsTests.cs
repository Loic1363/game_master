using Nodal.Circuit.Schematics;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class TransientSettingsTests
{
    [Fact]
    public void SettingsBecomeOptions()
    {
        var options = TransientOptions.From(new TransientSettings(0.01, 1e-5, 2e-7, true));

        Assert.Equal(0.01, options.StopTime);
        Assert.Equal(1e-5, options.OutputStep);
        Assert.Equal(2e-7, options.MaxStep);
        Assert.True(options.StartFromZero);
    }

    [Fact]
    public void UnsetStepsStayUnset()
    {
        var options = TransientOptions.From(new TransientSettings(0.01));

        Assert.Null(options.OutputStep);
        Assert.Null(options.MaxStep);
        Assert.False(options.StartFromZero);
    }

    [Fact]
    public void OptionsBecomeSettingsAgain()
    {
        var settings = new TransientSettings(0.01, 1e-5, 2e-7, true);

        Assert.Equal(settings, TransientOptions.From(settings).ToSettings());
    }

    [Fact]
    public void SettingsWithoutAPositiveStopTimeCannotBecomeOptions() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TransientOptions.From(new TransientSettings(0)));

    [Fact]
    public void AnalysisSettingsFromTheFileDriveTheRun()
    {
        var settings = new TransientSettings(1e-3, null, 1e-6, true);

        var result = TransientCircuits.Rc(1000, 100e-9)
            .Set("V1", "voltage", 5)
            .Transient(TransientOptions.From(settings));

        Assert.True(result.IsSolved);
        Assert.Equal(settings.StopTime, result.FinalTime);
        Approximately.Zero(result.VoltsAt("C1", 0), 1e-12);
    }
}
