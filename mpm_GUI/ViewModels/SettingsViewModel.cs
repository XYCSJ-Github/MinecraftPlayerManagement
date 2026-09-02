using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mpm_GUI.Services;

namespace mpm_GUI.ViewModels;

/// <summary>设置页：引擎路径、状态与运行日志。</summary>
public partial class SettingsViewModel : PageViewModel
{
    [ObservableProperty]
    private string _enginePath = string.Empty;

    [ObservableProperty]
    private string _engineStatus = "未连接";

    [ObservableProperty]
    private bool _isConnected;

    public ObservableCollection<string> Logs { get; } = new();

    public SettingsViewModel(MpmEngineService engine, SettingsStore settings, DialogService dialogs)
        : base(engine, settings, dialogs)
    {
        OnEngineState(engine.State);
    }

    public void Initialize(string configured, string? located)
    {
        EnginePath = !string.IsNullOrEmpty(configured) ? configured
            : located ?? string.Empty;
    }

    public void LoadSettingsIntoUi()
    {
        EnginePath = Settings.MpmPath;
    }

    internal void OnEngineState(EngineState state)
    {
        IsConnected = state == EngineState.Ready;
        EngineStatus = state switch
        {
            EngineState.Ready => "已就绪",
            EngineState.Connecting => "连接中...",
            EngineState.Error => "异常",
            _ => "未连接",
        };
    }

    public void AddLog(string line)
    {
        Logs.Add(line);
        while (Logs.Count > 400) Logs.RemoveAt(0);
    }

    [RelayCommand]
    private void BrowseEngine()
    {
        string? path = Dialogs.PickEngine();
        if (!string.IsNullOrEmpty(path)) EnginePath = path;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        if (string.IsNullOrWhiteSpace(EnginePath) || !File.Exists(EnginePath))
        {
            Notifier.Show("请选择有效的 mpm.exe 路径", true);
            return;
        }
        Settings.MpmPath = EnginePath.Trim();
        Settings.Save();
        Notifier.Show("设置已保存");
    }

    [RelayCommand]
    private async Task StartEngineAsync()
    {
        await RunBusyAsync(async () =>
        {
            string path = Settings.MpmPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                string? found = EngineLocator.Find(Settings);
                if (string.IsNullOrEmpty(found)) found = Dialogs.PickEngine();
                if (string.IsNullOrEmpty(found)) return;
                path = found;
                Settings.MpmPath = path;
                Settings.Save();
                EnginePath = path;
            }

            if (!await Engine.StartAsync(path))
                Notifier.Show("启动失败，请检查 mpm.exe", true);
        });
    }

    [RelayCommand]
    private async Task StopEngineAsync()
    {
        await RunBusyAsync(() => Engine.StopAsync());
    }

    [RelayCommand]
    private async Task RestartEngineAsync()
    {
        await RunBusyAsync(async () =>
        {
            await Engine.StopAsync();
            await StartEngineAsync();
        });
    }

    [RelayCommand]
    private void OpenEngineFolder()
    {
        if (string.IsNullOrEmpty(EnginePath)) return;
        string? dir = Path.GetDirectoryName(EnginePath);
        if (dir == null || !Directory.Exists(dir)) return;
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
        }
        catch { }
    }

    [RelayCommand]
    private void ClearLogs() => Logs.Clear();
}
