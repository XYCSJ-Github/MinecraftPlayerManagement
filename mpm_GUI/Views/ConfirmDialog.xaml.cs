using System.Windows;
using System.Windows.Media;

namespace mpm_GUI.Views;

public partial class ConfirmDialog : Window
{
    private TaskCompletionSource<bool>? _tcs;

    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public void SetContent(string title, string message, string okText, bool danger)
    {
        TitleText.Text = title;
        MessageText.Text = message;
        OkButton.Content = okText;

        if (danger)
        {
            var accent = (SolidColorBrush)Application.Current.TryFindResource("DangerBrush");
            if (accent != null) { IconBadge.Background = accent; OkButton.Background = accent; }
        }
        else
        {
            var accent = (SolidColorBrush)Application.Current.TryFindResource("AccentBrush");
            if (accent != null) { IconBadge.Background = accent; OkButton.Background = accent; }
        }
    }

    public Task<bool> ShowDialogAsync()
    {
        _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        ShowDialog();
        return _tcs.Task;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        _tcs?.TrySetResult(true);
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        _tcs?.TrySetResult(false);
        DialogResult = false;
    }

    private void OnClosed(object sender, EventArgs e)
    {
        _tcs?.TrySetResult(DialogResult == true);
    }
}
