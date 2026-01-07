// SharedMemoryCreator.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace g_mpm
{
    /// <summary>
    /// WPF共享内存创建者（服务端）
    /// </summary>
    public class SharedMemoryCreator : IDisposable
    {
        // 配置
        private readonly SharedMemoryConfig _config;

        // 句柄
        private IntPtr _hMapFile = IntPtr.Zero;
        private IntPtr _hMutex = IntPtr.Zero;
        private IntPtr _hEventFromAToB = IntPtr.Zero;
        private IntPtr _hEventFromBToA = IntPtr.Zero;
        private IntPtr _hInitEvent = IntPtr.Zero;
        private IntPtr _pSharedData = IntPtr.Zero;

        // 状态
        private bool _isInitialized = false;
        private bool _disposed = false;
        private CancellationTokenSource? _monitorCts;

        // C++进程
        private Process? _cppProcess;
        private bool _cppProcessRunning = false;

        // 统计
        private int _messagesSent = 0;
        private int _repliesReceived = 0;
        private DateTime _startTime;

        // 事件
        public event EventHandler<MessageEventArgs> MessageSent;
        public event EventHandler<MessageEventArgs> ReplyReceived;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;
        public event EventHandler<bool> ConnectionStatusChanged;
        public event EventHandler<string> StatusMessage;
        public event EventHandler<CppProcessEventArgs> CppProcessStatusChanged;

        // 新增：就绪信号处理
        private readonly ManualResetEventSlim _readySignal = new ManualResetEventSlim(false);
        private string _initialReadyMessage = string.Empty;
        private readonly object _readyLock = new object();

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized && !_disposed;

        /// <summary>
        /// C++进程是否在运行
        /// </summary>
        public bool IsCppProcessRunning => _cppProcessRunning;

        /// <summary>
        /// C++进程ID
        /// </summary>
        public int CppProcessId => _cppProcess?.Id ?? -1;

        /// <summary>
        /// 已发送消息数量
        /// </summary>
        public int MessagesSent => _messagesSent;

        /// <summary>
        /// 已接收回复数量
        /// </summary>
        public int RepliesReceived => _repliesReceived;

        /// <summary>
        /// 运行时间
        /// </summary>
        public TimeSpan Uptime => DateTime.Now - _startTime;

        /// <summary>
        /// 构造函数
        /// </summary>
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable IDE0290 // 使用主构造函数
        public SharedMemoryCreator(SharedMemoryConfig? config = null)
#pragma warning restore IDE0290 // 使用主构造函数
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
        {
            _config = config ?? new SharedMemoryConfig();
        }

        /// <summary>
        /// 初始化共享内存并启动C++进程
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (!_isInitialized)
            {
                try
                {
                    OnStatusMessage("正在初始化共享内存...");

                    // 创建初始化事件
                    _hInitEvent = CreateEvent(IntPtr.Zero, true, false, _config.InitEvent);
                    if (_hInitEvent == IntPtr.Zero)
                    {
                        OnError("创建初始化事件失败");
                        return false;
                    }

                    OnStatusMessage("正在创建共享内存...");

                    // 创建共享内存
                    _hMapFile = CreateFileMapping(
                        new IntPtr(-1),
                        IntPtr.Zero,
                        0x04, // PAGE_READWRITE
                        0,
                        (uint)Marshal.SizeOf<SharedDataBase>(),
                        _config.MemoryName);

                    if (_hMapFile == IntPtr.Zero)
                    {
                        OnError("创建共享内存失败");
                        CloseHandle(_hInitEvent);
                        _hInitEvent = IntPtr.Zero;
                        return false;
                    }

                    OnStatusMessage("正在映射共享内存...");

                    // 映射共享内存
                    _pSharedData = MapViewOfFile(
                        _hMapFile,
                        0xF001F, // FILE_MAP_ALL_ACCESS
                        0,
                        0,
                        (uint)Marshal.SizeOf<SharedDataBase>());

                    if (_pSharedData == IntPtr.Zero)
                    {
                        OnError("映射共享内存失败");
                        Cleanup();
                        return false;
                    }

                    OnStatusMessage("正在初始化共享数据...");

                    // 初始化共享数据
                    var initData = new SharedDataBase
                    {
                        MessageFromA = "",
                        ReplyFromB = "",
                        NewMessageFromA = false,
                        NewReplyFromB = false,
                        ExitFlag = false
                    };

                    Marshal.StructureToPtr(initData, _pSharedData, false);

                    OnStatusMessage("正在创建同步对象...");

                    // 创建同步对象
                    _hMutex = CreateMutex(IntPtr.Zero, false, _config.MutexName);
                    _hEventFromAToB = CreateEvent(IntPtr.Zero, false, false, _config.EventAToB);
                    _hEventFromBToA = CreateEvent(IntPtr.Zero, false, false, _config.EventBToA);

                    if (_hMutex == IntPtr.Zero || _hEventFromAToB == IntPtr.Zero || _hEventFromBToA == IntPtr.Zero)
                    {
                        OnError("创建同步对象失败");
                        Cleanup();
                        return false;
                    }

                    _isInitialized = true;
                    _startTime = DateTime.Now;

                    OnStatusMessage("共享内存初始化完成");
                    OnConnectionStatusChanged(true);

                    // 重置就绪信号
                    _readySignal.Reset();
                    _initialReadyMessage = string.Empty;

                    // 启动回复监控（在启动C++进程之前）
                    StartReplyMonitor();

                    // 启动C++进程
                    bool cppStarted = await StartCppProcessAsync();
                    if (!cppStarted)
                    {
                        OnError("启动C++进程失败");
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    OnError($"初始化异常: {ex.Message}", ex);
                    Cleanup();
                    return false;
                }
            }
            else
            {
                // 重置就绪信号
                _readySignal.Reset();
                _initialReadyMessage = string.Empty;

                // 启动回复监控（在启动C++进程之前）
                StartReplyMonitor();

                // 启动C++进程
                bool cppStarted = await StartCppProcessAsync();
                if (!cppStarted)
                {
                    OnError("启动C++进程失败");
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// 启动C++进程（mpm.exe）
        /// </summary>
        private async Task<bool> StartCppProcessAsync()
        {
            try
            {
                OnStatusMessage("正在启动C++进程 (mpm.exe)...");

                // 获取mpm.exe路径
                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mpm.exe");

                if (!File.Exists(exePath))
                {
                    // 尝试在父目录查找
#pragma warning disable CS8602 // 解引用可能出现空引用。
                    exePath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName, "mpm.exe");
#pragma warning restore CS8602 // 解引用可能出现空引用。

                    if (!File.Exists(exePath))
                    {
                        OnError($"找不到mpm.exe文件。请确保mpm.exe与程序在同一目录。\n搜索路径: {exePath}");
                        return false;
                    }
                }

                OnStatusMessage($"找到mpm程序: {exePath}");

                // 创建进程启动信息
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,  // 显示控制台窗口
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Arguments = "bg"
                };

                _cppProcess = new Process();
                _cppProcess.StartInfo = psi;
                _cppProcess.EnableRaisingEvents = true;

                // 订阅进程事件
                _cppProcess.Exited += (sender, args) =>
                {
                    _cppProcessRunning = false;
                    OnStatusMessage($"mpm进程已退出，退出代码: {_cppProcess.ExitCode}");
                    OnCppProcessStatusChanged(new CppProcessEventArgs(false, _cppProcess.ExitCode));

                    // 清理资源
                    _cppProcess?.Dispose();
                    _cppProcess = null;
                };

                // 输出重定向
                _cppProcess.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        OnStatusMessage($"mpm输出: {args.Data}");
                    }
                };

                _cppProcess.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        OnError($"mpm错误: {args.Data}");
                    }
                };

                // 启动进程
                bool started = _cppProcess.Start();
                if (!started)
                {
                    OnError("无法启动mpm进程");
                    return false;
                }

                _cppProcessRunning = true;

                // 开始异步读取输出
                _cppProcess.BeginOutputReadLine();
                _cppProcess.BeginErrorReadLine();

                OnStatusMessage($"mpm进程已启动 (PID: {_cppProcess.Id})");
                OnCppProcessStatusChanged(new CppProcessEventArgs(true, 0, _cppProcess.Id));

                // 等待进程初始化
                await Task.Delay(2000);

                // 通知C++进程初始化完成
                OnStatusMessage("通知mpm进程初始化完成...");
                bool signaled = SignalInitializationComplete();

                if (!signaled)
                {
                    OnWarning("发送初始化信号失败，但进程已启动");
                }
                else
                {
                    OnStatusMessage("已发送初始化完成信号给C++进程");
                }

                // 等待C++进程就绪回复（使用新的就绪检测方法）
                OnStatusMessage("等待mpm进程就绪...");
                var (readySuccess, readyReply) = await WaitForInitialReadyAsync(10000);
                
                if (readySuccess)
                {
                    OnStatusMessage($"mpm进程已就绪: {readyReply}");
                    return true;
                }
                else
                {
                    OnWarning($"等待mpm进程就绪失败: {readyReply}");
                    // 即使没有收到就绪回复，进程也可能正在运行
                    return _cppProcessRunning && !_cppProcess.HasExited;
                }
            }
            catch (Exception ex)
            {
                OnError($"启动mpm进程异常: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 停止C++进程
        /// </summary>
        public async Task<bool> StopCppProcessAsync()
        {
            try
            {
                if (_cppProcess == null || !_cppProcessRunning)
                {
                    OnStatusMessage("mpm进程未运行");
                    return true;
                }

                OnStatusMessage("正在停止mpm进程...");

                // 发送退出指令
                bool exitSent = SendExitCommand();
                if (exitSent)
                {
                    OnStatusMessage("已发送退出指令给mpm进程");

                    // 等待回复
                    await Task.Delay(1000);
                    var (replySuccess, reply) = await WaitForReplyAsync(3000);
                    if (replySuccess)
                    {
                        OnStatusMessage($"mpm进程回复: {reply}");
                    }

                    // 等待进程退出
                    bool exited = await WaitForProcessExitAsync(5000);
                    if (exited)
                    {
                        OnStatusMessage("mpm进程已优雅退出");
                        return true;
                    }
                }

                // 如果优雅退出失败，强制终止
                OnWarning("优雅退出失败，尝试强制终止...");
                return ForceTerminateCppProcess();
            }
            catch (Exception ex)
            {
                OnError($"停止mpm进程异常: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 等待进程退出
        /// </summary>
        private async Task<bool> WaitForProcessExitAsync(int timeoutMilliseconds)
        {
            try
            {
                if (_cppProcess == null) return true;

                var task = _cppProcess.WaitForExitAsync();
                var timeoutTask = Task.Delay(timeoutMilliseconds);

                var completedTask = await Task.WhenAny(task, timeoutTask);

                if (completedTask == task)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 强制终止C++进程
        /// </summary>
        private bool ForceTerminateCppProcess()
        {
            try
            {
                if (_cppProcess == null) return true;

                _cppProcess.Kill();
                _cppProcess.WaitForExit(3000);

                OnStatusMessage("C++进程已强制终止");
                return true;
            }
            catch (Exception ex)
            {
                OnError($"强制终止C++进程失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 通知客户端初始化完成
        /// </summary>
        public bool SignalInitializationComplete()
        {
            if (!IsInitialized || _hInitEvent == IntPtr.Zero)
            {
                OnError("无法发送初始化信号: 未初始化");
                return false;
            }

            try
            {
                bool success = SetEvent(_hInitEvent);
                if (success)
                {
                    OnStatusMessage("已发送初始化完成信号给客户端");
                }
                else
                {
                    OnError("发送初始化信号失败");
                }
                return success;
            }
            catch (Exception ex)
            {
                OnError($"发送初始化信号异常: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 发送消息到客户端
        /// </summary>
        public bool SendMessage(string message)
        {
            if (!IsInitialized)
            {
                OnError("无法发送消息: 共享内存未初始化");
                return false;
            }

            try
            {
                if (WaitForSingleObject(_hMutex, 0xFFFFFFFF) != 0)
                {
                    OnError("获取互斥锁失败");
                    return false;
                }

                try
                {
                    var currentData = Marshal.PtrToStructure<SharedDataBase>(_pSharedData);

                    var newData = new SharedDataBase
                    {
                        MessageFromA = message.Length > Constants.BufferSize - 1
                            ? message.Substring(0, Constants.BufferSize - 1)
                            : message,
                        ReplyFromB = currentData.ReplyFromB ?? "",
                        NewMessageFromA = true,
                        NewReplyFromB = currentData.NewReplyFromB,
                        ExitFlag = currentData.ExitFlag
                    };

                    Marshal.StructureToPtr(newData, _pSharedData, false);

                    bool eventSet = SetEvent(_hEventFromAToB);
                    if (eventSet)
                    {
                        Interlocked.Increment(ref _messagesSent);
                        OnMessageSent(new MessageEventArgs(message));
                        OnStatusMessage($"消息发送成功: {message}");
                    }
                    else
                    {
                        OnError("设置事件失败");
                    }

                    return eventSet;
                }
                finally
                {
                    ReleaseMutex(_hMutex);
                }
            }
            catch (Exception ex)
            {
                OnError($"发送消息异常: {ex.Message}", ex);
                return false;
            }
        }
        public bool SendCommand(string message, int defCommand, string more)
        {
            if (!IsInitialized)
            {
                OnError("无法发送消息: 共享内存未初始化");
                return false;
            }

            try
            {
                if (WaitForSingleObject(_hMutex, 0xFFFFFFFF) != 0)
                {
                    OnError("获取互斥锁失败");
                    return false;
                }

                try
                {
                    var currentData = Marshal.PtrToStructure<CommandRunningData>(_pSharedData);

                    var newData = new CommandRunningData
                    {
                        MessageFromA = message.Length > Constants.BufferSize - 1
                            ? message.Substring(0, Constants.BufferSize - 1)
                            : message,
                        ReplyFromB = currentData.ReplyFromB ?? "",
                        NewMessageFromA = true,
                        NewReplyFromB = currentData.NewReplyFromB,
                        ExitFlag = currentData.ExitFlag,

                        Command = defCommand,
                        input_path = more
                    };

                    Marshal.StructureToPtr(newData, _pSharedData, false);

                    bool eventSet = SetEvent(_hEventFromAToB);
                    if (eventSet)
                    {
                        Interlocked.Increment(ref _messagesSent);
                        OnMessageSent(new MessageEventArgs(message));
                        OnStatusMessage($"消息发送成功: {message}");
                    }
                    else
                    {
                        OnError("设置事件失败");
                    }

                    return eventSet;
                }
                finally
                {
                    ReleaseMutex(_hMutex);
                }
            }
            catch (Exception ex)
            {
                OnError($"发送消息异常: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 发送退出指令
        /// </summary>
        public bool SendExitCommand()
        {
            if (!IsInitialized)
            {
                OnError("无法发送退出指令: 共享内存未初始化");
                return false;
            }

            try
            {
                if (WaitForSingleObject(_hMutex, 0xFFFFFFFF) != 0)
                {
                    OnError("获取互斥锁失败");
                    return false;
                }

                try
                {
                    var currentData = Marshal.PtrToStructure<SharedDataBase>(_pSharedData);

                    var newData = new SharedDataBase
                    {
                        MessageFromA = currentData.MessageFromA ?? "",
                        ReplyFromB = currentData.ReplyFromB ?? "",
                        NewMessageFromA = true,
                        NewReplyFromB = currentData.NewReplyFromB,
                        ExitFlag = true
                    };

                    Marshal.StructureToPtr(newData, _pSharedData, false);

                    bool success = SetEvent(_hEventFromAToB);
                    if (success)
                    {
                        OnStatusMessage("退出指令已发送");
                    }

                    return success;
                }
                finally
                {
                    ReleaseMutex(_hMutex);
                }
            }
            catch (Exception ex)
            {
                OnError($"发送退出指令异常: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 等待初始就绪消息
        /// </summary>
        private async Task<(bool success, string reply)> WaitForInitialReadyAsync(int timeoutMilliseconds)
        {
            try
            {
                // 等待就绪信号
                bool signalReceived = await Task.Run(() => _readySignal.Wait(timeoutMilliseconds));

                if (signalReceived)
                {
                    lock (_readyLock)
                    {
                        return (true, _initialReadyMessage);
                    }
                }
                else
                {
                    // 超时后检查是否有回复
                    var (hasReply, reply) = CheckForReply();
                    if (hasReply && !string.IsNullOrEmpty(reply))
                    {
                        return (true, reply);
                    }

                    return (false, "等待就绪超时");
                }
            }
            catch (Exception ex)
            {
                return (false, $"等待就绪异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否有新回复
        /// </summary>
        public (bool hasReply, string reply) CheckForReply()
        {
            if (!IsInitialized || _pSharedData == IntPtr.Zero)
                return (false, "");

            try
            {
                // 非阻塞检查事件
                uint waitResult = WaitForSingleObject(_hEventFromBToA, 0);

                if (waitResult == 0)
                {
                    ResetEvent(_hEventFromBToA);

                    if (WaitForSingleObject(_hMutex, 0xFFFFFFFF) != 0)
                        return (false, "");

                    try
                    {
                        var data = Marshal.PtrToStructure<SharedDataBase>(_pSharedData);

                        if (data.NewReplyFromB)
                        {
                            string reply = data.ReplyFromB ?? "";

                            // 检查是否为初始就绪消息
                            if (reply.Contains("已就绪") || reply.Contains("B进程") ||
                                reply.Contains("ready", StringComparison.OrdinalIgnoreCase))
                            {
                                lock (_readyLock)
                                {
                                    _initialReadyMessage = reply;
                                    _readySignal.Set();
                                }
                            }

                            // 清空回复
                            data.NewReplyFromB = false;
                            data.ReplyFromB = "";
                            Marshal.StructureToPtr(data, _pSharedData, false);

                            Interlocked.Increment(ref _repliesReceived);
                            OnReplyReceived(new MessageEventArgs(reply));
                            return (true, reply);
                        }

                        return (false, "");
                    }
                    finally
                    {
                        ReleaseMutex(_hMutex);
                    }
                }

                // 双重检查：即使事件没触发，也检查一下内存
                waitResult = WaitForSingleObject(_hMutex, 0);
                if (waitResult == 0)
                {
                    try
                    {
                        var data = Marshal.PtrToStructure<SharedDataBase>(_pSharedData);
                        if (data.NewReplyFromB)
                        {
                            string reply = data.ReplyFromB ?? "";

                            // 检查是否为初始就绪消息
                            if (reply.Contains("已就绪") || reply.Contains("B进程") ||
                                reply.Contains("ready", StringComparison.OrdinalIgnoreCase))
                            {
                                lock (_readyLock)
                                {
                                    _initialReadyMessage = reply;
                                    _readySignal.Set();
                                }
                            }

                            // 触发事件以便下次检查处理
                            SetEvent(_hEventFromBToA);
                            return (false, ""); // 返回false，让调用方知道需要再次检查
                        }
                    }
                    finally
                    {
                        ReleaseMutex(_hMutex);
                    }
                }

                return (false, "");
            }
            catch (Exception ex)
            {
                OnError($"检查回复异常: {ex.Message}", ex);
                return (false, "");
            }
        }

        /// <summary>
        /// 等待回复
        /// </summary>
        public async Task<(bool success, string reply)> WaitForReplyAsync(int timeoutMilliseconds = -1)
        {
            if (!IsInitialized)
            {
                return (false, "未初始化");
            }

            if (timeoutMilliseconds < 0)
            {
                timeoutMilliseconds = _config.ReplyTimeout;
            }

            try
            {
                var task = Task.Run(() =>
                {
                    // 首先检查是否已经有就绪消息
                    lock (_readyLock)
                    {
                        if (!string.IsNullOrEmpty(_initialReadyMessage) && _readySignal.IsSet)
                        {
                            string reply = _initialReadyMessage;
                            _initialReadyMessage = string.Empty;
                            _readySignal.Reset();
                            return (true, reply);
                        }
                    }

                    uint waitResult = WaitForSingleObject(_hEventFromBToA, (uint)timeoutMilliseconds);

                    if (waitResult == 0)  // WAIT_OBJECT_0
                    {
                        ResetEvent(_hEventFromBToA);

                        if (WaitForSingleObject(_hMutex, 0xFFFFFFFF) != 0)
                        {
                            return (false, "获取互斥锁失败");
                        }

                        try
                        {
                            var data = Marshal.PtrToStructure<SharedDataBase>(_pSharedData);

                            if (data.NewReplyFromB)
                            {
                                string reply = data.ReplyFromB ?? "";

                                // 清空回复
                                data.NewReplyFromB = false;
                                data.ReplyFromB = "";
                                Marshal.StructureToPtr(data, _pSharedData, false);

                                Interlocked.Increment(ref _repliesReceived);
                                OnReplyReceived(new MessageEventArgs(reply));
                                return (true, reply);
                            }
                            else
                            {
                                return (false, "没有新回复");
                            }
                        }
                        finally
                        {
                            ReleaseMutex(_hMutex);
                        }
                    }
                    else if (waitResult == 0x00000102)  // WAIT_TIMEOUT
                    {
                        return (false, "等待回复超时");
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        return (false, $"等待事件失败，错误码: {errorCode}");
                    }
                });

                return await task;
            }
            catch (Exception ex)
            {
                OnError($"等待回复异常: {ex.Message}", ex);
                return (false, $"异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动回复监控
        /// </summary>
        private void StartReplyMonitor()
        {
            _monitorCts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!_monitorCts.Token.IsCancellationRequested && IsInitialized)
                {
                    try
                    {
                        var (hasReply, reply) = CheckForReply();
                        if (hasReply && !string.IsNullOrEmpty(reply))
                        {
                            // 如果是初始就绪消息，已经通过_readySignal处理
                            // 其他消息正常触发事件
                        }
                    }
                    catch (Exception ex)
                    {
                        OnError($"监控回复异常: {ex.Message}", ex);
                    }

                    await Task.Delay(100, _monitorCts.Token);
                }
            }, _monitorCts.Token);
        }

        // ... 其他方法保持不变 ...

        /// <summary>
        /// 清理资源
        /// </summary>
        private void Cleanup()
        {
            if (_disposed) return;

            try
            {
                _monitorCts?.Cancel();
                _monitorCts?.Dispose();
                _monitorCts = null;

                _readySignal?.Dispose();

                if (_pSharedData != IntPtr.Zero)
                {
                    UnmapViewOfFile(_pSharedData);
                    _pSharedData = IntPtr.Zero;
                }

                if (_hMapFile != IntPtr.Zero)
                {
                    CloseHandle(_hMapFile);
                    _hMapFile = IntPtr.Zero;
                }

                if (_hMutex != IntPtr.Zero)
                {
                    CloseHandle(_hMutex);
                    _hMutex = IntPtr.Zero;
                }

                if (_hEventFromAToB != IntPtr.Zero)
                {
                    CloseHandle(_hEventFromAToB);
                    _hEventFromAToB = IntPtr.Zero;
                }

                if (_hEventFromBToA != IntPtr.Zero)
                {
                    CloseHandle(_hEventFromBToA);
                    _hEventFromBToA = IntPtr.Zero;
                }

                if (_hInitEvent != IntPtr.Zero)
                {
                    CloseHandle(_hInitEvent);
                    _hInitEvent = IntPtr.Zero;
                }

                _isInitialized = false;
                OnConnectionStatusChanged(false);
                OnStatusMessage("资源已清理");
            }
            catch (Exception ex)
            {
                OnError($"清理资源异常: {ex.Message}", ex);
            }
            finally
            {
                _disposed = true;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Cleanup();
            GC.SuppressFinalize(this);
        }

        ~SharedMemoryCreator()
        {
            if (!_disposed)
            {
                Cleanup();
            }
        }
        #region 新增辅助方法

        protected virtual void OnWarning(string message)
        {
            AppendLog($"[WARN] {message}", "Orange");
        }

        protected virtual void OnCppProcessStatusChanged(CppProcessEventArgs e)
        {
            CppProcessStatusChanged?.Invoke(this, e);
        }

        private void AppendLog(string message, string? color = null)
        {
            // 这个方法应该在UI线程中调用，这里只是占位
            StatusMessage?.Invoke(this, message);
        }

        #endregion

        #region 事件触发方法

        protected virtual void OnMessageSent(MessageEventArgs e)
        {
            MessageSent?.Invoke(this, e);
        }

        protected virtual void OnReplyReceived(MessageEventArgs e)
        {
            ReplyReceived?.Invoke(this, e);
        }

        protected virtual void OnError(string message, Exception? ex = null)
        {
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(message, ex));
        }

        protected virtual void OnConnectionStatusChanged(bool isConnected)
        {
            ConnectionStatusChanged?.Invoke(this, isConnected);
        }

        protected virtual void OnStatusMessage(string message)
        {
            StatusMessage?.Invoke(this, message);
        }

        #endregion

        // Windows API 声明保持不变...
        // 注意：需要保留所有的DllImport声明

        #region Windows API

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateMutex(
            IntPtr lpMutexAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool bInitialOwner,
            string lpName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateEvent(
            IntPtr lpEventAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool bManualReset,
            [MarshalAs(UnmanagedType.Bool)] bool bInitialState,
            string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReleaseMutex(IntPtr hMutex);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ResetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFileMapping(
            IntPtr hFile,
            IntPtr lpFileMappingAttributes,
            uint flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(
            IntPtr hFileMappingObject,
            uint dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            uint dwNumberOfBytesToMap);

        #endregion

       
    }

    /// <summary>
    /// C++进程事件参数
    /// </summary>
    public class CppProcessEventArgs : EventArgs
    {
        public bool IsRunning { get; }
        public int ExitCode { get; }
        public int ProcessId { get; }

#pragma warning disable IDE0290 // 使用主构造函数
        public CppProcessEventArgs(bool isRunning, int exitCode, int processId = -1)
#pragma warning restore IDE0290 // 使用主构造函数
        {
            IsRunning = isRunning;
            ExitCode = exitCode;
            ProcessId = processId;
        }
    }
}