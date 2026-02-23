//声明共享内存属性
namespace g_mpm.SharedMemoryConfig
{
    /// <summary>
    /// 定义常量
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// 内存大小
        /// </summary>
        public const int BufferSize = 1024;
        /// <summary>
        /// 共享内存名称
        /// </summary>
        public const string MemoryName = "SharedMemory";
        /// <summary>
        /// 互斥锁名称
        /// </summary>
        public const string MutexName = "MutexLock";
        /// <summary>
        /// 发送事件名称
        /// </summary>
        public const string EventSend = "EventSend";
        /// <summary>
        /// 接收事件名称
        /// </summary>
        public const string EventRecv = "EventRecv";
        /// <summary>
        /// 初始事件名称
        /// </summary>
        public const string InitEvent = "SharedMemoryInitEvent";
    }

    /// <summary>
    /// 共享内存配置
    /// </summary>
    public class SharedMemoryConfig
    {
        public string MemoryName { get; set; } = Constants.MemoryName;
        public string MutexName { get; set; } = Constants.MutexName;
        public string EventSend { get; set; } = Constants.EventSend;
        public string EventRecv { get; set; } = Constants.EventRecv;
        public string InitEvent { get; set; } = Constants.InitEvent;
        public int MaxRetries { get; set; } = 10;
        public int RetryInterval { get; set; } = 1000;
        public int ReplyTimeout { get; set; } = 5000;
        public int InitTimeout { get; set; } = 30000;
        public bool EnableVerboseLogging { get; set; } = true;
    }
}
