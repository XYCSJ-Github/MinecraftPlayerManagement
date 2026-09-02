using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace mpm_GUI.Services;

/// <summary>轻量全局通知（消息栏提示）。</summary>
public static class Notifier
{
    public static event Action<string, bool>? Message;

    public static void Show(string text, bool isError = false)
        => Message?.Invoke(text, isError);

    /// <summary>切回 UI 线程执行。</summary>
    public static void OnUi(Action action)
    {
        var app = Application.Current;
        if (app == null) { action(); return; }
        var dispatcher = app.Dispatcher;
        if (dispatcher.CheckAccess()) action();
        else
        {
            try { dispatcher.BeginInvoke(DispatcherPriority.Normal, action); }
            catch { /* 关闭期间调度器不可用则忽略 */ }
        }
    }
}

/// <summary>文件夹选择与确认对话框。</summary>
public sealed class DialogService
{
    /// <summary>选择文件夹。</summary>
    public string? PickFolder(string? initialDirectory = null)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "选择 Minecraft 客户端(.minecraft)或服务端根目录",
            Multiselect = false,
        };
        if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            dialog.InitialDirectory = initialDirectory;
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    /// <summary>选择 mpm.exe。</summary>
    public string? PickEngine()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择 mpm.exe 引擎",
            Filter = "mpm 引擎|mpm.exe",
            CheckFileExists = true,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public async Task<bool> ConfirmAsync(
        string title,
        string message,
        string okText = "确定",
        bool danger = true)
    {
        var window = new Views.ConfirmDialog
        {
            Owner = Application.Current?.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        window.SetContent(title, message, okText, danger);
        return await window.ShowDialogAsync();
    }
}

/// <summary>在常见位置探测 mpm.exe。</summary>
public static class EngineLocator
{
    public static string? Find(SettingsStore settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.MpmPath) && File.Exists(settings.MpmPath))
            return settings.MpmPath;

        var candidates = new List<string?>
        {
            // GUI 同目录
            Path.Combine(AppContext.BaseDirectory, "mpm.exe"),
            // 项目内最新构建产物（沿父目录逐级向上查找）
            FindRelative(Path.Combine("mpm", "x64", "Release", "net10.0-windows", "mpm.exe")),
            FindRelative(Path.Combine("mpm", "x64", "Debug", "net10.0-windows", "mpm.exe")),
            // 仓库其他输出
            FindRelative(Path.Combine("x64", "Release", "net10.0-windows", "win-x64", "mpm.exe")),
            FindRelative(Path.Combine("x64", "Release", "net10.0-windows", "mpm.exe")),
            FindRelative(Path.Combine("x64", "Debug", "net10.0-windows", "mpm.exe")),
            FindRelative(Path.Combine("x64", "Tests", "Debug", "net10.0-windows", "mpm.exe")),
        };

        foreach (var c in candidates)
        {
            if (!string.IsNullOrEmpty(c) && File.Exists(c))
                return Path.GetFullPath(c);
        }
        return null;
    }

    private static string? FindRelative(string relative)
    {
        try
        {
            string? dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8 && dir != null; i++)
            {
                string probe = Path.Combine(dir, relative);
                if (File.Exists(probe)) return probe;
                dir = Path.GetDirectoryName(dir);
            }
        }
        catch { }
        return null;
    }
}
