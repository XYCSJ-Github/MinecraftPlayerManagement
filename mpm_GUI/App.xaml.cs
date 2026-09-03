using System.IO;
using System.Windows;
using System.Windows.Threading;
using mpm_GUI.Services;
using mpm_GUI.ViewModels;

namespace mpm_GUI;

public partial class App : Application
{
    private MpmEngineService? _engine;
    private SettingsStore? _settings;
    private ShellViewModel? _shell;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 自检模式：mpm_GUI.exe --smoke <工作目录> [结果文件]
        int smokeIndex = Array.IndexOf(e.Args, "--smoke");
        if (smokeIndex >= 0)
        {
            RunSmoke(e.Args, smokeIndex);
            Shutdown();
            return;
        }

        // UI 冒烟：短暂打开主窗口以捕获 XAML/绑定 运行期错误
        bool uiProbe = Array.IndexOf(e.Args, "--ui") >= 0;

        ShutdownMode = ShutdownMode.OnMainWindowClose;

        _settings = new SettingsStore();
        _engine = new MpmEngineService(_settings);
        var dialogs = new DialogService();
        var shell = new ShellViewModel(_engine, _settings, dialogs);

        _shell = shell;
        var window = new MainWindow { DataContext = shell };
        MainWindow = window;
        window.Show();

        if (uiProbe)
        {
            string logFile = Path.Combine(Path.GetTempPath(), "mpm_ui_probe.txt");
            void Trace(string m) { try { File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss.fff}] {m}\r\n"); } catch { } }
            Trace("window shown");
            DispatcherUnhandledException += (_, a) => { Trace("dispatcher exception: " + a.Exception); a.Handled = false; };

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            timer.Tick += (_, _) => { timer.Stop(); Trace("timer tick -> shutdown"); Shutdown(); };
            timer.Start();
        }
        else
        {
            _ = _shell.AutoConnectAsync();
        }
    }

    private static void RunSmoke(string[] args, int index)
    {
        string workDir = args.Length > index + 1 ? args[index + 1] : Path.GetTempPath();
        string outFile = args.Length > index + 2
            ? args[index + 2]
            : Path.Combine(Path.GetTempPath(), "mpm_gui_smoke_result.txt");

        string result;
        try
        {
            // 避免在阻塞 UI 线程的 GetResult 上造成 async 续体死锁，整体放到线程池执行
            result = Task.Run(() => SmokeRunner.RunAsync(workDir)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            result = "== FAIL ==\r\n" + ex;
        }

        File.WriteAllText(outFile, result);
        Environment.ExitCode = result.Contains("== PASS ==") ? 0 : 1;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 关闭时停止引擎（发送 EXIT 并等待，必要时强制结束）
        _engine?.Dispose();
        base.OnExit(e);
    }
}
