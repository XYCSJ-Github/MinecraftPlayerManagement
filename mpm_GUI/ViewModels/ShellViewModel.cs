using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using mpm_GUI.Services;

namespace mpm_GUI.ViewModels;

/// <summary>主窗口宿主视图模型：协调四个页签、引擎、提示栏。</summary>
public partial class ShellViewModel : ObservableObject
{
    private readonly MpmEngineService _engine;
    private readonly SettingsStore _settings;
    private readonly DialogService _dialogs;
    private DispatcherTimer? _noticeTimer;

    public OverviewViewModel Overview { get; }
    public WorldsViewModel Worlds { get; }
    public PlayersViewModel Players { get; }
    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    private bool _noticeVisible;

    [ObservableProperty]
    private string _noticeText = string.Empty;

    [ObservableProperty]
    private bool _noticeIsError;

    public string Title => "mpm 玩家档案管理";

    public ShellViewModel(
        MpmEngineService engine,
        SettingsStore settings,
        DialogService dialogs)
    {
        _engine = engine;
        _settings = settings;
        _dialogs = dialogs;

        Overview = new OverviewViewModel(engine, settings, dialogs, this);
        Worlds = new WorldsViewModel(engine, settings, dialogs);
        Players = new PlayersViewModel(engine, settings, dialogs);
        Settings = new SettingsViewModel(engine, settings, dialogs);

        Notifier.Message += OnNotifyMessage;
        _engine.LogLine += line => Notifier.OnUi(() => Settings.AddLog(line));
        _engine.StateChanged += state => Notifier.OnUi(() =>
        {
            Overview.OnEngineState(state);
            Settings.OnEngineState(state);
        });

        _settings.Load();
        string? found = EngineLocator.Find(_settings);
        if (found != null)
        {
            _settings.MpmPath = found;
            Settings.Initialize(_settings.MpmPath, found);
        }
        Settings.LoadSettingsIntoUi();
    }

    /// <summary>确保引擎处于就绪状态，必要时启动。</summary>
    public async Task<bool> EnsureEngineStartedAsync()
    {
        if (_engine.IsConnected) return true;

        string? path = _settings.MpmPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            path = EngineLocator.Find(_settings);
            if (string.IsNullOrEmpty(path))
            {
                path = _dialogs.PickEngine();
                if (string.IsNullOrEmpty(path)) return false;
            }
            _settings.MpmPath = path;
            _settings.Save();
            Settings.LoadSettingsIntoUi();
        }

        bool ok = await _engine.StartAsync(path);
        if (!ok)
        {
            Notifier.Show("引擎启动失败：请检查 mpm.exe 路径与运行权限", true);
            return false;
        }
        return true;
    }

    /// <summary>重启引擎（保留当前根目录状态并重新加载）。</summary>
    public async Task RestartEngineAsync()
    {
        await _engine.StopAsync();
        if (await EnsureEngineStartedAsync())
        {
            string? root = _settings.LastRootPath;
            if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
            {
                await TryReopenRootAsync(root);
            }
        }
    }

    /// <summary>打开根目录并加载全部列表。</summary>
    public async Task<bool> TryReopenRootAsync(string root)
    {
        try
        {
            string name = await _engine.OpenPathAsync(root);
            _settings.LastRootPath = root;
            _settings.Save();
            await ReloadAllAsync();
            Overview.SetRoot(name, root);
            Notifier.Show($"已打开：{name}");
            return true;
        }
        catch (MpmException ex)
        {
            Notifier.Show(ex.Message, true);
            return false;
        }
    }

    /// <summary>刷新全部页签数据与概览计数。</summary>
    public async Task ReloadAllAsync()
    {
        await Worlds.ReloadAsync();
        await Players.ReloadAsync();
        Overview.SetCounts(Worlds.Items.Count, Players.Items.Count);
    }

    private void OnNotifyMessage(string text, bool isError)
    {
        Notifier.OnUi(() =>
        {
            NoticeText = text;
            NoticeIsError = isError;
            NoticeVisible = true;
            _noticeTimer?.Stop();
            _noticeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
            _noticeTimer.Tick += (_, _) =>
            {
                NoticeVisible = false;
                _noticeTimer.Stop();
            };
            _noticeTimer.Start();
        });
    }
}
