using System.Collections.Immutable;
using System.Globalization;
using Nodal.Circuit.Schematics;
using Nodal.Foundation.Collections;
using Nodal.Foundation.Waveforms;

namespace Nodal.Simulation.Elaboration;

public sealed record Primitive(
    ComponentId Owner,
    string Designator,
    PrimitiveKind Kind,
    PrimitiveRole Role,
    ImmutableArray<string> Nodes,
    double Value,
    Waveform? Function = null,
    SwitchSettings? Switch = null)
{
    public Primitive(
        ComponentId owner,
        string designator,
        PrimitiveKind kind,
        PrimitiveRole role,
        string plusNode,
        string minusNode,
        double value,
        Waveform? function = null)
        : this(owner, designator, kind, role, [plusNode, minusNode], value, function)
    {
    }

    public string PlusNode => Nodes[0];

    public string MinusNode => Nodes[1];

    public string ControlNode => Nodes[2];

    public string ControlPlusNode => Nodes[2];

    public string ControlMinusNode => Nodes[3];

    public bool IsSource =>
        Kind is PrimitiveKind.VoltageSource or PrimitiveKind.CurrentSource;

    public bool StoresEnergy => Kind is PrimitiveKind.Capacitor or PrimitiveKind.Inductor;

    public bool IsMosfet => Kind is PrimitiveKind.Nmos or PrimitiveKind.Pmos;

    public bool IsBipolar => Kind is PrimitiveKind.Npn or PrimitiveKind.Pnp;

    public bool IsTransistor => IsMosfet || IsBipolar;

    public bool IsControlledSource =>
        Kind is PrimitiveKind.Vcvs or PrimitiveKind.Vccs
            or PrimitiveKind.Cccs or PrimitiveKind.Ccvs;

    public bool IsCurrentControlled => Kind is PrimitiveKind.Cccs or PrimitiveKind.Ccvs;

    public bool IsControlledVoltage => Kind is PrimitiveKind.Vcvs or PrimitiveKind.Ccvs;

    public bool IsControlledCurrent => Kind is PrimitiveKind.Vccs or PrimitiveKind.Cccs;

    public bool IsCurrentSense => Role == PrimitiveRole.CurrentSense;

    public bool IsNonlinear =>
        Kind == PrimitiveKind.Diode || Kind == PrimitiveKind.Switch || IsTransistor;

    public ImmutableArray<(string From, string To)> ConductionPaths => IsBipolar
        ? [(ControlNode, MinusNode), (ControlNode, PlusNode)]
        : [(PlusNode, MinusNode)];

    public double ValueAt(double time) => Function?.ValueAt(time) ?? Value;

    public double ResistanceAt(double time) => Switch?.ResistanceAt(time) ?? Value;

    public bool Equals(Primitive? other) =>
        other is not null
        && Owner == other.Owner
        && string.Equals(Designator, other.Designator, StringComparison.Ordinal)
        && Kind == other.Kind
        && Role == other.Role
        && StructuralEquality.ItemsEqual(Nodes, other.Nodes)
        && Value.Equals(other.Value)
        && Equals(Function, other.Function)
        && Equals(Switch, other.Switch);

    public override int GetHashCode() => HashCode.Combine(
        Owner,
        Designator,
        Kind,
        Role,
        StructuralEquality.ItemsHash(Nodes),
        Value,
        Function,
        Switch);

    public override string ToString() =>
        $"{Kind} {Designator} {Role} {string.Join(' ', Nodes)} "
        + (Function?.ToString() ?? Value.ToString("R", CultureInfo.InvariantCulture));
}
