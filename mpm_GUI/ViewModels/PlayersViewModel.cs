using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mpm_GUI.Models;
using mpm_GUI.Services;

namespace mpm_GUI.ViewModels;

/// <summary>玩家页：列出 usercache 玩家，查看跨存档情况并删除。</summary>
public partial class PlayersViewModel : PageViewModel
{
    private readonly List<PlayerEntry> _all = new();

    private string _searchText = string.Empty;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value)) ApplyFilter();
        }
    }

    [ObservableProperty]
    private PlayerEntry? _selected;

    [ObservableProperty]
    private bool _hasDetail;

    [ObservableProperty]
    private string _detailTitle = "选择一名玩家查看跨存档情况";

    [ObservableProperty]
    private string _detailSummary = string.Empty;

    [ObservableProperty]
    private int _totalCount;

    public ObservableCollection<PlayerEntry> Items { get; } = new();
    public ObservableCollection<PlayerInWorldPresence> Rows { get; } = new();

    public PlayersViewModel(MpmEngineService engine, SettingsStore settings, DialogService dialogs)
        : base(engine, settings, dialogs)
    {
    }

    private void ApplyFilter()
    {
        var kw = SearchText?.Trim() ?? string.Empty;
        Items.Clear();
        foreach (var p in _all)
        {
            if (kw.Length == 0
                || p.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || p.Uuid.Contains(kw, StringComparison.OrdinalIgnoreCase))
            {
                Items.Add(p);
            }
        }
    }

    public async Task ReloadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var list = await Engine.ListPlayersAsync();
            _all.Clear();
            _all.AddRange(list);
            TotalCount = _all.Count;
            ApplyFilter();
            if (_all.Count == 0) HasDetail = false;
        }
        catch (Exception ex)
        {
            _all.Clear();
            Items.Clear();
            TotalCount = 0;
            Notifier.Show(ex.Message, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Reload() => await ReloadAsync();

    [RelayCommand]
    private async Task ShowDetailAsync(PlayerEntry player)
    {
        await RunBusyAsync(async () =>
        {
            if (player == null) return;
            Selected = player;
            DetailTitle = $"「{player.Name}」在存档中的足迹";
            var rows = await Engine.OpenPlayerAsync(player.Name);
            Rows.Clear();
            foreach (var r in rows) Rows.Add(r);
            HasDetail = true;
            DetailSummary = rows.Count == 0
                ? $"「{player.Name}」在各存档中未发现任何进度/数据/统计文件。"
                : $"{player.Name} 共在 {rows.Count} 个存档中留有数据。删除操作会将文件移入回收站。";
        });
    }

    [RelayCommand]
    private void OpenProfileJsonFolder()
    {
        // 概览根目录在设置页可查看；此处打开引擎所在目录避免无意义操作
        if (!string.IsNullOrEmpty(Settings.LastRootPath))
        {
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{Settings.LastRootPath}\"") { UseShellExecute = true });
            }
            catch { }
        }
    }

    [RelayCommand]
    private async Task DeletePlayerAsync(PlayerEntry player)
    {
        if (player == null) return;
        bool ok = await Dialogs.ConfirmAsync(
            "彻底删除玩家",
            $"将从所有存档及 usercache/usernamecache 中彻底删除玩家「{player.Name}」\nUUID：{player.Uuid}\n\n涉及的所有数据/进度/统计文件将移入回收站，不可通过本程序恢复。",
            "彻底删除", danger: true);
        if (!ok) return;

        await RunBusyAsync(async () =>
        {
            await Engine.DeletePlayerAsync(player.Name);
            Notifier.Show($"已彻底删除「{player.Name}」");
            await ReloadAsync();
            if (player.Equals(Selected)) { Rows.Clear(); HasDetail = false; }
        });
    }

    [RelayCommand]
    private async Task DeleteFromWorldAsync(PlayerInWorldPresence row)
    {
        if (row == null) return;
        bool ok = await Dialogs.ConfirmAsync(
            "删除该存档中的玩家",
            $"将从存档「{row.WorldName}」中删除玩家「{row.PlayerName}」的进度/数据/统计等文件（移入回收站）。",
            "删除", danger: true);
        if (!ok) return;

        await RunBusyAsync(async () =>
        {
            await Engine.DeletePlayerFromWorldAsync(row.PlayerName, row.WorldName);
            Notifier.Show($"已从「{row.WorldName}」删除「{row.PlayerName}」");
            await ShowDetailAsync(Selected!);
        });
    }

    [RelayCommand]
    private async Task ClearCacheAsync(PlayerEntry player)
    {
        if (player == null) return;
        bool ok = await Dialogs.ConfirmAsync(
            "移除玩家名称缓存",
            $"仅从 usercache.json / usernamecache.json 中移除「{player.Name}」，不影响任何存档数据。",
            "移除", danger: false);
        if (!ok) return;

        await RunBusyAsync(async () =>
        {
            await Engine.ClearJsonCacheAsync(player.Name);
            Notifier.Show($"已移除「{player.Name}」的缓存记录");
            await ReloadAsync();
        });
    }

    [RelayCommand]
    private async Task ClearAllCachesAsync()
    {
        bool ok = await Dialogs.ConfirmAsync(
            "清空全部玩家名称缓存",
            "将清空 usercache.json 与 usernamecache.json 中的全部玩家名称记录，不影响任何存档数据。",
            "清空", danger: true);
        if (!ok) return;

        await RunBusyAsync(async () =>
        {
            await Engine.ClearJsonCacheAsync(null);
            Notifier.Show("已清空全部玩家名称缓存");
            await ReloadAsync();
        });
    }
}
