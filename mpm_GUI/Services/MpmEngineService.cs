using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using mpm_GUI.Models;

namespace mpm_GUI.Services;

public enum EngineState
{
    Stopped,
    Connecting,
    Ready,
    Error
}

/// <summary>
/// 管理 mpm.exe(bg) 引擎生命周期并通过共享内存收发命令。
/// 服务端创建内核对象，拉起 C++ 引擎，串行执行命令队列。
/// </summary>
public sealed class MpmEngineService : IDisposable
{
    private readonly SettingsStore _settings;

    public event Action<EngineState>? StateChanged;
    public event Action<string>? LogLine;

    public EngineState State { get; private set; } = EngineState.Stopped;
    public bool IsConnected => State == EngineState.Ready;
    public string? CurrentRootPath { get; private set; }
    public string? CurrentRootName { get; private set; }
    public LoadMode CurrentMode { get; private set; }

    // 句柄与映射视图
    private IntPtr _hMapFile;
    private IntPtr _hMutex;
    private IntPtr _hSend;
    private IntPtr _hRecv;
    private IntPtr _hInit;
    private IntPtr _view;

    private Process? _proc;
    private string _enginePath = string.Empty;

    private BlockingCollection<EngineRequest>? _queue;
    private Thread? _worker;
    private volatile bool _workerStop;

    private const int DefaultTimeoutMs = 20000;
    private int _startGuard;

    public MpmEngineService(SettingsStore settings)
    {
        _settings = settings;
    }

    // ---------------- 生命周期 ----------------

