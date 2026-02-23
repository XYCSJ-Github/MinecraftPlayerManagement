using g_mpm.Enums;
using g_mpm.Structs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SMC = g_mpm.SharedMemoryConfig.SharedMemoryConfig;
using Wapi = g_mpm.WinAPI.WinAPI;

namespace g_mpm
{
    public class SharedMemory(SMC sharedMemoryConfig)
    {
        /// <summary>
        /// 配置
        /// </summary>
        private SMC _config => sharedMemoryConfig;

        #region 句柄定义

        /// <summary>
        /// 共享内存句柄
        /// </summary>
        private IntPtr _hMapFile = IntPtr.Zero;
        /// <summary>
        /// 主互斥锁句柄
        /// </summary>
        private IntPtr _hMutex = IntPtr.Zero;
        /// <summary>
        /// 发送互斥锁句柄
        /// </summary>
        private IntPtr _hEvent_Send = IntPtr.Zero;
        /// <summary>
        /// 接受互斥锁句柄
        /// </summary>
        private IntPtr _hEvent_Recv = IntPtr.Zero;
        /// <summary>
        /// 初始化互斥锁句柄
        /// </summary>
        private IntPtr _hInitEvent = IntPtr.Zero;
        /// <summary>
        /// 共享内存内容指针
        /// </summary>
        private IntPtr sharedMemoryCommand = IntPtr.Zero;

        #endregion

        #region mpm进程信息

        /// <summary>
        /// 连接状态
        /// </summary>
        public ConnectStatus ConnectStatus = ConnectStatus.NOT_INITIALIZED;
        /// <summary>
        /// mpm状态
        /// </summary>
        public ProgramStatus ProgramStatus = ProgramStatus.STOP;
        /// <summary>
        /// mpm进程句柄
        /// </summary>
        private Process? mpmProcess;
        /// <summary>
        /// mpm进程ID
        /// </summary>
        private int mpmProcessID => mpmProcess?.Id ?? -1;

        #endregion

        public async Task<bool> InitializeAsync()
        {
            return await InitializeSharedMemory() && await StratmpmProcessAsync();
        }

        /// <summary>
        /// 初始化共享内存
        /// </summary>
        public async Task<bool> InitializeSharedMemory()
        {
            if (ConnectStatus == ConnectStatus.NOT_INITIALIZED)
            {
                try
                {
                    // 创建初始化事件
                    _hInitEvent = Wapi.CreateEvent(IntPtr.Zero, true, false, _config.InitEvent);
                    if (_hInitEvent == IntPtr.Zero)
                    {
                        return false;
                    }

                    // 创建共享内存
                    _hMapFile = Wapi.CreateFileMapping(
                        new IntPtr(-1),
                        IntPtr.Zero,
                        0x04, // PAGE_READWRITE
                        0,
                        (uint)Marshal.SizeOf<SharedMemoryCommand>(),
                        _config.MemoryName);

                    if (_hMapFile == IntPtr.Zero)
                    {
                        Wapi.CloseHandle(_hInitEvent);
                        _hInitEvent = IntPtr.Zero;
                        return false;
                    }

                    // 映射共享内存
                    sharedMemoryCommand = Wapi.MapViewOfFile(
                        _hMapFile,
                        0xF001F, // FILE_MAP_ALL_ACCESS
                        0,
                        0,
                        (uint)Marshal.SizeOf<SharedMemoryCommand>());

                    if (sharedMemoryCommand == IntPtr.Zero)
                    {
                        Cleanup();
                        return false;
                    }


                    // 初始化共享数据
                    var initData = new SharedMemoryCommand
                    {
                        Writer = 0,
                        DefCommand = 0,
                        RunStatus = 0,
                        StructDataType = 0,
                    };

                    Marshal.StructureToPtr(initData, sharedMemoryCommand, false);

                    if (_hMutex == IntPtr.Zero || _hEvent_Send == IntPtr.Zero || _hEvent_Recv == IntPtr.Zero)
                    {
                        Cleanup();
                        return false;
                    }

                    ConnectStatus = ConnectStatus.INITIALIZED;


                    return true;
                }
                catch (Exception)
                {
                    ConnectStatus = ConnectStatus.NOT_INITIALIZED;
                    Cleanup();
                    return false;
                }
            }
            else
            {
                ConnectStatus = ConnectStatus.INITIALIZED;

                return true;
            }
        }

