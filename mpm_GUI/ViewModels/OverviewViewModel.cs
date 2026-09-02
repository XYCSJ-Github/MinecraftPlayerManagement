using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mpm_GUI.Services;

namespace mpm_GUI.ViewModels;

/// <summary>概览页：连接引擎、选择根目录、快捷统计。</summary>
public partial class OverviewViewModel : PageViewModel
{
    private readonly ShellViewModel _shell;

    [ObservableProperty]
    private string _rootName = "尚未打开任何目录";

    [ObservableProperty]
    private string _rootPath = string.Empty;

    [ObservableProperty]
    private string _modeText = string.Empty;

    [ObservableProperty]
    private bool _isClient;

    [ObservableProperty]
    private bool _isServer;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionText = "引擎未连接";

    [ObservableProperty]
    private string _enginePath = string.Empty;

    [ObservableProperty]
    private int _worldCount;

    [ObservableProperty]
    private int _playerCount;

    public OverviewViewModel(
        MpmEngineService engine,
        SettingsStore settings,
        DialogService dialogs,
        ShellViewModel shell)
        : base(engine, settings, dialogs)
    {
        _shell = shell;
        EnginePath = settings.MpmPath;
        OnEngineState(engine.State);
    }

    internal void OnEngineState(EngineState state)
    {
        EnginePath = Settings.MpmPath;
        IsConnected = state == EngineState.Ready;
        ConnectionText = state switch
        {
            EngineState.Ready => "引擎已就绪",
            EngineState.Connecting => "正在连接引擎...",
            EngineState.Error => "引擎异常",
            _ => "引擎未连接",
        };
    }

    internal void SetRoot(string name, string path)
    {
        RootName = name;
        RootPath = path;
        SetMode(Engine.CurrentMode);
    }

    internal void SetCounts(int worlds, int players)
    {
        WorldCount = worlds;
        PlayerCount = players;
    }

    private void SetMode(LoadMode mode)
    {
        IsClient = mode == LoadMode.CLIENT;
        IsServer = mode == LoadMode.SERVER;
        ModeText = mode switch
        {
            LoadMode.CLIENT => "客户端",
            LoadMode.SERVER => "服务端",
            _ => "未知",
        };
    }

    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!await _shell.EnsureEngineStartedAsync()) return;

            string? folder = Dialogs.PickFolder(Settings.LastRootPath);
            if (string.IsNullOrEmpty(folder)) return;

            await _shell.TryReopenRootAsync(folder);
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!Engine.IsConnected)
            {
                Notifier.Show("引擎未就绪", true);
                return;
            }
            await _shell.ReloadAllAsync();
            Notifier.Show("数据已刷新");
        });
    }

    [RelayCommand]
    private async Task RestartEngineAsync()
    {
        await RunBusyAsync(async () => await _shell.RestartEngineAsync());
    }

    [RelayCommand]
    private void OpenRootFolder()
    {
        if (string.IsNullOrEmpty(RootPath) || !Directory.Exists(RootPath))
        {
            Notifier.Show("当前根目录不存在或未设置", true);
            return;
        }
        try { Process.Start(new ProcessStartInfo("explorer.exe", $"\"{RootPath}\"") { UseShellExecute = true }); }
        catch { /* ignore */ }
    }
}
