using Nodal.Circuit.Schematics;
using Nodal.Simulation.Dc;
using Xunit;

namespace Nodal.Simulation.Tests.Dc;

public class DiodeTests
{
    private const double Supply = 5;
    private const double Series = 1000;
    private const double Drive = 1e-3;

    private static readonly DcOptions Tight = new()
    {
        RelTol = 1e-9,
        VnTol = 1e-12,
        AbsTol = 1e-15,
    };

    private static Schematic Forward() => Schematic.Empty
        .Add(Circuits.VoltageSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Resistor, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Diode, 6, 6, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 8)
        .Wire(0, -2, 6, -2)
        .Wire(6, 2, 6, 4)
        .Set("V1", "voltage", Supply)
        .Set("R1", "resistance", Series);

    private static Schematic DrivenByACurrentSource() => Schematic.Empty
        .Add(Circuits.CurrentSource, 0, 0)
        .Add(Circuits.Ground, 0, 2)
        .Add(Circuits.Diode, 6, 0, Circuits.FirstPinUp)
        .Add(Circuits.Ground, 6, 2)
        .Wire(0, -2, 6, -2)
        .Set("I1", "current", Drive);

    [Fact]
    public void OneMilliampereIntoADiodeMatchesTheShockleyEquation()
    {
        var expected = DiodeModel.EmissionVoltage
            * Math.Log(1 + (Drive / DiodeModel.SaturationCurrent));

        var solution = DcAnalysis.Solve(
            DrivenByACurrentSource().Compile(), BaseCatalog.Catalog, Tight);

        Assert.True(solution.IsSolved);
        Approximately.Equal(expected, solution.Of("D1").Voltage.Value, 1e-7);
    }

    [Fact]
    public void TheDiodeCarriesTheDrivenCurrentIntoItsAnode()
    {
        var solution = DcAnalysis.Solve(
            DrivenByACurrentSource().Compile(), BaseCatalog.Catalog, Tight);

        Approximately.Equal(Drive, solution.Of("D1").Current.Value, 1e-9);
        Approximately.Equal(-Drive, solution.Of("I1").Current.Value, 1e-9);
    }

    [Fact]
    public void AForwardBiasedDiodeAbsorbsPowerAndDropsAboutSevenTenthsOfAVolt()
    {
        var solution = DcAnalysis.Solve(Forward().Compile(), BaseCatalog.Catalog, Tight);
        var diode = solution.Of("D1");

        Assert.True(solution.IsSolved);
        Assert.True(diode.Absorbs);
        Assert.InRange(diode.Voltage.Value, 0.6, 0.8);
        Approximately.Equal(
            (Supply - diode.Voltage.Value) / Series, diode.Current.Value, 1e-9);
        Approximately.Equal(
            diode.Voltage.Value * diode.Current.Value, diode.Power.Value, 1e-9);
    }

    [Fact]
    public void AReverseBiasedDiodeCarriesOnlySaturationAndGminCurrent()
    {
        var schematic = Forward()
            .Add(Circuits.Diode, 12, 6, Circuits.FirstPinDown)
            .Add(Circuits.Ground, 12, 8)
            .Wire(6, 4, 12, 4);

        var solution = DcAnalysis.Solve(schematic.Compile(), BaseCatalog.Catalog, Tight);
        var reverse = solution.Of("D2");

        Assert.True(solution.IsSolved);
        Approximately.Equal(
            DiodeModel.Current(reverse.Voltage.Value), reverse.Current.Value, 1e-9);
        Assert.True(reverse.Current.Value < 0);
        Assert.True(Math.Abs(reverse.Current.Value) < 1e-11);
    }

    [Fact]
    public void TheVoltageAcrossADiodeIsTheAnodeMinusTheCathode()
    {
        var schematic = Forward();
        var solution = DcAnalysis.Solve(schematic.Compile(), BaseCatalog.Catalog, Tight);

        Approximately.Equal(
            solution.Volts(schematic.NetAt("D1", "anode"))
            - solution.Volts(schematic.NetAt("D1", "cathode")),
            solution.Of("D1").Voltage.Value);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(-0.5)]
    [InlineData(0)]
    [InlineData(0.2)]
    [InlineData(0.4)]
    [InlineData(0.6)]
    [InlineData(0.7)]
    public void TheCompanionConductanceMatchesTheFiniteDifferenceOfTheCurrent(double bias)
    {
        const double step = 1e-6;

        var difference =
            (DiodeModel.Current(bias + step) - DiodeModel.Current(bias - step)) / (2 * step);

        Approximately.Equal(difference, DiodeModel.Conductance(bias), 1e-6);
    }

    [Fact]
    public void TheCriticalVoltageFollowsTheSpiceFormula()
    {
        var expected = DiodeModel.EmissionVoltage * Math.Log(
            DiodeModel.EmissionVoltage
            / (Math.Sqrt(2) * DiodeModel.SaturationCurrent));

        Approximately.Equal(expected, DiodeModel.CriticalVoltage);
        Assert.InRange(DiodeModel.CriticalVoltage, 0.72, 0.74);
    }

    [Fact]
    public void TheThermalVoltageUsesTheConstantsOfTheReferenceSimulator()
    {
        Approximately.Equal(
            DiodeModel.BoltzmannConstant * DiodeModel.Temperature / DiodeModel.ElementaryCharge,
            DiodeModel.ThermalVoltage);

        Assert.InRange(DiodeModel.ThermalVoltage, 0.02586, 0.02587);
    }
}
