// SharedDataStructures.cs
using System;
using System.Runtime.InteropServices;

namespace g_mpm
{
    /// <summary>
    /// 基础共享数据结构体，必须与C++端完全匹配
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct SharedDataBase
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.BufferSize)]
        public string MessageFromA;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.BufferSize)]
        public string ReplyFromB;

        [MarshalAs(UnmanagedType.Bool)]
        public bool NewMessageFromA;

        [MarshalAs(UnmanagedType.Bool)]
        public bool NewReplyFromB;

        [MarshalAs(UnmanagedType.Bool)]
        public bool ExitFlag;

        public const int BufferSize = Constants.BufferSize;
    }

    /// <summary>
    /// 扩展共享数据结构体，包含更多统计信息
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct ExtendedSharedData
    {
        // 基础字段
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.BufferSize)]
        public string MessageFromA;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.BufferSize)]
        public string ReplyFromB;

        [MarshalAs(UnmanagedType.Bool)]
        public bool NewMessageFromA;

        [MarshalAs(UnmanagedType.Bool)]
        public bool NewReplyFromB;

        [MarshalAs(UnmanagedType.Bool)]
        public bool ExitFlag;

        // 扩展字段
        public int MessageCount;
        public long Timestamp;
        public int ProcessId;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public byte[] Reserved;
    }

    /// <summary>
    /// 共享数据接口
    /// </summary>
    public interface ISharedData
    {
        string MessageFromA { get; set; }
        string ReplyFromB { get; set; }
        bool NewMessageFromA { get; set; }
        bool NewReplyFromB { get; set; }
        bool ExitFlag { get; set; }
    }

    /// <summary>
    /// 常量定义
    /// </summary>
    public static class Constants
    {
        public const int BufferSize = 256;
        public const string DefaultMemoryName = "ShareMemory";
        public const string DefaultMutexName = "ShareMutex";
        public const string DefaultEventAToB = "EventFromAToB";
        public const string DefaultEventBToA = "EventFromBToA";
        public const string DefaultInitEvent = "SharedMemoryInitEvent";
    }

    /// <summary>
    /// 共享内存配置
    /// </summary>
    public class SharedMemoryConfig
    {
        public string MemoryName { get; set; } = Constants.DefaultMemoryName;
        public string MutexName { get; set; } = Constants.DefaultMutexName;
        public string EventAToB { get; set; } = Constants.DefaultEventAToB;
        public string EventBToA { get; set; } = Constants.DefaultEventBToA;
        public string InitEvent { get; set; } = Constants.DefaultInitEvent;
        public int MaxRetries { get; set; } = 10;
        public int RetryInterval { get; set; } = 1000;
        public int ReplyTimeout { get; set; } = 5000;
        public int InitTimeout { get; set; } = 30000;
        public bool EnableVerboseLogging { get; set; } = true;
    }

    /// <summary>
    /// 消息事件参数
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        public string Message { get; }
        public DateTime Timestamp { get; }

#pragma warning disable IDE0290 // 使用主构造函数
        public MessageEventArgs(string message)
#pragma warning restore IDE0290 // 使用主构造函数
        {
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 错误事件参数
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; }
        public Exception Exception { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0290:使用主构造函数", Justification = "<挂起>")]
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
        public ErrorEventArgs(string message, Exception? ex = null)
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。

        {
            ErrorMessage = message;
#pragma warning disable CS8601 // 引用类型赋值可能为 null。
            Exception = ex;
#pragma warning restore CS8601 // 引用类型赋值可能为 null。
        }
    }
}