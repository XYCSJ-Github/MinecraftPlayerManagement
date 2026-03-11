using g_mpm.Enums;
using g_mpm.Structs;
using System.Diagnostics;
using Smc = g_mpm.SharedMemoryConfig.SharedMemoryConfig;

namespace g_mpm
{
    public class SharedMemoryLauncher(Smc? config = null)
    {
        private readonly Smc _config = config ?? new Smc();
        private readonly SharedMemoryFunc _func = new SharedMemoryFunc();
        private Process? _cppProcess;
        private ConnectStatus _connectStatus = ConnectStatus.NOT_INITIALIZED;
        private HandlePtr _handles = new HandlePtr();
        private ProgramStatus _programStatus = ProgramStatus.STOP;

        // 状态属性
        public ConnectStatus ConnectStatus => _connectStatus;
        public ProgramStatus ProgramStatus => _programStatus;
        public bool IsRunning => _cppProcess != null && !_cppProcess.HasExited;

        /// <summary>
        /// 额外的辅助方法
        /// </summary>
        public HandlePtr GetHandles() => _handles;
        public SharedMemoryFunc GetFunc() => _func;

        /// <summary>
        /// 阶段1：初始化共享内存
        /// </summary>
        public bool Stage1_InitializeSharedMemory()
        {
            try
            {
                Console.WriteLine("[Stage1] 初始化共享内存...");

                bool result = SharedMemoryFunc.InitializeSharedMemory(
                    _config, ref _connectStatus, ref _handles);

                if (result)
                {
                    Console.WriteLine("[Stage1] 共享内存初始化成功");
                    return true;
                }
                else
                {
                    Console.WriteLine("[Stage1] 共享内存初始化失败");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Stage1] 异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 阶段2：等待C++程序就绪
        /// </summary>
        public async Task<bool> Stage2_WaitForCppReadyAsync()
        {
            try
            {
                Console.WriteLine("[Stage2] 等待C++程序就绪...");

                // 等待初始化事件
                uint waitResult = WinAPI.WinAPI.WaitForSingleObject(
                    _handles._hInitEvent, (uint)_config.InitTimeout);

                if (waitResult == 0)
                {
                    _connectStatus = ConnectStatus.CONNECTED;
                    Console.WriteLine("[Stage2] C++程序就绪");
                    return true;
                }
                else
                {
                    Console.WriteLine("[Stage2] C++程序就绪超时");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Stage2] 异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 阶段3：启动C++进程
        /// </summary>
        public bool Stage3_StartCppProcess()
        {
            try
            {
                Console.WriteLine("[Stage3] 启动C++进程...");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "mpm.exe",
                    Arguments = "bg",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _cppProcess = SharedMemoryFunc.StartProcess(
                    ref _programStatus, startInfo);

                if (_cppProcess != null)
                {
                    // 订阅输出
                    _cppProcess.OutputDataReceived += (s, e) =>
                        Console.WriteLine($"[C++输出] {e.Data}");
                    _cppProcess.BeginOutputReadLine();

                    _cppProcess.ErrorDataReceived += (s, e) =>
                        Console.WriteLine($"[C++错误] {e.Data}");
                    _cppProcess.BeginErrorReadLine();

                    Console.WriteLine("[Stage3] C++进程启动成功");
                    return true;
                }
                else
                {
                    Console.WriteLine("[Stage3] C++进程启动失败");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Stage3] 异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 阶段4：启动回复监听
        /// </summary>
        public void Stage4_StartReplyListener()
        {
            try
            {
                Console.WriteLine("[Stage4] 启动回复监听...");

                _func.StartReplyListener(_connectStatus, _handles);

                // 订阅事件
                _func.ReplyReceived += OnReplyReceived;
                _func.ErrorOccurred += OnErrorOccurred;
                _func.ConnectionStatusChanged += OnConnectionStatusChanged;

                Console.WriteLine("[Stage4] 回复监听启动成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Stage4] 异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 完整启动流程
        /// </summary>
        public async Task<bool> LaunchAsync()
        {
            // 阶段1：初始化共享内存
            if (!Stage1_InitializeSharedMemory())
                return false;

            // 阶段3：启动C++进程（阶段2需要C++进程启动后才能完成）
            if (!Stage3_StartCppProcess())
                return false;

            // 阶段2：等待C++就绪
            if (!await Stage2_WaitForCppReadyAsync())
                return false;

            // 阶段4：启动监听
            Stage4_StartReplyListener();

            return true;
        }

        /// <summary>
        /// 关闭清理
        /// </summary>
        public void Shutdown()
        {
            Console.WriteLine("[Shutdown] 开始清理...");

            // 停止监听
            _func.StopReplyListener();

            // 发送退出命令
            if (_connectStatus == ConnectStatus.CONNECTED)
            {
                SharedMemoryFunc.SendExitCommand(ref _connectStatus, ref _handles);
            }

            // 强制终止进程
            if (_cppProcess != null && !_cppProcess.HasExited)
            {
                _func.ForceTerminateCppProcess(_cppProcess);
            }

            // 清理资源
            SharedMemoryFunc.Cleanup(ref _handles, ref _connectStatus);

            Console.WriteLine("[Shutdown] 清理完成");
        }

        // 事件处理方法
        private void OnReplyReceived(object sender, SharedMemoryFunc.ReplyReceivedEventArgs e)
        {
            Console.WriteLine($"[事件] 收到回复: 类型={e.DataType}, 数据大小={e.Data?.Length ?? 0}");
        }

        private void OnErrorOccurred(object sender, ErrorEventArgs e)
        {
            Console.WriteLine($"[事件] 错误: {e.ErrorMessage}");
        }

        private void OnConnectionStatusChanged(object sender, SharedMemoryFunc.ConnectionStatusChangedEventArgs e)
        {
            Console.WriteLine($"[事件] 状态变更: {e.OldStatus} -> {e.NewStatus}");
            _connectStatus = e.NewStatus;
        }
    }
}
