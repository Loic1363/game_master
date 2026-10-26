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
using Nodal.Editor.Waveforms;
using Nodal.Foundation;
using Nodal.Foundation.Quantities;
using Nodal.Game.Adapters;
using Nodal.Game.Input;
using Nodal.Game.Rendering;
using Nodal.Game.Style;
using Nodal.Simulation.Dc;
using Nodal.Simulation.Diagnostics;
using Nodal.Simulation.Spice;
using Nodal.Simulation.Transient;

namespace Nodal.Game.Presentation;

public partial class MainScreen : Control
{
    private const string MenuBarPath = "Layout/MenuBar";
    private const string SplitPath = "Layout/Split";
    private const string CanvasPath = "Layout/Split/SchematicCanvas";
    private const string WaveformPanelPath = "Layout/Split/WaveformPanel";
    private const string StatusBarPath = "Layout/StatusBar";
    private const string StatusLabelPath = "Layout/StatusBar/StatusLabel";
    private const string ComponentsDirectoryPath = "res://content/components";
    private const string SymbolsDirectoryPath = "res://content/symbols";
    private const string StatusSeparator = "    ";
    private const string NetPrefix = "net ";
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
    private const string SpiceDialogTitle = "Export a SPICE netlist";
    private const string SpiceFilterLabel = "SPICE netlists";
    private const string SpiceRefusedTitle = "The SPICE export was refused";
    private const string ExportFailedTitle = "The netlist could not be exported";
    private const string OpenFailedTitle = "The file could not be opened";
    private const string SaveFailedTitle = "The file could not be saved";
    private const string RepairedTitle = "The file was repaired";
    private const string DcRefusedTitle = "The DC analysis was refused";
    private const string TransientRefusedTitle = "The transient analysis was refused";
    private const string RatingWarningsTitle = "Rating warnings";
    private const string MoreSuffix = " more";
    private const int MaximumListedLines = 20;
    private const string DemoProbeNet = "N2";
    private const double DemoStopTime = 500e-6;
    private const double DemoProbeTime = 100e-6;

    private readonly ConfirmationDialog _confirm = new();
    private readonly AcceptDialog _about = new();
    private readonly AcceptDialog _shortcuts = new();
    private readonly AcceptDialog _message = new();
    private readonly PropertiesDialog _properties = new();
    private readonly TransientDialog _transient = new();
    private readonly SchematicDocument _document = new();
    private readonly UserPreferences _preferences = new();

    private ComponentCatalog _catalog = ComponentCatalog.Empty;
    private SymbolLibrary _symbols = SymbolLibrary.Empty;
    private SchematicCanvas? _canvas;
    private WaveformPanel? _panel;
    private VSplitContainer? _split;
    private EditorMenus? _menus;
    private Label? _statusLabel;
    private FileDialog? _openDialog;
    private FileDialog? _saveDialog;
    private FileDialog? _spiceDialog;
    private EditHistory? _history;
    private EditorSession? _session;
    private ConnectivityResult? _connectivity;
    private OperatingPoint? _operatingPoint;
    private TransientResult? _transientResult;
    private TransientRun? _transientRun;
    private SolutionSnapshot? _solution;
    private Schematic? _solvedSchematic;
    private Net? _hoveredNet;
    private ComponentId? _hoveredComponent;
    private string? _inlineError;
    private Action? _confirmed;
    private Action? _afterSave;
    private int _netCount;
    private int _warningCount;
    private int _panelHeight;
    private bool _panModifierHeld;

