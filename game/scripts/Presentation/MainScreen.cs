using Godot;
using Nodal.Content.Components;
using Nodal.Foundation;
using Nodal.Game.Adapters;

namespace Nodal.Game.Presentation;

public partial class MainScreen : Control
{
    private const string TitleLabelPath = "TitleLabel";
    private const string ComponentsDirectoryPath = "res://content/components";

    public override void _Ready()
    {
        var titleLabel = GetNode<Label>(TitleLabelPath);
        titleLabel.Text = $"{NodalInfo.Name} — development build\nVersion {NodalInfo.Version}";
        GD.Print(titleLabel.Text);

        PrintComponentCatalog();
    }

    private static void PrintComponentCatalog()
    {
        var result = ComponentCatalogLoader.Load(new GodotContentSource(ComponentsDirectoryPath));

        GD.Print(
            $"Loaded {result.Catalog.Definitions.Count} component definitions, "
            + $"{result.Diagnostics.Count} diagnostics");

        foreach (var diagnostic in result.Diagnostics)
        {
            GD.Print(diagnostic);
        }
    }
}
