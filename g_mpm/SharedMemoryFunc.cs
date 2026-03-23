// SharedMemoryFunc.cs - 修复事件触发逻辑
using g_mpm.Enums;
using g_mpm.Structs;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Smc = g_mpm.SharedMemoryConfig.SharedMemoryConfig;
using Wapi = g_mpm.WinAPI.WinAPI;

namespace g_mpm
{

    public class ErrorEventArgs : EventArgs
    {
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
    }

    public class SharedMemoryFunc
    {
        #region 事件定义

        public event EventHandler<ReplyReceivedEventArgs>? ReplyReceived;
        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;
        public event EventHandler<ErrorEventArgs>? ErrorOccurred;
        public event EventHandler<OutputReceivedEventArgs>? OutputReceived;
        public event EventHandler<ProgramStatusChangedEventArgs>? ProgramStatusChanged;

        private CancellationTokenSource? _listenerCts;
        private Task? _listenerTask;
        private Process? _monitoredProcess;

        #endregion

        #region 事件参数

        public class ReplyReceivedEventArgs : EventArgs
        {
            public StructDataType DataType { get; set; }
            public byte[]? Data { get; set; }
            public Command OriginalCommand { get; set; }
            public string? ErrorInfo { get; set; }
            public bool IsSuccess { get; set; }
            public LoadMode mode { get; set; }
            public string Title { get; set; }
        }

        public class OutputReceivedEventArgs : EventArgs
        {
            public string? Data { get; set; }
            public bool IsError { get; set; }
        }

        public class ConnectionStatusChangedEventArgs : EventArgs
        {
            public ConnectStatus OldStatus { get; set; }
            public ConnectStatus NewStatus { get; set; }
        }

        public class ProgramStatusChangedEventArgs : EventArgs
        {
            public ProgramStatus OldStatus { get; set; }
            public ProgramStatus NewStatus { get; set; }
        }

        #endregion

        #region 事件附加函数

        /// <summary>
        /// 启动回复监听
        /// </summary>
        public void StartReplyListener(ConnectStatus status, HandlePtr handles)
        {
            StopReplyListener(); // 确保先停止现有的监听

            _listenerCts = new CancellationTokenSource();
            _listenerTask = Task.Run(async () =>
            {
                int errorCount = 0;
                while (!_listenerCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var (type, data, error, command, runStatus, errorInfo, loadmode, title) = CheckReply(status, handles);

                        if (!string.IsNullOrEmpty(error))
                        {
                            errorCount++;
                            ErrorOccurred?.Invoke(this, new ErrorEventArgs
                            {
                                ErrorMessage = error
                            });

                            if (errorCount > 10)
                            {
                                ErrorOccurred?.Invoke(this, new ErrorEventArgs
                                {
                                    ErrorMessage = "Too many consecutive errors, stopping listener"
                                });
                                break;
                            }
                        }
                        else
                        {
                            errorCount = 0; // 重置错误计数

                            if (type != StructDataType.EMPTY_STRUCT || !string.IsNullOrEmpty(errorInfo))
                            {
                                ReplyReceived?.Invoke(this, new ReplyReceivedEventArgs
                                {
                                    DataType = type,
                                    Data = data,
                                    ErrorInfo = errorInfo,
                                    IsSuccess = runStatus == RunStatus.SUCCESSFUL,
                                    mode = loadmode,
                                    Title = title
                                });
                            }
                        }

                        await Task.Delay(10, _listenerCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.Invoke(this, new ErrorEventArgs
                        {
                            ErrorMessage = $"Listener error: {ex.Message}",
                            Exception = ex
                        });
                        await Task.Delay(100, _listenerCts.Token); // 出错后稍等再试
                    }
                }
            }, _listenerCts.Token);
        }

        /// <summary>
        /// 停止回复监听
        /// </summary>
        public void StopReplyListener()
        {
            try
            {
                _listenerCts?.Cancel();
                _listenerTask?.Wait(1000);
            }
            catch (AggregateException)
            {
                // 忽略取消时的异常
            }
            finally
            {
                _listenerCts?.Dispose();
                _listenerCts = null;
                _listenerTask = null;
            }
        }

        /// <summary>
        /// 开始监控进程输出
        /// </summary>
        public void StartProcessMonitor(Process process)
        {
            _monitoredProcess = process;

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OutputReceived?.Invoke(this, new OutputReceivedEventArgs
                    {
                        Data = e.Data,
                        IsError = false
                    });
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OutputReceived?.Invoke(this, new OutputReceivedEventArgs
                    {
                        Data = e.Data,
                        IsError = true
                    });
                }
            };

