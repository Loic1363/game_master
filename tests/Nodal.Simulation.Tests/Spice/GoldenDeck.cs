using Nodal.Circuit.Schematics;

namespace Nodal.Simulation.Tests.Spice;

internal static class GoldenDeck
{
    public const string FileName = "golden-deck.cir";

    public static Schematic Build() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 2)
        .Add(Circuits.Capacitor, 12, 0, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 12, 2)
        .Add(Circuits.Inductor, 18, 0, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 18, 2)
        .Add(Circuits.Diode, 24, 0, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 24, 2)
        .Add(Circuits.CurrentSource, 30, 0)
        .Add(Circuits.Ground, 30, 2)
        .Add(Circuits.Nmos, 36, 0)
        .Add(Circuits.Ground, 36, 2)
        .Add(Circuits.Pmos, 42, 0)
        .Add(Circuits.Ground, 42, 2)
        .Add(Circuits.Npn, 48, 0)
        .Add(Circuits.Ground, 48, 2)
        .Add(Circuits.Pnp, 54, 0)
        .Add(Circuits.Ground, 54, 2)
        .Wire(0, -2, 6, -2)
        .Wire(6, -2, 12, -2)
        .Wire(12, -2, 18, -2)
        .Wire(18, -2, 24, -2)
        .Wire(24, -2, 30, -2)
        .Wire(30, -2, 36, -2)
        .Wire(36, -2, 42, -2)
        .Wire(42, -2, 48, -2)
        .Wire(48, -2, 54, -2)
        .Wire(34, 0, 34, -2)
        .Wire(40, 0, 40, -2)
        .Wire(46, 0, 46, -2)
        .Wire(52, 0, 52, -2)
        .Set("V1", "voltage", 12)
        .Set("V1", "series-resistance", 10)
        .Set("R1", "resistance", 1000)
        .Set("C1", "capacitance", 1e-7)
        .Set("C1", "parallel-resistance", 1e6)
        .Set("C1", "esr", 0.05)
        .Set("C1", "esl", 1e-9)
        .Set("C1", "parallel-capacitance", 1e-12)
        .Set("L1", "inductance", 1e-3)
        .Set("L1", "series-resistance", 2)
        .Set("L1", "parallel-capacitance", 2e-12)
        .Set("I1", "current", 2e-3)
        .Set("I1", "parallel-resistance", 2200);

    public static string GoldenText() =>
        File.ReadAllText(Path.Combine(RepositoryPaths.SpiceDirectory, FileName))
            .ReplaceLineEndings("\n");
}
