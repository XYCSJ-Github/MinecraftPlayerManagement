using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mpm_GUI.Services;

namespace mpm_GUI.ViewModels;

/// <summary>日志级别。</summary>
public enum LogLevel
{
    Info,
    Warning,
    Error,
    Debug,
}

/// <summary>一条可着色的运行日志。</summary>
public sealed record LogEntry(string Text, LogLevel Level, Brush Foreground);

/// <summary>设置页：mpm路径、状态与运行日志。</summary>
public partial class SettingsViewModel : PageViewModel
{
    private const int MaxLogCount = 400;

    private static readonly Brush LogInfoBrush = MakeBrush("#D4D4D4");
    private static readonly Brush LogWarningBrush = MakeBrush("#FFD54F");
    private static readonly Brush LogErrorBrush = MakeBrush("#FF8A80");
    private static readonly Brush LogDebugBrush = MakeBrush("#82AAFF");

    private static Brush MakeBrush(string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    [ObservableProperty]
    private string _enginePath = string.Empty;

    [ObservableProperty]
    private string _engineStatus = "未连接";

    [ObservableProperty]
    private bool _isConnected;

    public ObservableCollection<LogEntry> Logs { get; } = new();

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

    /// <summary>追加本进程(GUI 侧)产生的日志。</summary>
    public void AddLog(string line)
    {
        if (string.IsNullOrEmpty(line)) return;
        LogLevel level = ContainsErrorText(line) ? LogLevel.Error : LogLevel.Info;
        AddEntry(line, level);
    }

    /// <summary>追加 mpm.exe 自身输出的一行（stdout/stderr），按级别着色。</summary>
    public void AddEngineLine(string line, bool isError)
    {
        if (string.IsNullOrEmpty(line)) return;
        AddEntry(line, Classify(line, isError));
    }

    private static LogLevel Classify(string line, bool isError)
    {
        if (line.Contains("[Warning]", StringComparison.OrdinalIgnoreCase)) return LogLevel.Warning;
        if (line.Contains("[Debug]", StringComparison.OrdinalIgnoreCase)) return LogLevel.Debug;
        if (isError || line.Contains("[Error]", StringComparison.OrdinalIgnoreCase)) return LogLevel.Error;
        return LogLevel.Info;
    }

    private static bool ContainsErrorText(string line)
        => line.Contains("失败", StringComparison.Ordinal)
            || line.Contains("异常", StringComparison.Ordinal)
            || line.Contains("错误", StringComparison.Ordinal)
            || line.Contains("超时", StringComparison.Ordinal)
            || line.Contains("退出", StringComparison.Ordinal);

    private void AddEntry(string line, LogLevel level)
    {
        Brush brush = level switch
        {
            LogLevel.Warning => LogWarningBrush,
            LogLevel.Error => LogErrorBrush,
            LogLevel.Debug => LogDebugBrush,
            _ => LogInfoBrush,
        };
        Logs.Add(new LogEntry(line, level, brush));
        while (Logs.Count > MaxLogCount) Logs.RemoveAt(0);
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
