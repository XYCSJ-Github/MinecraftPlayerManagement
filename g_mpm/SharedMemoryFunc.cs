using g_mpm.Enums;
using g_mpm.Structs;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Smc = g_mpm.SharedMemoryConfig.SharedMemoryConfig;
using Wapi = g_mpm.WinAPI.WinAPI;

namespace g_mpm
{
    public class SharedMemoryFunc
    {
        ///<summary>
        /// 初始化共享内存
        /// </summary>
        public static bool InitializeSharedMemory(Smc sharedMemoryConfig, ref ConnectStatus connectStatus, ref HandlePtr handlePtr)
        {
            if (connectStatus == ConnectStatus.NOT_INITIALIZED)
            {
                try
                {
                    //创建初始化互斥锁
                    handlePtr._hInitEvent = Wapi.CreateEvent(IntPtr.Zero, true, true, sharedMemoryConfig.InitEvent);
                    if (handlePtr._hInitEvent == IntPtr.Zero)
                    {
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
                        DefCommand = (int)MemoryCommand.EMPTY_COMMAND,
                        RunStatus = RunStatus.EMPTY_STATUS,
                        StructDataType = StructDataType.EMPTY_STRUCT
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
                            DefCommand = (int)Command.EXIT
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
        public static bool CSend(int Command, string additionaCommand, ConnectStatus connectStatus, HandlePtr handlePtr)
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
                        var currentData = Marshal.PtrToStructure<SharedMemoryCommand>(handlePtr.sharedMemoryCommand);

                        var NData = new SharedMemoryCommand
                        {
                            Writer = WriteStatus.WHITEWITHCS,
                            DefCommand = (int)Command,
                            AdditionaCommand = additionaCommand
                        };

                        Marshal.StructureToPtr(NData, handlePtr.sharedMemoryCommand, false);

                        bool eventSet = Wapi.SetEvent(handlePtr._hEvent_Send);
                        if (eventSet)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    finally
                    {
                        Wapi.ReleaseMutex(handlePtr._hMutex);
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
        /// 接受指令
        ///</summary>
        public static string CRecv(ConnectStatus connectStatus, HandlePtr handlePtr)
        {
            return String.Empty;
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
    }
}
