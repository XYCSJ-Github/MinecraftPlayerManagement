using CommunityToolkit.Mvvm.ComponentModel;
using mpm_GUI.Services;

namespace mpm_GUI.ViewModels;

/// <summary>页面视图模型基类。</summary>
public abstract partial class PageViewModel : ObservableObject
{
    protected readonly MpmEngineService Engine;
    protected readonly SettingsStore Settings;
    protected readonly DialogService Dialogs;

    [ObservableProperty]
    private bool _isBusy;

    protected PageViewModel(MpmEngineService engine, SettingsStore settings, DialogService dialogs)
    {
        Engine = engine;
        Settings = settings;
        Dialogs = dialogs;
    }

    /// <summary>统一带忙碌状态与异常提示地执行异步任务。</summary>
    protected async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await action();
        }
        catch (MpmException ex)
        {
            Notifier.Show(ex.Message, true);
        }
        catch (Exception ex)
        {
            Notifier.Show(ex.Message, true);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
