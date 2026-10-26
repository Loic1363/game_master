using System.Collections.Immutable;
using Nodal.Circuit.Commands;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Hit;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Geometry;

namespace Nodal.Editor.Tools;

internal sealed class ProbeTool : IEditorTool
{
    private const string ToolLabel = "Probe";

    private readonly EditorSession _session;

    public ProbeTool(EditorSession session) => _session = session;

    public EditorToolKind Kind => EditorToolKind.Probe;

    public string Label => ToolLabel;

    public ComponentGhost? Ghost => null;

    public ImmutableArray<WireSegment> Preview => [];

    public void PointerMoved(GridVector point)
    {
    }

    public void PrimaryPressed(GridVector point)
    {
    }

    public void PrimaryReleased(GridVector point)
    {
        var connectivity = _session.Analyze();

        if (ProbeAt(point, connectivity) is { } probe)
        {
            _session.Toggle(probe, connectivity);
        }
    }

    public bool SecondaryPressed(GridVector point) => false;

    public void DoubleClicked()
    {
    }

    public bool Cancel() => false;

    public void Rotate()
    {
    }

    public void Mirror()
    {
    }

    private Probe? ProbeAt(GridVector point, ConnectivityResult connectivity)
    {
        var hit = _session.Pick(point);

        if (hit is null)
        {
            return null;
        }

        if (hit.Pin is { } pin)
        {
            return VoltageProbeOn(pin, connectivity);
        }

        if (hit.Wire is { } wire)
        {
            return VoltageProbeOn(wire, connectivity);
        }

        return hit.Body is { } body ? Probe.Through(body) : null;
    }

    private Probe VoltageProbeOn(PinRef pin, ConnectivityResult connectivity) =>
        connectivity.TryGetNetOfPin(pin, out var net)
            ? Probe.At(FirstPinOf(net) ?? pin)
            : Probe.At(pin);

    private Probe VoltageProbeOn(WireId wire, ConnectivityResult connectivity)
    {
        if (!connectivity.TryGetNetOfWire(wire, out var net))
        {
            return Probe.At(wire);
        }

        return FirstPinOf(net) is { } pin ? Probe.At(pin) : Probe.At(wire);
    }

    private PinRef? FirstPinOf(Net net)
    {
        if (net.Pins.IsEmpty)
        {
            return null;
        }

        var order = PinRefOrder.For(_session.Schematic, _session.Catalog);

        return net.Pins.OrderBy(pin => pin, order).First();
    }
}
