using Godot;
using Nodal.Circuit.Connectivity;
using Nodal.Circuit.Netlists;
using Nodal.Content.Components;
using Nodal.Content.Symbols;
using Nodal.Foundation;
using Nodal.Game.Adapters;
using Nodal.Game.Rendering;

namespace Nodal.Game.Presentation;

public partial class MainScreen : Control
{
    private const string CanvasPath = "SchematicCanvas";
    private const string StatusBarPath = "StatusBar";
    private const string StatusLabelPath = "StatusBar/StatusLabel";
    private const string ComponentsDirectoryPath = "res://content/components";
    private const string SymbolsDirectoryPath = "res://content/symbols";
    private const string FontColorOverride = "font_color";
    private const string StatusSeparator = "    ";

    private Label? _statusLabel;
    private int _netCount;
    private int _warningCount;

    public override void _Ready()
    {
        var catalog = LoadCatalog();
        var symbols = LoadSymbols(catalog);
        var history = DemoSchematic.Build(catalog);
        var connectivity = ConnectivityAnalyzer.Analyze(history.Schematic, catalog);

        GD.Print(NetlistCompiler.Compile(history.Schematic, catalog).ToDebugText());

        _netCount = connectivity.Nets.Length;
        _warningCount = connectivity.Diagnostics.Count(
            diagnostic => diagnostic.Severity == ConnectivitySeverity.Warning);

        GetNode<ColorRect>(StatusBarPath).Color = VisualTokens.Background;

        _statusLabel = GetNode<Label>(StatusLabelPath);
        _statusLabel.AddThemeColorOverride(FontColorOverride, VisualTokens.LabelText);

        var canvas = GetNode<SchematicCanvas>(CanvasPath);
        canvas.HoveredNetChanged += ShowStatus;
        canvas.Display(history, symbols, connectivity);

        ShowStatus(null);
    }

    private static ComponentCatalog LoadCatalog()
    {
        var result = ComponentCatalogLoader.Load(new GodotContentSource(ComponentsDirectoryPath));

        GD.Print(
            $"Loaded {result.Catalog.Definitions.Count} component definitions, "
            + $"{result.Diagnostics.Count} diagnostics");

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

        GD.Print($"Loaded {result.Library.Count} symbols, {result.Diagnostics.Count} diagnostics");

        foreach (var diagnostic in result.Diagnostics)
        {
            GD.Print(diagnostic);
        }

        return result.Library;
    }

    private void ShowStatus(Net? hoveredNet)
    {
        if (_statusLabel is null)
        {
            return;
        }

        var text = $"{NodalInfo.Name} {NodalInfo.Version}{StatusSeparator}"
            + $"{Counted(_netCount, "net")}{StatusSeparator}{Counted(_warningCount, "warning")}";

        _statusLabel.Text = hoveredNet is null
            ? text
            : text + StatusSeparator + "net " + hoveredNet.Name;
    }

    private static string Counted(int value, string noun) =>
        value == 1 ? $"1 {noun}" : $"{value} {noun}s";
}
