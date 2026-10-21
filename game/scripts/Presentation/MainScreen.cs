using Godot;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Editing;
using Nodal.Circuit.Netlists;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Content.Symbols;
using Nodal.Editor;
using Nodal.Foundation;
using Nodal.Game.Adapters;
using Nodal.Game.Input;
using Nodal.Game.Rendering;

namespace Nodal.Game.Presentation;

public partial class MainScreen : Control
{
    private const string MenuBarPath = "Layout/MenuBar";
    private const string CanvasPath = "Layout/SchematicCanvas";
    private const string StatusBarPath = "Layout/StatusBar";
    private const string StatusLabelPath = "Layout/StatusBar/StatusLabel";
    private const string ComponentsDirectoryPath = "res://content/components";
    private const string SymbolsDirectoryPath = "res://content/symbols";
    private const string FontColorOverride = "font_color";
    private const string StatusSeparator = "    ";
    private const string AboutTitle = "About";
    private const string AboutSuffix = " — development build";
    private const string ShortcutsTitle = "Keyboard shortcuts";
    private const string DiscardTitle = "Discard the schematic?";
    private const string NewQuestion = "The schematic has unsaved changes. Start a new one?";
    private const string QuitQuestion = "The schematic has unsaved changes. Quit?";

    private readonly ConfirmationDialog _confirm = new();
    private readonly AcceptDialog _about = new();
    private readonly AcceptDialog _shortcuts = new();

    private ComponentCatalog _catalog = ComponentCatalog.Empty;
    private SymbolLibrary _symbols = SymbolLibrary.Empty;
    private SchematicCanvas? _canvas;
    private EditorMenus? _menus;
    private Label? _statusLabel;
    private EditHistory? _history;
    private EditorSession? _session;
    private Net? _hoveredNet;
    private Action? _confirmed;
    private int _netCount;
    private int _warningCount;
    private bool _panModifierHeld;

    public override void _Ready()
    {
        _catalog = LoadCatalog();
        _symbols = LoadSymbols(_catalog);

        EditorShortcuts.Register();

        GetNode<ColorRect>(StatusBarPath).Color = VisualTokens.Background;

        _statusLabel = GetNode<Label>(StatusLabelPath);
        _statusLabel.AddThemeColorOverride(FontColorOverride, VisualTokens.LabelText);

        _canvas = GetNode<SchematicCanvas>(CanvasPath);
        _canvas.HoveredNetChanged += OnHoveredNetChanged;
        _canvas.ViewChanged += ShowStatus;

        BuildDialogs();

        _menus = new EditorMenus(GetNode<MenuBar>(MenuBarPath), CurrentHistory, _catalog);
        _menus.CommandChosen += OnCommand;
        _menus.PlacementChosen += OnPlacement;

        Start(DemoSchematic.Build(_catalog));

        GD.Print(NetlistCompiler.Compile(CurrentHistory().Schematic, _catalog).ToDebugText());
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key)
        {
            return;
        }

        if (key.Keycode == Key.Space)
        {
            HoldPanModifier(key.Pressed);

            return;
        }

        if (!key.Pressed || key.Echo)
        {
            return;
        }

        if (_panModifierHeld && new ShortcutStroke(key.Keycode, key.CtrlPressed, key.ShiftPressed)
            .IsUnmodifiedLetter)
        {
            return;
        }

