using Godot;

namespace Nodal.Game.Adapters;

public sealed class UserPreferences
{
    private const string FilePath = "user://preferences.cfg";
    private const string WaveformSection = "waveform";
    private const string PanelHeightKey = "panel_height";

    private readonly ConfigFile _file = new();

    public UserPreferences()
    {
        var error = _file.Load(FilePath);

        if (error is not Error.Ok and not Error.FileNotFound)
        {
            GD.PrintErr($"Could not read {FilePath}: {error}");
        }
    }

    public int WaveformPanelHeight(int fallback) =>
        (int)_file.GetValue(WaveformSection, PanelHeightKey, fallback);

    public void SaveWaveformPanelHeight(int height)
    {
        _file.SetValue(WaveformSection, PanelHeightKey, height);

        var error = _file.Save(FilePath);

        if (error != Error.Ok)
        {
            GD.PrintErr($"Could not write {FilePath}: {error}");
        }
    }
}
