using Nodal.Circuit.Schematics;

namespace Nodal.Simulation.Transient;

public sealed record TransientOptions
{
    public TransientOptions(double stopTime)
    {
        if (!double.IsFinite(stopTime) || stopTime <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stopTime),
                stopTime,
                "A transient analysis needs a finite positive stop time in seconds.");
        }

        StopTime = stopTime;
    }

    public static TransientOptions From(TransientSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new TransientOptions(settings.StopTime)
        {
            OutputStep = settings.OutputStep,
            MaxStep = settings.MaxStep,
            StartFromZero = settings.StartFromZero,
        };
    }

    public TransientSettings ToSettings() =>
        new(StopTime, OutputStep, MaxStep, StartFromZero);

    public double StopTime { get; }

    public double? OutputStep { get; init; }

    public double? MaxStep { get; init; }

    public double? MinStep { get; init; }

    public double RelTol { get; init; } = 1e-3;

    public double VnTol { get; init; } = 1e-6;

    public double AbsTol { get; init; } = 1e-12;

    public double ChgTol { get; init; } = 1e-14;

    public double TrTol { get; init; } = 7;

    public int MaxIterations { get; init; } = 10;

    public double Gmin { get; init; } = 1e-12;

    public bool StartFromZero { get; init; }
}
