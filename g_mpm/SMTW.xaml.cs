// SMTW.xaml.cs - 完整的GUI实现
using g_mpm.Enums;
using g_mpm.Structs;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Smc = g_mpm.SharedMemoryConfig.SharedMemoryConfig;

namespace g_mpm
{
    public partial class SMTW : Window
    {
        private SharedMemoryLauncher _launcher;
        private DispatcherTimer _statusTimer;
        private Dictionary<string, Command> _commandDict;
        private readonly object _logLock = new object();

        public SMTW()
        {
            InitializeComponent();
            InitializeCommands();
            InitializeLauncher();
            SetupStatusTimer();
            ParseCommandLineArgs();
            this.Closing += Window_Closing;
            WorldComdo.IsEnabled = false;
            PlayerComdo.IsEnabled = false;
        }

        private void InitializeCommands()
        {
            _commandDict = new Dictionary<string, Command>
            {
                ["空命令"] = Command.EMPTY_COMMAND,
                ["设置路径"] = Command.M_SET_PATH,
                ["打开存档"] = Command.OPEN_WORLD,
                ["打开玩家"] = Command.OPEN_PLAYER,
                ["列出存档"] = Command.LIST_WORLD,
                ["列出玩家"] = Command.LIST_PLAYER,
                ["删除玩家"] = Command.DEL_PLAYER,
                ["删除存档"] = Command.DEL_WORLD,
                ["删除JSON记录"] = Command.DEL_JS,
                ["删除指定世界玩家"] = Command.DEL_PW,
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

            // 订阅所有事件
            _launcher.ReplyReceived += OnReplyReceived;
            _launcher.ErrorOccurred += OnErrorOccurred;
            _launcher.OutputReceived += OnOutputReceived;
            _launcher.ProgramStatusChanged += OnProgramStatusChanged;
            _launcher.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        private void ParseCommandLineArgs()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                Log($"[启动参数] {string.Join(" ", args.Skip(1))}");
                txtAdditional.Text = string.Join(" ", args.Skip(1));
            }
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
            Dispatcher.Invoke(() =>
            {
                txtConnectStatus.Text = _launcher?.ConnectStatus.ToString() ?? "未知";
                txtProgramStatus.Text = _launcher?.ProgramStatus.ToString() ?? "未知";

                txtConnectStatus.Foreground = _launcher?.ConnectStatus switch
                {
                    ConnectStatus.NOT_INITIALIZED => Brushes.Gray,
                    ConnectStatus.INITIALIZED => Brushes.Orange,
                    ConnectStatus.CONNECTED => Brushes.Green,
                    _ => Brushes.Gray
                };

                txtProgramStatus.Foreground = _launcher?.IsRunning == true ? Brushes.Green : Brushes.Red;
            });
        }

        // SMTW.xaml.cs - 增强日志显示，支持中文字符
        /// <summary>
        /// 日志输出（修复乱码显示）
        /// </summary>
        private void Log(string message, bool isError = false)
        {
            Dispatcher.Invoke(() =>
            {
                lock (_logLock)
                {
                    try
                    {
                        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                        string prefix = isError ? "❌ " : "";

                        // 确保消息不为空
                        message = message ?? "";

                        // 构建完整日志行
                        string logLine = $"[{timestamp}] {prefix}{message}\n";

                        // 添加到文本框
                        txtLog.AppendText(logLine);

                        // 自动滚动
                        if (chkAutoScroll.IsChecked == true)
                        {
                            txtLog.ScrollToEnd();
                        }

                        // 同时输出到调试窗口（便于调试）
                        Debug.WriteLine(logLine.TrimEnd());
                    }
                    catch (Exception ex)
                    {
                        // 如果日志写入失败，至少尝试写入错误信息
                        try
                        {
                            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] [日志错误] {ex.Message}\n");
                        }
                        catch { }
                    }
                }
            });
        }

        /// <summary>
        /// 处理进程输出（增强版）
        /// </summary>
        private void OnOutputReceived(object? sender, SharedMemoryFunc.OutputReceivedEventArgs e)
        {
            string prefix = e.IsError ? "[C++错误]" : "[C++输出]";

            // 处理可能的乱码
            string displayData = e.Data ?? "";

            // 如果是字节数组形式，尝试转换
            if (displayData.StartsWith("System.Byte[]"))
            {
                displayData = "接收到二进制数据";
            }

            Log($"{prefix} {displayData}", e.IsError);
        }

        #region 事件处理

