namespace Nodal.Simulation.Diagnostics;

public enum SimulationDiagnosticCode
{
    Connectivity,
    NoGround,
    UnsupportedComponent,
    NoDcPath,
    Singular,
    NonConvergence,
    UnboundedCurrent,
    UnsupportedWaveformFeature,
    TimestepTooSmall,
    RatingExceeded,
}
