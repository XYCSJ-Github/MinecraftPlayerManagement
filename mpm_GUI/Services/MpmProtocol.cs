using System.Globalization;
using System.Text;

namespace mpm_GUI.Services;

/// <summary>共享内存协议常量与结构体布局偏移（对齐 C++ Struct.h / SharedMemory.h）。</summary>
internal static class MpmProtocol
{
    public const int BufferSize = 10240;

    public const string MemoryName = "SharedMemory";
    public const string MutexName = "MutexLock";
    public const string EventSend = "EventSend";
    public const string EventRecv = "EventRecv";
    public const string InitEventName = "SharedMemoryInitEvent";

    // SharedMemoryCommand 字段偏移（MSVC 4 字节对齐推算）
    public const int OffWriter = 0;                    // WriteStatus  int32
    public const int OffLoadMode = 4;                  // LoadMode     int32
    public const int OffDefCommand = 8;                // Command      int32
    public const int OffAdditionaCommand = 12;         // char[10240]
    public const int OffRunStatus = 12 + 10240;        // RunStatus    int32  (10252)
    public const int OffErrorInfo = 10256;             // char[10240]
    public const int OffTitleName = 20496;             // char[10240]
    public const int OffStructDataType = 30736;        // StructType   int32
    public const int OffStructData = 30740;            // BYTE[10240]

    /// <summary>sizeof(SharedMemoryCommand)，与 C++ 一致。</summary>
    public const int CommandStructSize = 40980;
}

/// <summary>写入状态（对齐 C++ WriteStatus）。</summary>
public enum WriteStatus
{
    EMPTY_WRITER = 0,
    WHITEWITHCPP = 1,
    WHITEWITHCS = 2
}

/// <summary>路径加载模式（对齐 C++ LoadMode）。</summary>
public enum LoadMode
{
    KEEP = 0,
    EMPTY = 1,
    CLIENT = 2,
    SERVER = 3
}

/// <summary>执行状态（对齐 C++ RunStatus）。</summary>
public enum RunStatus
{
    EMPTY_STATUS = 0,
    SUCCESSFUL = 1,
    FAILED = 2
}

/// <summary>结构体数据类型（对齐 C++ StructType）。</summary>
public enum StructDataType
{
    EMPTY_STRUCT = 0,
    WDNL = 1,
    WDN = 2,
    UI = 3,
    PI_AS = 4,
    PI_D = 5,
    PIWI = 6,
    PIWIL = 7
}

/// <summary>命令（对齐 C++ Command）。</summary>
public enum MpmCommand
{
    EMPTY_COMMAND = 0,
    M_SET_PATH = 1,
    EXIT = 2,
    BREAK = 3,
    OPEN_WORLD = 4,
    OPEN_PLAYER = 5,
    LIST_WORLD = 6,
    LIST_PLAYER = 7,
    DEL_PLAYER = 8,
    DEL_WORLD = 9,
    DEL_PW = 10,
    DEL_JS = 11,
    NULL_BACK = 12,
    REFRESH = 13
}

/// <summary>C++ 引擎返回的原始回复。</summary>
public sealed record MpmReply(
    RunStatus Status,
    StructDataType DataType,
    byte[] Data,
    string ErrorInfo,
    string TitleName,
    LoadMode Mode)
{
    public bool Ok => Status == RunStatus.SUCCESSFUL;
}

/// <summary>引擎调用失败时抛出。</summary>
public sealed class MpmException : Exception
{
    public MpmException(string message) : base(message) { }
}

/// <summary>字符串编解码策略（C++ 窄字符串编码与 UTF-8 玩家名混合的兜底方案）。</summary>
internal static class SmText
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    static SmText()
    {
        // 注册系统代码页（GBK 等），否则 .NET 默认仅支持 UTF-8/Unicode
        try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); }
        catch { /* 旧框架无该类型时忽略 */ }
    }

    private static Encoding? _acp;
    /// <summary>当前 ANSI 代码页编码，用于文件系统路径/目录名。</summary>
    private static Encoding Acp
    {
        get
        {
            if (_acp == null)
            {
                try { _acp = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage); }
                catch { _acp = Encoding.Default; }
            }
            return _acp;
        }
    }

    /// <summary>智能解码：优先严格 UTF-8，失败回退系统 ANSI，截断到首个 \0。</summary>
    public static string Decode(byte[] bytes, int start, int count)
    {
        if (bytes == null || count <= 0) return string.Empty;
        int len = count;
        for (int i = start; i < start + count; i++)
        {
            if (bytes[i] == 0) { len = i - start; break; }
        }
        if (len <= 0) return string.Empty;
        try
        {
            return StrictUtf8.GetString(bytes, start, len);
        }
        catch (DecoderFallbackException)
        {
            return Acp.GetString(bytes, start, len);
        }
    }

    public static string Decode(byte[] bytes) => Decode(bytes, 0, bytes.Length);

    /// <summary>路径等文件系统字符串，编码为目标代码页。</summary>
    public static byte[] EncodePath(string value) => Acp.GetBytes(value ?? string.Empty);

    /// <summary>玩家名等源自 JSON(UTF-8) 的字符串。</summary>
    public static byte[] EncodeUtf8(string value) => StrictUtf8.GetBytes(value ?? string.Empty);
}

/// <summary>二进制缓冲区的小端 int 与字符串读写辅助。</summary>
internal static class Bin
{
    public static int ReadI32(byte[] data, ref int off)
    {
        int v = BitConverter.ToInt32(data, off);
        off += 4;
        return v;
    }

    public static string ReadString(byte[] data, ref int off)
    {
        int len = ReadI32(data, ref off);
        if (len <= 0) return string.Empty;
        string s = SmText.Decode(data, off, len);
        off += len;
        return s;
    }
}
