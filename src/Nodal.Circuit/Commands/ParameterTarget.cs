using Nodal.Circuit.Schematics;
using Nodal.Content.Components;

namespace Nodal.Circuit.Commands;

internal sealed record ParameterTarget(ComponentInstance Component, ParameterDefinition Parameter);
