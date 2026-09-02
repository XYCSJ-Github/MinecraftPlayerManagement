using System.Runtime.InteropServices;

namespace mpm_GUI.Services;

internal static class NativeMethods
{
    private const string Kernel32 = "kernel32.dll";

    // CreateFileMapping / MapViewOfFile 保护与访问标志
    public const uint PageReadWrite = 0x04;
    public const uint FileMapAllAccess = 0x000F001F;
    public const uint ErrorAlreadyExists = 183;

    // WaitForSingleObject 返回值
    public const uint WaitObject0 = 0x0000_0000;
    public const uint WaitAbandoned = 0x0000_0080;
    public const uint WaitTimeout = 0x0000_0102;
    public const uint WaitFailed = 0xFFFF_FFFF;

    [DllImport(Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateFileMappingW(
        IntPtr hFile,
        IntPtr lpFileMappingAttributes,
        uint flProtect,
        uint dwMaximumSizeHigh,
        uint dwMaximumSizeLow,
        string lpName);

    [DllImport(Kernel32, SetLastError = true)]
    public static extern IntPtr MapViewOfFile(
        IntPtr hFileMappingObject,
        uint dwDesiredAccess,
        uint dwFileOffsetHigh,
        uint dwFileOffsetLow,
        UIntPtr dwNumberOfBytesToMap);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport(Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateMutexW(
        IntPtr lpMutexAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInitialOwner,
        string lpName);

    [DllImport(Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateEventW(
        IntPtr lpEventAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bManualReset,
        [MarshalAs(UnmanagedType.Bool)] bool bInitialState,
        string lpName);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetEvent(IntPtr hEvent);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ResetEvent(IntPtr hEvent);

    [DllImport(Kernel32, SetLastError = true)]
    public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReleaseMutex(IntPtr hMutex);
}
