using Nodal.Circuit.Schematics;
using Nodal.Foundation.Waveforms;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Transient;
using Xunit;

namespace Nodal.Simulation.Tests.Transient;

public class TransientRefusalTests
{
    private static SimulationDiagnostic Single(TransientResult result)
    {
        Assert.False(result.IsSolved);
        Assert.False(result.IsPartial);
        Assert.False(result.HasValues);
        Assert.Empty(result.Times);
        Assert.Null(result.Statistics);
        Assert.Null(result.FailureTime);

        return Assert.Single(result.Errors);
    }

    [Fact]
    public void ACircuitWithoutGroundIsRefused()
    {
        var schematic = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Wire(0, -2, 6, -2)
            .Wire(0, 2, 6, 2)
            .Set("V1", "voltage", 5)
            .Set("R1", "resistance", 1000);

        var diagnostic = Single(schematic.Transient(new TransientOptions(1e-3)));

        Assert.Equal(SimulationDiagnosticCode.NoGround, diagnostic.Code);
    }

    [Fact]
    public void AMinimumStepCoarserThanAPulseEdgeIsReportedWithTheTime()
    {
        var pulse = new PulseWaveform(0, 5, 1e-6, 1e-9, 1e-9, 2e-6, 5e-6);

        var result = TransientCircuits.Rc(1000, 100e-12)
            .Function("V1", pulse)
            .Transient(new TransientOptions(20e-6)
            {
                OutputStep = 1e-6,
                MaxStep = 1e-6,
                MinStep = 50e-9,
            });

        Assert.False(result.IsSolved);
        Assert.True(result.IsPartial);

        var diagnostic = Assert.Single(result.Errors);

        Assert.Equal(SimulationDiagnosticCode.TimestepTooSmall, diagnostic.Code);
        Assert.Equal(
            "The transient analysis failed at t = 1 µs: the timestep became too small.",
            diagnostic.Message);
    }

    [Fact]
    public void ACapacitorDirectlyAcrossAnIdealSourceCannotStartFromZero()
    {
        var schematic = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Capacitor, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 2)
            .Wire(0, -2, 6, -2)
            .Set("V1", "voltage", 5)
            .Set("C1", "capacitance", 100e-9);

        var result = schematic.Transient(
            new TransientOptions(1e-3) { StartFromZero = true });

        var diagnostic = Single(result);

        Assert.Equal(SimulationDiagnosticCode.Singular, diagnostic.Code);
    }

    [Fact]
    public void ADiodeDirectlyAcrossAnIdealSourceReportsTheUnboundedCurrent()
    {
        var schematic = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Diode, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 2)
            .Wire(0, -2, 6, -2)
            .Set("V1", "voltage", 5);

        var diagnostic = Single(schematic.Transient(new TransientOptions(1e-3)));

        Assert.Equal(SimulationDiagnosticCode.UnboundedCurrent, diagnostic.Code);
    }

    [Fact]
    public void ANetWithoutAPathToGroundIsWarnedAboutAndLeftOut()
    {
        var schematic = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.CurrentSource, 12, 0, Circuits.FirstPinUp)
            .Wire(0, -2, 6, -2)
            .Wire(6, 2, 12, 2)
            .Set("V1", "voltage", 5)
            .Set("R1", "resistance", 1000)
            .Set("I1", "current", 1e-3);

        var result = schematic.Transient(new TransientOptions(1e-3));

        Assert.True(result.IsSolved);
        Assert.NotEmpty(result.UndefinedNets);

        foreach (var net in result.UndefinedNets)
        {
            Assert.DoesNotContain(net, result.NetVoltages.Keys);
        }
    }
}
