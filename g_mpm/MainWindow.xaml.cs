using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace g_mpm
{
    public partial class MainWindow : Window
    {
        private SharedMemoryCreator? _memoryCreator;
        private DispatcherTimer? _statusTimer;
        private DispatcherTimer? _replyCheckTimer;
        private CancellationTokenSource? _cts;
        private bool _isRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeApp();
        }

        private async void InitializeApp()
        {
            try
            {
                // 显示进程ID
                txtPid.Text = Process.GetCurrentProcess().Id.ToString();

                // 创建共享内存管理器
                var config = new SharedMemoryConfig
                {
                    EnableVerboseLogging = true,
                    ReplyTimeout = 3000,
                    InitTimeout = 10000
                };

                _memoryCreator = new SharedMemoryCreator(config);

                // 订阅事件
                _memoryCreator.MessageSent += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        AppendLog($"[{e.Timestamp:HH:mm:ss.fff}] 📤 发送: {e.Message}");
                    });
                };

                _memoryCreator.ReplyReceived += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        AppendLog($"[{e.Timestamp:HH:mm:ss.fff}] 📥 接收: {e.Message}");
                        txtReceivedCount.Text = _memoryCreator.RepliesReceived.ToString();
                        txtMessageInfo.Text = $"收到回复: {e.Message}";
                    });
                };

                _memoryCreator.ErrorOccurred += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        string errorMsg = $"[ERROR] {e.ErrorMessage}";
                        if (e.Exception != null)
                        {
                            errorMsg += $" - {e.Exception.Message}";
                        }
                        AppendLog(errorMsg);
                        txtMessageInfo.Text = $"错误: {e.ErrorMessage}";
                    });
                };

                _memoryCreator.ConnectionStatusChanged += (s, isConnected) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtConnectionStatus.Text = isConnected ? "🟢 已连接" : "🔴 未连接";
                        txtConnectionStatus.Foreground = isConnected ?
                            Brushes.LightGreen :
                            Brushes.LightCoral;

                        UpdateControlStates(isConnected);
                    });
                };

                _memoryCreator.CppProcessStatusChanged += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (e.IsRunning)
                        {
                            txtClientStatus.Text = $"运行中 (PID: {e.ProcessId})";
                            txtClientStatus.Foreground = Brushes.LightGreen;
                        }
                        else
                        {
                            txtClientStatus.Text = $"已退出 (代码: {e.ExitCode})";
                            txtClientStatus.Foreground = Brushes.LightCoral;

                            if (_memoryCreator?.IsInitialized == true)
                            {
                                txtConnectionStatus.Text = "🔴 未连接";
                                txtConnectionStatus.Foreground = Brushes.LightCoral;
                                UpdateControlStates(false);
                            }
                        }
                    });
                };

                _memoryCreator.StatusMessage += (s, message) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        AppendLog($"[INFO] {message}");
                        txtInfo.Text = message;
                    });
                };

                // 初始化定时器
                _statusTimer = new DispatcherTimer();
                _statusTimer.Interval = TimeSpan.FromSeconds(1);

#pragma warning disable CS8622 // 参数类型中引用类型的为 Null 性与目标委托不匹配(可能是由于为 Null 性特性)。

                _statusTimer.Tick += StatusTimer_Tick;
                _statusTimer.Start();

                _replyCheckTimer = new DispatcherTimer();
                _replyCheckTimer.Interval = TimeSpan.FromMilliseconds(500);
                _replyCheckTimer.Tick += ReplyCheckTimer_Tick;