        foreach (var binding in EditorShortcuts.All)
        {
            if (key.IsActionPressed(binding.Action, exactMatch: true))
            {
                Dispatch(binding);
                AcceptEvent();

                return;
            }
        }
    }

    private EditHistory CurrentHistory() =>
        _history ?? throw new InvalidOperationException("The editor session is not started.");

    private void Start(EditHistory history)
    {
        if (_history is not null)
        {
            _history.Changed -= OnSchematicChanged;
        }

        _history = history;
        _history.Changed += OnSchematicChanged;

        _session = new EditorSession(history, _symbols);
        _session.Changed += OnSessionChanged;

        _canvas?.Display(_session, _symbols, Analyse(history));
        _menus?.Refresh();

        ShowStatus();
    }

    private ConnectivityResult Analyse(EditHistory history)
    {
        var connectivity = ConnectivityAnalyzer.Analyze(history.Schematic, _catalog);

        _netCount = connectivity.Nets.Length;
        _warningCount = connectivity.Diagnostics.Count(
            diagnostic => diagnostic.Severity == ConnectivitySeverity.Warning);

        return connectivity;
    }

    private void OnSchematicChanged() => _canvas?.SetConnectivity(Analyse(CurrentHistory()));

    private void OnSessionChanged()
    {
        _menus?.Refresh();
        _canvas?.QueueRedraw();

        ShowStatus();
    }

    private void OnHoveredNetChanged(Net? net)
    {
        _hoveredNet = net;

        ShowStatus();
    }

    private void HoldPanModifier(bool held)
    {
        _panModifierHeld = held;

        if (_canvas is null)
        {
            return;
        }

        _canvas.PanModifierHeld = held;

        if (!held)
        {
            _canvas.PanModifierReleased();
        }
    }

    private void Dispatch(ShortcutBinding binding)
    {
        if (binding.PlacesDefinitionId is { } definitionId)
        {
            OnPlacement(definitionId);

            return;
        }

        switch (binding.Action)
        {
            case EditorAction.WireTool:
                OnCommand(EditorCommand.EditWireTool);
                break;

            case EditorAction.Delete:
                OnCommand(EditorCommand.EditDelete);
                break;

            case EditorAction.Rotate:
                OnCommand(EditorCommand.EditRotate);
                break;

            case EditorAction.Mirror:
                OnCommand(EditorCommand.EditMirror);
                break;

            case EditorAction.Undo:
                OnCommand(EditorCommand.EditUndo);
                break;

            case EditorAction.Redo:
                OnCommand(EditorCommand.EditRedo);
                break;

            case EditorAction.Fit:
                OnCommand(EditorCommand.ViewFit);
                break;

            case EditorAction.ZoomIn:
                OnCommand(EditorCommand.ViewZoomIn);
                break;

            case EditorAction.ZoomOut:
                OnCommand(EditorCommand.ViewZoomOut);
                break;

            case EditorAction.Cancel:
                _session?.Cancel();
                OnSessionChanged();
                break;
        }
    }

    private void OnPlacement(string definitionId)
    {
        _session?.ChoosePlace(definitionId);

        OnSessionChanged();
    }

    private void OnCommand(EditorCommand command)
    {
        switch (command)
        {
            case EditorCommand.FileNew:
                Confirm(NewQuestion, () => Start(new EditHistory(Schematic.Empty, _catalog)));
                break;

            case EditorCommand.FileQuit:
                Confirm(QuitQuestion, () => GetTree().Quit());
                break;

            case EditorCommand.EditUndo:
                _session?.Undo();
                break;

            case EditorCommand.EditRedo:
                _session?.Redo();
                break;

            case EditorCommand.EditDelete:
                _session?.DeleteSelection();
                break;

            case EditorCommand.EditRotate:
                _session?.Rotate();
                break;

            case EditorCommand.EditMirror:
                _session?.Mirror();
                break;

            case EditorCommand.EditWireTool:
                _session?.ChooseWire();
                break;

            case EditorCommand.ViewZoomIn:
                _canvas?.ZoomAtCentre(CanvasMetrics.ZoomStep);
                break;

            case EditorCommand.ViewZoomOut:
                _canvas?.ZoomAtCentre(1f / CanvasMetrics.ZoomStep);
                break;

            case EditorCommand.ViewFit:
                _canvas?.FitToSchematic();
                break;

            case EditorCommand.ViewShowGrid:
                ToggleGrid();
                break;

            case EditorCommand.HelpShortcuts:
                _shortcuts.PopupCentered();
                break;

            case EditorCommand.HelpAbout:
                _about.PopupCentered();
                break;
        }

        OnSessionChanged();
    }

    private void ToggleGrid()
    {
        if (_menus is null || _canvas is null)
        {
            return;
        }

        _menus.ShowGrid = !_menus.ShowGrid;
        _canvas.ShowGrid = _menus.ShowGrid;
    }

    private void Confirm(string question, Action accepted)
    {
        if (!CurrentHistory().CanUndo)
        {
            accepted();

            return;
        }

        _confirmed = accepted;
        _confirm.DialogText = question;

        _confirm.PopupCentered();
    }

    private void BuildDialogs()
    {
        _confirm.Title = DiscardTitle;
        _confirm.Confirmed += OnConfirmed;
        AddChild(_confirm);

        _about.Title = AboutTitle;
        _about.DialogText = $"{NodalInfo.Name} {NodalInfo.Version}{AboutSuffix}";
        AddChild(_about);

        _shortcuts.Title = ShortcutsTitle;
        _shortcuts.DialogText = ShortcutsText();
        AddChild(_shortcuts);
    }

    private void OnConfirmed()
    {
        var accepted = _confirmed;
        _confirmed = null;

        accepted?.Invoke();
    }

    private static string ShortcutsText()
    {
        var width = EditorShortcuts.All.Max(binding => binding.Label.Length);

        return string.Join(
            "\n",
            EditorShortcuts.All.Select(
                binding => binding.Label.PadRight(width) + "   " + EditorShortcuts.Describe(binding)));
    }

    private static ComponentCatalog LoadCatalog()
    {
        var result = ComponentCatalogLoader.Load(new GodotContentSource(ComponentsDirectoryPath));

        GD.Print(
            $"Loaded {result.Catalog.Definitions.Length} component definitions, "
            + $"{result.Diagnostics.Length} diagnostics");

        foreach (var diagnostic in result.Diagnostics)
        {
            GD.Print(diagnostic);
        }

        return result.Catalog;
    }

    private static SymbolLibrary LoadSymbols(ComponentCatalog catalog)
    {
        var result = SymbolLibraryLoader.Load(
            new GodotContentSource(SymbolsDirectoryPath), catalog);

        GD.Print($"Loaded {result.Library.Count} symbols, {result.Diagnostics.Length} diagnostics");

        foreach (var diagnostic in result.Diagnostics)
        {
            GD.Print(diagnostic);
        }

        return result.Library;
    }

    private void ShowStatus()
    {
        if (_statusLabel is null || _session is null)
        {
            return;
        }

        var view = _session.View;

        var text = $"{NodalInfo.Name} {NodalInfo.Version}{StatusSeparator}"
            + $"{Counted(_netCount, "net")}{StatusSeparator}{Counted(_warningCount, "warning")}"
            + $"{StatusSeparator}{view.ToolLabel}{StatusSeparator}{view.Cursor}";

        _statusLabel.Text = _hoveredNet is null
            ? text
            : text + StatusSeparator + "net " + _hoveredNet.Name;
    }

    private static string Counted(int value, string noun) =>
        value == 1 ? $"1 {noun}" : $"{value} {noun}s";
}
