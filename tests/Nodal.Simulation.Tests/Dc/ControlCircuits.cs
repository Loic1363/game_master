using Nodal.Circuit.Schematics;
using Nodal.Foundation.Schedules;

namespace Nodal.Simulation.Tests.Dc;

internal static class ControlCircuits
{
    public const string Vcvs = "vcvs";
    public const string Vccs = "vccs";
    public const string Cccs = "cccs";
    public const string Ccvs = "ccvs";
    public const string Switch = "switch";
    public const string PushButton = "push-button";

    public static Schematic VoltageControlled(string definitionId, double control) =>
        Schematic.Empty
            .Add(Circuits.VoltageSource, -8, 0)
            .Add(Circuits.Ground, -8, 2)
            .Add(definitionId, 0, 0)
            .Wire(-8, -2, -2, -2)
            .Wire(-2, -2, -2, -1)
            .Wire(-2, 1, -2, 2)
            .Wire(-2, 2, -8, 2)
            .Set("V1", "voltage", control);

    public static Schematic CurrentControlled(
        string definitionId, double source, double resistance) =>
        Schematic.Empty
            .Add(Circuits.VoltageSource, -14, 0)
            .Add(Circuits.Ground, -14, 2)
            .Add(Circuits.Resistor, -8, -2)
            .Add(definitionId, 0, 0)
            .Wire(-14, -2, -10, -2)
            .Wire(-6, -2, -2, -2)
            .Wire(-2, -2, -2, -1)
            .Wire(-2, 1, -2, 2)
            .Wire(-2, 2, -14, 2)
            .Set("V1", "voltage", source)
            .Set("R1", "resistance", resistance);

    public static Schematic VcvsAmplifier(double control, double gain, double load) =>
        Loaded(VoltageControlled(Vcvs, control), load).Set("E1", "gain", gain);

    public static Schematic VccsLoad(double control, double transconductance, double load) =>
        Loaded(VoltageControlled(Vccs, control), load)
            .Set("G1", "transconductance", transconductance);

    public static Schematic CccsLoad(
        double source, double resistance, double gain, double load) =>
        Loaded(CurrentControlled(Cccs, source, resistance), load, "R2").Set("F1", "gain", gain);

    public static Schematic CcvsLoad(
        double source, double resistance, double transresistance, double load) =>
        Loaded(CurrentControlled(Ccvs, source, resistance), load, "R2")
            .Set("H1", "transresistance", transresistance);

    public static Schematic SwitchLoad(double control, double supply, double load) =>
        VoltageControlled(Switch, control)
            .Add(Circuits.VoltageSource, 6, 0)
            .Add(Circuits.Ground, 6, 2)
            .Add(Circuits.Resistor, 0, 6, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 0, 8)
            .Wire(6, -2, 0, -2)
            .Wire(0, 2, 0, 4)
            .Set("V2", "voltage", supply)
            .Set("R1", "resistance", load);

    public static Schematic RelaxationOscillator(
        double supply, double resistance, double capacitance, double onResistance) =>
        Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Resistor, 4, -2)
            .Add(Circuits.Capacitor, 6, 2, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 4)
            .Add(Switch, 12, 0)
            .Add(Circuits.Ground, 12, 2)
            .Add(Circuits.Ground, 10, 1)
            .Wire(0, -2, 2, -2)
            .Wire(6, -2, 12, -2)
            .Wire(6, -2, 6, 0)
            .Wire(10, -2, 10, -1)
            .Set("V1", "voltage", supply)
            .Set("R1", "resistance", resistance)
            .Set("C1", "capacitance", capacitance)
            .Set("S1", "on-resistance", onResistance);

    public static Schematic PushButtonLoad(PressSchedule presses, double supply, double load) =>
        Schematic.Empty
            .Add(Circuits.VoltageSource, -8, 0)
            .Add(Circuits.Ground, -8, 2)
            .Add(PushButton, 0, 0)
            .Add(Circuits.Resistor, 6, 0)
            .Add(Circuits.Ground, 8, 0)
            .Wire(-8, -2, -2, -2)
            .Wire(-2, -2, -2, 0)
            .Wire(2, 0, 4, 0)
            .Set("V1", "voltage", supply)
            .Set("R1", "resistance", load)
            .Presses("SW1", presses);

    public static Schematic SwitchRelaxation(double supply, double resistance) =>
        Schematic.Empty
            .Add(Circuits.VoltageSource, -8, 0)
            .Add(Circuits.Ground, -8, 2)
            .Add(Circuits.Resistor, -4, -2)
            .Add(Switch, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Wire(-8, -2, -6, -2)
            .Wire(-2, -2, 0, -2)
            .Wire(-2, -2, -2, -1)
            .Wire(-2, 1, -2, 2)
            .Wire(-2, 2, 0, 2)
            .Set("V1", "voltage", supply)
            .Set("R1", "resistance", resistance);

    public static Schematic VcvsFeedback(double gain) =>
        Schematic.Empty
            .Add(Vcvs, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Wire(0, -2, -2, -2)
            .Wire(-2, -2, -2, -1)
            .Wire(-2, 1, -2, 2)
            .Wire(-2, 2, 0, 2)
            .Set("E1", "gain", gain);

    public static Schematic ParallelControlledSources(double gain) =>
        VcvsFeedback(gain)
            .Add(Vcvs, 12, 0)
            .Add(Circuits.Ground, 12, 2)
            .Add(Circuits.Ground, 10, 1)
            .Wire(12, -2, 0, -2)
            .Wire(10, -1, 10, 0)
            .Wire(10, 0, 8, 0)
            .Set("E2", "gain", gain);

    public static Schematic InvertingAmplifier(
        double gain, double input, double inputResistance, double feedbackResistance) =>
        Schematic.Empty
            .Add(Circuits.VoltageSource, -20, 0)
            .Add(Circuits.Ground, -20, 2)
            .Add(Circuits.Resistor, -14, -2)
            .Add(Vcvs, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Ground, -6, -1)
            .Add(Circuits.Resistor, -6, -8)
            .Wire(-20, -2, -16, -2)
            .Wire(-12, -2, -12, 4)
            .Wire(-12, 4, -2, 4)
            .Wire(-2, 4, -2, 1)
            .Wire(-2, -1, -6, -1)
            .Wire(0, -2, 0, -8)
            .Wire(0, -8, -4, -8)
            .Wire(-8, -8, -12, -8)
            .Wire(-12, -8, -12, -2)
            .Set("V1", "voltage", input)
            .Set("R1", "resistance", inputResistance)
            .Set("R2", "resistance", feedbackResistance)
            .Set("E1", "gain", gain);

    private static Schematic Loaded(
        Schematic schematic, double load, string designator = "R1") =>
        schematic
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 2)
            .Wire(0, -2, 6, -2)
            .Set(designator, "resistance", load);
}
