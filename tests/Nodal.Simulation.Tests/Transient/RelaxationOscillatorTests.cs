using Nodal.Simulation.Tests.Dc;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class RelaxationOscillatorTests
{
    private const double Supply = 5;
    private const double Resistance = 10000;
    private const double Capacitance = 1e-7;
    private const double OnResistance = 100;
    private const double Threshold = 2.5;
    private const double Hysteresis = 1;
    private const double StopTime = 5e-3;
    private const double RelativeInstant = 1e-4;
    private const double RelativeVoltage = 1e-3;

    private const double High = Threshold + Hysteresis;
    private const double Low = Threshold - Hysteresis;
    private const double ChargeTau = Resistance * Capacitance;

    [Fact]
    public void TheOscillatorClosesItsSwitchAtTheChargingThreshold()
    {
        var closings = Transitions(closing: true);

        Assert.NotEmpty(closings);

        Approximately.Equal(
            ChargeTau * Math.Log(Supply / (Supply - High)), closings[0].Time, RelativeInstant);

        foreach (var closing in closings)
        {
            Approximately.Equal(High, closing.Voltage, RelativeVoltage);
        }
    }

    [Fact]
    public void TheOscillatorOpensItsSwitchAtTheDischargingThreshold()
    {
        var openings = Transitions(closing: false);

        Assert.NotEmpty(openings);

        foreach (var opening in openings)
        {
            Approximately.Equal(Low, opening.Voltage, RelativeVoltage);
        }
    }

    [Fact]
    public void EveryPeriodOfTheOscillatorMatchesItsTimeConstants()
    {
        var closings = Transitions(closing: true);

        Assert.True(closings.Count >= 5, $"only {closings.Count} closings were observed.");

        for (var index = 1; index < closings.Count; index++)
        {
            Approximately.Equal(
                ExpectedPeriod, closings[index].Time - closings[index - 1].Time, RelativeInstant);
        }
    }

    private static double ExpectedPeriod
    {
        get
        {
            var dischargeResistance = 1 / ((1 / OnResistance) + (1 / Resistance));
            var floor = Supply * OnResistance / (OnResistance + Resistance);
            var discharge = dischargeResistance * Capacitance
                * Math.Log((High - floor) / (Low - floor));

            return (ChargeTau * Math.Log((Supply - Low) / (Supply - High))) + discharge;
        }
    }

    private static List<Transition> Transitions(bool closing)
    {
        var result = ControlCircuits
            .RelaxationOscillator(Supply, Resistance, Capacitance, OnResistance)
            .Set("S1", "threshold", Threshold)
            .Set("S1", "hysteresis", Hysteresis)
            .Transient(new TransientOptions(StopTime)
            {
                StartFromZero = true,
                MaxStep = 1e-6,
                RelTol = 1e-6,
                VnTol = 1e-12,
                AbsTol = 1e-15,
                ChgTol = 1e-16,
                TrTol = 1,
            });

        Assert.True(result.IsSolved, string.Join(
            "; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));

        var id = result.Of("S1").Id;
        var transitions = new List<Transition>();
        var previous = result.IsClosedAt(id, result.Times[0]);

        foreach (var time in result.Times)
        {
            var closed = result.IsClosedAt(id, time);

            if (closed != previous && closed == closing)
            {
                transitions.Add(new Transition(time, result.VoltsAt("C1", time)));
            }

            previous = closed;
        }

        return transitions;
    }

    private readonly record struct Transition(double Time, double Voltage);
}