        private void OnReplyReceived(object? sender, SharedMemoryFunc.ReplyReceivedEventArgs e)
        {
            Log($"[回复] 类型={e.DataType}, 成功={e.IsSuccess}, 错误={e.ErrorInfo ?? "无"}。加载模式={e.mode}");

            Dispatcher.Invoke(() =>
            {
                txtReplyType.Text = e.DataType.ToString();
                txtReplySize.Text = e.Data?.Length.ToString() ?? "0";
                txtReplyError.Text = e.ErrorInfo ?? (e.IsSuccess ? "成功" : "失败");
                txtReplyError.Foreground = e.IsSuccess ? Brushes.Green : Brushes.Red;
                if (e.mode != LoadMode.KEEP)
                {
                    txtLoadMode.Text = e.mode.ToString();
                }
                if (e.Title != "")
                {
                    txtTitleName.Text = e.Title.ToString();
                }


                switch (e.DataType)
                {
                    case StructDataType.WDNL:
                        if (e.Data != null)
                        {
                            WorldDirectoriesNameList wunl = WorldDirectoriesNameList.FromBytes(e.Data);
                            Dictionary<string, string> _WorldList = new Dictionary<string, string>();
                            for (int i = 0; i < wunl.world_name_list.Count; i++)
                            {
                                _WorldList.Add(wunl.world_name_list[i], wunl.world_directory_list[i]);
                            }

                            WorldComdo.ItemsSource = _WorldList;
                            WorldComdo.DisplayMemberPath = "Key";
                            WorldComdo.SelectedValuePath = "Value";
                            WorldComdo.SelectedIndex = 0;

                            WorldComdo.IsEnabled = true;
                        }
                        else
                        {
                            Log("传来的数组是空的", true);
                        }

                        break;

                    case StructDataType.UI:
                        if (e.Data != null)
                        {
                            List<UserInfo> ui = UserInfoListSerializer.FromBytes(e.Data);
                            Dictionary<string, string> _PlayerList = new Dictionary<string, string>();
                            for (int i = 0; i < ui.Count; i++)
                            {
                                _PlayerList.Add(ui[i].user_name, ui[i].uuid);
                            }
                            PlayerComdo.ItemsSource = _PlayerList;
                            PlayerComdo.DisplayMemberPath = "Key";
                            PlayerComdo.SelectedValuePath = "Value";
                            PlayerComdo.SelectedIndex = 0;

                            PlayerComdo.IsEnabled = true;
                        }
                        else
                        {
                            Log("传来的数组是空的", true);
                        }

                        break;

                    case StructDataType.PIWIL:
                        if (e.Data != null)
                        {
                            PlayerInWorldInfoList piwil = PlayerInWorldInfoList.FromBytes(e.Data);
                        }
                        break;

                    default:
                        break;
                }

            });

            if (e.Data != null && e.Data.Length > 0 && _launcher.Func != null)
            {
                Log($"[数据] 前64字节: {BitConverter.ToString(e.Data.Take(64).ToArray()).Replace("-", " ")}");
            }

        }

        private void OnErrorOccurred(object? sender, ErrorEventArgs e)
        {
            Log($"[错误] {e.ErrorMessage}", true);
            if (e.Exception != null)
            {
                Log($"[异常详情] {e.Exception}", true);
            }
        }

        private void OnProgramStatusChanged(object? sender, SharedMemoryFunc.ProgramStatusChangedEventArgs e)
        {
            Log($"[程序状态] {e.OldStatus} -> {e.NewStatus}");
        }

        private void OnConnectionStatusChanged(object? sender, SharedMemoryFunc.ConnectionStatusChangedEventArgs e)
        {
            Log($"[连接状态] {e.OldStatus} -> {e.NewStatus}");
        }

        #endregion

        #region 按钮事件

        private async void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("=== 开始完整启动流程 ===");
                btnLaunch.IsEnabled = false;

                string? args = !string.IsNullOrWhiteSpace(txtAdditional.Text) ? txtAdditional.Text : null;
                bool success = await _launcher.LaunchAsync(args);

