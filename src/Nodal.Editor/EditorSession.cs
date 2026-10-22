using Nodal.Circuit.Commands;
using Nodal.Circuit.Editing;
using Nodal.Circuit.Geometry;
using Nodal.Circuit.Hit;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Content.Symbols;
using Nodal.Editor.Tools;
using Nodal.Foundation.Geometry;

namespace Nodal.Editor;

public sealed class EditorSession
{
    private const double DefaultHitTolerance = 0.5;
    private const double DefaultDragThreshold = 0.25;

    private readonly EditHistory _history;
    private readonly SymbolLibrary _symbols;

    private IEditorTool _tool;
    private Selection _selection = Selection.None;
    private GridPoint _cursor = GridPoint.Origin;
    private double _hitTolerance = DefaultHitTolerance;
    private double _dragThreshold = DefaultDragThreshold;

    public EditorSession(EditHistory history, SymbolLibrary symbols)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(symbols);

        _history = history;
        _symbols = symbols;
        _tool = new SelectTool(this);
    }

    public event Action? Changed;

    public event Action<ComponentId>? PropertiesRequested;

    public EditHistory History => _history;

    public Schematic Schematic => _history.Schematic;

    public ComponentCatalog Catalog => _history.Catalog;

    public SymbolLibrary Symbols => _symbols;

    public Selection Selection => _selection;

    public EditorToolKind Tool => _tool.Kind;

    public double HitTolerance
    {
        get => _hitTolerance;
        set
        {
            if (!double.IsFinite(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, "A hit tolerance must be finite and not negative.");
            }

            _hitTolerance = value;
        }
    }

    public double DragThreshold
    {
        get => _dragThreshold;
        set
        {
            if (!double.IsFinite(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, "A drag threshold must be finite and not negative.");
            }

            _dragThreshold = value;
        }
    }

    public EditorView View => new(
        _tool.Kind, _tool.Label, _cursor, _selection, _tool.Ghost, _tool.Preview);

    public void ChooseSelect() => Replace(new SelectTool(this));

    public void ChoosePlace(string definitionId)
    {
        ArgumentNullException.ThrowIfNull(definitionId);

        if (!_history.Catalog.TryGetById(definitionId, out var definition))
        {
            return;
        }

        Replace(new PlaceTool(this, definition.Id, definition.Name, _cursor));
    }

    public void ChooseWire() => Replace(new WireTool(this, _cursor));

    public void PointerMoved(GridVector point)
    {
        _cursor = GridSnap.Nearest(point);
        _tool.PointerMoved(point);
    }

    public void PrimaryPressed(GridVector point)
    {
        _cursor = GridSnap.Nearest(point);
        _tool.PrimaryPressed(point);
    }

    public void PrimaryReleased(GridVector point)
    {
        _cursor = GridSnap.Nearest(point);
        _tool.PrimaryReleased(point);
    }

    public void SecondaryPressed(GridVector point)
    {
        _cursor = GridSnap.Nearest(point);

        if (_tool.SecondaryPressed(point))
        {
            Changed?.Invoke();

            return;
        }

        Cancel();
    }

    public void DoubleClicked() => _tool.DoubleClicked();

    public void RequestPropertiesForSelection()
    {
        if (_selection.Component is { } component)
        {
            RequestProperties(component);
        }
    }

    public void Cancel()
    {
        if (_tool.Cancel())
        {
            Changed?.Invoke();

            return;
        }

        if (_tool.Kind == EditorToolKind.Select)
        {
            Changed?.Invoke();

            return;
        }

        ChooseSelect();
    }

    public void Rotate() => _tool.Rotate();

    public void Mirror() => _tool.Mirror();

    public void DeleteSelection()
    {
        if (_selection.Component is { } component)
        {
            Run(new RemoveComponent(component));
        }
        else if (_selection.Wire is { } wire)
        {
            Run(new RemoveWire(wire));
        }
    }

    public void Undo()
    {
        if (_history.Undo())
        {
            AfterHistoryChanged();
        }
    }

    public void Redo()
    {
        if (_history.Redo())
        {
            AfterHistoryChanged();
        }
    }

    public CommandResult Run(IEditCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var result = _history.Execute(command);

        AfterHistoryChanged();

        return result;
    }

    internal SchematicHit? Pick(GridVector point) =>
        SchematicHitTester.Nearest(Schematic, Catalog, _symbols, point, _hitTolerance);

    internal void RequestProperties(ComponentId id) => PropertiesRequested?.Invoke(id);

    internal void Select(Selection selection)
    {
        if (selection == _selection)
        {
            return;
        }

        _selection = selection;
        Changed?.Invoke();
    }

    private void Replace(IEditorTool tool)
    {
        _tool = tool;
        _selection = Selection.None;

        Changed?.Invoke();
    }

    private void AfterHistoryChanged()
    {
        PruneSelection();
        Changed?.Invoke();
    }

    private void PruneSelection()
    {
        if (_selection.Component is { } component
            && !Schematic.TryGetComponent(component, out _))
        {
            _selection = Selection.None;
        }
        else if (_selection.Wire is { } wire && !Schematic.TryGetWire(wire, out _))
        {
            _selection = Selection.None;
        }
    }
}