            process.Exited += (s, e) =>
            {
                // 检查退出代码，判断是正常退出还是异常退出
                int exitCode = process.ExitCode;
                bool isNormalExit = exitCode == 0; // 假设正常退出返回0

                ProgramStatusChanged?.Invoke(this, new ProgramStatusChangedEventArgs
                {
                    OldStatus = ProgramStatus.RUNNING,
                    NewStatus = ProgramStatus.STOP
                });

                // 根据退出代码决定日志级别
                OutputReceived?.Invoke(this, new OutputReceivedEventArgs
                {
                    Data = isNormalExit
                        ? "C++ process exited normally"
                        : $"C++ process exited with code {exitCode}",
                    IsError = !isNormalExit // 只有非正常退出才标为错误
                });
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        #endregion

        #region 功能函数

        ///<summary>
        /// 初始化共享内存
        /// </summary>
        public static bool InitializeSharedMemory(Smc sharedMemoryConfig, ref ConnectStatus connectStatus, ref HandlePtr handlePtr)
        {
            if (connectStatus == ConnectStatus.NOT_INITIALIZED)
            {
                try
                {
                    // 创建初始化事件
                    handlePtr._hInitEvent = Wapi.CreateEvent(IntPtr.Zero, true, false, sharedMemoryConfig.InitEvent);
                    if (handlePtr._hInitEvent == IntPtr.Zero)
                    {
                        int error = Marshal.GetLastWin32Error();
                        Debug.WriteLine($"CreateEvent failed: {error}");
                        return false;
                    }

                    // 创建互斥锁
                    handlePtr._hMutex = Wapi.CreateMutex(IntPtr.Zero, false, sharedMemoryConfig.MutexName);
                    if (handlePtr._hMutex == IntPtr.Zero)
                    {
                        int error = Marshal.GetLastWin32Error();
                        Debug.WriteLine($"CreateMutex failed: {error}");
                        Cleanup(ref handlePtr, ref connectStatus);
                        return false;
                    }

                    // 创建事件
                    handlePtr._hEvent_Send = Wapi.CreateEvent(IntPtr.Zero, false, false, sharedMemoryConfig.EventSend);
                    handlePtr._hEvent_Recv = Wapi.CreateEvent(IntPtr.Zero, false, false, sharedMemoryConfig.EventRecv);

                    if (handlePtr._hEvent_Send == IntPtr.Zero || handlePtr._hEvent_Recv == IntPtr.Zero)
                    {
                        int error = Marshal.GetLastWin32Error();
                        Debug.WriteLine($"CreateEvent failed: {error}");
                        Cleanup(ref handlePtr, ref connectStatus);
                        return false;
                    }

                    // 创建共享内存
                    handlePtr._hMapFile = Wapi.CreateFileMapping(new IntPtr(-1), IntPtr.Zero,
                        0x04, 0, (uint)Marshal.SizeOf<SharedMemoryCommand>(), sharedMemoryConfig.MemoryName);

                    if (handlePtr._hMapFile == IntPtr.Zero)
                    {
                        int error = Marshal.GetLastWin32Error();
                        Debug.WriteLine($"CreateFileMapping failed: {error}");
                        Cleanup(ref handlePtr, ref connectStatus);
                        return false;
                    }

                    // 映射共享内存
                    handlePtr.sharedMemoryCommand = Wapi.MapViewOfFile(handlePtr._hMapFile,
                        0xF001F, 0, 0, (uint)Marshal.SizeOf<SharedMemoryCommand>());

                    if (handlePtr.sharedMemoryCommand == IntPtr.Zero)
                    {
                        int error = Marshal.GetLastWin32Error();
                        Debug.WriteLine($"MapViewOfFile failed: {error}");
                        Cleanup(ref handlePtr, ref connectStatus);
                        return false;
                    }

                    // 初始化结构体数据
                    var InitData = new SharedMemoryCommand
                    {
                        Writer = WriteStatus.EMPTY_WRITER,
                        DefCommand = Command.EMPTY_COMMAND,
                        RunStatus = RunStatus.EMPTY_STATUS,
                        LoadMod = LoadMode.EMPTY_MOD,
                        TitleName = "",
                        StructDataType = StructDataType.EMPTY_STRUCT,
                        AdditionaCommand = "",
                        ErrorInfo = "",
                        StructData = new byte[sharedMemoryConfig.BufSize]
                    };

                    // 应用更改
                    Marshal.StructureToPtr(InitData, handlePtr.sharedMemoryCommand, false);

                    connectStatus = ConnectStatus.INITIALIZED;
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"InitializeSharedMemory exception: {ex}");
                    Cleanup(ref handlePtr, ref connectStatus);
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 启动mpm
        /// </summary>
        public Process? StartProcess(ref ProgramStatus programStatus, ProcessStartInfo processStartInfo)
        {
            Process? process = null;

            try
            {
                if (!File.Exists("mpm.exe"))
                {
                    Debug.WriteLine("mpm.exe not found");
                    return process;
                }

                programStatus = ProgramStatus.STARTING;

                process = new Process();
                process.StartInfo = processStartInfo;
                process.EnableRaisingEvents = true;

                // 启动进程
                bool is_su = process.Start();
                if (!is_su)
                {
                    programStatus = ProgramStatus.STOP;
                    return null;
                }

                programStatus = ProgramStatus.RUNNING;
                return process;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StartProcess exception: {ex}");
                programStatus = ProgramStatus.STOP;
                return null;
            }
        }

        ///<summary>
        /// 发送退出指令
        ///</summary>
        public static bool SendExitCommand(ref ConnectStatus connectStatus, ref HandlePtr handlePtr)
        {
            if (connectStatus != ConnectStatus.CONNECTED)
                return false;

            try
            {
                uint waitResult = Wapi.WaitForSingleObject(handlePtr._hMutex, 5000);
                if (waitResult != 0)
                {
                    Debug.WriteLine($"WaitForSingleObject failed: {waitResult}");
                    return false;
                }

                try
                {
                    var NewData = new SharedMemoryCommand
                    {
                        Writer = WriteStatus.WHITEWITHCS,
                        DefCommand = Command.EXIT,
                        RunStatus = RunStatus.EMPTY_STATUS,
                        StructDataType = StructDataType.EMPTY_STRUCT,
                        AdditionaCommand = "",
                        ErrorInfo = "",
                        StructData = new byte[SharedMemoryConfig.Constants.BufferSize]
                    };

                    Marshal.StructureToPtr(NewData, handlePtr.sharedMemoryCommand, false);

                    bool is_suc = Wapi.SetEvent(handlePtr._hEvent_Send);

                    // 立即更新连接状态为断开中
                    connectStatus = ConnectStatus.NOT_CONNECTED;

                    return is_suc;
                }
                finally
                {
                    Wapi.ReleaseMutex(handlePtr._hMutex);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SendExitCommand exception: {ex}");
                return false;
            }
        }

        ///<summary>
        /// 发送指令
        ///</summary>
        public static bool CSend(Command command, string additionaCommand, ConnectStatus connectStatus, HandlePtr handlePtr)
        {
            if (connectStatus != ConnectStatus.CONNECTED)
                return false;

            uint waitResult = Wapi.WaitForSingleObject(handlePtr._hMutex, 5000);
            if (waitResult != 0)
                return false;

            try
            {
                var NData = new SharedMemoryCommand
                {
                    Writer = WriteStatus.WHITEWITHCS,
                    DefCommand = command,
                    AdditionaCommand = additionaCommand ?? "",
                    RunStatus = RunStatus.EMPTY_STATUS,
                    StructDataType = StructDataType.EMPTY_STRUCT,
                    ErrorInfo = "",
                    StructData = new byte[SharedMemoryConfig.Constants.BufferSize]
                };

                Marshal.StructureToPtr(NData, handlePtr.sharedMemoryCommand, false);
                return Wapi.SetEvent(handlePtr._hEvent_Send);
            }
            finally
            {
                Wapi.ReleaseMutex(handlePtr._hMutex);
            }
        }

        ///<summary>
        /// 回复监听
        ///</summary>
        public static (StructDataType, byte[], string Error, Command, RunStatus, string, LoadMode,string) CheckReply(ConnectStatus connectStatus, HandlePtr handlePtr)
        {
            if (connectStatus != ConnectStatus.CONNECTED)
                return (StructDataType.EMPTY_STRUCT, Array.Empty<byte>(), "Not Connected", Command.EMPTY_COMMAND, RunStatus.EMPTY_STATUS, "", LoadMode.EMPTY_MOD, "");

            uint waitResult = Wapi.WaitForSingleObject(handlePtr._hEvent_Recv, 0); // 使用 Recv 事件
            if (waitResult != 0)
                return (StructDataType.EMPTY_STRUCT, Array.Empty<byte>(), "", Command.EMPTY_COMMAND, RunStatus.EMPTY_STATUS, "", LoadMode.EMPTY_MOD, "");

            Wapi.ResetEvent(handlePtr._hEvent_Recv);

            waitResult = Wapi.WaitForSingleObject(handlePtr._hMutex, 5000);
            if (waitResult != 0)
                return (StructDataType.EMPTY_STRUCT, Array.Empty<byte>(), "Mutex timeout", Command.EMPTY_COMMAND, RunStatus.EMPTY_STATUS, "", LoadMode.EMPTY_MOD, "");

            try
            {
                var data = Marshal.PtrToStructure<SharedMemoryCommand>(handlePtr.sharedMemoryCommand);

                if (data.Writer == WriteStatus.WHITEWITHCPP)
                {
                    StructDataType type = data.StructDataType;
                    byte[] sd = data.StructData ?? Array.Empty<byte>();
                    Command cmd = data.DefCommand;
                    LoadMode loadMode = data.LoadMod;
                    RunStatus status = data.RunStatus;
                    string errorInfo = data.ErrorInfo ?? "";
                    string title = data.TitleName;

                    // 重置结构体
                    var resetData = new SharedMemoryCommand
                    {
                        Writer = WriteStatus.WHITEWITHCS,
                        DefCommand = Command.EMPTY_COMMAND,
                        RunStatus = RunStatus.EMPTY_STATUS,
                        StructDataType = StructDataType.EMPTY_STRUCT,
                        AdditionaCommand = "",
                        TitleName = "",
                        ErrorInfo = "",
                        StructData = new byte[SharedMemoryConfig.Constants.BufferSize]
                    };
                    Marshal.StructureToPtr(resetData, handlePtr.sharedMemoryCommand, false);

                    return (type, sd, "", cmd, status, errorInfo, loadMode, title);
                }
            }
            finally
            {
                Wapi.ReleaseMutex(handlePtr._hMutex);
            }

            return (StructDataType.EMPTY_STRUCT, Array.Empty<byte>(), "", Command.EMPTY_COMMAND, RunStatus.EMPTY_STATUS, "", LoadMode.EMPTY_MOD, "");
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public static bool Cleanup(ref HandlePtr handlePtr, ref ConnectStatus connectStatus)
        {
            try
            {
                if (handlePtr.sharedMemoryCommand != IntPtr.Zero)
                {
                    Wapi.UnmapViewOfFile(handlePtr.sharedMemoryCommand);
                    handlePtr.sharedMemoryCommand = IntPtr.Zero;
                }

                if (handlePtr._hMapFile != IntPtr.Zero)
                {
                    Wapi.CloseHandle(handlePtr._hMapFile);
                    handlePtr._hMapFile = IntPtr.Zero;
                }

                if (handlePtr._hMutex != IntPtr.Zero)
                {
                    Wapi.CloseHandle(handlePtr._hMutex);
                    handlePtr._hMutex = IntPtr.Zero;
                }

                if (handlePtr._hEvent_Send != IntPtr.Zero)
                {
                    Wapi.CloseHandle(handlePtr._hEvent_Send);
                    handlePtr._hEvent_Send = IntPtr.Zero;
                }

                if (handlePtr._hEvent_Recv != IntPtr.Zero)
                {
                    Wapi.CloseHandle(handlePtr._hEvent_Recv);
                    handlePtr._hEvent_Recv = IntPtr.Zero;
                }

                if (handlePtr._hInitEvent != IntPtr.Zero)
                {
                    Wapi.CloseHandle(handlePtr._hInitEvent);
                    handlePtr._hInitEvent = IntPtr.Zero;
                }

                connectStatus = ConnectStatus.NOT_INITIALIZED;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cleanup exception: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 强制终止C++进程
        /// </summary>
        public bool ForceTerminateCppProcess(Process? process)
        {
            try
            {
                if (process == null || process.HasExited)
                    return true;

                process.Kill();
                return process.WaitForExit(3000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ForceTerminateCppProcess exception: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 等待C++程序就绪
        /// </summary>
        public async Task<bool> WaitForCppReadyAsync(HandlePtr handlePtr, int timeoutMs)
        {
            try
            {
                uint waitResult = await Task.Run(() =>
                    Wapi.WaitForSingleObject(handlePtr._hInitEvent, (uint)timeoutMs));

                return waitResult == 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}