                Log(success ? "✓ 完整启动流程完成" : "✗ 完整启动流程失败");
            }
            catch (Exception ex)
            {
                Log($"✗ 启动异常: {ex.Message}", true);
            }
            finally
            {
                btnLaunch.IsEnabled = true;
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
                Log($"✗ 阶段1异常: {ex.Message}", true);
            }
        }

        private void BtnStage2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("[阶段2] 启动C++进程...");
                string? args = !string.IsNullOrWhiteSpace(txtAdditional.Text) ? txtAdditional.Text : null;
                bool success = _launcher.Stage2_StartCppProcess(args);
                Log(success ? "✓ 阶段2完成" : "✗ 阶段2失败");
            }
            catch (Exception ex)
            {
                Log($"✗ 阶段2异常: {ex.Message}", true);
            }
        }

        private async void BtnStage3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("[阶段3] 等待C++就绪...");
                bool success = await _launcher.Stage3_WaitForCppReadyAsync();
                Log(success ? "✓ 阶段3完成" : "✗ 阶段3失败");
            }
            catch (Exception ex)
            {
                Log($"✗ 阶段3异常: {ex.Message}", true);
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
                Log($"✗ 阶段4异常: {ex.Message}", true);
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
                Log($"✗ 清理异常: {ex.Message}", true);
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
                string additional = txtAdditional.Text;

                Log($"➤ 发送命令: {selected.Key} ({additional})");

                bool success = _launcher.SendCommand(selected.Value, additional);
                Log(success ? "✓ 命令发送成功" : "✗ 命令发送失败");
                txtAdditional.Text = "";
            }
            catch (Exception ex)
            {
                Log($"✗ 发送异常: {ex.Message}", true);
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
                string additional = txtAdditional.Text;

                Log($"➤ 发送命令并等待回复: {selected.Key}");

                var (success, type, data, error) = await _launcher.SendCommandAndWaitAsync(
                    selected.Value, additional, 5000);

                if (success)
                {
                    Log($"✓ 收到回复: 类型={type}, 数据大小={data?.Length ?? 0}, 错误={error ?? "无"}");
                }
                else
                {
                    Log($"✗ 发送并等待失败: {error ?? "未知错误"}");
                }
            }
            catch (Exception ex)
            {
                Log($"✗ 发送并等待异常: {ex.Message}", true);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _statusTimer?.Stop();
            _launcher?.Dispose();
            base.OnClosed(e);
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 清空日志文本框
                txtLog.Clear();

                // 添加清空记录
                Log("=== 日志已清空 ===");

                // 可选：同时清空回复显示区的数据
                txtReplyType.Text = "无";
                txtReplySize.Text = "0";
                txtReplyError.Text = "无";
                txtReplyError.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                // 异常处理
                MessageBox.Show($"清空日志时发生错误: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private void Pathchoose(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.RootDirectory = "C:\\Desktop\\";
            openFolderDialog.ShowDialog();
            txtAdditional.Text = openFolderDialog.FolderName;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _launcher.Dispose();
        }

        private void IsEnable_Checked(object sender, RoutedEventArgs e)
        {
            btnStage1.IsEnabled = false;
            btnStage2.IsEnabled = false;
            btnStage3.IsEnabled = false;
            btnStage4.IsEnabled = false;
        }

        private void IsEnable_Unchecked(object sender, RoutedEventArgs e)
        {
            btnStage1.IsEnabled = true;
            btnStage2.IsEnabled = true;
            btnStage3.IsEnabled = true;
            btnStage4.IsEnabled = true;
        }

        private void cmbCommands_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Command command = (Command)cmbCommands.SelectedValue;
            switch (command)
            {
                case Command.DEL_PLAYER:
                    {
                        KeyValuePair<string, string> key = (KeyValuePair<string, string>)PlayerComdo.SelectedItem;
                        txtAdditional.Text = key.Key;
                        break;
                    }

                case Command.DEL_WORLD:
                    {
                        KeyValuePair<string, string> key = (KeyValuePair<string, string>)WorldComdo.SelectedItem;
                        txtAdditional.Text = key.Key;
                        break;
                    }

                case Command.DEL_JS:
                    {
                        KeyValuePair<string, string> key = (KeyValuePair<string, string>)PlayerComdo.SelectedItem;
                        txtAdditional.Text = key.Key;
                        break;
                    }

                case Command.DEL_PW:
                    {
                        string tmp;
                        KeyValuePair<string, string> key = (KeyValuePair<string, string>)PlayerComdo.SelectedItem;
                        tmp = key.Key;
                        tmp += " ";
                        KeyValuePair<string, string> keya = (KeyValuePair<string, string>)WorldComdo.SelectedItem;
                        tmp += keya.Key;

                        txtAdditional.Text = tmp;

                        break;
                    }

                default:
                    break;
            }
        }

        private void PlayerComdo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            KeyValuePair<string, string> key = (KeyValuePair<string, string>)PlayerComdo.SelectedItem;
            txtAdditional.Text = key.Key;
        }

        private void WorldComdo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            KeyValuePair<string, string> key = (KeyValuePair<string, string>)WorldComdo.SelectedItem;
            txtAdditional.Text = key.Key;
        }
    }
}