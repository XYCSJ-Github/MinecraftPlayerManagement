using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mpm_GUI.Models;
using mpm_GUI.Services;

namespace mpm_GUI.ViewModels;

/// <summary>存档页：列出存档，查看/删除存档内玩家。</summary>
public partial class WorldsViewModel : PageViewModel
{
    [ObservableProperty]
    private WorldEntry? _selected;

    [ObservableProperty]
    private string _detailTitle = "请选择一个存档查看详情";

    [ObservableProperty]
    private bool _hasDetail;

    [ObservableProperty]
    private string _detailSummary = string.Empty;

    public ObservableCollection<WorldEntry> Items { get; } = new();
    public ObservableCollection<PlayerInWorldPresence> Rows { get; } = new();

    public WorldsViewModel(MpmEngineService engine, SettingsStore settings, DialogService dialogs)
        : base(engine, settings, dialogs)
    {
    }

    public async Task ReloadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var list = await Engine.ListWorldsAsync();
            Items.Clear();
            foreach (var item in list) Items.Add(item);
        }
        catch (Exception ex)
        {
            Items.Clear();
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
    private async Task ShowDetailAsync(WorldEntry world)
    {
        await RunBusyAsync(async () =>
        {
            if (world == null) return;
            Selected = world;
            DetailTitle = $"「{world.Name}」中的玩家";
            var rows = await Engine.OpenWorldAsync(world.Name);
            Rows.Clear();
            foreach (var r in rows) Rows.Add(r);
            HasDetail = true;
            DetailSummary = rows.Count == 0
                ? "未在该存档中发现可管理的玩家数据（玩家须存在于 usercache 且拥有进度/数据/统计文件）。"
                : $"共发现 {rows.Count} 名玩家数据。删除操作会将文件移入回收站。";
        });
    }

    [RelayCommand]
    private void OpenFolder(WorldEntry world)
    {
        if (world == null || string.IsNullOrEmpty(world.Directory)) return;
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{world.Directory}\"") { UseShellExecute = true });
        }
        catch { }
    }

    [RelayCommand]
    private async Task DeletePlayerInWorldAsync(PlayerInWorldPresence row)
    {
        if (row == null || Selected == null) return;
        bool ok = await Dialogs.ConfirmAsync(
            "删除该存档中的玩家",
            $"将从存档「{Selected.Name}」中删除玩家「{row.PlayerName}」的进度、玩家数据、旧数据、盔甲架数据与统计文件（移入回收站）。\n\n此操作不可通过本程序恢复。",
            "删除", danger: true);
        if (!ok) return;

        await RunBusyAsync(async () =>
        {
            await Engine.DeletePlayerFromWorldAsync(row.PlayerName, Selected.Name);
            Notifier.Show($"已从「{Selected.Name}」删除「{row.PlayerName}」");
            await ShowDetailAsync(Selected);
        });
    }

    [RelayCommand]
    private async Task DeleteAllInWorldAsync()
    {
        if (Selected == null || Rows.Count == 0) return;

        var names = new StringBuilder();
        int shown = 0;
        foreach (var r in Rows)
        {
            if (shown >= 8) { names.AppendLine($"...等共 {Rows.Count} 名"); break; }
            names.AppendLine($"· {r.PlayerName}");
            shown++;
        }

        bool ok = await Dialogs.ConfirmAsync(
            "清除该存档内全部玩家数据",
            $"将删除存档「{Selected.Name}」中以下玩家的进度/数据/统计等文件（移入回收站）：\n{names}\n\n此操作不可通过本程序恢复。",
            "全部删除", danger: true);
        if (!ok) return;

        await RunBusyAsync(async () =>
        {
            int success = 0;
            var errors = new List<string>();
            foreach (var r in Rows)
            {
                try
                {
                    await Engine.DeletePlayerFromWorldAsync(r.PlayerName, Selected.Name);
                    success++;
                }
                catch (MpmException ex)
                {
                    errors.Add($"{r.PlayerName}: {ex.Message}");
                }
            }

            if (errors.Count == 0)
                Notifier.Show($"已在「{Selected.Name}」删除 {success} 名玩家的数据");
            else
                Notifier.Show($"部分完成：成功 {success} 项。{errors.FirstOrDefault()}", true);

            await ShowDetailAsync(Selected);
        });
    }
}
