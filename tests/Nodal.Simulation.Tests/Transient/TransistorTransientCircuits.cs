using Nodal.Circuit.Schematics;
using Nodal.Foundation.Waveforms;

namespace Nodal.Simulation.Tests.Transient;

internal static class TransistorTransientCircuits
{
    public static Schematic CmosInverter(double supply, double load, Waveform input) =>
        Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Pmos, 6, 2)
            .Add(Circuits.Nmos, 6, 8)
            .Add(Circuits.Ground, 6, 10)
            .Add(Circuits.VoltageSource, 0, 12)
            .Add(Circuits.Ground, 0, 14)
            .Add(Circuits.Capacitor, 12, 8, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 12, 10)
            .Wire(0, -2, 6, -2)
            .Wire(6, -2, 6, 0)
            .Wire(6, 4, 6, 6)
            .Wire(4, 2, 4, 8)
            .Wire(0, 10, 4, 10)
            .Wire(4, 10, 4, 8)
            .Wire(6, 6, 12, 6)
            .Set("V1", "voltage", supply)
            .Set("C1", "capacitance", load)
            .Function("V2", input);

    public static Schematic NpnSwitch(
        double supply, double load, double baseResistance, Waveform input) =>
        Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Npn, 6, 6)
            .Add(Circuits.Ground, 6, 8)
            .Add(Circuits.Resistor, 0, 6)
            .Add(Circuits.VoltageSource, -6, 8)
            .Add(Circuits.Ground, -6, 10)
            .Wire(0, -2, 6, -2)
            .Wire(6, 2, 6, 4)
            .Wire(2, 6, 4, 6)
            .Wire(-6, 6, -2, 6)
            .Set("V1", "voltage", supply)
            .Set("R1", "resistance", load)
            .Set("R2", "resistance", baseResistance)
            .Function("V2", input);
}
