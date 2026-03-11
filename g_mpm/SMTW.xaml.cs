using g_mpm.Enums;
using System.Windows;
using System.Windows.Threading;
using Smc = g_mpm.SharedMemoryConfig.SharedMemoryConfig;

namespace g_mpm
{
    public partial class SMTW : Window
    {
        private SharedMemoryLauncher _launcher;
        private DispatcherTimer _statusTimer;
        private Dictionary<string, Command> _commandDict;

        public SMTW()
        {
            InitializeComponent();
            InitializeCommands();
            InitializeLauncher();
            SetupStatusTimer();
        }

        private void InitializeCommands()
        {
            _commandDict = new Dictionary<string, Command>
            {
                ["空命令"] = Command.EMPTY_COMMAND,
                ["设置路径"] = Command.M_SET_PATH,
                ["退出"] = Command.EXIT,
                ["返回"] = Command.BREAK,
                ["打开存档"] = Command.OPEN_WORLD,
                ["打开玩家"] = Command.OPEN_PLAYER,
                ["列出存档"] = Command.LIST_WORLD,
                ["列出玩家"] = Command.LIST_PLAYER,
                ["删除玩家"] = Command.DEL_PLAYER,
                ["删除存档"] = Command.DEL_WORLD,
                ["刷新"] = Command.REFRESH
            };

            cmbCommands.ItemsSource = _commandDict;
            cmbCommands.DisplayMemberPath = "Key";
            cmbCommands.SelectedValuePath = "Value";
            cmbCommands.SelectedIndex = 0;
        }

        private void InitializeLauncher()
        {
            var config = new Smc
            {
                EnableVerboseLogging = true,
                InitTimeout = 30000,
                ReplyTimeout = 5000
            };
            _launcher = new SharedMemoryLauncher(config);
        }

        private void SetupStatusTimer()
        {
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _statusTimer.Tick += (s, e) => UpdateStatusDisplay();
            _statusTimer.Start();
        }

        private void UpdateStatusDisplay()
        {
            txtConnectStatus.Text = _launcher?.ConnectStatus.ToString() ?? "未知";
            txtProgramStatus.Text = _launcher?.ProgramStatus.ToString() ?? "未知";

            switch (_launcher?.ConnectStatus)
            {
                case ConnectStatus.NOT_INITIALIZED:
                    txtConnectStatus.Foreground = System.Windows.Media.Brushes.Gray;
                    break;
                case ConnectStatus.INITIALIZED:
                    txtConnectStatus.Foreground = System.Windows.Media.Brushes.Orange;
                    break;
                case ConnectStatus.CONNECTED:
                    txtConnectStatus.Foreground = System.Windows.Media.Brushes.Green;
                    break;
            }

            txtProgramStatus.Foreground = _launcher?.IsRunning == true
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
                scrollViewer.ScrollToBottom();
            });
        }

        // 按钮事件处理
        private async void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("=== 开始完整启动流程 ===");
                bool success = await _launcher.LaunchAsync();
                Log(success ? "✓ 完整启动流程完成" : "✗ 完整启动流程失败");
            }
            catch (Exception ex)
            {
                Log($"✗ 启动异常: {ex.Message}");
            }
        }

        private void BtnStage1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("[阶段1] 初始化共享内存...");
                bool success = _launcher.Stage1_InitializeSharedMemory();
                Log(success ? "✓ 阶段1完成" : "✗ 阶段1失败");
            }
            catch (Exception ex)
            {
                Log($"✗ 阶段1异常: {ex.Message}");
            }
        }

        private void BtnStage3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("[阶段3] 启动C++进程...");
                bool success = _launcher.Stage3_StartCppProcess();
                Log(success ? "✓ 阶段3完成" : "✗ 阶段3失败");
            }
            catch (Exception ex)
            {
                Log($"✗ 阶段3异常: {ex.Message}");
            }
        }

        private async void BtnStage2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("[阶段2] 等待C++就绪...");
                bool success = await _launcher.Stage2_WaitForCppReadyAsync();
                Log(success ? "✓ 阶段2完成" : "✗ 阶段2失败");
            }
            catch (Exception ex)
            {
                Log($"✗ 阶段2异常: {ex.Message}");
            }
        }

        private void BtnStage4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("[阶段4] 启动回复监听...");
                _launcher.Stage4_StartReplyListener();
                Log("✓ 阶段4完成");
            }
            catch (Exception ex)
            {
                Log($"✗ 阶段4异常: {ex.Message}");
            }
        }

        private void BtnShutdown_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("=== 开始清理 ===");
                _launcher.Shutdown();
                Log("✓ 清理完成");
            }
            catch (Exception ex)
            {
                Log($"✗ 清理异常: {ex.Message}");
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbCommands.SelectedItem == null)
                {
                    Log("✗ 请选择命令");
                    return;
                }

                var selected = (KeyValuePair<string, Command>)cmbCommands.SelectedItem;
                Command cmd = selected.Value;
                string additional = txtAdditional.Text;

                Log($"➤ 发送命令: {selected.Key} ({additional})");

                bool success = SharedMemoryFunc.CSend(
                    cmd, additional,
                    _launcher.ConnectStatus,
                    _launcher.GetHandles());  // 需要添加GetHandles方法

                Log(success ? "✓ 命令发送成功" : "✗ 命令发送失败");
            }
            catch (Exception ex)
            {
                Log($"✗ 发送异常: {ex.Message}");
            }
        }

        private async void BtnSendAndWait_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbCommands.SelectedItem == null)
                {
                    Log("✗ 请选择命令");
                    return;
                }

                var selected = (KeyValuePair<string, Command>)cmbCommands.SelectedItem;
                Command cmd = selected.Value;
                string additional = txtAdditional.Text;

                Log($"➤ 发送命令并等待回复: {selected.Key}");

                // 使用TaskCompletionSource等待回复
                var tcs = new TaskCompletionSource<(StructDataType, byte[])>();

                EventHandler<SharedMemoryFunc.ReplyReceivedEventArgs>? handler = null;
                handler = (s, args) =>
                {
                    _ = tcs.TrySetResult((args.DataType, args.Data));
                    _launcher.GetFunc().ReplyReceived -= handler;
                };

                _launcher.GetFunc().ReplyReceived += handler;

                bool sendSuccess = SharedMemoryFunc.CSend(
                    cmd, additional,
                    _launcher.ConnectStatus,
                    _launcher.GetHandles());

                if (!sendSuccess)
                {
                    _launcher.GetFunc().ReplyReceived -= handler;
                    Log("✗ 命令发送失败");
                    return;
                }

                // 等待回复（5秒超时）
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == tcs.Task)
                {
                    var (type, data) = await tcs.Task;
                    Log($"✓ 收到回复: 类型={type}, 数据大小={data?.Length ?? 0}");

                    // 显示回复信息
                    txtReplyType.Text = type.ToString();
                    txtReplySize.Text = data?.Length.ToString() ?? "0";
                    txtReplyError.Text = "无";
                }
                else
                {
                    _launcher.GetFunc().ReplyReceived -= handler;
                    Log("✗ 等待回复超时");
                    txtReplyError.Text = "超时";
                }
            }
            catch (Exception ex)
            {
                Log($"✗ 发送并等待异常: {ex.Message}");
            }
        }
    }
}