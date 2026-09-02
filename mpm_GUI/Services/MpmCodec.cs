using mpm_GUI.Models;

namespace mpm_GUI.Services;

/// <summary>StructData 载荷反序列化（对齐 C++ Struct.h 各 Serialize 顺序）。</summary>
internal static class MpmCodec
{
    /// <summary>WDNL：存档路径列表与名称列表。</summary>
    public static List<WorldEntry> ParseWorldDirectoriesNameList(byte[] data)
    {
        int off = 0;
        int dirCount = Bin.ReadI32(data, ref off);
        int nameCount = Bin.ReadI32(data, ref off);

        var dirs = new List<string>(Math.Max(0, dirCount));
        for (int i = 0; i < dirCount; i++) dirs.Add(Bin.ReadString(data, ref off));

        var names = new List<string>(Math.Max(0, nameCount));
        for (int i = 0; i < nameCount; i++) names.Add(Bin.ReadString(data, ref off));

        var result = new List<WorldEntry>();
        for (int i = 0; i < Math.Min(dirs.Count, names.Count); i++)
            result.Add(new WorldEntry(names[i], dirs[i]));
        return result;
    }

    /// <summary>UI：玩家信息列表（name/uuid/expiresOn）。</summary>
    public static List<PlayerEntry> ParseUserInfoList(byte[] data)
    {
        int off = 0;
        int count = Bin.ReadI32(data, ref off);
        var result = new List<PlayerEntry>(Math.Max(0, count));
        for (int i = 0; i < count; i++)
        {
            string name = Bin.ReadString(data, ref off);
            string uuid = Bin.ReadString(data, ref off);
            string expires = Bin.ReadString(data, ref off);
            result.Add(new PlayerEntry(name, uuid, expires));
        }
        return result;
    }

    /// <summary>PIWIL：仅提取 playerinworldinfo_list 为展示行。</summary>
    public static PlayerInWorldInfoList ParsePlayerInWorldInfoList(byte[] data)
    {
        int off = 0;

        int advCount = Bin.ReadI32(data, ref off);
        for (int i = 0; i < advCount; i++)
        {
            Bin.ReadString(data, ref off); // path
            Bin.ReadString(data, ref off); // uuid
        }

        int pdCount = Bin.ReadI32(data, ref off);
        for (int i = 0; i < pdCount; i++)
        {
            Bin.ReadString(data, ref off); // dat_path
            Bin.ReadString(data, ref off); // dat_old_path
            Bin.ReadString(data, ref off); // cosarmor_path
            Bin.ReadString(data, ref off); // uuid
            Bin.ReadString(data, ref off); // old_uuid
            Bin.ReadString(data, ref off); // cosarmor_uuid
        }

        int stCount = Bin.ReadI32(data, ref off);
        for (int i = 0; i < stCount; i++)
        {
            Bin.ReadString(data, ref off); // path
            Bin.ReadString(data, ref off); // uuid
        }

        int rowCount = Bin.ReadI32(data, ref off);
        var rows = new List<PlayerInWorldPresence>(Math.Max(0, rowCount));
        for (int i = 0; i < rowCount; i++)
        {
            // world_dir_name
            string wdir = Bin.ReadString(data, ref off);
            string wname = Bin.ReadString(data, ref off);
            // player
            string pname = Bin.ReadString(data, ref off);
            string puuid = Bin.ReadString(data, ref off);
            string _ = Bin.ReadString(data, ref off); // expiresOn

            string adv = Bin.ReadString(data, ref off);
            string pd = Bin.ReadString(data, ref off);
            string pdOld = Bin.ReadString(data, ref off);
            string cos = Bin.ReadString(data, ref off);
            string st = Bin.ReadString(data, ref off);

            bool Has(string s) => s.Length > 0 && s != "无";

            rows.Add(new PlayerInWorldPresence(
                wname, wdir, pname, puuid,
                Has(adv), Has(pd), Has(pdOld), Has(cos), Has(st)));
        }

        return new PlayerInWorldInfoList(rows);
    }
}
