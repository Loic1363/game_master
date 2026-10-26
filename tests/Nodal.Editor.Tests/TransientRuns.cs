using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Waveforms;
using Nodal.Simulation.Transient;

namespace Nodal.Editor.Tests;

internal static class TransientRuns
{
    public const double Load = 1000;
    public const double Capacitance = 100e-12;
    public const double StoppedAt = 1e-6;

    public static Waveform Pulse => new PulseWaveform(0, 5, 1e-6, 1e-9, 1e-9, 2e-6, 5e-6);

    public static Schematic Rc() => Schematic.Empty
        .Add("voltage-source", 0, 0)
        .Add("ground", 0, 2)
        .Add("resistor", 6, 0, Sessions.FirstPinUp)
        .Add("capacitor", 6, 6, Sessions.FirstPinUp)
        .Add("ground", 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("R1", "resistance", Load)
        .Set("C1", "capacitance", Capacitance);

    public static Schematic PulsedRc() => Rc().Function("V1", Pulse);

    public static TransientResult Solved() =>
        Run(PulsedRc(), new TransientOptions(20e-6) { StartFromZero = true });

    public static TransientResult Stopped() => Run(
        PulsedRc(),
        new TransientOptions(20e-6)
        {
            OutputStep = 1e-6,
            MaxStep = 1e-6,
            MinStep = 50e-9,
        });

    public static TransientResult Run(Schematic schematic, TransientOptions options) =>
        TransientAnalysis.Run(
            NetlistCompiler.Compile(schematic, BaseContent.Catalog),
            BaseContent.Catalog,
            options);
}
