using System.Collections.Immutable;
using System.Text;
using Nodal.Circuit.Connectivity;

namespace Nodal.Circuit.Netlists;

public sealed class Netlist
{
    internal Netlist(
        ImmutableArray<NetlistElement> elements,
        ImmutableArray<string> netNames,
        ImmutableArray<ConnectivityDiagnostic> diagnostics)
    {
        Elements = elements;
        NetNames = netNames;
        Diagnostics = diagnostics;
    }

    public ImmutableArray<NetlistElement> Elements { get; }

    public ImmutableArray<string> NetNames { get; }

    public ImmutableArray<ConnectivityDiagnostic> Diagnostics { get; }

    public string ToDebugText()
    {
        var text = new StringBuilder();

        foreach (var element in Elements)
        {
            if (text.Length > 0)
            {
                text.Append('\n');
            }

            text.Append(element.Designator).Append(' ').Append(element.DefinitionId);

            foreach (var pin in element.Pins)
            {
                text.Append(' ').Append(pin.PinName).Append('=').Append(pin.NetName);
            }

            foreach (var parameter in element.Parameters)
            {
                text.Append(' ').Append(parameter.Key).Append('=').Append(parameter.Value);
            }
        }

        return text.ToString();
    }
}