        /// <summary>
        /// 启动mpm
        /// </summary>
        public async Task<bool> StratmpmProcessAsync()
        {
            try
            {
                if (!File.Exists("mpm.exe"))
                {
                    return false;
                }

                ProgramStatus = ProgramStatus.STARTING;

                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = "mpm.exe",
                    CreateNoWindow = true,
                    Arguments = "bg"
                };

                mpmProcess = new Process();
                mpmProcess.StartInfo = processStartInfo;
                mpmProcess.EnableRaisingEvents = true;

                //定义进程事件
                mpmProcess.Exited += (sender, args) =>
                {
                    ProgramStatus = ProgramStatus.STOP;

                    mpmProcess?.Dispose();
                    mpmProcess = null;
                };

                //输出重定向
                mpmProcess.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                    }
                };
                mpmProcess.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                    }
                };

                //回复监听
                StartReplyMonitor();

                // 启动进程
                bool started = mpmProcess.Start();
                if (!started)
                {
                    ProgramStatus = ProgramStatus.STOP;
                    return false;
                }

                ProgramStatus = ProgramStatus.RUNNING;

                // 开始异步读取输出
                mpmProcess.BeginOutputReadLine();
                mpmProcess.BeginErrorReadLine();

                // 等待进程初始化
                await Task.Delay(2000);

                // 通知C++进程初始化完成
                bool signaled = SignalInitializationComplete();

                if (!signaled)
                {
                }
                else
                {
                }

                // 等待C++进程就绪回复（使用新的就绪检测方法）
                var (readySuccess, readyReply) = await WaitForInitialReadyAsync(10000);

                if (readySuccess)
                {
                    return true;
                }
                else
                {
                    // 即使没有收到就绪回复，进程也可能正在运行
                    return ProgramStatus == ProgramStatus.RUNNING && !mpmProcess.HasExited;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 通知客户端初始化完成
        /// </summary>
        public bool SignalInitializationComplete()
        {
            if (ConnectStatus == ConnectStatus.INITIALIZED || _hInitEvent == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                bool success = Wapi.SetEvent(_hInitEvent);
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

        private void AppendLog(string message, string? color = null)
        {
            // 这个方法应该在UI线程中调用，这里只是占位
            StatusMessage?.Invoke(this, message);
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
            if (ConnectStatus == ConnectStatus.INITIALIZED || sharedMemoryCommand == IntPtr.Zero)
                return (false, "");

            try
            {
                // 非阻塞检查事件
                uint waitResult = WaitForSingleObject(_hEvent_Send, 0);

                if (waitResult == 0)
                {
                    ResetEvent(_hEvent_Recv);

                    if (WaitForSingleObject(_hMutex, 0xFFFFFFFF) != 0)
                        return (false, "");

                    try
                    {
                        var data = Marshal.PtrToStructure<SharedMemoryCommand>(sharedMemoryCommand);
                        string reply = "";

                        if (data.Writer == (int)WriteStatus.WHITEWITHCPP)
                        {
                            // 检查是否为初始就绪消息
                            if (data.DefCommand == (int)MemoryCommand.REDAY)
                            {
                                lock (_readyLock)
                                {
                                    _initialReadyMessage = reply;
                                    _readySignal.Set();
                                }
                            }

                            // 清空回复
                            data.Writer = (int)WriteStatus.EMPTY_WRITER;
                            data.DefCommand = (int)MemoryCommand.EMPTY_COMMAND;
                            Marshal.StructureToPtr(data, sharedMemoryCommand, false);

                            OnReplyRecv(new MessageEventArgs(reply));
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
                        var data = Marshal.PtrToStructure<SharedMemoryCommand>(sharedMemoryCommand);
                        string reply = "";

                        if (data.Writer == (int)WriteStatus.WHITEWITHCPP)
                        {

                            // 检查是否为初始就绪消息
                            if (data.DefCommand == (int)MemoryCommand.REDAY)
                            {
                                lock (_readyLock)
                                {
                                    _initialReadyMessage = reply;
                                    _readySignal.Set();
                                }
                            }

                            // 触发事件以便下次检查处理
                            SetEvent(_hEvent_Recv);
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
        /// 强制终止C++进程
        /// </summary>
        private bool ForceTerminateCppProcess()
        {
            try
            {
                if (mpmProcess == null) return true;

                mpmProcess.Kill();
                mpmProcess.WaitForExit(3000);

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
        /// 等待进程退出
        /// </summary>
        private async Task<bool> WaitForProcessExitAsync(int timeoutMilliseconds)
        {
            try
            {
                if (mpmProcess == null) return true;

                var task = mpmProcess.WaitForExitAsync();
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
        /// 停止C++进程
        /// </summary>
        public async Task<bool> StopCppProcessAsync()
        {
            try
            {
                if (mpmProcess == null || ProgramStatus == ProgramStatus.STOP)
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
        /// 发送消息到客户端
        /// </summary>
        public bool SendMessage(string message)
        {
            if (ConnectStatus == ConnectStatus.NOT_INITIALIZED)
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
                    var currentData = Marshal.PtrToStructure<SharedMemoryCommand>(sharedMemoryCommand);

                    var newData = new SharedMemoryCommand
                    {
                        //MessageFromA = message.Length > Constants.BufferSize - 1
                        //    ? message.Substring(0, Constants.BufferSize - 1)
                        //    : message,
                        //ReplyFromB = currentData.ReplyFromB ?? "",
                        //NewMessageFromA = true,
                        //NewReplyFromB = currentData.NewReplyFromB,
                        //ExitFlag = currentData.ExitFlag
                    };

                    Marshal.StructureToPtr(newData, sharedMemoryCommand, false);

                    bool eventSet = SetEvent(_hEvent_Send);
                    if (eventSet)
                    {
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
            if (ConnectStatus == ConnectStatus.NOT_INITIALIZED)
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
                    var currentData = Marshal.PtrToStructure<SharedMemoryCommand>(sharedMemoryCommand);

                    var newData = new SharedMemoryCommand
                    {
                        //MessageFromA = message.Length > Constants.BufferSize - 1
                        //    ? message.Substring(0, Constants.BufferSize - 1)
                        //    : message,
                        //ReplyFromB = currentData.ReplyFromB ?? "",
                        //NewMessageFromA = true,
                        //NewReplyFromB = currentData.NewReplyFromB,
                        //ExitFlag = currentData.ExitFlag,

                        //Command = defCommand,
                        //input_path = more
                    };

                    Marshal.StructureToPtr(newData, sharedMemoryCommand, false);

                    bool eventSet = SetEvent(_hEvent_Send);
                    if (eventSet)
                    {
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
            if (ConnectStatus == ConnectStatus.NOT_INITIALIZED)
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
                    var currentData = Marshal.PtrToStructure<SharedMemoryCommand>(sharedMemoryCommand);

                    var newData = new SharedMemoryCommand
                    {
                        //MessageFromA = currentData.MessageFromA ?? "",
                        //ReplyFromB = currentData.ReplyFromB ?? "",
                        //NewMessageFromA = true,
                        //NewReplyFromB = currentData.NewReplyFromB,
                        //ExitFlag = true
                    };

                    Marshal.StructureToPtr(newData, sharedMemoryCommand, false);

                    bool success = SetEvent(_hEvent_Send);
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
        /// 等待回复
        /// </summary>
        public async Task<(bool success, string reply)> WaitForReplyAsync(int timeoutMilliseconds = -1)
        {
            if (ConnectStatus == ConnectStatus.NOT_INITIALIZED)
            {
                return (false, "未初始化");
            }

            if (timeoutMilliseconds < 0)
            {
                //timeoutMilliseconds = _config.ReplyTimeout;
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

                    uint waitResult = WaitForSingleObject(_hEvent_Recv, (uint)timeoutMilliseconds);

                    if (waitResult == 0)  // WAIT_OBJECT_0
                    {
                        ResetEvent(_hEvent_Recv);

                        if (WaitForSingleObject(_hMutex, 0xFFFFFFFF) != 0)
                        {
                            return (false, "获取互斥锁失败");
                        }

                        try
                        {
                            var data = Marshal.PtrToStructure<SharedMemoryCommand>(sharedMemoryCommand);

                            if (data.Writer == (int)WriteStatus.WHITEWITHCPP)
                            {
                                string reply = "";

                                //// 清空回复
                                //data.NewReplyFromB = false;
                                //data.ReplyFromB = "";
                                //Marshal.StructureToPtr(data, _pSharedData, false);

                                //Interlocked.Increment(ref _repliesReceived);
                                //OnReplyReceived(new MessageEventArgs(reply));
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
                while (!_monitorCts.Token.IsCancellationRequested && ConnectStatus == ConnectStatus.INITIALIZED)
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

                if (sharedMemoryCommand != IntPtr.Zero)
                {
                    UnmapViewOfFile(sharedMemoryCommand);
                    sharedMemoryCommand = IntPtr.Zero;
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

                if (_hEvent_Send != IntPtr.Zero)
                {
                    CloseHandle(_hEvent_Send);
                    _hEvent_Send = IntPtr.Zero;
                }

                if (_hEvent_Recv != IntPtr.Zero)
                {
                    CloseHandle(_hEvent_Recv);
                    _hEvent_Recv = IntPtr.Zero;
                }

                if (_hInitEvent != IntPtr.Zero)
                {
                    CloseHandle(_hInitEvent);
                    _hInitEvent = IntPtr.Zero;
                }

                ConnectStatus = ConnectStatus.NOT_INITIALIZED;
                OnConnectionStatusChanged(ConnectStatus);
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

#endregion

    }
}