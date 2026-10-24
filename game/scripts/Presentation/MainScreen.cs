using System.Collections.Immutable;
using Godot;
using Nodal.Circuit.Commands;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Editing;
using Nodal.Circuit.Netlists;
using Nodal.Circuit.Persistence;
using Nodal.Circuit.Schematics;
using Nodal.Content.Components;
using Nodal.Content.Symbols;
using Nodal.Editor;
using Nodal.Editor.Simulation;
using Nodal.Foundation;
using Nodal.Game.Adapters;
using Nodal.Game.Input;
using Nodal.Game.Rendering;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Diagnostics;

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
    private const string DiscardTitle = "Unsaved changes";
    private const string NewQuestion = "The schematic has unsaved changes. Start a new one?";
    private const string OpenQuestion = "The schematic has unsaved changes. Open another one?";
    private const string QuitQuestion = "The schematic has unsaved changes. Quit?";
    private const string DiscardLabel = "Discard";
    private const string SaveLabel = "Save";
    private const string SaveAction = "save";
    private const string OpenDialogTitle = "Open a schematic";
    private const string SaveDialogTitle = "Save the schematic";
    private const string SchematicFilterLabel = "Nodal schematics";
    private const string OpenFailedTitle = "The file could not be opened";
    private const string SaveFailedTitle = "The file could not be saved";
    private const string RepairedTitle = "The file was repaired";
    private const string DcRefusedTitle = "The DC analysis was refused";
    private const string RatingWarningsTitle = "Rating warnings";
    private const string DcSolvedText = "DC: solved";
    private const string DcNotRunText = "DC: not run";
    private const string MoreSuffix = " more";
    private const int MaximumListedLines = 20;

    private readonly ConfirmationDialog _confirm = new();
    private readonly AcceptDialog _about = new();
    private readonly AcceptDialog _shortcuts = new();
    private readonly AcceptDialog _message = new();
    private readonly PropertiesDialog _properties = new();
    private readonly SchematicDocument _document = new();

    private ComponentCatalog _catalog = ComponentCatalog.Empty;
    private SymbolLibrary _symbols = SymbolLibrary.Empty;
    private SchematicCanvas? _canvas;
    private EditorMenus? _menus;
    private Label? _statusLabel;
    private FileDialog? _openDialog;
    private FileDialog? _saveDialog;
    private EditHistory? _history;
    private EditorSession? _session;
    private OperatingPoint? _operatingPoint;
    private Schematic? _solvedSchematic;
    private Net? _hoveredNet;
    private ComponentId? _hoveredComponent;
    private Action? _confirmed;
    private Action? _afterSave;
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
        _canvas.HoveredComponentChanged += OnHoveredComponentChanged;
        _canvas.ViewChanged += ShowStatus;

        GetTree().AutoAcceptQuit = false;

        BuildDialogs();

        _menus = new EditorMenus(
            GetNode<MenuBar>(MenuBarPath), CurrentHistory, _catalog, HasComponentSelected);
        _menus.CommandChosen += OnCommand;
        _menus.PlacementChosen += OnPlacement;

        Start(DemoSchematic.Build(_catalog));
        CurrentHistory().MarkSaved();

        var netlist = NetlistCompiler.Compile(CurrentHistory().Schematic, _catalog);

        GD.Print(netlist.ToDebugText());
        GD.Print(DcAnalysis.Solve(netlist, _catalog).ToDebugText());
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            OnCommand(EditorCommand.FileQuit);
        }
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
        _session.PropertiesRequested += OnPropertiesRequested;

        ClearOperatingPoint();

        _canvas?.Display(_session, _symbols, Analyse(history));
        _menus?.Refresh();

        ShowStatus();
        ShowTitle();
    }

    private void Load(Schematic schematic, string? path)
    {
        if (path is null)
        {
            _document.Reset();
        }
        else
        {
            _document.Track(path);
        }

        Start(new EditHistory(schematic, _catalog));
    }

    private ConnectivityResult Analyse(EditHistory history)
    {
        var connectivity = ConnectivityAnalyzer.Analyze(history.Schematic, _catalog);

        _netCount = connectivity.Nets.Length;
        _warningCount = connectivity.Diagnostics.Count(
            diagnostic => diagnostic.Severity == ConnectivitySeverity.Warning);

        return connectivity;
    }

    private void OnSchematicChanged()
    {
        _canvas?.SetConnectivity(Analyse(CurrentHistory()));

        if (_operatingPoint is not null
            && !ReferenceEquals(CurrentHistory().Schematic, _solvedSchematic))
        {
            ClearOperatingPoint();
        }

        ShowTitle();
    }

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

    private void OnHoveredComponentChanged(ComponentId? component)
    {
        _hoveredComponent = component;

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
            case EditorAction.Open:
                OnCommand(EditorCommand.FileOpen);
                break;

            case EditorAction.Save:
                OnCommand(EditorCommand.FileSave);
                break;

            case EditorAction.SaveAs:
                OnCommand(EditorCommand.FileSaveAs);
                break;

            case EditorAction.Properties:
                OnCommand(EditorCommand.EditProperties);
                break;

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

            case EditorAction.DcOperatingPoint:
                OnCommand(EditorCommand.SimulateDcOperatingPoint);
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
                Confirm(NewQuestion, () => Load(Schematic.Empty, null));
                break;

            case EditorCommand.FileOpen:
                Confirm(OpenQuestion, ShowOpenDialog);
                break;

            case EditorCommand.FileSave:
                SaveThen(null);
                break;

            case EditorCommand.FileSaveAs:
                _afterSave = null;
                ShowSaveDialog();
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

            case EditorCommand.EditProperties:
                _session?.RequestPropertiesForSelection();
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

            case EditorCommand.SimulateDcOperatingPoint:
                RunDcOperatingPoint();
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

    private void RunDcOperatingPoint()
    {
        if (_session is null)
        {
            return;
        }

        var schematic = CurrentHistory().Schematic;
        var solution = DcAnalysis.Solve(
            NetlistCompiler.Compile(schematic, _catalog), _catalog);

        if (!solution.IsSolved)
        {
            ClearOperatingPoint();
            ShowMessage(DcRefusedTitle, Listed(solution.Errors));

            return;
        }

        _operatingPoint = solution;
        _solvedSchematic = schematic;
        _canvas?.ShowOperatingPoint(solution);

        ShowStatus();

        if (solution.RatingWarnings.Length > 0)
        {
            ShowMessage(RatingWarningsTitle, Listed(solution.RatingWarnings));
        }
    }

    private void ClearOperatingPoint()
    {
        _operatingPoint = null;
        _solvedSchematic = null;

        _canvas?.ClearOperatingPoint();
        ShowStatus();
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
        if (!CurrentHistory().IsDirty)
        {
            accepted();

            return;
        }

        _confirmed = accepted;
        _confirm.DialogText = question;

        _confirm.PopupCentered();
    }

    private void ShowOpenDialog()
    {
        if (_openDialog is null)
        {
            return;
        }

        _openDialog.CurrentDir = SchematicFiles.DefaultDirectory();
        _openDialog.PopupCentered();
    }

    private void ShowSaveDialog()
    {
        if (_saveDialog is null)
        {
            return;
        }

        _saveDialog.CurrentDir = SchematicFiles.DefaultDirectory();
        _saveDialog.CurrentFile = _document.IsUntitled ? string.Empty : _document.Name;
        _saveDialog.PopupCentered();
    }

    private void SaveThen(Action? next)
    {
        _afterSave = next;

        if (_document.FilePath is { } path)
        {
            WriteTo(path);

            return;
        }

        ShowSaveDialog();
    }

    private void OnSavePathChosen(string path) => WriteTo(SchematicFiles.WithExtension(path));

    private void OnSaveDialogCanceled() => _afterSave = null;

    private void WriteTo(string path)
    {
        var text = SchematicFile.Write(CurrentHistory().Schematic);

        if (!SchematicFiles.TryWrite(path, text, out var error))
        {
            _afterSave = null;

            ShowMessage(SaveFailedTitle, error);

            return;
        }

        _document.Track(path);
        CurrentHistory().MarkSaved();
        ShowTitle();

        var next = _afterSave;
        _afterSave = null;

        next?.Invoke();
    }

    private void OnOpenPathChosen(string path)
    {
        if (!SchematicFiles.TryRead(path, out var text, out var error))
        {
            ShowMessage(OpenFailedTitle, error);

            return;
        }

        var result = SchematicFile.Read(text, _catalog);

        if (!result.Loaded)
        {
            ShowMessage(OpenFailedTitle, Listed(result.Errors));

            return;
        }

        Load(result.Schematic, path);

        if (result.Warnings.Length > 0)
        {
            ShowMessage(RepairedTitle, Listed(result.Warnings));
        }
    }

    private void ShowMessage(string title, string text)
    {
        _message.Title = title;
        _message.DialogText = text;

        _message.PopupCentered();
    }

    private static string Listed(ImmutableArray<SchematicDiagnostic> diagnostics) =>
        Capped(diagnostics.Select(
            diagnostic => $"{diagnostic.Location}: {diagnostic.Message}"));

    private static string Listed(ImmutableArray<SimulationDiagnostic> diagnostics) =>
        Capped(diagnostics.Select(diagnostic => diagnostic.Message));

    private static string Capped(IEnumerable<string> messages)
    {
        var all = messages.ToList();
        var lines = all.Take(MaximumListedLines).ToList();

        if (all.Count > MaximumListedLines)
        {
            lines.Add($"and {all.Count - MaximumListedLines}{MoreSuffix}");
        }

        return string.Join("\n", lines);
    }

    private void ShowTitle() =>
        DisplayServer.WindowSetTitle(_document.Title(_history?.IsDirty ?? false));

    private void BuildDialogs()
    {
        _confirm.Title = DiscardTitle;
        _confirm.OkButtonText = DiscardLabel;
        _confirm.AddButton(SaveLabel, right: false, SaveAction);
        _confirm.Confirmed += OnConfirmed;
        _confirm.Canceled += OnConfirmCanceled;
        _confirm.CustomAction += OnConfirmCustomAction;
        AddChild(_confirm);

        _openDialog = BuildFileDialog(FileDialog.FileModeEnum.OpenFile, OpenDialogTitle);
        _openDialog.FileSelected += OnOpenPathChosen;

        _saveDialog = BuildFileDialog(FileDialog.FileModeEnum.SaveFile, SaveDialogTitle);
        _saveDialog.FileSelected += OnSavePathChosen;
        _saveDialog.Canceled += OnSaveDialogCanceled;

        AddChild(_message);

        _about.Title = AboutTitle;
        _about.DialogText = $"{NodalInfo.Name} {NodalInfo.Version}{AboutSuffix}";
        AddChild(_about);

        _shortcuts.Title = ShortcutsTitle;
        _shortcuts.DialogText = ShortcutsText();
        AddChild(_shortcuts);

        _properties.Applied += OnPropertiesApplied;
        AddChild(_properties);
    }

    private bool HasComponentSelected() => _session?.Selection.Component is not null;

    private void OnPropertiesRequested(ComponentId id)
    {
        if (_session is null
            || !_session.Schematic.TryGetComponent(id, out var component)
            || !_catalog.TryGetById(component.DefinitionId, out var definition))
        {
            return;
        }

        _properties.Open(component, definition);
    }

    private void OnPropertiesApplied(IEditCommand command)
    {
        _session?.Run(command);

        OnSessionChanged();
    }

    private void OnConfirmed()
    {
        var accepted = _confirmed;
        _confirmed = null;

        accepted?.Invoke();
    }

    private void OnConfirmCanceled() => _confirmed = null;

    private void OnConfirmCustomAction(StringName action)
    {
        if (action != SaveAction)
        {
            return;
        }

        var accepted = _confirmed;
        _confirmed = null;

        _confirm.Hide();

        SaveThen(accepted);
    }

    private FileDialog BuildFileDialog(FileDialog.FileModeEnum mode, string title)
    {
        var dialog = new FileDialog
        {
            Title = title,
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = mode,
            UseNativeDialog = true,
        };

        dialog.AddFilter("*" + SchematicFile.Extension, SchematicFilterLabel);
        AddChild(dialog);

        return dialog;
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
            + $"{StatusSeparator}{(_operatingPoint is null ? DcNotRunText : DcSolvedText)}"
            + $"{StatusSeparator}{view.ToolLabel}{StatusSeparator}{view.Cursor}";

        if (_hoveredNet is { } net)
        {
            text += StatusSeparator + "net " + net.Name;
        }

        if (HoveredDescription() is { } description)
        {
            text += StatusSeparator + description;
        }

        _statusLabel.Text = text;
    }

    private string? HoveredDescription() =>
        _operatingPoint is not null && _hoveredComponent is { } component
            ? OperatingPointOverlay.Describe(_operatingPoint, component)
            : null;

    private static string Counted(int value, string noun) =>
        value == 1 ? $"1 {noun}" : $"{value} {noun}s";
}