    public override void _Ready()
    {
        _catalog = LoadCatalog();
        _symbols = LoadSymbols(_catalog);

        EditorShortcuts.Register();

        _statusLabel = GetNode<Label>(StatusLabelPath);

        StatusBarStyle.Apply(GetNode<ColorRect>(StatusBarPath), _statusLabel);

        _canvas = GetNode<SchematicCanvas>(CanvasPath);
        _canvas.HoveredNetChanged += OnHoveredNetChanged;
        _canvas.HoveredComponentChanged += OnHoveredComponentChanged;
        _canvas.ViewChanged += ShowStatus;
        _canvas.ValueEdited += OnValueEdited;
        _canvas.InlineErrorChanged += OnInlineErrorChanged;

        BuildWaveformPanel();

        GetTree().AutoAcceptQuit = false;

        BuildDialogs();

        _menus = new EditorMenus(
            GetNode<MenuBar>(MenuBarPath),
            CurrentHistory,
            _catalog,
            HasComponentSelected,
            IsSimulating);
        _menus.CommandChosen += OnCommand;
        _menus.PlacementChosen += OnPlacement;

        Start(DemoSchematic.Build(_catalog));
        CurrentHistory().MarkSaved();

        var netlist = NetlistCompiler.Compile(CurrentHistory().Schematic, _catalog);

        GD.Print(netlist.ToDebugText());
        GD.Print(DcAnalysis.Solve(netlist, _catalog).ToDebugText());

        PrintDemoTransient(netlist);
    }

    private void PrintDemoTransient(Netlist netlist)
    {
        var result = TransientAnalysis.Run(
            netlist,
            _catalog,
            new TransientOptions(DemoStopTime) { StartFromZero = true });

        GD.Print(result.ToDebugText());

        foreach (var time in new[] { DemoProbeTime, DemoStopTime })
        {
            var voltage = new Quantity(result.VoltageAt(DemoProbeNet, time), Unit.Volt);

            GD.Print($"V({DemoProbeNet}) at {new Quantity(time, Unit.Second)} = {voltage}");
        }
    }

    private void BuildWaveformPanel()
    {
        _split = GetNode<VSplitContainer>(SplitPath);
        _panel = GetNode<WaveformPanel>(WaveformPanelPath);
        _panelHeight = WaveformStyle.HeightOf(
            _preferences.WaveformPanelHeight(WaveformMetrics.DefaultPanelHeight));

        WaveformStyle.ApplySplit(_split);

        _split.DragEnded += SavePanelHeight;
        _panel.CursorTimeChanged += OnCursorTimeChanged;
        _panel.ProbeRemoved += OnProbeRemoved;
    }

    public override void _Process(double delta)
    {
        if (_transientRun is not null)
        {
            CollectTransient();
        }
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
        _session.RefusalChanged += OnInlineErrorChanged;

        ClearOperatingPoint();
        ClearPlot();

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
        _connectivity = connectivity;

        return connectivity;
    }

