using System.IO;
using System.Text.Json;

namespace mpm_GUI.Services;

/// <summary>本地设置存储（%AppData%\mpm_GUI\settings.json）。</summary>
public sealed class SettingsStore
{
    private readonly string _dir;
    private readonly string _file;

    public SettingsStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _dir = Path.Combine(appData, "mpm_GUI");
        _file = Path.Combine(_dir, "settings.json");
    }

    public string MpmPath { get; set; } = string.Empty;
    public string LastRootPath { get; set; } = string.Empty;

    public void Load()
    {
        try
        {
            if (!File.Exists(_file)) return;
            var data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(_file));
            if (data == null) return;
            MpmPath = data.MpmPath ?? string.Empty;
            LastRootPath = data.LastRootPath ?? string.Empty;
        }
        catch
        {
            // 忽略损坏的配置
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_dir);
            var data = new SettingsData { MpmPath = MpmPath, LastRootPath = LastRootPath };
            File.WriteAllText(_file, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // 写失败不阻塞运行
        }
    }

    private sealed class SettingsData
    {
        public string? MpmPath { get; set; }
        public string? LastRootPath { get; set; }
    }
}
