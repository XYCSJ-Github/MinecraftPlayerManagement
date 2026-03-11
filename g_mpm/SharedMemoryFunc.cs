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
    }

    public class SharedMemoryFunc
    {
        #region 事件定义

        public event EventHandler<ReplyReceivedEventArgs>? ReplyReceived;
        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;
        public event EventHandler<ErrorEventArgs>? ErrorOccurred;

        private CancellationTokenSource? _listenerCts;
        private Task? _listenerTask;

        #endregion

        #region 事件参数
        // 事件参数类
        public class ReplyReceivedEventArgs : EventArgs
        {
            public StructDataType DataType { get; set; }
            public byte[]? Data { get; set; }
            public Command OriginalCommand { get; set; }
        }

        public class ConnectionStatusChangedEventArgs : EventArgs
        {
            public ConnectStatus OldStatus { get; set; }
            public ConnectStatus NewStatus { get; set; }
        }

        #endregion

        #region 事件附加函数

        /// <summary>
        /// 启动回复监听
        /// </summary>
        public void StartReplyListener(ConnectStatus status, HandlePtr handles)
        {
            _listenerCts = new CancellationTokenSource();
            _listenerTask = Task.Run(async () =>
            {
                while (!_listenerCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var (type, data, error) = CheckReply(status, handles);

                        if (error != null)
                        {
                            ErrorOccurred?.Invoke(this, new ErrorEventArgs
                            {
                                ErrorMessage = error
                            });
                        }
                        else if (type != StructDataType.EMPTY_STRUCT)
                        {
                            ReplyReceived?.Invoke(this, new ReplyReceivedEventArgs
                            {
                                DataType = type,
                                Data = data
                            });
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
                            ErrorMessage = $"Listener error: {ex.Message}"
                        });
                    }
                }
            }, _listenerCts.Token);
        }

        /// <summary>
        /// 停止回复监听
        /// </summary>
        public void StopReplyListener()
        {
            _listenerCts?.Cancel();
            _listenerTask?.Wait(1000);
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
                    //创建初始化事件
                    handlePtr._hInitEvent = Wapi.CreateEvent(IntPtr.Zero, true, true, sharedMemoryConfig.InitEvent);
                    if (handlePtr._hInitEvent == IntPtr.Zero)
                    {
                        return false;
                    }

                    //创建互斥锁
                    handlePtr._hMutex = Wapi.CreateMutex(IntPtr.Zero, false, sharedMemoryConfig.MutexName);
                    if (handlePtr._hMutex == IntPtr.Zero)
                    {
                        Cleanup(ref handlePtr, ref connectStatus);
                        return false;
                    }

                    // 3. 创建事件
                    handlePtr._hEvent_Send = Wapi.CreateEvent(IntPtr.Zero, false, false, sharedMemoryConfig.EventSend);
                    handlePtr._hEvent_Recv = Wapi.CreateEvent(IntPtr.Zero, false, false, sharedMemoryConfig.EventRecv);

                    if (handlePtr._hEvent_Send == IntPtr.Zero || handlePtr._hEvent_Recv == IntPtr.Zero)
                    {
                        Cleanup(ref handlePtr, ref connectStatus);
                        return false;
                    }

                    //创建共享内存
                    handlePtr._hMapFile = Wapi.CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, (uint)Marshal.SizeOf<SharedMemoryCommand>(), sharedMemoryConfig.MemoryName);
                    if (handlePtr._hMapFile == IntPtr.Zero)
                    {
                        Wapi.CloseHandle(handlePtr._hInitEvent);
                        handlePtr._hInitEvent = IntPtr.Zero;
                        return false;
                    }

                    //映射共享内存
                    handlePtr.sharedMemoryCommand = Wapi.MapViewOfFile(handlePtr._hMapFile, 0xF001F, 0, 0, (uint)Marshal.SizeOf<SharedMemoryCommand>());
                    if (handlePtr.sharedMemoryCommand == IntPtr.Zero)
                    {
                        Cleanup(ref handlePtr, ref connectStatus);
                        return false;
                    }

                    //初始化结构体数据
                    var InitData = new SharedMemoryCommand
                    {
                        Writer = WriteStatus.EMPTY_WRITER,
                        DefCommand = Command.EMPTY_COMMAND,
                        RunStatus = RunStatus.EMPTY_STATUS,
                        StructDataType = StructDataType.EMPTY_STRUCT,
                        StructData = new byte[sharedMemoryConfig.BufSize]
                    };

                    //应用更改
                    Marshal.StructureToPtr(InitData, handlePtr.sharedMemoryCommand, false);
                    if (handlePtr._hMutex == IntPtr.Zero || handlePtr._hEvent_Send == IntPtr.Zero || handlePtr._hEvent_Recv == IntPtr.Zero)
                    {
                        Cleanup(ref handlePtr, ref connectStatus);
                        return false;
                    }

                    connectStatus = ConnectStatus.INITIALIZED;
                    return true;
                }
                catch (Exception)
                {
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
        public static Process? StartProcess(ref ProgramStatus programStatus, ProcessStartInfo processStartInfo)
        {
            Process? process = null;

            try
            {
                if (!File.Exists("mpm.exe"))
                {
                    return process;
                }

                programStatus = ProgramStatus.STARTING;

                process = new Process();
                process.StartInfo = processStartInfo;
                process.EnableRaisingEvents = true;

                //订阅进程事件
                process.Exited += (sender, args) =>
                {
                    process?.Dispose();
                    process = null;
                };


                //启动进程
                bool is_su = process.Start();
                if (!is_su)
                {
                    programStatus = ProgramStatus.STOP;
                    return process;
                }
                programStatus = ProgramStatus.RUNNING;
            }
            catch (Exception)
            {
                return process;
            }

            return process;
        }

        ///<summary>
        /// 发送退出指令
        ///</summary>
        public static bool SendExitCommand(ref ConnectStatus connectStatus, ref HandlePtr handlePtr)
        {
            if (connectStatus == ConnectStatus.CONNECTED)
            {
                try
                {
                    if (Wapi.WaitForSingleObject(handlePtr._hMutex, 0xFFFFFFFF) != 0)
                    {
                        return false;
                    }

                    try
                    {
                        var ExitData = Marshal.PtrToStructure<SharedMemoryCommand>(handlePtr.sharedMemoryCommand);

                        var NewData = new SharedMemoryCommand
                        {
                            Writer = WriteStatus.WHITEWITHCS,
                            DefCommand = Command.EXIT
                        };

                        Marshal.StructureToPtr(NewData, handlePtr.sharedMemoryCommand, false);

                        bool is_suc = Wapi.SetEvent(handlePtr._hEvent_Send);
                        if (!is_suc)
                        {
                            return false;
                        }

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        ///<summary>
        /// 发送指令
        ///</summary>
        public static bool CSend(Command Command, string additionaCommand, ConnectStatus connectStatus, HandlePtr handlePtr)
        {
            if (connectStatus != ConnectStatus.CONNECTED)
                return false;
            if (Wapi.WaitForSingleObject(handlePtr._hMutex, 0xFFFFFFFF) != 0)
                return false;

            try
            {
                var NData = new SharedMemoryCommand
                {
                    Writer = WriteStatus.WHITEWITHCS,
                    DefCommand = Command,
                    AdditionaCommand = additionaCommand ?? ""
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
        public static (StructDataType, byte[], String Error) CheckReply(ConnectStatus connectStatus, HandlePtr handlePtr)
        {
            StructDataType type = StructDataType.EMPTY_STRUCT;
            byte[] sd = { };

            if (connectStatus != ConnectStatus.CONNECTED)
                return (StructDataType.EMPTY_STRUCT, sd, "Not Connect");
            uint waitResult = Wapi.WaitForSingleObject(handlePtr._hEvent_Send, 0);
            if (waitResult != 0)
                return (StructDataType.EMPTY_STRUCT, sd, "");

            Wapi.ResetEvent(handlePtr._hEvent_Send);

            if (Wapi.WaitForSingleObject(handlePtr._hMutex, 0xFFFFFFFF) != 0)
                return (type, sd, "Mutex timeout");

            try
            {
                var data = Marshal.PtrToStructure<SharedMemoryCommand>(handlePtr.sharedMemoryCommand);

                if (data.Writer == WriteStatus.WHITEWITHCPP)
                {
                    if (data.RunStatus == RunStatus.SUCCESSFUL)
                    {
                        type = data.StructDataType;
                        sd = data.StructData;

                        ResetSM(ref data);
                        Marshal.StructureToPtr(data, handlePtr.sharedMemoryCommand, false);

                        return (type, sd, "");
                    }
                    else if (data.RunStatus == RunStatus.FAILED)
                    {
                        return (type, sd, "");
                    }
                }

            }
            finally
            {
                Wapi.ReleaseMutex(handlePtr._hMutex);
            }

            return (type, sd, "");
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
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose(ref HandlePtr handlePtr, ref ConnectStatus connectStatus)
        {
            Cleanup(ref handlePtr, ref connectStatus);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 清空结构体数据
        /// </summary>
        public static void ResetSM(ref SharedMemoryCommand sharedMemoryCommand)
        {
            sharedMemoryCommand.Writer = WriteStatus.WHITEWITHCS;
            sharedMemoryCommand.DefCommand = Command.EMPTY_COMMAND;
            sharedMemoryCommand.AdditionaCommand = "";
            sharedMemoryCommand.RunStatus = RunStatus.EMPTY_STATUS;
            sharedMemoryCommand.ErrorInfo = "";
            sharedMemoryCommand.StructDataType = StructDataType.EMPTY_STRUCT;
            Array.Clear(sharedMemoryCommand.StructData, 0, sharedMemoryCommand.StructData.Length);
        }

        /// <summary>
        /// 强制终止C++进程
        /// </summary>
        public bool ForceTerminateCppProcess(Process? process)
        {
            try
            {
                if (process == null) return true;

                process.Kill();
                process.WaitForExit(3000);

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
