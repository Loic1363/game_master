using Nodal.Circuit.Commands;
using Nodal.Circuit.Editing;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Foundation.Geometry;
using Nodal.Foundation.Quantities;
using Nodal.Foundation.Waveforms;

namespace Nodal.Editor.Tests;

internal static class Sessions
{
    public const double Tolerance = 0.4;

    public const double DragThreshold = 0.25;

    public static EditorSession Empty() => Of(Schematic.Empty);

    public static EditorSession Of(Schematic schematic) =>
        new(new EditHistory(schematic, BaseContent.Catalog), BaseContent.Symbols)
        {
            HitTolerance = Tolerance,
            DragThreshold = DragThreshold,
        };

    public static Orientation FirstPinUp => new(Rotation.R90, false);

    public static Schematic Add(this Schematic schematic, string definitionId, int x, int y) =>
        schematic.Add(definitionId, x, y, Orientation.Default);

    public static Schematic Add(
        this Schematic schematic, string definitionId, int x, int y, Orientation orientation) =>
        Succeed(new AddComponent(definitionId, new GridPoint(x, y), orientation), schematic);

    public static Schematic Set(
        this Schematic schematic, string designator, string parameterName, double value)
    {
        var component = ComponentOf(schematic, designator);
        var parameter = ParameterOf(
            component, candidate => string.Equals(
                candidate.Name, parameterName, StringComparison.Ordinal));

        return Succeed(
            new SetParameter(component.Id, parameter.Name, new Quantity(value, parameter.Unit)),
            schematic);
    }

    public static Schematic Function(
        this Schematic schematic, string designator, Waveform waveform)
    {
        var component = ComponentOf(schematic, designator);
        var parameter = ParameterOf(
            component, candidate => candidate.Kind == ParameterKind.Waveform);

        return Succeed(
            new SetParameter(
                component.Id, parameter.Name, ParameterValue.Of(waveform, parameter.Unit)),
            schematic);
    }

    public static Schematic Wire(this Schematic schematic, int startX, int startY, int endX, int endY) =>
        Succeed(new AddWire(new GridPoint(startX, startY), new GridPoint(endX, endY)), schematic);

    public static void ClickAt(this EditorSession session, double x, double y)
    {
        session.PrimaryPressed(new GridVector(x, y));
        session.PrimaryReleased(new GridVector(x, y));
    }

    public static void DragFrom(
        this EditorSession session, double fromX, double fromY, double toX, double toY)
    {
        session.PrimaryPressed(new GridVector(fromX, fromY));
        session.PointerMoved(new GridVector(toX, toY));
        session.PrimaryReleased(new GridVector(toX, toY));
    }

    public static ComponentId IdOf(this Schematic schematic, string designator) =>
        schematic.Components.Values.First(
            component => string.Equals(
                component.Designator, designator, StringComparison.Ordinal)).Id;

    public static ComponentInstance Component(this EditorSession session, int id)
    {
        Assert(session.Schematic.TryGetComponent(new ComponentId(id), out var component));

        return component!;
    }

    private static ComponentInstance ComponentOf(Schematic schematic, string designator) =>
        schematic.Components.Values.First(
            component => string.Equals(
                component.Designator, designator, StringComparison.Ordinal));

    private static ParameterDefinition ParameterOf(
        ComponentInstance component, Func<ParameterDefinition, bool> matches)
    {
        if (!BaseContent.Catalog.TryGetById(component.DefinitionId, out var definition))
        {
            throw new InvalidOperationException(
                $"The catalog has no definition {component.DefinitionId}.");
        }

        return definition.Parameters.First(matches);
    }

    private static Schematic Succeed(IEditCommand command, Schematic schematic)
    {
        var result = command.Apply(schematic, BaseContent.Catalog);

        return result.Succeeded
            ? result.Schematic
            : throw new InvalidOperationException($"The command failed with {result.Error}.");
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("The schematic does not hold the expected component.");
        }
    }
}
