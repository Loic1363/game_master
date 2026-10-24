using Nodal.Foundation.Geometry;

namespace Nodal.Editor.Simulation;

public sealed record NetVoltageLabel(string NetName, GridVector Position, string Text);