    private void OnSchematicChanged()
    {
        _canvas?.SetConnectivity(Analyse(CurrentHistory()));

        if (!ReferenceEquals(CurrentHistory().Schematic, _solvedSchematic))
        {
            _panel?.MarkStale();

            if (_solution is not null)
            {
                ClearOperatingPoint();
            }
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

            case EditorAction.ProbeTool:
                OnCommand(EditorCommand.EditProbeTool);
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

            case EditorAction.Transient:
                OnCommand(EditorCommand.SimulateTransient);
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
                if (_transientRun is null)
                {
                    _session?.Cancel();
                }
                else
                {
                    StopTransient();
                }

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

            case EditorCommand.FileExportSpice:
                ShowSpiceDialog();
                break;

            case EditorCommand.FileQuit:
                Confirm(QuitQuestion, Quit);
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

            case EditorCommand.EditProbeTool:
                _session?.ChooseProbe();
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

            case EditorCommand.ViewWaveformPanel:
                SetPanelVisible(_panel is not { Visible: true });
                break;

            case EditorCommand.SimulateDcOperatingPoint:
                RunDcOperatingPoint();
                break;

            case EditorCommand.SimulateTransient:
                _transient.Open(CurrentHistory().Schematic.Analysis);
                break;

            case EditorCommand.SimulateStop:
                StopTransient();
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
        _transientResult = null;
        _solvedSchematic = schematic;
        _solution = SolutionSnapshot.FromDc(solution);
        _canvas?.ShowSolution(_solution);

        ShowStatus();

        if (solution.RatingWarnings.Length > 0)
        {
            ShowMessage(RatingWarningsTitle, Listed(solution.RatingWarnings));
        }
    }

    private void ClearOperatingPoint()
    {
        _operatingPoint = null;
        _transientResult = null;
        _solution = null;
        _solvedSchematic = null;

        _canvas?.ClearSolution();
        ShowStatus();
    }

    private bool IsSimulating() => _transientRun is { IsRunning: true };

    private void OnTransientRequested(TransientSettings settings, IEditCommand? command)
    {
        if (_session is null)
        {
            return;
        }

        if (command is not null)
        {
            _session.Run(command);
        }

        StartTransient(settings);
        OnSessionChanged();
    }

    private void StartTransient(TransientSettings settings)
    {
        StopTransient();
        ClearOperatingPoint();

        _transientRun = TransientRun.Start(
            CurrentHistory().Schematic, _catalog, TransientOptions.From(settings));

        ShowStatus();
    }

    private void StopTransient()
    {
        _transientRun?.Cancel();
        _transientRun = null;

        ShowStatus();
    }

    private void CollectTransient()
    {
        if (_transientRun is not { } run)
        {
            return;
        }

        if (!run.TryTakeResult(out var result))
        {
            ShowStatus();

            return;
        }

        _transientRun = null;
        _menus?.Refresh();

        if (result is null || !ReferenceEquals(run.Schematic, CurrentHistory().Schematic))
        {
            ShowStatus();

            return;
        }

        if (!result.HasValues)
        {
            ShowMessage(TransientRefusedTitle, Listed(result.Errors));
            ShowStatus();

            return;
        }

        _transientResult = result;
        _solvedSchematic = run.Schematic;
        _solution = SolutionSnapshot.FromTransient(result, result.FinalTime);
        _canvas?.ShowSolution(_solution);

        ShowPlot(result);
        ShowStatus();
    }

    private void ShowPlot(TransientResult result)
    {
        if (_panel is null || _connectivity is not { } connectivity)
        {
            return;
        }

        _panel.ShowPlot(
            WaveformPlot.Of(
                result, connectivity, CurrentHistory().Schematic.Analysis.Probes));

        SetPanelVisible(true);
    }

    private void ClearPlot()
    {
        _panel?.Clear();

        SetPanelVisible(false);
    }

    private void SetPanelVisible(bool visible)
    {
        if (_panel is null)
        {
            return;
        }

        if (!visible && _panel.Visible)
        {
            SavePanelHeight();
        }

        _panel.Visible = visible;

        if (visible)
        {
            Callable.From(ApplyPanelHeight).CallDeferred();
        }

        if (_menus is not null)
        {
            _menus.ShowWaveformPanel = visible;
            _menus.Refresh();
        }
    }

    private void ApplyPanelHeight()
    {
        if (_split is { } split)
        {
            split.SplitOffsets = [WaveformStyle.SplitOffsetFor(_panelHeight)];
        }
    }

    private void SavePanelHeight()
    {
        if (_panel is null)
        {
            return;
        }

        _panelHeight = WaveformStyle.HeightOf((int)_panel.Size.Y);

        _preferences.SaveWaveformPanelHeight(_panelHeight);
    }

    private void OnCursorTimeChanged(double? time)
    {
        if (_transientResult is not { } result)
        {
            return;
        }

        _solution = SolutionSnapshot.FromTransient(result, time ?? result.FinalTime);

        _canvas?.ShowSolution(_solution);
        ShowStatus();
    }

    private void OnProbeRemoved(Probe probe)
    {
        _session?.Run(new RemoveProbe(probe));

        OnSessionChanged();
    }

    private void Quit()
    {
        if (_panel is { Visible: true })
        {
            SavePanelHeight();
        }

        GetTree().Quit();
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

    private void ShowSpiceDialog()
    {
        if (_spiceDialog is null)
        {
            return;
        }

        _spiceDialog.CurrentDir = SchematicFiles.DefaultDirectory();
        _spiceDialog.CurrentFile = _document.IsUntitled
            ? string.Empty
            : Path.ChangeExtension(_document.Name, SpiceExporter.Extension);

        _spiceDialog.PopupCentered();
    }

    private void OnSpicePathChosen(string path) =>
        ExportSpiceTo(SchematicFiles.WithExtension(path, SpiceExporter.Extension));

    private void ExportSpiceTo(string path)
    {
        var netlist = NetlistCompiler.Compile(CurrentHistory().Schematic, _catalog);

        if (!SpiceExporter.TryExport(netlist, _catalog, out var deck, out var refusals))
        {
            ShowMessage(SpiceRefusedTitle, Listed(refusals));

            return;
        }

        if (!SchematicFiles.TryWrite(path, deck, out var error))
        {
            ShowMessage(ExportFailedTitle, error);
        }
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

        _spiceDialog = BuildFileDialog(
            FileDialog.FileModeEnum.SaveFile,
            SpiceDialogTitle,
            SpiceExporter.Extension,
            SpiceFilterLabel);
        _spiceDialog.FileSelected += OnSpicePathChosen;

        AddChild(_message);

        _about.Title = AboutTitle;
        _about.DialogText = $"{NodalInfo.Name} {NodalInfo.Version}{AboutSuffix}";
        AddChild(_about);

        _shortcuts.Title = ShortcutsTitle;
        _shortcuts.DialogText = ShortcutsText();
        AddChild(_shortcuts);

        _properties.Applied += OnPropertiesApplied;
        AddChild(_properties);

        _transient.Requested += OnTransientRequested;
        AddChild(_transient);
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

    private void OnValueEdited(IEditCommand command)
    {
        _inlineError = null;
        _session?.Run(command);

        OnSessionChanged();
    }

    private void OnInlineErrorChanged(string? error)
    {
        _inlineError = error;

        ShowStatus();
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

    private FileDialog BuildFileDialog(FileDialog.FileModeEnum mode, string title) =>
        BuildFileDialog(mode, title, SchematicFile.Extension, SchematicFilterLabel);

    private FileDialog BuildFileDialog(
        FileDialog.FileModeEnum mode, string title, string extension, string filterLabel)
    {
        var dialog = new FileDialog
        {
            Title = title,
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = mode,
            UseNativeDialog = true,
        };

        dialog.AddFilter("*" + extension, filterLabel);
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
            + $"{StatusSeparator}{SimulationStatus()}"
            + $"{StatusSeparator}{view.ToolLabel}{StatusSeparator}{view.Cursor}";

        if (_hoveredNet is { } net)
        {
            text += StatusSeparator + NetPrefix + HoveredNetDescription(net);
        }

        if (HoveredDescription() is { } description)
        {
            text += StatusSeparator + description;
        }

        if (_inlineError is { } error)
        {
            text += StatusSeparator + error;
        }

        _statusLabel.Text = text;
    }

    private string SimulationStatus()
    {
        if (_transientRun is { } run)
        {
            return TransientStatusText.Running(run.Fraction);
        }

        return _transientResult is { } result
            ? TransientStatusText.Of(result)
            : DcStatusText.Of(_operatingPoint);
    }

    private string HoveredNetDescription(Net net) =>
        _solution is { } solution
        && OperatingPointOverlay.DescribeNet(solution, net.Name) is { } described
            ? described
            : net.Name;

    private string? HoveredDescription() =>
        _solution is { } solution && _hoveredComponent is { } component
            ? OperatingPointOverlay.Describe(solution, component)
            : null;

    private static string Counted(int value, string noun) =>
        value == 1 ? $"1 {noun}" : $"{value} {noun}s";
}
