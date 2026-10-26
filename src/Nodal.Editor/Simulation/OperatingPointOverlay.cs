using System.Collections.Immutable;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Geometry;
using Nodal.Foundation.Quantities;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Diagnostics;

namespace Nodal.Editor.Simulation;

public static class OperatingPointOverlay
{
    public const string UndefinedText = "undefined";
    public const string AbsorbedText = "absorbed";
    public const string DeliveredText = "delivered";

    private const double WireMidpoint = 0.5;

    public static ImmutableArray<NetVoltageLabel> Labels(
        Schematic schematic,
        ComponentCatalog catalog,
        ConnectivityResult connectivity,
        OperatingPoint operatingPoint) =>
        Labels(schematic, catalog, connectivity, SolutionSnapshot.FromDc(operatingPoint));

    public static ImmutableArray<NetVoltageLabel> Labels(
        Schematic schematic,
        ComponentCatalog catalog,
        ConnectivityResult connectivity,
        SolutionSnapshot solution)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(connectivity);
        ArgumentNullException.ThrowIfNull(solution);

        if (!solution.IsSolved)
        {
            return [];
        }

        var labels = ImmutableArray.CreateBuilder<NetVoltageLabel>(connectivity.Nets.Length);

        foreach (var net in connectivity.Nets)
        {
            if (string.Equals(
                    net.Name, SimulationDiagnostic.GroundNetName, StringComparison.Ordinal)
                || TextOf(solution, net.Name) is not { } text
                || AnchorOf(schematic, catalog, connectivity, net) is not { } position)
            {
                continue;
            }

            labels.Add(new NetVoltageLabel(net.Name, position, text));
        }

        return labels.ToImmutable();
    }

    public static string? Describe(OperatingPoint operatingPoint, ComponentId id) =>
        Describe(SolutionSnapshot.FromDc(operatingPoint), id);

    public static string? Describe(SolutionSnapshot solution, ComponentId id)
    {
        ArgumentNullException.ThrowIfNull(solution);

        if (!solution.TryGetComponent(id, out var component))
        {
            return null;
        }

        return $"{component.Designator}: {DisplayValue.Magnitude(component.Voltage)}, "
            + $"{DisplayValue.Magnitude(component.Current)}, {Magnitude(component.Power)} "
            + (component.Absorbs ? AbsorbedText : DeliveredText);
    }

    public static string? DescribeNet(OperatingPoint operatingPoint, string netName) =>
        DescribeNet(SolutionSnapshot.FromDc(operatingPoint), netName);

    public static string? DescribeNet(SolutionSnapshot solution, string netName)
    {
        ArgumentNullException.ThrowIfNull(solution);

        return TextOf(solution, netName) is { } text ? $"{netName} = {text}" : null;
    }

    private static string? TextOf(SolutionSnapshot solution, string netName)
    {
        if (!solution.IsSolved)
        {
            return null;
        }

        if (solution.IsUndefined(netName))
        {
            return UndefinedText;
        }

        return solution.TryGetNetVoltage(netName, out var voltage)
            ? DisplayValue.Text(voltage)
            : null;
    }

    private static GridVector? AnchorOf(
        Schematic schematic, ComponentCatalog catalog, ConnectivityResult connectivity, Net net) =>
        JunctionOf(schematic, catalog, connectivity, net)
        ?? FirstWireMidpoint(schematic, net)
        ?? FirstPin(schematic, catalog, net);

    private static GridVector? JunctionOf(
        Schematic schematic, ComponentCatalog catalog, ConnectivityResult connectivity, Net net)
    {
        foreach (var junction in connectivity.Junctions)
        {
            if (TouchesWire(schematic, net, junction) || TouchesPin(schematic, catalog, net, junction))
            {
                return Of(junction);
            }
        }

        return null;
    }

    private static bool TouchesWire(Schematic schematic, Net net, GridPoint point) =>
        net.Wires.Any(
            id => schematic.TryGetWire(id, out var wire) && WireGeometry.Contains(wire, point));

    private static bool TouchesPin(
        Schematic schematic, ComponentCatalog catalog, Net net, GridPoint point) =>
        net.Pins.Any(pin => PositionOf(schematic, catalog, pin) == point);

    private static GridVector? FirstWireMidpoint(Schematic schematic, Net net)
    {
        if (net.Wires.Length == 0 || !schematic.TryGetWire(net.Wires[0], out var wire))
        {
            return null;
        }

        return new GridVector(
            (wire.Start.X + wire.End.X) * WireMidpoint, (wire.Start.Y + wire.End.Y) * WireMidpoint);
    }

    private static GridVector? FirstPin(
        Schematic schematic, ComponentCatalog catalog, Net net) =>
        net.Pins.Length > 0 && PositionOf(schematic, catalog, net.Pins[0]) is { } position
            ? Of(position)
            : null;

    private static GridPoint? PositionOf(Schematic schematic, ComponentCatalog catalog, PinRef pin)
    {
        if (!schematic.TryGetPinPositions(pin.ComponentId, catalog, out var positions))
        {
            return null;
        }

        foreach (var placed in positions)
        {
            if (string.Equals(placed.PinName, pin.PinName, StringComparison.Ordinal))
            {
                return placed.Position;
            }
        }

        return null;
    }

    private static GridVector Of(GridPoint point) => new(point.X, point.Y);

    private static string Magnitude(Quantity quantity) =>
        new Quantity(Math.Abs(quantity.Value), quantity.Unit).ToString();
}
