using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;

namespace Nodal.Circuit.Connectivity;

public sealed class ConnectivityResult
{
    private readonly ImmutableDictionary<PinRef, Net> _netByPin;
    private readonly ImmutableDictionary<WireId, Net> _netByWire;

    internal ConnectivityResult(
        ImmutableArray<Net> nets,
        ImmutableArray<GridPoint> junctions,
        ImmutableArray<ConnectivityDiagnostic> diagnostics)
    {
        Nets = nets;
        Junctions = junctions;
        Diagnostics = diagnostics;
        NetNames = [.. nets.Select(net => net.Name)];

        var byPin = ImmutableDictionary.CreateBuilder<PinRef, Net>();
        var byWire = ImmutableDictionary.CreateBuilder<WireId, Net>();

        foreach (var net in nets)
        {
            foreach (var pin in net.Pins)
            {
                byPin[pin] = net;
            }

            foreach (var wire in net.Wires)
            {
                byWire[wire] = net;
            }
        }

        _netByPin = byPin.ToImmutable();
        _netByWire = byWire.ToImmutable();
    }

    public ImmutableArray<Net> Nets { get; }

    public ImmutableArray<string> NetNames { get; }

    public ImmutableArray<GridPoint> Junctions { get; }

    public ImmutableArray<ConnectivityDiagnostic> Diagnostics { get; }

    public bool HasErrors =>
        Diagnostics.Any(diagnostic => diagnostic.Severity == ConnectivitySeverity.Error);

    public bool TryGetNetOfPin(PinRef pin, [NotNullWhen(true)] out Net? net) =>
        _netByPin.TryGetValue(pin, out net);

    public bool TryGetNetOfWire(WireId id, [NotNullWhen(true)] out Net? net) =>
        _netByWire.TryGetValue(id, out net);
}
