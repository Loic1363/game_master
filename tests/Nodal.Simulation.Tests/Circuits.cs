using Nodal.Circuit.Commands;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Quantities;
using Nodal.Foundation.Waveforms;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Elaboration;
using Nodal.Simulation.Transient;

namespace Nodal.Simulation.Tests;

internal static class Circuits
{
    public const string Resistor = "resistor";
    public const string Capacitor = "capacitor";
    public const string Inductor = "inductor";
    public const string VoltageSource = "voltage-source";
    public const string CurrentSource = "current-source";
    public const string Diode = "diode";
    public const string Ground = "ground";

    public static Orientation Upright => Orientation.Default;

    public static Orientation FirstPinUp => new(Rotation.R90, false);

    public static Orientation FirstPinDown => new(Rotation.R270, false);

    public static Schematic Add(this Schematic schematic, string definitionId, int x, int y) =>
        schematic.Add(definitionId, x, y, Orientation.Default);

    public static Schematic Add(
        this Schematic schematic, string definitionId, int x, int y, Orientation orientation) =>
        Succeed(new AddComponent(definitionId, new GridPoint(x, y), orientation), schematic);

    public static Schematic Wire(
        this Schematic schematic, int startX, int startY, int endX, int endY) =>
        Succeed(new AddWire(new GridPoint(startX, startY), new GridPoint(endX, endY)), schematic);

    public static Schematic Set(
        this Schematic schematic, string designator, string parameterName, double value)
    {
        var component = ComponentOf(schematic, designator);
        var definition = DefinitionOf(component);
        var parameter = definition.Parameters.First(
            candidate => string.Equals(candidate.Name, parameterName, StringComparison.Ordinal));

        return Succeed(
            new SetParameter(component.Id, parameterName, new Quantity(value, parameter.Unit)),
            schematic);
    }

    public static Schematic Function(
        this Schematic schematic, string designator, Waveform waveform)
    {
        var component = ComponentOf(schematic, designator);
        var definition = DefinitionOf(component);
        var parameter = definition.Parameters.First(
            candidate => candidate.Kind == ParameterKind.Waveform);

        return Succeed(
            new SetParameter(
                component.Id, parameter.Name, ParameterValue.Of(waveform, parameter.Unit)),
            schematic);
    }

    public static Netlist Compile(this Schematic schematic) =>
        NetlistCompiler.Compile(schematic, BaseCatalog.Catalog);

    public static ElaboratedCircuit Elaborate(this Schematic schematic) =>
        Elaborator.Elaborate(schematic.Compile(), BaseCatalog.Catalog);

    public static OperatingPoint Solve(this Schematic schematic) =>
        DcAnalysis.Solve(schematic.Compile(), BaseCatalog.Catalog);

    public static TransientResult Transient(
        this Schematic schematic, TransientOptions options) =>
        TransientAnalysis.Run(schematic.Compile(), BaseCatalog.Catalog, options);

    public static TransientSeries Of(this TransientResult result, string designator)
    {
        var series = result.Components.Values.FirstOrDefault(
            candidate => string.Equals(candidate.Designator, designator, StringComparison.Ordinal));

        return series ?? throw new InvalidOperationException(
            $"The result has no component {designator}.");
    }

    public static double AmperesAt(
        this TransientResult result, string designator, double time) =>
        result.CurrentAt(result.Of(designator).Id, time);

    public static double VoltsAt(this TransientResult result, string designator, double time) =>
        result.ComponentVoltageAt(result.Of(designator).Id, time);

    public static double Volts(this OperatingPoint operatingPoint, string netName)
    {
        Xunit.Assert.True(
            operatingPoint.TryGetNetVoltage(netName, out var voltage),
            $"The solution has no net {netName}.");

        return voltage.Value;
    }

    public static ComponentOperatingPoint Of(
        this OperatingPoint operatingPoint, string designator)
    {
        var component = operatingPoint.Components.Values.FirstOrDefault(
            candidate => string.Equals(candidate.Designator, designator, StringComparison.Ordinal));

        return component ?? throw new InvalidOperationException(
            $"The solution has no component {designator}.");
    }

    public static double Amperes(this OperatingPoint operatingPoint, string designator) =>
        operatingPoint.Of(designator).Current.Value;

    public static double Watts(this OperatingPoint operatingPoint, string designator) =>
        operatingPoint.Of(designator).Power.Value;

    public static string NetAt(this Schematic schematic, string designator, string pinName)
    {
        var connectivity = ConnectivityAnalyzer.Analyze(schematic, BaseCatalog.Catalog);
        var pin = new PinRef(schematic.IdOf(designator), pinName);

        return connectivity.TryGetNetOfPin(pin, out var net)
            ? net.Name
            : throw new InvalidOperationException($"The pin {pin} belongs to no net.");
    }

    public static ComponentId IdOf(this Schematic schematic, string designator) =>
        ComponentOf(schematic, designator).Id;

    private static ComponentInstance ComponentOf(Schematic schematic, string designator) =>
        schematic.Components.Values.First(
            candidate => string.Equals(candidate.Designator, designator, StringComparison.Ordinal));

    private static ComponentDefinition DefinitionOf(ComponentInstance component) =>
        BaseCatalog.Catalog.TryGetById(component.DefinitionId, out var definition)
            ? definition
            : throw new InvalidOperationException(
                $"The catalog has no definition {component.DefinitionId}.");

    private static Schematic Succeed(IEditCommand command, Schematic schematic)
    {
        var result = command.Apply(schematic, BaseCatalog.Catalog);

        return result.Succeeded
            ? result.Schematic
            : throw new InvalidOperationException($"The command failed with {result.Error}.");
    }
}
