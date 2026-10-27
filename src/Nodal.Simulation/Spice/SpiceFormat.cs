using System.Globalization;

namespace Nodal.Simulation.Spice;

internal static class SpiceFormat
{
    public const string LineSeparator = "\n";
    public const string GroundNode = "0";
    public const string EsrSuffix = "_esr";
    public const string EslSuffix = "_esl";
    public const string ParallelSuffix = "_par";
    public const string DiodeModelName = "NODAL_D";
    public const string NmosModelName = "NODAL_NMOS";
    public const string PmosModelName = "NODAL_PMOS";
    public const string NpnModelName = "NODAL_NPN";
    public const string PnpModelName = "NODAL_PNP";
    public const string ChannelWidth = "1e-6";
    public const string ChannelLength = "1e-6";
    public const string OptionsLine = ".options TNOM=27";
    public const string EndLine = ".end";
    public const string TitlePrefix = "* ";
    public const string TitleSuffix = " export";

    private const string MosfetParameters =
        "LEVEL=1 KP=1e-3 LAMBDA=0.02 GAMMA=0 PHI=0.6 IS=1e-14 TOX=0 "
        + "CGSO=0 CGDO=0 CGBO=0 CBD=0 CBS=0 CJ=0 CJSW=0 JS=0 RD=0 RS=0 RSH=0 LD=0";

    private const string BipolarParameters =
        "IS=1e-14 BF=100 BR=1 NF=1 NR=1 VAF=0 VAR=0 IKF=0 IKR=0 ISE=0 ISC=0 "
        + "RB=0 RE=0 RC=0 CJE=0 CJC=0 CJS=0 TF=0 TR=0";

    public const string DiodeModelLine = ".model NODAL_D D(IS=1e-14 N=1)";

    public const string NmosModelLine =
        ".model " + NmosModelName + " NMOS(VTO=1 " + MosfetParameters + ")";

    public const string PmosModelLine =
        ".model " + PmosModelName + " PMOS(VTO=-1 " + MosfetParameters + ")";

    public const string NpnModelLine =
        ".model " + NpnModelName + " NPN(" + BipolarParameters + ")";

    public const string PnpModelLine =
        ".model " + PnpModelName + " PNP(" + BipolarParameters + ")";

    public static string Value(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    public static string Count(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    public static string Name(string designator, char type, string suffix) =>
        (designator.StartsWith(type.ToString(), StringComparison.OrdinalIgnoreCase)
            ? designator
            : type + designator) + suffix;
}
