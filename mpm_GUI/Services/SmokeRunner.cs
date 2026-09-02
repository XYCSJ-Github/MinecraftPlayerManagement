using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mpm_GUI.Services;

/// <summary>
/// 端到端自检：构造一个迷你客户端根目录，验证引擎握手、
/// 路径识别、列表与详情命令全链路。经 `--smoke` 参数由 App 触发。
/// </summary>
internal static class SmokeRunner
{
    private const string Uuid = "11111111-1111-1111-1111-111111111111";

    public static async Task<string> RunAsync(string baseDir)
    {
        var sb = new StringBuilder();
        var engine = new MpmEngineService(new SettingsStore());
        var recent = new List<string>();

        string progressPath = Path.Combine(baseDir, "mpm_smoke_progress.txt");
        void Progress(string message)
        {
            File.AppendAllText(progressPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\r\n");
        }

        engine.LogLine += line =>
        {
            lock (recent)
            {
                recent.Add(line);
                if (recent.Count > 40) recent.RemoveAt(0);
            }
            try { Progress(line); } catch { }
        };

        string root = string.Empty;
        try
        {
            sb.AppendLine("== mpm GUI 自检开始 ==");
            Progress("开始");

            string enginePath = EngineLocator.Find(new SettingsStore())
                ?? throw new InvalidOperationException("未找到 mpm.exe");
            sb.AppendLine($"引擎: {enginePath}");
            Progress($"引擎路径: {enginePath}");

            root = CreateFixture(baseDir);
            sb.AppendLine($"测试目录: {root}");
            Progress($"测试目录: {root}");

            Progress("启动引擎...");
            bool ok = await engine.StartAsync(enginePath);
            Progress($"StartAsync -> {ok}");
            Assert(sb, "引擎握手(StartAsync)", ok);

            Progress("设置根目录...");
            string name = await engine.OpenPathAsync(root);
            Progress($"OpenPath -> {name}");
            Assert(sb, "设置根目录(OpenPath)", name.Length > 0 && engine.CurrentMode == LoadMode.CLIENT,
                $"标题={name}, 模式={engine.CurrentMode}");

            Progress("列出存档...");
            var worlds = await engine.ListWorldsAsync();
            Progress($"ListWorlds -> {worlds.Count}");
            var worldNames = worlds.Select(w => w.Name).ToHashSet();
            Assert(sb, "列出存档(ListWorlds)", worldNames.Contains("WorldOne") && worldNames.Contains("EmptyWorld"),
                $"存档={string.Join(",", worldNames)}");

            Progress("列出玩家...");
            var players = await engine.ListPlayersAsync();
            Progress($"ListPlayers -> {players.Count}");
            Assert(sb, "列出玩家(ListPlayers)",
                players.Any(p => p.Name == "Steve" && p.Uuid == Uuid),
                $"玩家={string.Join(",", players.Select(p => p.Name))}");

            Progress("打开存档详情...");
            var inWorld = await engine.OpenWorldAsync("WorldOne");
            Progress($"OpenWorld rows={inWorld.Count}");
            Assert(sb, "打开存档详情(OpenWorld)",
                inWorld.Count == 1 && inWorld[0].PlayerName == "Steve"
                && inWorld[0].HasPlayerData && inWorld[0].HasOldPlayerData && inWorld[0].HasCosArmor
                && inWorld[0].HasAdvancement && inWorld[0].HasStats,
                $"行数={inWorld.Count}, 摘要={(inWorld.Count > 0 ? inWorld[0] : "空")}");

            Progress("打开空存档详情...");
            var emptyWorld = await engine.OpenWorldAsync("EmptyWorld");
            Progress($"OpenWorld(Empty) rows={emptyWorld.Count}");
            Assert(sb, "空存档详情返回空行集", emptyWorld.Count == 0, $"行数={emptyWorld.Count}");

            Progress("打开玩家详情...");
            var playerView = await engine.OpenPlayerAsync("Steve");
            Progress($"OpenPlayer rows={playerView.Count}");
            Assert(sb, "打开玩家详情(OpenPlayer)",
                playerView.Count == 1 && playerView[0].WorldName == "WorldOne",
                $"行数={playerView.Count}");

            bool threw = false;
            try { await engine.OpenPlayerAsync("Ghost"); }
            catch (MpmException) { threw = true; }
            Assert(sb, "不存在的玩家应报错", threw);

            sb.AppendLine("== PASS ==");
            Progress("== PASS ==");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"== FAIL ==");
            sb.AppendLine(ex.ToString());
            Progress($"异常: {ex}");
        }
        finally
        {
            Progress("停止引擎...");
            try { await engine.StopAsync(); }
            catch (Exception ex) { Progress($"StopAsync 异常: {ex.Message}"); }
            Progress("清理目录...");
            try { if (Directory.Exists(root)) Directory.Delete(root, true); }
            catch { }

            lock (recent)
            {
                sb.AppendLine();
                sb.AppendLine("--- 引擎日志(节选) ---");
                foreach (var line in recent) sb.AppendLine(line);
            }
            Progress("完成");
        }
        return sb.ToString();
    }

    private static void Assert(StringBuilder sb, string name, bool condition, string? detail = null)
    {
        sb.AppendLine($"[{(condition ? "OK" : "FAIL")}] {name}{(detail != null ? " | " + detail : "")}");
    }

    private static string CreateFixture(string baseDir)
    {
        string root = Path.Combine(baseDir, "mpm_fixture");
        Directory.CreateDirectory(root);

        string saves = Path.Combine(root, "saves");
        Directory.CreateDirectory(saves);

        // usercache.json
        string usercache = "[{\"name\":\"Steve\",\"uuid\":\"" + Uuid + "\",\"expiresOn\":\"2030-01-01T00:00:00.000Z\"}]";
        File.WriteAllText(Path.Combine(root, "usercache.json"), usercache, Encoding.UTF8);

        string world = Path.Combine(saves, "WorldOne");
        Directory.CreateDirectory(Path.Combine(world, "advancements"));
        Directory.CreateDirectory(Path.Combine(world, "playerdata"));
        Directory.CreateDirectory(Path.Combine(world, "stats"));

        File.WriteAllText(Path.Combine(world, "advancements", Uuid + ".json"), "{}");
        File.WriteAllText(Path.Combine(world, "playerdata", Uuid + ".dat"), "{}");
        File.WriteAllText(Path.Combine(world, "playerdata", Uuid + "_old.dat"), "{}");
        File.WriteAllText(Path.Combine(world, "playerdata", Uuid + ".cosa"), "{}");
        File.WriteAllText(Path.Combine(world, "stats", Uuid + ".json"), "{}");

        Directory.CreateDirectory(Path.Combine(saves, "EmptyWorld"));
        return root;
    }
}
