// SharedMemoryLauncher.cs - 修复启动器和事件处理
using g_mpm.Enums;
using g_mpm.Structs;
using System.Diagnostics;
using Smc = g_mpm.SharedMemoryConfig.SharedMemoryConfig;

namespace g_mpm
{
    public class SharedMemoryLauncher : IDisposable
    {
        private readonly Smc _config;
        private readonly SharedMemoryFunc _func;
        private Process? _cppProcess;
        private ConnectStatus _connectStatus = ConnectStatus.NOT_INITIALIZED;
        private HandlePtr _handles;
        private ProgramStatus _programStatus = ProgramStatus.STOP;

        // 公开事件
        public event EventHandler<SharedMemoryFunc.ReplyReceivedEventArgs>? ReplyReceived;
        public event EventHandler<SharedMemoryFunc.ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;
        public event EventHandler<ErrorEventArgs>? ErrorOccurred;
        public event EventHandler<SharedMemoryFunc.OutputReceivedEventArgs>? OutputReceived;
        public event EventHandler<SharedMemoryFunc.ProgramStatusChangedEventArgs>? ProgramStatusChanged;

        // 状态属性
        public ConnectStatus ConnectStatus => _connectStatus;
        public ProgramStatus ProgramStatus => _programStatus;
        public bool IsRunning => _cppProcess != null && !_cppProcess.HasExited;
        public HandlePtr Handles => _handles;
        public SharedMemoryFunc Func => _func;

        public SharedMemoryLauncher(Smc? config = null)
        {
            _config = config ?? new Smc();
            _func = new SharedMemoryFunc();
            _handles = new HandlePtr();

            // 订阅内部事件
            _func.ReplyReceived += OnReplyReceived;
            _func.ConnectionStatusChanged += OnConnectionStatusChanged;
            _func.ErrorOccurred += OnErrorOccurred;
            _func.OutputReceived += OnOutputReceived;
            _func.ProgramStatusChanged += OnProgramStatusChanged; // 确保这行存在
        }

        private void OnProgramStatusChanged(object? sender, SharedMemoryFunc.ProgramStatusChangedEventArgs e)
        {
            _programStatus = e.NewStatus;

            // 当程序状态变为 STOP 时，更新连接状态
            if (e.NewStatus == ProgramStatus.STOP)
            {
                // 如果是正常退出，将连接状态设为 NOT_CONNECTED 而不是 NOT_INITIALIZED
                _connectStatus = ConnectStatus.NOT_CONNECTED;

                // 触发连接状态变更事件
                ConnectionStatusChanged?.Invoke(this, new SharedMemoryFunc.ConnectionStatusChangedEventArgs
                {
                    OldStatus = e.OldStatus == ProgramStatus.RUNNING ? ConnectStatus.CONNECTED : _connectStatus,
                    NewStatus = ConnectStatus.NOT_CONNECTED
                });
            }

            ProgramStatusChanged?.Invoke(this, e);
        }

        // 修改 Shutdown 方法，添加正常退出标记
        public void Shutdown(bool isNormalShutdown = true)
        {
            // 停止监听
            _func.StopReplyListener();

            // 发送退出命令
            if (_connectStatus == ConnectStatus.CONNECTED)
            {
                SharedMemoryFunc.SendExitCommand(ref _connectStatus, ref _handles);

                if (isNormalShutdown)
                {
                    // 正常关闭：等待进程自己退出
                    Thread.Sleep(1000); // 给进程一点时间退出
                }
            }

            // 强制终止进程（如果还在运行）
            if (_cppProcess != null && !_cppProcess.HasExited)
            {
                if (isNormalShutdown)
                {
                    // 正常关闭时，再给一点时间
                    if (!_cppProcess.WaitForExit(2000))
                    {
                        _func.ForceTerminateCppProcess(_cppProcess);
                    }
                }
                else
                {
                    // 非正常关闭时直接强制终止
                    _func.ForceTerminateCppProcess(_cppProcess);
                }

                _cppProcess?.Dispose();
                _cppProcess = null;
            }

            // 只有在非正常关闭时才清理共享内存资源
            // 正常关闭时，C++已经清理过了
            if (!isNormalShutdown)
            {
                SharedMemoryFunc.Cleanup(ref _handles, ref _connectStatus);
            }
            else
            {
                // 只是重置状态，不清理Windows内核对象（因为C++已经清理了）
                _handles.sharedMemoryCommand = IntPtr.Zero;
                _connectStatus = ConnectStatus.NOT_CONNECTED;
            }
        }

        #region 事件转发

        private void OnReplyReceived(object? sender, SharedMemoryFunc.ReplyReceivedEventArgs e)
        {
            ReplyReceived?.Invoke(this, e);
        }

        private void OnConnectionStatusChanged(object? sender, SharedMemoryFunc.ConnectionStatusChangedEventArgs e)
        {
            _connectStatus = e.NewStatus;
            ConnectionStatusChanged?.Invoke(this, e);
        }

