using System.Collections.Immutable;
using Nodal.Circuit.Commands;
using Nodal.Circuit.Schematics;

namespace Nodal.Editor.Simulation;

public sealed class TransientForm
{
    public const string StopTimeLabel = "Stop time";
    public const string OutputStepLabel = "Output step";
    public const string MaxStepLabel = "Max step";
    public const string StartFromZeroLabel = "Start from zero";

    private readonly AnalysisSettings _analysis;
    private readonly TransientSettings? _original;

    private TransientForm(AnalysisSettings analysis)
    {
        var settings = analysis.Transient;

        _analysis = analysis;
        _original = settings;
        StopTime = new TimeField(StopTimeLabel, true, settings?.StopTime);
        OutputStep = new TimeField(OutputStepLabel, false, settings?.OutputStep);
        MaxStep = new TimeField(MaxStepLabel, false, settings?.MaxStep);
        StartFromZero = settings?.StartFromZero ?? false;
    }

    public TimeField StopTime { get; }

    public TimeField OutputStep { get; }

    public TimeField MaxStep { get; }

    public bool StartFromZero { get; set; }

    public ImmutableArray<TimeField> Fields => [StopTime, OutputStep, MaxStep];

    public bool IsValid => Fields.All(entry => entry.IsValid) && StopTime.Seconds is not null;

    public TransientSettings? Settings => IsValid
        ? new TransientSettings(
            StopTime.Seconds!.Value, OutputStep.Seconds, MaxStep.Seconds, StartFromZero)
        : null;

    public bool HasChanges => IsValid && Settings != _original;

    public static TransientForm For(AnalysisSettings analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        return new TransientForm(analysis);
    }

    public void Write(TimeField field, string? text) => Owned(field).Write(text);

    public IEditCommand? BuildCommand() =>
        Settings is { } settings && settings != _original
            ? new SetAnalysisSettings(_analysis with { Transient = settings })
            : null;

    private TimeField Owned(TimeField field)
    {
        ArgumentNullException.ThrowIfNull(field);

        return Fields.Contains(field)
            ? field
            : throw new ArgumentException("This field belongs to another form.", nameof(field));
    }
}