    public async Task<bool> StartAsync(string mpmPath)
    {
        if (Interlocked.Exchange(ref _startGuard, 1) != 0) return false;
        try
        {
            return await Task.Run(() => StartCore(mpmPath)).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _startGuard, 0);
        }
    }

    private bool StartCore(string mpmPath)
    {
        _enginePath = mpmPath;
        SetState(EngineState.Connecting);
        Log($"准备启动引擎: {mpmPath}");

        if (!File.Exists(mpmPath))
        {
            SetState(EngineState.Error);
            Log("未找到 mpm.exe，请检查引擎路径");
            return false;
        }

        try
        {
            CleanupCore();
            ReapOrphanEngines(mpmPath);

            // 1. 共享内存
            _hMapFile = NativeMethods.CreateFileMappingW(
                new IntPtr(-1), IntPtr.Zero, NativeMethods.PageReadWrite,
                0, MpmProtocol.CommandStructSize, MpmProtocol.MemoryName);
            if (_hMapFile == IntPtr.Zero)
            {
                Log($"创建共享内存失败: 错误码 {Marshal.GetLastWin32Error()}");
                SetState(EngineState.Error);
                return false;
            }

            _view = NativeMethods.MapViewOfFile(_hMapFile, NativeMethods.FileMapAllAccess,
                0, 0, (UIntPtr)MpmProtocol.CommandStructSize);
            if (_view == IntPtr.Zero)
            {
                Log($"映射共享内存失败: 错误码 {Marshal.GetLastWin32Error()}");
                SetState(EngineState.Error);
                return false;
            }

            // 2. 同步对象
            _hMutex = NativeMethods.CreateMutexW(IntPtr.Zero, false, MpmProtocol.MutexName);
            _hSend = NativeMethods.CreateEventW(IntPtr.Zero, false, false, MpmProtocol.EventSend);
            _hRecv = NativeMethods.CreateEventW(IntPtr.Zero, false, false, MpmProtocol.EventRecv);
            _hInit = NativeMethods.CreateEventW(IntPtr.Zero, true, false, MpmProtocol.InitEventName);
            if (_hMutex == IntPtr.Zero || _hSend == IntPtr.Zero || _hRecv == IntPtr.Zero || _hInit == IntPtr.Zero)
            {
                Log($"创建同步对象失败: 错误码 {Marshal.GetLastWin32Error()}");
                SetState(EngineState.Error);
                return false;
            }

            Log("共享内存与同步对象创建完成");

            // 3. 拉起 C++ 引擎(bg)
            var psi = new ProcessStartInfo
            {
                FileName = mpmPath,
                Arguments = "bg",
                WorkingDirectory = Path.GetDirectoryName(mpmPath) ?? string.Empty,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            try
            {
                _proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                _proc.Exited += OnEngineExited;
                _proc.Start();
            }
            catch (Exception ex)
            {
                Log($"启动引擎失败: {ex.Message}");
                SetState(EngineState.Error);
                return false;
            }

            Log($"引擎已启动 (PID {_proc.Id})，等待握手...");

            // 4. 握手：等待 C++ 设置初始化事件
            bool ready = WaitForHandshake();
            if (!ready)
            {
                Log("握手超时或引擎提前退出");
                SetState(EngineState.Error);
                return false;
            }

            _queue = new BlockingCollection<EngineRequest>();
            _workerStop = false;
            _worker = new Thread(WorkerLoop) { IsBackground = true, Name = "MpmCommandWorker" };
            _worker.Start();

            SetState(EngineState.Ready);
            Log("引擎就绪");
            return true;
        }
        catch (Exception ex)
        {
            Log($"初始化异常: {ex}");
            SetState(EngineState.Error);
            return false;
        }
    }

    private bool WaitForHandshake()
    {
        var sw = Stopwatch.StartNew();
        long lastLog = 0;
        while (sw.ElapsedMilliseconds < 30000)
        {
            if (_proc != null && _proc.HasExited)
            {
                Log($"引擎提前退出，退出码 {(_proc.ExitCode.ToString() ?? "?")}");
                return false;
            }
            if (sw.ElapsedMilliseconds - lastLog > 5000)
            {
                lastLog = sw.ElapsedMilliseconds;
                Log($"等待 C++ 握手... {sw.ElapsedMilliseconds / 1000}s");
            }
            uint r = NativeMethods.WaitForSingleObject(_hInit, 200);
            if (r == NativeMethods.WaitObject0)
            {
                NativeMethods.ResetEvent(_hInit);
                return true;
            }
            if (r == NativeMethods.WaitFailed) return false;
        }
        return false;
    }

    private void OnEngineExited(object? sender, EventArgs e)
    {
        if (_workerStop) return;
        Log($"引擎进程已退出 (退出码 {(_proc?.ExitCode.ToString() ?? "?")})");
        _workerStop = true;
        try { _queue?.CompleteAdding(); } catch { /* ignore */ }
        SetState(EngineState.Stopped);
    }

    public async Task StopAsync()
    {
        await Task.Run(() => StopSync()).ConfigureAwait(false);
    }

    /// <summary>同步停止引擎（用于退出路径，避免依赖 UI 调度器续体）。</summary>
    public void StopSync()
    {
        try
        {
            _workerStop = true;
            try { _queue?.CompleteAdding(); } catch { }

            // 让工作线程尽早在等待中退出
            for (int i = 0; i < 20 && _worker?.IsAlive == true; i++) Thread.Sleep(100);

            SendExitCommand();
            WaitForProcessExit();
            CleanupCore();
            Log("引擎已停止");
        }
        catch
        {
            // 退出时清理异常不抛出
        }
        SetStateQuietly(EngineState.Stopped);
    }

    private void SetStateQuietly(EngineState state)
    {
        try { SetState(state); }
        catch { /* 关闭期间调度器可能已不可用 */ }
    }

    private void SendExitCommand()
    {
        if (_view == IntPtr.Zero || _hMutex == IntPtr.Zero || _hSend == IntPtr.Zero) return;
        if (_proc != null && _proc.HasExited) return;

        if (NativeMethods.WaitForSingleObject(_hMutex, 3000) != NativeMethods.WaitObject0) return;
        try
        {
            WriteRequest(MpmCommand.EXIT, Array.Empty<byte>());
            NativeMethods.SetEvent(_hSend);
        }
        finally
        {
            NativeMethods.ReleaseMutex(_hMutex);
        }
    }

    private void WaitForProcessExit()
    {
        if (_proc == null) return;
        try
        {
            if (!_proc.HasExited && !_proc.WaitForExit(4000))
            {
                _proc.Kill(true);
                _proc.WaitForExit(2000);
            }
        }
        catch { /* ignore */ }
        finally
        {
            _proc.Dispose();
            _proc = null;
        }
    }

    private void CleanupCore()
    {
        if (_view != IntPtr.Zero) { NativeMethods.UnmapViewOfFile(_view); _view = IntPtr.Zero; }
        SafeClose(ref _hMapFile);
        SafeClose(ref _hMutex);
        SafeClose(ref _hSend);
        SafeClose(ref _hRecv);
        SafeClose(ref _hInit);
        _queue?.Dispose();
        _queue = null;
    }

    private static void SafeClose(ref IntPtr h)
    {
        if (h != IntPtr.Zero) { NativeMethods.CloseHandle(h); h = IntPtr.Zero; }
    }

    // ---------------- 工作线程：命令执行 ----------------

    private sealed class EngineRequest
    {
        public MpmCommand Command;
        public byte[] Additional = Array.Empty<byte>();
        public int TimeoutMs;
        public TaskCompletionSource<MpmReply> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private void WorkerLoop()
    {
        try
        {
            while (!_workerStop)
            {
                EngineRequest? req;
                try { req = _queue?.Take(); }
                catch (InvalidOperationException) { break; }
                if (req == null) break;

                try
                {
                    var reply = Exchange(req.Command, req.Additional, req.TimeoutMs);
                    req.Tcs.TrySetResult(reply);
                }
                catch (Exception ex)
                {
                    req.Tcs.TrySetException(ex);
                }
            }
        }
        catch { /* worker 异常终止 */ }
    }

    private MpmReply Exchange(MpmCommand command, byte[] additional, int timeoutMs)
    {
        // 写请求（持有互斥锁）
        if (NativeMethods.WaitForSingleObject(_hMutex, 5000) != NativeMethods.WaitObject0)
            throw new MpmException("获取互斥锁超时");
        try
        {
            if (_view == IntPtr.Zero) throw new MpmException("共享内存已释放");
            WriteRequest(command, additional);
            NativeMethods.SetEvent(_hSend);
        }
        finally
        {
            NativeMethods.ReleaseMutex(_hMutex);
        }

        // 等待 C++ 回复事件
        var sw = Stopwatch.StartNew();
        uint result;
        do
        {
            if (_workerStop) throw new MpmException("引擎已停止");
            result = NativeMethods.WaitForSingleObject(_hRecv, 200);
            if (result == NativeMethods.WaitObject0) break;
            if (_proc != null && _proc.HasExited)
                throw new MpmException("引擎进程已退出");
        }
        while (result != NativeMethods.WaitFailed && sw.ElapsedMilliseconds < timeoutMs);

        if (result != NativeMethods.WaitObject0)
            throw new MpmException("等待引擎回复超时");

        // 读取回复（持有互斥锁）
        if (NativeMethods.WaitForSingleObject(_hMutex, 5000) != NativeMethods.WaitObject0)
            throw new MpmException("获取互斥锁超时");
        try
        {
            if (_view == IntPtr.Zero) throw new MpmException("共享内存已释放");
            return ReadReply();
        }
        finally
        {
            NativeMethods.ReleaseMutex(_hMutex);
        }
    }

    // ---------------- 共享内存读写 ----------------

    private void WriteRequest(MpmCommand command, byte[] additional)
    {
        Marshal.WriteInt32(_view, MpmProtocol.OffWriter, (int)WriteStatus.WHITEWITHCS);
        Marshal.WriteInt32(_view, MpmProtocol.OffLoadMode, (int)LoadMode.KEEP);
        Marshal.WriteInt32(_view, MpmProtocol.OffDefCommand, (int)command);
        Marshal.WriteInt32(_view, MpmProtocol.OffRunStatus, (int)RunStatus.EMPTY_STATUS);
        Marshal.WriteInt32(_view, MpmProtocol.OffStructDataType, (int)StructDataType.EMPTY_STRUCT);

        // 清理附加命令区域并写入
        IntPtr p = IntPtr.Add(_view, MpmProtocol.OffAdditionaCommand);
        int len = Math.Min(additional.Length, MpmProtocol.BufferSize);
        if (len > 0) Marshal.Copy(additional, 0, p, len);
        if (len < MpmProtocol.BufferSize) Marshal.WriteByte(p, len, 0);
        else Marshal.WriteByte(IntPtr.Add(_view, MpmProtocol.OffAdditionaCommand + MpmProtocol.BufferSize - 1), 0);
    }

    private MpmReply ReadReply()
    {
        int writer = Marshal.ReadInt32(_view, MpmProtocol.OffWriter);
        LoadMode mode = (LoadMode)Marshal.ReadInt32(_view, MpmProtocol.OffLoadMode);
        RunStatus status = (RunStatus)Marshal.ReadInt32(_view, MpmProtocol.OffRunStatus);
        StructDataType type = (StructDataType)Marshal.ReadInt32(_view, MpmProtocol.OffStructDataType);

        string error = ReadFixedString(MpmProtocol.OffErrorInfo);
        string title = ReadFixedString(MpmProtocol.OffTitleName);

        var data = new byte[MpmProtocol.BufferSize];
        Marshal.Copy(IntPtr.Add(_view, MpmProtocol.OffStructData), data, 0, data.Length);

        Log($"回复: 命令处理者={writer}, 状态={status}, 类型={type}");
        return new MpmReply(status, type, data, error, title, mode);
    }

    private string ReadFixedString(int offset)
    {
        var bytes = new byte[MpmProtocol.BufferSize];
        Marshal.Copy(IntPtr.Add(_view, offset), bytes, 0, bytes.Length);
        return SmText.Decode(bytes);
    }

    // ---------------- 业务命令 ----------------

    private Task<MpmReply> EnqueueAsync(MpmCommand command, byte[] additional, int timeoutMs = DefaultTimeoutMs)
    {
        if (_queue == null || _workerStop)
            throw new MpmException("引擎未就绪，请先启动引擎");

        var req = new EngineRequest { Command = command, Additional = additional, TimeoutMs = timeoutMs };
        _queue.Add(req);
        return req.Tcs.Task;
    }

    private static void EnsureOk(MpmReply reply, string fallback)
    {
        if (!reply.Ok)
            throw new MpmException(string.IsNullOrEmpty(reply.ErrorInfo) ? fallback : $"mpm: {reply.ErrorInfo}");
    }

    /// <summary>设置根目录（客户端 .minecraft 或服务端目录）。返回目录显示名。</summary>
    public async Task<string> OpenPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new MpmException("路径不能为空");
        var reply = await EnqueueAsync(MpmCommand.M_SET_PATH, SmText.EncodePath(path), 30000);
        EnsureOk(reply, "路径无法识别或处理失败");
        CurrentRootPath = path;
        CurrentRootName = string.IsNullOrEmpty(reply.TitleName) ? Path.GetFileName(path.TrimEnd('\\', '/')) : reply.TitleName;
        CurrentMode = reply.Mode;
        return CurrentRootName;
    }

    public async Task<IReadOnlyList<WorldEntry>> ListWorldsAsync()
    {
        var reply = await EnqueueAsync(MpmCommand.LIST_WORLD, Array.Empty<byte>());
        EnsureOk(reply, "获取存档列表失败");
        return MpmCodec.ParseWorldDirectoriesNameList(reply.Data);
    }

    public async Task<IReadOnlyList<PlayerEntry>> ListPlayersAsync()
    {
        var reply = await EnqueueAsync(MpmCommand.LIST_PLAYER, Array.Empty<byte>());
        EnsureOk(reply, "获取玩家列表失败");
        return MpmCodec.ParseUserInfoList(reply.Data);
    }

    public async Task RefreshAsync()
    {
        var reply = await EnqueueAsync(MpmCommand.REFRESH, Array.Empty<byte>());
        EnsureOk(reply, "刷新失败");
    }

    /// <summary>查看某存档内的玩家及数据存在情况。</summary>
    public async Task<IReadOnlyList<PlayerInWorldPresence>> OpenWorldAsync(string worldName)
    {
        var reply = await EnqueueAsync(MpmCommand.OPEN_WORLD, SmText.EncodePath(worldName), 30000);
        EnsureOk(reply, "打开存档失败");
        return MpmCodec.ParsePlayerInWorldInfoList(reply.Data).Rows;
    }

    /// <summary>查看某玩家在各存档中的数据存在情况。</summary>
    public async Task<IReadOnlyList<PlayerInWorldPresence>> OpenPlayerAsync(string playerName)
    {
        var reply = await EnqueueAsync(MpmCommand.OPEN_PLAYER, SmText.EncodeUtf8(playerName), 30000);
        EnsureOk(reply, "查看玩家失败");
        return MpmCodec.ParsePlayerInWorldInfoList(reply.Data).Rows;
    }

    /// <summary>彻底删除某玩家（所有存档数据 + usercache 缓存，进回收站）。</summary>
    public async Task DeletePlayerAsync(string playerName)
    {
        var reply = await EnqueueAsync(MpmCommand.DEL_PLAYER, SmText.EncodeUtf8(playerName), 90000);
        EnsureOk(reply, "删除玩家失败");
    }

    /// <summary>仅在某存档内删除该玩家的数据。</summary>
    public async Task DeletePlayerFromWorldAsync(string playerName, string worldName)
    {
        // C++ 侧按 "玩家名 存档名" 空格拆分；玩家名存自 usercache(UTF-8)，存档名来自文件系统(ACP)。
        var combined = Concat(SmText.EncodeUtf8(playerName), new byte[] { 0x20 }, SmText.EncodePath(worldName));
        var reply = await EnqueueAsync(MpmCommand.DEL_PW, combined, 90000);
        EnsureOk(reply, "从存档删除玩家失败");
    }

    /// <summary>移除 usercache/usernamecache 中的玩家；playerName 为空时清除全部缓存。</summary>
    public async Task ClearJsonCacheAsync(string? playerName = null)
    {
        byte[] arg = string.IsNullOrEmpty(playerName)
            ? SmText.EncodePath("_ALL_PJS_")
            : SmText.EncodeUtf8(playerName);
        var reply = await EnqueueAsync(MpmCommand.DEL_JS, arg, 30000);
        EnsureOk(reply, "清理缓存失败");
    }

    private static byte[] Concat(params byte[][] parts)
    {
        int total = parts.Sum(p => p.Length);
        var result = new byte[total];
        int off = 0;
        foreach (var p in parts) { Buffer.BlockCopy(p, 0, result, off, p.Length); off += p.Length; }
        return result;
    }

    // ---------------- 其它 ----------------

    private void ReapOrphanEngines(string enginePath)
    {
        try
        {
            string full = Path.GetFullPath(enginePath);
            foreach (var p in Process.GetProcessesByName("mpm"))
            {
                try
                {
                    if (p.HasExited) continue;
                    if (p.MainWindowHandle != IntPtr.Zero) continue; // 保留用户可见的控制台实例
                    string module = p.MainModule?.FileName ?? string.Empty;
                    if (string.IsNullOrEmpty(module)) continue;
                    if (string.Equals(Path.GetFullPath(module), full, StringComparison.OrdinalIgnoreCase))
                    {
                        Log($"终止残留引擎进程 PID {p.Id}");
                        p.Kill(true);
                    }
                }
                catch { /* 无权访问则跳过 */ }
                finally { p.Dispose(); }
            }
        }
        catch { /* 枚举失败忽略 */ }
    }

    private void SetState(EngineState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }

    private void Log(string line)
    {
        LogLine?.Invoke($"[{DateTime.Now:HH:mm:ss}] {line}");
    }

    public void Dispose()
    {
        StopSync();
        GC.SuppressFinalize(this);
    }
}
