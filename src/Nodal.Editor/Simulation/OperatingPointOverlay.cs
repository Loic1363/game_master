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
        OperatingPoint operatingPoint)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(connectivity);
        ArgumentNullException.ThrowIfNull(operatingPoint);

        if (!operatingPoint.IsSolved)
        {
            return [];
        }

        var labels = ImmutableArray.CreateBuilder<NetVoltageLabel>(connectivity.Nets.Length);

        foreach (var net in connectivity.Nets)
        {
            if (string.Equals(
                    net.Name, SimulationDiagnostic.GroundNetName, StringComparison.Ordinal)
                || !operatingPoint.TryGetNetVoltage(net.Name, out var voltage)
                || AnchorOf(schematic, catalog, connectivity, net) is not { } position)
            {
                continue;
            }

            labels.Add(new NetVoltageLabel(
                net.Name, position, TextOf(net.Name, voltage, operatingPoint)));
        }

        return labels.ToImmutable();
    }

    public static string? Describe(OperatingPoint operatingPoint, ComponentId id)
    {
        ArgumentNullException.ThrowIfNull(operatingPoint);

        if (!operatingPoint.TryGetComponent(id, out var component))
        {
            return null;
        }

        return $"{component.Designator}: {Magnitude(component.Voltage)}, "
            + $"{Magnitude(component.Current)}, {Magnitude(component.Power)} "
            + (component.Absorbs ? AbsorbedText : DeliveredText);
    }

    private static string TextOf(string netName, Quantity voltage, OperatingPoint operatingPoint) =>
        operatingPoint.IsUndefined(netName) ? UndefinedText : voltage.ToString();

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
