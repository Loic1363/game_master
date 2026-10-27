using Nodal.Circuit.Schematics;

namespace Nodal.Simulation.Tests.Dc;

internal static class TransistorCircuits
{
    public const double Supply = 5;

    public static Schematic BiasBox(string definitionId) =>
        BiasBox(definitionId, Supply, ControlFor(definitionId));

    public static double ControlFor(string definitionId) => definitionId switch
    {
        Circuits.Nmos => 3,
        Circuits.Pmos => Supply - 3,
        Circuits.Npn => 0.65,
        _ => Supply - 0.65,
    };

    public static Schematic CrossCoupledInverters() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Pmos, 6, 2)
        .Add(Circuits.Nmos, 6, 8)
        .Add(Circuits.Ground, 6, 10)
        .Add(Circuits.Pmos, 18, 2)
        .Add(Circuits.Nmos, 18, 8)
        .Add(Circuits.Ground, 18, 10)
        .Wire(0, -2, 6, -2)
        .Wire(6, -2, 18, -2)
        .Wire(6, -2, 6, 0)
        .Wire(18, -2, 18, 0)
        .Wire(6, 4, 6, 6)
        .Wire(4, 2, 4, 8)
        .Wire(18, 4, 18, 6)
        .Wire(16, 2, 16, 8)
        .Wire(6, 6, 16, 6)
        .Wire(18, 4, 22, 4)
        .Wire(22, 4, 22, 14)
        .Wire(22, 14, 4, 14)
        .Wire(4, 14, 4, 8)
        .Set("V1", "voltage", Supply);

    public static Schematic BiasBox(string definitionId, double supply, double control) =>
        Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(definitionId, 6, 6)
            .Add(Circuits.Ground, 6, 8)
            .Add(Circuits.VoltageSource, 0, 10)
            .Add(Circuits.Ground, 0, 12)
            .Wire(0, -2, 6, -2)
            .Wire(6, -2, 6, 4)
            .Wire(0, 8, 4, 8)
            .Wire(4, 8, 4, 6)
            .Set("V1", "voltage", supply)
            .Set("V2", "voltage", control);
}