#pragma warning restore CS8622 // 参数类型中引用类型的为 Null 性与目标委托不匹配(可能是由于为 Null 性特性)。


                // 更新初始状态
                AppendLog($"[INFO] g_mpm 应用程序启动成功，PID: {Process.GetCurrentProcess().Id}");
                txtInfo.Text = "g_mpm 应用程序已启动";

                // 自动启动共享内存和C++进程
                await AutoInitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                AppendLog($"[ERROR] 初始化失败: {ex.Message}");
            }
        }

        // 在MainWindow.xaml.cs中修改AutoInitializeAsync方法
        private async Task AutoInitializeAsync()
        {
            try
            {
                AppendLog("[INFO] 正在自动初始化共享内存并启动C++进程...");

                // 禁用初始化按钮，防止重复点击
                btnInitialize.IsEnabled = false;
                btnInitialize.Content = "正在初始化...";

                if (_memoryCreator != null)
                {
                    // 先启动监控线程（在初始化之前）
                    _isRunning = true;
                    _cts = new CancellationTokenSource();
                    _replyCheckTimer?.Start();

                    // 启动异步监控
                    _ = Task.Run(() => MonitorRepliesAsync(_cts.Token));

                    // 然后初始化
                    bool success = await _memoryCreator.InitializeAsync();

                    if (success)
                    {
                        AppendLog("[INFO] 自动初始化完成，C++进程已启动");
                    }
                    else
                    {
                        AppendLog("[ERROR] 自动初始化失败");
                        btnInitialize.IsEnabled = true;
                        btnInitialize.Content = "重新初始化";

                        // 停止监控
                        _isRunning = false;
                        _cts?.Cancel();
                        _replyCheckTimer?.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 自动初始化异常: {ex.Message}");
                btnInitialize.IsEnabled = true;
                btnInitialize.Content = "重新初始化";
            }
        }

        // 修改MonitorRepliesAsync方法，添加就绪消息处理
        private async Task MonitorRepliesAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_memoryCreator?.IsInitialized == true)
                    {
                        // 使用CheckForReply检查消息
                        var (hasReply, reply) = _memoryCreator.CheckForReply();
                        if (hasReply && !string.IsNullOrEmpty(reply))
                        {
                            // 消息已经在CheckForReply中处理，这里不需要额外处理
                        }
                    }
                    await Task.Delay(100, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AppendLog($"[ERROR] 监控线程异常: {ex.Message}");
                    });
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        private void UpdateControlStates(bool isConnected)
        {
            if (isConnected)
            {
                btnInitialize.IsEnabled = false;
                btnInitialize.Content = "已连接";
                btnRestartCpp.IsEnabled = true;
                btnTestPing.IsEnabled = true;
                btnTestBatch.IsEnabled = true;
                btnStopCpp.IsEnabled = true;
                btnDispose.IsEnabled = true;
                txtMessage.IsEnabled = true;
                btnSend.IsEnabled = true;
            }
            else
            {
                btnInitialize.IsEnabled = true;
                btnInitialize.Content = "重新初始化";
                btnRestartCpp.IsEnabled = false;
                btnTestPing.IsEnabled = false;
                btnTestBatch.IsEnabled = false;
                btnStopCpp.IsEnabled = false;
                btnDispose.IsEnabled = true;
                txtMessage.IsEnabled = false;
                btnSend.IsEnabled = false;
            }
        }

        private async void btnInitialize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_memoryCreator == null) return;

                txtMessageInfo.Text = "正在初始化共享内存...";

                bool success = await _memoryCreator.InitializeAsync();
                if (success)
                {
                    txtMessageInfo.Text = "共享内存初始化成功，C++进程已启动";

                    // 启动回复监控
                    _isRunning = true;
                    _cts = new CancellationTokenSource();
                    _replyCheckTimer?.Start();

                    // 启动异步监控
                    _ = Task.Run(() => MonitorRepliesAsync(_cts.Token));
                }
                else
                {
                    txtMessageInfo.Text = "初始化失败";
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 初始化失败: {ex.Message}");
                txtMessageInfo.Text = $"初始化失败: {ex.Message}";
            }
        }

        private async void btnRestartCpp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_memoryCreator == null) return;

                // 先停止现有进程
                await _memoryCreator.StopCppProcessAsync();

                // 重新启动
                AppendLog("[INFO] 正在重启C++进程...");
                bool success = await _memoryCreator.InitializeAsync();

                if (success)
                {
                    AppendLog("[INFO] C++进程重启成功");
                }
                else
                {
                    AppendLog("[ERROR] C++进程重启失败");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 重启C++进程异常: {ex.Message}");
            }
        }

        private async void btnDispose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtMessageInfo.Text = "正在清理资源...";

                _isRunning = false;
                _cts?.Cancel();
                _replyCheckTimer?.Stop();

                // 停止C++进程
                if (_memoryCreator != null)
                {
                    await _memoryCreator.StopCppProcessAsync();
                    _memoryCreator.Dispose();
                    _memoryCreator = null;
                }

                txtMessageInfo.Text = "资源已清理";
                txtClientStatus.Text = "未启动";
                txtClientStatus.Foreground = Brushes.Gray;
                txtSentCount.Text = "0";
                txtReceivedCount.Text = "0";
                txtUptime.Text = "00:00:00";

                // 重新启用初始化按钮
                btnInitialize.IsEnabled = true;
                btnInitialize.Content = "重新初始化";

                // 更新连接状态
                txtConnectionStatus.Text = "🔴 未连接";
                txtConnectionStatus.Foreground = Brushes.LightCoral;
                UpdateControlStates(false);
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 清理资源异常: {ex.Message}");
                txtMessageInfo.Text = $"清理失败: {ex.Message}";
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void txtMessage_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == 0)
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void SendMessage()
        {
            string message = txtMessage.Text.Trim();

            if (string.IsNullOrEmpty(message))
            {
                MessageBox.Show("请输入消息内容", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_memoryCreator == null || !_memoryCreator.IsInitialized)
            {
                MessageBox.Show("共享内存未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                bool success = _memoryCreator.SendMessage(message);
                if (success)
                {
                    txtSentCount.Text = _memoryCreator.MessagesSent.ToString();
                    txtMessage.Clear();
                    txtMessageInfo.Text = $"消息发送成功: {message}";
                }
                else
                {
                    txtMessageInfo.Text = "消息发送失败";
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 发送消息失败: {ex.Message}");
                txtMessageInfo.Text = $"发送失败: {ex.Message}";
            }
        }

        private async void btnTestPing_Click(object sender, RoutedEventArgs e)
        {
            if (_memoryCreator == null || !_memoryCreator.IsInitialized)
            {
                MessageBox.Show("请先初始化共享内存", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _memoryCreator.SendMessage("ping");

                // 等待回复
                await Task.Delay(1000);
                var (success, reply) = await _memoryCreator.WaitForReplyAsync(3000);
                if (success)
                {
                    AppendLog($"[TEST] ping测试成功: {reply}");
                }
                else
                {
                    AppendLog($"[TEST] ping测试失败: {reply}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 测试异常: {ex.Message}");
            }
        }

        private async void btnTestBatch_Click(object sender, RoutedEventArgs e)
        {
            if (_memoryCreator == null || !_memoryCreator.IsInitialized)
            {
                MessageBox.Show("请先初始化共享内存", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string[] testMessages = new[]
            {
                "ping",
                "Hello from g_mpm Server!",
                "测试中文消息",
                "查询状态",
                "这是一条较长的测试消息，用于测试缓冲区处理能力",
                "1234567890",
                "最后一条测试消息"
            };

            AppendLog("[TEST] 开始批量测试...");

            foreach (var message in testMessages)
            {
                try
                {
                    _memoryCreator.SendMessage(message);
                    AppendLog($"[TEST] 发送: {message}");

                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    AppendLog($"[ERROR] 测试消息失败: {ex.Message}");
                }
            }

            AppendLog("[TEST] 批量测试完成");
        }

        private async void btnStopCpp_Click(object sender, RoutedEventArgs e)
        {
            if (_memoryCreator == null || !_memoryCreator.IsInitialized)
            {
                MessageBox.Show("请先初始化共享内存", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("确定要停止C++进程吗？",
                "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bool success = await _memoryCreator.StopCppProcessAsync();
                    if (success)
                    {
                        AppendLog("[INFO] C++进程已停止");
                    }
                    else
                    {
                        AppendLog("[ERROR] 停止C++进程失败");
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"[ERROR] 停止C++进程失败: {ex.Message}");
                }
            }
        }

        private void QuickMessage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string message)
            {
                txtMessage.Text = message;
                txtMessage.Focus();
                txtMessage.CaretIndex = message.Length;
            }
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
            AppendLog("[INFO] 日志已清空");
        }

        private void txtMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            int length = txtMessage.Text.Length;
            txtCharCount.Text = $"{length}/256";

            if (length > 256)
            {
                txtCharCount.Foreground = Brushes.Red;
                txtCharCount.FontWeight = FontWeights.Bold;
            }
            else if (length > 200)
            {
                txtCharCount.Foreground = Brushes.Orange;
                txtCharCount.FontWeight = FontWeights.Normal;
            }
            else
            {
                txtCharCount.Foreground = Brushes.Gray;
                txtCharCount.FontWeight = FontWeights.Normal;
            }
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            // 更新时间
            txtTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 更新运行时间
            if (_memoryCreator?.IsInitialized == true)
            {
                var uptime = _memoryCreator.Uptime;
                txtUptime.Text = $"{uptime.Hours:00}:{uptime.Minutes:00}:{uptime.Seconds:00}";
            }

            // 更新状态信息
            if (_memoryCreator?.IsInitialized == true)
            {
                txtConnectionStatus.Text = "🟢 已连接";
                txtConnectionStatus.Foreground = Brushes.LightGreen;
            }
            else
            {
                txtConnectionStatus.Text = "🔴 未连接";
                txtConnectionStatus.Foreground = Brushes.LightCoral;
            }
        }

        private void ReplyCheckTimer_Tick(object sender, EventArgs e)
        {
            if (_memoryCreator == null || !_memoryCreator.IsInitialized || !_isRunning)
                return;

            try
            {
                var (hasReply, reply) = _memoryCreator.CheckForReply();
                // 事件处理已经在SharedMemoryCreator中完成
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 检查回复异常: {ex.Message}");
            }
        }

        private void AppendLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText($"{message}\n");
                txtLog.ScrollToEnd();
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _isRunning = false;
                _cts?.Cancel();
                _statusTimer?.Stop();
                _replyCheckTimer?.Stop();

                if (_memoryCreator != null)
                {
                    // 尝试优雅停止C++进程
                    _ = _memoryCreator.StopCppProcessAsync();
                    _memoryCreator.Dispose();
                }

                AppendLog("[INFO] g_mpm 应用程序正常关闭");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 关闭时异常: {ex.Message}");
            }
        }
    }
}