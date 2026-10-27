using Nodal.Circuit.Schematics;
using Nodal.Simulation.Dc;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class MultiTerminalResultTests
{
    [Theory]
    [InlineData(Circuits.Nmos, "M1", "drain", "source", "gate")]
    [InlineData(Circuits.Pmos, "M1", "drain", "source", "gate")]
    [InlineData(Circuits.Npn, "Q1", "collector", "emitter", "base")]
    [InlineData(Circuits.Pnp, "Q1", "collector", "emitter", "base")]
    public void EveryTransistorReportsOnePinPerTerminal(
        string definitionId, string designator, string first, string second, string control)
    {
        var component = TransistorCircuits.BiasBox(definitionId).Solve().Of(designator);

        Assert.Equal(
            new[] { first, second, control },
            component.Pins.Select(pin => pin.Name));
    }

    [Theory]
    [InlineData(Circuits.Nmos, "M1")]
    [InlineData(Circuits.Pmos, "M1")]
    [InlineData(Circuits.Npn, "Q1")]
    [InlineData(Circuits.Pnp, "Q1")]
    public void ThePinCurrentsOfATransistorSumToZero(string definitionId, string designator)
    {
        var component = TransistorCircuits.BiasBox(definitionId).Solve().Of(designator);

        Approximately.Zero(component.Pins.Sum(pin => pin.Current.Value), 1e-15);
    }

    [Theory]
    [InlineData(Circuits.Nmos, "M1", "drain")]
    [InlineData(Circuits.Pmos, "M1", "drain")]
    [InlineData(Circuits.Npn, "Q1", "collector")]
    [InlineData(Circuits.Pnp, "Q1", "collector")]
    public void TheComponentCurrentIsTheDrainOrCollectorPinCurrent(
        string definitionId, string designator, string pinName)
    {
        var component = TransistorCircuits.BiasBox(definitionId).Solve().Of(designator);

        Assert.True(component.TryGetPin(pinName, out var pin));
        Assert.Equal(component.Current.Value, pin.Current.Value);
    }

    [Theory]
    [InlineData(Circuits.Nmos, "M1")]
    [InlineData(Circuits.Npn, "Q1")]
    public void ThePinVoltagesAreMeasuredFromGround(string definitionId, string designator)
    {
        var schematic = TransistorCircuits.BiasBox(definitionId);
        var solution = schematic.Solve();
        var component = solution.Of(designator);

        foreach (var pin in component.Pins)
        {
            Approximately.Equal(
                solution.Volts(schematic.NetAt(designator, pin.Name)),
                pin.Voltage.Value,
                1e-12);
        }
    }

    [Theory]
    [InlineData(Circuits.Nmos, "M1")]
    [InlineData(Circuits.Pmos, "M1")]
    [InlineData(Circuits.Npn, "Q1")]
    [InlineData(Circuits.Pnp, "Q1")]
    public void ThePowerIsTheSumOverThePins(string definitionId, string designator)
    {
        var component = TransistorCircuits.BiasBox(definitionId).Solve().Of(designator);

        Approximately.Equal(
            component.Pins.Sum(pin => pin.Voltage.Value * pin.Current.Value),
            component.Power.Value,
            1e-12);

        Assert.True(component.Absorbs, $"{designator} must absorb power.");
    }

    [Fact]
    public void ATwoTerminalComponentKeepsItsVoltageTimesCurrentPower()
    {
        var solution = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 2)
            .Wire(0, -2, 6, -2)
            .Set("V1", "voltage", 10)
            .Set("R1", "resistance", 1000)
            .Solve();

        var resistor = solution.Of("R1");

        Approximately.Equal(0.1, resistor.Power.Value, 1e-12);
        Approximately.Equal(
            resistor.Voltage.Value * resistor.Current.Value, resistor.Power.Value, 1e-12);

        Assert.Equal(2, resistor.Pins.Length);
        Approximately.Zero(resistor.Pins.Sum(pin => pin.Current.Value), 1e-15);
    }

    [Fact]
    public void ADiodeReportsNoDeviceRegion()
    {
        var solution = Schematic.Empty
            .Add(Circuits.VoltageSource, 0, 0)
            .Add(Circuits.Ground, 0, 2)
            .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
            .Add(Circuits.Diode, 6, 6, Circuits.FirstPinUp)
            .Add(Circuits.Ground, 6, 8)
            .Wire(0, -2, 6, -2)
            .Wire(6, 2, 6, 4)
            .Set("V1", "voltage", 5)
            .Set("R1", "resistance", 1000)
            .Solve();

        Assert.Null(solution.Of("D1").Device);
        Assert.Equal(2, solution.Of("D1").Pins.Length);
    }

    [Fact]
    public void AMosfetReportsItsRegionAndKeyQuantities()
    {
        var device = TransistorCircuits.BiasBox(Circuits.Nmos).Solve().Of("M1").Device;

        Assert.NotNull(device);
        Assert.Equal(DeviceRegion.Saturation, device.Region);
        Assert.Equal(
            new[] { "Vgs", "Vds", "Id" }, device.Quantities.Select(quantity => quantity.Name));

        Approximately.Equal(3, device.Quantities[0].Value.Value, 1e-9);
        Approximately.Equal(5, device.Quantities[1].Value.Value, 1e-9);
    }

    [Fact]
    public void ABipolarReportsItsRegionKeyQuantitiesAndEffectiveBeta()
    {
        var device = TransistorCircuits.BiasBox(Circuits.Npn).Solve().Of("Q1").Device;

        Assert.NotNull(device);
        Assert.Equal(DeviceRegion.ForwardActive, device.Region);
        Assert.Equal(
            new[] { "Vbe", "Vce", "Ic", "Ib", "β" },
            device.Quantities.Select(quantity => quantity.Name));

        Approximately.Equal(0.65, device.Quantities[0].Value.Value, 1e-9);
        Approximately.Equal(5, device.Quantities[1].Value.Value, 1e-6);
        Approximately.Equal(BipolarModel.ForwardBeta, device.Quantities[4].Value.Value, 1e-4);
    }

    [Fact]
    public void AnUnbiasedBipolarReportsNoEffectiveBeta()
    {
        var device = TransistorCircuits.BiasBox(Circuits.Npn, 0, 0).Solve().Of("Q1").Device;

        Assert.NotNull(device);
        Assert.Equal(DeviceRegion.Cutoff, device.Region);
        Assert.DoesNotContain("β", device.Quantities.Select(quantity => quantity.Name));
    }

    [Fact]
    public void ACutoffBipolarWithABiasedCollectorStillLeaksAboveThePicoampThreshold()
    {
        var component = TransistorCircuits.BiasBox(Circuits.Npn, 5, 0).Solve().Of("Q1");
        var device = component.Device;

        Assert.NotNull(device);
        Assert.Equal(DeviceRegion.Cutoff, device.Region);
        Assert.True(
            Math.Abs(component.Pins.Single(pin => pin.Name == "base").Current.Value) > 1e-12,
            "The junction gmin keeps the base current above the reporting threshold.");

        Assert.Contains("β", device.Quantities.Select(quantity => quantity.Name));
    }

    [Theory]
    [InlineData(Circuits.Nmos, "M1", "N-channel MOSFET")]
    [InlineData(Circuits.Pmos, "M1", "P-channel MOSFET")]
    [InlineData(Circuits.Npn, "Q1", "NPN transistor")]
    [InlineData(Circuits.Pnp, "Q1", "PNP transistor")]
    public void EveryComponentCarriesItsCatalogName(
        string definitionId, string designator, string name)
    {
        Assert.Equal(
            name, TransistorCircuits.BiasBox(definitionId).Solve().Of(designator).Name);
    }
}
