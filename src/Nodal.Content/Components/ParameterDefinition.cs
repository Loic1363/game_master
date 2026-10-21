using Nodal.Foundation.Quantities;

namespace Nodal.Content.Components;

public sealed record ParameterDefinition(string Name, Unit Unit, Quantity Default, StandardSeries? Series);
