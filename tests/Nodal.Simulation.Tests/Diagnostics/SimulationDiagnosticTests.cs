using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Schematics;
using Nodal.Simulation.Diagnostics;
using Xunit;

namespace Nodal.Simulation.Tests.Diagnostics;

public class SimulationDiagnosticTests
{
    private static ComponentId First => ComponentId.First;

    [Fact]
    public void AConnectivityErrorKeepsItsMessageAndComponents()
    {
        var source = ConnectivityDiagnostic.UnknownDefinition(First, "flux-capacitor");
        var diagnostic = SimulationDiagnostic.Connectivity(source);

        Assert.Equal(SimulationSeverity.Error, diagnostic.Severity);
        Assert.Equal(SimulationDiagnosticCode.Connectivity, diagnostic.Code);
        Assert.Equal(source.Message, diagnostic.Message);
        Assert.Equal([First], diagnostic.Components);
    }

    [Fact]
    public void AnUnsupportedComponentNamesItsDesignatorAndDefinition()
    {
        var diagnostic = SimulationDiagnostic.UnsupportedComponent(First, "D1", "Diode");

        Assert.Equal("D1 (Diode) is not supported by the simulator yet.", diagnostic.Message);
        Assert.Equal(SimulationSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void AMissingGroundIsAnError()
    {
        var diagnostic = SimulationDiagnostic.NoGround();

        Assert.Equal(SimulationSeverity.Error, diagnostic.Severity);
        Assert.Empty(diagnostic.Components);
        Assert.Empty(diagnostic.Nets);
    }

    [Fact]
    public void ANodeWithoutADcPathIsAWarningCarryingItsNet()
    {
        var diagnostic = SimulationDiagnostic.NoDcPath("N3");

        Assert.Equal(SimulationSeverity.Warning, diagnostic.Severity);
        Assert.Equal(
            "N3 has no DC path to ground; its voltage is undefined (shown as 0 V).",
            diagnostic.Message);
        Assert.Equal(["N3"], diagnostic.Nets);
    }

    [Fact]
    public void AnIdentifiedSingularCauseIsAppendedToTheMessage()
    {
        var diagnostic = SimulationDiagnostic.Singular("V1 and V2 clash.", [First], ["0"]);

        Assert.Equal(
            "The circuit has no unique DC solution: V1 and V2 clash.", diagnostic.Message);
    }

    [Fact]
    public void AnUnidentifiedSingularCauseSaysSo()
    {
        var diagnostic = SimulationDiagnostic.Singular(null, [], []);

        Assert.Equal(
            "The circuit has no unique DC solution; the matrix is singular and no structural "
            + "cause was found.",
            diagnostic.Message);
    }

    [Fact]
    public void DiagnosticsCompareByValue()
    {
        var left = SimulationDiagnostic.NoDcPath("N3");
        var right = SimulationDiagnostic.NoDcPath("N3");

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.NotEqual(left, SimulationDiagnostic.NoDcPath("N4"));
    }

    [Fact]
    public void TheTextFormShowsSeverityAndCode()
    {
        Assert.Equal(
            "Warning NoDcPath: N3 has no DC path to ground; its voltage is undefined "
            + "(shown as 0 V).",
            SimulationDiagnostic.NoDcPath("N3").ToString());
    }

    [Fact]
    public void ANullConnectivityDiagnosticIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => SimulationDiagnostic.Connectivity(null!));
    }
}
