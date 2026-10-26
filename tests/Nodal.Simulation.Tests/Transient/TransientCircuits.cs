using Nodal.Circuit.Schematics;

namespace Nodal.Simulation.Tests.Transient;

internal static class TransientCircuits
{
    public static Schematic SourceAndResistor(double resistance) => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 2)
        .Wire(0, -2, 6, -2)
        .Set("R1", "resistance", resistance);

    public static Schematic Rc(double resistance, double capacitance) => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Capacitor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("R1", "resistance", resistance)
        .Set("C1", "capacitance", capacitance);

    public static Schematic Rl(double inductance, double resistance) => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Inductor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Resistor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("L1", "inductance", inductance)
        .Set("R1", "resistance", resistance);

    public static Schematic Rlc(double resistance, double inductance, double capacitance) =>
        Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Inductor, 6, 6, Circuits.FirstPinUp)
            .Add(Circuits.Capacitor, 6, 12, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 14)
            .Wire(0, -2, 6, -2)
            .Wire(6, 2, 6, 4)
            .Wire(6, 8, 6, 10)
            .Set("R1", "resistance", resistance)
            .Set("L1", "inductance", inductance)
            .Set("C1", "capacitance", capacitance);

    public static Schematic HalfWaveRectifier(double load) => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Diode, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Resistor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("R1", "resistance", load);

    public static Schematic SmoothedRectifier(double load, double smoothing) => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Diode, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Resistor, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Add(Circuits.Capacitor, 12, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 12, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Wire(6, 4, 12, 4)
        .Set("R1", "resistance", load)
        .Set("C1", "capacitance", smoothing);
}