        private void OnErrorOccurred(object? sender, ErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        private void OnOutputReceived(object? sender, SharedMemoryFunc.OutputReceivedEventArgs e)
        {
            OutputReceived?.Invoke(this, e);
        }

        #endregion

        /// <summary>
        /// 阶段1：初始化共享内存
        /// </summary>
        public bool Stage1_InitializeSharedMemory()
        {
            try
            {
                return SharedMemoryFunc.InitializeSharedMemory(_config, ref _connectStatus, ref _handles);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(this, new ErrorEventArgs { ErrorMessage = $"Stage1 failed: {ex.Message}", Exception = ex });
                return false;
            }
        }

        /// <summary>
        /// 阶段2：启动C++进程
        /// </summary>
        public bool Stage2_StartCppProcess(string? arguments = null)
        {
            try
            {
                ProcessStartInfo startInfo;
                string[] args = Environment.GetCommandLineArgs();
                bool isCreateWindow = args.Length > 1 && args[1] == "createwindow";
                if (isCreateWindow)
                {
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "mpm.exe",
                        Arguments = "bg",
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                    };
                }
                else
                {
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "mpm.exe",
                        Arguments = "bg",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };
                }

                _cppProcess = _func.StartProcess(ref _programStatus, startInfo);

                if (_cppProcess != null)
                {
                    _func.StartProcessMonitor(_cppProcess);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(this, new ErrorEventArgs { ErrorMessage = $"Stage2 failed: {ex.Message}", Exception = ex });
                return false;
            }
        }

        /// <summary>
        /// 阶段3：等待C++程序就绪
        /// </summary>
        public async Task<bool> Stage3_WaitForCppReadyAsync()
        {
            try
            {
                return await _func.WaitForCppReadyAsync(_handles, _config.InitTimeout);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(this, new ErrorEventArgs { ErrorMessage = $"Stage3 failed: {ex.Message}", Exception = ex });
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
                _connectStatus = ConnectStatus.CONNECTED;
                _func.StartReplyListener(_connectStatus, _handles);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(this, new ErrorEventArgs { ErrorMessage = $"Stage4 failed: {ex.Message}", Exception = ex });
            }
        }

        /// <summary>
        /// 完整启动流程
        /// </summary>
        public async Task<bool> LaunchAsync(string? processArguments = null)
        {
            // 阶段1：初始化共享内存
            if (!Stage1_InitializeSharedMemory())
                return false;

            // 阶段2：启动C++进程
            if (!Stage2_StartCppProcess(processArguments))
                return false;

            // 阶段3：等待C++就绪
            if (!await Stage3_WaitForCppReadyAsync())
                return false;

            // 阶段4：启动监听
            Stage4_StartReplyListener();

            return true;
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        public bool SendCommand(Command command, string additional = "")
        {
            return SharedMemoryFunc.CSend(command, additional, _connectStatus, _handles);
        }

        /// <summary>
        /// 发送命令并等待回复
        /// </summary>
        public async Task<(bool Success, StructDataType DataType, byte[]? Data, string? Error)>
            SendCommandAndWaitAsync(Command command, string additional = "", int timeoutMs = 5000)
        {
            var tcs = new TaskCompletionSource<(StructDataType, byte[]?, string?)>();

            EventHandler<SharedMemoryFunc.ReplyReceivedEventArgs>? handler = null;
            handler = (s, e) =>
            {
                _func.ReplyReceived -= handler;
                tcs.TrySetResult((e.DataType, e.Data, e.ErrorInfo));
            };

            _func.ReplyReceived += handler;

            try
            {
                if (!SendCommand(command, additional))
                {
                    _func.ReplyReceived -= handler;
                    return (false, StructDataType.EMPTY_STRUCT, null, "Send failed");
                }

                var timeoutTask = Task.Delay(timeoutMs);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == tcs.Task)
                {
                    var (type, data, error) = await tcs.Task;
                    return (true, type, data, error);
                }
                else
                {
                    _func.ReplyReceived -= handler;
                    return (false, StructDataType.EMPTY_STRUCT, null, "Timeout");
                }
            }
            catch (Exception ex)
            {
                _func.ReplyReceived -= handler;
                return (false, StructDataType.EMPTY_STRUCT, null, ex.Message);
            }
        }

        /// <summary>
        /// 关闭清理
        /// </summary>
        public void Shutdown()
        {
            // 停止监听
            _func.StopReplyListener();

            // 发送退出命令
            if (_connectStatus == ConnectStatus.CONNECTED)
            {
                SharedMemoryFunc.SendExitCommand(ref _connectStatus, ref _handles);
                Thread.Sleep(800); // 给进程一点时间退出
            }

            // 强制终止进程
            if (_cppProcess != null && !_cppProcess.HasExited)
            {
                _func.ForceTerminateCppProcess(_cppProcess);
                _cppProcess?.Dispose();
                _cppProcess = null;
            }

            // 清理资源
            SharedMemoryFunc.Cleanup(ref _handles, ref _connectStatus);
        }

        public void Dispose()
        {
            Shutdown();
            GC.SuppressFinalize(this);
        }
    }
}