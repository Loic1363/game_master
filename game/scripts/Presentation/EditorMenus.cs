using Godot;
using Nodal.Circuit.Editing;
using Nodal.Content.Components;
using Nodal.Game.Input;

namespace Nodal.Game.Presentation;

public sealed class EditorMenus
{
    private const int PlaceIdBase = 1000;
    private const string UndoLabel = "Undo";
    private const string RedoLabel = "Redo";
    private const string ShowGridLabel = "Show grid";
    private const string PropertiesLabel = "Properties…";

    private static readonly ComponentCategory[] PlaceCategories =
    [
        ComponentCategory.Passive,
        ComponentCategory.Semiconductor,
        ComponentCategory.Source,
        ComponentCategory.Reference,
    ];

    private readonly Func<EditHistory> _history;
    private readonly Func<bool> _hasComponentSelected;
    private readonly ComponentCatalog _catalog;
    private readonly PopupMenu _edit = new();
    private readonly PopupMenu _view = new();

    private int _undoIndex;
    private int _redoIndex;
    private int _propertiesIndex;
    private int _showGridIndex;

    public EditorMenus(
        MenuBar bar,
        Func<EditHistory> history,
        ComponentCatalog catalog,
        Func<bool> hasComponentSelected)
    {
        ArgumentNullException.ThrowIfNull(bar);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(hasComponentSelected);

        _history = history;
        _hasComponentSelected = hasComponentSelected;
        _catalog = catalog;

        BuildFile(bar);
        BuildEdit(bar);
        BuildView(bar);
        BuildPlace(bar);
        BuildSimulate(bar);
        BuildHelp(bar);
    }

    public event Action<EditorCommand>? CommandChosen;

    public event Action<string>? PlacementChosen;

    public bool ShowGrid { get; set; } = true;

    public void Refresh()
    {
        var history = _history();

        _edit.SetItemText(_undoIndex, Described(UndoLabel, history.UndoDescription));
        _edit.SetItemDisabled(_undoIndex, !history.CanUndo);
        _edit.SetItemText(_redoIndex, Described(RedoLabel, history.RedoDescription));
        _edit.SetItemDisabled(_redoIndex, !history.CanRedo);
        _edit.SetItemDisabled(_propertiesIndex, !_hasComponentSelected());
        _view.SetItemChecked(_showGridIndex, ShowGrid);
    }

    private static string Described(string label, string? description) =>
        description is null ? label : $"{label} {description}";

    private void BuildFile(MenuBar bar)
    {
        var menu = AddMenu(bar, "File");

        AddCommand(menu, "New", EditorCommand.FileNew, null);
        AddCommand(menu, "Open…", EditorCommand.FileOpen, EditorAction.Open);
        AddCommand(menu, "Save", EditorCommand.FileSave, EditorAction.Save);
        AddCommand(menu, "Save As…", EditorCommand.FileSaveAs, EditorAction.SaveAs);
        menu.AddSeparator();
        AddCommand(menu, "Quit", EditorCommand.FileQuit, null);
    }

    private void BuildEdit(MenuBar bar)
    {
        Attach(bar, _edit, "Edit");

        AddCommand(_edit, UndoLabel, EditorCommand.EditUndo, EditorAction.Undo);
        _undoIndex = _edit.ItemCount - 1;
        AddCommand(_edit, RedoLabel, EditorCommand.EditRedo, EditorAction.Redo);
        _redoIndex = _edit.ItemCount - 1;
        _edit.AddSeparator();
        AddCommand(_edit, "Delete", EditorCommand.EditDelete, EditorAction.Delete);
        AddCommand(_edit, "Rotate", EditorCommand.EditRotate, EditorAction.Rotate);
        AddCommand(_edit, "Mirror", EditorCommand.EditMirror, EditorAction.Mirror);
        _edit.AddSeparator();
        AddCommand(_edit, PropertiesLabel, EditorCommand.EditProperties, EditorAction.Properties);
        _propertiesIndex = _edit.ItemCount - 1;
        _edit.AddSeparator();
        AddCommand(_edit, "Wire tool", EditorCommand.EditWireTool, EditorAction.WireTool);
    }

    private void BuildView(MenuBar bar)
    {
        Attach(bar, _view, "View");

        AddCommand(_view, "Zoom in", EditorCommand.ViewZoomIn, EditorAction.ZoomIn);
        AddCommand(_view, "Zoom out", EditorCommand.ViewZoomOut, EditorAction.ZoomOut);
        AddCommand(_view, "Fit to schematic", EditorCommand.ViewFit, EditorAction.Fit);
        _view.AddSeparator();
        _view.AddCheckItem(ShowGridLabel, (int)EditorCommand.ViewShowGrid);
        _showGridIndex = _view.ItemCount - 1;
        _view.SetItemChecked(_showGridIndex, ShowGrid);
    }

    private void BuildPlace(MenuBar bar)
    {
        var menu = AddMenu(bar, "Place");

        foreach (var category in PlaceCategories)
        {
            var submenu = new PopupMenu { Name = "Place" + category };

            for (var index = 0; index < _catalog.Definitions.Length; index++)
            {
                var definition = _catalog.Definitions[index];

                if (definition.Category != category)
                {
                    continue;
                }

                submenu.AddItem(definition.Name, PlaceIdBase + index);
                ShowShortcut(submenu, EditorShortcuts.ForPlacement(definition.Id));
            }

            submenu.IdPressed += OnPlacementPressed;
            menu.AddSubmenuNodeItem(category.ToString(), submenu);
        }
    }

    private void BuildSimulate(MenuBar bar)
    {
        var menu = AddMenu(bar, "Simulate");

        AddCommand(
            menu,
            "DC operating point",
            EditorCommand.SimulateDcOperatingPoint,
            EditorAction.DcOperatingPoint);
    }

    private void BuildHelp(MenuBar bar)
    {
        var menu = AddMenu(bar, "Help");

        AddCommand(menu, "Keyboard shortcuts", EditorCommand.HelpShortcuts, null);
        AddCommand(menu, "About", EditorCommand.HelpAbout, null);
    }

    private PopupMenu AddMenu(MenuBar bar, string title)
    {
        var menu = new PopupMenu();

        Attach(bar, menu, title);

        return menu;
    }

    private void Attach(MenuBar bar, PopupMenu menu, string title)
    {
        menu.Name = title;
        menu.IdPressed += OnCommandPressed;
        bar.AddChild(menu);
    }

    private void AddCommand(PopupMenu menu, string label, EditorCommand command, string? action)
    {
        menu.AddItem(label, (int)command);

        if (action is not null)
        {
            ShowShortcut(menu, EditorShortcuts.ForAction(action));
        }
    }

    private static void ShowShortcut(PopupMenu menu, ShortcutBinding? binding)
    {
        if (binding is null)
        {
            return;
        }

        var index = menu.ItemCount - 1;

        menu.SetItemShortcut(index, binding.ToShortcut());
        menu.SetItemShortcutDisabled(index, true);
    }

    private void OnCommandPressed(long id) => CommandChosen?.Invoke((EditorCommand)id);

    private void OnPlacementPressed(long id)
    {
        var index = (int)id - PlaceIdBase;

        if (index >= 0 && index < _catalog.Definitions.Length)
        {
            PlacementChosen?.Invoke(_catalog.Definitions[index].Id);
        }
    }
}
