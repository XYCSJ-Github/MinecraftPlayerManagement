using g_mpm.Enums;
using g_mpm.Structs;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Smc = g_mpm.SharedMemoryConfig.SharedMemoryConfig;

namespace g_mpm
{
    public partial class IntorWindow : Window
    {
        // 共享内存相关字段
        private SharedMemoryLauncher _launcher;
        private DispatcherTimer _statusTimer;

        public IntorWindow()
        {
            InitializeComponent();

            // 初始化共享内存组件
            InitializeLauncher();
            SetupStatusTimer();

            // 启动时自动完成初始化
            this.Loaded += async (s, e) => await InitializeOnStartup();
            this.Closing += Window_Closing;

            Storyboard sb = (Storyboard)this.Resources["Intor"];
            sb.SpeedRatio = 0.8;
            sb.Begin();
        }

        #region 共享内存初始化

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
            // 状态更新逻辑
        }

        /// <summary>
        /// 启动时自动初始化
        /// </summary>
        private async System.Threading.Tasks.Task InitializeOnStartup()
        {
            // 执行完整的启动流程
            bool success = await LaunchAsync();

            if (success)
            {
                // 初始化成功后的处理
                Debug.WriteLine("共享内存初始化成功");

                // 可选：发送初始命令，例如列出存档
                // SendCommand(Command.LIST_WORLD);
            }
            else
            {
                // 初始化失败后的处理
                Debug.WriteLine("共享内存初始化失败");
            }
        }

        #endregion

        #region 事件处理

        private void OnOutputReceived(object? sender, SharedMemoryFunc.OutputReceivedEventArgs e)
        {
            // 处理C++输出
        }

        private void OnReplyReceived(object? sender, SharedMemoryFunc.ReplyReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // 处理回复数据
                if (e.Title != "")
                {
                    Path.Text = e.Title;
                }

                switch (e.DataType)
                {
                    case StructDataType.WDNL:
                        if (e.Data != null)
                        {
                            WorldDirectoriesNameList wunl = WorldDirectoriesNameList.FromBytes(e.Data);
                        }
                        break;

                    case StructDataType.UI:
                        if (e.Data != null)
                        {
                            List<UserInfo> ui = UserInfoListSerializer.FromBytes(e.Data);
                        }
                        break;

                    case StructDataType.PIWIL:
                        if (e.Data != null)
                        {
                            PlayerInWorldInfoList piwil = PlayerInWorldInfoList.FromBytes(e.Data);
                        }
                        break;
                }
            });
        }

        private void OnErrorOccurred(object? sender, ErrorEventArgs e)
        {
            // 处理错误
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"错误: {e.ErrorMessage}");
            });
        }

        private void OnProgramStatusChanged(object? sender, SharedMemoryFunc.ProgramStatusChangedEventArgs e)
        {
            // 处理程序状态变化
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"程序状态: {e.OldStatus} -> {e.NewStatus}");
            });
        }

        private void OnConnectionStatusChanged(object? sender, SharedMemoryFunc.ConnectionStatusChangedEventArgs e)
        {
            // 处理连接状态变化
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"连接状态: {e.OldStatus} -> {e.NewStatus}");
            });
        }

        #endregion

        #region 公共方法 - 供界面调用

        /// <summary>
        /// 启动完整流程
        /// </summary>
        public async System.Threading.Tasks.Task<bool> LaunchAsync(string? args = null)
        {
            try
            {
                return await _launcher.LaunchAsync(args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 阶段1：初始化共享内存
        /// </summary>
        public bool Stage1_InitializeSharedMemory()
        {
            try
            {
                return _launcher.Stage1_InitializeSharedMemory();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"阶段1异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 阶段2：启动C++进程
        /// </summary>
        public bool Stage2_StartCppProcess(string? args = null)
        {
            try
            {
                return _launcher.Stage2_StartCppProcess(args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"阶段2异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 阶段3：等待C++就绪
        /// </summary>
        public async System.Threading.Tasks.Task<bool> Stage3_WaitForCppReadyAsync()
        {
            try
            {
                return await _launcher.Stage3_WaitForCppReadyAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"阶段3异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 阶段4：启动回复监听
        /// </summary>
        public bool Stage4_StartReplyListener()
        {
            try
            {
                _launcher.Stage4_StartReplyListener();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"阶段4异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        public bool SendCommand(Command command, string additional = "")
        {
            try
            {
                return _launcher.SendCommand(command, additional);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"发送命令异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送命令并等待回复
        /// </summary>
        public async System.Threading.Tasks.Task<(bool success, StructDataType type, byte[]? data, string? error)>
            SendCommandAndWaitAsync(Command command, string additional = "", int timeoutMs = 5000)
        {
            try
            {
                return await _launcher.SendCommandAndWaitAsync(command, additional, timeoutMs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"发送命令并等待异常: {ex.Message}");
                return (false, StructDataType.EMPTY_STRUCT, null, ex.Message);
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Shutdown()
        {
            try
            {
                _launcher?.Shutdown();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清理异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取Launcher实例
        /// </summary>
        public SharedMemoryLauncher GetLauncher() => _launcher;

        /// <summary>
        /// 检查是否正在运行
        /// </summary>
        public bool IsRunning => _launcher?.IsRunning ?? false;

        /// <summary>
        /// 获取连接状态
        /// </summary>
        public ConnectStatus ConnectStatus => _launcher?.ConnectStatus ?? ConnectStatus.NOT_INITIALIZED;

        /// <summary>
        /// 获取程序状态
        /// </summary>
        public ProgramStatus ProgramStatus => _launcher?.ProgramStatus ?? ProgramStatus.STOP;

        #endregion

        #region 清理

        protected override void OnClosed(EventArgs e)
        {
            _statusTimer?.Stop();
            _launcher?.Dispose();
            base.OnClosed(e);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _launcher.Dispose();
        }

        #endregion

        private void button_Click(object sender, RoutedEventArgs e)
        {
            button.IsEnabled = false;
            Back.IsEnabled = true;

            Storyboard sb = (Storyboard)this.Resources["AbortIntor"];

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += (s, args) =>
            {
                sb.Begin(this);
                timer.Stop();
            };
            timer.Start();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Back.IsEnabled = false;
            button.IsEnabled = true;

            Storyboard sb = (Storyboard)this.Resources["AbortIntorB"];

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += (s, args) =>
            {
                sb.Begin(this);
                timer.Stop();
            };
            timer.Start();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SendCommand(Command.M_SET_PATH, Path.Text.ToString());
        }

        private void Choose_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog open = new OpenFolderDialog();
            open.ShowDialog();
            Path.Text = open.FolderName;
        }
    }
}